using SAOTRPG.Items;
using SAOTRPG.Items.Definitions;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Systems.RunState;

namespace SAOTRPG.Systems.Combat;

// Centralized loot tables, rarity scaling, and equipment creation.
// Static data + pure logic; TurnManager handles orchestration.
public static class LootGenerator
{
    // Rarity roll cumulative %: Common 60, Uncommon 25, Rare 11, Epic 4.
    private const int RarityCommonCeiling = 60;
    private const int RarityUncommonCeiling = 85;
    private const int RarityRareCeiling = 96;

    // 0-11 = weapon types, 12-15 = armor, 16 = shield, 17 = accessory fallback.
    // Rolls 0-16 are the explicit cases in CreateRandomEquipment and the last falls to the
    // accessory branch. Adding a case means bumping this, or the new case is dead.
    private const int EquipmentTypeCount = 18;

    // Mob loot tables by LootTag → (name, Col). ChainMaterialByName routes through ItemRegistry.Create for Anvil Evolve.
    public static readonly Dictionary<string, (string Name, int Value)[]> MobLootTable = new()
    {
        { "beast",     new[] { ("Raw Hide",        8), ("Beast Fang",     12), ("Sinew",         6), ("Wolf Pelt",       10), ("Rabbit Meat",      7), ("Hunter Ingot",   80) } },
        { "kobold",    new[] { ("Kobold Ear",      5), ("Crude Dagger",   15), ("Tattered Cloth", 4), ("Lesser Slicing Stone", 3), ("Sharpening Ingot", 80) } },
        { "insect",    new[] { ("Chitin Shard",   10), ("Wing Fragment",   8), ("Venom Sac",     14), ("Valkyrie Feather", 3) } },
        { "plant",     new[] { ("Herb Bundle",    12), ("Toxic Spore",    10), ("Root Fiber",     6), ("Slime Gel",        8) } },
        { "humanoid",  new[] { ("Coin Pouch",     20), ("Iron Ring",      15), ("Worn Map",      10), ("Demonic Sigil",    3), ("Oni Ash",          3) } },
        { "reptile",   new[] { ("Scale Plate",    12), ("Forked Tongue",   8), ("Reptile Eye",   14) } },
        { "undead",    new[] { ("Bone Fragment",    6), ("Soul Dust",      18), ("Cursed Shard",  14), ("Lunar Core",       3), ("Greater Slicing Stone", 3), ("Lunar Ingot",     80) } },
        { "construct", new[] { ("Gear Fragment",   10), ("Crystal Core",   20), ("Iron Bolt",      8), ("Geometric Shard",  3), ("Titan Fragment",   3), ("Warden Ingot",    80) } },
        { "dragon",    new[] { ("Dragon Scale",   30), ("Flame Essence",  25), ("Dragon Claw",   20), ("Infernal Gem",     3), ("Nidhogg Scale",    3), ("Perfect Slicing Stone", 3) } },
        { "elemental", new[] { ("Fire Crystal",   22), ("Essence Wisp",   18), ("Elemental Ash",  12), ("Trishula Tip",     3) } },
        { "aquatic",   new[] { ("Water Core",     15), ("Fish Scale",      8), ("Murky Pearl",   18) } },
        // Hollow/corrupted/celestial — F76+ endgame mobs. Ash White ore themed.
        { "hollow",    new[] { ("Hollow Essence", 22), ("Corrupted Shard", 18), ("Void Particle", 14), ("Ectoplasm",       8) } },
    };

    // Chain catalyst name → ItemRegistry DefId. Matches route through
    // ItemRegistry.Create so Anvil Evolve flow can find/consume them.
    public static readonly Dictionary<string, string> ChainMaterialByName = new()
    {
        // Refinement ingots and mob-drop materials. MobLootTable is keyed by display
        // name and TurnManager.Loot builds a bare MobDrop unless the name maps through here —
        // and Refinement.Socket / CountMaterialByDefId both key on DefinitionId, so an ingot
        // dropped without one lands in the backpack and can never be socketed.
        ["Sharpening Ingot"]    = "sharpening_ingot",
        ["Warden Ingot"]        = "warden_ingot",
        ["Hunter Ingot"]        = "hunter_ingot",
        ["Lunar Ingot"]         = "lunar_ingot",
        ["Slime Gel"]           = "slime_gel",
        ["Wolf Pelt"]           = "wolf_pelt",
        ["Rabbit Meat"]         = "rabbit_meat",
        ["Demonic Sigil"]    = "demonic_sigil",
        ["Geometric Shard"]  = "geometric_shard",
        ["Infernal Gem"]     = "infernal_gem",
        ["Valkyrie Feather"] = "valkyrie_feather",
        ["Lunar Core"]       = "lunar_core",
        ["Oni Ash"]          = "oni_ash",
        ["Titan Fragment"]   = "titan_fragment",
        ["Nidhogg Scale"]    = "nidhogg_scale",
        ["Trishula Tip"]     = "trishula_tip",
        // Alt-path catalysts. Routing through here is what gives the dropped stone its
        // DefinitionId — CountMaterialByDefId keys on that, so a stone built without it
        // would sit in the backpack and never be spendable.
        ["Lesser Slicing Stone"]  = "slicing_stone_lesser",
        ["Greater Slicing Stone"] = "slicing_stone_greater",
        ["Perfect Slicing Stone"] = "slicing_stone_perfect",
    };

