using MediatR;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Users.Commands;

public record ProfileImageFile(Stream Stream, string ContentType, string FileName, long Size);

public record UpdateProfileCommand(
    Guid UserId,
    string? DisplayName,
    string? Bio,
    string? Pronouns,
    string? ProfileBackgroundColor,
    ProfileImageFile? AvatarFile,
    ProfileImageFile? BannerFile) : IRequest;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly IR2StorageService _r2;

    public UpdateProfileCommandHandler(IUnitOfWork uow, IR2StorageService r2)
    {
        _uow = uow;
        _r2 = r2;
    }

    public async Task Handle(UpdateProfileCommand cmd, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(cmd.UserId)
            ?? throw new InvalidOperationException("User not found.");

        var allowedTypes = new[] { "image/png", "image/jpeg", "image/webp", "image/gif" };

        // Avatar upload
        if (cmd.AvatarFile is { } avatar)
        {
            if (avatar.Size > 15 * 1024 * 1024)
                throw new ArgumentException("Avatar must be under 15MB.");
            if (!allowedTypes.Contains(avatar.ContentType.ToLower()))
                throw new ArgumentException("Avatar must be PNG, JPG, WebP, or GIF.");

            var ext = Path.GetExtension(avatar.FileName);
            var key = $"avatars/{cmd.UserId}{ext}";
            user.AvatarUrl = await _r2.UploadProfileImageAsync(key, avatar.Stream, avatar.ContentType);
        }

        // Banner upload
        if (cmd.BannerFile is { } banner)
        {
            if (banner.Size > 15 * 1024 * 1024)
                throw new ArgumentException("Banner must be under 15MB.");
            if (!allowedTypes.Contains(banner.ContentType.ToLower()))
                throw new ArgumentException("Banner must be PNG, JPG, WebP, or GIF.");

            var ext = Path.GetExtension(banner.FileName);
            var key = $"banners/{cmd.UserId}{ext}";
            user.BannerUrl = await _r2.UploadProfileImageAsync(key, banner.Stream, banner.ContentType);
        }

        // Text fields
        user.DisplayName = string.IsNullOrWhiteSpace(cmd.DisplayName) ? null : cmd.DisplayName.Trim();

        if (cmd.Bio != null && cmd.Bio.Length > 200)
            throw new ArgumentException("Bio must be 200 characters or fewer.");
        user.Bio = string.IsNullOrWhiteSpace(cmd.Bio) ? null : cmd.Bio.Trim();

        user.Pronouns = string.IsNullOrWhiteSpace(cmd.Pronouns) ? null : cmd.Pronouns.Trim();
        user.ProfileBackgroundColor = string.IsNullOrWhiteSpace(cmd.ProfileBackgroundColor)
            ? null : cmd.ProfileBackgroundColor.Trim();

        await _uow.SaveChangesAsync();
    }
}
