using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Definitions;
using SAOTRPG.Items.Definitions.Weapons;
using SAOTRPG.UI.Helpers;
using Tile = SAOTRPG.Map.Tile;

namespace SAOTRPG.Systems;

public partial class TurnManager
{
    private void DropLoot(Monster monster)
    {
        int mx = monster.X, my = monster.Y;

        // Canon-named mob drops: check overrides BEFORE the generic tag roll.
        // Some mobs have multiple rolls (e.g. Frenzy Boar drops meat + hide + maybe tusk).
        string baseName = StripPrefixes(monster.Name);
        if (LootGenerator.NamedMobDrops.TryGetValue(baseName, out var namedDrops))
        {
            foreach (var (defId, chance) in namedDrops)
            {
                if (Random.Shared.NextDouble() >= chance) continue;
                var namedItem = ItemRegistry.Create(defId);
                if (namedItem != null) DropItem(mx, my, namedItem, monster.Name);
            }
        }

        if (monster is Mob mob && LootGenerator.MobLootTable.TryGetValue(mob.LootTag, out var lootTable)
            && Random.Shared.Next(100) < 40)
        {
            var entry = lootTable[Random.Shared.Next(lootTable.Length)];

            // Chain catalysts need DefinitionId set (for Anvil Evolve) — route
            // via ItemRegistry when the drop name matches.
            BaseItem? lootItem;
            if (LootGenerator.ChainMaterialByName.TryGetValue(entry.Name, out var chainDefId))
            {
                lootItem = ItemRegistry.Create(chainDefId);
            }
            else
            {
                lootItem = new Items.Materials.MobDrop
                {
                    Name = entry.Name, Value = entry.Value + CurrentFloor * 3,
                    SourceMonster = monster.Name, Quantity = 1, MaxStacks = 10,
                    CraftingTier = Math.Clamp(1 + CurrentFloor / 20, 1, 5),
                    MaterialType = "Monster Material",
                };
            }
            if (lootItem != null) DropItem(mx, my, lootItem, monster.Name);
        }

        int roll = Random.Shared.Next(100);
        if (roll < 30)
        {
            var potion = CurrentFloor >= 3 && Random.Shared.Next(3) == 0
                ? (Random.Shared.Next(2) == 0 ? PotionDefinitions.CreateSpeedPotion() : PotionDefinitions.CreateIronSkinPotion())
                : PotionDefinitions.CreateHealthPotion();
            DropItem(mx, my, potion, monster.Name);
        }
        else if (roll < 45)
        {
            var food = CurrentFloor >= 2 && Random.Shared.Next(3) == 0
                ? FoodDefinitions.CreateSpicedJerky() : FoodDefinitions.CreateBread();
            DropItem(mx, my, food, monster.Name);
        }

        if (monster is Mob { CanPoison: true } && Random.Shared.Next(100) < 40)
            DropItem(mx, my, PotionDefinitions.CreateAntidote(), monster.Name);

        if (Random.Shared.Next(100) < 10)
        {
            var gear = LootGenerator.CreateRandomEquipment(CurrentFloor);
            if (gear != null) DropItem(mx, my, gear, monster.Name);
        }

        // IM Enhancement Ores — themed mob drops, once per kill. Rare-boss
        // drops use RollBossOreDrops in TurnManager.Combat.
        if (monster is Mob mob2
            && LootGenerator.OreByLootTag.TryGetValue(mob2.LootTag, out var oreDefId)
            && Random.Shared.Next(100) < LootGenerator.OreDropChancePercent)
        {
            var ore = ItemRegistry.Create(oreDefId);
            if (ore != null) DropItem(mx, my, ore, monster.Name);
        }
    }

