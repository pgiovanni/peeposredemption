using MediatR;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Shop.Commands
{
    public record ProcessStripeWebhookCommand(string Payload, string Signature) : IRequest;

    public class ProcessStripeWebhookCommandHandler : IRequestHandler<ProcessStripeWebhookCommand>
    {
        private readonly IUnitOfWork _uow;
        private readonly IStripeWebhookService _webhookService;
        private readonly IEmailService _emailService;

        public ProcessStripeWebhookCommandHandler(IUnitOfWork uow, IStripeWebhookService webhookService, IEmailService emailService)
        {
            _uow = uow;
            _webhookService = webhookService;
            _emailService = emailService;
        }

        public async Task Handle(ProcessStripeWebhookCommand cmd, CancellationToken ct)
        {
            var evt = _webhookService.ParseAndVerify(cmd.Payload, cmd.Signature);

            // ── Subscription lifecycle events ──────────────────────────────

            if (evt.Type == "checkout.session.completed" && evt.SubscriptionId != null)
            {
                // Gold subscription checkout completed — activate the pending record
                var sub = evt.SessionId != null
                    ? await _uow.GoldSubscriptions.GetByStripeSessionIdAsync(evt.SessionId)
                    : null;

                if (sub != null && sub.Status == SubscriptionStatus.Pending)
                {
                    sub.StripeSubscriptionId = evt.SubscriptionId;
                    sub.Status = SubscriptionStatus.Active;
                    sub.StartedAt = DateTime.UtcNow;
                    sub.NextBillingAt = DateTime.UtcNow.AddMonths(1);
                    sub.LastOrbCreditAt = DateTime.UtcNow;

                    var user = await _uow.Users.GetByIdAsync(sub.UserId);
                    if (user != null)
                        await CreditGoldOrbsAsync(user, "Torvex Gold subscription — welcome bonus");

                    await _uow.SaveChangesAsync();
                }
                return;
            }

            if (evt.Type == "customer.subscription.updated" && evt.SubscriptionId != null)
            {
                var sub = await _uow.GoldSubscriptions.GetByStripeSubscriptionIdAsync(evt.SubscriptionId);
                if (sub != null)
                {
                    if (evt.SubscriptionStatus == "active") sub.Status = SubscriptionStatus.Active;
                    else if (evt.SubscriptionStatus == "past_due") sub.Status = SubscriptionStatus.PastDue;
                    else if (evt.SubscriptionStatus == "canceled") sub.Status = SubscriptionStatus.Expired;

                    if (evt.PeriodStart.HasValue)
                        sub.NextBillingAt = evt.PeriodStart.Value.AddMonths(1);

                    // Credit renewal orbs — only once per billing period (idempotency guard)
                    if (evt.SubscriptionStatus == "active" && evt.PeriodStart.HasValue)
                    {
                        var periodStartDate = evt.PeriodStart.Value.Date;
                        var lastCreditDate = sub.LastOrbCreditAt?.Date;
                        if (lastCreditDate == null || lastCreditDate < periodStartDate)
                        {
                            var user = await _uow.Users.GetByIdAsync(sub.UserId);
                            if (user != null)
                            {
                                await CreditGoldOrbsAsync(user, "Torvex Gold monthly renewal bonus");
                                sub.LastOrbCreditAt = evt.PeriodStart.Value;
                            }
                        }
                    }

                    await _uow.SaveChangesAsync();
                }
                return;
            }

            if (evt.Type == "customer.subscription.deleted" && evt.SubscriptionId != null)
            {
                var sub = await _uow.GoldSubscriptions.GetByStripeSubscriptionIdAsync(evt.SubscriptionId);
                if (sub != null)
                {
                    sub.Status = SubscriptionStatus.Expired;
                    await _uow.SaveChangesAsync();
                }
                return;
            }

            if (evt.Type == "invoice.payment_failed" && evt.SubscriptionId != null)
            {
                var sub = await _uow.GoldSubscriptions.GetByStripeSubscriptionIdAsync(evt.SubscriptionId);
                if (sub != null)
                {
                    sub.Status = SubscriptionStatus.PastDue;
                    await _uow.SaveChangesAsync();
                }
                return;
            }

            // ── One-time payment events ────────────────────────────────────

            if (evt.Type != "checkout.session.completed" || evt.SessionId == null)
                return;

            // Idempotency: check if this session was already attributed for referral
            var alreadyAttributed = await _uow.Referrals.PurchaseExistsAsync(evt.SessionId);

            // Handle orb purchases
            var orbPurchase = await _uow.OrbPurchases.GetBySessionIdAsync(evt.SessionId);
            if (orbPurchase != null && orbPurchase.Status == PurchaseStatus.Pending)
            {
                orbPurchase.Status = PurchaseStatus.Completed;
                var orbUser = await _uow.Users.GetByIdAsync(orbPurchase.UserId);
                if (orbUser != null)
                {
                    await _uow.OrbTransactions.AddAsync(new OrbTransaction
                    {
                        UserId = orbPurchase.UserId,
                        Amount = orbPurchase.OrbAmount,
                        Type = OrbTransactionType.StripePurchase,
                        Description = $"Purchased {orbPurchase.OrbAmount} orbs"
                    });
                    orbUser.OrbBalance += orbPurchase.OrbAmount;

                    // Track referral for the ACTUAL purchaser
                    if (!alreadyAttributed)
                        await TryAttributeReferralAsync(orbUser, evt.SessionId, orbPurchase.PriceCents);
                }

                await _uow.SaveChangesAsync();
                return;
            }

            // Handle storage upgrades
            var purchase = await _uow.StorageUpgrades.GetBySessionIdAsync(evt.SessionId);
            if (purchase == null) return;

            purchase.Status = PurchaseStatus.Completed;

            var server = await _uow.Servers.GetByIdAsync(purchase.ServerId);
            if (server != null && purchase.TargetTier > server.StorageTier)
                server.StorageTier = purchase.TargetTier;

            // Track referral for the ACTUAL purchaser (from UserId on purchase record or Stripe metadata)
            if (!alreadyAttributed)
            {
                var purchaserId = purchase.UserId
                    ?? (evt.UserId != null ? Guid.Parse(evt.UserId) : (Guid?)null);

                if (purchaserId.HasValue)
                {
                    var purchaserUser = await _uow.Users.GetByIdAsync(purchaserId.Value);
                    if (purchaserUser != null)
                        await TryAttributeReferralAsync(purchaserUser, evt.SessionId, evt.AmountTotal);
                }
            }

            await _uow.SaveChangesAsync();
        }

        private async Task CreditGoldOrbsAsync(User user, string description)
        {
            const int goldOrbBonus = 500;
            await _uow.OrbTransactions.AddAsync(new OrbTransaction
            {
                UserId = user.Id,
                Amount = goldOrbBonus,
                Type = OrbTransactionType.GoldRenewal,
                Description = description
            });
            user.OrbBalance += goldOrbBonus;
        }

        private async Task TryAttributeReferralAsync(User purchaser, string stripeSessionId, long amountCents)
        {
            if (purchaser.ReferredByCodeId == null) return;

            var refCode = await _uow.Referrals.GetCodeByIdAsync(purchaser.ReferredByCodeId.Value);
            if (refCode == null) return;

            // Don't let someone earn commission on their own purchases
            if (refCode.OwnerId == purchaser.Id) return;

            await _uow.Referrals.AddPurchaseAsync(new ReferralPurchase
            {
                ReferralCodeId = purchaser.ReferredByCodeId.Value,
                PurchaserId = purchaser.Id,
                AmountCents = amountCents,
                StripeSessionId = stripeSessionId
            });

            var marketer = await _uow.Users.GetByIdAsync(refCode.OwnerId);
            if (marketer != null)
                await _emailService.SendReferralPurchaseNotificationAsync(
                    marketer.Username, purchaser.Username, amountCents);
        }
    }
}
