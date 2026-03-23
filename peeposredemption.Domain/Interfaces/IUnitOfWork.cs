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
        INotificationRepository Notifications { get; }
        IReferralRepository Referrals { get; }
        IRefreshTokenRepository RefreshTokens { get; }
        IOrbTransactionRepository OrbTransactions { get; }
        IUserLoginStreakRepository UserLoginStreaks { get; }
        IOrbPurchaseRepository OrbPurchases { get; }
        IOrbGiftRepository OrbGifts { get; }
        IParentalLinkRepository ParentalLinks { get; }
        IBadgeDefinitionRepository BadgeDefinitions { get; }
        IUserBadgeRepository UserBadges { get; }
        IUserActivityStatsRepository UserActivityStats { get; }
        IArtistRepository Artists { get; }
        IArtItemRepository ArtItems { get; }
        IArtistCommissionRepository ArtistCommissions { get; }
        IArtistPayoutRepository ArtistPayouts { get; }
        IArtistSubmissionRepository ArtistSubmissions { get; }

        // Game system
        IPlayerCharacterRepository PlayerCharacters { get; }
        IItemDefinitionRepository ItemDefinitions { get; }
        IPlayerInventoryItemRepository PlayerInventoryItems { get; }
        IMonsterDefinitionRepository MonsterDefinitions { get; }
        IMonsterLootEntryRepository MonsterLootEntries { get; }
        ICombatSessionRepository CombatSessions { get; }
        IPlayerSkillRepository PlayerSkills { get; }
        IGameChannelConfigRepository GameChannelConfigs { get; }
        ICraftingRecipeRepository CraftingRecipes { get; }
        IMarketplaceListingRepository MarketplaceListings { get; }
        ITradeOfferRepository TradeOffers { get; }

        // Voice sessions
        IVoiceSessionRepository VoiceSessions { get; }

        // Feature requests
        IFeatureRequestRepository FeatureRequests { get; }

        // Support tickets
        ISupportTicketRepository SupportTickets { get; }

        // Anti-alt security
        IIpBanRepository IpBans { get; }
        IUserDeviceRepository UserDevices { get; }
        IUserIpLogRepository UserIpLogs { get; }
        IUserFingerprintRepository UserFingerprints { get; }
        IBannedFingerprintRepository BannedFingerprints { get; }

        Task<int> SaveChangesAsync();
    }

}
