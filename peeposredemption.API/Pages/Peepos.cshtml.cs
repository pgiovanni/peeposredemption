using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.API.Pages;

public class PeepoViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string CurrentRarity { get; set; } = string.Empty;
    public long BuyPrice { get; set; }
    public long SellPrice { get; set; }
    public Dictionary<string, int> VoteCounts { get; set; } = new();
    public string? MyVote { get; set; }
}

public class PeeposModel : PageModel
{
    private readonly AppDbContext _db;

    public PeeposModel(AppDbContext db)
    {
        _db = db;
    }

    public List<PeepoViewModel> Peepos { get; set; } = new();

    public async Task OnGetAsync()
    {
        var ip = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault()
              ?? HttpContext.Connection.RemoteIpAddress?.ToString()
              ?? "unknown";

        var items = await _db.ItemDefinitions
            .Where(i => i.Type == GameItemType.Collectible && i.SubType == ItemSubType.Peepo)
            .OrderBy(i => i.Rarity)
            .ThenBy(i => i.Name)
            .ToListAsync();

        var allVotes = await _db.PeepoRarityVotes
            .GroupBy(v => new { v.PeepoName, v.VotedRarity })
            .Select(g => new { g.Key.PeepoName, g.Key.VotedRarity, Count = g.Count() })
            .ToListAsync();

        var myVotes = await _db.PeepoRarityVotes
            .Where(v => v.IpAddress == ip)
            .ToDictionaryAsync(v => v.PeepoName, v => v.VotedRarity);

        Peepos = items.Select(item =>
        {
            var voteCounts = allVotes
                .Where(v => v.PeepoName == item.Name)
                .ToDictionary(v => v.VotedRarity, v => v.Count);

            myVotes.TryGetValue(item.Name, out var myVote);

            return new PeepoViewModel
            {
                Name = item.Name,
                Icon = item.Icon,
                CurrentRarity = item.Rarity.ToString(),
                BuyPrice = item.BuyPrice,
                SellPrice = item.SellPrice,
                VoteCounts = voteCounts,
                MyVote = myVote
            };
        }).ToList();
    }
}
