namespace SAOTRPG.Items.Materials;

/// <summary>
/// Materials obtained from defeating monsters.
/// </summary>
public class MobDrop : Material
{
    public string? SourceMonster { get; set; }
    public float DropRate { get; set; }
    public bool IsBossDrop { get; set; }
}