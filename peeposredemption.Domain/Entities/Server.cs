namespace peeposredemption.Domain.Entities
{
    public class Server
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string? IconUrl { get; set; }
        public Guid OwnerId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public StorageTier StorageTier { get; set; } = StorageTier.Free;
        public string? WelcomeInviteCode { get; set; }
        public bool RequireMfaForModerators { get; set; } = false;
        public bool IsPrivate { get; set; } = false;
        public bool IsPersonal { get; set; } = false;

        public User Owner { get; set; } = null!;
        public ICollection<Channel> Channels { get; set; } = new List<Channel>();
        public ICollection<ServerMember> Members { get; set; } = new List<ServerMember>();
        public ICollection<ServerEmoji> Emojis { get; set; } = new List<ServerEmoji>();
    }

}