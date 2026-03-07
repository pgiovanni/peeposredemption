using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.Features.Servers.Commands;
using peeposredemption.Application.Features.Servers.Queries;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App
{
    public class InviteModel : PageModel
    {
        private readonly IMediator _mediator;
        public InviteModel(IMediator mediator) => _mediator = mediator;

        [BindProperty(SupportsGet = true)]
        public string Code { get; set; }

        public string ServerName { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            System.Diagnostics.Debugger.Break(); // BP: invite page loaded, Code in URL

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return RedirectToPage("/Auth/Login");

            // Peek at the invite to show the server name before they accept
            var invite = await _mediator.Send(new PeekInviteQuery(Code));
            if (invite == null) return NotFound();

            ServerName = invite.ServerName;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return RedirectToPage("/Auth/Login");

            var userId = Guid.Parse(userIdClaim);
            var serverId = await _mediator.Send(new JoinServerCommand(Code, userId));

            System.Diagnostics.Debugger.Break(); // BP: join complete, redirecting to server

            return RedirectToPage("/App/Index");
        }
    }
}
