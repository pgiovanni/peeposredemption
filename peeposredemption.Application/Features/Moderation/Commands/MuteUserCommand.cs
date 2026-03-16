using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Moderation.Commands
{
    public record MuteUserCommand(Guid ServerId, Guid RequesterId, Guid TargetUserId, int DurationMinutes = 10) : IRequest<bool>;

    public class MuteUserCommandHandler : IRequestHandler<MuteUserCommand, bool>
    {
        private readonly IUnitOfWork _uow;
        public MuteUserCommandHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<bool> Handle(MuteUserCommand cmd, CancellationToken ct)
        {
            var requesterRole = await _uow.Servers.GetMemberRoleAsync(cmd.ServerId, cmd.RequesterId);
            if (requesterRole < ServerRole.Moderator)
                throw new UnauthorizedAccessException("Only moderators and above can mute members.");

            if (cmd.TargetUserId == cmd.RequesterId)
                throw new InvalidOperationException("You cannot mute yourself.");

            var targetMember = await _uow.Servers.GetMemberAsync(cmd.ServerId, cmd.TargetUserId);
            if (targetMember == null)
                throw new InvalidOperationException("User is not a member of this server.");

            // Can't mute someone with equal or higher role
            var targetRole = targetMember.Role;
            if (targetRole >= requesterRole)
                throw new InvalidOperationException("You cannot mute someone with an equal or higher role.");

            targetMember.IsMuted = true;
            targetMember.MutedUntil = DateTime.UtcNow.AddMinutes(cmd.DurationMinutes);

            await _uow.ModerationLogs.AddAsync(new ModerationLog
            {
                ServerId = cmd.ServerId,
                ModeratorId = cmd.RequesterId,
                Action = ModerationAction.Mute,
                TargetUserId = cmd.TargetUserId,
                Reason = $"Muted for {cmd.DurationMinutes} minutes"
            });

            await _uow.SaveChangesAsync();
            return true;
        }
    }
}
