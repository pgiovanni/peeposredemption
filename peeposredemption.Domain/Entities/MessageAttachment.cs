namespace peeposredemption.Domain.Entities;

public class MessageAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? MessageId { get; set; }   // null until the message is saved
    public Guid ChannelId { get; set; }
    public Guid UploaderId { get; set; }
    public string R2Key { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Message? Message { get; set; }
    public User Uploader { get; set; } = null!;
}
