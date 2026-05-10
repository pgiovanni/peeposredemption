using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Game.Commands;

public class ProcessGameCommandHandler : IRequestHandler<ProcessGameCommandRequest, GameCommandResult>
{
    private readonly IUnitOfWork _uow;

    public ProcessGameCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<GameCommandResult> Handle(ProcessGameCommandRequest request, CancellationToken ct)
    {
        var parts = request.RawInput.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return GameCommandResult.NotHandled();

        var command = parts[0].ToLower();
        var args = parts.Length > 1 ? string.Join(' ', parts[1..]) : string.Empty;

        // Ensure player exists (auto-create on first game command)
        var player = await EnsurePlayerAsync(request.UserId, request.Username);

        // Check for expired combat session
        var activeCombat = await _uow.CombatSessions.GetActiveByPlayerIdAsync(player.Id);
        if (activeCombat != null && activeCombat.LastTurnAt.AddMinutes(10) < DateTime.UtcNow)
        {
            activeCombat.State = CombatState.Expired;
            activeCombat.EndedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();
            activeCombat = null;
        }

        return command switch
        {
            "/help" => HandleHelp(),
            "/stats" => await HandleStats(player),
            "/fight" => await HandleFight(player, args, request.ChannelId),
            "/attack" => await HandleCombatAction(player, CombatAction.Attack, args),
            "/defend" => await HandleCombatAction(player, CombatAction.Defend, args),
            "/magic" => await HandleCastSpell(player, args, request.TargetUserId),
            "/item" => await HandleUseItem(player, args),
            "/flee" => await HandleCombatAction(player, CombatAction.Flee, args),
            "/inventory" or "/inv" => await HandleInventory(player),
            "/equip" => await HandleEquip(player, args),
            "/unequip" => await HandleUnequip(player, args),
            "/mine" or "/fish" or "/chop" => await HandleGather(player, command),
            "/cook" => await HandleCook(player, args),
            "/craft" => await HandleCraft(player, args),
            "/recipes" => await HandleRecipes(player),
            "/boss" => await HandleFight(player, args, request.ChannelId, bossOnly: true),
            "/shop" => await HandleShop(player, args),
            "/buy"  => await HandleBuy(player, args),
            "/sell" => await HandleSell(player, args),
            "/trade" => await HandleTrade(player, args, request.ChannelId),
            "/market" => await HandleMarket(player, args),
            "/leaderboard" or "/lb" => await HandleLeaderboard(),
            "/game" => await HandleGameConfig(request.UserId, request.ChannelId, args),
            "/enchant" => await HandleEnchant(player, args),
            "/craftbook" => await HandleCraftBook(player, args),
            _ => GameCommandResult.NotHandled()
        };
    }

    private async Task<PlayerCharacter> EnsurePlayerAsync(Guid userId, string username)
    {
        var player = await _uow.PlayerCharacters.GetByUserIdAsync(userId);
        if (player != null) return player;

        player = new PlayerCharacter
        {
            UserId = userId,
            CharacterName = username,
            Class = GameClass.Warrior,
            Level = 1,
            XP = 0,
            STR = 10, DEF = 10, INT = 10, DEX = 10, VIT = 10, LUK = 5,
            CurrentHp = 100, MaxHp = 100,
            CurrentMp = 80, MaxMp = 80
        };
        await _uow.PlayerCharacters.AddAsync(player);

        // Initialize all skills at level 1
        foreach (SkillType skill in Enum.GetValues<SkillType>())
        {
            await _uow.PlayerSkills.AddAsync(new PlayerSkill
            {
                PlayerId = player.Id,
                SkillType = skill,
                Level = 1,
                XP = 0,
                XpToNextLevel = 75
            });
        }

        // Give starter weapon
        var starterSword = await _uow.ItemDefinitions.GetByNameAsync("Wooden Sword");
        if (starterSword != null)
        {
            var invItem = new PlayerInventoryItem
            {
                PlayerId = player.Id,
                ItemDefinitionId = starterSword.Id,
                Quantity = 1,
                IsEquipped = true,
                EquippedSlot = EquipSlot.MainHand
            };
            await _uow.PlayerInventoryItems.AddAsync(invItem);
        }

        await _uow.SaveChangesAsync();
        return player;
    }

    private GameCommandResult HandleHelp()
    {
        var helpText = new
        {
            title = "RPG Commands",
            commands = new[]
            {
                new { cmd = "/help", desc = "Show this help message" },
                new { cmd = "/stats", desc = "View your character stats" },
                new { cmd = "/fight [monster]", desc = "Start combat (random or named monster)" },
                new { cmd = "/attack", desc = "Attack the monster" },
                new { cmd = "/defend", desc = "Defend (halve incoming damage)" },
                new { cmd = "/magic [spell]", desc = "Cast a spell (costs MP)" },
                new { cmd = "/item [name]", desc = "Use an item in combat" },
                new { cmd = "/flee", desc = "Try to escape combat" },
                new { cmd = "/inventory", desc = "View your inventory" },
                new { cmd = "/equip [item]", desc = "Equip an item" },
                new { cmd = "/unequip [slot]", desc = "Unequip from a slot" },
                new { cmd = "/mine", desc = "Mine for ore — tier scales with Mining level (5s cooldown)" },
                new { cmd = "/fish", desc = "Go fishing — tier scales with Fishing level (5s cooldown)" },
                new { cmd = "/chop", desc = "Chop wood — tier scales with Woodcutting level (5s cooldown)" },
                new { cmd = "/cook [raw fish]", desc = "Cook raw fish into food" },
                new { cmd = "/craft [item]", desc = "Craft an item" },
                new { cmd = "/recipes", desc = "View available recipes" },
                new { cmd = "/trade @user [item] [qty]", desc = "Trade with another player" },
                new { cmd = "/market list/browse/buy", desc = "Marketplace commands" },
                new { cmd = "/leaderboard", desc = "View top players" },
                new { cmd = "/game mute/unmute", desc = "Mute/unmute game bot (mods)" },
                new { cmd = "/enchant [book] [slot?]", desc = "Apply enchant book to equipped item" },
                new { cmd = "/craftbook [book name]", desc = "Craft an enchant book from materials" }
            }
        };
        return GameCommandResult.Single("help", helpText);
    }

    private async Task<GameCommandResult> HandleStats(PlayerCharacter player)
    {
        var equipped = await _uow.PlayerInventoryItems.GetEquippedItemsAsync(player.Id);
        var skills = await _uow.PlayerSkills.GetByPlayerIdAsync(player.Id);
        var activeCombat = await _uow.CombatSessions.GetActiveByPlayerIdAsync(player.Id);
        var inCombat = activeCombat != null;

        int bonusSTR = 0, bonusDEF = 0, bonusINT = 0, bonusDEX = 0, bonusVIT = 0, bonusLUK = 0;
        foreach (var item in equipped)
        {
            bonusSTR += item.ItemDefinition.BonusSTR;
            bonusDEF += item.ItemDefinition.BonusDEF;
            bonusINT += item.ItemDefinition.BonusINT;
            bonusDEX += item.ItemDefinition.BonusDEX;
            bonusVIT += item.ItemDefinition.BonusVIT;
            bonusLUK += item.ItemDefinition.BonusLUK;
        }

        var xpToNext = XpToLevel(player.Level + 1);

        return GameCommandResult.Single("stats", new
        {
            name = player.CharacterName,
            className = player.Class.ToString(),
            level = player.Level,
            xp = player.XP,
            xpToNext,
            hp = player.CurrentHp,
            maxHp = player.MaxHp,
            mp = player.CurrentMp,
            maxMp = player.MaxMp,
            str = player.STR,
            def = player.DEF,
            @int = player.INT,
            dex = player.DEX,
            vit = player.VIT,
            luk = player.LUK,
            bonusStr = bonusSTR,
            bonusDef = bonusDEF,
            bonusInt = bonusINT,
            bonusDex = bonusDEX,
            bonusVit = bonusVIT,
            bonusLuk = bonusLUK,
            kills = player.TotalMonstersKilled,
            deaths = player.TotalDeaths,
            coinBalance = player.CoinBalance,
            inCombat,
            skills = skills.Select(s => new { skill = s.SkillType.ToString(), level = s.Level, xp = s.XP, xpToNext = s.XpToNextLevel }),
            gear = equipped.Select(i => new
            {
                slot = i.EquippedSlot.ToString(),
                name = i.ItemDefinition.Name,
                icon = i.ItemDefinition.Icon,
                bonusStr = i.ItemDefinition.BonusSTR,
                bonusDef = i.ItemDefinition.BonusDEF,
                bonusInt = i.ItemDefinition.BonusINT,
                bonusDex = i.ItemDefinition.BonusDEX,
                bonusVit = i.ItemDefinition.BonusVIT,
                bonusLuk = i.ItemDefinition.BonusLUK,
                minDmg = i.ItemDefinition.MinDamage,
                maxDmg = i.ItemDefinition.MaxDamage
            })
        });
    }

    // Bosses: HP > level * 200 (all regular monsters are well below this threshold)
    private static bool IsBoss(MonsterDefinition m) => m.MaxHp > m.Level * 200;

    private async Task<GameCommandResult> HandleFight(PlayerCharacter player, string args, Guid channelId, bool bossOnly = false)
    {
        var existing = await _uow.CombatSessions.GetActiveByPlayerIdAsync(player.Id);
        if (existing != null)
            return GameCommandResult.Single("error", new { message = "You are already in combat! Use /attack, /defend, /magic, /item, or /flee." });

        if (player.CurrentHp <= 0)
        {
            player.CurrentHp = (int)(player.MaxHp * 0.25);
            await _uow.SaveChangesAsync();
        }

        MonsterDefinition? monster;
        if (!string.IsNullOrWhiteSpace(args))
        {
            monster = await _uow.MonsterDefinitions.GetByNameAsync(args.Trim());
            if (monster == null)
                return GameCommandResult.Single("error", new { message = $"No monster named '{args.Trim()}' found." });
            if (bossOnly && !IsBoss(monster))
                return GameCommandResult.Single("error", new { message = $"**{monster.Name}** is not a boss. Use `/rpg fight` for regular monsters." });
        }
        else if (bossOnly)
        {
            // List available bosses near player level
            var all = await _uow.MonsterDefinitions.GetByLevelRangeAsync(1, 999);
            var bosses = all.Where(IsBoss).OrderBy(b => b.Level).ToList();
            if (!bosses.Any())
                return GameCommandResult.Single("error", new { message = "No bosses found. Ask an admin to run /rpg sync." });
            var lines = bosses.Select(b => $"**{b.Icon} {b.Name}** (Lv{b.Level}) — ❤️ {b.MaxHp:N0} HP");
            return GameCommandResult.Single("boss_list", new { bosses = string.Join("\n", lines) });
        }
        else
        {
            monster = await _uow.MonsterDefinitions.GetRandomNearLevelAsync(player.Level);
            // Random fight should never pick a boss
            if (monster != null && IsBoss(monster))
                monster = await _uow.MonsterDefinitions.GetRandomNearLevelAsync(player.Level);
        }

        if (monster == null)
            return GameCommandResult.Single("error", new { message = "No monster found! Try /fight without arguments." });

        var session = new CombatSession
        {
            PlayerId = player.Id,
            MonsterDefinitionId = monster.Id,
            ChannelId = channelId,
            State = CombatState.AwaitingAction,
            TurnNumber = 1,
            IsPlayerTurn = true,
            MonsterCurrentHp = monster.MaxHp,
            MonsterMaxHp = monster.MaxHp,
            PlayerHpAtStart = player.CurrentHp,
            CombatLog = "[]",
            PlayerStatusJson = player.StatusJson   // carry over persistent effects
        };

        await _uow.CombatSessions.AddAsync(session);
        await _uow.SaveChangesAsync();

        return GameCommandResult.Broadcast("combat_start", new
        {
            playerName = player.CharacterName,
            playerHp = player.CurrentHp,
            playerMaxHp = player.MaxHp,
            monsterName = monster.Name,
            monsterIcon = monster.Icon,
            monsterLevel = monster.Level,
            monsterHp = monster.MaxHp,
            monsterMaxHp = monster.MaxHp,
            monsterZone = monster.Zone,
            monsterElement = monster.Element.ToString()
        });
    }

