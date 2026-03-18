using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Moderation.Commands
{
    public record UnbanMemberCommand(Guid ServerId, Guid RequesterId, Guid TargetUserId) : IRequest<bool>;

    public class UnbanMemberCommandHandler : IRequestHandler<UnbanMemberCommand, bool>
    {
        private readonly IUnitOfWork _uow;
        public UnbanMemberCommandHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<bool> Handle(UnbanMemberCommand cmd, CancellationToken ct)
        {
            var requesterRole = await _uow.Servers.GetMemberRoleAsync(cmd.ServerId, cmd.RequesterId);
            if (requesterRole < ServerRole.Admin)
                throw new UnauthorizedAccessException("Only admins and the server owner can unban members.");

            var ban = await _uow.BannedMembers.GetAsync(cmd.ServerId, cmd.TargetUserId);
            if (ban == null)
                throw new InvalidOperationException("That user is not banned from this server.");

            _uow.BannedMembers.Remove(ban);

            await _uow.ModerationLogs.AddAsync(new ModerationLog
            {
                ServerId = cmd.ServerId,
                ModeratorId = cmd.RequesterId,
                Action = ModerationAction.Unban,
                TargetUserId = cmd.TargetUserId
            });

            await _uow.SaveChangesAsync();
            return true;
        }
    }
}
