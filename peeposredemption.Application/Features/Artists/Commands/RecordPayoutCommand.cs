using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Artists.Commands;

public record RecordPayoutCommand(
    Guid ArtistId,
    long AmountCents,
    string? Reference,
    Guid AdminUserId) : IRequest<Guid>;

public class RecordPayoutCommandHandler : IRequestHandler<RecordPayoutCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    public RecordPayoutCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(RecordPayoutCommand cmd, CancellationToken ct)
    {
        var artist = await _uow.Artists.GetByIdAsync(cmd.ArtistId)
            ?? throw new InvalidOperationException("Artist not found.");

        long pending = artist.TotalEarnedCents - artist.TotalPaidCents;
        if (cmd.AmountCents > pending)
            throw new InvalidOperationException($"Payout amount ({cmd.AmountCents}c) exceeds pending balance ({pending}c).");

        if (cmd.AmountCents < 500)
            throw new InvalidOperationException("Minimum payout is $5.00 (500 cents).");

        var payout = new ArtistPayout
        {
            ArtistId = cmd.ArtistId,
            AmountCents = cmd.AmountCents,
            PayoutMethod = artist.PayoutMethod,
            Reference = cmd.Reference,
            CreatedBy = cmd.AdminUserId
        };

        await _uow.ArtistPayouts.AddAsync(payout);

        // Update denormalized total
        artist.TotalPaidCents += cmd.AmountCents;

        await _uow.SaveChangesAsync();

        return payout.Id;
    }
}
