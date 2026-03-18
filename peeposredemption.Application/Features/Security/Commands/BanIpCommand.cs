using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Security.Commands;

public record BanIpCommand(string IpAddress, Guid AdminUserId, string? Reason) : IRequest<Unit>;

public class BanIpCommandHandler : IRequestHandler<BanIpCommand, Unit>
{
    private readonly IUnitOfWork _uow;

    public BanIpCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Unit> Handle(BanIpCommand cmd, CancellationToken ct)
    {
        await _uow.IpBans.AddAsync(new IpBan
        {
            IpAddress = cmd.IpAddress,
            BannedByUserId = cmd.AdminUserId,
            Reason = cmd.Reason
        });
        await _uow.SaveChangesAsync();
        return Unit.Value;
    }
}
