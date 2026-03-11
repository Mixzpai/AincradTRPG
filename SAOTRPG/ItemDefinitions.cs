/****************************************************************************************/
// Concrete item types
public class Weapon : Equipment
{
    public int BaseDamage { get; set; }
}

public class Armor : Equipment
{
    public int BaseDefense { get; set; }
}

public class Accessory : Equipment { }

public class Potion : Consumable { }

public class Food : Consumable { }

/****************************************************************************************/
// Static registry of all items
public static class ItemDefinitions
{
    /****************************************************************************************/
    //  WEAPONS
    public static Weapon IronSword => new()
    {
        Name = "Iron Sword",
        Value = 100,
        Rarity = "Common",
        ItemDurability = 50,
        RequiredLevel = 1,
        EquipmentType = "Sword",
        BaseDamage = 10,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 8)
    };

    /****************************************************************************************/
    // ARMOR 
    public static Armor LeatherChest => new()
    {
        Name = "Leather Chestplate",
        Value = 80,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 1,
        EquipmentType = "Chest",
        BaseDefense = 5,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 5)
            .Add(StatType.Vitality, 2)
    };

    /****************************************************************************************/
    // ACCESSORIES
    public static Accessory RingOfStrength => new()
    {
        Name = "Ring of Strength",
        Value = 500,
        Rarity = "Uncommon",
        ItemDurability = 100,
        RequiredLevel = 10,
        EquipmentType = "Ring",
        Bonuses = new StatModifierCollection()
            .Add(StatType.Strength, 10)
            .Add(StatType.Attack, 5)
    };

    /****************************************************************************************/
    // POTIONS
    public static Potion HealthPotion => new()
    {
        Name = "Health Potion",
        Value = 25,
        Rarity = "Common",
        Quantity = 1,
        MaxStacks = 99,
        ConsumableType = "Healing",
        EffectDescription = "Restores 50 HP instantly.",
        Effects = new StatModifierCollection()
            .Add(StatType.Health, 50)
    };

    public static Potion BattleElixir => new()
    {
        Name = "Battle Elixir",
        Value = 150,
        Rarity = "Rare",
        Quantity = 1,
        MaxStacks = 20,
        ConsumableType = "Buff",
        EffectDescription = "Increases Attack and Speed for 60 seconds.",
        Effects = new StatModifierCollection()
            .Add(StatType.Attack, 15, duration: 60)
            .Add(StatType.Speed, 10, duration: 60)
    };

    /****************************************************************************************/
    // FOOD
    public static Food Bread => new()
    {
        Name = "Bread",
        Value = 5,
        Rarity = "Common",
        Quantity = 1,
        MaxStacks = 50,
        ConsumableType = "Food",
        EffectDescription = "Restores a small amount of HP over time.",
        Effects = new StatModifierCollection()
            .Add(StatType.Health, 5, duration: 10) // 5 HP per tick for 10 ticks
    };

    /****************************************************************************************/
    // HELPER METHODS

    // Get a fresh copy of an item (for inventory/drops)
    public static Weapon CreateIronSword() => IronSword;
    public static Potion CreateHealthPotion(int quantity = 1)
    {
        var potion = HealthPotion;
        potion.Quantity = quantity;
        return potion;
    }

    // Lookup by name (optional)
    public static BaseItem? GetByName(string name) => name switch
    {
        "Iron Sword" => IronSword,
        "Health Potion" => HealthPotion,
        "Battle Elixir" => BattleElixir,
        _ => null
    };
}
/****************************************************************************************/