    // Field/floor boss ore drops — 20% chance of 1-2 ores; each rolled
    // independently so a boss can yield mixed types.
    private void RollBossOreDrops(int bx, int by, string sourceName)
    {
        if (Random.Shared.Next(100) >= LootGenerator.BossOreDropChancePercent) return;
        int count = 1 + Random.Shared.Next(2); // 1 or 2 ores
        for (int i = 0; i < count; i++)
        {
            var defId = LootGenerator.PickRandomOreDefId();
            var ore = ItemRegistry.Create(defId);
            if (ore != null) DropItem(bx, by, ore, sourceName);
        }
    }

    private void DropItem(int x, int y, BaseItem item, string source)
    {
        _map.AddItem(x, y, item);
        // Divine Objects get a bespoke sleek log line (colored BrightRed via
        // LogColorRules ◈ keyword match). Skips the usual rare-loot flavor.
        if (item.Rarity == "Divine")
        {
            _log.LogLoot($"  ◈ {source} drops {item.Name} — Divine Object.");
            return;
        }
        _log.LogLoot($"  {RarityHelper.LogTag(item.Rarity)}{source} dropped a {item.Name}!");
        if (item.Rarity is "Rare" or "Epic" or "Legendary")
        {
            string msg = string.Format(
                FlavorText.RareLootFlavors[Random.Shared.Next(FlavorText.RareLootFlavors.Length)],
                item.Rarity, item.Name);
            _log.LogLoot(msg);
        }
    }

    private void OpenChest(Tile tile, int chestX, int chestY)
    {
        int dist = Math.Max(Math.Abs(chestX - _map.Width / 2), Math.Abs(chestY - _map.Height / 2));
        string tier = dist > 40 ? "far" : dist >= 20 ? "mid" : "near";

        if (tier == "far") _log.LogLoot("You open a treasure chest — it gleams with a golden light!");
        else if (tier == "mid") _log.LogLoot("You open a treasure chest — a faint silver shimmer catches your eye.");
        else _log.LogLoot("You open a treasure chest!");

        int col = 20 + CurrentFloor * 15 + Random.Shared.Next(0, 20);
        if (tier == "far") col = col * 3 / 2;
        _player.ColOnHand += col;
        TotalColEarned += col;
        _log.LogLoot($"  Found {col} Col!");

        int rolls = tier == "far" ? 2 : 1 + Random.Shared.Next(0, 2);
        for (int r = 0; r < rolls; r++)
        {
            BaseItem item = RollChestItem(tier);
            _map.AddItem(chestX, chestY, item);
            if (item.Rarity == "Divine")
                _log.LogLoot($"  ◈ The chest holds {item.Name} — Divine Object.");
            else
                _log.LogLoot($"  {RarityHelper.LogTag(item.Rarity)}Found {item.Name}!");
            // Chest-peek compare line: only surfaces for equipment whose slot
            // has something equipped. Single-row summary so it reads in log.
            if (item is SAOTRPG.Items.Equipment.EquipmentBase)
            {
                string diff = UI.Helpers.GearCompare.BuildDiffForPlayer(_player, item);
                if (!string.IsNullOrEmpty(diff) && diff != "(new)" && diff != "(no change)")
                    _log.Log($"    vs. equipped: {diff}");
            }
        }
        _floorItemsFound += rolls;
    }

    private BaseItem RollChestItem(string tier)
    {
        int loot = Random.Shared.Next(100);

        if (tier == "far" && loot < 40)
            return RollEquipmentWithRegisteredPool()
                   ?? (BaseItem)PotionDefinitions.CreateGreaterHealthPotion();

        int equipThreshold = tier == "mid" ? 75 : 95;
        return loot switch
        {
            < 20 => PotionDefinitions.CreateHealthPotion(),
            < 30 => FoodDefinitions.CreateBread(),
            < 38 => PotionDefinitions.CreateAntidote(),
            < 48 => FoodDefinitions.CreateGrilledMeat(),
            < 56 => PotionDefinitions.CreateGreaterHealthPotion(),
            < 62 => PotionDefinitions.CreateBattleElixir(),
            < 68 => FoodDefinitions.CreateHoneyBread(),
            < 74 => FoodDefinitions.CreateSpicedJerky(),
            _ when loot < equipThreshold => RollChestConsumable(),
            _ => RollEquipmentWithRegisteredPool()
                 ?? (BaseItem)PotionDefinitions.CreateHealthPotion(),
        };
    }

