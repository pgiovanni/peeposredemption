using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Resend;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using peeposredemption.API.Hubs;
using peeposredemption.API.Infrastructure;
using peeposredemption.Application.Features.Auth.Commands;
using peeposredemption.Application.Features.Emoji.Queries;
using peeposredemption.Application.Features.Orbs.Commands;
using peeposredemption.Application.Features.Orbs.Queries;
using peeposredemption.Application.Features.Shop.Commands;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Application.Validators;
using peeposredemption.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Persist DataProtection keys so antiforgery tokens survive restarts
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        builder.Configuration["DataProtection:KeyPath"] ?? "/var/www/peeposredemption-keys"));

// Infrastructure (DbContext, Repos, UoW)
builder.Services.AddInfrastructure(builder.Configuration);

// MediatR — scans Application for all handlers
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<RegisterCommand>());

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<RegisterValidator>();

// Application services
builder.Services.AddScoped<TokenService>();
builder.Services.AddOptions();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
}
else
{
    var resendApiKey = builder.Configuration["Email:ResendApiKey"]
        ?? throw new InvalidOperationException(
            "Email:ResendApiKey is missing from configuration. Emails will not work.");
    builder.Services.AddHttpClient<ResendClient>();
    builder.Services.Configure<ResendClientOptions>(o => o.ApiToken = resendApiKey);
    builder.Services.AddTransient<IResend, ResendClient>();
    builder.Services.AddScoped<IEmailService, EmailService>();
}
builder.Services.AddSingleton<IUserIdProvider, UserIdProvider>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<LinkScannerService>();
builder.Services.AddSingleton<ILinkScannerService>(sp => sp.GetRequiredService<LinkScannerService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<LinkScannerService>());

// VPN/Tor detection
builder.Services.AddSingleton<VpnDetectionService>();
builder.Services.AddSingleton<IVpnDetectionService>(sp => sp.GetRequiredService<VpnDetectionService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<VpnDetectionService>());

builder.Services.AddSingleton<VoiceStateTracker>();
builder.Services.AddSingleton<PresenceTracker>();
builder.Services.AddMemoryCache();

// JWT Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
        };
        // Pass JWT via query string for SignalR WebSocket connections
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx => {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR(o => o.EnableDetailedErrors = true);
builder.Services.AddControllers();
builder.Services.AddRazorPages();

var app = builder.Build();

// Anti-alt security middleware
app.UseMiddleware<IpBanMiddleware>();
app.UseMiddleware<DeviceIdMiddleware>();

app.Use(async (context, next) =>
{
    var token = context.Request.Cookies["jwt"];
    if (!string.IsNullOrEmpty(token))
    {
        context.Request.Headers["Authorization"] = $"Bearer {token}";
        context.Items["CurrentJwt"] = token;
    }
    else if (!string.IsNullOrEmpty(context.Request.Cookies["refreshToken"])
             && !context.Request.Path.StartsWithSegments("/api/auth/refresh")
             && !context.Request.Path.StartsWithSegments("/Auth")
             && !context.Request.Path.StartsWithSegments("/hubs")
             && !context.Request.Path.StartsWithSegments("/webhooks"))
    {
        var mediator = context.RequestServices.GetRequiredService<IMediator>();
        try
        {
            var ip = IpBanMiddleware.GetClientIp(context) ?? "unknown";
            var ua = context.Request.Headers.UserAgent.ToString();
            var devId = context.Items["DeviceId"] is Guid dId ? dId : (Guid?)null;
            var result = await mediator.Send(
                new RefreshTokenCommand(context.Request.Cookies["refreshToken"]!, ip, ua, devId));

            context.Response.Cookies.Append("jwt", result.Token!, new CookieOptions
            {
                HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromMinutes(15)
            });
            context.Response.Cookies.Append("refreshToken", result.RefreshToken!, new CookieOptions
            {
                HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromDays(30)
            });
            context.Request.Headers["Authorization"] = $"Bearer {result.Token}";
            context.Items["CurrentJwt"] = result.Token;
        }
        catch
        {
            context.Response.Cookies.Delete("jwt");
            context.Response.Cookies.Delete("refreshToken");
        }
    }
    await next();
});

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();
app.MapHub<ChatHub>("/hubs/chat");

// Token refresh endpoint
app.MapPost("/api/auth/refresh", async (HttpRequest req, IMediator mediator) =>
{
    var refreshToken = req.Cookies["refreshToken"];
    if (string.IsNullOrEmpty(refreshToken))
        return Results.Unauthorized();

    try
    {
        var ip = IpBanMiddleware.GetClientIp(req.HttpContext) ?? "unknown";
        var ua = req.Headers.UserAgent.ToString();
        var devId = req.HttpContext.Items["DeviceId"] is Guid dId ? dId : (Guid?)null;
        var result = await mediator.Send(new RefreshTokenCommand(refreshToken, ip, ua, devId));

        req.HttpContext.Response.Cookies.Append("jwt", result.Token!, new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromMinutes(15)
        });
        req.HttpContext.Response.Cookies.Append("refreshToken", result.RefreshToken!, new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromDays(30)
        });
        return Results.Ok(new { token = result.Token });
    }
    catch (UnauthorizedAccessException)
    {
        req.HttpContext.Response.Cookies.Delete("jwt");
        req.HttpContext.Response.Cookies.Delete("refreshToken");
        return Results.Unauthorized();
    }
}).AllowAnonymous().DisableAntiforgery();

