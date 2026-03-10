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

            if (evt.Type == "checkout.session.completed" && evt.SessionId != null)
            {
                var purchase = await _uow.StorageUpgrades.GetBySessionIdAsync(evt.SessionId);
                if (purchase == null) return;

                purchase.Status = PurchaseStatus.Completed;

                var server = await _uow.Servers.GetByIdAsync(purchase.ServerId);
                if (server != null && purchase.TargetTier > server.StorageTier)
                    server.StorageTier = purchase.TargetTier;

                // Track referral for any referred user who made a purchase
                var serverMembers = await _uow.Servers.GetServerMembersAsync(purchase.ServerId);
                if (serverMembers != null)
                {
                    foreach (var member in serverMembers)
                    {
                        var user = await _uow.Users.GetByIdAsync(member.UserId);
                        if (user?.ReferredByCodeId == null) continue;
                        if (await _uow.Referrals.PurchaseExistsAsync(evt.SessionId)) continue;

                        var refCode = await _uow.Referrals.GetCodeByIdAsync(user.ReferredByCodeId.Value);
                        var marketer = refCode != null ? await _uow.Users.GetByIdAsync(refCode.OwnerId) : null;

                        await _uow.Referrals.AddPurchaseAsync(new Domain.Entities.ReferralPurchase
                        {
                            ReferralCodeId = user.ReferredByCodeId.Value,
                            PurchaserId = user.Id,
                            AmountCents = evt.AmountTotal,
                            StripeSessionId = evt.SessionId
                        });

                        if (marketer != null)
                            await _emailService.SendReferralPurchaseNotificationAsync(marketer.Username, user.Username, evt.AmountTotal);

                        break; // one purchase = one referral attribution
                    }
                }

                await _uow.SaveChangesAsync();
            }
        }
    }
}
