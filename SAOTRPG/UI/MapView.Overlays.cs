using Terminal.Gui;
using System.Runtime.InteropServices;
using SAOTRPG.Entities;
using SAOTRPG.Map;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// All overlay rendering passes drawn on top of the base tile layer.
public partial class MapView
{
    // Toggled by Shift+G. Renders GameMap.DebugHeights as a grayscale bg layer
    // for visual QA. UI-thread-only; no persistence.
    internal static bool HeightmapDebugEnabled;

    // dtMs threaded in from OnDrawingContent (FrameClock.Tick) so all overlays share one wall-clock dt.
    private void RenderOverlays(int w, int h, int dtMs)
    {
        using var _overlaysScope = Profiler.Begin("MapView.Overlays");
        RenderHeightmapOverlay(w, h);
        // Biome tint overlay disabled — overwrote ASCII glyph contrast. Kept method for potential revival at subtle alpha.
        if (SAOTRPG.Systems.UserSettings.Current.ShowFootsteps) RenderFootstepTrail(w, h);
        RenderBossBar(w);
        RenderCorpseMarkers(w, h, dtMs);
        RenderLootSparkle(w, h);
        RenderDoorFlashes(w, h);
        RenderGasVentParticles(w, h);
        RenderShrineGlow(w, h);
        RenderWeaponSwings(w, h, dtMs);
        if (SAOTRPG.Systems.UserSettings.Current.ShowDamageFlash) RenderDamageFlashes(w, h, dtMs);
        RenderScorchMarks(w, h, dtMs);
        RenderMobTrails(w, h);
        RenderAggroIndicators(w, h);
        // Ambient tile layer (implemented by TileAnimations partial — stub here).
        RenderAmbientTiles(w, h);
        RenderSkillFlashes(w, h, dtMs);
        RenderKillStreakFlash(w, h, dtMs);
        RenderLevelUpFlash(w, h, dtMs);
        // Border flash draws LAST to win the cell over the low-HP background pulse.
        RenderRainOverlay(w, h);
        RenderBossEntrance(w, h);
        RenderLowHpPulse(w, h);
        RenderBorderFlash(w, h, dtMs);
        RenderLookMode(w, h);
        RenderRangedFireMode(w, h);
        TickShake(dtMs);
        // Particles draw BELOW projectiles + popups so later passes overwrite.
        RenderParticles(w, h, dtMs);
        RenderProjectiles(w, h, dtMs);
        RenderDamagePopups(w, h, dtMs);
        RenderToasts(w, h);
        // Divine obtain banner draws LAST — above every other overlay so the
        // 3s celebration is unmissable; auto-dismisses via DivineObtainBanner.Tick.
        RenderDivineObtainBanner(w, h);
    }

