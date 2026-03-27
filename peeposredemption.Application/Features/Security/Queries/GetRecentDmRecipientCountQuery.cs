using MediatR;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Security.Queries;

public record GetRecentDmRecipientCountQuery(Guid UserId, int Hours) : IRequest<int>;

public class GetRecentDmRecipientCountQueryHandler : IRequestHandler<GetRecentDmRecipientCountQuery, int>
{
    private readonly IUnitOfWork _uow;
    public GetRecentDmRecipientCountQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<int> Handle(GetRecentDmRecipientCountQuery query, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddHours(-query.Hours);
        return await _uow.DirectMessages.GetRecentRecipientCountAsync(query.UserId, since);
    }
}
