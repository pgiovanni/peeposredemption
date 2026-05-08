using peeposredemption.Domain.Entities;

namespace peeposredemption.Application.Features.Game.Commands;

/// <summary>
/// Element advantage table and helpers.
/// Cycle: Fire > Ice > Earth > Lightning > Fire (each strong against the next)
/// Counter: Dark ↔ Holy (both strong against each other: 1.5×)
/// Void: neutral vs all except Holy (weak to Holy 0.75×); Holy is NOT weak to Void (Void has no advantage counter).
/// Advantage = 1.5×, Disadvantage = 0.75×, Neutral = 1.0×
/// </summary>
public static class ElementSystem
{
    // ── Damage multiplier ─────────────────────────────────────────────────────

    public static double GetMultiplier(Element attacker, Element defender)
    {
        if (attacker == Element.None || defender == Element.None) return 1.0;
        if (attacker == defender) return 1.0;

        if (IsStrong(attacker, defender)) return 1.5;
        if (IsStrong(defender, attacker)) return 0.75;

        // Dark ↔ Holy both deal 1.5× to each other
        if ((attacker == Element.Dark && defender == Element.Holy) ||
            (attacker == Element.Holy && defender == Element.Dark))
            return 1.5;

        // Holy strong vs Void
        if (attacker == Element.Holy && defender == Element.Void) return 1.5;
        if (attacker == Element.Void && defender == Element.Holy) return 0.75;

        return 1.0;
    }

    private static bool IsStrong(Element a, Element d) => (a, d) switch
    {
        // Fire > Ice > Earth > Lightning > Fire
        (Element.Fire,      Element.Ice)       => true,
        (Element.Ice,       Element.Earth)      => true,
        (Element.Earth,     Element.Lightning)  => true,
        (Element.Lightning, Element.Fire)       => true,
        _ => false
    };

    // ── Status effect mapping ─────────────────────────────────────────────────

    public static string? GetElementalStatusEffect(Element element) => element switch
    {
        Element.Fire      => "Burn",
        Element.Ice       => "Freeze",
        Element.Lightning => "Shock",
        Element.Earth     => "DefenseDown",
        Element.Dark      => "Blind",
        Element.Holy      => "Regeneration",
        Element.Void      => "Corrupt",
        _ => null
    };

    public static (int Strength, int Turns) GetStatusParams(Element element) => element switch
    {
        Element.Fire      => (8,  3),
        Element.Ice       => (5,  2),
        Element.Lightning => (6,  2),
        Element.Earth     => (5,  3),
        Element.Dark      => (3,  3),
        Element.Holy      => (12, 3),
        Element.Void      => (5,  4),
        _ => (0, 0)
    };

    // ── Elemental material drops ──────────────────────────────────────────────

    public static string? GetElementalMaterial(Element element) => element switch
    {
        Element.Fire      => "Ember Shard",
        Element.Ice       => "Frost Crystal",
        Element.Lightning => "Storm Essence",
        Element.Earth     => "Stone Fragment",
        Element.Dark      => "Dark Essence",
        Element.Holy      => "Holy Dust",
        Element.Void      => "Void Fragment",
        _ => null
    };

    // ── Display helpers ───────────────────────────────────────────────────────

    public static string GetElementIcon(Element element) => element switch
    {
        Element.Fire      => "🔥",
        Element.Ice       => "🧊",
        Element.Lightning => "⚡",
        Element.Earth     => "🌍",
        Element.Dark      => "🌑",
        Element.Holy      => "✨",
        Element.Void      => "🌀",
        _ => ""
    };

    public static string GetEnchantPrefix(Element element) => element switch
    {
        Element.Fire      => "Blazing",
        Element.Ice       => "Glacial",
        Element.Lightning => "Storming",
        Element.Earth     => "Earthen",
        Element.Dark      => "Shadow",
        Element.Holy      => "Radiant",
        Element.Void      => "Void-Touched",
        _ => element.ToString()
    };

    public static string TierToRoman(int tier) => tier switch
    {
        1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V", _ => tier.ToString()
    };

    /// <summary>Generate the display name for an enchant slot e.g. "Blazing II".</summary>
    public static string MakeEnchantName(Element element, int tier)
        => $"{GetEnchantPrefix(element)} {TierToRoman(tier)}";

    /// <summary>Bonus stat per tier (additive damage/resistance bonus).</summary>
    public static int BonusForTier(int tier) => tier switch
    {
        1 => 10, 2 => 22, 3 => 38, 4 => 58, 5 => 85, _ => 10
    };
}
