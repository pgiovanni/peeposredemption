using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.Features.Auth.Commands;

namespace peeposredemption.API.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly IMediator _mediator;
        public RegisterModel(IMediator mediator) => _mediator = mediator;

        [BindProperty] public RegisterCommand Input { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();
            try
            {
                var result = await _mediator.Send(Input);
                Response.Cookies.Append("jwt", result.Token, new CookieOptions
                { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
                return RedirectToPage("/App/Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }
    }
}
