using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class AltSuspicionRepository : IAltSuspicionRepository
{
    private readonly AppDbContext _db;
    public AltSuspicionRepository(AppDbContext db) => _db = db;

    public async Task<List<AltSuspicion>> GetPendingAsync() =>
        await _db.AltSuspicions
            .Include(s => s.User1)
            .Include(s => s.User2)
            .Where(s => s.IsConfirmed == null)
            .OrderByDescending(s => s.Score)
            .ToListAsync();

    public async Task<AltSuspicion?> GetByIdAsync(Guid id) =>
        await _db.AltSuspicions
            .Include(s => s.User1)
            .Include(s => s.User2)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<AltSuspicion?> GetByUserPairAsync(Guid userId1, Guid userId2)
    {
        // Normalize pair order so (A,B) == (B,A)
        var (low, high) = userId1.CompareTo(userId2) <= 0 ? (userId1, userId2) : (userId2, userId1);
        return await _db.AltSuspicions
            .FirstOrDefaultAsync(s => s.UserId1 == low && s.UserId2 == high);
    }

    public async Task AddAsync(AltSuspicion suspicion)
    {
        // Normalize pair order
        if (suspicion.UserId1.CompareTo(suspicion.UserId2) > 0)
            (suspicion.UserId1, suspicion.UserId2) = (suspicion.UserId2, suspicion.UserId1);

        await _db.AltSuspicions.AddAsync(suspicion);
    }
}
