using System.Diagnostics;
using Terminal.Gui;
using SAOTRPG.Map;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Core tile resolution and post-effect pipeline.
// Converts map state into (char, fg, bg) per cell each frame.
public partial class MapView
{
    protected override bool OnDrawingContent()
    {
        using var _drawScope = Profiler.Begin("MapView.OnDrawingContent");
        // Wave 2: single canonical FrameClock tick per frame; dt threaded through render passes.
        int dtMs = FrameClock.Tick();
        var vp = Viewport;
        _camera.ViewWidth = vp.Width;
        _camera.ViewHeight = vp.Height;
        // CenterOn → then bolt the shake offset onto the coord mapper so every
        // MapToVx/VxToMap call picks up the jitter uniformly across layers.
        _camera.CenterOn(_player.X, _player.Y);

        // FOV radius from smaller viewport dim — terminal chars ~2:1 aspect → height limits.
        int halfH = vp.Height / 2 + 2;
        Map.DayNightCycle.ViewportRadius = halfH;
        Map.DayNightCycle.FovRadius = halfH * Map.DayNightCycle.FovMultiplier;

        TrackFootstep();

        // === H3 frame cache === Idle frames blit the last snapshot and skip recompute.
        if (!IsFrameDirty(vp.Width, vp.Height) && TryBlitFrame(vp.Width, vp.Height))
            return true;

        long memTotal = 0, visibleTotal = 0, lightingTotal = 0, tintTotal = 0, wallsTotal = 0, emitTotal = 0;
        int memCells = 0, visibleCells = 0;

        using (Profiler.Begin("MapView.TileLoop"))
        {
            for (int vy = 0; vy < vp.Height; vy++)
            for (int vx = 0; vx < vp.Width; vx++)
            {
                int mx = VxToMap(vx), my = VyToMap(vy);
                char ch; Color fg, bg;

                if (!_map.InBounds(mx, my) || !_map.IsExplored(mx, my))
                {
                    ch = ' '; fg = Color.Black; bg = Color.Black;
                }
                else if (!_map.IsVisible(mx, my))
                {
                    long t0 = Stopwatch.GetTimestamp();
                    (ch, fg, bg) = ResolveMemoryTile(mx, my);
                    memTotal += Stopwatch.GetTimestamp() - t0;
                    memCells++;
                }
                else
                {
                    long t0 = Stopwatch.GetTimestamp();
                    var tile = _map.GetTile(mx, my);
                    (ch, fg, bg) = ResolveVisibleTile(_map, tile, mx, my);
                    long t1 = Stopwatch.GetTimestamp();
                    visibleTotal += t1 - t0;

                    ApplyConnectedWalls(tile, mx, my, ref ch);
                    long t2 = Stopwatch.GetTimestamp();
                    wallsTotal += t2 - t1;

                    ApplyLighting(mx, my, ref fg, ref bg);
                    long t3 = Stopwatch.GetTimestamp();
                    lightingTotal += t3 - t2;

                    ApplyStatusTint(mx, my, ref fg);
                    tintTotal += Stopwatch.GetTimestamp() - t3;
                    visibleCells++;
                }

                long tEmit = Stopwatch.GetTimestamp();
                Gfx.PutCell(this, vx, vy, ch, fg, bg);
                emitTotal += Stopwatch.GetTimestamp() - tEmit;
            }
        }
        Profiler.RecordRaw("TileLoop.Memory", memTotal);
        Profiler.RecordRaw("TileLoop.Visible", visibleTotal);
        Profiler.RecordRaw("TileLoop.ConnectedWalls", wallsTotal);
        Profiler.RecordRaw("TileLoop.Lighting", lightingTotal);
        Profiler.RecordRaw("TileLoop.StatusTint", tintTotal);
        Profiler.RecordRaw("TileLoop.DriverEmit", emitTotal);
        Profiler.RecordCount("TileLoop.MemCells", memCells);
        Profiler.RecordCount("TileLoop.VisibleCells", visibleCells);

        RenderOverlays(vp.Width, vp.Height, dtMs);
        TickFrameEffects(dtMs);

        // Always capture the post-render frame so the cache has a snapshot for any
        // quiet window. Active animations still mark the next frame dirty so counters tick.
        CaptureFrame(vp.Width, vp.Height);
        bool active = HasActiveAnimations();
        if (active || _animTailDirty)
        {
            DirtyFrame();
            _animTailDirty = active;
        }
        return true;
    }