    // Non-modal Divine drop banner. Reads static state in DivineObtainBanner;
    // renders a centered gold-bordered frame with ◈ DIVINE OBJECT OBTAINED ◈.
    private void RenderDivineObtainBanner(int w, int h)
    {
        if (!DivineObtainBanner.Tick()) return;
        float alpha = DivineObtainBanner.CurrentAlpha;
        if (alpha <= 0f) return;

        int bw = System.Math.Min(DivineObtainBanner.BannerWidth, System.Math.Max(24, w - 2));
        int bh = System.Math.Min(DivineObtainBanner.BannerHeight, System.Math.Max(3, h - 2));
        int x0 = System.Math.Max(0, (w - bw) / 2);
        int y0 = System.Math.Max(1, h / 3 - bh / 2); // upper-third anchor, above toast row.
        int x1 = x0 + bw - 1, y1 = y0 + bh - 1;
        if (x1 >= w || y1 >= h) return;

        Color goldFaded = ScaleColor(Color.BrightYellow, alpha);
        Color whiteFaded = ScaleColor(Color.White, alpha);
        var borderAttr = Gfx.Attr(goldFaded, Color.Black);
        var headerAttr = Gfx.Attr(goldFaded, Color.Black);
        var nameAttr   = Gfx.Attr(whiteFaded, Color.Black);

        // ── Frame borders (double-box for weight) ────────────────────
        Driver!.SetAttribute(borderAttr);
        Move(x0, y0); Driver!.AddRune(new System.Text.Rune('╔'));
        for (int x = x0 + 1; x < x1; x++) { Move(x, y0); Driver!.AddRune(new System.Text.Rune('═')); }
        Move(x1, y0); Driver!.AddRune(new System.Text.Rune('╗'));
        for (int y = y0 + 1; y < y1; y++)
        {
            Move(x0, y); Driver!.AddRune(new System.Text.Rune('║'));
            for (int x = x0 + 1; x < x1; x++) { Move(x, y); Driver!.AddRune(new System.Text.Rune(' ')); }
            Move(x1, y); Driver!.AddRune(new System.Text.Rune('║'));
        }
        Move(x0, y1); Driver!.AddRune(new System.Text.Rune('╚'));
        for (int x = x0 + 1; x < x1; x++) { Move(x, y1); Driver!.AddRune(new System.Text.Rune('═')); }
        Move(x1, y1); Driver!.AddRune(new System.Text.Rune('╝'));

        // ── Row 1: header banner ─────────────────────────────────────
        // Bundle 9: awakening variant swaps header + flavor; obtain variant unchanged.
        bool awakening = DivineObtainBanner.IsAwakening;
        string header = awakening ? "◈ DIVINE AWAKENED ◈" : "◈ DIVINE OBJECT OBTAINED ◈";
        int hx = x0 + (bw - header.Length) / 2;
        int hy = y0 + 1;
        DrawTextAtView(hx, hy, header, headerAttr, w, h);

        // ── Row 2: weapon name, accent-styled ────────────────────────
        if (bh >= 4)
        {
            string name = DivineObtainBanner.WeaponName;
            if (name.Length > bw - 4) name = name.Substring(0, bw - 4);
            int nx = x0 + (bw - name.Length) / 2;
            int ny = y0 + 2;
            DrawTextAtView(nx, ny, name, nameAttr, w, h);
        }

        // ── Row 3: flavor, dimmed ────────────────────────────────────
        if (bh >= 5)
        {
            string flavor = awakening
                ? $"The blade unfolds further  ◈{DivineObtainBanner.AwakeningLevel}"
                : "A legendary blade chooses its wielder.";
            if (flavor.Length > bw - 4) flavor = flavor.Substring(0, bw - 4);
            int fx = x0 + (bw - flavor.Length) / 2;
            int fy = y0 + 3;
            DrawTextAtView(fx, fy, flavor, Gfx.Attr(ScaleColor(Color.Gray, alpha), Color.Black), w, h);
        }
    }

    // Shift+G debug: grayscale heightmap over tile layer. Reads DebugHeights
    // populated by HeightmapPass.
    private void RenderHeightmapOverlay(int w, int h)
    {
        if (!HeightmapDebugEnabled) return;
        var heights = _map.DebugHeights;
        if (heights == null) return;
        int hw = heights.GetLength(0), hh = heights.GetLength(1);
        for (int vy = 0; vy < h; vy++)
        for (int vx = 0; vx < w; vx++)
        {
            int mx = VxToMap(vx), my = VyToMap(vy);
            if (mx < 0 || my < 0 || mx >= hw || my >= hh) continue;
            float hval = heights[mx, my];
            Color c = hval switch
            {
                < 0.2f => Color.Black,
                < 0.4f => Color.DarkGray,
                < 0.6f => Color.Gray,
                < 0.8f => Color.White,
                _      => Color.BrightYellow,
            };
            DrawGlyph_View(vx, vy, '·', Gfx.Attr(c, Color.Black));
        }
    }

