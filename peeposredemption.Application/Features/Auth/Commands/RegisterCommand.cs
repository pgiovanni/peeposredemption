using MediatR;
using peeposredemption.Application.DTOs.Auth;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Application.Features.Auth.Commands
{
    public record RegisterCommand(string Username, string Email, string Password)
     : IRequest<Unit>;

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Unit>
    {
        private readonly IUnitOfWork _uow;
        private readonly EmailService _emailService;

        public RegisterCommandHandler(IUnitOfWork uow, EmailService emailService)
        { _uow = uow; _emailService = emailService; }

        public async Task<Unit> Handle(RegisterCommand cmd, CancellationToken ct)
        {
            if (await _uow.Users.EmailExistsAsync(cmd.Email))
                throw new InvalidOperationException("Email already in use.");

            var confirmationToken = Guid.NewGuid().ToString();

            var user = new User
            {
                Username = cmd.Username,
                Email = cmd.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(cmd.Password),
                EmailConfirmationtoken = confirmationToken
            };

            await _uow.Users.AddAsync(user);
            await _uow.SaveChangesAsync();

            var confirmationLink = $"https://localhost:443/Auth/Confirm?token={confirmationToken}";
            await _emailService.SendConfirmationEmailAsync(cmd.Email, confirmationLink);

            return Unit.Value;
        }
    }

}
