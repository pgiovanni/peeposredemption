using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IMonsterDefinitionRepository
{
    Task<MonsterDefinition?> GetByIdAsync(Guid id);
    Task<MonsterDefinition?> GetByNameAsync(string name);
    Task<List<MonsterDefinition>> GetByLevelRangeAsync(int minLevel, int maxLevel);
    Task<MonsterDefinition?> GetRandomNearLevelAsync(int playerLevel);
    Task AddAsync(MonsterDefinition monster);
    Task<bool> AnyAsync();
}
