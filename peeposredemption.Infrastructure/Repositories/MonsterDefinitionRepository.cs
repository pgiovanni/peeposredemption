using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class MonsterDefinitionRepository : IMonsterDefinitionRepository
{
    private readonly AppDbContext _db;
    public MonsterDefinitionRepository(AppDbContext db) => _db = db;

    public Task<MonsterDefinition?> GetByIdAsync(Guid id) =>
        _db.MonsterDefinitions
            .Include(m => m.LootTable)
                .ThenInclude(l => l.ItemDefinition)
            .FirstOrDefaultAsync(m => m.Id == id);

    public Task<MonsterDefinition?> GetByNameAsync(string name) =>
        _db.MonsterDefinitions
            .Include(m => m.LootTable)
                .ThenInclude(l => l.ItemDefinition)
            .FirstOrDefaultAsync(m => m.Name.ToLower() == name.ToLower());

    public Task<List<MonsterDefinition>> GetByLevelRangeAsync(int minLevel, int maxLevel) =>
        _db.MonsterDefinitions
            .Where(m => m.Level >= minLevel && m.Level <= maxLevel)
            .ToListAsync();

    public async Task<MonsterDefinition?> GetRandomNearLevelAsync(int playerLevel)
    {
        var candidates = await _db.MonsterDefinitions
            .Include(m => m.LootTable)
                .ThenInclude(l => l.ItemDefinition)
            .Where(m => m.Level >= playerLevel - 2 && m.Level <= playerLevel + 2)
            .ToListAsync();

        if (candidates.Count == 0)
        {
            candidates = await _db.MonsterDefinitions
                .Include(m => m.LootTable)
                    .ThenInclude(l => l.ItemDefinition)
                .OrderBy(m => Math.Abs(m.Level - playerLevel))
                .Take(3)
                .ToListAsync();
        }

        if (candidates.Count == 0) return null;
        return candidates[Random.Shared.Next(candidates.Count)];
    }

    public async Task AddAsync(MonsterDefinition monster) =>
        await _db.MonsterDefinitions.AddAsync(monster);

    public Task<bool> AnyAsync() =>
        _db.MonsterDefinitions.AnyAsync();
}
