using MediatR;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Security.Queries;

public record GetAltSuspectsQuery(Guid? ServerId = null) : IRequest<List<AltSuspectPairDto>>;

public class AltSuspectPairDto
{
    public Guid UserAId { get; set; }
    public string UserAName { get; set; } = "";
    public Guid UserBId { get; set; }
    public string UserBName { get; set; } = "";
    public int Score { get; set; }
    public List<string> MatchReasons { get; set; } = new();
}

public class GetAltSuspectsQueryHandler : IRequestHandler<GetAltSuspectsQuery, List<AltSuspectPairDto>>
{
    private readonly IUnitOfWork _uow;
    public GetAltSuspectsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<List<AltSuspectPairDto>> Handle(GetAltSuspectsQuery query, CancellationToken ct)
    {
        // Build candidate user list
        List<Guid> userIds;
        if (query.ServerId.HasValue)
        {
            var members = await _uow.Servers.GetServerMembersAsync(query.ServerId.Value);
            userIds = members.Select(m => m.UserId).ToList();
        }
        else
        {
            var allUsers = await _uow.Users.GetAllAsync();
            userIds = allUsers.Select(u => u.Id).ToList();
        }

        if (userIds.Count < 2) return new List<AltSuspectPairDto>();

        // Load signals for all users in scope
        var ipsByUser = new Dictionary<Guid, HashSet<string>>();
        var devicesByUser = new Dictionary<Guid, HashSet<Guid>>();
        var fpsByUser = new Dictionary<Guid, HashSet<string>>();

        foreach (var uid in userIds)
        {
            var ips = await _uow.UserIpLogs.GetByUserIdAsync(uid);
            ipsByUser[uid] = new HashSet<string>(ips.Select(l => l.IpAddress));

            var devs = await _uow.UserDevices.GetByUserIdAsync(uid);
            devicesByUser[uid] = new HashSet<Guid>(devs.Select(d => d.DeviceId));

            var fps = await _uow.UserFingerprints.GetByUserIdAsync(uid);
            fpsByUser[uid] = new HashSet<string>(fps.Select(f => f.FingerprintHash));
        }

        // Load usernames
        var allUsers2 = await _uow.Users.GetAllAsync();
        var usernames = allUsers2.Where(u => userIds.Contains(u.Id))
                                  .ToDictionary(u => u.Id, u => u.Username);

        var pairs = new Dictionary<(Guid, Guid), AltSuspectPairDto>();

        for (int i = 0; i < userIds.Count; i++)
        {
            for (int j = i + 1; j < userIds.Count; j++)
            {
                var a = userIds[i];
                var b = userIds[j];
                var reasons = new List<string>();
                int score = 0;

                // Shared fingerprints (+70 each unique match, capped by count)
                var sharedFps = fpsByUser[a].Intersect(fpsByUser[b]).ToList();
                if (sharedFps.Any())
                {
                    score += 70;
                    reasons.Add($"Shared fingerprint: {sharedFps[0][..Math.Min(16, sharedFps[0].Length)]}…");
                }

                // Shared devices (+60)
                var sharedDevices = devicesByUser[a].Intersect(devicesByUser[b]).ToList();
                if (sharedDevices.Any())
                {
                    score += 60;
                    reasons.Add($"Shared device: {sharedDevices[0]}");
                }

                // Shared IPs (+50)
                var sharedIps = ipsByUser[a].Intersect(ipsByUser[b]).ToList();
                if (sharedIps.Any())
                {
                    score += 50;
                    reasons.Add($"Shared IP: {sharedIps[0]}");
                }

                score = Math.Min(score, 99);

                if (score >= 40)
                {
                    pairs[(a, b)] = new AltSuspectPairDto
                    {
                        UserAId = a,
                        UserAName = usernames.GetValueOrDefault(a, "?"),
                        UserBId = b,
                        UserBName = usernames.GetValueOrDefault(b, "?"),
                        Score = score,
                        MatchReasons = reasons
                    };
                }
            }
        }

        return pairs.Values.OrderByDescending(p => p.Score).ToList();
    }
}
