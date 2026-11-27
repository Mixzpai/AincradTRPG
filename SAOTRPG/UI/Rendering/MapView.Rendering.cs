using System.Diagnostics;
using SAOTRPG.Map;
using SAOTRPG.UI.Helpers;
using SAOTRPG.Map.Lighting;
using SAOTRPG.Systems.Diagnostics;
using SAOTRPG.Systems.Persistence;
using SAOTRPG.UI.Effects;

namespace SAOTRPG.UI.Rendering;

// Core tile resolution and post-effect pipeline.
// Converts map state into (char, fg, bg) per cell each frame.
public partial class MapView
{
    // ── Draw-cost sampling (diagnostic — see DebugMode.PerfSampling) ──
    // Accumulated by every draw, drained once per second by the main-loop sampler
    // so a slow frame can be told apart from a starved loop.
    private static readonly bool s_sampling = SAOTRPG.Systems.Diagnostics.DebugMode.PerfSampling;
    private static double s_drawTotalMs;
    private static int s_drawCount;

    internal static double DrainDrawAvgMs()
    {
        double avg = s_drawCount == 0 ? 0 : s_drawTotalMs / s_drawCount;
        s_drawTotalMs = 0; s_drawCount = 0;
        return avg;
    }

    // Read by the verification probes, like DrainFramesDrawn: the wall clock this view spent
    // inside OnDrawingContent since the last read, with the frame count it covers, so the whole
    // map draw can be timed without a profiler scope inside the loop being timed.
    // Accumulates only under --perf. AppHost drains the same pair through DrainDrawAvgMs, but its
    // sampler is attached at AppHost.Start and a probe enables sampling after that, so the two
    // never both run.
    public static (double TotalMs, int Frames) DrainDrawTotals()
    {
        var result = (s_drawTotalMs, s_drawCount);
        s_drawTotalMs = 0; s_drawCount = 0;
        return result;
    }

    // Tile loops run against frames that painted tiles at all: the direct readout of whether
    // the layered cache is skipping the full loop. Cells resolved against cells scanned is
    // the same readout one level down, for the incremental path.
    private static int s_tileLoops, s_tilePaints;
    private static int s_framesDrawn;

    // Painted frames since the last read, public for the verification probes; reading resets, so
    // two reads cannot overlap by accident.
    public static int DrainFramesDrawn()
    {
        int n = s_framesDrawn;
        s_framesDrawn = 0;
        return n;
    }
    private static long s_cellsResolved, s_cellsScanned;

    // Public for the verification probes, like DrainFramesDrawn above. The frame cache means the
    // tile loop runs on only a few frames, so a per-cell figure needs the resolved-cell count as
    // its denominator, not the painted-frame count.
    public static (int loops, int paints, long resolved, long scanned) DrainTileLayerCounts()
    {
        var result = (s_tileLoops, s_tilePaints, s_cellsResolved, s_cellsScanned);
        s_tileLoops = 0; s_tilePaints = 0; s_cellsResolved = 0; s_cellsScanned = 0;
        return result;
    }

    // The tile loop paints every cell of the viewport every pass, so the framework's
    // pre-draw clear is pure duplicate output. Worse, it blanks the buffer, so the
    // previous frame can never be diffed against and every cell counts as changed.
    // Cancelling it lets unchanged frames cost nothing.
    protected override bool OnClearingViewport() => true;

    // Raw timestamps of this view's last paint. The loop sampler brackets them against
    // the iteration start and the draw-complete event to split a frame into: work before
    // any view painted, this view's own paint, and everything after it (remaining views
    // plus the driver's flush to the terminal).
    public static long LastDrawStartTicks, LastDrawEndTicks;