    private async Task<GameCommandResult> HandleUseItem(PlayerCharacter player, string args)
    {
        // In combat — hand off to the combat turn handler (monster still attacks back)
        var session = await _uow.CombatSessions.GetActiveByPlayerIdAsync(player.Id);
        if (session != null)
            return await HandleCombatAction(player, CombatAction.UseItem, args);

        // Out of combat — apply item directly, no monster retaliation
        if (string.IsNullOrWhiteSpace(args))
            return GameCommandResult.Single("error", new { message = "Specify an item: /item <name>" });

        var itemDef = await _uow.ItemDefinitions.GetByNameAsync(args.Trim());
        if (itemDef == null)
            return GameCommandResult.Single("error", new { message = $"Item '{args.Trim()}' not found." });

        var invItem = await _uow.PlayerInventoryItems.GetByPlayerAndItemAsync(player.Id, itemDef.Id);
        if (invItem == null || invItem.Quantity <= 0)
            return GameCommandResult.Single("error", new { message = $"You don't have any {itemDef.Name}." });

        if (itemDef.Type != GameItemType.Consumable)
            return GameCommandResult.Single("error", new { message = $"{itemDef.Name} is not a consumable." });

        var log = new List<string>();

        if (itemDef.HealAmount > 0)
        {
            // Potions heal % of MaxHp; food heals a flat amount
            bool isPotion = itemDef.SubType is ItemSubType.HealthPotion or ItemSubType.ManaPotion;
            int rawHeal = isPotion
                ? (int)(player.MaxHp * itemDef.HealAmount / 100.0)
                : itemDef.HealAmount;
            int healed = Math.Min(rawHeal, player.MaxHp - player.CurrentHp);
            player.CurrentHp += healed;
            log.Add(healed > 0 ? $"Restored {healed} HP." : "HP is already full.");
        }
        if (itemDef.ManaRestoreAmount > 0)
        {
            bool isPotion = itemDef.SubType is ItemSubType.HealthPotion or ItemSubType.ManaPotion;
            int rawMp = isPotion
                ? (int)(player.MaxMp * itemDef.ManaRestoreAmount / 100.0)
                : itemDef.ManaRestoreAmount;
            int restored = Math.Min(rawMp, player.MaxMp - player.CurrentMp);
            player.CurrentMp += restored;
            log.Add(restored > 0 ? $"Restored {restored} MP." : "MP is already full.");
        }
        if (itemDef.HealAmount == 0 && itemDef.ManaRestoreAmount == 0)
            log.Add($"Used {itemDef.Name}. (No effect — might taste good though.)");

        invItem.Quantity--;
        if (invItem.Quantity <= 0) _uow.PlayerInventoryItems.Remove(invItem);
        await _uow.SaveChangesAsync();

        return GameCommandResult.Single("use_item", new
        {
            item    = itemDef.Name,
            message = $"🍖 {itemDef.Name} — {string.Join(" ", log)}",
            hp      = player.CurrentHp,
            maxHp   = player.MaxHp,
            mp      = player.CurrentMp,
            maxMp   = player.MaxMp,
        });
    }

    // Utility spells usable out of combat: (mpCost, hpHeal% of INT, mpRestore% of INT)
    private static readonly Dictionary<string, (int MpCost, float HpScale, float MpScale, string Desc)> _utilitySpells = new(StringComparer.OrdinalIgnoreCase)
    {
        ["heal"]         = (15,  3.0f, 0f,   "Restored {hp} HP."),
        ["barrier"]      = (20,  0f,   2.5f, "Fortified your mind. Restored {mp} MP."),
        ["revitalize"]   = (30,  5.0f, 0f,   "Revitalized! Restored {hp} HP."),
        ["regen"]        = (35,  4.0f, 0f,   "Regenerated {hp} HP."),
        ["ward"]         = (25,  0f,   3.0f, "Warded yourself. Restored {mp} MP."),
        ["cleanse"]      = (20,  0f,   0f,   "Cleansed! Status effects removed."),
        ["resurrection"] = (100, 100f, 0f,   "Fully restored HP!"),
    };

    private async Task<GameCommandResult> HandleCastSpell(PlayerCharacter player, string args, Guid? targetUserId = null)
    {
        // In combat — use the existing combat magic handler (targeting not supported in combat)
        var session = await _uow.CombatSessions.GetActiveByPlayerIdAsync(player.Id);
        if (session != null)
            return await HandleCombatAction(player, CombatAction.Magic, args);

        // Out of combat — utility spells only
        // Use first word only so "/magic heal" works even if extra text follows
        var spellName = args.Trim().ToLower().Split(' ')[0];
        if (string.IsNullOrWhiteSpace(spellName))
            return GameCommandResult.Single("error", new { message = "Specify a spell: /magic <name>" });

        if (!_utilitySpells.TryGetValue(spellName, out var spell))
            return GameCommandResult.Single("error", new { message = $"Unknown spell '{spellName}'. Out-of-combat spells: {string.Join(", ", _utilitySpells.Keys)}." });

        // Resolve target — defaults to self
        var target = targetUserId.HasValue
            ? await _uow.PlayerCharacters.GetByUserIdAsync(targetUserId.Value) ?? player
            : player;
        bool isSelf = target.Id == player.Id;

        if (player.CurrentMp < spell.MpCost)
            return GameCommandResult.Single("error", new
            {
                message = $"Not enough MP! {args.Trim()} costs {spell.MpCost} MP, you have {player.CurrentMp}."
            });

        player.CurrentMp -= spell.MpCost;

        int hpRestored = 0, mpRestored = 0;

        if (spell.HpScale > 0)
        {
            int rawHeal = spellName == "resurrection"
                ? target.MaxHp
                : (int)(player.INT * spell.HpScale) + 5;   // caster's INT determines potency
            hpRestored = Math.Min(rawHeal, target.MaxHp - target.CurrentHp);
            target.CurrentHp += hpRestored;
        }
        if (spell.MpScale > 0)
        {
            int rawMp = (int)(player.INT * spell.MpScale) + 5;
            mpRestored = Math.Min(rawMp, target.MaxMp - target.CurrentMp);
            target.CurrentMp += mpRestored;
        }

        await _uow.SaveChangesAsync();

        var effectMsg = spell.Desc
            .Replace("{hp}", hpRestored.ToString())
            .Replace("{mp}", mpRestored.ToString());

        var spellTitle = $"{char.ToUpper(args.Trim()[0])}{args.Trim()[1..]}";
        var targetName = isSelf ? "yourself" : target.CharacterName;
        var message    = $"✨ {spellTitle} → **{targetName}** — {effectMsg} (-{spell.MpCost} MP)";

        return GameCommandResult.Single("cast_spell", new
        {
            spell      = args.Trim(),
            message,
            targetName,
            isSelf,
            hp         = target.CurrentHp,
            maxHp      = target.MaxHp,
            casterMp   = player.CurrentMp,
            casterMaxMp= player.MaxMp,
        });
    }

