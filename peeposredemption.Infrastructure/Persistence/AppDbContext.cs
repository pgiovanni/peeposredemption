using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<Server> Servers { get; set; }
        public DbSet<ServerMember> ServerMembers { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<DirectMessage> DirectMessages { get; set; }
        public DbSet<ServerInvite> ServerInvites { get; set; }
        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<BannedMember> BannedMembers { get; set; }
        public DbSet<ModerationLog> ModerationLogs { get; set; }
        public DbSet<ServerEmoji> ServerEmojis { get; set; }
        public DbSet<StorageUpgradePurchase> StorageUpgradePurchases { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ReferralCode> ReferralCodes { get; set; }
        public DbSet<ReferralPurchase> ReferralPurchases { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<OrbTransaction> OrbTransactions { get; set; }
        public DbSet<UserLoginStreak> UserLoginStreaks { get; set; }
        public DbSet<OrbPurchase> OrbPurchases { get; set; }
        public DbSet<OrbGift> OrbGifts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Force snake_case so Windows dev and Linux prod behave identically
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.SetTableName(ToSnakeCase(entity.GetTableName()!));
                foreach (var prop in entity.GetProperties())
                    prop.SetColumnName(ToSnakeCase(prop.GetColumnName()!));
                foreach (var key in entity.GetKeys())
                    key.SetName(ToSnakeCase(key.GetName()!));
                foreach (var fk in entity.GetForeignKeys())
                    fk.SetConstraintName(ToSnakeCase(fk.GetConstraintName()!));
            }

            // Unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Composite key for join table
            modelBuilder.Entity<ServerMember>()
                .HasKey(sm => new { sm.UserId, sm.ServerId });

            // Map ServerInvite.CreatedByUserId as the FK for the CreatedBy nav property
            modelBuilder.Entity<ServerInvite>()
                .HasOne(si => si.CreatedBy)
                .WithMany()
                .HasForeignKey(si => si.CreatedByUserId);

            // Prevent cascade delete conflict on DirectMessages
            modelBuilder.Entity<DirectMessage>()
                .HasOne(dm => dm.Sender)
                .WithMany(u => u.SentDirectMessages)
                .HasForeignKey(dm => dm.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DirectMessage>()
                .HasOne(dm => dm.Recipient)
                .WithMany(u => u.ReceivedDirectMessages)
                .HasForeignKey(dm => dm.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FriendRequest>()
                .HasOne(fr => fr.Sender)
                .WithMany()
                .HasForeignKey(fr => fr.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FriendRequest>()
                .HasOne(fr => fr.Receiver)
                .WithMany()
                .HasForeignKey(fr => fr.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent cascade delete conflict on BannedMember (multiple FKs to Users)
            modelBuilder.Entity<BannedMember>()
                .HasOne(b => b.BannedBy)
                .WithMany()
                .HasForeignKey(b => b.BannedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent cascade delete conflict on ModerationLog (multiple FKs to Users)
            modelBuilder.Entity<ModerationLog>()
                .HasOne(ml => ml.Moderator)
                .WithMany()
                .HasForeignKey(ml => ml.ModeratorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ModerationLog>()
                .HasOne(ml => ml.TargetUser)
                .WithMany()
                .HasForeignKey(ml => ml.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ServerEmoji: unique (ServerId, Name)
            modelBuilder.Entity<ServerEmoji>()
                .HasIndex(e => new { e.ServerId, e.Name })
                .IsUnique();

            modelBuilder.Entity<ServerEmoji>()
                .HasOne(e => e.UploadedBy)
                .WithMany()
                .HasForeignKey(e => e.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StorageUpgradePurchase>()
                .HasOne(p => p.Server)
                .WithMany()
                .HasForeignKey(p => p.ServerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.FromUser)
                .WithMany()
                .HasForeignKey(n => n.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReferralCode>()
                .HasOne(r => r.Owner)
                .WithMany()
                .HasForeignKey(r => r.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReferralPurchase>()
                .HasOne(p => p.Purchaser)
                .WithMany()
                .HasForeignKey(p => p.PurchaserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReferralPurchase>()
                .HasOne(p => p.ReferralCode)
                .WithMany(r => r.Purchases)
                .HasForeignKey(p => p.ReferralCodeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RefreshToken>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(r => r.Token)
                .IsUnique();

            // OrbTransaction
            modelBuilder.Entity<OrbTransaction>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrbTransaction>()
                .HasIndex(t => new { t.UserId, t.CreatedAt });

            // UserLoginStreak — one per user
            modelBuilder.Entity<UserLoginStreak>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserLoginStreak>()
                .HasIndex(s => s.UserId)
                .IsUnique();

            // OrbGift — multiple FKs to Users
            modelBuilder.Entity<OrbGift>()
                .HasOne(g => g.Sender)
                .WithMany()
                .HasForeignKey(g => g.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrbGift>()
                .HasOne(g => g.Recipient)
                .WithMany()
                .HasForeignKey(g => g.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

            // OrbPurchase
            modelBuilder.Entity<OrbPurchase>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        // Converts PascalCase to snake_case
        private static string ToSnakeCase(string name) =>
            string.Concat(name.Select((c, i) =>
                i > 0 && char.IsUpper(c)
                    ? "_" + char.ToLower(c)
                    : char.ToLower(c).ToString()));
    }

}
