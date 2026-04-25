namespace SAOTRPG.Systems;

// Weapon evolution chains: 9 × 4 tiers. T4 apex (NextDefId=null).
// Mat qty: T1→T2=3, T2→T3=8, T3→T4=20 + peak-extra rare mat.

public enum ChainTier { T1, T2, T3, T4 }

public record ChainStep(
    string ChainId,           // e.g., "ohs_tyrfing"
    ChainTier Tier,           // current tier of this weapon
    string? NextDefId,        // null at T4 (apex)
    string MaterialDefId,     // chain catalyst item id
    int MaterialQty,          // 3/8/20 depending on tier, 0 at T4
    string? PeakExtraMatId    // extra rare material required for T3→T4 (null at earlier tiers)
);

public static class WeaponEvolutionChains
{
    // Weapon DefId → ChainStep (missing = not in a chain). PeakExtraMatId
    // substitutes use registered proxies below (unreg'd reagents → proxy DefId).
    public static readonly Dictionary<string, ChainStep> Chains = new()
    {
        // ── 1H Sword chain: Final Espada → Asmodeus → Final Avalanche → Tyrfing
        ["final_espada"]    = new("ohs_tyrfing", ChainTier.T1, "asmodeus",        "demonic_sigil",  3,  null),
        ["asmodeus"]        = new("ohs_tyrfing", ChainTier.T2, "final_avalanche", "demonic_sigil",  8,  null),
        ["final_avalanche"] = new("ohs_tyrfing", ChainTier.T3, "tyrfing",         "demonic_sigil", 20, "dragon_scale"),
        ["tyrfing"]         = new("ohs_tyrfing", ChainTier.T4, null,              "demonic_sigil",  0,  null),

        // ── Rapier chain: Prima Sabre → Pentagramme → Charadrios → Hexagramme
        ["prima_sabre"]     = new("rapier_hexagramme", ChainTier.T1, "pentagramme",  "geometric_shard",  3,  null),
        ["pentagramme"]     = new("rapier_hexagramme", ChainTier.T2, "charadrios",   "geometric_shard",  8,  null),
        ["charadrios"]      = new("rapier_hexagramme", ChainTier.T3, "hexagramme",   "geometric_shard", 20, "bat_wing"),  // wing_fragment substitute
        ["hexagramme"]      = new("rapier_hexagramme", ChainTier.T4, null,           "geometric_shard",  0,  null),

        // ── Scimitar chain: Moonstruck Saber → Diablo Esperanza → Iblis → Satanachia
        ["moonstruck_saber"] = new("scim_satanachia", ChainTier.T1, "diablo_esperanza", "infernal_gem",  3,  null),
        ["diablo_esperanza"] = new("scim_satanachia", ChainTier.T2, "iblis",            "infernal_gem",  8,  null),
        ["iblis"]            = new("scim_satanachia", ChainTier.T3, "satanachia",       "infernal_gem", 20, "flame_core"), // flame_essence substitute
        ["satanachia"]       = new("scim_satanachia", ChainTier.T4, null,               "infernal_gem",  0,  null),

        // ── Dagger chain: Heated Razor → Valkyrie → Misericorde → The Iron Maiden
        ["heated_razor"]       = new("dagger_iron_maiden", ChainTier.T1, "valkyrie",          "valkyrie_feather",  3,  null),
        ["valkyrie"]           = new("dagger_iron_maiden", ChainTier.T2, "misericorde",       "valkyrie_feather",  8,  null),
        ["misericorde"]        = new("dagger_iron_maiden", ChainTier.T3, "iron_maiden_dagger", "valkyrie_feather", 20, "venom_gland"), // venom_sac substitute
        ["iron_maiden_dagger"] = new("dagger_iron_maiden", ChainTier.T4, null,                "valkyrie_feather",  0,  null),

        // ── Mace chain: Lunatic Press → Nemesis → Yggdrasil → Mjolnir
        ["lunatic_press"] = new("mace_mjolnir", ChainTier.T1, "nemesis",    "lunar_core",  3,  null),
        ["nemesis"]       = new("mace_mjolnir", ChainTier.T2, "yggdrasil",  "lunar_core",  8,  null),
        ["yggdrasil"]     = new("mace_mjolnir", ChainTier.T3, "mjolnir",    "lunar_core", 20, "mithril_trace"), // gear_fragment substitute
        ["mjolnir"]       = new("mace_mjolnir", ChainTier.T4, null,         "lunar_core",  0,  null),

        // ── Katana chain: Matamon → Shishi-Otoshi → Shichishito → Masamune
        ["matamon"]       = new("katana_masamune", ChainTier.T1, "shishi_otoshi", "oni_ash",  3,  null),
        ["shishi_otoshi"] = new("katana_masamune", ChainTier.T2, "shichishito",   "oni_ash",  8,  null),
        ["shichishito"]   = new("katana_masamune", ChainTier.T3, "masamune",      "oni_ash", 20, "ectoplasm"), // soul_dust substitute
        ["masamune"]      = new("katana_masamune", ChainTier.T4, null,            "oni_ash",  0,  null),

        // ── 2H Sword chain: Matter Dissolver → Titan's Blade → Ifrit → Ascalon
        ["matter_dissolver"] = new("ths_ascalon", ChainTier.T1, "titans_blade", "titan_fragment",  3,  null),
        ["titans_blade"]     = new("ths_ascalon", ChainTier.T2, "ifrit",        "titan_fragment",  8,  null),
        ["ifrit"]            = new("ths_ascalon", ChainTier.T3, "ascalon",      "titan_fragment", 20, "dragon_scale"),
        ["ascalon"]          = new("ths_ascalon", ChainTier.T4, null,           "titan_fragment",  0,  null),

        // ── Axe chain: Bardiche → Archaic Murder → Nidhogg's Fang → Ouroboros
        ["bardiche"]        = new("axe_ouroboros", ChainTier.T1, "archaic_murder", "nidhogg_scale",  3,  null),
        ["archaic_murder"]  = new("axe_ouroboros", ChainTier.T2, "nidhoggs_fang",  "nidhogg_scale",  8,  null),
        ["nidhoggs_fang"]   = new("axe_ouroboros", ChainTier.T3, "ouroboros",      "nidhogg_scale", 20, "dragon_scale"),
        ["ouroboros"]       = new("axe_ouroboros", ChainTier.T4, null,             "nidhogg_scale",  0,  null),

        // ── Spear chain: Heart Piercer → Trishula → Vijaya → Caladbolg
        ["heart_piercer"] = new("spear_caladbolg", ChainTier.T1, "trishula",  "trishula_tip",  3,  null),
        ["trishula"]      = new("spear_caladbolg", ChainTier.T2, "vijaya",    "trishula_tip",  8,  null),
        ["vijaya"]        = new("spear_caladbolg", ChainTier.T3, "caladbolg", "trishula_tip", 20, "crystallite_ingot"), // crystal_core substitute
        ["caladbolg"]     = new("spear_caladbolg", ChainTier.T4, null,        "trishula_tip",  0,  null),
    };

