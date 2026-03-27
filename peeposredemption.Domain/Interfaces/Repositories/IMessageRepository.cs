using peeposredemption.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Domain.Interfaces.Repositories
{
    public interface IMessageRepository
    {
        Task<List<Message>> GetChannelMessagesAsync(Guid channelId, int page, int pageSize);
        Task AddAsync(Message message);
        Task<Message?> GetByIdAsync(Guid messageId);
        Task<int[]> GetHourlyActivityAsync(Guid userId);
    }

}
