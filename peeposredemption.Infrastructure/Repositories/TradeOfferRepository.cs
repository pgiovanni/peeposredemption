using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class TradeOfferRepository : ITradeOfferRepository
{
    private readonly AppDbContext _db;
    public TradeOfferRepository(AppDbContext db) => _db = db;

    public Task<TradeOffer?> GetByIdAsync(Guid id) =>
        _db.TradeOffers
            .Include(t => t.Initiator).ThenInclude(p => p.User)
            .Include(t => t.Recipient).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(t => t.Id == id);

    public Task<TradeOffer?> GetPendingByRecipientIdAsync(Guid recipientId) =>
        _db.TradeOffers
            .Include(t => t.Initiator).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(t => t.RecipientId == recipientId
                && t.Status == TradeStatus.Pending
                && t.ExpiresAt > DateTime.UtcNow);

    public async Task AddAsync(TradeOffer offer) =>
        await _db.TradeOffers.AddAsync(offer);
}