    // True when any time-bounded visual effect is still alive — keeps the cache dirty so
    // the next frame retraces the recompute path and ticks counters.
    private bool HasActiveAnimations()
    {
        return _hitFlashes.Count > 0
            || _doorFlashes.Count > 0
            || _critScreenFlashRemainingMs > 0
            || _bossEntranceRemainingMs > 0
            || _borderFlashRemainingMs > 0
            || _levelUpFlashRemainingMs > 0
            || _killStreakFlashRemainingMs > 0
            || _damageFlashes.Count > 0
            || _polyBursts.Count > 0
            || _skillFlashes.Count > 0
            || _weaponSwings.Count > 0
            || _scorchMarks.Count > 0
            || _corpseMarkers.Count > 0
            || HasActivePopups
            || IsShaking
            || ParticleQueue.HasAny
            || ToastQueue.Peek() != null;
    }

    // Public gate for the 50ms render timer. True when any time-bounded effect is alive
    // OR a floor with animated tiles is loaded.
    public bool HasActiveRealtimeAnimations()
    {
        return HasActiveAnimations() || _map.HasAnimatedTiles;
    }

    private void TickFrameEffects(int dtMs)
    {
        if (_critScreenFlashRemainingMs > 0)
            _critScreenFlashRemainingMs = Math.Max(0, _critScreenFlashRemainingMs - dtMs);
        for (int i = _hitFlashes.Count - 1; i >= 0; i--)
        {
            var f = _hitFlashes[i];
            if (f.RemainingMs <= dtMs)
            {
                _hitFlashes.RemoveAt(i);
                // Only drop from set once no other active entry flashes this tile (dup-hit safe).
                bool stillFlashing = false;
                for (int j = 0; j < _hitFlashes.Count; j++)
                {
                    if (_hitFlashes[j].X == f.X && _hitFlashes[j].Y == f.Y)
                    { stillFlashing = true; break; }
                }
                if (!stillFlashing) _hitFlashSet.Remove((f.X, f.Y));
            }
            else _hitFlashes[i] = (f.X, f.Y, f.RemainingMs - dtMs);
        }
        for (int i = _doorFlashes.Count - 1; i >= 0; i--)
        {
            var f = _doorFlashes[i];
            if (f.RemainingMs <= dtMs) _doorFlashes.RemoveAt(i);
            else _doorFlashes[i] = (f.X, f.Y, f.RemainingMs - dtMs);
        }
        if (_bossEntranceRemainingMs > 0)
            _bossEntranceRemainingMs = Math.Max(0, _bossEntranceRemainingMs - dtMs);
    }

    private void TrackFootstep()
    {
        if (_player.X == _lastPlayerPos.X && _player.Y == _lastPlayerPos.Y) return;
        if (_lastPlayerPos.X >= 0)
        {
            _footsteps.Enqueue(_lastPlayerPos);
            if (_footsteps.Count > FootstepTrailLength) _footsteps.Dequeue();
        }
        _lastPlayerPos = (_player.X, _player.Y);
    }

    private (char ch, Color fg, Color bg) ResolveTileVisual(int mx, int my)
    {
        if (!_map.InBounds(mx, my)) return (' ', Color.Black, Color.Black);
        if (!_map.IsExplored(mx, my)) return (' ', Color.Black, Color.Black);
        if (!_map.IsVisible(mx, my)) return ResolveMemoryTile(mx, my);

        var tile = _map.GetTile(mx, my);
        var (ch, fg, bg) = ResolveVisibleTile(_map, tile, mx, my);
        ApplyConnectedWalls(tile, mx, my, ref ch);
        ApplyLighting(mx, my, ref fg, ref bg);
        ApplyStatusTint(mx, my, ref fg);
        return (ch, fg, bg);
    }

    // Memory tint: explored-but-unseen = deep cool blue; structural (walls/doors/stairs) brighter.
    private static readonly Color MemoryStructural = new(80, 100, 140);
    private static readonly Color MemoryTerrain    = new(35,  45,  75);
    private static readonly Color MemoryLandmark   = new(110, 130, 170);

