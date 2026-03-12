namespace YourGame.Items.Materials;

/// <summary>
/// Materials obtained from gathering in the world.
/// </summary>
public class Gathered : Material
{
    public string? GatheringType { get; set; }
    public int RequiredGatheringLevel { get; set; }
    public string? GatheringLocation { get; set; }
}