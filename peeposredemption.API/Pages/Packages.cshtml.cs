using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace peeposredemption.API.Pages;

[AllowAnonymous]
public class PackagesModel : PageModel
{
    public void OnGet()
    {
    }
}
