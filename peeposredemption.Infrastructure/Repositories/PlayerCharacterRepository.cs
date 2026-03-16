using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class PlayerCharacterRepository : IPlayerCharacterRepository
{
    private readonly AppDbContext _db;
    public PlayerCharacterRepository(AppDbContext db) => _db = db;

    public Task<PlayerCharacter?> GetByUserIdAsync(Guid userId) =>
        _db.PlayerCharacters
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

    public Task<PlayerCharacter?> GetByIdAsync(Guid id) =>
        _db.PlayerCharacters.FirstOrDefaultAsync(p => p.Id == id);

    public Task<PlayerCharacter?> GetByIdWithInventoryAsync(Guid id) =>
        _db.PlayerCharacters
            .Include(p => p.Inventory)
                .ThenInclude(i => i.ItemDefinition)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task AddAsync(PlayerCharacter character) =>
        await _db.PlayerCharacters.AddAsync(character);

    public Task<List<PlayerCharacter>> GetTopByLevelAsync(int count) =>
        _db.PlayerCharacters
            .Include(p => p.User)
            .OrderByDescending(p => p.Level)
            .ThenByDescending(p => p.XP)
            .Take(count)
            .ToListAsync();

    public Task<List<PlayerCharacter>> GetTopByKillsAsync(int count) =>
        _db.PlayerCharacters
            .Include(p => p.User)
            .OrderByDescending(p => p.TotalMonstersKilled)
            .Take(count)
            .ToListAsync();
}
