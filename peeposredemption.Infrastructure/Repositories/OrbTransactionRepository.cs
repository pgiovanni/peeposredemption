using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class OrbTransactionRepository : IOrbTransactionRepository
{
    private readonly AppDbContext _db;
    public OrbTransactionRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(OrbTransaction transaction) =>
        await _db.OrbTransactions.AddAsync(transaction);

    public Task<long> GetBalanceAsync(Guid userId) =>
        _db.OrbTransactions.Where(t => t.UserId == userId).SumAsync(t => t.Amount);

    public Task<List<OrbTransaction>> GetRecentAsync(Guid userId, int count) =>
        _db.OrbTransactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(count)
            .ToListAsync();
}
