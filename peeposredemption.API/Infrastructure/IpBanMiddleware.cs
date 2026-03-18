using Microsoft.Extensions.Caching.Memory;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.API.Infrastructure;

public class IpBanMiddleware
{
    private readonly RequestDelegate _next;
    private const string CacheKey = "ip_bans_set";

    public IpBanMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IUnitOfWork uow, IMemoryCache cache)
    {
        var ip = GetClientIp(context);

        if (!string.IsNullOrEmpty(ip))
        {
            var bannedIps = await cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                var bans = await uow.IpBans.GetAllAsync();
                return new HashSet<string>(
                    bans.Where(b => b.ExpiresAt == null || b.ExpiresAt > DateTime.UtcNow)
                        .Select(b => b.IpAddress),
                    StringComparer.OrdinalIgnoreCase);
            });

            if (bannedIps != null && bannedIps.Contains(ip))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Access denied.");
                return;
            }
        }

        await _next(context);
    }

    public static string? GetClientIp(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
            return realIp.ToString();
        return context.Connection.RemoteIpAddress?.ToString();
    }

    public static void InvalidateCache(IMemoryCache cache) => cache.Remove(CacheKey);
}
