using MediatR;
using peeposredemption.Application.DTOs.Servers;
using peeposredemption.Domain.Interfaces;
 
namespace peeposredemption.Application.Features.Servers.Queries;
 
public record GetUserServersQuery(Guid UserId) : IRequest<List<ServerDto>>;
 
public class GetUserServersQueryHandler : IRequestHandler<GetUserServersQuery, List<ServerDto>>
{
    private readonly IUnitOfWork _uow;
 
    public GetUserServersQueryHandler(IUnitOfWork uow) => _uow = uow;
 
    public async Task<List<ServerDto>> Handle(GetUserServersQuery query, CancellationToken ct)
    {
        var servers = await _uow.Servers.GetUserServersAsync(query.UserId);
 
        return servers.Select(s => new ServerDto(
            s.Id,
            s.Name,
            s.IconUrl
        )).ToList();
    }
}
