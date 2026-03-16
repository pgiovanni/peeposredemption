using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.DTOs.Friends;
using peeposredemption.Application.DTOs.Users;
using peeposredemption.Application.Features.Channels.Queries;
using peeposredemption.Application.Features.Friends.Commands;
using peeposredemption.Application.Features.Friends.Queries;
using peeposredemption.Application.Features.Servers.Commands;
using peeposredemption.Application.Features.Servers.Queries;
using peeposredemption.Domain.Interfaces;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App;

public class IndexModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _uow;

    public IndexModel(IMediator mediator, IUnitOfWork uow)
    {
        _mediator = mediator;
        _uow = uow;
    }

    public ServerListViewModel ServerList { get; set; } = new();
    public List<UserDto> Friends { get; set; } = new();
    public List<FriendRequestDto> PendingRequests { get; set; } = new();
    public UserDto? ActiveFriend { get; set; }
    public Guid? ActiveFriendId { get; set; }
    public List<DmViewModel> DmMessages { get; set; } = new();
    public string? FriendRequestError { get; set; }
    public bool FriendRequestSuccess { get; set; }
    public long OrbBalance { get; set; }
    public int CurrentStreak { get; set; }
    public bool ClaimedToday { get; set; }
    public string? CurrentUserAvatarUrl { get; set; }
    public string CurrentUserDisplayName { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(Guid? friendId)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        await LoadCommonDataAsync(userId.Value);

        if (friendId.HasValue)
        {
            ActiveFriendId = friendId;
            var friendUser = await _uow.Users.GetByIdAsync(friendId.Value);
            if (friendUser != null)
            {
                ActiveFriend = new UserDto(friendUser.Id, friendUser.Username, friendUser.AvatarUrl, friendUser.DisplayName);
                var dms = await _uow.DirectMessages.GetConversationAsync(userId.Value, friendId.Value, 1, 50);
                DmMessages = dms.Select(dm => new DmViewModel
                {
                    Content = dm.Content,
                    SentAt = dm.SentAt,
                    IsMine = dm.SenderId == userId.Value
                }).ToList();

                // Mark DMs from this friend as read
                await _uow.DirectMessages.MarkConversationReadAsync(userId.Value, friendId.Value);
                await _uow.SaveChangesAsync();
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSendFriendRequestAsync(string RecipientUsername)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        var sent = await _mediator.Send(new SendFriendRequestCommand(userId.Value, RecipientUsername));

        await LoadCommonDataAsync(userId.Value);
        FriendRequestSuccess = sent;
        if (!sent) FriendRequestError = "User not found or request already exists.";

        return Page();
    }

    public async Task<IActionResult> OnPostRespondRequestAsync(Guid RequestId, bool Accept)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        await _mediator.Send(new RespondFriendRequestCommand(RequestId, userId.Value, Accept));
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateServerAsync(string Name)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        var server = await _mediator.Send(new CreateServerCommand(userId.Value, Name, null));
        var channels = await _mediator.Send(new GetServerChannelsQuery(server.Id));
        var defaultChannel = channels.FirstOrDefault();

        if (defaultChannel != null)
            return RedirectToPage("/App/Channel", new { channelId = defaultChannel.Id, serverId = server.Id });

        return RedirectToPage();
    }

    private async Task LoadCommonDataAsync(Guid userId)
    {
        var servers = await _mediator.Send(new GetUserServersQuery(userId));
        var defaultChannels = new Dictionary<Guid, Guid>();
        foreach (var s in servers)
        {
            var ch = await _mediator.Send(new GetServerChannelsQuery(s.Id));
            var first = ch.FirstOrDefault();
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

        Friends = await _mediator.Send(new GetFriendsQuery(userId));
        PendingRequests = await _mediator.Send(new GetPendingRequestsQuery(userId));

        // Load orb data
        var currentUser = await _uow.Users.GetByIdAsync(userId);
        OrbBalance = currentUser?.OrbBalance ?? 0;
        CurrentUserAvatarUrl = currentUser?.AvatarUrl;
        CurrentUserDisplayName = currentUser?.DisplayOrUsername ?? "";
        var streak = await _uow.UserLoginStreaks.GetByUserIdAsync(userId);
        CurrentStreak = streak?.CurrentStreak ?? 0;
        var today = DateTime.UtcNow.Date;
        ClaimedToday = streak?.LastClaimedDate.HasValue == true && streak.LastClaimedDate.Value.Date == today;
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim != null ? Guid.Parse(claim) : null;
    }
}

