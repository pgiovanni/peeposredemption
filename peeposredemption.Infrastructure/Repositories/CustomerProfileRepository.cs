using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class CustomerProfileRepository : ICustomerProfileRepository
{
    private readonly AppDbContext _db;

    public CustomerProfileRepository(AppDbContext db) => _db = db;

    public Task<CustomerProfile?> GetByUserIdAsync(Guid userId) =>
        _db.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

    public async Task AddAsync(CustomerProfile profile) =>
        await _db.CustomerProfiles.AddAsync(profile);
}
