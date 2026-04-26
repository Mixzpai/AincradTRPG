using Terminal.Gui;

namespace SAOTRPG.Map;

// Ambient-tile frame registry. Maps TileType → animation (frames + per-frame ms).
// MapView renders from the registry each frame, FOV-gated and density-capped to 3.
public static class TileAnimator
{
    // A single animation: frame glyphs cycled at IntervalMs per frame, rendered in Color.
    public readonly record struct Animation(char[] Frames, int IntervalMs, Color Color, int Priority);

    // Priority values from research §4 (higher = render first when over the density cap).
    public const int PriorityChest   = 70;
    public const int PriorityTerminal = 60;
    public const int PriorityHearth  = 50;
    public const int PriorityTorch   = 45;
    public const int PriorityVent    = 40;
    public const int PriorityFan     = 30;
    public const int PriorityWater   = 20;

    // Tile → animation. Only tiles listed here ever animate; missing = static tile.
    public static readonly Dictionary<TileType, Animation> Registry = new()
    {
        // Vent steam rising — dim gray drift.
        [TileType.GasVent] = new Animation(
            new[] { '·', '\'', '`', '`' }, IntervalMs: 500, new Color(130, 200, 130), PriorityVent),

        // Fountain water ripple (aquatic-floor tiles share this).
        [TileType.Fountain] = new Animation(
            new[] { '~', '≈', '~' }, IntervalMs: 800, new Color(100, 220, 255), PriorityWater),

        // Hearth flicker — Campfire warm cue; auto-disabled during combat.
        [TileType.Campfire] = new Animation(
            new[] { '·', '*' }, IntervalMs: 700, new Color(255, 200, 80), PriorityHearth),

        // Shrine flicker as torch-sconce stand-in (gold flicker, combat-suppressed).
        [TileType.EnchantShrine] = new Animation(
            new[] { '¤', '*' }, IntervalMs: 700, new Color(255, 220, 80), PriorityTorch),

        // Mechanical fan — rotating blade; fast 4-frame cycle.
        [TileType.Lever] = new Animation(
            new[] { '|', '/', '-', '\\' }, IntervalMs: 200, new Color(180, 180, 180), PriorityFan),

        // Terminal (BountyBoard stand-in) — blinking cursor.
        [TileType.BountyBoard] = new Animation(
            new[] { '_', ' ' }, IntervalMs: 600, new Color(100, 220, 255), PriorityTerminal),

        // Chest sparkle — sparse glint; 200ms bursts inside a 2000ms cycle.
        [TileType.Chest] = new Animation(
            new[] { ' ' }, IntervalMs: 250, new Color(255, 220, 80), PriorityChest),
    };

    // Hearth/torch categories suppressed during combat (research §4).
    public static bool IsCombatSuppressed(TileType type) =>
        type is TileType.Campfire or TileType.EnchantShrine;

    // Chest is an overlay sparkle (not a replacement glyph) — treated as an event burst.
    public static bool IsSparkleOverlay(TileType type) => type is TileType.Chest;

    // Resolve the current-frame glyph for a tile, using the wall-clock so animations run
    // smoothly regardless of viewport redraw cadence.
    public static (char Glyph, Color Color) CurrentFrame(TileType type)
    {
        if (!Registry.TryGetValue(type, out var anim) || anim.Frames.Length == 0)
            return ('?', Color.White);
        long now = SAOTRPG.Systems.FrameClock.ElapsedMs;
        int idx = (int)((now / anim.IntervalMs) % anim.Frames.Length);
        return (anim.Frames[idx], anim.Color);
    }

    // Chest glint: 200ms burst every 2000ms. Returns the sparkle glyph or null if the
    // sparkle is in the "off" phase of its cycle.
    public static (char Glyph, Color Color)? ChestSparkle()
    {
        long now = SAOTRPG.Systems.FrameClock.ElapsedMs;
        long phase = now % 2000;
        if (phase > 200) return null;
        char[] glyphs = { '·', '◇', '*' };
        int idx = (int)((phase / 70) % glyphs.Length);
        return (glyphs[idx], new Color(255, 240, 120));
    }

    // Combat-suppression flag. MapView sets this each frame from TurnManager.LastCombatTurn.
    public static bool CombatActive { get; set; }

    // Global on/off. Future Options toggle can set this; default on.
    public static bool Enabled { get; set; } = true;

    // Max concurrent animated tiles within FOV (research §4 density cap).
    public const int MaxConcurrent = 3;
}
