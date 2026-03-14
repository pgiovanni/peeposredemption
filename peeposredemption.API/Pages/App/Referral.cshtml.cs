using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.Features.Channels.Queries;
using peeposredemption.Application.Features.Servers.Queries;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App;

public class ReferralModel : PageModel
{
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public ReferralModel(IUnitOfWork uow, IMediator mediator) { _uow = uow; _mediator = mediator; }

    public ReferralCode MyCode { get; set; }
    public int Signups { get; set; }
    public List<ReferralPurchase> Purchases { get; set; } = new();
    public string ReferralLink { get; set; } = string.Empty;
    public ServerListViewModel ServerList { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        // Server list for sidebar
        var servers = await _mediator.Send(new GetUserServersQuery(userId.Value));
        var defaultChannels = new Dictionary<Guid, Guid>();
        foreach (var s in servers)
        {
            var chs = await _mediator.Send(new GetServerChannelsQuery(s.Id));
            var first = chs.FirstOrDefault();
            if (first != null) defaultChannels[s.Id] = first.Id;
        }
        var unreadDms = await _uow.DirectMessages.GetUnreadCountAsync(userId.Value);
        var unreadPings = await _uow.Notifications.GetUnreadCountAsync(userId.Value);
        var serverUnreadCounts = await _uow.Notifications.GetUnreadCountByServerAsync(userId.Value);
        var dmUnreadCounts = await _uow.DirectMessages.GetUnreadCountBySenderAsync(userId.Value);
        ServerList = new ServerListViewModel
        {
            Servers = servers,
            ServerDefaultChannels = defaultChannels,
            UnreadCount = unreadDms + unreadPings,
            ServerUnreadCounts = serverUnreadCounts,
            DmUnreadCounts = dmUnreadCounts
        };

        var code = await _uow.Referrals.GetCodeByOwnerIdAsync(userId.Value);
        if (code == null)
        {
            code = new ReferralCode { OwnerId = userId.Value };
            await _uow.Referrals.AddCodeAsync(code);
            await _uow.SaveChangesAsync();
        }

        MyCode = code;
        Signups = await _uow.Referrals.GetReferredUserCountAsync(code.Id);
        Purchases = await _uow.Referrals.GetPurchasesByCodeIdAsync(code.Id);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        ReferralLink = $"{baseUrl}/Auth/Register?ref={code.Code}";

        return Page();
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim == null ? null : Guid.Parse(claim);
    }
}