    // Biome tint wash: per-tile RGB alpha blend bg_new = bg_old*(1-a) + tint*a
    // so the base palette reads through at the configured alpha strength.
    private void RenderTintOverlay(int w, int h)
    {
        var cfg = Systems.BiomeSystem.CurrentGenConfig;
        if (cfg == null) return;
        var tint = cfg.GetTintColor(out byte alpha);
        if (tint == null || alpha == 0) return;
        float a = alpha / 255f;
        float ia = 1f - a;
        byte tr = tint.Value.R, tg = tint.Value.G, tb = tint.Value.B;
        for (int vy = 0; vy < h; vy++)
        for (int vx = 0; vx < w; vx++)
        {
            int mx = VxToMap(vx), my = VyToMap(vy);
            if (!_map.InBounds(mx, my) || !_map.IsVisible(mx, my)) continue;
            var tile = _map.GetTile(mx, my);
            var (glyph, fg, bg) = _visualCache.Get(mx, my, tile.Type);
            var blended = new Color(
                (byte)(bg.R * ia + tr * a),
                (byte)(bg.G * ia + tg * a),
                (byte)(bg.B * ia + tb * a));
            DrawGlyph_View(vx, vy, glyph, Gfx.Attr(fg, blended));
        }
    }

    private void RenderFootstepTrail(int w, int h)
    {
        var settings = SAOTRPG.Systems.UserSettings.Current;
        if (settings.FootstepStyle == SAOTRPG.Systems.FootstepStyle.Off) return;
        if (settings.FootstepLength <= 0) return;

        char glyph = settings.FootstepStyle switch
        {
            SAOTRPG.Systems.FootstepStyle.Dots       => '·',
            SAOTRPG.Systems.FootstepStyle.Dashes     => '-',
            SAOTRPG.Systems.FootstepStyle.Paws       => '"',
            SAOTRPG.Systems.FootstepStyle.Bootprints => ':',
            SAOTRPG.Systems.FootstepStyle.Chevrons   => '^',
            _                                         => '·',
        };
        Color color = settings.FootstepOpacity switch
        {
            SAOTRPG.Systems.FootstepOpacity.Subtle => Color.DarkGray,
            SAOTRPG.Systems.FootstepOpacity.Medium => Color.Gray,
            SAOTRPG.Systems.FootstepOpacity.Bold   => Color.White,
            _                                       => Color.DarkGray,
        };
        var attr = Gfx.Attr(color, Color.Black);
        foreach (var (fx, fy) in _footsteps)
        {
            if (!_map.InBounds(fx, fy) || !_map.IsVisible(fx, fy)) continue;
            var tile = _map.GetTile(fx, fy);
            if (tile.Occupant != null || _map.HasItemsAt(fx, fy) || !tile.IsWalkable) continue;
            DrawGlyph(fx, fy, glyph, attr, w, h);
        }
    }

    private void RenderBossBar(int w)
    {
        Boss? boss = null;
        foreach (var b in _map.Bosses)
            if (!b.IsDefeated && _map.IsVisible(b.X, b.Y)) { boss = b; break; }
        if (boss == null) return;

        // First-sight detection: each boss Id triggers one entrance banner.
        if (_bossesSeen.Add(boss.Id))
        {
            TriggerBossEntrance(boss.Name);
            FlashBorder(Color.BrightRed, 100);
        }

        const int barWidth = 20;
        // Wave 2 — tweened HP for smooth bar drift on boss damage.
        int displayedHp = GetDisplayedMonsterHp(boss.Id, boss.CurrentHealth);
        string hpBar = BarBuilder.BuildGradient(displayedHp, boss.MaxHealth, barWidth);
        string label = $" {boss.Name} {hpBar} {boss.CurrentHealth}/{boss.MaxHealth} ";
        DrawCenteredBanner(label, row: 0, Gfx.Attr(Color.BrightRed, Color.DarkGray), w);
    }

    // Draws a centered text banner on the given row (viewport-space).
    private void DrawCenteredBanner(string text, int row, Terminal.Gui.Attribute attr, int w)
    {
        int startX = Math.Max(0, (w - text.Length) / 2);
        for (int i = 0; i < text.Length && startX + i < w; i++)
        {
            Driver!.SetAttribute(attr); Move(startX + i, row);
            Driver!.AddRune(new System.Text.Rune(text[i]));
        }
    }

