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
        Register("tough_anneal_blade",  () => OneHandedSwordDefinitions.CreateToughAnnealBlade());
        Register("pitch_black_anneal_blade", () => OneHandedSwordDefinitions.CreatePitchBlackAnnealBlade());
        Register("sword_breaker",       () => OneHandedSwordDefinitions.CreateSwordBreaker());
        Register("queens_knightsword",  () => OneHandedSwordDefinitions.CreateQueensKnightsword());
        Register("azure_sky_blade",     () => OneHandedSwordDefinitions.CreateAzureSkyBlade());
        Register("elucidator",          () => OneHandedSwordDefinitions.CreateElucidator());
        Register("dark_repulser",       () => OneHandedSwordDefinitions.CreateDarkRepulser());
        Register("liberator",           () => OneHandedSwordDefinitions.CreateLiberator());
        // HR Evolution Chain (1H Sword): Final Espada -> Asmodeus -> Final Avalanche -> Tyrfing
        Register("final_espada",        () => OneHandedSwordDefinitions.CreateFinalEspada());
        Register("asmodeus",            () => OneHandedSwordDefinitions.CreateAsmodeus());
        Register("final_avalanche",     () => OneHandedSwordDefinitions.CreateFinalAvalanche());
        Register("tyrfing",             () => OneHandedSwordDefinitions.CreateTyrfing());
        Register("sword_of_eventide",   () => OneHandedSwordDefinitions.CreateSwordOfEventide());
        Register("blue_boar",           () => OneHandedSwordDefinitions.CreateBlueBoar());
        Register("crimson_longsword",   () => OneHandedSwordDefinitions.CreateCrimsonLongsword());
        Register("remains_heart",       () => OneHandedSwordDefinitions.CreateRemainsHeart());
        // Alicization Lycoris Divine Beast drops (1H Sword)
        Register("savage_squall",       () => OneHandedSwordDefinitions.CreateSavageSquall());
        Register("void_eater",          () => OneHandedSwordDefinitions.CreateVoidEater());
        // Hollow Fragment / Infinity Moment Legendaries (1H Sword)
        Register("eurynomes_holy_sword",  () => OneHandedSwordDefinitions.CreateEurynomesHolySword());
        Register("fiendblade_deathbringer", () => OneHandedSwordDefinitions.CreateFiendbladeDeathbringer());
        Register("fayblade_tizona",       () => OneHandedSwordDefinitions.CreateFaybladeTizona());
        Register("godblade_dragonslayer", () => OneHandedSwordDefinitions.CreateGodbladeDragonslayer());
        // Divine Objects (1H Sword)
        Register("night_sky_sword",      () => OneHandedSwordDefinitions.CreateNightSkySword());
        Register("blue_rose_sword",      () => OneHandedSwordDefinitions.CreateBlueRoseSword());
        Register("fragrant_olive_sword", () => OneHandedSwordDefinitions.CreateFragrantOliveSword());
        Register("time_piercing_sword",  () => OneHandedSwordDefinitions.CreateTimePiercingSword());
        Register("black_lily_sword",     () => OneHandedSwordDefinitions.CreateBlackLilySword());

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
        // HR Evolution Chain (Rapier): Prima Sabre -> Pentagramme -> Charadrios -> Hexagramme
        Register("prima_sabre",         () => RapierDefinitions.CreatePrimaSabre());
        Register("pentagramme",         () => RapierDefinitions.CreatePentagramme());
        Register("charadrios",          () => RapierDefinitions.CreateCharadrios());
        Register("hexagramme",          () => RapierDefinitions.CreateHexagramme());
        Register("radiant_light",       () => RapierDefinitions.CreateRadiantLight());
        Register("mothers_rosario",     () => RapierDefinitions.CreateMothersRosario());
        // Divine Objects (Rapier)
        Register("heaven_piercing_blade", () => RapierDefinitions.CreateHeavenPiercingBlade());
        // Alicization Lycoris Divine Beast drops (Rapier)
        Register("midnight_rain",       () => RapierDefinitions.CreateMidnightRain());

        // ── Daggers ──────────────────────────────────────────────────
        Register("rusty_dagger",        () => DaggerDefinitions.CreateRustyDagger());
        Register("steel_dagger",        () => DaggerDefinitions.CreateSteelDagger());
        Register("mythril_dagger",      () => DaggerDefinitions.CreateMythrilDagger());
        Register("adamantite_dagger",   () => DaggerDefinitions.CreateAdamantiteDagger());
        Register("celestial_dagger",    () => DaggerDefinitions.CreateCelestialDagger());
        Register("assassin_dagger",     () => DaggerDefinitions.CreateAssassinDagger());
        Register("snow_warheit",        () => DaggerDefinitions.CreateSnowWarheit());
        Register("mate_chopper",        () => DaggerDefinitions.CreateMateChopper());
        // HR Evolution Chain (Dagger): Heated Razor -> Valkyrie -> Misericorde -> The Iron Maiden
        Register("heated_razor",        () => DaggerDefinitions.CreateHeatedRazor());
        Register("valkyrie",            () => DaggerDefinitions.CreateValkyrie());
        Register("misericorde",         () => DaggerDefinitions.CreateMisericorde());
        Register("iron_maiden_dagger",  () => DaggerDefinitions.CreateTheIronMaiden());
        Register("argos_claws",         () => DaggerDefinitions.CreateArgosClaws());
        Register("stout_brave",         () => DaggerDefinitions.CreateStoutBrave());
        // Alicization Lycoris Divine Beast drops (Dagger)
        Register("phantasmagoria",      () => DaggerDefinitions.CreatePhantasmagoria());

        // ── Two-Handed Swords ────────────────────────────────────────
        Register("iron_greatsword",       () => TwoHandedSwordDefinitions.CreateIronGreatsword());
        Register("steel_greatsword",      () => TwoHandedSwordDefinitions.CreateSteelGreatsword());
        Register("mythril_greatsword",    () => TwoHandedSwordDefinitions.CreateMythrilGreatsword());
        Register("adamantite_greatsword", () => TwoHandedSwordDefinitions.CreateAdamantiteGreatsword());
        Register("celestial_greatsword",  () => TwoHandedSwordDefinitions.CreateCelestialGreatsword());
        Register("tyrant_dragon",         () => TwoHandedSwordDefinitions.CreateTyrantDragon());
        // HR Evolution Chain (2H Sword): Matter Dissolver -> Titan's Blade -> Ifrit -> Ascalon
        Register("matter_dissolver",      () => TwoHandedSwordDefinitions.CreateMatterDissolver());
        Register("titans_blade",          () => TwoHandedSwordDefinitions.CreateTitansBlade());
        Register("ifrit",                 () => TwoHandedSwordDefinitions.CreateIfrit());
        Register("ascalon",               () => TwoHandedSwordDefinitions.CreateAscalon());
        Register("verdant_lord",          () => TwoHandedSwordDefinitions.CreateVerdantLord());
        // Hollow Fragment / Infinity Moment Legendaries (2H Sword)
        Register("saintblade_durandal",   () => TwoHandedSwordDefinitions.CreateSaintbladeDurandal());
        Register("stigmablade_arondight", () => TwoHandedSwordDefinitions.CreateStigmabladeArondight());
        Register("demonblade_gram",       () => TwoHandedSwordDefinitions.CreateDemonbladeGram());
        // Alicization Lycoris Divine Beast drops (2H Sword)
        Register("demonblade_crimson_stream", () => TwoHandedSwordDefinitions.CreateDemonbladeCrimsonStream());

        // ── Katanas ──────────────────────────────────────────────────
        Register("iron_katana",         () => KatanaDefinitions.CreateIronKatana());
        Register("steel_katana",        () => KatanaDefinitions.CreateSteelKatana());
        Register("mythril_katana",      () => KatanaDefinitions.CreateMythrilKatana());
        Register("adamantite_katana",   () => KatanaDefinitions.CreateAdamantiteKatana());
        Register("celestial_katana",    () => KatanaDefinitions.CreateCelestialKatana());
        Register("karakurenai",         () => KatanaDefinitions.CreateKarakurenai());
        // HR Evolution Chain (Katana): Matamon -> Shishi-Otoshi -> Shichishito -> Masamune
        Register("matamon",             () => KatanaDefinitions.CreateMatamon());
        Register("shishi_otoshi",       () => KatanaDefinitions.CreateShishiOtoshi());
        Register("shichishito",         () => KatanaDefinitions.CreateShichishito());
        Register("masamune",            () => KatanaDefinitions.CreateMasamune());
        Register("soul_eater",          () => KatanaDefinitions.CreateSoulEater());
        Register("kagenui",             () => KatanaDefinitions.CreateKagenui());
        // Hollow Fragment / Infinity Moment Legendaries (Katana)
        Register("jato_onikirimaru",    () => KatanaDefinitions.CreateJatoOnikirimaru());
        Register("shinto_ama_no_murakumo", () => KatanaDefinitions.CreateShintoAmaNoMurakumo());
        Register("yato_masamune",       () => KatanaDefinitions.CreateYatoMasamune());
        // Alicization Lycoris Divine Beast drops (Katana)
        Register("midnight_sun",        () => KatanaDefinitions.CreateMidnightSun());

        // ── Axes ─────────────────────────────────────────────────────
        Register("hand_axe",            () => AxeDefinitions.CreateHandAxe());
        Register("battle_axe",          () => AxeDefinitions.CreateBattleAxe());
        Register("mythril_axe",         () => AxeDefinitions.CreateMythrilAxe());
        Register("adamantite_axe",      () => AxeDefinitions.CreateAdamantiteAxe());
        Register("celestial_axe",       () => AxeDefinitions.CreateCelestialAxe());
        Register("pale_edge",           () => AxeDefinitions.CreatePaleEdge());
        // HR Evolution Chain (Axe): Bardiche -> Archaic Murder -> Nidhogg's Fang -> Ouroboros
        Register("bardiche",             () => AxeDefinitions.CreateBardiche());
        Register("archaic_murder",       () => AxeDefinitions.CreateArchaicMurder());
        Register("nidhoggs_fang",        () => AxeDefinitions.CreateNidhoggsFang());
        Register("ouroboros",            () => AxeDefinitions.CreateOuroboros());
        Register("samurai_axe",          () => AxeDefinitions.CreateSamuraiAxe());
        Register("ochigaitou",           () => AxeDefinitions.CreateOchigaitou());
        // Hollow Fragment / Infinity Moment Legendaries (Axe)
        Register("ragnaroks_bane_headsman", () => AxeDefinitions.CreateRagnaroksBaneHeadsman());

        // ── Maces ────────────────────────────────────────────────────
        Register("wooden_club",          () => MaceDefinitions.CreateWoodenClub());
        Register("iron_mace",            () => MaceDefinitions.CreateIronMace());
        Register("mythril_mace",         () => MaceDefinitions.CreateMythrilMace());
        Register("adamantite_mace",      () => MaceDefinitions.CreateAdamantiteMace());
        Register("celestial_mace",       () => MaceDefinitions.CreateCelestialMace());
        Register("minotaur_warhammer",   () => MaceDefinitions.CreateMinotaurWarhammer());
        // HR Evolution Chain (Mace): Lunatic Press -> Nemesis -> Yggdrasil -> Mjolnir
        Register("lunatic_press",        () => MaceDefinitions.CreateLunaticPress());
        Register("nemesis",              () => MaceDefinitions.CreateNemesis());
        Register("yggdrasil",            () => MaceDefinitions.CreateYggdrasil());
        Register("mjolnir",              () => MaceDefinitions.CreateMjolnir());
        Register("mace_of_lord",         () => MaceDefinitions.CreateMaceOfLord());
        // Hollow Fragment / Infinity Moment Legendaries (Mace)
        Register("mace_of_asclepius",    () => MaceDefinitions.CreateMaceOfAsclepius());
        Register("infinite_ouroboros",   () => MaceDefinitions.CreateInfiniteOuroboros());
        Register("starmace_elysium",     () => MaceDefinitions.CreateStarmaceElysium());
        // Alicization Lycoris Divine Beast drops (Mace)
        Register("cactus_bludgeon",      () => MaceDefinitions.CreateCactusBludgeon());

        // ── Spears ───────────────────────────────────────────────────
        Register("wooden_spear",         () => SpearDefinitions.CreateWoodenSpear());
        Register("iron_spear",           () => SpearDefinitions.CreateIronSpear());
        Register("mythril_spear",        () => SpearDefinitions.CreateMythrilSpear());
        Register("adamantite_spear",     () => SpearDefinitions.CreateAdamantiteSpear());
        Register("celestial_spear",      () => SpearDefinitions.CreateCelestialSpear());
        Register("guilty_thorn",         () => SpearDefinitions.CreateGuiltyThorn());
        Register("anubis_spear",         () => SpearDefinitions.CreateAnubisSpear());
        // HR Evolution Chain (Spear): Heart Piercer -> Trishula -> Vijaya -> Caladbolg
        Register("heart_piercer",        () => SpearDefinitions.CreateHeartPiercer());
        Register("trishula",             () => SpearDefinitions.CreateTrishula());
        Register("vijaya",               () => SpearDefinitions.CreateVijaya());
        Register("caladbolg",            () => SpearDefinitions.CreateCaladbolg());
        // Hollow Fragment / Infinity Moment Legendaries (Spear)
        Register("demonspear_gae_bolg",  () => SpearDefinitions.CreateDemonspearGaeBolg());
        Register("saintspear_rhongomyniad", () => SpearDefinitions.CreateSaintspearRhongomyniad());
        Register("godspear_gungnir",     () => SpearDefinitions.CreateGodspearGungnir());

        // ── Bows ─────────────────────────────────────────────────────
        Register("short_bow",            () => BowDefinitions.CreateShortBow());
        Register("long_bow",             () => BowDefinitions.CreateLongBow());
        Register("mythril_bow",          () => BowDefinitions.CreateMythrilBow());
        Register("adamantite_bow",       () => BowDefinitions.CreateAdamantiteBow());
        Register("celestial_bow",        () => BowDefinitions.CreateCelestialBow());
        Register("tias_longbow",         () => BowDefinitions.CreateTiasLongbow());
        // Divine Objects (Bow)
        Register("conflagrant_flame_bow", () => BowDefinitions.CreateConflagrantFlameBow());
        // Alicization Lycoris Divine Beast drops (Bow)
        Register("starfall",             () => BowDefinitions.CreateStarfall());

        // ── Scimitars (curved blades) ────────────────────────────────
        Register("iron_scimitar",        () => ScimitarDefinitions.CreateIronScimitar());
        Register("steel_scimitar",       () => ScimitarDefinitions.CreateSteelScimitar());
        Register("mythril_scimitar",     () => ScimitarDefinitions.CreateMythrilScimitar());
        Register("adamantite_scimitar",  () => ScimitarDefinitions.CreateAdamantiteScimitar());
        Register("celestial_scimitar",   () => ScimitarDefinitions.CreateCelestialScimitar());
        // HR Evolution Chain (Scimitar): Moonstruck Saber -> Diablo Esperanza -> Iblis -> Satanachia
        Register("moonstruck_saber",     () => ScimitarDefinitions.CreateMoonstruckSaber());
        Register("diablo_esperanza",     () => ScimitarDefinitions.CreateDiabloEsperanza());
        Register("iblis",                () => ScimitarDefinitions.CreateIblis());
        Register("satanachia",           () => ScimitarDefinitions.CreateSatanachia());

        // ── Claws (dual-fisted martial) ──────────────────────────────
        Register("iron_claws",           () => ClawsDefinitions.CreateIronClaws());
        Register("steel_claws",          () => ClawsDefinitions.CreateSteelClaws());
        Register("mythril_claws",        () => ClawsDefinitions.CreateMythrilClaws());
        Register("adamantite_claws",     () => ClawsDefinitions.CreateAdamantiteClaws());
        Register("celestial_claws",      () => ClawsDefinitions.CreateCelestialClaws());

        // ── Scythes (heavy reach, death-themed) ──────────────────────
        Register("iron_scythe",          () => ScytheDefinitions.CreateIronScythe());
        Register("steel_scythe",         () => ScytheDefinitions.CreateSteelScythe());
        Register("mythril_scythe",       () => ScytheDefinitions.CreateMythrilScythe());
        Register("adamantite_scythe",    () => ScytheDefinitions.CreateAdamantiteScythe());
        Register("celestial_scythe",     () => ScytheDefinitions.CreateCelestialScythe());

        // ── Integral Factor weapons ──────────────────────────────────
        // Canon named-series weapons from SAO: Integral Factor (Bandai Namco).
        // Series ladder: Integral (F14 Epic) -> Nox (F25 Epic) ->
        //                Rosso (F61 Legendary) -> Yasha (F87 Legendary) ->
        //                Gaou (F90+ Legendary).
        // Agent 2 handles field-boss wiring + guaranteed drops for these.
        // Anneal Blade line (F1-10) is registered inline in the 1H-sword block above.

        // Integral Series (F14 Epic) — 4 weapons + 1 shield
        Register("bow_integral_arc_angel",   () => BowDefinitions.CreateIntegralArcAngel());
        Register("ohs_integral_radgrid",     () => OneHandedSwordDefinitions.CreateIntegralRadgrid());
        Register("rap_integral_gusion",      () => RapierDefinitions.CreateIntegralGusion());
        Register("ths_integral_after_glow",  () => TwoHandedSwordDefinitions.CreateIntegralAfterGlow());

        // Nox Series (F25 Epic) — 5 weapons + 1 shield
        Register("dag_nox_nocturne",         () => DaggerDefinitions.CreateNoxNocturne());
        Register("ohs_nox_radgrid",          () => OneHandedSwordDefinitions.CreateNoxRadgrid());
        Register("rap_nox_gusion",           () => RapierDefinitions.CreateNoxGusion());
        Register("bow_nox_arc_angel",        () => BowDefinitions.CreateNoxArcAngel());
        Register("ths_nox_after_glow",       () => TwoHandedSwordDefinitions.CreateNoxAfterGlow());

        // Rosso Series (F61 Legendary) — 5 weapons + 1 shield
        Register("ohs_rosso_forneus",        () => OneHandedSwordDefinitions.CreateRossoForneus());
        Register("bow_rosso_albatross",      () => BowDefinitions.CreateRossoAlbatross());
        Register("spr_rosso_sigrun",         () => SpearDefinitions.CreateRossoSigrun());
        Register("rap_rosso_rhapsody",       () => RapierDefinitions.CreateRossoRhapsody());
        Register("axe_rosso_dominion",       () => AxeDefinitions.CreateRossoDominion());

        // Yasha Series (F87 Legendary, moved from F85 per collision) — 3 weapons + 1 shield
        Register("ohs_yasha_astaroth",       () => OneHandedSwordDefinitions.CreateYashaAstaroth());
        Register("kat_yasha_oratorio",       () => KatanaDefinitions.CreateYashaOratorio());
        Register("dag_yasha_envy",           () => DaggerDefinitions.CreateYashaEnvy());

        // Gaou Series (F90+ Legendary) — 2 weapons + 1 shield
        Register("ohs_gaou_reginleifr",      () => OneHandedSwordDefinitions.CreateGaouReginleifr());
        Register("kat_gaou_oratorio",        () => KatanaDefinitions.CreateGaouOratorio());

        // ── Hollow Fragment Endgame Expansion (39 weapons) ─────────
        // Implement System gaps (8): F80-F99 canon slots not filled by priority-3 pass.
        Register("sci_arcaneblade_soul_binder",    () => ScimitarDefinitions.CreateArcanebladeSoulBinder());
        Register("sci_fellblade_ruinous_doom",     () => ScimitarDefinitions.CreateFellbladeRuinousDoom());
        Register("sci_deathglutton_epetamu",       () => ScimitarDefinitions.CreateDeathgluttonEpetamu());
        Register("rap_spiralblade_rendering_fail", () => RapierDefinitions.CreateSpiralbladeRenderingFail());
        Register("rap_glimmerblade_banishing_ray", () => RapierDefinitions.CreateGlimmerbladeBanishingRay());
        Register("axe_crusher_bond_cyclone",       () => AxeDefinitions.CreateCrusherBondCyclone());
        Register("axe_fellaxe_demons_scythe",      () => AxeDefinitions.CreateFellaxeDemonsScythe());
        Register("ohs_aurumbrand_hauteclaire",     () => OneHandedSwordDefinitions.CreateAurumbrandHauteclaire());

        // Hollow Area Uniques (5): spread across floor bands as rare drops.
        Register("ohs_traitorblade_argute_brand",  () => OneHandedSwordDefinitions.CreateTraitorbladeArguteBrand());
        Register("bow_shroudbow_star_stitcher",    () => BowDefinitions.CreateShroudbowStarStitcher());
        Register("scy_reaper_scythe",              () => ScytheDefinitions.CreateReaperScythe());
        Register("ohs_velocious_brain",            () => OneHandedSwordDefinitions.CreateFakeSwordVelociousBrain());
        Register("ths_saintblade_ragnarok",        () => TwoHandedSwordDefinitions.CreateSaintbladeRagnarok());

        // Avatar Weapons (8): Last-Attack Bonus drops from F70+ field bosses.
        Register("rap_ishvalca_avatar",            () => RapierDefinitions.CreateIshvalcaAvatar());
        Register("dag_genocide_avatar",            () => DaggerDefinitions.CreateGenocideAvatar());
        Register("sci_saphir_avatar",              () => ScimitarDefinitions.CreateSaphirAvatar());
        Register("kat_burning_haze_avatar",        () => KatanaDefinitions.CreateBurningHazeAvatar());
        Register("axe_lord_burster_avatar",        () => AxeDefinitions.CreateLordBursterAvatar());
        Register("ths_absoludia_avatar",           () => TwoHandedSwordDefinitions.CreateAbsoludiaAvatar());
        Register("spr_asleigeon_avatar",           () => SpearDefinitions.CreateAsleigeonAvatar());
        Register("mce_ijelfur_avatar",             () => MaceDefinitions.CreateIjelfurAvatar());

        // Lisbeth Rarity 6 Crafted (18): Lisbeth-craft-only at Lindarth F48.
        Register("ohs_variable_v_vice",            () => OneHandedSwordDefinitions.CreateVariableVVice());
        Register("ohs_liberator_astral_legion",    () => OneHandedSwordDefinitions.CreateLiberatorAstralLegion());
        Register("ohs_marginless_blade",           () => OneHandedSwordDefinitions.CreateMarginlessBlade());
        Register("ths_ogreblade_over_the_cross",   () => TwoHandedSwordDefinitions.CreateOgrebladeOverTheCross());
        Register("ths_deliverer_majestic_lord",    () => TwoHandedSwordDefinitions.CreateDelivererMajesticLord());
        Register("ths_ambitious_juggernaut",       () => TwoHandedSwordDefinitions.CreateAmbitiousJuggernaut());
        Register("rap_championfoil_radiant_chariot", () => RapierDefinitions.CreateChampionfoilRadiantChariot());
        Register("rap_glimmerspine_silver_bullet", () => RapierDefinitions.CreateGlimmerspineSilverBullet());
        Register("sci_crescentblade_original_sin", () => ScimitarDefinitions.CreateCrescentbladeOriginalSin());
        Register("dag_notes_end_trinity",          () => DaggerDefinitions.CreateNotesEndTrinity());
        Register("kat_godslayer_tattered_hope",    () => KatanaDefinitions.CreateGodslayerTatteredHope());
        Register("kat_avidya_samsara_blade",       () => KatanaDefinitions.CreateAvidyaSamsaraBlade());
        Register("spr_heavenslance_elpis_order",   () => SpearDefinitions.CreateHeavenslanceElpisOrder());
        Register("mce_dictators_punisher",         () => MaceDefinitions.CreateDictatorsPunisher());
        Register("mce_photon_hammer_xp_smasher",   () => MaceDefinitions.CreatePhotonHammerXPSmasher());
        Register("axe_hecatomb_giga_disaster",     () => AxeDefinitions.CreateHecatombAxeGigaDisaster());
        Register("axe_ingurgitator_belzericht",    () => AxeDefinitions.CreateIngurgitatorBelzericht());
        Register("scy_eldark_radius_sigma",        () => ScytheDefinitions.CreateEldarkRadiusSigma());

        // ── Refinement Ingots (Agent 3 partition — see IF_EXPANSION_SCOUT §9) ──
        Register("sharpening_ingot",  () => IngotDefinitions.CreateSharpeningIngot());
        Register("warden_ingot",      () => IngotDefinitions.CreateWardenIngot());
        Register("hunter_ingot",      () => IngotDefinitions.CreateHunterIngot());
        Register("lunar_ingot",       () => IngotDefinitions.CreateLunarIngot());
        Register("keen_ingot",        () => IngotDefinitions.CreateKeenIngot());
        Register("guardian_ingot",    () => IngotDefinitions.CreateGuardianIngot());
        Register("swiftstrike_ingot", () => IngotDefinitions.CreateSwiftstrikeIngot());
        Register("spellbind_ingot",   () => IngotDefinitions.CreateSpellbindIngot());
        Register("chimeric_ingot",    () => IngotDefinitions.CreateChimericIngot());
        Register("sovereign_ingot",   () => IngotDefinitions.CreateSovereignIngot());
        Register("vanguard_ingot",    () => IngotDefinitions.CreateVanguardIngot());
        Register("astral_ingot",      () => IngotDefinitions.CreateAstralIngot());

        // ── Shields ──────────────────────────────────────────────────
        Register("wooden_shield", () => ShieldDefinitions.CreateWoodenShield());
        Register("iron_shield",   () => ShieldDefinitions.CreateIronShield());
        // IF named-series shields
        Register("shd_fermat",        () => ShieldDefinitions.CreateFermat());
        Register("shd_nox_fermat",    () => ShieldDefinitions.CreateNoxFermat());
        Register("shd_rosso_aegis",   () => ShieldDefinitions.CreateRossoAegis());
        Register("shd_yasha_kavacha", () => ShieldDefinitions.CreateYashaKavacha());
        Register("shd_gaou_tatari",   () => ShieldDefinitions.CreateGaouTatari());

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

        // ── Evolution Catalysts (Priority 5 HR chains) ───────────────
        // One material per weapon-type evolution chain. Upgrade recipes wired in Phase B.
        Register("demonic_sigil",    () => EvolutionMaterialDefinitions.CreateDemonicSigil());
        Register("geometric_shard",  () => EvolutionMaterialDefinitions.CreateGeometricShard());
        Register("infernal_gem",     () => EvolutionMaterialDefinitions.CreateInfernalGem());
        Register("valkyrie_feather", () => EvolutionMaterialDefinitions.CreateValkyrieFeather());
        Register("lunar_core",       () => EvolutionMaterialDefinitions.CreateLunarCore());
        Register("oni_ash",          () => EvolutionMaterialDefinitions.CreateOniAsh());
        Register("titan_fragment",   () => EvolutionMaterialDefinitions.CreateTitanFragment());
        Register("nidhogg_scale",    () => EvolutionMaterialDefinitions.CreateNidhoggScale());
        Register("trishula_tip",     () => EvolutionMaterialDefinitions.CreateTrishulaTip());

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
