namespace SAOTRPG.Map.Generation;

// Unit of work in the generation pipeline. Pipeline iterates passes in order;
// ShouldRun lets biome config toggle passes off (e.g. RiverAlgorithm.None).
public interface IGenerationPass
{
    string Name { get; }
    bool ShouldRun(WorldContext ctx);
    void Execute(WorldContext ctx);
}
