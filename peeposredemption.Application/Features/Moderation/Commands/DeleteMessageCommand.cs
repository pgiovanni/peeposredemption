using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Moderation.Commands
{
    public record DeleteMessageCommand(Guid MessageId, Guid ServerId, Guid RequesterId) : IRequest<bool>;

    public class DeleteMessageCommandHandler : IRequestHandler<DeleteMessageCommand, bool>
    {
        private readonly IUnitOfWork _uow;
        public DeleteMessageCommandHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<bool> Handle(DeleteMessageCommand cmd, CancellationToken ct)
        {
            var requesterRole = await _uow.Servers.GetMemberRoleAsync(cmd.ServerId, cmd.RequesterId);
            if (requesterRole is null or ServerRole.Member)
                throw new UnauthorizedAccessException("Moderator or higher is required to delete messages.");

            var message = await _uow.Messages.GetByIdAsync(cmd.MessageId);
            if (message is null) return false;

            message.IsDeleted = true;

            await _uow.ModerationLogs.AddAsync(new ModerationLog
            {
                ServerId = cmd.ServerId,
                ModeratorId = cmd.RequesterId,
                Action = ModerationAction.DeleteMessage,
                TargetUserId = message.AuthorId,
                TargetMessageId = cmd.MessageId
            });

            await _uow.SaveChangesAsync();
            return true;
        }
    }
}