// ── Channel messages (AJAX channel switching while in voice) ─────────────
app.MapGet("/api/channels/{channelId:guid}/messages", async (Guid channelId, HttpContext ctx, peeposredemption.Domain.Interfaces.IUnitOfWork uow, IMediator mediator) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var channel = await uow.Channels.GetByIdAsync(channelId);
    if (channel == null) return Results.NotFound();
    if (!await uow.Servers.IsMemberAsync(channel.ServerId, Guid.Parse(uid)))
        return Results.Forbid();
    var messages = await mediator.Send(new peeposredemption.Application.Features.Messages.Queries.GetChannelMessagesQuery(channelId));
    return Results.Ok(messages);
}).RequireAuthorization();

// ── Session management API ──────────────────────────────────────────
app.MapGet("/api/sessions", async (HttpContext ctx, IMediator mediator) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var refreshCookie = ctx.Request.Cookies["refreshToken"];
    var currentHash = !string.IsNullOrEmpty(refreshCookie) ? TokenService.HashToken(refreshCookie) : null;
    var sessions = await mediator.Send(new peeposredemption.Application.Features.Sessions.GetActiveSessionsQuery(
        Guid.Parse(uid), currentHash));
    return Results.Ok(sessions);
}).RequireAuthorization();

app.MapDelete("/api/sessions/{id:guid}", async (Guid id, HttpContext ctx, IMediator mediator) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var ok = await mediator.Send(new peeposredemption.Application.Features.Sessions.RevokeSessionCommand(id, Guid.Parse(uid)));
    return ok ? Results.Ok() : Results.NotFound();
}).RequireAuthorization();

app.MapPost("/api/sessions/revoke-others", async (HttpContext ctx, IMediator mediator) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var refreshCookie = ctx.Request.Cookies["refreshToken"];
    if (string.IsNullOrEmpty(refreshCookie)) return Results.BadRequest();
    var count = await mediator.Send(new peeposredemption.Application.Features.Sessions.RevokeOtherSessionsCommand(
        Guid.Parse(uid), TokenService.HashToken(refreshCookie)));
    return Results.Ok(new { revoked = count });
}).RequireAuthorization().DisableAntiforgery();

// Admin session management
app.MapGet("/api/admin/sessions/{userId:guid}", async (Guid userId, HttpContext ctx, IMediator mediator, IConfiguration config) =>
{
    if (!IsTorvexOwner(ctx, config)) return Results.Forbid();
    var sessions = await mediator.Send(new peeposredemption.Application.Features.Sessions.GetActiveSessionsQuery(userId));
    return Results.Ok(sessions);
}).RequireAuthorization();

app.MapDelete("/api/admin/sessions/{userId:guid}/{tokenId:guid}", async (Guid userId, Guid tokenId, HttpContext ctx, IMediator mediator, IConfiguration config) =>
{
    if (!IsTorvexOwner(ctx, config)) return Results.Forbid();
    var ok = await mediator.Send(new peeposredemption.Application.Features.Sessions.RevokeSessionCommand(tokenId, userId));
    return ok ? Results.Ok() : Results.NotFound();
}).RequireAuthorization();

app.MapPost("/api/admin/sessions/{userId:guid}/revoke-all", async (Guid userId, HttpContext ctx, IMediator mediator, IConfiguration config) =>
{
    if (!IsTorvexOwner(ctx, config)) return Results.Forbid();
    var uow = ctx.RequestServices.GetRequiredService<peeposredemption.Domain.Interfaces.IUnitOfWork>();
    await uow.RefreshTokens.RevokeAllForUserAsync(userId);
    await uow.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization().DisableAntiforgery();

// Emoji list API
app.MapGet("/api/servers/{serverId:guid}/emojis", async (Guid serverId, IMediator mediator) =>
{
    var emojis = await mediator.Send(new GetServerEmojisQuery(serverId));
    return Results.Ok(emojis);
}).RequireAuthorization();

// Cross-server emoji list — all emojis from servers the user is in
app.MapGet("/api/users/emojis", async (HttpContext ctx, IMediator mediator) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var emojis = await mediator.Send(new GetUserEmojisQuery(Guid.Parse(uid)));
    return Results.Ok(emojis);
}).RequireAuthorization();

// Orbs API endpoints
app.MapPost("/api/orbs/daily-claim", async (HttpContext ctx, IMediator mediator) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    try
    {
        var result = await mediator.Send(new ClaimDailyLoginCommand(Guid.Parse(uid)));
        return Results.Ok(result);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/orbs/balance", async (HttpContext ctx, IMediator mediator) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var result = await mediator.Send(new GetOrbBalanceQuery(Guid.Parse(uid)));
    return Results.Ok(result);
}).RequireAuthorization();

app.MapGet("/api/orbs/transactions", async (HttpContext ctx, IMediator mediator) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var result = await mediator.Send(new GetOrbTransactionHistoryQuery(Guid.Parse(uid)));
    return Results.Ok(result);
}).RequireAuthorization();

