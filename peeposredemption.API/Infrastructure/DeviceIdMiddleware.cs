using Microsoft.Extensions.Caching.Memory;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.API.Infrastructure;

public class DeviceIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CookieName = "torvex_device_id";
    private const string CacheKey = "device_bans_set";

    public DeviceIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IUnitOfWork uow, IMemoryCache cache)
    {
        Guid deviceId;
        if (!context.Request.Cookies.TryGetValue(CookieName, out var deviceIdStr)
            || !Guid.TryParse(deviceIdStr, out deviceId))
        {
            deviceId = Guid.NewGuid();
            context.Response.Cookies.Append(CookieName, deviceId.ToString(), new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromDays(3650)
            });
        }

        context.Items["DeviceId"] = deviceId;

        var bannedDevices = await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            // Query all banned device IDs
            return await GetBannedDeviceIds(uow);
        });

        if (bannedDevices != null && bannedDevices.Contains(deviceId))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Access denied.");
            return;
        }

        await _next(context);
    }

    private static Task<HashSet<Guid>> GetBannedDeviceIds(IUnitOfWork uow) =>
        uow.UserDevices.GetAllBannedDeviceIdsAsync();

    public static void InvalidateCache(IMemoryCache cache) => cache.Remove(CacheKey);
}
