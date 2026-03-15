using SAOTRPG.Inventory.Core;
using SAOTRPG.Items;
using SAOTRPG.Items.Definitions;
using SAOTRPG.Items.Definitions.Weapons;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Systems;

/// <summary>
/// Centralized loot tables, rarity scaling, and equipment creation.
/// All static data and pure-logic methods live here to keep TurnManager focused on orchestration.
/// </summary>
public static class LootGenerator
{
    // ── Mob loot tables — themed drops by LootTag ─────────────────────
    // Each tag maps to an array of (item name, Col value). Add new tags/items freely.
    public static readonly Dictionary<string, (string Name, int Value)[]> MobLootTable = new()
    {
        { "beast",     new[] { ("Raw Hide",        8), ("Beast Fang",     12), ("Sinew",         6) } },
        { "kobold",    new[] { ("Kobold Ear",      5), ("Crude Dagger",   15), ("Tattered Cloth", 4) } },
        { "insect",    new[] { ("Chitin Shard",   10), ("Wing Fragment",   8), ("Venom Sac",     14) } },
        { "plant",     new[] { ("Herb Bundle",    12), ("Toxic Spore",    10), ("Root Fiber",     6) } },
        { "humanoid",  new[] { ("Coin Pouch",     20), ("Iron Ring",      15), ("Worn Map",      10) } },
        { "reptile",   new[] { ("Scale Plate",    12), ("Forked Tongue",   8), ("Reptile Eye",   14) } },
        { "undead",    new[] { ("Bone Fragment",    6), ("Soul Dust",      18), ("Cursed Shard",  14) } },
        { "construct", new[] { ("Gear Fragment",   10), ("Crystal Core",   20), ("Iron Bolt",      8) } },
        { "dragon",    new[] { ("Dragon Scale",   30), ("Flame Essence",  25), ("Dragon Claw",   20) } },
        { "elemental", new[] { ("Fire Crystal",   22), ("Essence Wisp",   18), ("Elemental Ash",  12) } },
    };

    // ── Rarity stat multipliers ───────────────────────────────────────
    // (StatMultiplier%, DurabilityBonus, ValueMultiplier%)
    private static readonly (int StatMul, int DurBonus, int ValMul)[] RarityScaling =
    {
        (100, 0,  100),  // Common
        (125, 10, 150),  // Uncommon — +25% stats, +10 durability
        (160, 25, 250),  // Rare     — +60% stats, +25 durability
        (200, 50, 400),  // Epic     — 2x stats, +50 durability
    };

    private static int RarityIndex(string rarity) => rarity switch
    {
        "Uncommon" => 1, "Rare" => 2, "Epic" => 3, _ => 0
    };

    // ── Equipment name tables ─────────────────────────────────────────
    private static readonly string[] WeaponPrefixes = { "Rusty", "Iron", "Steel", "Sharp", "Gleaming", "Worn", "Fine" };
    private static readonly string[] WeaponNouns = { "Sword", "Blade", "Saber", "Longsword" };
    private static readonly string[] DaggerNouns = { "Dagger", "Knife", "Stiletto", "Shiv" };
    private static readonly string[] ShieldNouns = { "Shield", "Buckler", "Kite Shield", "Tower Shield" };
    private static readonly string[] ArmorPrefixes = { "Leather", "Iron", "Studded", "Chainmail", "Plated", "Worn" };

    private static string PickWeaponName() =>
        $"{WeaponPrefixes[Random.Shared.Next(WeaponPrefixes.Length)]} {WeaponNouns[Random.Shared.Next(WeaponNouns.Length)]}";

    private static string PickDaggerName() =>
        $"{WeaponPrefixes[Random.Shared.Next(WeaponPrefixes.Length)]} {DaggerNouns[Random.Shared.Next(DaggerNouns.Length)]}";

    private static string PickArmorName(string slot) =>
        $"{ArmorPrefixes[Random.Shared.Next(ArmorPrefixes.Length)]} {slot}";

    private static string PickShieldName() =>
        $"{ArmorPrefixes[Random.Shared.Next(ArmorPrefixes.Length)]} {ShieldNouns[Random.Shared.Next(ShieldNouns.Length)]}";

    public static string PickRarity()
    {
        int r = Random.Shared.Next(100);
        return r < 60 ? "Common" : r < 85 ? "Uncommon" : r < 96 ? "Rare" : "Epic";
    }

