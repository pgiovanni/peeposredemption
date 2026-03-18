using MediatR;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Security.Commands;

public record ToggleSuspiciousCommand(Guid TargetUserId, bool IsSuspicious) : IRequest<Unit>;

public class ToggleSuspiciousCommandHandler : IRequestHandler<ToggleSuspiciousCommand, Unit>
{
    private readonly IUnitOfWork _uow;

    public ToggleSuspiciousCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Unit> Handle(ToggleSuspiciousCommand cmd, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(cmd.TargetUserId)
            ?? throw new InvalidOperationException("User not found.");
        user.IsSuspicious = cmd.IsSuspicious;
        await _uow.SaveChangesAsync();
        return Unit.Value;
    }
}
