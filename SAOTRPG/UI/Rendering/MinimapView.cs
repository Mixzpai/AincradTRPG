using SAOTRPG.Entities;
using SAOTRPG.Map;

namespace SAOTRPG.UI.Rendering;

// The full map's (WorldMapView) sampler and legend: a scaled cell of the floor shows its most
// important thing, occupant, then landmark, then terrain. The sidebar minimap is OvermapView.
public static class MinimapView
{
    public const char PlayerMarker = '@';
    private const char EntityMarker = '*', ItemMarker = '!';
    private const char DoorMarker = '+', StairsMarker = '>', ShrineMarker = 'S', CampfireMarker = '&';
    // A boss is the one occupant worth re-routing around, so it does not share the monster
    // marker. It must not share the mithril vein's glyph either: colour is the only thing
    // separating them and Amber Mono collapses colour.
    private const char BossMarker = 'Ω';
    // A secret shrine has its own marker, distinct from the dropped-item marker.
    private const char SecretMarker = '?';
    // Friend and foe is the one occupant distinction on this widget worth a glyph of its own:
    // allies and townspeople told apart from monsters by colour alone would not survive a theme
    // that collapses colour.
    private const char FriendMarker = 'n';
    // Open-ground landmarks: exactly what a player crosses a floor for.
    private const char ChestMarker = '$';
    private const char AnvilMarker = 'A';
    private const char EnchantMarker = 'E';
    private const char LoreMarker = 'L';
    // Depleted veins deliberately get no marker — a spent vein is not a destination.
    private const char OreMarker = '◊';        // Iron, matching the main map
    private const char OreMithrilMark = '◆';   // Mithril
    private const char OreDivineMark = '◈';    // Divine

    // Single-pass sampler: best-ranked visible entity/item, best-ranked explored landmark,
    // any-explored/any-visible flags. Priority: entity/item > landmark > center-tile terrain.
    // The reference for the incremental cache, and the full map's sampler at any scale.
    public static (char ch, Color fg) SampleRegion(GameMap map, int playerLevel, int startX, int startY, int width, int height)
    {
        bool anyExplored = false, anyVisible = false;

        char entityCh = ' '; Color entityFg = Color.Black; int entityRank = int.MaxValue;
        (char Ch, Color Fg) landmark = (' ', Color.Black); int landmarkRank = int.MaxValue;

        int cx = Math.Min(startX + width / 2, map.Width - 1);
        int cy = Math.Min(startY + height / 2, map.Height - 1);

        int xMax = Math.Min(startX + width, map.Width);
        int yMax = Math.Min(startY + height, map.Height);

        for (int x = startX; x < xMax; x++)
        for (int y = startY; y < yMax; y++)
        {
            bool explored = map.IsExplored(x, y);
            if (!explored) continue;
            anyExplored = true;
            bool visible = map.IsVisible(x, y);
            if (visible) anyVisible = true;

            // Fetched once: the entity and landmark checks below both need it.
            var tile = map.GetTile(x, y);

            // Best-ranked visible entity wins: boss, then monster, then ally/NPC, then item.
            if (entityRank > 0 && visible)
            {
                var occupant = tile.Occupant;
                if (occupant != null && !occupant.IsDefeated)
                {
                    int r = OccupantRank(occupant, playerLevel, out char oc, out Color of);
                    if (r < entityRank) { entityRank = r; entityCh = oc; entityFg = of; }
                }
                if (entityRank > 3 && map.HasItemsAt(x, y))
                { entityCh = ItemMarker; entityFg = Color.BrightYellow; entityRank = 3; }
            }

            // Best-ranked explored landmark wins (lowest rank); on a tie, the first the scan meets.
            if (landmarkRank > 0)
            {
                int rank = LandmarkRank(tile.Type, out char landmarkCh, out Color landmarkFg);
                if (rank < landmarkRank) { landmarkRank = rank; landmark = (landmarkCh, landmarkFg); }
            }
        }

        if (!anyExplored) return (' ', Color.Black);

        if (entityRank != int.MaxValue) return (entityCh, entityFg);
        if (landmarkRank != int.MaxValue) return landmark;

        if (!map.InBounds(cx, cy)) return (' ', Color.Black);
        var centerTile = map.GetTile(cx, cy);
        char glyph = GetTerrainGlyph(centerTile.Type);
        Color color = anyVisible ? GetTerrainColor(centerTile.Type) : Color.DarkGray;
        return (glyph, color);
    }

