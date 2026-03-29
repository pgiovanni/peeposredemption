namespace peeposredemption.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string? AvatarUrl { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? Pronouns { get; set; }
    public string? BannerUrl { get; set; }
    public string? ProfileBackgroundColor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool EmailConfirmed {  get; set; }
    public string? EmailConfirmationtoken { get; set; }
 
    public Guid? ReferredByCodeId { get; set; }
    public long OrbBalance { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public bool IsMinor => DateOfBirth.HasValue
        && DateOfBirth.Value.AddYears(18) > DateTime.UtcNow
        && DateOfBirth.Value.AddYears(13) <= DateTime.UtcNow;

    public bool IsSuspicious { get; set; }

    // MFA (TOTP)
    public bool IsMfaEnabled { get; set; }
    public string? TotpSecret { get; set; }
    public string? MfaRecoveryCodes { get; set; }

    // Password reset
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    public string DisplayOrUsername => DisplayName ?? Username;

    public ICollection<ServerMember> ServerMemberships { get; set; }
    public ICollection<Message> Messages { get; set; }
    public ICollection<DirectMessage> SentDirectMessages { get; set; }
    public ICollection<DirectMessage> ReceivedDirectMessages { get; set; }
    public ICollection<ParentalLink> ParentalLinksAsChild { get; set; }
    public ICollection<ParentalLink> ParentalLinksAsParent { get; set; }
    public ICollection<TorvexGoldSubscription> GoldSubscriptions { get; set; }
}
