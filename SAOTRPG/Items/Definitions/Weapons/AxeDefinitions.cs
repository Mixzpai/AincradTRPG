using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

// Static registry of all axe weapons.
// Axes: high damage, slow attack speed. Strength-oriented.
public static class AxeDefinitions
{
    private static Weapon Make(string id, string name, int value, string rarity, int durability,
        int level, int baseDmg, StatModifierCollection bonuses, string? specialEffect = null)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = durability, RequiredLevel = level,
            EquipmentType = "Weapon", WeaponType = "Axe",
            BaseDamage = baseDmg, AttackSpeed = 2, Range = 1,
            SpecialEffect = specialEffect, Bonuses = bonuses,
        };

    private static StatModifierCollection B() => new();

    public static Weapon CreateHandAxe() => Make("hand_axe", "Hand Axe", 90, "Common", 45, 1, 12,
        B().Add(StatType.Attack, 10).Add(StatType.Strength, 3));

    public static Weapon CreateBattleAxe() => Make("battle_axe", "Battle Axe", 280, "Uncommon", 70, 10, 30,
        B().Add(StatType.Attack, 18).Add(StatType.Strength, 8));

    public static Weapon CreateMythrilAxe() => Make("mythril_axe", "Mythril Axe", 850, "Rare", 110, 25, 55,
        B().Add(StatType.Attack, 30).Add(StatType.Strength, 10));

    public static Weapon CreateAdamantiteAxe() => Make("adamantite_axe", "Adamantite Axe", 2800, "Epic", 170, 50, 95,
        B().Add(StatType.Attack, 50).Add(StatType.Strength, 18));

    public static Weapon CreateCelestialAxe() => Make("celestial_axe", "Celestial Axe", 6500, "Legendary", 220, 75, 155,
        B().Add(StatType.Attack, 80).Add(StatType.Strength, 25).Add(StatType.Vitality, 10));

    public static Weapon CreatePaleEdge() => Make("pale_edge", "Pale Edge", 400, "Rare", 65, 15, 22,
        B().Add(StatType.Attack, 14).Add(StatType.Strength, 6).Add(StatType.Agility, 4));

    // ── Hollow Realization Evolution Chain (Axe) ────────────────────
    // Bardiche -> Archaic Murder -> Nidhogg's Fang -> Ouroboros.

    // Bardiche, crescent-polearm-axe, reliable through heavy armor. T1 of the Ouroboros chain.
    public static Weapon CreateBardiche() => Make("bardiche", "Bardiche", 2100, "Rare", 120, 15, 68,
        B().Add(StatType.Attack, 35).Add(StatType.Strength, 10));

    // Archaic Murder, primeval head of black iron, feels older than smithing. T2 of the Ouroboros chain.
    public static Weapon CreateArchaicMurder() => Make("archaic_murder", "Archaic Murder", 5800, "Epic", 160, 35, 112,
        B().Add(StatType.Attack, 55).Add(StatType.Strength, 15).Add(StatType.Vitality, 8), "CritRate+10");

    // Nidhogg's Fang, root-serpent tooth mounted on a war-haft. T3 of the Ouroboros chain.
    public static Weapon CreateNidhoggsFang() => Make("nidhoggs_fang", "Nidhogg's Fang", 14000, "Legendary", 210, 60, 152,
        B().Add(StatType.Attack, 75).Add(StatType.Strength, 22).Add(StatType.Vitality, 14), "Bleed+20");

    // T4 Divine of the Axe evolution chain.
    public static Weapon CreateOuroboros() => Make("ouroboros", "Ouroboros", 20000, "Divine", 999, 80, 170,
        B().Add(StatType.Attack, 85).Add(StatType.Strength, 28).Add(StatType.Vitality, 15), "AoERadius+1");

    // Agil's starter broadaxe. Reliable through the lower floors.
    public static Weapon CreateSamuraiAxe() => Make("samurai_axe", "Samurai Axe", 900, "Uncommon", 130, 8, 38,
        B().Add(StatType.Attack, 20).Add(StatType.Strength, 8), "DurabilityBonus+25");

    // Agil's 'falling-lotus' battleaxe. Earned through a mid-game befriend quest.
    public static Weapon CreateOchigaitou() => Make("ochigaitou", "Ochigaitou", 9500, "Epic", 210, 42, 105,
        B().Add(StatType.Attack, 52).Add(StatType.Strength, 22).Add(StatType.Vitality, 8), "KnockbackChance+30");

    // ── Hollow Fragment / Infinity Moment Legendaries ──────────────

    // Hollow Fragment F94 implement. Executioner's axe; massive damage vs low-HP foes (flavor).
    public static Weapon CreateRagnaroksBaneHeadsman() => Make("ragnaroks_bane_headsman", "Ragnarok's Bane: Headsman", 27000, "Legendary", 260, 90, 178,
        B().Add(StatType.Attack, 100).Add(StatType.Strength, 40).Add(StatType.Vitality, 15), "ExecuteThreshold+40");

    // ── Integral Factor Series (Axe entries) ────────────────────────

    // F61 Rosso Series 2H axe. Dominion — red-order greataxe, archangel rank.
    public static Weapon CreateRossoDominion() => Make("axe_rosso_dominion", "Rosso Dominion", 14500, "Legendary", 225, 55, 150,
        B().Add(StatType.Attack, 74).Add(StatType.Strength, 24).Add(StatType.Vitality, 12), "KnockbackChance+30");

    // ── Hollow Fragment Implement System (2H Axe) ─────────────────

    // HF F85 implement. Crusher — Bond Cyclone; +50 ATK / -50 DEF trade (flavor). Quest NPC reward.
    public static Weapon CreateCrusherBondCyclone() => Make("axe_crusher_bond_cyclone", "Crusher: Bond Cyclone", 19000, "Legendary", 220, 82, 160,
        B().Add(StatType.Attack, 90).Add(StatType.Strength, 28).Add(StatType.Vitality, 10), "RagingCyclone+50");

    // HF F86 implement. Fellaxe — demon's scythe; damage scales with lost HP (flavor).
    public static Weapon CreateFellaxeDemonsScythe() => Make("axe_fellaxe_demons_scythe", "Fellaxe: Demon's Scythe", 20000, "Legendary", 230, 84, 165,
        B().Add(StatType.Attack, 86).Add(StatType.Strength, 30).Add(StatType.Vitality, 12), "HPScalingDamage+25");

    // ── Hollow Fragment Avatar Weapons (Axe) ─────────────────────

    public static Weapon CreateLordBursterAvatar() => Make("axe_lord_burster_avatar", "Lord Burster Avatar", 22000, "Legendary", 240, 84, 168,
        B().Add(StatType.Attack, 82).Add(StatType.Strength, 28).Add(StatType.Vitality, 14), "ExecuteThreshold+30");

    // ── Lisbeth Rarity 6 Crafted (2H Axe) ────────────────────────

    public static Weapon CreateHecatombAxeGigaDisaster() => Make("axe_hecatomb_giga_disaster", "Hecatomb Axe: Giga Disaster", 31500, "Legendary", 265, 90, 180,
        B().Add(StatType.Attack, 88).Add(StatType.Strength, 30).Add(StatType.Vitality, 16), "ExecuteThreshold+25");

    public static Weapon CreateIngurgitatorBelzericht() => Make("axe_ingurgitator_belzericht", "Ingurgitator: Belzericht", 32000, "Legendary", 265, 91, 182,
        B().Add(StatType.Attack, 90).Add(StatType.Strength, 30).Add(StatType.Vitality, 18), "HPDrain+10");

    // ── Infinity Moment shop 2H axe ─────────────────────────────────

    // IM Legendary-band shop weapon. Lightning-etched black-steel axe.
    public static Weapon CreateSchwarzsBlitz() => Make("axe_schwarzs_blitz", "Schwarzs Blitz", 19000, "Legendary", 225, 77, 160,
        B().Add(StatType.Attack, 78).Add(StatType.Strength, 26).Add(StatType.Vitality, 14), "Stun+15");

    // ── Infinity Moment LAB 2H axe (non-enhanceable) ────────────────

    // IM F96 floor-boss LAB reward. Auroral glacial axe — frost bite.
    public static Weapon CreateNorthernLight()
    {
        var w = Make("axe_northern_light", "Northern Light", 26500, "Legendary", 250, 86, 180,
            B().Add(StatType.Attack, 94).Add(StatType.Strength, 30).Add(StatType.Vitality, 16),
            "FrostDamage+25");
        w.IsEnhanceable = false;
        return w;
    }
}