    // Draws the same glyph pair across the top (row 0) and bottom (row h-1) edges.
    private void DrawEdgeBar(char topGlyph, char bottomGlyph, Terminal.Gui.Attribute attr, int w, int h)
    {
        for (int x = 0; x < w; x++)
        {
            Driver!.SetAttribute(attr); Move(x, 0);
            Driver!.AddRune(new System.Text.Rune(topGlyph));
            Move(x, h - 1);
            Driver!.AddRune(new System.Text.Rune(bottomGlyph));
        }
    }

    private void RenderDamageFlashes(int w, int h, int dtMs)
    {
        var span = CollectionsMarshal.AsSpan(_damageFlashes);
        for (int i = span.Length - 1; i >= 0; i--)
        {
            ref var f = ref span[i];
            // Visible-only — prevents shroud leak after the player walks out of sight.
            if (_map.InBounds(f.X, f.Y) && _map.IsVisible(f.X, f.Y))
            {
                int vx = MapToVx(f.X), vy = MapToVy(f.Y - 1);
                if (vy < 0) vy = MapToVy(f.Y);
                if (f.IsCrit && f.RemainingMs <= CritDamageFlashMs - 66) vy = Math.Max(0, vy - 1);
                DrawTextAtView(vx, vy, f.Text, Gfx.Attr(f.Color, Color.Black), w, h);
            }
            if (f.RemainingMs <= dtMs) _damageFlashes.RemoveAt(i);
            else f.RemainingMs -= dtMs;
        }
    }

    private void RenderCorpseMarkers(int w, int h, int dtMs)
    {
        for (int i = _corpseMarkers.Count - 1; i >= 0; i--)
        {
            var (cx, cy, remainingMs) = _corpseMarkers[i];
            if (!_map.InBounds(cx, cy) || !_map.IsVisible(cx, cy)) continue;
            var tile = _map.GetTile(cx, cy);
            if (tile.Occupant != null || _map.HasItemsAt(cx, cy)) continue;
            double life = (double)remainingMs / CorpseMarkerMs;
            Color c = life > 0.66 ? Color.Red : life > 0.33 ? Color.Gray : Color.DarkGray;
            DrawGlyph(cx, cy, '†', Gfx.Attr(c, Color.Black), w, h);
            if (remainingMs <= dtMs) _corpseMarkers.RemoveAt(i);
            else _corpseMarkers[i] = (cx, cy, remainingMs - dtMs);
        }
    }

    private void RenderLootSparkle(int w, int h)
    {
        var attr = Gfx.Attr(Color.White, Color.Black);
        foreach (var (mx, my) in _map.ItemTiles)
        {
            if (!MapInView(mx, my, w, h)) continue;
            if (!_map.IsVisible(mx, my)) continue;
            var tile = _map.GetTile(mx, my);
            if (!_map.HasItemsAt(mx, my) || tile.Occupant != null) continue;
            var (glyph, _) = GetItemRarityVisual(_map.GetItemsAt(mx, my));
            DrawGlyph(mx, my, glyph, attr, w, h);
        }
    }

    private void RenderScorchMarks(int w, int h, int dtMs)
    {
        for (int i = _scorchMarks.Count - 1; i >= 0; i--)
        {
            var (sx, sy, remainingMs) = _scorchMarks[i];
            if (!_map.InBounds(sx, sy) || !_map.IsVisible(sx, sy)) continue;
            if (_map.GetTile(sx, sy).Occupant != null) continue;
            Color c = remainingMs > ScorchMarkMs / 2 ? Color.Red : Color.DarkGray;
            DrawGlyph(sx, sy, '░', Gfx.Attr(c, Color.Black), w, h);
            if (remainingMs <= dtMs) _scorchMarks.RemoveAt(i);
            else _scorchMarks[i] = (sx, sy, remainingMs - dtMs);
        }
    }

