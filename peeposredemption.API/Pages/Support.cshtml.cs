using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace peeposredemption.API.Pages;

[AllowAnonymous]
public class SupportModel : PageModel
{
    public void OnGet() { }
}
