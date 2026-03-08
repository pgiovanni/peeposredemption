namespace peeposredemption.Domain.Entities
{
    public enum ModerationAction { Kick = 0, Ban = 1, DeleteMessage = 2, DeleteChannel = 3 }

    public class ModerationLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ServerId { get; set; }
        public Guid ModeratorId { get; set; }
        public ModerationAction Action { get; set; }
        public Guid TargetUserId { get; set; }
        public Guid? TargetMessageId { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Server Server { get; set; } = null!;
        public User Moderator { get; set; } = null!;
        public User TargetUser { get; set; } = null!;
    }
}