    private void RenderMobTrails(int w, int h)
    {
        var attr = Gfx.Attr(Color.DarkGray, Color.Black);
        foreach (var (_, trail) in _mobTrails)
        foreach (var (tx, ty) in trail)
        {
            if (!MapInView(tx, ty, w, h)) continue;
            if (!_map.InBounds(tx, ty) || !_map.IsVisible(tx, ty)) continue;
            var tile = _map.GetTile(tx, ty);
            if (tile.Occupant != null || _map.HasItemsAt(tx, ty) || !tile.IsWalkable) continue;
            DrawGlyph(tx, ty, '·', attr, w, h);
        }
    }

    private void RenderAggroIndicators(int w, int h)
    {
        foreach (var monster in _map.Monsters)
        {
            if (monster.IsDefeated) continue;
            // Viewport-rect early-out: skip the visible+manhattan check for off-screen mobs.
            if (!MapInView(monster.X, monster.Y, w, h)) continue;
            if (!MapEffects.ShouldShowAggroIndicator(_map, monster.X, monster.Y, _player.X, _player.Y)) continue;
            Color c = Color.BrightRed;
            DrawGlyph(monster.X, monster.Y - 1, '!',
                Gfx.Attr(c, Color.Black), w, h);
        }
    }

    // Sword skill activation -- expanding ring of colored light.
    // ringStep grows from 1 → SkillFlashMs/StepMs as the flash ages.
    private void RenderSkillFlashes(int w, int h, int dtMs)
    {
        for (int i = _skillFlashes.Count - 1; i >= 0; i--)
        {
            var (cx, cy, remainingMs, totalMs, color) = _skillFlashes[i];
            int step = (totalMs - remainingMs) / SkillFlashStepMs;
            int ring = step + 1;

            for (int dx = -ring; dx <= ring; dx++)
            for (int dy = -ring; dy <= ring; dy++)
            {
                int cheb = Math.Max(Math.Abs(dx), Math.Abs(dy));
                if (cheb != ring) continue;
                int px = cx + dx, py = cy + dy;
                if (!_map.InBounds(px, py) || !_map.IsVisible(px, py)) continue;

                char glyph = (dx, dy) switch
                {
                    (0, _) => '|', (_, 0) => '-',
                    _ when dx == dy => '\\', _ => '/',
                };
                Color c = remainingMs >= totalMs / 3 ? color : Color.DarkGray;
                DrawGlyph(px, py, glyph, Gfx.Attr(c, Color.Black), w, h);
            }

            if (remainingMs <= dtMs) _skillFlashes.RemoveAt(i);
            else _skillFlashes[i] = (cx, cy, remainingMs - dtMs, totalMs, color);
        }
    }

    private void RenderKillStreakFlash(int w, int h, int dtMs)
    {
        if (_killStreakFlashRemainingMs <= 0) return;
        _killStreakFlashRemainingMs = Math.Max(0, _killStreakFlashRemainingMs - dtMs);

        // Starburst around player for streak ≥ 3
        if (_killStreakLevel >= 3)
        {
            var starAttr = Gfx.Attr(_killStreakColor, Color.Black);
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            { if (dx == 0 && dy == 0) continue;
              DrawGlyph(_player.X + dx, _player.Y + dy, '*', starAttr, w, h); }
        }

        // Centered tier text banner — color pulses on a 66ms cadence
        if (string.IsNullOrEmpty(_killStreakText)) return;
        Color bannerColor = ((_killStreakFlashRemainingMs / 66) & 1) == 0 ? _killStreakColor : Color.White;
        string banner = $"  {_killStreakText}  x{_killStreakLevel}  ";
        DrawCenteredBanner(banner, Math.Max(1, h / 3), Gfx.Attr(bannerColor, Color.Black), w);
    }

    private void RenderLevelUpFlash(int w, int h, int dtMs)
    {
        if (_levelUpFlashRemainingMs <= 0) return;
        _levelUpFlashRemainingMs = Math.Max(0, _levelUpFlashRemainingMs - dtMs);
        DrawCenteredBanner("* LEVEL UP! *", h / 2, Gfx.Attr(Color.BrightYellow, Color.Black), w);
    }

