using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IUserIpLogRepository
{
    Task<List<UserIpLog>> GetByUserIdAsync(Guid userId);
    Task<List<UserIpLog>> GetByIpAddressAsync(string ipAddress);
    Task AddAsync(UserIpLog log);
}
