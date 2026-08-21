using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.Features.Badges.Queries;
using peeposredemption.Application.Features.Channels.Queries;
using peeposredemption.Application.Features.Servers.Queries;
using peeposredemption.Application.Features.Auth.Commands;
using peeposredemption.Application.Features.Users.Commands;
using peeposredemption.Domain.Interfaces;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App;

public class ProfileModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProfileModel> _logger;

    public ProfileModel(IMediator mediator, IUnitOfWork uow, ILogger<ProfileModel> logger)
    {
        _mediator = mediator;
        _uow = uow;
        _logger = logger;
    }

    public ServerListViewModel ServerList { get; set; } = new();

    // Profile data (either own or viewed user)
    public Guid ProfileUserId { get; set; }
    public string Username { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? Bio { get; set; }
    public string? Pronouns { get; set; }
    public string? ProfileBackgroundColor { get; set; }
    public long OrbBalance { get; set; }
    public bool IsOwnProfile { get; set; }
    public List<UserBadgeDto> Badges { get; set; } = new();
    public DateTime MemberSince { get; set; }
    public bool IsMfaEnabled { get; set; }

    public string? ErrorMessage { get; set; }
    public bool SaveSuccess { get; set; }
    public bool PasswordChangeRequested { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid? userId)
    {
        var currentUserId = GetUserId();
        if (currentUserId == null) return RedirectToPage("/Auth/Login");

        // Own profile lives in the Dashboard now — this page only VIEWS profiles
        if (userId == null) return RedirectToPage("/Dashboard", new { tab = "account" });

        await LoadServerListAsync(currentUserId.Value);

        var profileId = userId.Value;
        IsOwnProfile = profileId == currentUserId.Value;

        await LoadProfileAsync(profileId);
        Badges = await _mediator.Send(new GetUserBadgesQuery(profileId));

        return Page();
    }

    // Editing moved to /Dashboard?tab=account (the single profile editor) —
    // this page is now view-only for both own and other profiles.

    private async Task LoadProfileAsync(Guid userId)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null) return;

        ProfileUserId = userId;
        Username = user.Username;
        DisplayName = user.DisplayName;
        AvatarUrl = user.AvatarUrl;
        BannerUrl = user.BannerUrl;
        Bio = user.Bio;
        Pronouns = user.Pronouns;
        ProfileBackgroundColor = user.ProfileBackgroundColor;
        OrbBalance = user.OrbBalance;
        MemberSince = user.CreatedAt;
        IsMfaEnabled = user.IsMfaEnabled;
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim == null ? null : Guid.Parse(claim);
    }

    private async Task LoadServerListAsync(Guid userId)
    {
        var servers = await _mediator.Send(new GetUserServersQuery(userId));
        var defaultChannels = new Dictionary<Guid, Guid>();
        foreach (var s in servers)
        {
            var chs = await _mediator.Send(new GetServerChannelsQuery(s.Id));
            var first = chs.FirstOrDefault();
            if (first != null) defaultChannels[s.Id] = first.Id;
        }
        var unreadDms = await _uow.DirectMessages.GetUnreadCountAsync(userId);
        var unreadPings = await _uow.Notifications.GetUnreadCountAsync(userId);
        var serverUnreadCounts = await _uow.Notifications.GetUnreadCountByServerAsync(userId);
        var dmUnreadCounts = await _uow.DirectMessages.GetUnreadCountBySenderAsync(userId);
        ServerList = new ServerListViewModel
        {
            Servers = servers,
            ServerDefaultChannels = defaultChannels,
            UnreadCount = unreadDms + unreadPings,
            ServerUnreadCounts = serverUnreadCounts,
            DmUnreadCounts = dmUnreadCounts
        };
    }
}
