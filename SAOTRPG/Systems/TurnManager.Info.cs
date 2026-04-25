using SAOTRPG.Entities;
using SAOTRPG.Map;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.Systems;

// Info queries and display helpers — tile inspection, context hints, stat blocks.
public partial class TurnManager
{
    // The active floor map. Swapped on floor transitions.
    public GameMap Map => _map;

    // Turns elapsed on the current floor (resets each floor change).
    public int FloorTurns => TurnCount - _floorStartTurn;

    // Par turn count for the given floor (speed bonus threshold).
    public static int GetFloorPar(int floor) =>
        FloorParTurns[Math.Min(floor - 1, FloorParTurns.Length - 1)];

    // Full tile inspection — occupants, terrain, items.
    public string GetTileInfo(int x, int y)
    {
        if (!_map.InBounds(x, y)) return "Out of bounds";
        var tile = _map.GetTile(x, y);
        if (x == _player.X && y == _player.Y)
            return $"You — Lv.{_player.Level} HP:{_player.CurrentHealth}/{_player.MaxHealth} ATK:{_player.Attack} DEF:{_player.Defense}";
        if (tile.Occupant != null && !tile.Occupant.IsDefeated)
            return DescribeOccupant(tile.Occupant);
        string terrain = DescribeTerrain(tile);
        if (tile.HasItems) terrain += $" [{tile.Items.Count} item(s)]";
        return terrain;
    }

    private string DescribeOccupant(Entity e)
    {
        if (e is Monster m)
        {
            string threat = GetThreatLabel(m.Level - _player.Level);
            string abilities = GetMobAbilityTags(m);
            string status = GetMobStatusTags(m);
            string hpBar = BarBuilder.BuildGradient(m.CurrentHealth, m.MaxHealth, 10);
            return $"{m.Name} (Lv.{m.Level}) {threat} {hpBar} ATK:{m.BaseAttack} DEF:{m.BaseDefense}{abilities}{status}";
        }
        if (e is Vendor v) return $"{v.Name} — [{v.ShopName ?? "Shop"}] {v.ShopStock.Count} items | Bump to browse";
        if (e is WorldSpawn ws) return $"{ws.Name} — Has dialogue | Bump to talk";
        if (e is NPC npc) return $"{npc.Name} — Bump to talk";
        return e.Name;
    }

    private static string GetThreatLabel(int levelDiff) => levelDiff switch
    {
        >= 5  => "[!! DEADLY !!]",
        >= 3  => "[! Dangerous]",
        >= 1  => "[Strong]",
        0     => "[Even match]",
        >= -2 => "[Weaker]",
        _     => "[Trivial]",
    };

    private static string GetMobAbilityTags(Monster m)
    {
        if (m is not Mob mob) return "";
        var tags = new List<string>();
        if (mob.CanPoison) tags.Add("Poison");
        if (mob.CanBleed) tags.Add("Bleed");
        if (mob.CanStun) tags.Add("Stun");
        if (mob.CanSlow) tags.Add("Slow");
        return tags.Count > 0 ? " [" + string.Join(",", tags) + "]" : "";
    }

    private static string GetMobStatusTags(Monster m)
    {
        string s = "";
        if (m is Boss boss && boss.IsEnraged) s += " [ENRAGED]";
        if (m is Mob afxMob && afxMob.Affix != null) s += $" [{afxMob.Affix.ToUpper()}]";
        return s;
    }

    private string DescribeTerrain(Tile tile) => tile.Type switch
    {
        TileType.Grass or TileType.GrassTall or TileType.GrassSparse => "Grassland",
        TileType.Flowers     => "Wildflowers",
        TileType.Path        => "Dirt path",
        TileType.Floor       => "Stone floor",
        TileType.Wall        => "Stone wall",
        TileType.Water or TileType.WaterDeep => "Water",
        TileType.Tree or TileType.TreePine   => "Tree",
        TileType.Bush        => "Bush",
        TileType.Mountain    => "Mountain rock",
        TileType.Rock        => "Boulder",
        TileType.Door        => "Doorway",
        TileType.StairsUp    => _map.Entities.Any(e => e is Boss && !e.IsDefeated)
            ? "Stairs leading up [SEALED — defeat the Boss]"
            : "Stairs leading up [OPEN]",
        TileType.TrapSpike     => "Suspicious ground...",
        TileType.TrapTeleport  => "Strange markings on the ground...",
        TileType.TrapPoison    => "The air smells faintly of chemicals...",
        TileType.TrapAlarm     => "A thin wire stretches across the floor...",
        TileType.CrackedWall   => "The wall looks cracked here...",
        TileType.Lava          => "Molten lava — standing here deals damage!",
        TileType.Campfire      => "A warm campfire (heals HP, cures status)",
        TileType.Fountain      => "A crystal fountain (step on to heal + restore hunger)",
        TileType.Shrine        => "An ancient shrine (step on for a temporary buff)",
        TileType.Pillar        => "A glowing pillar (step on to reveal the map)",
        TileType.Chest         => "A treasure chest! (step on to open)",
        TileType.ChestOpened   => "An opened treasure chest (empty)",
        TileType.LoreStone     => "A mysterious stone tablet... (step on to read)",
        TileType.DangerZone    => "Corrupted ground — standing here hurts!",
        TileType.Anvil         => "A repair anvil (step on to repair gear for Col)",
        TileType.BountyBoard   => "A bounty board (step on for a kill contract)",
        TileType.Sand          => "Loose sand — easy footing, still hot.",
        TileType.DuneSand      => "Wind-carved dune — the ridge shifts underfoot.",
        TileType.Snow          => "Fresh snow — your steps leave a print.",
        TileType.Ice           => "Smooth ice — slick but solid.",
        TileType.CrackedIce    => "Cracked ice — may slip (Stun) on entry!",
        TileType.Basalt        => "Cooled basalt — dark volcanic rock.",
        TileType.Ash           => "Volcanic ash — warm, soft, drifts with each step.",
        TileType.Mud           => "Thick mud — slows your steps (Slow on entry).",
        TileType.BogWater      => "Fetid bogwater — requires Swimming 1; poisons on entry!",
        TileType.Reeds         => "Tall reeds — marsh fringe, easy to push through.",
        _                      => "Unknown terrain"
    };

