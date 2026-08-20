using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using peeposredemption.API.Infrastructure;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.API.Pages
{
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class ContactModel : PageModel
    {
        private readonly IUnitOfWork _uow;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<ContactModel> _logger;

        public ContactModel(IUnitOfWork uow, IEmailService emailService, IMemoryCache cache,
            IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<ContactModel> logger)
        {
            _uow = uow;
            _emailService = emailService;
            _cache = cache;
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
        }

        [BindProperty, Required(ErrorMessage = "Please tell me your name."), MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [BindProperty, Required(ErrorMessage = "Please include an email so I can get back to you."), EmailAddress(ErrorMessage = "That email doesn't look right."), MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [BindProperty, MaxLength(200)]
        public string? Company { get; set; }

        [BindProperty]
        public string? Package { get; set; }

        [BindProperty, Required(ErrorMessage = "Please describe what you need."), MaxLength(4000)]
        public string Message { get; set; } = string.Empty;

        public bool Sent { get; set; }

        public void OnGet(string? package = null)
        {
            if (ServiceCatalog.IsValidName(package)) Package = package;
            ViewData["TurnstileSiteKey"] = _config["Turnstile:SiteKey"] ?? "";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ViewData["TurnstileSiteKey"] = _config["Turnstile:SiteKey"] ?? "";

            if (!ModelState.IsValid) return Page();

            // Verify Turnstile CAPTCHA (same flow as registration)
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

            // Rate limit: max 5 inquiries per IP per 24h
            var ip = IpBanMiddleware.GetClientIp(HttpContext) ?? "unknown";
            var cacheKey = $"contact_ip_{ip}";
            var count = _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                return 0;
            });
            if (count >= 5)
            {
                ModelState.AddModelError(string.Empty, "Too many messages from this location today. Please try again tomorrow or email admin@torvex.app directly.");
                return Page();
            }
            _cache.Set(cacheKey, count + 1, TimeSpan.FromHours(24));

            var lead = new Lead
            {
                Name = Name.Trim(),
                Email = Email.Trim(),
                Company = string.IsNullOrWhiteSpace(Company) ? null : Company.Trim(),
                Package = ServiceCatalog.IsValidName(Package) ? Package : null,
                Message = Message.Trim(),
                IpAddress = ip
            };
            await _uow.Leads.AddAsync(lead);
            await _uow.SaveChangesAsync();

            // The lead is saved either way — a mail hiccup must not lose the inquiry
            try
            {
                await _emailService.SendLeadNotificationAsync(lead.Name, lead.Email, lead.Company, lead.Package, lead.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lead {LeadId} saved but notification email failed", lead.Id);
            }

            Sent = true;
            ModelState.Clear();
            Name = Email = Message = string.Empty;
            Company = null;
            Package = null;
            return Page();
        }
    }
}
