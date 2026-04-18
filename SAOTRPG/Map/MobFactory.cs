using Terminal.Gui;
using SAOTRPG.Entities;

namespace SAOTRPG.Map;

// Creates floor-appropriate mobs with difficulty scaling and variant rolls.
// Mob templates are organized by floor tier (0-indexed).
// Variant system: 10% Elite (1.5× stats), 3% Champion (2× stats).
public static class MobFactory
{
    // Per-mob template record. LootTag maps to themed drops in TurnManager.GetMobLoot().
    // Set Poison/Bleed to true for status-inflicting mobs.
    private record MobTemplate(string Name, char Symbol, Color Color, int Aggro,
        bool Poison = false, bool Bleed = false, bool Stun = false, bool Slow = false,
        string LootTag = "generic", int Range = 1, string? Ability = null);

    // Floor-tiered mob template table. Index mapped via FloorToTier() so lower
    // floors (F1-F5) each get a dedicated canon-named roster, and later floors
    // share broader era-pools. All names sourced from SAO canon (Progressive,
    // anime, Integral Factor, Hollow Fragment) where possible.
    private static readonly MobTemplate[][] FloorMobs =
    {
        // Tier 0 — Floor 1: Town of Beginnings wilderness (Progressive Vol 1)
        new[]
        {
            new MobTemplate("Frenzy Boar",            'b', Color.BrightRed,     6, LootTag: "beast", Ability: "Charge"),
            new MobTemplate("Little Nepent",          'p', Color.BrightGreen,   3, Poison: true, LootTag: "plant"),
            new MobTemplate("Sharp-Hook Nepent",      'p', Color.Green,         4, Bleed: true, LootTag: "plant"),
            new MobTemplate("Three-Pronged Nepent",   'P', Color.BrightGreen,   5, Poison: true, LootTag: "plant"),
            new MobTemplate("Dire Wolf",              'w', Color.Gray,          7, LootTag: "beast", Ability: "Leap"),
            new MobTemplate("Ruin Kobold Trooper",    'k', Color.BrightRed,     5, LootTag: "kobold"),
            new MobTemplate("Ruin Kobold Sentinel",   's', Color.BrightCyan,    4, LootTag: "kobold"),
            new MobTemplate("Windwasp",               'i', Color.BrightYellow,  8, LootTag: "insect", Ability: "Leap"),
        },
        // Tier 1 — Floor 2: Urbus foothills (Progressive Vol 2)
        new[]
        {
            new MobTemplate("Lesser Taurus",          't', Color.BrightYellow,  6, LootTag: "beast", Ability: "Charge"),
            new MobTemplate("Trembling Ox",           'o', Color.Yellow,        3, Stun: true, LootTag: "beast", Ability: "Charge"),
            new MobTemplate("Heavy Hammer Taurus",    'T', Color.BrightRed,     5, Stun: true, LootTag: "beast"),
            new MobTemplate("Bullbous Bow",           'B', Color.Red,           7, LootTag: "beast", Range: 2),
            new MobTemplate("Plumed Mist Lizard",     'l', Color.Green,         6, Slow: true, LootTag: "reptile"),
        },
        // Tier 2 — Floor 3: Zumfut / Elf War (Progressive Vol 3)
        new[]
        {
            new MobTemplate("Treant Sapling",         'p', Color.Green,         4, LootTag: "plant"),
            new MobTemplate("Elder Treant",           'P', Color.BrightGreen,   5, Slow: true, LootTag: "plant"),
            new MobTemplate("Forest Elf Scout",       'e', Color.BrightGreen,   9, LootTag: "humanoid", Range: 2, Ability: "Leap"),
            new MobTemplate("Kobold Trapper",         'k', Color.Red,           7, Slow: true, LootTag: "kobold"),
            new MobTemplate("Cave Bat",               'v', Color.DarkGray,      8, LootTag: "insect", Ability: "Leap"),
            new MobTemplate("Toadstool Walker",       'f', Color.BrightMagenta, 4, Poison: true, LootTag: "plant"),
        },
        // Tier 3 — Floor 4: Rovia flooded (Progressive Vol 4)
        new[]
        {
            new MobTemplate("Water Drake",            'd', Color.BrightBlue,    6, LootTag: "dragon", Range: 3),
            new MobTemplate("Lakeshore Crab",         'c', Color.Cyan,          4, LootTag: "aquatic"),
            new MobTemplate("Giant Clam",             'C', Color.Blue,          2, LootTag: "aquatic"),
            new MobTemplate("Water Wight",            'u', Color.BrightCyan,    7, Slow: true, LootTag: "undead"),
            new MobTemplate("Scavenger Toad",         'f', Color.Green,         4, Poison: true, LootTag: "beast"),
        },
        // Tier 4 — Floor 5: Karluin / Pitch-Black Cathedral (Progressive Vol 5)
        new[]
        {
            new MobTemplate("Cathedral Bat",          'v', Color.DarkGray,      8, LootTag: "insect", Ability: "Leap"),
            new MobTemplate("Cursed Ghoul",           'u', Color.BrightMagenta, 5, Stun: true, LootTag: "undead"),
            new MobTemplate("Shadow Stalker",         'S', Color.DarkGray,     10, LootTag: "undead", Ability: "Leap"),
            new MobTemplate("Vacant Sentinel",        'G', Color.Gray,          3, Stun: true, LootTag: "construct", Ability: "Charge"),
            new MobTemplate("Skeleton Warrior",       'z', Color.White,         6, LootTag: "undead"),
        },
        // Tier 5 — Floors 6-10: Stone era (mines, sky, coast, puzzle)
        new[]
        {
            new MobTemplate("Ruin Kobold Miner",      'k', Color.Red,           5, LootTag: "kobold"),
            new MobTemplate("Rock Worm",              'r', Color.BrightYellow,  3, LootTag: "beast"),
            new MobTemplate("Cave Crawler",           'x', Color.DarkGray,      6, Poison: true, LootTag: "insect"),
            new MobTemplate("Wind Serpent",           's', Color.BrightCyan,    7, LootTag: "dragon", Range: 2),
            new MobTemplate("Cloud Wyvern",           'D', Color.BrightBlue,    8, LootTag: "dragon", Ability: "Leap"),
            new MobTemplate("Sky Dogma Thief",        'h', Color.BrightMagenta, 9, Bleed: true, LootTag: "humanoid", Ability: "Leap"),
            new MobTemplate("Cursed Stone Golem",     'G', Color.Gray,          3, Stun: true, LootTag: "construct"),
            new MobTemplate("Puzzle Wraith",          'w', Color.BrightMagenta, 8, LootTag: "undead"),
        },
        // Tier 6 — Floors 11-25: mid-tier frontier (mountain, forest, trap)
        new[]
        {
            new MobTemplate("Alpine Wolf",            'w', Color.BrightCyan,    7, LootTag: "beast", Ability: "Leap"),
            new MobTemplate("Bandit Raider",          'r', Color.Red,           8, Bleed: true, LootTag: "humanoid"),
            new MobTemplate("Granite Elemental",      'G', Color.Gray,          4, Stun: true, LootTag: "construct", Ability: "Charge"),
            new MobTemplate("Dark Dwarf Miner",       'd', Color.Red,           5, LootTag: "humanoid"),
            new MobTemplate("Forest Wolf Alpha",      'W', Color.BrightYellow,  8, LootTag: "beast", Ability: "Leap"),
            new MobTemplate("Giant Spider",           'x', Color.BrightMagenta, 6, Poison: true, LootTag: "insect", Ability: "Leap"),
            new MobTemplate("Gemini Dragon Hatchling",'D', Color.BrightGreen,   5, LootTag: "dragon"),
        },
        // Tier 7 — Floors 26-50: industrial / martial (KoB frontier, forge, desert, ice)
        new[]
        {
            new MobTemplate("Titan's Hand PKer",      'p', Color.BrightRed,    10, Bleed: true, LootTag: "humanoid", Range: 2),
            new MobTemplate("Snow Wolf",              'w', Color.White,         8, LootTag: "beast", Ability: "Leap"),
            new MobTemplate("Frost Goblin",           'g', Color.BrightCyan,    6, Slow: true, LootTag: "humanoid"),
            new MobTemplate("Drake Rider",            'D', Color.BrightYellow,  8, LootTag: "dragon", Ability: "Charge"),
            new MobTemplate("Iron Warden",            'W', Color.Gray,          4, LootTag: "construct"),
            new MobTemplate("Sandstorm Scorpion",     's', Color.Yellow,        6, Poison: true, LootTag: "insect"),
            new MobTemplate("Crimson Longsword (PK)", 'p', Color.Red,           9, Bleed: true, LootTag: "humanoid"),
        },
        // Tier 8 — Floors 51-75: endgame frontier (KoB HQ era, Gleam Eyes approach)
        new[]
        {
            new MobTemplate("Fatal Scythe Echo",      'R', Color.BrightMagenta, 10, Stun: true, LootTag: "undead"),
            new MobTemplate("Laughing Coffin PKer",   'L', Color.Red,          12, Bleed: true, LootTag: "humanoid", Range: 2),
            new MobTemplate("Flame Elemental",        'F', Color.BrightRed,     7, LootTag: "elemental", Range: 3),
            new MobTemplate("Ancient Dragon",         'D', Color.BrightRed,     8, Bleed: true, LootTag: "dragon", Ability: "Charge"),
            new MobTemplate("Bone Archer",            'z', Color.White,         8, LootTag: "undead", Range: 3),
            new MobTemplate("Shadow Wraith",          'S', Color.DarkGray,     10, Slow: true, LootTag: "undead", Ability: "Leap"),
            new MobTemplate("Fallen Paladin",         'u', Color.BrightYellow,  8, Stun: true, LootTag: "humanoid"),
        },
        // Tier 9 — Floors 76-100: Hollow Fragment + divine ascension
        new[]
        {
            new MobTemplate("Hollow Mutated Wolf",    'w', Color.BrightMagenta, 10, Bleed: true, LootTag: "hollow", Ability: "Leap"),
            new MobTemplate("Death Gaze",             'E', Color.DarkGray,     12, Stun: true, LootTag: "undead", Range: 4),
            new MobTemplate("Crawling Pain",          'x', Color.BrightMagenta, 8, Poison: true, LootTag: "insect"),
            new MobTemplate("Infected Griffin",       'g', Color.Green,         9, Poison: true, LootTag: "beast", Ability: "Leap"),
            new MobTemplate("Unholy Dragon",          'D', Color.BrightRed,    10, Bleed: true, LootTag: "dragon", Range: 3),
            new MobTemplate("Void Seraph",            'A', Color.White,        12, Slow: true, LootTag: "hollow", Range: 3),
            new MobTemplate("Cardinal Error",         'C', Color.BrightMagenta, 8, LootTag: "hollow"),
            new MobTemplate("Immortal Echo",          '@', Color.BrightRed,    10, Bleed: true, LootTag: "hollow"),
        },
    };

