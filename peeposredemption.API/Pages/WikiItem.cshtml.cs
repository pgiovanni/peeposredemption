using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.API.Pages;

[AllowAnonymous]
public class WikiItemModel : PageModel
{
    private readonly AppDbContext _db;
    public WikiItemModel(AppDbContext db) => _db = db;

    public WikiModel.WikiItem? Item { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var allItems = await _db.ItemDefinitions.ToListAsync();
        var match = allItems.FirstOrDefault(i => Slugify(i.Name) == slug);
        if (match == null) return NotFound();

        var recipes = await _db.CraftingRecipes
            .Include(r => r.Ingredients).ThenInclude(ing => ing.ItemDefinition)
            .Where(r => r.OutputItemId == match.Id)
            .ToListAsync();

        var drops = await _db.MonsterLootEntries
            .Include(l => l.MonsterDefinition)
            .Where(l => l.ItemDefinitionId == match.Id)
            .OrderByDescending(l => l.DropChance)
            .ToListAsync();

        Item = new WikiModel.WikiItem
        {
            Id                = match.Id,
            Name              = match.Name,
            Description       = match.Description,
            Icon              = match.Icon,
            Type              = match.Type,
            SubType           = match.SubType,
            Rarity            = match.Rarity,
            LevelReq          = match.LevelReq,
            BuyPrice          = match.BuyPrice,
            SellPrice         = match.SellPrice,
            Element           = match.Element,
            MinDamage         = match.MinDamage,
            MaxDamage         = match.MaxDamage,
            BonusSTR          = match.BonusSTR,
            BonusDEF          = match.BonusDEF,
            BonusINT          = match.BonusINT,
            BonusDEX          = match.BonusDEX,
            BonusVIT          = match.BonusVIT,
            BonusLUK          = match.BonusLUK,
            BonusHP           = match.BonusHP,
            BonusMP           = match.BonusMP,
            HealAmount        = match.HealAmount,
            ManaRestoreAmount = match.ManaRestoreAmount,
            EnchantTier       = match.EnchantTier,
            Recipes = recipes.Select(r => new WikiModel.WikiRecipe
            {
                Name        = r.Name,
                Skill       = r.RequiredSkill.ToString(),
                SkillLevel  = r.RequiredSkillLevel,
                OrbCost     = r.OrbCost,
                SuccessRate = (int)Math.Round((double)r.BaseSuccessRate * 100),
                OutputQty   = r.OutputQuantity,
                Ingredients = r.Ingredients.Select(ing => new WikiModel.WikiIngredient
                {
                    Name = ing.ItemDefinition.Name,
                    Icon = ing.ItemDefinition.Icon,
                    Qty  = ing.Quantity
                }).ToList()
            }).ToList(),
            DroppedBy = drops.Select(d => new WikiModel.WikiDrop
            {
                MonsterName = d.MonsterDefinition.Name,
                MonsterIcon = d.MonsterDefinition.Icon,
                Zone        = d.MonsterDefinition.Zone,
                DropChance  = (int)Math.Round((double)d.DropChance * 100),
                MinQty      = d.MinQuantity,
                MaxQty      = d.MaxQuantity
            }).ToList()
        };

        return Page();
    }

    public static string Slugify(string name) =>
        System.Text.RegularExpressions.Regex
            .Replace(name.ToLower().Replace(" ", "-"), "[^a-z0-9-]", "");
}
