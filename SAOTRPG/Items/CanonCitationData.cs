namespace SAOTRPG.Items;

// Static citation database for high-profile Legendary weapons. Keyed by DefinitionId.
// Source: LEGENDARY_REDISTRIBUTION_PROPOSAL.md §5 (Bundle 11 Wave 1 anchor table).
// ~70 entries cover canon LN/AL/IF/HF/LR/MD/FD/myth-named blades; non-listed items
// fall back to "Invented for AincradTRPG" tag in the inspect popup.
public static class CanonCitationData
{
    public readonly record struct Citation(string Source, string FloorAnchor, string Detail);

    private static readonly Dictionary<string, Citation> _entries = new()
    {
        // ── One-Handed Sword ─────────────────────────────────────────
        ["elucidator"] = new("SAO LN vol 4 (canon)", "F50 floor-boss",
            "Kirito takes Elucidator from F50 boss \"The Gleam Eyes\"."),
        ["dark_repulser"] = new("SAO LN vol 2 / HF (canon)", "F55 field-boss",
            "Lisbeth-forged white blade gifted to Kirito on F55."),
        ["remains_heart"] = new("LN/IF (canon)", "F48 Lisbeth quest",
            "Lisbeth character-quest reward — relocated from R6 craft."),
        ["savage_squall"] = new("Invented (AL Lycoris-style)", "F17 floor-boss",
            "AincradTRPG floor-boss original drop."),
        ["void_eater"] = new("Invented", "F30 floor-boss",
            "AincradTRPG floor-boss original drop."),
        ["eurynomes_holy_sword"] = new("Hollow Fragment (canon)", "F90 NPC quest",
            "High Priestess Sola implement quest, F90 — HF endgame."),
        ["fiendblade_deathbringer"] = new("Hollow Fragment (canon)", "F81 NPC quest",
            "Ranger Torva implement quest — HF F81 canon."),
        ["fayblade_tizona"] = new("Hollow Fragment (canon)", "F83 NPC quest",
            "Apiarist Nell implement quest — HF F83 canon."),
        ["godblade_dragonslayer"] = new("Hollow Fragment (canon)", "F85 field-boss",
            "Abased Beast field-boss drop — HF F85 canon."),
        ["ohs_rosso_forneus"] = new("Integral Factor (canon)", "F61 field-boss",
            "Crimson Forneus IF Series boss drop — Rosso quartet."),
        ["ohs_yasha_astaroth"] = new("Integral Factor (canon)", "F87 field-boss",
            "Yasha IF Series boss drop."),
        ["ohs_gaou_reginleifr"] = new("Integral Factor (canon)", "F90 field-boss",
            "Gaou IF Series boss drop."),
        ["ohs_aurumbrand_hauteclaire"] = new("Hollow Fragment (canon)", "F92 NPC quest",
            "Auric Knight Halric implement quest — HF F92."),
        ["ohs_velocious_brain"] = new("Hollow Fragment (canon)", "F80–F86 chest",
            "HF F82 Hollow Area implement floor band."),
        ["ohs_unfolding_truth_fragrant_olive"] = new("Alicization / SAO MD (canon)", "F65 NPC quest",
            "Selka chain reward — SAO MD-Alicization arc canon."),
        ["ohs_elucidator_rouge"] = new("Last Recollection / FD (canon)", "F98 field-boss",
            "Ashen Kirito Simulacrum drop — LR red-edge canon."),
        ["ohs_blade_of_the_lightwolf"] = new("Invented (AL Lycoris)", "F77–F83 chest",
            "AL Lycoris invented brand — non-canon Aincrad."),
        ["ohs_illustrious_sword"] = new("Invented (AL DLC)", "F79–F85 chest",
            "AL DLC invented blade — non-canon."),

        // ── Two-Handed Sword ─────────────────────────────────────────
        ["saintblade_durandal"] = new("Hollow Fragment (canon) / myth", "F87 field-boss",
            "Night Stalker drop — HF canon, named after Roland's blade."),
        ["stigmablade_arondight"] = new("Hollow Fragment (canon) / myth", "F95 field-boss",
            "Gaia Breaker drop — Arondight (Lancelot's blade) myth name."),
        ["demonblade_gram"] = new("Hollow Fragment (canon) / Norse myth", "F96 field-boss",
            "Eternal Dragon drop — Gram is Sigurd's dragonslaying sword."),
        ["ths_sacred_cross"] = new("Infinity Moment (canon)", "F92 floor-boss LAB",
            "IM F92 LAB-floor canon drop."),
        ["ths_wice_ritter"] = new("IF/IM (canon)", "F86–F92 shop tier",
            "Infinity Moment shop-tier canon weapon."),
        ["ths_demonslayer"] = new("Invented (AL Lycoris)", "F82–F88 chest",
            "AL Lycoris invented brand."),

        // ── Katana ───────────────────────────────────────────────────
        ["jato_onikirimaru"] = new("Hollow Fragment (canon)", "F80 NPC quest",
            "Hunter Kojiro implement quest — HF F80 canon."),
        ["shinto_ama_no_murakumo"] = new("Hollow Fragment / Shinto myth", "F95 NPC quest",
            "Elder Beastkeeper quest — Susanoo's cloud-splitter blade."),
        ["yato_masamune"] = new("Hollow Fragment (canon)", "F98 field-boss",
            "Blaze Armor drop — HF Hollow Area canon."),
        ["kat_muramasa"] = new("Infinity Moment (canon) / myth", "F90–F94 shop tier",
            "IM canon shop-tier — named after the cursed-blade smith."),
        ["kat_saku"] = new("Infinity Moment (canon)", "F94 floor-boss LAB",
            "IM F94 LAB-floor canon drop."),
        ["kat_demon_blade_muramasa"] = new("Lost Song (canon)", "F65–F72 chest",
            "LS canon Salamander apex katana."),
        ["midnight_sun"] = new("Invented", "F49 floor-boss",
            "AincradTRPG floor-boss original."),

        // ── Rapier ───────────────────────────────────────────────────
        ["mothers_rosario"] = new("SAO LN — Mother's Rosario arc", "F76 NPC quest",
            "Yuuki canon — given to Asuna in the MR arc."),
        ["rap_rosso_rhapsody"] = new("Integral Factor (canon)", "F58–F64 chest",
            "Rosso quartet IF chest band, anchored at F61 boss."),
        ["rap_spiralblade_rendering_fail"] = new("Hollow Fragment (canon)", "F84 NPC quest",
            "Spiralist Vey implement quest — HF F84."),
        ["rap_glimmerblade_banishing_ray"] = new("Hollow Fragment (canon)", "F93 field-boss",
            "Banishing Ray drop — HF F93."),
        ["midnight_rain"] = new("Invented", "F43 floor-boss",
            "AincradTRPG floor-boss original."),

        // ── Axe ──────────────────────────────────────────────────────
        ["axe_ground_gorge"] = new("Hollow Fragment (canon)", "F55 NPC quest",
            "Agil's Apprentice quest reward — HF canon."),
        ["ragnaroks_bane_headsman"] = new("Hollow Fragment (canon) / Norse myth", "F94 field-boss",
            "Ark Knight drop — HF F94 canon."),
        ["axe_rosso_dominion"] = new("Integral Factor (canon)", "F58–F64 chest",
            "Rosso quartet IF — anchored at F61."),
        ["axe_crusher_bond_cyclone"] = new("Hollow Fragment (canon)", "F85 NPC quest",
            "Crusher Drago implement quest — HF F85."),
        ["axe_fellaxe_demons_scythe"] = new("Hollow Fragment (canon)", "F86 field-boss",
            "Fellaxe Revenant drop — HF F86 canon."),

        // ── Mace ─────────────────────────────────────────────────────
        ["yggdrasil"] = new("Norse myth — chain T3", "F62–F68 chain",
            "Mjolnir chain T3 mythic crafting anchor."),
        ["mace_of_asclepius"] = new("Hollow Fragment / Greek myth", "F77 field-boss",
            "Goblin Leader drop — HF F77; Asclepius healer myth."),
        ["infinite_ouroboros"] = new("Hollow Fragment (canon)", "F79 NPC quest",
            "Scholar Ellroy quest — HF F79 implement."),
        ["starmace_elysium"] = new("Hollow Fragment (canon)", "F88 NPC quest",
            "Watcher Kael quest — HF F88 implement."),
        ["mce_caduceus"] = new("Lost Song / LR (canon)", "F70–F76 chest",
            "LS canon healer-staff motif."),
        ["cactus_bludgeon"] = new("Invented", "F38 floor-boss",
            "AincradTRPG floor-boss original."),

        // ── Spear ────────────────────────────────────────────────────
        ["vijaya"] = new("Indian myth — chain T3", "F60–F67 chain",
            "Caladbolg chain T3 mythic crafting anchor."),
        ["demonspear_gae_bolg"] = new("Hollow Fragment / Celtic myth", "F83 field-boss",
            "Arboreal Fear drop — HF F83; Cú Chulainn's spear."),
        ["saintspear_rhongomyniad"] = new("Hollow Fragment / Arthurian myth", "F91 NPC quest",
            "Torchbearer Meir quest — HF F91; Arthur's spear."),
        ["godspear_gungnir"] = new("Hollow Fragment / Norse myth", "F98 NPC quest",
            "Sentinel Captain quest — Odin's spear."),
        ["spr_lunatic_roof"] = new("Infinity Moment (canon)", "F98 floor-boss LAB",
            "IM F98 LAB-floor canon drop."),
        ["spr_wave_schneider"] = new("Infinity Moment (canon)", "F92–F96 shop tier",
            "IM canon shop-tier weapon."),
        ["spr_divine_laevateinn"] = new("LR / Norse myth", "F86–F92 chest",
            "Surtr's flame sword reimagined as polearm."),

        // ── Dagger ───────────────────────────────────────────────────
        ["mate_chopper"] = new("LN / Integral Factor (canon)", "F1–F5 chest",
            "IF canon F1-area starter — \"Mate's Chopper\" anchor."),
        ["misericorde"] = new("Medieval myth — chain T3", "F50–F58 chain",
            "Iron Maiden chain T3 mythic crafting anchor."),
        ["dag_rue_feuille"] = new("Infinity Moment (canon)", "F94–F98 shop tier",
            "IM canon shop-tier dagger."),
        ["dag_mirage_knife"] = new("Infinity Moment (canon)", "F95 floor-boss LAB",
            "IM F95 LAB-floor canon drop."),
        ["dag_obsidian_dagger"] = new("Fractured Daydream (canon) / Yui", "F70–F76 chest",
            "Yui FD anchor band."),
        ["dag_thunder_gods_rift_blade"] = new("Fractured Daydream (canon) / Argo", "F78–F84 chest",
            "Argo FD anchor band."),
        ["dag_fragarach"] = new("LR / Celtic myth", "F76–F82 chest",
            "Lugh's wind-cutting blade."),
        ["phantasmagoria"] = new("Invented", "F24 floor-boss",
            "AincradTRPG floor-boss original."),

        // ── Bow ──────────────────────────────────────────────────────
        ["bow_zephyros"] = new("Infinity Moment (canon)", "F85 floor-boss LAB",
            "IM F85 LAB-floor canon drop."),
        ["bow_artemis"] = new("Infinity Moment (canon) / Greek myth", "F99 floor-boss LAB",
            "IM F99 LAB-floor — paired with Night Sky Divine."),
        ["bow_rosso_albatross"] = new("Integral Factor (canon)", "F58–F64 chest",
            "Rosso quartet IF chest band."),
        ["bow_silvan_bow"] = new("Lost Song (canon)", "F69–F75 chest",
            "LS canon Sylph apex bow."),
        ["bow_holy_larc_qui_ne_faut"] = new("Lost Song / LR myth", "F89–F95 chest",
            "LS canon mythic apex bow."),
        ["starfall"] = new("Invented", "F11 floor-boss",
            "AincradTRPG floor-boss original."),

        // ── Scimitar ─────────────────────────────────────────────────
        ["iblis"] = new("Islamic myth — chain T3", "F45–F52 chain",
            "Satanachia chain T3 mythic crafting anchor."),
        ["sci_arcaneblade_soul_binder"] = new("Hollow Fragment (canon)", "F80 field-boss",
            "Soul Binder drop — HF F80 implement."),
        ["sci_fellblade_ruinous_doom"] = new("Hollow Fragment (canon)", "F83 field-boss",
            "Ruinous Herald drop — HF F83."),
        ["sci_deathglutton_epetamu"] = new("Hollow Fragment (canon)", "F99 NPC quest",
            "Last Herald Xiv quest — HF F99 apex implement."),
        ["sci_silver_wing"] = new("Infinity Moment (canon)", "F92–F96 shop tier",
            "IM canon shop-tier scimitar."),
        ["sci_glow_haze"] = new("Infinity Moment (canon)", "F93 floor-boss LAB",
            "IM F93 LAB-floor canon drop."),

        // ── Shield (Legendary armor) ─────────────────────────────────
        ["shd_rosso_aegis"] = new("Integral Factor (canon)", "F61 field-boss",
            "Rosso secondary drop — IF Series boss."),
        ["shd_yasha_kavacha"] = new("Integral Factor (canon)", "F87 field-boss",
            "Yasha secondary drop — IF Series boss."),
        ["shd_gaou_tatari"] = new("Integral Factor (canon)", "F90 field-boss",
            "Gaou secondary drop — IF Series boss."),
        ["shd_ancile"] = new("LR / Roman myth", "F84–F90 chest",
            "Sacred shield of Mars — mythic chest band."),

        // ── Claws ────────────────────────────────────────────────────
        ["clw_iron_fist_oguma"] = new("Lost Song (canon)", "F77–F83 chest",
            "LS canon Salamander apex claws."),
    };

    // Look up a citation by definition id. Returns null when no canon entry exists —
    // caller falls back to "Invented for AincradTRPG" or "No canon citation available".
    public static Citation? Lookup(string? defId)
    {
        if (string.IsNullOrEmpty(defId)) return null;
        return _entries.TryGetValue(defId, out var c) ? c : null;
    }

    // Total registered citation count — used for documentation / coverage queries.
    public static int Count => _entries.Count;
}
