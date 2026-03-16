using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
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
        public ChatHub(IMediator mediator, IUnitOfWork uow) { _mediator = mediator; _uow = uow; }

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
    }

}