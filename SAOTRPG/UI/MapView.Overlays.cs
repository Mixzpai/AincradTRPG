using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Map;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// All overlay rendering passes drawn on top of the base tile layer.
public partial class MapView
{
    private void RenderOverlays(int w, int h)
    {
        if (SAOTRPG.Systems.UserSettings.Current.ShowFootsteps) RenderFootstepTrail(w, h);
        RenderBossBar(w);
        RenderCorpseMarkers(w, h);
        RenderLootSparkle(w, h);
        RenderDoorFlashes(w, h);
        RenderGasVentParticles(w, h);
        RenderShrineGlow(w, h);
        RenderNightStars(w, h);
        RenderWeaponSwings(w, h);
        if (SAOTRPG.Systems.UserSettings.Current.ShowDamageFlash) RenderDamageFlashes(w, h);
        RenderScorchMarks(w, h);
        RenderMobTrails(w, h);
        RenderAggroIndicators(w, h);
        RenderShatterParticles(w, h);
        RenderSkillFlashes(w, h);
        RenderKillStreakFlash(w, h);
        RenderLevelUpFlash(w, h);
        // Low-HP pulse is the background state; border flash is momentary
        // and higher-priority, so it must draw LAST to win the cell.
        RenderWeatherOverlay(w, h);
        RenderAllyIndicators(w, h);
        RenderBossEntrance(w, h);
        RenderLowHpPulse(w, h);
        RenderBorderFlash(w, h);
        RenderLookMode(w, h);
    }