    // 15% chance to pull from floor-banded registered pool; else procedural.
    private BaseItem? RollEquipmentWithRegisteredPool()
    {
        if (Random.Shared.Next(100) < 15)
        {
            var defId = LootGenerator.PickFloorBandedRegisteredDefId(CurrentFloor);
            if (defId != null)
            {
                var reg = ItemRegistry.Create(defId);
                if (reg != null) return reg;
            }
        }
        return LootGenerator.CreateRandomEquipment(CurrentFloor);
    }

    private BaseItem RollChestConsumable()
    {
        int sub = Random.Shared.Next(5);
        return sub switch
        {
            0 => CurrentFloor >= 3 ? FoodDefinitions.CreateFishStew() : FoodDefinitions.CreateGrilledMeat(),
            1 => CurrentFloor >= 3 ? PotionDefinitions.CreateSpeedPotion() : PotionDefinitions.CreateGreaterHealthPotion(),
            2 => CurrentFloor >= 3 ? PotionDefinitions.CreateIronSkinPotion() : PotionDefinitions.CreateGreaterHealthPotion(),
            3 => CurrentFloor >= 4 ? FoodDefinitions.CreateElvenWaybread() : FoodDefinitions.CreateGrilledMeat(),
            _ => PotionDefinitions.CreateGreaterHealthPotion(),
        };
    }

    // Strip Elite/Champion + affix prefixes to resolve the base canon mob name
    // for NamedMobDrops lookup. Affixes are space-separated before the variant.
    private static readonly string[] _knownPrefixes =
    {
        "Swift ", "Armored ", "Vampiric ", "Berserker ",
        "Crystallite ", "Regenerating ", "Pack Hunter ", "Ethereal ",
        "Cardinal-Marked ", "Immortal-Marked ",
        "Elite ", "Champion ",
    };
    private static string StripPrefixes(string name)
    {
        string s = name;
        bool stripped;
        do
        {
            stripped = false;
            foreach (var prefix in _knownPrefixes)
            {
                if (s.StartsWith(prefix))
                {
                    s = s[prefix.Length..];
                    stripped = true;
                    break;
                }
            }
        } while (stripped);
        return s;
    }

    public void PickupItems()
    {
        int px = _player.X, py = _player.Y;
        if (!_map.HasItemsAt(px, py))
        {
            _log.Log(FlavorText.EmptyGroundFlavors[Random.Shared.Next(FlavorText.EmptyGroundFlavors.Length)]);
            return;
        }

        var picked = new List<BaseItem>();
        bool emittedPickup = false;
        // Snapshot before pickup mutates the sparse list.
        foreach (var item in _map.GetItemsAt(px, py).ToList())
        {
            if (_player.Inventory.AddItem(item))
            {
                picked.Add(item);
                _floorItemsFound++;
                _map.RemoveItem(px, py, item);
                QuestSystem.OnItemPickup(item.Name ?? "", _log);
                if (!emittedPickup)
                {
                    ParticleQueue.Emit(ParticleEvent.ItemPickup, px, py);
                    emittedPickup = true;
                }
                if (item.Rarity == "Divine")
                {
                    _log.LogLoot($"  ◈ You receive {item.Name} — Divine Object.");
                }
                else
                {
                    string rarityTag = RarityHelper.LogTag(item.Rarity);
                    string pickMsg = string.Format(
                        FlavorText.PickupFlavors[Random.Shared.Next(FlavorText.PickupFlavors.Length)], item.Name);
                    _log.LogLoot($"{rarityTag}{pickMsg}");
                }
            }
            else
            {
                _log.Log(FlavorText.InventoryFullFlavors[Random.Shared.Next(FlavorText.InventoryFullFlavors.Length)]);
                break;
            }
        }
    }
}
