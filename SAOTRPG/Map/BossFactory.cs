using SAOTRPG.Entities;

namespace SAOTRPG.Map;

// Creates the floor boss for each floor. Every floor has a unique named boss
// sourced from SAO canon (light novels, Progressive, Hollow Fragment, Integral
// Factor) or original designs fitting the floor's era theme.
// Floor 100 is special: a mirror clone of the player character.
public static class BossFactory
{
    // Full 100-floor boss roster. Index = floor - 1.
    // Canon sources: SAO Wiki, Progressive LNs, Hollow Fragment, Integral Factor, CBR rankings.
    // Fields: (Name, Title).
    private static readonly (string Name, string Title)[] BossRoster =
    {
        // ── Verdant Era (Floors 1-5) — nature, beasts, kobolds ──────
        ("Illfang the Kobold Lord",       "Tyrant of the First Floor"),        // F1  — canon
        ("Asterius the Taurus King",      "Sovereign of the Labyrinth"),       // F2  — canon
        ("Nerius the Evil Treant",        "The Rotting Ancient"),              // F3  — canon
        ("Wythege the Hippocampus",       "Serpent of the Drowned Cavern"),    // F4  — canon
        ("Fuscus the Vacant Colossus",    "The Hollow Giant"),                 // F5  — IF canon

        // ── Stone Era (Floors 6-10) — golems, constructs, puzzles ───
        ("The Irrational Cube",           "Enigma of the Sixth Floor"),        // F6  — Progressive canon
        ("The Storm Magnus Duo",          "Twin Sentinels of the Stone Keep"), // F7  — Progressive canon
        ("Wadjet the Flaming Serpent",    "Inferno of the Eighth Floor"),      // F8  — invented, stone/fire
        ("Cagnazzo the Toad Demon",       "Tyrant of the Drowned Vault"),      // F9  — Progressive canon
        ("Kagachi the Samurai Lord",      "Blade of the Rising Moon"),         // F10 — Integral Factor canon

        // ── Crimson Era (Floors 11-15) — fire, demons, molten ───────
        ("Felos the Ember Drake",         "Wings of Living Flame"),            // F11
        ("Volcanus the Molten King",      "Lord of the Slag Throne"),          // F12
        ("Charion the Ash Wraith",        "Specter of Cinder"),               // F13
        ("Ignaroth the Infernal",         "Demon of the Burning Halls"),       // F14
        ("Surtr the Flame Giant",         "The Floor-Scorcher"),               // F15

        // ── Crystal Era (Floors 16-20) — ice, crystal, elemental ────
        ("Crystalis the Frost Weaver",    "Architect of the Ice Labyrinth"),   // F16
        ("Gelidus the Frozen Colossus",   "The Rime-Bound Titan"),             // F17
        ("Prismalynx the Shardcat",       "Predator of Refracted Light"),      // F18
        ("Gelmyre the Crystal Hydra",     "Three Heads of Living Ice"),        // F19
        ("Absolut the Winter Monarch",    "Sovereign of Eternal Frost"),       // F20

        // ── Twilight Era (Floors 21-25) — shadow, undead, dark ──────
        ("Morgath the Lich King",         "Deathless Lord of Floor 21"),       // F21
        ("The Witch of the West",         "Mistress of the Black Marsh"),      // F22 — canon ref
        ("Skullvane the Bone Dragon",     "The Rattling Sky"),                 // F23
        ("Grimhollow the Phantom",        "The Shape That Haunts"),            // F24
        ("The Two-Headed Giant",          "Terror of the Twin Peaks"),         // F25 — canon

        // ── Verdant II (Floors 26-30) — jungle, beast, primal ───────
        ("Venomfang the Basilisk",        "Gaze of Petrification"),            // F26
        ("The Four-Armed Giant",          "Colossus of the Jungle Ruins"),     // F27 — canon
        ("Thornqueen Selvaria",           "Empress of the Briar Maze"),        // F28
        ("Raptoros the Apex Hunter",      "The Unseen Strike"),                // F29
        ("Primos the World Serpent",      "Coil That Encircles the Floor"),    // F30

        // ── Stone II (Floors 31-35) — fortress, golem, siege ────────
        ("Garrison the Living Wall",      "The Unbreakable Bulwark"),          // F31
        ("Ironhyde the Siege Golem",      "Breaker of Gates"),                 // F32
        ("Ballistor the War Machine",     "Construct of Endless Arrows"),      // F33
        ("Warden Keloth",                 "Jailer of the Deep Dungeon"),       // F34
        ("Nicholas The Renegade",         "The Fallen Christmas Knight"),      // F35 — canon

        // ── Crimson II (Floors 36-40) — volcanic, dragon, magma ─────
        ("Pyroclast the Lava Titan",      "Born of the Caldera"),              // F36
        ("Infernus the Red Wyvern",       "Scourge of the Ashen Skies"),       // F37
        ("Obsidian the Black Knight",     "Forged in Volcanic Glass"),         // F38
        ("Magmaron the Core Beast",       "The Living Eruption"),              // F39
        ("Dracoflame the Elder Wyrm",     "Grandfather of Fire"),              // F40

        // ── Crystal II (Floors 41-45) — deep ocean, aquatic ─────────
        ("Leviathan the Depth Lord",      "Terror of the Sunken Halls"),       // F41
        ("Coralith the Reef Titan",       "The Living Atoll"),                 // F42
        ("Undine the Water Maiden",       "Siren of the Deep"),                // F43
        ("Abyssal Kraken",                "Tentacles of the Drowned Floor"),   // F44
        ("Tidecaller Nereus",             "Herald of the Endless Flood"),      // F45

        // ── Twilight II (Floors 46-50) — nightmare, psychic ─────────
        ("The Ant Queen",                 "Matriarch of the Hive Floor"),      // F46 — canon
        ("Mindflayer Zethos",             "The Thought Devourer"),             // F47
        ("Nightweaver Morrigan",          "Spinner of Bad Dreams"),            // F48
        ("Shadowstep Assassin",           "The Boss You Never See Coming"),    // F49
        ("The Six-Armed Buddha",          "Metallic Enlightenment"),           // F50 — canon

        // ── Verdant III (Floors 51-55) — ancient forest, mythic ─────
        ("Yggdrath the World Tree",       "The Rooted Colossus"),              // F51
        ("Fenrir the Dread Wolf",         "The Unchained Beast"),              // F52
        ("Sylphiel the Storm Dryad",      "Wrath of the Ancient Wood"),        // F53
        ("Titanoak the Living Fortress",  "Where the Forest Fights Back"),     // F54
        ("X'rphan the White Wyrm",        "The Pale Dragon of Floor 55"),      // F55 — canon

        // ── Stone III (Floors 56-60) — mountain, giant, earth ───────
        ("Geocrawler",                    "The Burrowing Menace"),             // F56 — canon
        ("Atlas the Mountainbreaker",     "He Who Carries the Ceiling"),       // F57
        ("Gravelthorn the Earth Elemental","Heart of the Mountain"),           // F58
        ("Stonewyrm Basileus",            "Petrified Dragon King"),            // F59
        ("The Armoured Stone Warrior",    "Golem of the Granite Throne"),      // F60 — canon

        // ── Crimson III (Floors 61-65) — hellfire, demon lord ────────
        ("Belzeroth the Pit Fiend",       "Duke of the Infernal Court"),       // F61
        ("Hellion the Chaos Dancer",      "Madness Made Manifest"),            // F62
        ("Ashborn the Ember Lich",        "Death That Burns"),                 // F63
        ("Moloch the Soul Furnace",       "The Hunger That Never Ends"),       // F64
        ("Abaddon the Destroyer",         "Annihilation Incarnate"),           // F65

        // ── Crystal III (Floors 66-70) — void, astral, cosmic ───────
        ("Void Sentinel Nyx",             "Watcher of the Starless Dark"),     // F66
        ("Cosmolith the Star Eater",      "The Gravity That Devours"),         // F67
        ("Etheron the Phase Shifter",     "The Boss Between Dimensions"),      // F68
        ("Nebulord Vortex",               "Storm of Collapsing Stars"),        // F69
        ("Celestine the Radiant",         "Light That Blinds and Burns"),      // F70

        // ── Final Gauntlet (Floors 71-75) — legendary, mythic tier ──
        ("Stormbringer Raijin",           "The Thunder God's Wrath"),          // F71
        ("Bloodfang the Vampire Lord",    "Eternal Night's Master"),           // F72
        ("Deathweaver Arachne",           "Mother of All Spiders"),            // F73
        ("The Gleam Eyes",                "The Blue Demon of Floor 74"),       // F74 — canon
        ("The Skull Reaper",              "Death's Scythe"),                   // F75 — canon

        // ── Endgame (Floors 76-80) — Hollow Fragment canon ──────────
        ("The Ghastlygaze",               "The All-Seeing Abomination"),       // F76 — Hollow Fragment canon
        ("The Crystalize Claw",           "Prismatic Scorpion of Floor 77"),   // F77 — Hollow Fragment canon
        ("The Horn of Madness",           "Berserker of the Endless Maze"),    // F78 — Hollow Fragment canon
        ("The Tempest of Trihead",        "Three-Headed Storm Hydra"),         // F79 — Hollow Fragment canon
        ("The Guilty Scythe",             "Reaper of the Eightieth Floor"),    // F80 — Hollow Fragment canon

        // ── Endgame (Floors 81-85) — Hollow Fragment canon ───────────
        ("The Knight of Darkness",        "Black Paladin of the Void Keep"),   // F81 — Hollow Fragment canon
        ("The Legacy of Grand",           "The Ancient Construct Guardian"),   // F82 — Hollow Fragment canon
        ("The Horn of Furious",           "Flame-Aura Minotaur Lord"),         // F83 — Hollow Fragment canon
        ("The Queen of Ant",              "Matriarch of the Insect Deeps"),    // F84 — Hollow Fragment canon
        ("The Maelstrom of Trihead",      "Upgraded Storm Hydra"),             // F85 — Hollow Fragment canon

        // ── Endgame (Floors 86-90) — Hollow Fragment canon ───────────
        ("The King of Skeleton",          "Lich-Lord of the Bone Throne"),     // F86 — Hollow Fragment canon
        ("The Radiance Eater",            "The Light-Devouring Beast"),        // F87 — Hollow Fragment canon
        ("The Rebellious Eyes",           "Hundred-Eyed Aberration"),          // F88 — Hollow Fragment canon
        ("The Murderer Fang",             "Alpha of the Bleeding Pack"),       // F89 — Hollow Fragment canon
        ("Colossus of Aincrad",           "The Living Floor"),                 // F90 — invented

        // ── Endgame (Floors 91-95) — impossible tier ─────────────────
        ("Seraphiel the Fallen",          "Angel of the Burning Sword"),       // F91
        ("Apollyon the World-Ender",      "The Seventy-Second Demon"),         // F92
        ("Ragnarok the Final Beast",      "End of All Things"),                // F93
        ("Immortal Phoenix",              "The Boss That Won't Stay Dead"),    // F94
        ("Abyss Walker",                  "The Darkness Between Floors"),      // F95

        // ── The Ruby Palace (Floors 96-100) ──────────────────────────
        ("Herald of the Ruby Palace",     "The Last Gatekeeper"),              // F96
        ("Cardinal the System Error",     "When the Game Fights Back"),        // F97
        ("Incarnation of the Radius",     "An Impossible Geometry"),           // F98 — canon
        ("Heathcliff's Shadow",           "Echo of the Creator"),              // F99
        ("???",                           "The Final Trial"),                  // F100 — player clone
    };

