# Torvex RPG — Game Design Document

> Living document. Update this before assigning agents to feature work.
> Source of truth for all game mechanics, balance, and story direction.

---

## Table of Contents
1. [Vision & Philosophy](#1-vision--philosophy)
2. [Influences](#2-influences)
3. [Story Design](#3-story-design)
4. [Combat System](#4-combat-system)
5. [Classes & Abilities](#5-classes--abilities)
6. [Elements & Damage Types](#6-elements--damage-types)
7. [Stats System](#7-stats-system)
8. [Items & Equipment](#8-items--equipment)
9. [Enchanting](#9-enchanting)
10. [Economy Loop](#10-economy-loop)
11. [Skills & Gathering](#11-skills--gathering)
12. [Progression & Tiers](#12-progression--tiers)
13. [PvP System](#13-pvp-system)
14. [Endgame](#14-endgame)
15. [Gradual Rollout Plan](#15-gradual-rollout-plan)
16. [Monetization](#16-monetization)
17. [Peepo Collectibles](#17-peepo-collectibles)
18. [Summons](#18-summons)
19. [Party System](#19-party-system)

---

## 1. Vision & Philosophy

**Torvex = Chat platform + Text-based RPG.**

The short version: Discord UI, RuneScape economy depth, SMT/FF combat feel, original story.

Players live inside a chat platform. The RPG is embedded in it — not a separate app. Characters grow alongside the community. Chat level = RPG level. Your social presence and your combat power are the same thing.

### Design Pillars
- **Depth without overwhelm** — complexity unlocks by level so new players aren't drowned
- **Player-driven economy** — crafting, trading, and the marketplace matter; no P2W
- **Skill expression** — positioning, element matchups, and class triangle create real decisions
- **Lore in the margins** — story lives in item descriptions, NPC fragments, and zone atmosphere; never forced exposition
- **Earned weight** — make players love things before taking them away

---

## 2. Influences

| # | Game / Work | What it contributes |
|---|-------------|-------------------|
| 1 | **Final Fantasy X** | The gold standard for cohesion. Every character, location, and mechanic answers the same thematic question from a different angle. Multiple emotional peaks spaced so each one lands. The journey itself is the point — you mourn it ending. |
| 2 | **Dragon Quest 8** | Inhabited world — towns with history that predates you. Puppet villain concealing deeper evil. Party members with quiet, earned arcs. Grief without clean catharsis. Yangus: reads as pure comedic relief but his loyalty is unconditional and costs him nothing to give. The joke and the sincerity at the same time. |
| 3 | **Dragon Quest 11** | World broken mid-story and you keep going anyway. Death in the gap (Veronica). The Sylvando — reads as comic relief, turns out to be the emotional spine. The mermaid and the lost sailor: story already over when you find it, pieced together through letters and a grave. |
| 4 | **Mouthwashing** | Psychological horror told non-linearly. The player assembles the truth out of order. The sadness is slow — you warm to the crew before you understand their complicity. Swansea knew something was wrong and said nothing. Not evil — just afraid and tired. The recontextualization at the end makes everything worse, not just different. |
| 5 | **Attack on Titan** | Mystery that expands outward in stages. Every faction believes they are the protagonist. Every cliffhanger confirms your suspicion then makes it worse. The ruling faction maintains power through the story they tell about the threat. |
| 6 | **Halo** | Scale — you are small inside something ancient and indifferent. Lore in fragments and terminals, never exposition. The Arbiter and Chief: enemies who never chose each other, no dramatic reconciliation, bond built through proximity and shared survival. |
| 7 | **Twilight Princess** | Melancholy. A world dying slowly. The companion (Midna) matters more than the quest. Puppet villain concealing the real one. |
| 8 | **Ocarina of Time** | Each area/arc owns a different emotion. Permission for tonal variety. Journey as transformation — the player you are at the end is not who you were at the beginning. |
| 9 | **Breath of the Wild** | *(Negative reference)* — Avoid the word "Calamity." Too associated with this game's specific flavor and narrative distance. |
| 10 | **Tears of the Kingdom** | More grounded than BotW. More evidence of the world before. Better at showing what was lost. |
| 11 | **Kingdom Hearts** | The motif of crossing impossible distances for someone — not because it's strategic, not because the world depends on it, but because you made a promise. Tragic love works best in the margins, not the main plot. Nostalgia and music do structural work. |
| 12 | **Clair Obscura: Expedition 33** | *(Negative reference)* — Interesting concept but the world feels empty. No evidence of what existed before the threat started. Players can't feel what was lost because they never saw it. Lesson: show the ruins. Show locked rooms. Show old NPCs who remember. Give players a "before" to grieve. |
| 13 | **RuneScape** | Battle system and economy reference only. Skill-based progression, inventory management, gear gating, trading economy between players. Class combat triangle (Melee > Ranger > Mage). |
| 14 | **Persona 5** | Ruling institution as villain — each party member was failed by a different institution wearing the same face. Social bonds mechanically mattering in combat. |
| 15 | **Avatar: The Last Airbender** | Light-hearted beginning that earns the weight of what comes later. Season 2 Ba Sing Se — the lie maintained inside the city. Zuko's double redemption: one redemption is a story beat, two is a character. Former enemy earning trust through action, not words. |
| 16 | **Avatar: Legend of Korra** | Zahir — a philosopher who is also lethal. His danger isn't raw power, it's that he's always three steps ahead and he *believes* in what he's doing. The most dangerous enemies have a coherent worldview. |
| 17 | **Cyberpunk 2077** | Tragic hero with no way out. The good ending is dying on your own terms. Victory and survival are not the same thing. |
| 18 | **Orthodox Lives of the Saints** | *[TBD — referenced by user, to be expanded in a future session]* |
| 19 | **SMT (Shin Megami Tensei)** | Press Turn combat economy. Hit a weakness = bonus action. Hit immunity = lose all remaining turns. Creates high-stakes tactical decisions in every single fight. |
| 20 | **Darkest Dungeon** | Positioning system — Front/Mid/Back row determines what moves are available and who can be targeted. Stress/Morale as a combat resource. |
| 21 | **Fire Emblem** | Support bonds between units mechanically mattering. Characters who fight together gain passive bonuses. |
| 22 | **Final Fantasy (series)** | Element system, named spells per element tier (Fire → Fira → Firaga), equipment linking. Full element roster: Fire, Ice, Thunder, Wind, Water, Earth, Holy, Dark, Poison, Gravity, Time, Void. |

---

## 3. Story Design

### The World-Level Lie
The central threat is not a natural disaster — it is maintained and manufactured by a ruling faction whose power depends on it continuing. The religion/order built around surviving it is their control mechanism. This is the unifying pattern across almost every influence on this list.

**No "Calamity"** — find original language for the central threat.

### Story Principles

| Principle | Source | Description |
|-----------|--------|-------------|
| **The puppet structure** | DQ8, Twilight Princess | Visible threat conceals deeper one. Defeating it feels incomplete. |
| **Show the before** | Anti-Clair Obscura | Every arc needs evidence of what the world was. Ruins, locked rooms, old NPCs who remember. |
| **Tragic love in the margins** | KH, DQ11 | Item descriptions, NPC dialogue, a grave with a date. Implied not spelled out. |
| **The recontextualization standard** | Mouthwashing | The reveal makes things *worse*, not just different. |
| **Make them love first, reveal second** | Mouthwashing (Swansea) | Warm/funny characters before their complicity is revealed. |
| **The Veronica principle** | DQ11 | Major character death happens in the gap. You find out through someone else's grief. |
| **The Sylvando slot** | DQ11 | One character who reads as comic relief, turns out to be the emotional spine. |
| **Canon implies itself** | Halo | Write enough that players believe the rest exists. Fragments, contradicting accounts, religious texts that don't agree. |
| **OoT arc model** | Ocarina of Time | Each major zone owns one dominant emotion. Tonal variety is intentional. |

### Zone Emotional Spread

| Zone Type | Dominant Emotion | Reference |
|-----------|-----------------|-----------|
| Psychological horror | Dread, slow realization, complicity | Mouthwashing |
| Tragic lovers | Quiet grief, love across impossible boundary | DQ11 mermaid/sailor |
| Brotherhood | Earned loyalty, unlikely bond | Halo Arbiter/Chief |
| Grand lie | Expanding mystery, world-scale betrayal | Attack on Titan |
| Warmth | Safety, belonging, community — something to lose | — |
| Scale | Smallness, ancient stakes, awe | Halo Forerunners |

**Design rule:** Don't blend tones within a zone. Commit fully. The contrast between zones is what makes each one land.

### Protagonist Arc
Inspired by Cyberpunk 2077's V: the protagonist is dying. Not metaphorically — there is a clock. Every path leads to the same end. The only question is *how* and *whether it meant something*. The good ending is dying on your own terms, surrounded by people you fought for. The bad ending is surrendering — your identity consumed by the thing you were fighting.

### Companion Archetypes

| Archetype | Reference | Description |
|-----------|-----------|-------------|
| **The Unlikely Bond** | Halo — Arbiter/Chief | Enemies or opposites who never chose each other. No dramatic reconciliation. Bond built through survival. |
| **The Singular One** | Halo — Sgt. Johnson | Larger than life, funny, capable, unshakeable. The joke is nothing touches them. The tragedy is eventually something does. |
| **The Double Redemption** | ATLA — Zuko | Earns trust, betrays it, earns it back a second time. The second arc is harder and more meaningful. |

---

## 4. Combat System

### Overview
Turn-based with a **Press Turn economy** (SMT-inspired). Actions consume turn icons. Hitting weaknesses and landing crits earn bonus icons. Hitting resistances or immunities costs extra icons. This creates real stakes on every turn — not just "do the most damage."

### Turn Icons
- Each combatant gets **1–3 turn icons** per round based on SPD stat tier
- **Hit weakness or crit** → spend ½ icon (effectively a bonus action)
- **Hit resistance** → spend 1½ icons (penalty)
- **Hit immunity** → lose ALL remaining icons for your team this round
- Dead party members lose their icons

### Combat Actions (per turn)
| Action | Cost | Effect |
|--------|------|--------|
| **Attack** | 1 icon | Physical damage, scales STR + weapon |
| **Magic** | 1 icon | Elemental damage, scales INT, costs MP |
| **Skill** | 1 icon | Class-specific ability (see Classes) |
| **Item** | 1 icon | Use consumable from 6-slot inventory |
| **Defend** | 1 icon | Halve incoming damage this turn, slight counter chance |
| **Flee** | 1 icon | Escape attempt; success rate = `30% + (SPD - enemySPD) * 2%`, clamp 10–90% |

### Positioning (unlocks at Level 20)
Three rows: **Front / Mid / Back**

| Row | Who belongs here | Rules |
|-----|-----------------|-------|
| Front | Warriors, tanks | Can use all melee attacks. Absorbs hits before Back row. |
| Mid | Rogues, Rangers | Access melee and ranged. Can be targeted by AoE. |
| Back | Mages, Clerics | +20% magic damage. Protected unless Front/Mid wiped. |

Enemies have the same row system. Melee cannot reach Back row unless Front is cleared.

### Damage Formulas

**Physical Attack:**
```
base = roll(weapon.MinDmg, weapon.MaxDmg) + STR * 0.5
damage = max(1, base - target.DEF * 0.3)
crit_chance = min(25%, LUK * 0.5% + DEX * 0.2%)
final = damage * element_multiplier * (crit ? 1.5 : 1.0)
```

**Magic Attack:**
```
base = INT * 1.5 + roll(5, 15)
mp_cost = 10 + caster_level
damage = max(1, base - target.MDEF * 0.3)
final = damage * element_multiplier
```

**Monster Attack:**
```
raw = roll(monster.MinDmg, monster.MaxDmg) + monster.STR * 0.3
damage = max(1, raw - target.DEF * 0.3)
if target defending: damage /= 2
```

### Status Effects

#### Elemental Procs (applied by element attacks, chance based on LUK)
| Status | Element | Effect | Duration |
|--------|---------|--------|----------|
| **Burn** | Fire | DoT: 5% max HP/turn | 3 turns |
| **Frozen** | Ice | -30% miss chance, -20% SPD | 2 turns |
| **Stunned** | Lightning | Skip next turn | 1 turn |
| **Rooted** | Earth | Cannot flee, -30% DEX | 2 turns |
| **Drenched** | Water | +25% Lightning dmg taken | 2 turns |
| **Pushed** | Wind | Forced Back row, -15% phys DEF | 1 turn |
| **Poisoned** | Poison/Cursed | Flat DoT, stacks twice | 3 turns |
| **Blinded** | Light | -40% accuracy | 2 turns |
| **Drained** | Shadow | Attacker heals 10% dmg dealt | 2 turns |
| **Exposed** | Void | -30% DEF and MDEF | 2 turns |
| **Weakened** | Dark | -25% all damage output | 3 turns |
| **Silenced** | Holy | Cannot use Magic or Skills | 1 turn |

#### Debuff Statuses (applied by debuff spells/abilities/items)
| Status | Source | Effect | Duration |
|--------|--------|--------|----------|
| **Confused** | Spells, items | 40% chance attacks hit random target (incl. self) | 2 turns |
| **Berserk** | Spells, boss ability | Must attack every turn, +30% dmg, cannot defend/flee | 3 turns |
| **Cursed** | Dark spells, items | All healing received reduced by 50% | 3 turns |
| **Charmed** | Spells | Attacks own ally for 1 turn | 1 turn |
| **Frightened** | Spells, boss ability | Must flee if possible; if unable, -50% dmg | 2 turns |
| **Bleeding** | Physical crits, Rogue | Flat DoT (physical), doesn't stack, refreshes | 3 turns |
| **Paralyzed** | Lightning crits, items | 50% chance to skip turn each turn | 2 turns |
| **Marked** | Ranger, items | Takes +25% damage from all sources | 3 turns |
| **Shattered** | Ice crits | DEF -40% (armor weakened by the freeze) | 2 turns |
| **Slow** | Spells, items | SPD halved, -1 turn icon per round | 2 turns |
| **Corroded** | Debuff spells/items | Specific element resist -30% (e.g. Fire Resist -30%) | 3 turns |

#### Buff Statuses (applied by healing spells/abilities/items)
| Status | Source | Effect | Duration |
|--------|--------|--------|----------|
| **Regenerating** | Cleric Regen, meals | Restore HP per turn | 3 turns |
| **Barrier** | Cleric, items | Absorb next X damage (then expires) | Until broken |
| **Warded** | Cleric Ward, items | Immune to next status effect (then expires) | Until triggered |
| **Empowered** | Spells, food | +25% all damage output | 3 turns |
| **Hasted** | Wind spells, potions | SPD doubled, +1 turn icon | 2 turns |
| **Fortified** | Warrior Rally, items | +30% DEF and MDEF | 3 turns |
| **Focused** | Ranger, items | +25% crit chance | 3 turns |

Buffs and debuffs are separate stacks from elemental procs. Cleanse removes debuffs. Dispel (enemy ability) removes buffs.

#### Cleansing & Removal
| Method | Removes |
|--------|---------|
| Cleric: Cleanse | All debuff statuses on one target |
| Antidote (item) | Poisoned + Corroded |
| Holy Water (item) | Cursed + Bleeding |
| Smelling Salts (item) | Confused + Frightened + Paralyzed |
| Dispel (enemy ability) | All buff statuses on target |

### Morale (unlocks at Level 35)
A 0–100 meter per combatant. Changes based on combat events:
- Kill an enemy: +10
- Take a crit: -10
- Ally dies: -20
- Land a weakness hit: +5
- At 0: **Panic** — random actions, 50% chance to skip turn
- At 100: **Resolve** — +25% all stats

### Victory Rewards
1. XP (from monster definition)
2. Coins (random range from monster)
3. Combat Skill XP (50% of monster XP)
4. Loot drops (LUK-influenced drop chance)
5. Level-up if XP threshold crossed → full HP/MP restore

### Defeat Penalties
1. Lose 10% CoinBalance
2. `TotalDeaths++`
3. Respawn at 25% HP

---

## 5. Combat Styles

**No classes. No locked-in choices. Everyone starts the same.**

Your combat style is defined entirely by what you equip. Swap your gear, swap your style — the same character can be a melee tank one fight, a fire mage the next, a summoner after that. The depth comes from your gear, your stats, and your decisions — not from a menu you picked at character creation.

> *Halo philosophy: everyone starts equal. Power comes from what you find and how you use it. A skilled player with basic gear beats an unskilled player with great gear — but great gear is still worth chasing.*

### The Four Combat Styles

| Style | Driven by | Key Stat | Core Action |
|-------|-----------|----------|-------------|
| ⚔️ **Melee** | Sword / Axe / Dagger equipped | STR | `/rpg attack` |
| 🏹 **Ranged** | Bow equipped | DEX | `/rpg attack` (ranged) |
| 🔮 **Magic** | Staff equipped | INT | `/rpg magic <spell>` |
| 👻 **Summoning** | Summoning Tome equipped | INT + Summoning skill | `/rpg summon <creature>` |

You are never locked into one. Mid-dungeon you can swap a staff for a sword if you need to tank a hit. Nothing stops you.

---

### Melee ⚔️

**Weapons:** Sword, Axe, Dagger, Spear *(each with a different feel)*

| Weapon Type | Feel | Special |
|-------------|------|---------|
| Sword | Balanced damage + DEF | Can equip shield in offhand |
| Axe | High damage, slower | Cleave: hits adjacent enemies |
| Dagger | Fast, low damage | High crit chance; Backstab on first hit |
| Spear | Long reach | Can attack from Mid row; keeps range advantage |

Elemental weapons (Fire Blade, Frost Axe, etc.) add element to all attacks — the same element system that spells use. A Fire Blade in a Dungeon melts undead exactly like a Fire spell would.

**Melee scales with:** STR (damage), DEF (damage reduction from STR on blocks), VIT (HP)

---

### Ranged 🏹

**Weapons:** Bow, Crossbow

Arrows are consumable and **determine the element** of ranged attacks. No arrows equipped = physical damage only.

| Arrow Type | Element | Unlock |
|------------|---------|--------|
| Iron Arrow | Physical | From the start |
| Fire Arrow | Fire 🔥 | Fletcher skill 15 |
| Ice Arrow | Ice ❄️ | Fletcher skill 25 |
| Holy Arrow | Holy ✨ | Fletcher skill 40 |
| Void Arrow | Void 🕳️ | Fletcher skill 80 |

**Ranged scales with:** DEX (damage + accuracy), LUK (crit chance)

---

### Magic 🔮

**Weapons:** Staff *(element determined by the staff)*

Spells are unlocked by **character level** — they're skills you've learned, available to anyone who equips a staff. A Lv20 player who picks up a staff gets Lv1–20 spells. A Lv60 player gets everything.

The staff's element determines which spells are most effective — a Fire Staff boosts Fire spells. You can still cast Ice spells from a Fire Staff, but at no bonus.

#### Spell List (unlocked by level, available to any staff user)

| Level | Spell | Element | MP Cost | Effect |
|-------|-------|---------|---------|--------|
| 1 | **Fire** | 🔥 | 12 | Single target, chance to Burn |
| 1 | **Blizzard** | ❄️ | 12 | Single target, chance to Frozen |
| 1 | **Thunder** | ⚡ | 12 | Single target, chance to Stun |
| 1 | **Heal** | — | 15 | Restore HP to self |
| 10 | **Fira** | 🔥 | 22 | Upgraded Fire |
| 10 | **Blizzara** | ❄️ | 22 | Upgraded Blizzard |
| 10 | **Thundara** | ⚡ | 22 | Upgraded Thunder |
| 10 | **Corrode** | ☠️ | 20 | Target element resist -30% for 3 turns |
| 15 | **Quake** | 🪨 | 28 | AoE, chance to Root all |
| 15 | **Flood** | 💧 | 28 | AoE, applies Drenched |
| 15 | **Barrier** | — | 20 | Absorb next X damage |
| 20 | **Firaga** | 🔥 | 40 | Massive single-target Fire |
| 20 | **Blizzaga** | ❄️ | 40 | Massive Ice |
| 20 | **Thundaga** | ⚡ | 40 | Jumps to 2 targets |
| 20 | **Slow** | — | 25 | SPD halved for 2 turns |
| 25 | **Confuse** | — | 30 | 40% chance attacks random target for 2 turns |
| 25 | **Regen** | — | 30 | Restore HP/turn for 3 turns |
| 30 | **Aeroga** | 🌪️ | 55 | AoE Wind, pushes enemies back |
| 30 | **Gravity** | 🌑 | 50 | Reduce enemy HP by 25% (non-lethal) |
| 30 | **Curse** | — | 35 | Healing -50% for 3 turns |
| 30 | **Cleanse** | — | 20 | Remove all debuffs from target |
| 40 | **Holy** | ✨ | 70 | Massive Holy damage, +75% vs undead |
| 40 | **Dark Pulse** | 🌫️ | 65 | Shadow damage, heals caster 20% of damage |
| 40 | **Silence** | — | 30 | No magic/skills for 2 turns |
| 45 | **Petrify** | — | 45 | Shattered DEF + Paralyzed 1 turn |
| 50 | **Arcane Surge** | — | 0 | Next spell costs 0 MP, deals double damage |
| 55 | **Dispel** | — | 40 | Remove all buffs from target |
| 60 | **Meteor** | 🕳️ | 120 | Void damage ignoring all resists |
| 60 | **Doom** | — | 60 | KO after 3 turns unless Cleansed |

**MP economy:** ~60 MP at Lv1. Fire = 5 casts before dry. MP is a real resource — potions and Abyssal Eel restore it. Arcane Surge is the kill-shot when you need one big hit.

**Magic scales with:** INT (spell damage + heal amount)

---

### Summoning 👻

**Weapons:** Summoning Tome *(replaces offhand or mainhand)*

Summoning is a fourth combat style, not a support role. Your summon fights independently every turn — you still attack/defend/use items on your own turn. Effectively you're a two-person party solo.

Summons are unlocked by **Summoning skill level** (trained by using `/rpg summon`). Higher skill = stronger creatures available.

#### Summon Roster

| Summon Skill | Creature | Element | What It Does |
|-------------|---------|---------|--------------|
| 1 | **Wisp** | Holy | Weak auto-attack; occasionally heals you for 5 HP |
| 10 | **Stone Golem** | Earth | Tanky, high DEF; draws enemy attacks toward itself |
| 20 | **Fire Sprite** | Fire 🔥 | Low HP, high damage; applies Burn |
| 30 | **Frost Wraith** | Ice ❄️ | Applies Frozen on hit; slows enemies |
| 40 | **Thunder Drake** | Lightning ⚡ | Fast; AoE lightning every 3 turns |
| 50 | **Shadow Wolf** | Shadow 🌫️ | Applies Bleeding; gains power when enemies are debuffed |
| 60 | **Ifrit** | Fire 🔥 | Powerful; AoE fire each turn; enrages at 50% HP |
| 70 | **Shiva** | Ice ❄️ | Heals you each turn + Freezes enemies |
| 80 | **Alexander** | Holy ✨ | Full heal + Holy AoE once per combat |
| 90 | **Odin** | Void 🕳️ | Instant-kill chance on hit; massive Void burst finisher |

Only **one summon** can be active at a time. If your summon dies, re-summon next turn (costs MP). Higher Summoning skill reduces the MP cost and increases summon HP.

**Summoning scales with:** INT (summon damage/heal), Summoning skill level (what you can summon)

---

### Stat Growth Per Level (same for everyone)

No class taxes. Everyone gets the same base growth per level:

| STR | DEF | INT | DEX | VIT | LUK |
|-----|-----|-----|-----|-----|-----|
| +1 | +1 | +1 | +1 | +1 | +1 |

**Additional stats come from gear.** A sword stacked with STR bonuses makes you a better melee fighter. A staff stacked with INT makes your spells stronger. Your stat identity is your equipment, not a class choice you made at level 1.

---

### PvP Balance Without Classes

Without a class triangle, PvP balance comes from:
- **Element matchups** — Fire vs Ice, Holy vs Dark, etc.
- **Gear tier** — higher level gear wins all else equal, but is earnable by anyone
- **Skill level** — Summoning skill, Combat skill, etc. reflect actual time invested
- **Preparation** — bringing the right arrows, the right spells, the right summon for who you're fighting

No ability hard-counters. No "I lose because they picked the right class." You lose because they outgeared you, outprepared you, or outplayed you.

---

## 6. Elements & Damage Types

### The 3 Physical Types
Forms a triangle — matching type beats the next one:
```
Melee  >  Ranger  >  Mage  >  Melee
```
+15% damage when in-triangle advantage. Applies in PvP via class matchup.

### The 12 Elements

| Element | Color | Secondary Effect | Strong Against | Weak Against |
|---------|-------|-----------------|----------------|--------------|
| **Fire** 🔥 | Red | Burn (DoT) | Ice, Earth | Water, Wind |
| **Ice** ❄️ | Blue | Frozen (miss chance) | Wind, Water | Fire, Lightning |
| **Lightning** ⚡ | Yellow | Stunned (skip turn) | Water, Ice | Earth, Wind |
| **Earth** 🪨 | Brown | Rooted (can't flee) | Lightning, Fire | Water, Ice |
| **Water** 💧 | Cyan | Drenched (+Lightning dmg taken) | Fire, Earth | Ice, Lightning |
| **Wind** 🌪️ | Green | Pushed (Back row, -DEF) | Lightning, Earth | Fire, Ice |
| **Holy** ✨ | Gold | Silenced (no magic/skills) | Dark, Undead, Demons | Shadow, Cursed |
| **Dark** 🌑 | Purple | Weakened (-dmg output) | Holy, Light | Holy, Void |
| **Light** 💡 | White | Blinded (-accuracy) | Dark, Shadow | Dark, Void |
| **Shadow** 🌫️ | Dark Gray | Drained (lifesteal) | Light, Holy | Holy, Fire |
| **Poison/Cursed** ☠️ | Green-black | Poisoned (stacking DoT) | Earth, Water | Holy, Fire |
| **Void** 🕳️ | Black | Exposed (-DEF/-MDEF) | All elements (ignores resist) | Nothing — Void has no weakness |

### Element Multipliers
- **Advantage:** 1.25×
- **Neutral:** 1.0×
- **Resistance:** 0.75×
- **Immunity:** 0× (but hitting immunity costs ALL remaining turn icons — high risk)
- **Absorption:** Heals target (legendary items only)

**Void is special:** Void damage ignores all elemental resistances and immunities, always deals 1.0× — no bonus, but no penalty either. The "true damage" element.

---

## 7. Stats System

| Stat | Abbreviation | Role |
|------|-------------|------|
| Strength | STR | Physical/melee damage scaling |
| Defense | DEF | Reduces physical damage taken |
| Intelligence | INT | Magic damage scaling + Heal scaling |
| Magic Defense | MDEF | Reduces magic/elemental damage taken (separate from DEF) |
| Dexterity | DEX | Crit chance, accuracy, flee success |
| Vitality | VIT | Max HP scaling |
| Luck | LUK | Crit chance, loot drop rate, status proc chance |
| Speed | SPD | Turn order, number of turn icons per round (1–3) |

**MaxHP** = `100 + (VIT * 5) + (Level * 10)`
**MaxMP** = `50 + (INT * 2) + (Level * 5)`
**Turn Icons** = `1 if SPD < 20, 2 if SPD 20–49, 3 if SPD 50+`

### Elemental Resistances (on PlayerCharacter)
Separate resist value per element: `ResistFire`, `ResistIce`, `ResistLightning`, `ResistEarth`, `ResistWater`, `ResistWind`, `ResistHoly`, `ResistDark`, `ResistLight`, `ResistShadow`, `ResistPoison`, `ResistVoid`

- Range: -50% (weakness) to +75% (hard cap — true immunity only via legendary proc)
- Stacks additively from gear, buffs, and passive abilities

---

## 8. Items & Equipment

### Item Tiers

| Tier | Level Range | Rarity | Zone |
|------|------------|--------|------|
| 1 — Starter | 1–20 | Common, Uncommon | Plains, Forest |
| 2 — Adventurer | 20–40 | Uncommon, Rare | Mountains, Dungeon |
| 3 — Elite | 40–60 | Rare, Epic | Volcano, Abyss |
| 4 — Endgame | 60–80 | Epic, Legendary | Celestial Spire |
| 5 — Transcendent | 80–100 | Legendary, Unique | Void Realm |

### Equipment Slots
MainHand, OffHand, Head, Chest, Legs, Feet, Ring, Amulet, **Accessory** (new — resistance-focused slot)

### Consumables — Potions

All potions are usable in combat (costs 1 turn icon). Buyable at NPC shop or craftable via Alchemy.

#### HP Potions
| Potion | HP Restored | Alchemy Level | NPC Price |
|--------|------------|--------------|-----------|
| Small Health Potion | 30 HP | 1 | 30 coins |
| Medium Health Potion | 80 HP | 20 | 80 coins |
| Large Health Potion | 150 HP | 40 | 150 coins |
| Super Restore | 250 HP + cure 1 status | 60 | 400 coins |

#### MP Potions
| Potion | MP Restored | Alchemy Level | NPC Price |
|--------|------------|--------------|-----------|
| Small Mana Potion | 20 MP | 5 | 25 coins |
| Medium Mana Potion | 50 MP | 25 | 60 coins |
| Large Mana Potion | 100 MP | 45 | 120 coins |

#### Utility Potions
| Potion | Effect | Alchemy Level | NPC Price |
|--------|--------|--------------|-----------|
| Antidote | Cure Poisoned status | 1 | 15 coins |
| Elixir | 75 HP + 40 MP | 60 | 350 coins |
| Strength Brew | +15% STR for 3 turns | 35 | 200 coins |
| Intelligence Draft | +15% INT for 3 turns | 35 | 200 coins |
| Smoke Grenade | Apply Blinded to 1 enemy | 30 | 180 coins |

**Design rule:** Potions are reliable burst. Food (cooked) is cheaper and more varied but skill-gated. Both compete for your 6-slot inventory.

---

### Weapon Types & Stats
| Type | Class | Scaling | Special |
|------|-------|---------|---------|
| **Sword** | Warrior | STR | Balanced — high floor, high ceiling |
| **Axe** | Warrior | STR | +20% damage, -10% accuracy |
| **Dagger** | Rogue | DEX | Low raw damage, +15% crit chance |
| **Staff** | Mage | INT | Elemental affinity slot — set element per staff |
| **Bow** | Ranger | DEX | +15% damage from Back row |
| **Wand** | Mage | INT | Lower damage than staff, costs ½ turn icon *[TBD — ability system]* |
| **Spear** | Warrior | STR | Reaches Back row from Front *[TBD — positioning system]* |
| **Greatsword** | Warrior | STR | Two-handed (no OffHand), +40% damage *[TBD]* |
| **Crossbow** | Ranger | DEX | Slower reload (every other turn), but higher burst *[TBD]* |

### Craftable Weapons

Crafted via Smithing (melee/ranged) or Alchemy+Woodcutting (staves/bows).

**Tier 1 — Starter (Smithing Lv 1–15)**
| Weapon | Ingredients | Lv Req |
|--------|------------|--------|
| Copper Dagger | 1x Copper Bar | 1 |
| Iron Sword | 2x Iron Bar | 10 |
| Iron Axe | 3x Iron Bar | 12 |
| Oak Staff | 3x Normal Logs | 8 |
| Oak Bow | 2x Oak Logs | 15 |

**Tier 2 — Adventurer (Smithing Lv 20–40)**
| Weapon | Ingredients | Lv Req |
|--------|------------|--------|
| Steel Sword | 3x Steel Bar | 20 |
| Steel Axe | 4x Steel Bar | 22 |
| Mithril Dagger | 2x Mithril Bar | 35 |
| Willow Bow | 3x Willow Log + 1x Iron Bar | 30 |
| Silver Staff | 3x Willow Log + 1x Silver Ore | 28 |

**Tier 3 — Elite (Smithing Lv 45–65)**
| Weapon | Ingredients | Lv Req |
|--------|------------|--------|
| Adamantite Sword | 3x Adamantite Bar | 50 |
| Adamantite Axe | 4x Adamantite Bar | 52 |
| Adamantium Dagger | 2x Adamantium Bar + 1x Diamond | 60 |
| Yew Bow | 3x Yew Log + 2x Mithril Bar | 55 |
| Magic Staff | 3x Magic Log + 1x Enchanting Material | 60 |

**Tier 4–5 — Endgame (crafted from boss materials + Voidstone/Void Wood)**
- See Named Unique Items above — these are drop-only, not craftable
- Tier 5 armor/weapons require Prismatic Shards + Voidstone + Void Wood — all endgame gated

### Armor Sets by Tier

Stat values shown are per-piece (Chest). Helm = ~60%, Legs = ~80%, Feet = ~50%, Ring/Amulet = bonus stats only.

**Tier 1 — Starter**
| Set | Lv | DEF | VIT | Other | Craft |
|-----|----|-----|-----|-------|-------|
| Leather | 1 | +4 | — | — | 4x Leather Scraps |
| Iron | 10 | +10 | +2 | — | 4x Iron Bar |

**Tier 2 — Adventurer**
| Set | Lv | DEF | VIT | Other | Craft |
|-----|----|-----|-----|-------|-------|
| Steel | 20 | +20 | +5 | — | 4x Steel Bar |
| Shadow (Rare) | 35 | +15 | +3 | +8 INT, +5 LUK | 2x Steel Bar + 1x Dark Essence |

**Tier 3 — Elite**
| Set | Lv | DEF | VIT | Other | Craft |
|-----|----|-----|-----|-------|-------|
| Dragonscale (Epic) | 50 | +35 | +10 | +8 STR | 4x Dragonscale + 2x Adamantite Bar |
| Adamantium Plate | 55 | +40 | +8 | — | 4x Adamantium Bar |
| Mage Vestments | 45 | +12 | +5 | +15 INT, +10 MDEF | 3x Magic Log + 2x Silver Ore |

**Tier 4 — Endgame**
| Set | Lv | DEF | VIT | Other | Craft/Source |
|-----|----|-----|-----|-------|-------------|
| Celestial Plate (Epic) | 65 | +55 | +12 | +15 MDEF, +20% Holy resist | Boss drop + Voidstone craft |
| Voidweave (Epic, Mage) | 70 | +25 | +8 | +25 INT, +20 MDEF, spells -50% MP | Lich drop + Magic Log craft |
| Stormfeather (Ranger) | 72 | +30 | +10 | +20 DEX, Lightning element | Harpy drop + Yew Bow material |

**Tier 5 — Transcendent (Lv85–100)**

| Set | Class | Theme | Special Passive |
|-----|-------|-------|----------------|
| **Nullstone Aegis** | Warrior | Black stone, absorbs hits | +25% all resist, 10% chance to reflect damage |
| **Celestial Vestments** | Cleric | Glowing white/gold | Holy dmg +40%, Regen aura (passive HP/turn) |
| **Voidheart Robe** | Mage | Dark matter aesthetic | Void element, absorb 10% dmg as MP |
| **Phantom Wraps** | Rogue | Invisible shimmer | +50% Backstab damage, 20% dodge chance |
| **Stormfeather Crown** | Ranger | Crackling feathers | Lightning element, multishot always crits |
| **Prismatic Plate** | Any | Color-shifting | Adapts resistance to last element hit (+30% to that element) |

**Prismatic Plate** is the all-rounder trophy — best-in-slot for mixed content. Requires all 5 tier-5 materials to craft.

### Named Unique Items (0.5–1% drop rate, tradeable)
| Item | Type | Source | Effect |
|------|------|--------|--------|
| **Soulrender** | Lv90 Sword | Archdemon only | Dark element, 5% lifesteal on hit |
| **Aegis of the Forgotten** | Lv95 Shield | Final boss only | +30% all resist, immune to status effects |
| **Wraithcaller Staff** | Lv85 Staff | Lich only | Summons shadow spirit each combat round |
| **The Pale Bow** | Lv80 Bow | Blood Moon Werewolf | Void element, arrows pierce DEF completely |
| **Sunbreaker** | Lv88 Axe | Archdemon | Holy + Fire hybrid, +40% vs undead |
| **Heartseeker** | Lv82 Dagger | Ancient Vampire | +25% crit, crits apply Drained status |

### Economy Loop — Every Item
Every item must complete this full loop:
- ✅ **Droppable** — from monsters or gathering
- ✅ **Buyable** — NPC coin shop (tiers 1–3) or marketplace (all tiers)
- ✅ **Sellable** — to NPC vendor for coins (`SellPrice` on ItemDefinition)
- ✅ **Craftable** — via smithing/tailoring/alchemy recipe
- ✅ **Enchantable** — enchant slots scale with rarity
- ✅ **Tradeable** — player-to-player via `/trade`
- ✅ **Listable** — on marketplace for coins or orbs

---

## 9. Enchanting

### Enchant Slots by Rarity
| Rarity | Slots |
|--------|-------|
| Common | 0 |
| Uncommon | 1 |
| Rare | 2 |
| Epic | 3 |
| Legendary | 3 + 1 innate (fixed) |

### Named Enchantments (with Tiers I / II / III)

Each enchant has 3 tiers. Higher tier = higher Enchanting level + rarer material.

**Offensive**
| Enchant | I | II | III | Enchanting Level |
|---------|---|----|----|-----------------|
| **Lifesteal** | 2% lifesteal on hit | 4% | 7% | 10 / 30 / 55 |
| **Firebrand** | +8% Fire damage | +15% | +25% | 10 / 30 / 55 |
| **Frostbite** | +8% Ice damage | +15% | +25% | 10 / 30 / 55 |
| **Thunderstruck** | +8% Lightning damage | +15% | +25% | 10 / 30 / 55 |
| **Vorpal** | +5% crit chance | +10% | +15% | 15 / 35 / 60 |
| **Voidtouched** | +6% Void damage | +12% | +20% | 40 / 65 / 85 |
| **Reflect** | 3% chance to reflect | 6% | 10% | 25 / 50 / 70 |

**Defensive**
| Enchant | I | II | III | Enchanting Level |
|---------|---|----|----|-----------------|
| **Defender** | +5 DEF | +10 | +20 | 10 / 25 / 50 |
| **Guardian** | +5% max HP | +10% | +20% | 15 / 35 / 60 |
| **Scholar's Ward** | +5 MDEF | +10 | +18 | 10 / 25 / 50 |
| **Fireward** | +10% Fire resist | +20% | +35% | 15 / 35 / 60 |
| **Iceward** | +10% Ice resist | +20% | +35% | 15 / 35 / 60 |

**Utility**
| Enchant | I | II | III | Enchanting Level |
|---------|---|----|----|-----------------|
| **Scholar** | +5 INT | +10 | +18 | 10 / 25 / 50 |
| **Strength** | +5 STR | +10 | +18 | 10 / 25 / 50 |
| **Lucky** | +5 LUK | +10 | +18 | 10 / 25 / 50 |
| **Gatherer** | +1 item per gather | +2 | +3 | 5 / 20 / 45 |
| **Haste** | Gathering cooldown -5s | -10s | -15s | 10 / 30 / 55 |
| **XP Siphon** | +5% skill XP | +10% | +20% | 20 / 40 / 65 |

### Enchanting Mechanics
- Requires: Enchanting materials + Enchanting skill level ≥ required tier level
- Materials: Tier I = common gems (Sapphire), Tier II = rare gems (Emerald/Ruby), Tier III = Diamond + Dark Essence
- **Overenchanting:** attempting to add past the slot cap has 40% chance to destroy the enchant (not the item) and return half the materials
- **Disenchant:** break any enchanted item to recover enchanting materials (item is destroyed)
- **Upgrade path:** Tier I enchant can be upgraded to Tier II in-place (costs Tier II materials, no slot used)

---

## 10. Economy Loop

### Currencies
| Currency | Use | Earn |
|----------|-----|------|
| **Coins** 🪙 | In-game — gear, crafting, NPC shop | Monsters, gathering, PvP wins, daily bonus, quests |
| **Orbs** ✨ | Premium — crates, marketplace premium listings, cosmetics | Daily login, chat activity, voice, real money |

**Design rule:** Coins are the grind currency. Orbs are the premium layer. Nothing gameplay-essential should require real money.

### Coin Sources
| Source | Amount |
|--------|--------|
| Monster defeat | Random range per monster definition |
| Gathering (mine/chop/fish) | `1 + (skillLevel / 5)` per action |
| PvP win | 10 flat |
| Daily first message | 5 flat |
| NPC vendor sell | `ItemDefinition.SellPrice` |
| Daily quest completion | 20–100 (scales with quest difficulty) |
| Weekly boss kill | 200–500 |
| Opening drop boxes | Coins as possible loot |
| Selling duplicate peepos | Partial refund of purchase price |
| Cooking and selling food | Player-driven food economy |
| Login streak bonuses | Escalating daily rewards |
| Crafting items | Small coin bonus per craft, scales with item tier |
| Enchanting items | Small coin bonus per enchant, scales with enchant rarity |
| Cooking successfully | Coin bonus per cooked item, scales with cooking level |

### Coin Sinks
| Sink | Cost |
|------|------|
| NPC shop (tier 1–3 gear) | `ItemDefinition.BuyPrice` |
| Crafting material costs | Per recipe |
| Enchanting | Material costs |
| Respawn fee (optional future) | TBD |

### Loot Crates (General — not just peepos)

Crates drop from monsters, quests, and bosses. They contain a random roll from a loot pool — not just peepos. Think Runescape clue scrolls meets Diablo loot boxes.

**Crate Contents Pool (weighted by crate tier):**
- Coins (flat amount — always possible)
- Potions (health, mana, stat boost)
- Food (cooked fish, stew, special meals)
- Weapons (any tier up to crate's max tier)
- Armor pieces (helmet, chest, legs, boots, shield)
- Staves, bows, arrows
- Peepo collectibles
- Enchanting materials
- Crafting materials (ores, logs, gems)
- **Special ability items** (see below)

**Special Ability Items — rarest crate drops:**
These are permanent-use items that grant effects. Harder to get = more valuable = drives the economy.

| Item Type | Effect | Rarity |
|-----------|--------|--------|
| **Enchantment Scroll** | Permanently adds 1 enchant to any item (bypasses Enchanting skill req) | Rare |
| **Stat Tome** | Permanently +1 to a chosen stat | Epic |
| **Ability Crystal** | Unlocks a class ability one level early | Epic |
| **Elemental Essence** | Permanently adds an element to a weapon that had none | Rare |
| **Prismatic Shard** | Crafting ingredient for Prismatic Plate (endgame) | Legendary |
| **Summon Scroll** | Grants a summon without defeating the boss | Legendary |
| **Rune of Mastery** | Upgrades an enchant tier (Lifesteal I → Lifesteal II) | Rare |

**Value hierarchy:** Items are valued by how much strength they give players.
- Stat Tomes and Ability Crystals are the most sought after — permanent power gains
- Enchantment Scrolls are reliable income on the marketplace
- Legendary scrolls/shards are whale-tier — people will pay big on the marketplace

**Crate Tiers:**
| Crate | Source | Max Item Tier |
|-------|--------|--------------|
| Wooden Crate | Common monster drops | Tier 1–2 gear, Common/Uncommon peepos |
| Iron Crate | Quest rewards, rare monster drops | Tier 2–3 gear, Rare peepos possible |
| Gold Crate | Boss kills, daily streak milestone | Tier 3–4 gear, Epic peepos possible |
| Void Crate | Weekly boss, endgame only | Tier 5 gear, Legendary peepos, Special ability items |

### NPC Shop
Available via `/rpg shop [category]`
- Sells all Tier 1–3 gear at `BuyPrice`
- Sells all consumables at `BuyPrice`
- Sells crafting materials at market price
- **Does not sell** Tier 4–5 gear — those require crafting or drops

### Player Marketplace
Available via `/market`
- List items for coins or orbs
- 5% transaction fee (coin sink)
- 7-day listing expiration

---

## 11. Skills & Gathering

### 8 Skills
| Skill | Trained By | Produces |
|-------|-----------|---------|
| Combat | Fighting monsters | XP, kills |
| Mining | `/mine` | Ores, gems — tier gated by level |
| Woodcutting | `/chop` | Logs — tier gated by level |
| Fishing | `/fish` | Fish — tier gated by level |
| Smithing | `/craft` (weapons/armor) | Equipment |
| Alchemy | `/craft` (potions/consumables) | Consumables, enchanting materials |
| Cooking | `/cook` | Food that heals HP outside combat |
| Enchanting | `/enchant` | Enchanted items |

### Gathering Formula
- XP per action: `15 + (skillLevel * 2)`
- Coins per action: `1 + (skillLevel / 5)`
- Items per action: `1 + (skillLevel / 10)`
- Cooldown: **30 seconds** (reduced from 60)

### Skill Level Cap: 99

### Resource Tiers (RS-style — skill level gates what you can gather)

**Mining**
| Level | Resource | Use |
|-------|----------|-----|
| 1 | Copper Ore | Basic smithing |
| 10 | Iron Ore | Common smithing |
| 20 | Silver Ore | Jewelry, alchemy |
| 30 | Gold Ore | High value, jewelry |
| 40 | Mithril Ore | Mid-tier smithing |
| 55 | Adamantite Ore | High-tier smithing |
| 70 | Adamantium Ore | Endgame smithing |
| 85 | Voidstone | Tier 5 crafting |
| Any | Gems (Sapphire→Diamond) | Random chance, scales with level |

**Fishing**
| Level | Resource | Heals (cooked) |
|-------|----------|----------------|
| 1 | Raw Shrimp | 3 HP |
| 10 | Raw Trout | 7 HP |
| 20 | Raw Salmon | 12 HP |
| 35 | Raw Tuna | 18 HP |
| 50 | Raw Lobster | 25 HP |
| 65 | Raw Swordfish | 35 HP |
| 80 | Raw Shark | 50 HP |
| 90 | Raw Abyssal Eel | 70 HP + 20 MP |

**Woodcutting**
| Level | Resource | Use |
|-------|----------|-----|
| 1 | Normal Logs | Basic crafting |
| 15 | Oak Logs | Bow material |
| 30 | Willow Logs | Better bows |
| 45 | Maple Logs | Mid-tier crafting |
| 60 | Yew Logs | High-tier bows |
| 75 | Magic Logs | Endgame staves |
| 90 | Void Wood | Tier 5 crafting |

### Cooking & Food
- `/cook <fish>` turns raw fish into cooked food
- Higher cooking level = lower burn chance: `max(0%, 40% - cookingLevel * 0.5%)`
- Burnt food: sells to vendor for 1 coin
- **Food is usable in combat only — same as potions.** No out-of-combat passive healing.

#### Raw Food — DO NOT EAT
Eating uncooked food **poisons you** — applies Poisoned status (DoT: flat damage per turn for 2 turns). Community-driven mechanic: funny, punishing, and entirely your fault. No warning given in the command. You will learn.

| Raw Food | Poison DoT |
|----------|-----------|
| Any raw fish | 5–10 dmg/turn for 2 turns |
| Raw meat (future) | 8–15 dmg/turn for 3 turns |

#### Cooked Food — Heal Values
All cooked food is usable in combat. Healing scales with fish tier.

| Food | Cooking Level | HP Healed | Special |
|------|--------------|-----------|---------|
| Shrimp | 1 | 5 HP | — |
| Trout | 10 | 12 HP | — |
| Salmon | 20 | 20 HP | — |
| Tuna | 35 | 30 HP | — |
| Lobster | 50 | 42 HP | — |
| Swordfish | 65 | 58 HP | — |
| Shark | 80 | 80 HP | — |
| Abyssal Eel | 90 | 100 HP + 25 MP | Dual restore |

#### Special Meals (Cooking endgame — requires multiple ingredients)
| Meal | Ingredients | Effect | Cooking Level |
|------|------------|--------|--------------|
| Fish Stew | Any fish + Normal Logs (fire) | HoT: 15 HP/turn for 3 turns | 15 |
| Warrior's Feast | Shark + Iron Ore (seasoning) | +10% STR for 3 turns | 55 |
| Mage's Brew | Abyssal Eel + Silver Ore | +10% INT + 30 MP | 70 |
| Ranger's Ration | Swordfish + Oak Logs | +10% DEX + crit chance +5% for 3 turns | 50 |
| Grand Feast | Shark + Lobster + Magic Logs | Heal 60 HP + all stats +5% for 4 turns | 85 |

**Design intent:**
- Raw food → poison (funny, punishing, no warning)
- Cooked fish → reliable HP, scaled to tier. Cheaper to get than potions.
- Stew → HoT, fills a niche potions don't — spread healing over turns
- Special meals → endgame Cooking payoff, temporary stat buffs potions can't give
- Potions = reliable burst, buyable, expensive. Food = skill-gated, player-cooked, more variety.

---

## 12. Progression & Tiers

### Level Cap: 100
`MaxLevel = 100` — defined as a named constant. Raise to 150 or 200 in a future expansion by changing the constant and adding a Tier 6 seeder.

### XP Formula
```
XpToLevel(level) = floor(100 * level ^ 1.5)
```

### Seeder Architecture
Each content tier lives in its own seeder file, safe to run idempotently on startup (checks by name before inserting):
- `SeedGameDataCommand.cs` — Base: Tier 1 monsters + items
- `GameExpansionSeeder.cs` — Tier 2–3: monsters + items (62 items, 52 monsters)
- `GameTier2Seeder.cs` — Tier 4–5: Lv60–100 monsters + items + zones *[TODO]*
- `GameTierNSeeder.cs` — Future tiers follow same pattern

### Zone Progression & Elemental Identity

Each zone has a dominant element that its enemies **use** and a clear weakness players can exploit. Mages who bring the right spell, Rangers who craft the right arrows, and Warriors who equip the right elemental weapon will deal significantly more damage. Ignoring elements is fine early on — punishing at endgame.

| Zone | Levels | Enemy Element | **Weak To** | **Resists** | Flavor |
|------|--------|--------------|-------------|-------------|--------|
| Plains | 1–8 | None / Physical | Anything | — | Tutorial, safe, bright |
| Forest | 8–18 | Earth 🪨, Poison ☠️ | Fire 🔥, Lightning ⚡ | Water 💧, Earth 🪨 | Inhabited, slightly dangerous |
| Swamp | 18–30 | Water 💧, Poison ☠️ | Lightning ⚡, Fire 🔥 | Water 💧, Poison ☠️ | Murky, diseased, loud at night |
| Mountains | 30–42 | Earth 🪨, Ice ❄️ | Fire 🔥, Water 💧 | Earth 🪨, Ice ❄️ | Harsh, rewarding, vertical |
| Dungeon | 38–52 | Dark 🌑, Undead | Holy ✨, Fire 🔥 | Dark 🌑, Shadow 🌫️ | Claustrophobic, echoing, undead everywhere |
| Tundra | 45–58 | Ice ❄️, Wind 🌪️ | Fire 🔥, Earth 🪨 | Ice ❄️, Wind 🌪️ | Frozen, blinding blizzards, slow movement |
| Volcano | 50–65 | Fire 🔥, Earth 🪨 | Water 💧, Ice ❄️ | Fire 🔥 (immune to Burn) | Hostile, lava floors, everything is angry |
| Abyss | 62–75 | Dark 🌑, Shadow 🌫️ | Holy ✨, Light 💡 | Dark 🌑, Shadow 🌫️, Poison ☠️ | Ancient, suffocating, no light of its own |
| Corrupted Forest | 70–82 | Void 🕳️, Poison ☠️ | Holy ✨, Fire 🔥 | Poison ☠️, Dark 🌑 | The Forest — but wrong. Something happened here. |
| Celestial Spire | 78–90 | Holy ✨, Light 💡 | Dark 🌑, Shadow 🌫️ | Holy ✨, Light 💡 | Bright but cold, ancient and indifferent |
| Void Realm | 88–100 | Void 🕳️ | Holy ✨ (partial) | Everything else | No light, no rules, Void ignores most resists |

#### Zone Design Notes
- **Plains** enemies have no element — purely for learning the combat loop before elements matter
- **Dungeon** enemies are undead — Holy deals +75% to them specifically (Smite, Holy, Consecrate are strong here)
- **Volcano** enemies are immune to **Burn** — Fire spells still deal damage, but the proc does nothing. Water and Ice are the go-to.
- **Abyss** enemies resist three elements — the game starts punishing unprepared casters here
- **Void Realm** enemies use Void, which ignores resists — but they are weak to Holy as their one exploitable gap
- **Corrupted Forest** feels wrong on purpose — it's the same Forest zone biome but broken. Lore-significant.

#### Practical Guide (what to bring per zone)
| Zone | Mage brings | Ranger brings | Warrior brings |
|------|------------|--------------|----------------|
| Forest | Fire / Thunder | Poison Arrows | Any weapon |
| Swamp | Thunder / Firaga | Lightning Arrows | Fire-element weapon |
| Mountains | Firaga / Flood | Fire Arrows | Fire Blade |
| Dungeon | Holy / Smite | Holy Arrows | Holy-enchanted weapon |
| Tundra | Firaga / Quake | Fire Arrows | Fire Blade |
| Volcano | Flood / Blizzaga | Water Arrows | Frost weapon |
| Abyss | Holy / Dark Pulse | Holy Arrows | Holy/Light weapon |
| Corrupted Forest | Holy / Firaga | Holy Arrows | Holy weapon |
| Celestial Spire | Gravity / Dark Pulse | Shadow Arrows | Dark weapon |
| Void Realm | Holy / Meteor | Holy Arrows | Holy + Void resist gear |

---

## 13. PvP System

### Class Triangle
Applied as a ±15% damage modifier in all PvP:
- Warrior > Ranger > Mage > Warrior
- Rogue > Mage; Cleric > Rogue

### Stats in PvP
PvP uses full RPG character stats + equipped gear (not Discord level).
Chat level = RPG level so this naturally reflects community engagement.

### PvP Rewards
- Winner: 10 coins + XP based on opponent's level
- Loser: No coin penalty (unlike PvE death — PvP is opt-in)

### Future: Ranked PvP + Leagues
- Seasonal ranked ladder
- Draft/ban format for tournament play (Lv 60+)
- See gradual rollout plan

---

## 14. Endgame

### What Endgame Players Chase
1. **Tier 5 named unique items** — 0.5–1% drop rate, tradeable
2. **Prismatic Plate** — all-element resist set, requires rare crafting materials from Void Realm
3. **Enchanting** — maxing out enchant slots on endgame gear
4. **Skill level 99** in all 8 skills
5. **Weekly boss events** — rotating boss, rare cosmetic/gear rewards
6. **Ranked PvP** — seasonal ladder

### Endgame Loop
Kill Void Realm monsters → get rare materials → craft/enchant Tier 5 gear → use it to kill Void Realm harder content → trade surplus on marketplace

### Monster Roster by Zone

Seeder status: Tier 1 (`SeedGameDataCommand.cs`), Tier 2–3 (`GameExpansionSeeder.cs` — 52 monsters). Tier 4–5 seeder is `[TODO]`. **Need 200+ total monsters across all zones.**

#### Monster Variant System
Every named monster has 3 variants — same stats template, different difficulty:
- **Normal** — base stats
- **Elite** — 1.5× HP/ATK/DEF, drops at +1 rarity tier, prefix "Feral" / "Ancient" / "Veteran"
- **Champion** — 2.5× stats, 2 special abilities, rare drop guaranteed, prefix "Blight" / "Corrupted" / "Wrathful"

Champions are rare spawns (~5% chance instead of normal). They're the hardest non-boss encounter.

---

**Zone: Plains (Lv 1–10)** — Tutorial zone. Forgiving. Bright.
| Monster | Lv | Element | Special Ability | Notable Drop |
|---------|----|---------|-----------------|----|
| Slime | 1 | Water | — | Slime Jelly |
| Goblin Scout | 2 | None | — | — |
| Goblin Warrior | 3 | None | Shield Block (50% dmg reduction 1 turn) | Iron Dagger |
| Giant Rat | 4 | Poison | Gnaw (Bleeding) | Rat Tail |
| Scarecrow | 6 | Wind | Gust (Pushed) | — |
| Goblin Shaman | 7 | Fire | Fireball | Ember Shard |
| Mud Slime | 8 | Earth | Stick (Rooted) | Slime Jelly |
| Scarecrow Champion | 10 | Wind | Gust + Confuse | — |

**Zone: Forest (Lv 8–20)** — Inhabited. Something watches you.
| Monster | Lv | Element | Special Ability | Notable Drop |
|---------|----|---------|-----------------|----|
| Wisp | 9 | Light | Blind (Blinded) | Wisp Dust |
| Woodland Spider | 10 | Poison | Web (Rooted + Slow) | Spider Silk |
| Werewolf Scout | 11 | None | Pounce (Bleeding) | Fang |
| Bandit | 13 | None | Steal (removes 1 item) | Iron Dagger |
| Dryad | 15 | Earth | Entangle (Rooted AoE) | Bark Sap |
| Bandit Captain | 16 | None | Battle Cry + Power Strike | Steel Sword |
| Dark Wisp | 17 | Shadow | Drain (Drained status) | Wisp Dust |
| Forest Troll | 18 | Earth | Regenerate (heals 5% HP/turn) | Troll Hide |
| Werewolf | 19 | None | Frenzy (Berserk on self) | Fang |
| Elder Dryad | 20 | Earth | Thorns (reflects 15% dmg) + Entangle | Bark Armor (Rare) |

**Zone: Swamp (Lv 18–30)** — New zone. Poison/Earth. Visibility is low. Things bubble up from the water.
| Monster | Lv | Element | Special Ability | Notable Drop |
|---------|----|---------|-----------------|----|
| Bog Slime | 18 | Poison | Corrode (Corroded) | Toxin Gland |
| Swamp Toad | 20 | Water | Tongue Grab (Rooted 2 turns) | — |
| Plague Rat | 21 | Poison | Infect (Poisoned + Cursed) | Plague Essence |
| Vine Horror | 23 | Earth | Constrict (Rooted + Bleeding) | Vine Whip |
| Will-o-Wisp | 24 | Light | Lure (Charmed 1 turn) | Wisp Dust |
| Swamp Witch | 26 | Poison | Hex (Cursed + Confused) | Witch's Brew |
| Hydra | 28 | Water | Multi-Head Strike (3 hits) + Regen | Hydra Scale |
| Bog Shambler | 29 | Earth | Slam (Stunned) + Toxic Aura (Poisoned AoE) | — |
| Swamp Witch Champion | 30 | Poison | Hex + Doom | Witch's Staff (Rare) |

**Zone: Mountains (Lv 25–38)** — Harsh. Rewarding. Wind-dominant.
| Monster | Lv | Element | Special Ability | Notable Drop |
|---------|----|---------|-----------------|----|
| Stone Golem | 25 | Earth | Tremor (Rooted AoE) | Golem Core |
| Harpy | 26 | Wind | Screech (Frightened) | Harpy Feather |
| Ice Wolf | 27 | Ice | Frost Bite (Frozen) | Wolf Pelt |
| Mountain Troll | 28 | None | Boulder Throw (Stunned) | Troll Hide |
| Wind Sprite | 29 | Wind | Updraft (Pushed + Slow) | — |
| Frost Harpy | 30 | Ice | Blizzard + Screech | Harpy Feather |
| Rock Golem | 32 | Earth | Quake AoE + Shield | Golem Core (Titan material) |
| Snow Yeti | 34 | Ice | Blizzard AoE + Frozen | Yeti Fur |
| Crystal Slime | 35 | Ice | Crystal Shard (Shattered) | Crystal Shard (Shiva material) |
| Wyvern | 37 | Wind | Dive Bomb (high damage) + Push | Wyvern Scale |
| Storm Eagle Champion | 38 | Lightning | Chain Lightning + Eagle Eye | — |

**Zone: Dungeon (Lv 35–52)** — Dark. Claustrophobic. Undead-heavy.
| Monster | Lv | Element | Special Ability | Notable Drop |
|---------|----|---------|-----------------|----|
| Skeleton Warrior | 35 | None | Bone Shield (DEF +50% 1 turn) | Bone Shard |
| Skeleton Archer | 36 | None | Pinning Shot (Rooted) | Bone Arrow |
| Giant Bat | 37 | Shadow | Drain + Blind | — |
| Cursed Armor | 38 | Dark | Curse (Cursed) + Taunt | Dark Ore |
| Zombie | 39 | Poison | Infect (Poisoned + Cursed) | Rotted Flesh |
| Vampire Thrall | 40 | Shadow | Drain + Charm | Bloodvial |
| Gargoyle | 42 | Earth | Stone Form (immune 1 turn) | Stone Fragment |
| Necromancer | 43 | Dark | Raise Dead (summons Skeleton mid-battle) + Silence | Dark Essence |
| Dark Knight | 45 | Dark | Weakened AoE + Power Strike | Dark Plate |
| Vampire | 48 | Shadow | Drain + Charm + Regenerate | Bloodvial, Heartseeker (0.5%) |
| Lich Apprentice | 50 | Dark | Doom + Silence + Dispel | Dark Essence |
| Dungeon Mimic | 52 | None | Surprise Attack (3× dmg first turn) + drops random rare item | Random Rare Item |

**Zone: Tundra (Lv 45–58)** — New zone. Ice/Lightning. Cold and hostile. Things have adapted to kill.
| Monster | Lv | Element | Special Ability | Notable Drop |
|---------|----|---------|-----------------|----|
| Frost Wraith | 45 | Ice | Frozen AoE + Slow | Frost Essence |
| Thunder Hawk | 46 | Lightning | Dive + Stun | Hawk Feather |
| Glacial Golem | 48 | Ice | Permafrost (Frozen + Shattered) | Ice Core |
| Blizzard Wolf | 49 | Ice | Howl (Frightened AoE) + Frostbite | Ice Fang |
| Arctic Serpent | 50 | Ice | Coil (Rooted 3 turns) | Serpent Scale |
| Thunder Elemental | 52 | Lightning | Chain Lightning AoE + Stun | Thunder Core |
| Frost Giant | 54 | Ice | Glacier Throw (Frozen AoE) + Tremor | Giant's Club |
| Permafrost Golem | 56 | Ice | Immune to Fire · Frozen AoE | Ice Core |
| Rimebound Dragon | 58 | Ice | Blizzaga + Slow AoE | Dragonscale (Ice) |

**Zone: Volcano (Lv 48–62)** — Hostile. Fire/Earth dominant.
| Monster | Lv | Element | Special Ability | Notable Drop |
|---------|----|---------|-----------------|----|
| Ember Imp | 48 | Fire | Fireball + Burn AoE | Ember Core |
| Lava Crawler | 50 | Fire | Melt Armor (Corroded — Fire resist -30%) | — |
| Fire Elemental | 52 | Fire | Wildfire AoE (Burn all) | Ember Core (Ifrit material) |
| Obsidian Golem | 54 | Earth | Immune to Fire · Tremor | Obsidian Shard |
| Magma Serpent | 55 | Fire | Coil + Burn AoE | Magma Scale |
| Flame Drake | 57 | Fire | Firaga + Berserk (self) | Dragonscale |
| Lava Wyrm | 58 | Fire | Dive (Burn + Rooted) | Lava Scale (Leviathan material) |
| Volcanic Troll | 60 | Fire | Regenerate + Slam AoE | Troll Hide (Fire) |
| Infernal Drake | 62 | Fire | Firaga + Terrifying Roar (Frightened AoE) | Dragonscale |

**Zone: Abyss (Lv 60–75)** — Ancient. Shadow/Dark dominant. Lore-heavy.
| Monster | Lv | Element | Special Ability | Notable Drop |
|---------|----|---------|-----------------|----|
| Shadow Wraith | 60 | Shadow | Drain + Phase (untargetable 1 turn) | Shadow Essence |
| Abyssal Hound | 62 | Dark | Howl (Weakened AoE) + Bite | Dark Fang |
| Dark Elemental | 64 | Dark | Gravity + Silence | Dark Core |
| Soul Eater | 65 | Shadow | Drain + Cursed | Soul Fragment |
| Demon Knight | 67 | Dark | Dark Pulse + Power Strike + Weakened | Dark Plate |
| Banshee | 68 | Shadow | Wail (Frightened + Paralyzed AoE) | — |
| Ancient Vampire | 70 | Shadow | Drain + Charm + Regenerate (50% HP/turn) | Heartseeker Dagger (1%) |
| Void Tendril | 71 | Void | Exposed AoE + Slow | Void Fragment |
| Abyssal Horror | 73 | Shadow | Confuse + Drain + Doom | Void Fragment |
| Lich | 75 | Dark | Doom + Silence + Raise Dead + Dispel | Wraithcaller Staff (1%), Odin material |

**Zone: Corrupted Forest (Lv 65–78)** — New zone. Forest gone wrong. Everything you fought before, but cursed.
| Monster | Lv | Element | Special Ability | Notable Drop |
|---------|----|---------|-----------------|----|
| Blighted Dryad | 65 | Poison | Toxic Thorns (Poison AoE + Bleeding) | Blight Sap |
| Corrupted Werewolf | 67 | Dark | Frenzy + Curse | Dark Fang |
| Plague Treant | 68 | Poison | Toxic Aura (Poisoned AoE) + Entangle | Plague Bark |
| Blight Wisp | 70 | Shadow | Drain + Confuse | Blight Essence |
| Corrupted Troll | 72 | Poison | Regenerate + Infect AoE | — |
| Wraithwood Sentinel | 75 | Shadow | Taunt + Phase + Drain | Shadow Wood |
| Blighted Hydra | 77 | Poison | Multi-Head + Toxic Breath AoE + Regenerate | Blight Scale |
| Corrupted Elder | 78 | Dark | Curse + Doom + Confuse | Blight Crown (Rare) |

**Zone: Celestial Spire (Lv 72–88)** — Holy/Light dominant. Cold and bright. Something was built here.
| Monster | Lv | Element | Special Ability | Notable Drop |
|---------|----|---------|-----------------|----|
| Seraph | 72 | Holy | Smite + Ward (self) | Holy Feather |
| Fallen Knight | 74 | Dark | Taunt + Dark Pulse + Silence | Fallen Plate |
| Holy Construct | 76 | Holy | Barrier (self) + Consecrate AoE | Holy Core |
| Void-touched Angel | 78 | Void | Exposed AoE + Gravity | Void Feather |
| Archdemon | 80 | Dark | Doom + Dark Pulse + Dispel | Soulrender (1%), Sunbreaker (1%) |
| Celestial Wyrm | 82 | Holy | Firaga (Holy element) + Ward AoE | Dragon Scale (Holy) |
| Fallen Paladin | 84 | Dark | Divine Wrath (reflects) + Curse + Silence | Celestial Plate fragment |
| Radiant Horror | 86 | Light | Blinded AoE + Confuse + Paralyzed | — |
| Celestial Dragon | 88 | Holy | Blizzaga (Holy) + Ward AoE + Immune to Dark | Alexander material |

**Zone: Void Realm (Lv 85–100)** — Void dominant. Endgame. Lore-heavy. The truth lives here.
| Monster | Lv | Element | Special Ability | Notable Drop |
|---------|----|---------|-----------------|----|
| Void Stalker | 85 | Void | Exposed AoE + Phase | Voidstone |
| Reality Fracture | 87 | Void | Gravity + Doom + Confuse | Void Fragment |
| Blood Moon Werewolf | 88 | Void | Frenzy + Exposed + Mark | The Pale Bow (1%) |
| Void Colossus | 90 | Void | Tremor AoE + Exposed AoE + Immune Fire/Ice | Prismatic Shard |
| Corrupted Seraph | 92 | Void | Dispel + Doom + Drain | Void Feather |
| Void Dragon | 95 | Void | Meteor (Void) + Exposed AoE + Berserk (self) | Voidstone |
| Ancient Dragon | 97 | Void | Firaga/Blizzaga/Thundaga + Immune all elements | Bahamut material |
| Archdemon (Void) | 100 | Void | Doom + Dispel + Corrode all resists | Aegis of the Forgotten (1%) |

---

### Boss System

Bosses are separate from normal/elite/champion encounters. They're triggered via `/rpg boss` (weekly event or zone challenge). They don't replace regular combat — they're opt-in fights with much higher stakes.

#### Boss Design Pillars
- **Always challenging** — scaled to be hard even for max-geared players. Never "over-leveled" out of danger.
- **Mechanic-specific** — each boss is designed around countering specific playstyles. A boss that silences you constantly destroys Mages. A boss with permanent Taunt punishes Rogues.
- **The Unkillable Bosses** — "Counter-All" bosses that resist, block, or punish every common strategy. Require coordination and adaptation. The hardest content in the game.

#### Zone Bosses (unlock when zone is cleared)
| Boss | Zone | Lv | Gimmick | Special Drop |
|------|------|----|---------|-------------|
| **Gorehide** | Plains | 10 | Berserks below 30% HP — can't be stunned | Rare Ring |
| **The Briar King** | Forest | 20 | Immune to all debuffs; applies Entangle every turn | Bark Crown |
| **Grimfang** | Swamp | 30 | Applies Doom each turn; Cleanse costs Cleric's entire turn | Plague Staff |
| **Stonecrown** | Mountains | 38 | Immune to physical damage when Fortified (self); Fortify breaks only on magic hits | Mountain Heart |
| **Lord of the Dungeon** | Dungeon | 52 | Raises defeated monsters; has 4 Skeleton adds | Lich's Crown |
| **Ragnarock** | Tundra | 58 | Frozen hits deal 2× normal; immune to fire | Glacial Core |
| **The Flame Sovereign** | Volcano | 62 | Gains Barrier each time you deal damage; must break Barrier to hit him | Infernal Heart |
| **The Void Below** | Abyss | 75 | Permanently Silenced anyone who uses magic 3+ times in a row | Abyss Key |
| **The Pale Judge** | Corrupted Forest | 78 | All healing reduced by 75% (Cursed aura, can't be cleansed) | Blight Crown |
| **Exarch of Light** | Celestial Spire | 88 | Dispels all buffs at start of each turn; immune to Shadow | Celestial Sigil |

#### Raid Bosses (Weekly — Level 60+ recommended)
| Boss | Lv | What Makes It Hard | Exclusive Drop |
|------|----|--------------------|---------------|
| **The Wandering Lich** | 80 | Silences, Dooms, raises dead. If Lich is killed while any undead remain → resurrects with 50% HP | Odin Scroll |
| **Titanfall** | 85 | Immune to all status effects. Deals damage based on your max HP — tankier = more damage | Titan Heart |
| **The Dreaming Horror** | 90 | Confuses the party randomly each turn. Confused hits deal 3× to teammates | Dream Shard |
| **Void Architect** | 95 | Corrodes ALL your elemental resists every 2 turns. No element is safe. | Prismatic Shard |

#### Counter-All Bosses (Endgame — max gear required)
These bosses are designed so NO single strategy wins. They actively punish whatever you're doing well.

| Boss | Lv | Counters | Notes |
|------|----|--------------------|-------|
| **The Mirror** | 95 | Copies your class abilities. If you Power Strike, it Power Strikes back. | Forces players to use abilities they don't usually run |
| **Null** | 98 | All elemental damage dealt to it is absorbed as HP. Only Void deals neutral. Void restores its MP. | Every element feeds it. Void sustains it. Pure physical or die. |
| **The Arbiter** | 99 | Immune to debuffs. Dispels buffs on itself every turn. Heals when attacked by magic. | Physical-only, no status, no buffs. Just raw stats. |
| **The End** | 100 | Doom on turn 1 (expires turn 6). Dispels every 2 turns. Immune to all elements. Berserks at 30% HP. Counters every physical hit. | The final wall. Intended to be a 6-month goal for new players. |

### Leaderboard

`/rpg leaderboard` — top 10 players by total monsters killed. Implemented.

Future leaderboard tabs (add to embed as select menu):
- Most kills
- Highest level
- Richest (CoinBalance)
- Top skill (Mining, Fishing, etc.)
- PvP win rate (when ranked is live)

---

## 15. Gradual Rollout Plan

New players unlock mechanics by level so they aren't overwhelmed:

| Level | Unlocks |
|-------|---------|
| 1 | 1v1 combat, 3 elements (Fire/Ice/Lightning), Attack/Defend/Flee |
| 5 | `/equip`, NPC shop |
| 10 | All 12 elements, Gathering skills, Marketplace |
| 15 | Crafting (Smithing) |
| 20 | Positioning (Front/Mid/Back), Class abilities tier 1 |
| 25 | Enchanting |
| 30 | Class abilities tier 2, Alchemy |
| 35 | Morale system, Buff/debuff full system |
| 40 | Press Turn (full icon system), Ranked PvP |
| 50 | Class abilities tier 3, Support bonds |
| 60+ | Void Realm access, Named unique drops, Weekly boss |

---

## 16. Monetization

**Principle:** No P2W. Orbs are earnable. Coins are earnable. Real money buys convenience and cosmetics.

| Purchase | Cost | Notes |
|----------|------|-------|
| Orb packs | $0.99–$9.99 | Earnable free via daily/chat/voice |
| Torvex Gold ($4.99/mo) | Subscription | Premium perks TBD — must not be P2W |
| Loot crates | Orbs | Cosmetic items, Peepo collectibles |
| Artist skins | Orbs | Item reskins, no stat difference |
| Crafting TMs | Orbs | Unlock special recipes — available as rare drops too |
| Fusion | Orbs | Endgame mechanic — combine 2 items |
| Seasonal passes | Orbs | Battle pass model with cosmetic rewards |

---

---

## 17. Peepo Collectibles

### Vision
Peepos are the social collectible layer of the game. Like Pokémon cards meets Discord emojis. You earn coins, you buy peepos, you show them off, you trade them. Every peepo has personality. Rare ones feel special.

### Decoupled from Server Emojis
Peepos are their own standalone database — not just mirrored server emojis. They have:
- Name, image URL, rarity, flavor text
- Drop sources (shop, crates, monster drops, limited edition)
- A price that scales with rarity

Server emojis can still be *promoted* to peepos by admins, but peepos are not dependent on emojis existing.

### Rarity Tiers & Pricing

The pricing curve is intentionally steep. Common peepos are casual purchases. Legendaries are genuine endgame status — not achievable in a week.

| Rarity | Crate Weight | Coin Price | Orb Price | Notes |
|--------|-------------|-----------|-----------|-------|
| Common | 62% | 250 coins | — | Shop always available. ~1–2 days of activity. |
| Uncommon | 25% | 1,500 coins | — | Shop always available. ~1 week of grinding. |
| Rare | 9% | 10,000 coins | 100 orbs | Shop + crates. Serious goal — ~1 month. |
| Epic | 3.5% | — | 400 orbs | **Crates only** + rare boss drops. Orbs only, no coin direct buy. |
| Legendary | 0.5% | — | — | **Crates only** + boss drops. Never sold directly. True endgame. |
| Limited | — | — | Special | Seasonal / events only, never returns. |

**Design intent:** A dedicated player grinding daily (~300 coins/day) reaches Rare in ~1 month.
Epic requires orbs — either earned slowly or purchased. Legendary is never guaranteed — you chase it through crates and boss kills.
No matter how much someone grinds, they cannot guarantee a Legendary. That's the point.

### How to Get Peepos
1. **Coin shop** — Common + Uncommon always available for coins; Rare available for coins or orbs
2. **Peepo Crates** — random pull from the crate pool. Higher rarity = exponentially rarer.
3. **Boss drops** — specific legendary/epic peepos are loot table entries on named bosses only
4. **Quest rewards** — certain quest chains award a unique peepo (one-time, not re-earnable)
5. **Seasonal/limited** — holiday events, milestones, never re-released

### Peepo Crates

One crate type. No "basic" vs "premium" split — the rarity curve does the work.

| Crate | Cost | Odds |
|-------|------|------|
| **Peepo Crate** | 5,000 coins | Common 62% · Uncommon 25% · Rare 9% · Epic 3.5% · **Legendary 0.5%** |
| **Limited Crate** | Event only | Guaranteed Rare+, chance at Limited edition |

**Expected pulls for Legendary:** ~200 crates = ~1,000,000 coins at pure grind rate.
This is intentional — Legendary peepos should be rare enough to be a genuine flex.
Players who get one from a boss drop or lucky crate pull are genuinely lucky.

**Priority:** Get coin earning rates right before building crates. Players need a healthy coin economy first so crate pricing feels fair, not punishing.

### Commands
- `/peepo shop` — browse buyable peepos by rarity
- `/peepo buy <name>` — buy a specific peepo with coins or orbs
- `/peepo crate` — open a Peepo Crate (5,000 coins)
- `/peepo collection` — view your peepos
- `/peepo gift @user <name>` — gift a peepo
- `/peepo trade @user` — offer a peepo trade (uses existing trade system)
- `/peepo showcase` — set your displayed peepo (shows in profile/stats)

### Economy
- Peepos are tradeable player-to-player
- Peepos are listable on marketplace for coins
- Duplicate peepos can be sold to NPC for partial coin refund
- Rarity drives demand — Legendary peepos become status symbols

---

## 18. Summons

### Vision
Summons are powerful beings you call into combat. They have lore, personality, and a massive moment when they appear. Not just a spell — an event.

### How to Earn Summons
- Boss drops (e.g. defeat Fire Elemental → chance at Ifrit summon item)
- End of quest chains in specific zones
- Rare monster drops in matching element zones
- Crafted from rare zone materials

### How Summons Work in Combat
- Equip a summon in the Accessory slot
- Costs MP + a **Summon Gauge** that builds during combat (like Limit Break)
- One summon per combat
- Massive damage or utility effect matching the summon's element
- Each summon has a secondary effect (status, heal, buff)

### Summon Roster (draft)

| Summon | Element | Source | Effect |
|--------|---------|--------|--------|
| **Ifrit** | Fire | Fire Elemental drop | Massive Fire AoE, applies Burn to all |
| **Shiva** | Ice | Crystal Slime drop | AoE Freeze, restores 30% party HP |
| **Ramuh** | Lightning | Harpy drop | Chain Lightning, stuns all enemies |
| **Titan** | Earth | Rock Golem drop | Root all enemies, +50% DEF for 2 turns |
| **Leviathan** | Water | Lava Wyrm drop | Drench all, massive damage |
| **Sylph** | Wind | quest chain reward | Push all to Back row, ally SPD +30% |
| **Bahamut** | Void | Ancient Dragon drop | Pierces all resistances, ultimate damage |
| **Alexander** | Holy | Celestial Spire boss | AoE Holy, heals all allies |
| **Odin** | Shadow | Lich drop | Instant kill chance, Shadow damage |

### Summon Gauge
Builds from: taking damage, landing crits, hitting weaknesses. At 100% you can summon. Resets after use.

---

## 19. Party System

### Vision

The party system is the social heartbeat of the game. You're in the same Discord server. Your friend is fighting. You can see it happening. You can jump in. That's the whole vibe — the RPG is embedded in the community, not separate from it.

Solo players can do everything. Parties do it faster and can access content solo can't clear. High-level players don't replace lower-level ones — role composition matters more than raw level.

---

### Party Management

| Command | Effect |
|---------|--------|
| `/party create` | Create a party — you're the leader |
| `/party invite @user` | Invite someone to your party (they get a ping with Accept button) |
| `/party leave` | Leave the party |
| `/party disband` | Leader disbands the whole party |
| `/party roster` | Show current party members, levels, classes, HP |
| `/party fight [monster]` | Start a party encounter — all members enter combat together |
| `/party boss` | Challenge a zone boss as a party |
| `/party raid` | Challenge a weekly raid boss (Lv 60+ required) |

**Party size:** 2–4 players. All must be in the same Discord server.
**Party channel:** Combat plays out in the channel where `/party fight` was used. All members can see it.

---

### Co-op Combat — How Turns Work

Turn order is determined by each player's SPD stat, interleaved with enemy turns. Fastest character goes first regardless of whether they're a player or enemy.

Each player's turn:
1. Bot mentions them: `@User — your turn! [Turn 3] | ❤️ 142/200 | 💧 60/90`
2. They have **90 seconds** to send a command (`/rpg attack`, `/rpg magic fire`, `/rpg skill heal @ally`, etc.)
3. If they time out → auto-**Defend** for that turn (safe fallback, no panic)

**Turn timeout = auto-Defend, not auto-skip.** Missing your turn doesn't punish the party — it just plays it safe.

Enemies get smarter in party battles. If a Warrior is tanking, the boss may switch targets to the Cleric. Positioning and taunts matter more in group fights.

---

### Targeting in Party Combat

- `/rpg attack` — attacks the current enemy target (shared target lock)
- `/rpg attack [name]` — switch target (e.g. attacking an add instead of the boss)
- `/rpg skill heal @ally` — use a skill on a specific ally by mention
- `/rpg skill barrier @user` — apply buff to ally
- `/rpg skill [name]` — context-sensitive (offensive = targets enemy, defensive = targets self)

The bot always shows the current state after each turn — all HP bars, statuses, turn order.

---

### Combo / Resonance Attacks

When party members coordinate element attacks in the right order, they trigger **Resonance** — a chain reaction bonus that neither could get alone.

| Setup | Follow-Up | Resonance Effect |
|-------|-----------|-----------------|
| Apply **Drenched** (Water) | Any Lightning attack | **Electrolysis** — +75% Lightning damage, guaranteed Stunned |
| Apply **Frozen** (Ice) | Any physical attack | **Shatter** — +60% damage, applies Shattered (-40% DEF) |
| Apply **Burn** (Fire) | Any Wind attack | **Firestorm** — Burn spreads to all enemies |
| Apply **Rooted** (Earth) | Any ranged/magic attack | **Exposed** — target can't dodge, +40% damage this hit |
| Apply **Weakened** (Dark) | Holy attack | **Reckoning** — Holy damage ×3 vs Weakened target |
| Apply **Silenced** (Holy) | Any physical attack | **Punish** — +50% physical damage vs Silenced target |
| Apply **Bleeding** (Rogue) | Apply **Poisoned** | **Hemorrhage** — both DoTs deal double, stack 3× instead of 2× |
| Any ally below 20% HP | Cleric uses Barrier | **Last Rites** — Barrier absorbs 3× normal amount |

Resonance bonuses are announced visibly: `💥 RESONANCE — Electrolysis! Chain damage!`

---

### 🆘 SOS — Emergency Aid System

This is the "my friend is about to die and I can jump in" feature. One of the most important social mechanics in the game.

#### How It Triggers
When a player's HP drops below **15%** during solo combat, the bot auto-broadcasts in the same channel:

> ⚠️ **[CharName] is in critical danger!**
> ❤️ `18 / 142 HP` fighting a **Dungeon Mimic** (Lv 42)
> A friend can rush to their aid — type `/rpg aid @[user]` within 60 seconds!

#### How Aid Works
1. Any other player types `/rpg aid @dying_player`
2. The aid-giver's character **joins the active combat immediately** on the next turn
3. They enter at **full HP and MP** (they're rushing in fresh)
4. Combat continues with the new ally — existing enemy HP stays the same
5. The aid window closes — no more than 1 aid-giver per SOS (keeps it manageable)

#### SOS Rewards
If the combat is won after aid is given:
- **Dying player:** Full XP and loot as normal. No death penalty.
- **Aid-giver:** 50% of the XP the dying player earned + a **"Hero" bonus**: flat 25 coins + a small chance at a bonus loot roll from the enemy's table

If the aid-giver's HP hits 0 during the fight: they **exhaust and withdraw** (no death penalty to them — they were helping). The original player continues solo.

#### Opting Out
Players who don't want SOS broadcasts can disable it: `/rpg settings sos off`

---

### Role Composition

Parties need roles. No single class does everything:

| Role | Best Class | What They Do |
|------|-----------|--------------|
| Tank | Warrior | Taunt, absorb hits, protect squishies |
| Healer | Cleric | Keep everyone alive, cleanse, Resurrect |
| DPS | Mage / Ranger | Burst damage, elemental control |
| Disruptor | Rogue | Debuffs, silence, expose, steal turns |
| Support | Any | Buffs, Resonance setup, debuff-then-hit combos |

A Warrior can solo-progress, but they can't heal themselves in endgame raids. A Cleric can heal perfectly but won't kill the final boss alone. **Party composition is the actual puzzle.**

---

### Party Loot

- Each member rolls their own loot independently — no splitting, no competition
- Party bonus: **+15% drop rate** for all members in a party fight
- Raid bonus: **+30% drop rate** and guaranteed at least 1 Rare+ drop per boss kill
- Elite/Champion kill bonus: party members each get an extra roll on the loot table

---

### Why It Solves the Max Flexer Problem

A solo Lv100 Warrior with max gear still needs:
- A Cleric to heal through The End's Berserker phase
- A Rogue to Silence before The Void Architect corrupts all their resists
- A Mage to set up Resonance combos that deal meaningful damage to Null

High level doesn't make other players irrelevant — it makes their **specific role** more valuable, not less.

---

### Solo Fallback

Everything is soloable up through Abyss (Lv 75). Summons substitute for missing party roles:
- **Ifrit** — tanks and deals AoE damage
- **Shiva** — heals and freezes
- **Alexander** — full party heal, Holy damage
- **Odin** — instant kill chance, burst finisher

Void Realm and Raid Bosses are *possible* solo but designed to require coordination. Solo clears are a flex, not the intended path.

---

## Open Questions / TBD

### Story
- [ ] Orthodox Lives of the Saints influence — what does it contribute?
- [ ] Void Realm zone lore — needs emotional identity per zone design rules
- [ ] Celestial Spire zone lore — Holy/Light dominant, what's the story here?
- [ ] Named unique item lore — each unique should have 1–2 sentences of history in its description

### Systems Not Yet Implemented
- [ ] MDEF — add to PlayerCharacter entity + ItemDefinition stat fields
- [ ] SPD stat — add to PlayerCharacter entity
- [ ] Class triangle modifier — not in PvP handler yet
- [ ] Press Turn icon system — current combat is simple turn-based
- [ ] Positioning (Front/Mid/Back) — not implemented
- [ ] Morale meter — not implemented
- [ ] Limit Breaks — tied to Morale at 100, class signature move
- [ ] Status effects — Burn/Freeze/Stun etc. not wired into combat yet
- [ ] Food system — cooking produces edible items that heal HP outside combat
- [ ] SP (Skill Points) — separate resource for class abilities vs MP for magic
- [ ] Class abilities — no ability system exists yet, just generic attack/magic/defend
- [ ] Summons system — not implemented
- [ ] Party/dungeon system — not implemented
- [ ] Job system / ability mixing — endgame, not implemented
- [ ] Drop boxes / loot crates — M5 on roadmap, not built
- [ ] Daily quests / bounties — not implemented
- [ ] Weekly boss events — not implemented
- [ ] NPC coin shop (`/rpg shop`) — not implemented
- [ ] `/rpg sell <item>` — not implemented
- [ ] Peepo rarity tiers + scaled pricing — not implemented
- [ ] Peepo crates — not implemented
- [ ] Peepo monster drops — not implemented
- [ ] Craft recipes for all existing items — mostly missing
- [ ] Coins awarded on crafting — small bonus per craft scaling with item tier
- [ ] Coins awarded on enchanting — small bonus per enchant scaling with rarity
- [ ] Coins awarded on cooking — bonus per successful cook scaling with level
- [ ] Gathering cooldown reduced to 30s in ProcessGameCommandHandler.cs (currently 60s)
- [ ] Resource tiers per skill — mining/fishing/woodcutting gated by skill level (RS-style)

### Design Decisions Pending
- [ ] Speed penalty from heavy armor — shelved, revisit later
- [ ] Wand and Spear weapon types — add when ability system is built
- [ ] Support Bonds — how units who fight together gain passive bonuses
- [ ] Fusion — combine 2 items/units into 1 (endgame)
- [ ] Job system details — mix and match abilities across classes (FF5 style)
