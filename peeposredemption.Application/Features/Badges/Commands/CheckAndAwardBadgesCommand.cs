using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Badges.Commands;

public record BadgeEarned(Guid BadgeId, string Name, string Icon);

public record CheckAndAwardBadgesCommand(Guid UserId, string StatKey, long CurrentValue) : IRequest<List<BadgeEarned>>;

public class CheckAndAwardBadgesCommandHandler : IRequestHandler<CheckAndAwardBadgesCommand, List<BadgeEarned>>
{
    private readonly IUnitOfWork _uow;
    public CheckAndAwardBadgesCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<List<BadgeEarned>> Handle(CheckAndAwardBadgesCommand cmd, CancellationToken ct)
    {
        var earned = new List<BadgeEarned>();
        var badges = await _uow.BadgeDefinitions.GetByStatKeyAsync(cmd.StatKey);

        foreach (var badge in badges)
        {
            if (cmd.CurrentValue < badge.Threshold) continue;
            if (await _uow.UserBadges.HasBadgeAsync(cmd.UserId, badge.Id)) continue;

            await _uow.UserBadges.AddAsync(new UserBadge
            {
                UserId = cmd.UserId,
                BadgeDefinitionId = badge.Id
            });

            earned.Add(new BadgeEarned(badge.Id, badge.Name, badge.Icon));
        }

        if (earned.Count > 0)
            await _uow.SaveChangesAsync();

        return earned;
    }
}
