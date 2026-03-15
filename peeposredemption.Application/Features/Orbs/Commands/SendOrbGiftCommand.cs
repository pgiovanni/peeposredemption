using MediatR;
using peeposredemption.Application.Features.Badges.Commands;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Orbs.Commands;

public record OrbGiftResult(long SenderNewBalance, long RecipientNewBalance, Guid GiftId);

public record SendOrbGiftCommand(
    Guid SenderId,
    Guid RecipientId,
    long Amount,
    Guid? ChannelId,
    Guid? ServerId,
    string? Message) : IRequest<OrbGiftResult>;

public class SendOrbGiftCommandHandler : IRequestHandler<SendOrbGiftCommand, OrbGiftResult>
{
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;
    public SendOrbGiftCommandHandler(IUnitOfWork uow, IMediator mediator)
    {
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<OrbGiftResult> Handle(SendOrbGiftCommand cmd, CancellationToken ct)
    {
        if (cmd.Amount < 1)
            throw new InvalidOperationException("Gift must be at least 1 orb.");

        var sender = await _uow.Users.GetByIdAsync(cmd.SenderId)
            ?? throw new InvalidOperationException("Sender not found.");

        var recipient = await _uow.Users.GetByIdAsync(cmd.RecipientId)
            ?? throw new InvalidOperationException("Recipient not found.");

        if (sender.OrbBalance < cmd.Amount)
            throw new InvalidOperationException("Insufficient orb balance.");

        // Debit sender
        await _uow.OrbTransactions.AddAsync(new OrbTransaction
        {
            UserId = cmd.SenderId,
            Amount = -cmd.Amount,
            Type = OrbTransactionType.GiftSent,
            Description = $"Gift to {recipient.Username}",
            RelatedUserId = cmd.RecipientId
        });
        sender.OrbBalance -= cmd.Amount;

        // Credit recipient
        await _uow.OrbTransactions.AddAsync(new OrbTransaction
        {
            UserId = cmd.RecipientId,
            Amount = cmd.Amount,
            Type = OrbTransactionType.GiftReceived,
            Description = $"Gift from {sender.Username}",
            RelatedUserId = cmd.SenderId
        });
        recipient.OrbBalance += cmd.Amount;

        // Record gift
        var gift = new OrbGift
        {
            SenderId = cmd.SenderId,
            RecipientId = cmd.RecipientId,
            Amount = cmd.Amount,
            ChannelId = cmd.ChannelId,
            ServerId = cmd.ServerId,
            Message = cmd.Message
        };
        await _uow.OrbGifts.AddAsync(gift);

        await _uow.SaveChangesAsync();

        // Update gifting activity stats + check badges
        var stats = await _mediator.Send(new UpdateActivityStatsCommand(cmd.SenderId, IncrementOrbsGifted: cmd.Amount), ct);
        await _mediator.Send(new CheckAndAwardBadgesCommand(cmd.SenderId, "TotalOrbsGifted", stats.TotalOrbsGifted), ct);

        // Check recipient's peak balance badge
        await _mediator.Send(new UpdateActivityStatsCommand(cmd.RecipientId, NewOrbBalance: recipient.OrbBalance), ct);

        return new OrbGiftResult(sender.OrbBalance, recipient.OrbBalance, gift.Id);
    }
}
