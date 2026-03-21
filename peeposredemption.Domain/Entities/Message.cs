namespace peeposredemption.Domain.Entities
{
    public class Message
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ChannelId { get; set; }
        public Guid AuthorId { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsEdited { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public DateTime? EditedAt { get; set; }
        public Guid? ReplyToMessageId { get; set; }
        public Channel Channel { get; set; }
        public User Author { get; set; }
        public Message? ReplyToMessage { get; set; }
    }

}
