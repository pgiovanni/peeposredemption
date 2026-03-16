using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IPlayerInventoryItemRepository
{
    Task<List<PlayerInventoryItem>> GetByPlayerIdAsync(Guid playerId);
    Task<PlayerInventoryItem?> GetByPlayerAndItemAsync(Guid playerId, Guid itemDefinitionId);
    Task<PlayerInventoryItem?> GetEquippedInSlotAsync(Guid playerId, EquipSlot slot);
    Task<List<PlayerInventoryItem>> GetEquippedItemsAsync(Guid playerId);
    Task AddAsync(PlayerInventoryItem item);
    void Remove(PlayerInventoryItem item);
}
