using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.DTOs.Emoji;
using peeposredemption.Application.Features.Emoji.Commands;
using peeposredemption.Application.Features.Emoji.Queries;
using peeposredemption.Application.Features.Moderation.Commands;
using peeposredemption.Application.Features.Moderation.Queries;
using peeposredemption.Application.Features.Shop.Commands;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App;

public class ServerSettingsModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _uow;

    public ServerSettingsModel(IMediator mediator, IUnitOfWork uow)
    {
        _mediator = mediator;
        _uow = uow;
    }

    public Guid ServerId { get; set; }
    public ServerRole CurrentUserRole { get; set; }
    public StorageTier ServerStorageTier { get; set; }
    public int EmojiLimit { get; set; }
    public List<ServerMemberDto> Members { get; set; } = new();
    public List<ModerationLog> AuditLog { get; set; } = new();
    public List<ServerEmojiDto> Emojis { get; set; } = new();
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid serverId, bool upgraded = false)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");
        ServerId = serverId;

        try
        {
            CurrentUserRole = await _mediator.Send(new GetMemberRoleQuery(serverId, userId.Value));
            Members = await _mediator.Send(new GetServerMembersQuery(serverId, userId.Value));
            AuditLog = await _uow.ModerationLogs.GetServerLogsAsync(serverId);
            Emojis = await _mediator.Send(new GetServerEmojisQuery(serverId));

            var server = await _uow.Servers.GetByIdAsync(serverId);
            if (server != null)
            {
                ServerStorageTier = server.StorageTier;
                EmojiLimit = StorageLimits.GetLimit(server.StorageTier);
            }
        }
        catch (UnauthorizedAccessException) { return Forbid(); }

        if (upgraded) StatusMessage = $"Server upgraded to {StorageLimits.GetLabel(ServerStorageTier)}! Your emoji limit is now {EmojiLimit}.";

        return Page();
    }

    public async Task<IActionResult> OnPostKickAsync(Guid serverId, Guid targetUserId)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");
        try
        {
            await _mediator.Send(new KickMemberCommand(serverId, userId.Value, targetUserId));
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        return RedirectToPage(new { serverId });
    }

    public async Task<IActionResult> OnPostBanAsync(Guid serverId, Guid targetUserId)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");
        try
        {
            await _mediator.Send(new BanMemberCommand(serverId, userId.Value, targetUserId));
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        return RedirectToPage(new { serverId });
    }

    public async Task<IActionResult> OnPostUploadEmojiAsync(Guid serverId, string emojiName, IFormFile emojiFile)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        if (emojiFile == null || emojiFile.Length == 0)
        {
            TempData["Error"] = "Please select a file.";
            return RedirectToPage(new { serverId });
        }

        try
        {
            using var stream = emojiFile.OpenReadStream();
            await _mediator.Send(new UploadServerEmojiCommand(
                serverId,
                userId.Value,
                emojiName.ToLower().Trim(),
                stream,
                emojiFile.ContentType,
                emojiFile.Length));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage(new { serverId });
    }

    public async Task<IActionResult> OnPostDeleteEmojiAsync(Guid serverId, Guid emojiId)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");
        try
        {
            await _mediator.Send(new DeleteServerEmojiCommand(emojiId, userId.Value));
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or KeyNotFoundException)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage(new { serverId });
    }

    public async Task<IActionResult> OnPostBuyBoostAsync(Guid serverId, StorageTier targetTier)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");
        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var checkoutUrl = await _mediator.Send(new CreateStorageUpgradeSessionCommand(serverId, userId.Value, targetTier, baseUrl));
            return Redirect(checkoutUrl);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException or KeyNotFoundException)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage(new { serverId });
        }
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim == null ? null : Guid.Parse(claim);
    }
}
