using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.API.Pages.App.Admin;

public class SupportTicketAdminModel : PageModel
{
    private readonly IUnitOfWork _uow;
    private readonly string _adminEmail;

    public SupportTicketAdminModel(IUnitOfWork uow, IConfiguration config)
    {
        _uow = uow;
        _adminEmail = config["Email:AdminEmail"] ?? string.Empty;
    }

    public List<SupportTicket> Tickets { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!IsAdmin()) return Forbid();
        Tickets = await _uow.SupportTickets.GetAllAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(Guid ticketId, SupportTicketStatus status)
    {
        if (!IsAdmin()) return Forbid();

        var ticket = await _uow.SupportTickets.GetByIdAsync(ticketId);
        if (ticket != null)
        {
            ticket.Status = status;
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
