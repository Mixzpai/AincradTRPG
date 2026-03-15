using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Map;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

/// <summary>
/// Main dungeon map renderer and input handler.
///
/// Renders visible/explored tiles using the Camera to center on the player.
/// Handles all gameplay keybinds and fires events consumed by GameScreen.
///
/// Rendering layers (in priority order):
///   1. Out of bounds / unexplored → black
///   2. Explored but not visible   → dimmed terrain hints
///   3. Visible tile:
///      a. Occupant (monster/NPC/vendor) → entity symbol + color
///      b. Items on ground              → '!' bright yellow
///      c. Terrain                      → TileDefinitions visual
///   4. Post-effects: light glow, visibility falloff
/// </summary>
public class MapView : View
{
    // ── State ────────────────────────────────────────────────────────
    private GameMap _map;
    private readonly Camera _camera;
    private readonly Player _player;

    // ── Rendering constants ──────────────────────────────────────────
    private const int   VisibilityRadius    = 18;     // Max visibility range for falloff calc
    private const double FalloffThreshold   = 0.75;   // Beyond this, tiles dim to DarkGray
    private const int   LightGlowRadius     = 2;      // How far light sources glow (x-axis)

    // ── Footstep trail — recent positions rendered as dim dots ─────
    private const int FootstepTrailLength = 10;
    private readonly Queue<(int X, int Y)> _footsteps = new();
    private (int X, int Y) _lastPlayerPos = (-1, -1);

    // ── Damage flash — brief damage numbers shown at combat locations ──
    private readonly List<(int X, int Y, string Text, Color Color, int FramesLeft)> _damageFlashes = new();
    private const int DamageFlashFrames = 3;  // how many render frames to show

    /// <summary>Queue a damage number to flash at a map position.</summary>
    public void AddDamageFlash(int mx, int my, int damage, bool isPlayerDamage)
    {
        string text = damage.ToString();
        Color color = isPlayerDamage ? Color.BrightRed : Color.BrightYellow;
        _damageFlashes.Add((mx, my, text, color, DamageFlashFrames));
    }

    // ── Corpse markers — positions where monsters died ──────────────
    private readonly List<(int X, int Y, int FramesLeft)> _corpseMarkers = new();
    private const int CorpseMarkerFrames = 40;  // ~30 seconds at animation rate

    /// <summary>Mark a position where a monster was slain.</summary>
    public void AddCorpseMarker(int mx, int my)
    {
        _corpseMarkers.Add((mx, my, CorpseMarkerFrames));
    }

    // ── Loot sparkle — shimmer effect on tiles with items ───────────
    // Driven by AnimTick, no extra state needed

    // ── Scorch marks — visual damage marks from lava/trap tiles ──────
    private readonly List<(int X, int Y, int FramesLeft)> _scorchMarks = new();
    private const int ScorchMarkFrames = 60;

    /// <summary>Mark a position with a scorch/damage mark (from trap or lava).</summary>
    public void AddScorchMark(int mx, int my)
    {
        // Avoid duplicates at same position
        if (!_scorchMarks.Exists(s => s.X == mx && s.Y == my))
            _scorchMarks.Add((mx, my, ScorchMarkFrames));
    }

    // ── Mob patrol trails — recent positions of monsters ─────────────
    private readonly Dictionary<int, Queue<(int X, int Y)>> _mobTrails = new();
    private const int MobTrailLength = 4;

    /// <summary>Record a monster's position for trail rendering.</summary>
    public void TrackMobPosition(int entityId, int x, int y)
    {
        if (!_mobTrails.TryGetValue(entityId, out var trail))
        {
            trail = new Queue<(int, int)>();
            _mobTrails[entityId] = trail;
        }
        if (trail.Count > 0 && trail.Last() == (x, y)) return;
        trail.Enqueue((x, y));
        if (trail.Count > MobTrailLength) trail.Dequeue();
    }

    /// <summary>Remove a mob's trail data (called on death).</summary>
    public void ClearMobTrail(int entityId) => _mobTrails.Remove(entityId);

    // ── Shatter particles — SAO polygon burst on monster death ──────
    private readonly List<(int X, int Y, int Frame)> _shatterParticles = new();
    private const int ShatterTotalFrames = 4;
    private static readonly (int dx, int dy)[] ShatterOffsets =
    {
        (-1, -1), (0, -1), (1, -1),
        (-1,  0),          (1,  0),
        (-1,  1), (0,  1), (1,  1),
    };

    /// <summary>Queue a shatter particle burst at a map position (SAO polygon break).</summary>
    public void AddShatterParticle(int mx, int my)
    {
        _shatterParticles.Add((mx, my, ShatterTotalFrames));
    }

    // ── Kill streak flash — brief magenta player flash + star burst ──
    private int _killStreakFlashFrames;
    private int _killStreakLevel;  // 2=double, 3=triple, etc.

    /// <summary>Trigger a kill streak visual flash. Level: 2=double, 3+=triple.</summary>
    public void TriggerKillStreakFlash(int streakLevel)
    {
        _killStreakFlashFrames = 3;
        _killStreakLevel = streakLevel;
    }

    // ── Status effect screen tint ────────────────────────────────────
    private Color? _statusTintColor;

    /// <summary>Set a screen tint color for active status effects (null = none).</summary>
    public void SetStatusTint(Color? color) => _statusTintColor = color;

    // ── Door state — tracks which doors have been opened by the player ──
    private readonly HashSet<(int X, int Y)> _openedDoors = new();

    /// <summary>Mark a door as opened (called when player walks through).</summary>
    public void MarkDoorOpened(int x, int y) => _openedDoors.Add((x, y));

