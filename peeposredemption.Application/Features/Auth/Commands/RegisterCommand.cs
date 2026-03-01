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
     : IRequest<TokenResponseDto>;

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, TokenResponseDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly TokenService _tokenService;

        public RegisterCommandHandler(IUnitOfWork uow, TokenService tokenService)
        { _uow = uow; _tokenService = tokenService; }

        public async Task<TokenResponseDto> Handle(RegisterCommand cmd, CancellationToken ct)
        {
            if (await _uow.Users.EmailExistsAsync(cmd.Email))
                throw new InvalidOperationException("Email already in use.");

            var user = new User
            {
                Username = cmd.Username,
                Email = cmd.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(cmd.Password)
            };

            await _uow.Users.AddAsync(user);
            await _uow.SaveChangesAsync();
            return new TokenResponseDto(_tokenService.GenerateToken(user));
        }
    }

}