    private (char ch, Color fg, Color bg) ResolveMemoryTile(int mx, int my)
    {
        var memTile = _map.GetTile(mx, my);
        var tileType = memTile.Type;
        if (memTile.TrapHidden && tileType is TileType.TrapSpike or TileType.TrapTeleport
            or TileType.TrapPoison or TileType.TrapAlarm)
            tileType = TileType.Floor;

        var visual = _visualCache.Get(mx, my, tileType);
        char ch = visual.Ch;
        if (tileType is TileType.Wall or TileType.CrackedWall)
            ch = MapEffects.GetConnectedWallGlyph(_map, mx, my);
        if (tileType == TileType.Door && _openedDoors.Contains((mx, my))) ch = '·';

        Color fg = tileType switch
        {
            TileType.Wall or TileType.CrackedWall or TileType.Mountain => MemoryStructural,
            TileType.Door or TileType.StairsUp or TileType.StairsDown  => MemoryLandmark,
            TileType.Shrine or TileType.Fountain or TileType.Anvil
                or TileType.BountyBoard or TileType.EnchantShrine
                or TileType.SecretShrine
                or TileType.Pillar or TileType.Chest or TileType.Campfire
                or TileType.LoreStone or TileType.Journal             => MemoryLandmark,
            _                                                          => MemoryTerrain,
        };

        // Fading memory: full strength for 40 turns, then fades to ~40% over the next 200.
        int age = Map.DayNightCycle.CurrentTurn - _map.GetLastSeenTurn(mx, my);
        if (age > 40)
        {
            float t = Math.Min(1f, (age - 40) / 200f);
            float scale = 1f - 0.6f * t;
            fg = new Color((byte)(fg.R * scale), (byte)(fg.G * scale), (byte)(fg.B * scale));
        }
        return (ch, fg, Color.Black);
    }

    private (char ch, Color fg, Color bg) ResolveVisibleTile(Map.GameMap map, Map.Tile tile, int mx, int my)
    {
        if (tile.Occupant != null && !tile.Occupant.IsDefeated) return ResolveOccupant(tile, mx, my);
        if (map.HasItemsAt(mx, my))
        {
            var (glyph, color) = GetItemRarityVisual(map.GetItemsAt(mx, my));
            return (glyph, color, Color.Black);
        }
        return ResolveTerrain(tile, mx, my);
    }

    private (char ch, Color fg, Color bg) ResolveOccupant(Map.Tile tile, int mx, int my)
    {
        char ch = tile.Occupant!.Symbol;
        Color fg = tile.Occupant.SymbolColor;
        if (tile.Occupant == _player)
        {
            double hpPct = _player.MaxHealth > 0 ? (double)_player.CurrentHealth / _player.MaxHealth : 1.0;
            fg = hpPct switch
            {
                > 0.50 => Color.BrightYellow,
                > 0.25 => Color.Yellow,
                > 0.10 => Color.BrightRed,
                _      => Color.Red,
            };
        }
        return (ch, fg, Color.Black);
    }

    private (char ch, Color fg, Color bg) ResolveTerrain(Map.Tile tile, int mx, int my)
    {
        var renderType = tile.Type;
        if (tile.TrapHidden && renderType is TileType.TrapSpike or TileType.TrapTeleport
            or TileType.TrapPoison or TileType.TrapAlarm)
            renderType = TileType.Floor;

        var visual = _visualCache.Get(mx, my, renderType);
        char ch = visual.Ch; Color fg = visual.Fg; Color bg = visual.Bg;
        if (tile.Type == TileType.Door && _openedDoors.Contains((mx, my))) ch = '·';
        if (tile.Type is TileType.Water or TileType.WaterDeep)
            ResolveWater(tile, mx, my, ref ch, ref fg);

        var transition = MapEffects.GetTransitionBorder(_map, mx, my, tile.Type);
        if (transition != null) { ch = transition.Value.Glyph; fg = transition.Value.Color; }

        var roomTheme = MapEffects.GetRoomTheme(mx, my, tile.Type);
        if (roomTheme.Tint != null && fg == Color.Gray) fg = roomTheme.Tint.Value;
        if (roomTheme.Glyph != null && tile.Type == TileType.Floor) ch = roomTheme.Glyph.Value;
        return (ch, fg, bg);
    }

