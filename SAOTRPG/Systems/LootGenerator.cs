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
    // 0-11 = weapon types, 12-15 = armor/shield, 16 = accessory fallback.
    private const int EquipmentTypeCount = 17;

    // ── Mob loot tables — themed drops by LootTag ─────────────────────
    // Themed drop tables keyed by mob LootTag. Each entry maps to an array
    // of (item name, Col value) tuples. Add new tags/items freely.
    //
    // The 9 chain catalyst materials are woven into the themed tables. When
    // TurnManager.DropLoot picks a tuple whose Name matches an entry in
    // ChainMaterialByName, the drop is routed through ItemRegistry.Create so
    // the player receives a real registered item with a proper DefinitionId
    // (required for the Anvil Evolve flow to see it).
    public static readonly Dictionary<string, (string Name, int Value)[]> MobLootTable = new()
    {
        { "beast",     new[] { ("Raw Hide",        8), ("Beast Fang",     12), ("Sinew",         6) } },
        { "kobold",    new[] { ("Kobold Ear",      5), ("Crude Dagger",   15), ("Tattered Cloth", 4) } },
        { "insect",    new[] { ("Chitin Shard",   10), ("Wing Fragment",   8), ("Venom Sac",     14), ("Valkyrie Feather", 3) } },
        { "plant",     new[] { ("Herb Bundle",    12), ("Toxic Spore",    10), ("Root Fiber",     6) } },
        { "humanoid",  new[] { ("Coin Pouch",     20), ("Iron Ring",      15), ("Worn Map",      10), ("Demonic Sigil",    3), ("Oni Ash",          3) } },
        { "reptile",   new[] { ("Scale Plate",    12), ("Forked Tongue",   8), ("Reptile Eye",   14) } },
        { "undead",    new[] { ("Bone Fragment",    6), ("Soul Dust",      18), ("Cursed Shard",  14), ("Lunar Core",       3) } },
        { "construct", new[] { ("Gear Fragment",   10), ("Crystal Core",   20), ("Iron Bolt",      8), ("Geometric Shard",  3), ("Titan Fragment",   3) } },
        { "dragon",    new[] { ("Dragon Scale",   30), ("Flame Essence",  25), ("Dragon Claw",   20), ("Infernal Gem",     3), ("Nidhogg Scale",    3) } },
        { "elemental", new[] { ("Fire Crystal",   22), ("Essence Wisp",   18), ("Elemental Ash",  12), ("Trishula Tip",     3) } },
        { "aquatic",   new[] { ("Water Core",     15), ("Fish Scale",      8), ("Murky Pearl",   18) } },
        // Hollow / corrupted / celestial — F76+ endgame mobs (Hollow Mutated
        // Wolf, Cardinal Error, Immortal Echo, Void Seraph). Flavoured toward
        // Ash White ore + hollow-themed mats.
        { "hollow",    new[] { ("Hollow Essence", 22), ("Corrupted Shard", 18), ("Void Particle", 14), ("Ectoplasm",       8) } },
    };

    // Chain catalyst display name → ItemRegistry DefId. When the themed-drop
    // roll picks a name that lives in this map, DropLoot routes the drop
    // through ItemRegistry.Create so the resulting item has a DefinitionId
    // (so the Anvil "Evolve Weapon" flow can find + consume it).
    public static readonly Dictionary<string, string> ChainMaterialByName = new()
    {
        ["Demonic Sigil"]    = "demonic_sigil",
        ["Geometric Shard"]  = "geometric_shard",
        ["Infernal Gem"]     = "infernal_gem",
        ["Valkyrie Feather"] = "valkyrie_feather",
        ["Lunar Core"]       = "lunar_core",
        ["Oni Ash"]          = "oni_ash",
        ["Titan Fragment"]   = "titan_fragment",
        ["Nidhogg Scale"]    = "nidhogg_scale",
        ["Trishula Tip"]     = "trishula_tip",
    };

    // Floor-boss guaranteed drops — Divine Objects AND Legendary hand-placed
    // rewards. When a floor boss is defeated on one of these floors, the
    // matching item is guaranteed-dropped alongside usual boss rewards.
    // DropItem() auto-formats Divine rarity with the bespoke ◈ log line;
    // Legendary rarity uses the standard [Legendary] format. Field boss
    // divines live in FieldBossFactory. Quest-rewarded items are handled
    // inline by their NPC dialogue handlers and not listed here.
    //
    // P4 Alicization Lycoris Divine Beast drops (F1-F50) are placed on
    // NON-CANON floor bosses (no Progressive/novel/Hollow Fragment sources)
    // so canonical floor bosses keep their existing drop-table behaviour.
    public static readonly Dictionary<int, string> FloorBossGuaranteedDrops = new()
    {
        // ── Priority 4 Alicization Lycoris Divine Beast drops ──────
        [11] = "starfall",                    // F11 Felos the Ember Drake (invented)
        [17] = "savage_squall",               // F17 Gelidus the Frozen Colossus (invented)
        [24] = "phantasmagoria",              // F24 Grimhollow the Phantom (invented)
        [30] = "void_eater",                  // F30 Primos the World Serpent (invented)
        [38] = "cactus_bludgeon",             // F38 Obsidian the Black Knight (invented)
        [40] = "demonblade_crimson_stream",   // F40 Dracoflame the Elder Wyrm (invented)
        [43] = "midnight_rain",               // F43 Undine the Water Maiden (invented)
        [49] = "midnight_sun",                // F49 Shadowstep Assassin (invented)

        // ── Divine Objects (canon Integrity Knight weapons) ────────
        [20] = "blue_rose_sword",             // Absolut the Winter Monarch — ice theme, Eugeo canon
        [99] = "night_sky_sword",             // Heathcliff's Shadow — pre-F100 endgame, Kirito canon
    };

    // ── Infinity Moment Last-Attack Bonus (Floor Bosses) ───────────
    // When the PLAYER lands the killing blow on a floor boss at one of
    // these floors, the matching weapon drops at 100% rate IN ADDITION to
    // any FloorBossGuaranteedDrops entry for the same floor (e.g. F99
    // drops both Night Sky Sword Divine AND Artemis). All 8 entries are
    // non-enhanceable Legendaries — canon IM tradeoff. Wired in
    // TurnManager.Combat.HandleMonsterKill → Boss branch.
    public static readonly Dictionary<int, string> FloorBossLastAttackDrops = new()
    {
        [85] = "bow_zephyros",
        [92] = "ths_sacred_cross",
        [93] = "sci_glow_haze",
        [94] = "kat_saku",
        [95] = "dag_mirage_knife",
        [96] = "axe_northern_light",
        [98] = "spr_lunatic_roof",
        [99] = "bow_artemis",           // additional drop alongside Night Sky Sword
    };

    // ── Hollow Fragment Last-Attack Bonus (Avatar Weapons) ─────────
    // When a field boss F70+ is killed, if the killer's weapon type matches
    // an Avatar Weapon type, roll the drop chance. 2% for regular F70+ field
    // bosses; 10% for canon HNM bosses listed in CanonHnmBosses. OHS doesn't
    // have a canon Avatar, so OHS kills don't trigger.
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

    // Canon Hollow Named Monster (HNM) field bosses — 10% Last-Attack drop
    // rate, vs the 2% base rate. Keyed by FieldBoss.FieldBossId.
    public static readonly HashSet<string> CanonHnmBosses = new()
    {
        "abased_beast_f85",
        "ark_knight_f94",
        "gaia_breaker_f95",
        "eternal_dragon_f96",
    };

    // Field-boss secondary guaranteed drops — paired with the primary
    // GuaranteedDropId in FieldBossFactory. Used for series that drop both
    // a signature weapon AND a series shield (IF canon: each series has
    // its matching Fermat-style shield). Keyed by FieldBoss.FieldBossId.
    public static readonly Dictionary<string, string> FieldBossSecondaryDrops = new()
    {
        // IF Integral Series — F14, canon shield Fermat.
        ["starlight_sentinel_f14"] = "shd_fermat",
        // IF Nox Series — F25, canon shield Nox Fermat.
        ["labyrinth_warden_f25"]   = "shd_nox_fermat",
        // IF Rosso Series — F61, [INVENTED] Rosso Aegis.
        ["crimson_forneus_f61"]    = "shd_rosso_aegis",
        // IF Yasha Series — F87, [INVENTED] Yasha Kavacha.
        ["yasha_night_demon_f87"]  = "shd_yasha_kavacha",
        // IF Gaou Series — F90, [INVENTED] Gaou Tatari.
        ["gaou_ox_king_f90"]       = "shd_gaou_tatari",
    };

    // Floor-banded registered-item loot pool. When RollChestItem picks the
    // "registered loot" branch (~5% of equipment rolls), a DefId is chosen
    // from the pool whose (minFloor, maxFloor) band contains CurrentFloor.
    // This is how the IF Anneal line (F1-10) and non-guaranteed series
    // weapons reach the player — they are drop-table items that would
    // otherwise only be reachable via the series field boss.
    public static readonly (int MinFloor, int MaxFloor, string DefId)[] FloorBandedRegisteredLoot =
    {
        // Anneal line — Sachi/Kirito-era starter OHS (IF canon, F1-10).
        (1,  10, "anneal_blade"),                 // Common-Uncommon; also stocked at Agil
        (4,  10, "tough_anneal_blade"),           // Rare upgrade
        (8,  12, "pitch_black_anneal_blade"),     // Rare black-steel variant

        // IF Integral Series secondaries (primary = Arc Angel via F14 boss).
        (12, 22, "ohs_integral_radgrid"),
        (12, 22, "rap_integral_gusion"),
        (12, 22, "ths_integral_after_glow"),

        // IF Nox Series secondaries (primary = Nox Radgrid via F25 boss).
        (22, 34, "dag_nox_nocturne"),
        (22, 34, "rap_nox_gusion"),
        (22, 34, "bow_nox_arc_angel"),
        (22, 34, "ths_nox_after_glow"),

        // IF Rosso Series secondaries (primary = Rosso Forneus via F61 boss).
        (58, 72, "bow_rosso_albatross"),
        (58, 72, "spr_rosso_sigrun"),
        (58, 72, "rap_rosso_rhapsody"),
        (58, 72, "axe_rosso_dominion"),

        // IF Yasha Series secondaries (primary = Yasha Astaroth via F87 boss).
        (84, 94, "kat_yasha_oratorio"),
        (84, 94, "dag_yasha_envy"),

        // IF Gaou Series secondaries (primary = Gaou Reginleifr via F90 boss).
        (88, 99, "kat_gaou_oratorio"),

        // ── Hollow Fragment Hollow Area Uniques (5) — rare chest drops ─
        // Spread across floor bands per scope doc. NOT guaranteed.
        (30, 40, "ohs_traitorblade_argute_brand"), // F35
        (50, 60, "bow_shroudbow_star_stitcher"),   // F55
        (65, 75, "scy_reaper_scythe"),             // F70
        (78, 88, "ohs_velocious_brain"),           // F82
        (92, 99, "ths_saintblade_ragnarok"),       // F95

        // ── Infinity Moment Shop weapons — fallback drop paths ─────
        // These also rotate into F50+ dynamic shop tiers (ShopTierSystem),
        // but are added here so they can still reach the player via the
        // chest/drop-table path when a shop isn't reachable.
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

        // ── Memory Defrag + Fractured Daydream expansion ────────────
        // Placement notes: MD Originals distributed per rarity band.
        // FD character canon weapons placed at flavor-appropriate floors.
        // FD elemental variants all go into F50+ rare/epic pool per spec.

        // MD Originals — Rare F25-50, Epic F50-75, Legendary F75-99.
        (28, 50, "ohs_cobalt_tristan"),             // MD Rare
        (28, 50, "ohs_atlantis_sword"),             // MD Rare
        (28, 50, "rap_venus_heart"),                // MD Rare
        (28, 50, "rap_bloody_rapier"),              // MD Rare
        (28, 50, "rap_holy_flower_rapier"),         // MD Rare
        (28, 50, "rap_mithril_rapier"),             // MD Rare
        (28, 50, "bow_cheer_of_love"),              // MD Rare
        (50, 75, "dag_purple_star_baselard"),       // MD Epic
        (50, 75, "spr_neo_atlantis"),               // MD Epic
        (50, 75, "ohs_eternal_promise"),            // MD Epic
        (50, 75, "bow_aqua_spread"),                // MD Epic
        (50, 75, "rap_chivalrous_rapier"),          // MD Epic
        (75, 99, "ohs_sword_of_diva"),              // MD Legendary
        (75, 99, "rap_espada_of_sword_dance"),      // MD Legendary
        (75, 99, "ths_sword_of_causality"),         // MD Legendary
        (75, 99, "kat_shining_nemesisz"),           // MD Legendary

        // MD Alicization gap weapons — quest-reward-flavored placements.
        (65, 80, "ohs_unfolding_truth_fragrant_olive"), // MD Alice F65+ upgrade
        (80, 99, "ohs_black_iron_dual_sword_a"),    // Underworld Kirito pair A F80+
        (80, 99, "ohs_black_iron_dual_sword_b"),    // Underworld Kirito pair B F80+
        // ohs_red_rose_sword — relocated to F95 field boss
        //   (Warden of the Blooming Rose, FieldBossFactory).

        // FD Character Core Canon — Legendary-band rare drops per spec.
        // Several entries relocated to dedicated field bosses / quest NPCs
        // per Canon Field Boss Wiring pass:
        //   ohs_elucidator_rouge → F98 Ashen Kirito Simulacrum
        //   ths_flame_lord       → F80 Pyre Lord of Heathcliff
        //   ohs_silvery_ruler    → F97 Administrator's Regent
        //   rap_macafitel        → F85 Yuuki's Echo
        (85, 99, "ohs_chaos_raider_dual"),          // Kirito dual F85+
        (88, 99, "kat_murasama_g4_dual"),           // Kirito F90+
        (85, 99, "axe_naz"),                        // Agil F85+
        (75, 90, "ohs_golden_osmanthus"),           // Alice F75+
        (85, 99, "mce_grida_replicant"),            // Lisbeth F85+
        (70, 85, "dag_obsidian_dagger"),            // Yui F70+
        (80, 95, "dag_thunder_gods_rift_blade"),    // Argo F80+
        (75, 90, "ohs_tanquiem"),                   // Oberon F75+

        // FD Character Canon — quest/lower-band flavor placements.
        (40, 55, "mce_plain_mace"),                 // Lisbeth Rare craft/shop F40+
        (45, 60, "dag_virt_katze"),                 // Argo quest-reward band F45+
        // axe_ground_gorge — relocated to F55 NPC quest (Agil's Apprentice).
        (50, 65, "kat_sweep_saber"),                // Leafa field-boss band F50+
        // kat_spirit_kagutsuchi — relocated to F60 Kagutsuchi the Fire Samurai.
        // kat_spirit_susanoo    — relocated to F70 Susanoo the Storm Blade.

        // FD Elemental Variants — ALL F50+ rare/epic pool per spec.
        // Rare (no-element-prefix) band: F50-75; Epic band: F60-85.
        (50, 75, "ohs_sword_of_the_gentle_breeze"), // Alice wind Rare
        (50, 75, "ohs_purple_bellflower_sword"),    // Alice dark Rare
        (50, 75, "ohs_arc_order"),                  // Heathcliff water Rare
        (50, 75, "ohs_abyss_keeper"),               // Heathcliff dark Rare
        (50, 75, "rap_ray_grace"),                  // Asuna water Rare
        (50, 75, "rap_shadow_grace"),               // Asuna dark Rare
        (50, 75, "kat_futari_shizuka"),             // Klein dark Rare
        (50, 75, "kat_icicle_blade"),               // Leafa water Rare
        (50, 75, "dag_defeza"),                     // Silica water Rare
        (50, 75, "dag_hermit_fang"),                // Argo wind Rare
        (60, 85, "ohs_flare_pulsar"),               // Kirito fire Epic
        (60, 85, "ohs_lightning_divider_dual"),     // Kirito thunder Epic
        (60, 85, "ohs_red_peony_sword"),            // Alice fire Epic
        (60, 85, "ohs_thunderclap_sword"),          // Alice thunder Epic
        (60, 85, "ohs_topaz_edge"),                 // Heathcliff thunder Epic
        (60, 85, "ohs_saint_guarder"),              // Heathcliff light Epic
        (60, 85, "ohs_excalibur_oberon"),           // Oberon light Epic
        (60, 85, "ohs_bloodthirst"),                // Oberon fire Epic
        (60, 85, "rap_volt_rapier"),                // Asuna thunder Epic
        (60, 85, "rap_dazzling_blink"),             // Asuna light Epic
        (60, 85, "kat_white_plum_blade"),           // Klein light Epic
        (60, 85, "kat_eradicate_saber"),            // Leafa light Epic
        (60, 85, "axe_ignite_bardiche"),            // Agil fire Epic
        (60, 85, "axe_tyrant_fall"),                // Agil water Epic
        (60, 85, "axe_sturm_welt"),                 // Agil wind Epic
        (60, 85, "mce_blazing_torch"),              // Lisbeth fire Epic
        (60, 85, "mce_elemental_hammer"),           // Lisbeth light Epic

        // ── Cross-Game Sweep (AL / Lost Song / Last Recollection) ──────
        // Final cross-game weapon pass: 55 weapons placed across the F50-99
        // endgame bands. Starlight Banner (Dorothy's Divine) + Corrupted
        // variants are NOT in this pool — quest-reward + stone-transform
        // respectively. Corruption Stones drop via field-boss F95+ path
        // wired in TurnManager.Combat.

        // Group 1 — AL Normal Raid (Epic F70-85)
        (70, 85, "axe_skyrend"),
        (70, 85, "rap_timestream"),
        (70, 85, "dag_veinshredder"),
        (70, 85, "ohs_dragonstar"),
        (70, 85, "spr_heavenstriker"),
        (70, 85, "ohs_superior_blade"),
        (70, 85, "ths_sacred_inferno"),

        // Group 2 — AL Extreme Raid (Legendary F85-95)
        (85, 95, "ohs_blade_of_the_lightwolf"),
        (85, 95, "rap_graceful_needle"),
        (85, 95, "dag_whitespark"),
        (85, 95, "ths_demonslayer"),
        (85, 95, "ths_blazewyrm_greatsword"),
        (85, 95, "spr_arctic_pillar"),
        (85, 95, "mce_starshatter"),

        // Group 3 — AL Relic Boss Drops (Legendary F80-95)
        (80, 95, "ohs_scorching_blade"),
        (80, 95, "mce_beasthowl"),
        (80, 95, "ohs_double_edged_blade"),
        (80, 95, "mce_whirlpool_hammer"),
        (80, 95, "bow_cinder_bow"),
        (80, 95, "ohs_dragoncrest"),
        (80, 95, "axe_deathbringer"),
        (80, 95, "spr_frostpeak"),
        (80, 95, "axe_snowsunder"),

        // Group 4 — AL DLC (Legendary F85-99)
        (85, 99, "bow_loveblight_bow"),
        (85, 99, "ths_purgatorial_greatsword"),
        (85, 99, "sci_savage_sandstorm"),
        (85, 99, "ohs_illustrious_sword"),
        (85, 99, "ths_lifestream_greatsword"),
        (85, 99, "bow_glitzwood_bow"),

        // Group 5 — Lost Song Top-Tier per Type (Legendary F80-95)
        (80, 95, "ohs_blazing_sword"),
        (80, 95, "rap_glaring_light"),
        (80, 95, "dag_fragarach"),
        (80, 95, "kat_demon_blade_muramasa"),
        (80, 95, "axe_lang"),
        (80, 95, "mce_caduceus"),
        (80, 95, "spr_brave_song"),
        (80, 95, "bow_silvan_bow"),
        (80, 95, "clw_iron_fist_oguma"),

        // Group 6 — Lost Song Canon Mythological (Legendary F75-99)
        (75, 99, "kat_futsu_no_mitama"),
        (75, 99, "axe_nadr"),
        (75, 99, "spr_divine_laevateinn"),
        (75, 99, "spr_elders_trident"),
        (75, 99, "bow_holy_larc_qui_ne_faut"),
        (75, 99, "bow_artemis_fult"),
        (75, 99, "clw_paopei"),
        (75, 99, "ths_object_eraser"),
        (75, 99, "dag_giardino"),
        (75, 99, "shd_ancile"),

        // Group 7 — Lost Song Easter Eggs (Epic F50-70)
        (50, 70, "ohs_kagetsu_4"),
        (50, 70, "ohs_laser_sword_hg"),

        // Group 8 — Last Recollection Game-Original (Epic + Legendary)
        (50, 65, "scy_azuretear_scythe"),            // Dorothy's base scythe
        (80, 95, "kat_darkness_rending_blade"),      // Eydis's shadow katana

        // Group 9 — Last Recollection DLC skins
        (90, 99, "ohs_rainbow_blade_ex_eterna"),     // Rainbow Blade Ex Eterna Legendary
        (60, 80, "ohs_aetherial_glow"),              // Aetherial Glow Epic
    };

    // ── IM Enhancement Ore themed drops ──────────────────────────────
    // Mob LootTag → ore DefId. When a matching mob dies, TurnManager rolls
    // OreDropChancePercent for the themed ore. If no tag matches, no ore
    // drops via this path — rare-boss drops still fire from a separate path.
    public static readonly Dictionary<string, string> OreByLootTag = new()
    {
        // Crimson Flame — fire/demon/volcanic themes (dragon+elemental proxy).
        ["dragon"]    = "ore_crimson_flame",
        ["elemental"] = "ore_crimson_flame",
        // Adamant — armored/construct/golem themes.
        ["construct"] = "ore_adamant",
        // Crust — earth/giant/undead themes.
        ["undead"]    = "ore_crust",
        ["reptile"]   = "ore_crust",
        // Sharp Blade — humanoid/bandit/PK themes.
        ["humanoid"]  = "ore_sharp_blade",
        ["kobold"]    = "ore_sharp_blade",
        // Flowing Water — aquatic/ice themes.
        ["aquatic"]   = "ore_flowing_water",
        // Wind Flower — insect/beast/flying themes.
        ["insect"]    = "ore_wind_flower",
        ["beast"]     = "ore_wind_flower",
        // Ash White — hollow/corrupted/celestial F76+ endgame mobs. The
        // "hollow" LootTag covers Hollow Mutated Wolf, Cardinal Error,
        // Immortal Echo, Void Seraph.
        ["hollow"]    = "ore_ash_white",
    };

    // 4% themed ore drop chance per tagged mob kill. Tunable from one spot.
    public const int OreDropChancePercent = 4;

    // Rare boss path — 20% chance per field/floor boss to drop a random ore.
    public const int BossOreDropChancePercent = 20;

    // Pick a random ore DefId for boss drops (uniform across the 7 ores).
    public static string PickRandomOreDefId()
    {
        var ids = EnhancementOreDefinitions.AllOreDefIds;
        return ids[Random.Shared.Next(ids.Length)];
    }

    // Pick a floor-banded registered loot DefId for the current floor, or
    // null if no entries cover this floor. Used by RollChestItem to wire
    // IF canon weapons (Anneal line + series secondaries) into chests.
    public static string? PickFloorBandedRegisteredDefId(int floor)
    {
        var pool = new List<string>();
        foreach (var (minF, maxF, defId) in FloorBandedRegisteredLoot)
            if (floor >= minF && floor <= maxF) pool.Add(defId);
        if (pool.Count == 0) return null;
        return pool[Random.Shared.Next(pool.Count)];
    }

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
        { "Scimitar",         new[] { "Scimitar", "Cutlass", "Saber", "Falchion", "Khopesh" } },
        { "Claws",            new[] { "Claws", "Talons", "Katars", "Punchblades", "Fang Gauntlets" } },
        { "Scythe",           new[] { "Scythe", "Warscythe", "Reaper", "Death's Edge", "Sickle" } },
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
    // All 12 weapon types can drop. Returns null on the fallback roll.
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
            9  => MakeWeapon("Scimitar",         currentFloor, rarity, scale, 0.95, 2, 1, StatType.Dexterity),
            10 => MakeWeapon("Claws",            currentFloor, rarity, scale, 0.7, 3, 1, StatType.Agility),
            11 => MakeWeapon("Scythe",           currentFloor, rarity, scale, 1.35, 0, 2, StatType.Strength),
            12 => MakeArmor("Chest", "Chestplate", currentFloor, rarity, scale, 1.0, 6),
            13 => MakeArmor("Helmet", "Helmet",    currentFloor, rarity, scale, 0.8, 3),
            14 => MakeArmor("Boots", "Boots",      currentFloor, rarity, scale, 0.7, 2),
            15 => new Armor
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
