using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Items;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Map;
using Tile = SAOTRPG.Map.Tile;

namespace SAOTRPG.Systems;

public partial class TurnManager
{
    private bool HandleTileInteraction(Tile tile, int tx, int ty)
    {
        switch (tile.Type)
        {
            case TileType.Campfire:      HandleCampfire(tile); break;
            case TileType.Fountain:      HandleFountain(tile, tx, ty); break;
            case TileType.Shrine:        HandleShrine(tile, tx, ty); break;
            case TileType.Pillar:        HandlePillar(tile, tx, ty); break;
            case TileType.Chest:         HandleChest(tile, tx, ty); break;
            case TileType.Anvil:         HandleAnvil(tile); break;
            case TileType.BountyBoard:   HandleBountyBoard(); break;
            case TileType.Lava:          if (HandleLava(tile)) return true; break;
            case TileType.LoreStone:     HandleLoreStone(tile, tx, ty); break;
            case TileType.MonumentOfSwordsmen: HandleMonumentOfSwordsmen(); break;
            case TileType.DangerZone:    if (HandleDangerZone(tile)) return true; break;
            case TileType.Journal:       HandleJournal(tile, tx, ty); break;
            case TileType.EnchantShrine: HandleEnchantShrine(tile, tx, ty); break;
            case TileType.SecretShrine:  HandleSecretShrine(tile, tx, ty); break;
            case TileType.Lever:         HandleLever(tile); break;
            case TileType.PressurePlate: HandlePressurePlate(tile); break;
            case TileType.Mud:           HandleMud(tile); break;
            case TileType.BogWater:      HandleBogWater(tile); break;
            case TileType.CrackedIce:    HandleCrackedIce(tile); break;
        }
        return false;
    }

    private void HandleCampfire(Tile tile)
    {
        bool hadStatus = _poisonTurnsLeft > 0 || _bleedTurnsLeft > 0 || _slowTurnsLeft > 0;
        if (hadStatus)
        {
            _poisonTurnsLeft = 0; _bleedTurnsLeft = 0; _slowTurnsLeft = 0;
            _log.LogSystem("The campfire's warmth purges the toxins from your body!");
        }

        int heal = Math.Min(15 + CurrentFloor * 5, _player.MaxHealth - _player.CurrentHealth);
        if (heal > 0)
        {
            _player.CurrentHealth += heal;
            _log.Log($"The campfire's warmth restores {heal} HP.");
            _log.Log(FlavorText.CampfireQuotes[Random.Shared.Next(FlavorText.CampfireQuotes.Length)]);
        }
        else if (!hadStatus)
            _log.Log(FlavorText.CampfireFullHpFlavors[Random.Shared.Next(FlavorText.CampfireFullHpFlavors.Length)]);
        _restCounter = 0;
        _fatiguedWarned = false;
        _exhaustedWarned = false;
        // FB-051 — Campfire counts as restful sleep. Slightly less XP than
        // a full rest (10 vs 20) since the player doesn't spend turns.
        GrantCampfireSleepXp();
        CookingInteraction?.Invoke();
    }

    private void HandleFountain(Tile tile, int tx, int ty)
    {
        int heal = Math.Min(30 + CurrentFloor * 10, _player.MaxHealth - _player.CurrentHealth);
        if (heal > 0) { _player.CurrentHealth += heal; _log.LogSystem($"You drink from the fountain and restore {heal} HP!"); }
        else _log.Log("The fountain's water is refreshing, but you're already at full health.");
        Satiety = Math.Min(MaxSatiety, Satiety + 20);
        _restCounter = 0;
        _fatiguedWarned = false;
        _exhaustedWarned = false;
        _map.SetTileType(tx, ty, TileType.Floor);
    }

    private void HandleShrine(Tile tile, int tx, int ty)
    {
        int buff = 3 + CurrentFloor;
        _shrineBuff = buff;
        _shrineBuffTurns = 30;
        _log.LogSystem($"The shrine empowers you! (+{buff} ATK & DEF for 30 turns)");
        _map.SetTileType(tx, ty, TileType.Floor);
    }

    private void HandlePillar(Tile tile, int tx, int ty)
    {
        int revealRadius = 15;
        for (int px = -revealRadius; px <= revealRadius; px++)
        for (int py = -revealRadius; py <= revealRadius; py++)
        {
            int rx = _player.X + px, ry = _player.Y + py;
            if (_map.InBounds(rx, ry)) _map.SetExplored(rx, ry);
        }
        _log.LogSystem("The ancient pillar hums — a vision of the surrounding area fills your mind!");
        _map.SetTileType(tx, ty, TileType.Floor);
    }

    private void HandleChest(Tile tile, int tx, int ty)
    {
        _map.SetTileType(tx, ty, TileType.ChestOpened);
        OpenChest(tile, tx, ty);
    }

    private void HandleAnvil(Tile tile)
    {
        _log.Log("You approach the anvil. The forge glows with heat.");
        AnvilInteraction?.Invoke();
    }

