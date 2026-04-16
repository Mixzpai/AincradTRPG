namespace SAOTRPG.Entities;

// Named roaming elite that spawns in the wilderness, not the labyrinth boss
// room. Field bosses drop a canonical guaranteed item on death, never respawn
// in the same run, and may be event-gated (Nicholas only appears at Christmas).
public class FieldBoss : Boss
{
    // Unique identifier for persistence. E.g. "frost_dragon_f48", "nicholas_f49".
    public string FieldBossId { get; set; } = "";

    // DefinitionId of the guaranteed drop on kill — dropped 100% of the time.
    public string? GuaranteedDropId { get; set; }

    // Flavor line emitted on floor entry when this boss is present.
    public string EncounterFlavor { get; set; } = "";

    // Seasonal / conditional gating (date-driven). If true, spawn only when
    // SeasonalEvents active state matches SeasonalEventId.
    public bool IsSeasonal { get; set; }
    public string? SeasonalEventId { get; set; }

    // Public setters mirroring GenericBoss pattern so factory can customize glyph.
    public void SetBossTitle(string title) => BossTitle = title;
    public void SetSymbol(char sym) => Symbol = sym;
    public void SetColor(Terminal.Gui.Color col) => SymbolColor = col;
}
