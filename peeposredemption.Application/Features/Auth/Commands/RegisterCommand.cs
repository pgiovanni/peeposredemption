using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly ILogger<RegisterCommandHandler> _logger;

        public RegisterCommandHandler(IUnitOfWork uow, IEmailService emailService, IConfiguration config, ILogger<RegisterCommandHandler> logger)
        { _uow = uow; _emailService = emailService; _config = config; _logger = logger; }

        public async Task<Unit> Handle(RegisterCommand cmd, CancellationToken ct)
        {
            if (await _uow.Users.UsernameExistsAsync(cmd.Username))
                throw new InvalidOperationException("Username already taken.");

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

            var baseUrl = _config["AppBaseUrl"] ?? "https://localhost:443";
            var confirmationLink = $"{baseUrl}/Auth/Confirm?token={confirmationToken}";
            _ = _emailService.SendConfirmationEmailAsync(cmd.Email, confirmationLink)
                .ContinueWith(t => _logger.LogError(t.Exception, "Failed to send confirmation email to {Email}", cmd.Email),
                    TaskContinuationOptions.OnlyOnFaulted);
            _ = _emailService.SendNewUserNotificationAsync(cmd.Username, cmd.Email)
                .ContinueWith(t => _logger.LogError(t.Exception, "Failed to send new user notification for {Username}", cmd.Username),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Unit.Value;
        }
    }

}
