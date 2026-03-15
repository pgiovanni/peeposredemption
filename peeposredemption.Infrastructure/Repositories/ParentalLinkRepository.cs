using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class ParentalLinkRepository : IParentalLinkRepository
{
    private readonly AppDbContext _db;
    public ParentalLinkRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(ParentalLink link) => await _db.ParentalLinks.AddAsync(link);

    public Task<ParentalLink?> GetByIdAsync(Guid id) =>
        _db.ParentalLinks.Include(l => l.Child).Include(l => l.Parent).FirstOrDefaultAsync(l => l.Id == id);

    public Task<ParentalLink?> GetByCodeAsync(string code) =>
        _db.ParentalLinks.Include(l => l.Child).Include(l => l.Parent).FirstOrDefaultAsync(l => l.LinkCode == code);

    public Task<ParentalLink?> GetActiveByChildIdAsync(Guid childUserId) =>
        _db.ParentalLinks.Include(l => l.Parent)
            .FirstOrDefaultAsync(l => l.ChildUserId == childUserId && l.Status == ParentalLinkStatus.Active);

    public Task<List<ParentalLink>> GetActiveByParentIdAsync(Guid parentUserId) =>
        _db.ParentalLinks.Include(l => l.Child).ThenInclude(c => c.ServerMemberships)
            .Include(l => l.Child)
            .Where(l => l.ParentUserId == parentUserId && l.Status == ParentalLinkStatus.Active)
            .ToListAsync();

    public Task<ParentalLink?> GetPendingByChildIdAsync(Guid childUserId) =>
        _db.ParentalLinks.FirstOrDefaultAsync(l => l.ChildUserId == childUserId
            && l.Status == ParentalLinkStatus.Pending
            && l.CreatedAt > DateTime.UtcNow.AddHours(-24));
}