    // Floor-boss guaranteed drops — Divine Objects + Legendary rewards. Divine uses
    // ◈ log format; Legendary uses [Legendary]. Field-boss divines live in FieldBossFactory.
    public static readonly Dictionary<int, string> FloorBossGuaranteedDrops = new()
    {
        // Divine Beast drops
        [11] = "starfall",                    // F11 Felos the Ember Drake
        [17] = "savage_squall",               // F17 Gelidus the Frozen Colossus
        [24] = "phantasmagoria",              // F24 Grimhollow the Phantom
        [30] = "void_eater",                  // F30 Primos the World Serpent
        [38] = "cactus_bludgeon",             // F38 Obsidian the Black Knight
        [40] = "demonblade_crimson_stream",   // F40 Dracoflame the Elder Wyrm
        [43] = "midnight_rain",               // F43 Undine the Water Maiden
        [49] = "midnight_sun",                // F49 Shadowstep Assassin

        // Divine Objects (Integrity Knight weapons)
        [20] = "blue_rose_sword",             // Absolut the Winter Monarch — ice theme, Eugeo's sword
        [99] = "night_sky_sword",             // Heathcliff's Shadow — pre-F100 endgame, Kirito's sword

        // Divines held by the F75-F98 floor bosses.
        // No per-run cap; every guaranteed Divine slot drops Divine.
        [75] = "masamune",                    // Skull Reaper — endgame katana apex
        [82] = "hexagramme",                  // Legacy of Grand — hex-pattern arcane rapier
        [84] = "caladbolg",                   // Queen of Ant — Caladbolg Irish mythic spear
        [86] = "tyrfing",                     // King of Skeleton — Tyrfing cursed sword
        [87] = "iron_maiden_dagger",          // Radiance Eater — caged pain dagger
        [88] = "ouroboros",                   // Rebellious Eyes — self-consuming serpent axe
        [91] = "mjolnir",                     // Seraphiel the Fallen — thunderbolt hammer
        [93] = "ascalon",                     // Ragnarok Final Beast — dragon-slayer 2H
        [97] = "time_piercing_sword",         // Cardinal System Error — Bercouli clock-piercer
        [98] = "black_lily_sword",            // Incarnation of the Radius — Sheyta severing strike
    };

    // Last-Attack Bonus: player last-hit → 100% drop alongside FloorBossGuaranteedDrops.
    // Non-enhanceable Legendaries; Elucidator at F50 is exempt.
    public static readonly Dictionary<int, string> FloorBossLastAttackDrops = new()
    {
        // Kirito's Elucidator, from the F50 floor boss.
        [50] = "elucidator",
        [85] = "bow_zephyros",
        [92] = "ths_sacred_cross",
        [93] = "sci_glow_haze",
        [94] = "kat_saku",
        [95] = "dag_mirage_knife",
        [96] = "axe_northern_light",
        [98] = "spr_lunatic_roof",
        [99] = "bow_artemis",           // additional drop alongside Night Sky Sword
    };

    // Avatar Weapons: F70+ field boss + matching weapon → 2% (10% for CanonHnmBosses).
    // One-Handed Sword has no Avatar weapon, so OHS kills don't trigger.
    public static readonly Dictionary<string, string> AvatarWeaponByWeaponType = new()
    {
        ["Rapier"]            = "rap_ishvalca_avatar",
        ["Dagger"]            = "dag_genocide_avatar",
        ["Scimitar"]          = "sci_saphir_avatar",
        ["Katana"]            = "kat_burning_haze_avatar",
        ["Axe"]               = "axe_lord_burster_avatar",
        ["Two-Handed Sword"]  = "ths_absoludia_avatar",
        ["Spear"]             = "spr_asleigeon_avatar",
        ["Mace"]              = "mce_ijelfur_avatar",
    };

    // Hollow Named Monster (HNM) field bosses — 10% Last-Attack drop
    // rate, vs the 2% base rate. Keyed by FieldBoss.FieldBossId.
    public static readonly HashSet<string> CanonHnmBosses = new()
    {
        "abased_beast_f85",
        "ark_knight_f94",
        "gaia_breaker_f95",
        "eternal_dragon_f96",
    };

