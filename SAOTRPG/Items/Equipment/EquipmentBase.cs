namespace SAOTRPG.Items.Equipment;

// Base class for equipment items that provide stat bonuses when equipped.
public abstract class EquipmentBase : BaseItem
{
    // Minimum player level required to equip this item.
    public int RequiredLevel { get; set; }
    // Slot category: "Weapon", "Armor", or "Accessory".
    public string? EquipmentType { get; set; }
    // Tiles this item lights while worn; 0 for almost everything. The player has no light of
    // their own, so equipment declaring a radius is the only light that follows them. Declared on
    // the base type so any equipment can carry a light without a new slot or type.
    public int LightRadius { get; set; }

    // The item's own durability ceiling, or 0 when it declares none. Declared on the base type
    // because the repair path honours it for every item: a floor-derived ceiling alone would
    // repair a starter torch past the endgame lantern's capacity and invert the tier ladder.
    public int MaxDurability { get; set; }
    // Stat modifiers applied while this equipment is worn.
    public StatModifierCollection Bonuses { get; set; } = new();

    // Enhancement level (+0 to +10). Each level adds flat bonus stats.
    // Enhancement is done at the Anvil using materials + Col.
    public int EnhancementLevel { get; set; }

    // Display name with enhancement + awakening suffixes:
    // "Iron Sword" → "Iron Sword +3" → "Night Sky Sword +3 ◈2".
    // Kept with what it was built from: the sidebar reads it on every redraw.
    public string EnhancedName
    {
        get
        {
            int awakening = this is Weapon w ? w.AwakeningLevel : 0;
            var key = (Name, EnhancementLevel, awakening);
            if (_enhancedName != null && key == _enhancedNameOf) return _enhancedName;
            string core = EnhancementLevel > 0 ? $"{Name} +{EnhancementLevel}" : (Name ?? "");
            if (awakening > 0)
                core = $"{core} ◈{awakening}";
            _enhancedNameOf = key;
            return _enhancedName = core;
        }
    }
    private string? _enhancedName;
    private (string? Name, int Enhancement, int Awakening) _enhancedNameOf;

    // Named effect string — save-format authority. Parsed by EquipmentSpecialEffectRegistry
    // into typed records. Weapons, shields, and armor all parse — defensive keys
    // (BlockChance/ParryChance/EvadeRegen/HPRegen/SPRegen) sum across slots.
    public string? SpecialEffect { get; set; }

    // Typed view of SpecialEffect, parsed once on first read and held in a plain field: it is read
    // many times per combat exchange (FirstEffect on the hit path, SumAllSlots once per equipped
    // slot per defence event), so the lookup must cost no more than a field read. A later write to
    // SpecialEffect is not noticed.
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

    // ── Refinement — 3 Ingot DefId slots; mutate only via Refinement.Socket (direct writes skip stat-folding + prior-ingot destruction). Divine cannot refine.
    public const int RefinementSlotCount = 3;
    public string?[] RefinementSlots { get; set; } = new string?[RefinementSlotCount];

    // Convenience: any non-null slot → refined. Indexed loop rather than Any(), which would walk
    // the array through IEnumerable.
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