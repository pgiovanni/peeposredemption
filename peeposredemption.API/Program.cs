using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
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

// Block admin paths from non-admin host — /App/Admin/* and /api/admin/* are only reachable via admin.torvex.app
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    var host = context.Request.Host.Host;
    var isAdminPath = path.StartsWith("/App/Admin", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase);
    if (isAdminPath && !host.Equals("admin.torvex.app", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = 404;
        return;
    }
    await next();
});

// Catch antiforgery 400s and redirect to login with a helpful message instead of blank Chrome error page
app.Use(async (context, next) =>
{
    await next();
    if (context.Response.StatusCode == 400 && context.Response.ContentLength is null or 0
        && context.Request.Method == "POST"
        && (context.Request.Path.StartsWithSegments("/Auth/Login")
            || context.Request.Path.StartsWithSegments("/Auth/Register")
            || context.Request.Path.StartsWithSegments("/Auth/MfaVerify")))
    {
        var path = context.Request.Path.Value ?? "/Auth/Login";
        context.Response.Redirect($"{path}?error=session");
    }
});

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
                HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax,
                Domain = ".torvex.app", MaxAge = TimeSpan.FromMinutes(15)
            });
            context.Response.Cookies.Append("refreshToken", result.RefreshToken!, new CookieOptions
            {
                HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax,
                Domain = ".torvex.app", MaxAge = TimeSpan.FromDays(30)
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
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax,
            Domain = ".torvex.app", MaxAge = TimeSpan.FromMinutes(15)
        });
        req.HttpContext.Response.Cookies.Append("refreshToken", result.RefreshToken!, new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax,
            Domain = ".torvex.app", MaxAge = TimeSpan.FromDays(30)
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

// Account switcher — validates a stored JWT (ignores expiry), issues a fresh session
app.MapPost("/api/auth/switch", async (HttpRequest req, IConfiguration config, peeposredemption.Domain.Interfaces.IUnitOfWork uow) =>
{
    var body = await req.ReadFromJsonAsync<SwitchAccountRequest>();
    if (string.IsNullOrEmpty(body?.Jwt)) return Results.BadRequest();

    var tokenService = new peeposredemption.Application.Services.TokenService(config);
    var principal = tokenService.ValidateTokenForSwitch(body.Jwt);
    if (principal == null) return Results.Unauthorized();

    var userIdStr = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

    var user = await uow.Users.GetByIdAsync(userId);
    if (user == null) return Results.Unauthorized();

    var newJwt = tokenService.GenerateToken(user);
    var newRefresh = tokenService.GenerateRefreshToken();
    var ip = IpBanMiddleware.GetClientIp(req.HttpContext) ?? "unknown";
    var ua = req.Headers.UserAgent.ToString();
    await uow.RefreshTokens.AddAsync(new peeposredemption.Domain.Entities.RefreshToken
    {
        Token = peeposredemption.Application.Services.TokenService.HashToken(newRefresh),
        UserId = userId,
        ExpiresAt = DateTime.UtcNow.AddDays(30),
        IpAddress = ip,
        UserAgent = ua,
    });
    await uow.SaveChangesAsync();

    req.HttpContext.Response.Cookies.Append("jwt", newJwt, new CookieOptions
    {
        HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax,
        Domain = ".torvex.app", MaxAge = TimeSpan.FromMinutes(15)
    });
    req.HttpContext.Response.Cookies.Append("refreshToken", newRefresh, new CookieOptions
    {
        HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax,
        Domain = ".torvex.app", MaxAge = TimeSpan.FromDays(30)
    });

    return Results.Ok(new { jwt = newJwt, username = user.Username, avatarUrl = user.AvatarUrl });
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

// ── File attachment upload ──────────────────────────────────────────
app.MapPost("/api/channels/{channelId:guid}/upload", async (
    Guid channelId,
    HttpContext ctx,
    peeposredemption.Domain.Interfaces.IUnitOfWork uow,
    peeposredemption.Application.Services.IR2StorageService r2,
    peeposredemption.Application.Services.IImageProcessingService imgProc) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var userId = Guid.Parse(uid);

    // Confirm channel exists and user is a member
    var channel = await uow.Channels.GetByIdAsync(channelId);
    if (channel == null) return Results.NotFound("Channel not found.");
    if (!await uow.Servers.IsMemberAsync(channel.ServerId, userId))
        return Results.Forbid();

    // Read uploaded file
    var form = await ctx.Request.ReadFormAsync();
    var file = form.Files.GetFile("file");
    if (file == null || file.Length == 0)
        return Results.BadRequest(new { error = "No file provided." });

    // Determine size limit: Gold users get 50MB, free users get 8MB
    var goldSub = await uow.GoldSubscriptions.GetByUserIdAsync(userId);
    var isGold = goldSub is { Status: peeposredemption.Domain.Entities.SubscriptionStatus.Active };
    long maxBytes = isGold ? 50L * 1024 * 1024 : 8L * 1024 * 1024;

    if (file.Length > maxBytes)
    {
        var limitMb = maxBytes / (1024 * 1024);
        return Results.BadRequest(new { error = $"File too large. Limit: {limitMb}MB." });
    }

    // Allowed MIME types + magic byte signatures
    var allowedImageMimes = new HashSet<string> { "image/jpeg", "image/png", "image/gif", "image/webp" };
    var allowedAudioMimes = new HashSet<string> { "audio/mpeg", "audio/ogg", "audio/wav", "audio/wave", "audio/x-wav" };

    // Magic byte signatures: mime → (offset, bytes)
    var magicBytes = new Dictionary<string, (int offset, byte[] sig)>
    {
        ["image/jpeg"] = (0, new byte[] { 0xFF, 0xD8, 0xFF }),
        ["image/png"]  = (0, new byte[] { 0x89, 0x50, 0x4E, 0x47 }),
        ["image/gif"]  = (0, new byte[] { 0x47, 0x49, 0x46 }),
        ["image/webp"] = (8, new byte[] { 0x57, 0x45, 0x42, 0x50 }),
        ["audio/mpeg"] = (0, new byte[] { 0xFF, 0xFB }),      // MP3
        ["audio/mpeg_id3"] = (0, new byte[] { 0x49, 0x44, 0x33 }), // ID3 MP3
        ["audio/ogg"]  = (0, new byte[] { 0x4F, 0x67, 0x67, 0x53 }),
        ["audio/wav"]  = (0, new byte[] { 0x52, 0x49, 0x46, 0x46 }),
    };

    var contentType = file.ContentType?.ToLower() ?? "";
    var isImage = allowedImageMimes.Contains(contentType);
    var isAudio = allowedAudioMimes.Contains(contentType);

    if (!isImage && !isAudio)
        return Results.BadRequest(new { error = "File type not allowed. Images and audio only." });

    // Read file into memory for magic byte check (and processing)
    using var ms = new MemoryStream();
    await file.CopyToAsync(ms);
    ms.Position = 0;
    var header = ms.ToArray().Take(12).ToArray();

    // Validate magic bytes — map audio/wav, audio/wave, audio/x-wav → "audio/wav"
    var normalizedMime = contentType switch
    {
        "audio/wave" or "audio/x-wav" => "audio/wav",
        "audio/mpeg" => header.Length >= 3 && header[0] == 0x49 ? "audio/mpeg_id3" : "audio/mpeg",
        _ => contentType
    };

    var validMagic = false;
    if (magicBytes.TryGetValue(normalizedMime, out var magic))
    {
        var slice = header.Skip(magic.offset).Take(magic.sig.Length).ToArray();
        validMagic = slice.SequenceEqual(magic.sig);
    }
    // WEBP special case: needs both RIFF and WEBP markers
    if (normalizedMime == "image/webp")
    {
        var riff = header.Take(4).ToArray();
        var webp = header.Skip(8).Take(4).ToArray();
        validMagic = riff.SequenceEqual(new byte[] { 0x52, 0x49, 0x46, 0x46 })
                  && webp.SequenceEqual(new byte[] { 0x57, 0x45, 0x42, 0x50 });
    }

    if (!validMagic)
        return Results.BadRequest(new { error = "File contents do not match its declared type." });

    // Images: re-encode via ImageSharp to strip EXIF and neutralize polyglots
    Stream uploadStream;
    string uploadContentType;
    if (isImage)
    {
        ms.Position = 0;
        var (processed, processedType) = await imgProc.ProcessAsync(ms, contentType);
        uploadStream = processed;
        uploadContentType = processedType;
    }
    else
    {
        ms.Position = 0;
        uploadStream = ms;
        uploadContentType = contentType == "audio/wave" || contentType == "audio/x-wav" ? "audio/wav" : contentType;
    }

    // Generate unguessable key and upload
    var key = Guid.NewGuid().ToString("N");
    var ext = uploadContentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png"  => ".png",
        "image/webp" => ".webp",
        "audio/mpeg" => ".mp3",
        "audio/ogg"  => ".ogg",
        "audio/wav"  => ".wav",
        _            => ""
    };

    string url;
    try
    {
        url = await r2.UploadAttachmentAsync(key + ext, uploadStream, uploadContentType);
    }
    finally
    {
        await uploadStream.DisposeAsync();
    }

    // Save pending attachment record (MessageId = null until send)
    var attachment = new peeposredemption.Domain.Entities.MessageAttachment
    {
        ChannelId = channelId,
        UploaderId = userId,
        R2Key = $"attachments/{key}{ext}",
        Url = url,
        FileName = file.FileName,
        FileSize = file.Length,
        ContentType = uploadContentType
    };
    await uow.MessageAttachments.AddAsync(attachment);
    await uow.SaveChangesAsync();

    return Results.Ok(new
    {
        attachmentId = attachment.Id,
        url = attachment.Url,
        fileName = attachment.FileName,
        contentType = attachment.ContentType
    });
}).RequireAuthorization().DisableAntiforgery();

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

app.MapGet("/api/game/coins", async (HttpContext ctx, peeposredemption.Infrastructure.Persistence.AppDbContext db) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var player = await db.PlayerCharacters.FirstOrDefaultAsync(p => p.UserId == Guid.Parse(uid));
    return Results.Ok(new { coins = player == null ? 0L : player.CoinBalance });
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

// Torvex Gold endpoints
app.MapPost("/api/gold/subscribe", async (HttpContext ctx, IMediator mediator) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
    try
    {
        var url = await mediator.Send(new peeposredemption.Application.Features.Shop.Commands.CreateGoldSubscriptionSessionCommand(Guid.Parse(uid), baseUrl));
        return Results.Ok(new { url });
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/gold/cancel", async (HttpContext ctx, IMediator mediator) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    try
    {
        await mediator.Send(new peeposredemption.Application.Features.Shop.Commands.CancelGoldSubscriptionCommand(Guid.Parse(uid)));
        return Results.Ok();
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/gold/status", async (HttpContext ctx, IMediator mediator) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var result = await mediator.Send(new peeposredemption.Application.Features.Shop.Queries.GetGoldSubscriptionQuery(Guid.Parse(uid)));
    return Results.Ok(result);
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
    if (!IsTorvexOwner(ctx, config)) return Results.Forbid();
    var result = await mediator.Send(new peeposredemption.Application.Features.Artists.Queries.GetAllArtistsQuery());
    return Results.Ok(result);
}).RequireAuthorization();

// Admin — record a payout
app.MapPost("/api/admin/artists/payout", async (HttpContext ctx, IMediator mediator, IConfiguration config, ArtistPayoutRequest body) =>
{
    if (!IsTorvexOwner(ctx, config)) return Results.Forbid();
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

app.MapPost("/api/admin/security/clear-login-lockout", (HttpContext ctx, IConfiguration config, Microsoft.Extensions.Caching.Memory.IMemoryCache cache) =>
{
    if (!IsTorvexOwner(ctx, config)) return Results.Forbid();
    var body = ctx.Request.ReadFromJsonAsync<ClearLockoutRequest>().GetAwaiter().GetResult();
    if (body == null || string.IsNullOrWhiteSpace(body.IpAddress)) return Results.BadRequest("IP required.");
    cache.Remove($"login_fail:{body.IpAddress}");
    return Results.Ok(new { cleared = true, ip = body.IpAddress });
}).RequireAuthorization().DisableAntiforgery();

app.MapGet("/api/admin/security/ip-bans", async (HttpContext ctx, IMediator mediator, IConfiguration config) =>
{
    if (!IsTorvexOwner(ctx, config)) return Results.Forbid();
    var result = await mediator.Send(new peeposredemption.Application.Features.Security.Queries.GetIpBansQuery());
    return Results.Ok(result);
}).RequireAuthorization();

static bool IsTorvexOwner(HttpContext ctx, IConfiguration config) =>
    AdminAuthHelper.IsTorvexOwner(ctx.User, config, ctx.Request.Headers);

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
    var expansionDb = scope.ServiceProvider.GetRequiredService<peeposredemption.Infrastructure.Persistence.AppDbContext>();
    await peeposredemption.API.Infrastructure.GameExpansionSeeder.SeedAsync(expansionDb);
}

// =====================================================================
// BOT API — secured by X-Bot-Key header
// =====================================================================
// Fixed channel Guid representing "Discord Bot" context for game sessions
var discordBotChannelId = Guid.Parse("00000000-0000-0000-dcdc-000000000001");

bool BotAuth(HttpContext ctx, IConfiguration cfg)
{
    var key = cfg["Bot:ApiKey"];
    if (string.IsNullOrEmpty(key)) return false;
    return ctx.Request.Headers.TryGetValue("X-Bot-Key", out var v) && v == key;
}

// Ensures a DiscordLink + User + PlayerCharacter exist for the given Discord ID.
// If any part is missing it is created automatically — no /link command required.
async Task<PlayerCharacter> EnsureDiscordPlayer(
    string discordId,
    peeposredemption.Infrastructure.Persistence.AppDbContext db)
{
    // 1. Find or create User + DiscordLink
    var link = await db.DiscordLinks
        .Include(l => l.User)
        .FirstOrDefaultAsync(l => l.DiscordUserId == discordId);

    peeposredemption.Domain.Entities.User user;
    if (link == null)
    {
        // Build a unique username derived from the Discord ID
        var baseUsername = $"discord_{discordId}";
        baseUsername = baseUsername[..Math.Min(baseUsername.Length, 20)];
        var username = baseUsername;
        var suffix = 1;
        while (await db.Users.AnyAsync(u => u.Username == username))
            username = $"{baseUsername[..Math.Min(baseUsername.Length, 17)]}_{suffix++}";

        user = new peeposredemption.Domain.Entities.User
        {
            Username      = username,
            Email         = $"discord_{discordId}@bot.torvex.app",
            PasswordHash  = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
            EmailConfirmed = true
        };
        db.Users.Add(user);

        link = new peeposredemption.Domain.Entities.DiscordLink
        {
            DiscordUserId = discordId,
            TorvexUserId  = user.Id,
            User          = user
        };
        db.DiscordLinks.Add(link);
        await db.SaveChangesAsync();
    }
    else
    {
        user = link.User;
    }

    // 2. Find or create PlayerCharacter
    var player = await db.PlayerCharacters.FirstOrDefaultAsync(p => p.UserId == link.TorvexUserId);
    if (player != null) return player;

    player = new PlayerCharacter
    {
        UserId      = link.TorvexUserId,
        CharacterName = user.Username,
        Class       = GameClass.Warrior,
        Level       = 1,
        XP          = 0,
        STR = 10, DEF = 10, INT = 10, DEX = 10, VIT = 10, LUK = 5,
        CurrentHp = 100, MaxHp = 100,
        CurrentMp = 50,  MaxMp = 50
    };
    db.PlayerCharacters.Add(player);

    // Initialize all skills at level 1
    foreach (SkillType skill in Enum.GetValues<SkillType>())
    {
        db.PlayerSkills.Add(new PlayerSkill
        {
            PlayerId        = player.Id,
            SkillType       = skill,
            Level           = 1,
            XP              = 0,
            XpToNextLevel   = 75
        });
    }

    // Give starter weapon
    var starterSword = await db.ItemDefinitions
        .FirstOrDefaultAsync(i => i.Name == "Wooden Sword");
    if (starterSword != null)
    {
        db.PlayerInventoryItems.Add(new PlayerInventoryItem
        {
            PlayerId         = player.Id,
            ItemDefinitionId = starterSword.Id,
            Quantity         = 1,
            IsEquipped       = true,
            EquippedSlot     = EquipSlot.MainHand
        });
    }

    await db.SaveChangesAsync();
    return player;
}

// Auto-create a Torvex account and link it for a Discord user (no manual link needed)
app.MapPost("/api/bot/auto-link", async (
    HttpContext ctx,
    IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    IMediator mediator,
    BotAutoLinkRequest req) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();

    // Already linked?
    var existing = await db.DiscordLinks
        .Include(l => l.User)
        .FirstOrDefaultAsync(l => l.DiscordUserId == req.DiscordUserId);
    if (existing != null)
        return Results.Ok(new { torvexUserId = existing.TorvexUserId, username = existing.User.Username });

    // Sanitize Discord username into a valid Torvex username
    var baseUsername = System.Text.RegularExpressions.Regex.Replace(req.DiscordUsername, @"[^a-zA-Z0-9_]", "_");
    if (baseUsername.Length < 3) baseUsername = $"dc_{baseUsername}";
    baseUsername = baseUsername[..Math.Min(baseUsername.Length, 20)];

    // Ensure unique username
    var username = baseUsername;
    var suffix = 1;
    while (await db.Users.AnyAsync(u => u.Username == username))
        username = $"{baseUsername[..Math.Min(baseUsername.Length, 17)]}_{suffix++}";

    var fakeEmail = $"discord_{req.DiscordUserId}@bot.torvex.app";
    var password = Guid.NewGuid().ToString("N");
    var user = new peeposredemption.Domain.Entities.User
    {
        Username = username,
        Email = fakeEmail,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        EmailConfirmed = true
    };
    db.Users.Add(user);

    db.DiscordLinks.Add(new peeposredemption.Domain.Entities.DiscordLink
    {
        DiscordUserId = req.DiscordUserId,
        TorvexUserId = user.Id,
        User = user
    });

    await db.SaveChangesAsync();
    return Results.Ok(new { torvexUserId = user.Id, username = user.Username });
});

// Link a Discord user to a Torvex account
app.MapPost("/api/bot/link", async (
    HttpContext ctx,
    IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    BotLinkRequest req) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(req.DiscordUserId) || string.IsNullOrWhiteSpace(req.TorvexUsername))
        return Results.BadRequest(new { error = "DiscordUserId and TorvexUsername required." });

    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == req.TorvexUsername);
    if (user == null) return Results.NotFound(new { error = "Torvex user not found." });

    var existing = await db.DiscordLinks.FirstOrDefaultAsync(l => l.DiscordUserId == req.DiscordUserId);
    if (existing != null)
    {
        existing.TorvexUserId = user.Id;
        existing.LinkedAt = DateTime.UtcNow;
    }
    else
    {
        db.DiscordLinks.Add(new peeposredemption.Domain.Entities.DiscordLink
        {
            DiscordUserId = req.DiscordUserId,
            TorvexUserId = user.Id
        });
    }

    await db.SaveChangesAsync();
    return Results.Ok(new { torvexUserId = user.Id, username = user.Username, displayName = user.DisplayName });
});

