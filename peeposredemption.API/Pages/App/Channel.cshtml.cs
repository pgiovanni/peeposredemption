using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.DTOs.Messages;
using peeposredemption.Application.Features.Messages.Queries;
using peeposredemption.Application.Features.Servers.Commands;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App
{
    public class ChannelModel : PageModel
    {
        private readonly IMediator _mediator;
        public ChannelModel(IMediator mediator) => _mediator = mediator;

        public Guid ChannelId { get; set; }
        public Guid ServerId { get; set; }
        public List<MessageDto> Messages { get; set; }
        public string InviteLink { get; set; }

        public async Task OnGetAsync(Guid channelId, Guid serverId)
        {
            ChannelId = channelId;
            ServerId = serverId;
            Messages = await _mediator.Send(new GetChannelMessagesQuery(channelId));

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim != null)
            {
                var code = await _mediator.Send(new CreateInviteCommand(serverId, Guid.Parse(userIdClaim)));
                InviteLink = $"{Request.Scheme}://{Request.Host}/App/Invite/{code}";
            }
        }
    }
}
