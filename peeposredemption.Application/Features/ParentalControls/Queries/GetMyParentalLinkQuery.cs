using MediatR;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.ParentalControls.Queries;

public record MyParentalLinkDto(
    Guid LinkId,
    string? ParentUsername,
    bool AccountFrozen,
    bool DmFriendsOnly);

public record GetMyParentalLinkQuery(Guid ChildUserId) : IRequest<MyParentalLinkDto?>;

public class GetMyParentalLinkQueryHandler : IRequestHandler<GetMyParentalLinkQuery, MyParentalLinkDto?>
{
    private readonly IUnitOfWork _uow;
    public GetMyParentalLinkQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<MyParentalLinkDto?> Handle(GetMyParentalLinkQuery query, CancellationToken ct)
    {
        var link = await _uow.ParentalLinks.GetActiveByChildIdAsync(query.ChildUserId);
        if (link == null) return null;

        return new MyParentalLinkDto(
            link.Id,
            link.Parent?.Username,
            link.AccountFrozen,
            link.DmFriendsOnly);
    }
}
