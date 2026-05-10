using peeposredemption.Domain.Entities;

namespace peeposredemption.Application.Features.Game.Commands;

/// <summary>
/// Full 12-element advantage table.
///
///   S = Strong  (weapon 1.25×, magic 1.5×)
///   R = Resisted (0.75×)
///   M = Mutual  (both sides are Strong vs each other)
///   . = Neutral (1.0×)
///   Same element = Resisted (0.75×)
///
///            Fr  Ic  Lt  Er  Wa  Wi  Ho  Dk  Li  Sh  Po  Vo
///   Fire      .   S   .   M   R   R   .   .   .   .   .   .
///   Ice       R   .   R   .   S   S   .   .   .   .   .   .
///   Lightning .   S   .   R   S   R   .   .   .   .   .   .
///   Earth     M   R   S   .   R   .   .   .   .   .   .   .
///   Water     S   R   R   S   .   .   .   .   .   .   .   .
///   Wind      R   R   S   S   .   .   .   .   .   .   .   .
///   Holy      .   .   .   .   .   .   .   M   .   M   R   .
///   Dark      .   .   .   .   .   .   M   .   M   .   .   R
///   Light     .   .   .   .   .   .   .   M   .   M   .   R
///   Shadow    R   .   .   .   .   .   M   .   M   .   .   .
///   Poison    R   .   .   S   S   .   R   .   .   .   .   .
///   Void      .   .   .   .   .   .   .   .   .   .   .   .
/// </summary>
public static class ElementSystem
{
    // ── Damage multiplier ─────────────────────────────────────────────────────
    // isMagic = true  → strong = 1.5×
    // isMagic = false → strong = 1.25×
    // Resist / same element = 0.75× either way

    public static double GetMultiplier(Element attacker, Element defender, bool isMagic = false)
    {
        if (attacker == Element.None || defender == Element.None) return 1.0;

        // Same element is always resisted
        if (attacker == defender) return 0.75;

        double strongMult = isMagic ? 1.5 : 1.25;

        // Mutual: both sides are strong vs each other
        if (IsMutual(attacker, defender)) return strongMult;
        if (IsMutual(defender, attacker)) return strongMult;  // symmetric, but be explicit

        if (IsStrong(attacker, defender)) return strongMult;
        if (IsStrong(defender, attacker)) return 0.75;        // defender is strong vs attacker → resisted

        return 1.0;
    }

    // Strong (one-directional)
    private static bool IsStrong(Element a, Element d) => (a, d) switch
    {
        (Element.Fire,      Element.Ice)       => true,
        (Element.Ice,       Element.Water)     => true,
        (Element.Ice,       Element.Wind)      => true,
        (Element.Lightning, Element.Ice)       => true,
        (Element.Lightning, Element.Water)     => true,
        (Element.Earth,     Element.Lightning) => true,
        (Element.Water,     Element.Fire)      => true,
        (Element.Water,     Element.Earth)     => true,
        (Element.Wind,      Element.Lightning) => true,
        (Element.Wind,      Element.Earth)     => true,
        (Element.Poison,    Element.Earth)     => true,
        (Element.Poison,    Element.Water)     => true,
        _ => false
    };

    // Mutual: both deal strong damage to each other
    private static bool IsMutual(Element a, Element b)
    {
        var pair = (a, b);
        return pair == (Element.Fire,   Element.Earth)  ||
               pair == (Element.Earth,  Element.Fire)   ||
               pair == (Element.Holy,   Element.Dark)   ||
               pair == (Element.Dark,   Element.Holy)   ||
               pair == (Element.Holy,   Element.Shadow) ||
               pair == (Element.Shadow, Element.Holy)   ||
               pair == (Element.Dark,   Element.Light)  ||
               pair == (Element.Light,  Element.Dark)   ||
               pair == (Element.Light,  Element.Shadow) ||
               pair == (Element.Shadow, Element.Light);
    }

    // ── Status effect mapping ─────────────────────────────────────────────────

    public static string? GetElementalStatusEffect(Element element) => element switch
    {
        Element.Fire      => "Burn",
        Element.Ice       => "Freeze",
        Element.Lightning => "Shock",
        Element.Earth     => "DefenseDown",
        Element.Water     => "MpDrain",
        Element.Wind      => "Windblast",
        Element.Dark      => "Blind",
        Element.Holy      => "Regeneration",
        Element.Light     => "Blind",
        Element.Shadow    => "Confusion",
        Element.Poison    => "Poison",
        Element.Void      => "Corrupt",
        _ => null
    };