    // Map a floor number to a tier index. F1-F5 each get a dedicated canon
    // roster; F6-10, F11-25, F26-50, F51-75, F76-100 share broader era pools.
    private static int FloorToTier(int floor)
    {
        if (floor <= 5)  return floor - 1;
        if (floor <= 10) return 5;
        if (floor <= 25) return 6;
        if (floor <= 50) return 7;
        if (floor <= 75) return 8;
        return 9;
    }

    // Get base mob names for a given floor tier (0-indexed). Used by bounty system.
    public static string[] GetFloorMobNames(int tier)
    {
        tier = Math.Clamp(tier, 0, FloorMobs.Length - 1);
        return FloorMobs[tier].Select(t => t.Name).ToArray();
    }

    // FB-058 Title System support: resolve a species Name (as recorded by
    // Bestiary, which may carry Elite/Champion/Affix prefixes stripped by
    // callers) back to its template LootTag. Walks every tier so species
    // shared across tiers resolve against the first-seen entry. Returns
    // null for unrecognized names so callers can default to "generic".
    public static string? GetLootTagForName(string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        // Try exact match first.
        foreach (var tier in FloorMobs)
            foreach (var t in tier)
                if (t.Name == name) return t.LootTag;
        // Fall back to suffix match so "Elite Frenzy Boar" still resolves
        // if the caller forgot to strip the prefix.
        foreach (var tier in FloorMobs)
            foreach (var t in tier)
                if (name.EndsWith(t.Name)) return t.LootTag;
        return null;
    }

