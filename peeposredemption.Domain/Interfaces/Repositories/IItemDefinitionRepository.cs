using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IItemDefinitionRepository
{
    Task<ItemDefinition?> GetByIdAsync(Guid id);
    Task<ItemDefinition?> GetByNameAsync(string name);
    Task<List<ItemDefinition>> GetAllAsync();
    Task AddAsync(ItemDefinition item);
    Task<bool> AnyAsync();
}
