using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;
using System.Text.Json;

namespace peeposredemption.Infrastructure.Services;

/// <summary>
/// Scores candidate user pairs for alt-account likelihood and persists AltSuspicion records.
/// Signals and weights:
///   Shared fingerprint +40, shared device +30, shared IP (7d) +20
///   DM recipient overlap >30% +25, server overlap >2 +15, active-hours similarity >85% +20
///   Account created within 24h of each other +10, Tor login +25, VPN login +15
/// </summary>
public class AltDetectionService : IAltDetectionService
{
    private readonly IUnitOfWork _uow;

    public AltDetectionService(IUnitOfWork uow) => _uow = uow;

    public async Task<int> RunScanAsync()
    {
        var allUsers = await _uow.Users.GetAllAsync();
        var userIds = allUsers.Select(u => u.Id).ToList();
        if (userIds.Count < 2) return 0;

        var createdAt = allUsers.ToDictionary(u => u.Id, u => u.CreatedAt);

        // Pre-load signals for every user
        var ips = new Dictionary<Guid, HashSet<string>>();
        var recentIps = new Dictionary<Guid, HashSet<string>>();  // within 7 days
        var devices = new Dictionary<Guid, HashSet<Guid>>();
        var fps = new Dictionary<Guid, HashSet<string>>();
        var servers = new Dictionary<Guid, HashSet<Guid>>();
        var dmRecipients = new Dictionary<Guid, HashSet<Guid>>();
        var msgHours = new Dictionary<Guid, int[]>();
        var vcHours = new Dictionary<Guid, int[]>();
        var torFlags = new Dictionary<Guid, bool>();
        var vpnFlags = new Dictionary<Guid, bool>();

        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        foreach (var uid in userIds)
        {
            var ipLogs = await _uow.UserIpLogs.GetByUserIdAsync(uid);
            ips[uid] = new HashSet<string>(ipLogs.Select(l => l.IpAddress));
            recentIps[uid] = new HashSet<string>(ipLogs
                .Where(l => l.SeenAt >= sevenDaysAgo)
                .Select(l => l.IpAddress));

            var latest = ipLogs.OrderByDescending(l => l.SeenAt).FirstOrDefault();
            torFlags[uid] = latest?.IsTor ?? false;
            vpnFlags[uid] = latest?.IsVpn ?? false;

            var devs = await _uow.UserDevices.GetByUserIdAsync(uid);
            devices[uid] = new HashSet<Guid>(devs.Select(d => d.DeviceId));

            var fpList = await _uow.UserFingerprints.GetByUserIdAsync(uid);
            fps[uid] = new HashSet<string>(fpList.Select(f => f.FingerprintHash));

            var srvList = await _uow.Servers.GetUserServersAsync(uid);
            servers[uid] = new HashSet<Guid>(srvList.Select(s => s.Id));

            var recipientList = await _uow.DirectMessages.GetDistinctRecipientsAsync(uid);
            dmRecipients[uid] = new HashSet<Guid>(recipientList);

            msgHours[uid] = await _uow.Messages.GetHourlyActivityAsync(uid);
            vcHours[uid] = await _uow.VoiceSessions.GetHourlyActivityAsync(uid);
        }

        int newRecords = 0;

        for (int i = 0; i < userIds.Count; i++)
        {
            for (int j = i + 1; j < userIds.Count; j++)
            {
                var a = userIds[i];
                var b = userIds[j];

                int score = 0;
                var signals = new List<string>();

                // Hardware signals
                if (fps[a].Overlaps(fps[b]))
                {
                    score += 40;
                    signals.Add("shared_fingerprint");
                }

                if (devices[a].Overlaps(devices[b]))
                {
                    score += 30;
                    signals.Add("shared_device");
                }

                if (recentIps[a].Overlaps(recentIps[b]))
                {
                    score += 20;
                    signals.Add("shared_ip_7d");
                }

                // Behavioral signals
                double dmOverlap = JaccardSimilarity(
                    dmRecipients[a].Except(new[] { b }).ToHashSet(),
                    dmRecipients[b].Except(new[] { a }).ToHashSet());
                if (dmOverlap > 0.30)
                {
                    score += 25;
                    signals.Add($"dm_recipient_overlap_{dmOverlap:P0}");
                }

                int sharedServers = servers[a].Count(id => servers[b].Contains(id));
                if (sharedServers > 2)
                {
                    score += 15;
                    signals.Add($"server_overlap_{sharedServers}");
                }

                double hourSim = CosineSimilarity(
                    CombineHours(msgHours[a], vcHours[a]),
                    CombineHours(msgHours[b], vcHours[b]));
                if (hourSim > 0.85)
                {
                    score += 20;
                    signals.Add($"active_hours_similarity_{hourSim:P0}");
                }

                // Account creation timing
                var ageDiff = Math.Abs((createdAt[a] - createdAt[b]).TotalHours);
                if (ageDiff < 24)
                {
                    score += 10;
                    signals.Add("created_within_24h");
                }

                // Network signals
                if (torFlags[a] || torFlags[b])
                {
                    score += 25;
                    signals.Add("tor_login");
                }
                if (vpnFlags[a] || vpnFlags[b])
                {
                    score += 15;
                    signals.Add("vpn_login");
                }

                score = Math.Min(score, 99);

                if (score >= 50)
                {
                    var existing = await _uow.AltSuspicions.GetByUserPairAsync(a, b);
                    if (existing == null)
                    {
                        await _uow.AltSuspicions.AddAsync(new AltSuspicion
                        {
                            UserId1 = a,
                            UserId2 = b,
                            Score = score,
                            Signals = JsonSerializer.Serialize(signals),
                            DetectedAt = DateTime.UtcNow
                        });
                        newRecords++;
                    }
                    else if (existing.IsConfirmed == null)
                    {
                        // Re-score pending records
                        existing.Score = score;
                        existing.Signals = JsonSerializer.Serialize(signals);
                        existing.DetectedAt = DateTime.UtcNow;
                    }
                }
            }
        }

        await _uow.SaveChangesAsync();
        return newRecords;
    }

    private static double JaccardSimilarity(HashSet<Guid> a, HashSet<Guid> b)
    {
        if (a.Count == 0 && b.Count == 0) return 0;
        int intersection = a.Count(x => b.Contains(x));
        int union = a.Union(b).Count();
        return union == 0 ? 0 : (double)intersection / union;
    }

    private static int[] CombineHours(int[] msg, int[] vc)
    {
        var combined = new int[24];
        for (int i = 0; i < 24; i++) combined[i] = msg[i] + vc[i];
        return combined;
    }

    private static double CosineSimilarity(int[] a, int[] b)
    {
        double dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < 24; i++)
        {
            dot += a[i] * b[i];
            magA += (double)a[i] * a[i];
            magB += (double)b[i] * b[i];
        }
        if (magA == 0 || magB == 0) return 0;
        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}
