using Terminal.Gui;

namespace SAOTRPG.UI.Helpers;

public enum ThemeId { Default = 0, HighContrast = 1, ColourblindSafe = 2, AmberMono = 3 }

// One complete set of role colours. Every scheme in ColorSchemes is assembled from these ten
// values, so a theme is exactly this record and nothing else — which is what makes swapping one
// a whole-interface change rather than a hunt through call sites.
//
// The rules a theme has to keep, inherited from the palette design:
//   1. At most one saturated accent. Everything else is a value ramp between Muted and Emph.
//   2. Hierarchy lives in BRIGHTNESS, hue only reinforcing it — strip the colour out and the
//      screens must still read. That is also what makes a colourblind theme possible at all.
//
// Ok / Warn / Info / Crit are the four SEMANTIC hues. They are what the HP bar zones, the status
// tray and the log categories paint with, so a theme reaches them; and in Colourblind Safe they
// are separated by luminance as well as hue, because that is the only channel that survives.
public sealed record ColorTheme(
    ThemeId Id,
    string Name,
    string Description,
    Color Base,
    Color Surface,
    Color Overlay,
    Color Muted,
    Color Body,
    Color Emph,
    Color Accent,
    Color AccentDim,
    Color Ok,
    Color Warn,
    Color Info,
    Color Crit)
{
    public static readonly ColorTheme Default = new(
        ThemeId.Default, "Default", "Amber accent on black.",
        Base:    new(0x00, 0x00, 0x00),
        Surface: new(0x28, 0x32, 0x44),
        Overlay: new(0x16, 0x1A, 0x24),
        Muted:   new(0x5A, 0x64, 0x72),
        Body:    new(0x9A, 0xA7, 0xB8),
        Emph:    new(0xE6, 0xED, 0xF5),
        Accent:    new(0xFF, 0xB4, 0x54),
        AccentDim: new(0x8A, 0x64, 0x30),
        Ok:   new(0x52, 0xC7, 0x7E),
        Warn: new(0xE0, 0xB1, 0x3E),
        Info: new(0x5A, 0xB0, 0xE0),
        Crit: new(0xE5, 0x48, 0x4D));

    // Pushes the text ramp apart and brightens the accent. Only VALUES move — the hues are the
    // default theme's — so it helps on a dim panel, in glare, or with low vision without
    // changing what any colour means.
    public static readonly ColorTheme HighContrast = new(
        ThemeId.HighContrast, "High Contrast", "Wider tonal range for dim screens and glare.",
        Base:    new(0x00, 0x00, 0x00),
        Surface: new(0x36, 0x42, 0x56),
        Overlay: new(0x28, 0x30, 0x3E),
        Muted:   new(0x9A, 0xA7, 0xB8),
        Body:    new(0xCB, 0xD6, 0xE4),
        Emph:    new(0xFF, 0xFF, 0xFF),
        Accent:    new(0xFF, 0xCE, 0x70),
        AccentDim: new(0xB4, 0x88, 0x40),
        Ok:   new(0x7B, 0xE8, 0xA4),
        Warn: new(0xFF, 0xD8, 0x6B),
        Info: new(0x8C, 0xD6, 0xFF),
        Crit: new(0xFF, 0x7B, 0x7B));

    // Retires the red/green status pair, which is the one genuinely dangerous collision here:
    // deuteranopia and protanopia together affect roughly 8% of men, and both flatten red
    // against green. Ok and Crit move to a blue/orange axis, which survives all three common
    // types, and the accent moves off amber so it cannot be confused with Crit.
    public static readonly ColorTheme ColourblindSafe = new(
        ThemeId.ColourblindSafe, "Colourblind Safe", "Blue/orange status axis instead of red/green.",
        Base:    new(0x00, 0x00, 0x00),
        Surface: new(0x25, 0x30, 0x40),
        Overlay: new(0x18, 0x1E, 0x28),
        Muted:   new(0x74, 0x80, 0x8F),
        Body:    new(0xA8, 0xB4, 0xC4),
        Emph:    new(0xEF, 0xF4, 0xFA),
        Accent:    new(0x6C, 0xB4, 0xFF),
        AccentDim: new(0x36, 0x5C, 0x8C),
        Ok:   new(0x5A, 0xA8, 0xEC),
        Warn: new(0x8A, 0x5A, 0x18),
        Info: new(0x8E, 0xC8, 0xF5),
        Crit: new(0xFF, 0xC4, 0x68));

    // A single-hue terminal, the SAO system-window motif taken literally. Aesthetic rather than
    // accessible, but it is the strongest test that hierarchy really does live in brightness:
    // with one hue, value is the only thing left to carry it.
    public static readonly ColorTheme AmberMono = new(
        ThemeId.AmberMono, "Amber Mono", "Single-hue amber terminal.",
        Base:    new(0x00, 0x00, 0x00),
        Surface: new(0x3A, 0x28, 0x08),
        Overlay: new(0x22, 0x17, 0x04),
        Muted:   new(0x8E, 0x63, 0x18),
        Body:    new(0xC2, 0x8B, 0x28),
        Emph:    new(0xFF, 0xD2, 0x80),
        Accent:    new(0xFF, 0xB4, 0x2E),
        AccentDim: new(0x8A, 0x60, 0x18),
        Ok:   new(0xE0, 0xB0, 0x50),
        Warn: new(0xB0, 0x80, 0x2A),
        Info: new(0x8A, 0x63, 0x18),
        Crit: new(0xFF, 0xE2, 0xA8));

    // ── The world ────────────────────────────────────────────────────
    //
    // Tiles, terrain and weather are NOT role colours. A tile's colour is an identity — grass is
    // green — not a semantic slot, so a theme TRANSFORMS the authored colour rather than replacing
    // it. That keeps one authored palette instead of four, and means a tile added later follows
    // every theme without anyone picking three more constants for it.
    //
    // Default and Colourblind Safe are the IDENTITY transform: the world carries no information
    // that hue alone conveys (rarity, tier and status are all glyph- or letter-backed), so
    // recolouring it would buy a colourblind player nothing and cost the game its look.
    public bool TintsWorld => Id is ThemeId.AmberMono or ThemeId.HighContrast;

    public Color World(Color c) => Id switch
    {
        ThemeId.AmberMono    => Amber(c),
        ThemeId.HighContrast => Brighten(c),
        _                    => c,
    };

    // Collapse to one hue, keeping relative brightness so the world's value structure survives —
    // a lit campfire still reads brighter than the stone around it.
    private static Color Amber(Color c)
    {
        int y = (299 * c.R + 587 * c.G + 114 * c.B) / 1000;
        return new Color(Math.Min(255, y * 5 / 4), y * 46 / 64, y * 15 / 64);
    }

    // Lifts the world the way the theme lifts the interface: mid tones move most, so terrain
    // separates without the bright end clipping to white.
    private static Color Brighten(Color c) => new(
        Math.Min(255, c.R + (255 - c.R) / 4),
        Math.Min(255, c.G + (255 - c.G) / 4),
        Math.Min(255, c.B + (255 - c.B) / 4));

    public static readonly ColorTheme[] All = [Default, HighContrast, ColourblindSafe, AmberMono];

    public static ColorTheme ById(ThemeId id) =>
        All.FirstOrDefault(t => t.Id == id) ?? Default;
}
