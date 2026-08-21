namespace peeposredemption.API.Infrastructure;

public record ServicePackage(
    string Name,
    string Icon,
    string Price,
    string? PriceNote,
    string Blurb,
    string[] Features,
    bool IsSubscription,
    bool IsBotAddon = false);

/// <summary>
/// The sellable service catalog. Rendered on /Packages and used for the
/// package dropdown on /Contact — edit packages and prices here only.
/// </summary>
public static class ServiceCatalog
{
    public static readonly ServicePackage[] Packages =
    {
        // ── Monthly subscriptions ────────────────────────────────────
        new(
            "Managed Sysadmin — Essentials",
            "🖥️",
            "$199/mo",
            "cancel anytime",
            "Your systems, looked after — so small problems never become big ones.",
            new[]
            {
                "Monitoring & alerting on your servers and services",
                "Patches and updates applied on a schedule",
                "Backup checks — verified, not assumed",
                "Email support with next-business-day response",
                "Monthly health report in plain English"
            },
            IsSubscription: true),
        new(
            "Managed Sysadmin — Business",
            "🏢",
            "$449/mo",
            "cancel anytime",
            "Everything in Essentials, plus the response times a business actually needs.",
            new[]
            {
                "Everything in Essentials",
                "Priority same-day response",
                "User & access management (onboarding/offboarding)",
                "Security hardening and account audits",
                "Quarterly planning review"
            },
            IsSubscription: true),
        new(
            "Managed Cloud Hosting — your cloud account",
            "☁️",
            "from $99/mo",
            "+ one-time setup",
            "Hosting on YOUR AWS, Azure, or GCP account. You own the infrastructure and the bill — it just runs like someone's paid to care, because someone is.",
            new[]
            {
                "Setup, deployment, SSL, and DNS on your cloud account",
                "Monitoring, updates, and incident response",
                "Backups configured and tested",
                "Monthly cost review — no surprise cloud bills",
                "No lock-in: it's your account, always"
            },
            IsSubscription: true),
        new(
            "Discord AI Add-on — Torvex Forerunner",
            "🤖",
            "$15/mo",
            "per server · cancel anytime",
            "AI chat for your Discord server: members talk to the Torvex Forerunner bot with /ask or by pinging it. Metered fairly — the bot's core features stay free.",
            new[]
            {
                "/ask and ping-to-chat answers in your server",
                "Daily free energy for every member — no per-user fees",
                "Hard monthly budget ceiling — costs can't run away",
                "Privacy-scoped: private and staff channels are never read",
                "Requires the free Torvex Forerunner bot"
            },
            IsSubscription: true,
            IsBotAddon: true),

        // ── One-off projects ─────────────────────────────────────────
        new(
            "Network Install & Wi-Fi",
            "🌐",
            "from $499",
            "hardware billed at cost",
            "An office network done properly: coverage where you need it, security by default, documentation you can hand to the next person.",
            new[]
            {
                "Site survey and equipment recommendation",
                "Router, switch, AP, and VPN setup",
                "Guest and staff network separation",
                "Remote access configured securely",
                "Full documentation of what was built"
            },
            IsSubscription: false),
        new(
            "ERP Customization & Integration",
            "⚙️",
            "from $1,500",
            "scoped per project",
            "Your ERP, made to fit how you actually work — custom reports, integrations, and automations on the system you already own.",
            new[]
            {
                "Custom reports and dashboards",
                "Integrations with the tools around your ERP",
                "Workflow automation for repetitive entry",
                "Data cleanup and migrations",
                "Training for your team on what changed"
            },
            IsSubscription: false),
        new(
            "Custom Software / Website Build",
            "🧩",
            "from $2,500",
            "scoped per project",
            "Internal tools, dashboards, and websites built around your business — delivered hosting-ready with handoff documentation.",
            new[]
            {
                "Scoped, fixed-price build — no hourly creep",
                "Web app, internal tool, or business website",
                "Hosting-ready (pairs with Managed Cloud Hosting)",
                "Source code and documentation are yours",
                "30 days of post-launch fixes included"
            },
            IsSubscription: false),
        new(
            "Discord Community Setup",
            "🛡️",
            "from $199",
            "one-time",
            "A community server built by someone who runs one: structure, moderation, verification, and bots that actually work.",
            new[]
            {
                "Channel and role structure designed for your community",
                "Moderation and anti-raid tooling configured",
                "Member verification and alt protection",
                "Custom bot setup and automation",
                "Staff onboarding notes included"
            },
            IsSubscription: false),
    };

    public static IEnumerable<string> Names => Packages.Select(p => p.Name);

    public static bool IsValidName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && Packages.Any(p => p.Name == name);
}
