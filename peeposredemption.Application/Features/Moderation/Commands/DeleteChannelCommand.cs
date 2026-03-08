using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Moderation.Commands
{
    public record DeleteChannelCommand(Guid ChannelId, Guid ServerId, Guid RequesterId) : IRequest<bool>;

    public class DeleteChannelCommandHandler : IRequestHandler<DeleteChannelCommand, bool>
    {
        private readonly IUnitOfWork _uow;
        public DeleteChannelCommandHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<bool> Handle(DeleteChannelCommand cmd, CancellationToken ct)
        {
            var requesterRole = await _uow.Servers.GetMemberRoleAsync(cmd.ServerId, cmd.RequesterId);
            if (requesterRole is null or ServerRole.Member)
                throw new UnauthorizedAccessException("Moderator or higher is required to delete channels.");

            var channel = await _uow.Channels.GetByIdAsync(cmd.ChannelId);
            if (channel is null || channel.ServerId != cmd.ServerId) return false;

            await _uow.ModerationLogs.AddAsync(new ModerationLog
            {
                ServerId = cmd.ServerId,
                ModeratorId = cmd.RequesterId,
                Action = ModerationAction.DeleteChannel,
                TargetUserId = cmd.RequesterId
            });

            await _uow.Channels.RemoveAsync(channel);
            await _uow.SaveChangesAsync();
            return true;
        }
    }
}
