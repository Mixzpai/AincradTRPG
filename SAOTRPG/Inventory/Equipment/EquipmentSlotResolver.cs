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

    // SPECIFIC BEFORE BROAD, and the order is the whole method.
    //
    // This used to try EquipmentType first and treat the sub-type properties as a fallback. Every
    // Armor carries EquipmentType "Armor" and every Accessory carries "Accessory", and BOTH are
    // registered as broad mappings ("armor" → Chest, "accessory" → Bracelet) — so the direct
    // lookup never missed and the sub-type branches below were unreachable by construction.
    //
    // Measured on the shipped registry: all 395 equipment items landed in FOUR of the eleven
    // declared slots (Weapon 364, Chest 22, Bracelet 6, Tool 3). Head, Legs, Feet, RightRing,
    // LeftRing, Necklace and OffHand were unreachable, so a helmet and a chestplate fought for one
    // slot, every ring and necklace fought for another — and every SHIELD resolved to Chest, which
    // means the OffHand slot was always empty and TryShieldBlock could never fire.
    public EquipmentSlot? ResolveSlot(EquipmentBase equipment)
    {
        // Sub-type first: "Helmet" → Head, "Shield" → OffHand, "Ring" → RightRing.
        if (equipment is Armor armor && !string.IsNullOrWhiteSpace(armor.ArmorSlot)
            && _slotMappings.TryGetValue(armor.ArmorSlot, out var armorSlot))
            return armorSlot;

        if (equipment is Accessory accessory && !string.IsNullOrWhiteSpace(accessory.AccessorySlot)
            && _slotMappings.TryGetValue(accessory.AccessorySlot, out var accSlot))
            return accSlot;

        if (equipment is Weapon weapon && !string.IsNullOrWhiteSpace(weapon.WeaponType)
            && _slotMappings.TryGetValue(weapon.WeaponType, out var weaponSlot))
            return weaponSlot;

        // Broad category last, and still load-bearing: a WeaponType the table does not name
        // (Katana, Spear, Axe …) falls through to "weapon" → Weapon, which is correct.
        if (!string.IsNullOrWhiteSpace(equipment.EquipmentType)
            && _slotMappings.TryGetValue(equipment.EquipmentType, out var broadSlot))
            return broadSlot;

        return null;
    }

    // What slot a type NAME maps to, independent of any item. Exists so a checker can ask the
    // resolver its own table rather than keeping a second copy that can drift from it.
    public EquipmentSlot? ResolveSlotForType(string? equipmentType) =>
        !string.IsNullOrWhiteSpace(equipmentType)
        && _slotMappings.TryGetValue(equipmentType, out var slot) ? slot : null;

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
            "legs", "pants", "leggings", "trousers", "greaves", "chausses");

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

        // Tool slot for pickaxes (mining bump-action).
        RegisterMapping("pickaxe", EquipmentSlot.Tool);
    }

    private void RegisterMany(EquipmentSlot slot, params string[] types)
    {
        foreach (var type in types)
        {
            RegisterMapping(type, slot);
        }
    }
}