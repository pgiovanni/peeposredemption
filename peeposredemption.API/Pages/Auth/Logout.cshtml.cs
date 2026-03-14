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
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                await _uow.RefreshTokens.RevokeAllForUserAsync(userId);
                await _uow.SaveChangesAsync();
            }

            Response.Cookies.Delete("jwt");
            Response.Cookies.Delete("refreshToken");
            return RedirectToPage("/Auth/Login");
        }
    }
}
