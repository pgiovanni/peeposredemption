using MediatR;
using peeposredemption.Application.DTOs.Auth;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Application.Features.Auth.Commands
{
    public record LoginCommand(string Email, string Password) : IRequest<TokenResponseDto>;

    public class LoginCommandHandler : IRequestHandler<LoginCommand, TokenResponseDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly TokenService _tokenService;

        public LoginCommandHandler(IUnitOfWork uow, TokenService tokenService)
        {
            _uow = uow;
            _tokenService = tokenService;
        }

        public async Task<TokenResponseDto> Handle(LoginCommand cmd, CancellationToken ct)
        {
            var user = await _uow.Users.GetByEmailAsync(cmd.Email)
                ?? throw new Exception("Invalid credentials.");

            if (!BCrypt.Net.BCrypt.Verify(cmd.Password, user.PasswordHash))
                throw new Exception("Invalid credentials.");

            return new TokenResponseDto(_tokenService.GenerateToken(user));
        }
    }
}
