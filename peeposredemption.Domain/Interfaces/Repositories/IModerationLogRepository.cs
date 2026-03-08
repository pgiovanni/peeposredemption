using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories
{
    public interface IModerationLogRepository
    {
        Task AddAsync(ModerationLog entry);
        Task<List<ModerationLog>> GetServerLogsAsync(Guid serverId, int limit = 100);
    }
}
