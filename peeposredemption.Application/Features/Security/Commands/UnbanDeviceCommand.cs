using MediatR;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Security.Commands;

public record UnbanDeviceCommand(Guid DeviceId) : IRequest<Unit>;

public class UnbanDeviceCommandHandler : IRequestHandler<UnbanDeviceCommand, Unit>
{
    private readonly IUnitOfWork _uow;

    public UnbanDeviceCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Unit> Handle(UnbanDeviceCommand cmd, CancellationToken ct)
    {
        var devices = await _uow.UserDevices.GetByDeviceIdAsync(cmd.DeviceId);
        foreach (var device in devices)
            device.IsBanned = false;
        await _uow.SaveChangesAsync();
        return Unit.Value;
    }
}