    // Creates a floor-appropriate mob with difficulty scaling.
    // Picks a random template from the floor's tier, applies stat scaling,
    // then rolls for Elite/Champion variant.
    public static Mob CreateFloorMob(int floor, int statScale = 100)
    {
        int tier = FloorToTier(Math.Max(1, floor));
        var templates = FloorMobs[tier];
        var template = templates[Random.Shared.Next(templates.Length)];
        int level = Math.Max(1, floor + Random.Shared.Next(-1, 3));

        int Scale(int val) => Math.Max(1, val * statScale / 100);

        var mob = new Mob
        {
            Name = template.Name,
            Level = level,
            AggroRange = template.Aggro,
            BaseAttack = Scale(3 + floor * 2),
            BaseDefense = Scale(2 + floor),
            BaseSpeed = 3 + floor,
            BaseSkillDamage = 1 + floor,
            Strength = 1 + floor,
            Vitality = 1 + floor,
            Endurance = 1 + floor,
            Dexterity = 1,
            Agility = 1 + floor,
            Intelligence = 1,
            MaxHealth = Scale(15 + (floor * 10) + Random.Shared.Next(0, 10)),
            ExperienceYield = 20 + (floor * 15),
            ColYield = 5 + (floor * 10)
        };
        mob.SetAppearance(template.Symbol, template.Color);
        mob.CanPoison = template.Poison;
        mob.CanBleed = template.Bleed;
        mob.CanStun = template.Stun;
        mob.CanSlow = template.Slow;
        mob.LootTag = template.LootTag;
        mob.AttackRange = template.Range;
        mob.SpecialAbility = template.Ability;

        // Variant roll — 10% Elite (1.5x stats/2x rewards), 3% Champion (2x stats/3x rewards)
        // Add new variants by extending this block.
        int variantRoll = Random.Shared.Next(100);
        if (variantRoll < 3)
        {
            mob.Variant = "Champion";
            mob.Name = $"Champion {template.Name}";
            mob.BaseAttack = mob.BaseAttack * 2;
            mob.BaseDefense = mob.BaseDefense * 2;
            mob.MaxHealth = mob.MaxHealth * 2;
            mob.ExperienceYield = mob.ExperienceYield * 3;
            mob.ColYield = mob.ColYield * 3;
            mob.Level += 3;
            mob.SetAppearance(char.ToUpper(template.Symbol), Color.BrightMagenta);
        }
        else if (variantRoll < 13)
        {
            mob.Variant = "Elite";
            mob.Name = $"Elite {template.Name}";
            mob.BaseAttack = mob.BaseAttack * 3 / 2;
            mob.BaseDefense = mob.BaseDefense * 3 / 2;
            mob.MaxHealth = mob.MaxHealth * 3 / 2;
            mob.ExperienceYield = mob.ExperienceYield * 2;
            mob.ColYield = mob.ColYield * 2;
            mob.Level += 2;
            mob.SetAppearance(template.Symbol, Color.BrightYellow);
        }

        // Affix roll for elite/champion mobs — random combat modifiers
        if (mob.Variant is "Elite" or "Champion")
            ApplyAffix(mob, mob.Variant == "Champion" ? 2 : 1);

        mob.CurrentHealth = mob.MaxHealth;
        mob.Id = Random.Shared.Next(20000, 99999);
        return mob;
    }

