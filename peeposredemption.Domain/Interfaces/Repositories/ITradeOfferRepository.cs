using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface ITradeOfferRepository
{
    Task<TradeOffer?> GetByIdAsync(Guid id);
    Task<TradeOffer?> GetPendingByRecipientIdAsync(Guid recipientId);
    Task AddAsync(TradeOffer offer);
}
