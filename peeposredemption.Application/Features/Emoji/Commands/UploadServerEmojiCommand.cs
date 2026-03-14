using MediatR;
using peeposredemption.Application.DTOs.Emoji;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;
using System.Text.RegularExpressions;

namespace peeposredemption.Application.Features.Emoji.Commands
{
    public record UploadServerEmojiCommand(
        Guid ServerId,
        Guid UploaderUserId,
        string Name,
        Stream ImageStream,
        string ContentType,
        long FileSize) : IRequest<ServerEmojiDto>;

    public class UploadServerEmojiCommandHandler : IRequestHandler<UploadServerEmojiCommand, ServerEmojiDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IR2StorageService _r2;

        public UploadServerEmojiCommandHandler(IUnitOfWork uow, IR2StorageService r2)
        {
            _uow = uow;
            _r2 = r2;
        }

        public async Task<ServerEmojiDto> Handle(UploadServerEmojiCommand cmd, CancellationToken ct)
        {
            // Validate name: alphanumeric + underscore, 2-32 chars
            if (!Regex.IsMatch(cmd.Name, @"^[a-z0-9_]{2,32}$"))
                throw new ArgumentException("Emoji name must be 2-32 lowercase alphanumeric characters or underscores.");

            // Validate file size (256KB max)
            if (cmd.FileSize > 256 * 1024)
                throw new ArgumentException("Emoji image must be 256KB or smaller.");

            // Validate content type
            var allowed = new[] { "image/png", "image/gif", "image/webp" };
            if (!allowed.Contains(cmd.ContentType))
                throw new ArgumentException("Emoji must be PNG, GIF, or WebP.");

            // Check role
            var role = await _uow.Servers.GetMemberRoleAsync(cmd.ServerId, cmd.UploaderUserId);
            if (role == null || role < ServerRole.Moderator)
                throw new UnauthorizedAccessException("Only moderators and owners can upload emojis.");

            // Check limit
            var server = await _uow.Servers.GetByIdAsync(cmd.ServerId)
                ?? throw new KeyNotFoundException("Server not found.");
            var count = await _uow.ServerEmojis.CountByServerIdAsync(cmd.ServerId);
            var limit = StorageLimits.GetLimit(server.StorageTier);
            if (count >= limit)
                throw new InvalidOperationException($"Emoji limit reached ({limit}). {(server.StorageTier == StorageTier.Free ? "Boost your server to add up to 100 emojis." : "")}");

            // Check name uniqueness
            var existing = await _uow.ServerEmojis.GetByNameAsync(cmd.ServerId, cmd.Name);
            if (existing != null)
                throw new InvalidOperationException($"An emoji named '{cmd.Name}' already exists in this server.");

            // Upload to R2
            var key = $"emojis/{cmd.ServerId}/{cmd.Name}";
            var imageUrl = await _r2.UploadEmojiAsync(key, cmd.ImageStream, cmd.ContentType);

            var emoji = new ServerEmoji
            {
                ServerId = cmd.ServerId,
                UploadedByUserId = cmd.UploaderUserId,
                Name = cmd.Name,
                ImageUrl = imageUrl,
                R2Key = key
            };

            await _uow.ServerEmojis.AddAsync(emoji);
            await _uow.SaveChangesAsync();

            return new ServerEmojiDto(emoji.Id, emoji.Name, emoji.ImageUrl, emoji.ServerId, "");
        }
    }
}
