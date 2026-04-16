using Terminal.Gui;
using SAOTRPG.Systems;

namespace SAOTRPG.Entities
{
    // Non-player character — friendly entities the player can interact with by bumping.
    // Rendered as 'N' in cyan. Supports simple dialogue or branching dialogue trees.
    public abstract class NPC : Entity
    {
        public override char Symbol { get; protected set; } = 'N';
        public override Color SymbolColor { get; protected set; } = Color.BrightCyan;

        /****************************************************************************************/
        // NPC-Specific Properties

        // Simple single-line dialogue shown in the game log on bump.
        public string Dialogue { get; set; } = string.Empty;
        // Whether this NPC can be interacted with. False disables bump interaction.
        public bool CanInteract { get; set; } = true;

        // Optional branching dialogue. When set, bumping opens the NPC
        // dialog instead of logging a single line. Null = use Dialogue.
        public DialogueLine[]? DialogueLines { get; set; }
    }
}
