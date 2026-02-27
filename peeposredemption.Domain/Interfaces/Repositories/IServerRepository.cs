using peeposredemption.Domain.Entities;


namespace peeposredemption.Domain.Interfaces.Repositories
{
    public interface IServerRepository
    {
        Task<Server?> GetByIdAsync(Guid id);
        Task<List<Server>> GetUserServersAsync(Guid userId);
        Task<bool> IsMemberAsync(Guid serverId, Guid userId);
        Task AddAsync(Server server);
    }

}
