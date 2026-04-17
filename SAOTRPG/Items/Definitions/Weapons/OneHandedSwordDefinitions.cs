using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;


// Static registry of all one-handed sword weapons.
public static class OneHandedSwordDefinitions
{
    private static Weapon Make(string id, string name, int value, string rarity, int durability,
        int level, int baseDmg, StatModifierCollection bonuses, string? specialEffect = null)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = durability, RequiredLevel = level,
            EquipmentType = "Weapon", WeaponType = "One-Handed Sword",
            BaseDamage = baseDmg, AttackSpeed = 1, Range = 1,
            SpecialEffect = specialEffect, Bonuses = bonuses,
        };

    private static StatModifierCollection B() => new();

    public static Weapon CreateIronSword() => Make("iron_sword", "Iron Sword", 100, "Common", 50, 1, 10,
        B().Add(StatType.Attack, 8));

    public static Weapon CreateSteelSword() => Make("steel_sword", "Steel Sword", 250, "Uncommon", 75, 10, 25,
        B().Add(StatType.Attack, 15).Add(StatType.Strength, 5));

    public static Weapon CreateMythrilSword() => Make("mythril_sword", "Mythril Sword", 800, "Rare", 100, 25, 45,
        B().Add(StatType.Attack, 25).Add(StatType.Strength, 8));

    public static Weapon CreateAdamantiteSword() => Make("adamantite_sword", "Adamantite Sword", 2500, "Epic", 150, 50, 80,
        B().Add(StatType.Attack, 45).Add(StatType.Strength, 15).Add(StatType.Dexterity, 8));

    public static Weapon CreateCelestialBlade() => Make("celestial_blade", "Celestial Blade", 6000, "Legendary", 200, 75, 130,
        B().Add(StatType.Attack, 70).Add(StatType.Strength, 20).Add(StatType.Agility, 10));

    // --- Named / SAO Legendary Weapons ---

    public static Weapon CreateAnnealBlade() => Make("anneal_blade", "Anneal Blade", 200, "Rare", 60, 5, 14,
        B().Add(StatType.Attack, 12).Add(StatType.Strength, 3));

    public static Weapon CreateSwordBreaker() => Make("sword_breaker", "Sword Breaker", 400, "Uncommon", 70, 15, 20,
        B().Add(StatType.Attack, 10).Add(StatType.Dexterity, 5), "ParryChance+10");

    public static Weapon CreateQueensKnightsword() => Make("queens_knightsword", "Queen's Knightsword", 1200, "Epic", 100, 20, 30,
        B().Add(StatType.Attack, 20).Add(StatType.Strength, 8), "ComboBonus+50");

    public static Weapon CreateAzureSkyBlade() => Make("azure_sky_blade", "Azure Sky Blade", 1800, "Rare", 120, 30, 50,
        B().Add(StatType.Attack, 30).Add(StatType.Agility, 8));

    public static Weapon CreateElucidator() => Make("elucidator", "Elucidator", 10000, "Legendary", 200, 50, 90,
        B().Add(StatType.Attack, 50).Add(StatType.Strength, 18).Add(StatType.Agility, 10), "SkillCooldown-1");

    public static Weapon CreateDarkRepulser() => Make("dark_repulser", "Dark Repulser", 10000, "Legendary", 200, 50, 85,
        B().Add(StatType.Attack, 48).Add(StatType.Dexterity, 12).Add(StatType.Strength, 12), "CritHeal+5");

    public static Weapon CreateLiberator() => Make("liberator", "Liberator", 15000, "Legendary", 250, 75, 140,
        B().Add(StatType.Attack, 65).Add(StatType.Strength, 25), "BlockChance+15");

    // ── Hollow Realization Evolution Chain (1H Sword) ───────────────
    // Final Espada -> Asmodeus -> Final Avalanche -> Tyrfing.

    // Final Espada, gleaming duel-sword. T1 of the Tyrfing chain.
    public static Weapon CreateFinalEspada() => Make("final_espada", "Final Espada", 1900, "Rare", 120, 15, 62,
        B().Add(StatType.Attack, 35).Add(StatType.Strength, 10));

    // Asmodeus, demon-prince blade wreathed in sin. T2 of the Tyrfing chain.
    public static Weapon CreateAsmodeus() => Make("asmodeus", "Asmodeus", 5400, "Epic", 160, 35, 108,
        B().Add(StatType.Attack, 55).Add(StatType.Strength, 15).Add(StatType.Dexterity, 8), "CritRate+10");

    // Final Avalanche, cascading-strike longsword. T3 of the Tyrfing chain.
    public static Weapon CreateFinalAvalanche() => Make("final_avalanche", "Final Avalanche", 12500, "Legendary", 210, 60, 148,
        B().Add(StatType.Attack, 75).Add(StatType.Strength, 22).Add(StatType.Dexterity, 14), "SkillCooldown-1");

    // T4 Divine of the One-Handed Sword evolution chain.
    public static Weapon CreateTyrfing() => Make("tyrfing", "Tyrfing", 20000, "Divine", 999, 80, 160,
        B().Add(StatType.Attack, 80).Add(StatType.Strength, 20).Add(StatType.Dexterity, 15), "SkillDamage+25");

    // F3 Elf War reward. Dark-elven steel, faint twilight glow.
    public static Weapon CreateSwordOfEventide() => Make("sword_of_eventide", "Sword of Eventide", 2200, "Rare", 120, 6, 28,
        B().Add(StatType.Attack, 16).Add(StatType.Agility, 6).Add(StatType.Dexterity, 4), "CritRate+10");

    // Alternate F3-4 Elf War reward. Dark blue blade with a boar-tusk guard.
    public static Weapon CreateBlueBoar() => Make("blue_boar", "Blue Boar", 1800, "Rare", 110, 8, 32,
        B().Add(StatType.Attack, 18).Add(StatType.Strength, 8), "BackstabDmg+30");

    // Drop from Laughing Coffin-aligned PKers. Bloody red, jagged.
    public static Weapon CreateCrimsonLongsword() => Make("crimson_longsword", "Crimson Longsword", 4200, "Epic", 150, 25, 62,
        B().Add(StatType.Attack, 34).Add(StatType.Strength, 12).Add(StatType.Dexterity, 6), "Bleed+15");

    // Remains Heart — Lisbeth's masterwork (Infinity Moment / Hollow Fragment canon).
    // Her character-episode questline ends in this weapon — the sole top-tier
    // Legendary that is fully enhanceable in canon. Strongest enhanceable 1H sword.
    public static Weapon CreateRemainsHeart() => Make("remains_heart", "Remains Heart", 22000, "Legendary", 240, 85, 158,
        B().Add(StatType.Attack, 78).Add(StatType.Strength, 22).Add(StatType.Dexterity, 12).Add(StatType.Defense, 10), "SkillCooldown-1");

    // Alicization Lycoris Divine Beast drops (Priority 4). Dropped by non-canon
    // floor bosses on F1-F50 as their canonical hand-placed reward.

    // F17 Gelidus the Frozen Colossus drops. Storm-wrapped blade. "Squall" flavor.
    public static Weapon CreateSavageSquall() => Make("savage_squall", "Savage Squall", 9500, "Legendary", 180, 38, 110,
        B().Add(StatType.Attack, 55).Add(StatType.Strength, 18).Add(StatType.Agility, 12), "Slow+15");

    // F30 Primos the World Serpent drops. Void-blackened blade, devours skill time.
    public static Weapon CreateVoidEater() => Make("void_eater", "Void Eater", 15000, "Legendary", 210, 35, 135,
        B().Add(StatType.Attack, 68).Add(StatType.Strength, 20).Add(StatType.Dexterity, 12).Add(StatType.Agility, 8), "SkillCooldown-1");

    // ── Hollow Fragment / Infinity Moment Legendaries ──────────────
    // Endgame drops from F77-F99 in the Hollow Fragment canon.

    // Hollow Fragment F90 floor-boss weapon. Holy radiance, blessed steel.
    public static Weapon CreateEurynomesHolySword() => Make("eurynomes_holy_sword", "Eurynome's Holy Sword", 18000, "Legendary", 220, 85, 155,
        B().Add(StatType.Attack, 75).Add(StatType.Strength, 22).Add(StatType.Dexterity, 12), "HolyDamage+20");

    // Hollow Fragment F81 implement. Absorbs 20% HP on hit (flavor).
    public static Weapon CreateFiendbladeDeathbringer() => Make("fiendblade_deathbringer", "Fiendblade: Deathbringer", 16000, "Legendary", 200, 78, 148,
        B().Add(StatType.Attack, 72).Add(StatType.Strength, 20).Add(StatType.Dexterity, 10), "Bleed+25");

    // Hollow Fragment F83 implement. High-speed fay-forged blade.
    public static Weapon CreateFaybladeTizona() => Make("fayblade_tizona", "Fayblade: Tizona", 17000, "Legendary", 210, 80, 152,
        B().Add(StatType.Attack, 70).Add(StatType.Speed, 18).Add(StatType.Agility, 15).Add(StatType.Dexterity, 10), "AttackSpeed+2");

    // Hollow Fragment F85 implement. +50% damage versus dragons (flavor).
    public static Weapon CreateGodbladeDragonslayer() => Make("godblade_dragonslayer", "Godblade: Dragonslayer", 19000, "Legendary", 220, 82, 160,
        B().Add(StatType.Attack, 78).Add(StatType.Strength, 25).Add(StatType.Dexterity, 12), "DragonSlayer+50");

    // ── Divine Objects ──────────────────────────────────────────────
    // Above Legendary. Hand-placed only (no random drops). Unbreakable.
    // Bypass enemy block rolls (armor damage reduction still applies).
    // Canon Priority / Sacred Object tier from Alicization arc.

    // Kirito's Priority 46 Divine Object. Wooden blade carved from the Gigas
    // Cedar's top branch. Canonical Underworld weapon — "bypass armor" feel.
    public static Weapon CreateNightSkySword() => Make("night_sky_sword", "Night Sky Sword", 40000, "Divine", 999, 80, 180,
        B().Add(StatType.Attack, 95).Add(StatType.Strength, 28).Add(StatType.Agility, 15).Add(StatType.Dexterity, 12), "ArmorPierce+30");

    // Eugeo's ice-attribute Divine Object. Pure white blade with blue rose guard.
    // Full Control Art freezes a wide field; we model it as high freeze-on-hit.
    public static Weapon CreateBlueRoseSword() => Make("blue_rose_sword", "Blue Rose Sword", 40000, "Divine", 999, 80, 175,
        B().Add(StatType.Attack, 90).Add(StatType.Strength, 25).Add(StatType.Dexterity, 18), "Freeze+20");

    // Alice Synthesis Thirty's Divine Object. Forged from a fragrant olive tree
    // blessed by Goddess Stacia. Full Control Art scatters thousands of petals.
    public static Weapon CreateFragrantOliveSword() => Make("fragrant_olive_sword", "Fragrant Olive Sword", 45000, "Divine", 999, 85, 178,
        B().Add(StatType.Attack, 92).Add(StatType.Strength, 22).Add(StatType.Agility, 14).Add(StatType.Dexterity, 14).Add(StatType.SkillDamage, 15), "HolyAoE+15");

    // Bercouli Synthesis One's Divine Object. Created from the original System
    // Clock of the Underworld — can slice a moment of the past or future.
    public static Weapon CreateTimePiercingSword() => Make("time_piercing_sword", "Time Piercing Sword", 48000, "Divine", 999, 88, 185,
        B().Add(StatType.Attack, 95).Add(StatType.Strength, 30).Add(StatType.Dexterity, 15), "ExecuteThreshold+25");

    // Sheyta Synthesis Twelve's Divine Object. Granted by Administrator;
    // said to cut through any and all things. Severing flavor.
    public static Weapon CreateBlackLilySword() => Make("black_lily_sword", "Black Lily Sword", 50000, "Divine", 999, 90, 190,
        B().Add(StatType.Attack, 100).Add(StatType.Strength, 28).Add(StatType.Dexterity, 22), "SeveringStrike+50");
}
