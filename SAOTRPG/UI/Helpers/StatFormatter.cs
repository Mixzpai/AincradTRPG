using SAOTRPG.Items;

namespace SAOTRPG.UI.Helpers;

// Shared stat abbreviation and tooltip lookup.
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

    // Brief tooltip describing what each stat does, shown in stat detail views.
    public static string Tooltip(StatType type) => type switch
    {
        StatType.Attack       => "Physical damage dealt",
        StatType.Defense      => "Physical damage reduced",
        StatType.Speed        => "Turn priority and dodge chance",
        StatType.Health       => "Maximum hit points",
        StatType.Strength     => "+2 ATK per point",
        StatType.Vitality     => "+10 Max HP per point",
        StatType.Endurance    => "+2 DEF per point",
        StatType.Dexterity    => "+Crit rate and accuracy",
        StatType.Agility      => "+2 SPD and dodge per point",
        StatType.Intelligence => "+2 Skill damage per point",
        _                     => ""
    };
}
