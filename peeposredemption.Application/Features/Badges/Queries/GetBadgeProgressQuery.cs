using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Badges.Queries;

public record BadgeProgressDto(
    Guid BadgeId,
    string Name,
    string Description,
    string Icon,
    string Category,
    long Threshold,
    long CurrentValue,
    bool Earned,
    DateTime? EarnedAt);

public record GetBadgeProgressQuery(Guid UserId) : IRequest<List<BadgeProgressDto>>;

public class GetBadgeProgressQueryHandler : IRequestHandler<GetBadgeProgressQuery, List<BadgeProgressDto>>
{
    private readonly IUnitOfWork _uow;
    public GetBadgeProgressQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<List<BadgeProgressDto>> Handle(GetBadgeProgressQuery query, CancellationToken ct)
    {
        var allBadges = await _uow.BadgeDefinitions.GetAllAsync();
        var earned = await _uow.UserBadges.GetByUserIdAsync(query.UserId);
        var earnedLookup = earned.ToDictionary(ub => ub.BadgeDefinitionId);
        var stats = await _uow.UserActivityStats.GetByUserIdAsync(query.UserId);

        var result = new List<BadgeProgressDto>();
        foreach (var badge in allBadges)
        {
            long currentValue = GetStatValue(stats, badge.StatKey);
            var isEarned = earnedLookup.TryGetValue(badge.Id, out var ub);

            result.Add(new BadgeProgressDto(
                badge.Id,
                badge.Name,
                badge.Description,
                badge.Icon,
                badge.Category.ToString(),
                badge.Threshold,
                currentValue,
                isEarned,
                isEarned ? ub!.EarnedAt : null
            ));
        }

        return result;
    }

    private static long GetStatValue(UserActivityStats? stats, string statKey)
    {
        if (stats == null) return 0;
        return statKey switch
        {
            "TotalMessages" => stats.TotalMessages,
            "LongestStreak" => stats.LongestStreak,
            "TotalOrbsGifted" => stats.TotalOrbsGifted,
            "ServersJoined" => stats.ServersJoined,
            "PeakOrbBalance" => stats.PeakOrbBalance,
            _ => 0
        };
    }
}
