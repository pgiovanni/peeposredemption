namespace peeposredemption.Domain.Entities
{
    public enum NotificationType { Ping = 0 }

    public class Notification
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }        // recipient
        public Guid FromUserId { get; set; }    // who triggered it
        public NotificationType Type { get; set; } = NotificationType.Ping;
        public string Content { get; set; } = string.Empty;  // e.g. "pgiovanni mentioned you in #general"
        public Guid? ServerId { get; set; }
        public Guid? ChannelId { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
        public User FromUser { get; set; } = null!;
    }
}
