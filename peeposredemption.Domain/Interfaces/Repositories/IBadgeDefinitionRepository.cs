using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IBadgeDefinitionRepository
{
    Task<List<BadgeDefinition>> GetAllAsync();
    Task<BadgeDefinition?> GetByIdAsync(Guid id);
    Task<List<BadgeDefinition>> GetByStatKeyAsync(string statKey);
    Task AddAsync(BadgeDefinition badge);
    Task<int> CountAsync();
}
