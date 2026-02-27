namespace peeposredemption.Domain.Entities
{
    public class DirectMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SenderId { get; set; }
        public Guid RecipientId { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
        public User Sender { get; set; }
        public User Recipient { get; set; }


    }
}
