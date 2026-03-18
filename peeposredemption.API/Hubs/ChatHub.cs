using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using peeposredemption.API.Infrastructure;
using peeposredemption.Application.Features.Game.Commands;
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
        public ChatHub(IMediator mediator, IUnitOfWork uow, VoiceStateTracker voiceTracker)
        {
            _mediator = mediator;
            _uow = uow;
            _voiceTracker = voiceTracker;
        }

        private Guid CurrentUserId =>
            Guid.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        private string CurrentUsername =>
            Context.User!.FindFirst(ClaimTypes.Name)!.Value;

        private async Task<string> GetDisplayNameAsync()
        {
            var user = await _uow.Users.GetByIdAsync(CurrentUserId);
            return user?.DisplayOrUsername ?? CurrentUsername;
        }

        public async Task JoinChannel(Guid serverId, Guid channelId) =>
            await Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{channelId}");

        public async Task SendChannelMessage(Guid channelId, string content)
        {
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

            var dto = await _mediator.Send(
                new SendMessageCommand(channelId, CurrentUserId, displayName, content));
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
            var removed = _voiceTracker.Leave(channelId, CurrentUserId);
            if (removed == null) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"voice:{channelId}");
            await Clients.Group($"voice:{channelId}")
                .SendAsync("VoiceUserLeft", new { UserId = CurrentUserId });

            // Update sidebar
            var remaining = _voiceTracker.GetParticipants(channelId);
            await Clients.Group($"channel:{channelId}")
                .SendAsync("VoiceChannelState", new { ChannelId = channelId, Participants = remaining.Select(p => new { p.UserId, p.DisplayName, p.AvatarUrl }) });
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

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var removed = _voiceTracker.LeaveByConnectionId(Context.ConnectionId);
            foreach (var (channelId, participant) in removed)
            {
                await Clients.Group($"voice:{channelId}")
                    .SendAsync("VoiceUserLeft", new { participant.UserId });

                var remaining = _voiceTracker.GetParticipants(channelId);
                await Clients.Group($"channel:{channelId}")
                    .SendAsync("VoiceChannelState", new { ChannelId = channelId, Participants = remaining.Select(p => new { p.UserId, p.DisplayName, p.AvatarUrl }) });
            }
            await base.OnDisconnectedAsync(exception);
        }
    }

}