// Unlink a Discord user
app.MapDelete("/api/bot/link/{discordUserId}", async (
    HttpContext ctx,
    IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    string discordUserId) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();
    var link = await db.DiscordLinks.FirstOrDefaultAsync(l => l.DiscordUserId == discordUserId);
    if (link == null) return Results.NotFound();
    db.DiscordLinks.Remove(link);
    await db.SaveChangesAsync();
    return Results.Ok();
});

// Get linked Torvex user for a Discord user
app.MapGet("/api/bot/link/{discordUserId}", async (
    HttpContext ctx,
    IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    string discordUserId) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();
    var link = await db.DiscordLinks
        .Include(l => l.User)
        .FirstOrDefaultAsync(l => l.DiscordUserId == discordUserId);
    if (link == null) return Results.NotFound();
    return Results.Ok(new
    {
        torvexUserId = link.TorvexUserId,
        username = link.User.Username,
        displayName = link.User.DisplayName,
        orbBalance = link.User.OrbBalance,
        linkedAt = link.LinkedAt
    });
});

// Process a game command on behalf of a linked Discord user
app.MapPost("/api/bot/game/command", async (
    HttpContext ctx,
    IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    IMediator mediator,
    BotGameCommandRequest req) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();

    await EnsureDiscordPlayer(req.DiscordUserId, db);
    var cmdLink = await db.DiscordLinks
        .Include(l => l.User)
        .FirstOrDefaultAsync(l => l.DiscordUserId == req.DiscordUserId);

    var result = await mediator.Send(new peeposredemption.Application.Features.Game.Commands.ProcessGameCommandRequest(
        cmdLink!.TorvexUserId,
        cmdLink.User.Username,
        discordBotChannelId,
        req.Command));

    return Results.Ok(result);
});

