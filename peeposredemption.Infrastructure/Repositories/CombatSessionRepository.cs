using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class CombatSessionRepository : ICombatSessionRepository
{
    private readonly AppDbContext _db;
    public CombatSessionRepository(AppDbContext db) => _db = db;

    public Task<CombatSession?> GetActiveByPlayerIdAsync(Guid playerId) =>
        _db.CombatSessions
            .Include(c => c.MonsterDefinition)
            .Include(c => c.Player)
            .FirstOrDefaultAsync(c => c.PlayerId == playerId
                && (c.State == CombatState.AwaitingAction || c.State == CombatState.InProgress));

    public Task<CombatSession?> GetByIdAsync(Guid id) =>
        _db.CombatSessions
            .Include(c => c.MonsterDefinition)
            .Include(c => c.Player)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task AddAsync(CombatSession session) =>
        await _db.CombatSessions.AddAsync(session);
}
