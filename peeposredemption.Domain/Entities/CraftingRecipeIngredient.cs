namespace peeposredemption.Domain.Entities;

public class CraftingRecipeIngredient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecipeId { get; set; }
    public Guid ItemDefinitionId { get; set; }
    public int Quantity { get; set; } = 1;

    // Navigation
    public CraftingRecipe Recipe { get; set; } = null!;
    public ItemDefinition ItemDefinition { get; set; } = null!;
}