    // Create the boss for a given floor. Every floor has a unique name.
    // Floor 100 is handled separately as a player clone.
    public static Boss CreateFloorBoss(int floor)
    {
        if (floor < 1) floor = 1;
        int idx = Math.Clamp(floor - 1, 0, BossRoster.Length - 1);
        var (name, title) = BossRoster[idx];

        // Stat curve: exponential growth so early bosses are beatable
        // and late bosses are genuinely threatening.
        int level = 10 + floor * 2;
        int atk   = 8  + (int)(floor * 1.8 + floor * floor * 0.02);
        int def   = 6  + (int)(floor * 1.2 + floor * floor * 0.015);
        int hp    = 150 + floor * 30 + (int)(floor * floor * 0.5);
        int xp    = 800 + floor * 200 + floor * floor * 5;
        int col   = 4000 + floor * 500 + floor * floor * 20;

        // FB-564 Heathcliff's Gauntlet — boss HP x2, ATK x1.3.
        if (Systems.RunModifiers.IsActive(Systems.RunModifier.HeathcliffsGauntlet))
        {
            hp = (int)(hp * 2.0);
            atk = (int)(atk * 1.3);
        }

        var boss = new GenericBoss
        {
            Id = 9000 + floor,
            Name = name,
            Level = level,
            BaseAttack = atk,
            BaseDefense = def,
            BaseCriticalRate = 5 + floor / 10,
            BaseCriticalHitDamage = 10 + floor / 5,
            BaseSpeed = 5 + floor / 2,
            BaseSkillDamage = 3 + floor,
            Vitality = 5 + floor * 2,
            Strength = 4 + floor * 2,
            Endurance = 3 + floor,
            Dexterity = 3 + floor,
            Agility = 2 + floor,
            Intelligence = 2 + floor,
            ExperienceYield = xp,
            ColYield = col,
        };
        boss.SetBossTitle(title);
        boss.MaxHealth = hp;
        boss.CurrentHealth = hp;
        boss.Abilities = GetAbilities(floor);
        return boss;
    }

