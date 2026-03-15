namespace SAOTRPG.Systems;

/// <summary>
/// Per-floor weather state. Randomly assigned on floor generation.
/// Visual-only for Clear/Wind/Rain. Fog reduces visibility radius.
/// </summary>
public enum WeatherType { Clear, Rain, Fog, Wind }

public static class WeatherSystem
{
    /// <summary>Current floor weather state.</summary>
    public static WeatherType Current { get; private set; } = WeatherType.Clear;

    /// <summary>Visibility radius reduction caused by fog.</summary>
    public const int FogRadiusReduction = 3;

    // Weighted probabilities: Clear 40%, Rain 25%, Fog 15%, Wind 20%
    private static readonly (WeatherType Type, int Weight)[] WeatherWeights =
    {
        (WeatherType.Clear, 40),
        (WeatherType.Rain,  25),
        (WeatherType.Fog,   15),
        (WeatherType.Wind,  20),
    };

    /// <summary>Roll a new random weather for the given floor.</summary>
    public static void RollWeather(int floor)
    {
        int total = 0;
        foreach (var (_, w) in WeatherWeights) total += w;

        int roll = Random.Shared.Next(total);
        int acc = 0;
        foreach (var (type, weight) in WeatherWeights)
        {
            acc += weight;
            if (roll < acc)
            {
                Current = type;
                return;
            }
        }
        Current = WeatherType.Clear;
    }

    /// <summary>Returns a label string for the current weather.</summary>
    public static string GetLabel() => Current switch
    {
        WeatherType.Rain => "Rainy",
        WeatherType.Fog  => "Foggy",
        WeatherType.Wind => "Windy",
        _                => "Clear",
    };
}
