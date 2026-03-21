using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.Features.Channels.Queries;
using peeposredemption.Application.Features.Servers.Queries;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App;

public class ReportModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _email;

    public ReportModel(IMediator mediator, IUnitOfWork uow, IEmailService email)
    {
        _mediator = mediator;
        _uow = uow;
        _email = email;
    }

    public ServerListViewModel ServerList { get; set; } = new();
    public long OrbBalance { get; set; }
    public List<SupportTicket> MyTickets { get; set; } = new();
    public bool SubmitSuccess { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    [BindProperty]
    public string Subject { get; set; } = "";

    [BindProperty]
    public string Description { get; set; } = "";

    [BindProperty]
    public SupportTicketCategory SelectedCategory { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        await LoadDataAsync(userId.Value);
        SubmitSuccess = TempData["SubmitSuccess"] is true;

        // Pre-select category from query param
        if (!string.IsNullOrEmpty(Category) && Enum.TryParse<SupportTicketCategory>(Category, true, out var cat))
            SelectedCategory = cat;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        if (string.IsNullOrWhiteSpace(Subject) || Subject.Length > 150)
        {
            await LoadDataAsync(userId.Value);
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Description) || Description.Length > 2000)
        {
            await LoadDataAsync(userId.Value);
            return Page();
        }

        var ticket = new SupportTicket
        {
            UserId = userId.Value,
            Category = SelectedCategory,
            Subject = Subject.Trim(),
            Description = Description.Trim()
        };

        await _uow.SupportTickets.AddAsync(ticket);
        await _uow.SaveChangesAsync();

        var user = await _uow.Users.GetByIdAsync(userId.Value);
        var catLabel = SelectedCategory switch
        {
            SupportTicketCategory.BugReport => "Bug Report",
            SupportTicketCategory.AccountHelp => "Account Help",
            SupportTicketCategory.GeneralQuestion => "General Question",
            SupportTicketCategory.TrustSafety => "Trust & Safety",
            _ => SelectedCategory.ToString()
        };
        _ = _email.SendSupportTicketNotificationAsync(
            user?.Username ?? "Unknown", catLabel, ticket.Subject, ticket.Description);

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

        MyTickets = await _uow.SupportTickets.GetByUserIdAsync(userId);
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim == null ? null : Guid.Parse(claim);
    }
}
