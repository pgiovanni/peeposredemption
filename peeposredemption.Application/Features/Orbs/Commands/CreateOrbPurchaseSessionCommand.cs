using MediatR;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Orbs.Commands;

public record CreateOrbPurchaseSessionCommand(Guid UserId, OrbPackTier Tier, string ReturnBaseUrl) : IRequest<string>;

public class CreateOrbPurchaseSessionCommandHandler : IRequestHandler<CreateOrbPurchaseSessionCommand, string>
{
    private readonly IUnitOfWork _uow;
    private readonly IStripeService _stripe;

    public CreateOrbPurchaseSessionCommandHandler(IUnitOfWork uow, IStripeService stripe)
    {
        _uow = uow;
        _stripe = stripe;
    }

    public async Task<string> Handle(CreateOrbPurchaseSessionCommand cmd, CancellationToken ct)
    {
        var (orbAmount, priceCents) = cmd.Tier switch
        {
            OrbPackTier.Pack100 => (100, 99L),
            OrbPackTier.Pack600 => (600, 499L),
            OrbPackTier.Pack1500 => (1500, 999L),
            _ => throw new ArgumentException("Invalid orb pack tier.")
        };

        var successUrl = $"{cmd.ReturnBaseUrl}/App/Wallet?purchased=true";
        var cancelUrl = $"{cmd.ReturnBaseUrl}/App/OrbShop";

        var result = await _stripe.CreateOrbPurchaseSessionAsync(
            cmd.UserId, orbAmount, priceCents, successUrl, cancelUrl);

        await _uow.OrbPurchases.AddAsync(new OrbPurchase
        {
            UserId = cmd.UserId,
            StripeSessionId = result.SessionId,
            OrbAmount = orbAmount,
            PriceCents = priceCents
        });

        await _uow.SaveChangesAsync();
        return result.Url;
    }
}
