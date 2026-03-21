using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class FeatureRequestRepository : IFeatureRequestRepository
{
    private readonly AppDbContext _db;
    public FeatureRequestRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(FeatureRequest featureRequest) =>
        await _db.FeatureRequests.AddAsync(featureRequest);

    public Task<List<FeatureRequest>> GetByUserIdAsync(Guid userId) =>
        _db.FeatureRequests
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

    public Task<List<FeatureRequest>> GetAllAsync() =>
        _db.FeatureRequests
            .Include(f => f.User)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

    public Task<FeatureRequest?> GetByIdAsync(Guid id) =>
        _db.FeatureRequests.FirstOrDefaultAsync(f => f.Id == id);
}
