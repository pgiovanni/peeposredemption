using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.API.Infrastructure;
using peeposredemption.Application.Features.Artists.Queries;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.API.Pages.App.Admin;

public class ArtistAdminModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _config;

    public ArtistAdminModel(IMediator mediator, IConfiguration config)
    {
        _mediator = mediator;
        _config = config;
    }

    public List<ArtistSummaryDto> Artists { get; set; } = new();
    public long GrandTotalEarned { get; set; }
    public long GrandTotalPaid { get; set; }
    public long GrandTotalPending { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!IsAdmin()) return Forbid();

        Artists = await _mediator.Send(new GetAllArtistsQuery());
        GrandTotalEarned = Artists.Sum(a => a.TotalEarnedCents);
        GrandTotalPaid = Artists.Sum(a => a.TotalPaidCents);
        GrandTotalPending = Artists.Sum(a => a.PendingCents);
        return Page();
    }

    private bool IsAdmin() => AdminAuthHelper.IsTorvexOwner(User, _config);
}