    private async Task<GameCommandResult> HandleCombatAction(PlayerCharacter player, CombatAction action, string args)
    {
        var session = await _uow.CombatSessions.GetActiveByPlayerIdAsync(player.Id);
        if (session == null)
            return GameCommandResult.Single("error", new { message = "You are not in combat! Use /fight to start." });

        var monster = session.MonsterDefinition;
        var equipped = await _uow.PlayerInventoryItems.GetEquippedItemsAsync(player.Id);
        var weapon = equipped.FirstOrDefault(e => e.EquippedSlot == EquipSlot.MainHand);

        int bonusSTR = equipped.Sum(e => e.ItemDefinition.BonusSTR);
        int bonusDEF = equipped.Sum(e => e.ItemDefinition.BonusDEF);
        int bonusINT = equipped.Sum(e => e.ItemDefinition.BonusINT);
        int bonusDEX = equipped.Sum(e => e.ItemDefinition.BonusDEX);
        int bonusLUK = equipped.Sum(e => e.ItemDefinition.BonusLUK);

        int totalSTR = player.STR + bonusSTR;
        int totalDEF = player.DEF + bonusDEF;
        int totalINT = player.INT + bonusINT;
        int totalDEX = player.DEX + bonusDEX;
        int totalLUK = player.LUK + bonusLUK;

        var logEntries = new List<string>();
        bool combatEnded = false;

        // ── Load active status effects ────────────────────────────────────────
        var playerFx  = StatusEffects.Load(session.PlayerStatusJson);
        var monsterFx = StatusEffects.Load(session.MonsterStatusJson);

        // ── Monster passive resistances (PhysResist / MagicResist) ───────────
        var monsterAbilities = StatusEffects.LoadAbilities(monster.AbilityJson);
        double physResist  = Math.Min(0.75, monsterAbilities
            .Where(a => a.Type.Equals("PhysResist",  StringComparison.OrdinalIgnoreCase))
            .Sum(a => a.Strength) / 100.0);
        double magicResist = Math.Min(0.75, monsterAbilities
            .Where(a => a.Type.Equals("MagicResist", StringComparison.OrdinalIgnoreCase))
            .Sum(a => a.Strength) / 100.0);

        // ── Tick status effects at turn start ─────────────────────────────────
        var (fxLog, fxHpLost, fxMpLost, skipTurn, silenced) =
            StatusEffects.Tick(playerFx, player, Random.Shared);
        logEntries.AddRange(fxLog);

        // Slow: skip every other turn (odd turns only when slowed)
        if (StatusEffects.IsSlowed(playerFx) && session.TurnNumber % 2 == 0)
        {
            skipTurn = true;
            logEntries.Add("⏳ **Slowed!** You move sluggishly and lose your turn.");
        }

        // ── Tick monster DoTs (Burn, Shock, Corrupt, etc. applied by player) ─────
        if (!combatEnded)
        {
            var (monsterDotLog, monsterDotDmg) = StatusEffects.TickMonster(monsterFx, monster.Name);
            if (monsterDotLog.Count > 0)
            {
                logEntries.AddRange(monsterDotLog);
                session.MonsterCurrentHp = Math.Max(0, session.MonsterCurrentHp - monsterDotDmg);
                if (session.MonsterCurrentHp <= 0)
                {
                    session.State = CombatState.Victory;
                    session.EndedAt = DateTime.UtcNow;
                    combatEnded = true;
                }
            }
        }

        // ── Apply effective stats with status modifiers ───────────────────────
        int effDEF = StatusEffects.EffectiveDef(playerFx, totalDEF);
        int effSTR = StatusEffects.EffectiveStr(playerFx, totalSTR);

        // Monster berserk: boost its damage
        bool monsterBerserk = monsterFx.Any(e => e.Type.Equals("Berserk", StringComparison.OrdinalIgnoreCase));

        if (!skipTurn)
        {
            switch (action)
            {
                case CombatAction.Flee:
                    double fleeChance = Math.Clamp(0.3 + (totalDEX - monster.DEX) * 0.02, 0.1, 0.9);
                    if (Random.Shared.NextDouble() < fleeChance)
                    {
                        session.State = CombatState.Fled;
                        session.EndedAt = DateTime.UtcNow;
                        combatEnded = true;
                        logEntries.Add("You fled from battle!");
                    }
                    else
                    {
                        logEntries.Add("Failed to flee!");
                        var fleeDmg = CalculateMonsterDamage(monster, effDEF, false, monsterBerserk);
                        player.CurrentHp = Math.Max(0, player.CurrentHp - fleeDmg);
                        logEntries.Add($"{monster.Name} attacks for {fleeDmg} damage!");
                        ApplyMonsterAbility(monster, playerFx, logEntries, player);
                    }
                    break;

                case CombatAction.Defend:
                    session.PlayerDefending = true;
                    logEntries.Add("You take a defensive stance!");
                    var defDmg = CalculateMonsterDamage(monster, effDEF, true, monsterBerserk);
                    player.CurrentHp = Math.Max(0, player.CurrentHp - defDmg);
                    logEntries.Add($"{monster.Name} attacks for {defDmg} damage! *(defended)*");
                    ApplyMonsterAbility(monster, playerFx, logEntries, player);
                    break;

                case CombatAction.Attack:
                    int minDmg = weapon?.ItemDefinition.MinDamage ?? 1;
                    int maxDmg = weapon?.ItemDefinition.MaxDamage ?? 3;
                    // Enchant element overrides base weapon element
                    var weaponBaseElement   = weapon?.ItemDefinition.Element ?? Element.None;
                    var weaponEnchantElement = weapon?.GetPrimaryElement() ?? Element.None;
                    var weaponElement = weaponEnchantElement != Element.None ? weaponEnchantElement : weaponBaseElement;

                    // Confusion: 50% chance to hit self
                    bool confused = StatusEffects.IsConfused(playerFx) && Random.Shared.NextDouble() < 0.5;
                    // Blind: 50% miss chance
                    bool blind    = StatusEffects.IsBlind(playerFx)    && Random.Shared.NextDouble() < 0.5;

                    if (confused)
                    {
                        int selfDmg = Math.Max(1, Random.Shared.Next(minDmg, maxDmg + 1) + (int)(effSTR * 0.3));
                        player.CurrentHp = Math.Max(0, player.CurrentHp - selfDmg);
                        logEntries.Add($"💫 **Confused!** You attack yourself for **{selfDmg}** damage!");
                    }
                    else if (blind)
                    {
                        logEntries.Add("👁️ **Blinded!** Your attack misses!");
                    }
                    else
                    {
                        int playerAttack = Random.Shared.Next(minDmg, maxDmg + 1) + (int)(effSTR * 0.5);
                        int damage = Math.Max(1, playerAttack - (int)(monster.DEF * 0.3));
                        double elementBonus = ElementSystem.GetMultiplier(weaponElement, monster.Element, isMagic: false);
                        double critChance = Math.Min(0.25, totalLUK * 0.005 + totalDEX * 0.002);
                        bool crit = Random.Shared.NextDouble() < critChance;
                        int finalDamage = (int)(damage * elementBonus * (crit ? 1.5 : 1.0) * (1.0 - physResist));
                        finalDamage = Math.Max(1, finalDamage);

                        session.MonsterCurrentHp = Math.Max(0, session.MonsterCurrentHp - finalDamage);
                        string resistNote  = physResist > 0 ? $" 🪖 *({(int)(physResist*100)}% resisted)*" : "";
                        string elementMsg  = elementBonus >= 1.25 ? $" {ElementSystem.GetElementIcon(weaponElement)} **Effective!**"
                                           : elementBonus <= 0.75 ? " 🛡️ *Not very effective...*"
                                           : "";
                        logEntries.Add($"You attack for **{finalDamage}** damage!{resistNote}{(crit ? " **CRITICAL HIT!**" : "")}{elementMsg}");

                        // Apply weapon element status effect to monster (30% base chance)
                        var weaponEffect = ElementSystem.GetElementalStatusEffect(weaponElement);
                        if (weaponEffect != null && session.MonsterCurrentHp > 0 && Random.Shared.NextDouble() < 0.30)
                        {
                            var (effStr, effTurns) = ElementSystem.GetStatusParams(weaponElement);
                            StatusEffects.Apply(monsterFx, weaponEffect, effStr, effTurns);
                            logEntries.Add($"{StatusEffects.Icons.GetValueOrDefault(weaponEffect, "")} **{weaponElement}** inflicts **{weaponEffect}** on {monster.Name}!");
                        }
                    }

                    if (session.MonsterCurrentHp <= 0)
                    {
                        session.State = CombatState.Victory;
                        session.EndedAt = DateTime.UtcNow;
                        combatEnded = true;
                    }
                    else
                    {
                        session.PlayerDefending = false;
                        var atkDmg = CalculateMonsterDamage(monster, effDEF, false, monsterBerserk);
                        player.CurrentHp = Math.Max(0, player.CurrentHp - atkDmg);
                        logEntries.Add($"{monster.Name} attacks for **{atkDmg}** damage!");
                        ApplyMonsterAbility(monster, playerFx, logEntries, player);
                    }
                    break;

                case CombatAction.Magic:
                    if (silenced)
                    {
                        logEntries.Add("🔇 **Silenced!** Cannot cast magic this turn!");
                        var silDmg = CalculateMonsterDamage(monster, effDEF, false, monsterBerserk);
                        player.CurrentHp = Math.Max(0, player.CurrentHp - silDmg);
                        logEntries.Add($"{monster.Name} attacks for **{silDmg}** damage!");
                        ApplyMonsterAbility(monster, playerFx, logEntries, player);
                        break;
                    }

                    var spellName = args.Trim().ToLower().Split(' ')[0];
                    if (string.IsNullOrWhiteSpace(spellName))
                    {
                        logEntries.Add("Specify a spell: /magic fire  (or fira, firaga, firaja, blizzard, thunder, water, aero, cure, etc.)");
                        break;
                    }

                    var spellDef = ElementSystem.FindSpell(spellName);
                    if (spellDef == null)
                    {
                        logEntries.Add($"Unknown spell '{spellName}'. Try: fire, fira, firaga, firaja, blizzard, thunder, water, aero, dark, holy, cure, cura, etc.");
                        break;
                    }
                    if (player.Level < spellDef.LevelReq)
                    {
                        logEntries.Add($"Requires level {spellDef.LevelReq} to cast {spellDef.Name}.");
                        break;
                    }
                    if (player.CurrentMp < spellDef.MpCost)
                    {
                        logEntries.Add($"Not enough MP! **{spellDef.Name}** costs {spellDef.MpCost} MP, you have {player.CurrentMp}.");
                        break;
                    }
                    player.CurrentMp -= spellDef.MpCost;

                    // ── Healing spells ──────────────────────────────────────
                    if (spellDef.IsHeal)
                    {
                        int rawHeal = (int)(totalINT * spellDef.IntScale) + 5;
                        // Curse halves healing
                        if (StatusEffects.IsCursed(playerFx)) rawHeal /= 2;
                        int healed = Math.Min(rawHeal, player.MaxHp - player.CurrentHp);
                        player.CurrentHp += healed;
                        string curseNote = StatusEffects.IsCursed(playerFx) ? " *(Curse halved healing!)*" : "";
                        logEntries.Add($"✨ **{spellDef.Name}** restores **{healed}** HP! (-{spellDef.MpCost} MP){curseNote}");

                        // Monster still attacks
                        var healRetDmg = CalculateMonsterDamage(monster, effDEF, false, monsterBerserk);
                        player.CurrentHp = Math.Max(0, player.CurrentHp - healRetDmg);
                        logEntries.Add($"{monster.Name} attacks for **{healRetDmg}** damage!");
                        ApplyMonsterAbility(monster, playerFx, logEntries, player);
                        break;
                    }

                    // ── Offensive spells ─────────────────────────────────────
                    int spellBase = spellDef.BaseDamage + (int)(totalINT * spellDef.IntScale) + Random.Shared.Next(1, 8);
                    double elemMult = ElementSystem.GetMultiplier(spellDef.Element, monster.Element, isMagic: true);
                    int magicDmg = Math.Max(1, (int)((spellBase - (int)(monster.DEF * 0.15)) * elemMult * (1.0 - magicResist)));

                    session.MonsterCurrentHp = Math.Max(0, session.MonsterCurrentHp - magicDmg);

                    string magElemIcon  = ElementSystem.GetElementIcon(spellDef.Element);
                    string magResistNote = magicResist > 0 ? $" ✨ *({(int)(magicResist*100)}% resisted)*" : "";
                    string magElemMsg   = elemMult >= 1.5  ? $" {magElemIcon} **Super effective!**"
                                        : elemMult >= 1.25 ? $" {magElemIcon} *Effective!*"
                                        : elemMult <= 0.75 ? " 🛡️ *Not very effective...*"
                                        : "";
                    logEntries.Add($"{magElemIcon} **{spellDef.Name}** hits for **{magicDmg}** damage! (-{spellDef.MpCost} MP){magResistNote}{magElemMsg}");

                    // Apply elemental status to monster (35% chance)
                    var magEffect = ElementSystem.GetElementalStatusEffect(spellDef.Element);
                    if (magEffect != null && session.MonsterCurrentHp > 0 && Random.Shared.NextDouble() < 0.35)
                    {
                        var (mStr, mTurns) = ElementSystem.GetStatusParams(spellDef.Element);
                        StatusEffects.Apply(monsterFx, magEffect, mStr, mTurns);
                        logEntries.Add($"{StatusEffects.Icons.GetValueOrDefault(magEffect, "")} {monster.Name} is afflicted with **{magEffect}**!");
                    }

                    if (session.MonsterCurrentHp <= 0)
                    {
                        session.State = CombatState.Victory;
                        session.EndedAt = DateTime.UtcNow;
                        combatEnded = true;
                    }
                    else
                    {
                        session.PlayerDefending = false;
                        var magRetDmg = CalculateMonsterDamage(monster, effDEF, false, monsterBerserk);
                        player.CurrentHp = Math.Max(0, player.CurrentHp - magRetDmg);
                        logEntries.Add($"{monster.Name} attacks for **{magRetDmg}** damage!");
                        ApplyMonsterAbility(monster, playerFx, logEntries, player);
                    }
                    break;

                case CombatAction.UseItem:
                    if (string.IsNullOrWhiteSpace(args))
                    {
                        logEntries.Add("Specify an item name: /item Health Potion");
                        break;
                    }
                    var itemDef = await _uow.ItemDefinitions.GetByNameAsync(args.Trim());
                    if (itemDef == null) { logEntries.Add($"Item '{args.Trim()}' not found."); break; }
                    var invItem = await _uow.PlayerInventoryItems.GetByPlayerAndItemAsync(player.Id, itemDef.Id);
                    if (invItem == null || invItem.Quantity <= 0) { logEntries.Add($"You don't have any {itemDef.Name}."); break; }
                    if (itemDef.Type != GameItemType.Consumable) { logEntries.Add($"{itemDef.Name} is not a consumable."); break; }

                    if (itemDef.HealAmount > 0)
                    {
                        bool isPotion = itemDef.SubType is ItemSubType.HealthPotion or ItemSubType.ManaPotion;
                        int healAmt = isPotion
                            ? (int)(player.MaxHp * itemDef.HealAmount / 100.0)
                            : itemDef.HealAmount;
                        if (StatusEffects.IsCursed(playerFx)) healAmt /= 2;
                        int healed = Math.Min(healAmt, player.MaxHp - player.CurrentHp);
                        player.CurrentHp += healed;
                        logEntries.Add($"Used {itemDef.Name}! Restored **{healed}** HP.{(StatusEffects.IsCursed(playerFx) ? " *(Curse halved healing!)*" : "")}");
                    }
                    if (itemDef.ManaRestoreAmount > 0)
                    {
                        bool isManaPotion = itemDef.SubType is ItemSubType.HealthPotion or ItemSubType.ManaPotion;
                        int mpAmt = isManaPotion
                            ? (int)(player.MaxMp * itemDef.ManaRestoreAmount / 100.0)
                            : itemDef.ManaRestoreAmount;
                        int restored = Math.Min(mpAmt, player.MaxMp - player.CurrentMp);
                        player.CurrentMp += restored;
                        logEntries.Add($"Used {itemDef.Name}! Restored **{restored}** MP.");
                    }

                    invItem.Quantity--;
                    if (invItem.Quantity <= 0) _uow.PlayerInventoryItems.Remove(invItem);

                    var itemDmg = CalculateMonsterDamage(monster, effDEF, session.PlayerDefending, monsterBerserk);
                    player.CurrentHp = Math.Max(0, player.CurrentHp - itemDmg);
                    logEntries.Add($"{monster.Name} attacks for **{itemDmg}** damage!");
                    ApplyMonsterAbility(monster, playerFx, logEntries, player);
                    break;
            }
        }

        // ── Save status effects back ──────────────────────────────────────────
        session.PlayerStatusJson  = StatusEffects.Save(playerFx);
        session.MonsterStatusJson = StatusEffects.Save(monsterFx);

        // ── Check player death ────────────────────────────────────────────────
        if (player.CurrentHp <= 0 && !combatEnded)
        {
            session.State = CombatState.Defeat;
            session.EndedAt = DateTime.UtcNow;
            combatEnded = true;
        }

        session.TurnNumber++;
        session.LastTurnAt = DateTime.UtcNow;

        // Handle victory/defeat rewards/penalties
        object? resultPayload = null;
        if (session.State == CombatState.Victory)
        {
            var chatBonus    = 1.0 + Math.Min(player.ChatLevel * 0.05, 1.0);
            // Level-relative XP: bonus for punching up, penalty for farming weaklings
            var levelDiff    = monster.Level - player.Level;
            var levelMult    = Math.Max(0.3, Math.Min(2.0, 1.0 + levelDiff * 0.15));
            var xpMultiplier = chatBonus * levelMult;
            var xpGained     = (long)(monster.XpReward * xpMultiplier);
            var coinsGained = Random.Shared.NextInt64(monster.OrbRewardMin, monster.OrbRewardMax + 1);

            player.XP += xpGained;
            player.CoinBalance += coinsGained;
            player.TotalMonstersKilled++;

            // Combat skill XP
            var combatSkill = await _uow.PlayerSkills.GetByPlayerAndSkillAsync(player.Id, SkillType.Combat);
            if (combatSkill != null)
            {
                combatSkill.XP += xpGained / 2;
                CheckSkillLevelUp(combatSkill);
            }

            // Loot drops
            var lootDrops = new List<object>();
            double luckBonus = totalLUK * 0.002;
            foreach (var loot in monster.LootTable)
            {
                if (Random.Shared.NextDouble() < (double)loot.DropChance + luckBonus)
                {
                    int qty = Random.Shared.Next(loot.MinQuantity, loot.MaxQuantity + 1);
                    await AddItemToInventory(player.Id, loot.ItemDefinitionId, qty);
                    lootDrops.Add(new
                    {
                        name = loot.ItemDefinition.Name,
                        quantity = qty,
                        rarity = loot.ItemDefinition.Rarity.ToString()
                    });
                }
            }

            // Elemental material drop — based on monster element
            var dropElement = monster.EnchantDropElement ?? monster.Element;
            if (dropElement != Element.None)
            {
                double enchantDropChance = monster.EnchantedDropChance > 0
                    ? (double)monster.EnchantedDropChance
                    : 0.10;  // 10% base chance for any elemental monster
                if (Random.Shared.NextDouble() < enchantDropChance + luckBonus)
                {
                    var materialName = ElementSystem.GetElementalMaterial(dropElement);
                    if (materialName != null)
                    {
                        var matDef = await _uow.ItemDefinitions.GetByNameAsync(materialName);
                        if (matDef != null)
                        {
                            await AddItemToInventory(player.Id, matDef.Id, 1);
                            lootDrops.Add(new
                            {
                                name     = matDef.Name,
                                quantity = 1,
                                rarity   = matDef.Rarity.ToString()
                            });
                        }
                    }
                }
            }

            // Check level up
            bool leveledUp = false;
            while (player.XP >= XpToLevel(player.Level + 1) && player.Level < 100)
            {
                player.Level++;
                leveledUp = true;
                ApplyLevelUpStats(player);
            }

            resultPayload = new
            {
                result = "victory",
                xpGained,
                coinsGained,
                loot = lootDrops,
                leveledUp,
                newLevel = player.Level
            };
        }
        else if (session.State == CombatState.Defeat)
        {
            player.TotalDeaths++;
            long coinPenalty = 0;
            if (player.CoinBalance > 0)
            {
                coinPenalty = (long)(player.CoinBalance * 0.20);
                player.CoinBalance -= coinPenalty;
            }
            // Lose 10% of XP progress toward next level (can't drop below current level floor)
            long xpFloor   = XpToLevel(player.Level);
            long xpPenalty = (long)((player.XP - xpFloor) * 0.10);
            player.XP      = Math.Max(xpFloor, player.XP - xpPenalty);
            player.CurrentHp = (int)(player.MaxHp * 0.10);

            resultPayload = new
            {
                result    = "defeat",
                coinsLost = coinPenalty,
                xpLost    = xpPenalty
            };
        }

        // ── Persist status effects back to player when combat ends ───────────
        if (combatEnded)
            player.StatusJson = StatusEffects.Save(StatusEffects.Persistent(playerFx));

        await _uow.SaveChangesAsync();

        return GameCommandResult.Broadcast("combat_turn", new
        {
            playerName  = player.CharacterName,
            playerHp    = player.CurrentHp,
            playerMaxHp = player.MaxHp,
            playerMp    = player.CurrentMp,
            playerMaxMp = player.MaxMp,
            monsterName = monster.Name,
            monsterIcon = monster.Icon,
            monsterHp   = session.MonsterCurrentHp,
            monsterMaxHp= session.MonsterMaxHp,
            turn        = session.TurnNumber,
            log         = logEntries,
            state       = session.State.ToString(),
            statusEffects = StatusEffects.Format(StatusEffects.Load(session.PlayerStatusJson)),
            combatResult = resultPayload
        });
    }