    // ── Affix system — random modifiers for elite/champion mobs ──
    private static readonly string[] AffixPool =
    {
        "Swift", "Armored", "Vampiric", "Berserker",
        "Crystallite", "Regenerating", "Pack Hunter", "Ethereal",
        "Cardinal-Marked", "Immortal-Marked",
    };

    private static void ApplyAffix(Mob mob, int count)
    {
        var available = new List<string>(AffixPool);
        var applied = new List<string>();

        for (int i = 0; i < count && available.Count > 0; i++)
        {
            int idx = Random.Shared.Next(available.Count);
            string affix = available[idx];
            available.RemoveAt(idx);
            applied.Add(affix);

            switch (affix)
            {
                case "Swift":
                    mob.BaseSpeed += 3;
                    mob.AggroRange += 2;
                    break;
                case "Armored":
                    mob.BaseDefense = mob.BaseDefense * 3 / 2;
                    break;
                case "Vampiric":
                    // Handled in TurnManager combat — heals 25% of damage dealt
                    break;
                case "Berserker":
                    mob.BaseAttack = mob.BaseAttack * 13 / 10;  // +30%
                    mob.BaseDefense = mob.BaseDefense * 8 / 10; // −20%
                    break;
                case "Crystallite":
                    // Ice-armored — extra def, triggers slow proc in TurnManager
                    mob.BaseDefense = mob.BaseDefense * 13 / 10;
                    mob.CanSlow = true;
                    break;
                case "Regenerating":
                    // Heals 2% of MaxHealth per turn — handled in TurnManager AI tick
                    mob.MaxHealth = mob.MaxHealth * 12 / 10;
                    break;
                case "Pack Hunter":
                    // +30% damage when adjacent to another mob (hook consumed elsewhere)
                    mob.BaseAttack = mob.BaseAttack * 11 / 10;
                    mob.AggroRange += 3;
                    break;
                case "Ethereal":
                    // 30% miss chance on incoming melee attacks
                    mob.BaseSpeed += 2;
                    break;
                case "Cardinal-Marked":
                    // System-glitched — random buff per turn (hook in AI)
                    mob.BaseAttack = mob.BaseAttack * 12 / 10;
                    mob.BaseDefense = mob.BaseDefense * 12 / 10;
                    break;
                case "Immortal-Marked":
                    // Cannot die below 1 HP once (Heathcliff echo) — flag consumed in AI
                    mob.MaxHealth = mob.MaxHealth * 15 / 10;
                    break;
            }
        }

        mob.Affix = string.Join(" ", applied);
        mob.Name = $"{mob.Affix} {mob.Name}";
    }
}
