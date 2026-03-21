using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.API.Pages.App.Admin;

public class FeatureRequestAdminModel : PageModel
{
    private readonly IUnitOfWork _uow;
    private readonly string _adminEmail;

    public FeatureRequestAdminModel(IUnitOfWork uow, IConfiguration config)
    {
        _uow = uow;
        _adminEmail = config["Email:AdminEmail"] ?? string.Empty;
    }

    public List<FeatureRequest> Requests { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!IsAdmin()) return Forbid();
        Requests = await _uow.FeatureRequests.GetAllAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(Guid requestId, FeatureRequestStatus status)
    {
        if (!IsAdmin()) return Forbid();

        var request = await _uow.FeatureRequests.GetByIdAsync(requestId);
        if (request != null)
        {
            request.Status = status;
            await _uow.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    private bool IsAdmin()
    {
        var emailClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        return !string.IsNullOrEmpty(_adminEmail) &&
               string.Equals(emailClaim, _adminEmail, StringComparison.OrdinalIgnoreCase);
    }
}
