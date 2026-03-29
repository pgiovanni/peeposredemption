using MediatR;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Shop.Commands;

public record CancelGoldSubscriptionCommand(Guid UserId) : IRequest;

public class CancelGoldSubscriptionCommandHandler : IRequestHandler<CancelGoldSubscriptionCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly IStripeService _stripe;

    public CancelGoldSubscriptionCommandHandler(IUnitOfWork uow, IStripeService stripe)
    {
        _uow = uow;
        _stripe = stripe;
    }

    public async Task Handle(CancelGoldSubscriptionCommand cmd, CancellationToken ct)
    {
        var subscription = await _uow.GoldSubscriptions.GetByUserIdAsync(cmd.UserId);
        if (subscription == null || subscription.Status != SubscriptionStatus.Active)
            throw new InvalidOperationException("No active Torvex Gold subscription found.");

        // CancelAtPeriodEnd = true — user keeps Gold until billing period ends
        await _stripe.CancelSubscriptionAsync(subscription.StripeSubscriptionId);

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync();
    }
}
