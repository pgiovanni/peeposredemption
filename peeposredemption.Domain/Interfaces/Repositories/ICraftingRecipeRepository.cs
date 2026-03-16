using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface ICraftingRecipeRepository
{
    Task<CraftingRecipe?> GetByNameAsync(string name);
    Task<List<CraftingRecipe>> GetBySkillAndMaxLevelAsync(SkillType skill, int maxLevel);
    Task<List<CraftingRecipe>> GetAllAsync();
    Task AddAsync(CraftingRecipe recipe);
}
