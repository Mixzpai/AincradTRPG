using SAOTRPG.Entities;

namespace SAOTRPG.Items.Equipment;

// Base class for equipment items that provide stat bonuses when equipped.
public abstract class EquipmentBase : BaseItem
{
    // Minimum player level required to equip this item.
    public int RequiredLevel { get; set; }
    // Slot category: "Weapon", "Armor", or "Accessory".
    public string? EquipmentType { get; set; }
    // Stat modifiers applied while this equipment is worn.
    public StatModifierCollection Bonuses { get; set; } = new();

    // Enhancement level (+0 to +10). Each level adds flat bonus stats.
    // Enhancement is done at the Anvil using materials + Col.
    public int EnhancementLevel { get; set; }

    // Display name with enhancement + awakening suffixes:
    // "Iron Sword" → "Iron Sword +3" → "Night Sky Sword +3 ◈2" (Bundle 9).
    public string EnhancedName
    {
        get
        {
            string core = EnhancementLevel > 0 ? $"{Name} +{EnhancementLevel}" : (Name ?? "");
            if (this is Weapon w && w.AwakeningLevel > 0)
                core = $"{core} ◈{w.AwakeningLevel}";
            return core;
        }
    }

    // Named effect string — save-format authority. Parsed by SwordSkillEngine.GetSpecialEffectValue
    // (raw int) and EquipmentSpecialEffectRegistry (typed records, B12). Weapons + shields share
    // since Bundle 8 (shield effects live-wired); Bundle 10 lifts armor parsing for defensive keys.
    public string? SpecialEffect { get; set; }

    // Bundle 10 (B12) — typed view of SpecialEffect. Lazy + cached per-instance via ConditionalWeakTable.
    public IReadOnlyList<EquipmentSpecialEffect> ParsedEffects =>
        EquipmentSpecialEffectRegistry.GetParsed(this);

    // ── IF Refinement — 3 Ingot DefId slots; mutate ONLY via Refinement.Socket (direct writes skip stat-folding + prior-ingot destruction). Divine cannot refine.
    public const int RefinementSlotCount = 3;
    public string?[] RefinementSlots { get; set; } = new string?[RefinementSlotCount];

    // Convenience: any non-null slot → refined.
    public bool HasAnyRefinement =>
        RefinementSlots != null && RefinementSlots.Any(s => !string.IsNullOrEmpty(s));

    // Apply stat bonuses to the target entity when equipped.
    public virtual void Equip(IStatModifiable target)
    {
        Bonuses.ApplyTo(target);
    }

    // Remove stat bonuses from the target entity when unequipped.
    public virtual void Unequip(IStatModifiable target)
    {
        Bonuses.RemoveFrom(target);
    }
}