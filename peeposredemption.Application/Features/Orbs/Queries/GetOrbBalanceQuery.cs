using MediatR;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Orbs.Queries;

public record OrbBalanceResult(long Balance, int CurrentStreak, int LongestStreak, bool ClaimedToday);

public record GetOrbBalanceQuery(Guid UserId) : IRequest<OrbBalanceResult>;

public class GetOrbBalanceQueryHandler : IRequestHandler<GetOrbBalanceQuery, OrbBalanceResult>
{
    private readonly IUnitOfWork _uow;
    public GetOrbBalanceQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<OrbBalanceResult> Handle(GetOrbBalanceQuery query, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(query.UserId);
        if (user == null) return new OrbBalanceResult(0, 0, 0, false);

        var streak = await _uow.UserLoginStreaks.GetByUserIdAsync(query.UserId);
        var today = DateTime.UtcNow.Date;
        var claimedToday = streak?.LastClaimedDate.HasValue == true && streak.LastClaimedDate.Value.Date == today;

        return new OrbBalanceResult(
            user.OrbBalance,
            streak?.CurrentStreak ?? 0,
            streak?.LongestStreak ?? 0,
            claimedToday);
    }
}
