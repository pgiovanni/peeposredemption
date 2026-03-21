using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IVoiceSessionRepository
{
    Task AddAsync(VoiceSession session);
    Task<List<VoiceSession>> GetByUserIdAsync(Guid userId, int limit = 50);
    Task<long> GetTodayOrbsEarnedAsync(Guid userId);
    Task<double> GetTotalHoursAsync(Guid userId);
    Task<long> GetTotalOrbsEarnedAsync(Guid userId);
    Task<List<(Guid ServerId, double Hours, long Orbs)>> GetBreakdownByServerAsync(Guid userId);
}
