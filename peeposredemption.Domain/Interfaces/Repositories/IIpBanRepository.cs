using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IIpBanRepository
{
    Task<bool> IsBannedAsync(string ipAddress);
    Task<List<IpBan>> GetAllAsync();
    Task AddAsync(IpBan ban);
    Task RemoveAsync(Guid id);
}
