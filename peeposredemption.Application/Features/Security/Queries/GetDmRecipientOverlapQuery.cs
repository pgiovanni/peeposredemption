using MediatR;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Security.Queries;

public record GetDmRecipientOverlapQuery(Guid UserId1, Guid UserId2) : IRequest<double>;

public class GetDmRecipientOverlapQueryHandler : IRequestHandler<GetDmRecipientOverlapQuery, double>
{
    private readonly IUnitOfWork _uow;
    public GetDmRecipientOverlapQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<double> Handle(GetDmRecipientOverlapQuery query, CancellationToken ct)
    {
        var r1 = (await _uow.DirectMessages.GetDistinctRecipientsAsync(query.UserId1)).ToHashSet();
        var r2 = (await _uow.DirectMessages.GetDistinctRecipientsAsync(query.UserId2)).ToHashSet();

        // Remove each user from the other's recipient set (they may DM each other)
        r1.Remove(query.UserId2);
        r2.Remove(query.UserId1);

        if (r1.Count == 0 && r2.Count == 0) return 0;

        int intersection = r1.Count(id => r2.Contains(id));
        int union = r1.Union(r2).Count();

        return union == 0 ? 0 : (double)intersection / union;
    }
}
