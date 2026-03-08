using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.Features.Moderation.Commands;
using peeposredemption.Application.Features.Moderation.Queries;
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
    public List<ServerMemberDto> Members { get; set; } = new();
    public List<ModerationLog> AuditLog { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid serverId)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");
        ServerId = serverId;

        try
        {
            CurrentUserRole = await _mediator.Send(new GetMemberRoleQuery(serverId, userId.Value));
            Members = await _mediator.Send(new GetServerMembersQuery(serverId, userId.Value));
            AuditLog = await _uow.ModerationLogs.GetServerLogsAsync(serverId);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }

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

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim == null ? null : Guid.Parse(claim);
    }
}
