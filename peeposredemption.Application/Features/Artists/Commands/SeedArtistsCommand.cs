using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Artists.Commands;

public record SeedArtistsCommand : IRequest;

public class SeedArtistsCommandHandler : IRequestHandler<SeedArtistsCommand>
{
    private readonly IUnitOfWork _uow;
    public SeedArtistsCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(SeedArtistsCommand cmd, CancellationToken ct)
    {
        if (await _uow.Artists.CountAsync() > 0) return;

        var artists = new List<Artist>
        {
            new()
            {
                DisplayName = "Artist 1",
                Bio = "Illustrator — static art, borders, banners",
                PayoutEmail = "artist1@placeholder.com",
                PayoutMethod = PayoutMethod.PayPal
            },
            new()
            {
                DisplayName = "Artist 2",
                Bio = "Illustrator — static art, badges, backgrounds",
                PayoutEmail = "artist2@placeholder.com",
                PayoutMethod = PayoutMethod.PayPal
            }
        };

        foreach (var artist in artists)
            await _uow.Artists.AddAsync(artist);

        await _uow.SaveChangesAsync();
    }
}
