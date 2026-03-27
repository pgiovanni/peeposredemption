using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.Features.Security.Queries;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.API.Pages.App.Admin;

public class SecurityAdminModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _uow;
    private readonly string _adminEmail;

    public SecurityAdminModel(IMediator mediator, IUnitOfWork uow, IConfiguration config)
    {
        _mediator = mediator;
        _uow = uow;
        _adminEmail = config["Email:AdminEmail"] ?? string.Empty;
    }

    public List<IpBanDto> IpBans { get; set; } = new();
    public List<UserSecuritySummary> Users { get; set; } = new();
    public List<UserSecuritySummary> ScamFlags { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!IsTorvexOwner()) return Forbid();

        IpBans = await _mediator.Send(new GetIpBansQuery());

        var allUsers = await _uow.Users.GetAllAsync();
        foreach (var user in allUsers)
        {
            var ipLogs = await _uow.UserIpLogs.GetByUserIdAsync(user.Id);
            var devices = await _uow.UserDevices.GetByUserIdAsync(user.Id);
            var lastIp = ipLogs.FirstOrDefault();

            var summary = new UserSecuritySummary
            {
                UserId = user.Id,
                Username = user.Username,
                IsSuspicious = user.IsSuspicious,
                AccountAgeDays = (int)(DateTime.UtcNow - user.CreatedAt).TotalDays,
                LastIp = lastIp?.IpAddress,
                IsVpn = lastIp?.IsVpn ?? false,
                IsTor = lastIp?.IsTor ?? false,
                DeviceCount = devices.Select(d => d.DeviceId).Distinct().Count(),
                LoginCount = ipLogs.Count,
                LastSeen = lastIp?.SeenAt
            };

            Users.Add(summary);
        }

        Users = Users.OrderByDescending(u => u.LastSeen).ToList();

        // Scam flags: suspicious users with recent DM activity info
        foreach (var u in Users.Where(u => u.IsSuspicious))
        {
            var since = DateTime.UtcNow.AddHours(-24);
            var dmCount = await _uow.DirectMessages.GetRecentRecipientCountAsync(u.UserId, since);
            u.DmsSentLast24h = dmCount;
            ScamFlags.Add(u);
        }

        return Page();
    }

    private bool IsTorvexOwner()
    {
        var emailClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        return !string.IsNullOrEmpty(_adminEmail) &&
               string.Equals(emailClaim, _adminEmail, StringComparison.OrdinalIgnoreCase);
    }

    public class UserSecuritySummary
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = "";
        public bool IsSuspicious { get; set; }
        public int AccountAgeDays { get; set; }
        public string? LastIp { get; set; }
        public bool IsVpn { get; set; }
        public bool IsTor { get; set; }
        public int DeviceCount { get; set; }
        public int LoginCount { get; set; }
        public DateTime? LastSeen { get; set; }
        public int DmsSentLast24h { get; set; }
    }
}
