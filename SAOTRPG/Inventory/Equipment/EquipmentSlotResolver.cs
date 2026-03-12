using Inventory.Core;

namespace Inventory.Equipment;

public class EquipmentSlotResolver : IEquipmentSlotResolver
{
    private readonly Dictionary<string, EquipmentSlot> _slotMappings = new(StringComparer.OrdinalIgnoreCase);

    public EquipmentSlotResolver()
    {
        RegisterDefaultMappings();
    }

    public EquipmentSlot? ResolveSlot(global::Equipment equipment)
    {
        if (string.IsNullOrWhiteSpace(equipment.EquipmentType))
            return null;

        return _slotMappings.TryGetValue(equipment.EquipmentType, out var slot) ? slot : null;
    }

    public void RegisterMapping(string equipmentType, EquipmentSlot slot)
    {
        _slotMappings[equipmentType.ToLowerInvariant()] = slot;
    }

    private void RegisterDefaultMappings()
    {
        // Weapons
        RegisterMany(EquipmentSlot.Weapon, 
            "broadsword", "longsword", "rapier", "dagger", "mace", 
            "hammer", "polearm", "handaxe", "staff", "bow");

        // Head
        RegisterMany(EquipmentSlot.Head,
            "cap", "bandana", "hood", "circlet", "mask", 
            "coif", "helm", "helmet", "visor", "crown");

        // Chest
        RegisterMany(EquipmentSlot.Chest,
            "coat", "robe", "mail", "jacket", "plate", "tunic", "vest");

        // Legs
        RegisterMany(EquipmentSlot.Legs,
            "pants", "leggings", "trousers", "greaves", "chausses");

        // Feet
        RegisterMany(EquipmentSlot.Feet,
            "shoes", "boots", "sabatons", "sandals");

        // Accessories
        RegisterMapping("ring-right", EquipmentSlot.RightRing);
        RegisterMapping("ring-left", EquipmentSlot.LeftRing);
        RegisterMany(EquipmentSlot.Bracelet, "bracelet", "bangle", "armlet");
        RegisterMany(EquipmentSlot.Necklace, "necklace", "pendant", "amulet", "chain");
    }

    private void RegisterMany(EquipmentSlot slot, params string[] types)
    {
        foreach (var type in types)
        {
            RegisterMapping(type, slot);
        }
    }
}