// Award message orb reward for a linked Discord user (peepo bucks)
app.MapPost("/api/bot/orbs/message-reward", async (
    HttpContext ctx,
    IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    IMediator mediator,
    BotDiscordUserRequest req) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();

    var link = await db.DiscordLinks.FirstOrDefaultAsync(l => l.DiscordUserId == req.DiscordUserId);
    if (link == null) return Results.Ok(new { rewarded = false }); // unlinked users silently skipped

    await mediator.Send(new peeposredemption.Application.Features.Orbs.Commands.RecordMessageOrbRewardCommand(link.TorvexUserId));
    return Results.Ok(new { rewarded = true });
});

// Get player stats for a Discord user (for PvP combat)
app.MapGet("/api/bot/player/{discordUserId}", async (
    HttpContext ctx,
    IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    string discordUserId) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();
    var player = await EnsureDiscordPlayer(discordUserId, db);
    var playerLink = await db.DiscordLinks
        .Include(l => l.User)
        .FirstOrDefaultAsync(l => l.DiscordUserId == discordUserId);

    return Results.Ok(new {
        username = playerLink!.User.Username,
        characterName = player.CharacterName,
        @class = player.Class.ToString(),
        level = player.Level,
        xp = player.XP,
        currentHp = player.CurrentHp,
        maxHp = player.MaxHp,
        currentMp = player.CurrentMp,
        maxMp = player.MaxMp,
        str = player.STR,
        def = player.DEF,
        @int = player.INT,
        dex = player.DEX,
        vit = player.VIT,
        luk = player.LUK,
        totalMonstersKilled = player.TotalMonstersKilled,
        totalDeaths = player.TotalDeaths,
        coinBalance = player.CoinBalance
    });
});

// GET /api/bot/game/inventory/{discordUserId} -- items with definition IDs for trade resolution
app.MapGet("/api/bot/game/inventory/{discordUserId}", async (
    string discordUserId, HttpContext ctx, IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();
    var player = await EnsureDiscordPlayer(discordUserId, db);
    var items = await db.PlayerInventoryItems
        .Include(i => i.ItemDefinition)
        .Where(i => i.PlayerId == player.Id)
        .ToListAsync();
    return Results.Ok(items.Select(i => new
    {
        itemDefinitionId = i.ItemDefinitionId,
        name     = i.ItemDefinition.Name,
        rarity   = i.ItemDefinition.Rarity.ToString(),
        type     = i.ItemDefinition.Type.ToString(),
        quantity = i.Quantity,
        isEquipped = i.IsEquipped
    }));
});

// Award PvP XP to winner and loser
app.MapPost("/api/bot/pvp/reward", async (
    HttpContext ctx,
    IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    BotPvpRewardRequest req) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();

    var winner = await EnsureDiscordPlayer(req.WinnerDiscordId, db);
    var loser  = await EnsureDiscordPlayer(req.LoserDiscordId,  db);

    long winnerXp = Math.Max(50, loser.Level * 25);
    long loserXp  = Math.Max(10, loser.Level * 5);

    winner.XP += winnerXp;
    loser.XP  += loserXp;

    // Level up check (simple: level = floor(xp / 500) + 1, capped at 100)
    winner.Level = Math.Min(100, (int)(winner.XP / 500) + 1);
    loser.Level  = Math.Min(100, (int)(loser.XP  / 500) + 1);

    await db.SaveChangesAsync();
    return Results.Ok(new { winnerXpGained = winnerXp, loserXpGained = loserXp });
});

// Add coins to a player's CoinBalance (gathering rewards, PvP wins, daily bonus, etc.)
app.MapPost("/api/bot/game/add-coins", async (
    HttpContext ctx,
    IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    BotAddCoinsRequest req) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();

    var player = await EnsureDiscordPlayer(req.DiscordId, db);
    player.CoinBalance += req.Amount;
    await db.SaveChangesAsync();

    return Results.Ok(new { newBalance = player.CoinBalance, added = req.Amount });
}).DisableAntiforgery();

