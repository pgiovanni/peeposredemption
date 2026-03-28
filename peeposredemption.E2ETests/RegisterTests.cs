using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace peeposredemption.E2ETests;

[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class RegisterTests : PageTest
{
    private string _registerUrl = null!;

    [SetUp]
    public void SetUp()
    {
        _registerUrl = $"{TestConfig.BaseUrl}/Auth/Register";
    }

    // ── Happy path ──────────────────────────────────────────────────────────

    [Test]
    public async Task ValidRegistration_RedirectsAwayFromRegisterPage()
    {
        // Turnstile CAPTCHA blocks submission on production — only runs against dev (E2E_BASE_URL=http://localhost:5000).
        if (!TestConfig.BaseUrl.Contains("localhost") && !TestConfig.BaseUrl.Contains("127.0.0.1"))
            Assert.Ignore("Skipped on production — Cloudflare Turnstile requires manual interaction.");

        var email = TestConfig.NewUserEmail;
        var username = TestConfig.NewUsername;

        await Page.GotoAsync(_registerUrl);
        await Page.FillAsync("input[name='Input.Username'], input[placeholder*='sername']", username);
        await Page.FillAsync("input[type='email']", email);
        await Page.FillAsync("input[name='Input.Password'], input[type='password']:first-of-type", TestConfig.NewUserPassword);

        var confirmInput = Page.Locator("input[name='Input.ConfirmPassword'], input[type='password']:nth-of-type(2)");
        if (await confirmInput.CountAsync() > 0)
            await confirmInput.FillAsync(TestConfig.NewUserPassword);

        await Page.ClickAsync("button[type='submit']");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Either redirected to app, or shows "confirm email" page — both mean success
        var url = Page.Url;
        var body = await Page.InnerTextAsync("body");
        var succeeded = !url.Contains("/Auth/Register") || body.Contains("confirm") || body.Contains("email sent");

        Assert.That(succeeded, "Registration should redirect away from the register page or show confirmation message");
    }

    // ── Validation ───────────────────────────────────────────────────────────

    [Test]
    public async Task DuplicateEmail_ShowsError()
    {
        if (string.IsNullOrEmpty(TestConfig.TestEmail))
            Assert.Ignore("E2E_TEST_EMAIL not configured — skipping.");

        await Page.GotoAsync(_registerUrl);
        await Page.FillAsync("input[name='Input.Username'], input[placeholder*='sername']", $"dupetest{Guid.NewGuid():N[..6]}");
        await Page.FillAsync("input[type='email']", TestConfig.TestEmail); // already exists
        await Page.FillAsync("input[name='Input.Password'], input[type='password']", TestConfig.NewUserPassword);

        var confirmInput = Page.Locator("input[name='Input.ConfirmPassword']");
        if (await confirmInput.CountAsync() > 0)
            await confirmInput.FillAsync(TestConfig.NewUserPassword);

        await Page.ClickAsync("button[type='submit']");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        var body = await Page.InnerTextAsync("body");
        Assert.That(body, Does.Contain("already").Or.Contain("taken").Or.Contain("exists").Or.Contain("in use"),
            "Should show error for duplicate email");
    }

    [Test]
    public async Task ShortPassword_ShowsValidationError()
    {
        // Password min-length (8) is enforced server-side via FluentValidation.
        // Requires form submission, which requires CAPTCHA on prod — dev only.
        if (!TestConfig.BaseUrl.Contains("localhost") && !TestConfig.BaseUrl.Contains("127.0.0.1"))
            Assert.Ignore("Skipped on production — requires form submission blocked by Cloudflare Turnstile.");

        await Page.GotoAsync(_registerUrl);
        await Page.FillAsync("input[name='Input.Username'], input[placeholder*='sername']", "testuser99");
        await Page.FillAsync("input[type='email']", "testshortpw@example.com");
        await Page.FillAsync("input[name='Input.Password'], input[type='password']", "abc");

        var confirmInput = Page.Locator("input[name='Input.ConfirmPassword']");
        if (await confirmInput.CountAsync() > 0)
            await confirmInput.FillAsync("abc");

        await Page.ClickAsync("button[type='submit']");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        Assert.That(Page.Url, Does.Contain("/Auth/Register"), "Should stay on register page");

        var body = await Page.InnerTextAsync("body");
        Assert.That(body, Does.Contain("password").Or.Contain("Password").Or.Contain("characters").Or.Contain("minimum"),
            "Should show password validation error");
    }

    [Test]
    public async Task EmptyForm_ShowsValidationErrors()
    {
        await Page.GotoAsync(_registerUrl);
        await Page.ClickAsync("button[type='submit']");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        Assert.That(Page.Url, Does.Contain("/Auth/Register"), "Should stay on register page");

        var errorElements = Page.Locator(".text-danger, [data-valmsg-for], .field-validation-error");
        var emailInput = Page.Locator("input[type='email']");

        var hasServerErrors = await errorElements.CountAsync() > 0;
        var hasClientRequired = await emailInput.GetAttributeAsync("required") != null;

        Assert.That(hasServerErrors || hasClientRequired,
            "Should show validation errors or have required attributes");
    }

    // ── Page quality ─────────────────────────────────────────────────────────

    [Test]
    public async Task RegisterPage_HasNoIndexMetaTag()
    {
        await Page.GotoAsync(_registerUrl);
        var robots = await Page.GetAttributeAsync("meta[name='robots']", "content");
        Assert.That(robots, Is.Not.Null, "Register page should have robots meta tag");
        Assert.That(robots, Does.Contain("noindex"), "Register page should not be indexed");
    }

    [Test]
    public async Task RegisterPage_HasLinkToLogin()
    {
        await Page.GotoAsync(_registerUrl);
        var loginLink = Page.Locator("a[href*='/Auth/Login'], a[href*='Login']");
        Assert.That(await loginLink.CountAsync(), Is.GreaterThan(0),
            "Register page should have a link back to login");
    }
}
