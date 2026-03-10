using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App;

public class ReferralModel : PageModel
{
    private readonly IUnitOfWork _uow;

    public ReferralModel(IUnitOfWork uow) => _uow = uow;

    public ReferralCode MyCode { get; set; }
    public int Signups { get; set; }
    public List<ReferralPurchase> Purchases { get; set; } = new();
    public string ReferralLink { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        var code = await _uow.Referrals.GetCodeByOwnerIdAsync(userId.Value);
        if (code == null)
        {
            code = new ReferralCode { OwnerId = userId.Value };
            await _uow.Referrals.AddCodeAsync(code);
            await _uow.SaveChangesAsync();
        }

        MyCode = code;
        Signups = await _uow.Referrals.GetReferredUserCountAsync(code.Id);
        Purchases = await _uow.Referrals.GetPurchasesByCodeIdAsync(code.Id);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        ReferralLink = $"{baseUrl}/Auth/Register?ref={code.Code}";

        return Page();
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim == null ? null : Guid.Parse(claim);
    }
}
