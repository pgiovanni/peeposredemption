using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using peeposredemption.Application.Services;

namespace peeposredemption.API.Infrastructure
{
    public class LinkScannerService : BackgroundService, ILinkScannerService
    {
        private static readonly string[] FallbackDomains =
        [
            // Original IP loggers
            "grabify.link", "iplogger.org", "iplogger.ru", "iplogger.co",
            "blasze.tk", "2no.co", "yip.su", "ps3cfw.com", "bmwforum.co",
            "leancoding.co", "stopify.co", "freegiftcards.co", "lovelocator.co",
            "track.rs", "iplis.ru", "02ip.ru",
            // Grabify shorteners
            "account.beauty", "bit.ly", "canarytokens.org", "community.defconwarningsystem.com",
            "cryp-o.online", "crypto-o.click", "foot.wiki", "gamergirl.pro",
            "gamingfun.me", "imagehub.fun", "imghost.pics", "location.cyou",
            "map-s.online", "mymap.icu", "mymap.quest", "myprivate.pics",
            "photospace.life", "photovault.pics", "photovault.store", "pichost.pics",
            "picshost.pics", "screenshare.pics", "screenshot.best", "screensnaps.top",
            "sharevault.cloud", "shorturl.at", "shrekis.life", "tinyurl.com",
            "xtube.chat", "xx762.de"
        ];

        private const string DomainListUrl =
            "https://raw.githubusercontent.com/piperun/iploggerfilter/master/filterlist";

        private volatile HashSet<string> _domains =
            new(FallbackDomains, StringComparer.OrdinalIgnoreCase);

        private readonly IHttpClientFactory _http;
        private readonly ILogger<LinkScannerService> _logger;

        public LinkScannerService(IHttpClientFactory http, ILogger<LinkScannerService> logger)
        {
            _http = http;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await FetchDomainsAsync();

            using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await FetchDomainsAsync();
        }

        private async Task FetchDomainsAsync()
        {
            try
            {
                var client = _http.CreateClient();
                var text = await client.GetStringAsync(DomainListUrl);
                var domains = text
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(l => !l.StartsWith('#') && !l.StartsWith('!') && l.Contains('.'))
                    .Select(l => l.TrimStart('|').Split('^')[0].Split('$')[0].Trim())
                    .Where(l => l.Length > 0 && l.Contains('.'))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (domains.Count > 0)
                {
                    foreach (var d in FallbackDomains)
                        domains.Add(d);
                    _domains = domains;
                    _logger.LogInformation("Link scanner loaded {Count} domains (remote + custom).", domains.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch IP logger domain list — using fallback ({Count} domains).", _domains.Count);
            }
        }

        public bool ContainsMaliciousLink(string content)
        {
            var lower = content.ToLowerInvariant();
            return _domains.Any(domain => lower.Contains(domain));
        }
    }
}
