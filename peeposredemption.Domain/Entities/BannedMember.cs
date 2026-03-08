namespace peeposredemption.Domain.Entities
{
    public class BannedMember
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ServerId { get; set; }
        public Guid UserId { get; set; }
        public Guid BannedByUserId { get; set; }
        public DateTime BannedAt { get; set; } = DateTime.UtcNow;
        public Server Server { get; set; } = null!;
        public User User { get; set; } = null!;
        public User BannedBy { get; set; } = null!;
    }
}
