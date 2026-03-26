using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using peeposredemption.Application.Features.Auth.Commands;
using peeposredemption.Application.Features.Security.Commands;
using peeposredemption.API.Infrastructure;
using System.Text.Json;

namespace peeposredemption.API.Pages.Auth
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class RegisterModel : PageModel
    {
        private readonly IMediator _mediator;
        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public RegisterModel(IMediator mediator, IMemoryCache cache, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _mediator = mediator;
            _cache = cache;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        [BindProperty] public RegisterCommand Input { get; set; }
        [BindProperty] public string? ConfirmPassword { get; set; }
        [BindProperty] public string? RefCode { get; set; }
        [BindProperty] public string? InviteCode { get; set; }

        public void OnGet(string? @ref = null, string? invite = null)
        {
            RefCode = @ref;
            InviteCode = invite;
            ViewData["TurnstileSiteKey"] = _config["Turnstile:SiteKey"] ?? "";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ViewData["TurnstileSiteKey"] = _config["Turnstile:SiteKey"] ?? "";

            if (!ModelState.IsValid) return Page();

            // Verify Turnstile CAPTCHA
            var turnstileToken = Request.Form["cf-turnstile-response"].ToString();
            var turnstileSecret = _config["Turnstile:SecretKey"] ?? "";
            if (!string.IsNullOrEmpty(turnstileSecret))
            {
                var client = _httpClientFactory.CreateClient();
                var resp = await client.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify",
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["secret"] = turnstileSecret,
                        ["response"] = turnstileToken,
                        ["remoteip"] = IpBanMiddleware.GetClientIp(HttpContext) ?? ""
                    }));
                var json = await resp.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.GetProperty("success").GetBoolean())
                {
                    ModelState.AddModelError(string.Empty, "Please complete the CAPTCHA.");
                    return Page();
                }
            }

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
