using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.DTOs.Channels;
using peeposredemption.Application.DTOs.Messages;
using peeposredemption.Application.Features.Channels.Commands;
using peeposredemption.Application.Features.Channels.Queries;
using peeposredemption.Application.Features.Messages.Queries;
using peeposredemption.Application.Features.Servers.Commands;
using peeposredemption.Application.Features.Servers.Queries;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App;

public class ChannelModel : PageModel
{
    private readonly IMediator _mediator;
    public ChannelModel(IMediator mediator) => _mediator = mediator;

    public ServerListViewModel ServerList { get; set; } = new();
    public Guid ChannelId { get; set; }
    public Guid ServerId { get; set; }
    public string ChannelName { get; set; } = "";
    public string ServerName { get; set; } = "";
    public List<MessageDto> Messages { get; set; } = new();
    public List<ChannelDto> Channels { get; set; } = new();
    public string? InviteLink { get; set; }

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
        ServerList = new ServerListViewModel
        {
            Servers = servers,
            ServerDefaultChannels = defaultChannels,
            ActiveServerId = serverId
        };

        // Current server info
        var currentServer = servers.FirstOrDefault(s => s.Id == serverId);
        ServerName = currentServer?.Name ?? "";

        // Load channels for this server
        Channels = await _mediator.Send(new GetServerChannelsQuery(serverId));
        var activeChannel = Channels.FirstOrDefault(c => c.Id == channelId);
        ChannelName = activeChannel?.Name ?? "";

        // Load messages
        Messages = await _mediator.Send(new GetChannelMessagesQuery(channelId));

        // Generate invite link
        var code = await _mediator.Send(new CreateInviteCommand(serverId, userId));
        InviteLink = $"{Request.Scheme}://{Request.Host}/App/Invite/{code}";

        return Page();
    }

    public async Task<IActionResult> OnPostCreateChannelAsync(Guid serverId, string name)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return RedirectToPage("/Auth/Login");

        var channel = await _mediator.Send(new CreateChannelCommand(serverId, name));
        return RedirectToPage(new { channelId = channel.Id, serverId });
    }
}
