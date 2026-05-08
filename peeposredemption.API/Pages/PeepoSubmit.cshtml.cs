using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Domain.Entities;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.API.Pages;

[AllowAnonymous]
public class PeepoSubmitModel : PageModel
{
    private readonly AppDbContext _db;

    public PeepoSubmitModel(AppDbContext db)
    {
        _db = db;
    }

    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    // Repopulate form on validation error
    public string FormName { get; set; } = string.Empty;
    public string FormImageUrl { get; set; } = string.Empty;
    public string? FormSubmitterName { get; set; }
    public string? FormNote { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string name, string imageUrl, string? submitterName, string? note)
    {
        name = name?.Trim() ?? string.Empty;
        imageUrl = imageUrl?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(name))
        {
            ErrorMessage = "Peepo name is required.";
            FormName = name; FormImageUrl = imageUrl; FormSubmitterName = submitterName; FormNote = note;
            return Page();
        }

        if (string.IsNullOrEmpty(imageUrl) || !Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "https" && uri.Scheme != "http"))
        {
            ErrorMessage = "Please enter a valid image URL.";
            FormName = name; FormImageUrl = imageUrl; FormSubmitterName = submitterName; FormNote = note;
            return Page();
        }

        _db.PeepoSubmissions.Add(new PeepoSubmission
        {
            Name = name,
            ImageUrl = imageUrl,
            SubmitterName = string.IsNullOrWhiteSpace(submitterName) ? null : submitterName.Trim(),
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        });

        await _db.SaveChangesAsync();

        Success = true;
        return Page();
    }
}
