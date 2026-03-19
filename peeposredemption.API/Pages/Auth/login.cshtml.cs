using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.Features.Auth.Commands;
using peeposredemption.Application.Features.Security.Commands;
using peeposredemption.API.Infrastructure;

namespace peeposredemption.API.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly IMediator _mediator;
        public LoginModel(IMediator mediator) => _mediator = mediator;

        [BindProperty] public LoginCommand Input { get; set; }

        public IActionResult OnGet() =>
            User.Identity?.IsAuthenticated == true ? RedirectToPage("/App/Index") : Page();

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();
            try
            {
                var ip = IpBanMiddleware.GetClientIp(HttpContext) ?? "unknown";
                var ua = Request.Headers.UserAgent.ToString();
                var deviceId = HttpContext.Items["DeviceId"] is Guid d2 ? d2 : (Guid?)null;
                var cmd = Input with { IpAddress = ip, UserAgent = ua, DeviceId = deviceId };
                var result = await _mediator.Send(cmd);

                if (result.RequiresMfa)
                {
                    Response.Cookies.Append("mfa_pending", result.MfaPendingToken!, new CookieOptions
                    {
                        HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict,
                        MaxAge = TimeSpan.FromMinutes(5)
                    });
                    return RedirectToPage("/Auth/MfaVerify");
                }

                Response.Cookies.Append("jwt", result.Token!, new CookieOptions
                {
                    HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict,
                    MaxAge = TimeSpan.FromMinutes(15)
                });
                Response.Cookies.Append("refreshToken", result.RefreshToken!, new CookieOptions
                {
                    HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict,
                    MaxAge = TimeSpan.FromDays(30)
                });

                // Record IP + device for security tracking
                await _mediator.Send(new RecordUserLoginInfoCommand(result.UserId, ip, deviceId ?? Guid.Empty));

                return RedirectToPage("/App/Index");
            }
            catch (UnauthorizedAccessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }
    }

}