    // Field-boss secondary drops — paired with FieldBossFactory.GuaranteedDropId
    // for series dropping both weapon + matching shield. Keyed by FieldBossId.
    public static readonly Dictionary<string, string> FieldBossSecondaryDrops = new()
    {
        // Integral Series — F14, shield Fermat.
        ["starlight_sentinel_f14"] = "shd_fermat",
        // Nox Series — F25, shield Nox Fermat.
        ["labyrinth_warden_f25"]   = "shd_nox_fermat",
        // Rosso Series — F61, shield Rosso Aegis.
        ["crimson_forneus_f61"]    = "shd_rosso_aegis",
        // Yasha Series — F87, shield Yasha Kavacha.
        ["yasha_night_demon_f87"]  = "shd_yasha_kavacha",
        // Gaou Series — F90, shield Gaou Tatari.
        ["gaou_ox_king_f90"]       = "shd_gaou_tatari",
    };

    // Floor-banded registered loot pool. RollChestItem ~5% picks a DefId whose
    // (minFloor, maxFloor) band contains CurrentFloor.
    //
    // A floor's entries are its pool breadth, not how much it drops: one chest roll picks one
    // DefId from the whole in-band pool, so widening a band changes the variety a floor can show
    // and not the amount of loot it gives. Constraints on the table:
    //   F1 carries exactly 2 (mate_chopper + lambent_light), F100 carries none, and no floor's
    //   Legendary pool exceeds a budget of 23.
    public static readonly (int MinFloor, int MaxFloor, string DefId)[] FloorBandedRegisteredLoot =
    {
        // Anneal line — Sachi/Kirito-era starter one-handed swords (F1-10). Uncommon/Rare, not Legendary.
        (1,  10, "anneal_blade"),                 // Common-Uncommon; also stocked at Agil
        (4,  10, "tough_anneal_blade"),           // Rare upgrade
        (8,  12, "pitch_black_anneal_blade"),     // Rare black-steel variant

        // ── F1 anchors (Legendary, F1=2 hard requirement) ────────────────
        (1,  5,  "mate_chopper"),                 // F1-area starter dagger
        (1,  8,  "lambent_light"),                // Asuna's rapier, an early tease

        // Integral Series secondaries (primary = Arc Angel via F14 boss). Epic.
        (12, 22, "ohs_integral_radgrid"),
        (12, 22, "rap_integral_gusion"),
        (12, 22, "ths_integral_after_glow"),

        // Nox Series secondaries (primary = Nox Radgrid via F25 boss). Epic.
        (22, 34, "dag_nox_nocturne"),
        (22, 34, "rap_nox_gusion"),
        (22, 34, "bow_nox_arc_angel"),
        (22, 34, "ths_nox_after_glow"),

        // Rosso Series secondaries (primary = Rosso Forneus via F61 boss). F58-64 band.
        (58, 64, "bow_rosso_albatross"),
        (58, 64, "spr_rosso_sigrun"),
        (58, 64, "rap_rosso_rhapsody"),
        (58, 64, "axe_rosso_dominion"),

        // Yasha Series secondaries (primary = Yasha Astaroth via F87 boss). F85-90 band.
        (85, 90, "kat_yasha_oratorio"),
        (85, 90, "dag_yasha_envy"),

        // Gaou Series secondaries (primary = Gaou Reginleifr via F90 boss). F88-94 band.
        (88, 94, "kat_gaou_oratorio"),

        // ── F56-F60 chest band: liberator ────────────────────────────────
        (56, 60, "liberator"),

        // ── Hollow Area Uniques — 3 Rare/Epic + 2 Legendary ──
        (30, 40, "ohs_traitorblade_argute_brand"), // Rare F35
        (50, 60, "bow_shroudbow_star_stitcher"),   // Epic F55
        (65, 75, "scy_reaper_scythe"),             // Epic F70
        (80, 86, "ohs_velocious_brain"),           // Legendary F82 Hollow Area
        (92, 98, "ths_saintblade_ragnarok"),       // Legendary F95 apex

        // Shop weapons — fallback drop paths (also in F50+ shop tiers).
        (76, 85, "rap_edelweiss"),                  // Epic band
        (86, 99, "rap_noctis_strasse"),             // Legendary band
        (76, 85, "ths_fasislawine"),                // Epic band
        (86, 99, "ths_wice_ritter"),                // Legendary band
        (86, 99, "axe_schwarzs_blitz"),             // Legendary band
        (86, 99, "kat_muramasa"),                   // Legendary band
        (76, 85, "spr_foa_stoss"),                  // Epic band
        (86, 99, "spr_wave_schneider"),             // Legendary band
        (76, 85, "sci_poisoned_syringe"),           // Epic band
        (86, 99, "sci_silver_wing"),                // Legendary band
        (76, 85, "dag_flyheight_fang"),             // Epic band
        (86, 99, "dag_rue_feuille"),                // Legendary band

        // Rare F28-50, Epic F50-75.
        (28, 50, "ohs_cobalt_tristan"),             // Rare
        (28, 50, "ohs_atlantis_sword"),             // Rare
        (28, 50, "rap_venus_heart"),                // Rare
        (28, 50, "rap_bloody_rapier"),              // Rare
        (28, 50, "rap_holy_flower_rapier"),         // Rare
        (28, 50, "rap_mithril_rapier"),             // Rare
        (28, 50, "bow_cheer_of_love"),              // Rare
        (50, 75, "dag_purple_star_baselard"),       // Epic
        (50, 75, "spr_neo_atlantis"),               // Epic
        (50, 75, "ohs_eternal_promise"),            // Epic
        (50, 75, "bow_aqua_spread"),                // Epic
        (50, 75, "rap_chivalrous_rapier"),          // Epic
        // Legendaries, mid-game
        (70, 76, "ohs_sword_of_diva"),              // Legendary
        (60, 66, "rap_espada_of_sword_dance"),      // Legendary
        (62, 68, "ths_sword_of_causality"),         // Legendary
        (66, 72, "kat_shining_nemesisz"),           // Legendary

        // Underworld pair — Kirito's early Underworld blades.
        // ohs_unfolding_truth_fragrant_olive — placed via F65 NPC quest (Selka).
        (78, 84, "ohs_black_iron_dual_sword_a"),    // Underworld Kirito pair A
        (78, 84, "ohs_black_iron_dual_sword_b"),    // Underworld Kirito pair B
        // ohs_red_rose_sword — F95 field boss (Warden of Blooming Rose).

        // Character signature weapons — chest-band rare drops. Placed elsewhere:
        // elucidator_rouge→F98, flame_lord→F80, silvery_ruler→F97, macafitel→F85, kagutsuchi→F60, susanoo→F70.
        (76, 82, "ohs_chaos_raider_dual"),          // Kirito dual
        (90, 96, "kat_murasama_g4_dual"),           // Kirito
        (70, 76, "axe_naz"),                        // Agil
        (74, 80, "ohs_golden_osmanthus"),           // Alice
        (75, 81, "mce_grida_replicant"),            // Lisbeth, mid-game
        (66, 72, "dag_obsidian_dagger"),            // Yui
        (78, 84, "dag_thunder_gods_rift_blade"),    // Argo
        (70, 76, "ohs_tanquiem"),                   // Oberon

        // Character weapons — Epic/Rare quest/flavor placements.
        (40, 55, "mce_plain_mace"),                 // Lisbeth Rare craft/shop
        (45, 60, "dag_virt_katze"),                 // Argo quest-reward band
        // axe_ground_gorge — placed via F55 NPC quest (Agil's Apprentice).
        (50, 65, "kat_sweep_saber"),                // Leafa field-boss band

        // Elemental variants, staggered F15-F90 (Rare/Epic).
        // Rare tier: 2 sub-bands across F15-F60.
        (15, 35, "ohs_sword_of_the_gentle_breeze"), // Alice wind Rare
        (15, 35, "ohs_purple_bellflower_sword"),    // Alice dark Rare
        (15, 35, "ohs_arc_order"),                  // Heathcliff water Rare
        (15, 35, "ohs_abyss_keeper"),               // Heathcliff dark Rare
        (15, 35, "rap_ray_grace"),                  // Asuna water Rare
        (35, 60, "rap_shadow_grace"),               // Asuna dark Rare
        (35, 60, "kat_futari_shizuka"),             // Klein dark Rare
        (35, 60, "kat_icicle_blade"),               // Leafa water Rare
        (35, 60, "dag_defeza"),                     // Silica water Rare
        (35, 60, "dag_hermit_fang"),                // Argo wind Rare
        // Epic tier: 5 sub-bands across F30-F90.
        (30, 50, "ohs_flare_pulsar"),               // Kirito fire Epic
        (30, 50, "ohs_lightning_divider_dual"),     // Kirito thunder Epic
        (30, 50, "ohs_red_peony_sword"),            // Alice fire Epic
        (30, 50, "ohs_thunderclap_sword"),          // Alice thunder Epic
        (40, 60, "ohs_topaz_edge"),                 // Heathcliff thunder Epic
        (40, 60, "ohs_saint_guarder"),              // Heathcliff light Epic
        (40, 60, "ohs_excalibur_oberon"),           // Oberon light Epic
        (40, 60, "ohs_bloodthirst"),                // Oberon fire Epic
        (50, 70, "rap_volt_rapier"),                // Asuna thunder Epic
        (50, 70, "rap_dazzling_blink"),             // Asuna light Epic
        (50, 70, "kat_white_plum_blade"),           // Klein light Epic
        (60, 80, "kat_eradicate_saber"),            // Leafa light Epic
        (60, 80, "axe_ignite_bardiche"),            // Agil fire Epic
        (60, 80, "axe_tyrant_fall"),                // Agil water Epic
        (70, 90, "axe_sturm_welt"),                 // Agil wind Epic
        (70, 90, "mce_blazing_torch"),              // Lisbeth fire Epic
        (70, 90, "mce_elemental_hammer"),           // Lisbeth light Epic

        // Groups 1-9: named weapons, mostly F50-99.
        // Starlight Banner + Corrupted variants are not here (quest reward + stone transform).

        // Group 1 — Epic F55-75.
        (55, 75, "axe_skyrend"),
        (55, 75, "rap_timestream"),
        (55, 75, "dag_veinshredder"),
        (55, 75, "ohs_dragonstar"),
        (55, 75, "spr_heavenstriker"),
        (55, 75, "ohs_superior_blade"),
        (55, 75, "ths_sacred_inferno"),

        // Group 2 — Legendary core, bands staggered F60-97.
        (77, 83, "ohs_blade_of_the_lightwolf"),
        (91, 97, "rap_graceful_needle"),
        (60, 67, "dag_whitespark"),
        (82, 88, "ths_demonslayer"),
        (66, 73, "ths_blazewyrm_greatsword"),
        (78, 84, "spr_arctic_pillar"),
        (62, 69, "mce_starshatter"),

        // Group 3 — F66-94.
        (69, 75, "ohs_scorching_blade"),
        (70, 76, "mce_beasthowl"),
        (71, 77, "ohs_double_edged_blade"),
        (80, 86, "mce_whirlpool_hammer"),
        (73, 79, "bow_cinder_bow"),
        (84, 90, "ohs_dragoncrest"),
        (88, 94, "axe_deathbringer"),
        (84, 90, "spr_frostpeak"),
        (66, 72, "axe_snowsunder"),

        // Group 4 — F63-98.
        (80, 86, "bow_loveblight_bow"),
        (91, 97, "ths_purgatorial_greatsword"),
        (63, 70, "sci_savage_sandstorm"),
        (79, 85, "ohs_illustrious_sword"),
        (92, 98, "ths_lifestream_greatsword"),
        (84, 90, "bow_glitzwood_bow"),

        // Group 5 — top tier per weapon type, mid-game.
        (66, 72, "ohs_blazing_sword"),              // Salamander, mid-game
        (77, 83, "rap_glaring_light"),
        (76, 82, "dag_fragarach"),                  // Celtic myth
        (65, 72, "kat_demon_blade_muramasa"),       // Salamander katana
        (44, 50, "axe_lang"),                       // F49-50 chest fill
        (72, 78, "spr_brave_song"),
        (69, 75, "bow_silvan_bow"),                 // Sylph
        (77, 83, "clw_iron_fist_oguma"),            // Salamander

        // Group 6 — mythological weapons; the first seven fill sparse F12-F49 pools.
        (12, 18, "axe_nadr"),                       // fills F12-18
        (18, 25, "kat_futsu_no_mitama"),            // Shinto spirit blade — fills F18-25
        (22, 29, "bow_artemis_fult"),               // fills F22-29
        (25, 32, "dag_giardino"),                   // fills F25-32
        (40, 49, "mce_caduceus"),                   // F49 chest fill (width 10)
        (30, 37, "clw_paopei"),                     // fills F30-37
        (35, 42, "spr_elders_trident"),             // fills F35-42
        // High floors, tail-weighted to F93-99 for late-game support.
        (93, 99, "spr_divine_laevateinn"),          // Surtr's flame, apex tail
        (93, 99, "bow_holy_larc_qui_ne_faut"),      // apex tail
        (93, 99, "ths_object_eraser"),              // apex armor-pierce
        (84, 90, "shd_ancile"),                     // mythological shield

        // Group 7 — Epic F50-70.
        (50, 70, "ohs_kagetsu_4"),
        (50, 70, "ohs_laser_sword_hg"),

        // Group 8 — Epic + Legendary.
        (50, 65, "scy_azuretear_scythe"),            // Dorothy's base scythe
        (84, 90, "kat_darkness_rending_blade"),      // Eydis

        // Group 9 — F60-99.
        (93, 99, "ohs_rainbow_blade_ex_eterna"),     // Rainbow Blade, apex tail
        (60, 80, "ohs_aetherial_glow"),              // Aetherial Glow Epic

        // ── Late game ────────────────────────────────────────────────────
        (88, 94, "radiant_light"),                   // Asuna, postgame

        // ── Celestial mid-tier chest half ────────────────────────────────
        // The Celestial family is twelve weapons split by acquisition route: the six that
        // CanonCitationData lists as "shop tier" are stocked in ShopTierSystem, and these six are
        // chest drops, banded as CanonCitationData declares.
        (62, 68, "celestial_mace"),
        (65, 72, "celestial_axe"),
        (65, 71, "celestial_scimitar"),
        (66, 72, "celestial_claws"),
        (70, 76, "celestial_dagger"),
        (70, 76, "celestial_scythe"),

        // ── Armour sets ──────────────────────────────────────────────────
        // EquipmentSets keys membership on DefinitionId, and procedural armour carries none, so a
        // set can only ever be assembled from these registered pieces, and this table is their
        // acquisition path. Bands follow each tier's RequiredLevel (10 / 25 / 50 / 75), wide enough
        // that four pieces are collectable inside one tier's stretch of floors. Celestial stops at
        // F99 because F100 must carry an empty pool.
        (10, 28, "steel_chestplate"),
        (10, 28, "steel_helmet"),
        (10, 28, "steel_legs"),
        (10, 28, "steel_boots"),

        (26, 52, "mythril_chestplate"),
        (26, 52, "mythril_helmet"),
        (26, 52, "mythril_legs"),
        (26, 52, "mythril_boots"),

        (50, 78, "adamantite_chestplate"),
        (50, 78, "adamantite_helmet"),
        (50, 78, "adamantite_legs"),
        (50, 78, "adamantite_boots"),

        (76, 99, "celestial_chestplate"),
        (76, 99, "celestial_helmet"),
        (76, 99, "celestial_legs"),
        (76, 99, "celestial_boots"),

        // ── Named baseline-tier weapons ──────────────────────────────────
        // Hand-authored one-offs above the shop ladder and below the Legendaries.
        // Bands follow each one's RequiredLevel.
        (3,  12, "stout_brave"),
        (5,  15, "sword_of_eventide"),
        (8,  18, "blue_boar"),
        (10, 22, "argos_claws"),
        (12, 24, "tias_longbow"),
        (14, 26, "agate_rapier"),
        (16, 30, "soul_eater"),
        (18, 32, "verdant_lord"),
        (24, 40, "minotaur_warhammer"),

        // Mithril Pickaxe is deliberately find-only — the shop ladder stops at Iron.
        (30, 60, "mithril_pickaxe"),

        // ── Utility crystals above shop tier ─────────────────────────────
        // Every one has a live effect in TurnManager.HandleCrystal. Anti-Crystal and Mirage
        // Sphere are deliberately absent: HandleCrystal prints a sentence for each and sets no
        // state.
        (14, 40, "corridor_crystal"),
        (60, 95, "pneuma_flower"),

        // ── Refinement ingots, Rare and up ───────────────────────────────
        // The Common four drop from mobs (MobLootTable above); these are the chest half.
        // Refinement.Socket takes a def id, so an ingot that arrives without one cannot be
        // spent — everything here resolves through ItemRegistry and carries its DefinitionId.
        (20, 45, "keen_ingot"),
        (20, 45, "guardian_ingot"),
        (22, 48, "swiftstrike_ingot"),
        (22, 48, "spellbind_ingot"),
        (45, 75, "chimeric_ingot"),
        (48, 78, "sovereign_ingot"),
        (50, 80, "vanguard_ingot"),
        (78, 99, "astral_ingot"),
    };

