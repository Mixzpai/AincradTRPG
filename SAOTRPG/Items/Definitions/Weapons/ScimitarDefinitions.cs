using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

// Static registry of all scimitar (curved blade) weapons.
// Scimitars: fast slashing curved blades. Distinct from 1H swords — trade
// block chance for attack speed, and have a natural bleed affinity.
// Canon across SAO Hollow Realization, Hollow Fragment, Infinity Moment.
public static class ScimitarDefinitions
{
    private static Weapon Make(string id, string name, int value, string rarity, int durability,
        int level, int baseDmg, StatModifierCollection bonuses, string? specialEffect = null)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = durability, RequiredLevel = level,
            EquipmentType = "Weapon", WeaponType = "Scimitar",
            BaseDamage = baseDmg, AttackSpeed = 2, Range = 1,
            SpecialEffect = specialEffect, Bonuses = bonuses,
        };

    private static StatModifierCollection B() => new();

    public static Weapon CreateIronScimitar() => Make("iron_scimitar", "Iron Scimitar", 100, "Common", 50, 1, 9,
        B().Add(StatType.Attack, 7).Add(StatType.Dexterity, 3));

    public static Weapon CreateSteelScimitar() => Make("steel_scimitar", "Steel Scimitar", 250, "Uncommon", 75, 10, 22,
        B().Add(StatType.Attack, 13).Add(StatType.Dexterity, 6));

    public static Weapon CreateMythrilScimitar() => Make("mythril_scimitar", "Mythril Scimitar", 800, "Rare", 100, 25, 40,
        B().Add(StatType.Attack, 22).Add(StatType.Dexterity, 10).Add(StatType.Agility, 4));

    public static Weapon CreateAdamantiteScimitar() => Make("adamantite_scimitar", "Adamantite Scimitar", 2500, "Epic", 150, 50, 72,
        B().Add(StatType.Attack, 40).Add(StatType.Dexterity, 14).Add(StatType.Agility, 8));

    public static Weapon CreateCelestialScimitar() => Make("celestial_scimitar", "Celestial Scimitar", 6000, "Legendary", 200, 75, 118,
        B().Add(StatType.Attack, 62).Add(StatType.Dexterity, 18).Add(StatType.Agility, 12));

    // ── Hollow Realization Evolution Chain (Scimitar) ───────────────
    // Moonstruck Saber -> Diablo Esperanza -> Iblis -> Satanachia.

    // Moonstruck Saber, lunar-tempered curved blade. T1 of the Satanachia chain.
    public static Weapon CreateMoonstruckSaber() => Make("moonstruck_saber", "Moonstruck Saber", 1800, "Rare", 120, 15, 62,
        B().Add(StatType.Attack, 35).Add(StatType.Dexterity, 10));

    // Diablo Esperanza, demon-hope saber that drinks blood from each cut. T2 of the Satanachia chain.
    public static Weapon CreateDiabloEsperanza() => Make("diablo_esperanza", "Diablo Esperanza", 5200, "Epic", 160, 35, 105,
        B().Add(StatType.Attack, 55).Add(StatType.Dexterity, 15).Add(StatType.Agility, 8), "Bleed+10");

    // Iblis, scimitar of the fallen prince — severs flesh in a single arc. T3 of the Satanachia chain.
    public static Weapon CreateIblis() => Make("iblis", "Iblis", 12000, "Legendary", 210, 60, 145,
        B().Add(StatType.Attack, 75).Add(StatType.Dexterity, 22).Add(StatType.Agility, 14), "Bleed+20");

    // T4 Divine — Scimitar evolution chain apex.
    // No existing Legendary Scimitar peer; this creates the Scimitar chain endpoint.
    public static Weapon CreateSatanachia() => Make("satanachia", "Satanachia", 40000, "Divine", 999, 80, 165,
        B().Add(StatType.Attack, 85).Add(StatType.Dexterity, 26).Add(StatType.Agility, 18).Add(StatType.Strength, 14), "Bleed+25");

    // ── Hollow Fragment Implement System (Scimitar) ────────────────
    // Canon HF F80-F99 implements filling the Scimitar gaps.

    // HF F80 implement. Arcaneblade — soul-binder curve edge; SP gain on hit (flavor).
    public static Weapon CreateArcanebladeSoulBinder() => Make("sci_arcaneblade_soul_binder", "Arcaneblade: Soul Binder", 15000, "Legendary", 195, 76, 140,
        B().Add(StatType.Attack, 72).Add(StatType.Dexterity, 22).Add(StatType.Agility, 16), "SPOnHit+5");

    // HF F83 implement. Fellblade — ruinous doom; drains party HP for +50% ATK (flavor).
    public static Weapon CreateFellbladeRuinousDoom() => Make("sci_fellblade_ruinous_doom", "Fellblade: Ruinous Doom", 17000, "Legendary", 205, 79, 155,
        B().Add(StatType.Attack, 80).Add(StatType.Strength, 18).Add(StatType.Dexterity, 20), "PartyHPDrainATK+50");

    // HF F99 implement. Deathglutton — forbidden blade; -80% HP for +100% ATK (flavor).
    public static Weapon CreateDeathgluttonEpetamu() => Make("sci_deathglutton_epetamu", "Deathglutton: Epetamu", 36000, "Legendary", 265, 95, 185,
        B().Add(StatType.Attack, 95).Add(StatType.Dexterity, 28).Add(StatType.Agility, 22).Add(StatType.Strength, 18), "HPCostATKBonus+100");

    // ── Hollow Fragment Avatar Weapons (Scimitar) ─────────────────

    public static Weapon CreateSaphirAvatar() => Make("sci_saphir_avatar", "Saphir Avatar", 20500, "Legendary", 220, 82, 160,
        B().Add(StatType.Attack, 78).Add(StatType.Dexterity, 24).Add(StatType.Agility, 18), "CritRate+20");

    // ── Lisbeth Rarity 6 Crafted (Scimitar) ───────────────────────

    public static Weapon CreateCrescentbladeOriginalSin() => Make("sci_crescentblade_original_sin", "Crescentblade: Original Sin", 29500, "Legendary", 255, 88, 172,
        B().Add(StatType.Attack, 84).Add(StatType.Dexterity, 28).Add(StatType.Agility, 20), "LifeSteal+10");

    // ── Infinity Moment shop scimitars ──────────────────────────────

    // IM Epic-band shop weapon. Venom-etched apothecary blade.
    public static Weapon CreatePoisonedSyringe() => Make("sci_poisoned_syringe", "Poisoned Syringe", 5800, "Epic", 160, 38, 128,
        B().Add(StatType.Attack, 54).Add(StatType.Dexterity, 22).Add(StatType.Agility, 10), "Poison+25");

    // IM Legendary-band shop weapon. Swan-feather light curve, weightless swing.
    public static Weapon CreateSilverWing() => Make("sci_silver_wing", "Silver Wing", 20000, "Legendary", 225, 78, 162,
        B().Add(StatType.Attack, 76).Add(StatType.Dexterity, 24).Add(StatType.Agility, 20), "AttackSpeed+3");

    // ── Infinity Moment LAB scimitar (non-enhanceable) ──────────────

    // IM F93 floor-boss LAB reward. Refracting mirage edge, blinds on cut.
    public static Weapon CreateGlowHaze()
    {
        var w = Make("sci_glow_haze", "Glow Haze", 24500, "Legendary", 240, 83, 172,
            B().Add(StatType.Attack, 86).Add(StatType.Dexterity, 26).Add(StatType.Agility, 18),
            "BlindOnHit+15");
        w.IsEnhanceable = false;
        return w;
    }
}
