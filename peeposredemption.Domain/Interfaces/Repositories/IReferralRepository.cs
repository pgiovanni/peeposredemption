using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IReferralRepository
{
    Task<ReferralCode?> GetCodeByStringAsync(string code);
    Task<ReferralCode?> GetCodeByOwnerIdAsync(Guid ownerId);
    Task AddCodeAsync(ReferralCode code);
    Task AddPurchaseAsync(ReferralPurchase purchase);
    Task<int> GetReferredUserCountAsync(Guid codeId);
    Task<List<ReferralPurchase>> GetPurchasesByCodeIdAsync(Guid codeId);
    Task<List<ReferralCode>> GetAllCodesAsync();
    Task<bool> PurchaseExistsAsync(string stripeSessionId);
}