    private void HandleBountyBoard()
    {
        if (_bountyComplete)
        { _log.Log("You've already completed this floor's bounty."); }
        else if (_bountyTarget != null)
        { _log.Log($"BOUNTY: Slay {_bountyKillsNeeded} {_bountyTarget} — {_bountyKillsCurrent}/{_bountyKillsNeeded} complete."); }
        else
        {
            int tier = Math.Clamp(CurrentFloor - 1, 0, 4);
            var mobNames = MobFactory.GetFloorMobNames(tier);
            _bountyTarget = mobNames[Random.Shared.Next(mobNames.Length)];
            _bountyKillsNeeded = Math.Min(5, 2 + CurrentFloor / 3);
            _bountyKillsCurrent = 0;
            _bountyRewardCol = 100 + CurrentFloor * 50;
            _bountyRewardXp = 50 + CurrentFloor * 25;
            _log.LogSystem(FlavorText.BountyAcceptFlavors[Random.Shared.Next(FlavorText.BountyAcceptFlavors.Length)]);
            _log.LogSystem($"  BOUNTY: Slay {_bountyKillsNeeded} {_bountyTarget}!");
            _log.LogSystem($"  Reward: {_bountyRewardCol} Col + {_bountyRewardXp} XP");
        }
    }

    private bool HandleLava(Tile tile)
    {
        int lavaDmg = 4 + CurrentFloor * 2;
        _player.TakeDamage(lavaDmg);
        _log.LogCombat($"The molten lava burns you! {lavaDmg} damage!");
        if (_player.IsDefeated)
        {
            LastKillerName = "lava";
            _log.LogSystem(FlavorText.DeathFlavors[Random.Shared.Next(FlavorText.DeathFlavors.Length)]);
            PlayerDied?.Invoke();
            return true;
        }
        return false;
    }

    private void HandleLoreStone(Tile tile, int tx, int ty)
    {
        int loreIdx = Random.Shared.Next(FlavorText.LoreStoneEntries.Length);
        string lore = FlavorText.LoreStoneEntries[loreIdx];
        _discoveredLore.Add(loreIdx);
        int loreXp = 5 * CurrentFloor;
        int loreLvl = _player.Level;
        _player.GainExperience(loreXp);
        if (_player.Level > loreLvl) LeveledUp?.Invoke();
        _log.Log($"You examine the ancient stone tablet...");
        _log.Log(lore);
        _log.LogLoot($"  +{loreXp} XP from the inscription. (Lore: {_discoveredLore.Count}/{FlavorText.LoreStoneEntries.Length})");
        _map.SetTileType(tx, ty, TileType.Floor);
    }

    private bool HandleDangerZone(Tile tile)
    {
        int dangerDmg = 1 + CurrentFloor / 5;
        _player.TakeDamage(dangerDmg);
        _log.LogCombat($"Corrupted ground sears you! {dangerDmg} damage!");
        if (_player.IsDefeated)
        {
            LastKillerName = "corrupted ground";
            _log.LogSystem(FlavorText.DeathFlavors[Random.Shared.Next(FlavorText.DeathFlavors.Length)]);
            PlayerDied?.Invoke();
            return true;
        }
        return false;
    }

    private static readonly string[] JournalEntries =
    {
        "Day 47 — We found a safe room behind a cracked wall. The chest inside saved us.",
        "The boss on this floor charges without warning. Keep your distance and counter.",
        "Note to self: levers open sealed passages. Look for them near dead ends.",
        "I can hear something through the walls... gas vents, maybe. Watch the green tiles.",
        "Argo's info was right — pressure plates near structures toggle hidden doors.",
        "The weather affects everything here. Rain makes traps harder to spot.",
        "If you're poisoned AND bleeding, get to a campfire fast. The reaction is brutal.",
        "Day 112 — Made it to Floor 25. The monsters here don't play fair.",
        "Pro tip: backstab unaware enemies for double damage. Approach from stealth.",
        "The shrines grant temporary power. Don't waste them on easy floors.",
        "Someone carved this into the wall: 'V to guard, riposte to win.'",
        "Day 203 — The wind on this floor carries throwables further. Use it.",
    };

    private void HandleJournal(Tile tile, int tx, int ty)
    {
        string entry = JournalEntries[Random.Shared.Next(JournalEntries.Length)];
        _log.Log("You find a weathered journal on the ground...");
        _log.Log($"  \"{entry}\"");
        int xpReward = 3 * CurrentFloor;
        int lvlBefore = _player.Level;
        _player.GainExperience(xpReward);
        if (_player.Level > lvlBefore) LeveledUp?.Invoke();
        _log.LogLoot($"  +{xpReward} XP");
        _map.SetTileType(tx, ty, TileType.Floor);
    }

