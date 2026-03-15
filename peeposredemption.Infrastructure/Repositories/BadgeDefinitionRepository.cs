using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class BadgeDefinitionRepository : IBadgeDefinitionRepository
{
    private readonly AppDbContext _db;
    public BadgeDefinitionRepository(AppDbContext db) => _db = db;

    public Task<List<BadgeDefinition>> GetAllAsync() =>
        _db.BadgeDefinitions.OrderBy(b => b.SortOrder).ToListAsync();

    public Task<BadgeDefinition?> GetByIdAsync(Guid id) =>
        _db.BadgeDefinitions.FirstOrDefaultAsync(b => b.Id == id);

    public Task<List<BadgeDefinition>> GetByStatKeyAsync(string statKey) =>
        _db.BadgeDefinitions.Where(b => b.StatKey == statKey).OrderBy(b => b.Threshold).ToListAsync();

    public async Task AddAsync(BadgeDefinition badge) =>
        await _db.BadgeDefinitions.AddAsync(badge);

    public Task<int> CountAsync() =>
        _db.BadgeDefinitions.CountAsync();
}
