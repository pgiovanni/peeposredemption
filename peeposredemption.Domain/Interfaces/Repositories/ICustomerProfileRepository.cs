using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface ICustomerProfileRepository
{
    Task<CustomerProfile?> GetByUserIdAsync(Guid userId);
    Task AddAsync(CustomerProfile profile);
}
