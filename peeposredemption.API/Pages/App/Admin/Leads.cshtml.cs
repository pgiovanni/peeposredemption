using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.API.Infrastructure;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.API.Pages.App.Admin;

public class LeadsModel : PageModel
{
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;

    public LeadsModel(IUnitOfWork uow, IConfiguration config)
    {
        _uow = uow;
        _config = config;
    }

    public List<Lead> Leads { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!IsAdmin()) return Forbid();
        Leads = await _uow.Leads.GetAllAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync(Guid leadId, LeadStatus status, string? notes)
    {
        if (!IsAdmin()) return Forbid();

        var lead = await _uow.Leads.GetByIdAsync(leadId);
        if (lead != null)
        {
            lead.Status = status;
            lead.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            await _uow.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    private bool IsAdmin() => AdminAuthHelper.IsTorvexOwner(User, _config);
}
