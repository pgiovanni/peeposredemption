using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories
{
    public interface IBannedMemberRepository
    {
        Task<bool> IsBannedAsync(Guid serverId, Guid userId);
        Task AddAsync(BannedMember ban);
    }
}
