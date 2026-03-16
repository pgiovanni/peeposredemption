using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface ICombatSessionRepository
{
    Task<CombatSession?> GetActiveByPlayerIdAsync(Guid playerId);
    Task<CombatSession?> GetByIdAsync(Guid id);
    Task AddAsync(CombatSession session);
}