    private void HandleEnchantShrine(Tile tile, int tx, int ty)
    {
        var slots = Enum.GetValues<EquipmentSlot>();
        var equipped = new List<(EquipmentSlot slot, EquipmentBase item)>();
        foreach (var slot in slots)
        {
            var eq = _player.Inventory.GetEquipped(slot);
            if (eq != null) equipped.Add((slot, eq));
        }

        if (equipped.Count == 0)
        {
            _log.Log("The enchant shrine glows, but you have nothing equipped to enchant.");
            return;
        }

        var (pickedSlot, pickedItem) = equipped[Random.Shared.Next(equipped.Count)];
        // Add +2 to a random core stat
        (string label, StatType statType)[] stats =
        {
            ("ATK", StatType.Attack),
            ("DEF", StatType.Defense),
            ("SPD", StatType.Speed),
        };
        var (statLabel, chosen) = stats[Random.Shared.Next(stats.Length)];
        // Add to the item's bonuses so unequip/re-equip keeps the enchant
        pickedItem.Bonuses.Add(chosen, 2);
        // Also apply immediately since the item is already equipped
        switch (chosen)
        {
            case StatType.Attack:  _player.BaseAttack += 2; break;
            case StatType.Defense: _player.BaseDefense += 2; break;
            case StatType.Speed:   _player.BaseSpeed += 2; break;
        }
        _log.LogSystem($"The shrine's energy flows into your {pickedItem.Name}! (+2 {statLabel})");
        _log.LogLoot($"  {pickedItem.Name} enchanted!");
        _map.SetTileType(tx, ty, TileType.Floor);
    }

    // Secret Shrine: one-shot T1 chain weapon grant; tile reverts to Floor.
    private void HandleSecretShrine(Tile tile, int tx, int ty)
    {
        if (!WeaponEvolutionChains.SecretShrineByFloor.TryGetValue(CurrentFloor, out var defId))
        {
            // Shrine on unlisted floor — defensively convert silently.
            _map.SetTileType(tx, ty, TileType.Floor);
            return;
        }

        var weapon = ItemRegistry.Create(defId);
        if (weapon == null)
        {
            _log.LogSystem($"The Secret Shrine hums... but whatever it held has vanished.");
            _map.SetTileType(tx, ty, TileType.Floor);
            return;
        }

        // Add to inventory; if full, drop to ground for later pickup. The ◈ prefix
        // triggers LogColorRules BrightRed (Divine Object cadence).
        if (_player.Inventory.AddItem(weapon))
        {
            _log.LogLoot($"  ◈ A Secret Shrine hums. You receive {weapon.Name}.");
        }
        else
        {
            _map.AddItem(tx, ty, weapon);
            _log.LogLoot($"  ◈ A Secret Shrine hums. {weapon.Name} rests at your feet (inventory full).");
        }

        _map.SetTileType(tx, ty, TileType.Floor);
    }

    private void ToggleLinkedDoor(Tile tile, string source)
    {
        if (tile.LinkedDoor is not var (dx, dy) || !_map.InBounds(dx, dy)) return;
        var doorTile = _map.GetTile(dx, dy);
        if (doorTile.Type == TileType.Wall)
        {
            _map.SetTileType(dx, dy, TileType.Door);
            _log.LogSystem($"The {source} activates — a wall slides open in the distance!");
        }
        else if (doorTile.Type == TileType.Door)
        {
            _map.SetTileType(dx, dy, TileType.Wall);
            _log.LogSystem($"The {source} activates — a passage seals shut!");
        }
    }

    // Opens the Monument of Swordsmen dialog. Tile does not consume itself so
    // the player can check progress repeatedly.
    private void HandleMonumentOfSwordsmen()
    {
        _log.Log("You stand before the Monument of Swordsmen...");
        _log.Log("  Names, victories, and titles are etched into the black iron.");
        MonumentInteraction?.Invoke();
    }

    private void HandleLever(Tile tile)
    {
        ToggleLinkedDoor(tile, "lever");
    }

    private void HandlePressurePlate(Tile tile)
    {
        ToggleLinkedDoor(tile, "pressure plate");
    }

    // Mud — on-entry Slow. Tops up to 2 turns; harmless if already slowed
    // longer (per-step refresh would spam the log).
    private void HandleMud(Tile tile)
    {
        if (_slowTurnsLeft >= 2) return;
        _slowTurnsLeft = Math.Max(_slowTurnsLeft, 1 + Random.Shared.Next(2));
        _log.LogCombat("The mud sucks at your boots — you slow down.");
    }

    // BogWater — on-entry Poison drip. Mirrors HandlePoisonTrap cadence but
    // only applies when not already poisoned so wading doesn't stack forever.
    private void HandleBogWater(Tile tile)
    {
        if (_poisonTurnsLeft > 0) return;
        _poisonTurnsLeft = 2 + Random.Shared.Next(2);
        _poisonDamagePerTick = 1 + CurrentFloor / 2;
        _log.LogCombat($"The fetid bogwater seeps into your cuts. ({_poisonDamagePerTick} dmg/turn for {_poisonTurnsLeft} turns)");
    }

    // CrackedIce — 25% chance per step to stun for 1 turn (slip). Otherwise
    // a flavor whisper log only. Respects existing stun cooldown.
    private void HandleCrackedIce(Tile tile)
    {
        if (_stunTurnsLeft > 0) return;
        if (Random.Shared.Next(100) < 25)
        {
            _stunTurnsLeft = 1;
            _log.LogCombat("The cracked ice gives way — you slip and lose your footing!");
        }
        else if (Random.Shared.Next(4) == 0)
            _log.Log("The ice groans underfoot.");
    }
}
