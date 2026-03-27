using MediatR;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Security.Queries;

public record GetServerOverlapQuery(Guid UserId1, Guid UserId2) : IRequest<int>;

public class GetServerOverlapQueryHandler : IRequestHandler<GetServerOverlapQuery, int>
{
    private readonly IUnitOfWork _uow;
    public GetServerOverlapQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<int> Handle(GetServerOverlapQuery query, CancellationToken ct)
    {
        var servers1 = (await _uow.Servers.GetUserServersAsync(query.UserId1))
            .Select(s => s.Id).ToHashSet();
        var servers2 = (await _uow.Servers.GetUserServersAsync(query.UserId2))
            .Select(s => s.Id);

        return servers2.Count(id => servers1.Contains(id));
    }
}