    private void RenderBorderFlash(int w, int h, int dtMs)
    {
        if (_borderFlashRemainingMs <= 0) return;
        _borderFlashRemainingMs = Math.Max(0, _borderFlashRemainingMs - dtMs);
        // Top and bottom rows only — left/right would interfere with map edges too much
        DrawEdgeBar('▁', '▔', Gfx.Attr(_borderFlashColor, Color.Black), w, h);
    }

    private void RenderWeaponSwings(int w, int h, int dtMs)
    {
        for (int i = _weaponSwings.Count - 1; i >= 0; i--)
        {
            var (fx, fy, tx, ty, color, remainingMs) = _weaponSwings[i];
            int dx = tx - fx, dy = ty - fy;

            char glyph = (dx, dy) switch
            {
                (0, -1) or (0, 1) => '|',
                (-1, 0) or (1, 0) => '-',
                (> 0, > 0) or (< 0, < 0) => '\\',
                _ => '/',
            };

            int mx = tx, my = ty;
            if (_map.InBounds(mx, my) && _map.IsVisible(mx, my))
            {
                DrawGlyph(mx, my, glyph, Gfx.Attr(color, Color.Black), w, h);

                // First half of life: trail behind the slash for heavier feel.
                if (remainingMs >= WeaponSwingMs / 2)
                {
                    int trailX = mx - Math.Sign(dx), trailY = my - Math.Sign(dy);
                    if (_map.InBounds(trailX, trailY) && _map.IsVisible(trailX, trailY))
                        DrawGlyph(trailX, trailY, '~', Gfx.Attr(Color.DarkGray, Color.Black), w, h);
                }
            }
            if (remainingMs <= dtMs) _weaponSwings.RemoveAt(i);
            else _weaponSwings[i] = (fx, fy, tx, ty, color, remainingMs - dtMs);
        }
    }

    // Cyan apostrophes scattered randomly — gives Rain weather a visible overlay.
    // Fog removed (visual noise vs ASCII map).
    private void RenderRainOverlay(int w, int h)
    {
        if (Systems.WeatherSystem.Current != Systems.WeatherType.Rain) return;
        var attr = Gfx.Attr(Color.Cyan, Color.Black);
        var rng = Random.Shared;
        for (int i = 0; i < 8; i++)
        {
            int vx = rng.Next(0, w), vy = rng.Next(0, h);
            int mx = VxToMap(vx), my = VyToMap(vy);
            if (!_map.InBounds(mx, my) || !_map.IsVisible(mx, my)) continue;
            var tile = _map.GetTile(mx, my);
            if (tile.Occupant != null) continue;
            Driver!.SetAttribute(attr); Move(vx, vy);
            Driver!.AddRune(new System.Text.Rune('\''));
        }
    }

    // Brief yellow | flash on a door when first opened.
    private void RenderDoorFlashes(int w, int h)
    {
        var brightAttr = Gfx.Attr(Color.BrightYellow, Color.Black);
        foreach (var (dx, dy, remainingMs) in _doorFlashes)
        {
            if (!_map.InBounds(dx, dy) || !_map.IsVisible(dx, dy)) continue;
            char glyph = remainingMs >= DoorFlashMs / 2 ? '|' : '·';
            DrawGlyph(dx, dy, glyph, brightAttr, w, h);
        }
    }

    // Wisps of green gas puffing up from GasVent tiles. Position-hashed +
    // wall-clock so emission is deterministic per tile per real-time tick.
    private void RenderGasVentParticles(int w, int h)
    {
        var attr = Gfx.Attr(new Color(120, 255, 120), Color.Black);
        int turn = (int)(SAOTRPG.Systems.FrameClock.ElapsedMs / 500);
        var vents = _map.GasVents;
        for (int i = 0; i < vents.Count; i++)
        {
            var (mx, my) = vents[i];
            if (!MapInView(mx, my, w, h)) continue;
            if (!_map.IsVisible(mx, my)) continue;

            int hash = (mx * 73856093 ^ my * 19349663 ^ turn * 83492791) & 0x7FFFFFFF;
            if (hash % 8 != 0) continue;
            int lift = 1 + (hash >> 3) % 2;
            int py = my - lift;
            if (!_map.InBounds(mx, py) || !_map.IsVisible(mx, py)) continue;
            if (_map.GetTile(mx, py).Occupant != null) continue;
            DrawGlyph(mx, py, '·', attr, w, h);
        }
    }