    // The landmark a tile type marks, and its rank (lower wins); int.MaxValue when none.
    public static int LandmarkRank(TileType type, out char landmarkCh, out Color landmarkFg)
    {
        landmarkCh = ' '; landmarkFg = Color.Black; int rank = int.MaxValue;
        switch (type)
        {
            case TileType.StairsUp:          landmarkCh = StairsMarker;   landmarkFg = Color.BrightCyan;    rank = 0; break;
            case TileType.LabyrinthEntrance: landmarkCh = 'Π';            landmarkFg = Color.BrightCyan;    rank = 0; break;
            case TileType.Door:              landmarkCh = DoorMarker;     landmarkFg = Color.BrightYellow;  rank = 1; break;
            case TileType.Shrine:
            case TileType.Fountain:          landmarkCh = ShrineMarker;   landmarkFg = Color.BrightMagenta; rank = 2; break;
            case TileType.SecretShrine:      landmarkCh = SecretMarker;   landmarkFg = Color.BrightMagenta; rank = 3; break;
            case TileType.Campfire:          landmarkCh = CampfireMarker; landmarkFg = Color.Yellow;        rank = 4; break;
            // Ore veins rank below navigation: a vein is a detour. Depleted veins get no
            // marker. Per-tier glyphs, mirroring the main map, because Amber Mono
            // collapses the colour that would otherwise be the only tier cue.
            case TileType.OreVeinIron:       landmarkCh = OreMarker;      landmarkFg = Color.Gray;          rank = 5; break;
            case TileType.OreVeinMithril:    landmarkCh = OreMithrilMark; landmarkFg = Color.BrightCyan;    rank = 6; break;
            case TileType.OreVeinDivine:     landmarkCh = OreDivineMark;  landmarkFg = Color.BrightMagenta; rank = 7; break;
            case TileType.Chest:             landmarkCh = ChestMarker;    landmarkFg = Color.BrightYellow;  rank = 8; break;
            case TileType.Anvil:             landmarkCh = AnvilMarker;    landmarkFg = Color.BrightYellow;  rank = 9; break;
            case TileType.EnchantShrine:     landmarkCh = EnchantMarker;  landmarkFg = Color.BrightYellow;  rank = 10; break;
            case TileType.LoreStone:
            case TileType.Journal:           landmarkCh = LoreMarker;     landmarkFg = Color.BrightCyan;    rank = 11; break;
        }
        return rank;
    }

    // The marker an occupant paints, and its rank (lower wins); int.MaxValue when it paints none.
    public static int OccupantRank(Entity occ, int playerLevel, out char entityCh, out Color entityFg)
    {
        entityCh = ' '; entityFg = Color.Black;
        if (occ is Boss) { entityCh = BossMarker; entityFg = Color.BrightRed; return 0; }
        if (occ is Monster m)
        {
            Color threatColor = (m.Level - playerLevel) switch
            {
                >= 3 => Color.BrightRed,
                >= 0 => Color.Red,
                _    => Color.DarkGray,
            };
            entityCh = EntityMarker; entityFg = threatColor; return 1;
        }
        if (occ is Ally) { entityCh = FriendMarker; entityFg = Color.BrightGreen; return 2; }
        if (occ is NPC) { entityCh = FriendMarker; entityFg = Color.BrightCyan; return 2; }
        return int.MaxValue;
    }

