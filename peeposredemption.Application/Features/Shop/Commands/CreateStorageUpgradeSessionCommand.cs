using MediatR;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Shop.Commands
{
    public record CreateStorageUpgradeSessionCommand(
        Guid ServerId,
        Guid RequestingUserId,
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
            if (role != ServerRole.Owner)
                throw new UnauthorizedAccessException("Only the server owner can purchase a boost.");

            if (server.StorageTier == StorageTier.Boosted)
                throw new InvalidOperationException("This server is already boosted.");

            var successUrl = $"{cmd.ReturnBaseUrl}/App/ServerSettings?serverId={cmd.ServerId}&boosted=true";
            var cancelUrl = $"{cmd.ReturnBaseUrl}/App/ServerSettings?serverId={cmd.ServerId}";

            var result = await _stripe.CreateStorageUpgradeSessionAsync(
                cmd.ServerId, server.Name, successUrl, cancelUrl);

            // Save pending purchase so webhook can look it up by session ID
            await _uow.StorageUpgrades.AddAsync(new StorageUpgradePurchase
            {
                ServerId = cmd.ServerId,
                StripeSessionId = result.SessionId,
                Status = PurchaseStatus.Pending
            });
            await _uow.SaveChangesAsync();

            return result.Url;
        }
    }
}
