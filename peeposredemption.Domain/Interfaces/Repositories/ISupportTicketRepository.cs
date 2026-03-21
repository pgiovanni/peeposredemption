using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface ISupportTicketRepository
{
    Task AddAsync(SupportTicket ticket);
    Task<List<SupportTicket>> GetByUserIdAsync(Guid userId);
    Task<List<SupportTicket>> GetAllAsync();
    Task<SupportTicket?> GetByIdAsync(Guid id);
}
