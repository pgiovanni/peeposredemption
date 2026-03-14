using peeposredemption.Domain.Interfaces;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _db;

        public IUserRepository Users { get; }
        public IServerRepository Servers { get; }
        public IMessageRepository Messages { get; }
        public IDirectMessageRepository DirectMessages { get; }
        public IChannelRepository Channels { get; }
        public IServerInviteRepository ServerInvites { get; }
        public IFriendRequestRepository FriendRequests { get; }
        public IBannedMemberRepository BannedMembers { get; }
        public IModerationLogRepository ModerationLogs { get; }
        public IServerEmojiRepository ServerEmojis { get; }
        public IStorageUpgradePurchaseRepository StorageUpgrades { get; }
        public INotificationRepository Notifications { get; }
        public IReferralRepository Referrals { get; }
        public IRefreshTokenRepository RefreshTokens { get; }
        public IOrbTransactionRepository OrbTransactions { get; }
        public IUserLoginStreakRepository UserLoginStreaks { get; }
        public IOrbPurchaseRepository OrbPurchases { get; }
        public IOrbGiftRepository OrbGifts { get; }

        public UnitOfWork(AppDbContext db,
            IUserRepository users, IServerRepository servers,
            IMessageRepository messages, IDirectMessageRepository directMessages,
            IChannelRepository channels, IServerInviteRepository serverInvites,
            IFriendRequestRepository friendRequests,
            IBannedMemberRepository bannedMembers,
            IModerationLogRepository moderationLogs,
            IServerEmojiRepository serverEmojis,
            IStorageUpgradePurchaseRepository storageUpgrades,
            INotificationRepository notifications,
            IReferralRepository referrals,
            IRefreshTokenRepository refreshTokens,
            IOrbTransactionRepository orbTransactions,
            IUserLoginStreakRepository userLoginStreaks,
            IOrbPurchaseRepository orbPurchases,
            IOrbGiftRepository orbGifts)
        {
            _db = db;
            Users = users;
            Servers = servers;
            Messages = messages;
            DirectMessages = directMessages;
            Channels = channels;
            ServerInvites = serverInvites;
            FriendRequests = friendRequests;
            BannedMembers = bannedMembers;
            ModerationLogs = moderationLogs;
            ServerEmojis = serverEmojis;
            StorageUpgrades = storageUpgrades;
            Notifications = notifications;
            Referrals = referrals;
            RefreshTokens = refreshTokens;
            OrbTransactions = orbTransactions;
            UserLoginStreaks = userLoginStreaks;
            OrbPurchases = orbPurchases;
            OrbGifts = orbGifts;
        }

        public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
    }

}
