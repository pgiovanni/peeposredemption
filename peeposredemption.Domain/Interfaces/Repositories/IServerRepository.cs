using peeposredemption.Domain.Entities;


namespace peeposredemption.Domain.Interfaces.Repositories
{
    public interface IServerRepository
    {
        Task<Server?> GetByIdAsync(Guid id);
        Task<List<Server>> GetUserServersAsync(Guid userId);
        Task<bool> IsMemberAsync(Guid serverId, Guid userId);
        Task AddMemberAsync(ServerMember member);
        Task AddAsync(Server server);
        Task<ServerMember?> GetMemberAsync(Guid serverId, Guid userId);
        Task RemoveMemberAsync(Guid serverId, Guid userId);
        Task<ServerRole?> GetMemberRoleAsync(Guid serverId, Guid userId);
        Task<List<ServerMember>> GetServerMembersAsync(Guid serverId);
        Task ReorderServersAsync(Guid userId, List<Guid> serverIds);
    }

}
