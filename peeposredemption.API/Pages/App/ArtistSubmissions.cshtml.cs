using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App;

public class ArtistSubmissionsModel : PageModel
{
    private readonly IUnitOfWork _uow;
    private readonly string _adminEmail;

    public ArtistSubmissionsModel(IUnitOfWork uow, IConfiguration config)
    {
        _uow = uow;
        _adminEmail = config["Email:AdminEmail"] ?? string.Empty;
    }

    public List<ArtistSubmission> Submissions { get; set; } = new();
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!IsAdmin()) return Forbid();

        Submissions = await _uow.ArtistSubmissions.GetAllAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid submissionId)
    {
        if (!IsAdmin()) return Forbid();

        var submission = await _uow.ArtistSubmissions.GetByIdAsync(submissionId);
        if (submission == null) return NotFound();

        submission.Status = SubmissionStatus.Approved;
        await _uow.SaveChangesAsync();

        StatusMessage = $"Approved application from {submission.DisplayName}.";
        Submissions = await _uow.ArtistSubmissions.GetAllAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid submissionId)
    {
        if (!IsAdmin()) return Forbid();

        var submission = await _uow.ArtistSubmissions.GetByIdAsync(submissionId);
        if (submission == null) return NotFound();

        submission.Status = SubmissionStatus.Rejected;
        await _uow.SaveChangesAsync();

        StatusMessage = $"Rejected application from {submission.DisplayName}.";
        Submissions = await _uow.ArtistSubmissions.GetAllAsync();
        return Page();
    }

    private bool IsAdmin()
    {
        var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
        return !string.IsNullOrEmpty(_adminEmail) &&
               string.Equals(emailClaim, _adminEmail, StringComparison.OrdinalIgnoreCase);
    }
}