    // ── Events — fired by keybinds, consumed by GameScreen ───────────
    public event Action<int, int>? PlayerMoveRequested;     // (dx, dy) movement
    public event Action? LookRequested;                     // L — look around
    public event Action? WaitRequested;                     // Space — skip turn
    public event Action? InventoryRequested;                // I — open inventory
    public event Action? PickupRequested;                   // G — grab items
    public event Action? AutoExploreRequested;              // X — auto-explore
    public event Action? StatsRequested;                    // P — skill points
    public event Action? HelpRequested;                     // H — help screen
    public event Action? RestRequested;                     // R — rest (skip turns, heal)
    public event Action? KillStatsRequested;                 // K — kill/run statistics
    public event Action? EquipmentRequested;                 // T — equipment overview
    public event Action? LogScrollUpRequested;                // PageUp — scroll message log
    public event Action? LogScrollDownRequested;              // PageDown — scroll message log
    public event Action<int, int>? SprintRequested;          // Shift+direction — sprint 2 tiles
    public event Action<int, int>? StealthMoveRequested;     // Ctrl+direction — stealth move
    public event Action? SaveRequested;                       // F5 — quick save

    /// <summary>Hot-swap the map reference (used on floor change).</summary>
    public void SetMap(GameMap map)
    {
        _map = map;
        _footsteps.Clear();
        _lastPlayerPos = (-1, -1);
        _openedDoors.Clear();
        _scorchMarks.Clear();
        _mobTrails.Clear();
        _shatterParticles.Clear();
        _killStreakFlashFrames = 0;
        _statusTintColor = null;
    }

    public MapView(GameMap map, Camera camera, Player player)
    {
        _map = map;
        _camera = camera;
        _player = player;
        CanFocus = true;
    }

    // ══════════════════════════════════════════════════════════════════
    //  RENDERING
    // ══════════════════════════════════════════════════════════════════

    protected override bool OnDrawingContent()
    {
        var vp = Viewport;
        _camera.ViewWidth = vp.Width;
        _camera.ViewHeight = vp.Height;
        _camera.CenterOn(_player.X, _player.Y, _map.Width, _map.Height);

        // Advance animation tick each frame (drives water/flower cycling)
        TileDefinitions.AnimTick++;

        // Track footstep trail — record player position when it changes
        if (_player.X != _lastPlayerPos.X || _player.Y != _lastPlayerPos.Y)
        {
            if (_lastPlayerPos.X >= 0)
            {
                _footsteps.Enqueue(_lastPlayerPos);
                if (_footsteps.Count > FootstepTrailLength)
                    _footsteps.Dequeue();
            }
            _lastPlayerPos = (_player.X, _player.Y);
        }

        for (int vy = 0; vy < vp.Height; vy++)
        {
            for (int vx = 0; vx < vp.Width; vx++)
            {
                int mx = vx + _camera.OffsetX;
                int my = vy + _camera.OffsetY;

                var (ch, fg, bg) = ResolveTileVisual(mx, my);

                Driver.SetAttribute(new Terminal.Gui.Attribute(fg, bg));
                Move(vx, vy);
                Driver.AddRune(new System.Text.Rune(ch));
            }
        }

        // ── Room reveal flash — bright highlight on newly explored tiles ──
        RenderRevealFlash(vp.Width, vp.Height);

        // ── Footstep trail overlay — dim dots where player has walked ──
        RenderFootstepTrail(vp.Width, vp.Height);

        // ── Grid coordinate overlay — column/row numbers every 5th tile ──
        RenderGridCoordinates(vp.Width, vp.Height);

        // ── Boss HP bar overlay — rendered at top of map when a boss is visible ──
        RenderBossBar(vp.Width);

        // ── Ambient particles — wind, mist, embers ──
        RenderAmbientParticles(vp.Width, vp.Height);

        // ── Corpse markers — dim '%' where monsters died ──
        RenderCorpseMarkers(vp.Width, vp.Height);

        // ── Loot sparkle — shimmer on item tiles ──
        RenderLootSparkle(vp.Width, vp.Height);

        // ── Damage numbers — brief flash at combat locations ──
        RenderDamageFlashes(vp.Width, vp.Height);

        // ── Scorch marks — '░' at trap/lava damage locations ──
        RenderScorchMarks(vp.Width, vp.Height);

        // ── Mob patrol trails — dim dots showing monster movement ──
        RenderMobTrails(vp.Width, vp.Height);

        // ── Rain particle overlay — weather-driven ──
        RenderRainParticles(vp.Width, vp.Height);

        // ── Monster aggro indicators — '!' above chasing mobs ──
        RenderAggroIndicators(vp.Width, vp.Height);

        // ── Interactable sparkle — ✦/✧ pulse on doors, stairs, shrines, etc. ──
        RenderInteractableSparkle(vp.Width, vp.Height);

        // ── Shatter particles — ◇ diamond burst on monster death (SAO polygon break) ──
        RenderShatterParticles(vp.Width, vp.Height);

        // ── Kill streak flash — magenta @ pulse + ★ burst ──
        RenderKillStreakFlash(vp.Width, vp.Height);

        return true;
    }

