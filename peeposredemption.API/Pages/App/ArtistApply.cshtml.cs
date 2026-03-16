using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.Features.Artists.Commands;
using peeposredemption.Application.Features.Channels.Queries;
using peeposredemption.Application.Features.Servers.Queries;
using peeposredemption.Domain.Interfaces;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App;

public class ArtistApplyModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _uow;

    public ArtistApplyModel(IMediator mediator, IUnitOfWork uow)
    {
        _mediator = mediator;
        _uow = uow;
    }

    public ServerListViewModel ServerList { get; set; } = new();
    public bool AlreadyApplied { get; set; }
    public string? ErrorMessage { get; set; }
    public bool SubmitSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        await LoadServerListAsync(userId.Value);

        var existing = await _uow.ArtistSubmissions.GetActiveByUserIdAsync(userId.Value);
        AlreadyApplied = existing != null;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string displayName, string email, string portfolioUrl, string? message, List<IFormFile> sampleFiles)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        await LoadServerListAsync(userId.Value);

        if (sampleFiles == null || sampleFiles.Count == 0)
        {
            ErrorMessage = "Please upload at least one sample image.";
            return Page();
        }

        try
        {
            var samples = new List<ArtistSampleFile>();
            foreach (var file in sampleFiles)
            {
                samples.Add(new ArtistSampleFile(
                    file.OpenReadStream(),
                    file.ContentType,
                    file.FileName,
                    file.Length));
            }

            await _mediator.Send(new SubmitArtistApplicationCommand(
                userId.Value,
                displayName,
                email,
                portfolioUrl,
                message,
                samples));

            SubmitSuccess = true;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = ex.Message;
        }

        return Page();
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
