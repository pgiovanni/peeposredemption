using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.API.Pages.Auth
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly IUnitOfWork _uow;
        public ConfirmEmailModel(IUnitOfWork uow) => _uow = uow;

        public async Task<IActionResult> OnGetAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToPage("/Auth/Login");

            var user = await _uow.Users.GetByConfirmationTokenAsync(token);
            if (user is null)
                return RedirectToPage("/Auth/Login");

            user.EmailConfirmed = true;
            user.EmailConfirmationtoken = null;
            await _uow.SaveChangesAsync();

            return Page();
        }
    }
}
