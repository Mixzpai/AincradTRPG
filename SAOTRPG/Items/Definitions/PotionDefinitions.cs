using SAOTRPG.Items.Consumables;

namespace SAOTRPG.Items.Definitions;

// Static registry of all potions.
public static class PotionDefinitions
{
    private static Potion Make(string id, string name, int value, string rarity,
        string potionType, int cooldown, string effect, int maxStacks = 99,
        StatModifierCollection? effects = null)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            Quantity = 1, MaxStacks = maxStacks,
            ConsumableType = "Potion", PotionType = potionType,
            Cooldown = cooldown, EffectDescription = effect,
            Effects = effects ?? new StatModifierCollection(),
        };

    public static Potion CreateHealthPotion() => Make("health_potion", "Health Potion", 25, "Common",
        "Healing", 5, "Restores 50 HP instantly.",
        effects: new StatModifierCollection().Add(StatType.Health, 50));

    public static Potion CreateGreaterHealthPotion() => Make("greater_health_potion", "Greater Health Potion", 75, "Uncommon",
        "Healing", 5, "Restores 150 HP instantly.",
        effects: new StatModifierCollection().Add(StatType.Health, 150));

    public static Potion CreateAntidote() => Make("antidote", "Antidote", 30, "Common",
        "Antidote", 0, "Cures poison and bleed.");

    public static Potion CreateBattleElixir() => Make("battle_elixir", "Battle Elixir", 150, "Rare",
        "Buff", 60, "Increases Attack and Speed for 60 seconds.", maxStacks: 20,
        effects: new StatModifierCollection()
            .Add(StatType.Attack, 15, duration: 60)
            .Add(StatType.Speed, 10, duration: 60));

    public static Potion CreateSpeedPotion() => Make("speed_potion", "Speed Potion", 80, "Uncommon",
        "Buff", 30, "Increases Speed by 10 for 30 turns.", maxStacks: 20,
        effects: new StatModifierCollection().Add(StatType.Speed, 10, duration: 30));

    public static Potion CreateIronSkinPotion() => Make("iron_skin_potion", "Iron Skin Potion", 80, "Uncommon",
        "Buff", 30, "Increases Defense by 10 for 30 turns.", maxStacks: 20,
        effects: new StatModifierCollection().Add(StatType.Defense, 10, duration: 30));

    public static Potion CreateEscapeRope() => Make("escape_rope", "Escape Rope", 75, "Uncommon",
        "Teleport", 0, "Warps you back to the floor entrance.", maxStacks: 5);

    public static Potion CreateReviveCrystal() => Make("revive_crystal", "Revive Crystal", 500, "Rare",
        "Revive", 0, "Auto-triggers on death — restores 50% HP.", maxStacks: 3);
}
