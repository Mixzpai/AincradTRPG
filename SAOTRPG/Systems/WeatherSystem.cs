namespace SAOTRPG.Systems;

// Per-floor weather state. Randomly assigned on floor generation.
// Each type has a gameplay effect: Clear = +1 regen, Rain = −3 crit,
// Fog = reduced visibility, Wind = +5 throwable damage.
public enum WeatherType
{
    // Pleasant conditions — +1 passive HP regen bonus.
    Clear,
    // Wet conditions — −3 crit rate for all combatants.
    Rain,
    // Reduces visibility radius.
    Fog,
    // Tailwind — +5 damage on throwable items.
    Wind
}

// Manages per-floor weather. Rolled randomly on floor generation using
// weighted probabilities: Clear 40%, Rain 25%, Fog 15%, Wind 20%.
public static class WeatherSystem
{
    // Current floor weather state.
    public static WeatherType Current { get; private set; } = WeatherType.Clear;

    // Per-weather configuration: label, gameplay modifiers, flavor text.
    // Public API funnels through this table — adding a new weather type means
    // adding one row here instead of touching six switch statements.
    private readonly record struct WeatherConfig(
        string Label,
        int Weight,
        int CritModifier,
        int RegenBonus,
        int TrapDetectionPenalty,
        int PoisonDurationBonus,
        string FlavorDescription);

    private static readonly Dictionary<WeatherType, WeatherConfig> Configs = new()
    {
        [WeatherType.Clear] = new("Clear", 40,  0, 1,   0, 0, ""),
        [WeatherType.Rain]  = new("Rainy", 25, -3, 0, -10, 1, "Rain patters against the stone walls of the labyrinth."),
        [WeatherType.Fog]   = new("Foggy", 15,  0, 0, -20, 0, "A thick fog rolls in, reducing visibility."),
        [WeatherType.Wind]  = new("Windy", 20,  0, 0,   0, 0, "A strong wind sweeps through the corridors."),
    };

    // Roll a new random weather for the given floor.
    // Called once during floor generation. The floor parameter is reserved
    // for future biome-specific weather tables.
    public static void RollWeather(int floor)
    {
        int total = 0;
        foreach (var cfg in Configs.Values) total += cfg.Weight;

        int roll = Random.Shared.Next(total);
        int acc = 0;
        foreach (var (type, cfg) in Configs)
        {
            acc += cfg.Weight;
            if (roll < acc)
            {
                Current = type;
                return;
            }
        }
        Current = WeatherType.Clear;
    }

    // Returns a short label for the current weather (e.g. "Rainy").
    public static string GetLabel() => Configs[Current].Label;

    // ── Gameplay modifiers per weather type ─────────────────────────

    // Crit rate modifier. Rain: −3 (slippery), others: 0.
    public static int GetCritModifier() => Configs[Current].CritModifier;

    // Passive HP regen bonus. Clear: +1 (pleasant), others: 0.
    public static int GetRegenBonus() => Configs[Current].RegenBonus;

    // Rain hides traps better (harder to detect). Fog hides traps completely until stepped on.
    public static int GetTrapDetectionPenalty() => Configs[Current].TrapDetectionPenalty;

    // Rain makes gas vents more potent (+1 poison turn).
    public static int GetPoisonDurationBonus() => Configs[Current].PoisonDurationBonus;

    // Returns a flavorful one-liner describing the current weather.
    public static string GetFlavorDescription() => Configs[Current].FlavorDescription;
}