    public static ChainStep? Get(string? defId)
        => defId != null && Chains.TryGetValue(defId, out var step) ? step : null;

    public static bool IsChainWeapon(string? defId)
        => defId != null && Chains.ContainsKey(defId);

    // Bundle 13 — Slicing Stone alt-path map.
    // Player presents Slicing Stone (tier-matched) instead of canon catalyst → routes to alt DefId.
    // Tier match: Lesser→T1weapon, Greater→T2weapon, Perfect→T3weapon. T4 apex has no alt branch.
    // Stone qty consumed: 1 (single-stone evolve, undercuts canon 3/8/20 by design).
    public record AltStep(string AltNextDefId, string StoneDefId);

    // Keyed by current weapon DefId. Alt target is a DIFFERENT DefId from canon NextDefId.
    // Targets reuse existing weapons that thematically fit the alt branch — no new DefIds required at this stage.
    public static readonly Dictionary<string, AltStep> AltChains = new()
    {
        // 1H Sword chain alt — Final Espada → Anneal Blade line (canon-called-out, Tier-Forge alt)
        ["final_espada"]    = new("anneal_blade",            "slicing_stone_lesser"),
        ["asmodeus"]        = new("tough_anneal_blade",      "slicing_stone_greater"),
        ["final_avalanche"] = new("pitch_black_anneal_blade", "slicing_stone_perfect"),

        // Rapier chain alt — Prima Sabre → Lambent Light line (Asuna canon)
        ["prima_sabre"]     = new("chivalric_rapier",        "slicing_stone_lesser"),
        ["pentagramme"]     = new("wind_fleuret",            "slicing_stone_greater"),
        ["charadrios"]      = new("lambent_light",           "slicing_stone_perfect"),

        // Scimitar chain alt — Moonstruck Saber → Ladder
        ["moonstruck_saber"] = new("steel_scimitar",         "slicing_stone_lesser"),
        ["diablo_esperanza"] = new("mythril_scimitar",       "slicing_stone_greater"),
        ["iblis"]            = new("celestial_scimitar",     "slicing_stone_perfect"),

        // Dagger chain alt — Heated Razor → Mate Chopper / Snow Warheit line
        ["heated_razor"]      = new("mate_chopper",          "slicing_stone_lesser"),
        ["valkyrie"]          = new("assassin_dagger",       "slicing_stone_greater"),
        ["misericorde"]       = new("snow_warheit",          "slicing_stone_perfect"),

        // Mace chain alt — Lunatic Press → Mace of Lord line
        ["lunatic_press"] = new("iron_mace",                 "slicing_stone_lesser"),
        ["nemesis"]       = new("mythril_mace",              "slicing_stone_greater"),
        ["yggdrasil"]     = new("mace_of_lord",              "slicing_stone_perfect"),

        // Katana chain alt — Matamon → Karakurenai / Kagenui line
        ["matamon"]       = new("steel_katana",              "slicing_stone_lesser"),
        ["shishi_otoshi"] = new("karakurenai",               "slicing_stone_greater"),
        ["shichishito"]   = new("kagenui",                   "slicing_stone_perfect"),

        // 2H Sword chain alt — Matter Dissolver → Tyrant Dragon line
        ["matter_dissolver"] = new("steel_greatsword",       "slicing_stone_lesser"),
        ["titans_blade"]     = new("mythril_greatsword",     "slicing_stone_greater"),
        ["ifrit"]            = new("tyrant_dragon",          "slicing_stone_perfect"),

        // Axe chain alt — Bardiche → Pale Edge / Ochigaitou line
        ["bardiche"]       = new("battle_axe",               "slicing_stone_lesser"),
        ["archaic_murder"] = new("pale_edge",                "slicing_stone_greater"),
        ["nidhoggs_fang"]  = new("ochigaitou",               "slicing_stone_perfect"),

        // Spear chain alt — Heart Piercer → Guilty Thorn / Anubis Spear line
        ["heart_piercer"] = new("iron_spear",                "slicing_stone_lesser"),
        ["trishula"]      = new("guilty_thorn",              "slicing_stone_greater"),
        ["vijaya"]        = new("anubis_spear",              "slicing_stone_perfect"),

        // Anneal Blade alt-path entry — canon F1-10 line gets Slicing Stone branching upward
        ["anneal_blade"]              = new("queens_knightsword", "slicing_stone_greater"),
        ["tough_anneal_blade"]        = new("azure_sky_blade",    "slicing_stone_perfect"),
    };

