using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.Features.Channels.Queries;
using peeposredemption.Application.Features.Servers.Queries;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App;

public class VcStatsModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _uow;

    public VcStatsModel(IMediator mediator, IUnitOfWork uow)
    {
        _mediator = mediator;
        _uow = uow;
    }

    public ServerListViewModel ServerList { get; set; } = new();
    public double TotalHours { get; set; }
    public long TotalOrbsEarned { get; set; }
    public List<ServerBreakdown> Breakdown { get; set; } = new();
    public List<RecentSession> RecentSessions { get; set; } = new();
    public long OrbBalance { get; set; }

    public class ServerBreakdown
    {
        public string ServerName { get; set; } = "";
        public double Hours { get; set; }
        public long Orbs { get; set; }
    }

    public class RecentSession
    {
        public DateTime Date { get; set; }
        public string ServerName { get; set; } = "";
        public double DurationMinutes { get; set; }
        public long OrbsEarned { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return RedirectToPage("/Auth/Login");
        var userId = Guid.Parse(userIdClaim);

        // Server list for sidebar
        var servers = await _mediator.Send(new GetUserServersQuery(userId));
        var defaultChannels = new Dictionary<Guid, Guid>();
        foreach (var s in servers)
        {
            var chs = await _mediator.Send(new GetServerChannelsQuery(s.Id));
            var first = chs.FirstOrDefault();
            if (first != null) defaultChannels[s.Id] = first.Id;
        }
        var unreadDms = await _uow.DirectMessages.GetUnreadCountAsync(userId);
        var serverUnreadCounts = await _uow.Notifications.GetUnreadCountByServerAsync(userId);
        var dmUnreadCounts = await _uow.DirectMessages.GetUnreadCountBySenderAsync(userId);
        ServerList = new ServerListViewModel
        {
            Servers = servers,
            ServerDefaultChannels = defaultChannels,
            UnreadCount = unreadDms,
            ServerUnreadCounts = serverUnreadCounts,
            DmUnreadCounts = dmUnreadCounts
        };

        var user = await _uow.Users.GetByIdAsync(userId);
        OrbBalance = user?.OrbBalance ?? 0;

        // VC stats
        TotalHours = await _uow.VoiceSessions.GetTotalHoursAsync(userId);
        TotalOrbsEarned = await _uow.VoiceSessions.GetTotalOrbsEarnedAsync(userId);

        // Breakdown by server
        var breakdownRaw = await _uow.VoiceSessions.GetBreakdownByServerAsync(userId);
        var serverLookup = servers.ToDictionary(s => s.Id, s => s.Name);
        Breakdown = breakdownRaw.Select(b => new ServerBreakdown
        {
            ServerName = serverLookup.GetValueOrDefault(b.ServerId, "Unknown Server"),
            Hours = b.Hours,
            Orbs = b.Orbs
        }).OrderByDescending(b => b.Hours).ToList();

        // Recent sessions
        var sessions = await _uow.VoiceSessions.GetByUserIdAsync(userId, 20);
        RecentSessions = sessions.Select(s => new RecentSession
        {
            Date = s.LeftAt,
            ServerName = serverLookup.GetValueOrDefault(s.ServerId, "Unknown Server"),
            DurationMinutes = (s.LeftAt - s.JoinedAt).TotalMinutes,
            OrbsEarned = s.OrbsEarned
        }).ToList();

        return Page();
    }
}
