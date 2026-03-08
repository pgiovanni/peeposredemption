using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Moderation.Commands
{
    public record BanMemberCommand(Guid ServerId, Guid RequesterId, Guid TargetUserId) : IRequest<bool>;

    public class BanMemberCommandHandler : IRequestHandler<BanMemberCommand, bool>
    {
        private readonly IUnitOfWork _uow;
        public BanMemberCommandHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<bool> Handle(BanMemberCommand cmd, CancellationToken ct)
        {
            var requesterRole = await _uow.Servers.GetMemberRoleAsync(cmd.ServerId, cmd.RequesterId);
            if (requesterRole != ServerRole.Owner)
                throw new UnauthorizedAccessException("Only the server owner can ban members.");

            if (cmd.TargetUserId == cmd.RequesterId)
                throw new InvalidOperationException("You cannot ban yourself.");

            var alreadyBanned = await _uow.BannedMembers.IsBannedAsync(cmd.ServerId, cmd.TargetUserId);
            if (!alreadyBanned)
            {
                await _uow.BannedMembers.AddAsync(new BannedMember
                {
                    ServerId = cmd.ServerId,
                    UserId = cmd.TargetUserId,
                    BannedByUserId = cmd.RequesterId
                });
            }

            await _uow.ModerationLogs.AddAsync(new ModerationLog
            {
                ServerId = cmd.ServerId,
                ModeratorId = cmd.RequesterId,
                Action = ModerationAction.Ban,
                TargetUserId = cmd.TargetUserId
            });

            await _uow.Servers.RemoveMemberAsync(cmd.ServerId, cmd.TargetUserId);
            await _uow.SaveChangesAsync();
            return true;
        }
    }
}
