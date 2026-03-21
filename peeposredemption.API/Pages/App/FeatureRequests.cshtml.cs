using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.Features.Channels.Queries;
using peeposredemption.Application.Features.Servers.Queries;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App;

public class FeatureRequestsModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _uow;

    public FeatureRequestsModel(IMediator mediator, IUnitOfWork uow)
    {
        _mediator = mediator;
        _uow = uow;
    }

    public ServerListViewModel ServerList { get; set; } = new();
    public long OrbBalance { get; set; }
    public List<FeatureRequest> MyRequests { get; set; } = new();
    public bool SubmitSuccess { get; set; }

    [BindProperty]
    public string Title { get; set; } = "";

    [BindProperty]
    public string Description { get; set; } = "";

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        await LoadDataAsync(userId.Value);
        SubmitSuccess = TempData["SubmitSuccess"] is true;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        if (string.IsNullOrWhiteSpace(Title) || Title.Length > 100)
        {
            await LoadDataAsync(userId.Value);
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Description) || Description.Length > 1000)
        {
            await LoadDataAsync(userId.Value);
            return Page();
        }

        var request = new FeatureRequest
        {
            UserId = userId.Value,
            Title = Title.Trim(),
            Description = Description.Trim()
        };

        await _uow.FeatureRequests.AddAsync(request);
        await _uow.SaveChangesAsync();

        TempData["SubmitSuccess"] = true;
        return RedirectToPage();
    }

    private async Task LoadDataAsync(Guid userId)
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

        MyRequests = await _uow.FeatureRequests.GetByUserIdAsync(userId);
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim == null ? null : Guid.Parse(claim);
    }
}
