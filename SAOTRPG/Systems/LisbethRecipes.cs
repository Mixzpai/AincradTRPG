namespace SAOTRPG.Systems;

// Canon Hollow Fragment Lisbeth Rarity 6 craft recipes. 18 weapons,
// each crafted at Lindarth (F48) via LisbethCraftDialog. Canon 3M Col
// cost per recipe. Each recipe also consumes 3-5 rare materials, all
// cross-checked against ItemRegistry DefIds (see IngredientDefinitions).
public static class LisbethRecipes
{
    public record MaterialRequirement(string DefId, int Qty);
    public record Recipe(
        string WeaponDefId,
        string DisplayName,
        int ColCost,
        MaterialRequirement[] Materials);

    // Canon 3M Col per craft.
    private const int CanonColCost = 3_000_000;

    // All 18 Lisbeth Rarity 6 recipes. Each uses a mix of the existing
    // rare-tier mob drops in MobDropDefinitions so crafting feels
    // distinct and sends the player to multiple sources per recipe.
    public static readonly Recipe[] All =
    {
        // 1H Sword (3)
        new("ohs_variable_v_vice", "Variable V Vice", CanonColCost, new[]
        {
            new MaterialRequirement("hollow_essence", 5),
            new MaterialRequirement("cardinal_shard", 3),
            new MaterialRequirement("crystallite_ingot", 2),
        }),
        new("ohs_liberator_astral_legion", "Liberator: Astral Legion", CanonColCost, new[]
        {
            new MaterialRequirement("seraph_feather", 4),
            new MaterialRequirement("cardinal_shard", 3),
            new MaterialRequirement("immortal_fragment", 2),
            new MaterialRequirement("dragon_heart", 2),
        }),
        new("ohs_marginless_blade", "Marginless Blade", CanonColCost, new[]
        {
            new MaterialRequirement("hollow_essence", 5),
            new MaterialRequirement("cardinal_shard", 4),
            new MaterialRequirement("immortal_fragment", 3),
            new MaterialRequirement("seraph_feather", 2),
            new MaterialRequirement("crystallite_ingot", 2),
        }),

        // 2H Sword (3)
        new("ths_ogreblade_over_the_cross", "Ogreblade: Over the Cross", CanonColCost, new[]
        {
            new MaterialRequirement("corrupted_scale", 4),
            new MaterialRequirement("dragon_heart", 3),
            new MaterialRequirement("mammoth_tusk", 3),
        }),
        new("ths_deliverer_majestic_lord", "Deliverer: Majestic Lord", CanonColCost, new[]
        {
            new MaterialRequirement("seraph_feather", 5),
            new MaterialRequirement("cardinal_shard", 3),
            new MaterialRequirement("immortal_fragment", 2),
            new MaterialRequirement("crystallite_ingot", 2),
        }),
        new("ths_ambitious_juggernaut", "Ambitious Juggernaut", CanonColCost, new[]
        {
            new MaterialRequirement("mammoth_tusk", 5),
            new MaterialRequirement("corrupted_scale", 3),
            new MaterialRequirement("dragon_heart", 3),
            new MaterialRequirement("crystallite_ingot", 2),
        }),

        // Rapier (2)
        new("rap_championfoil_radiant_chariot", "Championfoil: Radiant Chariot", CanonColCost, new[]
        {
            new MaterialRequirement("seraph_feather", 4),
            new MaterialRequirement("hollow_essence", 4),
            new MaterialRequirement("cardinal_shard", 3),
        }),
        new("rap_glimmerspine_silver_bullet", "Glimmerspine: Silver Bullet", CanonColCost, new[]
        {
            new MaterialRequirement("crystallite_ingot", 4),
            new MaterialRequirement("cardinal_shard", 3),
            new MaterialRequirement("immortal_fragment", 2),
            new MaterialRequirement("seraph_feather", 2),
        }),

        // Scimitar (1)
        new("sci_crescentblade_original_sin", "Crescentblade: Original Sin", CanonColCost, new[]
        {
            new MaterialRequirement("corrupted_scale", 4),
            new MaterialRequirement("hollow_essence", 5),
            new MaterialRequirement("dragon_heart", 2),
            new MaterialRequirement("ectoplasm", 4),
        }),

        // Dagger (1)
        new("dag_notes_end_trinity", "Notes' End Trinity", CanonColCost, new[]
        {
            new MaterialRequirement("ectoplasm", 6),
            new MaterialRequirement("hollow_essence", 4),
            new MaterialRequirement("cardinal_shard", 3),
        }),

        // Katana (2)
        new("kat_godslayer_tattered_hope", "Godslayer: Tattered Hope", CanonColCost, new[]
        {
            new MaterialRequirement("immortal_fragment", 4),
            new MaterialRequirement("cardinal_shard", 3),
            new MaterialRequirement("corrupted_scale", 3),
            new MaterialRequirement("dragon_heart", 2),
        }),
        new("kat_avidya_samsara_blade", "Avidya Samsara Blade", CanonColCost, new[]
        {
            new MaterialRequirement("seraph_feather", 4),
            new MaterialRequirement("immortal_fragment", 3),
            new MaterialRequirement("cardinal_shard", 3),
            new MaterialRequirement("hollow_essence", 4),
            new MaterialRequirement("crystallite_ingot", 2),
        }),

        // Spear (1)
        new("spr_heavenslance_elpis_order", "Heavenslance: Elpis Order", CanonColCost, new[]
        {
            new MaterialRequirement("seraph_feather", 5),
            new MaterialRequirement("cardinal_shard", 3),
            new MaterialRequirement("crystallite_ingot", 3),
            new MaterialRequirement("dragon_heart", 2),
        }),

        // Mace (2)
        new("mce_dictators_punisher", "Dictator's Punisher", CanonColCost, new[]
        {
            new MaterialRequirement("mammoth_tusk", 4),
            new MaterialRequirement("corrupted_scale", 3),
            new MaterialRequirement("cardinal_shard", 3),
            new MaterialRequirement("crystallite_ingot", 2),
        }),
        new("mce_photon_hammer_xp_smasher", "Photon Hammer: XP Smasher", CanonColCost, new[]
        {
            new MaterialRequirement("crystallite_ingot", 5),
            new MaterialRequirement("hollow_essence", 4),
            new MaterialRequirement("seraph_feather", 2),
        }),

        // 2H Axe (2)
        new("axe_hecatomb_giga_disaster", "Hecatomb Axe: Giga Disaster", CanonColCost, new[]
        {
            new MaterialRequirement("mammoth_tusk", 5),
            new MaterialRequirement("corrupted_scale", 4),
            new MaterialRequirement("dragon_heart", 2),
            new MaterialRequirement("immortal_fragment", 2),
        }),
        new("axe_ingurgitator_belzericht", "Ingurgitator: Belzericht", CanonColCost, new[]
        {
            new MaterialRequirement("corrupted_scale", 5),
            new MaterialRequirement("hollow_essence", 5),
            new MaterialRequirement("dragon_heart", 3),
            new MaterialRequirement("cardinal_shard", 2),
        }),

        // Scythe (1)
        new("scy_eldark_radius_sigma", "Eldark Radius Sigma", CanonColCost, new[]
        {
            new MaterialRequirement("corrupted_scale", 4),
            new MaterialRequirement("ectoplasm", 5),
            new MaterialRequirement("hollow_essence", 4),
            new MaterialRequirement("cardinal_shard", 3),
        }),
    };

    public static Recipe? FindByWeaponDefId(string defId)
    {
        foreach (var r in All)
            if (r.WeaponDefId == defId) return r;
        return null;
    }
}
