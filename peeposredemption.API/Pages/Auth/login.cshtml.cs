using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using peeposredemption.Application.Features.Auth.Commands;
using peeposredemption.Application.Features.Security.Commands;
using peeposredemption.API.Infrastructure;

namespace peeposredemption.API.Pages.Auth
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class LoginModel : PageModel
    {
        private readonly IMediator _mediator;
        private readonly IMemoryCache _cache;

        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        public LoginModel(IMediator mediator, IMemoryCache cache)
        {
            _mediator = mediator;
            _cache = cache;
        }

        [BindProperty] public LoginCommand Input { get; set; }

        public IActionResult OnGet([FromQuery] bool addAccount = false)
        {
            if (User.Identity?.IsAuthenticated == true && !addAccount)
                return RedirectToPage("/App/Index");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var ip = IpBanMiddleware.GetClientIp(HttpContext) ?? "unknown";
            var failKey = $"login_fail:{ip}";

            if (_cache.TryGetValue(failKey, out int attempts) && attempts >= MaxFailedAttempts)
            {
                ModelState.AddModelError(string.Empty, "Too many failed attempts. Try again in 15 minutes.");
                return Page();
            }

            try
            {
                var ua = Request.Headers.UserAgent.ToString();
                var deviceId = HttpContext.Items["DeviceId"] is Guid d2 ? d2 : (Guid?)null;
                var cmd = Input with { IpAddress = ip, UserAgent = ua, DeviceId = deviceId };
                var result = await _mediator.Send(cmd);

                // Clear failure counter on success
                _cache.Remove(failKey);

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
                    Domain = ".torvex.app", MaxAge = TimeSpan.FromMinutes(15)
                });
                Response.Cookies.Append("refreshToken", result.RefreshToken!, new CookieOptions
                {
                    HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict,
                    Domain = ".torvex.app", MaxAge = TimeSpan.FromDays(30)
                });

                // Record IP + device for security tracking
                await _mediator.Send(new RecordUserLoginInfoCommand(result.UserId, ip, deviceId ?? Guid.Empty));

                return RedirectToPage("/App/Index");
            }
            catch (UnauthorizedAccessException ex)
            {
                // Increment failure counter
                var current = _cache.GetOrCreate(failKey, e =>
                {
                    e.AbsoluteExpirationRelativeToNow = LockoutDuration;
                    return 0;
                });
                _cache.Set(failKey, current + 1, LockoutDuration);

                var remaining = MaxFailedAttempts - (current + 1);
                var message = remaining > 0
                    ? $"{ex.Message} ({remaining} attempt{(remaining == 1 ? "" : "s")} remaining)"
                    : "Too many failed attempts. Try again in 15 minutes.";

                ModelState.AddModelError(string.Empty, message);
                return Page();
            }
        }
    }

}
