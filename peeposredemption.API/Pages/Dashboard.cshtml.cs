using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.Features.Auth.Commands;
using peeposredemption.Application.Features.Badges.Queries;
using peeposredemption.Application.Features.Users.Commands;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.API.Pages;

public class DashboardModel : PageModel
{
    private static readonly string[] ValidTabs = { "account", "projects", "billing", "orders", "community" };

    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly ILogger<DashboardModel> _logger;

    public DashboardModel(IUnitOfWork uow, IMediator mediator, IEmailService emailService,
        IConfiguration config, ILogger<DashboardModel> logger)
    {
        _uow = uow;
        _mediator = mediator;
        _emailService = emailService;
        _config = config;
        _logger = logger;
    }

    public string Tab { get; set; } = "account";
    public User CurrentUser { get; set; } = null!;
    public CustomerProfile? Contact { get; set; }
    public List<Lead> Orders { get; set; } = new();
    public List<UserBadgeDto> Badges { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? tab = null)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");
        if (!await LoadAsync(userId.Value)) return RedirectToPage("/Auth/Login");

        Tab = ValidTabs.Contains(tab) ? tab! : "account";

        if (Tab == "account")
            Badges = await _mediator.Send(new GetUserBadgesQuery(userId.Value));
        if (Tab == "orders")
            Orders = await _uow.Leads.GetByEmailAsync(CurrentUser.Email);

        return Page();
    }

    // ── Account tab: community/chat identity (same User row the chat edits) ──
    public async Task<IActionResult> OnPostProfileAsync(
        string? displayName, string? bio, string? pronouns, string? profileBackgroundColor,
        IFormFile? avatarFile, IFormFile? bannerFile)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        try
        {
            ProfileImageFile? avatar = avatarFile is { Length: > 0 }
                ? new ProfileImageFile(avatarFile.OpenReadStream(), avatarFile.ContentType, avatarFile.FileName, avatarFile.Length)
                : null;
            ProfileImageFile? banner = bannerFile is { Length: > 0 }
                ? new ProfileImageFile(bannerFile.OpenReadStream(), bannerFile.ContentType, bannerFile.FileName, bannerFile.Length)
                : null;

            await _mediator.Send(new UpdateProfileCommand(
                userId.Value, displayName, bio, pronouns, profileBackgroundColor, avatar, banner));
            TempData["AccountMsg"] = "Profile saved. This is the same profile the chat uses — it's updated everywhere.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            TempData["AccountErr"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard profile save failed for {UserId}", userId);
            TempData["AccountErr"] = "Failed to save profile. Please try again.";
        }

        return RedirectToPage("/Dashboard", new { tab = "account" });
    }

    // ── Account tab: contact & billing details ──────────────────────────────
    public async Task<IActionResult> OnPostContactAsync(
        string? fullName, string? company, string? phone,
        string? addressLine1, string? addressLine2,
        string? city, string? state, string? postalCode, string? country)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        var profile = await _uow.CustomerProfiles.GetByUserIdAsync(userId.Value);
        if (profile == null)
        {
            profile = new CustomerProfile { UserId = userId.Value };
            await _uow.CustomerProfiles.AddAsync(profile);
        }

        static string? Clean(string? s, int max) =>
            string.IsNullOrWhiteSpace(s) ? null : s.Trim()[..Math.Min(s.Trim().Length, max)];

        profile.FullName = Clean(fullName, 150);
        profile.Company = Clean(company, 150);
        profile.Phone = Clean(phone, 40);
        profile.AddressLine1 = Clean(addressLine1, 200);
        profile.AddressLine2 = Clean(addressLine2, 200);
        profile.City = Clean(city, 100);
        profile.State = Clean(state, 100);
        profile.PostalCode = Clean(postalCode, 20);
        profile.Country = Clean(country, 100);
        profile.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        TempData["AccountMsg"] = "Contact & billing details saved.";
        return RedirectToPage("/Dashboard", new { tab = "account" });
    }

    // ── Account tab: email change (pending until the NEW address confirms) ──
    public async Task<IActionResult> OnPostEmailAsync(string? newEmail)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        var user = await _uow.Users.GetByIdAsync(userId.Value);
        if (user == null) return RedirectToPage("/Auth/Login");

        newEmail = newEmail?.Trim();
        if (string.IsNullOrEmpty(newEmail) || !new EmailAddressAttribute().IsValid(newEmail))
        {
            TempData["AccountErr"] = "That email doesn't look valid.";
        }
        else if (string.Equals(newEmail, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            TempData["AccountErr"] = "That's already your email.";
        }
        else if (await _uow.Users.EmailExistsAsync(newEmail))
        {
            TempData["AccountErr"] = "That email is already in use by another account.";
        }
        else
        {
            user.PendingEmail = newEmail;
            user.EmailConfirmationtoken = Guid.NewGuid().ToString("N");
            await _uow.SaveChangesAsync();

            var baseUrl = _config["AppBaseUrl"] ?? "https://torvex.app";
            var link = $"{baseUrl}/Auth/ConfirmEmail?token={user.EmailConfirmationtoken}";
            try
            {
                await _emailService.SendConfirmationEmailAsync(newEmail, link);
                TempData["AccountMsg"] = $"Verification sent to {newEmail}. Your current email keeps working until you confirm the new one.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email-change verification send failed for {UserId}", userId);
                TempData["AccountErr"] = "Couldn't send the verification email. Please try again.";
            }
        }

        return RedirectToPage("/Dashboard", new { tab = "account" });
    }

    // ── Account tab: password change via emailed link ────────────────────────
    public async Task<IActionResult> OnPostPasswordAsync()
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        try
        {
            await _mediator.Send(new RequestPasswordChangeCommand(userId.Value));
            TempData["AccountMsg"] = "Password change link sent to your email.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["AccountErr"] = ex.Message;
        }

        return RedirectToPage("/Dashboard", new { tab = "account" });
    }

    // Customer-friendly wording for internal lead statuses
    public static string CustomerStatus(LeadStatus status) => status switch
    {
        LeadStatus.New => "Received",
        LeadStatus.Contacted => "In contact",
        LeadStatus.Quoted => "Quote sent",
        LeadStatus.Won => "Active",
        LeadStatus.Lost => "Closed",
        _ => status.ToString()
    };

    private async Task<bool> LoadAsync(Guid userId)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null) return false;
        CurrentUser = user;
        Contact = await _uow.CustomerProfiles.GetByUserIdAsync(userId);
        return true;
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim == null ? null : Guid.Parse(claim);
    }
}
