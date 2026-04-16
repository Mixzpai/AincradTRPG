using SAOTRPG.Items.Materials;

namespace SAOTRPG.Items.Definitions;

// Cooking pantry ingredients (vendor-bought or gathered) + canon mob-drop
// materials. Consumed by CookingSystem recipes and crafting.
public static class IngredientDefinitions
{
    private static MobDrop Make(string id, string name, int value, string rarity = "Common",
        int tier = 1, int maxStacks = 99, string? source = null, float drop = 0f, bool boss = false)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            Quantity = 1, MaxStacks = maxStacks,
            MaterialType = "Ingredient", CraftingTier = tier,
            SourceMonster = source, DropRate = drop, IsBossDrop = boss,
        };

    // ── Cooking pantry (vendor-bought, non-mob) ──────────────────────────
    public static MobDrop CreateFlour()         => Make("flour",         "Flour",         5,  "Common");
    public static MobDrop CreateSugar()         => Make("sugar",         "Sugar",         8,  "Common");
    public static MobDrop CreateSalt()          => Make("salt",          "Salt",          4,  "Common");
    public static MobDrop CreateSpice()         => Make("spice",         "Spice Packet",  12, "Uncommon");
    public static MobDrop CreateEggs()          => Make("eggs",          "Fresh Eggs",    6,  "Common");
    public static MobDrop CreateCream()         => Make("cream",         "Cream",         15, "Uncommon");
    public static MobDrop CreateHighClassCream()=> Make("high_cream",    "High-Class Cream", 120, "Rare", tier: 3, maxStacks: 20);
    public static MobDrop CreateHoney()         => Make("honey",         "Honey",         18, "Uncommon");
    public static MobDrop CreateMilk()          => Make("milk",          "Fresh Milk",    7,  "Common");
    public static MobDrop CreateCocoa()         => Make("cocoa",         "Cocoa",         35, "Uncommon");
    public static MobDrop CreateTeaLeaves()     => Make("tea_leaves",    "Tea Leaves",    10, "Common");
    public static MobDrop CreateGinger()        => Make("ginger",        "Ginger Root",   12, "Common");
    public static MobDrop CreateWildGreens()    => Make("wild_greens",   "Wild Greens",   4,  "Common");
    public static MobDrop CreateFruit()         => Make("fruit",         "Fresh Fruit",   8,  "Common");
    public static MobDrop CreateElvenHerb()     => Make("elven_herb",    "Elven Herb",    60, "Rare",     tier: 3, maxStacks: 20, source: "Dark Elf");

    // ── Raw meats (from beast-type mob drops) ────────────────────────────
    public static MobDrop CreateRawMeat()       => Make("raw_meat",      "Raw Meat",      10, "Common",   source: "Beast", drop: 0.6f);
    public static MobDrop CreateBoarMeat()      => Make("boar_meat",     "Boar Meat",     18, "Common",   source: "Frenzy Boar", drop: 0.7f);
    public static MobDrop CreateWolfMeat()      => Make("wolf_meat",     "Wolf Meat",     14, "Common",   source: "Dire Wolf", drop: 0.5f);
    public static MobDrop CreateRabbitMeat()    => Make("rabbit_meat",   "Rabbit Meat",   12, "Common",   source: "Rabbit", drop: 0.6f);
    public static MobDrop CreateFish()          => Make("fish",          "Fish",          10, "Common",   source: "Water tile");
    public static MobDrop CreateRagoutRabbitMeat()=> Make("ragout_rabbit_meat", "Ragout Rabbit Meat", 2500, "Legendary", tier: 5, maxStacks: 3, source: "Ragout Rabbit", drop: 0.02f);

    // ── Canon monster materials — F1-F5 ──────────────────────────────────
    public static MobDrop CreateNepentOvule()   => Make("nepent_ovule",  "Little Nepent's Ovule", 50,  "Rare",  tier: 2, maxStacks: 10, source: "Three-Pronged Nepent", drop: 0.15f);
    public static MobDrop CreateBoarHide()      => Make("boar_hide",     "Boar Hide",     12, "Common",   source: "Frenzy Boar", drop: 0.6f);
    public static MobDrop CreateBoarTusk()      => Make("boar_tusk",     "Boar Tusk",     22, "Uncommon", source: "Frenzy Boar", drop: 0.3f);
    public static MobDrop CreateWolfPeltCanon() => Make("wolf_pelt_canon","Wolf Pelt",    15, "Common",   source: "Dire Wolf", drop: 0.6f);
    public static MobDrop CreateWolfFang()      => Make("wolf_fang",     "Wolf Fang",     18, "Uncommon", source: "Dire Wolf", drop: 0.3f);
    public static MobDrop CreateKoboldFang()    => Make("kobold_fang",   "Kobold Fang",   8,  "Common",   source: "Ruin Kobold", drop: 0.4f);
    public static MobDrop CreateRustyBlade()    => Make("rusty_blade",   "Rusty Blade",   20, "Common",   source: "Kobold Trooper", drop: 0.4f);
    public static MobDrop CreateKoboldHalberd() => Make("kobold_halberd","Kobold Halberd",40, "Uncommon", source: "Kobold Sentinel", drop: 0.2f);
    public static MobDrop CreateWaspStinger()   => Make("wasp_stinger",  "Wasp Stinger",  14, "Uncommon", source: "Windwasp", drop: 0.3f);
    public static MobDrop CreateTaurusHorn()    => Make("taurus_horn",   "Taurus Horn",   25, "Uncommon", source: "Lesser Taurus", drop: 0.3f);
    public static MobDrop CreateOxHide()        => Make("ox_hide",       "Ox Hide",       16, "Common",   source: "Trembling Ox", drop: 0.6f);
    public static MobDrop CreateBullbousHorn()  => Make("bullbous_horn", "Bullbous Horn", 150,"Rare",     tier: 3, maxStacks: 10, source: "Bullbous Bow", drop: 1.0f);
    public static MobDrop CreateTreantHeartwood()=>Make("treant_heartwood","Treant Heartwood", 80, "Rare", tier: 3, maxStacks: 10, source: "Elder Treant", drop: 0.25f);
    public static MobDrop CreateTreantSap()     => Make("treant_sap",    "Treant Sap",    22, "Uncommon", source: "Treant Sapling", drop: 0.4f);
    public static MobDrop CreateForestElfBow()  => Make("forest_elf_bow","Forest Elf Bow",90, "Rare",     tier: 3, maxStacks: 5,  source: "Forest Elf Scout", drop: 0.1f);
    public static MobDrop CreateBatWing()       => Make("bat_wing",      "Bat Wing",      10, "Common",   source: "Cave Bat", drop: 0.7f);
    public static MobDrop CreateToxicSpore()    => Make("toxic_spore",   "Toxic Spore",   18, "Uncommon", source: "Toadstool Walker", drop: 0.4f);
    public static MobDrop CreateDrakeScale()    => Make("drake_scale",   "Drake Scale",   45, "Uncommon", source: "Water Drake", drop: 0.4f);
    public static MobDrop CreateWaterCore()     => Make("water_core",    "Water Core",    120,"Rare",     tier: 3, maxStacks: 10, source: "Water Drake", drop: 0.15f);
    public static MobDrop CreateCrabClaw()      => Make("crab_claw",     "Crab Claw",     20, "Uncommon", source: "Lakeshore Crab", drop: 0.5f);
    public static MobDrop CreatePearl()         => Make("pearl",         "Pearl",         300,"Rare",     tier: 4, maxStacks: 10, source: "Giant Clam", drop: 0.08f);
    public static MobDrop CreateEctoplasm()     => Make("ectoplasm",     "Ectoplasm",     28, "Uncommon", source: "Water Wight", drop: 0.4f);
    public static MobDrop CreateObsidianShard() => Make("obsidian_shard","Obsidian Shard",40, "Uncommon", source: "Vacant Sentinel", drop: 0.3f);

    // ── Mid-tier materials (F11-F50) ─────────────────────────────────────
    public static MobDrop CreateAlpinePelt()    => Make("alpine_pelt",   "Alpine Pelt",   35, "Uncommon", source: "Alpine Wolf", drop: 0.5f);
    public static MobDrop CreateIronOre()       => Make("iron_ore",      "Iron Ore",      25, "Common",   source: "Miner / mineral"); // also vendor
    public static MobDrop CreateMithrilTrace()  => Make("mithril_trace", "Mithril Trace", 180,"Rare",     tier: 4, maxStacks: 20);
    public static MobDrop CreateSpiderSilk()    => Make("spider_silk",   "Spider Silk",   40, "Uncommon", source: "Giant Spider", drop: 0.4f);
    public static MobDrop CreateVenomGland()    => Make("venom_gland",   "Venom Gland",   55, "Uncommon", source: "Giant Spider", drop: 0.3f);
    public static MobDrop CreateFrostShard()    => Make("frost_shard",   "Frost Shard",   35, "Uncommon", source: "Frost Goblin / Snow Wolf", drop: 0.4f);
    public static MobDrop CreateScorpionTail()  => Make("scorpion_tail", "Scorpion Tail", 48, "Uncommon", source: "Sandstorm Scorpion", drop: 0.4f);
    public static MobDrop CreateMammothTusk()   => Make("mammoth_tusk",  "Mammoth Tusk",  450,"Epic",     tier: 4, maxStacks: 5,  source: "Magnatherium", drop: 1.0f, boss: true);
    public static MobDrop CreateKinglyAntler()  => Make("kingly_antler", "Kingly Antler", 380,"Rare",     tier: 4, maxStacks: 5,  source: "Forest King Stag", drop: 1.0f, boss: true);

    // ── Endgame materials (F51-F100) ─────────────────────────────────────
    public static MobDrop CreateCrystalliteIngot()=>Make("crystallite_ingot", "Crystallite Ingot", 2500, "Legendary", tier: 5, maxStacks: 3, source: "Frost Dragon", drop: 1.0f, boss: true);
    public static MobDrop CreateAncientScale()  => Make("ancient_scale", "Ancient Scale", 280,"Rare",     tier: 4, maxStacks: 10, source: "Ancient Dragon", drop: 0.3f);
    public static MobDrop CreateDragonHeart()   => Make("dragon_heart",  "Dragon Heart",  1500,"Epic",    tier: 5, maxStacks: 3,  source: "Ancient / Unholy Dragon", drop: 0.1f);
    public static MobDrop CreateFlameCore()     => Make("flame_core",    "Flame Core",    180,"Rare",     tier: 4, maxStacks: 10, source: "Flame Elemental", drop: 0.3f);
    public static MobDrop CreateHollowEssence() => Make("hollow_essence","Hollow Essence",220,"Rare",     tier: 4, maxStacks: 10, source: "Hollow mobs", drop: 0.4f);
    public static MobDrop CreateCorruptedScale()=> Make("corrupted_scale","Corrupted Scale", 400, "Epic", tier: 5, maxStacks: 5, source: "Unholy Dragon", drop: 0.2f);
    public static MobDrop CreateSeraphFeather() => Make("seraph_feather","Seraph Feather",420,"Epic",     tier: 5, maxStacks: 5,  source: "Void Seraph", drop: 0.15f);
    public static MobDrop CreateCardinalShard() => Make("cardinal_shard","Cardinal Shard",900,"Legendary",tier: 5, maxStacks: 3,  source: "Cardinal Error", drop: 0.2f);
    public static MobDrop CreateImmortalFragment()=>Make("immortal_fragment","Immortal Fragment", 1800, "Legendary", tier: 5, maxStacks: 3, source: "Immortal Echo", drop: 0.1f);
    public static MobDrop CreateOgresCleaver() => Make("ogres_cleaver", "Ogre's Cleaver", 550, "Epic",    tier: 4, maxStacks: 1,  source: "Ogre Lord", drop: 1.0f, boss: true);
}
