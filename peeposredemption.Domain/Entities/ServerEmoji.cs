namespace peeposredemption.Domain.Entities
{
    public class ServerEmoji
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ServerId { get; set; }
        public Server Server { get; set; } = null!;
        public Guid UploadedByUserId { get; set; }
        public User UploadedBy { get; set; } = null!;
        public string Name { get; set; } = string.Empty;       // "pepe" — used as :pepe:
        public string ImageUrl { get; set; } = string.Empty;   // R2 public URL
        public string R2Key { get; set; } = string.Empty;      // R2 object key for deletion
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