    /// <summary>
    /// Determines the character, foreground, and background color for a map cell.
    /// Handles all visibility states and rendering layers.
    /// </summary>
    private (char ch, Color fg, Color bg) ResolveTileVisual(int mx, int my)
    {
        // ── Layer 1: Out of bounds ───────────────────────────────────
        if (!_map.InBounds(mx, my))
            return (' ', Color.Black, Color.Black);

        // ── Layer 2: Unexplored — totally hidden ─────────────────────
        if (!_map.IsExplored(mx, my))
            return (' ', Color.Black, Color.Black);

        // ── Layer 3: Explored but not currently visible — dim memory ─
        if (!_map.IsVisible(mx, my))
        {
            var tileType = _map.GetTile(mx, my).Type;
            var visual = TileDefinitions.GetVisual(tileType, mx, my);
            char dimCh = visual.Glyph;
            Color dimFg = GetDimColor(tileType);
            // Explored room tint — floor tiles in explored rooms get a warmer gray
            if (tileType == TileType.Floor)
                dimFg = Color.Gray;
            // Door open/closed in memory
            if (tileType == TileType.Door && _openedDoors.Contains((mx, my)))
                dimCh = '╌';
            return (dimCh, dimFg, Color.Black);
        }

        // ── Layer 4: Fully visible tile ──────────────────────────────
        var tile = _map.GetTile(mx, my);
        char ch;
        Color fg, bg;

        // Priority: Occupant > Items > Terrain
        if (tile.Occupant != null && !tile.Occupant.IsDefeated)
        {
            ch = tile.Occupant.Symbol;
            fg = tile.Occupant.SymbolColor;
            bg = Color.Black;

            // ── Health-based player color ──────────────────────────
            if (tile.Occupant == _player)
            {
                double hpPct = _player.MaxHealth > 0
                    ? (double)_player.CurrentHealth / _player.MaxHealth : 1.0;
                fg = hpPct switch
                {
                    > 0.50 => Color.BrightYellow,    // Healthy
                    > 0.25 => Color.Yellow,           // Wounded
                    > 0.10 => Color.BrightRed,        // Critical
                    _ => (TileDefinitions.AnimTick % 4 < 2)
                        ? Color.BrightRed : Color.Red  // Near death — pulsing
                };
            }
        }
        else if (tile.HasItems)
        {
            // Rarity-based item markers — show highest rarity item on tile
            var (rarityGlyph, rarityColor) = GetItemRarityVisual(tile.Items);
            ch = rarityGlyph;
            fg = rarityColor;
            bg = Color.Black;
        }
        else
        {
            var visual = TileDefinitions.GetVisual(tile.Type, mx, my);
            ch = visual.Glyph;
            fg = visual.Foreground;
            bg = visual.Background;

            // ── Door open/closed state ────────────────────────────
            if (tile.Type == TileType.Door && _openedDoors.Contains((mx, my)))
                ch = '╌';  // Opened doors show as broken line

            // ── Water reflection + flow animation ──
            if (tile.Type is TileType.Water or TileType.WaterDeep)
            {
                var reflected = GetReflectedEntity(mx, my);
                if (reflected != null)
                {
                    ch = reflected.Symbol;
                    fg = Color.DarkGray;
                }
                else
                {
                    // Lily pads on ~10% of shallow water tiles (static)
                    var lilyPad = (tile.Type == TileType.Water)
                        ? MapEffects.GetWaterLilyPad(mx, my) : null;
                    if (lilyPad != null)
                    {
                        ch = lilyPad.Value.Glyph;
                        fg = lilyPad.Value.Color;
                    }
                    else
                    {
                        // Animated wave propagation
                        ch = MapEffects.GetWaterFlowGlyph(mx, my);
                    }
                }

                // ── Water edge foam — shoreline tiles are lighter ──
                if (MapEffects.IsWaterEdge(_map, mx, my, tile.Type))
                    fg = Color.BrightCyan;
            }

            // ── Terrain transition borders — ground near water/lava ──
            var transition = MapEffects.GetTransitionBorder(_map, mx, my, tile.Type);
            if (transition != null)
            {
                ch = transition.Value.Glyph;
                fg = transition.Value.Color;
            }

            // ── Room lighting variation — per-region tint on floor tiles ──
            var roomTint = MapEffects.GetRoomTint(mx, my, tile.Type);
            if (roomTint != null && fg == Color.Gray)
                fg = roomTint.Value;  // Only tint neutral gray floors
        }

        // ── Post-effect: cobweb decorations — gray '%' on wall corner tiles ──
        if (tile.Type == TileType.Wall)
        {
            var cobweb = MapEffects.GetCobwebDecoration(_map, mx, my);
            if (cobweb != null)
            {
                ch = cobweb.Value.Glyph;
                fg = cobweb.Value.Color;
            }
        }

        // ── Post-effect: wall edge shading — edge walls use █ with bright color ──
        if (tile.Type == TileType.Wall)
        {
            if (IsWallEdge(mx, my))
            {
                ch = '█';
                if (fg != Color.White) fg = Color.White;
            }
        }

        // ── Post-effect: shadow casting — floor below wall is darker ──
        if (MapEffects.IsInWallShadow(_map, mx, my))
            bg = Color.DarkGray;

        // ── Post-effect: lava danger zone — red bg within 2 tiles of lava ──
        if (tile.Type != TileType.Lava && tile.Type != TileType.Wall
            && MapEffects.IsInLavaDangerZone(_map, mx, my))
            bg = Color.DarkGray;

        // ── Post-effect: explored room glow — warm tint on well-explored floors ──
        var roomGlow = MapEffects.GetExploredRoomGlow(_map, mx, my);
        if (roomGlow != null && fg == Color.Gray)
            fg = roomGlow.Value;

        // ── Post-effect: warm glow near light sources (gradient) ─────
        int lightDist = GetLightSourceDistance(mx, my);
        if (lightDist >= 0)
        {
            if (lightDist <= 1)
                bg = Color.DarkGray;
            else if (fg == Color.DarkGray)
                fg = Color.Gray;
        }

        // ── Post-effect: shrine aura glow — mystical purple radiance ──
        int shrineDist = MapEffects.GetShrineDistance(_map, mx, my);
        if (shrineDist >= 0)
        {
            if (shrineDist <= 1)
                bg = Color.DarkGray;
            if (fg == Color.Gray || fg == Color.DarkGray)
                fg = Color.Magenta;  // Purple tint on nearby tiles
        }

        // ── Post-effect: edge-of-vision falloff ──────────────────────
        double falloff = _map.GetVisibilityFalloff(mx, my, _player.X, _player.Y, VisibilityRadius);

        // ── Post-effect: depth-based tinting (mid-range dimming) ──
        var depthColor = MapEffects.GetDepthTintedColor(fg, falloff);
        if (depthColor != null)
            fg = depthColor.Value;

        if (falloff > FalloffThreshold)
        {
            fg = Color.DarkGray;
            bg = Color.Black;
        }

        // ── Post-effect: campfire flicker glow — pulsing warm light ──
        int campDist = MapEffects.GetCampfireGlowDistance(_map, mx, my);
        if (campDist >= 0)
        {
            if (campDist <= 1)
                bg = Color.DarkGray;
            if (fg == Color.DarkGray)
                fg = Color.Yellow;
        }

        // ── Post-effect: weather color dimming (rain) ────────────────
        var weatherDim = MapEffects.GetWeatherDimmedColor(fg);
        if (weatherDim != null)
            fg = weatherDim.Value;

        // ── Post-effect: fog weather — reduce effective visibility ────
        if (WeatherSystem.Current == WeatherType.Fog)
        {
            int fogRadius = VisibilityRadius - WeatherSystem.FogRadiusReduction;
            double fogFalloff = _map.GetVisibilityFalloff(mx, my, _player.X, _player.Y, fogRadius);
            if (fogFalloff > FalloffThreshold)
            {
                fg = Color.DarkGray;
                bg = Color.Black;
            }
        }

        // ── Post-effect: status effect screen tint ────────────────────
        if (_statusTintColor != null && bg == Color.Black)
        {
            // Subtle tint: only apply to every other tile for a scanline effect
            if ((mx + my) % 3 == 0)
                fg = _statusTintColor.Value;
        }

        // ── Post-effect: fog of war edge gradient — ░▒▓ density shading ──
        var fogEdge = MapEffects.GetFogEdgeColor(_map, mx, my);
        if (fogEdge != null && fg != Color.DarkGray)
        {
            // Apply fog density block glyph overlay
            var fogGlyph = MapEffects.GetFogDensityGlyph(_map, mx, my);
            if (fogGlyph != null)
                ch = fogGlyph.Value;

            // Dim the foreground toward the fog edge color
            if (fogEdge == Color.DarkGray)
                fg = Color.DarkGray;
            else if (fg == Color.White || fg == Color.BrightYellow)
                fg = Color.Gray;
        }

        return (ch, fg, bg);
    }

