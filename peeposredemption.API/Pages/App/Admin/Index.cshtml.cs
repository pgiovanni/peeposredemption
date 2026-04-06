using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.API.Infrastructure;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App.Admin;

public class AdminIndexModel : PageModel
{
    private readonly IConfiguration _config;

    public AdminIndexModel(IConfiguration config)
    {
        _config = config;
    }

    public string Username { get; private set; } = "";

    public IActionResult OnGet()
    {
        if (!AdminAuthHelper.IsTorvexOwner(User, _config)) return Forbid();
        Username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Admin";
        return Page();
    }
}
