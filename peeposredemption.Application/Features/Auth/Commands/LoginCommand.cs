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
                ?? throw new UnauthorizedAccessException("Invalid credentials.");

            if (!BCrypt.Net.BCrypt.Verify(cmd.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid credentials.");

            if (!user.EmailConfirmed)
                throw new UnauthorizedAccessException("Please confirm your email before logging in.");

            var jwt = _tokenService.GenerateToken(user);
            var rawRefresh = _tokenService.GenerateRefreshToken();

            await _uow.RefreshTokens.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                Token = TokenService.HashToken(rawRefresh),
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            });
            await _uow.SaveChangesAsync();

            return new TokenResponseDto(jwt, rawRefresh);
        }
    }
}
