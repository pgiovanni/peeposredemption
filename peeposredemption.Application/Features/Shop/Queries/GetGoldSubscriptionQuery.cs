using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Shop.Queries;

public record GoldSubscriptionDto(bool IsActive, string Status, DateTime? NextBillingAt, DateTime? CancelledAt);

public record GetGoldSubscriptionQuery(Guid UserId) : IRequest<GoldSubscriptionDto>;

public class GetGoldSubscriptionQueryHandler : IRequestHandler<GetGoldSubscriptionQuery, GoldSubscriptionDto>
{
    private readonly IUnitOfWork _uow;

    public GetGoldSubscriptionQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<GoldSubscriptionDto> Handle(GetGoldSubscriptionQuery query, CancellationToken ct)
    {
        var sub = await _uow.GoldSubscriptions.GetByUserIdAsync(query.UserId);
        if (sub == null)
            return new GoldSubscriptionDto(false, "None", null, null);

        return new GoldSubscriptionDto(
            sub.Status == SubscriptionStatus.Active,
            sub.Status.ToString(),
            sub.NextBillingAt,
            sub.CancelledAt);
    }
}
