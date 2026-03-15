using MediatR;
using peeposredemption.Application.Features.Badges.Commands;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Orbs.Commands;

public record DailyClaimResult(long OrbsAwarded, int CurrentStreak, int LongestStreak, long NewBalance);

public record ClaimDailyLoginCommand(Guid UserId) : IRequest<DailyClaimResult>;

public class ClaimDailyLoginCommandHandler : IRequestHandler<ClaimDailyLoginCommand, DailyClaimResult>
{
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;
    public ClaimDailyLoginCommandHandler(IUnitOfWork uow, IMediator mediator)
    {
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<DailyClaimResult> Handle(ClaimDailyLoginCommand cmd, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var streak = await _uow.UserLoginStreaks.GetByUserIdAsync(cmd.UserId);

        if (streak == null)
        {
            streak = new UserLoginStreak { UserId = cmd.UserId };
            await _uow.UserLoginStreaks.AddAsync(streak);
        }

        if (streak.LastClaimedDate.HasValue && streak.LastClaimedDate.Value.Date == today)
            throw new InvalidOperationException("Daily orbs already claimed today.");

        // Calculate streak
        if (streak.LastClaimedDate.HasValue && streak.LastClaimedDate.Value.Date == today.AddDays(-1))
            streak.CurrentStreak++;
        else
            streak.CurrentStreak = 1;

        if (streak.CurrentStreak > streak.LongestStreak)
            streak.LongestStreak = streak.CurrentStreak;

        streak.LastClaimedDate = today;

        // Calculate reward
        long orbs = 10;
        if (streak.CurrentStreak >= 30) orbs += 200;
        else if (streak.CurrentStreak >= 7) orbs += 50;

        // Create transaction + update balance
        var user = await _uow.Users.GetByIdAsync(cmd.UserId)
            ?? throw new InvalidOperationException("User not found.");

        await _uow.OrbTransactions.AddAsync(new OrbTransaction
        {
            UserId = cmd.UserId,
            Amount = orbs,
            Type = OrbTransactionType.DailyLogin,
            Description = $"Daily login reward (day {streak.CurrentStreak})"
        });

        user.OrbBalance += orbs;
        await _uow.SaveChangesAsync();

        // Update activity stats + check badges
        var stats = await _mediator.Send(new UpdateActivityStatsCommand(
            cmd.UserId,
            NewLongestStreak: streak.LongestStreak,
            NewOrbBalance: user.OrbBalance), ct);
        await _mediator.Send(new CheckAndAwardBadgesCommand(cmd.UserId, "LongestStreak", stats.LongestStreak), ct);
        await _mediator.Send(new CheckAndAwardBadgesCommand(cmd.UserId, "PeakOrbBalance", stats.PeakOrbBalance), ct);

        return new DailyClaimResult(orbs, streak.CurrentStreak, streak.LongestStreak, user.OrbBalance);
    }
}
