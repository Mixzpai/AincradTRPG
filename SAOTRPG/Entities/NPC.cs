using Terminal.Gui;
using SAOTRPG.Systems;

namespace SAOTRPG.Entities
{
    // Friendly NPC — bump to interact. Single-line Dialogue or branching DialogueLines.
    public abstract class NPC : Entity
    {
        public override char Symbol { get; protected set; } = 'N';
        public override Color SymbolColor { get; protected set; } = Color.BrightCyan;

        public string Dialogue { get; set; } = string.Empty;
        public bool CanInteract { get; set; } = true;

        // Non-null → bump opens dialog instead of logging Dialogue.
        public DialogueLine[]? DialogueLines { get; set; }

        // Optional AsciiPortraits key. Null/unknown → auto-resolve from Name.
        public string? PortraitKey { get; set; }
    }
}
