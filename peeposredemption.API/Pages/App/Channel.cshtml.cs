using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.DTOs.Channels;
using peeposredemption.Application.DTOs.Messages;
using peeposredemption.Application.Features.Channels.Commands;
using peeposredemption.Application.Features.Channels.Queries;
using peeposredemption.Application.Features.Messages.Queries;
using peeposredemption.Application.Features.Moderation.Commands;
using peeposredemption.Application.Features.Moderation.Queries;
using peeposredemption.Application.Features.Servers.Commands;
using peeposredemption.Application.Features.Servers.Queries;
using peeposredemption.API.Infrastructure;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App;

public class ChannelModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _uow;
    private readonly PresenceTracker _presenceTracker;
    private readonly VoiceStateTracker _voiceTracker;
    public ChannelModel(IMediator mediator, IUnitOfWork uow, PresenceTracker presenceTracker, VoiceStateTracker voiceTracker) { _mediator = mediator; _uow = uow; _presenceTracker = presenceTracker; _voiceTracker = voiceTracker; }

    public List<string> MemberUsernames { get; set; } = new();
    public List<MemberInfo> MemberData { get; set; } = new();
    public Guid CurrentUserId { get; set; }
    public string CurrentUsername { get; set; } = "";

    public HashSet<Guid> OnlineMemberIds { get; set; } = new();

    public class MemberInfo
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = "";
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
        public string DisplayOrUsername => !string.IsNullOrEmpty(DisplayName) ? DisplayName : Username;
    }

    public ServerListViewModel ServerList { get; set; } = new();
    public Guid ChannelId { get; set; }
    public Guid ServerId { get; set; }
    public string ChannelName { get; set; } = "";
    public string ServerName { get; set; } = "";
    public List<MessageDto> Messages { get; set; } = new();
    public List<ChannelDto> Channels { get; set; } = new();
    public string? InviteLink { get; set; }
    public string? WelcomeBannerUrl { get; set; }
    public ServerRole CurrentUserRole { get; set; } = ServerRole.Member;
    public long OrbBalance { get; set; }
    public int ChannelType { get; set; }
    public Dictionary<Guid, List<(Guid UserId, string DisplayName, string? AvatarUrl)>> InitialVoiceParticipants { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid channelId, Guid serverId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return RedirectToPage("/Auth/Login");
        var userId = Guid.Parse(userIdClaim);

        ChannelId = channelId;
        ServerId = serverId;

        // Load servers for the server icon strip
        var servers = await _mediator.Send(new GetUserServersQuery(userId));
        var defaultChannels = new Dictionary<Guid, Guid>();
        foreach (var s in servers)
        {
            var chs = await _mediator.Send(new GetServerChannelsQuery(s.Id));
            var first = chs.FirstOrDefault();
            if (first != null) defaultChannels[s.Id] = first.Id;
        }
        // Mark pings for this server as read since the user is viewing it
        await _uow.Notifications.MarkServerNotificationsReadAsync(userId, serverId);
        await _uow.SaveChangesAsync();

        var unreadDms = await _uow.DirectMessages.GetUnreadCountAsync(userId);
        var serverUnreadCounts = await _uow.Notifications.GetUnreadCountByServerAsync(userId);
        var dmUnreadCounts = await _uow.DirectMessages.GetUnreadCountBySenderAsync(userId);
        ServerList = new ServerListViewModel
        {
            Servers = servers,
            ServerDefaultChannels = defaultChannels,
            ActiveServerId = serverId,
            UnreadCount = unreadDms,
            ServerUnreadCounts = serverUnreadCounts,
            DmUnreadCounts = dmUnreadCounts
        };

        // Current server info
        var currentServer = servers.FirstOrDefault(s => s.Id == serverId);
        ServerName = currentServer?.Name ?? "";

        // Load channels for this server
        Channels = await _mediator.Send(new GetServerChannelsQuery(serverId));
        var activeChannel = Channels.FirstOrDefault(c => c.Id == channelId);
        ChannelName = activeChannel?.Name ?? "";
        ChannelType = activeChannel?.Type ?? 0;

        // Load messages
        Messages = await _mediator.Send(new GetChannelMessagesQuery(channelId));

        // Generate invite link
        var code = await _mediator.Send(new CreateInviteCommand(serverId, userId));
        InviteLink = $"{Request.Scheme}://{Request.Host}/App/Invite/{code}";

        // Load current user's role for permission-gated UI
        CurrentUserRole = await _mediator.Send(new GetMemberRoleQuery(serverId, userId));

        // Member usernames for @mention autocomplete
        var members = await _uow.Servers.GetServerMembersAsync(serverId);
        MemberUsernames = members.Select(m => m.User.Username).ToList();
        MemberData = members.Select(m => new MemberInfo { Id = m.UserId, Username = m.User.Username, DisplayName = m.User.DisplayName, AvatarUrl = m.User.AvatarUrl }).ToList();
        OnlineMemberIds = _presenceTracker.GetOnlineUsers(MemberData.Select(m => m.Id));
        CurrentUserId = userId;

        // Load orb balance + username
        var currentUser = await _uow.Users.GetByIdAsync(userId);
        OrbBalance = currentUser?.OrbBalance ?? 0;
        CurrentUsername = currentUser?.Username ?? "";

        // Pre-populate voice channel participant lists for sidebar display
        foreach (var ch in Channels.Where(c => c.Type == 1))
        {
            var parts = _voiceTracker.GetParticipants(ch.Id);
            InitialVoiceParticipants[ch.Id] = parts.Select(p => (p.UserId, p.DisplayName, p.AvatarUrl)).ToList();
        }

        // Welcome banner: show for servers under 5 members, unless the server is marked private
        if (members.Count < 5 && InviteLink != null && !(currentServer?.IsPrivate ?? false))
        {
            var refCode = await _uow.Referrals.GetCodeByOwnerIdAsync(userId);
            WelcomeBannerUrl = refCode != null ? $"{InviteLink}?ref={refCode.Code}" : InviteLink;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCreateChannelAsync(Guid serverId, string name, int channelType = 0)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return RedirectToPage("/Auth/Login");
        var userId = Guid.Parse(userIdClaim);
        try
        {
            var type = (Domain.Entities.ChannelType)channelType;
            var channel = await _mediator.Send(new CreateChannelCommand(serverId, name, userId, type));
            return RedirectToPage(new { channelId = channel.Id, serverId });
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    public async Task<IActionResult> OnPostDeleteChannelAsync(Guid channelId, Guid serverId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return RedirectToPage("/Auth/Login");
        var userId = Guid.Parse(userIdClaim);
        try
        {
            await _mediator.Send(new DeleteChannelCommand(channelId, serverId, userId));
            // Redirect to first remaining channel or back to server root
            var channels = await _mediator.Send(new GetServerChannelsQuery(serverId));
            var first = channels.FirstOrDefault();
            if (first != null) return RedirectToPage(new { channelId = first.Id, serverId });
            return RedirectToPage("/App/Index");
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    public async Task<IActionResult> OnPostDeleteMessageAsync(Guid messageId, Guid serverId, Guid channelId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return RedirectToPage("/Auth/Login");
        var userId = Guid.Parse(userIdClaim);
        try
        {
            await _mediator.Send(new DeleteMessageCommand(messageId, serverId, userId));
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        return RedirectToPage(new { channelId, serverId });
    }
}
