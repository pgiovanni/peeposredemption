using peeposredemption.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Domain.Interfaces.Repositories
{
    public interface IChannelRepository
    {
        Task<Channel?> GetByIdAsync(Guid id);
        Task<List<Channel>> GetServerChannelsAsync(Guid serverId);
        Task AddAsync(Channel channel);
        Task RemoveAsync(Channel channel);
    }
}