    /// <summary>
    /// Creates a random equipment piece scaled to the given floor number.
    /// Returns null on the fallback roll (1-in-7 chance).
    /// </summary>
    public static BaseItem? CreateRandomEquipment(int currentFloor)
    {
        string rarity = PickRarity();
        var scale = RarityScaling[RarityIndex(rarity)];
        int atkBonus = (3 + currentFloor * 3 + Random.Shared.Next(0, 4)) * scale.StatMul / 100;
        int defBonus = (2 + currentFloor * 2 + Random.Shared.Next(0, 3)) * scale.StatMul / 100;
        int durBonus = scale.DurBonus;
        int roll = Random.Shared.Next(7);

        return roll switch
        {
            0 => new Weapon
            {
                Name = PickWeaponName(), Value = (50 + currentFloor * 30) * scale.ValMul / 100,
                Rarity = rarity, ItemDurability = 30 + currentFloor * 10 + durBonus,
                RequiredLevel = Math.Max(1, currentFloor - 1), EquipmentType = "Weapon",
                WeaponType = "One-Handed Sword", BaseDamage = atkBonus, AttackSpeed = 1, Range = 1,
                Bonuses = new StatModifierCollection().Add(StatType.Attack, atkBonus)
            },
            1 => new Weapon
            {
                Name = PickDaggerName(), Value = (40 + currentFloor * 25) * scale.ValMul / 100,
                Rarity = rarity, ItemDurability = 25 + currentFloor * 8 + durBonus,
                RequiredLevel = Math.Max(1, currentFloor - 1), EquipmentType = "Weapon",
                WeaponType = "Dagger", BaseDamage = Math.Max(1, atkBonus - 2), AttackSpeed = 2, Range = 1,
                Bonuses = new StatModifierCollection().Add(StatType.Attack, Math.Max(1, atkBonus - 2)).Add(StatType.Agility, 2)
            },
            2 => new Armor
            {
                Name = PickArmorName("Chestplate"), Value = (60 + currentFloor * 25) * scale.ValMul / 100,
                Rarity = rarity, ItemDurability = 40 + currentFloor * 10 + durBonus,
                RequiredLevel = Math.Max(1, currentFloor - 1), EquipmentType = "Armor",
                ArmorSlot = "Chest", BaseDefense = defBonus, Weight = 6,
                Bonuses = new StatModifierCollection().Add(StatType.Defense, defBonus)
            },
            3 => new Armor
            {
                Name = PickArmorName("Helmet"), Value = (45 + currentFloor * 20) * scale.ValMul / 100,
                Rarity = rarity, ItemDurability = 35 + currentFloor * 8 + durBonus,
                RequiredLevel = Math.Max(1, currentFloor - 1), EquipmentType = "Armor",
                ArmorSlot = "Helmet", BaseDefense = Math.Max(1, defBonus - 1), Weight = 3,
                Bonuses = new StatModifierCollection().Add(StatType.Defense, Math.Max(1, defBonus - 1))
            },
            4 => new Armor
            {
                Name = PickArmorName("Boots"), Value = (35 + currentFloor * 18) * scale.ValMul / 100,
                Rarity = rarity, ItemDurability = 30 + currentFloor * 8 + durBonus,
                RequiredLevel = Math.Max(1, currentFloor - 1), EquipmentType = "Armor",
                ArmorSlot = "Boots", BaseDefense = Math.Max(1, defBonus - 2), Weight = 2,
                Bonuses = new StatModifierCollection().Add(StatType.Defense, Math.Max(1, defBonus - 2)).Add(StatType.Agility, 1)
            },
            5 => new Armor
            {
                Name = PickShieldName(), Value = (40 + currentFloor * 22) * scale.ValMul / 100,
                Rarity = rarity, ItemDurability = 35 + currentFloor * 10 + durBonus,
                RequiredLevel = Math.Max(1, currentFloor - 1), EquipmentType = "Armor",
                ArmorSlot = "Shield", BaseDefense = Math.Max(1, defBonus - 1), Weight = 4,
                BlockChance = 10 + currentFloor + Random.Shared.Next(0, 6),
                Bonuses = new StatModifierCollection().Add(StatType.Defense, Math.Max(1, defBonus - 1))
            },
            _ => null
        };
    }
}
