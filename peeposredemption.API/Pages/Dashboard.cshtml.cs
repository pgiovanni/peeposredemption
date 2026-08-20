using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.API.Pages;

public class DashboardModel : PageModel
{
    private static readonly string[] ValidTabs = { "account", "projects", "billing", "orders", "community" };

    private readonly IUnitOfWork _uow;

    public DashboardModel(IUnitOfWork uow) => _uow = uow;

    public string Tab { get; set; } = "account";
    public User CurrentUser { get; set; } = null!;
    public List<Lead> Orders { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? tab = null)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        var user = await _uow.Users.GetByIdAsync(userId.Value);
        if (user == null) return RedirectToPage("/Auth/Login");
        CurrentUser = user;

        Tab = ValidTabs.Contains(tab) ? tab! : "account";

        if (Tab == "orders")
            Orders = await _uow.Leads.GetByEmailAsync(user.Email);

        return Page();
    }

    // Customer-friendly wording for internal lead statuses
    public static string CustomerStatus(LeadStatus status) => status switch
    {
        LeadStatus.New => "Received",
        LeadStatus.Contacted => "In contact",
        LeadStatus.Quoted => "Quote sent",
        LeadStatus.Won => "Active",
        LeadStatus.Lost => "Closed",
        _ => status.ToString()
    };

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim == null ? null : Guid.Parse(claim);
    }
}
