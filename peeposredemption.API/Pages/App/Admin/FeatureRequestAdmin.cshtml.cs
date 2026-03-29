using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.API.Infrastructure;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.API.Pages.App.Admin;

public class FeatureRequestAdminModel : PageModel
{
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;

    public FeatureRequestAdminModel(IUnitOfWork uow, IConfiguration config)
    {
        _uow = uow;
        _config = config;
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

    private bool IsAdmin() => AdminAuthHelper.IsTorvexOwner(User, _config);
}