    /// <summary>
    /// Returns a dimmed foreground color for explored-but-not-visible tiles.
    /// Water stays bluish, structures stay gray, everything else is dark gray.
    /// </summary>
    private static Color GetDimColor(TileType type) => type switch
    {
        TileType.Water or TileType.WaterDeep                 => Color.Blue,
        TileType.Wall or TileType.Mountain                   => Color.Gray,
        TileType.StairsUp                                      => Color.Gray,
        TileType.Campfire                                     => Color.DarkGray,
        TileType.Lava                                         => Color.Red,
        _                                                     => Color.DarkGray,
    };

    /// <summary>
    /// Returns the Manhattan distance to the nearest light source, or -1 if none nearby.
    /// Light sources: doors, stairs, campfires. Scans within LightGlowRadius.
    /// </summary>
    private int GetLightSourceDistance(int x, int y)
    {
        int closest = -1;
        for (int dx = -LightGlowRadius; dx <= LightGlowRadius; dx++)
        for (int dy = -LightGlowRadius; dy <= LightGlowRadius; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = x + dx, ny = y + dy;
            if (!_map.InBounds(nx, ny)) continue;

            var t = _map.GetTile(nx, ny).Type;
            if (t is TileType.Door or TileType.StairsUp or TileType.Campfire)
            {
                int dist = Math.Abs(dx) + Math.Abs(dy);
                if (closest < 0 || dist < closest)
                    closest = dist;
            }
        }
        return closest;
    }

    // ══════════════════════════════════════════════════════════════════
    //  WATER REFLECTION
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Checks tiles above and beside a water tile for a living entity.
    /// Returns the entity if found (for rendering a dimmed reflection).
    /// </summary>
    private Entity? GetReflectedEntity(int x, int y)
    {
        // Check tile directly above (reflection "below" the entity)
        if (_map.InBounds(x, y - 1))
        {
            var above = _map.GetTile(x, y - 1);
            if (above.Occupant != null && !above.Occupant.IsDefeated)
                return above.Occupant;
        }
        return null;
    }

