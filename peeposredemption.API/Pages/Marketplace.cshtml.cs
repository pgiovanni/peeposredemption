using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.API.Pages;

[AllowAnonymous]
public class MarketplaceModel : PageModel
{
    private readonly AppDbContext _db;
    public MarketplaceModel(AppDbContext db) => _db = db;

    public const int PageSize = 30;

    public List<ListingDto>  Listings    { get; set; } = new();
    public int               TotalCount  { get; set; }
    public int               CurrentPage { get; set; }
    public int               TotalPages  { get; set; }
    public string            Search      { get; set; } = "";
    public string            Currency    { get; set; } = "All";
    public string            Sort        { get; set; } = "newest";

    public async Task OnGetAsync(int page = 1, string search = "", string currency = "All", string sort = "newest")
    {
        CurrentPage = Math.Max(1, page);
        Search      = search.Trim();
        Currency    = currency;
        Sort        = sort;

        var now = DateTime.UtcNow;
        var query = _db.MarketplaceListings
            .Include(l => l.ItemDefinition)
            .Include(l => l.Seller).ThenInclude(s => s.User)
            .Where(l => l.Status == MarketListingStatus.Active && l.ExpiresAt > now);

        if (!string.IsNullOrEmpty(Search))
            query = query.Where(l => l.ItemDefinition.Name.ToLower().Contains(Search.ToLower()));

        if (currency == "Orbs")
            query = query.Where(l => l.CurrencyType == MarketplaceCurrencyType.Orbs);
        else if (currency == "Coins")
            query = query.Where(l => l.CurrencyType == MarketplaceCurrencyType.Coins);

        query = sort switch
        {
            "price_asc"  => query.OrderBy(l => l.PricePerUnit),
            "price_desc" => query.OrderByDescending(l => l.PricePerUnit),
            _            => query.OrderByDescending(l => l.CreatedAt)
        };

        TotalCount = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
        CurrentPage = Math.Min(CurrentPage, Math.Max(1, TotalPages));

        Listings = (await query
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync())
            .Select(l => new ListingDto
            {
                ItemName     = l.ItemDefinition.Name,
                ItemIcon     = l.ItemDefinition.Icon,
                ItemRarity   = l.ItemDefinition.Rarity.ToString(),
                ItemType     = l.ItemDefinition.Type.ToString(),
                ItemSlug     = WikiModel.Slugify(l.ItemDefinition.Name),
                SellerName   = l.Seller.User?.DisplayOrUsername ?? l.Seller.CharacterName,
                Quantity     = l.Quantity,
                PricePerUnit = l.PricePerUnit,
                TotalPrice   = l.PricePerUnit * l.Quantity,
                Currency     = l.CurrencyType == MarketplaceCurrencyType.Orbs ? "Orbs" : "Coins",
                CurrencyIcon = l.CurrencyType == MarketplaceCurrencyType.Orbs ? "🔮" : "🪙",
                ExpiresAt    = l.ExpiresAt,
                CreatedAt    = l.CreatedAt,
            }).ToList();
    }

    public record ListingDto
    {
        public string   ItemName     { get; init; } = "";
        public string   ItemIcon     { get; init; } = "";
        public string   ItemRarity   { get; init; } = "";
        public string   ItemType     { get; init; } = "";
        public string   ItemSlug     { get; init; } = "";
        public string   SellerName   { get; init; } = "";
        public int      Quantity     { get; init; }
        public long     PricePerUnit { get; init; }
        public long     TotalPrice   { get; init; }
        public string   Currency     { get; init; } = "";
        public string   CurrencyIcon { get; init; } = "";
        public DateTime ExpiresAt    { get; init; }
        public DateTime CreatedAt    { get; init; }
    }

    public string TimeLeftStr(DateTime expiresAt) => TimeLeft(expiresAt);

    public static string TimeLeft(DateTime expiresAt)
    {
        var diff = expiresAt - DateTime.UtcNow;
        if (diff.TotalDays >= 1)  return $"{(int)diff.TotalDays}d left";
        if (diff.TotalHours >= 1) return $"{(int)diff.TotalHours}h left";
        return $"{(int)diff.TotalMinutes}m left";
    }
}