// Item dictionary — weapons, armor, or materials
app.MapGet("/api/bot/items", async (
    HttpContext ctx,
    IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    string? type) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();

    var query = db.ItemDefinitions.AsQueryable();

    if (!string.IsNullOrWhiteSpace(type))
    {
        if (Enum.TryParse<GameItemType>(type, true, out var parsed))
            query = query.Where(i => i.Type == parsed);
    }

    var items = await query
        .OrderBy(i => i.LevelReq).ThenBy(i => i.Name)
        .Select(i => new {
            id          = i.Id,
            name        = i.Name,
            description = i.Description,
            icon        = i.Icon,
            type        = i.Type.ToString(),
            subType     = i.SubType.ToString(),
            equipSlot   = i.EquipSlot != null ? i.EquipSlot.ToString() : null,
            rarity      = i.Rarity.ToString(),
            levelReq    = i.LevelReq,
            minDamage   = i.MinDamage,
            maxDamage   = i.MaxDamage,
            element     = i.Element.ToString(),
            bonusSTR    = i.BonusSTR,
            bonusDEF    = i.BonusDEF,
            bonusINT    = i.BonusINT,
            bonusDEX    = i.BonusDEX,
            bonusVIT    = i.BonusVIT,
            bonusLUK    = i.BonusLUK,
            buyPrice    = i.BuyPrice,
            sellPrice   = i.SellPrice
        })
        .ToListAsync();

    return Results.Ok(items);
});

// Monster dictionary
app.MapGet("/api/bot/monsters", async (
    HttpContext ctx,
    IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    string? zone) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();

    var query = db.MonsterDefinitions.AsQueryable();
    if (!string.IsNullOrWhiteSpace(zone))
        query = query.Where(m => m.Zone.ToLower() == zone.ToLower());

    var monsters = await query
        .OrderBy(m => m.Level).ThenBy(m => m.Name)
        .Select(m => new {
            id      = m.Id,
            name    = m.Name,
            icon    = m.Icon,
            level   = m.Level,
            zone    = m.Zone,
            element = m.Element.ToString(),
            maxHp   = m.MaxHp,
            xp      = m.XpReward,
            orbMin  = m.OrbRewardMin,
            orbMax  = m.OrbRewardMax
        })
        .ToListAsync();

    return Results.Ok(monsters);
});

// ── Peepo Collectibles API ──────────────────────────────────────────────────

static GameItemRarity PeepoRarity(string name)
{
    using var sha = System.Security.Cryptography.SHA256.Create();
    var v = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(name.ToLower()))[0];
    return v switch
    {
        <= 127 => GameItemRarity.Common,
        <= 191 => GameItemRarity.Uncommon,
        <= 223 => GameItemRarity.Rare,
        <= 239 => GameItemRarity.Epic,
        _      => GameItemRarity.Legendary
    };
}

static (long buy, long sell, decimal drop) PeepoStats(GameItemRarity r) => r switch
{
    GameItemRarity.Common    => (50,   25,   0.03m),
    GameItemRarity.Uncommon  => (150,  75,   0.015m),
    GameItemRarity.Rare      => (500,  250,  0.005m),
    GameItemRarity.Epic      => (1500, 750,  0.0015m),
    _                        => (0,    0,    0.0005m)  // Legendary: orbs only, no coin price
};

// Coin price per rarity for shop display (Legendary = 0 = orbs only)
static long PeepoRarityShopPrice(GameItemRarity r) => r switch
{
    GameItemRarity.Common    => 50,
    GameItemRarity.Uncommon  => 150,
    GameItemRarity.Rare      => 500,
    GameItemRarity.Epic      => 1500,
    _                        => 0   // Legendary: not purchasable with coins
};

// Orb price per rarity (Rare+)
static long PeepoRarityOrbPrice(GameItemRarity r) => r switch
{
    GameItemRarity.Rare      => 50,
    GameItemRarity.Epic      => 150,
    GameItemRarity.Legendary => 500,
    _                        => 0
};

// POST /api/bot/peepos/sync — bot pushes Discord guild emojis; idempotent by name
app.MapPost("/api/bot/peepos/sync", async (
    HttpContext ctx, IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    List<BotPeepoEmojiDto> emojis) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();

    var monsterIds = await db.MonsterDefinitions.Select(m => m.Id).ToListAsync();
    int created = 0, updated = 0;

    int batchCount = 0;
    foreach (var emoji in emojis)
    {
        var existing = await db.ItemDefinitions.FirstOrDefaultAsync(i =>
            i.Name == emoji.Name && i.Type == GameItemType.Collectible && i.SubType == ItemSubType.Peepo);
        if (existing == null)
        {
            var rarity = PeepoRarity(emoji.Name);
            var (buy, sell, drop) = PeepoStats(rarity);
            var itemDef = new ItemDefinition
            {
                Name        = emoji.Name,
                Description = "A collectible peepo emoji.",
                Type        = GameItemType.Collectible,
                SubType     = ItemSubType.Peepo,
                Rarity      = rarity,
                Icon        = emoji.Url,
                BuyPrice    = buy,
                SellPrice   = sell,
                IsStackable = false,
            };
            db.ItemDefinitions.Add(itemDef);
            foreach (var mid in monsterIds)
                db.MonsterLootEntries.Add(new MonsterLootEntry
                {
                    MonsterDefinitionId = mid,
                    ItemDefinitionId    = itemDef.Id,
                    DropChance          = drop,
                    MinQuantity         = 1,
                    MaxQuantity         = 1
                });
            created++;
        }
        else if (existing.Icon != emoji.Url)
        {
            existing.Icon = emoji.Url;
            updated++;
        }

        batchCount++;
        if (batchCount % 50 == 0)
            await db.SaveChangesAsync();
    }

    await db.SaveChangesAsync();
    return Results.Ok(new { created, updated, total = emojis.Count });
}).DisableAntiforgery();

// GET /api/bot/peepos — all peepo ItemDefs sorted rarity → name
app.MapGet("/api/bot/peepos", async (
    HttpContext ctx, IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();
    var peepos = await db.ItemDefinitions
        .Where(i => i.Type == GameItemType.Collectible && i.SubType == ItemSubType.Peepo)
        .OrderBy(i => i.Rarity).ThenBy(i => i.Name)
        .Select(i => new { id = i.Id, name = i.Name, icon = i.Icon,
            rarity = i.Rarity.ToString(), buyPrice = i.BuyPrice, sellPrice = i.SellPrice })
        .ToListAsync();
    return Results.Ok(peepos);
});

// GET /api/bot/peepos/shop — peepos available for coin purchase, with rarity-scaled prices
app.MapGet("/api/bot/peepos/shop", async (
    HttpContext ctx, IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();
    var peepos = await db.ItemDefinitions
        .Where(i => i.Type == GameItemType.Collectible && i.SubType == ItemSubType.Peepo)
        .OrderBy(i => i.Rarity).ThenBy(i => i.Name)
        .ToListAsync();
    var result = peepos.Select(i => new
    {
        id         = i.Id,
        name       = i.Name,
        icon       = i.Icon,
        rarity     = i.Rarity.ToString(),
        coinPrice  = PeepoRarityShopPrice(i.Rarity),
        orbPrice   = PeepoRarityOrbPrice(i.Rarity),
        sellPrice  = i.SellPrice,
        buyWithCoins = PeepoRarityShopPrice(i.Rarity) > 0,
    });
    return Results.Ok(result);
});

// POST /api/bot/peepos/buy-coins — buy a peepo by itemDefinitionId using CoinBalance
app.MapPost("/api/bot/peepos/buy-coins", async (
    HttpContext ctx, IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    BotPeepoBuyCoinsRequest req) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();
    var link = await db.DiscordLinks.FirstOrDefaultAsync(l => l.DiscordUserId == req.DiscordId);
    if (link == null) return Results.BadRequest(new { error = "Account not linked." });
    var player = await db.PlayerCharacters.FirstOrDefaultAsync(p => p.UserId == link.TorvexUserId);
    if (player == null) return Results.BadRequest(new { error = "No character found. Use /rpg start first." });
    var itemDef = await db.ItemDefinitions.FirstOrDefaultAsync(i =>
        i.Id == req.ItemDefinitionId && i.Type == GameItemType.Collectible && i.SubType == ItemSubType.Peepo);
    if (itemDef == null) return Results.NotFound(new { error = "Peepo not found." });

    var coinPrice = PeepoRarityShopPrice(itemDef.Rarity);
    if (coinPrice <= 0)
        return Results.BadRequest(new { error = $"{itemDef.Name} is Legendary — purchasable with orbs only." });
    if (player.CoinBalance < coinPrice)
        return Results.BadRequest(new { error = $"Not enough coins. Need {coinPrice:N0}, have {player.CoinBalance:N0}." });

    var existing = await db.PlayerInventoryItems
        .FirstOrDefaultAsync(i => i.PlayerId == player.Id && i.ItemDefinitionId == itemDef.Id);
    if (existing != null)
        return Results.BadRequest(new { error = $"You already own {itemDef.Name}." });

    player.CoinBalance -= coinPrice;
    db.PlayerInventoryItems.Add(new PlayerInventoryItem
        { PlayerId = player.Id, ItemDefinitionId = itemDef.Id, Quantity = 1 });
    await db.SaveChangesAsync();
    return Results.Ok(new { newCoinBalance = player.CoinBalance, peepo = new { name = itemDef.Name, rarity = itemDef.Rarity.ToString(), icon = itemDef.Icon } });
}).DisableAntiforgery();

