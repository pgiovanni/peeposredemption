using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class LeadRepository : ILeadRepository
{
    private readonly AppDbContext _db;

    public LeadRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Lead lead) =>
        await _db.Leads.AddAsync(lead);

    public Task<List<Lead>> GetAllAsync() =>
        _db.Leads
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

    public Task<List<Lead>> GetByEmailAsync(string email) =>
        _db.Leads
            .Where(l => l.Email.ToLower() == email.ToLower())
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

    public Task<Lead?> GetByIdAsync(Guid id) =>
        _db.Leads.FirstOrDefaultAsync(l => l.Id == id);
}