    // Enhancement Ore themed drops: Mob LootTag → ore DefId at OreDropChancePercent.
    // No-match = no drop via this path (rare-boss drops use separate path).
    public static readonly Dictionary<string, string> OreByLootTag = new()
    {
        ["dragon"]    = "ore_crimson_flame",   // fire/demon/volcanic
        ["elemental"] = "ore_crimson_flame",
        ["construct"] = "ore_adamant",         // armored/golem
        ["undead"]    = "ore_crust",           // earth/giant/undead
        ["reptile"]   = "ore_crust",
        ["humanoid"]  = "ore_sharp_blade",     // humanoid/bandit/PK
        ["kobold"]    = "ore_sharp_blade",
        ["aquatic"]   = "ore_flowing_water",   // aquatic/ice
        ["insect"]    = "ore_wind_flower",     // insect/beast/flying
        ["beast"]     = "ore_wind_flower",
        ["hollow"]    = "ore_ash_white",       // hollow/corrupted F76+
    };

    // 4% themed ore drop chance per tagged mob kill. Tunable from one spot.
    public const int OreDropChancePercent = 4;

    // Rare boss path — 20% chance per field/floor boss to drop a random ore.
    public const int BossOreDropChancePercent = 20;

    // Pick a random ore DefId for boss drops (uniform across all ores).
    public static string PickRandomOreDefId()
    {
        var ids = EnhancementOreDefinitions.AllOreDefIds;
        return ids[RunRng.Next(ids.Length)];
    }

