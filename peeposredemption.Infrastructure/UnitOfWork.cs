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
        public IParentalLinkRepository ParentalLinks { get; }
        public IBadgeDefinitionRepository BadgeDefinitions { get; }
        public IUserBadgeRepository UserBadges { get; }
        public IUserActivityStatsRepository UserActivityStats { get; }
        public IArtistRepository Artists { get; }
        public IArtItemRepository ArtItems { get; }
        public IArtistCommissionRepository ArtistCommissions { get; }
        public IArtistPayoutRepository ArtistPayouts { get; }
        public IArtistSubmissionRepository ArtistSubmissions { get; }

        // Game system
        public IPlayerCharacterRepository PlayerCharacters { get; }
        public IItemDefinitionRepository ItemDefinitions { get; }
        public IPlayerInventoryItemRepository PlayerInventoryItems { get; }
        public IMonsterDefinitionRepository MonsterDefinitions { get; }
        public IMonsterLootEntryRepository MonsterLootEntries { get; }
        public ICombatSessionRepository CombatSessions { get; }
        public IPlayerSkillRepository PlayerSkills { get; }
        public IGameChannelConfigRepository GameChannelConfigs { get; }
        public ICraftingRecipeRepository CraftingRecipes { get; }
        public IMarketplaceListingRepository MarketplaceListings { get; }
        public ITradeOfferRepository TradeOffers { get; }

        // Voice sessions
        public IVoiceSessionRepository VoiceSessions { get; }

        // Feature requests
        public IFeatureRequestRepository FeatureRequests { get; }

        // Support tickets
        public ISupportTicketRepository SupportTickets { get; }

        // Anti-alt security
        public IIpBanRepository IpBans { get; }
        public IUserDeviceRepository UserDevices { get; }
        public IUserIpLogRepository UserIpLogs { get; }
        public IUserFingerprintRepository UserFingerprints { get; }
        public IBannedFingerprintRepository BannedFingerprints { get; }
        public IAltSuspicionRepository AltSuspicions { get; }

        // Torvex Gold
        public ITorvexGoldSubscriptionRepository GoldSubscriptions { get; }

        // Message attachments
        public IMessageAttachmentRepository MessageAttachments { get; }

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
            IOrbGiftRepository orbGifts,
            IParentalLinkRepository parentalLinks,
            IBadgeDefinitionRepository badgeDefinitions,
            IUserBadgeRepository userBadges,
            IUserActivityStatsRepository userActivityStats,
            IArtistRepository artists,
            IArtItemRepository artItems,
            IArtistCommissionRepository artistCommissions,
            IArtistPayoutRepository artistPayouts,
            IArtistSubmissionRepository artistSubmissions,
            IPlayerCharacterRepository playerCharacters,
            IItemDefinitionRepository itemDefinitions,
            IPlayerInventoryItemRepository playerInventoryItems,
            IMonsterDefinitionRepository monsterDefinitions,
            IMonsterLootEntryRepository monsterLootEntries,
            ICombatSessionRepository combatSessions,
            IPlayerSkillRepository playerSkills,
            IGameChannelConfigRepository gameChannelConfigs,
            ICraftingRecipeRepository craftingRecipes,
            IMarketplaceListingRepository marketplaceListings,
            ITradeOfferRepository tradeOffers,
            IVoiceSessionRepository voiceSessions,
            IFeatureRequestRepository featureRequests,
            ISupportTicketRepository supportTickets,
            IIpBanRepository ipBans,
            IUserDeviceRepository userDevices,
            IUserIpLogRepository userIpLogs,
            IUserFingerprintRepository userFingerprints,
            IBannedFingerprintRepository bannedFingerprints,
            IAltSuspicionRepository altSuspicions,
            ITorvexGoldSubscriptionRepository goldSubscriptions,
            IMessageAttachmentRepository messageAttachments)
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
            ParentalLinks = parentalLinks;
            BadgeDefinitions = badgeDefinitions;
            UserBadges = userBadges;
            UserActivityStats = userActivityStats;
            Artists = artists;
            ArtItems = artItems;
            ArtistCommissions = artistCommissions;
            ArtistPayouts = artistPayouts;
            ArtistSubmissions = artistSubmissions;
            PlayerCharacters = playerCharacters;
            ItemDefinitions = itemDefinitions;
            PlayerInventoryItems = playerInventoryItems;
            MonsterDefinitions = monsterDefinitions;
            MonsterLootEntries = monsterLootEntries;
            CombatSessions = combatSessions;
            PlayerSkills = playerSkills;
            GameChannelConfigs = gameChannelConfigs;
            CraftingRecipes = craftingRecipes;
            MarketplaceListings = marketplaceListings;
            TradeOffers = tradeOffers;
            VoiceSessions = voiceSessions;
            FeatureRequests = featureRequests;
            SupportTickets = supportTickets;
            IpBans = ipBans;
            UserDevices = userDevices;
            UserIpLogs = userIpLogs;
            UserFingerprints = userFingerprints;
            BannedFingerprints = bannedFingerprints;
            AltSuspicions = altSuspicions;
            GoldSubscriptions = goldSubscriptions;
            MessageAttachments = messageAttachments;
        }

        public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
    }

}
