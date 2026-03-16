using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class MonsterLootEntryRepository : IMonsterLootEntryRepository
{
    private readonly AppDbContext _db;
    public MonsterLootEntryRepository(AppDbContext db) => _db = db;

    public Task<List<MonsterLootEntry>> GetByMonsterIdAsync(Guid monsterDefinitionId) =>
        _db.MonsterLootEntries
            .Include(l => l.ItemDefinition)
            .Where(l => l.MonsterDefinitionId == monsterDefinitionId)
            .ToListAsync();

    public async Task AddAsync(MonsterLootEntry entry) =>
        await _db.MonsterLootEntries.AddAsync(entry);
}
