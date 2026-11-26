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
    // CritRate is live. The other four are declared and plumbed but READ BY NOTHING — granting one
    // from an item or a perk does nothing at all, silently. See StatModifierCollection.ApplyStat
    // for which working mechanism each is mistaken for.
    CritRate,
    AttackSpeed,
    BlockChance,
    HPRegen,
    SkillCooldown
}