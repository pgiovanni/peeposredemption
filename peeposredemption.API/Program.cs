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
using peeposredemption.Application.Features.Shop.Commands;
using peeposredemption.Application.Services;
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
        context.Request.Headers["Authorization"] = $"Bearer {token}";
    await next();
});

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();
app.MapHub<ChatHub>("/hubs/chat");
app.MapGet("/", () => Results.Redirect("/Auth/Login"));

// Emoji list API
app.MapGet("/api/servers/{serverId:guid}/emojis", async (Guid serverId, IMediator mediator) =>
{
    var emojis = await mediator.Send(new GetServerEmojisQuery(serverId));
    return Results.Ok(emojis);
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

app.Run();
