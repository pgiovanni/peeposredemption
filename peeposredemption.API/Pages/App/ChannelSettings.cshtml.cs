using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.Features.Channels.Queries;
using peeposredemption.Application.Features.Moderation.Commands;
using peeposredemption.Application.Features.Moderation.Queries;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App;

public class ChannelSettingsModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _uow;

    public ChannelSettingsModel(IMediator mediator, IUnitOfWork uow)
    {
        _mediator = mediator;
        _uow = uow;
    }

    public Guid ServerId { get; set; }
    public Guid ChannelId { get; set; }
    public string ChannelName { get; set; } = "";
    public string ServerName { get; set; } = "";
    public ServerRole CurrentUserRole { get; set; }
    public bool IsGeneral { get; set; }
    public bool GameBotMuted { get; set; }
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid channelId, Guid serverId)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        try
        {
            CurrentUserRole = await _mediator.Send(new GetMemberRoleQuery(serverId, userId.Value));
            if (CurrentUserRole < ServerRole.Moderator) return Forbid();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }

        await LoadDataAsync(channelId, serverId);
        return Page();
    }

    public async Task<IActionResult> OnPostToggleGameBotAsync(Guid channelId, Guid serverId)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        try
        {
            var role = await _mediator.Send(new GetMemberRoleQuery(serverId, userId.Value));
            if (role < ServerRole.Moderator) return Forbid();

            var config = await _uow.GameChannelConfigs.GetByChannelIdAsync(channelId);
            if (config == null)
            {
                config = new GameChannelConfig
                {
                    ChannelId = channelId,
                    GameBotMuted = true,
                    MutedByUserId = userId,
                    MutedAt = DateTime.UtcNow
                };
                await _uow.GameChannelConfigs.AddAsync(config);
            }
            else
            {
                config.GameBotMuted = !config.GameBotMuted;
                config.MutedByUserId = userId;
                config.MutedAt = DateTime.UtcNow;
            }
            await _uow.SaveChangesAsync();

            StatusMessage = config.GameBotMuted ? "Game bot disabled in this channel." : "Game bot enabled in this channel.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        await LoadDataAsync(channelId, serverId);
        return Page();
    }

    public async Task<IActionResult> OnPostRenameAsync(Guid channelId, Guid serverId, string newName)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        try
        {
            var role = await _mediator.Send(new GetMemberRoleQuery(serverId, userId.Value));
            if (role < ServerRole.Moderator) return Forbid();

            if (string.IsNullOrWhiteSpace(newName) || newName.Length > 100)
            {
                TempData["Error"] = "Channel name must be 1-100 characters.";
                return RedirectToPage(new { channelId, serverId });
            }

            var channel = await _uow.Channels.GetByIdAsync(channelId);
            if (channel != null)
            {
                channel.Name = newName.Trim().ToLower().Replace(' ', '-');
                await _uow.SaveChangesAsync();
                StatusMessage = "Channel renamed!";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        await LoadDataAsync(channelId, serverId);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid channelId, Guid serverId)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        try
        {
            await _mediator.Send(new DeleteChannelCommand(channelId, serverId, userId.Value));
            var channels = await _mediator.Send(new GetServerChannelsQuery(serverId));
            var first = channels.FirstOrDefault();
            if (first != null)
                return RedirectToPage("/App/Channel", new { channelId = first.Id, serverId });
            return RedirectToPage("/App/Index");
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    private async Task LoadDataAsync(Guid channelId, Guid serverId)
    {
        ServerId = serverId;
        ChannelId = channelId;

        var channel = await _uow.Channels.GetByIdAsync(channelId);
        if (channel != null)
        {
            ChannelName = channel.Name;
            IsGeneral = channel.Name == "general";
        }

        var server = await _uow.Servers.GetByIdAsync(serverId);
        if (server != null) ServerName = server.Name;

        var config = await _uow.GameChannelConfigs.GetByChannelIdAsync(channelId);
        GameBotMuted = config is { GameBotMuted: true };
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim == null ? null : Guid.Parse(claim);
    }
}
