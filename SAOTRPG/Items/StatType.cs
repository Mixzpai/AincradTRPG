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

    // Append-only; int order matters for Inventory._statBonusCache.
    // CritRate is consumed. The other four are declared and plumbed but read by nothing, so
    // granting one from an item or a perk silently does nothing. See StatModifierCollection.ApplyStat
    // for the working mechanism each is easily mistaken for.
    CritRate,
    AttackSpeed,
    BlockChance,
    HPRegen,
    SkillCooldown
}