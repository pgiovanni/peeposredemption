namespace peeposredemption.Domain.Entities
{
    public enum StorageTier { Free = 0, Standard = 1, Boosted = 2 }

    public enum PurchaseStatus { Pending, Completed, Failed }

    public class StorageUpgradePurchase
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ServerId { get; set; }
        public Server Server { get; set; } = null!;
        public string StripeSessionId { get; set; } = string.Empty;
        public StorageTier TargetTier { get; set; } = StorageTier.Standard;
        public PurchaseStatus Status { get; set; } = PurchaseStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
