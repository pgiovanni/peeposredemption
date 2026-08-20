using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface ILeadRepository
{
    Task AddAsync(Lead lead);
    Task<List<Lead>> GetAllAsync();
    Task<List<Lead>> GetByEmailAsync(string email);
    Task<Lead?> GetByIdAsync(Guid id);
}