    // This widget's picture depends on one glyph per meaning, and nothing here may rely on colour
    // to separate two things — an accessibility theme is allowed to collapse hue (Amber Mono maps
    // the world to luma), so every mark must stay distinguishable in monochrome. `Legend` below
    // is the single list of marks, and the verification probes hold it to the paint sites in both
    // directions: every legend glyph is painted, every painted glyph is explained, and no glyph
    // carries two meanings.
    // The minimap's own legend, declared once so the Player Guide can render it rather than
    // transcribe it.
    //
    // Only the glyphs a player needs to read a route are listed: the terrain that stops or slows
    // them, and the landmarks worth walking to.
    public static readonly (char Glyph, string Meaning)[] Legend =
    {
        ('@', "You"),
        ('*', "A monster"),
        ('n', "An ally or NPC"),
        ('!', "A dropped item"),
        ('Ω', "A boss — the floor boss, or a field elite"),
        ('>', "Stairs up — the way off this floor"),
        ('\u03a0', "The labyrinth archway"),
        ('+', "A door"),
        ('S', "A shrine or fountain"),
        ('?', "A secret shrine"),
        ('&', "A campfire — rest and cook here"),
        ('$', "An unopened chest"),
        ('A', "An anvil — repair and smith here"),
        ('E', "An enchanting shrine"),
        ('L', "A lore stone or journal"),
        ('\u25ca', "An iron ore vein"),
        ('\u25c6', "A mithril ore vein"),
        ('\u25c8', "A divine ore vein, the richest tier"),
        ('\u2248', "Shallow water — crossable from Swimming L1"),
        ('\u2261', "Deep water — needs Swimming L25"),
        ('\u00a7', "Lava — it will hurt"),
        ('%', "Corrupted ground — it burns you every step"),
        ('\u223d', "Bogwater — wading it poisons you"),
        ('~', "Mud — it slows you"),
        ('^', "Mountain"),
        ('T', "Trees"),
        ('"', "Bushes"),
        ('o', "Loose rock"),
        ('P', "A pillar"),
        ('.', "Open ground"),
        ('\u25aa', "Wall"),
    };

    // What a legend glyph means, up to its first clause ("Shallow water"); null when unlisted.
    public static string? Meaning(char glyph)
    {
        foreach (var (g, meaning) in Legend)
            if (g == glyph)
            {
                int cut = meaning.IndexOf(" — ", StringComparison.Ordinal);
                return cut < 0 ? meaning : meaning[..cut];
            }
        return null;
    }

    private static char GetTerrainGlyph(TileType type) => type switch
    {
        TileType.Wall => '▪', TileType.Mountain => '^',
        // Shallow and deep must differ: WaterDeep is a Swimming L25 wall, and a player reading
        // the minimap to plan a route needs to know which water they can actually cross, and
        // colour alone does not survive Amber Mono, which collapses it to luma.
        TileType.Water => '≈',
        TileType.WaterDeep => '≡',
        TileType.Tree or TileType.TreePine => 'T', TileType.Bush => '"',
        TileType.StairsUp => StairsMarker, TileType.Door => DoorMarker,
        TileType.Path or TileType.Floor => '.', TileType.Rock => 'o',
        TileType.Campfire => CampfireMarker, TileType.Fountain => ShrineMarker,
        TileType.Shrine => ShrineMarker, TileType.Pillar => 'P', TileType.Lava => '§',
        // Hazards are marked so a player plotting a path across the minimap never sees clear
        // ground where corrupted ground would burn them every step or bogwater would poison them.
        // Same marks as the main map, so the two surfaces read alike.
        TileType.DangerZone => '%', TileType.BogWater => '∽', TileType.Mud => '~',
        TileType.TrapSpike or TileType.TrapTeleport => '.',
        TileType.Grass or TileType.GrassTall or TileType.GrassSparse or TileType.Flowers => ' ',
        _ => ' ',
    };

    private static Color GetTerrainColor(TileType type) => type switch
    {
        TileType.Wall => Color.White, TileType.Mountain => Color.Gray,
        TileType.Water or TileType.WaterDeep => Color.BrightBlue,
        TileType.Tree or TileType.TreePine or TileType.Bush => Color.Green,
        TileType.StairsUp => Color.BrightCyan,
        TileType.Door or TileType.Path => Color.BrightYellow, TileType.Floor => Color.Gray,
        TileType.Rock => Color.DarkGray, TileType.Flowers => Color.BrightMagenta,
        TileType.Campfire => Color.Yellow, TileType.Fountain => Color.BrightCyan,
        TileType.Shrine => Color.BrightMagenta, TileType.Pillar => Color.White,
        TileType.Lava => Color.BrightRed,
        _ => Color.DarkGray,
    };
}
