using SAOTRPG.Inventory.Core;
using SAOTRPG.Items.Equipment;


namespace SAOTRPG.Inventory.Equipment;

public class EquipmentSlotResolver : IEquipmentSlotResolver
{
    // The default table is identical for every resolver, so it is built once and each instance
    // copies it; the copy constructor pre-sizes, so construction skips the per-key lowercasing
    // and the dictionary's growth.
    private static readonly Dictionary<string, EquipmentSlot> _defaultMappings = BuildDefaultMappings();

    private readonly Dictionary<string, EquipmentSlot> _slotMappings =
        new(_defaultMappings, StringComparer.OrdinalIgnoreCase);

    public EquipmentSlotResolver()
    {
    }

    // Specific before broad, and the order is the whole method. Every Armor carries
    // EquipmentType "Armor" and every Accessory carries "Accessory", and both are registered as
    // broad mappings ("armor" → Chest, "accessory" → Bracelet), so a broad-first lookup would
    // never miss: helmets, rings, necklaces and shields would all land in Chest or Bracelet, and
    // OffHand (which TryShieldBlock reads) would stay empty.
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

    // What slot a type name maps to, independent of any item. Lets the verification tools read
    // the resolver's own table rather than keep a second copy of it.
    public EquipmentSlot? ResolveSlotForType(string? equipmentType) =>
        !string.IsNullOrWhiteSpace(equipmentType)
        && _slotMappings.TryGetValue(equipmentType, out var slot) ? slot : null;

    public void RegisterMapping(string equipmentType, EquipmentSlot slot)
    {
        _slotMappings[equipmentType.ToLowerInvariant()] = slot;
    }

    private static Dictionary<string, EquipmentSlot> BuildDefaultMappings()
    {
        var map = new Dictionary<string, EquipmentSlot>(StringComparer.OrdinalIgnoreCase);
        void RegisterMapping(string equipmentType, EquipmentSlot slot)
            => map[equipmentType.ToLowerInvariant()] = slot;
        void RegisterMany(EquipmentSlot slot, params string[] types)
        {
            foreach (var type in types) RegisterMapping(type, slot);
        }

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
        // A torch competes with the pickaxe: carry a light or mine, not both.
        RegisterMapping("torch", EquipmentSlot.Tool);

        return map;
    }
}