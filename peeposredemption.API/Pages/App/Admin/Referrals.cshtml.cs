using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App.Admin;

public class ReferralsModel : PageModel
{
    private readonly IUnitOfWork _uow;
    private readonly string _adminEmail;

    public ReferralsModel(IUnitOfWork uow, IConfiguration config)
    {
        _uow = uow;
        _adminEmail = config["Email:AdminEmail"] ?? string.Empty;
    }

    public List<ReferralSummary> Summaries { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!IsAdmin()) return Forbid();

        var codes = await _uow.Referrals.GetAllCodesAsync();

        foreach (var code in codes)
        {
            var signups = await _uow.Referrals.GetReferredUserCountAsync(code.Id);
            Summaries.Add(new ReferralSummary
            {
                OwnerUsername = code.Owner?.Username ?? "—",
                Code = code.Code,
                LinkCopies = code.LinkCopies,
                LinkClicks = code.LinkClicks,
                Signups = signups,
                Purchases = code.Purchases?.ToList() ?? new()
            });
        }

        Summaries = Summaries.OrderByDescending(s => s.TotalRevenueCents).ToList();
        return Page();
    }

    private bool IsAdmin()
    {
        var emailClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        return !string.IsNullOrEmpty(_adminEmail) &&
               string.Equals(emailClaim, _adminEmail, StringComparison.OrdinalIgnoreCase);
    }

    public class ReferralSummary
    {
        public string OwnerUsername { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int LinkCopies { get; set; }
        public int LinkClicks { get; set; }
        public int Signups { get; set; }
        public List<ReferralPurchase> Purchases { get; set; } = new();
        public long TotalRevenueCents => Purchases.Sum(p => p.AmountCents);
        public long CommissionCents => (long)(TotalRevenueCents * 0.20);
    }
}
