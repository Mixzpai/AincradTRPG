using Terminal.Gui;

namespace SAOTRPG.Systems.Story;

// A scripted cutscene: an ordered list of beats, optionally unskippable.
public record CutsceneScript(
    string EventId,
    string Title,
    CutsceneBeat[] Beats,
    bool Unskippable = false,
    bool IsReplay = false
);

// One line (or narration block) within a cutscene.
// PortraitKey indexes into AsciiPortraits; null means narration-only.
// Letterbox=true draws top/bottom black bars for non-diegetic world events.
public record CutsceneBeat(
    string? Speaker,
    string? PortraitKey,
    Color NameColor,
    string Text,
    bool Letterbox = false,
    CutsceneChoice[]? Choices = null
);

// A branching option on a beat. Applied immediately when the player picks it.
public record CutsceneChoice(
    string Label,
    string? Response,
    Action? Apply
);
