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
        }

        // Converts PascalCase to snake_case
        private static string ToSnakeCase(string name) =>
            string.Concat(name.Select((c, i) =>
                i > 0 && char.IsUpper(c)
                    ? "_" + char.ToLower(c)
                    : char.ToLower(c).ToString()));
    }

}
