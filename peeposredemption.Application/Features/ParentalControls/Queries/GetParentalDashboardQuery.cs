using MediatR;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.ParentalControls.Queries;

public record ParentalDashboardChildDto(
    Guid LinkId,
    string ChildUsername,
    int ServerCount,
    int FriendCount,
    bool AccountFrozen,
    bool DmFriendsOnly);

public record GetParentalDashboardQuery(Guid ParentUserId) : IRequest<List<ParentalDashboardChildDto>>;

public class GetParentalDashboardQueryHandler : IRequestHandler<GetParentalDashboardQuery, List<ParentalDashboardChildDto>>
{
    private readonly IUnitOfWork _uow;
    public GetParentalDashboardQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<List<ParentalDashboardChildDto>> Handle(GetParentalDashboardQuery query, CancellationToken ct)
    {
        var links = await _uow.ParentalLinks.GetActiveByParentIdAsync(query.ParentUserId);
        var result = new List<ParentalDashboardChildDto>();

        foreach (var link in links)
        {
            var friends = await _uow.FriendRequests.GetAcceptedAsync(link.ChildUserId);
            result.Add(new ParentalDashboardChildDto(
                link.Id,
                link.Child.Username,
                link.Child.ServerMemberships?.Count ?? 0,
                friends.Count,
                link.AccountFrozen,
                link.DmFriendsOnly));
        }

        return result;
    }
}
