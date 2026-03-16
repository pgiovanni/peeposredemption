using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IPlayerSkillRepository
{
    Task<List<PlayerSkill>> GetByPlayerIdAsync(Guid playerId);
    Task<PlayerSkill?> GetByPlayerAndSkillAsync(Guid playerId, SkillType skillType);
    Task AddAsync(PlayerSkill skill);
}