    private void ResolveWater(Map.Tile tile, int mx, int my, ref char ch, ref Color fg)
    {
        var reflected = GetReflectedEntity(mx, my);
        if (reflected != null) { ch = reflected.Symbol; fg = Color.DarkGray; }
        else
        {
            var lilyPad = (tile.Type == TileType.Water) ? MapEffects.GetWaterLilyPad(mx, my) : null;
            if (lilyPad != null) { ch = lilyPad.Value.Glyph; fg = lilyPad.Value.Color; }
            else ch = MapEffects.GetWaterFlowGlyph(mx, my);
        }
        var shore = MapEffects.GetShorelineVisual(_map, mx, my, tile.Type);
        if (shore != null) { ch = shore.Value.Glyph; fg = shore.Value.Fg; }
    }

    private void ApplyConnectedWalls(Map.Tile tile, int mx, int my, ref char ch)
    {
        if (tile.Type is TileType.Wall or TileType.CrackedWall)
            ch = MapEffects.GetConnectedWallGlyph(_map, mx, my);
    }

    private const float InvLightScale = 1f / 255f;

    // Light-color bleed into cell bg — keeps glyph readable while producing Brogue-style ambient glow.
    private const float BgGlowScale = 0.12f;

    // fg = base × light (warm→orange near campfire/lava, cool→cyan near fountain);
    // bg = additive dim glow (implicit ≤50 cap since light ≤255 × 0.12 ≈ 30.6).
    private void ApplyLighting(int mx, int my, ref Color fg, ref Color bg)
    {
        var light = _map.Lighting.GetLightUnchecked(mx, my);

        // Foreground: multiply base color by light → warm/cool tint.
        byte fgR = (byte)(fg.R * light.R * InvLightScale);
        byte fgG = (byte)(fg.G * light.G * InvLightScale);
        byte fgB = (byte)(fg.B * light.B * InvLightScale);
        fg = new Color(fgR, fgG, fgB);

        // Dim colored bg glow — floor picks up light source hue (orange near campfire, cyan near fountain).
        byte bgR = (byte)(light.R * BgGlowScale);
        byte bgG = (byte)(light.G * BgGlowScale);
        byte bgB = (byte)(light.B * BgGlowScale);
        bg = new Color(bgR, bgG, bgB);

        // Hit flash: override bg with dim red on tiles that just took damage.
        if (IsHitFlashed(mx, my)) bg = new Color(120, 10, 10);

        // Crit screen flash: brief fg→white blend across all visible tiles.
        if (_critScreenFlashRemainingMs > 0)
        {
            fg = new Color(
                (byte)Math.Min(255, fg.R + 80),
                (byte)Math.Min(255, fg.G + 80),
                (byte)Math.Min(255, fg.B + 80));
        }
    }

    private void ApplyStatusTint(int mx, int my, ref Color fg)
    {
        if (_statusTintColor != null && (mx + my) % 3 == 0) fg = _statusTintColor.Value;
    }

    private Entities.Entity? GetReflectedEntity(int x, int y)
    {
        if (!_map.InBounds(x, y - 1)) return null;
        var above = _map.GetTile(x, y - 1);
        return (above.Occupant != null && !above.Occupant.IsDefeated) ? above.Occupant : null;
    }

    private static (char Glyph, Color Color) GetItemRarityVisual(IReadOnlyList<Items.BaseItem> items)
    {
        int bestRank = 0;
        foreach (var item in items)
        {
            int rank = item.Rarity switch
            {
                "Legendary" => 4, "Epic" => 3, "Rare" => 2, "Uncommon" => 1, _ => 0,
            };
            if (rank > bestRank) bestRank = rank;
        }
        return bestRank switch
        {
            4 => ('*', Color.BrightMagenta), 3 => ('*', Color.BrightYellow),
            2 => ('◆', Color.BrightCyan), 1 => ('◇', Color.BrightGreen),
            _ => ('•', Color.BrightYellow),
        };
    }
}
