using SAOTRPG.Entities;
using SAOTRPG.Map;
using SAOTRPG.Map.Lighting;
using SAOTRPG.Systems.RunState;

namespace SAOTRPG.Systems.Turns;

// Simple state AI: flee (<20% HP), attack (adj), chase (in aggro), wander.
// Chebyshev dist, 8-dir. Stealth halves aggro; leash returns to spawn.
public static class SimpleAI
{
    // ── 8-direction offsets ──────────
    private static readonly (int dx, int dy)[] Directions =
    {
        ( 0, -1), ( 0, 1), (-1, 0), ( 1, 0),
        (-1, -1), ( 1, -1), (-1, 1), ( 1, 1),
    };

    // What a monster with no per-mob aggro range of its own uses — bosses, and anything
    // deriving from Monster rather than Mob.
    public const int DefaultAggroRange = 6;

    // How much further a monster notices you in the dark. The night costs the player sight
    // (VisibilityRadius falls to MinVisibility), and this widens a monster's reach to match.
    //
    // Rounded rather than truncated: aggro ranges are small integers, so a 15% bonus is a
    // fraction of a tile for most of them and truncating would leave every range under 7
    // unchanged. Rounded, it is +1 tile for ranges 4 through 9 and +2 for 10 and 12, and nothing
    // for 2 and 3, where any extra tile would be a +33% to +50% jump.
    private const float NightAggroBonus = 0.15f;

    // The aggro range a monster is using this turn. Both readers go through here — the decision
    // below and TurnManager's sound cues, which derive their audible radius from it — so the two
    // cannot disagree.
    public static int EffectiveAggroRange(Monster monster) =>
        EffectiveAggroRange(monster as Mob);

    private static int EffectiveAggroRange(Mob? mob)
    {
        int baseRange = mob?.AggroRange ?? DefaultAggroRange;
        if (!DayNightCycle.IsNight) return baseRange;
        return (int)MathF.Round(baseRange * (1f + NightAggroBonus));
    }

    // Main AI decision for a single monster. Returns (dx, dy) for movement,
    // or null to signal "attack adjacent player".
    public static (int dx, int dy)? DecideAction(Monster monster, Player player, GameMap map,
        bool playerStealthed = false)
    {
        int mx = monster.X, my = monster.Y;
        int distX = player.X - mx;
        int distY = player.Y - my;
        int adx = distX < 0 ? -distX : distX;
        int ady = distY < 0 ? -distY : distY;
        int chebyshev = adx > ady ? adx : ady;

        // One type test shared by aggro range, attack range and the leash: this runs once per
        // monster per turn.
        var mob = monster as Mob;

        int aggro = EffectiveAggroRange(mob);
        if (playerStealthed) aggro /= 2;  // stealth halves detection range
        bool inAggro = chebyshev <= aggro;

        // Flee when below 20% HP (run away from player) — enraged bosses never flee.
        // `CurrentHealth * 5 < MaxHealth` is the same predicate as `(double)CurrentHealth /
        // MaxHealth < 0.20` for every value the game can hold: the quotient is never within
        // 2^-53 of the literal, and the degenerate MaxHealth == 0 case answers false either way.
        // The Boss test is inside the condition so it is only reached by a monster already low
        // enough to consider fleeing.
        if (inAggro && monster.CurrentHealth * 5 < monster.MaxHealth
            && !(monster is Boss boss && boss.IsEnraged))
        {
            var flee = FleeFrom(monster, player, map);
            // If can't flee and adjacent, fight desperately
            if (flee == (0, 0) && chebyshev == 1) return null;
            if (flee != (0, 0)) return flee;
        }

        // Within attack range — attack (melee = 1, ranged 2-4; a ranged shot needs line of sight)
        int atkRange = mob != null ? mob.AttackRange : 1;
        if (chebyshev <= 1 || (chebyshev <= atkRange && map.IsVisible(mx, my)))
            return null;

        // Aggro leashing — if mob is too far from spawn, return home instead of chasing
        if (mob != null && inAggro)
        {
            int sx = mx - mob.SpawnX, sy = my - mob.SpawnY;
            if (sx < 0) sx = -sx;
            if (sy < 0) sy = -sy;
            if ((sx > sy ? sx : sy) > mob.LeashRange)
                return MoveTowardPoint(monster, mob.SpawnX, mob.SpawnY, map);
        }

        // Within aggro range — move toward player
        if (inAggro)
            return MoveToward(monster, player, map);

        // Out of range — wander randomly (50% chance to idle)
        if (RunRng.Next(2) == 0)
            return (0, 0); // idle

        return Wander(monster, map);
    }

