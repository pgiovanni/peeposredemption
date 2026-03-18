using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using peeposredemption.Application.Services;

namespace peeposredemption.API.Infrastructure;

public class VpnDetectionService : BackgroundService, IVpnDetectionService
{
    private const string TorExitListUrl = "https://check.torproject.org/torbulkexitlist";
    private const string IpApiUrl = "http://ip-api.com/json";

    private volatile HashSet<string> _torExitNodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, (bool IsVpn, bool IsTor, DateTime CachedAt)> _cache = new();

    private readonly IHttpClientFactory _http;
    private readonly ILogger<VpnDetectionService> _logger;

    public VpnDetectionService(IHttpClientFactory http, ILogger<VpnDetectionService> logger)
    {
        _http = http;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await FetchTorExitNodesAsync();

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await FetchTorExitNodesAsync();
    }

    private async Task FetchTorExitNodesAsync()
    {
        try
        {
            var client = _http.CreateClient();
            var text = await client.GetStringAsync(TorExitListUrl);
            var nodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith('#'))
                    nodes.Add(trimmed);
            }
            _torExitNodes = nodes;
            _logger.LogInformation("Loaded {Count} Tor exit nodes", nodes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Tor exit node list");
        }
    }

    public async Task<(bool IsVpn, bool IsTor)> CheckIpAsync(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress) || ipAddress == "unknown")
            return (false, false);

        // Check cache (1h TTL)
        if (_cache.TryGetValue(ipAddress, out var cached) &&
            cached.CachedAt > DateTime.UtcNow.AddHours(-1))
            return (cached.IsVpn, cached.IsTor);

        var isTor = _torExitNodes.Contains(ipAddress);
        var isVpn = false;

        try
        {
            var client = _http.CreateClient();
            var response = await client.GetStringAsync($"{IpApiUrl}/{ipAddress}?fields=proxy,hosting");
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;
            isVpn = (root.TryGetProperty("proxy", out var proxy) && proxy.GetBoolean()) ||
                    (root.TryGetProperty("hosting", out var hosting) && hosting.GetBoolean());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check VPN status for {Ip}", ipAddress);
        }

        _cache[ipAddress] = (isVpn, isTor, DateTime.UtcNow);
        return (isVpn, isTor);
    }
}
