namespace peeposredemption.Domain.Entities
{
    public class Server
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string? IconUrl { get; set; }
        public Guid OwnerId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User Owner { get; set; }
        public ICollection<Channel> Channels { get; set; }
        public ICollection<ServerMember> Members { get; set; }
    }

}