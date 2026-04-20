namespace SAOTRPG.Systems;

// Sword Skills: hit count, dmg mult, cooldown, post-motion window, unlock threshold.
// Canon names from SAO LN/anime/games.

public enum SkillType { Power, Rush, AoE, Combo, Counter, Projectile }

public record SwordSkill(
    string Id,                // Unique key: "ohs_slant", "rap_stinger"
    string Name,              // Display: "Slant", "Stinger"
    string WeaponType,        // "One-Handed Sword", "Rapier", "Any", etc.
    int Hits,                 // Combo hit count (1-27)
    double DamageMultiplier,  // Total damage = base * multiplier
    int CooldownTurns,        // Turns before reusable
    int PostMotionDelay,      // Turns of +50% incoming dmg after use
    int RequiredProfKills,    // Weapon kills needed to unlock
    SkillType Type,           // Rush/AoE/Power/Combo/Counter/Projectile
    string Description,       // Flavor text in skill dialog
    int Range = 1,            // 1 = melee, 2-3 = rush/ranged
    double StatusChance = 0,  // 0-1 chance to apply status
    string? StatusEffect = null // "Poison", "Stun", "Bleed", null
);

// Static database of all sword skills grouped by weapon type.
public static class SwordSkillDatabase
{
    public static readonly SwordSkill[] AllSkills = BuildDatabase();

    private static readonly Dictionary<string, List<SwordSkill>> _byWeapon = new();

    static SwordSkillDatabase()
    {
        foreach (var s in AllSkills)
        {
            if (!_byWeapon.TryGetValue(s.WeaponType, out var list))
                _byWeapon[s.WeaponType] = list = new();
            list.Add(s);
        }
    }

    public static IReadOnlyList<SwordSkill> ForWeapon(string weaponType) =>
        _byWeapon.TryGetValue(weaponType, out var list) ? list : Array.Empty<SwordSkill>();

    public static SwordSkill? Get(string id) =>
        AllSkills.FirstOrDefault(s => s.Id == id);

    // Skills unlocked at the given kill count for a weapon type.
    public static IEnumerable<SwordSkill> UnlockedFor(string weaponType, int kills) =>
        ForWeapon(weaponType).Where(s => kills >= s.RequiredProfKills);

