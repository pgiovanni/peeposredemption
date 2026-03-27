using MediatR;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Security.Commands;

/// <summary>
/// Confirm (mark as real alt), dismiss (false positive), or ban (flag both users as suspicious).
/// </summary>
public record ReviewAltSuspicionCommand(Guid SuspicionId, string Action) : IRequest<bool>;
// Action: "confirm" | "dismiss" | "ban"

public class ReviewAltSuspicionCommandHandler : IRequestHandler<ReviewAltSuspicionCommand, bool>
{
    private readonly IUnitOfWork _uow;
    public ReviewAltSuspicionCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(ReviewAltSuspicionCommand cmd, CancellationToken ct)
    {
        var suspicion = await _uow.AltSuspicions.GetByIdAsync(cmd.SuspicionId);
        if (suspicion == null) return false;

        suspicion.ReviewedAt = DateTime.UtcNow;

        switch (cmd.Action.ToLower())
        {
            case "confirm":
                suspicion.IsConfirmed = true;
                var user2 = await _uow.Users.GetByIdAsync(suspicion.UserId2);
                if (user2 != null) user2.IsSuspicious = true;
                break;

            case "ban":
                suspicion.IsConfirmed = true;
                var u1 = await _uow.Users.GetByIdAsync(suspicion.UserId1);
                var u2 = await _uow.Users.GetByIdAsync(suspicion.UserId2);
                if (u1 != null) u1.IsSuspicious = true;
                if (u2 != null) u2.IsSuspicious = true;
                break;

            case "dismiss":
                suspicion.IsConfirmed = false;
                break;

            default:
                return false;
        }

        await _uow.SaveChangesAsync();
        return true;
    }
}
