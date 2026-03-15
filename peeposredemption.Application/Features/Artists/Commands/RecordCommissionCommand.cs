using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Artists.Commands;

public record RecordCommissionCommand(
    Guid ArtistId,
    Guid ArtItemId,
    Guid UserId,
    long OrbAmount,
    CommissionSource Source) : IRequest<Guid>;

public class RecordCommissionCommandHandler : IRequestHandler<RecordCommissionCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    public RecordCommissionCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(RecordCommissionCommand cmd, CancellationToken ct)
    {
        var artist = await _uow.Artists.GetByIdAsync(cmd.ArtistId)
            ?? throw new InvalidOperationException("Artist not found.");

        var artItem = await _uow.ArtItems.GetByIdAsync(cmd.ArtItemId)
            ?? throw new InvalidOperationException("Art item not found.");

        long commissionOrbs = cmd.OrbAmount / 2; // 50% split
        long commissionCents = commissionOrbs;    // 1 orb = 1 cent

        var commission = new ArtistCommission
        {
            ArtistId = cmd.ArtistId,
            ArtItemId = cmd.ArtItemId,
            UserId = cmd.UserId,
            OrbAmount = cmd.OrbAmount,
            CommissionOrbs = commissionOrbs,
            CommissionCents = commissionCents,
            Source = cmd.Source
        };

        await _uow.ArtistCommissions.AddAsync(commission);

        // Update denormalized total
        artist.TotalEarnedCents += commissionCents;

        await _uow.SaveChangesAsync();

        return commission.Id;
    }
}
