using MediatR;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Servers.Queries;

public record PeekInviteResult(Guid ServerId, string ServerName);
public record PeekInviteQuery(string Code) : IRequest<PeekInviteResult?>;

public class PeekInviteQueryHandler : IRequestHandler<PeekInviteQuery, PeekInviteResult?>
{
    private readonly IUnitOfWork _uow;
    public PeekInviteQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PeekInviteResult?> Handle(PeekInviteQuery query, CancellationToken ct)
    {
        var invite = await _uow.ServerInvites.GetByCodeAsync(query.Code);
        if (invite == null) return null;
        return new PeekInviteResult(invite.ServerId, invite.Server.Name);
    }
}