    protected override bool OnDrawingContent(DrawContext? context)
    {
        // A modal above this view has already painted this pass; drawing now would erase it.
        if (AppHost.IsBeneathTopSession(this)) return true;

        // A frame counter that runs outside --verify-tiles as well: VerifyFramesChecked counts
        // only in verify mode, and this one gives a per-frame denominator that excludes the verifier.
        s_framesDrawn++;

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
            // Only the average is accumulated; AppHost's PERF line carries "draw avg X worst Y"
            // for the whole draw phase, which is the figure to cross-check a stall against.
            s_drawTotalMs += (end - _drawStart) * 1000.0 / Stopwatch.Frequency;
            s_drawCount++;
        }
    }

    private bool DrawContent(DrawContext? context)
    {
        using var _drawScope = Profiler.Begin("MapView.OnDrawingContent");
        // Cleared up front so the full-frame blit path — which returns before overlays and
        // never builds a batch — can never leave last frame's stale clip in place.
        _frameBatchLive = false;
        // Single FrameClock tick per frame; dt threaded through render passes.
        int dtMs = FrameClock.Tick();
        // Must precede the cache test: the tile loop reads the parsed values per cell.
        RefreshBiomeTint();
        // Must precede the cache test as well: the gain is a whole-viewport input to every cell
        // the tile loop resolves, and MapView.FrameCache compares the value the layer was built
        // with against this one.
        RefreshExposure();
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
        //
        // Deliberately ~2x the farthest on-screen cell, and it must stay that way: this radius
        // also drives what is marked explored, and QuestSystem generates Explore quests targeting
        // 40-59% of a floor, which a viewport-sized radius would leave mostly unreachable by
        // exploring. A tighter radius needs the exploration model changed first.
        int halfH = vp.Height / 2 + 2;
        SAOTRPG.Map.Lighting.DayNightCycle.FovRadius = halfH * SAOTRPG.Map.Lighting.DayNightCycle.FovMultiplier;

        // Position compare is the backstop for movement that leaves no invalidation behind:
        // auto-explore steps on a timer rather than a keypress, and a step that neither
        // damages anything nor moves a monster otherwise reaches the cache test clean and
        // gets served a stale frame. The camera only ever follows the player, so this covers
        // scrolling too. Must run before the cache test.
        if (TrackFootstep()) DirtyFrame();

        // Must also precede the cache test, and for a sharper reason than the tint: spawning marks
        // the frame dirty, so if the top-up only ran on frames that were already dirty the layer
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
        _frameBatchLive = true;

        RenderTileLayer(vp.Width, vp.Height);

        RenderOverlays(vp.Width, vp.Height, dtMs);
        TickFrameEffects(dtMs);

        // The snapshot is only worth taking if the next frame could replay it. Any live effect
        // dirties the frame two lines down, so the capture would be a viewport of cell copies for
        // a path that cannot run — and with biome atmosphere on, ParticleQueue.HasAny keeps
        // HasActiveAnimations true permanently, so that is every frame on most floors.
        bool active = HasActiveAnimations();
        if (active || _animTailDirty)
        {
            // The pulse CaptureFrame would have consumed still has to be consumed, or
            // NeedsTileScan sees it forever. DirtyOverlays below re-raises _frameDirty, which is
            // what keeps "not dirty implies the last painted frame captured" true.
            s_externalDirty = false;
            DirtyOverlays();
            _animTailDirty = active;
        }
        else
        {
            CaptureFrame(vp.Width, vp.Height);
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
        if (SAOTRPG.Systems.Diagnostics.DebugMode.VerifyTiles && !full) VerifyTileLayer(w, h);

        CommitTileState();
        if (_tileDeferred) ArmDeferredRedraw();

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
        int turn = SAOTRPG.Map.Lighting.DayNightCycle.CurrentTurn;
        RefreshViewEdgePlanes(offX, offY, w, h);
        int memCells = 0, visibleCells = 0, timeVarying = 0;
        long emitTotal = 0;
        bool animatedInView = false;
        // Every cell is resolved here, so nothing stays held back.
        Array.Clear(_tileDeferMask!, 0, w * h);
        _tileDeferred = false;

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
        RefreshViewEdgePlanes(offX, offY, w, h);

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
        int turn = SAOTRPG.Map.Lighting.DayNightCycle.CurrentTurn;
        int resolved = 0, timeVarying = 0, deferred = 0;
        long emitTotal = 0;
        bool animatedInView = false;
        var defer = _tileDeferMask!;
        Array.Clear(defer, 0, n);
        // Spread only while the camera is still. A step scrolls the whole map, so every cell goes to
        // the terminal anyway, and holding back a carried torch's softly moving pool would make
        // each step land in bands, top rows first, over up to LightSpreadFrames frames.
        int spreadBudget = dx == 0 && dy == 0 ? Math.Max(1, n / LightSpreadFrames) : int.MaxValue;

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
                if (!dmg[i] && !tv && !occupied && keys[i].State == 0)
                {
                    if (key.InputsMatch(in keys[i])) continue;
                    // A sky step past this frame's share keeps its old key, so the next frame
                    // finds it again (see SubtleLight).
                    if (key.SubtleLightChange(in keys[i]) && --spreadBudget < 0)
                    {
                        defer[i] = true;
                        deferred++;
                        continue;
                    }
                }

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
        _tileDeferred = deferred > 0;
        if (deferred > 0) { s_spreadFrames++; s_spreadCells += deferred; }
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
            key.ViewEdge = ViewEdgeOcclusion(mx, my);
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
        ResolveVisibleCell(mx, my, key.Vignette, key.ViewEdge, in key.Light, prof, ref t,
                           out _, out ch, out fg, out bg, out key.State);
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
    // `band` and `viewEdge` are the values the key already sampled for this cell, passed down
    // rather than recomputed: they are per-cell render inputs and the two must agree by
    // construction, or shading drifts from what the damage tracker compared.
    private void ResolveVisibleCell(int mx, int my, byte band, byte viewEdge,
                                    in LightingSystem.LightRgb light,
                                    bool prof, ref TileLoopTimings t,
                                    out TileType type, out char ch, out Color fg, out Color bg,
                                    out byte state)
    {
        state = 0;
        long t0 = prof ? Stopwatch.GetTimestamp() : 0L;
        var tile = _map.GetTile(mx, my);
        type = tile.Type;
        // Both answers are needed twice — once to record what is here, once to decide what to
        // draw — and HasItemsAt is a dictionary probe keyed by a tuple, so asking once per cell
        // rather than twice is worth having on a path that runs thousands of times a frame.
        bool occupied = tile.Occupant != null && !tile.Occupant.IsDefeated;
        bool hasItems = !occupied && _map.HasItemsAt(mx, my);
        if (occupied) state |= TileKey.SOccupant;
        else if (hasItems) state |= TileKey.SItems;
        (ch, fg, bg) = ResolveVisibleTile(_map, tile, mx, my, occupied, hasItems);
        // The glow's hue comes from the ground, so a cell holding a monster or a dropped item has
        // to resolve its terrain anyway — otherwise a monster would drag a patch of its own colour
        // across the floor as it moved. Only a handful of cells per viewport take this branch.
        Color ground = (state & (TileKey.SOccupant | TileKey.SItems)) != 0
            ? ResolveTerrain(tile, mx, my).fg
            : fg;
        long t1 = prof ? Stopwatch.GetTimestamp() : 0L;
        if (prof) t.Visible += t1 - t0;

        ApplyConnectedWalls(tile, mx, my, ref ch);
        long t2 = prof ? Stopwatch.GetTimestamp() : 0L;
        if (prof) t.Walls += t2 - t1;

        ApplyLighting(band, viewEdge, in light, mx, my, ground, ref fg, ref bg);
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

    // Gate for the slower render timer. True when any time-bounded effect is alive or a
    // clock-driven tile is on screen.
    public bool HasActiveRealtimeAnimations()
    {
        return HasActiveAnimations() || _animatedTileInView;
    }

    // Gate for the fast render timer: time-bounded effects only. Clock-driven tiles are excluded
    // because they step on 500ms+ intervals, so polling them at 50ms just repaints the same phase
    // ten times over — and each of those repaints costs a tile-layer replay plus the overlay
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
        // skip markers that are off-FOV or sitting under an occupant. Ticking there would let
        // a marker the player walked away from live forever, and since HasActiveAnimations
        // counts both lists, one stranded entry would keep the frame cache permanently dirty.
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
            // snapshot is required — the loop writes back into the dictionary — but it goes
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
    // A whole-viewport colour wash driven by a wall-clock ease, not by tile state. It must stay a
    // whole-viewport input to the tile layer's change detection (it is compared against
    // _tileStatusTint in FrameCache) — treated per cell, the map would keep the previous tint
    // until something else happened to dirty each cell.
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
            float eased = SAOTRPG.UI.Effects.EasingHelper.Ease(t,
                SAOTRPG.UI.Effects.EasingHelper.EasingType.EaseOut);
            Color from = _statusTintSourceColor ?? Color.Black;
            Color to = _statusTintTargetColor ?? Color.Black;
            _statusTintCurrentColor = SAOTRPG.UI.Effects.EasingHelper.LerpColor(from, to, eased);
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
    // Unquantized, a remembered cell's rendered byte changes every few turns, and since cells sit
    // at different ages a scattered share of the remembered map changes on any given turn. No
    // damage tracking can enumerate that. Snapped to steps a cell changes only when it crosses
    // one, and cells of the same age cross together.
    //
    // Age cohorts are spatially contiguous — the explored frontier sweeps — so the step
    // boundaries render as contour lines across remembered ground. The step count keeps each
    // boundary near a just-noticeable difference while a cohort crossing still re-resolves only
    // a small share of remembered cells per turn.
    private const int MemoryFadeSteps = 24;
    private const int MemoryFadeDelayTurns = 40;
    private const int MemoryFadeSpanTurns = 200;
    private const float MemoryFadeDepth = 0.6f;

    // The remembered map tracks the sky, or the night inverts. The memory palette is fixed and
    // lit ground is not: by day remembered ground sits under seen ground, but at midnight seen
    // ground falls while memory does not move, so the cells the player is looking at would render
    // darker than the map around them.
    //
    // The fade ladder's top drops with the same night exposure that lifts the lit cells, as
    // 1 / (1 + MemoryNightDrop x (gain - 1)); its floor (1 - MemoryFadeDepth) stays put, so old
    // memory is exactly as visible at night as by day and only fresh memory dims toward it. At
    // the midnight gain of 1.73 the top is 0.60. Remembered walls stay above a moonlit wall: the
    // structural palette is bright so the map stays readable. Identity by day: the gain is 1 at
    // and above ExposureKneeAmbient. 0 gives a fixed memory palette.
    //
    // Derived from the gain, not read off the sky, and that is load-bearing: MapView.FrameCache
    // compares ExposureGain as a whole-viewport input and nothing else about the sky, so a memory
    // scale that moved on any other clock would leave remembered cells stale while the damage
    // tracker saw no change. The gain is injective in the sky below the knee and constant above
    // it, so "the gain moved" is exactly "this moved".
    //
    // The picture depends on freshly remembered ground never being brighter than the moonlit
    // ground in view; a fixed memory palette inverts the night.
    private const float MemoryNightDrop = 0.9f;
    private static float s_memTop = 1f;

    // A remembered emissive tile's colour at full night, as a share of its emission; 0 keeps the
    // plain memory palette at night.
    private const float MemoryEmber = 0.5f;

    // Read by the verification probes, like ExposureGain: the top of the memory ladder the last
    // frame applied. 1.0 means the remembered palette is untouched.
    public static float MemoryTop => s_memTop;

    private static byte MemoryFadeStep(int age)
    {
        if (age <= MemoryFadeDelayTurns) return 0;
        int step = (age - MemoryFadeDelayTurns) * MemoryFadeSteps / MemoryFadeSpanTurns;
        return (byte)(step >= MemoryFadeSteps ? MemoryFadeSteps : step);
    }

    // The daylight branch is a separate expression, so a day frame is the plain fade exactly
    // rather than whatever the night formula's float rounding gives at a top of 1.
    private static float MemoryFadeScale(byte step)
    {
        float q = step / (float)MemoryFadeSteps;
        if (s_memTop >= 1f) return 1f - MemoryFadeDepth * q;
        return s_memTop - (s_memTop - (1f - MemoryFadeDepth)) * q;
    }

    private (char ch, Color fg, Color bg) ResolveMemoryTile(int mx, int my, byte fadeStep)
    {
        var memTile = _map.GetTile(mx, my);
        var tileType = memTile.Type;
        if (memTile.TrapHidden && tileType is TileType.TrapSpike or TileType.TrapTeleport
            or TileType.TrapPoison or TileType.TrapAlarm
            or TileType.TrapWeb or TileType.TrapMagnet or TileType.TrapRune)
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

        // Fading memory: full strength for 40 turns, then fades to ~40% over the next 200 — from
        // a top that the night lowers, so step 0 is only the identity by day.
        float scale = MemoryFadeScale(fadeStep);
        if (scale < 1f)
            fg = new Color((byte)(fg.R * scale), (byte)(fg.G * scale), (byte)(fg.B * scale));

        // A remembered light keeps an ember of its own colour after dark, so a fire, a lava field
        // or a shrine the player has seen stays something to navigate by across the night. Ramped
        // in by the moonlight's weight, so it is derived from the gain and absent by day.
        if (s_scot > 0f && LightingSystem.GetEmission(tileType) is { } em)
        {
            float t = s_scot * (1f / ScotopicMax);
            fg = new Color((byte)(fg.R + (em.Color.R * MemoryEmber - fg.R) * t),
                           (byte)(fg.G + (em.Color.G * MemoryEmber - fg.G) * t),
                           (byte)(fg.B + (em.Color.B * MemoryEmber - fg.B) * t));
        }

        // Memory tiles bypass ApplyLighting — they have their own palette and no light — so the
        // theme's world transform has to be applied here or it never reaches them — and
        // explored-but-unseen ground is most of a mapped floor.
        // Applied after the fade so the two compose the same way lighting and theme do on a
        // visible tile: the transform acts on the colour actually being drawn.
        return (ch, OverlayColor(fg), Color.Black);
    }

    // `occupied` and `hasItems` are the answers the caller already has; they are not re-derived
    // here, so the cell's state record and what is drawn on it cannot disagree.
    private (char ch, Color fg, Color bg) ResolveVisibleTile(Map.GameMap map, Map.Tile tile,
                                                             int mx, int my,
                                                             bool occupied, bool hasItems)
    {
        if (occupied) return ResolveOccupant(tile, mx, my);
        if (hasItems)
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
            or TileType.TrapPoison or TileType.TrapAlarm
            or TileType.TrapWeb or TileType.TrapMagnet or TileType.TrapRune)
            renderType = TileType.Floor;

        var visual = _visualCache.Get(mx, my, renderType);
        char ch = visual.Ch; Color fg = visual.Fg; Color bg = visual.Bg;
        if (tile.Type == TileType.Door && _openedDoors.Contains((mx, my))) ch = '·';
        if (tile.Type is TileType.Water or TileType.WaterDeep)
            ResolveWater(tile, mx, my, ref ch, ref fg);

        var transition = MapEffects.GetTransitionBorder(_map, mx, my, tile.Type);
        if (transition != null) { ch = transition.Value.Glyph; fg = transition.Value.Color; }

        // A themed room takes the room's hue at the cell's own lightness, so the palette dither
        // survives and the cell's brightness does not move. GetRoomTheme is the whole gate.
        var roomTint = MapEffects.GetRoomTheme(mx, my, tile.Type);
        if (roomTint != null) fg = MapEffects.TintAtOwnLightness(fg, roomTint.Value);
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
            else ch = MapEffects.GetWaterFlowGlyph(mx, my, tile.Type == TileType.WaterDeep);
        }
        var shore = MapEffects.GetShorelineVisual(_map, mx, my, tile.Type);
        if (shore != null) { ch = shore.Value.Glyph; fg = shore.Value.Fg; }
    }

    private void ApplyConnectedWalls(Map.Tile tile, int mx, int my, ref char ch)
    {
        if (tile.Type is TileType.Wall or TileType.CrackedWall)
            ch = MapEffects.GetConnectedWallGlyph(_map, mx, my);
    }

    // Derived from the light field's own definition of full, never a second 255: a tile renders
    // at exactly its authored colour when the light equals LightFull, and above that it renders
    // brighter. One constant keeps the two halves of that contract in step.
    private const float InvLightScale = 1f / LightingSystem.LightFull;

    // ── The night's response curve ─────────────────────────────────────────────────────────────
    // fg = base x light / LightFull is a linear response, and it puts the whole night in the
    // bottom tenth of the range. Ambient bottoms out at (18, 22, 45), so a midnight tile renders
    // at 7-18% of its authored colour. That is not banding — the range simply is not there.
    //
    // The sky is not the lever; the response is. Raising Ambient moves every cell of every floor
    // and the banding, vignette and sight-edge profiles tuned against it. What is free is how a
    // light value becomes a pixel.
    //
    // The gain is keyed to the sky, not to the cell. A per-cell tone curve has to be the identity
    // above the darkest daylight cell (deep shadow at noon), so it must climb back to identity
    // across the narrow band between the night and that shadow, which renders twilight almost
    // flat. A gain that is one number for the whole scene has no transition band at all: a
    // uniform multiply preserves every ratio in frame exactly, so nothing is compressed anywhere,
    // and it is what the eye does — adapt to the ambient, not to the pixel.
    //
    // Gamma on the sky, a plain multiply on the cell. A power law applied per channel to the light
    // compresses the night's spread, and a flat additive lift raises daylight too. Only a multiply
    // moves the same relative spread up the L* curve, where it is worth more.
    //
    // Three constants, and each answers one question.
    //   ExposureKneeAmbient — the sky level at and above which this does nothing at all, so the
    //     top fifth of the day is byte-identical by construction.
    //   ExposureGamma       — how hard the dark sky is lifted: 1.0 is a linear response, lower
    //     lifts more. How bright a night should be is a look decision; the memory ladder's top
    //     tracks this gain (MemoryNightDrop), so seen and remembered ground cannot land on each
    //     other whatever this is set to.
    //   ExposureShoulder    — where a light already brighter than the sky stops being scaled, as
    //     a multiple of the sky's own level. A torch core at midnight must not be scaled again on
    //     top of the headroom SourceOverdrive already gives it. It must exceed the largest gain
    //     the gamma can produce, or the ramp back to the identity runs backwards; 5 against the
    //     midnight gain of 1.73 leaves room for the dial to be turned.
    private const float ExposureKneeAmbient = 0.60f;
    private const float ExposureGamma = 0.55f;
    private const float ExposureShoulder = 5f;

    // ── Moonlight ──────────────────────────────────────────────────────────────────────────────
    // Moonlight is (18, 22, 45): almost all blue, and blue is 7% of luminance, so blue light on
    // green grass multiplies out to a dark teal that a gain can only brighten by washing coloured
    // lights out. Night vision has no colour at all: the eye keeps the brightness of a moonlit
    // field and loses its hue. So the sky's share of a cell's light (per channel, up to the sky's
    // own level) is rendered as a cool grey of the luma a neutral light of that strength gives;
    // anything above it is a source's and adds in full colour. Split that way every term grows
    // with the light, so more light can never render a darker channel, which a weight that faded
    // out across the source's range would allow.
    //   ScotopicMax     — the share of the drift at the darkest gain; 0 turns the drift off.
    //   ScotopicFullAt  — the gain above 1 at which that share is reached, so twilight ramps in.
    //   ScotopicHueKeep — how much of the sky's own hue the grey keeps (0 neutral, 1 the sky's).
    // Derived from the gain like the memory ladder's top, so the frame cache already tracks it.
    private const float ScotopicMax = 0.6f;
    private const float ScotopicFullAt = 0.6f;
    private const float ScotopicHueKeep = 0.5f;
    private static float s_scot, s_scotSky, s_scotR = 1f, s_scotG = 1f, s_scotB = 1f;
    private static float s_skyR, s_skyG, s_skyB;   // the exposed sky, per channel

    // Read by the verification probes: the drift's weight on the sky's share this frame (0 by
    // day), and the exposed sky's own level (its brightest channel), above which light is a source's.
    public static float MoonlightWeight => s_scot;
    public static float MoonlightSkyLevel => s_scotSky;
    public static (float R, float G, float B) MoonlightTint => (s_scotR, s_scotG, s_scotB);

    // Live exposure, refreshed once per frame from the sky. Static because there is one sky and
    // one map: the verification probes read the operator through Expose() without holding a view.
    private static float s_expGain = 1f;
    private static float s_expKnee, s_expEnd, s_expSlope;

    // Read by the verification probes, like LightingSystem.SourcesLastUpdate: the gain the last frame
    // actually applied (1.0 means the operator is the identity), and the light level at and below
    // which every channel takes that full gain — the sky's own level, which is where the hue
    // guarantee holds.
    public static float ExposureGain => s_expGain;
    public static float ExposureKnee => s_expKnee;

    // The operator itself, so the verification probes can build a reference without a second
    // copy of the curve.
    public static LightingSystem.LightRgb Expose(in LightingSystem.LightRgb light)
    {
        float r = light.R, g = light.G, b = light.B;
        ExposeLight(ref r, ref g, ref b);
        return LightingSystem.LightRgb.Of(r, g, b);
    }

    // Recomputes the gain from the sky. Must run before the frame-cache test — the gain is a
    // whole-viewport input the per-cell scan cannot see, and MapView.FrameCache compares it.
    // Public so the verification probes can evaluate the operator at an hour the view is not
    // painting; it reads DayNightCycle and the theme and nothing else, so calling it out of band
    // cannot desynchronise anything the next frame will not immediately redo.
    public static void RefreshExposure()
    {
        var (ar, ag, ab) = SAOTRPG.Map.Lighting.DayNightCycle.Ambient;
        float a = MathF.Max(ar, MathF.Max(ag, ab)) * InvLightScale;
        // The shoulder has to be cleared, not just the gain: leaving the previous hour's knee and
        // slope behind would put Expose on its shoulder branch with a gain of 1, which scales
        // down. An end of 0 makes the first compare in ExposeLight exit for every light.
        if (a >= ExposureKneeAmbient || a <= 0f)
        {
            s_expGain = 1f;
            s_expKnee = 0f; s_expEnd = 0f; s_expSlope = 1f;
            s_memTop = 1f;
            s_scot = 0f;
            return;
        }
        // The rendered sky follows a power curve of the true sky, anchored at the knee.
        float target = ExposureKneeAmbient * MathF.Pow(a / ExposureKneeAmbient, ExposureGamma);
        float k = target / a;
        float knee = a * LightingSystem.LightFull;
        // Capped at full, or the curve invents over-range light out of ordinary light. The curve
        // is the identity at the shoulder, so a shoulder above LightFull leaves C(LightFull) above
        // LightFull: at dusk (sky 100) an uncapped 5x shoulder would put a channel of 255 at 268,
        // which the spill would then treat as a source. At midnight 5 x 45 is already under full,
        // so only the brighter skies reach the cap.
        float end = MathF.Min(knee * ExposureShoulder, LightingSystem.LightFull);
        s_expGain = k;
        s_expKnee = knee;
        s_expEnd = end;
        // The remembered map's ladder top, from the same gain; never below the ladder's floor.
        s_memTop = MathF.Max(1f - MemoryFadeDepth, 1f / (1f + MemoryNightDrop * (k - 1f)));
        RefreshMoonlight(k, knee, ar, ag, ab);
        // Positive by construction: the shoulder is at least the sky's level times
        // ExposureShoulder or LightFull, and the lifted sky k x knee can never exceed
        // ExposureKneeAmbient x LightFull = 153. Clamped anyway, since a non-positive slope would
        // make more light render darker across the shoulder.
        s_expSlope = MathF.Max(0f, (end - k * knee) / (end - knee));
    }

    // The moonlight drift for this sky: its weight, the exposed sky level, and the tint — the sky's
    // hue, partly desaturated, through the theme (so a monochrome theme stays monochrome), at unit
    // luma so the drift moves hue and never brightness.
    private static void RefreshMoonlight(float k, float knee, float ar, float ag, float ab)
    {
        s_scot = ScotopicMax * MathF.Min(1f, (k - 1f) / ScotopicFullAt);
        s_scotSky = knee * k;
        s_skyR = ar * k; s_skyG = ag * k; s_skyB = ab * k;
        float ys = Luma(ar, ag, ab);
        if (ys <= 0f) { s_scot = 0f; return; }
        float tr = 1f + (ar / ys - 1f) * ScotopicHueKeep;
        float tg = 1f + (ag / ys - 1f) * ScotopicHueKeep;
        float tb = 1f + (ab / ys - 1f) * ScotopicHueKeep;
        var theme = ColorSchemes.Theme;
        if (theme.TintsWorld)
        {
            float m = 120f / MathF.Max(tr, MathF.Max(tg, tb));
            var t = theme.World(new Color((byte)(tr * m), (byte)(tg * m), (byte)(tb * m)));
            tr = t.R; tg = t.G; tb = t.B;
        }
        float yt = Luma(tr, tg, tb);
        if (yt <= 0f) { s_scot = 0f; return; }
        s_scotR = tr / yt; s_scotG = tg / yt; s_scotB = tb / yt;
    }

    // Rec.709 luma on the stored (gamma-domain) channels: a weighting for a hue, not a lightness.
    private static float Luma(float r, float g, float b) => 0.2126f * r + 0.7152f * g + 0.0722f * b;

    // One scalar curve, applied to each channel independently. Three regions: at or below the
    // sky's own level the full gain; at or above the shoulder the identity, so a source keeps the
    // scale SourceOverdrive gave it; between them a straight line landing exactly on the identity.
    // Piecewise-linear rather than a smoothstep on purpose — a smoothstep's own slope can exceed
    // the shoulder's and run the ramp backwards, rendering a brighter light darker than a dimmer
    // one.
    //
    // Per channel, not one factor taken from the light's max channel: a shared factor holds hue
    // exactly but makes the map non-monotone across channels — a cell with far more red gets a
    // smaller factor, so its blue renders darker than the same ground gets in the dark, and
    // ground near a campfire renders darker under strictly more light. A scalar curve applied
    // per channel cannot do that.
    //
    // Hue is exact for any light at or below the sky's level — all three channels take the same
    // gain — which is the whole of the ambient-lit night. Above it the channels compress at
    // different points on the curve, which is a mild desaturation confined to the outer ring of a
    // source's own pool.
    //
    // It cannot invent over-range light: the curve is monotone and fixes LightFull (the shoulder
    // ends at or below it), so a channel at or under full comes back at or under full.
    //
    // The picture depends on a dark sky rendering brighter than it is, while anything already
    // brighter than that sky keeps its own brightness. It is a pure function of (light triple,
    // sky level) — the triple is already sampled by the damage tracker and the sky level is
    // tracked as a whole-viewport input — so it adds nothing to TileKey.
    private static void ExposeLight(ref float r, ref float g, ref float b)
    {
        // Duplicated at the ApplyLighting call site to save the call in the hot loop; kept here so
        // the public Expose() seam is the identity in daylight whatever the shoulder happens to
        // hold.
        if (s_expGain <= 1f) return;
        r = ExposeChannel(r); g = ExposeChannel(g); b = ExposeChannel(b);
    }

    private static float ExposeChannel(float c)
    {
        if (c >= s_expEnd) return c;
        if (c <= s_expKnee) return c * s_expGain;
        return s_expGain * s_expKnee + s_expSlope * (c - s_expKnee);
    }

    // Lit colours are snapped to this step per channel.
    //
    // This is not the tile layer's change detection: TileKey.Light holds the raw light triple,
    // so the scan already compares unquantized values. What the step buys is terminal escapes:
    // adjacent cells sharing one attribute travel as one SGR run.
    //
    // A coarse step bands. The background glow is light x BgGlowScale, a 0-102 range with light's
    // headroom to LightCeiling, so wide steps leave a viewport with few distinct background
    // levels and visible contour edges. At step 2 the per-cell error is one 8-bit code point, the
    // floor the colour depth itself sets.
    //
    // Step 1 would smooth the remaining coherent contour edges further, at the cost of more
    // attribute runs, and Quantize's output being always even is what keeps it observable from
    // the output that the glow is shaped before the quantizer.
    //
    // The light field is evaluated once per turn, so nothing in it may read a wall clock.
    private const int LightQuantizeStep = 2;

    // Takes a float and saturates, because an unchecked C# float->byte cast wraps: (byte)256f is
    // 0, (byte)300f is 44, (byte)(-3f) is 253 — so an over-range channel would render near-black,
    // not white. Light carries headroom to LightCeiling, so `base x light / LightFull` routinely
    // exceeds 255 near a source and reaches here after SpillOverRange has moved what it can into
    // the other channels.
    private static byte Quantize(float channel)
    {
        // Ordered so the ordinary case takes one compare: NaN fails `> 0f` and lands on 0.
        if (!(channel > 0f)) return 0;
        if (channel >= 255f) return 255;
        int q = ((int)channel + LightQuantizeStep / 2) / LightQuantizeStep * LightQuantizeStep;
        return (byte)(q > 255 ? 255 : q);
    }

    // ── The glow takes the ground's own hue ────────────────────────────────────────────────────
    // Every tile in TileDefinitions declares Color.Black as its background, so the map's colour
    // lives entirely in glyph foregrounds and an untinted glow would carry only the light's hue —
    // grey at noon whatever the player is standing on. With no authored background to multiply
    // by the light, the glow is steered toward the terrain colour, so floors read as ground
    // rather than as glyphs on black.
    //
    // Luma is held exactly (Rec.709, gamma domain — the same luma ApplyStatusTint uses), so this
    // moves hue and not lightness: the vignette ramp, the view-edge feather and the
    // visible/remembered step all keep their lightness.
    //
    // The FOV boundary is why the biome wash is foreground-only, and it does not apply here: this
    // rides on the glow, which the feather already ramps to black where sight ends, so the tint
    // fades out with it rather than ending on an edge.
    //
    // The picture depends on lit ground glowing in its own colour at the light's brightness. The
    // source is the terrain colour, not whatever the cell resolved to — tinting by an occupant
    // would make every monster look like it were emitting light.
    private const float GroundGlowTint = 0.5f;

    private static void TintGlowToGround(Color ground, ref float r, ref float g, ref float b)
    {
        float cl = ground.R * 0.2126f + ground.G * 0.7152f + ground.B * 0.0722f;
        if (cl < 1f) return;
        float gl = r * 0.2126f + g * 0.7152f + b * 0.0722f;
        if (gl <= 0f) return;
        // Renormalises the ground colour to the glow's own luma: keep the direction of the
        // colour vector, replace its length.
        float k = gl / cl * GroundGlowTint;
        const float ia = 1f - GroundGlowTint;
        r = r * ia + ground.R * k;
        g = g * ia + ground.G * k;
        b = b * ia + ground.B * k;
    }

    // Share of the light bled into a cell's background — the ambient glow.
    //
    // The step at the edge of sight, the vignette's band ramp and the night's glyph-vs-background
    // separation all scale with this, so a larger share spends the view-edge feather's smoothing
    // and sinks night glyphs into their own background. It is bounded by the darkest hour rather
    // than by how finely the value can be represented.
    private const float BgGlowScale = 0.20f;

    // ── FOV edge vignette ──────────────────────────────────────────────────────────────────────
    // Far-but-visible cells dim toward the edge of view, so the lit region reads as an
    // illumination sphere centred on the player rather than a flat field cut off by walls.
    //
    // The reference is the viewport, not DayNightCycle.FovRadius. FovRadius is `halfHeight ×
    // FovMultiplier(4)`, about twice the distance to the farthest on-screen cell, so a vignette
    // scaled from it would never reach a visible cell. FOV radius is deliberately generous so
    // everything on screen is lit; it is not a measure of what the player can see.
    //
    // Band count trades banding against churn. The bands are concentric contours over the whole
    // viewport, the textbook shape for visible banding, so each step has to stay under a
    // just-noticeable difference; the cost is that a band-crossing cell must be resolved again on
    // every step the player takes.
    //
    // The scale ladder is 255 - band x (255 - VignetteMinScale) / (VignetteBands - 1), so it comes
    // out even exactly when VignetteBands-1 divides 115 = 5 x 23 — i.e. at 2, 6, 24 or 116 bands.
    // Any other count either leaves truncated steps wider than their neighbours or needs
    // VignetteMinScale moved, which changes how deep the vignette is rather than how finely it is
    // sampled. At 116 the step is exactly 1 of 255 and the continuous envelope is untouched: the
    // floor an 8-bit channel sets.
    private const int VignetteBands = 116;
    private const float VignetteInnerFrac = 0.65f;  // no dimming inside this share of the extent
    private const int VignetteMinScale = 140;       // out of 255 at the outermost band (~55%)

    // Exported for the verification probes, which bucket cells by the renderer's own ladder; a
    // copy of either number would drift the first time the ladder is retuned.
    public const int VignetteBandCount = VignetteBands;
    public const float VignetteInnerFraction = VignetteInnerFrac;

    private float _vignetteInnerSq, _vignetteSpanSq;
    // 1/aspect², so the per-cell distance is one multiply. Aspect-corrected because cells are 2:1
    // and an uncorrected radius would make the vignette an ellipse.
    private static readonly float InvAspectSq =
        1f / (Map.Generation.FloorMask.CellAspectRatio * Map.Generation.FloorMask.CellAspectRatio);

    // The frame cache's fully-idle path (blit the finished frame, paint nothing) is unreachable
    // while ambient particles are alive, which on any biome that declares one with density on is
    // always. HasActiveAnimations() includes ParticleQueue.HasAny, which leads to DirtyOverlays(),
    // which sets _frameDirty, which IsFrameDirty tests. The steady state is therefore the overlay
    // path, not the idle one: a tile-layer blit plus the overlay passes each frame — but not
    // necessarily a terminal flush, because Gfx change detection skips cells whose contents did
    // not move, and a mote drifting at ~1 cell/second changes very few of them. Turning
    // ParticleDensity off restores the idle path.
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

    // Scale for a band, 255 = untouched. Derived from the band, never from the raw distance —
    // that is what keeps it consistent with the quantized value stored in TileKey. A continuous
    // scale against a quantized key would let a cell's shading drift without the damage tracker
    // ever noticing, which is exactly the stale-layer failure --verify-tiles exists to catch.
    private static int VignetteScale(byte band) =>
        band == 0 ? 255 : 255 - band * (255 - VignetteMinScale) / (VignetteBands - 1);

    // ── The glow's own shape ───────────────────────────────────────────────────────────────────
    // The background carries no information, which is what makes all of this safe. A glyph has
    // to be read, so the foreground vignette floors at VignetteMinScale; the background is pure
    // atmosphere and may go all the way to black.
    //
    // That matters because the largest discontinuity on the map is not a gradient artefact at
    // all — it is the edge of knowledge. ResolveMemoryTile returns Color.Black for a remembered
    // cell, while a lit one's background is light x BgGlowScale, so wherever sight runs out the
    // background falls off a cliff, steepest at noon and nearly gone at midnight.
    //
    // Two terms, and they answer different halves of it.
    //
    // The picture depends on the ambient glow being an illumination sphere that reaches zero by
    // the rim and softens wherever the visible region ends, while glyph brightness is barely
    // touched. Both terms are functions of quantized integers — the vignette band and a 0-8
    // neighbour count — never of a raw distance, because both are sampled into TileKey and a
    // continuous value compared against a quantized key lets shading drift unseen by the damage
    // tracker.
    private const int VignetteBgMinScale = 0;

    // ── The view-edge feather ──────────────────────────────────────────────────────────────────
    // Scale on the ambient glow by feather depth, out of 255. This spreads the cliff from lit
    // ground to a remembered cell's black background over two cells: interior 255, a cell two
    // away from the frontier 190, a straight frontier 105, then black. Those land on roughly equal
    // steps in CIE L*.
    //
    // Index is the depth ViewEdgeOcclusion reports: 0 interior, 1 the far tier, 2-9 an adjacent
    // contact (its unseen-neighbour count plus one). Index 9 — a lit cell alone in the dark —
    // reaches black exactly. A table rather than a linear step because the three visible rungs
    // have to be equal in L*, which is a cube root of the channel and not linear in it.
    private static readonly int[] EdgeFeatherRamp = { 255, 190, 165, 140, 105, 84, 63, 42, 21, 0 };

    // A cell that shares an edge with something unseen takes at least a straight frontier's
    // feather, whatever its neighbour count says: the step across a shared edge is the same size
    // however short that edge is, and a one- or two-neighbour contact would otherwise keep most
    // of its glow right beside the black.
    private const int ViewEdgeOrthoFloor = 3;

    // Combined 0-255 scale on the ambient glow. Derived from the band and the depth index, never
    // from the raw distance or the raw neighbourhood — see the note above.
    private static int GlowScale(byte band, byte viewEdge)
    {
        int s = band == 0 ? 255 : 255 - band * (255 - VignetteBgMinScale) / (VignetteBands - 1);
        if (viewEdge > 0)
        {
            int edge = EdgeFeatherRamp[viewEdge < EdgeFeatherRamp.Length
                                       ? viewEdge : EdgeFeatherRamp.Length - 1];
            s = edge <= 0 ? 0 : s * edge / 255;
        }
        return s;
    }

    // Feather depth, 0-9, indexing EdgeFeatherRamp:
    //   0     nothing unseen within two cells
    //   1     an unseen cell at Chebyshev distance 2, none adjacent — the far tier
    //   2-9   an adjacent unseen cell: that count (1-8), floored at ViewEdgeOrthoFloor when one of
    //         the four orthogonal neighbours is among them, plus one
    // Off-map counts as unseen — the camera is unclamped, so the map's outer ring can be on screen
    // and beyond it is black.
    //
    // One cell of feather only moves the cliff one cell in; the far tier is what makes the
    // falloff a ramp. It has to be the whole Chebyshev-2 ring, because testing only the four
    // cardinals (or the eight principal directions) leaves diagonal frontiers with a
    // 255-next-to-105 adjacency.

    // ── The feather's neighbourhood, as bit planes ─────────────────────────────────────────────
    // ViewEdgeOcclusion asks two yes/no questions of every visible cell — is anything unseen
    // within one cell, and within two — and answering them by probing costs up to 24 strided
    // reads of the visibility grid per cell on every scan. Both are morphological dilations of
    // the unseen set, and a dilation is separable: one bit per cell, three shifted ORs per word
    // horizontally, an OR of adjacent rows vertically. The whole neighbourhood costs one pass
    // over the region plus a few word operations per row, and the per-cell query becomes two
    // bit tests.
    //
    // The exact neighbour count still needs the eight probes, but only on cells that have an
    // unseen neighbour at all — the frontier, a few percent of a lit viewport.
    //
    // Region is the viewport grown by 2 on every side, so a query at the viewport's own edge
    // still has its whole 5x5 inside the planes. Off-map counts as unseen, matching the probing
    // form, which reads a bounds-checked IsVisible there.
    //
    // Pure bookkeeping over the visibility grid: plain neighbour probes give identical output,
    // and only the cost differs.
    private ulong[]? _edgeUnseen, _edgeH3, _edgeH5, _edgeB3, _edgeB5;
    private int _edgeX0, _edgeY0, _edgePw, _edgePh, _edgeWords;
    private bool _edgePlanesValid;

    // Rebuilt at the top of every pass that reads it, so it cannot go stale: the only inputs are
    // the visibility grid and the viewport rect, both fixed for the duration of a scan.
    private void RefreshViewEdgePlanes(int offX, int offY, int w, int h)
    {
        int pw = w + 4, ph = h + 4;
        int words = (pw + 63) >> 6;
        int need = words * ph;
        if (_edgeUnseen is null || _edgeUnseen.Length != need)
        {
            _edgeUnseen = new ulong[need];
            _edgeH3 = new ulong[need];
            _edgeH5 = new ulong[need];
            _edgeB3 = new ulong[need];
            _edgeB5 = new ulong[need];
        }
        _edgeWords = words; _edgePw = pw; _edgePh = ph;
        _edgeX0 = offX - 2; _edgeY0 = offY - 2;

        var u = _edgeUnseen!;
        var h3 = _edgeH3!;
        var h5 = _edgeH5!;
        var b3 = _edgeB3!;
        var b5 = _edgeB5!;
        Array.Clear(u, 0, need);

        int mapW = _map.Width, mapH = _map.Height;
        for (int ry = 0; ry < ph; ry++)
        {
            int my = _edgeY0 + ry, rowBase = ry * words;
            if ((uint)my >= (uint)mapH)
            {
                // A row entirely off the map is entirely unseen; the camera is unclamped, so this
                // really happens at the top and bottom of the world.
                for (int k = 0; k < words; k++) u[rowBase + k] = ulong.MaxValue;
            }
            else
            {
                for (int rx = 0; rx < pw; rx++)
                {
                    int mx = _edgeX0 + rx;
                    if ((uint)mx < (uint)mapW && _map.IsVisibleUnchecked(mx, my)) continue;
                    u[rowBase + (rx >> 6)] |= 1UL << (rx & 63);
                }
            }
            DilateRow(u, h3, rowBase, words);   // +-1 column
            DilateRow(h3, h5, rowBase, words);  // +-2 columns
        }

        // Vertical halves. Only the rows a query can reach need be correct — a query sits at
        // ry in [2, ph-3], so ry +- 2 is always inside the plane.
        for (int ry = 1; ry < ph - 1; ry++)
        {
            int r = ry * words, up = r - words, dn = r + words;
            for (int k = 0; k < words; k++) b3[r + k] = h3[up + k] | h3[r + k] | h3[dn + k];
        }
        for (int ry = 2; ry < ph - 2; ry++)
        {
            int r = ry * words, u1 = r - words, u2 = r - 2 * words, d1 = r + words, d2 = r + 2 * words;
            for (int k = 0; k < words; k++)
                b5[r + k] = h5[u2 + k] | h5[u1 + k] | h5[r + k] | h5[d1 + k] | h5[d2 + k];
        }
        _edgePlanesValid = true;
    }

    // dst[c] = src[c-1] | src[c] | src[c+1], one row, carrying across word boundaries.
    private static void DilateRow(ulong[] src, ulong[] dst, int rowBase, int words)
    {
        for (int k = 0; k < words; k++)
        {
            ulong cur = src[rowBase + k];
            ulong lo = k > 0 ? src[rowBase + k - 1] >> 63 : 0UL;
            ulong hi = k + 1 < words ? src[rowBase + k + 1] << 63 : 0UL;
            dst[rowBase + k] = cur | ((cur << 1) | lo) | ((cur >> 1) | hi);
        }
    }

    // The planes answer the two yes/no tiers with a bit test each; only a cell that really has an
    // unseen neighbour pays for the count. Falls back to the probing form wherever the planes do
    // not cover the cell, so the two can never disagree by construction.
    //
    // CONTRACT: the caller's pass must have called RefreshViewEdgePlanes for the same viewport
    // rect. Both scan paths do, immediately before their cell loop. A stale plane would survive
    // the range guard after a one-cell camera move, and the cross-check below is what would
    // catch it.
    private byte ViewEdgeOcclusion(int mx, int my)
    {
        byte v;
        int cx = mx - _edgeX0, cy = my - _edgeY0;
        if (_edgePlanesValid
            && (uint)(mx - 2) < (uint)(_map.Width - 4) && (uint)(my - 2) < (uint)(_map.Height - 4)
            && (uint)(cx - 2) < (uint)(_edgePw - 4) && (uint)(cy - 2) < (uint)(_edgePh - 4))
        {
            int idx = cy * _edgeWords + (cx >> 6);
            ulong bit = 1UL << (cx & 63);
            // Nothing unseen inside the 5x5 — the overwhelming majority of a lit viewport.
            if ((_edgeB5![idx] & bit) == 0) v = 0;
            // Something at Chebyshev 2 but the 3x3 is clear: the far tier.
            else if ((_edgeB3![idx] & bit) == 0) v = 1;
            // An adjacent unseen cell, so the exact count is needed. The centre is visible (this
            // is only called for visible cells), so the probing form takes its n > 0 branch.
            else v = ViewEdgeOcclusionProbed(mx, my);
        }
        else v = ViewEdgeOcclusionProbed(mx, my);

        // The planes are a second implementation of a predicate the --verify-tiles layer diff
        // cannot see: the patched layer and the full recompute use the same planes, so a wrong
        // plane would be wrong identically on both sides. Under --verify-tiles the probing form is
        // evaluated as well and any disagreement is counted.
        if (s_verifyEdges)
        {
            byte reference = ViewEdgeOcclusionProbed(mx, my);
            s_edgeChecked++;
            if (reference != v)
            {
                s_edgeMismatches++;
                if (s_edgeMismatches <= VerifyLogLimit)
                    DebugLogger.LogGame("VERIFY",
                        $"view-edge mismatch map({mx},{my}) planes={v} probed={reference}");
                v = reference;
            }
            else if (v == 0) s_edgeTier0++;
            else if (v == 1) s_edgeTier1++;
            else s_edgeTierN++;
        }
        return v;
    }

    // Read-only, for the verification probes. The tier counters show which branches a run
    // exercised: the interior, the far tier, and the counted neighbours.
    public static long VerifyEdgeChecked => s_edgeChecked;
    public static long VerifyEdgeMismatches => s_edgeMismatches;
    public static (long Interior, long FarTier, long Counted) VerifyEdgeTiers
        => (s_edgeTier0, s_edgeTier1, s_edgeTierN);

    private static readonly bool s_verifyEdges = SAOTRPG.Systems.Diagnostics.DebugMode.VerifyTiles;
    private static long s_edgeChecked, s_edgeMismatches, s_edgeTier0, s_edgeTier1, s_edgeTierN;

    // The reference form: probe the neighbours one at a time. The whole answer wherever the
    // planes do not reach, and the only path that produces the exact neighbour count. The far
    // tier uses short-circuit `||` rather than a counting loop, because the answer there is only
    // "is any of them unseen".
    private byte ViewEdgeOcclusionProbed(int mx, int my)
    {
        // One bounds test for the whole 5x5, then unchecked reads: per-call bounds tests on
        // eight IsVisible calls would be most of the cost.
        if ((uint)(mx - 2) < (uint)(_map.Width - 4) && (uint)(my - 2) < (uint)(_map.Height - 4))
        {
            int o = (_map.IsVisibleUnchecked(mx - 1, my) ? 0 : 1)
                  + (_map.IsVisibleUnchecked(mx + 1, my) ? 0 : 1)
                  + (_map.IsVisibleUnchecked(mx, my - 1) ? 0 : 1)
                  + (_map.IsVisibleUnchecked(mx, my + 1) ? 0 : 1);
            int n = o + (_map.IsVisibleUnchecked(mx - 1, my - 1) ? 0 : 1)
                      + (_map.IsVisibleUnchecked(mx + 1, my - 1) ? 0 : 1)
                      + (_map.IsVisibleUnchecked(mx - 1, my + 1) ? 0 : 1)
                      + (_map.IsVisibleUnchecked(mx + 1, my + 1) ? 0 : 1);
            if (n > 0)
            {
                if (o > 0 && n < ViewEdgeOrthoFloor) n = ViewEdgeOrthoFloor;
                return (byte)(n + 1);
            }
            return FarFeather(mx, my);
        }
        return ViewEdgeOcclusionAtBorder(mx, my);
    }

    // The far tier: any unseen cell on the Chebyshev-2 ring, with the 3x3 already known clear.
    private byte FarFeather(int mx, int my)
    {
        bool near = !_map.IsVisibleUnchecked(mx - 2, my - 2) || !_map.IsVisibleUnchecked(mx - 1, my - 2)
                 || !_map.IsVisibleUnchecked(mx,     my - 2) || !_map.IsVisibleUnchecked(mx + 1, my - 2)
                 || !_map.IsVisibleUnchecked(mx + 2, my - 2) || !_map.IsVisibleUnchecked(mx - 2, my + 2)
                 || !_map.IsVisibleUnchecked(mx - 1, my + 2) || !_map.IsVisibleUnchecked(mx,     my + 2)
                 || !_map.IsVisibleUnchecked(mx + 1, my + 2) || !_map.IsVisibleUnchecked(mx + 2, my + 2)
                 || !_map.IsVisibleUnchecked(mx - 2, my - 1) || !_map.IsVisibleUnchecked(mx - 2, my)
                 || !_map.IsVisibleUnchecked(mx - 2, my + 1) || !_map.IsVisibleUnchecked(mx + 2, my - 1)
                 || !_map.IsVisibleUnchecked(mx + 2, my)     || !_map.IsVisibleUnchecked(mx + 2, my + 1);
        return near ? (byte)1 : (byte)0;
    }

    // The map's outer two rings, where a neighbour index would run off the grid. Same answer, one
    // bounds test per probe; off-map counts as unseen, so the far tier is the floor here.
    private byte ViewEdgeOcclusionAtBorder(int mx, int my)
    {
        int n = 0, o = 0;
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            if ((dx | dy) == 0) continue;
            if (_map.IsVisible(mx + dx, my + dy)) continue;
            n++;
            if ((dx & dy) == 0) o++;   // exactly one of the two offsets is zero
        }
        if (n == 0) return 1;
        if (o > 0 && n < ViewEdgeOrthoFloor) n = ViewEdgeOrthoFloor;
        return (byte)(n + 1);
    }

    // ── Biome atmosphere particles ─────────────────────────────────────────────────────────────
    // Kept topped up around the camera: spawned where the player is looking and refreshed as
    // they expire, so the budget is always on screen rather than spread across the whole map.
    //
    // The densest biome (8 permille) keeps the whole budget; the rest scale down against it, so a
    // biome's declared densityPermille sets its share.
    private const int AmbientPermilleAtFullBudget = 8;
    private const int AmbientSpawnTries = 24;   // rejection sampling for a visible walkable cell
    private Map.GameMap? _ambientMap;

    // Target on-screen atmosphere for the player's density setting. Deliberately modest: these are
    // slow-drifting single glyphs, and many simultaneous animations read as visual noise.
    // 0 means the player turned atmosphere off, and the caller relies on that: it returns on a zero
    // budget before applying the at-least-one floor.
    private static int AmbientBudget() => UserSettings.Current.ParticleDensity switch
    {
        SAOTRPG.Systems.Persistence.ParticleDensity.Subtle     => 2,
        SAOTRPG.Systems.Persistence.ParticleDensity.Moderate   => 4,
        SAOTRPG.Systems.Persistence.ParticleDensity.Pronounced => 7,
        _                                  => 0,
    };

    // Tops the atmosphere back up to target inside the current viewport. Runs every frame; the
    // steady state is one integer compare, because the budget is only short when a particle has
    // just expired or the camera has moved somewhere emptier.
    private void TickAmbientParticles(int w, int h)
    {
        // Motion.Animate covers both reduce-motion and --freeze-anim.
        if (!SAOTRPG.UI.Effects.Motion.Animate) return;

        string? id = SAOTRPG.Systems.World.BiomeSystem.CurrentGenConfig?.AmbientOverlay?.ParticleId;
        if (string.IsNullOrEmpty(id)) return;

        // A floor change invalidates the atmosphere: the previous biome's motes should not drift
        // on into the new one. Generation does not clear them, so this is where it happens.
        if (!ReferenceEquals(_ambientMap, _map))
        {
            _ambientMap = _map;
            ParticleQueue.ClearAmbient();
        }

        int permille = SAOTRPG.Systems.World.BiomeSystem.CurrentGenConfig?.AmbientOverlay?.DensityPermille ?? 0;
        if (permille <= 0) return;

        // Off must return before the floor below, or turning atmosphere off would still show one.
        int budget = AmbientBudget();
        if (budget <= 0) return;

        // A biome that declares a particle shows at least one whenever atmosphere is on at all.
        // The permille scaling truncates, so without this floor the sparser biomes (Grassland,
        // Urban, Dark) would render their declared particle nowhere on the lower density settings.
        int target = Math.Max(1, budget * permille / AmbientPermilleAtFullBudget);
        target = Math.Min(target, ParticleQueue.MaxAmbient);
        int deficit = target - ParticleQueue.AmbientCount;
        if (deficit <= 0) return;

        // Random.Shared is the cosmetic stream, and using it here is load-bearing: ambient spawn
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
    // Parsed once per frame, never per cell: BiomeGenConfig.GetTintColor does string parsing inside
    // a try/catch, which cannot run tens of thousands of times a frame. `_tintA` of 0 means no
    // wash, which is also what a biome declaring no tintColor gets.
    private Map.Generation.BiomeGenConfig? _tintCfg;
    private byte _tintR, _tintG, _tintB;
    private int _tintA;

    // Re-parses only when BiomeSystem hands back a different config object, so the steady state
    // costs one reference compare per frame.
    private void RefreshBiomeTint()
    {
        var cfg = SAOTRPG.Systems.World.BiomeSystem.CurrentGenConfig;
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
    // bg = additive dim glow (cap: light ≤ LightCeiling × BgGlowScale 0.20, i.e. ≈102).
    //
    // The whole visual contract for a lit cell lives in this one method — every visible cell
    // passes through it, in this order: the biome wash, the theme's world transform, the fg light
    // multiply, the bg glow at BgGlowScale, the hit flash, the crit flash and the status tint.
    // The colour quantization is not part of change detection — TileKey.Light holds the raw
    // light triple, so the scan compares unquantized values — it exists only to let neighbouring
    // cells share one terminal colour escape.
    // Overlay colours go through the same world transform the tiles do.
    //
    // Particles, toasts and damage popups paint outside ApplyLighting, so without this they would
    // keep full colour while the world around them is remapped. These are authored colours (a
    // toast accent, a popup's red or green), not role-palette values, so transforming them is
    // exactly what the map's own authored colours get.
    //
    // Under Amber Mono the hue distinction between damage and heal is lost, as it is for every
    // tile; relative brightness survives, and the popup text itself still carries the sign. The
    // HUD's four semantic roles live in the role palette and are untouched by this.
    internal static Color OverlayColor(Color c)
    {
        var theme = ColorSchemes.Theme;
        return theme.TintsWorld ? theme.World(c) : c;
    }

    // The light is the triple the damage tracker already sampled into TileKey, handed down rather
    // than read again: one source per cell means the value compared and the value drawn are the
    // same by construction, and it saves a grid read on every resolved cell.
    private void ApplyLighting(byte band, byte viewEdge, in LightingSystem.LightRgb light,
                               int mx, int my, Color ground,
                               ref Color fg, ref Color bg)
    {
        // Adapt to the sky before anything reads the light, so the foreground multiply and the
        // background glow take the same exposure and cannot drift apart. One compare when off.
        float lightR = light.R, lightG = light.G, lightB = light.B;
        if (s_expGain > 1f) ExposeLight(ref lightR, ref lightG, ref lightB);

        // Two transforms, and the order between them is load-bearing. The biome wash is part of the
        // world's own identity, so it goes on the authored colour first; the theme then transforms
        // the result. Reversed, Amber Mono would collapse the world to amber and then have biome
        // hues painted back over it, breaking the one promise a monochrome theme makes.
        //
        // Foreground only, deliberately. The background is black except where light bleeds into it,
        // and washing that too would paint a coloured ground across exactly the visible region —
        // memory tiles keep a black ground, so the FOV boundary would read as a hard-edged
        // rectangle of colour. Raising BgGlowScale is the intended lever for the background.
        // The wash goes on `ground` too: it is part of the world's identity and the glow is
        // steered toward it below, so leaving it out would give a biome's ground one hue and its
        // own glow another.
        if (_tintA > 0) { fg = BlendBiomeTint(fg); ground = BlendBiomeTint(ground); }

        // The theme's world transform, applied before lighting so a lit tile keeps the same
        // relationship to an unlit one. Every visible cell passes through here, which is why this
        // is the only place the world is recoloured. Gated because Default and Colourblind Safe
        // are the identity transform and this is per-cell in the hot path.
        ColorTheme theme = ColorSchemes.Theme;
        bool themeTints = theme.TintsWorld;
        // `ground` is deliberately not transformed here. It is mixed into the background below,
        // and the background takes the transform once at the end — theming it here as well would
        // apply World() twice to that component, which High Contrast (a lift toward white) would
        // show as a background lifted twice.
        if (themeTints) fg = theme.World(fg);

        // Foreground: multiply base colour by light → warm/cool tint. The light may exceed
        // LightFull inside a source, in which case the product leaves the byte range and
        // SpillOverRange moves the excess onto the channels that still have room.
        float lr = fg.R * lightR * InvLightScale;
        float lg = fg.G * lightG * InvLightScale;
        float lb = fg.B * lightB * InvLightScale;
        // Moonlight: the sky's share of the light goes grey, a source's excess keeps its colour.
        if (s_scot > 0f)
        {
            float kr = MathF.Min(lightR, s_skyR), kg = MathF.Min(lightG, s_skyG), kb = MathF.Min(lightB, s_skyB);
            float y = Luma(fg.R, fg.G, fg.B) * MathF.Max(kr, MathF.Max(kg, kb));
            lr += (y * s_scotR - fg.R * kr) * InvLightScale * s_scot;
            lg += (y * s_scotG - fg.G * kg) * InvLightScale * s_scot;
            lb += (y * s_scotB - fg.B * kb) * InvLightScale * s_scot;
        }
        if (lr > 255f || lg > 255f || lb > 255f) SpillOverRange(ref lr, ref lg, ref lb);
        fg = new Color(Quantize(lr), Quantize(lg), Quantize(lb));

        // Dim colored bg glow — floor picks up light source hue (orange near campfire, cyan near
        // fountain) — shaped by distance from the player and by the edge of what can be seen.
        // Folded into the multiply before the quantizer, so the ladder stays one step of 2 in the
        // final space rather than a quantized value being scaled and re-quantized.
        float glow = BgGlowScale * GlowScale(band, viewEdge) * (1f / 255f);
        float gR = lightR * glow, gG = lightG * glow, gB = lightB * glow;
        TintGlowToGround(ground, ref gR, ref gG, ref gB);
        bg = new Color(Quantize(gR), Quantize(gG), Quantize(gB));
        // The glow is built from light and the ground's hue, so it is transformed once, after
        // both are in it — never per component, which would double-transform the ground half.
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

        // Edge vignette last, so it dims whatever the cell finally resolved to — the crit flash
        // included, which should recede with distance like everything else rather than punching
        // through at full strength on the rim. Foreground only here: the background already took
        // its own, deeper share of the same ramp above, before the hit flash overrode it.
        int vig = VignetteScale(band);
        if (vig < 255)
            fg = new Color((byte)(fg.R * vig / 255), (byte)(fg.G * vig / 255), (byte)(fg.B * vig / 255));
    }

    // ── Past full: where the extra light goes ───────────────────────────────────────
    // Once a channel is at 255 the cell cannot get brighter by scaling, so what happens to the
    // rest of the light decides whether headroom is worth having. Each channel's overflow is
    // split evenly into the other two and the result is clipped: the light is moved to where the
    // cell can still show it rather than discarded, as the screen combine one layer down does,
    // and below the clip the channel sum is conserved exactly.
    //
    // Clipping per channel would flatline a saturated colour — no light of any strength would
    // make a pure-green glyph brighter, and a lava glyph here is (255, 100, 30). Desaturating
    // toward luminance would hold L* exactly but turn a pure-green glyph white under moderate
    // light, since Rec.709 weights green at 0.72.
    //
    // Spill is monotone in the light at every base colour (each output is a non-decreasing
    // combination of non-decreasing terms), so distinct light levels keep producing distinct
    // cells, and it holds hue: green reaches (128, 255, 128) at m = 2.0 rather than white.
    //
    // One pass. A receiving channel that overflows in turn simply clips, which loses a little of
    // the sum at the very brightest cells and is what keeps white out of the picture until the
    // cell is genuinely blown out in all three channels. Iterating to convergence would conserve
    // the sum and reach white sooner.
    //
    // Nothing here can go negative or wrap: the inputs are non-negative and every term added
    // is non-negative, so the only value still outside the byte range is the channel that
    // overflowed in the first place, which Quantize saturates. No divide, no weights, no
    // taste constant.
    //
    // The picture depends on light past full brightening a cell along its own hue and only
    // reaching white when every channel is over. It is a pure function of (base colour, light
    // triple) — both already sampled by the damage tracker — which is what keeps it out of TileKey.
    private static void SpillOverRange(ref float r, ref float g, ref float b)
    {
        float er = r > 255f ? r - 255f : 0f;
        float eg = g > 255f ? g - 255f : 0f;
        float eb = b > 255f ? b - 255f : 0f;
        r += (eg + eb) * 0.5f;
        g += (er + eb) * 0.5f;
        b += (er + eg) * 0.5f;
    }

    // Share of the status colour blended into a lit cell. Covering every cell at full strength
    // would flatten the map to one solid colour, so full coverage only works as a blend. One
    // constant to tune the whole effect.
    private const float StatusTintAlpha = 0.5f;

    // Environmental wash for poison / bleed. Runs only on visible cells (memory tiles keep their
    // own palette) and only while a status is active, so it costs nothing the rest of the time.
    //
    // The tint is scaled by the cell's own luminance before blending, which is what stops it
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
