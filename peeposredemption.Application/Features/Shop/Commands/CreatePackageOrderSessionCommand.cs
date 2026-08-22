using MediatR;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Shop.Commands;

/// <summary>
/// Starts Stripe Checkout for a fixed-price catalog package. The caller (the
/// Razor page) owns the catalog lookup; this handler owns the Stripe customer,
/// the pending order row, and the session.
/// </summary>
public record CreatePackageOrderSessionCommand(
    Guid UserId,
    string PackageSlug,
    string PackageName,
    string Description,
    long PriceCents,
    string? DiscordGuildId,
    string? DiscordGuildName,
    decimal? CreditUsd,
    string ReturnBaseUrl) : IRequest<string>;

public class CreatePackageOrderSessionCommandHandler : IRequestHandler<CreatePackageOrderSessionCommand, string>
{
    private readonly IUnitOfWork _uow;
    private readonly IStripeService _stripe;

    public CreatePackageOrderSessionCommandHandler(IUnitOfWork uow, IStripeService stripe)
    {
        _uow = uow;
        _stripe = stripe;
    }

    public async Task<string> Handle(CreatePackageOrderSessionCommand cmd, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(cmd.UserId)
            ?? throw new InvalidOperationException("User not found.");
        if (user.Email.EndsWith("@bot.torvex.app", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Add a real email to your account before checking out — invoices are emailed.");

        var profile = await _uow.CustomerProfiles.GetByUserIdAsync(user.Id);
        var customerId = await _stripe.GetOrCreateCustomerAsync(user, profile?.FullName, profile?.Company);

        var order = new PackageOrder
        {
            UserId = user.Id,
            PackageSlug = cmd.PackageSlug,
            PackageName = cmd.PackageName,
            PriceCents = cmd.PriceCents,
            DiscordGuildId = cmd.DiscordGuildId,
            DiscordGuildName = cmd.DiscordGuildName,
            CreditUsd = cmd.CreditUsd
        };

        var metadata = new Dictionary<string, string> { ["packageSlug"] = cmd.PackageSlug };
        if (cmd.DiscordGuildId != null) metadata["discordGuildId"] = cmd.DiscordGuildId;
        if (cmd.CreditUsd.HasValue) metadata["creditUsd"] = cmd.CreditUsd.Value.ToString("0.##");

        var description = cmd.DiscordGuildName != null
            ? $"{cmd.Description} Server: {cmd.DiscordGuildName} ({cmd.DiscordGuildId})."
            : cmd.Description;

        var successUrl = $"{cmd.ReturnBaseUrl}/Dashboard?tab=billing&ordered=1";
        var cancelUrl = $"{cmd.ReturnBaseUrl}/Packages";

        var result = await _stripe.CreatePackageOrderSessionAsync(
            customerId, user.Id, order.Id, cmd.PackageName, description,
            cmd.PriceCents, metadata, successUrl, cancelUrl);

        order.StripeSessionId = result.SessionId;
        await _uow.PackageOrders.AddAsync(order);
        await _uow.SaveChangesAsync();   // also persists user.StripeCustomerId
        return result.Url;
    }
}
