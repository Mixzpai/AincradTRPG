using Terminal.Gui;

namespace SAOTRPG.Map;

// Per-cell snapshot of TileDefinitions.GetVisual output. Animated types bypass cache.
// Lazy-populated on first access; invalidated by GameMap.OnTileTypeChanged.
public sealed class TileVisualCache
{
    private struct Slot { public char Ch; public Color Fg; public Color Bg; public bool Cached; }
    private readonly Slot[] _buf;
    private readonly int _w, _h;

    public TileVisualCache(int w, int h)
    {
        _w = w; _h = h;
        _buf = new Slot[w * h];
    }

    public int Width => _w;
    public int Height => _h;

    public (char Ch, Color Fg, Color Bg) Get(int x, int y, TileType type)
    {
        if ((uint)x >= (uint)_w || (uint)y >= (uint)_h)
            return TileDefinitions.GetVisual(type, x, y);
        if (TileDefinitions.IsAnimated(type))
        {
            using var _ = SAOTRPG.Systems.Profiler.Begin("TileVisual.Animated");
            var av = TileDefinitions.GetVisual(type, x, y);
            return (av.Glyph, av.Foreground, av.Background);
        }

        int idx = y * _w + x;
        ref Slot slot = ref _buf[idx];
        if (slot.Cached)
        {
            using var _ = SAOTRPG.Systems.Profiler.Begin("TileVisual.CacheHit");
            return (slot.Ch, slot.Fg, slot.Bg);
        }

        using (SAOTRPG.Systems.Profiler.Begin("TileVisual.CacheMiss"))
        {
            var v = TileDefinitions.GetVisual(type, x, y);
            slot.Ch = v.Glyph; slot.Fg = v.Foreground; slot.Bg = v.Background;
            slot.Cached = true;
            return (v.Glyph, v.Foreground, v.Background);
        }
    }

    public void Invalidate(int x, int y)
    {
        if ((uint)x >= (uint)_w || (uint)y >= (uint)_h) return;
        _buf[y * _w + x].Cached = false;
    }

    public void InvalidateAll()
    {
        for (int i = 0; i < _buf.Length; i++) _buf[i].Cached = false;
    }
}
