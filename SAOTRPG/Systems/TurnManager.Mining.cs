using SAOTRPG.Inventory.Core;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Map;
using SAOTRPG.UI;

namespace SAOTRPG.Systems;

// Bundle 10 — mining bump-action handler. Diverted from TurnManager.Movement
// before the BlocksMovement gate when target tile is mineable + Pickaxe equipped.
public partial class TurnManager
{
    // Default strikes per tier (mirrors OreVeinPlacementPass.DefaultStrikes).
    // Used as fallback if VeinStrikesRemaining lacks an entry (legacy save / unseeded vein).
    private static int DefaultStrikesForTile(TileType t) => t switch
    {
        TileType.OreVeinIron    => 3,
        TileType.OreVeinMithril => 5,
        TileType.OreVeinDivine  => 8,
        _ => 0,
    };

    // Floor on strikes-remaining after MiningPower bonus per scout 2.1 table.
    private static int MinStrikesAfterPower(TileType t) => t switch
    {
        TileType.OreVeinIron    => 1,
        TileType.OreVeinMithril => 2,
        TileType.OreVeinDivine  => 3,
        _ => 1,
    };

    private static TileType DepletedFor(TileType t) => t switch
    {
        TileType.OreVeinIron    => TileType.OreVeinIronDepleted,
        TileType.OreVeinMithril => TileType.OreVeinMithrilDepleted,
        TileType.OreVeinDivine  => TileType.OreVeinDivineDepleted,
        _ => t,
    };

    public static bool IsMineable(TileType t) =>
        t is TileType.OreVeinIron or TileType.OreVeinMithril or TileType.OreVeinDivine;

    // Returns true when the move was diverted into a mining strike (caller must skip move).
    // Returns false to fall through to normal BlocksMovement handling.
    internal bool TryHandleMiningStrike(int tx, int ty)
    {
        var tile = _map.GetTile(tx, ty);
        if (!IsMineable(tile.Type)) return false;

        var pick = _player.Inventory.GetEquipped(EquipmentSlot.Tool) as Pickaxe;
        if (pick == null)
        {
            _log.Log("You'd need a pickaxe to mine that.");
            // Fall through to BlocksMovement — locked spec (scout 2.1).
            return false;
        }

        // Apply the strike: decrement remaining, degrade pickaxe, optional XP, drop on deplete.
        int remaining = DecrementVeinStrikes(tx, ty, tile.Type, pick.MiningPower);
        DegradePickaxe(pick);
        GrantMiningXp(tile.Type);

        string oreName = tile.Type switch
        {
            TileType.OreVeinIron => "iron vein",
            TileType.OreVeinMithril => "mithril vein",
            TileType.OreVeinDivine => "divine vein",
            _ => "vein",
        };
        _log.Log($"You strike the {oreName}!");
        DebugLogger.LogGame("MINING", $"strike vein={tile.Type} remaining={remaining} miningPower={pick.MiningPower}");

        // If pickaxe broke this strike, destroy it now (Q19 — broken pickaxe = DestroyEquipped).
        if (pick.ItemDurability <= 0)
        {
            _player.Inventory.DestroyEquipped(EquipmentSlot.Tool);
            _log.LogCombat("Your pickaxe shatters from the strain!");
        }

        if (remaining <= 0)
            DepleteVein(tx, ty, tile.Type);

        // Standard turn-consume tail (mirrors swing/move).
        AdvanceTurn();
        TickPoison(); TickBleed(); TickSlow();
        if (_player.IsDefeated) { TurnCompleted?.Invoke(); return true; }
        ProcessEntityTurns();
        PassiveRegen();
        UpdateVisibility();
        TurnCompleted?.Invoke();
        return true;
    }

    // Decrement strikes; MiningPower adds extra strike-equivalents but max single-hit drop is (default - floor).
    // Unseeded tiles pull the tier default lazily.
    private int DecrementVeinStrikes(int tx, int ty, TileType type, int miningPower)
    {
        var key = (tx, ty);
        if (!_map.VeinStrikesRemaining.TryGetValue(key, out int strikes))
            strikes = DefaultStrikesForTile(type);

        int floor = MinStrikesAfterPower(type);
        int reduction = 1 + Math.Max(0, miningPower);
        int next = Math.Max(0, strikes - reduction);
        int largestDrop = Math.Max(1, DefaultStrikesForTile(type) - floor);
        if (strikes - next > largestDrop) next = strikes - largestDrop;

        if (next <= 0) _map.VeinStrikesRemaining.Remove(key);
        else _map.VeinStrikesRemaining[key] = next;
        return next;
    }

    // Pickaxe-specific durability tick. Mirrors DegradeEquipment but Pickaxe is non-Divine.
    private void DegradePickaxe(Pickaxe pick)
    {
        if (pick.ItemDurability <= 0) return;
        pick.ItemDurability--;
        if (pick.ItemDurability == 5)
            _log.Log($"Your {pick.Name} is about to break! ({pick.ItemDurability} durability)");
    }

    // Convert vein → depleted variant; clears strike counter, drops ore via
    // MiningOreTables (LifeSkill + Pickaxe bonuses applied inside).
    private void DepleteVein(int tx, int ty, TileType from)
    {
        _map.SetTileType(tx, ty, DepletedFor(from));
        _map.VeinStrikesRemaining.Remove((tx, ty));

        var pick = _player.Inventory.GetEquipped(EquipmentSlot.Tool) as Pickaxe;
        var drops = MiningOreTables.RollDepletionDrops(from, pick, _player);
        foreach (var item in drops) _map.AddItem(tx, ty, item);

        if (drops.Count == 0)
            _log.LogLoot("The vein is exhausted but yields nothing.");
        else
            _log.LogLoot($"The vein crumbles, scattering {drops.Count} ore.");
        DebugLogger.LogGame("MINING", $"deplete vein={from} drops={drops.Count} dropList={string.Join(",", drops.Select(d => d.DefinitionId))}");
    }

    // Bundle 10 — Mining XP per strike (4/9/18 per Iron/Mithril/Divine, scout 2.2).
    private void GrantMiningXp(TileType veinType)
    {
        int xp = MiningOreTables.XpForStrike(veinType);
        if (xp > 0) _player.LifeSkills.GrantXp(LifeSkillType.Mining, xp);
    }
}
