using MediatR;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Artists.Queries;

public record ArtistSummaryDto(
    Guid ArtistId,
    string DisplayName,
    string PayoutEmail,
    string PayoutMethod,
    long TotalEarnedCents,
    long TotalPaidCents,
    long PendingCents);

public record GetAllArtistsQuery : IRequest<List<ArtistSummaryDto>>;

public class GetAllArtistsQueryHandler : IRequestHandler<GetAllArtistsQuery, List<ArtistSummaryDto>>
{
    private readonly IUnitOfWork _uow;
    public GetAllArtistsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<List<ArtistSummaryDto>> Handle(GetAllArtistsQuery query, CancellationToken ct)
    {
        var artists = await _uow.Artists.GetAllAsync();

        return artists.Select(a => new ArtistSummaryDto(
            a.Id,
            a.DisplayName,
            a.PayoutEmail,
            a.PayoutMethod.ToString(),
            a.TotalEarnedCents,
            a.TotalPaidCents,
            a.TotalEarnedCents - a.TotalPaidCents
        )).ToList();
    }
}
