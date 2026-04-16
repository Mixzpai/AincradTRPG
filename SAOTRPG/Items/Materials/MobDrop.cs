namespace SAOTRPG.Items.Materials;

// Materials obtained from defeating monsters.
public class MobDrop : Material
{
    public string? SourceMonster { get; set; }
    public float DropRate { get; set; }
    public bool IsBossDrop { get; set; }
}