    // ── Roll monster's special abilities and apply to player ─────────────────

    private static void ApplyMonsterAbility(
        MonsterDefinition monster, List<ActiveStatus> playerFx,
        List<string> log, PlayerCharacter player)
    {
        var abilities = StatusEffects.LoadAbilities(monster.AbilityJson);

        // Apply monster's base element status effect (30% chance, if not already in ability list)
        var baseElementEffect = ElementSystem.GetElementalStatusEffect(monster.Element);
        if (baseElementEffect != null)
        {
            bool coveredByAbility = abilities.Any(a => a.Type.Equals(baseElementEffect, StringComparison.OrdinalIgnoreCase));
            if (!coveredByAbility && Random.Shared.NextDouble() < 0.30)
            {
                var (elemStr, elemTurns) = ElementSystem.GetStatusParams(monster.Element);
                StatusEffects.Apply(playerFx, baseElementEffect, elemStr, elemTurns);
                var icon = StatusEffects.Icons.GetValueOrDefault(baseElementEffect, "");
                log.Add($"{icon} {monster.Name}'s **{monster.Element}** essence inflicts **{baseElementEffect}**!");
            }
        }

        if (abilities.Count == 0) return;

        // Cap how many abilities trigger per turn — scales with monster level
        int maxTriggers = monster.Level switch
        {
            <= 10 => 1,
            <= 25 => 2,
            <= 45 => 3,
            _     => 99
        };
        int triggered = 0;

        foreach (var ability in abilities)
        {
            if (triggered >= maxTriggers) break;
            if (Random.Shared.NextDouble() >= ability.Chance) continue;
            triggered++;

            // Scale strength by monster INT for relevant effects
            int strength = ability.Type.ToLower() switch
            {
                "poison"   => Math.Max(1, ability.Strength + monster.INT / 10),
                "burn"     => Math.Max(1, ability.Strength + monster.INT / 5),
                "bleed"    => Math.Max(1, ability.Strength + monster.STR / 8),
                "freeze"   => Math.Max(1, ability.Strength + monster.INT / 8),
                "mpdrain"  => Math.Max(1, ability.Strength + monster.INT / 6),
                _          => ability.Strength
            };

            StatusEffects.Apply(playerFx, ability.Type, strength, ability.Turns);

            var icon = StatusEffects.Icons.GetValueOrDefault(ability.Type, "❓");
            var desc = ability.Type.ToLower() switch
            {
                "poison"     => $"{icon} **{monster.Name}** poisons you! ({strength}% HP/turn for {ability.Turns} turns)",
                "burn"       => $"{icon} **{monster.Name}** sets you ablaze! ({strength} dmg/turn for {ability.Turns} turns)",
                "bleed"      => $"{icon} **{monster.Name}** causes you to bleed! ({strength} dmg/turn for {ability.Turns} turns)",
                "freeze"     => $"{icon} **{monster.Name}** freezes you solid! ({strength} dmg/turn, immobile)",
                "stone"      => $"{icon} **{monster.Name}** petrifies you! (immobile for {ability.Turns} turns)",
                "silence"    => $"{icon} **{monster.Name}** silences you! (no magic for {ability.Turns} turns)",
                "confusion"  => $"{icon} **{monster.Name}** confuses you! (may attack yourself for {ability.Turns} turns)",
                "defensedown"=> $"{icon} **{monster.Name}** shatters your guard! (DEF-{strength} for {ability.Turns} turns)",
                "attackdown" => $"{icon} **{monster.Name}** weakens your strikes! (STR-{strength} for {ability.Turns} turns)",
                "blind"      => $"{icon} **{monster.Name}** blinds you! (50% miss for {ability.Turns} turns)",
                "slow"       => $"{icon} **{monster.Name}** slows you down! (skip every other turn for {ability.Turns} turns)",
                "curse"      => $"{icon} **{monster.Name}** curses you! (healing halved for {ability.Turns} turns)",
                "mpdrain"    => $"{icon} **{monster.Name}** drains your mana! ({strength} MP/turn for {ability.Turns} turns)",
                _            => $"{icon} **{monster.Name}** uses {ability.Type}!"
            };
            log.Add(desc);
        }
    }

    private async Task<GameCommandResult> HandleShop(PlayerCharacter player, string category)
    {
        var all = await _uow.ItemDefinitions.GetAllAsync();
        var shopItems = all
            .Where(i => i.BuyPrice > 0 && i.Type != GameItemType.Collectible)
            .OrderBy(i => i.Type)
            .ThenBy(i => i.BuyPrice)
            .ToList();

        // Subcategory filter — supports "potions health", "potions mana", "weapons swords", etc.
        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim().ToLower();
            shopItems = shopItems.Where(i => cat switch
            {
                // Potions
                "potions health" or "health potions" => i.SubType == ItemSubType.HealthPotion && i.ManaRestoreAmount == 0,
                "potions mana"   or "mana potions"   => i.SubType == ItemSubType.ManaPotion   && i.HealAmount == 0,
                "potions elixirs" or "elixirs"        => i.HealAmount > 0 && i.ManaRestoreAmount > 0,
                "potions" or "potion"                 => i.Type == GameItemType.Consumable && i.SubType is ItemSubType.HealthPotion or ItemSubType.ManaPotion,
                // Food
                "food" or "food fish" or "fish"       => i.Type == GameItemType.Consumable && i.HealAmount > 0,
                // Weapons
                "weapons swords" or "swords"          => i.SubType == ItemSubType.Sword,
                "weapons axes"   or "axes"            => i.SubType == ItemSubType.Axe,
                "weapons bows"   or "bows"            => i.SubType == ItemSubType.Bow,
                "weapons staves" or "staves" or "staffs" => i.SubType == ItemSubType.Staff,
                "weapons daggers" or "daggers"        => i.SubType == ItemSubType.Dagger,
                "weapons"                             => i.Type == GameItemType.Weapon,
                // Armor
                "armor helmets" or "helmets"          => i.SubType == ItemSubType.Helmet,
                "armor chest"   or "chest"            => i.SubType == ItemSubType.Chestplate,
                "armor legs"    or "legs"             => i.SubType == ItemSubType.Leggings,
                "armor boots"   or "boots"            => i.SubType == ItemSubType.Boots,
                "armor shields" or "shields"          => i.SubType == ItemSubType.Shield,
                "armor rings"   or "rings"            => i.SubType == ItemSubType.Ring,
                "armor amulets" or "amulets"          => i.SubType == ItemSubType.Amulet,
                "armor"                               => i.Type == GameItemType.Armor,
                _                                     => true
            }).ToList();
        }

