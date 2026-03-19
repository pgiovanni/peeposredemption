using MediatR;
using peeposredemption.Application.DTOs.Auth;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;
using System;

namespace peeposredemption.Application.Features.Auth.Commands
{
    public record LoginCommand(
        string Email,
        string Password,
        string? IpAddress = null,
        string? UserAgent = null,
        Guid? DeviceId = null) : IRequest<LoginResultDto>;

    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResultDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly TokenService _tokenService;

        public LoginCommandHandler(IUnitOfWork uow, TokenService tokenService)
        {
            _uow = uow;
            _tokenService = tokenService;
        }

        public async Task<LoginResultDto> Handle(LoginCommand cmd, CancellationToken ct)
        {
            var user = await _uow.Users.GetByEmailAsync(cmd.Email)
                ?? throw new UnauthorizedAccessException("Invalid credentials.");

            if (!BCrypt.Net.BCrypt.Verify(cmd.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid credentials.");

            if (!user.EmailConfirmed)
                throw new UnauthorizedAccessException("Please confirm your email before logging in.");

            // If MFA is enabled, return a pending token instead of full auth
            if (user.IsMfaEnabled)
            {
                var pendingToken = _tokenService.GenerateMfaPendingToken(user);
                return LoginResultDtoExtensions.MfaPending(pendingToken, user.Id);
            }

            var jwt = _tokenService.GenerateToken(user);
            var rawRefresh = _tokenService.GenerateRefreshToken();

            await _uow.RefreshTokens.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                Token = TokenService.HashToken(rawRefresh),
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                IpAddress = cmd.IpAddress,
                UserAgent = cmd.UserAgent,
                DeviceId = cmd.DeviceId
            });
            await _uow.SaveChangesAsync();

            return LoginResultDtoExtensions.FullLogin(jwt, rawRefresh, user.Id);
        }
    }
}
