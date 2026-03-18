using MediatR;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Security.Commands;

public record RecordUserLoginInfoCommand(Guid UserId, string IpAddress, Guid DeviceId) : IRequest<Unit>;

public class RecordUserLoginInfoCommandHandler : IRequestHandler<RecordUserLoginInfoCommand, Unit>
{
    private readonly IUnitOfWork _uow;
    private readonly IVpnDetectionService _vpnDetection;

    public RecordUserLoginInfoCommandHandler(IUnitOfWork uow, IVpnDetectionService vpnDetection)
    {
        _uow = uow;
        _vpnDetection = vpnDetection;
    }

    public async Task<Unit> Handle(RecordUserLoginInfoCommand cmd, CancellationToken ct)
    {
        // Check VPN/Tor
        var (isVpn, isTor) = await _vpnDetection.CheckIpAsync(cmd.IpAddress);

        // Log IP
        await _uow.UserIpLogs.AddAsync(new UserIpLog
        {
            UserId = cmd.UserId,
            IpAddress = cmd.IpAddress,
            IsVpn = isVpn,
            IsTor = isTor
        });

        // Upsert device
        if (cmd.DeviceId != Guid.Empty)
        {
            var existing = await _uow.UserDevices.GetAsync(cmd.DeviceId, cmd.UserId);
            if (existing != null)
            {
                existing.LastSeenAt = DateTime.UtcNow;
            }
            else
            {
                await _uow.UserDevices.AddAsync(new UserDevice
                {
                    DeviceId = cmd.DeviceId,
                    UserId = cmd.UserId
                });
            }
        }

        await _uow.SaveChangesAsync();
        return Unit.Value;
    }
}
