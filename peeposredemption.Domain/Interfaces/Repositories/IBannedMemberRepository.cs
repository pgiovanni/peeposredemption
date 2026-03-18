using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories
{
    public interface IBannedMemberRepository
    {
        Task<bool> IsBannedAsync(Guid serverId, Guid userId);
        Task AddAsync(BannedMember ban);
        Task<BannedMember?> GetAsync(Guid serverId, Guid userId);
        Task<List<BannedMember>> GetByServerAsync(Guid serverId);
        Task<List<BannedMember>> GetAllAsync();
        void Remove(BannedMember ban);
    }
}
