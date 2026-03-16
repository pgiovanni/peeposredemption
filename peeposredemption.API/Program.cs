using FluentValidation;
using MediatR;
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
    builder.Services.AddHttpClient<ResendClient>();
    builder.Services.Configure<ResendClientOptions>(o =>
        o.ApiToken = builder.Configuration["Resend:ApiKey"]);
    builder.Services.AddTransient<IResend, ResendClient>();
    builder.Services.AddScoped<IEmailService, EmailService>();
}
builder.Services.AddSingleton<IUserIdProvider, UserIdProvider>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<LinkScannerService>();
builder.Services.AddSingleton<ILinkScannerService>(sp => sp.GetRequiredService<LinkScannerService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<LinkScannerService>());

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
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddRazorPages();

var app = builder.Build();

app.Use(async (context, next) =>
{
    var token = context.Request.Cookies["jwt"];
    if (!string.IsNullOrEmpty(token))
    {
        context.Request.Headers["Authorization"] = $"Bearer {token}";
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
            var result = await mediator.Send(
                new RefreshTokenCommand(context.Request.Cookies["refreshToken"]!));

            context.Response.Cookies.Append("jwt", result.Token, new CookieOptions
            {
                HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromMinutes(15)
            });
            context.Response.Cookies.Append("refreshToken", result.RefreshToken, new CookieOptions
            {
                HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromDays(30)
            });
            context.Request.Headers["Authorization"] = $"Bearer {result.Token}";
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
app.MapGet("/", () => Results.Redirect("/Auth/Login"));

// Token refresh endpoint
app.MapPost("/api/auth/refresh", async (HttpRequest req, IMediator mediator) =>
{
    var refreshToken = req.Cookies["refreshToken"];
    if (string.IsNullOrEmpty(refreshToken))
        return Results.Unauthorized();

    try
    {
        var result = await mediator.Send(new RefreshTokenCommand(refreshToken));

        req.HttpContext.Response.Cookies.Append("jwt", result.Token, new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromMinutes(15)
        });
        req.HttpContext.Response.Cookies.Append("refreshToken", result.RefreshToken, new CookieOptions
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

// Moderation API endpoints
app.MapPost("/api/moderation/kick", async (HttpContext ctx, IMediator mediator) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var body = await ctx.Request.ReadFromJsonAsync<ModerationActionRequest>();
    if (body == null) return Results.BadRequest();
    try
    {
        await mediator.Send(new peeposredemption.Application.Features.Moderation.Commands.KickMemberCommand(
            Guid.Parse(body.ServerId), Guid.Parse(uid), Guid.Parse(body.TargetUserId)));
        return Results.Ok();
    }
    catch (UnauthorizedAccessException ex) { return Results.Json(new { error = ex.Message }, statusCode: 403); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/moderation/ban", async (HttpContext ctx, IMediator mediator) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var body = await ctx.Request.ReadFromJsonAsync<ModerationActionRequest>();
    if (body == null) return Results.BadRequest();
    try
    {
        await mediator.Send(new peeposredemption.Application.Features.Moderation.Commands.BanMemberCommand(
            Guid.Parse(body.ServerId), Guid.Parse(uid), Guid.Parse(body.TargetUserId)));
        return Results.Ok();
    }
    catch (UnauthorizedAccessException ex) { return Results.Json(new { error = ex.Message }, statusCode: 403); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/moderation/mute", async (HttpContext ctx, IMediator mediator) =>
{
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (uid == null) return Results.Unauthorized();
    var body = await ctx.Request.ReadFromJsonAsync<MuteActionRequest>();
    if (body == null) return Results.BadRequest();
    try
    {
        await mediator.Send(new peeposredemption.Application.Features.Moderation.Commands.MuteUserCommand(
            Guid.Parse(body.ServerId), Guid.Parse(uid), Guid.Parse(body.TargetUserId), body.DurationMinutes));
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
