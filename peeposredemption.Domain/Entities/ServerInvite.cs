namespace peeposredemption.Domain.Entities
{
    public class ServerInvite
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ServerId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string Code { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Server Server { get; set; }
        public User CreatedBy { get; set; }
    }
}
