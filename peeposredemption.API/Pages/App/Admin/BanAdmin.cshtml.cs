using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.API.Infrastructure;
using peeposredemption.Application.Features.Moderation.Commands;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App.Admin;

public class BanAdminModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;

    public BanAdminModel(IMediator mediator, IUnitOfWork uow, IConfiguration config)
    {
        _mediator = mediator;
        _uow = uow;
        _config = config;
    }

    public List<BannedMember> AllBans { get; set; } = new();
    public List<ServerSummary> Servers { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!IsTorvexOwner()) return Forbid();

        AllBans = await _uow.BannedMembers.GetAllAsync();

        // Build server list from all users' servers
        var allUsers = await _uow.Users.GetAllAsync();
        var serverSet = new Dictionary<Guid, ServerSummary>();

        foreach (var user in allUsers)
        {
            var userServers = await _uow.Servers.GetUserServersAsync(user.Id);
            foreach (var server in userServers)
            {
                if (!serverSet.ContainsKey(server.Id))
                {
                    var members = await _uow.Servers.GetServerMembersAsync(server.Id);
                    var owner = members.FirstOrDefault(m => m.Role == ServerRole.Owner);
                    var ownerUser = owner != null ? await _uow.Users.GetByIdAsync(owner.UserId) : null;
                    var bans = AllBans.Count(b => b.ServerId == server.Id);

                    serverSet[server.Id] = new ServerSummary
                    {
                        Id = server.Id,
                        Name = server.Name,
                        OwnerUsername = ownerUser?.Username ?? "Unknown",
                        MemberCount = members.Count,
                        BanCount = bans,
                        CreatedAt = server.CreatedAt
                    };
                }
            }
        }

        Servers = serverSet.Values.OrderByDescending(s => s.MemberCount).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostUnbanAsync(Guid serverId, Guid targetUserId)
    {
        if (!IsTorvexOwner()) return Forbid();

        var ban = await _uow.BannedMembers.GetAsync(serverId, targetUserId);
        if (ban != null)
        {
            _uow.BannedMembers.Remove(ban);

            var adminUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _uow.ModerationLogs.AddAsync(new ModerationLog
            {
                ServerId = serverId,
                ModeratorId = adminUserId,
                Action = ModerationAction.Unban,
                TargetUserId = targetUserId
            });

            await _uow.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    private bool IsTorvexOwner() => AdminAuthHelper.IsTorvexOwner(User, _config);

    public class ServerSummary
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string OwnerUsername { get; set; } = "";
        public int MemberCount { get; set; }
        public int BanCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
