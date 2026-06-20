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

    public Task OnGetAsync() => Task.CompletedTask;

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
