using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.API.Pages;

[AllowAnonymous]
public class WikiModel : PageModel
{
    private readonly AppDbContext _db;
    public WikiModel(AppDbContext db) => _db = db;

    public List<WikiItem>    Items    { get; set; } = new();
    public List<WikiMonster> Monsters { get; set; } = new();

    public async Task OnGetAsync()
    {
        // ── Items (index only — no peepos, no inline detail) ───────────────────
        var allItems = await _db.ItemDefinitions
            .Where(i => i.Type != GameItemType.Collectible)
            .OrderBy(i => i.Type).ThenBy(i => i.LevelReq).ThenBy(i => i.Name)
            .ToListAsync();

        Items = allItems.Select(item => new WikiItem
        {
            Id      = item.Id,
            Name    = item.Name,
            Description = item.Description,
            Icon    = item.Icon,
            Type    = item.Type,
            SubType = item.SubType,
            Rarity  = item.Rarity,
            LevelReq = item.LevelReq,
            BuyPrice = item.BuyPrice,
            SellPrice = item.SellPrice,
            Element = item.Element,
            MinDamage = item.MinDamage,
            MaxDamage = item.MaxDamage,
            BonusSTR = item.BonusSTR,
            BonusDEF = item.BonusDEF,
            BonusINT = item.BonusINT,
            BonusDEX = item.BonusDEX,
            BonusVIT = item.BonusVIT,
            BonusLUK = item.BonusLUK,
            BonusHP  = item.BonusHP,
            BonusMP  = item.BonusMP,
            HealAmount = item.HealAmount,
            ManaRestoreAmount = item.ManaRestoreAmount,
            EnchantTier = item.EnchantTier,
        }).ToList();

        // ── Monsters ───────────────────────────────────────────────────────────
        var allMonsters = await _db.MonsterDefinitions
            .Include(m => m.LootTable).ThenInclude(l => l.ItemDefinition)
            .OrderBy(m => m.Zone).ThenBy(m => m.Level)
            .ToListAsync();

        Monsters = allMonsters.Select(m => new WikiMonster
        {
            Name         = m.Name,
            Description  = m.Description,
            Icon         = m.Icon,
            Level        = m.Level,
            Zone         = m.Zone,
            Element      = m.Element,
            MaxHp        = m.MaxHp,
            STR          = m.STR,
            DEF          = m.DEF,
            INT          = m.INT,
            DEX          = m.DEX,
            MinDamage    = m.MinDamage,
            MaxDamage    = m.MaxDamage,
            XpReward     = m.XpReward,
            OrbRewardMin = m.OrbRewardMin,
            OrbRewardMax = m.OrbRewardMax,
            Abilities    = ParseAbilities(m.AbilityJson),
            Loot         = m.LootTable.Select(l => new WikiLootEntry
            {
                ItemName   = l.ItemDefinition.Name,
                ItemIcon   = l.ItemDefinition.Icon,
                DropChance = (int)Math.Round((double)l.DropChance * 100),
                MinQty     = l.MinQuantity,
                MaxQty     = l.MaxQuantity
            }).OrderByDescending(l => l.DropChance).ToList()
        }).ToList();
    }

    private static List<string> ParseAbilities(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new();
        try
        {
            var list = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.JsonElement>>(json);
            return list?
                .Select(a => a.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "")
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList() ?? new();
        }
        catch { return new(); }
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────

    public static string Slugify(string name) =>
        System.Text.RegularExpressions.Regex
            .Replace(name.ToLower().Replace(" ", "-"), "[^a-z0-9-]", "");

    public record WikiItem
    {
        public Guid           Id                { get; init; }
        public string         Slug              => Slugify(Name);
        public string         Name              { get; init; } = "";
        public string         Description       { get; init; } = "";
        public string         Icon              { get; init; } = "";
        public GameItemType   Type              { get; init; }
        public ItemSubType    SubType           { get; init; }
        public GameItemRarity Rarity            { get; init; }
        public int            LevelReq          { get; init; }
        public long           BuyPrice          { get; init; }
        public long           SellPrice         { get; init; }
        public Element        Element           { get; init; }
        public int            MinDamage         { get; init; }
        public int            MaxDamage         { get; init; }
        public int            BonusSTR          { get; init; }
        public int            BonusDEF          { get; init; }
        public int            BonusINT          { get; init; }
        public int            BonusDEX          { get; init; }
        public int            BonusVIT          { get; init; }
        public int            BonusLUK          { get; init; }
        public int            BonusHP           { get; init; }
        public int            BonusMP           { get; init; }
        public int            HealAmount        { get; init; }
        public int            ManaRestoreAmount { get; init; }
        public int            EnchantTier       { get; init; }
        public List<WikiRecipe>     Recipes    { get; init; } = new();
        public List<WikiDrop>       DroppedBy  { get; init; } = new();
    }

    public record WikiRecipe
    {
        public string             Name        { get; init; } = "";
        public string             Skill       { get; init; } = "";
        public int                SkillLevel  { get; init; }
        public long               OrbCost     { get; init; }
        public int                SuccessRate { get; init; }
        public int                OutputQty   { get; init; }
        public List<WikiIngredient> Ingredients { get; init; } = new();
    }

    public record WikiIngredient
    {
        public string Name { get; init; } = "";
        public string Icon { get; init; } = "";
        public int    Qty  { get; init; }
    }

    public record WikiDrop
    {
        public string MonsterName { get; init; } = "";
        public string MonsterIcon { get; init; } = "";
        public string Zone        { get; init; } = "";
        public int    DropChance  { get; init; }
        public int    MinQty      { get; init; }
        public int    MaxQty      { get; init; }
    }

    public record WikiMonster
    {
        public string  Name         { get; init; } = "";
        public string  Description  { get; init; } = "";
        public string  Icon         { get; init; } = "";
        public int     Level        { get; init; }
        public string  Zone         { get; init; } = "";
        public Element Element      { get; init; }
        public int     MaxHp        { get; init; }
        public int     STR          { get; init; }
        public int     DEF          { get; init; }
        public int     INT          { get; init; }
        public int     DEX          { get; init; }
        public int     MinDamage    { get; init; }
        public int     MaxDamage    { get; init; }
        public long    XpReward     { get; init; }
        public long    OrbRewardMin { get; init; }
        public long    OrbRewardMax { get; init; }
        public List<string>        Abilities { get; init; } = new();
        public List<WikiLootEntry> Loot      { get; init; } = new();
    }

    public record WikiLootEntry
    {
        public string ItemName   { get; init; } = "";
        public string ItemIcon   { get; init; } = "";
        public int    DropChance { get; init; }
        public int    MinQty     { get; init; }
        public int    MaxQty     { get; init; }
    }
}