    // Assign abilities based on floor tier. Early bosses get 1, mid-game 2, endgame 3.
    private static BossAbility[] GetAbilities(int floor)
    {
        var list = new List<BossAbility>();

        // All bosses from floor 1+: a signature heavy strike.
        list.Add(new BossAbility
        {
            Name = "Power Strike", Type = BossAbilityType.HeavyStrike,
            MinPhase = 2, Cooldown = 4, DamageMultiplier = 1.8,
        });

        // Floor 5+: summon minions in phase 3.
        if (floor >= 5)
            list.Add(new BossAbility
            {
                Name = "Call Reinforcements", Type = BossAbilityType.SummonMinions,
                MinPhase = 3, Cooldown = 8, SummonCount = 1 + floor / 20,
            });

        // Floor 10+: AoE slam in phase 2.
        if (floor >= 10)
            list[0] = new BossAbility
            {
                Name = "Ground Slam", Type = BossAbilityType.AoESlam,
                MinPhase = 2, Cooldown = 5, DamageMultiplier = 1.3, AoERadius = 2,
            };

        // Floor 25+: status breath in phase 2.
        if (floor >= 25)
            list.Add(new BossAbility
            {
                Name = "Toxic Breath", Type = BossAbilityType.StatusBreath,
                MinPhase = 2, Cooldown = 6, AoERadius = 3,
                StatusEffect = floor % 3 == 0 ? "Poison" : floor % 3 == 1 ? "Bleed" : "Slow",
                StatusChance = 0.4,
            });

        // Floor 50+: self-heal in phase 3.
        if (floor >= 50)
            list.Add(new BossAbility
            {
                Name = "Regeneration", Type = BossAbilityType.HealSelf,
                MinPhase = 3, Cooldown = 10,
            });

        // Floor 75+: charge attack from range.
        if (floor >= 75)
            list.Add(new BossAbility
            {
                Name = "Devastating Charge", Type = BossAbilityType.ChargeAttack,
                MinPhase = 1, Cooldown = 5, DamageMultiplier = 2.5,
            });

        return list.ToArray();
    }