        return GameCommandResult.Single("shop", new
        {
            coins    = player.CoinBalance,
            category = string.IsNullOrWhiteSpace(category) ? "All" : category.Trim(),
            items = shopItems.Select(i => new
            {
                name      = i.Name,
                icon      = i.Icon,
                type      = i.Type.ToString(),
                subType   = i.SubType.ToString(),
                rarity    = i.Rarity.ToString(),
                levelReq  = i.LevelReq,
                buyPrice  = i.BuyPrice,
                effect    = (i.SubType is ItemSubType.HealthPotion or ItemSubType.ManaPotion)
                    ? (i.HealAmount > 0 && i.ManaRestoreAmount > 0
                        ? $"+{i.HealAmount}% HP / +{i.ManaRestoreAmount}% MP"
                        : i.HealAmount > 0 ? $"+{i.HealAmount}% HP"
                        : $"+{i.ManaRestoreAmount}% MP")
                    : (i.HealAmount > 0 && i.ManaRestoreAmount > 0
                        ? $"+{i.HealAmount} HP / +{i.ManaRestoreAmount} MP"
                        : i.HealAmount > 0 ? $"+{i.HealAmount} HP"
                        : i.ManaRestoreAmount > 0 ? $"+{i.ManaRestoreAmount} MP"
                        : ""),
                bonuses   = string.Join(" ", new[]
                {
                    i.BonusSTR > 0 ? $"STR+{i.BonusSTR}" : "",
                    i.BonusDEF > 0 ? $"DEF+{i.BonusDEF}" : "",
                    i.BonusINT > 0 ? $"INT+{i.BonusINT}" : "",
                    i.BonusDEX > 0 ? $"DEX+{i.BonusDEX}" : "",
                    i.BonusLUK > 0 ? $"LUK+{i.BonusLUK}" : "",
                }.Where(s => s.Length > 0))
            })
        });
    }

    private async Task<GameCommandResult> HandleBuy(PlayerCharacter player, string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return GameCommandResult.Single("error", new { message = "Specify an item: /buy <name>" });

        // Support optional quantity: /buy Mana Potion 5
        int qty = 1;
        var parts = args.Trim().Split(' ');
        if (parts.Length > 1 && int.TryParse(parts[^1], out int parsed) && parsed > 0)
        {
            qty  = Math.Min(parsed, 99);
            args = string.Join(' ', parts[..^1]);
        }

        var itemDef = await _uow.ItemDefinitions.GetByNameAsync(args.Trim());
        if (itemDef == null)
            return GameCommandResult.Single("error", new { message = $"'{args.Trim()}' not found in shop." });
        if (itemDef.BuyPrice <= 0)
            return GameCommandResult.Single("error", new { message = $"{itemDef.Name} is not sold here." });

        long total = itemDef.BuyPrice * qty;
        if (player.CoinBalance < total)
            return GameCommandResult.Single("error", new
            {
                message = $"Not enough coins. {qty}x {itemDef.Name} costs 🪙 {total:N0}, you have 🪙 {player.CoinBalance:N0}."
            });

        player.CoinBalance -= total;

        var inv = await _uow.PlayerInventoryItems.GetByPlayerAndItemAsync(player.Id, itemDef.Id);
        if (inv != null)
            inv.Quantity += qty;
        else
            await _uow.PlayerInventoryItems.AddAsync(new PlayerInventoryItem
                { PlayerId = player.Id, ItemDefinitionId = itemDef.Id, Quantity = qty });

        await _uow.SaveChangesAsync();

        return GameCommandResult.Single("buy", new
        {
            item           = itemDef.Name,
            icon           = itemDef.Icon,
            qty,
            total,
            newCoinBalance = player.CoinBalance
        });
    }

    private async Task<GameCommandResult> HandleSell(PlayerCharacter player, string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return GameCommandResult.Single("error", new { message = "Specify an item: /sell <name> [qty]" });

        // Optional quantity suffix: /sell Wolf Pelt 5
        int qty   = 1;
        var parts = args.Trim().Split(' ');
        if (parts.Length > 1 && int.TryParse(parts[^1], out int parsed) && parsed > 0)
        {
            qty  = Math.Min(parsed, 9999);
            args = string.Join(' ', parts[..^1]);
        }

        var itemDef = await _uow.ItemDefinitions.GetByNameAsync(args.Trim());
        if (itemDef == null)
            return GameCommandResult.Single("error", new { message = $"'{args.Trim()}' not found." });

        var inv = await _uow.PlayerInventoryItems.GetByPlayerAndItemAsync(player.Id, itemDef.Id);
        if (inv == null || inv.Quantity < qty)
            return GameCommandResult.Single("error", new { message = $"You don't have {qty}x {itemDef.Name}." });

        // 45% of buy price per unit (or explicit sell price if set)
        long basePrice = itemDef.SellPrice > 0 ? itemDef.SellPrice : (long)(itemDef.BuyPrice * 0.45);

        // Enchant bonus: +15 coins per tier per enchant slot
        var enchants     = inv.GetEnchants();
        long enchantBonus = enchants.Sum(e => (long)e.Tier * 15);
        long priceEach   = basePrice + enchantBonus;
        long total       = priceEach * qty;

        inv.Quantity -= qty;
        if (inv.Quantity <= 0) _uow.PlayerInventoryItems.Remove(inv);
        player.CoinBalance += total;
        await _uow.SaveChangesAsync();

        return GameCommandResult.Single("sell", new
        {
            item           = itemDef.Name,
            icon           = itemDef.Icon,
            qty,
            total,
            priceEach,
            enchantBonus,
            newCoinBalance = player.CoinBalance
        });
    }

    private async Task<GameCommandResult> HandleInventory(PlayerCharacter player)
    {
        var items = await _uow.PlayerInventoryItems.GetByPlayerIdAsync(player.Id);
        var itemList = items.Select(i =>
        {
            var enchants = i.GetEnchants();
            long sellValue = i.ItemDefinition.SellPrice > 0
                ? i.ItemDefinition.SellPrice
                : (long)(i.ItemDefinition.BuyPrice * 0.45);
            return new
            {
                name      = i.ItemDefinition.Name,
                icon      = i.ItemDefinition.Icon,
                type      = i.ItemDefinition.Type.ToString(),
                subType   = i.ItemDefinition.SubType.ToString(),
                rarity    = i.ItemDefinition.Rarity.ToString(),
                quantity  = i.Quantity,
                equipped  = i.IsEquipped,
                slot      = i.EquippedSlot?.ToString(),
                sellValue,
                bonusStr  = i.ItemDefinition.BonusSTR,
                bonusDef  = i.ItemDefinition.BonusDEF,
                bonusInt  = i.ItemDefinition.BonusINT,
                bonusDex  = i.ItemDefinition.BonusDEX,
                bonusVit  = i.ItemDefinition.BonusVIT,
                bonusLuk  = i.ItemDefinition.BonusLUK,
                minDmg    = i.ItemDefinition.MinDamage,
                maxDmg    = i.ItemDefinition.MaxDamage,
                healAmount= i.ItemDefinition.HealAmount,
                enchants  = enchants.Select(e => new
                {
                    element = e.Element.ToString(),
                    icon    = ElementSystem.GetElementIcon(e.Element),
                    name    = e.Name,
                    tier    = e.Tier,
                    bonus   = e.Bonus
                }),
                maxSlots  = PlayerInventoryItem.MaxSlotsForRarity(i.ItemDefinition.Rarity),
                usedSlots = enchants.Count
            };
        }).ToList();

        return GameCommandResult.Single("inventory", new { items = itemList });
    }

    private async Task<GameCommandResult> HandleEquip(PlayerCharacter player, string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return GameCommandResult.Single("error", new { message = "Specify item name: /equip Iron Sword" });

        var activeCombat = await _uow.CombatSessions.GetActiveByPlayerIdAsync(player.Id);
        if (activeCombat != null)
            return GameCommandResult.Single("error", new { message = "Cannot change equipment during combat!" });

        var itemDef = await _uow.ItemDefinitions.GetByNameAsync(args.Trim());
        if (itemDef == null)
            return GameCommandResult.Single("error", new { message = $"Item '{args.Trim()}' not found." });

        if (!itemDef.EquipSlot.HasValue)
            return GameCommandResult.Single("error", new { message = $"{itemDef.Name} cannot be equipped." });

        if (itemDef.LevelReq > player.Level)
            return GameCommandResult.Single("error", new { message = $"Requires level {itemDef.LevelReq}." });

        if (itemDef.ClassReq.HasValue && itemDef.ClassReq != player.Class)
            return GameCommandResult.Single("error", new { message = $"Requires class {itemDef.ClassReq}." });

        var invItem = await _uow.PlayerInventoryItems.GetByPlayerAndItemAsync(player.Id, itemDef.Id);
        if (invItem == null)
            return GameCommandResult.Single("error", new { message = $"You don't have {itemDef.Name}." });

        // Unequip current item in slot
        var currentEquipped = await _uow.PlayerInventoryItems.GetEquippedInSlotAsync(player.Id, itemDef.EquipSlot.Value);
        if (currentEquipped != null)
        {
            currentEquipped.IsEquipped = false;
            currentEquipped.EquippedSlot = null;
        }

        invItem.IsEquipped = true;
        invItem.EquippedSlot = itemDef.EquipSlot.Value;

        await _uow.SaveChangesAsync();

        return GameCommandResult.Single("equip", new
        {
            item = itemDef.Name,
            slot = itemDef.EquipSlot.Value.ToString(),
            unequipped = currentEquipped?.ItemDefinition?.Name
        });
    }

    private async Task<GameCommandResult> HandleUnequip(PlayerCharacter player, string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return GameCommandResult.Single("error", new { message = "Specify slot: /unequip MainHand" });

        if (!Enum.TryParse<EquipSlot>(args.Trim(), true, out var slot))
            return GameCommandResult.Single("error", new { message = $"Invalid slot. Valid: {string.Join(", ", Enum.GetNames<EquipSlot>())}" });

        var equipped = await _uow.PlayerInventoryItems.GetEquippedInSlotAsync(player.Id, slot);
        if (equipped == null)
            return GameCommandResult.Single("error", new { message = $"Nothing equipped in {slot}." });

        equipped.IsEquipped = false;
        equipped.EquippedSlot = null;
        await _uow.SaveChangesAsync();

        return GameCommandResult.Single("unequip", new { item = equipped.ItemDefinition.Name, slot = slot.ToString() });
    }

    private async Task<GameCommandResult> HandleGather(PlayerCharacter player, string command)
    {
        if (player.LastGatherAt.HasValue && player.LastGatherAt.Value.AddSeconds(5) > DateTime.UtcNow)
        {
            var remaining = (int)(player.LastGatherAt.Value.AddSeconds(5) - DateTime.UtcNow).TotalSeconds + 1;
            return GameCommandResult.Single("error", new { message = $"Gathering on cooldown! {remaining}s remaining." });
        }

        var activeCombat = await _uow.CombatSessions.GetActiveByPlayerIdAsync(player.Id);
        if (activeCombat != null)
            return GameCommandResult.Single("error", new { message = "Cannot gather during combat!" });

        SkillType skillType;

        switch (command)
        {
            case "/mine":
                skillType = SkillType.Mining;
                break;
            case "/fish":
                skillType = SkillType.Fishing;
                break;
            case "/chop":
                skillType = SkillType.Woodcutting;
                break;
            default:
                return GameCommandResult.NotHandled();
        }

        var skill = await _uow.PlayerSkills.GetByPlayerAndSkillAsync(player.Id, skillType);
        if (skill == null)
        {
            skill = new PlayerSkill { PlayerId = player.Id, SkillType = skillType, Level = 1, XP = 0, XpToNextLevel = 75 };
            await _uow.PlayerSkills.AddAsync(skill);
        }

        var level = skill.Level;

        // Check for an equipped gathering tool in MainHand (BonusLUK = extra qty)
        var equippedItems = await _uow.PlayerInventoryItems.GetEquippedItemsAsync(player.Id);
        var mainHand = equippedItems.FirstOrDefault(e => e.EquippedSlot == EquipSlot.MainHand);
        int toolBonus = 0;
        string? toolName = null;
        if (mainHand != null)
        {
            bool isMatchingTool = command switch
            {
                "/mine" => mainHand.ItemDefinition.SubType == ItemSubType.Pickaxe,
                "/fish" => mainHand.ItemDefinition.SubType == ItemSubType.FishingRod,
                "/chop" => mainHand.ItemDefinition.SubType == ItemSubType.Axe,
                _ => false
            };
            if (isMatchingTool)
            {
                toolBonus = mainHand.ItemDefinition.BonusLUK;
                toolName  = mainHand.ItemDefinition.Name;
            }
        }

        // Determine primary item name based on skill level (highest tier unlocked)
        // 80% chance of highest tier, 20% chance of a random lower tier
        string primaryItemName = command switch
        {
            "/mine" => level >= 85 ? "Voidstone"
                     : level >= 70 ? "Adamantium Ore"
                     : level >= 55 ? "Adamantite Ore"
                     : level >= 40 ? "Mithril Ore"
                     : level >= 30 ? "Gold Ore"
                     : level >= 20 ? "Silver Ore"
                     : level >= 10 ? "Iron Ore"
                     : "Copper Ore",

            "/fish" => level >= 75 ? "Raw Abyssal Eel"
                     : level >= 60 ? "Raw Shark"
                     : level >= 45 ? "Raw Swordfish"
                     : level >= 30 ? "Raw Lobster"
                     : level >= 20 ? "Raw Tuna"
                     : level >= 12 ? "Raw Salmon"
                     : level >=  5 ? "Raw Trout"
                     : "Raw Shrimp",

            "/chop" => level >= 90 ? "Void Wood"
                     : level >= 75 ? "Magic Logs"
                     : level >= 60 ? "Yew Logs"
                     : level >= 45 ? "Maple Logs"
                     : level >= 30 ? "Willow Logs"
                     : level >= 15 ? "Oak Logs"
                     : "Wood",

            _ => "Wood"
        };

        // Build all tiers the player can access (for the 20% lower-tier roll)
        string[] allUnlockedMine = level >= 85 ? new[] { "Copper Ore", "Iron Ore", "Silver Ore", "Gold Ore", "Mithril Ore", "Adamantite Ore", "Adamantium Ore", "Voidstone" }
                                 : level >= 70 ? new[] { "Copper Ore", "Iron Ore", "Silver Ore", "Gold Ore", "Mithril Ore", "Adamantite Ore", "Adamantium Ore" }
                                 : level >= 55 ? new[] { "Copper Ore", "Iron Ore", "Silver Ore", "Gold Ore", "Mithril Ore", "Adamantite Ore" }
                                 : level >= 40 ? new[] { "Copper Ore", "Iron Ore", "Silver Ore", "Gold Ore", "Mithril Ore" }
                                 : level >= 30 ? new[] { "Copper Ore", "Iron Ore", "Silver Ore", "Gold Ore" }
                                 : level >= 20 ? new[] { "Copper Ore", "Iron Ore", "Silver Ore" }
                                 : level >= 10 ? new[] { "Copper Ore", "Iron Ore" }
                                 : new[] { "Copper Ore" };

        string[] allUnlockedFish = level >= 75 ? new[] { "Raw Shrimp", "Raw Trout", "Raw Salmon", "Raw Tuna", "Raw Lobster", "Raw Swordfish", "Raw Shark", "Raw Abyssal Eel" }
                                 : level >= 60 ? new[] { "Raw Shrimp", "Raw Trout", "Raw Salmon", "Raw Tuna", "Raw Lobster", "Raw Swordfish", "Raw Shark" }
                                 : level >= 45 ? new[] { "Raw Shrimp", "Raw Trout", "Raw Salmon", "Raw Tuna", "Raw Lobster", "Raw Swordfish" }
                                 : level >= 30 ? new[] { "Raw Shrimp", "Raw Trout", "Raw Salmon", "Raw Tuna", "Raw Lobster" }
                                 : level >= 20 ? new[] { "Raw Shrimp", "Raw Trout", "Raw Salmon", "Raw Tuna" }
                                 : level >= 12 ? new[] { "Raw Shrimp", "Raw Trout", "Raw Salmon" }
                                 : level >=  5 ? new[] { "Raw Shrimp", "Raw Trout" }
                                 : new[] { "Raw Shrimp" };

        string[] allUnlockedChop = level >= 90 ? new[] { "Wood", "Oak Logs", "Willow Logs", "Maple Logs", "Yew Logs", "Magic Logs", "Void Wood" }
                                 : level >= 75 ? new[] { "Wood", "Oak Logs", "Willow Logs", "Maple Logs", "Yew Logs", "Magic Logs" }
                                 : level >= 60 ? new[] { "Wood", "Oak Logs", "Willow Logs", "Maple Logs", "Yew Logs" }
                                 : level >= 45 ? new[] { "Wood", "Oak Logs", "Willow Logs", "Maple Logs" }
                                 : level >= 30 ? new[] { "Wood", "Oak Logs", "Willow Logs" }
                                 : level >= 15 ? new[] { "Wood", "Oak Logs" }
                                 : new[] { "Wood" };

        // 80% chance of highest unlocked tier, 20% chance of a random lower tier
        string[] unlockedTiers = command switch
        {
            "/mine" => allUnlockedMine,
            "/fish" => allUnlockedFish,
            "/chop" => allUnlockedChop,
            _ => new[] { primaryItemName }
        };

        string itemName;
        if (unlockedTiers.Length == 1 || Random.Shared.NextDouble() < 0.80)
            itemName = primaryItemName;
        else
            itemName = unlockedTiers[Random.Shared.Next(unlockedTiers.Length - 1)]; // any tier except highest

        var itemDef = await _uow.ItemDefinitions.GetByNameAsync(itemName);

        int qty = 1 + skill.Level / 10 + toolBonus;
        long xpGained = 15 + skill.Level * 2 + toolBonus * 3;

        if (itemDef != null)
            await AddItemToInventory(player.Id, itemDef.Id, qty);

        // Gem bonus roll for mining (10% base + 0.5% per 10 levels)
        string? bonusGemName = null;
        if (command == "/mine")
        {
            double gemChance = 0.10 + (level / 10) * 0.005;
            if (Random.Shared.NextDouble() < gemChance)
            {
                bonusGemName = level >= 70 ? "Diamond"
                             : level >= 50 ? "Ruby"
                             : level >= 30 ? "Emerald"
                             : "Sapphire";
                var gemDef = await _uow.ItemDefinitions.GetByNameAsync(bonusGemName);
                if (gemDef != null)
                    await AddItemToInventory(player.Id, gemDef.Id, 1);
            }
        }

        skill.XP += xpGained;
        CheckSkillLevelUp(skill);

        player.LastGatherAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        return GameCommandResult.Single("gather", new
        {
            action = command.TrimStart('/'),
            item = itemName,
            quantity = qty,
            bonusGem = bonusGemName,
            xpGained,
            skillLevel = skill.Level,
            skillXp = skill.XP,
            skillXpToNext = skill.XpToNextLevel,
            toolName,
            toolBonus
        });
    }

    // ── Raw fish → cooked item mapping ────────────────────────────────────────
    private static readonly Dictionary<string, (string CookedName, long XpReward)> _fishCookMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Raw Shrimp"]      = ("Cooked Shrimp",      10),
            ["Raw Trout"]       = ("Cooked Trout",       15),
            ["Raw Salmon"]      = ("Cooked Salmon",      20),
            ["Raw Tuna"]        = ("Cooked Tuna",        25),
            ["Raw Lobster"]     = ("Cooked Lobster",     35),
            ["Raw Swordfish"]   = ("Cooked Swordfish",   45),
            ["Raw Shark"]       = ("Cooked Shark",       60),
            ["Raw Abyssal Eel"] = ("Cooked Abyssal Eel", 80),
        };

    private async Task<GameCommandResult> HandleCook(PlayerCharacter player, string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return GameCommandResult.Single("error", new { message = "Specify a raw fish to cook: /cook Raw Shark" });

        // Block cooking during active combat
        var activeCombat = await _uow.CombatSessions.GetActiveByPlayerIdAsync(player.Id);
        if (activeCombat != null)
            return GameCommandResult.Single("error", new { message = "Cannot cook during combat!" });

        var rawName = args.Trim();
        if (!_fishCookMap.TryGetValue(rawName, out var cookEntry))
            return GameCommandResult.Single("error", new
            {
                message = $"'{rawName}' is not a cookable fish. Valid options: {string.Join(", ", _fishCookMap.Keys)}"
            });

        // Check the player has the raw fish
        var rawDef = await _uow.ItemDefinitions.GetByNameAsync(rawName);
        if (rawDef == null)
            return GameCommandResult.Single("error", new { message = $"Item '{rawName}' not found." });

        var rawInv = await _uow.PlayerInventoryItems.GetByPlayerAndItemAsync(player.Id, rawDef.Id);
        if (rawInv == null || rawInv.Quantity <= 0)
            return GameCommandResult.Single("error", new { message = $"You don't have any {rawName}." });

        // Get or create Cooking skill
        var cookingSkill = await _uow.PlayerSkills.GetByPlayerAndSkillAsync(player.Id, SkillType.Cooking);
        if (cookingSkill == null)
        {
            cookingSkill = new PlayerSkill
            {
                PlayerId = player.Id,
                SkillType = SkillType.Cooking,
                Level = 1,
                XP = 0,
                XpToNextLevel = 75
            };
            await _uow.PlayerSkills.AddAsync(cookingSkill);
        }

        // Roll burn chance: max(0%, 40% - cookingLevel * 0.5%)
        double burnChance = Math.Max(0.0, 0.40 - cookingSkill.Level * 0.005);
        bool burnt = Random.Shared.NextDouble() < burnChance;

        // Consume one raw fish
        rawInv.Quantity--;
        if (rawInv.Quantity <= 0)
            _uow.PlayerInventoryItems.Remove(rawInv);

        long xpGained;
        string resultItemName;
        long coinBonus;

        if (burnt)
        {
            xpGained = 5;
            resultItemName = "Burnt Fish";
            coinBonus = 1;
        }
        else
        {
            xpGained = cookEntry.XpReward;
            resultItemName = cookEntry.CookedName;
            coinBonus = 1 + (cookingSkill.Level / 10);
        }

        player.CoinBalance += coinBonus;

        var resultDef = await _uow.ItemDefinitions.GetByNameAsync(resultItemName);
        if (resultDef != null)
            await AddItemToInventory(player.Id, resultDef.Id, 1);

        cookingSkill.XP += xpGained;
        CheckSkillLevelUp(cookingSkill);

        await _uow.SaveChangesAsync();

        return GameCommandResult.Single("cook", new
        {
            rawFish       = rawName,
            result        = resultItemName,
            burnt,
            xpGained,
            coinBonus,
            skillLevel    = cookingSkill.Level,
            skillXp       = cookingSkill.XP,
            skillXpToNext = cookingSkill.XpToNextLevel,
            burnChance    = Math.Round(burnChance * 100, 1)
        });
    }

    private async Task<GameCommandResult> HandleCraft(PlayerCharacter player, string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return GameCommandResult.Single("error", new { message = "Specify recipe: /craft Iron Sword" });

        var recipe = await _uow.CraftingRecipes.GetByNameAsync(args.Trim());
        if (recipe == null)
            return GameCommandResult.Single("error", new { message = $"Recipe '{args.Trim()}' not found. Use /recipes." });

        var skill = await _uow.PlayerSkills.GetByPlayerAndSkillAsync(player.Id, recipe.RequiredSkill);
        if (skill == null || skill.Level < recipe.RequiredSkillLevel)
            return GameCommandResult.Single("error", new { message = $"Requires {recipe.RequiredSkill} level {recipe.RequiredSkillLevel}." });

        // Check ingredients
        foreach (var ingredient in recipe.Ingredients)
        {
            var inv = await _uow.PlayerInventoryItems.GetByPlayerAndItemAsync(player.Id, ingredient.ItemDefinitionId);
            if (inv == null || inv.Quantity < ingredient.Quantity)
                return GameCommandResult.Single("error", new { message = $"Need {ingredient.Quantity}x {ingredient.ItemDefinition.Name}." });
        }

        // Check orb cost
        var user = await _uow.Users.GetByIdAsync(player.UserId);
        if (user != null && user.OrbBalance < recipe.OrbCost)
            return GameCommandResult.Single("error", new { message = $"Need {recipe.OrbCost} orbs." });

        // Deduct ingredients
        foreach (var ingredient in recipe.Ingredients)
        {
            var inv = await _uow.PlayerInventoryItems.GetByPlayerAndItemAsync(player.Id, ingredient.ItemDefinitionId);
            if (inv != null)
            {
                inv.Quantity -= ingredient.Quantity;
                if (inv.Quantity <= 0) _uow.PlayerInventoryItems.Remove(inv);
            }
        }

        // Deduct orbs
        if (user != null && recipe.OrbCost > 0)
        {
            user.OrbBalance -= recipe.OrbCost;
            await _uow.OrbTransactions.AddAsync(new OrbTransaction
            {
                UserId = player.UserId,
                Amount = -recipe.OrbCost,
                Type = OrbTransactionType.CraftingSpent,
                Description = $"Crafted {recipe.Name}"
            });
        }

        // Roll success
        double successRate = Math.Min(0.98, (double)recipe.BaseSuccessRate + (skill.Level - recipe.RequiredSkillLevel) * 0.02);
        bool success = Random.Shared.NextDouble() < successRate;

        if (success)
        {
            await AddItemToInventory(player.Id, recipe.OutputItemId, recipe.OutputQuantity);
            skill.XP += recipe.XpReward;
        }
        else
        {
            skill.XP += recipe.XpReward / 4;
        }

        CheckSkillLevelUp(skill);
        await _uow.SaveChangesAsync();

        return GameCommandResult.Single("craft", new
        {
            recipe = recipe.Name,
            success,
            outputItem = success ? recipe.OutputItem.Name : null,
            outputQty = success ? recipe.OutputQuantity : 0,
            xpGained = success ? recipe.XpReward : recipe.XpReward / 4,
            skillLevel = skill.Level
        });
    }

    private async Task<GameCommandResult> HandleRecipes(PlayerCharacter player)
    {
        var recipes = await _uow.CraftingRecipes.GetAllAsync();
        var recipeList = recipes.Select(r => new
        {
            name = r.Name,
            output = r.OutputItem.Name,
            skill = r.RequiredSkill.ToString(),
            skillLevel = r.RequiredSkillLevel,
            ingredients = r.Ingredients.Select(i => new { name = i.ItemDefinition.Name, qty = i.Quantity }),
            orbCost = r.OrbCost
        }).ToList();

        return GameCommandResult.Single("recipes", new { recipes = recipeList });
    }

    private async Task<GameCommandResult> HandleTrade(PlayerCharacter player, string args, Guid channelId)
    {
        if (string.IsNullOrWhiteSpace(args))
            return GameCommandResult.Single("error", new { message = "Usage: /trade @username [item] [qty]" });

        var tradeParts = args.Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (tradeParts.Length < 2)
            return GameCommandResult.Single("error", new { message = "Usage: /trade @username [item] [qty]" });

        // Handle accept/decline
        if (tradeParts[0].Equals("accept", StringComparison.OrdinalIgnoreCase))
        {
            var pending = await _uow.TradeOffers.GetPendingByRecipientIdAsync(player.Id);
            if (pending == null)
                return GameCommandResult.Single("error", new { message = "No pending trade offer." });

            pending.Status = TradeStatus.Accepted;
            await _uow.SaveChangesAsync();
            return GameCommandResult.Broadcast("trade_accepted", new { trade = pending.Id });
        }
        if (tradeParts[0].Equals("decline", StringComparison.OrdinalIgnoreCase))
        {
            var pending = await _uow.TradeOffers.GetPendingByRecipientIdAsync(player.Id);
            if (pending == null)
                return GameCommandResult.Single("error", new { message = "No pending trade offer." });

            pending.Status = TradeStatus.Declined;
            await _uow.SaveChangesAsync();
            return GameCommandResult.Broadcast("trade_declined", new { trade = pending.Id });
        }

        // Create new trade offer
        var targetUsername = tradeParts[0].TrimStart('@');
        var targetUser = await _uow.Users.GetByUsernameAsync(targetUsername);
        if (targetUser == null)
            return GameCommandResult.Single("error", new { message = $"User '{targetUsername}' not found." });

        var targetPlayer = await _uow.PlayerCharacters.GetByUserIdAsync(targetUser.Id);
        if (targetPlayer == null)
            return GameCommandResult.Single("error", new { message = $"{targetUsername} has no game character." });

        var itemName = tradeParts.Length >= 2 ? tradeParts[1] : "";
        int tradeQty = 1;
        if (tradeParts.Length >= 3 && int.TryParse(tradeParts[2], out var q)) tradeQty = q;

        return GameCommandResult.Broadcast("trade_offer", new
        {
            from = player.CharacterName,
            to = targetUsername,
            item = itemName,
            quantity = tradeQty
        });
    }

    private async Task<GameCommandResult> HandleMarket(PlayerCharacter player, string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return GameCommandResult.Single("error", new { message = "Usage: /market list|browse|buy|cancel|listings [args]" });

        var marketParts = args.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var subCommand = marketParts[0].ToLower();
        var subArgs = marketParts.Length > 1 ? marketParts[1] : "";

        switch (subCommand)
        {
            case "browse":
                if (string.IsNullOrWhiteSpace(subArgs))
                    return GameCommandResult.Single("error", new { message = "Usage: /market browse [item name]" });

                var listings = await _uow.MarketplaceListings.GetActiveByItemNameAsync(subArgs);
                return GameCommandResult.Single("market_browse", new
                {
                    item = subArgs,
                    listings = listings.Select(l => new
                    {
                        id = l.Id,
                        seller = l.Seller?.User?.Username ?? "Unknown",
                        quantity = l.Quantity,
                        pricePerUnit = l.PricePerUnit,
                        totalPrice = l.PricePerUnit * l.Quantity
                    })
                });

            case "list":
                var listParts = subArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (listParts.Length < 2)
                    return GameCommandResult.Single("error", new { message = "Usage: /market list [item] [price] [qty]" });

                // Parse: if last two tokens are both numbers → item=rest, price=second-to-last, qty=last
                //        otherwise → item=all-but-last, price=last, qty=1
                long price;
                string listItemName;
                int listQty;
                if (listParts.Length >= 3
                    && long.TryParse(listParts[^1], out var parsedQty)
                    && long.TryParse(listParts[^2], out var parsedPrice))
                {
                    price        = parsedPrice;
                    listQty      = (int)Math.Max(1, parsedQty);
                    listItemName = string.Join(' ', listParts[..^2]);
                }
                else if (long.TryParse(listParts[^1], out var singlePrice))
                {
                    price        = singlePrice;
                    listQty      = 1;
                    listItemName = string.Join(' ', listParts[..^1]);
                }
                else
                {
                    return GameCommandResult.Single("error", new { message = "Invalid price." });
                }

                var listItemDef = await _uow.ItemDefinitions.GetByNameAsync(listItemName);
                if (listItemDef == null)
                    return GameCommandResult.Single("error", new { message = $"Item '{listItemName}' not found." });

                var listInv = await _uow.PlayerInventoryItems.GetByPlayerAndItemAsync(player.Id, listItemDef.Id);
                if (listInv == null || listInv.Quantity < listQty)
                    return GameCommandResult.Single("error", new { message = $"You don't have enough {listItemDef.Name}." });

                listInv.Quantity -= listQty;
                if (listInv.Quantity <= 0) _uow.PlayerInventoryItems.Remove(listInv);

                await _uow.MarketplaceListings.AddAsync(new MarketplaceListing
                {
                    SellerId         = player.Id,
                    ItemDefinitionId = listItemDef.Id,
                    Quantity         = listQty,
                    PricePerUnit     = price,
                    Status           = MarketListingStatus.Active,
                    CurrencyType     = MarketplaceCurrencyType.Coins,
                });
                await _uow.SaveChangesAsync();

                return GameCommandResult.Single("market_listed", new { item = listItemDef.Name, price, quantity = listQty });

            case "buy":
                if (string.IsNullOrWhiteSpace(subArgs))
                    return GameCommandResult.Single("error", new { message = "Usage: /market buy [item name]" });

                var cheapest = await _uow.MarketplaceListings.GetCheapestByItemNameAsync(subArgs);
                if (cheapest == null)
                    return GameCommandResult.Single("error", new { message = $"No listings found for '{subArgs}'." });

                var totalCost    = cheapest.PricePerUnit * cheapest.Quantity;
                var tax          = (long)(totalCost * 0.05);
                var totalWithTax = totalCost + tax;

                if (player.CoinBalance < totalWithTax)
                    return GameCommandResult.Single("error", new { message = $"Not enough coins. Need 🪙 {totalWithTax:N0} ({totalCost:N0} + {tax:N0} tax), you have 🪙 {player.CoinBalance:N0}." });

                player.CoinBalance -= totalWithTax;

                // Pay seller (minus 5% tax which goes to the house)
                if (cheapest.Seller != null)
                    cheapest.Seller.CoinBalance += totalCost;

                await AddItemToInventory(player.Id, cheapest.ItemDefinitionId, cheapest.Quantity);
                cheapest.Status = MarketListingStatus.Sold;
                cheapest.BuyerId = player.Id;
                await _uow.SaveChangesAsync();

                return GameCommandResult.Single("market_bought", new
                {
                    item = cheapest.ItemDefinition.Name,
                    quantity = cheapest.Quantity,
                    cost = totalWithTax,
                    tax
                });

            case "listings":
                var myListings = await _uow.MarketplaceListings.GetActiveBySellerIdAsync(player.Id);
                return GameCommandResult.Single("market_listings", new
                {
                    listings = myListings.Select(l => new
                    {
                        id = l.Id,
                        item = l.ItemDefinition.Name,
                        quantity = l.Quantity,
                        pricePerUnit = l.PricePerUnit
                    })
                });

            case "cancel":
                if (!Guid.TryParse(subArgs.Trim(), out var listingId))
                    return GameCommandResult.Single("error", new { message = "Usage: /market cancel [listing id]" });

                var listing = await _uow.MarketplaceListings.GetByIdAsync(listingId);
                if (listing == null || listing.SellerId != player.Id)
                    return GameCommandResult.Single("error", new { message = "Listing not found or not yours." });

                listing.Status = MarketListingStatus.Cancelled;
                await AddItemToInventory(player.Id, listing.ItemDefinitionId, listing.Quantity);
                await _uow.SaveChangesAsync();

                return GameCommandResult.Single("market_cancelled", new { item = listing.ItemDefinition.Name });

            case "search":
                var allListings = await _uow.MarketplaceListings.GetAllActiveAsync();
                var searchFilter = subArgs.Trim().ToLower();
                var grouped = allListings
                    .GroupBy(l => l.ItemDefinitionId)
                    .Select(g => new
                    {
                        icon     = g.First().ItemDefinition.Icon,
                        name     = g.First().ItemDefinition.Name,
                        cheapest = g.Min(l => l.PricePerUnit),
                        totalQty = g.Sum(l => l.Quantity),
                        sellers  = g.Count()
                    })
                    .Where(g => string.IsNullOrEmpty(searchFilter) || g.name.ToLower().Contains(searchFilter))
                    .OrderBy(g => g.name)
                    .ToList();
                return GameCommandResult.Single("market_search", new { items = grouped, query = subArgs.Trim() });

            default:
                return GameCommandResult.Single("error", new { message = "Usage: /market list|browse|buy|cancel|listings|search" });
        }
    }

    private async Task<GameCommandResult> HandleLeaderboard()
    {
        var topLevel = await _uow.PlayerCharacters.GetTopByLevelAsync(10);
        var topKills = await _uow.PlayerCharacters.GetTopByKillsAsync(10);

        return GameCommandResult.Single("leaderboard", new
        {
            byLevel = topLevel.Select((p, i) => new { rank = i + 1, name = p.CharacterName, level = p.Level, xp = p.XP }),
            byKills = topKills.Select((p, i) => new { rank = i + 1, name = p.CharacterName, kills = p.TotalMonstersKilled })
        });
    }

    private async Task<GameCommandResult> HandleGameConfig(Guid userId, Guid channelId, string args)
    {
        var parts = args.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return GameCommandResult.Single("error", new { message = "Usage: /game mute or /game unmute" });

        var config = await _uow.GameChannelConfigs.GetByChannelIdAsync(channelId);

        switch (parts[0].ToLower())
        {
            case "mute":
                if (config == null)
                {
                    config = new GameChannelConfig
                    {
                        ChannelId = channelId,
                        GameBotMuted = true,
                        MutedByUserId = userId,
                        MutedAt = DateTime.UtcNow
                    };
                    await _uow.GameChannelConfigs.AddAsync(config);
                }
                else
                {
                    config.GameBotMuted = true;
                    config.MutedByUserId = userId;
                    config.MutedAt = DateTime.UtcNow;
                }
                await _uow.SaveChangesAsync();
                return GameCommandResult.Broadcast("game_config", new { action = "muted", message = "Game bot muted in this channel." });

            case "unmute":
                if (config != null)
                {
                    config.GameBotMuted = false;
                    await _uow.SaveChangesAsync();
                }
                return GameCommandResult.Broadcast("game_config", new { action = "unmuted", message = "Game bot unmuted in this channel." });

            default:
                return GameCommandResult.Single("error", new { message = "Usage: /game mute or /game unmute" });
        }
    }

    // ── /enchant ──────────────────────────────────────────────────────────────
    // Usage:
    //   /enchant                     — show guide
    //   /enchant info <item name>    — show enchants on an item
    //   /enchant <book> [slot]       — apply book to equipped slot (default: MainHand)

    private async Task<GameCommandResult> HandleEnchant(PlayerCharacter player, string args)
    {
        var parts = args.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            var guide = EnchantRecipeConfig.Recipes
                .GroupBy(r => r.Element)
                .Select(g => $"{ElementSystem.GetElementIcon(g.Key)} **{g.Key}** — {g.Key} enchants (Tiers {g.Min(r => r.Tier)}-{g.Max(r => r.Tier)}): {ElementSystem.GetElementalStatusEffect(g.Key) ?? "no effect"}");

            return GameCommandResult.Single("enchant_guide", new
            {
                message = "**Enchanting Guide**\n" +
                          "Apply enchant books to equipped gear using `/enchant <book name>`.\n" +
                          "Max slots: Common=1, Uncommon=2, Rare=3, Epic=4, Legendary=5.\n" +
                          "Craft books with `/craftbook`. Duplicate elements not allowed on the same item.\n\n" +
                          string.Join("\n", guide)
            });
        }

        // /enchant info <item name>
        if (parts[0].Equals("info", StringComparison.OrdinalIgnoreCase) && parts.Length > 1)
        {
            var itemName = string.Join(' ', parts[1..]);
            var itemDef  = await _uow.ItemDefinitions.GetByNameAsync(itemName.Trim());
            if (itemDef == null)
                return GameCommandResult.Single("error", new { message = $"Item '{itemName}' not found." });

            var invItem = await _uow.PlayerInventoryItems.GetByPlayerAndItemAsync(player.Id, itemDef.Id);
            if (invItem == null)
                return GameCommandResult.Single("error", new { message = $"You don't have '{itemDef.Name}'." });

            var enchants  = invItem.GetEnchants();
            var maxSlots  = PlayerInventoryItem.MaxSlotsForRarity(itemDef.Rarity);
            var enchantDisplay = enchants.Count > 0
                ? string.Join(", ", enchants.Select(e => $"{ElementSystem.GetElementIcon(e.Element)} **{e.Name}** (+{e.Bonus})"))
                : "*(unenchanted)*";

            return GameCommandResult.Single("enchant_info", new
            {
                item     = itemDef.Name,
                rarity   = itemDef.Rarity.ToString(),
                enchants = enchantDisplay,
                slots    = $"{enchants.Count}/{maxSlots}"
            });
        }

        // /enchant <book name> [slot]
        // Parse optional trailing slot keyword (mainhand, offhand, head, chest, legs, feet, ring, amulet)
        EquipSlot targetSlot = EquipSlot.MainHand;
        var bookName = args.Trim();
        var slotNames = Enum.GetNames<EquipSlot>();
        var lastWord  = parts[^1];
        if (slotNames.Any(s => s.Equals(lastWord, StringComparison.OrdinalIgnoreCase)))
        {
            targetSlot = Enum.Parse<EquipSlot>(lastWord, true);
            bookName   = string.Join(' ', parts[..^1]);
        }

        // Find the enchant book in inventory
        var bookDef = await _uow.ItemDefinitions.GetByNameAsync(bookName.Trim());
        if (bookDef == null || bookDef.SubType != ItemSubType.EnchantBook)
            return GameCommandResult.Single("error", new { message = $"'{bookName.Trim()}' is not an enchant book." });

        var bookInv = await _uow.PlayerInventoryItems.GetByPlayerAndItemAsync(player.Id, bookDef.Id);
        if (bookInv == null || bookInv.Quantity <= 0)
            return GameCommandResult.Single("error", new { message = $"You don't have any {bookDef.Name}." });

        // Find the target equipped item
        var equipped  = await _uow.PlayerInventoryItems.GetEquippedItemsAsync(player.Id);
        var targetInv = equipped.FirstOrDefault(e => e.EquippedSlot == targetSlot);
        if (targetInv == null)
            return GameCommandResult.Single("error", new { message = $"Nothing equipped in {targetSlot} slot. Equip an item first." });

        // Validate enchantable (weapons + armor only, not consumables/collectibles)
        if (targetInv.ItemDefinition.Type is not GameItemType.Weapon and not GameItemType.Armor)
            return GameCommandResult.Single("error", new { message = $"{targetInv.ItemDefinition.Name} cannot be enchanted." });

        // Check slots
        var currentEnchants = targetInv.GetEnchants();
        int maxSlots2 = PlayerInventoryItem.MaxSlotsForRarity(targetInv.ItemDefinition.Rarity);
        if (currentEnchants.Count >= maxSlots2)
            return GameCommandResult.Single("error", new
            {
                message = $"{targetInv.ItemDefinition.Name} is at max enchants ({maxSlots2}/{maxSlots2}). " +
                          $"Use a higher rarity item for more slots."
            });

        // Check duplicate element
        var bookElement = bookDef.Element;
        if (currentEnchants.Any(e => e.Element == bookElement))
            return GameCommandResult.Single("error", new
            {
                message = $"{targetInv.ItemDefinition.Name} already has a {bookElement} enchant. Cannot stack the same element."
            });

        // Apply enchant
        int tier  = bookDef.EnchantTier > 0 ? bookDef.EnchantTier : 1;
        int bonus = ElementSystem.BonusForTier(tier);
        var newEnchant = new EnchantmentSlot(bookElement, tier, bonus, ElementSystem.MakeEnchantName(bookElement, tier));
        currentEnchants.Add(newEnchant);
        targetInv.SetEnchants(currentEnchants);

        // Consume book
        bookInv.Quantity--;
        if (bookInv.Quantity <= 0) _uow.PlayerInventoryItems.Remove(bookInv);

        // Grant Enchanting XP
        var enchSkill = await _uow.PlayerSkills.GetByPlayerAndSkillAsync(player.Id, SkillType.Enchanting);
        if (enchSkill != null)
        {
            enchSkill.XP += 30 + tier * 20;
            CheckSkillLevelUp(enchSkill);
        }

        await _uow.SaveChangesAsync();

        var allEnchants = currentEnchants
            .Select(e => $"{ElementSystem.GetElementIcon(e.Element)} **{e.Name}** (+{e.Bonus})");

        return GameCommandResult.Single("enchant_applied", new
        {
            book     = bookDef.Name,
            item     = targetInv.ItemDefinition.Name,
            enchant  = newEnchant.Name,
            element  = bookElement.ToString(),
            icon     = ElementSystem.GetElementIcon(bookElement),
            tier,
            bonus,
            slots    = $"{currentEnchants.Count}/{maxSlots2}",
            allEnchants = string.Join(", ", allEnchants),
            message  = $"{ElementSystem.GetElementIcon(bookElement)} Applied **{newEnchant.Name}** (+{bonus}) to **{targetInv.ItemDefinition.Name}**! " +
                       $"({currentEnchants.Count}/{maxSlots2} slots)"
        });
    }

    // ── /craftbook ────────────────────────────────────────────────────────────
    // Usage:
    //   /craftbook               — list all recipes
    //   /craftbook <book name>   — craft that enchant book

    private async Task<GameCommandResult> HandleCraftBook(PlayerCharacter player, string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            // List all recipes grouped by element
            var inventory = await _uow.PlayerInventoryItems.GetByPlayerIdAsync(player.Id);
            var invMap    = inventory.ToDictionary(i => i.ItemDefinition.Name, i => i.Quantity, StringComparer.OrdinalIgnoreCase);

            var recipeLines = EnchantRecipeConfig.Recipes.Select(r =>
            {
                var ingrs = r.Ingredients.Select(ing =>
                {
                    int have = invMap.GetValueOrDefault(ing.ItemName, 0);
                    string status = have >= ing.Qty ? "✅" : $"❌({have}/{ing.Qty})";
                    return $"{status} {ing.Qty}× {ing.ItemName}";
                });
                return $"{ElementSystem.GetElementIcon(r.Element)} **{r.BookName}** — {string.Join(", ", ingrs)}";
            });

            return GameCommandResult.Single("craftbook_list", new
            {
                message = "**Enchant Book Recipes** — `/craftbook <name>` to craft\n" + string.Join("\n", recipeLines)
            });
        }

        var recipe = EnchantRecipeConfig.FindByName(args.Trim());
        if (recipe == null)
            return GameCommandResult.Single("error", new { message = $"No enchant book recipe named '{args.Trim()}'." });

        // Validate materials
        var inv2 = await _uow.PlayerInventoryItems.GetByPlayerIdAsync(player.Id);
        var invMap2 = inv2.ToDictionary(i => i.ItemDefinition.Name, i => i, StringComparer.OrdinalIgnoreCase);
        var missing = new List<string>();

        foreach (var (itemName, qty) in recipe.Ingredients)
        {
            int have = invMap2.TryGetValue(itemName, out var slot) ? slot.Quantity : 0;
            if (have < qty) missing.Add($"{qty - have}× {itemName}");
        }

        if (missing.Count > 0)
            return GameCommandResult.Single("error", new
            {
                message = $"Not enough materials for **{recipe.BookName}**. Missing: {string.Join(", ", missing)}."
            });

        // Consume materials
        foreach (var (itemName, qty) in recipe.Ingredients)
        {
            var slot = invMap2[itemName];
            slot.Quantity -= qty;
            if (slot.Quantity <= 0) _uow.PlayerInventoryItems.Remove(slot);
        }

        // Add the crafted book
        var bookItemDef = await _uow.ItemDefinitions.GetByNameAsync(recipe.BookName);
        if (bookItemDef == null)
            return GameCommandResult.Single("error", new { message = $"'{recipe.BookName}' item not found in DB. Run /rpg sync to seed it." });

        await AddItemToInventory(player.Id, bookItemDef.Id, 1);

        // Grant Enchanting XP
        var enchSkill = await _uow.PlayerSkills.GetByPlayerAndSkillAsync(player.Id, SkillType.Enchanting);
        if (enchSkill != null)
        {
            enchSkill.XP += 50 + recipe.Tier * 30;
            CheckSkillLevelUp(enchSkill);
        }

        await _uow.SaveChangesAsync();

        return GameCommandResult.Single("craftbook", new
        {
            book    = recipe.BookName,
            element = recipe.Element.ToString(),
            icon    = ElementSystem.GetElementIcon(recipe.Element),
            tier    = recipe.Tier,
            message = $"{ElementSystem.GetElementIcon(recipe.Element)} Crafted **{recipe.BookName}**! Use `/enchant {recipe.BookName}` to apply it."
        });
    }

    // ── Helpers ──

    private async Task AddItemToInventory(Guid playerId, Guid itemDefId, int qty)
    {
        var existing = await _uow.PlayerInventoryItems.GetByPlayerAndItemAsync(playerId, itemDefId);
        if (existing != null)
        {
            existing.Quantity += qty;
        }
        else
        {
            await _uow.PlayerInventoryItems.AddAsync(new PlayerInventoryItem
            {
                PlayerId = playerId,
                ItemDefinitionId = itemDefId,
                Quantity = qty
            });
        }
    }

    private static int CalculateMonsterDamage(MonsterDefinition monster, int playerDEF, bool defending, bool berserk = false)
    {
        int raw = Random.Shared.Next(monster.MinDamage, monster.MaxDamage + 1) + (int)(monster.STR * 0.3) + monster.Level * 2;
        if (berserk) raw = (int)(raw * 1.5);
        int damage = Math.Max(1, raw - (int)(playerDEF * 0.3));
        if (defending) damage /= 2;
        return Math.Max(1, damage);
    }

    // Element advantage is now in ElementSystem.GetMultiplier()

    private static long XpToLevel(int level) => (long)Math.Floor(20 * Math.Pow(level, 2.5));

    private static void ApplyLevelUpStats(PlayerCharacter player)
    {
        switch (player.Class)
        {
            case GameClass.Warrior:
                player.STR += 3; player.DEF += 3; player.INT += 1; player.DEX += 1; player.VIT += 2; player.LUK += 1;
                break;
            case GameClass.Mage:
                player.STR += 1; player.DEF += 1; player.INT += 3; player.DEX += 1; player.VIT += 2; player.LUK += 1;
                break;
            case GameClass.Ranger:
                player.STR += 1; player.DEF += 1; player.INT += 1; player.DEX += 3; player.VIT += 2; player.LUK += 2;
                break;
            case GameClass.Cleric:
                player.STR += 1; player.DEF += 2; player.INT += 2; player.DEX += 1; player.VIT += 3; player.LUK += 1;
                break;
            case GameClass.Rogue:
                player.STR += 2; player.DEF += 1; player.INT += 1; player.DEX += 3; player.VIT += 1; player.LUK += 2;
                break;
        }
        player.MaxHp += 10 + player.VIT;
        player.MaxMp += 5 + player.INT / 2;
    }

    private static void CheckSkillLevelUp(PlayerSkill skill)
    {
        while (skill.XP >= skill.XpToNextLevel && skill.Level < 99)
        {
            skill.XP -= skill.XpToNextLevel;
            skill.Level++;
            skill.XpToNextLevel = (long)Math.Floor(75 * Math.Pow(skill.Level, 1.4));
        }
    }
}
