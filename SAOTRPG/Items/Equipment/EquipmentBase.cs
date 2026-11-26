namespace SAOTRPG.Items.Equipment;

// Base class for equipment items that provide stat bonuses when equipped.
public abstract class EquipmentBase : BaseItem
{
    // Minimum player level required to equip this item.
    public int RequiredLevel { get; set; }
    // Slot category: "Weapon", "Armor", or "Accessory".
    public string? EquipmentType { get; set; }
    // Tiles this item lights while worn. 0 for almost everything — the player has no light of
    // their own, so a piece of equipment declaring a radius is the only reason anything glows
    // around them. Lives here rather than on Torch so a lantern, an enchanted ring or a glowing
    // blade can light the way without a new slot or a new type.
    public int LightRadius { get; set; }

    // The item's OWN durability ceiling, or 0 when it declares none. Lives here rather than on the
    // two Tool-slot types that used to own it, because the repair path has to honour it for every
    // item — a floor-derived ceiling that ignores this repaired a 60-fuel starter torch to 550 on
    // a deep floor, which is nearly double the endgame lantern's designed capacity and inverted
    // the whole tier ladder.
    public int MaxDurability { get; set; }
    // Stat modifiers applied while this equipment is worn.
    public StatModifierCollection Bonuses { get; set; } = new();

    // Enhancement level (+0 to +10). Each level adds flat bonus stats.
    // Enhancement is done at the Anvil using materials + Col.
    public int EnhancementLevel { get; set; }

    // Display name with enhancement + awakening suffixes:
    // "Iron Sword" → "Iron Sword +3" → "Night Sky Sword +3 ◈2".
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

    // Named effect string — save-format authority. Parsed by EquipmentSpecialEffectRegistry
    // into typed records. Weapons, shields, and armor all parse — defensive keys
    // (BlockChance/ParryChance/EvadeRegen/HPRegen/SPRegen) sum across slots.
    public string? SpecialEffect { get; set; }

    // Typed view of SpecialEffect. Parsed once on first read and held here.
    //
    // The cache is a plain field rather than a ConditionalWeakTable keyed on this instance. A CWT
    // has the same lifetime (the entry dies with the item) and the same staleness (neither notices
    // a later write to SpecialEffect), but every read costs an identity hash and a bucket walk —
    // measured at 9.2 ns against ~1 ns for a field. That is paid roughly 74 times per combat
    // exchange: ~19 FirstEffect calls on the hit path plus SumAllSlots, which reads this once per
    // equipped slot and is itself called five times per defence event.
    private List<EquipmentSpecialEffect>? _parsedEffects;

    public IReadOnlyList<EquipmentSpecialEffect> ParsedEffects =>
        _parsedEffects ??= EquipmentSpecialEffectRegistry.Parse(this);

    // First parsed effect of type T, or null. Indexed loop rather than
    // ParsedEffects.OfType<T>().FirstOrDefault(): that pair allocates an OfType iterator plus a
    // boxed list enumerator on every call, and the callers run per monster hit, per boss breath
    // and per sword-skill use. Walks the concrete List so the indexer is not an interface call.
    public T? FirstEffect<T>() where T : EquipmentSpecialEffect
    {
        var effects = _parsedEffects ??= EquipmentSpecialEffectRegistry.Parse(this);
        for (int i = 0; i < effects.Count; i++)
            if (effects[i] is T match) return match;
        return null;
    }

    // ── IF Refinement — 3 Ingot DefId slots; mutate ONLY via Refinement.Socket (direct writes skip stat-folding + prior-ingot destruction). Divine cannot refine.
    public const int RefinementSlotCount = 3;
    public string?[] RefinementSlots { get; set; } = new string?[RefinementSlotCount];

    // Convenience: any non-null slot → refined. Indexed over the three-element array; Any()
    // walks it through IEnumerable and costs an order of magnitude more for the same answer.
    public bool HasAnyRefinement
    {
        get
        {
            var slots = RefinementSlots;
            if (slots == null) return false;
            for (int i = 0; i < slots.Length; i++)
                if (!string.IsNullOrEmpty(slots[i])) return true;
            return false;
        }
    }
}