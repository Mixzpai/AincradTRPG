using SAOTRPG.Inventory.Core;
using SAOTRPG.Items;
using SAOTRPG.Items.Definitions;
using SAOTRPG.Items.Definitions.Weapons;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Systems;

// Centralized loot tables, rarity scaling, and equipment creation.
// All static data and pure-logic methods live here to keep TurnManager focused on orchestration.
public static class LootGenerator
{
    // ── Rarity roll thresholds (cumulative %) ──────────────────────────
    // Roll below this → Common (60% chance).
    private const int RarityCommonCeiling = 60;
    // Roll below this (but ≥ CommonCeiling) → Uncommon (25% chance).
    private const int RarityUncommonCeiling = 85;
    // Roll below this (but ≥ UncommonCeiling) → Rare (11% chance).
    private const int RarityRareCeiling = 96;
    // Remaining 4% → Epic

    // Number of equipment type slots in the random equipment roll.
    // 0-9 = weapon types, 10-13 = armor/shield/accessory, 14 = null fallback.
    private const int EquipmentTypeCount = 15;

    // ── Mob loot tables — themed drops by LootTag ─────────────────────
    // Themed drop tables keyed by mob LootTag. Each entry maps to an array
    // of (item name, Col value) tuples. Add new tags/items freely.
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
        { "aquatic",   new[] { ("Water Core",     15), ("Fish Scale",      8), ("Murky Pearl",   18) } },
    };

    // Canon-named mob → (DefinitionId, dropChance 0-1) overrides. Checked
    // BEFORE the generic LootTag roll so named mobs drop their iconic items
    // at the rate canon/game sources indicate. Multiple entries per mob = multi-roll.
    public static readonly Dictionary<string, (string DefId, float Chance)[]> NamedMobDrops = new()
    {
        ["Frenzy Boar"]            = new[] { ("boar_meat", 0.7f), ("boar_hide", 0.5f), ("boar_tusk", 0.2f) },
        ["Little Nepent"]          = new[] { ("toxic_spore", 0.4f) },
        ["Sharp-Hook Nepent"]      = new[] { ("toxic_spore", 0.4f) },
        ["Three-Pronged Nepent"]   = new[] { ("nepent_ovule", 0.15f), ("toxic_spore", 0.4f) },
        ["Dire Wolf"]              = new[] { ("wolf_meat", 0.5f), ("wolf_pelt_canon", 0.4f), ("wolf_fang", 0.25f) },
        ["Ruin Kobold Trooper"]    = new[] { ("kobold_fang", 0.4f), ("rusty_blade", 0.25f) },
        ["Ruin Kobold Sentinel"]   = new[] { ("kobold_halberd", 0.2f), ("kobold_fang", 0.3f) },
        ["Windwasp"]               = new[] { ("wasp_stinger", 0.3f) },
        ["Lesser Taurus"]          = new[] { ("taurus_horn", 0.3f), ("ox_hide", 0.5f) },
        ["Trembling Ox"]           = new[] { ("ox_hide", 0.6f), ("raw_meat", 0.5f) },
        ["Heavy Hammer Taurus"]    = new[] { ("taurus_horn", 0.4f) },
        ["Treant Sapling"]         = new[] { ("treant_sap", 0.4f) },
        ["Elder Treant"]           = new[] { ("treant_heartwood", 0.25f), ("treant_sap", 0.4f) },
        ["Forest Elf Scout"]       = new[] { ("forest_elf_bow", 0.1f) },
        ["Cave Bat"]               = new[] { ("bat_wing", 0.7f) },
        ["Toadstool Walker"]       = new[] { ("toxic_spore", 0.4f) },
        ["Water Drake"]            = new[] { ("drake_scale", 0.4f), ("water_core", 0.15f) },
        ["Lakeshore Crab"]         = new[] { ("crab_claw", 0.5f) },
        ["Giant Clam"]             = new[] { ("pearl", 0.08f) },
        ["Water Wight"]            = new[] { ("ectoplasm", 0.4f) },
        ["Vacant Sentinel"]        = new[] { ("obsidian_shard", 0.3f) },
        ["Alpine Wolf"]            = new[] { ("alpine_pelt", 0.5f) },
        ["Ruin Kobold Miner"]      = new[] { ("iron_ore", 0.5f), ("mithril_trace", 0.05f) },
        ["Giant Spider"]           = new[] { ("spider_silk", 0.4f), ("venom_gland", 0.3f) },
        ["Snow Wolf"]              = new[] { ("frost_shard", 0.4f), ("alpine_pelt", 0.4f) },
        ["Frost Goblin"]           = new[] { ("frost_shard", 0.4f) },
        ["Sandstorm Scorpion"]     = new[] { ("scorpion_tail", 0.4f) },
        ["Ancient Dragon"]         = new[] { ("ancient_scale", 0.3f), ("dragon_heart", 0.1f) },
        ["Flame Elemental"]        = new[] { ("flame_core", 0.3f) },
        ["Hollow Mutated Wolf"]    = new[] { ("hollow_essence", 0.4f) },
        ["Unholy Dragon"]          = new[] { ("corrupted_scale", 0.2f), ("dragon_heart", 0.1f) },
        ["Void Seraph"]            = new[] { ("seraph_feather", 0.15f) },
        ["Cardinal Error"]         = new[] { ("cardinal_shard", 0.2f) },
        ["Immortal Echo"]          = new[] { ("immortal_fragment", 0.1f) },
    };

    // ── Rarity stat multipliers ───────────────────────────────────────
    // Per-rarity scaling: (StatMultiplier%, DurabilityBonus, ValueMultiplier%).
    // Indexed by RarityIndex (0=Common, 1=Uncommon, 2=Rare, 3=Epic).
    private static readonly (int StatMul, int DurBonus, int ValMul)[] RarityScaling =
    {
        (100, 0,  100),  // Common
        (125, 10, 150),  // Uncommon — +25% stats, +10 durability
        (160, 25, 250),  // Rare     — +60% stats, +25 durability
        (200, 50, 400),  // Epic     — 2x stats, +50 durability
    };

    // Map a rarity string to its RarityScaling index.
    private static int RarityIndex(string rarity) => rarity switch
    {
        "Uncommon" => 1, "Rare" => 2, "Epic" => 3, _ => 0
    };

    // ── Accessory pool for random drops ────────────────────────────────
    private static readonly Func<Accessory>[] AccessoryPool =
    {
        AccessoryDefinitions.CreateRingOfStrength,
        AccessoryDefinitions.CreateAgilityNecklace,
        AccessoryDefinitions.CreateGuardianRing,
        AccessoryDefinitions.CreateScholarsPendant,
        AccessoryDefinitions.CreateSwiftBand,
        AccessoryDefinitions.CreateVitalityCharm,
    };

    // ── Equipment name tables ─────────────────────────────────────────
    private static readonly string[] MetalPrefixes = { "Rusty", "Iron", "Steel", "Sharp", "Gleaming", "Worn", "Fine", "Tempered", "Darksteel", "Mythril" };
    private static readonly string[] ArmorPrefixes = { "Leather", "Iron", "Studded", "Chainmail", "Plated", "Worn", "Mythril", "Hardened" };
    private static readonly string[] ShieldNouns = { "Shield", "Buckler", "Kite Shield", "Tower Shield" };

    private static readonly Dictionary<string, string[]> WeaponNouns = new()
    {
        { "One-Handed Sword", new[] { "Sword", "Blade", "Saber", "Longsword", "Falchion" } },
        { "Two-Handed Sword", new[] { "Greatsword", "Claymore", "Zweihander", "Flamberge" } },
        { "Dagger",           new[] { "Dagger", "Knife", "Stiletto", "Shiv", "Kris" } },
        { "Rapier",           new[] { "Rapier", "Estoc", "Epee", "Foil", "Sabre" } },
        { "Katana",           new[] { "Katana", "Tachi", "Wakizashi", "Nodachi" } },
        { "Axe",              new[] { "Axe", "Hatchet", "Cleaver", "Battleaxe", "Tomahawk" } },
        { "Mace",             new[] { "Mace", "Hammer", "Club", "Flail", "Morningstar" } },
        { "Spear",            new[] { "Spear", "Lance", "Pike", "Halberd", "Glaive" } },
        { "Bow",              new[] { "Bow", "Longbow", "Shortbow", "Recurve", "Composite Bow" } },
        { "Staff",            new[] { "Staff", "Wand", "Rod", "Scepter" } },
    };

    private static string PickName(string weaponType) =>
        $"{MetalPrefixes[Random.Shared.Next(MetalPrefixes.Length)]} " +
        (WeaponNouns.TryGetValue(weaponType, out var nouns)
            ? nouns[Random.Shared.Next(nouns.Length)]
            : "Weapon");

    private static string PickArmorName(string slot) =>
        $"{ArmorPrefixes[Random.Shared.Next(ArmorPrefixes.Length)]} {slot}";

    private static string PickShieldName() =>
        $"{ArmorPrefixes[Random.Shared.Next(ArmorPrefixes.Length)]} {ShieldNouns[Random.Shared.Next(ShieldNouns.Length)]}";

    // Roll a random rarity tier: Common 60%, Uncommon 25%, Rare 11%, Epic 4%.
    public static string PickRarity()
    {
        int r = Random.Shared.Next(100);
        return r < RarityCommonCeiling ? "Common"
             : r < RarityUncommonCeiling ? "Uncommon"
             : r < RarityRareCeiling ? "Rare"
             : "Epic";
    }

    // Creates a random equipment piece scaled to the given floor number.
    // All 10 weapon types can drop. Returns null on the fallback roll.
    public static BaseItem? CreateRandomEquipment(int currentFloor)
    {
        string rarity = PickRarity();
        var scale = RarityScaling[RarityIndex(rarity)];
        int dur = 30 + currentFloor * 10 + scale.DurBonus;
        int lvl = Math.Max(1, currentFloor - 1);
        int roll = Random.Shared.Next(EquipmentTypeCount);

        return roll switch
        {
            0  => MakeWeapon("One-Handed Sword", currentFloor, rarity, scale, 1.0, 1, 1, StatType.Attack),
            1  => MakeWeapon("Dagger",           currentFloor, rarity, scale, 0.8, 0, 1, StatType.Agility),
            2  => MakeWeapon("Two-Handed Sword",  currentFloor, rarity, scale, 1.3, 3, 1, StatType.Strength),
            3  => MakeWeapon("Axe",              currentFloor, rarity, scale, 1.2, 2, 1, StatType.Strength),
            4  => MakeWeapon("Katana",           currentFloor, rarity, scale, 1.0, 1, 1, StatType.Agility),
            5  => MakeWeapon("Rapier",           currentFloor, rarity, scale, 0.85, 0, 1, StatType.Speed),
            6  => MakeWeapon("Mace",             currentFloor, rarity, scale, 1.1, 2, 1, StatType.Vitality),
            7  => MakeWeapon("Spear",            currentFloor, rarity, scale, 0.95, 1, 2, StatType.Dexterity),
            8  => MakeWeapon("Bow",              currentFloor, rarity, scale, 0.85, 1, 3, StatType.Dexterity),
            9  => MakeWeapon("Staff",            currentFloor, rarity, scale, 0.5, 1, 1, StatType.Intelligence),
            10 => MakeArmor("Chest", "Chestplate", currentFloor, rarity, scale, 1.0, 6),
            11 => MakeArmor("Helmet", "Helmet",    currentFloor, rarity, scale, 0.8, 3),
            12 => MakeArmor("Boots", "Boots",      currentFloor, rarity, scale, 0.7, 2),
            13 => new Armor
            {
                Name = PickShieldName(), Value = (40 + currentFloor * 22) * scale.ValMul / 100,
                Rarity = rarity, ItemDurability = dur,
                RequiredLevel = lvl, EquipmentType = "Armor",
                ArmorSlot = "Shield", BaseDefense = ScaleDef(currentFloor, scale, 0.8), Weight = 4,
                BlockChance = 10 + currentFloor + Random.Shared.Next(0, 6),
                Bonuses = new StatModifierCollection().Add(StatType.Defense, ScaleDef(currentFloor, scale, 0.8))
            },
            _ => CreateRandomAccessory(currentFloor, rarity, scale, scale.DurBonus),
        };
    }

    // Procedural weapon factory — creates any weapon type with floor-scaled stats.
    private static Weapon MakeWeapon(string weaponType, int floor, string rarity,
        (int StatMul, int DurBonus, int ValMul) scale, double dmgFactor, int atkSpeed, int range,
        StatType secondaryStat)
    {
        int baseDmg = (int)((3 + floor * 1.5 + Random.Shared.Next(0, Math.Max(1, floor / 3))) * dmgFactor) * scale.StatMul / 100;
        int atkBonus = (int)((2 + floor * 1.2 + Random.Shared.Next(0, 3)) * dmgFactor) * scale.StatMul / 100;
        int secBonus = Math.Max(1, (1 + floor / 4) * scale.StatMul / 100);
        return new Weapon
        {
            Name = PickName(weaponType),
            Value = (int)((40 + floor * 25) * dmgFactor) * scale.ValMul / 100,
            Rarity = rarity,
            ItemDurability = 25 + floor * 8 + scale.DurBonus,
            RequiredLevel = Math.Max(1, floor - 1),
            EquipmentType = "Weapon",
            WeaponType = weaponType,
            BaseDamage = Math.Max(1, baseDmg),
            AttackSpeed = atkSpeed,
            Range = range,
            Bonuses = new StatModifierCollection()
                .Add(StatType.Attack, Math.Max(1, atkBonus))
                .Add(secondaryStat, secBonus)
        };
    }

    private static Armor MakeArmor(string slot, string displaySlot, int floor, string rarity,
        (int StatMul, int DurBonus, int ValMul) scale, double defFactor, int weight)
    {
        int def = ScaleDef(floor, scale, defFactor);
        return new Armor
        {
            Name = PickArmorName(displaySlot),
            Value = (int)((45 + floor * 20) * defFactor) * scale.ValMul / 100,
            Rarity = rarity,
            ItemDurability = 30 + floor * 10 + scale.DurBonus,
            RequiredLevel = Math.Max(1, floor - 1),
            EquipmentType = "Armor",
            ArmorSlot = slot, BaseDefense = def, Weight = weight,
            Bonuses = new StatModifierCollection().Add(StatType.Defense, def)
        };
    }

    private static int ScaleDef(int floor, (int StatMul, int DurBonus, int ValMul) scale, double factor) =>
        Math.Max(1, (int)((2 + floor * 1.5 + Random.Shared.Next(0, 3)) * factor) * scale.StatMul / 100);

    // Pick a random accessory from the pool and scale its value/durability to the floor.
    private static Accessory CreateRandomAccessory(int currentFloor, string rarity,
        (int StatMul, int DurBonus, int ValMul) scale, int durBonus)
    {
        var acc = AccessoryPool[Random.Shared.Next(AccessoryPool.Length)]();
        acc.Rarity = rarity;
        acc.Value = acc.Value * scale.ValMul / 100;
        acc.ItemDurability = 50 + currentFloor * 8 + durBonus;
        acc.RequiredLevel = Math.Max(1, currentFloor - 1);
        return acc;
    }
}
