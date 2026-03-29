using MediatR;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Shop.Commands;

public record CreateGoldSubscriptionSessionCommand(Guid UserId, string ReturnBaseUrl) : IRequest<string>;

public class CreateGoldSubscriptionSessionCommandHandler : IRequestHandler<CreateGoldSubscriptionSessionCommand, string>
{
    private readonly IUnitOfWork _uow;
    private readonly IStripeService _stripe;

    public CreateGoldSubscriptionSessionCommandHandler(IUnitOfWork uow, IStripeService stripe)
    {
        _uow = uow;
        _stripe = stripe;
    }

    public async Task<string> Handle(CreateGoldSubscriptionSessionCommand cmd, CancellationToken ct)
    {
        // Prevent double-subscribing: reject if there's already an active or pending subscription
        var existing = await _uow.GoldSubscriptions.GetByUserIdAsync(cmd.UserId);
        if (existing != null && (existing.Status == SubscriptionStatus.Active || existing.Status == SubscriptionStatus.Pending))
            throw new InvalidOperationException("You already have an active Torvex Gold subscription.");

        var successUrl = $"{cmd.ReturnBaseUrl}/App/Gold?activated=true";
        var cancelUrl = $"{cmd.ReturnBaseUrl}/App/Gold";

        var result = await _stripe.CreateGoldSubscriptionSessionAsync(cmd.UserId, successUrl, cancelUrl);

        await _uow.GoldSubscriptions.AddAsync(new TorvexGoldSubscription
        {
            UserId = cmd.UserId,
            StripeSessionId = result.SessionId,
            Status = SubscriptionStatus.Pending
        });

        await _uow.SaveChangesAsync();
        return result.Url;
    }
}
