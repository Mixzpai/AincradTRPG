namespace YourGame.Items;

/// <summary>
/// Individual stat effect with potency and duration.
/// </summary>
public class StatEffect
{
    public StatType Type { get; set; }
    public int Potency { get; set; }
    public int Duration { get; set; } // 0 = permanent (for equipment)
    public bool IsPercentage { get; set; } // flat vs percentage bonus

    public StatEffect(StatType type, int potency, int duration = 0, bool isPercentage = false)
    {
        Type = type;
        Potency = potency;
        Duration = duration;
        IsPercentage = isPercentage;
    }
}