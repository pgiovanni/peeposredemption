using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.DTOs.Servers;
using peeposredemption.Application.Features.Servers.Queries;
using peeposredemption.Application.Features.Channels.Queries;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App
{
    public class IndexModel : PageModel
    {
        private readonly IMediator _mediator;

        public IndexModel(IMediator mediator) => _mediator = mediator;

        public List<ServerDto> Servers { get; set; } = new();
        public Dictionary<Guid, Guid> ServerDefaultChannels { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return RedirectToPage("/Auth/Login");

            var userId = Guid.Parse(userIdClaim);
            Servers = await _mediator.Send(new GetUserServersQuery(userId));

            foreach (var server in Servers)
            {
                var channels = await _mediator.Send(new GetServerChannelsQuery(server.Id));
                var defaultChannel = channels.FirstOrDefault();
                if (defaultChannel != null)
                    ServerDefaultChannels[server.Id] = defaultChannel.Id;
            }

            return Page();
        }
    }
}
