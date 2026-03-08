using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Moderation.Commands
{
    public record KickMemberCommand(Guid ServerId, Guid RequesterId, Guid TargetUserId) : IRequest<bool>;

    public class KickMemberCommandHandler : IRequestHandler<KickMemberCommand, bool>
    {
        private readonly IUnitOfWork _uow;
        public KickMemberCommandHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<bool> Handle(KickMemberCommand cmd, CancellationToken ct)
        {
            var requesterRole = await _uow.Servers.GetMemberRoleAsync(cmd.ServerId, cmd.RequesterId);
            if (requesterRole != ServerRole.Owner)
                throw new UnauthorizedAccessException("Only the server owner can kick members.");

            if (cmd.TargetUserId == cmd.RequesterId)
                throw new InvalidOperationException("You cannot kick yourself.");

            await _uow.ModerationLogs.AddAsync(new ModerationLog
            {
                ServerId = cmd.ServerId,
                ModeratorId = cmd.RequesterId,
                Action = ModerationAction.Kick,
                TargetUserId = cmd.TargetUserId
            });

            await _uow.Servers.RemoveMemberAsync(cmd.ServerId, cmd.TargetUserId);
            await _uow.SaveChangesAsync();
            return true;
        }
    }
}
