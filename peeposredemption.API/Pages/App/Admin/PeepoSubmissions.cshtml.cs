using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using peeposredemption.API.Infrastructure;
using peeposredemption.Domain.Entities;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.API.Pages.App.Admin;

public class PeepoSubmissionsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public PeepoSubmissionsModel(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public List<PeepoSubmission> Submissions { get; set; } = new();
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!IsAdmin()) return Forbid();
        Submissions = await _db.PeepoSubmissions.OrderByDescending(s => s.SubmittedAt).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid submissionId)
    {
        if (!IsAdmin()) return Forbid();

        var sub = await _db.PeepoSubmissions.FindAsync(submissionId);
        if (sub == null) return NotFound();

        // Create ItemDefinition if it doesn't already exist
        var exists = await _db.ItemDefinitions.AnyAsync(i =>
            i.Name == sub.Name && i.Type == GameItemType.Collectible && i.SubType == ItemSubType.Peepo);

        if (!exists)
        {
            var rarity = GameItemRarity.Common;
            var (buy, sell, _) = PeepoStats(rarity);
            _db.ItemDefinitions.Add(new ItemDefinition
            {
                Name = sub.Name,
                Description = "A collectible peepo emoji.",
                Type = GameItemType.Collectible,
                SubType = ItemSubType.Peepo,
                Rarity = rarity,
                Icon = sub.ImageUrl,
                BuyPrice = buy,
                SellPrice = sell,
                IsStackable = false,
            });
            StatusMessage = $"Approved \"{sub.Name}\" — added to peepo pool as Common.";
        }
        else
        {
            StatusMessage = $"Approved \"{sub.Name}\" — already exists in the pool (not duplicated).";
        }

        sub.Status = SubmissionStatus.Approved;
        await _db.SaveChangesAsync();

        Submissions = await _db.PeepoSubmissions.OrderByDescending(s => s.SubmittedAt).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid submissionId)
    {
        if (!IsAdmin()) return Forbid();

        var sub = await _db.PeepoSubmissions.FindAsync(submissionId);
        if (sub == null) return NotFound();

        sub.Status = SubmissionStatus.Rejected;
        await _db.SaveChangesAsync();

        StatusMessage = $"Rejected \"{sub.Name}\".";
        Submissions = await _db.PeepoSubmissions.OrderByDescending(s => s.SubmittedAt).ToListAsync();
        return Page();
    }

    private bool IsAdmin() => AdminAuthHelper.IsTorvexOwner(User, _config);

    private static (long buy, long sell, decimal drop) PeepoStats(GameItemRarity r) => r switch
    {
        GameItemRarity.Common    => (250,    100,   0.03m),
        GameItemRarity.Uncommon  => (1500,   500,   0.015m),
        GameItemRarity.Rare      => (10000,  3000,  0.005m),
        GameItemRarity.Epic      => (0,      5000,  0.0015m),
        _                        => (0,      25000, 0.0003m), // Legendary
    };
}
