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

        public ProcessStripeWebhookCommandHandler(IUnitOfWork uow, IStripeWebhookService webhookService)
        {
            _uow = uow;
            _webhookService = webhookService;
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

                // Track referral if the server owner was referred by a marketer
                var serverMembers = await _uow.Servers.GetServerMembersAsync(purchase.ServerId);
                var ownerMembership = serverMembers?.FirstOrDefault(m => m.Role == Domain.Entities.ServerRole.Owner);
                if (ownerMembership != null)
                {
                    var buyer = await _uow.Users.GetByIdAsync(ownerMembership.UserId);
                    if (buyer?.ReferredByCodeId != null && !await _uow.Referrals.PurchaseExistsAsync(evt.SessionId))
                    {
                        await _uow.Referrals.AddPurchaseAsync(new Domain.Entities.ReferralPurchase
                        {
                            ReferralCodeId = buyer.ReferredByCodeId.Value,
                            PurchaserId = buyer.Id,
                            AmountCents = evt.AmountTotal,
                            StripeSessionId = evt.SessionId
                        });
                    }
                }

                await _uow.SaveChangesAsync();
            }
        }
    }
}
