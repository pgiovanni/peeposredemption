using peeposredemption.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Domain.Interfaces
{
    public interface IUnitOfWork
    {
        IUserRepository Users { get; }
        IServerRepository Servers { get; }
        IMessageRepository Messages { get; }
        IDirectMessageRepository DirectMessages { get; }
        IChannelRepository Channels { get; }
        IServerInviteRepository ServerInvites { get; }
        IFriendRequestRepository FriendRequests { get; }
        IBannedMemberRepository BannedMembers { get; }
        IModerationLogRepository ModerationLogs { get; }
        IServerEmojiRepository ServerEmojis { get; }
        IStorageUpgradePurchaseRepository StorageUpgrades { get; }
        Task<int> SaveChangesAsync();
    }

}
