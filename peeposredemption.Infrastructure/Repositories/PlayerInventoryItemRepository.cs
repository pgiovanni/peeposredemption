using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class PlayerInventoryItemRepository : IPlayerInventoryItemRepository
{
    private readonly AppDbContext _db;
    public PlayerInventoryItemRepository(AppDbContext db) => _db = db;

    public Task<List<PlayerInventoryItem>> GetByPlayerIdAsync(Guid playerId) =>
        _db.PlayerInventoryItems
            .Include(i => i.ItemDefinition)
            .Where(i => i.PlayerId == playerId)
            .ToListAsync();

    public Task<PlayerInventoryItem?> GetByPlayerAndItemAsync(Guid playerId, Guid itemDefinitionId) =>
        _db.PlayerInventoryItems
            .Include(i => i.ItemDefinition)
            .FirstOrDefaultAsync(i => i.PlayerId == playerId && i.ItemDefinitionId == itemDefinitionId && !i.IsEquipped);

    public Task<PlayerInventoryItem?> GetEquippedInSlotAsync(Guid playerId, EquipSlot slot) =>
        _db.PlayerInventoryItems
            .Include(i => i.ItemDefinition)
            .FirstOrDefaultAsync(i => i.PlayerId == playerId && i.IsEquipped && i.EquippedSlot == slot);

    public Task<List<PlayerInventoryItem>> GetEquippedItemsAsync(Guid playerId) =>
        _db.PlayerInventoryItems
            .Include(i => i.ItemDefinition)
            .Where(i => i.PlayerId == playerId && i.IsEquipped)
            .ToListAsync();

    public async Task AddAsync(PlayerInventoryItem item) =>
        await _db.PlayerInventoryItems.AddAsync(item);

    public void Remove(PlayerInventoryItem item) =>
        _db.PlayerInventoryItems.Remove(item);
}
