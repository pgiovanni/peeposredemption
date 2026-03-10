using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using peeposredemption.Application.Features.Messages.Commands;
using peeposredemption.Application.Features.Moderation.Commands;
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

        public async Task JoinChannel(Guid serverId, Guid channelId) =>
            await Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{channelId}");

        public async Task SendChannelMessage(Guid channelId, string content)
        {
            var dto = await _mediator.Send(
                new SendMessageCommand(channelId, CurrentUserId, CurrentUsername, content));
            await Clients.Group($"channel:{channelId}")
                .SendAsync("ReceiveChannelMessage", dto);

            // Detect @mentions and send notifications
            var mentions = Regex.Matches(content, @"@([a-zA-Z0-9_]+)")
                .Select(m => m.Groups[1].Value)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var username in mentions)
            {
                if (string.Equals(username, CurrentUsername, StringComparison.OrdinalIgnoreCase)) continue;
                var mentionedUser = await _uow.Users.GetByUsernameAsync(username);
                if (mentionedUser == null) continue;

                // Check they're a member of the server
                var channel = await _uow.Channels.GetByIdAsync(channelId);
                if (channel == null) continue;

                var notification = new Notification
                {
                    UserId = mentionedUser.Id,
                    FromUserId = CurrentUserId,
                    Type = NotificationType.Ping,
                    Content = $"{CurrentUsername} mentioned you in #{channel.Name}",
                    ChannelId = channelId,
                    ServerId = channel.ServerId
                };
                await _uow.Notifications.AddAsync(notification);
                await _uow.SaveChangesAsync();

                // Push real-time notification to the mentioned user
                await Clients.User(mentionedUser.Id.ToString())
                    .SendAsync("ReceiveNotification", new { notification.Content, notification.Id });
            }
        }

        public async Task SendDirectMessage(Guid recipientId, string content)
        {
            var dto = await _mediator.Send(
                new SendDirectMessageCommand(CurrentUserId, recipientId, content));
            var payload = new { dto.SenderId, dto.Content, dto.SentAt };
            await Clients.User(recipientId.ToString()).SendAsync("ReceiveDirectMessage", payload);
            await Clients.Caller.SendAsync("ReceiveDirectMessage", payload);
        }

        public async Task DeleteChannelMessage(Guid serverId, Guid channelId, Guid messageId)
        {
            await _mediator.Send(new DeleteMessageCommand(messageId, serverId, CurrentUserId));
            await Clients.Group($"channel:{channelId}").SendAsync("MessageDeleted", messageId);
        }

        public async Task TypingInChannel(Guid channelId) =>
            await Clients.OthersInGroup($"channel:{channelId}")
                .SendAsync("UserTyping", CurrentUserId);

        public async Task TypingInDm(Guid recipientId) =>
            await Clients.User(recipientId.ToString())
                .SendAsync("UserTypingDm", CurrentUserId);
    }

}