    // Floor 100 special: create a boss that mirrors the player's stats.
    // Called from PopulateFloor when floor == 100.
    public static Boss CreatePlayerClone(Entities.Player player)
    {
        var clone = new GenericBoss
        {
            Id = 9100,
            Name = player.FirstName + " " + player.LastName,
            Level = player.Level,
            BaseAttack = player.Attack,
            BaseDefense = player.Defense,
            BaseCriticalRate = player.CriticalRate,
            BaseCriticalHitDamage = player.CriticalHitDamage,
            BaseSpeed = player.Speed,
            BaseSkillDamage = player.SkillDamage,
            Vitality = player.Vitality,
            Strength = player.Strength,
            Endurance = player.Endurance,
            Dexterity = player.Dexterity,
            Agility = player.Agility,
            Intelligence = player.Intelligence,
            ExperienceYield = 50000,
            ColYield = 100000,
        };
        clone.SetBossTitle("Your Shadow — The Final Trial");
        // Clone has 150% of player's max HP to make it a real fight
        clone.MaxHealth = (int)(player.MaxHealth * 1.5);
        clone.CurrentHealth = clone.MaxHealth;
        clone.SetSymbol('@');
        clone.SetColor(Terminal.Gui.Color.BrightRed);
        return clone;
    }
}

// Generic boss subclass used by BossFactory. Exposes setters for visual customization.
public class GenericBoss : Boss
{
    public void SetBossTitle(string title) => BossTitle = title;
    public void SetSymbol(char sym) => Symbol = sym;
    public void SetColor(Terminal.Gui.Color col) => SymbolColor = col;
}
