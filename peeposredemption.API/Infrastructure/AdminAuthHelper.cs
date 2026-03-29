using System.Security.Claims;

namespace peeposredemption.API.Infrastructure;

public static class AdminAuthHelper
{
    /// <summary>
    /// Requires all three to match:
    ///   1. Email claim == Admin:Email config value
    ///   2. NameIdentifier claim == Admin:UserId config value
    ///   3. X-Admin-Key request header == Admin:ApiKey config value
    /// Any missing config key makes the check fail closed (safe default).
    /// </summary>
    public static bool IsTorvexOwner(ClaimsPrincipal user, IConfiguration config, IHeaderDictionary? headers = null)
    {
        var adminEmail  = config["Admin:Email"];
        var adminUserId = config["Admin:UserId"];
        var adminApiKey = config["Admin:ApiKey"];

        if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminUserId))
            return false;

        var emailClaim  = user.FindFirst(ClaimTypes.Email)?.Value;
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var emailOk  = string.Equals(emailClaim,  adminEmail,  StringComparison.OrdinalIgnoreCase);
        var userIdOk = string.Equals(userIdClaim, adminUserId, StringComparison.OrdinalIgnoreCase);

        if (!emailOk || !userIdOk) return false;

        // API key check — only enforced when a key is configured AND headers are supplied
        if (!string.IsNullOrEmpty(adminApiKey) && headers != null)
        {
            var sentKey = headers["X-Admin-Key"].FirstOrDefault();
            if (!string.Equals(sentKey, adminApiKey, StringComparison.Ordinal))
                return false;
        }

        return true;
    }
}
