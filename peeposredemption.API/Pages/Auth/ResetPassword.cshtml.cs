using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.Features.Auth.Commands;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.API.Pages.Auth
{
    public class ResetPasswordModel : PageModel
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _uow;

        public ResetPasswordModel(IMediator mediator, IUnitOfWork uow)
        {
            _mediator = mediator;
            _uow = uow;
        }

        public string? Token { get; set; }
        public bool TokenValid { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync(string? token)
        {
            Token = token;
            if (string.IsNullOrWhiteSpace(token))
            {
                TokenValid = false;
                return;
            }

            var user = await _uow.Users.GetByPasswordResetTokenAsync(token);
            TokenValid = user != null
                && user.PasswordResetTokenExpiry != null
                && user.PasswordResetTokenExpiry > DateTime.UtcNow;
        }

        public async Task<IActionResult> OnPostAsync(string? token, string? newPassword, string? confirmPassword)
        {
            Token = token;

            if (string.IsNullOrWhiteSpace(token))
            {
                TokenValid = false;
                return Page();
            }

            TokenValid = true;

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                ErrorMessage = "Password must be at least 6 characters.";
                return Page();
            }

            if (newPassword != confirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return Page();
            }

            try
            {
                await _mediator.Send(new ResetPasswordCommand(token, newPassword));
                Success = true;
            }
            catch (InvalidOperationException ex)
            {
                TokenValid = false;
                ErrorMessage = ex.Message;
            }

            return Page();
        }
    }
}
