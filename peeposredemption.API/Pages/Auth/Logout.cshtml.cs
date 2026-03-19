using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Domain.Interfaces;
using System.Security.Claims;

namespace peeposredemption.API.Pages.Auth
{
    public class LogoutModel : PageModel
    {
        private readonly IUnitOfWork _uow;
        public LogoutModel(IUnitOfWork uow) => _uow = uow;

        public async Task<IActionResult> OnPost()
        {
            var currentToken = Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(currentToken))
            {
                await _uow.RefreshTokens.RevokeByTokenAsync(currentToken);
                await _uow.SaveChangesAsync();
            }

            Response.Cookies.Delete("jwt");
            Response.Cookies.Delete("refreshToken");
            return RedirectToPage("/Auth/Login");
        }
    }
}
