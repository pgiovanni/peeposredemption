using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Artists.Queries;

public record ArtistDashboardDto(
    Guid ArtistId,
    string DisplayName,
    long TotalEarnedCents,
    long TotalPaidCents,
    long PendingCents,
    string PayoutEmail,
    string PayoutMethod,
    List<ArtistItemPerformanceDto> Items,
    List<ArtistCommissionDto> RecentCommissions);

public record ArtistItemPerformanceDto(
    Guid ItemId,
    string Name,
    string Rarity,
    string ItemType,
    long OrbValue,
    int TimesAcquired,
    long TotalCommissionCents);

public record ArtistCommissionDto(
    Guid Id,
    string ItemName,
    long OrbAmount,
    long CommissionCents,
    string Source,
    DateTime CreatedAt);

public record GetArtistDashboardQuery(Guid ArtistId) : IRequest<ArtistDashboardDto?>;

public class GetArtistDashboardQueryHandler : IRequestHandler<GetArtistDashboardQuery, ArtistDashboardDto?>
{
    private readonly IUnitOfWork _uow;
    public GetArtistDashboardQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ArtistDashboardDto?> Handle(GetArtistDashboardQuery query, CancellationToken ct)
    {
        var artist = await _uow.Artists.GetByIdAsync(query.ArtistId);
        if (artist == null) return null;

        var items = await _uow.ArtItems.GetByArtistIdAsync(artist.Id);
        var recentCommissions = await _uow.ArtistCommissions.GetByArtistIdAsync(artist.Id, 50);

        // Build item performance
        var itemPerformance = new List<ArtistItemPerformanceDto>();
        foreach (var item in items)
        {
            var itemCommissions = await _uow.ArtistCommissions.GetByArtItemIdAsync(item.Id);
            itemPerformance.Add(new ArtistItemPerformanceDto(
                item.Id,
                item.Name,
                item.Rarity.ToString(),
                item.ItemType.ToString(),
                item.OrbValue,
                itemCommissions.Count,
                itemCommissions.Sum(c => c.CommissionCents)
            ));
        }

        var commissionDtos = recentCommissions.Select(c =>
        {
            var item = items.FirstOrDefault(i => i.Id == c.ArtItemId);
            return new ArtistCommissionDto(
                c.Id,
                item?.Name ?? "Unknown",
                c.OrbAmount,
                c.CommissionCents,
                c.Source.ToString(),
                c.CreatedAt
            );
        }).ToList();

        return new ArtistDashboardDto(
            artist.Id,
            artist.DisplayName,
            artist.TotalEarnedCents,
            artist.TotalPaidCents,
            artist.TotalEarnedCents - artist.TotalPaidCents,
            artist.PayoutEmail,
            artist.PayoutMethod.ToString(),
            itemPerformance,
            commissionDtos
        );
    }
}
