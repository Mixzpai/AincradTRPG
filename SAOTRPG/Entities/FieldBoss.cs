namespace SAOTRPG.Entities;

// Named wilderness elite with 100% guaranteed canon drop. Never respawns in-run; may be event-gated (Nicholas=Christmas).
public class FieldBoss : Boss
{
    public string FieldBossId { get; set; } = "";
    // 100% drop DefinitionId on kill.
    public string? GuaranteedDropId { get; set; }

    // Flavor line on floor entry when present.
    public string EncounterFlavor { get; set; } = "";

    // Seasonal gate — spawn only when SeasonalEvents active state matches SeasonalEventId.
    public bool IsSeasonal { get; set; }
    public string? SeasonalEventId { get; set; }

    // Factory glyph/color customization (mirrors GenericBoss).
    public void SetBossTitle(string title) => BossTitle = title;
    public void SetSymbol(char sym) => Symbol = sym;
    public void SetColor(Terminal.Gui.Color col) => SymbolColor = col;
}
