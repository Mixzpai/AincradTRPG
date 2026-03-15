using SAOTRPG.Entities;
using SAOTRPG.Map;

namespace SAOTRPG.Systems;

public static class SimpleAI
{
    /// <summary>
    /// Returns (dx, dy) for a movement, or null to signal "attack adjacent player".
    /// </summary>
    public static (int dx, int dy)? DecideAction(Monster monster, Player player, GameMap map,
        bool playerStealthed = false)
    {
        int distX = player.X - monster.X;
        int distY = player.Y - monster.Y;
        int chebyshev = Math.Max(Math.Abs(distX), Math.Abs(distY));

        // Per-mob aggro range (defaults to 6 for non-Mob monsters like bosses)
        int aggro = monster is Mob mob ? mob.AggroRange : 6;
        if (playerStealthed) aggro /= 2;  // stealth halves detection range

        // Flee when below 20% HP (run away from player)
        double hpPct = (double)monster.CurrentHealth / monster.MaxHealth;
        if (hpPct < 0.20 && chebyshev <= aggro)
        {
            var flee = FleeFrom(monster, player, map);
            // If can't flee and adjacent, fight desperately
            if (flee == (0, 0) && chebyshev == 1) return null;
            if (flee != (0, 0)) return flee;
        }

        // Adjacent to player (including diagonals) — attack
        if (chebyshev == 1)
            return null;

        // Aggro leashing — if mob is too far from spawn, return home instead of chasing
        if (monster is Mob m && chebyshev <= aggro)
        {
            int fromSpawn = Math.Max(Math.Abs(monster.X - m.SpawnX), Math.Abs(monster.Y - m.SpawnY));
            if (fromSpawn > m.LeashRange)
                return MoveTowardPoint(monster, m.SpawnX, m.SpawnY, map);
        }

        // Within aggro range — move toward player
        if (chebyshev <= aggro)
            return MoveToward(monster, player, map);

        // Out of range — wander randomly (50% chance to idle)
        if (Random.Shared.Next(2) == 0)
            return (0, 0); // idle

        return Wander(monster, map);
    }

    private static (int dx, int dy) FleeFrom(Monster monster, Player player, GameMap map)
    {
        // Move away from player — opposite direction
        int fleeX = -Math.Sign(player.X - monster.X);
        int fleeY = -Math.Sign(player.Y - monster.Y);

        // Try diagonal flee
        if (fleeX != 0 && fleeY != 0 && CanMoveTo(map, monster.X + fleeX, monster.Y + fleeY))
            return (fleeX, fleeY);

        // Try cardinal flee
        if (fleeX != 0 && CanMoveTo(map, monster.X + fleeX, monster.Y))
            return (fleeX, 0);
        if (fleeY != 0 && CanMoveTo(map, monster.X, monster.Y + fleeY))
            return (0, fleeY);

        return (0, 0); // cornered
    }

    private static (int dx, int dy) MoveToward(Monster monster, Player player, GameMap map)
    {
        int distX = player.X - monster.X;
        int distY = player.Y - monster.Y;
        int stepX = Math.Sign(distX);
        int stepY = Math.Sign(distY);

        // Try diagonal first if both axes have distance
        if (stepX != 0 && stepY != 0 && CanMoveTo(map, monster.X + stepX, monster.Y + stepY))
            return (stepX, stepY);

        // Fall back to cardinal — prefer the larger axis
        if (Math.Abs(distX) >= Math.Abs(distY))
        {
            if (stepX != 0 && CanMoveTo(map, monster.X + stepX, monster.Y))
                return (stepX, 0);
            if (stepY != 0 && CanMoveTo(map, monster.X, monster.Y + stepY))
                return (0, stepY);
        }
        else
        {
            if (stepY != 0 && CanMoveTo(map, monster.X, monster.Y + stepY))
                return (0, stepY);
            if (stepX != 0 && CanMoveTo(map, monster.X + stepX, monster.Y))
                return (stepX, 0);
        }

        return (0, 0); // stuck
    }

    /// <summary>Move toward an arbitrary point (used for leash return).</summary>
    private static (int dx, int dy) MoveTowardPoint(Monster monster, int tx, int ty, GameMap map)
    {
        int stepX = Math.Sign(tx - monster.X);
        int stepY = Math.Sign(ty - monster.Y);

        if (stepX != 0 && stepY != 0 && CanMoveTo(map, monster.X + stepX, monster.Y + stepY))
            return (stepX, stepY);
        if (stepX != 0 && CanMoveTo(map, monster.X + stepX, monster.Y))
            return (stepX, 0);
        if (stepY != 0 && CanMoveTo(map, monster.X, monster.Y + stepY))
            return (0, stepY);
        return (0, 0);
    }

    private static (int dx, int dy) Wander(Monster monster, GameMap map)
    {
        int[] dirs = { 0, 1, 2, 3, 4, 5, 6, 7 };
        Shuffle(dirs);

        foreach (int dir in dirs)
        {
            var (dx, dy) = dir switch
            {
                0 => ( 0, -1),  // N
                1 => ( 0,  1),  // S
                2 => (-1,  0),  // W
                3 => ( 1,  0),  // E
                4 => (-1, -1),  // NW
                5 => ( 1, -1),  // NE
                6 => (-1,  1),  // SW
                _ => ( 1,  1),  // SE
            };

            if (CanMoveTo(map, monster.X + dx, monster.Y + dy))
                return (dx, dy);
        }

        return (0, 0);
    }

    private static bool CanMoveTo(GameMap map, int x, int y)
    {
        if (!map.InBounds(x, y)) return false;
        var tile = map.GetTile(x, y);
        return !tile.BlocksMovement && tile.Occupant == null;
    }

    private static void Shuffle(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}
