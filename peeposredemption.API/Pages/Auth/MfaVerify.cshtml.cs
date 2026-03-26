using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.Features.Auth.Commands;
using peeposredemption.Application.Features.Security.Commands;
using peeposredemption.API.Infrastructure;

namespace peeposredemption.API.Pages.Auth;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class MfaVerifyModel : PageModel
{
    private readonly IMediator _mediator;
    public MfaVerifyModel(IMediator mediator) => _mediator = mediator;

    [BindProperty] public string Code { get; set; } = "";
    [BindProperty] public bool UseRecovery { get; set; }

    public IActionResult OnGet(bool useRecovery = false)
    {
        if (string.IsNullOrEmpty(Request.Cookies["mfa_pending"]))
            return RedirectToPage("/Auth/Login");

        UseRecovery = useRecovery;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var pendingToken = Request.Cookies["mfa_pending"];
        if (string.IsNullOrEmpty(pendingToken))
        {
            ModelState.AddModelError(string.Empty, "MFA session expired. Please log in again.");
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Code))
        {
            ModelState.AddModelError(string.Empty, "Please enter a verification code.");
            return Page();
        }

        try
        {
            var ip = IpBanMiddleware.GetClientIp(HttpContext) ?? "unknown";
            var ua = Request.Headers.UserAgent.ToString();
            var deviceId = HttpContext.Items["DeviceId"] is Guid d2 ? d2 : (Guid?)null;
            var result = await _mediator.Send(new VerifyMfaCommand(pendingToken, Code, ip, ua, deviceId));

            // Clear MFA pending cookie
            Response.Cookies.Delete("mfa_pending");

            // Set auth cookies
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
