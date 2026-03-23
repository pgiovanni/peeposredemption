using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using peeposredemption.Application.Features.Auth.Commands;
using peeposredemption.Application.Features.Security.Commands;
using peeposredemption.API.Infrastructure;

namespace peeposredemption.API.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly IMediator _mediator;
        private readonly IMemoryCache _cache;
        public RegisterModel(IMediator mediator, IMemoryCache cache)
        {
            _mediator = mediator;
            _cache = cache;
        }

        [BindProperty] public RegisterCommand Input { get; set; }
        [BindProperty] public string? ConfirmPassword { get; set; }
        [BindProperty] public string? RefCode { get; set; }
        [BindProperty] public string? InviteCode { get; set; }

        public void OnGet(string? @ref = null, string? invite = null)
        {
            RefCode = @ref;
            InviteCode = invite;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            // Rate limit: max 3 registrations per IP per 24h
            var ip = IpBanMiddleware.GetClientIp(HttpContext) ?? "unknown";
            var cacheKey = $"reg_ip_{ip}";
            var count = _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                return 0;
            });
            if (count >= 3)
            {
                ModelState.AddModelError(string.Empty, "Too many accounts created from this location. Please try again tomorrow.");
                return Page();
            }
            _cache.Set(cacheKey, count + 1, TimeSpan.FromHours(24));

            if (Input.Password != ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
                return Page();
            }

            try
            {
                var cmd = Input with { ReferralCode = RefCode, InviteCode = InviteCode };
                var userId = await _mediator.Send(cmd);

                // Record IP + device for security tracking
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
