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
    private static double s_drawTotalMs;
    private static int s_drawCount;

    internal static double DrainDrawAvgMs()
    {
        double avg = s_drawCount == 0 ? 0 : s_drawTotalMs / s_drawCount;
        s_drawTotalMs = 0; s_drawCount = 0;
        return avg;
    }

    // Tile loops run against frames that painted tiles at all. The full loop costs roughly
    // eleven times a tile-layer blit, so this ratio is the direct readout of whether the
    // layered cache is actually skipping the loop. Cells resolved against cells scanned is
    // the same readout one level down, for the incremental path.
    private static int s_tileLoops, s_tilePaints;
    private static long s_cellsResolved, s_cellsScanned;

    internal static (int loops, int paints, long resolved, long scanned) DrainTileLayerCounts()
    {
        var result = (s_tileLoops, s_tilePaints, s_cellsResolved, s_cellsScanned);
        s_tileLoops = 0; s_tilePaints = 0; s_cellsResolved = 0; s_cellsScanned = 0;
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
            // Only the average is reported. A per-view max was tracked and drained here and never
            // printed anywhere; AppHost's PERF line already carries "draw avg X worst Y" for the
            // whole draw phase, which is the figure worth cross-checking a stall against.
            s_drawTotalMs += (end - _drawStart) * 1000.0 / Stopwatch.Frequency;
            s_drawCount++;
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
        // Must precede the cache test: the tile loop reads the parsed values per cell.
        RefreshBiomeTint();
        var vp = Viewport;
        // Vignette extent follows the viewport, so a resize re-scales it. Cheap enough to redo
        // every frame, and a resize forces a full tile recompute anyway.
        RefreshVignetteExtent(vp.Width, vp.Height);
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

        // Must also precede the cache test, and for a sharper reason than the tint: spawning marks
        // the frame dirty, so if the top-up only ran on frames that were ALREADY dirty the layer
        // would deadlock — the last mote expires, the frame goes clean, nothing draws, nothing
        // tops up, and the atmosphere never comes back for the rest of the floor.
        TickAmbientParticles(vp.Width, vp.Height);

        // Fully idle frames blit the finished frame and paint nothing at all.
        if (!IsFrameDirty(vp.Width, vp.Height) && TryBlitFrame(vp.Width, vp.Height))
        {
                return true;
        }

        // Overlays paint through a batch; the tile layer is composed in _tileBuf and replayed.
        _frameBatch = Gfx.Begin(this);

        RenderTileLayer(vp.Width, vp.Height);

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

    // Brings the tile layer up to date and replays it. Three paths, cheapest last-resort first:
    // rebuild everything, patch only what changed, or leave the buffer alone. Whichever ran,
    // _tileBuf ends up holding exactly what a full recompute would have produced — which is
    // what --verify-tiles checks, every frame.
    private void RenderTileLayer(int w, int h)
    {
        bool resized = EnsureTileBuffers(w, h);
        bool full = resized || NeedsFullTileRecompute();
        _verifyScanRan = false;
        if (full) RunFullTileLoop(w, h);
        else if (NeedsTileScan()) { _verifyScanRan = true; PatchTileLayer(w, h); }

        if (s_sampling)
        {
            s_tilePaints++;
            if (full) s_tileLoops++;
        }

        // A frame that already recomputed everything has nothing to check against.
        if (SAOTRPG.UI.DebugMode.VerifyTiles && !full) VerifyTileLayer(w, h);

        CommitTileState();

        using (Profiler.Begin("MapView.TileLayer.Blit"))
            Gfx.BlitGlyphs(this, _tileBuf!, w, h);
    }

    // Resolves every cell of the viewport from map state. Exhaustive by design: replaying the
    // layer is only a valid erase of the previous frame's overlays because no cell is skipped.
    private void RunFullTileLoop(int w, int h)
    {
        var buf = _tileBuf!;
        var keys = _tileKey!;
        // Stopwatch reads are not free at viewport scale (nine per cell); only pay for
        // them when the profiler is actually collecting.
        bool prof = Profiler.Enabled;
        var timings = default(TileLoopTimings);
        int offX = EffOffsetX, offY = EffOffsetY;
        int turn = Map.DayNightCycle.CurrentTurn;
        int memCells = 0, visibleCells = 0, timeVarying = 0;
        long emitTotal = 0;
        bool animatedInView = false;

        using (Profiler.Begin("MapView.TileLoop"))
        {
            for (int vy = 0; vy < h; vy++)
            {
                int my = vy + offY, row = vy * w;
                for (int vx = 0; vx < w; vx++)
                {
                    int mx = vx + offX;
                    var key = BuildTileKey(mx, my, turn, out bool tv, out bool anim, out _);
                    if (tv) timeVarying++;
                    if (anim) animatedInView = true;
                    if ((key.Flags & TileKey.FVisible) != 0) visibleCells++;
                    else if ((key.Flags & TileKey.FExplored) != 0) memCells++;

                    ResolveKeyedCell(mx, my, ref key, prof, ref timings,
                                     out char ch, out Color fg, out Color bg);

                    long tEmit = prof ? Stopwatch.GetTimestamp() : 0L;
                    buf[row + vx] = new Gfx.Glyph(ch, Gfx.Attr(fg, bg));
                    if (prof) emitTotal += Stopwatch.GetTimestamp() - tEmit;
                    keys[row + vx] = key;
                }
            }
        }

        RecordTileLoopTimings(in timings, emitTotal);
        Profiler.RecordCount("TileLoop.MemCells", memCells);
        Profiler.RecordCount("TileLoop.VisibleCells", visibleCells);
        _timeVaryingCount = timeVarying;
        _animatedTileInView = animatedInView;
        if (s_sampling) { s_cellsResolved += w * h; s_cellsScanned += w * h; }
    }

    // Scrolls the layer by the camera delta and re-resolves only the cells that changed.
    // One pass: the per-cell key is rebuilt for every cell anyway (that is how change is
    // detected), and a cell is resolved when its key moved, when it was pre-marked, or when
    // it holds something the key deliberately does not sample.
    private void PatchTileLayer(int w, int h)
    {
        using var _scope = Profiler.Begin("MapView.TileLayer.Patch");
        var buf = _tileBuf!;
        var keys = _tileKey!;
        var dmg = _tileDamage!;
        int n = w * h;
        int offX = EffOffsetX, offY = EffOffsetY;
        int dx = offX - _tileOffX, dy = offY - _tileOffY;
        _verifyDx = dx; _verifyDy = dy;

        using (Profiler.Begin("TileLayer.Shift"))
        {
            Array.Clear(dmg, 0, n);
            if (dx != 0 || dy != 0)
            {
                ShiftViewport(buf, w, h, dx, dy);
                ShiftViewport(keys, w, h, dx, dy);
                MarkExposedBand(dmg, w, h, dx, dy);
            }
        }
        using (Profiler.Begin("TileLayer.Mark"))
            MarkPointDamage(dmg, w, h, offX, offY);

        using var _scanScope = Profiler.Begin("TileLayer.Scan");
        bool prof = Profiler.Enabled;
        var timings = default(TileLoopTimings);
        int turn = Map.DayNightCycle.CurrentTurn;
        int resolved = 0, timeVarying = 0;
        long emitTotal = 0;
        bool animatedInView = false;

        for (int vy = 0; vy < h; vy++)
        {
            int my = vy + offY, row = vy * w;
            for (int vx = 0; vx < w; vx++)
            {
                int i = row + vx, mx = vx + offX;
                var key = BuildTileKey(mx, my, turn, out bool tv, out bool anim, out bool occupied);
                if (tv) timeVarying++;
                if (anim) animatedInView = true;

                // State carries what the previous resolve *found* here — an occupant, loose
                // items, a hit flash. None is sampled by the key, so a cell that had one is
                // re-resolved unconditionally; that is what catches its disappearance.
                if (!dmg[i] && !tv && !occupied && keys[i].State == 0 && key.InputsMatch(in keys[i]))
                    continue;

                ResolveKeyedCell(mx, my, ref key, prof, ref timings,
                                 out char ch, out Color fg, out Color bg);

                long tEmit = prof ? Stopwatch.GetTimestamp() : 0L;
                buf[i] = new Gfx.Glyph(ch, Gfx.Attr(fg, bg));
                if (prof) emitTotal += Stopwatch.GetTimestamp() - tEmit;
                keys[i] = key;
                resolved++;
            }
        }

        RecordTileLoopTimings(in timings, emitTotal);
        Profiler.RecordCount("TileLayer.PatchCells", resolved);
        _timeVaryingCount = timeVarying;
        _animatedTileInView = animatedInView;
        if (s_sampling) { s_cellsResolved += resolved; s_cellsScanned += n; }
    }

    // Marks the cells whose content the per-cell key cannot see: pending point damage, tiles
    // currently flashing, and tiles currently holding loose items. All three are small sets;
    // their *disappearance* is caught by TileKey.State instead.
    private void MarkPointDamage(bool[] dmg, int w, int h, int offX, int offY)
    {
        for (int i = 0; i < _tileDamagePoints.Count; i++)
        {
            var (mx, my) = _tileDamagePoints[i];
            MarkViewportCell(dmg, w, h, mx - offX, my - offY);
        }
        foreach (var (mx, my) in _hitFlashSet)
            MarkViewportCell(dmg, w, h, mx - offX, my - offY);
        foreach (var (mx, my) in _map.ItemTiles)
            MarkViewportCell(dmg, w, h, mx - offX, my - offY);
    }

    private static void MarkViewportCell(bool[] dmg, int w, int h, int vx, int vy)
    {
        if ((uint)vx < (uint)w && (uint)vy < (uint)h) dmg[vy * w + vx] = true;
    }

    // Samples everything the cell resolve reads that is cheap enough to read per cell.
    // `timeVarying` and `occupied` are reported separately because they are not comparable
    // state — they mean "this cell has to be resolved again regardless".
    private TileKey BuildTileKey(int mx, int my, int turn,
                                 out bool timeVarying, out bool animated, out bool occupied)
    {
        timeVarying = false; animated = false; occupied = false;
        var key = default(TileKey);
        if (!_map.InBounds(mx, my)) return key;

        key.Flags = TileKey.FInBounds;
        // Unexplored cells render blank whatever the tile says, so nothing else is read for
        // them — which on a partly-explored floor is most of the viewport.
        if (!_map.IsExploredUnchecked(mx, my)) return key;
        key.Flags |= TileKey.FExplored;

        ref var tile = ref _map.RefTile(mx, my);
        key.Type = (byte)tile.Type;
        if (tile.TrapHidden) key.Flags |= TileKey.FTrapHidden;

        // TileVisualCache bypasses its cache for animated types, so their glyph is re-derived
        // from the wall clock whether the cell is lit or only remembered.
        animated = TileDefinitions.IsAnimated(tile.Type);
        if (_map.IsVisibleUnchecked(mx, my))
        {
            key.Flags |= TileKey.FVisible;
            key.Light = _map.Lighting.GetLightUnchecked(mx, my);
            // The vignette moves with the player, so it is a per-cell input the scan must sample
            // or the incremental patch carries stale shading across a step.
            key.Vignette = VignetteBand(mx, my);
            occupied = tile.Occupant != null && !tile.Occupant.IsDefeated;
            // Water's cached visual is a pure position hash, but ResolveWater overrides the
            // glyph from a 2.5Hz clock phase — time-varying without being animated.
            timeVarying = animated || tile.Type is TileType.Water or TileType.WaterDeep;
        }
        else
        {
            key.Fade = MemoryFadeStep(turn - _map.GetLastSeenTurnUnchecked(mx, my));
            timeVarying = animated;
        }
        return key;
    }

    // The resolve for one cell, dispatched on the key's own flags. Sole implementation — the
    // full loop, the patch pass and the verifier all go through here so they cannot drift.
    private void ResolveKeyedCell(int mx, int my, ref TileKey key, bool prof, ref TileLoopTimings t,
                                  out char ch, out Color fg, out Color bg)
    {
        key.State = 0;
        if ((key.Flags & TileKey.FExplored) == 0)
        {
            ch = ' '; fg = Color.Black; bg = Color.Black;
            return;
        }
        if ((key.Flags & TileKey.FVisible) == 0)
        {
            long t0 = prof ? Stopwatch.GetTimestamp() : 0L;
            (ch, fg, bg) = ResolveMemoryTile(mx, my, key.Fade);
            if (prof) t.Memory += Stopwatch.GetTimestamp() - t0;
            return;
        }
        ResolveVisibleCell(mx, my, prof, ref t, out _, out ch, out fg, out bg, out key.State);
    }

    private static void RecordTileLoopTimings(in TileLoopTimings t, long emitTotal)
    {
        Profiler.RecordRaw("TileLoop.Memory", t.Memory);
        Profiler.RecordRaw("TileLoop.Visible", t.Visible);
        Profiler.RecordRaw("TileLoop.ConnectedWalls", t.Walls);
        Profiler.RecordRaw("TileLoop.Lighting", t.Lighting);
        Profiler.RecordRaw("TileLoop.StatusTint", t.Tint);
        Profiler.RecordRaw("TileLoop.DriverEmit", emitTotal);
    }

    // Per-cell profiler accumulators, passed by ref so the loop keeps its timing breakdown
    // while sharing one resolve path with the patch pass.
    private struct TileLoopTimings
    {
        public long Memory, Visible, Walls, Lighting, Tint;
    }

    // The full resolve for one visible cell. `state` reports what was found here, so the next
    // pass knows to re-resolve the cell even if nothing it samples has moved.
    private void ResolveVisibleCell(int mx, int my, bool prof, ref TileLoopTimings t,
                                    out TileType type, out char ch, out Color fg, out Color bg,
                                    out byte state)
    {
        state = 0;
        long t0 = prof ? Stopwatch.GetTimestamp() : 0L;
        var tile = _map.GetTile(mx, my);
        type = tile.Type;
        if (tile.Occupant != null && !tile.Occupant.IsDefeated) state |= TileKey.SOccupant;
        else if (_map.HasItemsAt(mx, my)) state |= TileKey.SItems;
        (ch, fg, bg) = ResolveVisibleTile(_map, tile, mx, my);
        long t1 = prof ? Stopwatch.GetTimestamp() : 0L;
        if (prof) t.Visible += t1 - t0;

        ApplyConnectedWalls(tile, mx, my, ref ch);
        long t2 = prof ? Stopwatch.GetTimestamp() : 0L;
        if (prof) t.Walls += t2 - t1;

        ApplyLighting(mx, my, ref fg, ref bg);
        if (IsHitFlashed(mx, my)) state |= TileKey.SHitFlash;
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
            int tweenMs = Motion.TweenMs(HpTweenDurationMs);
            _playerHpTween.Tick(dtMs, tweenMs);
            _playerXpTween.Tick(dtMs, tweenMs);
            _playerSatTween.Tick(dtMs, tweenMs);
            PlayerBarsTweenTick?.Invoke();
            // Tweened values feed the HUD labels and the boss bar overlay only — the tile
            // loop reads real current HP, so its output is unaffected.
            DirtyOverlays();
        }
        if (_monsterHpTween.Count > 0)
        {
            // Snapshot keys to avoid mutation-during-iteration since tween is a struct. The
            // snapshot is still required — the loop writes back into the dictionary — but it goes
            // into a reused list, because this ticks every frame for the length of a fight.
            // AddRange takes KeyCollection's ICollection.CopyTo path, so it does not allocate.
            _monsterTweenKeys.Clear();
            _monsterTweenKeys.AddRange(_monsterHpTween.Keys);
            bool anyActive = false;
            foreach (var id in _monsterTweenKeys)
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
    //
    // PATH-D-PORT: a whole-viewport colour wash driven by a wall-clock ease, not by tile state.
    // A renderer swap must keep it a WHOLE-VIEWPORT input to the tile layer's change detection
    // (it is compared against _tileStatusTint in FrameCache) — treating it per-cell leaves the map
    // on the previous tint until something else happens to dirty each cell.
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

    // Remembered tiles fade with age, and that fade is quantized.
    //
    // Unquantized it moves a remembered cell's colour by ~0.24 of a channel per turn, so a
    // given cell's rendered byte changes roughly every fourth turn — and since cells sit at
    // different ages, about a quarter of every remembered cell on screen changes on any given
    // turn, scattered. No damage tracking can enumerate that. Snapped to steps a cell changes
    // only when it crosses one, and cells of the same age cross together.
    private const int MemoryFadeSteps = 8;
    private const int MemoryFadeDelayTurns = 40;
    private const int MemoryFadeSpanTurns = 200;
    private const float MemoryFadeDepth = 0.6f;

    private static byte MemoryFadeStep(int age)
    {
        if (age <= MemoryFadeDelayTurns) return 0;
        int step = (age - MemoryFadeDelayTurns) * MemoryFadeSteps / MemoryFadeSpanTurns;
        return (byte)(step >= MemoryFadeSteps ? MemoryFadeSteps : step);
    }

    private static float MemoryFadeScale(byte step)
        => 1f - MemoryFadeDepth * (step / (float)MemoryFadeSteps);

    private (char ch, Color fg, Color bg) ResolveMemoryTile(int mx, int my, byte fadeStep)
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
        if (fadeStep > 0)
        {
            float scale = MemoryFadeScale(fadeStep);
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
    // At 0.12 this was sub-perceptible; 0.20 is the top of the range the lighting audit suggested.
    private const float BgGlowScale = 0.20f;

    // ── FOV edge vignette ──────────────────────────────────────────────────────────────────────
    // Far-but-visible cells dim toward the edge of view, so the lit region reads as an
    // illumination sphere centred on the player rather than a flat field cut off by walls.
    //
    // THE REFERENCE IS THE VIEWPORT, NOT DayNightCycle.FovRadius. FovRadius is `halfHeight ×
    // FovMultiplier(4)` — about 188 at a 316×91 terminal — while the farthest on-screen cell is
    // only ~91 aspect-corrected units away. Scaling from 75% of the FOV radius, as this was
    // originally specified, starts dimming at 141 and would therefore never affect a single
    // visible cell. FOV radius is deliberately generous so everything on screen is lit; it is not
    // a measure of what the player can see.
    private const int VignetteBands = 8;            // quantization: fewer bands, less cache churn
    private const float VignetteInnerFrac = 0.65f;  // no dimming inside this share of the extent
    private const int VignetteMinScale = 140;       // out of 255 at the outermost band (~55%)

    private float _vignetteInnerSq, _vignetteSpanSq;
    // 1/aspect², so the per-cell distance is one multiply. Aspect-corrected because cells are 2:1
    // and an uncorrected radius would make the vignette an ellipse.
    private static readonly float InvAspectSq =
        1f / (Map.Generation.FloorMask.CellAspectRatio * Map.Generation.FloorMask.CellAspectRatio);

    // NOTE FOR ANYONE MEASURING FRAME-CACHE PATHS: the fully-idle path (blit the finished frame,
    // paint nothing) is UNREACHABLE while ambient particles are alive, which since FB-693 means
    // "always", on any biome that declares one with density on. HasActiveAnimations() includes
    // ParticleQueue.HasAny, which calls DirtyOverlays(), which sets _frameDirty, which IsFrameDirty
    // tests. The steady state is therefore the OVERLAY path, not the idle one. That costs a
    // tile-layer blit plus the overlay passes each frame — but not necessarily a terminal flush,
    // because Gfx change detection skips cells whose contents did not move, and a mote drifting at
    // ~1 cell/second changes very few of them. Turning ParticleDensity off restores the idle path.
    private void RefreshVignetteExtent(int w, int h)
    {
        float halfW = w * 0.5f, halfH = h * 0.5f;
        float cornerSq = halfW * halfW * InvAspectSq + halfH * halfH;
        _vignetteInnerSq = cornerSq * (VignetteInnerFrac * VignetteInnerFrac);
        _vignetteSpanSq = MathF.Max(1f, cornerSq - _vignetteInnerSq);
    }

    // Quantized distance-from-player band, 0 (undimmed) to VignetteBands-1 (darkest).
    private byte VignetteBand(int mx, int my)
    {
        float dx = mx - _player.X, dy = my - _player.Y;
        float d2 = dx * dx * InvAspectSq + dy * dy;
        if (d2 <= _vignetteInnerSq) return 0;
        int band = 1 + (int)((d2 - _vignetteInnerSq) * (VignetteBands - 1) / _vignetteSpanSq);
        return (byte)Math.Min(band, VignetteBands - 1);
    }

    // Scale for a band, 255 = untouched. DERIVED FROM THE BAND, never from the raw distance —
    // that is what keeps it consistent with the quantized value stored in TileKey. A continuous
    // scale against a quantized key would let a cell's shading drift without the damage tracker
    // ever noticing, which is exactly the stale-layer failure --verify-tiles exists to catch.
    private static int VignetteScale(byte band) =>
        band == 0 ? 255 : 255 - band * (255 - VignetteMinScale) / (VignetteBands - 1);

    // ── Biome atmosphere particles ─────────────────────────────────────────────────────────────
    // Kept topped up around the CAMERA. This used to be seeded once per floor by a generation
    // pass, across the whole map: 35 particles over ~500,000 walkable cells put roughly ONE on
    // screen, and they all expired after 30 s with nothing to replace them. Same budget, spawned
    // where the player is looking, refreshed as they expire.
    //
    // The densest biome (8 permille) keeps the whole budget; the rest scale down against it, so a
    // biome's declared densityPermille finally decides something. It could not before — the old
    // pass clamped every biome's target to the same 35.
    private const int AmbientPermilleAtFullBudget = 8;
    private const int AmbientSpawnTries = 24;   // rejection sampling for a visible walkable cell
    private Map.GameMap? _ambientMap;

    // Target on-screen atmosphere for the player's density setting. Deliberately modest: these are
    // slow-drifting single glyphs and the standing preference is against busy simultaneous motion.
    // 0 means the player turned atmosphere off, and the caller relies on that: it returns on a zero
    // budget before applying the at-least-one floor.
    private static int AmbientBudget() => UserSettings.Current.ParticleDensity switch
    {
        Systems.ParticleDensity.Subtle     => 2,
        Systems.ParticleDensity.Moderate   => 4,
        Systems.ParticleDensity.Pronounced => 7,
        _                                  => 0,
    };

    // Tops the atmosphere back up to target inside the current viewport. Runs every frame; the
    // steady state is one integer compare, because the budget is only short when a particle has
    // just expired or the camera has moved somewhere emptier.
    private void TickAmbientParticles(int w, int h)
    {
        // Motion.Animate covers both reduce-motion and --freeze-anim.
        if (!Systems.Motion.Animate) return;

        string? id = Systems.BiomeSystem.CurrentGenConfig?.AmbientOverlay?.ParticleId;
        if (string.IsNullOrEmpty(id)) return;

        // A floor change invalidates the atmosphere: the previous biome's motes should not drift
        // on into the new one. Generation no longer clears them, so this is where it happens.
        if (!ReferenceEquals(_ambientMap, _map))
        {
            _ambientMap = _map;
            ParticleQueue.ClearAmbient();
        }

        int permille = Systems.BiomeSystem.CurrentGenConfig?.AmbientOverlay?.DensityPermille ?? 0;
        if (permille <= 0) return;

        // Off must return BEFORE the floor below, or turning atmosphere off would still show one.
        int budget = AmbientBudget();
        if (budget <= 0) return;

        // A biome that declares a particle shows at least one whenever atmosphere is on at all.
        // The permille scaling truncates, so without this floor the sparser biomes (Grassland,
        // Urban, Dark) render their declared particle NOWHERE on the lower density settings —
        // config that is set, read, and still produces nothing.
        int target = Math.Max(1, budget * permille / AmbientPermilleAtFullBudget);
        target = Math.Min(target, ParticleQueue.MaxAmbient);
        int deficit = target - ParticleQueue.AmbientCount;
        if (deficit <= 0) return;

        // Random.Shared is the COSMETIC stream, and using it here is load-bearing: ambient spawn
        // positions must never come from RunRng or a worldgen stream, or a purely visual setting
        // would shift the draw position of gameplay rolls.
        var rng = Random.Shared;
        for (int spawned = 0; spawned < deficit; spawned++)
        {
            if (!TrySpawnAmbientInView(id!, rng, w, h)) break;
        }
    }

    // Rejection-samples a visible, walkable cell in the viewport. Visible rather than merely
    // explored, so motes appear in the lit world the player can actually see rather than drifting
    // through remembered darkness. Gives up after a bounded number of tries — a corridor with
    // almost nothing visible simply carries less atmosphere, which is the right answer anyway.
    private bool TrySpawnAmbientInView(string id, Random rng, int w, int h)
    {
        for (int attempt = 0; attempt < AmbientSpawnTries; attempt++)
        {
            int mx = VxToMap(rng.Next(w));
            int my = VyToMap(rng.Next(h));
            if (!_map.InBounds(mx, my)) continue;
            if (!_map.IsVisible(mx, my)) continue;
            if (!_map.GetTile(mx, my).IsWalkable) continue;
            return ParticleQueue.SpawnAmbient(id, mx, my);
        }
        return false;
    }

    // ── Biome atmosphere wash ──────────────────────────────────────────────────────────────────
    // Parsed ONCE PER FRAME, never per cell: BiomeGenConfig.GetTintColor does string parsing inside
    // a try/catch, which cannot run tens of thousands of times a frame. `_tintA` of 0 means no
    // wash, which is also what a biome declaring no tintColor gets.
    private Map.Generation.BiomeGenConfig? _tintCfg;
    private byte _tintR, _tintG, _tintB;
    private int _tintA;

    // Re-parses only when BiomeSystem hands back a different config object, so the steady state
    // costs one reference compare per frame.
    private void RefreshBiomeTint()
    {
        var cfg = Systems.BiomeSystem.CurrentGenConfig;
        if (ReferenceEquals(cfg, _tintCfg)) return;
        _tintCfg = cfg;
        _tintA = 0;
        if (cfg == null) return;
        var tint = cfg.GetTintColor(out byte alpha);
        if (tint == null || alpha == 0) return;
        _tintR = tint.Value.R;
        _tintG = tint.Value.G;
        _tintB = tint.Value.B;
        _tintA = alpha;
    }

    // c*(255-a) + tint*a, all integer — the hot path takes no float divide.
    private Color BlendBiomeTint(Color c)
    {
        int ia = 255 - _tintA;
        return new Color(
            (byte)((c.R * ia + _tintR * _tintA) / 255),
            (byte)((c.G * ia + _tintG * _tintA) / 255),
            (byte)((c.B * ia + _tintB * _tintA) / 255));
    }

    // fg = base × light (warm→orange near campfire/lava, cool→cyan near fountain);
    // bg = additive dim glow (implicit ≤50 cap since light ≤255 × 0.12 ≈ 30.6).
    //
    // PATH-D-PORT: the whole visual contract for a lit cell lives in this one method — every
    // visible cell passes through it. A renderer swap reimplements, in this order: the biome wash,
    // the theme's world transform, the fg light multiply, the bg glow at BgGlowScale, the hit
    // flash, the crit flash and the status tint. The 8-step colour quantization is not cosmetic —
    // it is what keeps the damage-tracked tile layer's change detection tractable, so a port that
    // drops it makes every lit cell change every turn.
    private void ApplyLighting(int mx, int my, ref Color fg, ref Color bg)
    {
        var light = _map.Lighting.GetLightUnchecked(mx, my);

        // Two transforms, and the ORDER between them is load-bearing. The biome wash is part of the
        // world's own identity, so it goes on the authored colour first; the theme then transforms
        // the result. Reversed, Amber Mono would collapse the world to amber and then have biome
        // hues painted back over it, breaking the one promise a monochrome theme makes.
        //
        // FOREGROUND ONLY, deliberately. The background is black except where light bleeds into it,
        // and washing that too would paint a coloured ground across exactly the visible region —
        // memory tiles keep a black ground, so the FOV boundary would read as a hard-edged
        // rectangle of colour. Raising BgGlowScale is the intended lever for the background.
        if (_tintA > 0) fg = BlendBiomeTint(fg);

        // The theme's world transform, applied BEFORE lighting so a lit tile keeps the same
        // relationship to an unlit one. Every visible cell passes through here, which is why this
        // is the only place the world is recoloured. Gated because Default and Colourblind Safe
        // are the identity transform and this is per-cell in the hot path.
        ColorTheme theme = ColorSchemes.Theme;
        bool themeTints = theme.TintsWorld;
        if (themeTints) fg = theme.World(fg);

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
        // The floor glow is light, not a tile colour, so it is transformed after it is built.
        if (themeTints) bg = theme.World(bg);

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

        // Edge vignette LAST, so it dims whatever the cell finally resolved to — the crit flash
        // included, which should recede with distance like everything else rather than punching
        // through at full strength on the rim. Foreground only: the background is the light glow
        // and is already near-black out there.
        int vig = VignetteScale(VignetteBand(mx, my));
        if (vig < 255)
            fg = new Color((byte)(fg.R * vig / 255), (byte)(fg.G * vig / 255), (byte)(fg.B * vig / 255));
    }

    // Share of the status colour blended into a lit cell. The old effect REPLACED the foreground
    // outright on the (mx+my)%3 diagonal, which read as green stripes rather than a poisoned world;
    // covering every cell at full strength would instead flatten the map to one solid colour, so
    // full coverage only works as a blend. One constant to tune the whole effect.
    private const float StatusTintAlpha = 0.5f;

    // Environmental wash for poison / bleed. Runs only on VISIBLE cells (memory tiles keep their
    // own palette) and only while a status is active, so it costs nothing the rest of the time.
    //
    // The tint is scaled by the cell's OWN luminance before blending, which is what stops it
    // brightening the dark. Blending toward a fixed colour would make an unlit-but-visible cell
    // glow green, lighting the map up as a side effect of being poisoned; scaling first means a
    // black cell stays black, a dim cell shifts hue at its own brightness, and only lit cells carry
    // the wash strongly. Same reason the biome wash in ApplyLighting is foreground-only.
    private void ApplyStatusTint(int mx, int my, ref Color fg)
    {
        if (_statusTintCurrentColor == null) return;
        var tint = _statusTintCurrentColor.Value;

        // Rec.601 luma, integer: the cell's brightness caps how much tint it can take.
        int luma = (fg.R * 77 + fg.G * 150 + fg.B * 29) >> 8;
        int tr = tint.R * luma / 255, tg = tint.G * luma / 255, tb = tint.B * luma / 255;

        const int A = (int)(StatusTintAlpha * 256);
        fg = new Color(
            (byte)((fg.R * (256 - A) + tr * A) >> 8),
            (byte)((fg.G * (256 - A) + tg * A) >> 8),
            (byte)((fg.B * (256 - A) + tb * A) >> 8));
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