    private void RenderFootstepTrail(int w, int h)
    {
        var attr = Gfx.Attr(Color.DarkGray, Color.Black);
        foreach (var (fx, fy) in _footsteps)
        {
            if (!_map.InBounds(fx, fy) || !_map.IsVisible(fx, fy)) continue;
            var tile = _map.GetTile(fx, fy);
            if (tile.Occupant != null || tile.HasItems || !tile.IsWalkable) continue;
            DrawGlyph(fx, fy, '·', attr, w, h);
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
            FlashBorder(Color.BrightRed, 3);
        }

        const int barWidth = 20;
        string hpBar = BarBuilder.BuildGradient(boss.CurrentHealth, boss.MaxHealth, barWidth);
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

    private void RenderDamageFlashes(int w, int h)
    {
        for (int i = _damageFlashes.Count - 1; i >= 0; i--)
        {
            var (fx, fy, text, color, framesLeft, isCrit) = _damageFlashes[i];
            // Only draw if the tile is still visible — prevents flashes from
            // leaking through shroud after the player walks out of sight.
            if (_map.InBounds(fx, fy) && _map.IsVisible(fx, fy))
            {
                int vx = MapToVx(fx), vy = MapToVy(fy - 1);
                if (vy < 0) vy = MapToVy(fy);
                if (isCrit && framesLeft <= CritDamageFlashFrames - 2) vy = Math.Max(0, vy - 1);
                DrawTextAtView(vx, vy, text, Gfx.Attr(color, Color.Black), w, h);
            }
            if (framesLeft <= 1) _damageFlashes.RemoveAt(i);
            else _damageFlashes[i] = (fx, fy, text, color, framesLeft - 1, isCrit);
        }
    }

    private void RenderCorpseMarkers(int w, int h)
    {
        for (int i = _corpseMarkers.Count - 1; i >= 0; i--)
        {
            var (cx, cy, framesLeft) = _corpseMarkers[i];
            if (!_map.InBounds(cx, cy) || !_map.IsVisible(cx, cy)) continue;
            var tile = _map.GetTile(cx, cy);
            if (tile.Occupant != null || tile.HasItems) continue;
            double life = (double)framesLeft / CorpseMarkerFrames;
            Color c = life > 0.66 ? Color.Red : life > 0.33 ? Color.Gray : Color.DarkGray;
            DrawGlyph(cx, cy, '†', Gfx.Attr(c, Color.Black), w, h);
            if (framesLeft <= 1) _corpseMarkers.RemoveAt(i);
            else _corpseMarkers[i] = (cx, cy, framesLeft - 1);
        }
    }

    private void RenderLootSparkle(int w, int h)
    {
        var attr = Gfx.Attr(Color.White, Color.Black);
        for (int vy = 0; vy < h; vy++)
        for (int vx = 0; vx < w; vx++)
        {
            int mx = VxToMap(vx), my = VyToMap(vy);
            if (!_map.InBounds(mx, my) || !_map.IsVisible(mx, my)) continue;
            var tile = _map.GetTile(mx, my);
            if (!tile.HasItems || tile.Occupant != null) continue;
            var (glyph, _) = GetItemRarityVisual(tile.Items);
            DrawGlyph(mx, my, glyph, attr, w, h);
        }
    }

    private void RenderScorchMarks(int w, int h)
    {
        for (int i = _scorchMarks.Count - 1; i >= 0; i--)
        {
            var (sx, sy, framesLeft) = _scorchMarks[i];
            if (!_map.InBounds(sx, sy) || !_map.IsVisible(sx, sy)) continue;
            if (_map.GetTile(sx, sy).Occupant != null) continue;
            Color c = framesLeft > ScorchMarkFrames / 2 ? Color.Red : Color.DarkGray;
            DrawGlyph(sx, sy, '░', Gfx.Attr(c, Color.Black), w, h);
            if (framesLeft <= 1) _scorchMarks.RemoveAt(i);
            else _scorchMarks[i] = (sx, sy, framesLeft - 1);
        }
    }

    private void RenderMobTrails(int w, int h)
    {
        var attr = Gfx.Attr(Color.DarkGray, Color.Black);
        foreach (var (_, trail) in _mobTrails)
        foreach (var (tx, ty) in trail)
        {
            if (!_map.InBounds(tx, ty) || !_map.IsVisible(tx, ty)) continue;
            var tile = _map.GetTile(tx, ty);
            if (tile.Occupant != null || tile.HasItems || !tile.IsWalkable) continue;
            DrawGlyph(tx, ty, '·', attr, w, h);
        }
    }

    private void RenderAggroIndicators(int w, int h)
    {
        foreach (var monster in _map.Monsters)
        {
            if (monster.IsDefeated) continue;
            if (!MapEffects.ShouldShowAggroIndicator(_map, monster.X, monster.Y, _player.X, _player.Y)) continue;
            Color c = Color.BrightRed;
            DrawGlyph(monster.X, monster.Y - 1, '!',
                Gfx.Attr(c, Color.Black), w, h);
        }
    }

    // SAO polygon dissolution -- fragments scatter outward in expanding rings,
    // cycling through polygon-like glyphs with cyan-to-dark color fade.
    private void RenderShatterParticles(int w, int h)
    {
        for (int i = _polyBursts.Count - 1; i >= 0; i--)
        {
            var burst = _polyBursts[i];
            int elapsed = (burst.IsBoss ? PolyBurstFrames + 3 : PolyBurstFrames) - burst.Frame;

            // Frame 0: bright white flash at kill point
            if (elapsed == 0)
            {
                DrawGlyph(burst.X, burst.Y, '*', Gfx.Attr(Color.White, Color.Black), w, h);
            }

            // Frames 1+: fragments scatter outward
            foreach (var (dx, dy, speed, gi) in PolyFragments)
            {
                int dist = elapsed * speed / 2;
                if (dist < 1) continue;
                int px = burst.X + dx * dist / speed;
                int py = burst.Y + dy * dist / speed;
                if (!_map.InBounds(px, py) || !_map.IsVisible(px, py)) continue;

                // Color fade: bright cyan -> cyan -> blue -> dark gray
                float life = (float)burst.Frame / (burst.IsBoss ? PolyBurstFrames + 3 : PolyBurstFrames);
                Color c = life > 0.7f ? Color.BrightCyan
                        : life > 0.4f ? Color.Cyan
                        : life > 0.2f ? Color.Blue
                        : Color.DarkGray;

                // Boss kills use brighter colors
                if (burst.IsBoss && life > 0.5f) c = Color.White;

                char glyph = PolyGlyphs[(gi + elapsed) % PolyGlyphs.Length];
                DrawGlyph(px, py, glyph, Gfx.Attr(c, Color.Black), w, h);
            }

            if (burst.Frame <= 1) _polyBursts.RemoveAt(i);
            else _polyBursts[i] = burst with { Frame = burst.Frame - 1 };
        }
    }

    // Sword skill activation -- expanding ring of colored light
    private void RenderSkillFlashes(int w, int h)
    {
        for (int i = _skillFlashes.Count - 1; i >= 0; i--)
        {
            var (cx, cy, frame, color) = _skillFlashes[i];
            int ring = SkillFlashFrames - frame + 1;

            // Draw a ring at the current expansion radius
            for (int dx = -ring; dx <= ring; dx++)
            for (int dy = -ring; dy <= ring; dy++)
            {
                // Only draw the ring outline, not the filled circle
                int cheb = Math.Max(Math.Abs(dx), Math.Abs(dy));
                if (cheb != ring) continue;
                int px = cx + dx, py = cy + dy;
                if (!_map.InBounds(px, py) || !_map.IsVisible(px, py)) continue;

                char glyph = (dx, dy) switch
                {
                    (0, _) => '|', (_, 0) => '-',
                    _ when dx == dy => '\\', _ => '/',
                };
                Color c = frame >= 2 ? color : Color.DarkGray;
                DrawGlyph(px, py, glyph, Gfx.Attr(c, Color.Black), w, h);
            }

            if (frame <= 1) _skillFlashes.RemoveAt(i);
            else _skillFlashes[i] = (cx, cy, frame - 1, color);
        }
    }

    private void RenderKillStreakFlash(int w, int h)
    {
        if (_killStreakFlashFrames <= 0) return;
        _killStreakFlashFrames--;

        // Starburst around player for streak ≥ 3
        if (_killStreakLevel >= 3)
        {
            var starAttr = Gfx.Attr(_killStreakColor, Color.Black);
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            { if (dx == 0 && dy == 0) continue;
              DrawGlyph(_player.X + dx, _player.Y + dy, '*', starAttr, w, h); }
        }

        // Centered tier text banner — pulses every other frame
        if (string.IsNullOrEmpty(_killStreakText)) return;
        Color bannerColor = (_killStreakFlashFrames % 2 == 0) ? _killStreakColor : Color.White;
        string banner = $"  {_killStreakText}  x{_killStreakLevel}  ";
        DrawCenteredBanner(banner, Math.Max(1, h / 3), Gfx.Attr(bannerColor, Color.Black), w);
    }

    private void RenderLevelUpFlash(int w, int h)
    {
        if (_levelUpFlashFrames <= 0) return;
        _levelUpFlashFrames--;
        DrawCenteredBanner("* LEVEL UP! *", h / 2, Gfx.Attr(Color.BrightYellow, Color.Black), w);
    }

    private void RenderBorderFlash(int w, int h)
    {
        if (_borderFlashFrames <= 0) return;
        _borderFlashFrames--;
        // Top and bottom rows only — left/right would interfere with map edges too much
        DrawEdgeBar('▁', '▔', Gfx.Attr(_borderFlashColor, Color.Black), w, h);
    }

    private void RenderWeaponSwings(int w, int h)
    {
        for (int i = _weaponSwings.Count - 1; i >= 0; i--)
        {
            var (fx, fy, tx, ty, color, framesLeft) = _weaponSwings[i];
            int dx = tx - fx, dy = ty - fy;

            // Directional slash glyph based on attack angle
            char glyph = (dx, dy) switch
            {
                (0, -1) or (0, 1) => '|',   // vertical
                (-1, 0) or (1, 0) => '-',   // horizontal
                (> 0, > 0) or (< 0, < 0) => '\\',
                _ => '/',
            };

            // Draw slash at the target position
            int mx = tx, my = ty;
            if (_map.InBounds(mx, my) && _map.IsVisible(mx, my))
            {
                DrawGlyph(mx, my, glyph, Gfx.Attr(color, Color.Black), w, h);

                // Frame 1: draw a trail behind the slash for heavier feel
                if (framesLeft >= 2)
                {
                    int trailX = mx - Math.Sign(dx), trailY = my - Math.Sign(dy);
                    if (_map.InBounds(trailX, trailY) && _map.IsVisible(trailX, trailY))
                        DrawGlyph(trailX, trailY, '~', Gfx.Attr(Color.DarkGray, Color.Black), w, h);
                }
            }
            if (framesLeft <= 1) _weaponSwings.RemoveAt(i);
            else _weaponSwings[i] = (fx, fy, tx, ty, color, framesLeft - 1);
        }
    }

    // Sparse weather particles for Rain and Fog.
    private void RenderWeatherOverlay(int w, int h)
    {
        var weather = Systems.WeatherSystem.Current;
        if (weather == Systems.WeatherType.Clear || weather == Systems.WeatherType.Wind) return;

        int count = weather == Systems.WeatherType.Rain ? 8 : 5;
        char glyph = weather == Systems.WeatherType.Rain ? '\'' : '.';
        Color c = weather == Systems.WeatherType.Rain ? Color.Cyan : Color.DarkGray;
        var attr = Gfx.Attr(c, Color.Black);

        // Rain/fog is decorative noise — use the shared RNG to avoid a
        // per-frame allocation. Particle count and style are unchanged;
        // only the scatter pattern no longer reseeds from turn count.
        var rng = Random.Shared;
        for (int i = 0; i < count; i++)
        {
            int vx = rng.Next(0, w), vy = rng.Next(0, h);
            int mx = VxToMap(vx), my = VyToMap(vy);
            if (!_map.InBounds(mx, my) || !_map.IsVisible(mx, my)) continue;
            var tile = _map.GetTile(mx, my);
            if (tile.Occupant != null) continue;
            Driver!.SetAttribute(attr); Move(vx, vy);
            Driver!.AddRune(new System.Text.Rune(glyph));
        }
    }

    // Green dot under allies to distinguish from NPCs/enemies.
    private void RenderAllyIndicators(int w, int h)
    {
        var attr = Gfx.Attr(Color.BrightGreen, Color.Black);
        foreach (var ally in Systems.PartySystem.Members)
        {
            if (ally.IsDefeated) continue;
            if (!_map.InBounds(ally.X, ally.Y) || !_map.IsVisible(ally.X, ally.Y)) continue;
            // Draw a small dot below the ally (y+1)
            DrawGlyph(ally.X, ally.Y + 1, '.', attr, w, h);
        }
    }

    // Brief yellow | flash on a door when first opened.
    private void RenderDoorFlashes(int w, int h)
    {
        var brightAttr = Gfx.Attr(Color.BrightYellow, Color.Black);
        foreach (var (dx, dy, frames) in _doorFlashes)
        {
            if (!_map.InBounds(dx, dy) || !_map.IsVisible(dx, dy)) continue;
            char glyph = frames >= 2 ? '|' : '·';
            DrawGlyph(dx, dy, glyph, brightAttr, w, h);
        }
    }

    // Wisps of green gas puffing up from GasVent tiles. Position-hashed +
    // turn-based so the emission is deterministic per tile per turn.
    private void RenderGasVentParticles(int w, int h)
    {
        var attr = Gfx.Attr(new Color(120, 255, 120), Color.Black);
        int turn = SAOTRPG.Map.DayNightCycle.CurrentTurn;
        for (int vy = 0; vy < h; vy++)
        for (int vx = 0; vx < w; vx++)
        {
            int mx = VxToMap(vx), my = VyToMap(vy);
            if (!_map.InBounds(mx, my) || !_map.IsVisible(mx, my)) continue;
            if (_map.GetTile(mx, my).Type != SAOTRPG.Map.TileType.GasVent) continue;

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
        int turn = SAOTRPG.Map.DayNightCycle.CurrentTurn;
        int phase = turn % 8;
        if (phase > 1) return; // Only sparkle for 2 of every 8 turns.

        for (int vy = 0; vy < h; vy++)
        for (int vx = 0; vx < w; vx++)
        {
            int mx = VxToMap(vx), my = VyToMap(vy);
            if (!_map.InBounds(mx, my) || !_map.IsVisible(mx, my)) continue;
            var type = _map.GetTile(mx, my).Type;
            bool shrine = type == SAOTRPG.Map.TileType.Shrine || type == SAOTRPG.Map.TileType.EnchantShrine;
            bool fountain = type == SAOTRPG.Map.TileType.Fountain;
            if (!shrine && !fountain) continue;

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

    // Sparse starfield on unexplored viewport cells during night. Gives the
    // black void outside the torch bubble a sense of sky instead of empty.
    private void RenderNightStars(int w, int h)
    {
        string phase = SAOTRPG.Map.DayNightCycle.PhaseName;
        if (phase != "Night" && phase != "Dusk" && phase != "Dawn") return;
        int starCount = phase == "Night" ? 12 : 5;

        // Twinkling stars are decorative noise — the shared RNG avoids a
        // per-frame allocation. Star count is still phase-gated above.
        var rng = Random.Shared;
        var attr = Gfx.Attr(Color.White, Color.Black);
        var dimAttr = Gfx.Attr(Color.DarkGray, Color.Black);
        for (int i = 0; i < starCount; i++)
        {
            int vx = rng.Next(0, w), vy = rng.Next(0, h / 2); // top half of viewport
            int mx = VxToMap(vx), my = VyToMap(vy);
            // Only draw on cells that are outside explored area — not on
            // currently-visible terrain, not on remembered rooms.
            if (_map.InBounds(mx, my) && _map.IsExplored(mx, my)) continue;
            bool bright = rng.Next(3) == 0;
            DrawGlyph_View(vx, vy, bright ? '*' : '·', bright ? attr : dimAttr);
        }
    }

    // Viewport-space glyph draw (no map coords needed).
    private void DrawGlyph_View(int vx, int vy, char glyph, Terminal.Gui.Attribute attr)
    {
        Driver!.SetAttribute(attr); Move(vx, vy);
        Driver!.AddRune(new System.Text.Rune(glyph));
    }

    // Centered banner announcing a newly-sighted boss.
    private void RenderBossEntrance(int w, int h)
    {
        if (_bossEntranceFrames <= 0 || string.IsNullOrEmpty(_bossEntranceName)) return;
        // Banner text (with ToUpper + brackets) was precomputed in
        // TriggerBossEntrance so this per-frame draw allocates nothing.
        bool bright = (_bossEntranceFrames & 1) == 0;
        Color c = bright ? Color.BrightRed : Color.BrightYellow;
        DrawCenteredBanner(_bossEntranceBanner, Math.Max(2, h / 2 - 2), Gfx.Attr(c, Color.Black), w);
    }

    private void RenderLowHpPulse(int w, int h)
    {
        if (_player.IsDefeated || _player.CurrentHealth <= 0) return;
        if (_player.CurrentHealth > _player.MaxHealth / 4) return;
        // Steady red border when HP is at or below 25% — no flash.
        DrawEdgeBar('▁', '▔', Gfx.Attr(Color.BrightRed, Color.Black), w, h);
    }
}