    // Resolve floor-boss drop. There is no per-run Divine cap; every guaranteed
    // drop passes through unchanged (Divine slots stay Divine).
    public static string? ResolveFloorBossDropDefId(int floor)
    {
        if (!FloorBossGuaranteedDrops.TryGetValue(floor, out var dropId)) return null;
        return dropId;
    }

    // Pick a floor-banded registered loot DefId, or null if no band covers floor.
    // Used by RollChestItem to put registered weapons into chests.
    // Two passes over the table so no in-band pool list is allocated: count the matches, draw
    // once against that count, then walk to the k-th match in table order. Exactly one RunRng draw.
    public static string? PickFloorBandedRegisteredDefId(int floor)
    {
        var table = FloorBandedRegisteredLoot;
        int count = 0;
        for (int i = 0; i < table.Length; i++)
            if (floor >= table[i].MinFloor && floor <= table[i].MaxFloor) count++;
        if (count == 0) return null;

        int pick = RunRng.Next(count);
        for (int i = 0; i < table.Length; i++)
        {
            if (floor < table[i].MinFloor || floor > table[i].MaxFloor) continue;
            if (pick-- == 0) return table[i].DefId;
        }
        return null; // unreachable: pick < count and count matches were just counted
    }

    // Named mob → (DefId, dropChance 0-1) overrides. Rolled before the generic
    // LootTag so signature drops fire at their own rates. Multiple entries = multi-roll.
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
        ["Ruin Kobold Miner"]      = new[] { ("iron_ore", 0.5f), ("mithril_trace", 0.05f), ("mithril_ingot", 0.02f) },
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

