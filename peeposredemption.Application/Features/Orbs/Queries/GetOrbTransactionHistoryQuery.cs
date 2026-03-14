using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Orbs.Queries;

public record OrbTransactionDto(Guid Id, long Amount, string Type, string Description, DateTime CreatedAt);

public record GetOrbTransactionHistoryQuery(Guid UserId) : IRequest<List<OrbTransactionDto>>;

public class GetOrbTransactionHistoryQueryHandler : IRequestHandler<GetOrbTransactionHistoryQuery, List<OrbTransactionDto>>
{
    private readonly IUnitOfWork _uow;
    public GetOrbTransactionHistoryQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<List<OrbTransactionDto>> Handle(GetOrbTransactionHistoryQuery query, CancellationToken ct)
    {
        var transactions = await _uow.OrbTransactions.GetRecentAsync(query.UserId, 50);
        return transactions.Select(t => new OrbTransactionDto(
            t.Id, t.Amount, t.Type.ToString(), t.Description, t.CreatedAt)).ToList();
    }
}
