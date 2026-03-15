using SAOTRPG.Items.Definitions;
using SAOTRPG.Items.Definitions.Weapons;

namespace SAOTRPG.Items;

/// <summary>
/// Maps DefinitionId strings to factory methods for recreating items from save data.
/// </summary>
public static class ItemRegistry
{
    private static readonly Dictionary<string, Func<BaseItem>> _registry = new();

    static ItemRegistry()
    {
        // Weapons
        Register("iron_sword", () => OneHandedSwordDefinitions.CreateIronSword());
        Register("steel_sword", () => OneHandedSwordDefinitions.CreateSteelSword());

        // Armor
        Register("leather_chestplate", () => ArmorDefinitions.CreateLeatherChest());
        Register("iron_helmet", () => ArmorDefinitions.CreateIronHelmet());

        // Accessories
        Register("ring_of_strength", () => AccessoryDefinitions.CreateRingOfStrength());
        Register("agility_necklace", () => AccessoryDefinitions.CreateAgilityNecklace());

        // Potions
        Register("health_potion", () => PotionDefinitions.CreateHealthPotion());
        Register("greater_health_potion", () => PotionDefinitions.CreateGreaterHealthPotion());
        Register("antidote", () => PotionDefinitions.CreateAntidote());
        Register("battle_elixir", () => PotionDefinitions.CreateBattleElixir());

        // Food
        Register("bread", () => FoodDefinitions.CreateBread());
        Register("grilled_meat", () => FoodDefinitions.CreateGrilledMeat());

        // Damage Items
        Register("fire_bomb", () => DamageItemDefinitions.CreateFireBomb());
        Register("poison_vial", () => DamageItemDefinitions.CreatePoisonVial());

        // Mob Drops
        Register("slime_gel", () => MobDropDefinitions.CreateSlimeGel());
        Register("wolf_pelt", () => MobDropDefinitions.CreateWolfPelt());
        Register("dragon_scale", () => MobDropDefinitions.CreateDragonScale());
    }

    private static void Register(string id, Func<BaseItem> factory) => _registry[id] = factory;

    /// <summary>
    /// Recreate an item from its DefinitionId. Returns null if the ID is unknown.
    /// </summary>
    public static BaseItem? Create(string definitionId) =>
        _registry.TryGetValue(definitionId, out var factory) ? factory() : null;
}