    private static SwordSkill[] BuildDatabase() => new SwordSkill[]
    {
        // ONE-HANDED SWORD — balanced all-rounder, medium speed and power.
        new("ohs_horizontal", "Horizontal", "One-Handed Sword",
            1, 1.2, 0, 0, 0, SkillType.Power,
            "A simple horizontal slash — the bread and butter of swordsmanship."),

        new("ohs_vertical", "Vertical", "One-Handed Sword",
            1, 1.2, 0, 0, 0, SkillType.Power,
            "A swift downward vertical cut."),

        new("ohs_slant", "Slant", "One-Handed Sword",
            1, 1.3, 0, 0, 0, SkillType.Power,
            "A diagonal slash from shoulder to hip. Quick and reliable."),

        new("ohs_uppercut", "Uppercut", "One-Handed Sword",
            1, 1.4, 1, 0, 10, SkillType.Power,
            "A rising upward cut that launches the blade skyward."),

        new("ohs_rage_spike", "Rage Spike", "One-Handed Sword",
            1, 1.5, 2, 0, 10, SkillType.Rush,
            "A lunging thrust that closes distance in a flash.", 2),

        new("ohs_sonic_leap", "Sonic Leap", "One-Handed Sword",
            1, 1.8, 3, 1, 25, SkillType.Rush,
            "Charge forward and slash downward with momentum.", 3),

        new("ohs_snake_bite", "Snake Bite", "One-Handed Sword",
            2, 1.6, 2, 0, 25, SkillType.Combo,
            "Two rapid swings targeting the enemy's weapon arm."),

        new("ohs_vertical_arc", "Vertical Arc", "One-Handed Sword",
            2, 1.7, 2, 0, 50, SkillType.Combo,
            "A V-shaped double slash tracing an arc through the air."),

        new("ohs_horizontal_arc", "Horizontal Arc", "One-Handed Sword",
            2, 1.7, 2, 0, 50, SkillType.Combo,
            "Left-to-right swing followed by a powerful return slash."),

        new("ohs_sharp_nail", "Sharp Nail", "One-Handed Sword",
            3, 2.0, 3, 1, 75, SkillType.Combo,
            "Three scything slashes — upward diagonal, horizontal, then downward."),

        new("ohs_savage_fulcrum", "Savage Fulcrum", "One-Handed Sword",
            3, 2.2, 3, 1, 100, SkillType.Combo,
            "Traces the shape of '4' in the air with ice-tinged strikes.",
            1, 0.2, "Stun"),

        new("ohs_horizontal_square", "Horizontal Square", "One-Handed Sword",
            4, 2.5, 4, 1, 100, SkillType.AoE,
            "Four rapid slashes tracing a horizontal square around the target."),

        new("ohs_vertical_square", "Vertical Square", "One-Handed Sword",
            4, 2.5, 4, 1, 100, SkillType.Combo,
            "Four vertical cuts tracing a square shape in the air."),

        new("ohs_deadly_sins", "Deadly Sins", "One-Handed Sword",
            7, 3.5, 5, 1, 200, SkillType.Combo,
            "Seven devastating strikes with spins and a backwards somersault finish."),

        new("ohs_howling_octave", "Howling Octave", "One-Handed Sword",
            8, 4.0, 6, 2, 350, SkillType.Combo,
            "Five high-speed thrusts, a downward cut, upward cut, and a full-force finisher."),

        new("ohs_vorpal_strike", "Vorpal Strike", "One-Handed Sword",
            1, 6.0, 10, 2, 350, SkillType.Power,
            "A devastating heavy strike that doubles the blade's reach.", 2),

        new("ohs_nova_ascension", "Nova Ascension", "One-Handed Sword",
            10, 5.0, 8, 2, 500, SkillType.Combo,
            "A ten-hit rising combo that lifts the target skyward."),

        // RAPIER — fast thrusting weapon, high hit counts, low per-hit damage.
        new("rap_linear", "Linear", "Rapier",
            1, 1.2, 0, 0, 0, SkillType.Power,
            "A single precise thrust — the rapier's fundamental skill."),

        new("rap_stinger", "Stinger", "Rapier",
            1, 1.3, 1, 0, 0, SkillType.Rush,
            "Dash forward with a piercing thrust.", 2),

        new("rap_and_leap", "Star Leap", "Rapier",
            2, 1.5, 2, 0, 10, SkillType.Combo,
            "Two quick thrusts followed by a spinning evasive leap."),

        new("rap_fake_step", "Fake Step", "Rapier",
            1, 1.5, 3, 0, 25, SkillType.Counter,
            "Half-step back to dodge, then counter with two swift swipes."),

        new("rap_quadruple_pain", "Quadruple Pain", "Rapier",
            4, 2.5, 4, 1, 50, SkillType.Combo,
            "Four cascading thrusts at blinding speed."),

        new("rap_shooting_star", "Shooting Star", "Rapier",
            1, 3.0, 5, 1, 100, SkillType.Power,
            "A focused thrust that erupts in a small explosion on impact."),

        new("rap_flashing_penetrator", "Flashing Penetrator", "Rapier",
            1, 3.5, 5, 1, 150, SkillType.Rush,
            "A blinding-fast lunge that pierces through defenses.", 3),

        new("rap_star_splash", "Star Splash", "Rapier",
            8, 4.0, 6, 2, 200, SkillType.Combo,
            "Eight cascading thrusts that leave trails of light."),

        new("rap_neutron", "Neutron", "Rapier",
            5, 3.5, 5, 1, 300, SkillType.AoE,
            "A whirlwind of thrusts striking all adjacent enemies."),

        new("rap_mothers_rosario", "Mother's Rosario", "Rapier",
            11, 5.5, 8, 2, 500, SkillType.Combo,
            "An eleven-strike Original Sword Skill — the ultimate rapier technique."),

        // TWO-HANDED SWORD — slow heavy hitter, high damage per swing, long cooldowns.
        new("ths_around", "Around", "Two-Handed Sword",
            1, 1.3, 0, 0, 0, SkillType.Power,
            "A single heavy spinning slash."),

        new("ths_avalanche", "Avalanche", "Two-Handed Sword",
            1, 1.6, 2, 0, 10, SkillType.Rush,
            "Charge forward with the greatsword, cleaving through enemies.", 2),

        new("ths_eruption", "Eruption", "Two-Handed Sword",
            2, 1.8, 2, 0, 25, SkillType.Combo,
            "A quick two-hit combo with devastating power."),

        new("ths_cyclone", "Cyclone", "Two-Handed Sword",
            1, 2.0, 3, 0, 50, SkillType.AoE,
            "Spin in a full circle, striking everything within reach."),

        new("ths_break_time", "Break Time", "Two-Handed Sword",
            1, 2.2, 3, 1, 75, SkillType.Power,
            "A guard-breaking swing with the flat of the blade.",
            1, 0.3, "Stun"),

        new("ths_hollow_silhouette", "Hollow Silhouette", "Two-Handed Sword",
            1, 2.5, 4, 0, 100, SkillType.Counter,
            "Defensive stance that counters with a vicious jumping slash."),

        new("ths_demolition_wield", "Demolition Wield", "Two-Handed Sword",
            3, 2.5, 4, 1, 150, SkillType.Combo,
            "Three heavy overhead slashes that crush armor."),

        new("ths_furious_destroyer", "Furious Destroyer", "Two-Handed Sword",
            6, 3.5, 6, 2, 300, SkillType.Combo,
            "Six powerful horizontal slashes in rapid succession."),

        new("ths_ray_brandish", "Ray Brandish", "Two-Handed Sword",
            1, 7.0, 10, 2, 500, SkillType.Power,
            "Strike the ground to unleash a devastating shockwave."),

        // DAGGER — fastest weapon, status-inflicting, low raw damage.
        new("dag_rapid_bite", "Rapid Bite", "Dagger",
            1, 1.2, 0, 0, 0, SkillType.Rush,
            "Dash in and perform a quick close-range slash.", 2),

        new("dag_venom_bite", "Venom Bite", "Dagger",
            1, 1.4, 2, 0, 10, SkillType.Power,
            "A close-range impale with a chance to poison.",
            1, 0.35, "Poison"),

        new("dag_quick_throw", "Quick Throw", "Dagger",
            1, 1.3, 2, 0, 10, SkillType.Projectile,
            "Hurl a dagger at the target from range.", 3),

        new("dag_reckless_tusk", "Reckless Tusk", "Dagger",
            1, 1.8, 3, 0, 50, SkillType.Counter,
            "Reverse-grip counter thrust that punishes attackers."),

        new("dag_fade_edge", "Fade Edge", "Dagger",
            4, 2.2, 3, 1, 75, SkillType.Combo,
            "Four quick scything slashes in rapid succession."),

        new("dag_paralyze_bite", "Paralyze Bite", "Dagger",
            1, 1.6, 4, 0, 100, SkillType.Power,
            "A precise impale with a chance to stun.",
            1, 0.25, "Stun"),

        new("dag_mirage_fang", "Mirage Fang", "Dagger",
            6, 3.0, 5, 1, 150, SkillType.AoE,
            "Six horizontal zig-zag slashes followed by an upward finish."),

        new("dag_axel_raid", "Axel Raid", "Dagger",
            5, 3.0, 5, 1, 200, SkillType.Combo,
            "A flurry of close-range combination attacks."),

        new("dag_lightning_ripper", "Lightning Ripper", "Dagger",
            8, 4.0, 7, 2, 500, SkillType.Combo,
            "A spinning attack that shreds enemies multiple times."),

        // KATANA — iaido draw-slash style, strong combos, punishing post-motion.
        new("kat_tsujikaze", "Tsujikaze", "Katana",
            1, 1.4, 0, 0, 0, SkillType.Power,
            "A swift upward strike drawn from the hip."),

        new("kat_gengetsu", "Gengetsu", "Katana",
            1, 1.3, 1, 0, 0, SkillType.Power,
            "A crescent moon slash from a low stance."),

        new("kat_hiougi", "Hiougi", "Katana",
            3, 2.0, 3, 0, 25, SkillType.Combo,
            "Three swift consecutive slashes drawn from the sheath."),

        new("kat_kureyu", "Kureyu", "Katana",
            1, 1.8, 2, 0, 50, SkillType.Rush,
            "Leap forward with a rising slash.", 2),

        new("kat_zangetsu", "Zangetsu", "Katana",
            1, 2.0, 3, 0, 75, SkillType.Projectile,
            "Slash the air to send a crescent-shaped shockwave.", 3),

        new("kat_misogi_tsubaki", "Misogi Tsubaki", "Katana",
            1, 2.5, 4, 1, 100, SkillType.Power,
            "Gather strength into a single slash generating a shockwave."),

        new("kat_kyuuki", "Kyuuki", "Katana",
            7, 3.5, 5, 1, 200, SkillType.Combo,
            "Seven swift spinning vertical slashes with a rising finish."),

        new("kat_oborotsukiyo", "Oborotsukiyo", "Katana",
            4, 3.0, 5, 1, 300, SkillType.AoE,
            "Charge in to perform four heavy slashes across a wide area."),

        new("kat_rashoumon", "Rashoumon", "Katana",
            14, 6.0, 10, 2, 500, SkillType.Combo,
            "A legendary fourteen-hit combo. Devastating but leaves you exposed."),

        // AXE — heavy cleave weapon, AoE-focused, stun chance on power hits.
        new("axe_flat", "Flat", "Axe",
            1, 1.3, 0, 0, 0, SkillType.Power,
            "A backhand swing generating a small burst on impact."),

        new("axe_whirlwind", "Whirlwind", "Axe",
            1, 1.5, 2, 0, 10, SkillType.AoE,
            "Swing the axe from behind to damage a wide area."),

        new("axe_violent_spike", "Violent Spike", "Axe",
            2, 1.8, 2, 0, 25, SkillType.Combo,
            "Pounce on target to deliver two heavy attacks."),

        new("axe_lumberjack", "Lumberjack", "Axe",
            1, 2.0, 4, 0, 50, SkillType.Counter,
            "Guard all incoming attacks and counter with a heavy overhead."),

        new("axe_vertical_blast", "Vertical Blast", "Axe",
            1, 2.2, 3, 1, 75, SkillType.Power,
            "Force the axe upward with devastating lifting power.",
            1, 0.25, "Stun"),

        new("axe_round_triple", "Round Triple Smash", "Axe",
            3, 2.5, 4, 1, 100, SkillType.Combo,
            "Three consecutive heavy horizontal slashes."),

        new("axe_catapult_tomahawk", "Catapult Tomahawk", "Axe",
            1, 2.5, 5, 1, 150, SkillType.Projectile,
            "Fling the axe at a distant target with crushing force.", 3),

        new("axe_brutal_maul", "Brutal Maul", "Axe",
            3, 3.5, 6, 2, 300, SkillType.Combo,
            "Three full-body Olympic hammer-throw style swings."),

        new("axe_gravity_impact", "Gravity Impact", "Axe",
            1, 7.0, 10, 2, 500, SkillType.AoE,
            "Strike the ground to distort gravity, unleashing countless shockwaves."),

        // MACE — blunt impact weapon, ground-slam AoEs, frequent stun procs.
        new("mac_power_strike", "Power Strike", "Mace",
            1, 1.3, 0, 0, 0, SkillType.AoE,
            "Hit the ground to create a small explosion.",
            1, 0.15, "Stun"),

        new("mac_assault_dive", "Assault Dive", "Mace",
            1, 1.5, 2, 0, 10, SkillType.Rush,
            "Leap forward to deliver one crushing blow.", 2),

        new("mac_rage_blow", "Rage Blow", "Mace",
            3, 2.0, 3, 0, 25, SkillType.Combo,
            "Three consecutive bashes in rapid succession."),

        new("mac_forward_bunt", "Forward Bunt", "Mace",
            2, 1.8, 2, 0, 50, SkillType.Combo,
            "Step forward with a bunt, then a heavy backhand swing."),

        new("mac_spirit_bomber", "Spirit Bomber", "Mace",
            1, 2.5, 4, 1, 100, SkillType.AoE,
            "Gather energy and strike the ground, creating a large explosion.",
            1, 0.2, "Stun"),

        new("mac_riot_smash", "Riot Smash", "Mace",
            6, 3.5, 5, 1, 200, SkillType.Combo,
            "A furious combo of six consecutive bashes."),

        new("mac_adamantium_breaker", "Adamantium Breaker", "Mace",
            1, 5.0, 7, 2, 350, SkillType.Power,
            "Smash the ground to bring forth a devastating cluster impact."),

        new("mac_valiant_rush", "Valiant Rush", "Mace",
            5, 4.0, 8, 2, 500, SkillType.Combo,
            "Five consecutive heavy swings shielded by a barrier of force."),

        // SPEAR — long reach polearm, mix of thrusts, throws, and defensive spins.
        new("spr_triple_thrust", "Triple Thrust", "Spear",
            3, 1.8, 2, 0, 0, SkillType.Combo,
            "Three consecutive piercing pokes in rapid succession."),

        new("spr_spin_slash", "Spin Slash", "Spear",
            1, 1.5, 2, 0, 10, SkillType.AoE,
            "Spin the spear overhead then follow with a finishing slash."),

        new("spr_digger_glint", "Digger Glint", "Spear",
            1, 1.6, 2, 0, 25, SkillType.Power,
            "Thrust the spear at a point, creating a beam of light."),

        new("spr_forward_spiral", "Forward Spiral", "Spear",
            1, 2.0, 3, 0, 50, SkillType.Counter,
            "Step forward and spin the spear — guards during the spin."),

        new("spr_blast_spear", "Blast Spear", "Spear",
            1, 2.0, 3, 0, 75, SkillType.Projectile,
            "Hurl the spear at a target with piercing force.", 3),

        new("spr_vulture_stinger", "Vulture Stinger", "Spear",
            1, 2.5, 4, 1, 100, SkillType.Power,
            "Vault forward and stab the ground, generating a torrent."),

        new("spr_wild_twister", "Wild Twister", "Spear",
            5, 3.0, 5, 1, 200, SkillType.Combo,
            "Furiously spin the spear, finishing with a diagonal slash."),

        new("spr_fatal_thrust", "Fatal Thrust", "Spear",
            1, 5.0, 8, 2, 500, SkillType.Rush,
            "Launch yourself at the target with the spearhead piercing through.", 3),

        // MARTIAL ARTS — unarmed/knuckle, rush-in combos, counter-punches.
        new("fst_smash_knuckle", "Smash Knuckle", "Unarmed",
            1, 1.3, 0, 0, 0, SkillType.Rush,
            "Dash in and deliver a straight punch.", 2),

        new("fst_gazelle_rush", "Gazelle Rush", "Unarmed",
            3, 2.0, 3, 0, 10, SkillType.Combo,
            "Two roundhouse kicks followed by a finishing upward kick."),

        new("fst_beat_upper", "Beat Upper", "Unarmed",
            1, 1.8, 3, 0, 25, SkillType.Counter,
            "Counter with a powerful gut punch."),

        new("fst_leopold_blitz", "Leopold Blitz", "Unarmed",
            2, 1.8, 2, 0, 50, SkillType.Combo,
            "Step back, then retaliate with a swift kick and flying kick."),

        new("fst_blazing_blow", "Blazing Blow", "Unarmed",
            1, 2.5, 4, 1, 100, SkillType.Power,
            "Gather energy and punch the ground to bring up a torrent."),

        new("fst_turbulence_rush", "Turbulence Rush", "Unarmed",
            7, 3.5, 6, 2, 200, SkillType.Combo,
            "Rush in and unleash a spastic combination of punches and kicks."),

        new("fst_deadly_blow", "Deadly Blow", "Unarmed",
            1, 6.0, 10, 2, 500, SkillType.Power,
            "Channel all strength into a single devastating straight punch."),

        // DUAL BLADES — unique skill, hidden unlock, extreme hit counts and cooldowns.
        new("dbl_double_circular", "Double Circular", "Dual Blades",
            1, 2.0, 2, 0, 0, SkillType.Power,
            "Cross both blades on a single point with devastating force."),

        new("dbl_end_revolver", "End Revolver", "Dual Blades",
            1, 2.5, 3, 0, 0, SkillType.AoE,
            "Spin in place with both blades extended, striking all nearby."),

        new("dbl_specula_cross", "Specula Cross", "Dual Blades",
            2, 2.0, 3, 0, 0, SkillType.Counter,
            "Defensive stance that counters with a quick 1-2 strike."),

        new("dbl_starburst_stream", "Starburst Stream", "Dual Blades",
            16, 8.0, 15, 2, 0, SkillType.Combo,
            "A legendary sixteen-hit alternating dual-blade combo."),

        new("dbl_the_eclipse", "The Eclipse", "Dual Blades",
            27, 12.0, 30, 3, 0, SkillType.Combo,
            "The ultimate Dual Blades skill — a devastating twenty-seven-hit combo."),

        // HOLY SWORD — unique skill, 1H + shield stance, reflective.
        new("holy_sacred_edge", "Sacred Edge", "Holy Sword",
            1, 7.0, 8, 2, 0, SkillType.Power,
            "Heathcliff's signature overhead cross-cut. Grants brief damage reduction.", 2),

        new("holy_divine_cross", "Divine Cross", "Holy Sword",
            2, 4.5, 5, 1, 0, SkillType.Combo,
            "Two perpendicular strikes tracing a holy cross of white light."),

        // DARKNESS — unique skill, night-only empowered skills.
        new("dark_shadow_cleave", "Shadow Cleave", "Darkness",
            3, 3.5, 5, 1, 0, SkillType.Rush,
            "Flicker across the battlefield, striking from the dark in a three-part arc.", 3),

        // BLAZING / FROZEN — elemental unique skills, any weapon.
        new("fire_flame_strike", "Flame Strike", "Blazing",
            1, 3.5, 6, 1, 0, SkillType.AoE,
            "A cone of searing flame. Ignites affected tiles and enemies.", 2, 0.5, "Burn"),

        new("ice_glacial_slash", "Glacial Slash", "Frozen",
            1, 3.5, 6, 1, 0, SkillType.AoE,
            "A line of piercing ice. Freezes enemies in its path.", 3, 0.5, "Slow"),
    };
}
