using SAOTRPG.Items.Definitions;
using SAOTRPG.Items.Definitions.Weapons;

namespace SAOTRPG.Items;

// Maps DefinitionId strings to factory methods for recreating items from save data.
public static class ItemRegistry
{
    private static readonly Dictionary<string, Func<BaseItem>> _registry = new();

    static ItemRegistry()
    {
        // ── One-Handed Swords ─────────────────────────────────────────
        Register("iron_sword",          () => OneHandedSwordDefinitions.CreateIronSword());
        Register("steel_sword",         () => OneHandedSwordDefinitions.CreateSteelSword());
        Register("mythril_sword",       () => OneHandedSwordDefinitions.CreateMythrilSword());
        Register("adamantite_sword",    () => OneHandedSwordDefinitions.CreateAdamantiteSword());
        Register("celestial_blade",     () => OneHandedSwordDefinitions.CreateCelestialBlade());
        Register("anneal_blade",        () => OneHandedSwordDefinitions.CreateAnnealBlade());
        Register("sword_breaker",       () => OneHandedSwordDefinitions.CreateSwordBreaker());
        Register("queens_knightsword",  () => OneHandedSwordDefinitions.CreateQueensKnightsword());
        Register("azure_sky_blade",     () => OneHandedSwordDefinitions.CreateAzureSkyBlade());
        Register("elucidator",          () => OneHandedSwordDefinitions.CreateElucidator());
        Register("dark_repulser",       () => OneHandedSwordDefinitions.CreateDarkRepulser());
        Register("liberator",           () => OneHandedSwordDefinitions.CreateLiberator());
        Register("tyrfing",             () => OneHandedSwordDefinitions.CreateTyrfing());
        Register("sword_of_eventide",   () => OneHandedSwordDefinitions.CreateSwordOfEventide());
        Register("blue_boar",           () => OneHandedSwordDefinitions.CreateBlueBoar());
        Register("crimson_longsword",   () => OneHandedSwordDefinitions.CreateCrimsonLongsword());

        // ── Rapiers ──────────────────────────────────────────────────
        Register("copper_rapier",       () => RapierDefinitions.CreateCopperRapier());
        Register("steel_rapier",        () => RapierDefinitions.CreateSteelRapier());
        Register("mythril_rapier",      () => RapierDefinitions.CreateMythrilRapier());
        Register("adamantite_rapier",   () => RapierDefinitions.CreateAdamantiteRapier());
        Register("celestial_rapier",    () => RapierDefinitions.CreateCelestialRapier());
        Register("chivalric_rapier",    () => RapierDefinitions.CreateChivalricRapier());
        Register("wind_fleuret",        () => RapierDefinitions.CreateWindFleuret());
        Register("agate_rapier",        () => RapierDefinitions.CreateAgateRapier());
        Register("lambent_light",       () => RapierDefinitions.CreateLambentLight());
        Register("hexagramme",          () => RapierDefinitions.CreateHexagramme());
        Register("radiant_light",       () => RapierDefinitions.CreateRadiantLight());
        Register("mothers_rosario",     () => RapierDefinitions.CreateMothersRosario());

        // ── Daggers ──────────────────────────────────────────────────
        Register("rusty_dagger",        () => DaggerDefinitions.CreateRustyDagger());
        Register("steel_dagger",        () => DaggerDefinitions.CreateSteelDagger());
        Register("mythril_dagger",      () => DaggerDefinitions.CreateMythrilDagger());
        Register("adamantite_dagger",   () => DaggerDefinitions.CreateAdamantiteDagger());
        Register("celestial_dagger",    () => DaggerDefinitions.CreateCelestialDagger());
        Register("assassin_dagger",     () => DaggerDefinitions.CreateAssassinDagger());
        Register("snow_warheit",        () => DaggerDefinitions.CreateSnowWarheit());
        Register("mate_chopper",        () => DaggerDefinitions.CreateMateChopper());
        Register("iron_maiden_dagger",  () => DaggerDefinitions.CreateTheIronMaiden());
        Register("argos_claws",         () => DaggerDefinitions.CreateArgosClaws());
        Register("stout_brave",         () => DaggerDefinitions.CreateStoutBrave());

        // ── Two-Handed Swords ────────────────────────────────────────
        Register("iron_greatsword",       () => TwoHandedSwordDefinitions.CreateIronGreatsword());
        Register("steel_greatsword",      () => TwoHandedSwordDefinitions.CreateSteelGreatsword());
        Register("mythril_greatsword",    () => TwoHandedSwordDefinitions.CreateMythrilGreatsword());
        Register("adamantite_greatsword", () => TwoHandedSwordDefinitions.CreateAdamantiteGreatsword());
        Register("celestial_greatsword",  () => TwoHandedSwordDefinitions.CreateCelestialGreatsword());
        Register("tyrant_dragon",         () => TwoHandedSwordDefinitions.CreateTyrantDragon());
        Register("ascalon",               () => TwoHandedSwordDefinitions.CreateAscalon());
        Register("verdant_lord",          () => TwoHandedSwordDefinitions.CreateVerdantLord());

        // ── Katanas ──────────────────────────────────────────────────
        Register("iron_katana",         () => KatanaDefinitions.CreateIronKatana());
        Register("steel_katana",        () => KatanaDefinitions.CreateSteelKatana());
        Register("mythril_katana",      () => KatanaDefinitions.CreateMythrilKatana());
        Register("adamantite_katana",   () => KatanaDefinitions.CreateAdamantiteKatana());
        Register("celestial_katana",    () => KatanaDefinitions.CreateCelestialKatana());
        Register("karakurenai",         () => KatanaDefinitions.CreateKarakurenai());
        Register("masamune",            () => KatanaDefinitions.CreateMasamune());
        Register("soul_eater",          () => KatanaDefinitions.CreateSoulEater());
        Register("kagenui",             () => KatanaDefinitions.CreateKagenui());

        // ── Axes ─────────────────────────────────────────────────────
        Register("hand_axe",            () => AxeDefinitions.CreateHandAxe());
        Register("battle_axe",          () => AxeDefinitions.CreateBattleAxe());
        Register("mythril_axe",         () => AxeDefinitions.CreateMythrilAxe());
        Register("adamantite_axe",      () => AxeDefinitions.CreateAdamantiteAxe());
        Register("celestial_axe",       () => AxeDefinitions.CreateCelestialAxe());
        Register("pale_edge",           () => AxeDefinitions.CreatePaleEdge());
        Register("ouroboros",            () => AxeDefinitions.CreateOuroboros());
        Register("samurai_axe",          () => AxeDefinitions.CreateSamuraiAxe());
        Register("ochigaitou",           () => AxeDefinitions.CreateOchigaitou());

        // ── Maces ────────────────────────────────────────────────────
        Register("wooden_club",          () => MaceDefinitions.CreateWoodenClub());
        Register("iron_mace",            () => MaceDefinitions.CreateIronMace());
        Register("mythril_mace",         () => MaceDefinitions.CreateMythrilMace());
        Register("adamantite_mace",      () => MaceDefinitions.CreateAdamantiteMace());
        Register("celestial_mace",       () => MaceDefinitions.CreateCelestialMace());
        Register("minotaur_warhammer",   () => MaceDefinitions.CreateMinotaurWarhammer());
        Register("mjolnir",              () => MaceDefinitions.CreateMjolnir());
        Register("mace_of_lord",         () => MaceDefinitions.CreateMaceOfLord());

        // ── Spears ───────────────────────────────────────────────────
        Register("wooden_spear",         () => SpearDefinitions.CreateWoodenSpear());
        Register("iron_spear",           () => SpearDefinitions.CreateIronSpear());
        Register("mythril_spear",        () => SpearDefinitions.CreateMythrilSpear());
        Register("adamantite_spear",     () => SpearDefinitions.CreateAdamantiteSpear());
        Register("celestial_spear",      () => SpearDefinitions.CreateCelestialSpear());
        Register("guilty_thorn",         () => SpearDefinitions.CreateGuiltyThorn());
        Register("anubis_spear",         () => SpearDefinitions.CreateAnubisSpear());
        Register("caladbolg",            () => SpearDefinitions.CreateCaladbolg());

        // ── Bows ─────────────────────────────────────────────────────
        Register("short_bow",            () => BowDefinitions.CreateShortBow());
        Register("long_bow",             () => BowDefinitions.CreateLongBow());
        Register("mythril_bow",          () => BowDefinitions.CreateMythrilBow());
        Register("adamantite_bow",       () => BowDefinitions.CreateAdamantiteBow());
        Register("celestial_bow",        () => BowDefinitions.CreateCelestialBow());
        Register("tias_longbow",         () => BowDefinitions.CreateTiasLongbow());

        // ── Staves ───────────────────────────────────────────────────
        Register("apprentice_staff",     () => StaffDefinitions.CreateApprenticeStaff());
        Register("oak_staff",            () => StaffDefinitions.CreateOakStaff());
        Register("mythril_staff",        () => StaffDefinitions.CreateMythrilStaff());
        Register("adamantite_staff",     () => StaffDefinitions.CreateAdamantiteStaff());
        Register("celestial_staff",      () => StaffDefinitions.CreateCelestialStaff());

        // ── Shields ──────────────────────────────────────────────────
        Register("wooden_shield", () => ShieldDefinitions.CreateWoodenShield());
        Register("iron_shield",   () => ShieldDefinitions.CreateIronShield());

        // ── Armor (5 tiers) ──────────────────────────────────────────
        Register("leather_chestplate",     () => ArmorDefinitions.CreateLeatherChest());
        Register("iron_helmet",            () => ArmorDefinitions.CreateIronHelmet());
        Register("steel_chestplate",       () => ArmorDefinitions.CreateSteelChest());
        Register("steel_helmet",           () => ArmorDefinitions.CreateSteelHelmet());
        Register("steel_boots",            () => ArmorDefinitions.CreateSteelBoots());
        Register("mythril_chestplate",     () => ArmorDefinitions.CreateMythrilChest());
        Register("mythril_helmet",         () => ArmorDefinitions.CreateMythrilHelmet());
        Register("mythril_boots",          () => ArmorDefinitions.CreateMythrilBoots());
        Register("adamantite_chestplate",  () => ArmorDefinitions.CreateAdamantiteChest());
        Register("adamantite_helmet",      () => ArmorDefinitions.CreateAdamantiteHelmet());
        Register("adamantite_boots",       () => ArmorDefinitions.CreateAdamantiteBoots());
        Register("celestial_chestplate",   () => ArmorDefinitions.CreateCelestialChest());
        Register("celestial_helmet",       () => ArmorDefinitions.CreateCelestialHelmet());
        Register("celestial_boots",        () => ArmorDefinitions.CreateCelestialBoots());

        // Accessories
        Register("ring_of_strength", () => AccessoryDefinitions.CreateRingOfStrength());
        Register("agility_necklace", () => AccessoryDefinitions.CreateAgilityNecklace());
        Register("guardian_ring", () => AccessoryDefinitions.CreateGuardianRing());
        Register("scholars_pendant", () => AccessoryDefinitions.CreateScholarsPendant());
        Register("swift_band", () => AccessoryDefinitions.CreateSwiftBand());
        Register("vitality_charm", () => AccessoryDefinitions.CreateVitalityCharm());

        // Potions
        Register("health_potion", () => PotionDefinitions.CreateHealthPotion());
        Register("greater_health_potion", () => PotionDefinitions.CreateGreaterHealthPotion());
        Register("antidote", () => PotionDefinitions.CreateAntidote());
        Register("battle_elixir", () => PotionDefinitions.CreateBattleElixir());
        Register("escape_rope", () => PotionDefinitions.CreateEscapeRope());
        Register("revive_crystal", () => PotionDefinitions.CreateReviveCrystal());
        Register("speed_potion", () => PotionDefinitions.CreateSpeedPotion());
        Register("iron_skin_potion", () => PotionDefinitions.CreateIronSkinPotion());

        // Food
        Register("bread", () => FoodDefinitions.CreateBread());
        Register("grilled_meat", () => FoodDefinitions.CreateGrilledMeat());
        Register("honey_bread", () => FoodDefinitions.CreateHoneyBread());
        Register("fish_stew", () => FoodDefinitions.CreateFishStew());
        Register("elven_waybread", () => FoodDefinitions.CreateElvenWaybread());
        Register("spiced_jerky", () => FoodDefinitions.CreateSpicedJerky());
        Register("cream_filled_bread", () => FoodDefinitions.CreateCreamFilledBread());
        Register("black_bread_cream", () => FoodDefinitions.CreateBlackBreadWithCream());
        Register("honey_pie", () => FoodDefinitions.CreateHoneyPie());
        Register("aincrad_salad", () => FoodDefinitions.CreateAincradSalad());
        Register("asunas_sandwich", () => FoodDefinitions.CreateAsunasSandwich());
        Register("ragout_rabbit_stew", () => FoodDefinitions.CreateRagoutRabbitStew());
        Register("grilled_skewer", () => FoodDefinitions.CreateGrilledSkewer());
        Register("gingerbread_cookies", () => FoodDefinitions.CreateGingerbreadCookies());

        // Drinks
        Register("lindas_wine", () => FoodDefinitions.CreateLindasWine());
        Register("herbal_tea", () => FoodDefinitions.CreateHerbalTea());
        Register("warm_milk", () => FoodDefinitions.CreateWarmMilk());
        Register("hot_chocolate", () => FoodDefinitions.CreateHotChocolate());
        Register("ale", () => FoodDefinitions.CreateAle());
        Register("grape_juice", () => FoodDefinitions.CreateGrapeJuice());

        // Crystals (SAO-iconic instant consumables)
        Register("corridor_crystal", () => CrystalDefinitions.CreateCorridorCrystal());
        Register("anti_crystal", () => CrystalDefinitions.CreateAntiCrystal());
        Register("healing_crystal", () => CrystalDefinitions.CreateHealingCrystal());
        Register("high_healing_crystal", () => CrystalDefinitions.CreateHighHealingCrystal());
        Register("antidote_crystal", () => CrystalDefinitions.CreateAntidoteCrystal());
        Register("paralysis_cure_crystal", () => CrystalDefinitions.CreateParalysisCureCrystal());
        Register("mirage_sphere", () => CrystalDefinitions.CreateMirageSphere());
        Register("pneuma_flower", () => CrystalDefinitions.CreatePneumaFlower());
        Register("divine_stone_of_returning_soul", () => CrystalDefinitions.CreateDivineStone());
        // Teleport crystals for key canon cities
        Register("teleport_crystal_tolbana", () => CrystalDefinitions.CreateTeleportCrystal("Tolbana"));
        Register("teleport_crystal_urbus", () => CrystalDefinitions.CreateTeleportCrystal("Urbus"));
        Register("teleport_crystal_zumfut", () => CrystalDefinitions.CreateTeleportCrystal("Zumfut"));
        Register("teleport_crystal_rovia", () => CrystalDefinitions.CreateTeleportCrystal("Rovia"));
        Register("teleport_crystal_karluin", () => CrystalDefinitions.CreateTeleportCrystal("Karluin"));
        Register("teleport_crystal_mishe", () => CrystalDefinitions.CreateTeleportCrystal("Mishe"));
        Register("teleport_crystal_algade", () => CrystalDefinitions.CreateTeleportCrystal("Algade"));
        Register("teleport_crystal_lindarth", () => CrystalDefinitions.CreateTeleportCrystal("Lindarth"));
        Register("teleport_crystal_granzam", () => CrystalDefinitions.CreateTeleportCrystal("Granzam"));
        Register("teleport_crystal_collinia", () => CrystalDefinitions.CreateTeleportCrystal("Collinia"));

        // Damage Items
        Register("fire_bomb", () => DamageItemDefinitions.CreateFireBomb());
        Register("poison_vial", () => DamageItemDefinitions.CreatePoisonVial());
        Register("smoke_bomb", () => DamageItemDefinitions.CreateSmokeBomb());
        Register("flash_bomb", () => DamageItemDefinitions.CreateFlashBomb());

        // Mob Drops
        Register("slime_gel", () => MobDropDefinitions.CreateSlimeGel());
        Register("wolf_pelt", () => MobDropDefinitions.CreateWolfPelt());
        Register("dragon_scale", () => MobDropDefinitions.CreateDragonScale());

        // ── Cooking pantry ingredients ───────────────────────────────
        Register("flour",         () => IngredientDefinitions.CreateFlour());
        Register("sugar",         () => IngredientDefinitions.CreateSugar());
        Register("salt",          () => IngredientDefinitions.CreateSalt());
        Register("spice",         () => IngredientDefinitions.CreateSpice());
        Register("eggs",          () => IngredientDefinitions.CreateEggs());
        Register("cream",         () => IngredientDefinitions.CreateCream());
        Register("high_cream",    () => IngredientDefinitions.CreateHighClassCream());
        Register("honey",         () => IngredientDefinitions.CreateHoney());
        Register("milk",          () => IngredientDefinitions.CreateMilk());
        Register("cocoa",         () => IngredientDefinitions.CreateCocoa());
        Register("tea_leaves",    () => IngredientDefinitions.CreateTeaLeaves());
        Register("ginger",        () => IngredientDefinitions.CreateGinger());
        Register("wild_greens",   () => IngredientDefinitions.CreateWildGreens());
        Register("fruit",         () => IngredientDefinitions.CreateFruit());
        Register("elven_herb",    () => IngredientDefinitions.CreateElvenHerb());

        // ── Raw meats / fish ─────────────────────────────────────────
        Register("raw_meat",           () => IngredientDefinitions.CreateRawMeat());
        Register("boar_meat",          () => IngredientDefinitions.CreateBoarMeat());
        Register("wolf_meat",          () => IngredientDefinitions.CreateWolfMeat());
        Register("rabbit_meat",        () => IngredientDefinitions.CreateRabbitMeat());
        Register("fish",               () => IngredientDefinitions.CreateFish());
        Register("ragout_rabbit_meat", () => IngredientDefinitions.CreateRagoutRabbitMeat());

        // ── Canon monster materials ──────────────────────────────────
        Register("nepent_ovule",       () => IngredientDefinitions.CreateNepentOvule());
        Register("boar_hide",          () => IngredientDefinitions.CreateBoarHide());
        Register("boar_tusk",          () => IngredientDefinitions.CreateBoarTusk());
        Register("wolf_pelt_canon",    () => IngredientDefinitions.CreateWolfPeltCanon());
        Register("wolf_fang",          () => IngredientDefinitions.CreateWolfFang());
        Register("kobold_fang",        () => IngredientDefinitions.CreateKoboldFang());
        Register("rusty_blade",        () => IngredientDefinitions.CreateRustyBlade());
        Register("kobold_halberd",     () => IngredientDefinitions.CreateKoboldHalberd());
        Register("wasp_stinger",       () => IngredientDefinitions.CreateWaspStinger());
        Register("taurus_horn",        () => IngredientDefinitions.CreateTaurusHorn());
        Register("ox_hide",            () => IngredientDefinitions.CreateOxHide());
        Register("bullbous_horn",      () => IngredientDefinitions.CreateBullbousHorn());
        Register("treant_heartwood",   () => IngredientDefinitions.CreateTreantHeartwood());
        Register("treant_sap",         () => IngredientDefinitions.CreateTreantSap());
        Register("forest_elf_bow",     () => IngredientDefinitions.CreateForestElfBow());
        Register("bat_wing",           () => IngredientDefinitions.CreateBatWing());
        Register("toxic_spore",        () => IngredientDefinitions.CreateToxicSpore());
        Register("drake_scale",        () => IngredientDefinitions.CreateDrakeScale());
        Register("water_core",         () => IngredientDefinitions.CreateWaterCore());
        Register("crab_claw",          () => IngredientDefinitions.CreateCrabClaw());
        Register("pearl",              () => IngredientDefinitions.CreatePearl());
        Register("ectoplasm",          () => IngredientDefinitions.CreateEctoplasm());
        Register("obsidian_shard",     () => IngredientDefinitions.CreateObsidianShard());
        Register("alpine_pelt",        () => IngredientDefinitions.CreateAlpinePelt());
        Register("iron_ore",           () => IngredientDefinitions.CreateIronOre());
        Register("mithril_trace",      () => IngredientDefinitions.CreateMithrilTrace());
        Register("spider_silk",        () => IngredientDefinitions.CreateSpiderSilk());
        Register("venom_gland",        () => IngredientDefinitions.CreateVenomGland());
        Register("frost_shard",        () => IngredientDefinitions.CreateFrostShard());
        Register("scorpion_tail",      () => IngredientDefinitions.CreateScorpionTail());
        Register("mammoth_tusk",       () => IngredientDefinitions.CreateMammothTusk());
        Register("kingly_antler",      () => IngredientDefinitions.CreateKinglyAntler());
        Register("crystallite_ingot",  () => IngredientDefinitions.CreateCrystalliteIngot());
        Register("ancient_scale",      () => IngredientDefinitions.CreateAncientScale());
        Register("dragon_heart",       () => IngredientDefinitions.CreateDragonHeart());
        Register("flame_core",         () => IngredientDefinitions.CreateFlameCore());
        Register("hollow_essence",     () => IngredientDefinitions.CreateHollowEssence());
        Register("corrupted_scale",    () => IngredientDefinitions.CreateCorruptedScale());
        Register("seraph_feather",     () => IngredientDefinitions.CreateSeraphFeather());
        Register("cardinal_shard",     () => IngredientDefinitions.CreateCardinalShard());
        Register("immortal_fragment",  () => IngredientDefinitions.CreateImmortalFragment());
        Register("ogres_cleaver",      () => IngredientDefinitions.CreateOgresCleaver());
    }

    private static void Register(string id, Func<BaseItem> factory) => _registry[id] = factory;

    // Recreate an item from its DefinitionId. Returns null if the ID is unknown.
    public static BaseItem? Create(string definitionId) =>
        _registry.TryGetValue(definitionId, out var factory) ? factory() : null;
}
