using MediatR;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Security.Commands;

public record UnbanIpCommand(Guid IpBanId) : IRequest<Unit>;

public class UnbanIpCommandHandler : IRequestHandler<UnbanIpCommand, Unit>
{
    private readonly IUnitOfWork _uow;

    public UnbanIpCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Unit> Handle(UnbanIpCommand cmd, CancellationToken ct)
    {
        await _uow.IpBans.RemoveAsync(cmd.IpBanId);
        await _uow.SaveChangesAsync();
        return Unit.Value;
    }
}
