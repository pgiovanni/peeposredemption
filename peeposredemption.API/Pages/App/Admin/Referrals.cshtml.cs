using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.API.Infrastructure;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.API.Pages.App.Admin;

public class ReferralsModel : PageModel
{
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;

    public ReferralsModel(IUnitOfWork uow, IConfiguration config)
    {
        _uow = uow;
        _config = config;
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

    private bool IsAdmin() => AdminAuthHelper.IsTorvexOwner(User, _config);

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
