using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class VoiceSessionRepository : IVoiceSessionRepository
{
    private readonly AppDbContext _db;
    public VoiceSessionRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(VoiceSession session) =>
        await _db.VoiceSessions.AddAsync(session);

    public async Task<List<VoiceSession>> GetByUserIdAsync(Guid userId, int limit = 50) =>
        await _db.VoiceSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LeftAt)
            .Take(limit)
            .ToListAsync();

    public async Task<long> GetTodayOrbsEarnedAsync(Guid userId)
    {
        var todayUtc = DateTime.UtcNow.Date;
        return await _db.VoiceSessions
            .Where(s => s.UserId == userId && s.LeftAt >= todayUtc)
            .SumAsync(s => s.OrbsEarned);
    }

    public async Task<double> GetTotalHoursAsync(Guid userId)
    {
        var sessions = await _db.VoiceSessions
            .Where(s => s.UserId == userId)
            .Select(s => new { s.JoinedAt, s.LeftAt })
            .ToListAsync();
        return sessions.Sum(s => (s.LeftAt - s.JoinedAt).TotalHours);
    }

    public async Task<long> GetTotalOrbsEarnedAsync(Guid userId) =>
        await _db.VoiceSessions
            .Where(s => s.UserId == userId)
            .SumAsync(s => s.OrbsEarned);

    public async Task<List<(Guid ServerId, double Hours, long Orbs)>> GetBreakdownByServerAsync(Guid userId)
    {
        var sessions = await _db.VoiceSessions
            .Where(s => s.UserId == userId)
            .Select(s => new { s.ServerId, s.JoinedAt, s.LeftAt, s.OrbsEarned })
            .ToListAsync();

        return sessions
            .GroupBy(s => s.ServerId)
            .Select(g => (
                g.Key,
                g.Sum(s => (s.LeftAt - s.JoinedAt).TotalHours),
                g.Sum(s => s.OrbsEarned)))
            .ToList();
    }

    public async Task<int[]> GetHourlyActivityAsync(Guid userId)
    {
        var hours = await _db.VoiceSessions
            .Where(s => s.UserId == userId)
            .Select(s => s.JoinedAt.Hour)
            .ToListAsync();

        var result = new int[24];
        foreach (var h in hours) result[h]++;
        return result;
    }
}
