using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.Features.Auth.Commands;

namespace peeposredemption.API.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly IMediator _mediator;
        public LoginModel(IMediator mediator) => _mediator = mediator;

        [BindProperty] public LoginCommand Input { get; set; }

        public IActionResult OnGet() =>
            User.Identity?.IsAuthenticated == true ? RedirectToPage("/App/Index") : Page();

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();
            try
            {
                var result = await _mediator.Send(Input);
                Response.Cookies.Append("jwt", result.Token, new CookieOptions
                {
                    HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict,
                    MaxAge = TimeSpan.FromMinutes(15)
                });
                Response.Cookies.Append("refreshToken", result.RefreshToken, new CookieOptions
                {
                    HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict,
                    MaxAge = TimeSpan.FromDays(30)
                });
                return RedirectToPage("/App/Index");
            }
            catch (UnauthorizedAccessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }
    }

}