    // Shrines and fountains pulse a sparkle on adjacent floor tiles, giving
    // the colored ground light a breathing feel without changing radius.
    private void RenderShrineGlow(int w, int h)
    {
        int turn = (int)(SAOTRPG.Systems.FrameClock.ElapsedMs / 500);
        int phase = turn % 8;
        if (phase > 1) return; // Sparkle for 1 of every 4 seconds.

        var shrines = _map.Shrines;
        for (int i = 0; i < shrines.Count; i++)
        {
            var (mx, my) = shrines[i];
            if (!MapInView(mx, my, w, h)) continue;
            if (!_map.IsVisible(mx, my)) continue;
            var type = _map.GetTile(mx, my).Type;
            bool fountain = type == SAOTRPG.Map.TileType.Fountain;

            int hash = (mx * 374761393 ^ my * 668265263) & 0x7FFFFFFF;
            int ox = ((hash & 3) - 1);
            int oy = ((hash >> 2) & 3) - 1;
            if (ox == 0 && oy == 0) oy = 1;
            int px = mx + ox, py = my + oy;
            if (!_map.InBounds(px, py) || !_map.IsVisible(px, py)) continue;
            if (_map.GetTile(px, py).Occupant != null) continue;

            Color c = fountain ? new Color(140, 220, 255)
                    : type == SAOTRPG.Map.TileType.EnchantShrine ? new Color(255, 220, 100)
                    : new Color(220, 160, 255);
            DrawGlyph(px, py, '·', Gfx.Attr(c, Color.Black), w, h);
        }
    }

    // Viewport-space glyph draw (no map coords needed).
    private void DrawGlyph_View(int vx, int vy, char glyph, Terminal.Gui.Attribute attr)
    {
        Driver!.SetAttribute(attr); Move(vx, vy);
        Driver!.AddRune(new System.Text.Rune(glyph));
    }

    // Centered banner announcing a newly-sighted boss. Renders a portrait above
    // the banner when the boss name matches a registered AsciiPortraits entry.
    private void RenderBossEntrance(int w, int h)
    {
        if (_bossEntranceRemainingMs <= 0 || string.IsNullOrEmpty(_bossEntranceName)) return;
        // Color flips at ~100ms cadence so the banner pulses without flicker on dt jitter.
        bool bright = ((_bossEntranceRemainingMs / 100) & 1) == 0;
        Color c = bright ? Color.BrightRed : Color.BrightYellow;
        string? key = AsciiPortraits.KeyForName(_bossEntranceName);
        int baseRow = Math.Max(2, h / 2 - 2);
        if (key != null)
        {
            var portrait = AsciiPortraits.Get(key);
            int portraitH = portrait.Length;
            int startCol = Math.Max(0, (w - 8) / 2);
            int portraitRow = Math.Max(1, baseRow - portraitH - 1);
            Color pc = key == "fatal_scythe" ? Color.DarkGray : c;
            for (int i = 0; i < portraitH; i++)
                DrawTextAtView(startCol, portraitRow + i, portrait[i], Gfx.Attr(pc, Color.Black), w, h);
        }
        DrawCenteredBanner(_bossEntranceBanner, baseRow, Gfx.Attr(c, Color.Black), w);
    }

    private void RenderLowHpPulse(int w, int h)
    {
        if (_player.IsDefeated || _player.CurrentHealth <= 0) return;
        if (_player.CurrentHealth > _player.MaxHealth / 4) return;
        // Steady red border when HP is at or below 25% — no flash.
        DrawEdgeBar('▁', '▔', Gfx.Attr(Color.BrightRed, Color.Black), w, h);
    }
}
