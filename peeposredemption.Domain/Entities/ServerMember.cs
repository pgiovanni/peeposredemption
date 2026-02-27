namespace peeposredemption.Domain.Entities
{
    public class ServerMember
    {
        public Guid UserId { get; set; }
        public Guid ServerId { get; set; }
        public string Nickname { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public User User { get; set; }
        public Server Server { get; set; }
    }
}
