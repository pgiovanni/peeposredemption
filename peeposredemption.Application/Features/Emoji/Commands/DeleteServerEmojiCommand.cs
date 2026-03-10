using MediatR;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Emoji.Commands
{
    public record DeleteServerEmojiCommand(Guid EmojiId, Guid RequestingUserId) : IRequest;

    public class DeleteServerEmojiCommandHandler : IRequestHandler<DeleteServerEmojiCommand>
    {
        private readonly IUnitOfWork _uow;
        private readonly IR2StorageService _r2;

        public DeleteServerEmojiCommandHandler(IUnitOfWork uow, IR2StorageService r2)
        {
            _uow = uow;
            _r2 = r2;
        }

        public async Task Handle(DeleteServerEmojiCommand cmd, CancellationToken ct)
        {
            var emoji = await _uow.ServerEmojis.GetByIdAsync(cmd.EmojiId)
                ?? throw new KeyNotFoundException("Emoji not found.");

            var role = await _uow.Servers.GetMemberRoleAsync(emoji.ServerId, cmd.RequestingUserId);
            if (role == null || role < ServerRole.Moderator)
                throw new UnauthorizedAccessException("Only moderators and owners can delete emojis.");

            await _r2.DeleteEmojiAsync(emoji.R2Key);
            _uow.ServerEmojis.Delete(emoji);
            await _uow.SaveChangesAsync();
        }
    }
}
