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
            "/magic" => await HandleCombatAction(player, CombatAction.Magic, args),
            "/item" => await HandleCombatAction(player, CombatAction.UseItem, args),
            "/flee" => await HandleCombatAction(player, CombatAction.Flee, args),
            "/inventory" or "/inv" => await HandleInventory(player),
            "/equip" => await HandleEquip(player, args),
            "/unequip" => await HandleUnequip(player, args),
            "/mine" or "/fish" or "/chop" => await HandleGather(player, command),
            "/cook" => await HandleCook(player, args),
            "/craft" => await HandleCraft(player, args),
            "/recipes" => await HandleRecipes(player),
            "/trade" => await HandleTrade(player, args, request.ChannelId),
            "/market" => await HandleMarket(player, args),
            "/leaderboard" or "/lb" => await HandleLeaderboard(),
            "/game" => await HandleGameConfig(request.UserId, request.ChannelId, args),
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
            CurrentMp = 50, MaxMp = 50
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
                new { cmd = "/mine", desc = "Mine for ore (60s cooldown)" },
                new { cmd = "/fish", desc = "Go fishing (60s cooldown)" },
                new { cmd = "/chop", desc = "Chop wood (60s cooldown)" },
                new { cmd = "/cook [raw fish]", desc = "Cook raw fish into food" },
                new { cmd = "/craft [item]", desc = "Craft an item" },
                new { cmd = "/recipes", desc = "View available recipes" },
                new { cmd = "/trade @user [item] [qty]", desc = "Trade with another player" },
                new { cmd = "/market list/browse/buy", desc = "Marketplace commands" },
                new { cmd = "/leaderboard", desc = "View top players" },
                new { cmd = "/game mute/unmute", desc = "Mute/unmute game bot (mods)" }
            }
        };
        return GameCommandResult.Single("help", helpText);
    }

    private async Task<GameCommandResult> HandleStats(PlayerCharacter player)
    {
        var equipped = await _uow.PlayerInventoryItems.GetEquippedItemsAsync(player.Id);
        var skills = await _uow.PlayerSkills.GetByPlayerIdAsync(player.Id);

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
            skills = skills.Select(s => new { skill = s.SkillType.ToString(), level = s.Level, xp = s.XP, xpToNext = s.XpToNextLevel })
        });
    }

    private async Task<GameCommandResult> HandleFight(PlayerCharacter player, string args, Guid channelId)
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
            monster = await _uow.MonsterDefinitions.GetByNameAsync(args.Trim());
        else
            monster = await _uow.MonsterDefinitions.GetRandomNearLevelAsync(player.Level);

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
            CombatLog = "[]"
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
                    // Monster retaliates
                    var fleeDmg = CalculateMonsterDamage(monster, totalDEF, false);
                    player.CurrentHp = Math.Max(0, player.CurrentHp - fleeDmg);
                    logEntries.Add($"{monster.Name} attacks for {fleeDmg} damage!");
                }
                break;

            case CombatAction.Defend:
                session.PlayerDefending = true;
                logEntries.Add("You take a defensive stance!");
                // Monster attacks but damage halved
                var defDmg = CalculateMonsterDamage(monster, totalDEF, true);
                player.CurrentHp = Math.Max(0, player.CurrentHp - defDmg);
                logEntries.Add($"{monster.Name} attacks for {defDmg} damage! (defended)");
                break;

            case CombatAction.Attack:
                int minDmg = weapon?.ItemDefinition.MinDamage ?? 1;
                int maxDmg = weapon?.ItemDefinition.MaxDamage ?? 3;
                var weaponElement = weapon?.ItemDefinition.Element ?? Element.None;

                int playerAttack = Random.Shared.Next(minDmg, maxDmg + 1) + (int)(totalSTR * 0.5);
                int damage = Math.Max(1, playerAttack - (int)(monster.DEF * 0.3));
                double elementBonus = GetElementMultiplier(weaponElement, monster.Element);
                double critChance = Math.Min(0.25, totalLUK * 0.005 + totalDEX * 0.002);
                bool crit = Random.Shared.NextDouble() < critChance;
                int finalDamage = (int)(damage * elementBonus * (crit ? 1.5 : 1.0));

                session.MonsterCurrentHp = Math.Max(0, session.MonsterCurrentHp - finalDamage);
                logEntries.Add($"You attack for {finalDamage} damage!{(crit ? " CRITICAL HIT!" : "")}{(elementBonus > 1.0 ? " Super effective!" : "")}");

                if (session.MonsterCurrentHp <= 0)
                {
                    session.State = CombatState.Victory;
                    session.EndedAt = DateTime.UtcNow;
                    combatEnded = true;
                }
                else
                {
                    // Monster retaliates
                    session.PlayerDefending = false;
                    var atkDmg = CalculateMonsterDamage(monster, totalDEF, false);
                    player.CurrentHp = Math.Max(0, player.CurrentHp - atkDmg);
                    logEntries.Add($"{monster.Name} attacks for {atkDmg} damage!");
                }
                break;

            case CombatAction.Magic:
                int spellDamage = (int)(totalINT * 1.5) + Random.Shared.Next(5, 15);
                int mpCost = 10 + player.Level;
                if (player.CurrentMp < mpCost)
                {
                    logEntries.Add($"Not enough MP! Need {mpCost} MP.");
                    break;
                }
                player.CurrentMp -= mpCost;
                int magicDmg = Math.Max(1, spellDamage - (int)(monster.DEF * 0.15));
                session.MonsterCurrentHp = Math.Max(0, session.MonsterCurrentHp - magicDmg);
                logEntries.Add($"You cast a spell for {magicDmg} damage! (-{mpCost} MP)");

                if (session.MonsterCurrentHp <= 0)
                {
                    session.State = CombatState.Victory;
                    session.EndedAt = DateTime.UtcNow;
                    combatEnded = true;
                }
                else
                {
                    session.PlayerDefending = false;
                    var magRetDmg = CalculateMonsterDamage(monster, totalDEF, false);
                    player.CurrentHp = Math.Max(0, player.CurrentHp - magRetDmg);
                    logEntries.Add($"{monster.Name} attacks for {magRetDmg} damage!");
                }
                break;

            case CombatAction.UseItem:
                if (string.IsNullOrWhiteSpace(args))
                {
                    logEntries.Add("Specify an item name: /item Health Potion");
                    break;
                }
                var itemDef = await _uow.ItemDefinitions.GetByNameAsync(args.Trim());
                if (itemDef == null)
                {
                    logEntries.Add($"Item '{args.Trim()}' not found.");
                    break;
                }
                var invItem = await _uow.PlayerInventoryItems.GetByPlayerAndItemAsync(player.Id, itemDef.Id);
                if (invItem == null || invItem.Quantity <= 0)
                {
                    logEntries.Add($"You don't have any {itemDef.Name}.");
                    break;
                }
                if (itemDef.Type != GameItemType.Consumable)
                {
                    logEntries.Add($"{itemDef.Name} is not a consumable.");
                    break;
                }

                if (itemDef.HealAmount > 0)
                {
                    int healed = Math.Min(itemDef.HealAmount, player.MaxHp - player.CurrentHp);
                    player.CurrentHp += healed;
                    logEntries.Add($"Used {itemDef.Name}! Restored {healed} HP.");
                }
                if (itemDef.ManaRestoreAmount > 0)
                {
                    int restored = Math.Min(itemDef.ManaRestoreAmount, player.MaxMp - player.CurrentMp);
                    player.CurrentMp += restored;
                    logEntries.Add($"Used {itemDef.Name}! Restored {restored} MP.");
                }

                invItem.Quantity--;
                if (invItem.Quantity <= 0) _uow.PlayerInventoryItems.Remove(invItem);

                // Monster still attacks
                var itemDmg = CalculateMonsterDamage(monster, totalDEF, session.PlayerDefending);
                player.CurrentHp = Math.Max(0, player.CurrentHp - itemDmg);
                logEntries.Add($"{monster.Name} attacks for {itemDmg} damage!");
                break;
        }

        // Check player death
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
            var xpGained = monster.XpReward;
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
            foreach (var loot in monster.LootTable)
            {
                double luckBonus = totalLUK * 0.002;
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

            // Check level up
            bool leveledUp = false;
            while (player.XP >= XpToLevel(player.Level + 1) && player.Level < 100)
            {
                player.Level++;
                leveledUp = true;
                ApplyLevelUpStats(player);
            }

            if (leveledUp)
            {
                player.CurrentHp = player.MaxHp;
                player.CurrentMp = player.MaxMp;
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
                coinPenalty = (long)(player.CoinBalance * 0.10);
                player.CoinBalance -= coinPenalty;
            }
            player.CurrentHp = (int)(player.MaxHp * 0.25);

            resultPayload = new
            {
                result = "defeat",
                coinsLost = coinPenalty
            };
        }

        await _uow.SaveChangesAsync();

        return GameCommandResult.Broadcast("combat_turn", new
        {
            playerName = player.CharacterName,
            playerHp = player.CurrentHp,
            playerMaxHp = player.MaxHp,
            playerMp = player.CurrentMp,
            playerMaxMp = player.MaxMp,
            monsterName = monster.Name,
            monsterHp = session.MonsterCurrentHp,
            monsterMaxHp = session.MonsterMaxHp,
            turn = session.TurnNumber,
            log = logEntries,
            state = session.State.ToString(),
            combatResult = resultPayload
        });
    }

    private async Task<GameCommandResult> HandleInventory(PlayerCharacter player)
    {
        var items = await _uow.PlayerInventoryItems.GetByPlayerIdAsync(player.Id);
        var itemList = items.Select(i => new
        {
            name = i.ItemDefinition.Name,
            type = i.ItemDefinition.Type.ToString(),
            rarity = i.ItemDefinition.Rarity.ToString(),
            quantity = i.Quantity,
            equipped = i.IsEquipped,
            slot = i.EquippedSlot?.ToString()
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
        if (player.LastGatherAt.HasValue && player.LastGatherAt.Value.AddSeconds(60) > DateTime.UtcNow)
        {
            var remaining = (int)(player.LastGatherAt.Value.AddSeconds(60) - DateTime.UtcNow).TotalSeconds;
            return GameCommandResult.Single("error", new { message = $"Gathering on cooldown! {remaining}s remaining." });
        }

        var activeCombat = await _uow.CombatSessions.GetActiveByPlayerIdAsync(player.Id);
        if (activeCombat != null)
            return GameCommandResult.Single("error", new { message = "Cannot gather during combat!" });

        SkillType skillType;
        string[] possibleItems;

        switch (command)
        {
            case "/mine":
                skillType = SkillType.Mining;
                possibleItems = new[] { "Iron Ore", "Copper Ore" };
                break;
            case "/fish":
                skillType = SkillType.Fishing;
                possibleItems = new[] { "Raw Fish" };
                break;
            case "/chop":
                skillType = SkillType.Woodcutting;
                possibleItems = new[] { "Wood" };
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

        // Pick random item from possibilities
        var itemName = possibleItems[Random.Shared.Next(possibleItems.Length)];
        var itemDef = await _uow.ItemDefinitions.GetByNameAsync(itemName);

        int qty = 1 + skill.Level / 10;
        long xpGained = 15 + skill.Level * 2;

        if (itemDef != null)
            await AddItemToInventory(player.Id, itemDef.Id, qty);

        skill.XP += xpGained;
        CheckSkillLevelUp(skill);

        player.LastGatherAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        return GameCommandResult.Single("gather", new
        {
            action = command.TrimStart('/'),
            item = itemName,
            quantity = qty,
            xpGained,
            skillLevel = skill.Level,
            skillXp = skill.XP,
            skillXpToNext = skill.XpToNextLevel
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

                // Parse: last part is qty (optional), second-to-last is price, rest is item name
                if (!long.TryParse(listParts[^1], out var price))
                    return GameCommandResult.Single("error", new { message = "Invalid price." });

                var listItemName = string.Join(' ', listParts[..^1]);
                int listQty = 1;

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
                    SellerId = player.Id,
                    ItemDefinitionId = listItemDef.Id,
                    Quantity = listQty,
                    PricePerUnit = price,
                    Status = MarketListingStatus.Active
                });
                await _uow.SaveChangesAsync();

                return GameCommandResult.Single("market_listed", new { item = listItemDef.Name, price, quantity = listQty });

            case "buy":
                if (string.IsNullOrWhiteSpace(subArgs))
                    return GameCommandResult.Single("error", new { message = "Usage: /market buy [item name]" });

                var cheapest = await _uow.MarketplaceListings.GetCheapestByItemNameAsync(subArgs);
                if (cheapest == null)
                    return GameCommandResult.Single("error", new { message = $"No listings found for '{subArgs}'." });

                var totalCost = cheapest.PricePerUnit * cheapest.Quantity;
                var tax = (long)(totalCost * 0.05);
                var totalWithTax = totalCost + tax;

                var buyer = await _uow.Users.GetByIdAsync(player.UserId);
                if (buyer == null || buyer.OrbBalance < totalWithTax)
                    return GameCommandResult.Single("error", new { message = $"Need {totalWithTax} orbs ({totalCost} + {tax} tax)." });

                buyer.OrbBalance -= totalWithTax;
                await _uow.OrbTransactions.AddAsync(new OrbTransaction
                {
                    UserId = player.UserId,
                    Amount = -totalWithTax,
                    Type = OrbTransactionType.MarketplacePurchase,
                    Description = $"Bought {cheapest.ItemDefinition.Name} from marketplace"
                });

                var sellerUser = await _uow.Users.GetByIdAsync(cheapest.Seller.UserId);
                if (sellerUser != null)
                {
                    sellerUser.OrbBalance += totalCost;
                    await _uow.OrbTransactions.AddAsync(new OrbTransaction
                    {
                        UserId = sellerUser.Id,
                        Amount = totalCost,
                        Type = OrbTransactionType.MarketplaceSale,
                        Description = $"Sold {cheapest.ItemDefinition.Name} on marketplace"
                    });
                }

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

            default:
                return GameCommandResult.Single("error", new { message = "Usage: /market list|browse|buy|cancel|listings" });
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

    private static int CalculateMonsterDamage(MonsterDefinition monster, int playerDEF, bool defending)
    {
        int raw = Random.Shared.Next(monster.MinDamage, monster.MaxDamage + 1) + (int)(monster.STR * 0.3);
        int damage = Math.Max(1, raw - (int)(playerDEF * 0.3));
        if (defending) damage /= 2;
        return Math.Max(1, damage);
    }

    private static double GetElementMultiplier(Element attacker, Element defender)
    {
        if (attacker == Element.None || defender == Element.None) return 1.0;
        // Fire > Ice > Lightning > Earth > Fire
        if ((attacker == Element.Fire && defender == Element.Ice) ||
            (attacker == Element.Ice && defender == Element.Lightning) ||
            (attacker == Element.Lightning && defender == Element.Earth) ||
            (attacker == Element.Earth && defender == Element.Fire))
            return 1.25;
        if ((defender == Element.Fire && attacker == Element.Ice) ||
            (defender == Element.Ice && attacker == Element.Lightning) ||
            (defender == Element.Lightning && attacker == Element.Earth) ||
            (defender == Element.Earth && attacker == Element.Fire))
            return 0.75;
        // Dark <-> Holy
        if ((attacker == Element.Dark && defender == Element.Holy) ||
            (attacker == Element.Holy && defender == Element.Dark))
            return 1.25;
        return 1.0;
    }

    private static long XpToLevel(int level) => (long)Math.Floor(100 * Math.Pow(level, 1.5));

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
