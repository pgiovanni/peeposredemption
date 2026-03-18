using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.Features.Auth.Commands;
using peeposredemption.Application.Features.Security.Commands;
using peeposredemption.API.Infrastructure;

namespace peeposredemption.API.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly IMediator _mediator;
        public RegisterModel(IMediator mediator) => _mediator = mediator;

        [BindProperty] public RegisterCommand Input { get; set; }
        [BindProperty] public string? ConfirmPassword { get; set; }
        [BindProperty] public string? RefCode { get; set; }

        public void OnGet(string? @ref = null)
        {
            RefCode = @ref;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            if (Input.Password != ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
                return Page();
            }

            try
            {
                var cmd = Input with { ReferralCode = RefCode };
                var userId = await _mediator.Send(cmd);

                // Record IP + device for security tracking
                var ip = IpBanMiddleware.GetClientIp(HttpContext) ?? "unknown";
                var deviceId = HttpContext.Items["DeviceId"] is Guid d ? d : Guid.Empty;
                await _mediator.Send(new RecordUserLoginInfoCommand(userId, ip, deviceId));

                return RedirectToPage("/Auth/CheckEmail");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }
    }
}
