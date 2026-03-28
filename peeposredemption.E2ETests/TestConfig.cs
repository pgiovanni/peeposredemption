namespace peeposredemption.E2ETests;

/// <summary>
/// Central config — override via environment variables in CI or locally.
/// </summary>
public static class TestConfig
{
    /// <summary>Base URL of the app under test. Default: production.</summary>
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "https://torvex.app";

    /// <summary>A real account that exists in the DB and has email confirmed.</summary>
    public static string TestEmail =>
        Environment.GetEnvironmentVariable("E2E_TEST_EMAIL") ?? "";

    public static string TestPassword =>
        Environment.GetEnvironmentVariable("E2E_TEST_PASSWORD") ?? "";

    /// <summary>An email that does NOT exist in the DB — used for registration tests.</summary>
    public static string NewUserEmail =>
        Environment.GetEnvironmentVariable("E2E_NEW_EMAIL")
        ?? $"e2e-{Guid.NewGuid():N}@mailinator.com";

    public static string NewUserPassword => "E2eTest@99!";
    public static string NewUsername => $"e2euser{Guid.NewGuid().ToString("N")[..8]}";
}
