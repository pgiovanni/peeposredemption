using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class SupportTicketRepository : ISupportTicketRepository
{
    private readonly AppDbContext _db;
    public SupportTicketRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(SupportTicket ticket) =>
        await _db.SupportTickets.AddAsync(ticket);

    public Task<List<SupportTicket>> GetByUserIdAsync(Guid userId) =>
        _db.SupportTickets
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public Task<List<SupportTicket>> GetAllAsync() =>
        _db.SupportTickets
            .Include(t => t.User)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public Task<SupportTicket?> GetByIdAsync(Guid id) =>
        _db.SupportTickets.FirstOrDefaultAsync(t => t.Id == id);
}
