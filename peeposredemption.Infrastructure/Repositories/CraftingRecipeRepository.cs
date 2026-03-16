using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class CraftingRecipeRepository : ICraftingRecipeRepository
{
    private readonly AppDbContext _db;
    public CraftingRecipeRepository(AppDbContext db) => _db = db;

    public Task<CraftingRecipe?> GetByNameAsync(string name) =>
        _db.CraftingRecipes
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.ItemDefinition)
            .Include(r => r.OutputItem)
            .FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower());

    public Task<List<CraftingRecipe>> GetBySkillAndMaxLevelAsync(SkillType skill, int maxLevel) =>
        _db.CraftingRecipes
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.ItemDefinition)
            .Include(r => r.OutputItem)
            .Where(r => r.RequiredSkill == skill && r.RequiredSkillLevel <= maxLevel)
            .ToListAsync();

    public Task<List<CraftingRecipe>> GetAllAsync() =>
        _db.CraftingRecipes
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.ItemDefinition)
            .Include(r => r.OutputItem)
            .ToListAsync();

    public async Task AddAsync(CraftingRecipe recipe) =>
        await _db.CraftingRecipes.AddAsync(recipe);
}