// GET /api/bot/peepos/inventory/{discordUserId}
app.MapGet("/api/bot/peepos/inventory/{discordUserId}", async (
    string discordUserId, HttpContext ctx, IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();
    var player = await EnsureDiscordPlayer(discordUserId, db);
    var items = await db.PlayerInventoryItems
        .Include(i => i.ItemDefinition)
        .Where(i => i.PlayerId == player.Id
            && i.ItemDefinition.Type == GameItemType.Collectible
            && i.ItemDefinition.SubType == ItemSubType.Peepo)
        .Select(i => new { id = i.ItemDefinition.Id, name = i.ItemDefinition.Name,
            icon = i.ItemDefinition.Icon, rarity = i.ItemDefinition.Rarity.ToString(),
            quantity = i.Quantity })
        .ToListAsync();
    return Results.Ok(items);
});

// POST /api/bot/peepos/buy — buy from fixed shop
app.MapPost("/api/bot/peepos/buy", async (
    HttpContext ctx, IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    BotPeepoBuyRequest req) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();
    var player = await EnsureDiscordPlayer(req.DiscordUserId, db);
    var itemDef = await db.ItemDefinitions.FirstOrDefaultAsync(i =>
        i.Name == req.PeepoName && i.Type == GameItemType.Collectible && i.SubType == ItemSubType.Peepo);
    if (itemDef == null) return Results.NotFound(new { error = "Peepo not found." });
    if (player.CoinBalance < itemDef.BuyPrice)
        return Results.BadRequest(new { error = $"Not enough coins. Need {itemDef.BuyPrice:N0}, have {player.CoinBalance:N0}." });

    player.CoinBalance -= itemDef.BuyPrice;
    var existing = await db.PlayerInventoryItems
        .FirstOrDefaultAsync(i => i.PlayerId == player.Id && i.ItemDefinitionId == itemDef.Id);
    if (existing != null) existing.Quantity++;
    else db.PlayerInventoryItems.Add(new PlayerInventoryItem
        { PlayerId = player.Id, ItemDefinitionId = itemDef.Id, Quantity = 1 });
    await db.SaveChangesAsync();
    return Results.Ok(new { newCoinBalance = player.CoinBalance });
}).DisableAntiforgery();

// GET /api/bot/peepos/market — active coin-currency listings
app.MapGet("/api/bot/peepos/market", async (
    HttpContext ctx, IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();
    var listings = await db.MarketplaceListings
        .Include(l => l.ItemDefinition)
        .Include(l => l.Seller).ThenInclude(s => s.User)
        .Where(l => l.Status == MarketListingStatus.Active
            && l.CurrencyType == MarketplaceCurrencyType.Coins
            && l.ItemDefinition.Type == GameItemType.Collectible
            && l.ExpiresAt > DateTime.UtcNow)
        .OrderBy(l => l.PricePerUnit)
        .Select(l => new { id = l.Id, itemName = l.ItemDefinition.Name, icon = l.ItemDefinition.Icon,
            rarity = l.ItemDefinition.Rarity.ToString(), quantity = l.Quantity,
            pricePerUnit = l.PricePerUnit, sellerName = l.Seller.User.Username, listedAt = l.CreatedAt })
        .ToListAsync();
    return Results.Ok(listings);
});

// POST /api/bot/peepos/market/list — create listing (removes from inventory)
app.MapPost("/api/bot/peepos/market/list", async (
    HttpContext ctx, IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    BotPeepoMarketListRequest req) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();
    var player = await EnsureDiscordPlayer(req.DiscordUserId, db);
    var itemDef = await db.ItemDefinitions.FirstOrDefaultAsync(i =>
        i.Name == req.PeepoName && i.Type == GameItemType.Collectible && i.SubType == ItemSubType.Peepo);
    if (itemDef == null) return Results.NotFound(new { error = "Peepo not found." });
    if (req.Price <= 0) return Results.BadRequest(new { error = "Price must be positive." });

    var invItem = await db.PlayerInventoryItems
        .FirstOrDefaultAsync(i => i.PlayerId == player.Id && i.ItemDefinitionId == itemDef.Id);
    if (invItem == null || invItem.Quantity < 1)
        return Results.BadRequest(new { error = "You don't own that peepo." });

    invItem.Quantity--;
    if (invItem.Quantity <= 0) db.PlayerInventoryItems.Remove(invItem);
    db.MarketplaceListings.Add(new MarketplaceListing
    {
        SellerId         = player.Id,
        ItemDefinitionId = itemDef.Id,
        Quantity         = 1,
        PricePerUnit     = req.Price,
        CurrencyType     = MarketplaceCurrencyType.Coins
    });
    await db.SaveChangesAsync();
    return Results.Ok(new { listed = true });
}).DisableAntiforgery();

// POST /api/bot/peepos/market/buy — buy from market (5% coin sink)
app.MapPost("/api/bot/peepos/market/buy", async (
    HttpContext ctx, IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    BotPeepoMarketBuyRequest req) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();
    var buyer = await EnsureDiscordPlayer(req.DiscordUserId, db);
    var listing = await db.MarketplaceListings
        .Include(l => l.ItemDefinition)
        .FirstOrDefaultAsync(l => l.Id == req.ListingId
            && l.Status == MarketListingStatus.Active
            && l.CurrencyType == MarketplaceCurrencyType.Coins
            && l.ExpiresAt > DateTime.UtcNow);
    if (listing == null) return Results.NotFound(new { error = "Listing not found or expired." });
    if (listing.SellerId == buyer.Id)
        return Results.BadRequest(new { error = "Can't buy your own listing." });

    var totalCost = listing.PricePerUnit * listing.Quantity;
    if (buyer.CoinBalance < totalCost)
        return Results.BadRequest(new { error = $"Not enough coins. Need {totalCost:N0}." });

    var seller = await db.PlayerCharacters.FirstOrDefaultAsync(p => p.Id == listing.SellerId);
    if (seller == null) return Results.NotFound(new { error = "Seller not found." });

    buyer.CoinBalance  -= totalCost;
    seller.CoinBalance += (long)(totalCost * 0.95); // 5% tax sunk
    listing.Status  = MarketListingStatus.Sold;
    listing.BuyerId = buyer.Id;

    var existing = await db.PlayerInventoryItems
        .FirstOrDefaultAsync(i => i.PlayerId == buyer.Id && i.ItemDefinitionId == listing.ItemDefinitionId);
    if (existing != null) existing.Quantity += listing.Quantity;
    else db.PlayerInventoryItems.Add(new PlayerInventoryItem
        { PlayerId = buyer.Id, ItemDefinitionId = listing.ItemDefinitionId, Quantity = listing.Quantity });

    await db.SaveChangesAsync();
    return Results.Ok(new { newCoinBalance = buyer.CoinBalance });
}).DisableAntiforgery();

// DELETE /api/bot/peepos/market/{listingId} — cancel listing (returns item)
app.MapDelete("/api/bot/peepos/market/{listingId:guid}", async (
    Guid listingId, string discordUserId, HttpContext ctx, IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();
    var player = await EnsureDiscordPlayer(discordUserId, db);
    var listing = await db.MarketplaceListings
        .FirstOrDefaultAsync(l => l.Id == listingId && l.SellerId == player.Id
            && l.Status == MarketListingStatus.Active);
    if (listing == null) return Results.NotFound(new { error = "Listing not found." });

    listing.Status = MarketListingStatus.Cancelled;
    var existing = await db.PlayerInventoryItems
        .FirstOrDefaultAsync(i => i.PlayerId == player.Id && i.ItemDefinitionId == listing.ItemDefinitionId);
    if (existing != null) existing.Quantity += listing.Quantity;
    else db.PlayerInventoryItems.Add(new PlayerInventoryItem
        { PlayerId = player.Id, ItemDefinitionId = listing.ItemDefinitionId, Quantity = listing.Quantity });
    await db.SaveChangesAsync();
    return Results.Ok();
}).DisableAntiforgery();

