using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using peeposredemption.API.Infrastructure;
using peeposredemption.Application.Features.Game.Commands;
using peeposredemption.Application.DTOs.Messages;
using peeposredemption.Application.Features.Messages.Commands;
using peeposredemption.Application.Features.Moderation.Commands;
using peeposredemption.Application.Features.Orbs.Commands;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;
using System.Security.Claims;
using System.Text.RegularExpressions;


namespace peeposredemption.API.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _uow;
        private readonly VoiceStateTracker _voiceTracker;
        private readonly PresenceTracker _presenceTracker;
        private readonly ILogger<ChatHub> _logger;
        public ChatHub(IMediator mediator, IUnitOfWork uow, VoiceStateTracker voiceTracker, PresenceTracker presenceTracker, ILogger<ChatHub> logger)
        {
            _mediator = mediator;
            _uow = uow;
            _voiceTracker = voiceTracker;
            _presenceTracker = presenceTracker;
            _logger = logger;
        }

        private Guid CurrentUserId
        {
            get
            {
                var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(claim, out var id))
                    throw new HubException("Invalid user context.");
                return id;
            }
        }

        private string CurrentUsername =>
            Context.User!.FindFirst(ClaimTypes.Name)!.Value;

        private async Task<string> GetDisplayNameAsync()
        {
            var user = await _uow.Users.GetByIdAsync(CurrentUserId);
            return user?.DisplayOrUsername ?? CurrentUsername;
        }

        public async Task JoinChannel(Guid serverId, Guid channelId)
        {
            var channel = await _uow.Channels.GetByIdAsync(channelId);
            if (channel == null || channel.ServerId != serverId)
                throw new HubException("Channel not found.");
            var isMember = await _uow.Servers.IsMemberAsync(serverId, CurrentUserId);
            if (!isMember)
                throw new HubException("You are not a member of this server.");
            await Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{channelId}");
        }

        public async Task SendChannelMessage(Guid channelId, string content, Guid? replyToMessageId = null)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Message content cannot be empty.");

            // Slash command interception for RPG game system
            if (content.StartsWith("/"))
            {
                var config = await _uow.GameChannelConfigs.GetByChannelIdAsync(channelId);
                if (config is not { GameBotMuted: true })
                {
                    var result = await _mediator.Send(new ProcessGameCommandRequest(
                        CurrentUserId, CurrentUsername, channelId, content));
                    if (result.Handled)
                    {
                        foreach (var response in result.Responses)
                        {
                            if (response.BroadcastToChannel)
                                await Clients.Group($"channel:{channelId}")
                                    .SendAsync("ReceiveGameMessage", new { type = response.Type, payload = response.Payload });
                            else
                                await Clients.Caller
                                    .SendAsync("ReceiveGameMessage", new { type = response.Type, payload = response.Payload });
                        }
                        return;
                    }
                }
            }

            var displayName = await GetDisplayNameAsync();

            MessageDto dto;
            try
            {
                dto = await _mediator.Send(
                    new SendMessageCommand(channelId, CurrentUserId, displayName, content, replyToMessageId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendMessageCommand failed: channel={ChannelId} content=[{Content}]", channelId, content);
                throw;
            }
            await Clients.Group($"channel:{channelId}")
                .SendAsync("ReceiveChannelMessage", dto);

            // Award orbs for message activity
            await _mediator.Send(new RecordMessageOrbRewardCommand(CurrentUserId));

            // Detect @everyone and individual @mentions, send notifications
            var channel = await _uow.Channels.GetByIdAsync(channelId);
            if (channel != null)
            {
                var hasEveryone = Regex.IsMatch(content, @"@everyone\b", RegexOptions.IgnoreCase);
                if (hasEveryone)
                {
                    // Only server owner can use @everyone
                    var senderRole = await _uow.Servers.GetMemberRoleAsync(channel.ServerId, CurrentUserId);
                    if (senderRole != null && senderRole.Value == ServerRole.Owner)
                    {
                        var allMembers = await _uow.Servers.GetServerMembersAsync(channel.ServerId);
                        foreach (var member in allMembers)
                        {
                            if (member.UserId == CurrentUserId) continue;
                            var notification = new Notification
                            {
                                UserId = member.UserId,
                                FromUserId = CurrentUserId,
                                Type = NotificationType.Ping,
                                Content = $"{displayName} pinged @everyone in #{channel.Name}",
                                ChannelId = channelId,
                                ServerId = channel.ServerId
                            };
                            await _uow.Notifications.AddAsync(notification);
                            await _uow.SaveChangesAsync();
                            await Clients.User(member.UserId.ToString())
                                .SendAsync("ReceiveNotification", new { notification.Content, notification.Id, ServerId = channel.ServerId });
                        }
                    }
                }

                if (!hasEveryone)
                {
                    var mentions = Regex.Matches(content, @"@([a-zA-Z0-9_]+)")
                        .Select(m => m.Groups[1].Value)
                        .Distinct(StringComparer.OrdinalIgnoreCase);

                    foreach (var username in mentions)
                    {
                        var mentionedUser = await _uow.Users.GetByUsernameAsync(username);
                        if (mentionedUser == null) continue;

                        var notification = new Notification
                        {
                            UserId = mentionedUser.Id,
                            FromUserId = CurrentUserId,
                            Type = NotificationType.Ping,
                            Content = $"{displayName} mentioned you in #{channel.Name}",
                            ChannelId = channelId,
                            ServerId = channel.ServerId
                        };
                        await _uow.Notifications.AddAsync(notification);
                        await _uow.SaveChangesAsync();
                        await Clients.User(mentionedUser.Id.ToString())
                            .SendAsync("ReceiveNotification", new { notification.Content, notification.Id, ServerId = channel.ServerId });
                    }
                }
            }
        }

        public async Task SendDirectMessage(Guid recipientId, string content)
        {
            var dto = await _mediator.Send(
                new SendDirectMessageCommand(CurrentUserId, recipientId, content));
            var senderDisplay = await GetDisplayNameAsync();
            var payload = new { dto.SenderId, SenderUsername = senderDisplay, dto.Content, dto.SentAt };
            await Clients.User(recipientId.ToString()).SendAsync("ReceiveDirectMessage", payload);
            await Clients.Caller.SendAsync("ReceiveDirectMessage", payload);
        }

        public async Task DeleteChannelMessage(Guid serverId, Guid channelId, Guid messageId)
        {
            await _mediator.Send(new DeleteMessageCommand(messageId, serverId, CurrentUserId));
            await Clients.Group($"channel:{channelId}").SendAsync("MessageDeleted", messageId);
        }

        public async Task SendOrbGift(Guid channelId, Guid recipientId, long amount, string? message)
        {
            try
            {
                var channel = await _uow.Channels.GetByIdAsync(channelId);
                var result = await _mediator.Send(new SendOrbGiftCommand(
                    CurrentUserId, recipientId, amount, channelId, channel?.ServerId, message));

                var recipient = await _uow.Users.GetByIdAsync(recipientId);
                var senderDisplay = await GetDisplayNameAsync();
                var payload = new
                {
                    GiftId = result.GiftId,
                    SenderUsername = senderDisplay,
                    SenderId = CurrentUserId,
                    RecipientUsername = recipient?.DisplayOrUsername ?? "Unknown",
                    RecipientId = recipientId,
                    Amount = amount,
                    Message = message
                };

                // Broadcast to channel
                await Clients.Group($"channel:{channelId}").SendAsync("ReceiveOrbGift", payload);

                // Update sender's balance
                await Clients.Caller.SendAsync("OrbBalanceUpdated", result.SenderNewBalance);

                // Notify recipient with updated balance
                await Clients.User(recipientId.ToString()).SendAsync("OrbGiftReceived", payload);
                await Clients.User(recipientId.ToString()).SendAsync("OrbBalanceUpdated", result.RecipientNewBalance);
            }
            catch (InvalidOperationException ex)
            {
                await Clients.Caller.SendAsync("OrbGiftError", ex.Message);
            }
        }

        public async Task TypingInChannel(Guid channelId) =>
            await Clients.OthersInGroup($"channel:{channelId}")
                .SendAsync("UserTyping", CurrentUserId);

        public async Task TypingInDm(Guid recipientId) =>
            await Clients.User(recipientId.ToString())
                .SendAsync("UserTypingDm", CurrentUserId);

        // ── Voice Channel Methods ─────────────────────────────────

        public async Task JoinVoiceChannel(Guid channelId)
        {
            var channel = await _uow.Channels.GetByIdAsync(channelId);
            if (channel == null || channel.Type != ChannelType.Voice)
                throw new HubException("Not a voice channel.");
            var isMember = await _uow.Servers.IsMemberAsync(channel.ServerId, CurrentUserId);
            if (!isMember)
                throw new HubException("You are not a member of this server.");

            var user = await _uow.Users.GetByIdAsync(CurrentUserId);
            var displayName = user?.DisplayOrUsername ?? CurrentUsername;
            var avatarUrl = user?.AvatarUrl;

            var (success, participants) = _voiceTracker.TryJoin(
                channelId, CurrentUserId, displayName, avatarUrl, Context.ConnectionId);

            if (!success)
                throw new HubException("Voice channel is full (max 6).");

            await Groups.AddToGroupAsync(Context.ConnectionId, $"voice:{channelId}");

            // Tell everyone else in the channel
            await Clients.OthersInGroup($"voice:{channelId}")
                .SendAsync("VoiceUserJoined", new
                {
                    UserId = CurrentUserId,
                    DisplayName = displayName,
                    AvatarUrl = avatarUrl,
                    IsMuted = false,
                    IsDeafened = false,
                    IsCameraOn = false
                });

            // Return full participant list to caller
            await Clients.Caller.SendAsync("VoiceParticipantList", participants.Select(p => new
            {
                p.UserId, p.DisplayName, p.AvatarUrl, p.IsMuted, p.IsDeafened, p.IsCameraOn
            }));

            // Broadcast updated sidebar count to the text channel group too
            await Clients.Group($"channel:{channelId}")
                .SendAsync("VoiceChannelState", new { ChannelId = channelId, Participants = participants.Select(p => new { p.UserId, p.DisplayName, p.AvatarUrl }) });
        }

        public async Task LeaveVoiceChannel(Guid channelId)
        {
            // Check participant count BEFORE removing (for orb eligibility: 2+ people)
            var countBefore = _voiceTracker.GetParticipantCount(channelId);
            var removed = _voiceTracker.Leave(channelId, CurrentUserId);
            if (removed == null) return;

            await PersistVoiceSessionAsync(channelId, removed, countBefore);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"voice:{channelId}");
            await Clients.Group($"voice:{channelId}")
                .SendAsync("VoiceUserLeft", new { UserId = CurrentUserId });

            // Update sidebar
            var remaining = _voiceTracker.GetParticipants(channelId);
            await Clients.Group($"channel:{channelId}")
                .SendAsync("VoiceChannelState", new { ChannelId = channelId, Participants = remaining.Select(p => new { p.UserId, p.DisplayName, p.AvatarUrl }) });
        }

        private async Task PersistVoiceSessionAsync(Guid channelId, VoiceParticipant participant, int participantCountBefore)
        {
            var now = DateTime.UtcNow;
            var joinedAt = participant.JoinedAt == default ? now.AddMinutes(-1) : participant.JoinedAt;
            var durationMinutes = (now - joinedAt).TotalMinutes;

            // Get the server for this channel
            var channel = await _uow.Channels.GetByIdAsync(channelId);
            var serverId = channel?.ServerId ?? Guid.Empty;

            // Calculate orb rewards
            // Base: 1 orb per 5 min (unmuted, 2+ people). Camera on: 2 orbs per 5 min.
            long orbsEarned = 0;
            bool eligible = !participant.IsMuted && participantCountBefore >= 2;
            if (eligible && durationMinutes >= 1)
            {
                var intervals = (long)(durationMinutes / 5);
                var ratePerInterval = participant.IsCameraOn ? 2L : 1L;
                orbsEarned = intervals * ratePerInterval;

                // Daily cap: 200 orbs from VC
                if (orbsEarned > 0)
                {
                    var todayEarned = await _uow.VoiceSessions.GetTodayOrbsEarnedAsync(participant.UserId);
                    var remaining = Math.Max(0, 200 - todayEarned);
                    orbsEarned = Math.Min(orbsEarned, remaining);
                }

                // Credit to user's wallet
                if (orbsEarned > 0)
                {
                    var user = await _uow.Users.GetByIdAsync(participant.UserId);
                    if (user != null)
                    {
                        user.OrbBalance += orbsEarned;
                        var tx = new OrbTransaction
                        {
                            UserId = participant.UserId,
                            Amount = orbsEarned,
                            Type = OrbTransactionType.VoiceReward,
                            Description = $"VC reward: {durationMinutes:F0} min in voice"
                        };
                        await _uow.OrbTransactions.AddAsync(tx);
                    }
                }
            }

            // Persist the session record
            var session = new VoiceSession
            {
                UserId = participant.UserId,
                ChannelId = channelId,
                ServerId = serverId,
                JoinedAt = joinedAt,
                LeftAt = now,
                OrbsEarned = orbsEarned
            };
            await _uow.VoiceSessions.AddAsync(session);
            await _uow.SaveChangesAsync();
        }

        public async Task SendWebRtcOffer(string targetUserId, string sdp)
        {
            await Clients.User(targetUserId)
                .SendAsync("ReceiveWebRtcOffer", new { FromUserId = CurrentUserId.ToString(), Sdp = sdp });
        }

        public async Task SendWebRtcAnswer(string targetUserId, string sdp)
        {
            await Clients.User(targetUserId)
                .SendAsync("ReceiveWebRtcAnswer", new { FromUserId = CurrentUserId.ToString(), Sdp = sdp });
        }

        public async Task SendIceCandidate(string targetUserId, string candidate)
        {
            await Clients.User(targetUserId)
                .SendAsync("ReceiveIceCandidate", new { FromUserId = CurrentUserId.ToString(), Candidate = candidate });
        }

        public async Task UpdateVoiceState(Guid channelId, bool? muted, bool? deafened, bool? cameraOn)
        {
            _voiceTracker.UpdateState(channelId, CurrentUserId, muted, deafened, cameraOn);
            await Clients.OthersInGroup($"voice:{channelId}")
                .SendAsync("VoiceStateChanged", new
                {
                    UserId = CurrentUserId,
                    IsMuted = muted,
                    IsDeafened = deafened,
                    IsCameraOn = cameraOn
                });
        }

        public override async Task OnConnectedAsync()
        {
            var userId = CurrentUserId;
            var cameOnline = _presenceTracker.UserConnected(userId, Context.ConnectionId);

            // Join all server groups so we can receive ServerMemberOnline/Offline
            var servers = await _uow.Servers.GetUserServersAsync(userId);
            foreach (var server in servers)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"server:{server.Id}");

            if (cameOnline)
            {
                // Notify friends
                var friendRequests = await _uow.FriendRequests.GetAcceptedAsync(userId);
                var friendIds = friendRequests.Select(fr => fr.SenderId == userId ? fr.ReceiverId : fr.SenderId).Distinct();
                foreach (var friendId in friendIds)
                    await Clients.User(friendId.ToString()).SendAsync("UserOnline", userId);

                // Notify server members
                foreach (var server in servers)
                    await Clients.Group($"server:{server.Id}").SendAsync("ServerMemberOnline", userId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Voice cleanup — LeaveByConnectionId now returns count before removal for orb eligibility
            var removed = _voiceTracker.LeaveByConnectionId(Context.ConnectionId);
            foreach (var (channelId, participant, countBefore) in removed)
            {
                await PersistVoiceSessionAsync(channelId, participant, countBefore);

                await Clients.Group($"voice:{channelId}")
                    .SendAsync("VoiceUserLeft", new { participant.UserId });

                var remaining = _voiceTracker.GetParticipants(channelId);
                await Clients.Group($"channel:{channelId}")
                    .SendAsync("VoiceChannelState", new { ChannelId = channelId, Participants = remaining.Select(p => new { p.UserId, p.DisplayName, p.AvatarUrl }) });
            }

            // Presence cleanup
            var userId = CurrentUserId;
            var wentOffline = _presenceTracker.UserDisconnected(userId, Context.ConnectionId);

            if (wentOffline)
            {
                var friendRequests = await _uow.FriendRequests.GetAcceptedAsync(userId);
                var friendIds = friendRequests.Select(fr => fr.SenderId == userId ? fr.ReceiverId : fr.SenderId).Distinct();
                foreach (var friendId in friendIds)
                    await Clients.User(friendId.ToString()).SendAsync("UserOffline", userId);

                var servers = await _uow.Servers.GetUserServersAsync(userId);
                foreach (var server in servers)
                    await Clients.Group($"server:{server.Id}").SendAsync("ServerMemberOffline", userId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }

}