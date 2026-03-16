using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IPlayerCharacterRepository
{
    Task<PlayerCharacter?> GetByUserIdAsync(Guid userId);
    Task<PlayerCharacter?> GetByIdAsync(Guid id);
    Task<PlayerCharacter?> GetByIdWithInventoryAsync(Guid id);
    Task AddAsync(PlayerCharacter character);
    Task<List<PlayerCharacter>> GetTopByLevelAsync(int count);
    Task<List<PlayerCharacter>> GetTopByKillsAsync(int count);
}
