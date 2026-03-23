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
    public record RegisterCommand(string Username, string Email, string Password, DateTime? DateOfBirth = null, string? ReferralCode = null, string? InviteCode = null)
     : IRequest<Guid>;

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Guid>
    {
        private readonly IUnitOfWork _uow;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly ILogger<RegisterCommandHandler> _logger;

        public RegisterCommandHandler(IUnitOfWork uow, IEmailService emailService, IConfiguration config, ILogger<RegisterCommandHandler> logger)
        { _uow = uow; _emailService = emailService; _config = config; _logger = logger; }

        public async Task<Guid> Handle(RegisterCommand cmd, CancellationToken ct)
        {
            if (await _uow.Users.UsernameExistsAsync(cmd.Username))
                throw new InvalidOperationException("Username already taken.");

            if (await _uow.Users.EmailExistsAsync(cmd.Email))
                throw new InvalidOperationException("Email already in use.");

            var confirmationToken = Guid.NewGuid().ToString();

            Guid? referredByCodeId = null;
            if (!string.IsNullOrWhiteSpace(cmd.ReferralCode))
            {
                var refCode = await _uow.Referrals.GetCodeByStringAsync(cmd.ReferralCode);
                if (refCode != null) referredByCodeId = refCode.Id;
            }

            var user = new User
            {
                Username = cmd.Username,
                Email = cmd.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(cmd.Password),
                EmailConfirmationtoken = confirmationToken,
                ReferredByCodeId = referredByCodeId,
                DateOfBirth = cmd.DateOfBirth?.Date
            };

            await _uow.Users.AddAsync(user);
            await _uow.SaveChangesAsync();

            // Auto-friend mrdudebro1 and add to the main server
            var adminUser = await _uow.Users.GetByUsernameAsync("mrdudebro1");
            var mainServerId = _config["App:MainServerId"];
            if (adminUser != null && adminUser.Id != user.Id)
            {
                var alreadyFriends = await _uow.FriendRequests.ExistsAsync(user.Id, adminUser.Id);
                if (!alreadyFriends)
                {
                    await _uow.FriendRequests.AddAsync(new FriendRequest
                    {
                        SenderId = user.Id,
                        ReceiverId = adminUser.Id,
                        Status = FriendRequestStatus.Accepted
                    });
                }
            }
            if (mainServerId != null && Guid.TryParse(mainServerId, out var mainServerGuid))
            {
                var alreadyMember = await _uow.Servers.IsMemberAsync(mainServerGuid, user.Id);
                if (!alreadyMember)
                {
                    await _uow.Servers.AddMemberAsync(new ServerMember
                    {
                        ServerId = mainServerGuid,
                        UserId = user.Id
                    });
                }
            }

            // Create personal server for the new user
            var personalServer = new Server
            {
                Name = $"{cmd.Username}'s Server",
                OwnerId = user.Id
            };
            await _uow.Servers.AddAsync(personalServer);
            var generalChannel = new Channel { ServerId = personalServer.Id, Name = "general" };
            await _uow.Channels.AddAsync(generalChannel);
            await _uow.Servers.AddMemberAsync(new ServerMember
            {
                ServerId = personalServer.Id,
                UserId = user.Id,
                Role = ServerRole.Owner
            });

            // Create a permanent invite for the personal server and store it as the welcome invite
            var welcomeInvite = new ServerInvite { ServerId = personalServer.Id, CreatedByUserId = user.Id };
            await _uow.ServerInvites.AddAsync(welcomeInvite);
            personalServer.WelcomeInviteCode = welcomeInvite.Code;

            // If the user arrived via a server invite link, auto-join that server
            if (!string.IsNullOrWhiteSpace(cmd.InviteCode))
            {
                var serverInvite = await _uow.ServerInvites.GetByCodeAsync(cmd.InviteCode);
                if (serverInvite != null)
                {
                    var alreadyMember = await _uow.Servers.IsMemberAsync(serverInvite.ServerId, user.Id);
                    if (!alreadyMember)
                    {
                        await _uow.Servers.AddMemberAsync(new ServerMember
                        {
                            ServerId = serverInvite.ServerId,
                            UserId = user.Id
                        });
                    }
                }
            }

            await _uow.SaveChangesAsync();

            var baseUrl = _config["AppBaseUrl"] ?? "https://localhost:443";
            var confirmationLink = $"{baseUrl}/Auth/ConfirmEmail?token={confirmationToken}";
            await _emailService.SendConfirmationEmailAsync(cmd.Email, confirmationLink);
            await _emailService.SendNewUserNotificationAsync(cmd.Username, cmd.Email);

            return user.Id;
        }
    }

}
