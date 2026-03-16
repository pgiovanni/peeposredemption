using System.Text.Json;
using MediatR;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Artists.Commands;

public record ArtistSampleFile(Stream Stream, string ContentType, string FileName, long Size);

public record SubmitArtistApplicationCommand(
    Guid UserId,
    string DisplayName,
    string Email,
    string PortfolioUrl,
    string? Message,
    List<ArtistSampleFile> SampleFiles) : IRequest<Guid>;

public class SubmitArtistApplicationCommandHandler : IRequestHandler<SubmitArtistApplicationCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    private readonly IR2StorageService _r2;
    private readonly IEmailService _email;

    public SubmitArtistApplicationCommandHandler(IUnitOfWork uow, IR2StorageService r2, IEmailService email)
    {
        _uow = uow;
        _r2 = r2;
        _email = email;
    }

    public async Task<Guid> Handle(SubmitArtistApplicationCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.DisplayName))
            throw new ArgumentException("Display name is required.");
        if (string.IsNullOrWhiteSpace(cmd.Email))
            throw new ArgumentException("Email is required.");
        if (string.IsNullOrWhiteSpace(cmd.PortfolioUrl))
            throw new ArgumentException("Portfolio URL is required.");
        if (cmd.SampleFiles.Count == 0)
            throw new ArgumentException("At least one sample image is required.");
        if (cmd.SampleFiles.Count > 3)
            throw new ArgumentException("Maximum 3 sample images allowed.");

        var allowedTypes = new[] { "image/png", "image/jpeg", "image/webp" };
        foreach (var file in cmd.SampleFiles)
        {
            if (file.Size > 2 * 1024 * 1024)
                throw new ArgumentException($"File '{file.FileName}' exceeds 2MB limit.");
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                throw new ArgumentException($"File '{file.FileName}' must be PNG, JPG, or WebP.");
        }

        // Check for existing pending/approved submission
        var existing = await _uow.ArtistSubmissions.GetActiveByUserIdAsync(cmd.UserId);
        if (existing != null)
            throw new InvalidOperationException("You already have a pending or approved artist application.");

        // Upload sample images to R2
        var urls = new List<string>();
        var keys = new List<string>();

        foreach (var file in cmd.SampleFiles)
        {
            var key = $"artist-samples/{cmd.UserId}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var url = await _r2.UploadArtistSampleAsync(key, file.Stream, file.ContentType);
            urls.Add(url);
            keys.Add(key);
        }

        var submission = new ArtistSubmission
        {
            UserId = cmd.UserId,
            DisplayName = cmd.DisplayName.Trim(),
            Email = cmd.Email.Trim(),
            PortfolioUrl = cmd.PortfolioUrl.Trim(),
            Message = string.IsNullOrWhiteSpace(cmd.Message) ? null : cmd.Message.Trim(),
            SampleImageUrls = JsonSerializer.Serialize(urls),
            SampleImageKeys = JsonSerializer.Serialize(keys)
        };

        await _uow.ArtistSubmissions.AddAsync(submission);
        await _uow.SaveChangesAsync();

        // Send admin notification (fire and forget)
        try
        {
            await _email.SendArtistSubmissionNotificationAsync(cmd.DisplayName, cmd.Email, cmd.PortfolioUrl);
        }
        catch { /* Don't fail the submission if email fails */ }

        return submission.Id;
    }
}
