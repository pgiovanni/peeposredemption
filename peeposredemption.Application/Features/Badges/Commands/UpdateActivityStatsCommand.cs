using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Badges.Commands;

public record UpdateActivityStatsCommand(
    Guid UserId,
    long? IncrementMessages = null,
    int? NewLongestStreak = null,
    long? IncrementOrbsGifted = null,
    int? IncrementServersJoined = null,
    long? NewOrbBalance = null) : IRequest<UserActivityStats>;

public class UpdateActivityStatsCommandHandler : IRequestHandler<UpdateActivityStatsCommand, UserActivityStats>
{
    private readonly IUnitOfWork _uow;
    public UpdateActivityStatsCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<UserActivityStats> Handle(UpdateActivityStatsCommand cmd, CancellationToken ct)
    {
        var stats = await _uow.UserActivityStats.GetByUserIdAsync(cmd.UserId);
        if (stats == null)
        {
            stats = new UserActivityStats { UserId = cmd.UserId };
            await _uow.UserActivityStats.AddAsync(stats);
        }

        if (cmd.IncrementMessages.HasValue)
            stats.TotalMessages += cmd.IncrementMessages.Value;

        if (cmd.NewLongestStreak.HasValue && cmd.NewLongestStreak.Value > stats.LongestStreak)
            stats.LongestStreak = cmd.NewLongestStreak.Value;

        if (cmd.IncrementOrbsGifted.HasValue)
            stats.TotalOrbsGifted += cmd.IncrementOrbsGifted.Value;

        if (cmd.IncrementServersJoined.HasValue)
            stats.ServersJoined += cmd.IncrementServersJoined.Value;

        if (cmd.NewOrbBalance.HasValue && cmd.NewOrbBalance.Value > stats.PeakOrbBalance)
            stats.PeakOrbBalance = cmd.NewOrbBalance.Value;

        await _uow.SaveChangesAsync();
        return stats;
    }
}