// POST /api/bot/peepos/trade/offer — create pending trade offer
app.MapPost("/api/bot/peepos/trade/offer", async (
    HttpContext ctx, IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    BotPeepoTradeOfferRequest req) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();
    var initiator = await EnsureDiscordPlayer(req.InitiatorDiscordId, db);
    var recipient = await EnsureDiscordPlayer(req.RecipientDiscordId, db);

    // Validate initiator owns the peepo
    if (!string.IsNullOrEmpty(req.InitiatorPeepoName))
    {
        var itemDef = await db.ItemDefinitions.FirstOrDefaultAsync(i =>
            i.Name == req.InitiatorPeepoName && i.Type == GameItemType.Collectible);
        if (itemDef == null) return Results.BadRequest(new { error = "Peepo not found." });
        var inv = await db.PlayerInventoryItems
            .FirstOrDefaultAsync(i => i.PlayerId == initiator.Id && i.ItemDefinitionId == itemDef.Id);
        if (inv == null || inv.Quantity < 1)
            return Results.BadRequest(new { error = "You don't own that peepo." });
    }
    if (req.InitiatorCoins > 0 && initiator.CoinBalance < req.InitiatorCoins)
        return Results.BadRequest(new { error = "Not enough coins." });

    var itemsJson = string.IsNullOrEmpty(req.InitiatorPeepoName) ? "[]"
        : System.Text.Json.JsonSerializer.Serialize(
            new[] { new { name = req.InitiatorPeepoName, quantity = 1 } });

    var trade = new TradeOffer
    {
        InitiatorId    = initiator.Id,
        RecipientId    = recipient.Id,
        ChannelId      = discordBotChannelId,
        InitiatorItems = itemsJson,
        InitiatorCoins = req.InitiatorCoins,
        RecipientItems = "[]",
        RecipientCoins = 0
    };
    db.TradeOffers.Add(trade);
    await db.SaveChangesAsync();
    return Results.Ok(new { tradeOfferId = trade.Id });
}).DisableAntiforgery();

// POST /api/bot/peepos/trade/{id}/accept — executes swap
app.MapPost("/api/bot/peepos/trade/{id:guid}/accept", async (
    Guid id, HttpContext ctx, IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    BotPeepoTradeActionRequest req) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();
    var recipient = await EnsureDiscordPlayer(req.DiscordUserId, db);
    var trade = await db.TradeOffers.FirstOrDefaultAsync(t => t.Id == id && t.Status == TradeStatus.Pending);
    if (trade == null) return Results.NotFound(new { error = "Trade not found or already resolved." });
    if (recipient.Id != trade.RecipientId) return Results.Forbid();
    if (trade.ExpiresAt < DateTime.UtcNow)
    {
        trade.Status = TradeStatus.Expired;
        await db.SaveChangesAsync();
        return Results.BadRequest(new { error = "Trade expired." });
    }
    var initiator = await db.PlayerCharacters.FirstOrDefaultAsync(p => p.Id == trade.InitiatorId);
    if (initiator == null) return Results.BadRequest(new { error = "Initiator not found." });

    // Transfer initiator items → recipient
    if (trade.InitiatorItems != "[]")
    {
        var items = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.JsonElement>>(trade.InitiatorItems);
        foreach (var item in items ?? [])
        {
            var name    = item.GetProperty("name").GetString() ?? "";
            var itemDef = await db.ItemDefinitions.FirstOrDefaultAsync(i =>
                i.Name == name && i.Type == GameItemType.Collectible);
            if (itemDef == null) continue;
            var initInv = await db.PlayerInventoryItems
                .FirstOrDefaultAsync(i => i.PlayerId == initiator.Id && i.ItemDefinitionId == itemDef.Id);
            if (initInv != null) { initInv.Quantity--; if (initInv.Quantity <= 0) db.PlayerInventoryItems.Remove(initInv); }
            var recipInv = await db.PlayerInventoryItems
                .FirstOrDefaultAsync(i => i.PlayerId == recipient.Id && i.ItemDefinitionId == itemDef.Id);
            if (recipInv != null) recipInv.Quantity++;
            else db.PlayerInventoryItems.Add(new PlayerInventoryItem
                { PlayerId = recipient.Id, ItemDefinitionId = itemDef.Id, Quantity = 1 });
        }
    }

    // Transfer coins
    if (trade.InitiatorCoins > 0) { initiator.CoinBalance -= trade.InitiatorCoins; recipient.CoinBalance += trade.InitiatorCoins; }
    if (trade.RecipientCoins > 0) { recipient.CoinBalance -= trade.RecipientCoins; initiator.CoinBalance += trade.RecipientCoins; }

    trade.Status = TradeStatus.Accepted;
    await db.SaveChangesAsync();
    return Results.Ok(new { success = true });
}).DisableAntiforgery();

// POST /api/bot/peepos/crate/open — legacy endpoint kept for backward compat, spend 5000 coins
app.MapPost("/api/bot/peepos/crate/open", async (
    HttpContext ctx, IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    BotPeepoCrateRequest req) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();
    const long CRATE_COST = 5000;

    var player = await EnsureDiscordPlayer(req.DiscordUserId, db);
    if (player.CoinBalance < CRATE_COST)
        return Results.BadRequest(new { error = $"Not enough coins. Need {CRATE_COST:N0}, have {player.CoinBalance:N0}." });

    var roll = Random.Shared.NextDouble() * 100;
    var rarity = roll < 5.0  ? GameItemRarity.Rare
               : roll < 30.0 ? GameItemRarity.Uncommon
               : GameItemRarity.Common;

    var pool = await db.ItemDefinitions
        .Where(i => i.Type == GameItemType.Collectible && i.SubType == ItemSubType.Peepo && i.Rarity == rarity)
        .ToListAsync();
    if (!pool.Any()) { rarity = GameItemRarity.Common; pool = await db.ItemDefinitions.Where(i => i.Type == GameItemType.Collectible && i.SubType == ItemSubType.Peepo && i.Rarity == GameItemRarity.Common).ToListAsync(); }
    if (!pool.Any()) return Results.Problem("No peepos in catalog. Run /peepo sync first.");
    var winner = pool[Random.Shared.Next(pool.Count)];
    player.CoinBalance -= CRATE_COST;
    var inv = await db.PlayerInventoryItems.FirstOrDefaultAsync(i => i.PlayerId == player.Id && i.ItemDefinitionId == winner.Id);
    bool isNew = inv == null;
    if (inv != null) inv.Quantity++;
    else db.PlayerInventoryItems.Add(new PlayerInventoryItem { PlayerId = player.Id, ItemDefinitionId = winner.Id, Quantity = 1 });
    await db.SaveChangesAsync();
    return Results.Ok(new { name = winner.Name, icon = winner.Icon, rarity = rarity.ToString(), isNew, newCoinBalance = player.CoinBalance });
}).DisableAntiforgery();

