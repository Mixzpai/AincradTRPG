using Terminal.Gui;
using SAOTRPG.Map;
using SAOTRPG.Systems;

namespace SAOTRPG.UI.Helpers;

/// <summary>
/// Static helpers for map rendering post-effects.
/// Extracted from MapView to keep the main renderer focused on core logic.
///
/// Effects provided:
///   - Fog of war edge gradient (soft boundary between explored/unexplored)
///   - Ambient particles (wind dots, water mist, campfire embers)
///   - Terrain transition borders (smoothing between biomes)
///   - Shadow casting (walls cast 1-tile shadow below)
///   - Water edge foam (shoreline visual)
///   - Room lighting variation (per-room color tint)
///   - Depth-based tinting (distance dimming within visibility)
///   - Rain particle overlay (weather system)
///   - Weather color dimming (rain desaturates colors)
///   - Water flow animation (wave propagation)
///   - Campfire light flicker (pulsing radius)
/// </summary>
public static class MapEffects
{
    // ══════════════════════════════════════════════════════════════════
    //  FOG OF WAR EDGE GRADIENT
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns a fog-of-war edge character for tiles adjacent to unexplored areas.
    /// Explored tiles bordering unexplored get a dimmer treatment.
    /// Returns null if this tile is not on a fog edge.
    /// </summary>
    public static Color? GetFogEdgeColor(GameMap map, int mx, int my)
    {
        if (!map.IsExplored(mx, my)) return null;
        if (!map.IsVisible(mx, my)) return null;

        // Count adjacent unexplored tiles
        int unexploredNeighbors = 0;
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = mx + dx, ny = my + dy;
            if (!map.InBounds(nx, ny) || !map.IsExplored(nx, ny))
                unexploredNeighbors++;
        }

