using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Infrastructure.Persistence;
using System.Text.Json;

namespace peeposredemption.API.Pages;

[AllowAnonymous]
public class WikiMonsterModel : PageModel
{
    private readonly AppDbContext _db;
    public WikiMonsterModel(AppDbContext db) => _db = db;

    public MonsterData? Monster { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        // Load names only first, then fetch the single match with its loot
        var names = await _db.MonsterDefinitions
            .Select(m => new { m.Id, m.Name })
            .ToListAsync();
        var found = names.FirstOrDefault(m => WikiModel.Slugify(m.Name) == slug);
        if (found == null) return NotFound();

        var match = await _db.MonsterDefinitions
            .Include(m => m.LootTable).ThenInclude(l => l.ItemDefinition)
            .FirstOrDefaultAsync(m => m.Id == found.Id);
        if (match == null) return NotFound();

        Monster = new MonsterData
        {
            Name        = match.Name,
            Description = match.Description ?? "",
            Icon        = match.Icon,
            Level       = match.Level,
            Zone        = match.Zone,
            Element     = match.Element,
            MaxHp       = match.MaxHp,
            STR         = match.STR,
            DEF         = match.DEF,
            INT         = match.INT,
            DEX         = match.DEX,
            MinDamage   = match.MinDamage,
            MaxDamage   = match.MaxDamage,
            XpReward    = match.XpReward,
            OrbMin      = match.OrbRewardMin,
            OrbMax      = match.OrbRewardMax,
            Abilities   = ParseAbilities(match.AbilityJson),
            Loot        = match.LootTable
                .Where(l => l.ItemDefinition.Type != GameItemType.Collectible)
                .OrderByDescending(l => l.DropChance)
                .Select(l => new LootEntry
                {
                    ItemName   = l.ItemDefinition.Name,
                    ItemIcon   = l.ItemDefinition.Icon,
                    ItemSlug   = WikiModel.Slugify(l.ItemDefinition.Name),
                    DropChance = (int)Math.Round((double)l.DropChance * 100),
                    MinQty     = l.MinQuantity,
                    MaxQty     = l.MaxQuantity
                }).ToList()
        };

        return Page();
    }

    private static List<string> ParseAbilities(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new();
        try
        {
            var list = JsonSerializer.Deserialize<List<JsonElement>>(json);
            return list?
                .Select(a => a.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "")
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList() ?? new();
        }
        catch { return new(); }
    }

    public record MonsterData
    {
        public string  Name        { get; init; } = "";
        public string  Description { get; init; } = "";
        public string  Icon        { get; init; } = "";
        public int     Level       { get; init; }
        public string  Zone        { get; init; } = "";
        public Element Element     { get; init; }
        public int     MaxHp       { get; init; }
        public int     STR         { get; init; }
        public int     DEF         { get; init; }
        public int     INT         { get; init; }
        public int     DEX         { get; init; }
        public int     MinDamage   { get; init; }
        public int     MaxDamage   { get; init; }
        public long    XpReward    { get; init; }
        public long    OrbMin      { get; init; }
        public long    OrbMax      { get; init; }
        public List<string>    Abilities { get; init; } = new();
        public List<LootEntry> Loot      { get; init; } = new();
    }

    public record LootEntry
    {
        public string ItemName   { get; init; } = "";
        public string ItemIcon   { get; init; } = "";
        public string ItemSlug   { get; init; } = "";
        public int    DropChance { get; init; }
        public int    MinQty     { get; init; }
        public int    MaxQty     { get; init; }
    }
}
