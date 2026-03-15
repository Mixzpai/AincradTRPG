using Terminal.Gui;

namespace SAOTRPG.Entities
{
    public class WorldSpawn : NPC
    {
        public override char Symbol { get; protected set; } = 'N';
        public override Color SymbolColor { get; protected set; } = Color.BrightCyan;
        /****************************************************************************************/
        // Basic world character - background NPCs
    }
}