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

                await _uow.SaveChangesAsync();
            }
        }
    }
}