    // Rarity stat multipliers (StatMul%, DurBonus, ValMul%), indexed 0=Common..3=Epic.
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

    // ── Accessory pool for random drops ────────────────────────────────
    // The one accessory pool, shared by chest drops and Vendor stock. Public so there is one
    // list to add to.
    public static readonly Func<Accessory>[] AccessoryPool =
    {
        AccessoryDefinitions.CreateRingOfStrength,
        AccessoryDefinitions.CreateAgilityNecklace,
        AccessoryDefinitions.CreateGuardianRing,
        AccessoryDefinitions.CreateScholarsPendant,
        AccessoryDefinitions.CreateSwiftBand,
        AccessoryDefinitions.CreateVitalityCharm,
        AccessoryDefinitions.CreateLeatherBracer,
        AccessoryDefinitions.CreateDuelistsBangle,
        AccessoryDefinitions.CreateWardensArmlet,
    };

    // ── Equipment name tables ─────────────────────────────────────────
    private static readonly string[] MetalPrefixes = { "Rusty", "Iron", "Steel", "Sharp", "Gleaming", "Worn", "Fine", "Tempered", "Darksteel", "Mythril" };
    // No prefix here may name an armour-set material. Set membership is by DefinitionId and
    // procedural armour has none, so a drop reading "Mythril Chestplate" would be a different item
    // from the set piece of that exact name — indistinguishable to the player, and it would not
    // count toward the set.
    private static readonly string[] ArmorPrefixes = { "Leather", "Iron", "Studded", "Chainmail", "Plated", "Worn", "Banded", "Hardened" };
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
        { "Scimitar",         new[] { "Scimitar", "Cutlass", "Saber", "Falchion", "Khopesh" } },
        { "Claws",            new[] { "Claws", "Talons", "Katars", "Punchblades", "Fang Gauntlets" } },
        { "Scythe",           new[] { "Scythe", "Warscythe", "Reaper", "Death's Edge", "Sickle" } },
    };

    // The one place a supplied stream is resolved. Null means the ambient run stream, which is
    // what loot rolled during play wants; floor generation passes its own derived stream so the
    // floor cannot depend on how much play preceded it.
    private static int Draw(Random? rng, int maxExclusive) =>
        rng?.Next(maxExclusive) ?? RunRng.Next(maxExclusive);

    private static int Draw(Random? rng, int minInclusive, int maxExclusive) =>
        rng?.Next(minInclusive, maxExclusive) ?? RunRng.Next(minInclusive, maxExclusive);

    private static string PickName(string weaponType, Random? rng) =>
        $"{MetalPrefixes[Draw(rng, MetalPrefixes.Length)]} " +
        (WeaponNouns.TryGetValue(weaponType, out var nouns)
            ? nouns[Draw(rng, nouns.Length)]
            : "Weapon");

    private static string PickArmorName(string slot, Random? rng) =>
        $"{ArmorPrefixes[Draw(rng, ArmorPrefixes.Length)]} {slot}";

    private static string PickShieldName(Random? rng) =>
        $"{ArmorPrefixes[Draw(rng, ArmorPrefixes.Length)]} {ShieldNouns[Draw(rng, ShieldNouns.Length)]}";

    public static string PickRarity(Random? rng = null)
    {
        int r = Draw(rng, 100);
        return r < RarityCommonCeiling ? "Common"
             : r < RarityUncommonCeiling ? "Uncommon"
             : r < RarityRareCeiling ? "Rare"
             : "Epic";
    }

    // Random equipment scaled to floor. All 12 weapon types; null on fallback.
    // `rng` lets a caller that must reproduce on its own terms supply its stream. Floor
    // generation does: a prefab vendor's stock is part of the floor, and drawing it from the
    // ambient run stream would make the floor depend on how much play preceded it, breaking the
    // seed guarantee. Null keeps the run stream, which is right for loot rolled during play.
    public static BaseItem? CreateRandomEquipment(int currentFloor, Random? rng = null)
    {
        string rarity = PickRarity(rng);
        var scale = RarityScaling[RarityIndex(rarity)];
        int dur = 30 + currentFloor * 10 + scale.DurBonus;
        int lvl = Math.Max(1, currentFloor - 1);
        int roll = Draw(rng, EquipmentTypeCount);

        return roll switch
        {
            0  => MakeWeapon("One-Handed Sword", currentFloor, rarity, scale, 1.0, 1, 1, StatType.Attack, rng),
            1  => MakeWeapon("Dagger",           currentFloor, rarity, scale, 0.8, 0, 1, StatType.Agility, rng),
            2  => MakeWeapon("Two-Handed Sword",  currentFloor, rarity, scale, 1.3, 3, 1, StatType.Strength, rng),
            3  => MakeWeapon("Axe",              currentFloor, rarity, scale, 1.2, 2, 1, StatType.Strength, rng),
            4  => MakeWeapon("Katana",           currentFloor, rarity, scale, 1.0, 1, 1, StatType.Agility, rng),
            5  => MakeWeapon("Rapier",           currentFloor, rarity, scale, 0.85, 0, 1, StatType.Speed, rng),
            6  => MakeWeapon("Mace",             currentFloor, rarity, scale, 1.1, 2, 1, StatType.Vitality, rng),
            7  => MakeWeapon("Spear",            currentFloor, rarity, scale, 0.95, 1, 2, StatType.Dexterity, rng),
            8  => MakeWeapon("Bow",              currentFloor, rarity, scale, 0.85, 1, 3, StatType.Dexterity, rng),
            9  => MakeWeapon("Scimitar",         currentFloor, rarity, scale, 0.95, 2, 1, StatType.Dexterity, rng),
            10 => MakeWeapon("Claws",            currentFloor, rarity, scale, 0.7, 3, 1, StatType.Agility, rng),
            11 => MakeWeapon("Scythe",           currentFloor, rarity, scale, 1.35, 0, 2, StatType.Strength, rng),
            12 => MakeArmor("Chest", "Chestplate", currentFloor, rarity, scale, 1.0, 6, rng),
            13 => MakeArmor("Helmet", "Helmet",    currentFloor, rarity, scale, 0.8, 3, rng),
            14 => MakeArmor("Boots", "Boots",      currentFloor, rarity, scale, 0.7, 2, rng),
            // Legs sit between chest and helmet on both coverage and weight. This is the
            // procedural source for the Legs slot; the registered leg pieces are the only other.
            15 => MakeArmor("Legs", "Greaves",     currentFloor, rarity, scale, 0.9, 4, rng),
            16 => new Armor
            {
                Name = PickShieldName(rng), Value = (40 + currentFloor * 22) * scale.ValMul / 100,
                Rarity = rarity, ItemDurability = dur,
                RequiredLevel = lvl, EquipmentType = "Armor",
                ArmorSlot = "Shield", BaseDefense = ScaleDef(currentFloor, scale, 0.8, rng), Weight = 4,
                BlockChance = 10 + currentFloor + Draw(rng, 0, 6),
                Bonuses = new StatModifierCollection().Add(StatType.Defense, ScaleDef(currentFloor, scale, 0.8, rng))
            },
            _ => CreateRandomAccessory(currentFloor, rarity, scale, scale.DurBonus, rng),
        };
    }

    // Procedural weapon factory with floor-scaled stats.
    private static Weapon MakeWeapon(string weaponType, int floor, string rarity,
        (int StatMul, int DurBonus, int ValMul) scale, double dmgFactor, int atkSpeed, int range,
        StatType secondaryStat, Random? rng)
    {
        int baseDmg = (int)((3 + floor * 1.5 + Draw(rng, 0, Math.Max(1, floor / 3))) * dmgFactor) * scale.StatMul / 100;
        int atkBonus = (int)((2 + floor * 1.2 + Draw(rng, 0, 3)) * dmgFactor) * scale.StatMul / 100;
        int secBonus = Math.Max(1, (1 + floor / 4) * scale.StatMul / 100);
        return new Weapon
        {
            Name = PickName(weaponType, rng),
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
        (int StatMul, int DurBonus, int ValMul) scale, double defFactor, int weight, Random? rng)
    {
        int def = ScaleDef(floor, scale, defFactor, rng);
        return new Armor
        {
            Name = PickArmorName(displaySlot, rng),
            Value = (int)((45 + floor * 20) * defFactor) * scale.ValMul / 100,
            Rarity = rarity,
            ItemDurability = 30 + floor * 10 + scale.DurBonus,
            RequiredLevel = Math.Max(1, floor - 1),
            EquipmentType = "Armor",
            ArmorSlot = slot, BaseDefense = def, Weight = weight,
            Bonuses = new StatModifierCollection().Add(StatType.Defense, def)
        };
    }

    private static int ScaleDef(int floor, (int StatMul, int DurBonus, int ValMul) scale, double factor,
        Random? rng) =>
        Math.Max(1, (int)((2 + floor * 1.5 + Draw(rng, 0, 3)) * factor) * scale.StatMul / 100);

    private static Accessory CreateRandomAccessory(int currentFloor, string rarity,
        (int StatMul, int DurBonus, int ValMul) scale, int durBonus, Random? rng)
    {
        var acc = AccessoryPool[Draw(rng, AccessoryPool.Length)]();
        acc.Rarity = rarity;
        acc.Value = acc.Value * scale.ValMul / 100;
        acc.ItemDurability = 50 + currentFloor * 8 + durBonus;
        acc.RequiredLevel = Math.Max(1, currentFloor - 1);
        return acc;
    }
}
