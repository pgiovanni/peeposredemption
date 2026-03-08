namespace peeposredemption.Domain.Entities
{
    public enum ServerRole { Member = 0, Moderator = 1, Owner = 2 }

    public class ServerMember
    {
        public Guid UserId { get; set; }
        public Guid ServerId { get; set; }
        public string? Nickname { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public ServerRole Role { get; set; } = ServerRole.Member;
        public User User { get; set; }
        public Server Server { get; set; }
    }
}
