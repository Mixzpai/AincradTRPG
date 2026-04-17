using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

// Static registry of all dagger weapons.
// Daggers: low damage, fast attack speed. Agility/Dexterity-oriented.
public static class DaggerDefinitions
{
    private static Weapon Make(string id, string name, int value, string rarity, int durability,
        int level, int baseDmg, StatModifierCollection bonuses, string? specialEffect = null)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = durability, RequiredLevel = level,
            EquipmentType = "Weapon", WeaponType = "Dagger",
            BaseDamage = baseDmg, AttackSpeed = 0, Range = 1,
            SpecialEffect = specialEffect, Bonuses = bonuses,
        };

    private static StatModifierCollection B() => new();

    public static Weapon CreateRustyDagger() => Make("rusty_dagger", "Rusty Dagger", 50, "Common", 35, 1, 6,
        B().Add(StatType.Attack, 5).Add(StatType.Agility, 4));

    public static Weapon CreateSteelDagger() => Make("steel_dagger", "Steel Dagger", 200, "Uncommon", 55, 8, 14,
        B().Add(StatType.Attack, 10).Add(StatType.Agility, 6).Add(StatType.Dexterity, 4));

    public static Weapon CreateMythrilDagger() => Make("mythril_dagger", "Mythril Dagger", 650, "Rare", 80, 25, 28,
        B().Add(StatType.Attack, 18).Add(StatType.Agility, 10).Add(StatType.Dexterity, 8));

    public static Weapon CreateAdamantiteDagger() => Make("adamantite_dagger", "Adamantite Dagger", 2000, "Epic", 120, 50, 50,
        B().Add(StatType.Attack, 32).Add(StatType.Agility, 15).Add(StatType.Dexterity, 12));

    public static Weapon CreateCelestialDagger() => Make("celestial_dagger", "Celestial Dagger", 5000, "Legendary", 170, 75, 85,
        B().Add(StatType.Attack, 55).Add(StatType.Agility, 20).Add(StatType.Dexterity, 15));

    // --- Named / SAO Legendary Weapons ---

    public static Weapon CreateAssassinDagger() => Make("assassin_dagger", "Assassin Dagger", 500, "Rare", 70, 20, 22,
        B().Add(StatType.Attack, 15).Add(StatType.Agility, 8).Add(StatType.Dexterity, 6), "BackstabDmg+50");

    public static Weapon CreateSnowWarheit() => Make("snow_warheit", "Snow Warheit", 1500, "Epic", 100, 35, 40,
        B().Add(StatType.Attack, 28).Add(StatType.Agility, 12).Add(StatType.Dexterity, 10));

    public static Weapon CreateMateChopper() => Make("mate_chopper", "Mate Chopper", 6000, "Legendary", 150, 35, 55,
        B().Add(StatType.Attack, 35).Add(StatType.Agility, 15), "Bleed+20");

    // ── Hollow Realization Evolution Chain (Dagger) ─────────────────
    // Heated Razor -> Valkyrie -> Misericorde -> The Iron Maiden.

    // Heated Razor, a forge-tempered slip-knife. T1 of the Iron Maiden chain.
    public static Weapon CreateHeatedRazor() => Make("heated_razor", "Heated Razor", 1500, "Rare", 120, 15, 55,
        B().Add(StatType.Attack, 32).Add(StatType.Agility, 10));

    // Valkyrie, winged shortblade that hunts the fallen. T2 of the Iron Maiden chain.
    public static Weapon CreateValkyrie() => Make("valkyrie", "Valkyrie", 4500, "Epic", 160, 35, 95,
        B().Add(StatType.Attack, 50).Add(StatType.Agility, 15).Add(StatType.Dexterity, 8), "CritRate+10");

    // Misericorde, mercy-blade of the thin-slip duel. T3 of the Iron Maiden chain.
    public static Weapon CreateMisericorde() => Make("misericorde", "Misericorde", 10500, "Legendary", 210, 60, 135,
        B().Add(StatType.Attack, 68).Add(StatType.Agility, 22).Add(StatType.Dexterity, 14), "Bleed+20");

    // T4 Divine of the Dagger evolution chain.
    public static Weapon CreateTheIronMaiden() => Make("iron_maiden_dagger", "The Iron Maiden", 16000, "Divine", 999, 75, 95,
        B().Add(StatType.Attack, 60).Add(StatType.Agility, 22).Add(StatType.Dexterity, 18), "CritRate+15");

    // Argo the Rat's personal twin daggers. Reward for completing her info-broker chain.
    public static Weapon CreateArgosClaws() => Make("argos_claws", "Argo's Claws", 5200, "Rare", 140, 20, 42,
        B().Add(StatType.Attack, 22).Add(StatType.Agility, 16).Add(StatType.Dexterity, 12), "BackstabDmg+30");

    // Silica's F1-era starter dagger. Plain iron, reliable balance.
    public static Weapon CreateStoutBrave() => Make("stout_brave", "Stout Brave", 400, "Uncommon", 90, 2, 14,
        B().Add(StatType.Attack, 8).Add(StatType.Agility, 4), "CritRate+10");

    // F24 Grimhollow the Phantom drops (Alicization Lycoris Divine Beast tier).
    // Illusion-dagger. Tops out crit chance.
    public static Weapon CreatePhantasmagoria() => Make("phantasmagoria", "Phantasmagoria", 10500, "Legendary", 180, 28, 92,
        B().Add(StatType.Attack, 48).Add(StatType.Agility, 20).Add(StatType.Dexterity, 18), "CritRate+25");

    // ── Integral Factor Series (Dagger entries) ─────────────────────

    // F25 Nox Series dagger. Nocturne — midnight-song slip-blade from the Underground Labyrinth.
    public static Weapon CreateNoxNocturne() => Make("dag_nox_nocturne", "Nox Nocturne", 6800, "Epic", 155, 25, 75,
        B().Add(StatType.Attack, 38).Add(StatType.Agility, 16).Add(StatType.Dexterity, 14), "BackstabDmg+30");

    // F85→F87 Yasha Series dagger. Envy — demonic whisper-blade, poisons on the draw.
    public static Weapon CreateYashaEnvy() => Make("dag_yasha_envy", "Yasha Envy", 17500, "Legendary", 225, 78, 140,
        B().Add(StatType.Attack, 70).Add(StatType.Agility, 24).Add(StatType.Dexterity, 18), "Poison+20");

    // ── Hollow Fragment Avatar Weapons (Dagger) ───────────────────

    public static Weapon CreateGenocideAvatar() => Make("dag_genocide_avatar", "Genocide Avatar", 19000, "Legendary", 215, 80, 152,
        B().Add(StatType.Attack, 74).Add(StatType.Agility, 25).Add(StatType.Dexterity, 20), "Bleed+30");

    // ── Lisbeth Rarity 6 Crafted (Dagger) ─────────────────────────

    public static Weapon CreateNotesEndTrinity() => Make("dag_notes_end_trinity", "Notes' End Trinity", 28000, "Legendary", 245, 85, 165,
        B().Add(StatType.Attack, 80).Add(StatType.Agility, 28).Add(StatType.Dexterity, 22), "CritRate+25");

    // ── Infinity Moment shop daggers ─────────────────────────────────

    // IM Epic-band shop weapon. Assassin's flight-steel dagger, backstab focus.
    public static Weapon CreateFlyheightFang() => Make("dag_flyheight_fang", "Flyheight Fang", 4800, "Epic", 155, 36, 120,
        B().Add(StatType.Attack, 52).Add(StatType.Agility, 20).Add(StatType.Dexterity, 10), "BackstabDmg+30");

    // IM Legendary-band shop weapon. Leaf-thin rue blade, precision crit.
    public static Weapon CreateRueFeuille() => Make("dag_rue_feuille", "Rue Feuille", 18500, "Legendary", 220, 76, 155,
        B().Add(StatType.Attack, 72).Add(StatType.Agility, 25).Add(StatType.Dexterity, 22), "CritRate+20");

    // ── Infinity Moment LAB dagger (non-enhanceable) ────────────────

    // IM F95 floor-boss LAB reward. Shimmering illusion-dagger — post-hit fade.
    public static Weapon CreateMirageKnife()
    {
        var w = Make("dag_mirage_knife", "Mirage Knife", 24000, "Legendary", 235, 82, 165,
            B().Add(StatType.Attack, 82).Add(StatType.Agility, 28).Add(StatType.Dexterity, 22),
            "Invisibility+5");
        w.IsEnhanceable = false;
        return w;
    }

    // ── Memory Defrag Originals (Dagger entries) ───────────────────────

    // MD Epic. Assassin-flavor baselard, violet hilt.
    public static Weapon CreatePurpleStarBaselard() => Make("dag_purple_star_baselard", "Purple Star Baselard", 5600, "Epic", 155, 45, 105,
        B().Add(StatType.Attack, 48).Add(StatType.Agility, 18).Add(StatType.Dexterity, 12), "BackstabDmg+25");

    // ── Fractured Daydream — Character Core Canon (Dagger) ─────────────

    // Yui canon — volcanic-glass dagger. F70+ rare drop.
    public static Weapon CreateObsidianDagger() => Make("dag_obsidian_dagger", "Obsidian Dagger", 18500, "Legendary", 220, 70, 148,
        B().Add(StatType.Attack, 72).Add(StatType.Agility, 24).Add(StatType.Dexterity, 18), "CritRate+20");

    // Argo canon — virt(uoso) cat-strike dagger. F45+ quest reward band.
    public static Weapon CreateVirtKatze() => Make("dag_virt_katze", "Virt Katze", 4800, "Epic", 150, 45, 105,
        B().Add(StatType.Attack, 46).Add(StatType.Agility, 18).Add(StatType.Dexterity, 14), "BackstabDmg+25");

    // Argo canon — Thunder God's Rift Blade. F80+ Legendary.
    public static Weapon CreateThunderGodsRiftBlade() => Make("dag_thunder_gods_rift_blade", "Thunder God's Rift Blade", 21500, "Legendary", 235, 80, 158,
        B().Add(StatType.Attack, 78).Add(StatType.Agility, 26).Add(StatType.Dexterity, 18), "Stun+15");

    // ── Fractured Daydream — Elemental Variants (Dagger) ───────────────

    // Silica water variant. Defeza — aqua-guard tanto.
    public static Weapon CreateDefeza() => Make("dag_defeza", "Defeza", 3100, "Rare", 120, 45, 70,
        B().Add(StatType.Attack, 32).Add(StatType.Agility, 14).Add(StatType.Dexterity, 10), "Freeze+15");

    // Argo wind variant. Hermit Fang — recluse's razor.
    public static Weapon CreateHermitFang() => Make("dag_hermit_fang", "Hermit Fang", 3200, "Rare", 120, 45, 72,
        B().Add(StatType.Attack, 34).Add(StatType.Agility, 14).Add(StatType.Dexterity, 10), "Slow+15");
}
