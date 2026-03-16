using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IMonsterLootEntryRepository
{
    Task<List<MonsterLootEntry>> GetByMonsterIdAsync(Guid monsterDefinitionId);
    Task AddAsync(MonsterLootEntry entry);
}
