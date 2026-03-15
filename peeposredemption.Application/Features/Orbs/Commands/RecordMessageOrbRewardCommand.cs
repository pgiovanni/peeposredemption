using MediatR;
using peeposredemption.Application.Features.Badges.Commands;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Orbs.Commands;

public record RecordMessageOrbRewardCommand(Guid UserId) : IRequest;

public class RecordMessageOrbRewardCommandHandler : IRequestHandler<RecordMessageOrbRewardCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;
    public RecordMessageOrbRewardCommandHandler(IUnitOfWork uow, IMediator mediator)
    {
        _uow = uow;
        _mediator = mediator;
    }

    private const int MessagesPerOrb = 10;
    private const int DailyOrbCap = 50;

    public async Task Handle(RecordMessageOrbRewardCommand cmd, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var streak = await _uow.UserLoginStreaks.GetByUserIdAsync(cmd.UserId);

        if (streak == null)
        {
            streak = new UserLoginStreak { UserId = cmd.UserId };
            await _uow.UserLoginStreaks.AddAsync(streak);
        }

        // Reset counter if new day
        if (!streak.MessageCountDate.HasValue || streak.MessageCountDate.Value.Date != today)
        {
            streak.MessageCountToday = 0;
            streak.MessageCountDate = today;
        }

        streak.MessageCountToday++;

        // Check if we've hit a multiple of MessagesPerOrb and are under cap
        int orbsEarnedToday = (streak.MessageCountToday / MessagesPerOrb);
        int orbsEarnedBefore = ((streak.MessageCountToday - 1) / MessagesPerOrb);

        if (orbsEarnedToday > orbsEarnedBefore && orbsEarnedBefore < DailyOrbCap)
        {
            var user = await _uow.Users.GetByIdAsync(cmd.UserId);
            if (user != null)
            {
                await _uow.OrbTransactions.AddAsync(new OrbTransaction
                {
                    UserId = cmd.UserId,
                    Amount = 1,
                    Type = OrbTransactionType.MessageReward,
                    Description = "Message activity reward"
                });
                user.OrbBalance += 1;
            }
        }

        await _uow.SaveChangesAsync();

        // Update activity stats + check badges (fire-and-forget style within handler)
        var stats = await _mediator.Send(new UpdateActivityStatsCommand(cmd.UserId, IncrementMessages: 1), ct);
        await _mediator.Send(new CheckAndAwardBadgesCommand(cmd.UserId, "TotalMessages", stats.TotalMessages), ct);
    }
}
