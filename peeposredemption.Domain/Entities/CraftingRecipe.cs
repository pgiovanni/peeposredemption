namespace peeposredemption.Domain.Entities;

public class CraftingRecipe
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Guid OutputItemId { get; set; }
    public int OutputQuantity { get; set; } = 1;
    public SkillType RequiredSkill { get; set; }
    public int RequiredSkillLevel { get; set; }
    public long OrbCost { get; set; }
    public decimal BaseSuccessRate { get; set; } = 0.8m;
    public long XpReward { get; set; }

    // Navigation
    public ItemDefinition OutputItem { get; set; } = null!;
    public ICollection<CraftingRecipeIngredient> Ingredients { get; set; } = new List<CraftingRecipeIngredient>();
}
