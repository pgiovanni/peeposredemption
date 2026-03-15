using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.Features.Artists.Queries;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.API.Pages.App.Admin;

public class ArtistAdminModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly string _adminEmail;

    public ArtistAdminModel(IMediator mediator, IConfiguration config)
    {
        _mediator = mediator;
        _adminEmail = config["Email:AdminEmail"] ?? string.Empty;
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

    private bool IsAdmin()
    {
        var emailClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        return !string.IsNullOrEmpty(_adminEmail) &&
               string.Equals(emailClaim, _adminEmail, StringComparison.OrdinalIgnoreCase);
    }
}
