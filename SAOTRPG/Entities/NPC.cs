using Terminal.Gui;

namespace SAOTRPG.Entities
{
    public abstract class NPC : Entity
    {
        public override char Symbol { get; protected set; } = 'N';
        public override Color SymbolColor { get; protected set; } = Color.BrightCyan;
        /****************************************************************************************/
        // NPC-Specific Properties
        public string Dialogue { get; set; }
        public bool CanInteract { get; set; } = true;
    }
}