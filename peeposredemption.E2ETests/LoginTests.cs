using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace peeposredemption.E2ETests;

[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class LoginTests : PageTest
{
    private string _loginUrl = null!;

    [SetUp]
    public void SetUp()
    {
        _loginUrl = $"{TestConfig.BaseUrl}/Auth/Login";
    }

    // ── Happy path ──────────────────────────────────────────────────────────

    [Test]
    public async Task ValidCredentials_RedirectsToApp()
    {
        if (string.IsNullOrEmpty(TestConfig.TestEmail))
            Assert.Ignore("E2E_TEST_EMAIL not configured — skipping.");

        await Page.GotoAsync(_loginUrl);
        await Page.FillAsync("input[type='email']", TestConfig.TestEmail);
        await Page.FillAsync("input[type='password']", TestConfig.TestPassword);
        await Page.ClickAsync("button[type='submit']");

        // Should end up inside the app, not back on the login page
        await Page.WaitForURLAsync(url => !url.Contains("/Auth/Login"), new() { Timeout = 10_000 });
        Assert.That(Page.Url, Does.Not.Contain("/Auth/Login"));
    }

    [Test]
    public async Task ValidLogin_SetsCookieAndJwtMeta()
    {
        if (string.IsNullOrEmpty(TestConfig.TestEmail))
            Assert.Ignore("E2E_TEST_EMAIL not configured — skipping.");

        await Page.GotoAsync(_loginUrl);
        await Page.FillAsync("input[type='email']", TestConfig.TestEmail);
        await Page.FillAsync("input[type='password']", TestConfig.TestPassword);
        await Page.ClickAsync("button[type='submit']");

        await Page.WaitForURLAsync(url => !url.Contains("/Auth/Login"), new() { Timeout = 10_000 });

        // JWT meta tag should be populated on the next page
        var jwt = await Page.GetAttributeAsync("meta[name='jwt']", "content");
        Assert.That(jwt, Is.Not.Null.And.Not.Empty, "JWT meta tag should be populated after login");
    }

    // ── Error cases ──────────────────────────────────────────────────────────

    [Test]
    public async Task WrongPassword_ShowsErrorAndStaysOnLoginPage()
    {
        await Page.GotoAsync(_loginUrl);
        await Page.FillAsync("input[type='email']", "nonexistent@example.com");
        await Page.FillAsync("input[type='password']", "wrongpassword123");
        await Page.ClickAsync("button[type='submit']");

        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        Assert.That(Page.Url, Does.Contain("/Auth/Login"), "Should stay on login page after wrong credentials");

        var body = await Page.InnerTextAsync("body");
        Assert.That(body, Does.Contain("Invalid").Or.Contain("incorrect").Or.Contain("attempt"),
            "Should show an error message");
    }

    [Test]
    public async Task EmptyForm_ShowsValidationErrors()
    {
        await Page.GotoAsync(_loginUrl);
        await Page.ClickAsync("button[type='submit']");

        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        Assert.That(Page.Url, Does.Contain("/Auth/Login"));

        // Browser or server-side validation should fire
        var emailInput = Page.Locator("input[type='email']");
        var isRequired = await emailInput.GetAttributeAsync("required");
        var serverError = Page.Locator(".text-danger");

        var hasClientValidation = isRequired != null;
        var hasServerError = await serverError.CountAsync() > 0;

        Assert.That(hasClientValidation || hasServerError,
            "Should have client-side required attribute or server-side validation error");
    }

    [Test]
    public async Task LoginPage_HasNoIndexMetaTag()
    {
        await Page.GotoAsync(_loginUrl);
        var robots = await Page.GetAttributeAsync("meta[name='robots']", "content");
        Assert.That(robots, Is.Not.Null, "Login page should have robots meta tag");
        Assert.That(robots, Does.Contain("noindex"), "Login page should not be indexed");
    }

    [Test]
    public async Task RateLimiting_LocksOutAfterFiveFailedAttempts()
    {
        // Use a unique email per run so the counter starts fresh
        var uniqueEmail = $"ratelimit-{Guid.NewGuid():N}@example.com";

        for (int i = 0; i < 5; i++)
        {
            await Page.GotoAsync(_loginUrl);
            await Page.FillAsync("input[type='email']", uniqueEmail);
            await Page.FillAsync("input[type='password']", "wrongpassword");
            await Page.ClickAsync("button[type='submit']");
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        }

        // 6th attempt — should see lockout message
        await Page.GotoAsync(_loginUrl);
        await Page.FillAsync("input[type='email']", uniqueEmail);
        await Page.FillAsync("input[type='password']", "wrongpassword");
        await Page.ClickAsync("button[type='submit']");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        var body = await Page.InnerTextAsync("body");
        Assert.That(body, Does.Contain("locked").Or.Contain("too many").Or.Contain("15 min")
            .Or.Contain("Too many"),
            "Should show lockout message after 5 failed attempts");
    }

    // ── Navigation ───────────────────────────────────────────────────────────

    [Test]
    public async Task LoginPage_HasLinkToRegister()
    {
        await Page.GotoAsync(_loginUrl);
        var registerLink = Page.Locator("a[href*='/Auth/Register'], a[href*='Register']");
        Assert.That(await registerLink.CountAsync(), Is.GreaterThan(0),
            "Login page should have a link to the register page");
    }

    [Test]
    public async Task UnauthenticatedUser_RedirectsToLogin()
    {
        // Accessing a protected page without a session should redirect to login
        await Page.GotoAsync($"{TestConfig.BaseUrl}/App/Index");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        Assert.That(Page.Url, Does.Contain("/Auth/Login"),
            "Protected page should redirect unauthenticated users to login");
    }
}
