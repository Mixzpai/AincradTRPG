using SAOTRPG.Items;

namespace SAOTRPG.UI.Helpers;

// Shared stat abbreviation lookup.
// Single source of truth for stat display names across all UI.
public static class StatFormatter
{
    // Short 2-3 character label for a stat type.
    public static string Short(StatType type) => type switch
    {
        StatType.Attack       => "ATK",
        StatType.Defense      => "DEF",
        StatType.Speed        => "SPD",
        StatType.Health       => "HP",
        StatType.Strength     => "STR",
        StatType.Vitality     => "VIT",
        StatType.Endurance    => "END",
        StatType.Dexterity    => "DEX",
        StatType.Agility      => "AGI",
        StatType.Intelligence => "INT",
        _                     => type.ToString()
    };
}