    // Compass direction toward nearest monster (HUD display).
    public string GetDangerCompass()
    {
        Monster? closest = null;
        int closestDist = int.MaxValue;
        foreach (var entity in _map.Entities)
        {
            if (entity == _player || entity.IsDefeated || entity is not Monster m) continue;
            int dist = Math.Abs(m.X - _player.X) + Math.Abs(m.Y - _player.Y);
            if (dist < closestDist) { closest = m; closestDist = dist; }
        }
        if (closest == null) return "";
        int dx = closest.X - _player.X, dy = closest.Y - _player.Y;
        string dir = (dx, dy) switch
        {
            ( > 0,  < 0) => "NE", ( < 0,  < 0) => "NW",
            ( > 0,  > 0) => "SE", ( < 0,  > 0) => "SW",
            (   0,  < 0) => "N",  (   0,  > 0) => "S",
            ( > 0,    0) => "E",  ( < 0,    0) => "W",
            _ => "?"
        };
        return $"Threat: {dir}({closestDist})";
    }

    // Contextual action hint based on surroundings.
    public string GetContextHint()
    {
        var tile = _map.GetTile(_player.X, _player.Y);
        if (tile.HasItems) return "[G] Pick up";
        if (tile.Type == TileType.StairsUp) return "[Bump] Ascend";
        if (tile.Type == TileType.Campfire) return "Resting...";

        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = _player.X + dx, ny = _player.Y + dy;
            if (!_map.InBounds(nx, ny)) continue;
            var adj = _map.GetTile(nx, ny);
            if (adj.Occupant is Vendor) return "[Bump] Shop";
            if (adj.Occupant is Monster m && !m.IsDefeated) return "[Bump] Attack";
            if (adj.Type == TileType.Campfire && _player.CurrentHealth < _player.MaxHealth)
            {
                int heal = Math.Min(15 + CurrentFloor * 5, _player.MaxHealth - _player.CurrentHealth);
                return $"[Walk] Campfire (+{heal} HP)";
            }
        }
        return "";
    }

    // Count of living monsters on the current floor.
    public int GetMonsterCount() =>
        _map.Monsters.Count(m => !m.IsDefeated);

    // Floor danger label based on average monster level vs player.
    public string GetFloorDangerLabel()
    {
        var monsters = _map.Monsters.Where(m => !m.IsDefeated).ToList();
        if (monsters.Count == 0) return "Cleared";
        double avgLevel = monsters.Average(m => (double)m.Level);
        int diff = (int)Math.Round(avgLevel) - _player.Level;
        return diff switch
        {
            >= 5  => "Deadly",  >= 3 => "Dangerous", >= 1 => "Hard",
            0     => "Normal",  >= -2 => "Safe",     _    => "Trivial",
        };
    }

    // Visible monsters sorted by distance (nearest first).
    public List<Monster> GetVisibleMonsters() => _map.Monsters
        .Where(m => !m.IsDefeated && _map.IsVisible(m.X, m.Y))
        .OrderBy(m => Math.Max(Math.Abs(m.X - _player.X), Math.Abs(m.Y - _player.Y)))
        .ToList();

    private static string BuildMobHpBar(int current, int max, int width)
    {
        int filled = max > 0 ? (int)Math.Round((double)current / max * width) : 0;
        filled = Math.Clamp(filled, 0, width);
        return "[" + new string('#', filled) + new string('-', width - filled) + "]";
    }
}
