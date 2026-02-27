using peeposredemption.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Domain.Interfaces.Repositories
{
    public interface IDirectMessageRepository
    {
        Task<List<DirectMessage>> GetConversationAsync(
            Guid userA, Guid userB, int page, int pageSize);
        Task AddAsync(DirectMessage message);
    }

}
