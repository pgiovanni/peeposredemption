using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories
{
    public interface IServerInviteRepository
    {
        Task<ServerInvite?> GetByCodeAsync(string code);
        Task AddAsync(ServerInvite invite);
    }
}
