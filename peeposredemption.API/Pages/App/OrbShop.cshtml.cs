using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.Features.Channels.Queries;
using peeposredemption.Application.Features.Servers.Queries;
using peeposredemption.Domain.Interfaces;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App;

public class OrbShopModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _uow;
    public OrbShopModel(IMediator mediator, IUnitOfWork uow) { _mediator = mediator; _uow = uow; }

    public ServerListViewModel ServerList { get; set; } = new();
    public long OrbBalance { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return RedirectToPage("/Auth/Login");
        var userId = Guid.Parse(userIdClaim);

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

        var currentUser = await _uow.Users.GetByIdAsync(userId);
        OrbBalance = currentUser?.OrbBalance ?? 0;

        return Page();
    }
}
