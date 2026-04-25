namespace SAOTRPG.Items;

// Enumeration of all possible stats that can be modified.
public enum StatType
{
    // Core stats
    Health,
    Attack,
    Defense,
    Speed,
    SkillDamage,

    // Attributes
    Vitality,
    Strength,
    Endurance,
    Dexterity,
    Agility,
    Intelligence,

    // Bundle 10 (B13) — append-only; int order matters for Inventory._statBonusCache.
    // CritRate=% crit chance, AttackSpeed=weapon swing cadence, BlockChance=shield %,
    // HPRegen=HP per tick, SkillCooldown=turns reduction on sword skills.
    CritRate,
    AttackSpeed,
    BlockChance,
    HPRegen,
    SkillCooldown
}