using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Badges.Queries;

public record UserBadgeDto(
    Guid BadgeId,
    string Name,
    string Description,
    string Icon,
    string Category,
    DateTime EarnedAt,
    bool IsDisplayed);

public record GetUserBadgesQuery(Guid UserId) : IRequest<List<UserBadgeDto>>;

public class GetUserBadgesQueryHandler : IRequestHandler<GetUserBadgesQuery, List<UserBadgeDto>>
{
    private readonly IUnitOfWork _uow;
    public GetUserBadgesQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<List<UserBadgeDto>> Handle(GetUserBadgesQuery query, CancellationToken ct)
    {
        var badges = await _uow.UserBadges.GetByUserIdAsync(query.UserId);
        return badges.Select(ub => new UserBadgeDto(
            ub.BadgeDefinitionId,
            ub.BadgeDefinition.Name,
            ub.BadgeDefinition.Description,
            ub.BadgeDefinition.Icon,
            ub.BadgeDefinition.Category.ToString(),
            ub.EarnedAt,
            ub.IsDisplayed
        )).ToList();
    }
}
