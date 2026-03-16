using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class ItemDefinitionRepository : IItemDefinitionRepository
{
    private readonly AppDbContext _db;
    public ItemDefinitionRepository(AppDbContext db) => _db = db;

    public Task<ItemDefinition?> GetByIdAsync(Guid id) =>
        _db.ItemDefinitions.FirstOrDefaultAsync(i => i.Id == id);

    public Task<ItemDefinition?> GetByNameAsync(string name) =>
        _db.ItemDefinitions.FirstOrDefaultAsync(i => i.Name.ToLower() == name.ToLower());

    public Task<List<ItemDefinition>> GetAllAsync() =>
        _db.ItemDefinitions.ToListAsync();

    public async Task AddAsync(ItemDefinition item) =>
        await _db.ItemDefinitions.AddAsync(item);

    public Task<bool> AnyAsync() =>
        _db.ItemDefinitions.AnyAsync();
}
