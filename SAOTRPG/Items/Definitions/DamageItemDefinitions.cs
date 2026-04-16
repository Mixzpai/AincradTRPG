using SAOTRPG.Items.Consumables;

namespace SAOTRPG.Items.Definitions;

// Static registry of all throwable damage items.
public static class DamageItemDefinitions
{
    private static DamageItem Make(string id, string name, int value, string rarity,
        string damageType, int baseDamage, int areaOfEffect, string effect,
        int maxStacks = 20)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            Quantity = 1, MaxStacks = maxStacks,
            ConsumableType = "Throwable", DamageType = damageType,
            BaseDamage = baseDamage, AreaOfEffect = areaOfEffect,
            EffectDescription = effect,
        };

    public static DamageItem CreateFireBomb() => Make("fire_bomb", "Fire Bomb", 50, "Uncommon",
        "Fire", 30, 3, "Deals 30 fire damage in a 3-unit radius.");

    public static DamageItem CreatePoisonVial() => Make("poison_vial", "Poison Vial", 40, "Uncommon",
        "Poison", 10, 2, "Deals 10 poison damage and applies poison effect.");

    public static DamageItem CreateSmokeBomb() => Make("smoke_bomb", "Smoke Bomb", 50, "Uncommon",
        "Smoke", 0, 2, "Blinds nearby enemies, halving their attack for 3 turns.");

    public static DamageItem CreateFlashBomb() => Make("flash_bomb", "Flash Bomb", 70, "Uncommon",
        "Stun", 5, 2, "Deals 5 damage and stuns nearby enemies for 1 turn.");
}
