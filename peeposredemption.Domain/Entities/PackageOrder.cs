namespace peeposredemption.Domain.Entities;

/// <summary>
/// A paid order for a catalog package (today: the Discord AI credit pack).
/// One row per Stripe Checkout Session; the webhook flips it to Completed and
/// stores the invoice links so the customer can download it from /Dashboard.
/// </summary>
public class PackageOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Stable catalog slug (e.g. "discord-ai-addon").</summary>
    public string PackageSlug { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public long PriceCents { get; set; }

    /// <summary>For the AI add-on: the Discord guild that receives the credit.</summary>
    public string? DiscordGuildId { get; set; }
    public string? DiscordGuildName { get; set; }
    /// <summary>For the AI add-on: dollars of AI usage this order grants (price minus margin).</summary>
    public decimal? CreditUsd { get; set; }

    public string StripeSessionId { get; set; } = string.Empty;
    public string? StripePaymentIntentId { get; set; }
    public string? StripeInvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? InvoiceUrl { get; set; }
    public string? InvoicePdfUrl { get; set; }

    public PurchaseStatus Status { get; set; } = PurchaseStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    /// <summary>Set by the bot once the credit has actually landed on the guild's ledger.</summary>
    public DateTime? FulfilledAt { get; set; }
}
