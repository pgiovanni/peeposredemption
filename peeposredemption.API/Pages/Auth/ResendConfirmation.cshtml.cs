using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.Features.Auth.Commands;

namespace peeposredemption.API.Pages.Auth
{
    public class ResendConfirmationModel : PageModel
    {
        private readonly IMediator _mediator;
        public ResendConfirmationModel(IMediator mediator) => _mediator = mediator;

        [BindProperty] public string Email { get; set; } = string.Empty;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            await _mediator.Send(new ResendConfirmationCommand(Email));

            // Always redirect to CheckEmail — don't confirm whether the email exists
            return RedirectToPage("/Auth/CheckEmail");
        }
    }
}