    // ══════════════════════════════════════════════════════════════════
    //  ROOM REVEAL FLASH
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Highlights newly explored tiles with a bright flash for one render frame.
    /// Creates a satisfying "room reveal" effect when entering new areas.
    /// </summary>
    private void RenderRevealFlash(int viewWidth, int viewHeight)
    {
        if (_map.NewlyRevealed.Count == 0) return;

        var flashAttr = new Terminal.Gui.Attribute(Color.White, Color.DarkGray);

        foreach (var (fx, fy) in _map.NewlyRevealed)
        {
            int vx = fx - _camera.OffsetX;
            int vy = fy - _camera.OffsetY;
            if (vx < 0 || vx >= viewWidth || vy < 0 || vy >= viewHeight) continue;

            var tile = _map.GetTile(fx, fy);
            var visual = TileDefinitions.GetVisual(tile.Type, fx, fy);

            Driver.SetAttribute(flashAttr);
            Move(vx, vy);
            Driver.AddRune(new System.Text.Rune(visual.Glyph));
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  FOOTSTEP TRAIL
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders recent player positions as dim '·' dots on visible floor tiles.
    /// </summary>
    private void RenderFootstepTrail(int viewWidth, int viewHeight)
    {
        var trailAttr = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black);

        foreach (var (fx, fy) in _footsteps)
        {
            if (!_map.InBounds(fx, fy) || !_map.IsVisible(fx, fy)) continue;
            var tile = _map.GetTile(fx, fy);
            if (tile.Occupant != null || tile.HasItems) continue;
            if (!tile.IsWalkable) continue;

            int vx = fx - _camera.OffsetX;
            int vy = fy - _camera.OffsetY;
            if (vx < 0 || vx >= viewWidth || vy < 0 || vy >= viewHeight) continue;

            Driver.SetAttribute(trailAttr);
            Move(vx, vy);
            Driver.AddRune(new System.Text.Rune('·'));
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  BOSS HP BAR
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders a centered boss HP bar at the top of the map viewport
    /// when a living boss is visible on screen.
    /// Format: [BOSS NAME] [########----] HP/MaxHP
    /// </summary>
    private void RenderBossBar(int viewWidth)
    {
        Boss? boss = null;
        foreach (var entity in _map.Entities)
        {
            if (entity is Boss b && !b.IsDefeated && _map.IsVisible(b.X, b.Y))
            { boss = b; break; }
        }
        if (boss == null) return;

        const int barWidth = 20;
        string hpBar = BarBuilder.BuildGradient(boss.CurrentHealth, boss.MaxHealth, barWidth);
        string label = $" {boss.Name} {hpBar} {boss.CurrentHealth}/{boss.MaxHealth} ";

        int startX = Math.Max(0, (viewWidth - label.Length) / 2);

        for (int i = 0; i < label.Length && startX + i < viewWidth; i++)
        {
            Driver.SetAttribute(new Terminal.Gui.Attribute(Color.BrightRed, Color.DarkGray));
            Move(startX + i, 0);
            Driver.AddRune(new System.Text.Rune(label[i]));
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  GRID COORDINATES
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders faint column/row numbers along the top and left edges every 5 tiles.
    /// </summary>
    private void RenderGridCoordinates(int viewWidth, int viewHeight)
    {
        var coordAttr = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black);

        // Top edge — column numbers
        for (int vx = 0; vx < viewWidth; vx++)
        {
            int mx = vx + _camera.OffsetX;
            if (mx % 5 != 0 || mx < 0) continue;
            string num = (mx % 100).ToString();
            for (int c = 0; c < num.Length && vx + c < viewWidth; c++)
            {
                Driver.SetAttribute(coordAttr);
                Move(vx + c, 0);
                Driver.AddRune(new System.Text.Rune(num[c]));
            }
        }

        // Left edge — row numbers
        for (int vy = 1; vy < viewHeight; vy++)
        {
            int my = vy + _camera.OffsetY;
            if (my % 5 != 0 || my < 0) continue;
            string num = (my % 100).ToString();
            for (int c = 0; c < num.Length && c < viewWidth; c++)
            {
                Driver.SetAttribute(coordAttr);
                Move(c, vy);
                Driver.AddRune(new System.Text.Rune(num[c]));
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  DAMAGE FLASH
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders damage numbers at combat locations for a few frames, then removes them.
    /// </summary>
    private void RenderDamageFlashes(int viewWidth, int viewHeight)
    {
        for (int i = _damageFlashes.Count - 1; i >= 0; i--)
        {
            var (fx, fy, text, color, framesLeft) = _damageFlashes[i];
            // Render slightly above the combat position
            int vx = fx - _camera.OffsetX;
            int vy = (fy - 1) - _camera.OffsetY;  // one tile above
            if (vy < 0) vy = fy - _camera.OffsetY;  // fallback to same row

            if (vx >= 0 && vx + text.Length <= viewWidth && vy >= 0 && vy < viewHeight)
            {
                Driver.SetAttribute(new Terminal.Gui.Attribute(color, Color.Black));
                for (int c = 0; c < text.Length && vx + c < viewWidth; c++)
                {
                    Move(vx + c, vy);
                    Driver.AddRune(new System.Text.Rune(text[c]));
                }
            }

            if (framesLeft <= 1)
                _damageFlashes.RemoveAt(i);
            else
                _damageFlashes[i] = (fx, fy, text, color, framesLeft - 1);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  CORPSE MARKERS
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders '†' cross markers at positions where monsters were recently slain.
    /// Color fades from Red → DarkGray over the marker's lifetime.
    /// </summary>
    private void RenderCorpseMarkers(int viewWidth, int viewHeight)
    {
        for (int i = _corpseMarkers.Count - 1; i >= 0; i--)
        {
            var (cx, cy, framesLeft) = _corpseMarkers[i];
            if (!_map.InBounds(cx, cy) || !_map.IsVisible(cx, cy)) { continue; }
            var tile = _map.GetTile(cx, cy);
            if (tile.Occupant != null || tile.HasItems) { continue; }

            int vx = cx - _camera.OffsetX;
            int vy = cy - _camera.OffsetY;
            if (vx >= 0 && vx < viewWidth && vy >= 0 && vy < viewHeight)
            {
                // Fade: Red (fresh) → Gray (mid) → DarkGray (old)
                double life = (double)framesLeft / CorpseMarkerFrames;
                Color corpseColor = life > 0.66 ? Color.Red
                                  : life > 0.33 ? Color.Gray
                                  : Color.DarkGray;
                Driver.SetAttribute(new Terminal.Gui.Attribute(corpseColor, Color.Black));
                Move(vx, vy);
                Driver.AddRune(new System.Text.Rune('†'));
            }

            if (framesLeft <= 1)
                _corpseMarkers.RemoveAt(i);
            else
                _corpseMarkers[i] = (cx, cy, framesLeft - 1);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  LOOT SPARKLE
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Adds a twinkling effect to tiles containing items on the ground.
    /// Rarity-appropriate glyphs flash white on sparkle frames.
    /// </summary>
    private void RenderLootSparkle(int viewWidth, int viewHeight)
    {
        // Every few ticks, items flash white instead of their rarity color
        bool sparkleFrame = TileDefinitions.AnimTick % 4 == 0;
        if (!sparkleFrame) return;

        var sparkleAttr = new Terminal.Gui.Attribute(Color.White, Color.Black);

        for (int vy = 0; vy < viewHeight; vy++)
        for (int vx = 0; vx < viewWidth; vx++)
        {
            int mx = vx + _camera.OffsetX;
            int my = vy + _camera.OffsetY;
            if (!_map.InBounds(mx, my) || !_map.IsVisible(mx, my)) continue;

            var tile = _map.GetTile(mx, my);
            if (!tile.HasItems || tile.Occupant != null) continue;

            var (rarityGlyph, _) = GetItemRarityVisual(tile.Items);
            Driver.SetAttribute(sparkleAttr);
            Move(vx, vy);
            Driver.AddRune(new System.Text.Rune(rarityGlyph));
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  AMBIENT PARTICLES
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders sparse ambient particles on eligible terrain: wind on grass,
    /// mist on water, embers near campfires.
    /// </summary>
    private void RenderAmbientParticles(int viewWidth, int viewHeight)
    {
        for (int vy = 0; vy < viewHeight; vy++)
        for (int vx = 0; vx < viewWidth; vx++)
        {
            int mx = vx + _camera.OffsetX;
            int my = vy + _camera.OffsetY;
            if (!_map.InBounds(mx, my) || !_map.IsVisible(mx, my)) continue;

            var tile = _map.GetTile(mx, my);
            if (tile.Occupant != null || tile.HasItems) continue;

            var particle = MapEffects.GetAmbientParticle(tile.Type, mx, my);
            if (particle == null) continue;

            Driver.SetAttribute(new Terminal.Gui.Attribute(particle.Value.Color, Color.Black));
            Move(vx, vy);
            Driver.AddRune(new System.Text.Rune(particle.Value.Glyph));
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  SCORCH MARKS
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders '░' scorch marks at positions where trap/lava damage occurred.
    /// Fades after ScorchMarkFrames render cycles.
    /// </summary>
    private void RenderScorchMarks(int viewWidth, int viewHeight)
    {
        for (int i = _scorchMarks.Count - 1; i >= 0; i--)
        {
            var (sx, sy, framesLeft) = _scorchMarks[i];
            if (!_map.InBounds(sx, sy) || !_map.IsVisible(sx, sy)) { continue; }
            var tile = _map.GetTile(sx, sy);
            if (tile.Occupant != null) { continue; }

            int vx = sx - _camera.OffsetX;
            int vy = sy - _camera.OffsetY;
            if (vx >= 0 && vx < viewWidth && vy >= 0 && vy < viewHeight)
            {
                Color scorchColor = framesLeft > ScorchMarkFrames / 2 ? Color.Red : Color.DarkGray;
                Driver.SetAttribute(new Terminal.Gui.Attribute(scorchColor, Color.Black));
                Move(vx, vy);
                Driver.AddRune(new System.Text.Rune('░'));
            }

            if (framesLeft <= 1)
                _scorchMarks.RemoveAt(i);
            else
                _scorchMarks[i] = (sx, sy, framesLeft - 1);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  MOB PATROL TRAILS
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders dim '·' dots along recent monster movement paths.
    /// Only shows trails for visible, living monsters.
    /// </summary>
    private void RenderMobTrails(int viewWidth, int viewHeight)
    {
        var trailAttr = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black);

        foreach (var (entityId, trail) in _mobTrails)
        {
            foreach (var (tx, ty) in trail)
            {
                if (!_map.InBounds(tx, ty) || !_map.IsVisible(tx, ty)) continue;
                var tile = _map.GetTile(tx, ty);
                if (tile.Occupant != null || tile.HasItems || !tile.IsWalkable) continue;

                int vx = tx - _camera.OffsetX;
                int vy = ty - _camera.OffsetY;
                if (vx < 0 || vx >= viewWidth || vy < 0 || vy >= viewHeight) continue;

                Driver.SetAttribute(trailAttr);
                Move(vx, vy);
                Driver.AddRune(new System.Text.Rune('·'));
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  RAIN PARTICLES
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders falling rain particles when the weather is Rain.
    /// Sparse '|' and '/' characters across the visible viewport.
    /// </summary>
    private void RenderRainParticles(int viewWidth, int viewHeight)
    {
        if (WeatherSystem.Current != WeatherType.Rain) return;

        for (int vy = 0; vy < viewHeight; vy++)
        for (int vx = 0; vx < viewWidth; vx++)
        {
            int mx = vx + _camera.OffsetX;
            int my = vy + _camera.OffsetY;
            if (!_map.InBounds(mx, my) || !_map.IsVisible(mx, my)) continue;

            var tile = _map.GetTile(mx, my);
            if (tile.Occupant != null || tile.HasItems) continue;
            // Don't render rain on indoor tiles (floor, wall)
            if (tile.Type is TileType.Floor or TileType.Wall) continue;

            var particle = MapEffects.GetRainParticle(mx, my);
            if (particle == null) continue;

            Driver.SetAttribute(new Terminal.Gui.Attribute(particle.Value.Color, Color.Black));
            Move(vx, vy);
            Driver.AddRune(new System.Text.Rune(particle.Value.Glyph));
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  AGGRO INDICATORS
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders a bright '!' one tile above monsters that are chasing the player.
    /// </summary>
    private void RenderAggroIndicators(int viewWidth, int viewHeight)
    {
        foreach (var entity in _map.Entities)
        {
            if (entity is not Monster monster || monster.IsDefeated) continue;
            if (!MapEffects.ShouldShowAggroIndicator(_map, monster.X, monster.Y, _player.X, _player.Y))
                continue;

            // Render '!' one tile above the monster
            int vx = monster.X - _camera.OffsetX;
            int vy = (monster.Y - 1) - _camera.OffsetY;
            if (vy < 0) vy = monster.Y - _camera.OffsetY;

            if (vx >= 0 && vx < viewWidth && vy >= 0 && vy < viewHeight)
            {
                Color aggroColor = (TileDefinitions.AnimTick % 4 < 2) ? Color.BrightRed : Color.BrightYellow;
                Driver.SetAttribute(new Terminal.Gui.Attribute(aggroColor, Color.Black));
                Move(vx, vy);
                Driver.AddRune(new System.Text.Rune('!'));
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  INTERACTABLE SPARKLE
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders ✦/✧ sparkle overlays on interactable tiles (doors, stairs, shrines, etc.)
    /// to draw the player's eye to interactive points of interest.
    /// </summary>
    private void RenderInteractableSparkle(int viewWidth, int viewHeight)
    {
        for (int vy = 0; vy < viewHeight; vy++)
        for (int vx = 0; vx < viewWidth; vx++)
        {
            int mx = vx + _camera.OffsetX;
            int my = vy + _camera.OffsetY;
            if (!_map.InBounds(mx, my) || !_map.IsVisible(mx, my)) continue;

            var tile = _map.GetTile(mx, my);
            if (tile.Occupant != null) continue;  // Don't sparkle under entities

            var sparkle = MapEffects.GetInteractableSparkle(tile.Type, mx, my);
            if (sparkle == null) continue;

            Driver.SetAttribute(new Terminal.Gui.Attribute(sparkle.Value.Color, Color.Black));
            Move(vx, vy);
            Driver.AddRune(new System.Text.Rune(sparkle.Value.Glyph));
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  SHATTER PARTICLES (SAO Polygon Burst)
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders ◇ diamond fragments radiating from monster death positions.
    /// SAO-style polygon shatter: BrightCyan → Cyan → DarkGray over 4 frames.
    /// </summary>
    private void RenderShatterParticles(int viewWidth, int viewHeight)
    {
        for (int i = _shatterParticles.Count - 1; i >= 0; i--)
        {
            var (cx, cy, frame) = _shatterParticles[i];
            // Frame 4=fresh, 1=fading — show expanding ring of diamonds
            int radius = ShatterTotalFrames - frame + 1;  // 1,2,3,4

            Color shatterColor = frame switch
            {
                >= 3 => Color.BrightCyan,
                2    => Color.Cyan,
                _    => Color.DarkGray,
            };
            var attr = new Terminal.Gui.Attribute(shatterColor, Color.Black);

            foreach (var (dx, dy) in ShatterOffsets)
            {
                // Only show offsets at current radius distance
                int dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
                if (dist > radius) continue;

                int px = cx + dx * radius;
                int py = cy + dy * radius;
                if (!_map.InBounds(px, py) || !_map.IsVisible(px, py)) continue;

                int vx = px - _camera.OffsetX;
                int vy = py - _camera.OffsetY;
                if (vx < 0 || vx >= viewWidth || vy < 0 || vy >= viewHeight) continue;

                Driver.SetAttribute(attr);
                Move(vx, vy);
                Driver.AddRune(new System.Text.Rune('◇'));
            }

            if (frame <= 1)
                _shatterParticles.RemoveAt(i);
            else
                _shatterParticles[i] = (cx, cy, frame - 1);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  KILL STREAK FLASH
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Flashes the player '@' in magenta on Double Kill+.
    /// Shows ★ burst around player on Triple Kill+.
    /// </summary>
    private void RenderKillStreakFlash(int viewWidth, int viewHeight)
    {
        if (_killStreakFlashFrames <= 0) return;
        _killStreakFlashFrames--;

        int pvx = _player.X - _camera.OffsetX;
        int pvy = _player.Y - _camera.OffsetY;

        // Flash player symbol magenta (Double Kill+)
        if (pvx >= 0 && pvx < viewWidth && pvy >= 0 && pvy < viewHeight)
        {
            Driver.SetAttribute(new Terminal.Gui.Attribute(Color.BrightMagenta, Color.Black));
            Move(pvx, pvy);
            Driver.AddRune(new System.Text.Rune('@'));
        }

        // Triple Kill+ — ★ burst in cardinal + diagonal directions
        if (_killStreakLevel >= 3)
        {
            var burstAttr = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black);
            (int dx, int dy)[] dirs = { (0, -1), (0, 1), (-1, 0), (1, 0), (-1, -1), (1, -1), (-1, 1), (1, 1) };
            foreach (var (dx, dy) in dirs)
            {
                int bx = pvx + dx;
                int by = pvy + dy;
                if (bx < 0 || bx >= viewWidth || by < 0 || by >= viewHeight) continue;

                Driver.SetAttribute(burstAttr);
                Move(bx, by);
                Driver.AddRune(new System.Text.Rune('★'));
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  ITEM RARITY VISUAL
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns a Unicode glyph and color based on the highest rarity item on a tile.
    /// Common: •  Uncommon: ◇  Rare: ◆  Epic: ★  Legendary: ★ (magenta)
    /// </summary>
    private static (char Glyph, Color Color) GetItemRarityVisual(List<BaseItem> items)
    {
        // Find highest rarity among items on tile
        int bestRank = 0;
        foreach (var item in items)
        {
            int rank = item.Rarity switch
            {
                "Legendary" => 4,
                "Epic"      => 3,
                "Rare"      => 2,
                "Uncommon"  => 1,
                _           => 0,
            };
            if (rank > bestRank) bestRank = rank;
        }

        return bestRank switch
        {
            4 => ('★', Color.BrightMagenta),   // Legendary
            3 => ('★', Color.BrightYellow),     // Epic
            2 => ('◆', Color.BrightCyan),       // Rare
            1 => ('◇', Color.BrightGreen),      // Uncommon
            _ => ('•', Color.BrightYellow),      // Common
        };
    }

    // ══════════════════════════════════════════════════════════════════
    //  WALL EDGE DETECTION
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns true if this wall tile is adjacent to at least one non-wall tile.
    /// Edge walls are rendered brighter for a 3D depth effect.
    /// </summary>
    private bool IsWallEdge(int x, int y)
    {
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = x + dx, ny = y + dy;
            if (!_map.InBounds(nx, ny)) continue;
            var t = _map.GetTile(nx, ny).Type;
            if (t != TileType.Wall && t != TileType.Mountain)
                return true;
        }
        return false;
    }

    // ══════════════════════════════════════════════════════════════════
    //  INPUT HANDLING
    // ══════════════════════════════════════════════════════════════════

    protected override bool OnKeyDown(Key keyEvent)
    {
        int dx = 0, dy = 0;

        // Strip modifier flags so Shift+W still matches KeyCode.W
        var bareKey = keyEvent.KeyCode & ~KeyCode.ShiftMask & ~KeyCode.CtrlMask & ~KeyCode.AltMask;

        switch (bareKey)
        {
            // ── Cardinal movement ────────────────────────────────────
            case KeyCode.W: case KeyCode.CursorUp:    dy = -1; break;
            case KeyCode.S: case KeyCode.CursorDown:   dy = 1;  break;
            case KeyCode.A: case KeyCode.CursorLeft:   dx = -1; break;
            case KeyCode.D: case KeyCode.CursorRight:  dx = 1;  break;

            // ── Diagonal movement ────────────────────────────────────
            case KeyCode.Q: dx = -1; dy = -1; break;  // NW
            case KeyCode.E: dx =  1; dy = -1; break;  // NE
            case KeyCode.Z: dx = -1; dy =  1; break;  // SW
            case KeyCode.C: dx =  1; dy =  1; break;  // SE

            // ── Action keys ──────────────────────────────────────────
            case KeyCode.L:     return FireEvent(LookRequested,        keyEvent);
            case KeyCode.I:     return FireEvent(InventoryRequested,   keyEvent);
            case KeyCode.G:     return FireEvent(PickupRequested,      keyEvent);
            case KeyCode.X:     return FireEvent(AutoExploreRequested, keyEvent);
            case KeyCode.P:     return FireEvent(StatsRequested,       keyEvent);
            case KeyCode.H:     return FireEvent(HelpRequested,        keyEvent);
            case KeyCode.R:     return FireEvent(RestRequested,        keyEvent);
            case KeyCode.K:     return FireEvent(KillStatsRequested,  keyEvent);
            case KeyCode.T:     return FireEvent(EquipmentRequested,  keyEvent);
            case KeyCode.Space: return FireEvent(WaitRequested,        keyEvent);

            // ── Save ──────────────────────────────────────────────────
            case KeyCode.F5:    return FireEvent(SaveRequested,            keyEvent);

            // ── Log scrollback ─────────────────────────────────────
            case KeyCode.PageUp:   return FireEvent(LogScrollUpRequested,   keyEvent);
            case KeyCode.PageDown: return FireEvent(LogScrollDownRequested, keyEvent);

            default:
                return base.OnKeyDown(keyEvent);
        }

        // Movement key was pressed — Shift = sprint, Ctrl = stealth
        if (keyEvent.IsShift)
            SprintRequested?.Invoke(dx, dy);
        else if ((keyEvent.KeyCode & KeyCode.CtrlMask) != 0)
            StealthMoveRequested?.Invoke(dx, dy);
        else
            PlayerMoveRequested?.Invoke(dx, dy);
        keyEvent.Handled = true;
        return true;
    }

    /// <summary>
    /// Helper to fire a parameterless event and mark the key as handled.
    /// Reduces repetition in the key switch block.
    /// </summary>
    private static bool FireEvent(Action? handler, Key keyEvent)
    {
        handler?.Invoke();
        keyEvent.Handled = true;
        return true;
    }
}
