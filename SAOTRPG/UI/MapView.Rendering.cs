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
    // ── Draw-cost sampling (diagnostic — see DebugMode.PerfSampling) ──
    // Accumulated by every draw, drained once per second by the main-loop sampler
    // so a slow frame can be told apart from a starved loop.
    private static readonly bool s_sampling = SAOTRPG.UI.DebugMode.PerfSampling;
    private static double s_drawTotalMs, s_drawMaxMs;
    private static int s_drawCount;

    internal static double DrainDrawAvgMs()
    {
        double avg = s_drawCount == 0 ? 0 : s_drawTotalMs / s_drawCount;
        s_drawTotalMs = 0; s_drawCount = 0;
        return avg;
    }

    internal static double DrainDrawMaxMs()
    {
        double max = s_drawMaxMs;
        s_drawMaxMs = 0;
        return max;
    }

    // Tile loops run against frames that painted tiles at all. The tile loop costs roughly
    // eleven times a tile-layer blit, so this ratio is the direct readout of whether the
    // layered cache is actually skipping the loop.
    private static int s_tileLoops, s_tilePaints;

    internal static (int loops, int paints) DrainTileLayerCounts()
    {
        var result = (s_tileLoops, s_tilePaints);
        s_tileLoops = 0; s_tilePaints = 0;
        return result;
    }

    // The tile loop paints every cell of the viewport every pass, so the framework's
    // pre-draw clear is pure duplicate output. Worse, it blanks the buffer, so the
    // previous frame can never be diffed against and every cell counts as changed.
    // Cancelling it halves the writes and lets unchanged frames cost nothing.
    protected override bool OnClearingViewport() => true;

    // Raw timestamps of this view's last paint. The loop sampler brackets them against
    // the iteration start and the draw-complete event to split a frame into: work before
    // any view painted, this view's own paint, and everything after it (remaining views
    // plus the driver's flush to the terminal).
    internal static long LastDrawStartTicks, LastDrawEndTicks;

    protected override bool OnDrawingContent(DrawContext? context)
    {
        // A modal above us has already painted this pass; drawing now would erase it.
        if (AppHost.IsBeneathTopSession(this)) return true;

        if (!s_sampling) return DrawContent(context);

        long _drawStart = Stopwatch.GetTimestamp();
        LastDrawStartTicks = _drawStart;
        try
        {
            return DrawContent(context);
        }
        finally
        {
            long end = Stopwatch.GetTimestamp();
            LastDrawEndTicks = end;
            double ms = (end - _drawStart) * 1000.0 / Stopwatch.Frequency;
            s_drawTotalMs += ms;
            s_drawCount++;
            if (ms > s_drawMaxMs) s_drawMaxMs = ms;
        }
    }

    private bool DrawContent(DrawContext? context)
    {
        using var _drawScope = Profiler.Begin("MapView.OnDrawingContent");
        // Cleared up front so the full-frame blit path — which returns before overlays and
        // never builds a batch — can never leave last frame's stale clip in place.
        _frameBatch = null;
        // Single canonical FrameClock tick per frame; dt threaded through render passes.
        int dtMs = FrameClock.Tick();
        var vp = Viewport;
        _camera.ViewWidth = vp.Width;
        _camera.ViewHeight = vp.Height;
        // CenterOn → then bolt the shake offset onto the coord mapper so every
        // MapToVx/VxToMap call picks up the jitter uniformly across layers.
        _camera.CenterOn(_player.X, _player.Y);

        // FOV radius from smaller viewport dim — terminal chars ~2:1 aspect → height limits.
        int halfH = vp.Height / 2 + 2;
        Map.DayNightCycle.FovRadius = halfH * Map.DayNightCycle.FovMultiplier;

        // Position compare is the backstop for movement that leaves no invalidation behind:
        // auto-explore steps on a timer rather than a keypress, and a step that neither
        // damages anything nor moves a monster otherwise reaches the cache test clean and
        // gets served a stale frame. The camera only ever follows the player, so this covers
        // scrolling too. Must run before the cache test.
        if (TrackFootstep()) DirtyFrame();

        // Fully idle frames blit the finished frame and paint nothing at all.
        if (!IsFrameDirty(vp.Width, vp.Height) && TryBlitFrame(vp.Width, vp.Height))
        {
                return true;
        }

        var batch = Gfx.Begin(this);
        // Overlays share the tile loop's batch — same view, same clip, same paint.
        _frameBatch = batch;

        // Tiles static but overlays alive — the common case while any effect is running.
        // Replaying the tile layer costs about an eleventh of the tile loop and clears the
        // previous frame's overlays on the way through. Clock-driven tiles are then re-resolved
        // on top, so a handful of them no longer forfeits the whole replay.
        bool tilesPainted = !IsTileLayerDirty(vp.Width, vp.Height)
                            && CanPatchTimeVaryingCells(vp.Width, vp.Height)
                            && TryBlitTileLayer(vp.Width, vp.Height);
        if (tilesPainted)
        {
            RepaintTimeVaryingCells(batch);
        }
        else
        {
            RunTileLoop(batch, vp.Width, vp.Height);
            CaptureTileLayer(vp.Width, vp.Height);
        }
        if (s_sampling)
        {
            s_tilePaints++;
            if (!tilesPainted) s_tileLoops++;
        }

        RenderOverlays(vp.Width, vp.Height, dtMs);
        TickFrameEffects(dtMs);

        // Always capture the post-render frame so the cache has a snapshot for any
        // quiet window. Active animations still mark the next frame dirty so counters tick.
        CaptureFrame(vp.Width, vp.Height);
        bool active = HasActiveAnimations();
        if (active || _animTailDirty)
        {
            DirtyOverlays();
            _animTailDirty = active;
        }
        // Effects the tile loop itself reads get the same treatment against the tile layer.
        bool tileActive = HasTileLayerAnimations();
        if (tileActive || _tileTailDirty)
        {
            _tileDirty = true;
            _tileTailDirty = tileActive;
        }
        return true;
    }

    // Paints every cell of the viewport from map state. Exhaustive by design: the tile
    // layer snapshot taken straight after it is only a valid erase-and-replace for the
    // overlays because no cell is left untouched.
    private void RunTileLoop(Gfx.Batch batch, int vpWidth, int vpHeight)
    {
        long memTotal = 0, visibleTotal = 0, lightingTotal = 0, tintTotal = 0, wallsTotal = 0, emitTotal = 0;
        int memCells = 0, visibleCells = 0;
        // Stopwatch reads are not free at viewport scale (nine per cell); only pay for
        // them when the profiler is actually collecting.
        bool prof = Profiler.Enabled;
        // Recomputed each pass: does the viewport currently show an animated tile?
        // GameMap.HasAnimatedTiles is map-wide, so on any floor containing water it
        // stays true forever and keeps the 50ms repaint timer firing even when no
        // animated tile is on screen.
        bool animatedInView = false;
        _timeVaryingCells.Clear();
        var timings = default(TileLoopTimings);

        using (Profiler.Begin("MapView.TileLoop"))
        {
            for (int vy = 0; vy < vpHeight; vy++)
            for (int vx = 0; vx < vpWidth; vx++)
            {
                int mx = VxToMap(vx), my = VyToMap(vy);
                char ch; Color fg, bg;

                if (!_map.InBounds(mx, my) || !_map.IsExplored(mx, my))
                {
                    ch = ' '; fg = Color.Black; bg = Color.Black;
                }
                else if (!_map.IsVisible(mx, my))
                {
                    long t0 = prof ? Stopwatch.GetTimestamp() : 0L;
                    (ch, fg, bg) = ResolveMemoryTile(mx, my);
                    if (prof) memTotal += Stopwatch.GetTimestamp() - t0;
                    memCells++;
                }
                else
                {
                    ResolveVisibleCell(mx, my, prof, ref timings, out var type, out ch, out fg, out bg);
                    if (TileDefinitions.IsAnimated(type)) animatedInView = true;
                    if (IsTimeVaryingTile(type)) _timeVaryingCells.Add((vx, vy));
                    visibleCells++;
                }

                long tEmit = prof ? Stopwatch.GetTimestamp() : 0L;
                batch.Put(vx, vy, ch, Gfx.Attr(fg, bg));
                if (prof) emitTotal += Stopwatch.GetTimestamp() - tEmit;
            }
        }
        visibleTotal = timings.Visible;
        wallsTotal = timings.Walls;
        lightingTotal = timings.Lighting;
        tintTotal = timings.Tint;
        Profiler.RecordRaw("TileLoop.Memory", memTotal);
        Profiler.RecordRaw("TileLoop.Visible", visibleTotal);
        Profiler.RecordRaw("TileLoop.ConnectedWalls", wallsTotal);
        Profiler.RecordRaw("TileLoop.Lighting", lightingTotal);
        Profiler.RecordRaw("TileLoop.StatusTint", tintTotal);
        Profiler.RecordRaw("TileLoop.DriverEmit", emitTotal);
        Profiler.RecordCount("TileLoop.MemCells", memCells);
        Profiler.RecordCount("TileLoop.VisibleCells", visibleCells);
        _animatedTileInView = animatedInView;
    }

    // Tiles whose tile-layer output moves with the wall clock. TileDefinitions.IsAnimated
    // covers the types that re-evaluate their visual per frame; water is added because
    // ResolveWater overrides its glyph from a clock phase even though its cached visual
    // is a pure position hash.
    private static bool IsTimeVaryingTile(TileType type)
        => TileDefinitions.IsAnimated(type)
           || type is TileType.Water or TileType.WaterDeep;

    // Re-resolves the clock-driven cells on top of a replayed tile layer. Same resolve path as
    // the tile loop, so the result is identical to having recomputed the whole viewport.
    private void RepaintTimeVaryingCells(Gfx.Batch batch)
    {
        if (_timeVaryingCells.Count == 0) return;
        using var _scope = Profiler.Begin("MapView.TileLayer.Patch");
        var timings = default(TileLoopTimings);
        for (int i = 0; i < _timeVaryingCells.Count; i++)
        {
            var (vx, vy) = _timeVaryingCells[i];
            int mx = VxToMap(vx), my = VyToMap(vy);
            ResolveVisibleCell(mx, my, false, ref timings, out _, out char ch, out Color fg, out Color bg);
            batch.Put(vx, vy, ch, Gfx.Attr(fg, bg));
        }
        Profiler.RecordCount("TileLayer.PatchCells", _timeVaryingCells.Count);
    }

    // Per-cell profiler accumulators, passed by ref so the tile loop keeps its timing breakdown
    // while sharing one resolve path with the patch pass.
    private struct TileLoopTimings
    {
        public long Visible, Walls, Lighting, Tint;
    }

    // The full resolve for one visible cell. Sole implementation — the tile loop and the
    // tile-layer patch pass both go through here so their output cannot drift apart.
    private void ResolveVisibleCell(int mx, int my, bool prof, ref TileLoopTimings t,
                                    out TileType type, out char ch, out Color fg, out Color bg)
    {
        long t0 = prof ? Stopwatch.GetTimestamp() : 0L;
        var tile = _map.GetTile(mx, my);
        type = tile.Type;
        (ch, fg, bg) = ResolveVisibleTile(_map, tile, mx, my);
        long t1 = prof ? Stopwatch.GetTimestamp() : 0L;
        if (prof) t.Visible += t1 - t0;

        ApplyConnectedWalls(tile, mx, my, ref ch);
        long t2 = prof ? Stopwatch.GetTimestamp() : 0L;
        if (prof) t.Walls += t2 - t1;

        ApplyLighting(mx, my, ref fg, ref bg);
        long t3 = prof ? Stopwatch.GetTimestamp() : 0L;
        if (prof) t.Lighting += t3 - t2;

        ApplyStatusTint(mx, my, ref fg);
        if (prof) t.Tint += Stopwatch.GetTimestamp() - t3;
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
            || _deathBursts.Count > 0
            || _skillFlashes.Count > 0
            || _weaponSwings.Count > 0
            || _scorchMarks.Count > 0
            || _corpseMarkers.Count > 0
            || HasActivePopups
            || IsShaking
            || ParticleQueue.HasAny
            || ToastQueue.Peek() != null
            || _statusTintTransitionRemainingMs > 0
            || _playerHpTween.Active || _playerXpTween.Active || _playerSatTween.Active
            || HasAnyActiveMonsterTween();
    }

    // The subset of live effects the tile loop itself reads, so the tile layer cannot be
    // replayed while one is running: hit flash and crit flash are applied in ApplyLighting,
    // the status tint in ApplyStatusTint, and shake shifts every coordinate mapping.
    private bool HasTileLayerAnimations()
    {
        return _hitFlashes.Count > 0
            || _critScreenFlashRemainingMs > 0
            || _statusTintTransitionRemainingMs > 0
            || IsShaking;
    }

    private bool HasAnyActiveMonsterTween()
    {
        foreach (var kv in _monsterHpTween)
            if (kv.Value.Active) return true;
        return false;
    }

    // Gate for the slower render timer. True when any time-bounded effect is alive OR a
    // clock-driven tile is on screen.
    public bool HasActiveRealtimeAnimations()
    {
        return HasActiveAnimations() || _animatedTileInView;
    }

    // Gate for the fast render timer: time-bounded effects only. Clock-driven tiles are excluded
    // because they step on 500ms+ intervals, so polling them at 50ms just repaints the same phase
    // ten times over — and each of those repaints now costs a tile-layer replay plus the overlay
    // passes rather than a no-op frame blit.
    public bool HasActiveEffects => HasActiveAnimations();

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
        // Corpse and scorch timers tick here rather than in their render passes, which
        // skip markers that are off-FOV or sitting under an occupant. Ticking there let
        // a marker the player walked away from live forever, and since HasActiveAnimations
        // counts both lists, one stranded entry kept the frame cache permanently dirty.
        for (int i = _corpseMarkers.Count - 1; i >= 0; i--)
        {
            var m = _corpseMarkers[i];
            if (m.RemainingMs <= dtMs) _corpseMarkers.RemoveAt(i);
            else _corpseMarkers[i] = (m.X, m.Y, m.RemainingMs - dtMs);
        }
        for (int i = _scorchMarks.Count - 1; i >= 0; i--)
        {
            var s = _scorchMarks[i];
            if (s.RemainingMs <= dtMs) _scorchMarks.RemoveAt(i);
            else _scorchMarks[i] = (s.X, s.Y, s.RemainingMs - dtMs);
        }
        if (_bossEntranceRemainingMs > 0)
            _bossEntranceRemainingMs = Math.Max(0, _bossEntranceRemainingMs - dtMs);
        TickStatusTintTransition(dtMs);
        TickHpTweens(dtMs);
    }

    // Advances player HP/XP/SAT + per-monster HP tweens. Raises
    // PlayerBarsTweenTick so GameScreen can refresh the action-bar labels.
    private void TickHpTweens(int dtMs)
    {
        bool playerActive = _playerHpTween.Active || _playerXpTween.Active || _playerSatTween.Active;
        if (playerActive)
        {
            _playerHpTween.Tick(dtMs, HpTweenDurationMs);
            _playerXpTween.Tick(dtMs, HpTweenDurationMs);
            _playerSatTween.Tick(dtMs, HpTweenDurationMs);
            PlayerBarsTweenTick?.Invoke();
            // Tweened values feed the HUD labels and the boss bar overlay only — the tile
            // loop reads real current HP, so its output is unaffected.
            DirtyOverlays();
        }
        if (_monsterHpTween.Count > 0)
        {
            // Snapshot keys to avoid mutation-during-iteration since tween is a struct.
            var keys = _monsterHpTween.Keys.ToList();
            bool anyActive = false;
            foreach (var id in keys)
            {
                var tw = _monsterHpTween[id];
                if (!tw.Active) continue;
                tw.Tick(dtMs, HpTweenDurationMs);
                _monsterHpTween[id] = tw;
                anyActive = true;
            }
            if (anyActive) DirtyOverlays();
        }
    }

    // Eases _statusTintCurrentColor from source→target over 200ms.
    // Null transitions fade through black so poison-removed tints retreat smoothly.
    private void TickStatusTintTransition(int dtMs)
    {
        if (_statusTintTransitionRemainingMs <= 0) return;
        _statusTintTransitionRemainingMs = Math.Max(0, _statusTintTransitionRemainingMs - dtMs);

        if (_statusTintTransitionRemainingMs <= 0)
        {
            _statusTintCurrentColor = _statusTintTargetColor;
        }
        else
        {
            float t = 1f - _statusTintTransitionRemainingMs / (float)StatusTintTransitionMs;
            float eased = SAOTRPG.Systems.EasingHelper.Ease(t,
                SAOTRPG.Systems.EasingHelper.EasingType.EaseOut);
            Color from = _statusTintSourceColor ?? Color.Black;
            Color to = _statusTintTargetColor ?? Color.Black;
            _statusTintCurrentColor = SAOTRPG.Systems.EasingHelper.LerpColor(from, to, eased);
        }
        DirtyFrame();
    }

    // Returns true when the player's tile changed since the last painted frame.
    private bool TrackFootstep()
    {
        if (_player.X == _lastPlayerPos.X && _player.Y == _lastPlayerPos.Y) return false;
        if (_lastPlayerPos.X >= 0)
        {
            _footsteps.Enqueue(_lastPlayerPos);
            if (_footsteps.Count > FootstepTrailLength) _footsteps.Dequeue();
        }
        _lastPlayerPos = (_player.X, _player.Y);
        return true;
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

    // Lit colours are snapped to this step per channel. Smooth per-cell falloff
    // otherwise gives every cell a unique colour, which costs twice: neighbouring
    // cells can't share one colour escape when written, and the torch's 4Hz flicker
    // shifts every cell just enough to force a full-viewport retransmit each frame.
    // Snapping collapses both. Larger = cheaper, but coarser gradients.
    private const int LightQuantizeStep = 8;

    private static byte Quantize(byte channel)
    {
        int q = (channel + LightQuantizeStep / 2) / LightQuantizeStep * LightQuantizeStep;
        return (byte)(q > 255 ? 255 : q);
    }

    // Light-color bleed into cell bg — keeps glyph readable while producing Brogue-style ambient glow.
    private const float BgGlowScale = 0.12f;

    // fg = base × light (warm→orange near campfire/lava, cool→cyan near fountain);
    // bg = additive dim glow (implicit ≤50 cap since light ≤255 × 0.12 ≈ 30.6).
    private void ApplyLighting(int mx, int my, ref Color fg, ref Color bg)
    {
        var light = _map.Lighting.GetLightUnchecked(mx, my);

        // Foreground: multiply base color by light → warm/cool tint.
        byte fgR = Quantize((byte)(fg.R * light.R * InvLightScale));
        byte fgG = Quantize((byte)(fg.G * light.G * InvLightScale));
        byte fgB = Quantize((byte)(fg.B * light.B * InvLightScale));
        fg = new Color(fgR, fgG, fgB);

        // Dim colored bg glow — floor picks up light source hue (orange near campfire, cyan near fountain).
        byte bgR = Quantize((byte)(light.R * BgGlowScale));
        byte bgG = Quantize((byte)(light.G * BgGlowScale));
        byte bgB = Quantize((byte)(light.B * BgGlowScale));
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
        if (_statusTintCurrentColor != null && (mx + my) % 3 == 0)
            fg = _statusTintCurrentColor.Value;
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