    // Returns alt-step for (defId, presentedStoneDefId) pair, or null if no alt-path applies.
    public static AltStep? GetAlt(string? defId, string? stoneDefId)
    {
        if (defId == null || stoneDefId == null) return null;
        if (!AltChains.TryGetValue(defId, out var alt)) return null;
        return alt.StoneDefId == stoneDefId ? alt : null;
    }

    // True if presenting any Slicing Stone could route this weapon to an alt-path.
    public static bool HasAltPath(string? defId)
        => defId != null && AltChains.ContainsKey(defId);

    // Returns the canonical Slicing Stone DefId expected for this weapon's alt-path.
    public static string? AltStoneFor(string? defId)
        => defId != null && AltChains.TryGetValue(defId, out var alt) ? alt.StoneDefId : null;

    // Maps floor → T1 chain weapon DefId awarded at that floor's Secret Shrine.
    // Used by MapGenerator.Population (shrine spawn) and TurnManager.Tiles (shrine interact).
    public static readonly Dictionary<int, string> SecretShrineByFloor = new()
    {
        [5]  = "final_espada",      // 1H Sword
        [8]  = "prima_sabre",       // Rapier
        [12] = "moonstruck_saber",  // Scimitar
        [15] = "heated_razor",      // Dagger
        [18] = "matter_dissolver",  // 2H Sword
        [22] = "heart_piercer",     // Spear
        [28] = "lunatic_press",     // Mace
        [32] = "matamon",           // Katana
        [36] = "bardiche",          // Axe
    };
}
