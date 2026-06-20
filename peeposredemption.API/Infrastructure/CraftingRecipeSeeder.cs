using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.API.Infrastructure;

/// <summary>
/// Seeds all crafting recipes — smithing (smelt + forge), woodcraft, and cooking.
/// Safe to run on every startup — skips recipes that already exist by name.
/// </summary>
public static class CraftingRecipeSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        var items    = await db.ItemDefinitions.ToDictionaryAsync(i => i.Name, i => i.Id);
        var existing = await db.CraftingRecipes.Select(r => r.Name).ToHashSetAsync();

        var newRecipes = new List<(CraftingRecipe r, (string name, int qty)[] ing)>();

        void Add(string recipeName, string outputItem, int outputQty,
                 SkillType skill, int skillLevel, decimal successRate, long xpReward,
                 long orbCost = 0, params (string name, int qty)[] ingredients)
        {
            if (existing.Contains(recipeName)) return;
            if (!items.ContainsKey(outputItem)) return;
            var recipe = new CraftingRecipe
            {
                Name              = recipeName,
                OutputItemId      = items[outputItem],
                OutputQuantity    = outputQty,
                RequiredSkill     = skill,
                RequiredSkillLevel= skillLevel,
                BaseSuccessRate   = successRate,
                XpReward          = xpReward,
                OrbCost           = orbCost,
            };
            newRecipes.Add((recipe, ingredients));
        }

        // ── SMELTING: ore → bars (Smithing) ──────────────────────────────────
        Add("Copper Bar",     "Copper Bar",     1, SkillType.Smithing,  1, 0.99m,  10, ingredients: [("Copper Ore",     2)]);
        Add("Iron Bar",       "Iron Bar",       1, SkillType.Smithing, 10, 0.97m,  25, ingredients: [("Iron Ore",       2)]);
        Add("Silver Bar",     "Silver Bar",     1, SkillType.Smithing, 20, 0.95m,  50, ingredients: [("Silver Ore",     3)]);
        Add("Gold Bar",       "Gold Bar",       1, SkillType.Smithing, 30, 0.92m,  80, ingredients: [("Gold Ore",       3)]);
        Add("Mithril Bar",    "Mithril Bar",    1, SkillType.Smithing, 40, 0.90m, 130, ingredients: [("Mithril Ore",    4)]);
        Add("Adamantite Bar", "Adamantite Bar", 1, SkillType.Smithing, 55, 0.88m, 200, ingredients: [("Adamantite Ore", 4)]);
        Add("Adamantium Bar", "Adamantium Bar", 1, SkillType.Smithing, 70, 0.85m, 300, ingredients: [("Adamantium Ore", 5)]);
        Add("Void Ingot",     "Void Ingot",     1, SkillType.Smithing, 85, 0.80m, 450, ingredients: [("Voidstone",      3)]);

        // ── FORGING WEAPONS: bars → weapons (Smithing) ───────────────────────
        Add("Iron Sword",        "Iron Sword",        1, SkillType.Smithing, 10, 0.90m,  60, ingredients: [("Iron Bar",       3)]);
        Add("Steel Sword",       "Steel Sword",       1, SkillType.Smithing, 20, 0.88m, 100, ingredients: [("Iron Bar",       4), ("Silver Bar",  1)]);
        Add("Mithril Blade",     "Mithril Blade",     1, SkillType.Smithing, 40, 0.85m, 200, ingredients: [("Mithril Bar",    4)]);
        Add("Adamantite Sword",  "Adamantite Sword",  1, SkillType.Smithing, 55, 0.82m, 320, ingredients: [("Adamantite Bar", 5)]);
        Add("Adamantium Blade",  "Adamantium Blade",  1, SkillType.Smithing, 70, 0.75m, 500, ingredients: [("Adamantium Bar", 6), ("Diamond",      1)]);
        Add("Dragon Lance",      "Dragon Lance",      1, SkillType.Smithing, 75, 0.65m, 750, ingredients: [("Adamantium Bar", 5), ("Dragon Scale", 5), ("Dragon Claw", 2)]);

        // ── FORGING ARMOR: bars → armor (Smithing) ───────────────────────────
        Add("Iron Helmet",            "Iron Helmet",            1, SkillType.Smithing, 10, 0.92m,  45, ingredients: [("Iron Bar",       2)]);
        Add("Iron Chestplate",        "Iron Chestplate",        1, SkillType.Smithing, 12, 0.90m,  80, ingredients: [("Iron Bar",       4)]);
        Add("Steel Helmet",           "Steel Helmet",           1, SkillType.Smithing, 22, 0.88m,  90, ingredients: [("Iron Bar",       2), ("Silver Bar",      1)]);
        Add("Steel Chestplate",       "Steel Chestplate",       1, SkillType.Smithing, 25, 0.86m, 150, ingredients: [("Iron Bar",       4), ("Silver Bar",      2)]);
        Add("Steel Leggings",         "Steel Leggings",         1, SkillType.Smithing, 23, 0.87m, 110, ingredients: [("Iron Bar",       3), ("Silver Bar",      1)]);
        Add("Steel Boots",            "Steel Boots",            1, SkillType.Smithing, 21, 0.89m,  65, ingredients: [("Iron Bar",       2)]);
        Add("Mithril Helmet",         "Mithril Helmet",         1, SkillType.Smithing, 40, 0.85m, 180, ingredients: [("Mithril Bar",    3)]);
        Add("Mithril Chestplate",     "Mithril Chestplate",     1, SkillType.Smithing, 43, 0.83m, 300, ingredients: [("Mithril Bar",    6)]);
        Add("Mithril Leggings",       "Mithril Leggings",       1, SkillType.Smithing, 42, 0.84m, 240, ingredients: [("Mithril Bar",    5)]);
        Add("Mithril Boots",          "Mithril Boots",          1, SkillType.Smithing, 41, 0.86m, 120, ingredients: [("Mithril Bar",    2)]);
        Add("Adamantite Helmet",      "Adamantite Helmet",      1, SkillType.Smithing, 55, 0.80m, 280, ingredients: [("Adamantite Bar", 4)]);
        Add("Adamantite Chestplate",  "Adamantite Chestplate",  1, SkillType.Smithing, 58, 0.78m, 500, ingredients: [("Adamantite Bar", 8)]);
        Add("Adamantite Leggings",    "Adamantite Leggings",    1, SkillType.Smithing, 57, 0.79m, 380, ingredients: [("Adamantite Bar", 6)]);
        Add("Adamantite Boots",       "Adamantite Boots",       1, SkillType.Smithing, 56, 0.81m, 180, ingredients: [("Adamantite Bar", 3)]);

        // ── WOODCRAFT: logs → bows and staves (Woodcutting) ──────────────────
        Add("Oak Staff",       "Oak Staff",       1, SkillType.Woodcutting, 15, 0.92m,  70, ingredients: [("Oak Logs",   5)]);
        Add("Willow Shortbow", "Willow Shortbow", 1, SkillType.Woodcutting, 30, 0.90m, 120, ingredients: [("Willow Logs",4)]);
        Add("Maple Longbow",   "Maple Longbow",   1, SkillType.Woodcutting, 45, 0.87m, 200, ingredients: [("Maple Logs", 4)]);
        Add("Yew Longbow",     "Yew Longbow",     1, SkillType.Woodcutting, 60, 0.84m, 320, ingredients: [("Yew Logs",   5)]);
        Add("Magic Staff",     "Magic Staff",     1, SkillType.Woodcutting, 75, 0.80m, 500, ingredients: [("Magic Logs", 4)]);
        Add("Void Wood Staff", "Void Wood Staff", 1, SkillType.Woodcutting, 90, 0.72m, 750, ingredients: [("Void Wood",  3), ("Void Ingot", 1)]);

        // ── COOKING: raw fish → cooked fish (Cooking) ────────────────────────
        Add("Cooked Shrimp",      "Cooked Shrimp",      1, SkillType.Cooking,  1, 0.98m,   5, ingredients: [("Raw Shrimp",     1)]);
        Add("Cooked Trout",       "Cooked Trout",       1, SkillType.Cooking,  5, 0.96m,  12, ingredients: [("Raw Trout",      1)]);
        Add("Cooked Salmon",      "Cooked Salmon",      1, SkillType.Cooking, 12, 0.94m,  22, ingredients: [("Raw Salmon",     1)]);
        Add("Cooked Tuna",        "Cooked Tuna",        1, SkillType.Cooking, 20, 0.92m,  35, ingredients: [("Raw Tuna",       1)]);
        Add("Cooked Lobster",     "Cooked Lobster",     1, SkillType.Cooking, 30, 0.90m,  55, ingredients: [("Raw Lobster",    1)]);
        Add("Cooked Swordfish",   "Cooked Swordfish",   1, SkillType.Cooking, 45, 0.88m,  80, ingredients: [("Raw Swordfish",  1)]);
        Add("Cooked Shark",       "Cooked Shark",       1, SkillType.Cooking, 60, 0.85m, 120, ingredients: [("Raw Shark",      1)]);
        Add("Cooked Abyssal Eel", "Cooked Abyssal Eel", 1, SkillType.Cooking, 75, 0.82m, 180, ingredients: [("Raw Abyssal Eel",1)]);
        Add("Fish Stew",          "Fish Stew",          1, SkillType.Cooking, 20, 0.88m,  45, ingredients: [("Raw Salmon", 1), ("Oak Logs", 1)]);

        if (newRecipes.Count == 0) return;

        foreach (var (recipe, ingredients) in newRecipes)
        {
            db.CraftingRecipes.Add(recipe);
            foreach (var (name, qty) in ingredients)
            {
                if (!items.TryGetValue(name, out var itemId)) continue;
                db.CraftingRecipeIngredients.Add(new CraftingRecipeIngredient
                {
                    RecipeId         = recipe.Id,
                    ItemDefinitionId = itemId,
                    Quantity         = qty,
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