app.MapPost("/api/orbs/purchase", async (HttpContext ctx, IMediator mediator) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();

    var body = await ctx.Request.ReadFromJsonAsync<OrbPurchaseRequest>();
    if (body == null) return Results.BadRequest();

    var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
    var url = await mediator.Send(new CreateOrbPurchaseSessionCommand(Guid.Parse(uid), (OrbPackTier)body.Tier, baseUrl));
    return Results.Ok(new { url });
}).RequireAuthorization();

// Orb gift endpoint
app.MapPost("/api/orbs/gift", async (HttpContext ctx, IMediator mediator, peeposredemption.Domain.Interfaces.IUnitOfWork uow) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();

    var body = await ctx.Request.ReadFromJsonAsync<OrbGiftRequest>();
    if (body == null) return Results.BadRequest();

    var recipient = await uow.Users.GetByUsernameAsync(body.RecipientUsername);
    if (recipient == null) return Results.BadRequest(new { error = "User not found." });

    try
    {
        var result = await mediator.Send(new SendOrbGiftCommand(
            Guid.Parse(uid), recipient.Id, body.Amount,
            string.IsNullOrEmpty(body.ChannelId) ? null : Guid.Parse(body.ChannelId),
            null, body.Message));

        // Broadcast via SignalR
        var hubContext = ctx.RequestServices.GetRequiredService<Microsoft.AspNetCore.SignalR.IHubContext<ChatHub>>();
        var sender = await uow.Users.GetByIdAsync(Guid.Parse(uid));
        var payload = new
        {
            GiftId = result.GiftId,
            SenderUsername = sender?.Username ?? "Unknown",
            SenderId = uid,
            RecipientUsername = body.RecipientUsername,
            RecipientId = recipient.Id.ToString(),
            Amount = body.Amount,
            Message = body.Message
        };

        if (!string.IsNullOrEmpty(body.ChannelId))
            await hubContext.Clients.Group($"channel:{body.ChannelId}").SendAsync("ReceiveOrbGift", payload);

        await hubContext.Clients.User(uid).SendAsync("OrbBalanceUpdated", result.SenderNewBalance);
        await hubContext.Clients.User(recipient.Id.ToString()).SendAsync("OrbBalanceUpdated", result.RecipientNewBalance);
        await hubContext.Clients.User(recipient.Id.ToString()).SendAsync("OrbGiftReceived", payload);

        return Results.Ok(new { senderBalance = result.SenderNewBalance });
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// Referral link tracking
app.MapPost("/api/referral/track-copy", async (HttpContext ctx, peeposredemption.Domain.Interfaces.IUnitOfWork uow) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var code = await uow.Referrals.GetCodeByOwnerIdAsync(Guid.Parse(uid));
    if (code == null) return Results.NotFound();
    code.LinkCopies++;
    await uow.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization();

app.MapPost("/api/referral/track-click", async (HttpContext ctx, peeposredemption.Domain.Interfaces.IUnitOfWork uow) =>
{
    var body = await ctx.Request.ReadFromJsonAsync<ReferralClickRequest>();
    if (body == null || string.IsNullOrWhiteSpace(body.Code)) return Results.BadRequest();
    var code = await uow.Referrals.GetCodeByStringAsync(body.Code);
    if (code == null) return Results.NotFound();
    code.LinkClicks++;
    await uow.SaveChangesAsync();
    return Results.Ok();
}).AllowAnonymous();

// Moderation API endpoints
app.MapPost("/api/moderation/kick", async (HttpContext ctx, IMediator mediator, peeposredemption.Domain.Interfaces.IUnitOfWork uow) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var body = await ctx.Request.ReadFromJsonAsync<ModerationActionRequest>();
    if (body == null) return Results.BadRequest();
    try
    {
        var serverId = Guid.Parse(body.ServerId);
        var actorId = Guid.Parse(uid);
        // MFA enforcement check
        var server = await uow.Servers.GetByIdAsync(serverId);
        if (server?.RequireMfaForModerators == true)
        {
            var actor = await uow.Users.GetByIdAsync(actorId);
            if (actor != null && !actor.IsMfaEnabled)
                return Results.Json(new { error = "This server requires MFA to use moderation powers." }, statusCode: 403);
        }
        await mediator.Send(new peeposredemption.Application.Features.Moderation.Commands.KickMemberCommand(
            serverId, actorId, Guid.Parse(body.TargetUserId)));
        return Results.Ok();
    }
    catch (UnauthorizedAccessException ex) { return Results.Json(new { error = ex.Message }, statusCode: 403); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/moderation/ban", async (HttpContext ctx, IMediator mediator, peeposredemption.Domain.Interfaces.IUnitOfWork uow) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var body = await ctx.Request.ReadFromJsonAsync<ModerationActionRequest>();
    if (body == null) return Results.BadRequest();
    try
    {
        var serverId = Guid.Parse(body.ServerId);
        var actorId = Guid.Parse(uid);
        var targetId = Guid.Parse(body.TargetUserId);

        // MFA enforcement check
        var server = await uow.Servers.GetByIdAsync(serverId);
        if (server?.RequireMfaForModerators == true)
        {
            var actor = await uow.Users.GetByIdAsync(actorId);
            if (actor != null && !actor.IsMfaEnabled)
                return Results.Json(new { error = "This server requires MFA to use moderation powers." }, statusCode: 403);
        }

        await mediator.Send(new peeposredemption.Application.Features.Moderation.Commands.BanMemberCommand(
            serverId, actorId, targetId));

        // Ban device + fingerprint signals for the banned user
        var devices = await uow.UserDevices.GetByUserIdAsync(targetId);
        foreach (var device in devices)
        {
            device.IsBanned = true;
        }

        var fingerprints = await uow.UserFingerprints.GetByUserIdAsync(targetId);
        foreach (var fp in fingerprints)
        {
            var alreadyBanned = await uow.BannedFingerprints.IsBannedAsync(fp.FingerprintHash);
            if (!alreadyBanned)
            {
                await uow.BannedFingerprints.AddAsync(new peeposredemption.Domain.Entities.BannedFingerprint
                {
                    FingerprintHash = fp.FingerprintHash,
                    BannedByUserId = actorId
                });
            }
        }
        await uow.SaveChangesAsync();

        return Results.Ok();
    }
    catch (UnauthorizedAccessException ex) { return Results.Json(new { error = ex.Message }, statusCode: 403); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/moderation/unban", async (HttpContext ctx, IMediator mediator) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var body = await ctx.Request.ReadFromJsonAsync<ModerationActionRequest>();
    if (body == null) return Results.BadRequest();
    try
    {
        await mediator.Send(new peeposredemption.Application.Features.Moderation.Commands.UnbanMemberCommand(
            Guid.Parse(body.ServerId), Guid.Parse(uid), Guid.Parse(body.TargetUserId)));
        return Results.Ok();
    }
    catch (UnauthorizedAccessException ex) { return Results.Json(new { error = ex.Message }, statusCode: 403); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/moderation/mute", async (HttpContext ctx, IMediator mediator, peeposredemption.Domain.Interfaces.IUnitOfWork uow) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var body = await ctx.Request.ReadFromJsonAsync<MuteActionRequest>();
    if (body == null) return Results.BadRequest();
    try
    {
        var serverId = Guid.Parse(body.ServerId);
        var actorId = Guid.Parse(uid);

        // MFA enforcement check
        var server = await uow.Servers.GetByIdAsync(serverId);
        if (server?.RequireMfaForModerators == true)
        {
            var actor = await uow.Users.GetByIdAsync(actorId);
            if (actor != null && !actor.IsMfaEnabled)
                return Results.Json(new { error = "This server requires MFA to use moderation powers." }, statusCode: 403);
        }

        await mediator.Send(new peeposredemption.Application.Features.Moderation.Commands.MuteUserCommand(
            serverId, actorId, Guid.Parse(body.TargetUserId), body.DurationMinutes));
        return Results.Ok();
    }
    catch (UnauthorizedAccessException ex) { return Results.Json(new { error = ex.Message }, statusCode: 403); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// Stripe webhook — must read raw body, no antiforgery
app.MapPost("/webhooks/stripe", async (HttpRequest req, IMediator mediator) =>
{
    string payload;
    using (var reader = new StreamReader(req.Body))
        payload = await reader.ReadToEndAsync();
    var sig = req.Headers["Stripe-Signature"].ToString();
    try
    {
        await mediator.Send(new ProcessStripeWebhookCommand(payload, sig));
        return Results.Ok();
    }
    catch (UnauthorizedAccessException) { return Results.BadRequest(); }
    catch { return Results.Ok(); } // Don't expose internal errors to Stripe
}).AllowAnonymous().DisableAntiforgery();

// Notification endpoints
app.MapGet("/api/notifications", async (HttpContext ctx, peeposredemption.Domain.Interfaces.IUnitOfWork uow) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var notifications = await uow.Notifications.GetRecentAsync(Guid.Parse(uid));
    return Results.Ok(notifications.Select(n => new
    {
        n.Id,
        n.Content,
        n.IsRead,
        n.ServerId,
        n.ChannelId,
        n.CreatedAt,
        FromUsername = n.FromUser?.Username
    }));
}).RequireAuthorization();

app.MapPost("/api/notifications/read", async (HttpContext ctx, peeposredemption.Domain.Interfaces.IUnitOfWork uow) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    await uow.Notifications.MarkAllReadAsync(Guid.Parse(uid));
    await uow.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization();

// Badge endpoints
app.MapGet("/api/badges/progress", async (HttpContext ctx, IMediator mediator) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var result = await mediator.Send(new peeposredemption.Application.Features.Badges.Queries.GetBadgeProgressQuery(Guid.Parse(uid)));
    return Results.Ok(result);
}).RequireAuthorization();

app.MapGet("/api/users/{userId}/badges", async (Guid userId, IMediator mediator) =>
{
    var result = await mediator.Send(new peeposredemption.Application.Features.Badges.Queries.GetUserBadgesQuery(userId));
    return Results.Ok(result);
}).RequireAuthorization();

// Artist dashboard — artist views own earnings
app.MapGet("/api/artists/dashboard", async (HttpContext ctx, IMediator mediator, peeposredemption.Domain.Interfaces.IUnitOfWork uow) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var artist = await uow.Artists.GetByUserIdAsync(Guid.Parse(uid));
    if (artist == null) return Results.NotFound("No artist profile linked to this account.");
    var result = await mediator.Send(new peeposredemption.Application.Features.Artists.Queries.GetArtistDashboardQuery(artist.Id));
    return Results.Ok(result);
}).RequireAuthorization();

// Admin — view all artists + pending payouts
app.MapGet("/api/admin/artists", async (HttpContext ctx, IMediator mediator, IConfiguration config) =>
{
    var emailClaim = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
    var adminEmail = config["Email:AdminEmail"] ?? string.Empty;
    if (string.IsNullOrEmpty(adminEmail) || !string.Equals(emailClaim, adminEmail, StringComparison.OrdinalIgnoreCase))
        return Results.Forbid();
    var result = await mediator.Send(new peeposredemption.Application.Features.Artists.Queries.GetAllArtistsQuery());
    return Results.Ok(result);
}).RequireAuthorization();

// Admin — record a payout
app.MapPost("/api/admin/artists/payout", async (HttpContext ctx, IMediator mediator, IConfiguration config, ArtistPayoutRequest body) =>
{
    var emailClaim = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
    var adminEmail = config["Email:AdminEmail"] ?? string.Empty;
    if (string.IsNullOrEmpty(adminEmail) || !string.Equals(emailClaim, adminEmail, StringComparison.OrdinalIgnoreCase))
        return Results.Forbid();
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    try
    {
        var payoutId = await mediator.Send(new peeposredemption.Application.Features.Artists.Commands.RecordPayoutCommand(
            body.ArtistId, body.AmountCents, body.Reference, Guid.Parse(uid)));
        return Results.Ok(new { PayoutId = payoutId });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
}).RequireAuthorization();

// Security — fingerprint submission
app.MapPost("/api/security/fingerprint", async (HttpContext ctx, IMediator mediator, peeposredemption.Domain.Interfaces.IUnitOfWork uow) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var body = await ctx.Request.ReadFromJsonAsync<FingerprintRequest>();
    if (body == null || string.IsNullOrEmpty(body.FingerprintHash)) return Results.BadRequest();

    // Check if fingerprint is banned — flag account as suspicious
    var isBanned = await uow.BannedFingerprints.IsBannedAsync(body.FingerprintHash);
    if (isBanned)
    {
        var user = await uow.Users.GetByIdAsync(Guid.Parse(uid));
        if (user != null && !user.IsSuspicious)
        {
            user.IsSuspicious = true;
            await uow.SaveChangesAsync();
        }
    }

    await mediator.Send(new peeposredemption.Application.Features.Security.Commands.SubmitFingerprintCommand(
        Guid.Parse(uid), body.FingerprintHash, body.RawComponents));
    return Results.Ok();
}).RequireAuthorization();

// Security admin endpoints
app.MapPost("/api/admin/security/ip-ban", async (HttpContext ctx, IMediator mediator, IConfiguration config, Microsoft.Extensions.Caching.Memory.IMemoryCache cache) =>
{
    if (!IsTorvexOwner(ctx, config)) return Results.Forbid();
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    var body = await ctx.Request.ReadFromJsonAsync<IpBanRequest>();
    if (body == null) return Results.BadRequest();
    await mediator.Send(new peeposredemption.Application.Features.Security.Commands.BanIpCommand(body.IpAddress, Guid.Parse(uid!), body.Reason));
    IpBanMiddleware.InvalidateCache(cache);
    return Results.Ok();
}).RequireAuthorization();

app.MapDelete("/api/admin/security/ip-ban/{id:guid}", async (Guid id, HttpContext ctx, IMediator mediator, IConfiguration config, Microsoft.Extensions.Caching.Memory.IMemoryCache cache) =>
{
    if (!IsTorvexOwner(ctx, config)) return Results.Forbid();
    await mediator.Send(new peeposredemption.Application.Features.Security.Commands.UnbanIpCommand(id));
    IpBanMiddleware.InvalidateCache(cache);
    return Results.Ok();
}).RequireAuthorization();

app.MapPost("/api/admin/security/device-ban", async (HttpContext ctx, IMediator mediator, IConfiguration config, Microsoft.Extensions.Caching.Memory.IMemoryCache cache) =>
{
    if (!IsTorvexOwner(ctx, config)) return Results.Forbid();
    var body = await ctx.Request.ReadFromJsonAsync<DeviceBanRequest>();
    if (body == null) return Results.BadRequest();
    await mediator.Send(new peeposredemption.Application.Features.Security.Commands.BanDeviceCommand(body.DeviceId));
    DeviceIdMiddleware.InvalidateCache(cache);
    return Results.Ok();
}).RequireAuthorization();

app.MapDelete("/api/admin/security/device-ban/{deviceId:guid}", async (Guid deviceId, HttpContext ctx, IMediator mediator, IConfiguration config, Microsoft.Extensions.Caching.Memory.IMemoryCache cache) =>
{
    if (!IsTorvexOwner(ctx, config)) return Results.Forbid();
    await mediator.Send(new peeposredemption.Application.Features.Security.Commands.UnbanDeviceCommand(deviceId));
    DeviceIdMiddleware.InvalidateCache(cache);
    return Results.Ok();
}).RequireAuthorization();

app.MapPost("/api/admin/security/toggle-suspicious", async (HttpContext ctx, IMediator mediator, IConfiguration config) =>
{
    if (!IsTorvexOwner(ctx, config)) return Results.Forbid();
    var body = await ctx.Request.ReadFromJsonAsync<ToggleSuspiciousRequest>();
    if (body == null) return Results.BadRequest();
    await mediator.Send(new peeposredemption.Application.Features.Security.Commands.ToggleSuspiciousCommand(body.TargetUserId, body.IsSuspicious));
    return Results.Ok();
}).RequireAuthorization();

app.MapGet("/api/admin/security/user/{userId:guid}", async (Guid userId, HttpContext ctx, IMediator mediator, IConfiguration config) =>
{
    if (!IsTorvexOwner(ctx, config)) return Results.Forbid();
    var result = await mediator.Send(new peeposredemption.Application.Features.Security.Queries.GetUserSecurityInfoQuery(userId));
    return Results.Ok(result);
}).RequireAuthorization();

app.MapGet("/api/admin/security/ip-bans", async (HttpContext ctx, IMediator mediator, IConfiguration config) =>
{
    if (!IsTorvexOwner(ctx, config)) return Results.Forbid();
    var result = await mediator.Send(new peeposredemption.Application.Features.Security.Queries.GetIpBansQuery());
    return Results.Ok(result);
}).RequireAuthorization();

static bool IsTorvexOwner(HttpContext ctx, IConfiguration config)
{
    var emailClaim = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
    var adminEmail = config["Email:AdminEmail"] ?? string.Empty;
    return !string.IsNullOrEmpty(adminEmail) &&
           string.Equals(emailClaim, adminEmail, StringComparison.OrdinalIgnoreCase);
}

// MFA endpoints
app.MapPost("/api/auth/mfa/verify", async (HttpContext ctx, IMediator mediator) =>
{
    var body = await ctx.Request.ReadFromJsonAsync<MfaVerifyRequest>();
    if (body == null) return Results.BadRequest();
    try
    {
        var result = await mediator.Send(new peeposredemption.Application.Features.Auth.Commands.VerifyMfaCommand(
            body.MfaPendingToken, body.Code));
        return Results.Ok(new { result.Token, result.RefreshToken, result.UserId });
    }
    catch (UnauthorizedAccessException ex) { return Results.Json(new { error = ex.Message }, statusCode: 401); }
}).AllowAnonymous().DisableAntiforgery();

app.MapGet("/api/auth/mfa/setup", async (HttpContext ctx, IMediator mediator) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var result = await mediator.Send(new peeposredemption.Application.Features.Auth.Queries.GenerateMfaSetupQuery(Guid.Parse(uid)));
    return Results.Ok(new { result.Secret, result.QrCodeBase64 });
}).RequireAuthorization();

app.MapPost("/api/auth/mfa/confirm", async (HttpContext ctx, IMediator mediator) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var body = await ctx.Request.ReadFromJsonAsync<MfaConfirmRequest>();
    if (body == null) return Results.BadRequest();
    try
    {
        var codes = await mediator.Send(new peeposredemption.Application.Features.Auth.Commands.ConfirmMfaSetupCommand(
            Guid.Parse(uid), body.Secret, body.Code));
        return Results.Ok(new { RecoveryCodes = codes });
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization().DisableAntiforgery();

app.MapPost("/api/auth/mfa/disable", async (HttpContext ctx, IMediator mediator) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var body = await ctx.Request.ReadFromJsonAsync<MfaDisableRequest>();
    if (body == null) return Results.BadRequest();
    try
    {
        await mediator.Send(new peeposredemption.Application.Features.Auth.Commands.DisableMfaCommand(
            Guid.Parse(uid), body.Code));
        return Results.Ok();
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization().DisableAntiforgery();

// Alt suspects — admin view (all users) — cached 5 min to prevent O(n²) DoS
app.MapGet("/api/admin/alt-suspects", async (HttpContext ctx, IMediator mediator, IConfiguration config, Microsoft.Extensions.Caching.Memory.IMemoryCache cache) =>
{
    if (!IsTorvexOwner(ctx, config)) return Results.Forbid();
    var cached = cache.GetOrCreate("alt_suspects_global", e =>
    {
        e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        return (List<peeposredemption.Application.Features.Security.Queries.AltSuspectPairDto>?)null;
    });
    if (cached != null) return Results.Ok(cached);
    var result = await mediator.Send(new peeposredemption.Application.Features.Security.Queries.GetAltSuspectsQuery());
    cache.Set("alt_suspects_global", result, TimeSpan.FromMinutes(5));
    return Results.Ok(result);
}).RequireAuthorization();

// Alt suspects — server owner view (members of their server only) — cached 5 min
app.MapGet("/api/moderation/alt-suspects", async (HttpContext ctx, IMediator mediator, peeposredemption.Domain.Interfaces.IUnitOfWork uow, Microsoft.Extensions.Caching.Memory.IMemoryCache cache) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var serverIdStr = ctx.Request.Query["serverId"].ToString();
    if (!Guid.TryParse(serverIdStr, out var serverId)) return Results.BadRequest();
    var role = await uow.Servers.GetMemberRoleAsync(serverId, Guid.Parse(uid));
    if (role < peeposredemption.Domain.Entities.ServerRole.Admin) return Results.Forbid();
    var cacheKey = $"alt_suspects_{serverId}";
    var cached = cache.GetOrCreate(cacheKey, e =>
    {
        e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        return (List<peeposredemption.Application.Features.Security.Queries.AltSuspectPairDto>?)null;
    });
    if (cached != null) return Results.Ok(cached);
    var result = await mediator.Send(new peeposredemption.Application.Features.Security.Queries.GetAltSuspectsQuery(serverId));
    cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
    return Results.Ok(result);
}).RequireAuthorization();

// ── Alt Detection Service (on-demand scan) ──────────────────────────
app.MapPost("/api/admin/security/run-alt-scan", async (HttpContext ctx, peeposredemption.Application.Services.IAltDetectionService altSvc, IConfiguration config) =>
{
    if (!IsTorvexOwner(ctx, config)) return Results.Forbid();
    var newRecords = await altSvc.RunScanAsync();
    return Results.Ok(new { newRecords });
}).RequireAuthorization().DisableAntiforgery();

// ── Alt Suspicions from DB (pending review) ──────────────────────────
app.MapGet("/api/admin/alt-suspicions", async (HttpContext ctx, IMediator mediator, IConfiguration config) =>
{
    if (!IsTorvexOwner(ctx, config)) return Results.Forbid();
    var result = await mediator.Send(new peeposredemption.Application.Features.Security.Queries.GetAltSuspicionsQuery());
    return Results.Ok(result);
}).RequireAuthorization();

// ── Review alt suspicion (confirm / dismiss / ban) ───────────────────
app.MapPost("/api/admin/alt-suspicions/{id:guid}/review", async (Guid id, HttpContext ctx, IMediator mediator, IConfiguration config) =>
{
    if (!IsTorvexOwner(ctx, config)) return Results.Forbid();
    var body = await ctx.Request.ReadFromJsonAsync<AltReviewRequest>();
    if (body == null) return Results.BadRequest();
    var ok = await mediator.Send(new peeposredemption.Application.Features.Security.Commands.ReviewAltSuspicionCommand(id, body.Action));
    return ok ? Results.Ok() : Results.NotFound();
}).RequireAuthorization().DisableAntiforgery();

// Report message — creates a TrustSafety support ticket
app.MapPost("/api/report/message", async (HttpContext ctx, IMediator mediator, Microsoft.Extensions.Caching.Memory.IMemoryCache cache) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();

    // Rate limit: 5 reports per user per hour
    var rateKey = $"report_user_{uid}";
    var reportCount = cache.GetOrCreate(rateKey, e => { e.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1); return 0; });
    if (reportCount >= 5)
        return Results.Json(new { error = "You have submitted too many reports. Please try again later." }, statusCode: 429);
    cache.Set(rateKey, reportCount + 1, TimeSpan.FromHours(1));

    var body = await ctx.Request.ReadFromJsonAsync<ReportMessageRequest>();
    if (body == null || body.MessageId == Guid.Empty) return Results.BadRequest();
    try
    {
        await mediator.Send(new peeposredemption.Application.Features.Security.Commands.ReportMessageCommand(
            Guid.Parse(uid), body.MessageId, body.Reason, body.Note));
        return Results.Ok();
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization().DisableAntiforgery();

// Toggle RequireMfaForModerators on a server (server owner only)
app.MapPost("/api/servers/{serverId:guid}/require-mfa", async (Guid serverId, HttpContext ctx, peeposredemption.Domain.Interfaces.IUnitOfWork uow) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var role = await uow.Servers.GetMemberRoleAsync(serverId, Guid.Parse(uid));
    if (role != peeposredemption.Domain.Entities.ServerRole.Owner) return Results.Forbid();
    var server = await uow.Servers.GetByIdAsync(serverId);
    if (server == null) return Results.NotFound();
    var body = await ctx.Request.ReadFromJsonAsync<RequireMfaToggleRequest>();
    if (body == null) return Results.BadRequest();
    server.RequireMfaForModerators = body.Enabled;
    await uow.SaveChangesAsync();
    return Results.Ok(new { server.RequireMfaForModerators });
}).RequireAuthorization().DisableAntiforgery();

// Reorder servers for the current user
app.MapPost("/api/servers/reorder", async (HttpContext ctx, peeposredemption.Domain.Interfaces.IUnitOfWork uow) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var body = await ctx.Request.ReadFromJsonAsync<ServerReorderRequest>();
    if (body == null || body.ServerIds.Count == 0) return Results.BadRequest();
    await uow.Servers.ReorderServersAsync(Guid.Parse(uid), body.ServerIds);
    await uow.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization().DisableAntiforgery();

// Toggle IsPrivate on a server (server owner only)
app.MapPost("/api/servers/{serverId:guid}/private", async (Guid serverId, HttpContext ctx, peeposredemption.Domain.Interfaces.IUnitOfWork uow) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var role = await uow.Servers.GetMemberRoleAsync(serverId, Guid.Parse(uid));
    if (role != peeposredemption.Domain.Entities.ServerRole.Owner) return Results.Forbid();
    var server = await uow.Servers.GetByIdAsync(serverId);
    if (server == null) return Results.NotFound();
    var body = await ctx.Request.ReadFromJsonAsync<PrivateServerToggleRequest>();
    if (body == null) return Results.BadRequest();
    server.IsPrivate = body.Enabled;
    await uow.SaveChangesAsync();
    return Results.Ok(new { server.IsPrivate });
}).RequireAuthorization().DisableAntiforgery();

// ICE servers endpoint for WebRTC — generates ephemeral TURN credentials via HMAC-SHA1
app.MapGet("/api/ice-servers", (IConfiguration config) =>
{
    var urls = (config["Turn:Urls"] ?? "stun:stun.l.google.com:19302")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    var sharedSecret = config["Turn:SharedSecret"] ?? "";
    var ttl = int.TryParse(config["Turn:CredentialTtlSeconds"], out var t) ? t : 86400;

    // Ephemeral credentials: username = expiry timestamp, password = HMAC-SHA1(secret, username)
    var expiry = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ttl;
    var username = expiry.ToString();
    var credential = "";
    if (!string.IsNullOrEmpty(sharedSecret))
    {
        using var hmac = new System.Security.Cryptography.HMACSHA1(Encoding.UTF8.GetBytes(sharedSecret));
        credential = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(username)));
    }

    var servers = new List<object>();
    foreach (var url in urls)
    {
        if (url.StartsWith("stun:"))
            servers.Add(new { urls = url });
        else
            servers.Add(new { urls = url, username, credential });
    }
    return Results.Ok(servers);
}).RequireAuthorization();

// Seed badge definitions + artists on startup
using (var scope = app.Services.CreateScope())
{
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
    await mediator.Send(new peeposredemption.Application.Features.Badges.Commands.SeedBadgeDefinitionsCommand());
    await mediator.Send(new peeposredemption.Application.Features.Artists.Commands.SeedArtistsCommand());
    await mediator.Send(new peeposredemption.Application.Features.Game.Commands.SeedGameDataCommand());
}

app.Run();

record OrbPurchaseRequest(int Tier);
record OrbGiftRequest(string ChannelId, string RecipientUsername, long Amount, string? Message);
record ArtistPayoutRequest(Guid ArtistId, long AmountCents, string? Reference);
record ModerationActionRequest(string ServerId, string TargetUserId);
record MuteActionRequest(string ServerId, string TargetUserId, int DurationMinutes = 10);
record FingerprintRequest(string FingerprintHash, string? RawComponents);
record IpBanRequest(string IpAddress, string? Reason);
record DeviceBanRequest(Guid DeviceId);
record ToggleSuspiciousRequest(Guid TargetUserId, bool IsSuspicious);
record MfaVerifyRequest(string MfaPendingToken, string Code);
record MfaConfirmRequest(string Secret, string Code);
record MfaDisableRequest(string Code);
record ReferralClickRequest(string Code);
record ReportMessageRequest(Guid MessageId, string Reason, string? Note);
record RequireMfaToggleRequest(bool Enabled);
record PrivateServerToggleRequest(bool Enabled);
record AltReviewRequest(string Action); // "confirm" | "dismiss" | "ban"
record ServerReorderRequest(List<Guid> ServerIds);
