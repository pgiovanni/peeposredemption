using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.DTOs.Messages;
using peeposredemption.Application.Features.Messages.Queries;

namespace peeposredemption.API.Pages.App
{
    public class ChannelModel : PageModel
    {
        private readonly IMediator _mediator;
        public ChannelModel(IMediator mediator) => _mediator = mediator;

        public Guid ChannelId { get; set; }
        public List<MessageDto> Messages { get; set; }

        public async Task OnGetAsync(Guid channelId)
        {
            ChannelId = channelId;
            Messages = await _mediator.Send(new GetChannelMessagesQuery(channelId));
        }
    }

}
