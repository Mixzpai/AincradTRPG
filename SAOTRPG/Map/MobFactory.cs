using Terminal.Gui;
using SAOTRPG.Entities;

namespace SAOTRPG.Map;

public static class MobFactory
{
    // (Name, Symbol, Color, AggroRange, Poisonous, Bleeding, LootTag)
    // Add new poisonous/bleeding mobs by setting Poison/Bleed = true in their template.
    // LootTag maps to themed drops in TurnManager.GetMobLoot(). Add new tags freely.
    private record MobTemplate(string Name, char Symbol, Color Color, int Aggro,
        bool Poison = false, bool Bleed = false, string LootTag = "generic");

    private static readonly MobTemplate[][] FloorMobs =
    {
        // Floor 1 — Kobold territory
        new[]
        {
            new MobTemplate("Kobold Grunt",    'k', Color.BrightRed,    5, LootTag: "kobold"),
            new MobTemplate("Kobold Scout",    'k', Color.Red,          8, LootTag: "kobold"),
            new MobTemplate("Kobold Warrior",  'k', Color.BrightYellow, 5, Bleed: true, LootTag: "kobold"),
            new MobTemplate("Dire Wolf",       'w', Color.Gray,         7, LootTag: "beast"),
            new MobTemplate("Ruin Sentinel",   's', Color.BrightCyan,   4, LootTag: "construct"),
        },
        // Floor 2 — Wildlife
        new[]
        {
            new MobTemplate("Wind Wasp",       'i', Color.BrightYellow, 8, LootTag: "insect"),
            new MobTemplate("Trembling Ox",    'o', Color.Yellow,        3, LootTag: "beast"),
            new MobTemplate("Frenzy Boar",     'b', Color.BrightRed,    6, LootTag: "beast"),
            new MobTemplate("Nepenthes",       'p', Color.BrightGreen,  3, Poison: true, LootTag: "plant"),
            new MobTemplate("Killer Mantis",   'm', Color.Green,        5, Bleed: true, LootTag: "insect"),
        },
        // Floor 3 — Mixed hostiles
        new[]
        {
            new MobTemplate("Drunk Ape",       'a', Color.Yellow,        5, LootTag: "beast"),
            new MobTemplate("Forest Elf",      'e', Color.BrightGreen,  9, LootTag: "humanoid"),
            new MobTemplate("Lizardman",       'l', Color.Green,        6, LootTag: "reptile"),
            new MobTemplate("Gargoyle",        'g', Color.DarkGray,     4, LootTag: "construct"),
            new MobTemplate("Dark Dwarf",      'd', Color.Red,          5, LootTag: "humanoid"),
        },
        // Floor 4 — Undead dungeon
        new[]
        {
            new MobTemplate("Taurus Hurler",   't', Color.BrightRed,    6, LootTag: "beast"),
            new MobTemplate("Scavenger Toad",  'f', Color.Green,        4, Poison: true, LootTag: "beast"),
            new MobTemplate("Undead Knight",   'u', Color.BrightCyan,   7, Bleed: true, LootTag: "undead"),
            new MobTemplate("Skeleton",        'z', Color.White,        6, LootTag: "undead"),
            new MobTemplate("Stone Golem",     'G', Color.Gray,         3, LootTag: "construct"),
        },
        // Floor 5+ — Elite zone
        new[]
        {
            new MobTemplate("Dragon Knight",   'D', Color.BrightRed,    8, Bleed: true, LootTag: "dragon"),
            new MobTemplate("Minotaur",        'M', Color.BrightYellow, 6, LootTag: "beast"),
            new MobTemplate("Shadow Lurker",   'S', Color.DarkGray,     10, LootTag: "undead"),
            new MobTemplate("Cursed Blade",    'c', Color.BrightMagenta,5, Poison: true, LootTag: "construct"),
            new MobTemplate("Flame Elemental", 'F', Color.BrightRed,    7, LootTag: "elemental"),
        },
    };

    /// <summary>
    /// Creates a floor-appropriate mob. statScale is a percentage (100 = normal).
    /// Difficulty scaling is applied to ATK, DEF, HP. Add new tiers by adding MobTemplates above.
    /// </summary>
    public static Mob CreateFloorMob(int floor, int statScale = 100)
    {
        int tier = Math.Clamp(floor - 1, 0, FloorMobs.Length - 1);
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
        mob.LootTag = template.LootTag;

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

        mob.CurrentHealth = mob.MaxHealth;
        mob.Id = Random.Shared.Next(20000, 99999);
        return mob;
    }
}