        // 2+ unexplored neighbors = outer fog edge, 1 = inner edge
        return unexploredNeighbors switch
        {
            >= 3 => Color.DarkGray,
            >= 1 => Color.Gray,
            _    => null
        };
    }

    /// <summary>
    /// Returns a fog density block character for tiles on the fog edge.
    /// Uses ░▒▓ gradient based on how many unexplored neighbors surround the tile.
    /// Returns null if this tile is not on a fog edge.
    /// </summary>
    public static char? GetFogDensityGlyph(GameMap map, int mx, int my)
    {
        if (!map.IsExplored(mx, my) || !map.IsVisible(mx, my)) return null;

        int unexploredNeighbors = 0;
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = mx + dx, ny = my + dy;
            if (!map.InBounds(nx, ny) || !map.IsExplored(nx, ny))
                unexploredNeighbors++;
        }

        // Denser block element = more fog neighbors (deeper into fog)
        return unexploredNeighbors switch
        {
            >= 5 => '▓',   // Thick fog — mostly surrounded by unexplored
            >= 3 => '▒',   // Medium fog
            >= 1 => '░',   // Light fog fringe
            _    => null
        };
    }

    // ══════════════════════════════════════════════════════════════════
    //  AMBIENT PARTICLES
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns a particle character and color for ambient effects on certain terrain.
    /// Uses position hash + AnimTick for sparse, slowly drifting particles.
    /// Returns null glyph if no particle should show this frame.
    /// </summary>
    public static (char Glyph, Color Color)? GetAmbientParticle(TileType type, int x, int y)
    {
        int hash = (x * 374761393 + y * 668265263) & 0x7FFFFFFF;
        int tick = TileDefinitions.AnimTick;

        // Only ~5% of eligible tiles show a particle at any time
        int slot = (hash + tick / 2) % 20;
        if (slot != 0) return null;

        return type switch
        {
            // Wind particles on grass — faint drifting dots
            TileType.Grass or TileType.GrassTall or TileType.GrassSparse
                => ('·', Color.DarkGray),

            // Mist near water — rising apostrophe
            TileType.Water or TileType.WaterDeep
                => ('\'', Color.Blue),

            // Embers near campfire — size-cycling Unicode particles
            TileType.Campfire
                => ((tick % 3) switch { 0 => '•', 1 => '∘', _ => '◦' }, Color.BrightYellow),

            _ => null
        };
    }

    // ══════════════════════════════════════════════════════════════════
    //  TERRAIN TRANSITION BORDERS
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Checks if a floor/path/grass tile borders water and returns a transition glyph.
    /// Creates smoother visual boundaries between biomes.
    /// </summary>
    public static (char Glyph, Color Color)? GetTransitionBorder(GameMap map, int x, int y, TileType type)
    {
        // Only ground tiles can show transitions
        if (type is not (TileType.Floor or TileType.Path or TileType.Grass
            or TileType.GrassSparse or TileType.GrassTall))
            return null;

        // Check if adjacent to water
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = x + dx, ny = y + dy;
            if (!map.InBounds(nx, ny)) continue;
            var adjType = map.GetTile(nx, ny).Type;
            if (adjType is TileType.Water or TileType.WaterDeep)
                return (',', Color.Blue);
            if (adjType is TileType.Lava)
                return (',', Color.Red);
        }

        return null;
    }

    // ══════════════════════════════════════════════════════════════════
    //  SHADOW CASTING
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns true if a visible non-wall tile has a wall directly above it,
    /// creating a 1-tile "shadow" effect below walls.
    /// </summary>
    public static bool IsInWallShadow(GameMap map, int x, int y)
    {
        var tile = map.GetTile(x, y);
        if (tile.Type == TileType.Wall || tile.Type == TileType.Mountain)
            return false;

        // Check tile above — if it's a wall, this tile is in shadow
        if (map.InBounds(x, y - 1))
        {
            var above = map.GetTile(x, y - 1).Type;
            return above == TileType.Wall;
        }
        return false;
    }

    // ══════════════════════════════════════════════════════════════════
    //  SHRINE AURA GLOW
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the distance to the nearest shrine, or -1 if none within radius 2.
    /// Used for mystical purple glow around shrines.
    /// </summary>
    public static int GetShrineDistance(GameMap map, int x, int y)
    {
        const int radius = 2;
        int closest = -1;
        for (int dx = -radius; dx <= radius; dx++)
        for (int dy = -radius; dy <= radius; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = x + dx, ny = y + dy;
            if (!map.InBounds(nx, ny)) continue;
            if (map.GetTile(nx, ny).Type == TileType.Shrine)
            {
                int dist = Math.Abs(dx) + Math.Abs(dy);
                if (closest < 0 || dist < closest)
                    closest = dist;
            }
        }
        return closest;
    }

    // ══════════════════════════════════════════════════════════════════
    //  WATER EDGE FOAM
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns true if this water tile is adjacent to land, indicating shoreline.
    /// Shoreline water tiles render with a lighter blue-gray tint.
    /// </summary>
    public static bool IsWaterEdge(GameMap map, int x, int y, TileType type)
    {
        if (type is not (TileType.Water or TileType.WaterDeep))
            return false;

        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = x + dx, ny = y + dy;
            if (!map.InBounds(nx, ny)) continue;
            var adj = map.GetTile(nx, ny).Type;
            if (adj is not (TileType.Water or TileType.WaterDeep or TileType.Wall or TileType.Mountain))
                return true;  // Adjacent to land
        }
        return false;
    }

    // ══════════════════════════════════════════════════════════════════
    //  ROOM LIGHTING VARIATION
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns a room-specific ambient tint color based on position hash.
    /// Groups of floor tiles in the same room area get a consistent subtle tint.
    /// Uses a coarse grid (8x8 regions) so nearby floor tiles share the same tint.
    /// </summary>
    public static Color? GetRoomTint(int x, int y, TileType type)
    {
        // Only tint floor tiles
        if (type != TileType.Floor) return null;

        // Coarse region hash — tiles in the same 8x8 area share a tint
        int regionX = x / 8;
        int regionY = y / 8;
        int regionHash = (regionX * 48271 + regionY * 96137) & 0x7FFFFFFF;

        return (regionHash % 5) switch
        {
            0 => Color.Gray,         // Neutral (no change from default)
            1 => Color.BrightYellow, // Warm gold — living areas
            2 => Color.BrightCyan,   // Cool blue — storage/caves
            3 => Color.BrightGreen,  // Green — gardens/overgrown
            _ => null                // No tint
        };
    }

    // ══════════════════════════════════════════════════════════════════
    //  DEPTH-BASED MAP TINTING
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Applies distance-based color cooling. Tiles further from the player
    /// within visibility range get progressively dimmer/cooler foreground.
    /// Returns a modified foreground color, or null if no change needed.
    /// </summary>
    public static Color? GetDepthTintedColor(Color originalFg, double falloff)
    {
        // Only apply in the middle range (0.45–0.75, before hard falloff kicks in)
        if (falloff < 0.45 || falloff >= 0.75) return null;

        // Map bright colors to their dim equivalents at distance
        if (originalFg == Color.White)        return Color.Gray;
        if (originalFg == Color.BrightGreen)  return Color.Green;
        if (originalFg == Color.BrightBlue)   return Color.Blue;
        if (originalFg == Color.BrightCyan)   return Color.Cyan;
        if (originalFg == Color.BrightYellow) return Color.Yellow;
        if (originalFg == Color.Green)        return Color.DarkGray;
        if (originalFg == Color.Gray)         return Color.DarkGray;
        return null;
    }

    // ══════════════════════════════════════════════════════════════════
    //  RAIN PARTICLE OVERLAY
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns a rain particle character for the given position when weather is Rain.
    /// Sparse falling drops that drift with animation ticks.
    /// </summary>
    public static (char Glyph, Color Color)? GetRainParticle(int x, int y)
    {
        if (WeatherSystem.Current != WeatherType.Rain) return null;

        int hash = (x * 374761393 + y * 668265263) & 0x7FFFFFFF;
        int tick = TileDefinitions.AnimTick;

        // ~8% of tiles show a raindrop at any time
        int slot = (hash + tick) % 12;
        if (slot != 0) return null;

        // Alternate between '|' and '/' for directional rain
        char drop = (tick + hash) % 3 == 0 ? '/' : '|';
        return (drop, Color.Blue);
    }

    // ══════════════════════════════════════════════════════════════════
    //  WEATHER COLOR DIMMING
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Dims/desaturates foreground colors during rain weather.
    /// Returns modified color or null if no change needed.
    /// </summary>
    public static Color? GetWeatherDimmedColor(Color originalFg)
    {
        if (WeatherSystem.Current != WeatherType.Rain) return null;

        // Desaturate bright colors to their dimmer equivalents
        if (originalFg == Color.BrightGreen)  return Color.Green;
        if (originalFg == Color.BrightYellow) return Color.Yellow;
        if (originalFg == Color.BrightCyan)   return Color.Cyan;
        if (originalFg == Color.White)        return Color.Gray;
        if (originalFg == Color.BrightRed)    return Color.Red;
        return null;
    }

    // ══════════════════════════════════════════════════════════════════
    //  WATER FLOW ANIMATION
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns an animated wave propagation glyph for water tiles.
    /// Waves ripple outward from a position hash, creating flow patterns.
    /// </summary>
    public static char GetWaterFlowGlyph(int x, int y)
    {
        int hash = (x * 374761393 + y * 668265263) & 0x7FFFFFFF;
        int tick = TileDefinitions.AnimTick;

        // Wave phase based on position + time — creates moving ripples
        int phase = (hash + tick + x + y) % 8;
        return phase switch
        {
            0 or 1 => '~',
            2 or 3 => '≈',
            4      => '~',
            _      => '~'
        };
    }

    // ══════════════════════════════════════════════════════════════════
    //  CAMPFIRE LIGHT FLICKER
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the pulsing light radius of a campfire (2 or 3 tiles).
    /// Alternates between radii to create a flickering glow effect.
    /// </summary>
    public static int GetCampfireFlickerRadius(int campfireX, int campfireY)
    {
        int hash = (campfireX * 48271 + campfireY * 96137) & 0x7FFFFFFF;
        int tick = TileDefinitions.AnimTick;
        // Pulse between radius 2 and 3 with per-campfire phase offset
        return ((tick + hash) % 6 < 4) ? 3 : 2;
    }

    /// <summary>
    /// Returns the nearest campfire distance considering flicker radius.
    /// Returns -1 if no campfire is within its current flicker radius.
    /// </summary>
    public static int GetCampfireGlowDistance(GameMap map, int x, int y)
    {
        const int maxRadius = 3;
        int closest = -1;
        for (int dx = -maxRadius; dx <= maxRadius; dx++)
        for (int dy = -maxRadius; dy <= maxRadius; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = x + dx, ny = y + dy;
            if (!map.InBounds(nx, ny)) continue;
            if (map.GetTile(nx, ny).Type == TileType.Campfire)
            {
                int dist = Math.Abs(dx) + Math.Abs(dy);
                int flickerRadius = GetCampfireFlickerRadius(nx, ny);
                if (dist <= flickerRadius && (closest < 0 || dist < closest))
                    closest = dist;
            }
        }
        return closest;
    }

    // ══════════════════════════════════════════════════════════════════
    //  MONSTER AWARENESS INDICATOR
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns true if an entity tile should show an aggro '!' indicator.
    /// Shows when the monster is within chase range and can see the player.
    /// </summary>
    public static bool ShouldShowAggroIndicator(GameMap map, int monsterX, int monsterY,
        int playerX, int playerY)
    {
        int dist = Math.Abs(monsterX - playerX) + Math.Abs(monsterY - playerY);
        // Show '!' for monsters within 6 tiles (SimpleAI chase range)
        return dist <= 6 && map.IsVisible(monsterX, monsterY);
    }

    // ══════════════════════════════════════════════════════════════════
    //  INTERACTABLE SPARKLE
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns a sparkle glyph for interactable tiles (doors, stairs, shrines, fountains, pillars).
    /// Pulses ✦/✧ every few animation frames, offset by position hash so they don't all blink together.
    /// Returns null when the sparkle should not show this frame.
    /// </summary>
    public static (char Glyph, Color Color)? GetInteractableSparkle(TileType type, int x, int y)
    {
        // Only interactable tiles sparkle
        if (type is not (TileType.Door or TileType.StairsUp
            or TileType.Shrine or TileType.Fountain or TileType.Pillar or TileType.Campfire))
            return null;

        int hash = (x * 374761393 + y * 668265263) & 0x7FFFFFFF;
        int tick = TileDefinitions.AnimTick;

        // Show sparkle ~25% of the time, offset per tile
        int slot = (hash + tick) % 8;
        if (slot > 1) return null;

        char glyph = slot == 0 ? '✦' : '✧';
        Color color = type switch
        {
            TileType.Shrine     => Color.BrightMagenta,
            TileType.Fountain   => Color.BrightCyan,
            TileType.Campfire   => Color.BrightYellow,
            TileType.StairsUp => Color.White,
            _                   => Color.BrightYellow,
        };
        return (glyph, color);
    }

    // ══════════════════════════════════════════════════════════════════
    //  WATER LILY PADS
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns a lily pad glyph for ~10% of shallow water tiles.
    /// Static decoration — uses position hash only, no animation.
    /// Returns null for non-eligible tiles.
    /// </summary>
    public static (char Glyph, Color Color)? GetWaterLilyPad(int x, int y)
    {
        int hash = (x * 374761393 + y * 668265263) & 0x7FFFFFFF;
        // ~10% of shallow water tiles get a lily pad
        if (hash % 10 != 0) return null;
        return ('●', Color.Green);
    }

    // ══════════════════════════════════════════════════════════════════
    //  LAVA DANGER ZONE
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns true if this tile is within 2 Manhattan distance of a lava tile.
    /// Used to tint the background red as a heat warning.
    /// </summary>
    public static bool IsInLavaDangerZone(GameMap map, int x, int y)
    {
        const int radius = 2;
        for (int dx = -radius; dx <= radius; dx++)
        for (int dy = -radius; dy <= radius; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            if (Math.Abs(dx) + Math.Abs(dy) > radius) continue;
            int nx = x + dx, ny = y + dy;
            if (!map.InBounds(nx, ny)) continue;
            if (map.GetTile(nx, ny).Type == TileType.Lava)
                return true;
        }
        return false;
    }

    // ══════════════════════════════════════════════════════════════════
    //  COBWEB DECORATIONS
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns a cobweb glyph for wall tiles that form corners (adjacent to 2+ non-wall tiles).
    /// Static decoration — ~15% of eligible corner walls show a gray '%' cobweb.
    /// Returns null for non-corner or non-selected tiles.
    /// </summary>
    public static (char Glyph, Color Color)? GetCobwebDecoration(GameMap map, int x, int y)
    {
        if (map.GetTile(x, y).Type != TileType.Wall) return null;

        int hash = (x * 374761393 + y * 668265263) & 0x7FFFFFFF;
        // ~15% of wall tiles
        if (hash % 7 > 0) return null;

        // Must be a corner — adjacent to at least 2 non-wall/non-mountain tiles
        int openCount = 0;
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = x + dx, ny = y + dy;
            if (!map.InBounds(nx, ny)) continue;
            var t = map.GetTile(nx, ny).Type;
            if (t != TileType.Wall && t != TileType.Mountain)
                openCount++;
        }
        if (openCount < 2) return null;
        return ('%', Color.DarkGray);
    }

    // ══════════════════════════════════════════════════════════════════
    //  EXPLORED ROOM GLOW
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns a warm floor tint for explored rooms where all adjacent tiles
    /// have been visited. Gives a "safe/familiar" feeling to well-explored areas.
    /// Only applies to visible floor tiles.
    /// </summary>
    public static Color? GetExploredRoomGlow(GameMap map, int x, int y)
    {
        if (map.GetTile(x, y).Type != TileType.Floor) return null;

        // Check if all 4 cardinal neighbors are explored — indicates well-explored room
        int exploredCount = 0;
        (int dx, int dy)[] dirs = { (0, -1), (0, 1), (-1, 0), (1, 0) };
        foreach (var (dx, dy) in dirs)
        {
            int nx = x + dx, ny = y + dy;
            if (!map.InBounds(nx, ny)) continue;
            if (map.IsExplored(nx, ny)) exploredCount++;
        }
        // All 4 neighbors explored = warm glow
        if (exploredCount >= 4) return Color.Yellow;
        return null;
    }

    // ══════════════════════════════════════════════════════════════════
    //  AMBIENT SOUND TEXT
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns a flavor text string for ambient sound near certain terrain.
    /// ~1% chance per turn. Scales down near large water bodies.
    /// Returns null if no sound should play this turn.
    /// </summary>
    public static string? GetAmbientSoundText(GameMap map, int playerX, int playerY)
    {
        // 1% base chance
        if (Random.Shared.Next(100) != 0) return null;

        // Check nearby tiles for sound sources (radius 3)
        bool nearWater = false, nearCampfire = false, nearWind = false;
        int waterCount = 0;

        for (int dx = -3; dx <= 3; dx++)
        for (int dy = -3; dy <= 3; dy++)
        {
            int nx = playerX + dx, ny = playerY + dy;
            if (!map.InBounds(nx, ny)) continue;
            var t = map.GetTile(nx, ny).Type;
            if (t is TileType.Water or TileType.WaterDeep)
            {
                nearWater = true;
                waterCount++;
            }
            if (t == TileType.Campfire) nearCampfire = true;
            if (t is TileType.Grass or TileType.GrassTall or TileType.Mountain)
                nearWind = true;
        }

        // Scale down near large water bodies — skip 50% if 10+ water tiles nearby
        if (nearWater && waterCount >= 10 && Random.Shared.Next(2) == 0)
            return null;

        // Pick a sound category and random flavor line
        if (nearCampfire)
        {
            string[] lines =
            {
                "...the campfire crackles softly.",
                "...embers drift upward in the still air.",
                "...warmth radiates from the nearby fire.",
            };
            return lines[Random.Shared.Next(lines.Length)];
        }
        if (nearWater)
        {
            string[] lines =
            {
                "...water laps gently against the shore.",
                "...you hear a faint trickling sound.",
                "...a soft current murmurs nearby.",
            };
            return lines[Random.Shared.Next(lines.Length)];
        }
        if (nearWind)
        {
            string[] lines =
            {
                "...a cool breeze rustles through the area.",
                "...wind whispers across the floor.",
                "...the air shifts quietly around you.",
            };
            return lines[Random.Shared.Next(lines.Length)];
        }

        return null;
    }

    // ══════════════════════════════════════════════════════════════════
    //  EXPLORED AREA HEATMAP
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns a heatmap color based on visit count for a tile.
    /// More visits = warmer color. Only affects floor/path tiles.
    /// </summary>
    public static Color? GetHeatmapColor(int visitCount, TileType type)
    {
        if (type is not (TileType.Floor or TileType.Path or TileType.Grass
            or TileType.GrassSparse or TileType.GrassTall))
            return null;

        if (visitCount <= 0) return null;
        if (visitCount >= 10) return Color.BrightRed;
        if (visitCount >= 5)  return Color.Red;
        if (visitCount >= 3)  return Color.Yellow;
        if (visitCount >= 1)  return Color.DarkGray;
        return null;
    }
}
