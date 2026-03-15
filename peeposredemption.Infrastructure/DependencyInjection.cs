using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Interfaces;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;
using peeposredemption.Infrastructure.Repositories;
using peeposredemption.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IServerRepository, ServerRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<IDirectMessageRepository, DirectMessageRepository>();
            services.AddScoped<IChannelRepository, ChannelRepository>();
            services.AddScoped<IServerInviteRepository, ServerInviteRepository>();
            services.AddScoped<IFriendRequestRepository, FriendRequestRepository>();
            services.AddScoped<IBannedMemberRepository, BannedMemberRepository>();
            services.AddScoped<IModerationLogRepository, ModerationLogRepository>();
            services.AddScoped<IServerEmojiRepository, ServerEmojiRepository>();
            services.AddScoped<IStorageUpgradePurchaseRepository, StorageUpgradePurchaseRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IReferralRepository, ReferralRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IOrbTransactionRepository, OrbTransactionRepository>();
            services.AddScoped<IUserLoginStreakRepository, UserLoginStreakRepository>();
            services.AddScoped<IOrbPurchaseRepository, OrbPurchaseRepository>();
            services.AddScoped<IOrbGiftRepository, OrbGiftRepository>();
            services.AddScoped<IParentalLinkRepository, ParentalLinkRepository>();
            services.AddScoped<IBadgeDefinitionRepository, BadgeDefinitionRepository>();
            services.AddScoped<IUserBadgeRepository, UserBadgeRepository>();
            services.AddScoped<IUserActivityStatsRepository, UserActivityStatsRepository>();
            services.AddScoped<IArtistRepository, ArtistRepository>();
            services.AddScoped<IArtItemRepository, ArtItemRepository>();
            services.AddScoped<IArtistCommissionRepository, ArtistCommissionRepository>();
            services.AddScoped<IArtistPayoutRepository, ArtistPayoutRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<IR2StorageService, R2StorageService>();
            services.AddScoped<IStripeService, StripeService>();
            services.AddScoped<IStripeWebhookService, StripeWebhookService>();

            return services;
        }
    }
}
