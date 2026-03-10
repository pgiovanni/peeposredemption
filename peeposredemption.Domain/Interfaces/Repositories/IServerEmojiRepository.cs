using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories
{
    public interface IServerEmojiRepository
    {
        Task<List<ServerEmoji>> GetByServerIdAsync(Guid serverId);
        Task<ServerEmoji?> GetByNameAsync(Guid serverId, string name);
        Task<ServerEmoji?> GetByIdAsync(Guid id);
        Task AddAsync(ServerEmoji emoji);
        Task<int> CountByServerIdAsync(Guid serverId);
        void Delete(ServerEmoji emoji);
    }
}