// POST /api/bot/peepos/crate — open basic (200 coins) or premium (100 orbs) crate
app.MapPost("/api/bot/peepos/crate", async (
    HttpContext ctx, IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    BotPeepoCrateV2Request req) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();

    var crateType = req.CrateType?.ToLower();
    if (crateType is not ("basic" or "premium"))
        return Results.BadRequest(new { error = "crateType must be 'basic' or 'premium'." });

    var link = await db.DiscordLinks
        .Include(l => l.User)
        .FirstOrDefaultAsync(l => l.DiscordUserId == req.DiscordId);
    if (link == null) return Results.BadRequest(new { error = "Account not linked." });
    var player = await db.PlayerCharacters.FirstOrDefaultAsync(p => p.UserId == link.TorvexUserId);
    if (player == null) return Results.BadRequest(new { error = "No character found. Use /rpg start first." });

    GameItemRarity rarity;
    if (crateType == "basic")
    {
        const long BASIC_COST = 200;
        if (player.CoinBalance < BASIC_COST)
            return Results.BadRequest(new { error = $"Not enough coins. Need {BASIC_COST:N0}, have {player.CoinBalance:N0}." });

        var roll = Random.Shared.NextDouble() * 100;
        rarity = roll < 5.0  ? GameItemRarity.Rare
               : roll < 30.0 ? GameItemRarity.Uncommon
               : GameItemRarity.Common;  // 70% Common, 25% Uncommon, 5% Rare
        player.CoinBalance -= BASIC_COST;
    }
    else // premium
    {
        const long PREMIUM_COST = 100;
        if (link.User.OrbBalance < PREMIUM_COST)
            return Results.BadRequest(new { error = $"Not enough orbs. Need {PREMIUM_COST:N0}, have {link.User.OrbBalance:N0}." });

        var roll = Random.Shared.NextDouble() * 100;
        rarity = roll < 5.0  ? GameItemRarity.Legendary
               : roll < 20.0 ? GameItemRarity.Epic
               : roll < 60.0 ? GameItemRarity.Rare
               : GameItemRarity.Uncommon;  // 40% Uncommon, 40% Rare, 15% Epic, 5% Legendary
        link.User.OrbBalance -= PREMIUM_COST;
    }

    var pool = await db.ItemDefinitions
        .Where(i => i.Type == GameItemType.Collectible && i.SubType == ItemSubType.Peepo && i.Rarity == rarity)
        .ToListAsync();
    if (!pool.Any())
    {
        rarity = GameItemRarity.Common;
        pool   = await db.ItemDefinitions
            .Where(i => i.Type == GameItemType.Collectible && i.SubType == ItemSubType.Peepo && i.Rarity == GameItemRarity.Common)
            .ToListAsync();
    }
    if (!pool.Any()) return Results.Problem("No peepos in catalog. Run /peepo sync first.");

    var winner = pool[Random.Shared.Next(pool.Count)];

    var inv = await db.PlayerInventoryItems
        .FirstOrDefaultAsync(i => i.PlayerId == player.Id && i.ItemDefinitionId == winner.Id);
    bool alreadyOwned = inv != null;
    long refundAmount = 0;

    if (alreadyOwned)
    {
        // Give 50% coin refund of rarity price instead
        refundAmount = PeepoRarityShopPrice(rarity) / 2;
        if (refundAmount > 0) player.CoinBalance += refundAmount;
        inv!.Quantity++;
    }
    else
    {
        db.PlayerInventoryItems.Add(new PlayerInventoryItem
            { PlayerId = player.Id, ItemDefinitionId = winner.Id, Quantity = 1 });
    }

    await db.SaveChangesAsync();
    return Results.Ok(new
    {
        peepo        = new { name = winner.Name, rarity = rarity.ToString(), icon = winner.Icon },
        alreadyOwned,
        refundAmount,
        newCoinBalance = player.CoinBalance,
        newOrbBalance  = link.User.OrbBalance
    });
}).DisableAntiforgery();

// POST /api/bot/peepos/add — add a single peepo by name + URL (idempotent by name)
app.MapPost("/api/bot/peepos/add", async (
    HttpContext ctx, IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    BotPeepoEmojiDto emoji) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();
    var existing = await db.ItemDefinitions.FirstOrDefaultAsync(i =>
        i.Name == emoji.Name && i.Type == GameItemType.Collectible && i.SubType == ItemSubType.Peepo);
    if (existing != null)
    {
        existing.Icon = emoji.Url;
        await db.SaveChangesAsync();
        return Results.Ok(new { created = false, updated = true });
    }
    var rarity = PeepoRarity(emoji.Name);
    var (buy, sell, drop) = PeepoStats(rarity);
    var itemDef = new ItemDefinition
    {
        Name = emoji.Name, Description = "A collectible peepo emoji.",
        Type = GameItemType.Collectible, SubType = ItemSubType.Peepo,
        Rarity = rarity, Icon = emoji.Url, BuyPrice = buy, SellPrice = sell, IsStackable = false
    };
    db.ItemDefinitions.Add(itemDef);
    var monsterIds = await db.MonsterDefinitions.Select(m => m.Id).ToListAsync();
    foreach (var mid in monsterIds)
        db.MonsterLootEntries.Add(new MonsterLootEntry
            { MonsterDefinitionId = mid, ItemDefinitionId = itemDef.Id, DropChance = drop, MinQuantity = 1, MaxQuantity = 1 });
    await db.SaveChangesAsync();
    return Results.Ok(new { created = true, updated = false, rarity = rarity.ToString() });
}).DisableAntiforgery();

// POST /api/bot/peepos/trade/{id}/decline
app.MapPost("/api/bot/peepos/trade/{id:guid}/decline", async (
    Guid id, HttpContext ctx, IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    BotPeepoTradeActionRequest req) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();
    var trade = await db.TradeOffers.FirstOrDefaultAsync(t => t.Id == id && t.Status == TradeStatus.Pending);
    if (trade == null) return Results.NotFound(new { error = "Trade not found." });
    trade.Status = TradeStatus.Declined;
    await db.SaveChangesAsync();
    return Results.Ok();
}).DisableAntiforgery();

// POST /api/bot/game/gift-coins — transfer coins between two players
app.MapPost("/api/bot/game/gift-coins", async (
    HttpContext ctx,
    IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    BotGiftCoinsRequest req) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();
    if (req.Amount < 1)
        return Results.BadRequest(new { error = "Amount must be at least 1." });
    if (req.SenderDiscordId == req.RecipientDiscordId)
        return Results.BadRequest(new { error = "You cannot gift coins to yourself." });

    var sender    = await EnsureDiscordPlayer(req.SenderDiscordId,    db);
    var recipient = await EnsureDiscordPlayer(req.RecipientDiscordId, db);

    if (sender.CoinBalance < req.Amount)
        return Results.BadRequest(new { error = $"Not enough coins. You have {sender.CoinBalance:N0}, need {req.Amount:N0}." });

    sender.CoinBalance -= req.Amount;
    recipient.CoinBalance += req.Amount;
    await db.SaveChangesAsync();

    return Results.Ok(new { success = true, senderNewBalance = sender.CoinBalance, recipientNewBalance = recipient.CoinBalance });
}).DisableAntiforgery();

// POST /api/bot/game/sync-level — store Discord chat level on character (used as XP multiplier)
app.MapPost("/api/bot/game/sync-level", async (
    HttpContext ctx,
    IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    BotSyncLevelRequest req) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();

    var player = await EnsureDiscordPlayer(req.DiscordId, db);

    player.ChatLevel = req.NewLevel;
    await db.SaveChangesAsync();
    return Results.Ok(new { synced = true, chatLevel = player.ChatLevel });
}).DisableAntiforgery();

// POST /api/bot/game/sync-level-bulk — backfill chat levels for all linked users
app.MapPost("/api/bot/game/sync-level-bulk", async (
    HttpContext ctx,
    IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    List<BotSyncLevelRequest> requests) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();

    int synced = 0;
    foreach (var req in requests)
    {
        var link = await db.DiscordLinks.FirstOrDefaultAsync(l => l.DiscordUserId == req.DiscordId);
        if (link == null) continue;
        var player = await db.PlayerCharacters.FirstOrDefaultAsync(p => p.UserId == link.TorvexUserId);
        if (player == null) continue;
        player.ChatLevel = req.NewLevel;
        synced++;
    }
    await db.SaveChangesAsync();
    return Results.Ok(new { synced });
}).DisableAntiforgery();

// =====================================================================
// Game item trade endpoints (generic RPG inventory items)
// =====================================================================

// POST /api/bot/game/trade/offer -- create a pending game-item trade offer
app.MapPost("/api/bot/game/trade/offer", async (
    HttpContext ctx, IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    BotGameTradeOfferRequest req) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();

    var initiator = await EnsureDiscordPlayer(req.InitiatorDiscordId, db);
    var recipient = await EnsureDiscordPlayer(req.RecipientDiscordId, db);

    foreach (var offered in req.InitiatorItems ?? [])
    {
        var inv = await db.PlayerInventoryItems
            .FirstOrDefaultAsync(i => i.PlayerId == initiator.Id && i.ItemDefinitionId == offered.ItemDefinitionId);
        if (inv == null || inv.Quantity < offered.Quantity)
        {
            var itemDef = await db.ItemDefinitions.FindAsync(offered.ItemDefinitionId);
            var name = itemDef?.Name ?? offered.ItemDefinitionId.ToString();
            return Results.BadRequest(new { error = $"You don't have enough of '{name}'." });
        }
    }
    if (req.InitiatorCoins > 0 && initiator.CoinBalance < req.InitiatorCoins)
        return Results.BadRequest(new { error = "Not enough coins." });

    foreach (var wanted in req.RecipientItems ?? [])
    {
        var inv = await db.PlayerInventoryItems
            .FirstOrDefaultAsync(i => i.PlayerId == recipient.Id && i.ItemDefinitionId == wanted.ItemDefinitionId);
        if (inv == null || inv.Quantity < wanted.Quantity)
        {
            var itemDef = await db.ItemDefinitions.FindAsync(wanted.ItemDefinitionId);
            var name = itemDef?.Name ?? wanted.ItemDefinitionId.ToString();
            return Results.BadRequest(new { error = $"Recipient doesn't have enough of '{name}'." });
        }
    }
    if (req.RecipientCoins > 0 && recipient.CoinBalance < req.RecipientCoins)
        return Results.BadRequest(new { error = "Recipient doesn't have enough coins." });

    var trade = new TradeOffer
    {
        InitiatorId    = initiator.Id,
        RecipientId    = recipient.Id,
        ChannelId      = discordBotChannelId,
        InitiatorItems = System.Text.Json.JsonSerializer.Serialize(
            (req.InitiatorItems ?? []).Select(i => new { itemDefinitionId = i.ItemDefinitionId, quantity = i.Quantity })),
        InitiatorCoins = req.InitiatorCoins,
        RecipientItems = System.Text.Json.JsonSerializer.Serialize(
            (req.RecipientItems ?? []).Select(i => new { itemDefinitionId = i.ItemDefinitionId, quantity = i.Quantity })),
        RecipientCoins = req.RecipientCoins
    };
    db.TradeOffers.Add(trade);
    await db.SaveChangesAsync();

    var initItemNames = new List<string>();
    foreach (var i in req.InitiatorItems ?? [])
    {
        var d = await db.ItemDefinitions.FindAsync(i.ItemDefinitionId);
        if (d != null) initItemNames.Add($"{d.Name} x{i.Quantity}");
    }
    var recipItemNames = new List<string>();
    foreach (var i in req.RecipientItems ?? [])
    {
        var d = await db.ItemDefinitions.FindAsync(i.ItemDefinitionId);
        if (d != null) recipItemNames.Add($"{d.Name} x{i.Quantity}");
    }

    return Results.Ok(new
    {
        tradeOfferId   = trade.Id,
        initiatorItems = initItemNames,
        initiatorCoins = trade.InitiatorCoins,
        recipientItems = recipItemNames,
        recipientCoins = trade.RecipientCoins
    });
}).DisableAntiforgery();

