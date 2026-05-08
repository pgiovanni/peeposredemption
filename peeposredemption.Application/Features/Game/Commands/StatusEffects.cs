using System.Text.Json;
using peeposredemption.Domain.Entities;

namespace peeposredemption.Application.Features.Game.Commands;

// ── Data types ───────────────────────────────────────────────────────────────

public record ActiveStatus(string Type, int Strength, int TurnsLeft);
public record MonsterAbility(string Type, float Chance, int Strength, int Turns);

// ── Status effect helper ──────────────────────────────────────────────────────

public static class StatusEffects
{
    public static readonly Dictionary<string, string> Icons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Poison"]     = "☠️",
        ["Burn"]       = "🔥",
        ["Bleed"]      = "🩸",
        ["Freeze"]     = "🧊",
        ["Stone"]      = "🪨",
        ["Silence"]    = "🔇",
        ["Confusion"]  = "💫",
        ["DefenseDown"]= "🛡️↓",
        ["AttackDown"] = "⚔️↓",
        ["Blind"]      = "👁️",
        ["Slow"]       = "⏳",
        ["Curse"]      = "🌑",
        ["MpDrain"]    = "💧",
        ["Berserk"]    = "😡",
        ["PhysResist"] = "🪖",   // passive: % physical damage reduction
        ["MagicResist"]= "✨",   // passive: % magic damage reduction
    };

    // DoT effects that stack additively when reapplied (up to 3× base strength)
    private static readonly HashSet<string> _additiveDots = new(StringComparer.OrdinalIgnoreCase)
        { "Burn", "Bleed", "Poison", "MpDrain" };

    // ── Serialise / Deserialise ───────────────────────────────────────────────

    public static List<ActiveStatus> Load(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try { return JsonSerializer.Deserialize<List<ActiveStatus>>(json) ?? new(); }
        catch { return new(); }
    }

    public static string Save(List<ActiveStatus> effects) =>
        JsonSerializer.Serialize(effects);

    public static List<MonsterAbility> LoadAbilities(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try { return JsonSerializer.Deserialize<List<MonsterAbility>>(json) ?? new(); }
        catch { return new(); }
    }

    // ── Apply a new effect ────────────────────────────────────────────────────
    // DoTs (Burn/Bleed/Poison/MpDrain) stack additively up to 3× the base strength.
    // All other effects take the higher strength and refresh turns.

    public static void Apply(List<ActiveStatus> effects, string type, int strength, int turns)
    {
        var existing = effects.FirstOrDefault(e => e.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            effects.Remove(existing);
            int newStrength = _additiveDots.Contains(type)
                ? Math.Min(existing.Strength + strength, strength * 3)  // additive, capped at 3× base
                : Math.Max(existing.Strength, strength);                 // take higher
            effects.Add(existing with { Strength = newStrength, TurnsLeft = Math.Max(existing.TurnsLeft, turns) });
        }
        else
        {
            effects.Add(new ActiveStatus(type, strength, turns));
        }
    }

    // ── Tick one turn: apply DoT damage, decrement turns, return log lines ────
    // Returns (log lines, hpLost, mpLost)

    public static (List<string> log, int hpLost, int mpLost, bool skipTurn, bool silenced)
        Tick(List<ActiveStatus> effects, PlayerCharacter player, Random rng)
    {
        var log      = new List<string>();
        int hpLost   = 0;
        int mpLost   = 0;
        bool skip    = false;
        bool silenced= false;
        var toRemove = new List<ActiveStatus>();

        foreach (var fx in effects.ToList())
        {
            // Clear chance based on LUK: LUK * 10% per turn, capped at 80%
            float clearChance = Math.Min(0.80f, player.LUK * 0.10f);

            switch (fx.Type.ToLower())
            {
                case "poison":
                    int poisonDmg = Math.Max(1, (int)(player.MaxHp * fx.Strength / 100.0));
                    player.CurrentHp = Math.Max(1, player.CurrentHp - poisonDmg);
                    hpLost += poisonDmg;
                    log.Add($"☠️ Poison deals **{poisonDmg}** damage!");
                    break;

                case "burn":
                    int burnDmg = Math.Max(1, fx.Strength);
                    player.CurrentHp = Math.Max(1, player.CurrentHp - burnDmg);
                    hpLost += burnDmg;
                    log.Add($"🔥 Burn deals **{burnDmg}** damage!");
                    break;

                case "bleed":
                    int bleedDmg = Math.Max(1, fx.Strength);
                    player.CurrentHp = Math.Max(1, player.CurrentHp - bleedDmg);
                    hpLost += bleedDmg;
                    log.Add($"🩸 Bleed deals **{bleedDmg}** damage!");
                    break;

                case "freeze":
                    int freezeDmg = Math.Max(1, fx.Strength);
                    player.CurrentHp = Math.Max(1, player.CurrentHp - freezeDmg);
                    hpLost += freezeDmg;
                    skip = true;
                    log.Add($"🧊 Frozen! Takes **{freezeDmg}** cold damage and cannot act!");
                    break;

                case "stone":
                    skip = true;
                    log.Add($"🪨 Petrified! Cannot act this turn!");
                    break;

                case "silence":
                    silenced = true;
                    log.Add($"🔇 Silenced! Cannot cast magic!");
                    break;

                case "confusion":
                    // 50% chance to attack self — handled in combat action, flagged here
                    log.Add($"💫 Confused!");
                    break;

                case "mpdrain":
                    int mpDrain = Math.Max(1, fx.Strength);
                    player.CurrentMp = Math.Max(0, player.CurrentMp - mpDrain);
                    mpLost += mpDrain;
                    log.Add($"💧 MP Drain saps **{mpDrain}** MP!");
                    break;

                case "curse":
                case "defensedown":
                case "attackdown":
                case "blind":
                case "slow":
                    // Passive — checked in combat logic, no tick damage
                    break;
            }

            // Decrement turns or roll clear chance
            bool cleared = false;
            if (fx.TurnsLeft > 0)
            {
                var updated = fx with { TurnsLeft = fx.TurnsLeft - 1 };
                if (updated.TurnsLeft <= 0)
                {
                    toRemove.Add(fx);
                    cleared = true;
                    log.Add($"✨ {Icons.GetValueOrDefault(fx.Type, fx.Type)} **{fx.Type}** wore off.");
                }
                else
                {
                    effects.Remove(fx);
                    effects.Add(updated);
                    // Luck-based early clear
                    if (rng.NextDouble() < clearChance)
                    {
                        toRemove.Add(updated);
                        cleared = true;
                        log.Add($"✨ {Icons.GetValueOrDefault(fx.Type, fx.Type)} **{fx.Type}** was shaken off early!");
                    }
                }
            }
        }

        foreach (var r in toRemove) effects.Remove(r);

        return (log, hpLost, mpLost, skip, silenced);
    }

    // ── Compute effective DEF considering DefenseDown ─────────────────────────

    public static int EffectiveDef(List<ActiveStatus> effects, int baseDef)
    {
        var dd = effects.FirstOrDefault(e => e.Type.Equals("DefenseDown", StringComparison.OrdinalIgnoreCase));
        return dd != null ? Math.Max(0, baseDef - dd.Strength) : baseDef;
    }

    // ── Compute effective STR considering AttackDown ──────────────────────────

    public static int EffectiveStr(List<ActiveStatus> effects, int baseStr)
    {
        var ad = effects.FirstOrDefault(e => e.Type.Equals("AttackDown", StringComparison.OrdinalIgnoreCase));
        return ad != null ? Math.Max(1, baseStr - ad.Strength) : baseStr;
    }

    // ── Check if player is confused (should attack self) ─────────────────────

    public static bool IsConfused(List<ActiveStatus> effects) =>
        effects.Any(e => e.Type.Equals("Confusion", StringComparison.OrdinalIgnoreCase));

    public static bool IsBlind(List<ActiveStatus> effects) =>
        effects.Any(e => e.Type.Equals("Blind", StringComparison.OrdinalIgnoreCase));

    public static bool IsSlowed(List<ActiveStatus> effects) =>
        effects.Any(e => e.Type.Equals("Slow", StringComparison.OrdinalIgnoreCase));

    public static bool IsCursed(List<ActiveStatus> effects) =>
        effects.Any(e => e.Type.Equals("Curse", StringComparison.OrdinalIgnoreCase));

    // ── Filter to effects that persist between combats ────────────────────────
    // CC effects (Freeze, Stone, Silence, Confusion) are combat-only and cleared.

    private static readonly HashSet<string> _combatOnly = new(StringComparer.OrdinalIgnoreCase)
        { "Freeze", "Stone", "Silence", "Confusion" };

    public static List<ActiveStatus> Persistent(List<ActiveStatus> effects) =>
        effects.Where(e => !_combatOnly.Contains(e.Type)).ToList();

    // ── Format active effects for display ────────────────────────────────────

    public static string Format(List<ActiveStatus> effects)
    {
        if (effects.Count == 0) return "";
        return string.Join(" ", effects.Select(e =>
            $"{Icons.GetValueOrDefault(e.Type, "❓")}{e.Type}({e.TurnsLeft}t)"));
    }

    // ── Helper: build AbilityJson string for seeders ─────────────────────────

    public static string Abilities(params (string type, float chance, int strength, int turns)[] abilities) =>
        JsonSerializer.Serialize(abilities.Select(a =>
            new MonsterAbility(a.type, a.chance, a.strength, a.turns)).ToList());
}
