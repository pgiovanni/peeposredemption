using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Security.Commands;

public record SubmitFingerprintCommand(Guid UserId, string FingerprintHash, string? RawComponents) : IRequest<Unit>;

public class SubmitFingerprintCommandHandler : IRequestHandler<SubmitFingerprintCommand, Unit>
{
    private readonly IUnitOfWork _uow;

    public SubmitFingerprintCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Unit> Handle(SubmitFingerprintCommand cmd, CancellationToken ct)
    {
        // Deduplicate — skip if same user+hash already exists, but backfill rawComponents if missing
        var existing = await _uow.UserFingerprints.GetByUserIdAsync(cmd.UserId);
        var match = existing.FirstOrDefault(f => f.FingerprintHash == cmd.FingerprintHash);
        if (match != null)
        {
            if (match.RawComponents == null && cmd.RawComponents != null)
            {
                match.RawComponents = cmd.RawComponents;
                await _uow.SaveChangesAsync();
            }
            return Unit.Value;
        }

        await _uow.UserFingerprints.AddAsync(new UserFingerprint
        {
            UserId = cmd.UserId,
            FingerprintHash = cmd.FingerprintHash,
            RawComponents = cmd.RawComponents
        });
        await _uow.SaveChangesAsync();
        return Unit.Value;
    }
}