// POST /api/bot/game/trade/accept -- accept by offer ID, execute swap atomically
app.MapPost("/api/bot/game/trade/accept", async (
    HttpContext ctx, IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    BotGameTradeActionRequest req) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();

    var recipient = await EnsureDiscordPlayer(req.DiscordUserId, db);
    var trade = await db.TradeOffers.FirstOrDefaultAsync(t => t.Id == req.OfferId && t.Status == TradeStatus.Pending);
    if (trade == null) return Results.NotFound(new { error = "Trade not found or already resolved." });

    if (recipient.Id != trade.RecipientId) return Results.Forbid();

    if (trade.ExpiresAt < DateTime.UtcNow)
    {
        trade.Status = TradeStatus.Expired;
        await db.SaveChangesAsync();
        return Results.BadRequest(new { error = "Trade expired." });
    }

    var initiator = await db.PlayerCharacters.FirstOrDefaultAsync(p => p.Id == trade.InitiatorId);
    if (initiator == null) return Results.BadRequest(new { error = "Initiator not found." });

    async Task TransferGameItems(string itemsJson, Guid fromId, Guid toId)
    {
        if (string.IsNullOrEmpty(itemsJson) || itemsJson == "[]") return;
        var items = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.JsonElement>>(itemsJson);
        foreach (var item in items ?? [])
        {
            var defId   = item.GetProperty("itemDefinitionId").GetGuid();
            var qty     = item.GetProperty("quantity").GetInt32();
            var fromInv = await db.PlayerInventoryItems
                .FirstOrDefaultAsync(i => i.PlayerId == fromId && i.ItemDefinitionId == defId);
            if (fromInv != null)
            {
                fromInv.Quantity -= qty;
                if (fromInv.Quantity <= 0) db.PlayerInventoryItems.Remove(fromInv);
            }
            var toInv = await db.PlayerInventoryItems
                .FirstOrDefaultAsync(i => i.PlayerId == toId && i.ItemDefinitionId == defId);
            if (toInv != null) toInv.Quantity += qty;
            else db.PlayerInventoryItems.Add(new PlayerInventoryItem
                { PlayerId = toId, ItemDefinitionId = defId, Quantity = qty });
        }
    }

    await TransferGameItems(trade.InitiatorItems, initiator.Id, recipient.Id);
    await TransferGameItems(trade.RecipientItems, recipient.Id, initiator.Id);

    if (trade.InitiatorCoins > 0) { initiator.CoinBalance -= trade.InitiatorCoins; recipient.CoinBalance += trade.InitiatorCoins; }
    if (trade.RecipientCoins > 0) { recipient.CoinBalance -= trade.RecipientCoins; initiator.CoinBalance += trade.RecipientCoins; }

    trade.Status = TradeStatus.Accepted;
    await db.SaveChangesAsync();
    return Results.Ok(new { success = true });
}).DisableAntiforgery();

// POST /api/bot/game/trade/decline -- decline by offer ID
app.MapPost("/api/bot/game/trade/decline", async (
    HttpContext ctx, IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db,
    BotGameTradeActionRequest req) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();

    var trade = await db.TradeOffers.FirstOrDefaultAsync(t => t.Id == req.OfferId && t.Status == TradeStatus.Pending);
    if (trade == null) return Results.NotFound(new { error = "Trade not found." });

    var player = await EnsureDiscordPlayer(req.DiscordUserId, db);
    if (player.Id != trade.InitiatorId && player.Id != trade.RecipientId)
        return Results.Forbid();

    trade.Status = TradeStatus.Declined;
    await db.SaveChangesAsync();
    return Results.Ok(new { success = true });
}).DisableAntiforgery();

// GET /api/bot/game/trade/{offerId} -- get offer status and details
app.MapGet("/api/bot/game/trade/{offerId:guid}", async (
    Guid offerId, HttpContext ctx, IConfiguration cfg,
    peeposredemption.Infrastructure.Persistence.AppDbContext db) =>
{
    if (!BotAuth(ctx, cfg)) return Results.Unauthorized();
    var trade = await db.TradeOffers.FirstOrDefaultAsync(t => t.Id == offerId);
    if (trade == null) return Results.NotFound(new { error = "Trade not found." });
    return Results.Ok(new
    {
        id             = trade.Id,
        status         = trade.Status.ToString(),
        initiatorItems = trade.InitiatorItems,
        initiatorCoins = trade.InitiatorCoins,
        recipientItems = trade.RecipientItems,
        recipientCoins = trade.RecipientCoins,
        expiresAt      = trade.ExpiresAt
    });
});

app.Run();

record BotAutoLinkRequest(string DiscordUserId, string DiscordUsername);
record BotPvpRewardRequest(string WinnerDiscordId, string LoserDiscordId);
record BotAddCoinsRequest(string DiscordId, long Amount, string Reason);
record BotLinkRequest(string DiscordUserId, string TorvexUsername);
record BotGameCommandRequest(string DiscordUserId, string Command);
record BotDiscordUserRequest(string DiscordUserId);
record BotPeepoEmojiDto(string Name, string Url);
record BotPeepoBuyRequest(string DiscordUserId, string PeepoName);
record BotPeepoMarketListRequest(string DiscordUserId, string PeepoName, long Price);
record BotPeepoMarketBuyRequest(string DiscordUserId, Guid ListingId);
record BotPeepoTradeOfferRequest(string InitiatorDiscordId, string RecipientDiscordId, string InitiatorPeepoName, long InitiatorCoins);
record BotPeepoTradeActionRequest(string DiscordUserId);
record BotPeepoCrateRequest(string DiscordUserId);
record BotPeepoBuyCoinsRequest(string DiscordId, Guid ItemDefinitionId);
record BotPeepoCrateV2Request(string DiscordId, string CrateType);
record BotGiftCoinsRequest(string SenderDiscordId, string RecipientDiscordId, long Amount);
record BotSyncLevelRequest(string DiscordId, int NewLevel);
record BotGameTradeItemEntry(Guid ItemDefinitionId, int Quantity);
record BotGameTradeOfferRequest(string InitiatorDiscordId, string RecipientDiscordId,
    List<BotGameTradeItemEntry>? InitiatorItems, long InitiatorCoins,
    List<BotGameTradeItemEntry>? RecipientItems, long RecipientCoins);
record BotGameTradeActionRequest(string DiscordUserId, Guid OfferId);

record OrbPurchaseRequest(int Tier);
record OrbGiftRequest(string ChannelId, string RecipientUsername, long Amount, string? Message);
record ArtistPayoutRequest(Guid ArtistId, long AmountCents, string? Reference);
record ModerationActionRequest(string ServerId, string TargetUserId);
record MuteActionRequest(string ServerId, string TargetUserId, int DurationMinutes = 10);
record FingerprintRequest(string FingerprintHash, string? RawComponents);
record IpBanRequest(string IpAddress, string? Reason);
record ClearLockoutRequest(string IpAddress);
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
record SwitchAccountRequest(string Jwt);