    public static (int Strength, int Turns) GetStatusParams(Element element) => element switch
    {
        Element.Fire      => (8,  3),
        Element.Ice       => (5,  2),
        Element.Lightning => (6,  2),
        Element.Earth     => (5,  3),
        Element.Water     => (6,  3),
        Element.Wind      => (0,  1),   // Windblast: no strength, just skip turn
        Element.Dark      => (3,  3),
        Element.Holy      => (12, 3),
        Element.Light     => (3,  3),
        Element.Shadow    => (0,  3),   // Confusion: no strength
        Element.Poison    => (4,  4),
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
        Element.Water     => "Aqua Essence",
        Element.Wind      => "Gale Feather",
        Element.Dark      => "Dark Essence",
        Element.Holy      => "Holy Dust",
        Element.Light     => "Light Crystal",
        Element.Shadow    => "Shadow Wisp",
        Element.Poison    => "Venom Sac",
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
        Element.Water     => "💧",
        Element.Wind      => "🌪️",
        Element.Dark      => "🌑",
        Element.Holy      => "✨",
        Element.Light     => "🌟",
        Element.Shadow    => "👤",
        Element.Poison    => "☠️",
        Element.Void      => "🌀",
        _ => ""
    };

    public static string GetEnchantPrefix(Element element) => element switch
    {
        Element.Fire      => "Blazing",
        Element.Ice       => "Glacial",
        Element.Lightning => "Storming",
        Element.Earth     => "Earthen",
        Element.Water     => "Tidal",
        Element.Wind      => "Gale",
        Element.Dark      => "Shadow",
        Element.Holy      => "Radiant",
        Element.Light     => "Luminous",
        Element.Shadow    => "Umbral",
        Element.Poison    => "Venomous",
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

    // ── Spell tier definitions ────────────────────────────────────────────────
    // Tier 1: fixed base, no INT scale.  Tier 2-4: base + INT × scale.

    public record SpellDef(
        string Name,
        Element Element,
        int Tier,
        int MpCost,
        int BaseDamage,
        float IntScale,    // 0 = no INT scaling
        int LevelReq,
        bool IsHeal = false);   // true = heals player instead of damaging monster

    public static readonly IReadOnlyList<SpellDef> Spells = BuildSpells();

    private static List<SpellDef> BuildSpells()
    {
        var list = new List<SpellDef>();

        // Explicit names per tier — must match the bot's SPELLS list exactly
        // (Element, t1, t2, t3, t4, base1, base2, base3, base4)
        var defs = new (Element el, string t1, string t2, string t3, string t4, int b1, int b2, int b3, int b4)[]
        {
            (Element.Fire,      "Fire",    "Fira",     "Firaga",    "Firaja",    18, 28, 42, 60),
            (Element.Ice,       "Blizzard","Blizzara",  "Blizzaga",  "Blizzaja",  16, 26, 40, 58),
            (Element.Lightning, "Thunder", "Thundera",  "Thunderga", "Thunderja", 20, 32, 48, 68),
            (Element.Earth,     "Quake",   "Quakera",   "Quakega",   "Quakeja",   14, 24, 38, 54),
            (Element.Water,     "Water",   "Watera",    "Waterga",   "Waterja",   15, 25, 39, 56),
            (Element.Wind,      "Aero",    "Aerora",    "Aeroga",    "Aeroja",    13, 22, 35, 50),
            (Element.Dark,      "Dark",    "Darkra",    "Darkga",    "Darkja",    17, 28, 44, 64),
            (Element.Holy,      "Holy",    "Holra",     "Holga",     "Holja",     16, 26, 40, 58),
            (Element.Light,     "Flash",   "Flashra",   "Flashga",   "Flashja",   15, 24, 38, 54),
            (Element.Shadow,    "Shadow",  "Shadowra",  "Shadowga",  "Shadowja",  17, 27, 42, 60),
            (Element.Poison,    "Bio",     "Biora",     "Bioga",     "Bioja",     12, 20, 32, 48),
            (Element.Void,      "Void",    "Voidra",    "Voidga",    "Voidja",    22, 36, 55, 80),
        };

        int[] mpCosts     = { 7, 25, 55, 90 };
        float[] intScales = { 0f, 0.5f, 1.0f, 2.0f };
        int[] levelReqs   = { 1, 10, 25, 50 };

        foreach (var (el, t1, t2, t3, t4, b1, b2, b3, b4) in defs)
        {
            string[] names   = { t1, t2, t3, t4 };
            int[]    elmBases = { b1, b2, b3, b4 };
            for (int t = 0; t < 4; t++)
            {
                list.Add(new SpellDef(
                    names[t], el, t + 1,
                    mpCosts[t], elmBases[t], intScales[t],
                    levelReqs[t]));
            }
        }

        // Healing spells (Holy element, heal instead of damage)
        list.Add(new SpellDef("Cure",    Element.Holy, 1, 8,  0, 2.0f, 1,  IsHeal: true));
        list.Add(new SpellDef("Cura",    Element.Holy, 2, 22, 0, 4.0f, 12, IsHeal: true));
        list.Add(new SpellDef("Curaga",  Element.Holy, 3, 50, 0, 7.0f, 28, IsHeal: true));
        list.Add(new SpellDef("Curaja",  Element.Holy, 4, 85, 0, 12f,  55, IsHeal: true));

        return list;
    }

    public static SpellDef? FindSpell(string name) =>
        Spells.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
