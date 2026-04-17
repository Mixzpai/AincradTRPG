using SAOTRPG.Inventory.Core;
using SAOTRPG.Items.Equipment;


namespace SAOTRPG.Inventory.Equipment;

public class EquipmentSlotResolver : IEquipmentSlotResolver
{
    private readonly Dictionary<string, EquipmentSlot> _slotMappings = new(StringComparer.OrdinalIgnoreCase);

    public EquipmentSlotResolver()
    {
        RegisterDefaultMappings();
    }

    // True if the item is a legal OffHand occupant. Shields are always
    // legal; one-handed swords become legal once Dual Blades is unlocked;
    // canon FD "Paired" weapons (IsDualWieldPaired=true) are always legal
    // regardless of WeaponType and regardless of DualBlades unlock — these
    // are pre-tuned for dual-wield per Systems.DualWieldPairs.
    // Inventory.Equip handles the actual auto-routing; this predicate lets
    // UI / validation code ask the same question without duplicating logic.
    public bool CanGoInOffHand(EquipmentBase equipment)
    {
        var resolved = ResolveSlot(equipment);
        if (resolved == EquipmentSlot.OffHand) return true;  // shields, bucklers, etc.
        if (equipment is Weapon paired && paired.IsDualWieldPaired)
        {
            return true;
        }
        if (equipment is Weapon w
            && string.Equals(w.WeaponType, "One-Handed Sword", StringComparison.OrdinalIgnoreCase)
            && SAOTRPG.Systems.Skills.UniqueSkillSystem.HasDualBlades())
        {
            return true;
        }
        return false;
    }

    public EquipmentSlot? ResolveSlot(EquipmentBase equipment)
    {
        if (string.IsNullOrWhiteSpace(equipment.EquipmentType))
            return null;

        // Try direct mapping first (e.g. "broadsword" → Weapon)
        if (_slotMappings.TryGetValue(equipment.EquipmentType, out var slot))
            return slot;

        // Fallback: check sub-type properties on concrete types
        if (equipment is Weapon weapon && !string.IsNullOrWhiteSpace(weapon.WeaponType))
        {
            if (_slotMappings.TryGetValue(weapon.WeaponType, out slot))
                return slot;
        }

        if (equipment is Armor armor && !string.IsNullOrWhiteSpace(armor.ArmorSlot))
        {
            if (_slotMappings.TryGetValue(armor.ArmorSlot, out slot))
                return slot;
        }

        if (equipment is Accessory accessory && !string.IsNullOrWhiteSpace(accessory.AccessorySlot))
        {
            if (_slotMappings.TryGetValue(accessory.AccessorySlot, out slot))
                return slot;
        }

        return null;
    }

    public void RegisterMapping(string equipmentType, EquipmentSlot slot)
    {
        _slotMappings[equipmentType.ToLowerInvariant()] = slot;
    }

    private void RegisterDefaultMappings()
    {
        // Broad category mappings
        RegisterMapping("weapon", EquipmentSlot.Weapon);
        RegisterMapping("armor", EquipmentSlot.Chest);
        RegisterMapping("accessory", EquipmentSlot.Bracelet);

        // Weapons — specific subtypes
        RegisterMany(EquipmentSlot.Weapon,
            "sword", "broadsword", "longsword", "rapier", "dagger", "mace",
            "hammer", "polearm", "handaxe", "bow", "scimitar", "claws", "scythe");

        // Head
        RegisterMany(EquipmentSlot.Head,
            "cap", "bandana", "hood", "circlet", "mask",
            "coif", "helm", "helmet", "visor", "crown");

        // Chest
        RegisterMany(EquipmentSlot.Chest,
            "chest", "coat", "robe", "mail", "jacket", "plate", "tunic", "vest");

        // Legs
        RegisterMany(EquipmentSlot.Legs,
            "pants", "leggings", "trousers", "greaves", "chausses");

        // Feet
        RegisterMany(EquipmentSlot.Feet,
            "shoes", "boots", "sabatons", "sandals");

        // Accessories
        RegisterMapping("ring", EquipmentSlot.RightRing);
        RegisterMapping("ring-right", EquipmentSlot.RightRing);
        RegisterMapping("ring-left", EquipmentSlot.LeftRing);
        RegisterMany(EquipmentSlot.Bracelet, "bracelet", "bangle", "armlet");
        RegisterMany(EquipmentSlot.Necklace, "necklace", "pendant", "amulet", "chain");
        RegisterMany(EquipmentSlot.OffHand, "shield", "buckler", "kite shield", "tower shield");
    }

    private void RegisterMany(EquipmentSlot slot, params string[] types)
    {
        foreach (var type in types)
        {
            RegisterMapping(type, slot);
        }
    }
}