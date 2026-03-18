using MediatR;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Security.Queries;

public record GetIpBansQuery() : IRequest<List<IpBanDto>>;

public record IpBanDto(Guid Id, string IpAddress, string? Reason, string BannedByUsername, DateTime CreatedAt, DateTime? ExpiresAt);

public class GetIpBansQueryHandler : IRequestHandler<GetIpBansQuery, List<IpBanDto>>
{
    private readonly IUnitOfWork _uow;

    public GetIpBansQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<List<IpBanDto>> Handle(GetIpBansQuery query, CancellationToken ct)
    {
        var bans = await _uow.IpBans.GetAllAsync();
        return bans.Select(b => new IpBanDto(
            b.Id, b.IpAddress, b.Reason,
            b.BannedBy?.Username ?? "Unknown",
            b.CreatedAt, b.ExpiresAt)).ToList();
    }
}
