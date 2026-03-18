using MediatR;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Shop.Commands
{
    public record CreateStorageUpgradeSessionCommand(
        Guid ServerId,
        Guid RequestingUserId,
        StorageTier TargetTier,
        string ReturnBaseUrl) : IRequest<string>;

    public class CreateStorageUpgradeSessionCommandHandler : IRequestHandler<CreateStorageUpgradeSessionCommand, string>
    {
        private readonly IUnitOfWork _uow;
        private readonly IStripeService _stripe;

        public CreateStorageUpgradeSessionCommandHandler(IUnitOfWork uow, IStripeService stripe)
        {
            _uow = uow;
            _stripe = stripe;
        }

        public async Task<string> Handle(CreateStorageUpgradeSessionCommand cmd, CancellationToken ct)
        {
            var server = await _uow.Servers.GetByIdAsync(cmd.ServerId)
                ?? throw new KeyNotFoundException("Server not found.");

            var role = await _uow.Servers.GetMemberRoleAsync(cmd.ServerId, cmd.RequestingUserId);
            if (role == null)
                throw new UnauthorizedAccessException("You must be a member of this server to purchase a boost.");

            if (server.StorageTier >= cmd.TargetTier)
                throw new InvalidOperationException($"This server is already on the {StorageLimits.GetLabel(server.StorageTier)} tier or higher.");

            var successUrl = $"{cmd.ReturnBaseUrl}/App/ServerSettings?serverId={cmd.ServerId}&upgraded=true";
            var cancelUrl = $"{cmd.ReturnBaseUrl}/App/ServerSettings?serverId={cmd.ServerId}";

            var result = await _stripe.CreateStorageUpgradeSessionAsync(
                cmd.ServerId, cmd.RequestingUserId, server.Name, cmd.TargetTier, successUrl, cancelUrl);

            await _uow.StorageUpgrades.AddAsync(new StorageUpgradePurchase
            {
                ServerId = cmd.ServerId,
                UserId = cmd.RequestingUserId,
                StripeSessionId = result.SessionId,
                TargetTier = cmd.TargetTier,
                Status = PurchaseStatus.Pending
            });
            await _uow.SaveChangesAsync();

            return result.Url;
        }
    }
}