    // Move in the opposite direction from the player. Tries diagonal, then cardinal.
    private static (int dx, int dy) FleeFrom(Monster monster, Player player, GameMap map)
    {
        // Move away from player — opposite direction
        int fleeX = -Math.Sign(player.X - monster.X);
        int fleeY = -Math.Sign(player.Y - monster.Y);

        // Try diagonal flee
        if (fleeX != 0 && fleeY != 0 && CanMoveTo(map, monster, monster.X + fleeX, monster.Y + fleeY))
            return (fleeX, fleeY);

        // Try cardinal flee
        if (fleeX != 0 && CanMoveTo(map, monster, monster.X + fleeX, monster.Y))
            return (fleeX, 0);
        if (fleeY != 0 && CanMoveTo(map, monster, monster.X, monster.Y + fleeY))
            return (0, fleeY);

        return (0, 0); // cornered
    }

    // Step toward the player — diagonal first, then cardinal along the larger axis.
    private static (int dx, int dy) MoveToward(Monster monster, Player player, GameMap map)
    {
        int distX = player.X - monster.X;
        int distY = player.Y - monster.Y;
        int stepX = Math.Sign(distX);
        int stepY = Math.Sign(distY);

        // Try diagonal first if both axes have distance
        if (stepX != 0 && stepY != 0 && CanMoveTo(map, monster, monster.X + stepX, monster.Y + stepY))
            return (stepX, stepY);

        // Fall back to cardinal — prefer the larger axis
        if (Math.Abs(distX) >= Math.Abs(distY))
        {
            if (stepX != 0 && CanMoveTo(map, monster, monster.X + stepX, monster.Y))
                return (stepX, 0);
            if (stepY != 0 && CanMoveTo(map, monster, monster.X, monster.Y + stepY))
                return (0, stepY);
        }
        else
        {
            if (stepY != 0 && CanMoveTo(map, monster, monster.X, monster.Y + stepY))
                return (0, stepY);
            if (stepX != 0 && CanMoveTo(map, monster, monster.X + stepX, monster.Y))
                return (stepX, 0);
        }

        return (0, 0); // stuck
    }

    // Move toward an arbitrary point (used for leash return).
    private static (int dx, int dy) MoveTowardPoint(Monster monster, int tx, int ty, GameMap map)
    {
        int stepX = Math.Sign(tx - monster.X);
        int stepY = Math.Sign(ty - monster.Y);

        if (stepX != 0 && stepY != 0 && CanMoveTo(map, monster, monster.X + stepX, monster.Y + stepY))
            return (stepX, stepY);
        if (stepX != 0 && CanMoveTo(map, monster, monster.X + stepX, monster.Y))
            return (stepX, 0);
        if (stepY != 0 && CanMoveTo(map, monster, monster.X, monster.Y + stepY))
            return (0, stepY);
        return (0, 0);
    }

    // Pick a random walkable direction from the 8 cardinal/diagonal options.
    private static (int dx, int dy) Wander(Monster monster, GameMap map)
    {
        // Stack buffer, not a heap array: this runs once per monster per turn for every mob out
        // of aggro range. The sibling shufflers in TurnManager.AI do the same.
        Span<int> indices = stackalloc int[8];
        for (int i = 0; i < 8; i++) indices[i] = i;
        Shuffle(indices);

        int mx = monster.X, my = monster.Y;
        var dirs = Directions;
        for (int k = 0; k < 8; k++)
        {
            var (dx, dy) = dirs[indices[k]];
            if (CanMoveTo(map, monster, mx + dx, my + dy))
                return (dx, dy);
        }

        return (0, 0);
    }

    // In-bounds + walkable + unoccupied. Aquatic mobs (Monster.CanSwim) also
    // accept Water + WaterDeep tiles.
    private static bool CanMoveTo(GameMap map, Monster monster, int x, int y)
    {
        if (!map.InBounds(x, y)) return false;
        // RefTile, not GetTile: only three fields are read, so copying the whole 16-byte struct
        // out of the grid is wasted work on one of the hottest map lookups.
        ref var tile = ref map.RefTile(x, y);
        if (tile.Occupant != null) return false;
        if (!tile.BlocksMovement) return true;
        if (monster.CanSwim && tile.Type is TileType.Water or TileType.WaterDeep)
            return true;
        return false;
    }

    // Fisher-Yates in-place shuffle for direction randomization.
    // Span so the caller can shuffle a stack buffer. The draw order is part of the seeded RunRng
    // stream, so changing it changes which direction a mob picks.
    private static void Shuffle(Span<int> array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = RunRng.Next(i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}
