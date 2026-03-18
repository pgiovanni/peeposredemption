using MediatR;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Security.Commands;

public record BanDeviceCommand(Guid DeviceId) : IRequest<Unit>;

public class BanDeviceCommandHandler : IRequestHandler<BanDeviceCommand, Unit>
{
    private readonly IUnitOfWork _uow;

    public BanDeviceCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Unit> Handle(BanDeviceCommand cmd, CancellationToken ct)
    {
        var devices = await _uow.UserDevices.GetByDeviceIdAsync(cmd.DeviceId);
        foreach (var device in devices)
            device.IsBanned = true;
        await _uow.SaveChangesAsync();
        return Unit.Value;
    }
}
