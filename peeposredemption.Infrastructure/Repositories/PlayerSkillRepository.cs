using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class PlayerSkillRepository : IPlayerSkillRepository
{
    private readonly AppDbContext _db;
    public PlayerSkillRepository(AppDbContext db) => _db = db;

    public Task<List<PlayerSkill>> GetByPlayerIdAsync(Guid playerId) =>
        _db.PlayerSkills.Where(s => s.PlayerId == playerId).ToListAsync();

    public Task<PlayerSkill?> GetByPlayerAndSkillAsync(Guid playerId, SkillType skillType) =>
        _db.PlayerSkills.FirstOrDefaultAsync(s => s.PlayerId == playerId && s.SkillType == skillType);

    public async Task AddAsync(PlayerSkill skill) =>
        await _db.PlayerSkills.AddAsync(skill);
}
