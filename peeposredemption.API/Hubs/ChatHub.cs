using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using peeposredemption.Application.Features.Messages.Commands;
using System.Security.Claims;


namespace peeposredemption.API.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IMediator _mediator;
        public ChatHub(IMediator mediator) => _mediator = mediator;

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
        }

        public async Task SendDirectMessage(Guid recipientId, string content)
        {
            var payload = new { SenderId = CurrentUserId, Content = content, SentAt = DateTime.UtcNow };
            await Clients.User(recipientId.ToString()).SendAsync("ReceiveDirectMessage", payload);
            await Clients.Caller.SendAsync("ReceiveDirectMessage", payload);
        }

        public async Task TypingInChannel(Guid channelId) =>
            await Clients.OthersInGroup($"channel:{channelId}")
                .SendAsync("UserTyping", CurrentUserId);

        public async Task TypingInDm(Guid recipientId) =>
            await Clients.User(recipientId.ToString())
                .SendAsync("UserTypingDm", CurrentUserId);
    }

}