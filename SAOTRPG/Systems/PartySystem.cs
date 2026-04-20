using SAOTRPG.Entities;
using SAOTRPG.Map;
using SAOTRPG.UI;
using Terminal.Gui;

namespace SAOTRPG.Systems;

// Up to 2 AI allies. Follow + auto-attack; recruited via named NPC dialog.
public static class PartySystem
{
    public static List<Ally> Members { get; set; } = new();
    public const int MaxPartySize = 2;

    // Recruit an ally from an NPC interaction. Returns true if recruited.
    public static bool TryRecruit(string name, char symbol, Color color,
        string weaponType, string title, int playerLevel, IGameLog log)
    {
        if (Members.Count >= MaxPartySize)
        {
            log.Log("Your party is full (max 2 companions).");
            return false;
        }
        if (Members.Any(a => a.Name == name))
        {
            log.Log($"{name} is already in your party.");
            return false;
        }

        var ally = new Ally(symbol, color)
        {
            Name = name, Title = title, WeaponType = weaponType,
            Level = Math.Max(1, playerLevel - 1),
            BaseAttack = 5 + playerLevel * 2,
            BaseDefense = 3 + playerLevel,
            BaseSpeed = 4 + playerLevel,
            Vitality = 3 + playerLevel,
            Strength = 3 + playerLevel,
            Endurance = 2 + playerLevel,
            Dexterity = 2 + playerLevel,
            Agility = 2 + playerLevel,
            Intelligence = 1 + playerLevel,
        };
        ally.MaxHealth = 80 + playerLevel * 8;
        ally.CurrentHealth = ally.MaxHealth;
        Members.Add(ally);
        log.LogSystem($"{name} has joined your party!");
        return true;
    }

    // Place all party members near the player on map load.
    public static void PlaceAllies(GameMap map, int playerX, int playerY)
    {
        foreach (var ally in Members)
        {
            // Find an open adjacent tile
            for (int r = 1; r <= 3; r++)
            for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                int nx = playerX + dx, ny = playerY + dy;
                if (!map.InBounds(nx, ny)) continue;
                var t = map.GetTile(nx, ny);
                if (t.BlocksMovement || t.Occupant != null) continue;
                map.PlaceEntity(ally, nx, ny);
                goto placed;
            }
            placed:;
        }
    }

    // Process one ally's turn. Called from TurnManager.ProcessEntityTurns.
    public static void ProcessAllyTurn(Ally ally, Player player, GameMap map, IGameLog log)
    {
        if (ally.IsDefeated) return;

        // Passive regen: heal 1 HP per turn if not at max.
        if (ally.CurrentHealth < ally.MaxHealth)
            ally.CurrentHealth = Math.Min(ally.MaxHealth, ally.CurrentHealth + 1);

        // Find nearest visible enemy within aggro range.
        Monster? target = null;
        int nearDist = int.MaxValue;
        foreach (var e in map.Entities)
        {
            if (e == player || e == ally || e.IsDefeated || e is not Monster m) continue;
            int dist = Math.Max(Math.Abs(m.X - ally.X), Math.Abs(m.Y - ally.Y));
            if (dist <= Ally.AggroRange && dist < nearDist) { target = m; nearDist = dist; }
        }

        // Behavior: aggressive attacks nearest, defensive only attacks near player.
        if (ally.Behavior == AllyBehavior.Defensive && target != null)
        {
            int distToPlayer = Math.Max(Math.Abs(target.X - player.X), Math.Abs(target.Y - player.Y));
            if (distToPlayer > 2) target = null; // only engage enemies near player
        }

        if (ally.Behavior == AllyBehavior.Follow)
            target = null; // never attack

        // Adjacent to target: attack.
        if (target != null && nearDist <= 1)
        {
            AllyAttack(ally, target, map, log);
            return;
        }

        // Has a target but not adjacent: move toward it.
        if (target != null)
        {
            MoveToward(ally, target.X, target.Y, map);
            return;
        }

        // No target: follow the player if too far.
        int playerDist = Math.Max(Math.Abs(player.X - ally.X), Math.Abs(player.Y - ally.Y));
        if (playerDist > Ally.FollowDistance)
            MoveToward(ally, player.X, player.Y, map);
    }

    // Ally attacks a monster.
    private static void AllyAttack(Ally ally, Monster target, GameMap map, IGameLog log)
    {
        int dmg = Math.Max(1, ally.AttackDamage - target.BaseDefense / 3);
        bool crit = Random.Shared.Next(100) < 8 + ally.Dexterity / 3;
        if (crit) dmg = (int)(dmg * 1.5);

        var reward = target.TakeDamage(dmg);
        string critTag = crit ? " CRIT!" : "";
        log.LogCombat($"  {ally.Name} hits {target.Name} for {dmg}!{critTag}");

        if (target.IsDefeated)
            log.LogCombat($"  {ally.Name} defeated {target.Name}!");
    }

    // Move ally one step toward a target position.
    private static void MoveToward(Ally ally, int tx, int ty, GameMap map)
    {
        int dx = Math.Sign(tx - ally.X), dy = Math.Sign(ty - ally.Y);

        // Try diagonal, then cardinal.
        if (dx != 0 && dy != 0 && CanMove(map, ally.X + dx, ally.Y + dy))
        { map.MoveEntity(ally, ally.X + dx, ally.Y + dy); return; }
        if (dx != 0 && CanMove(map, ally.X + dx, ally.Y))
        { map.MoveEntity(ally, ally.X + dx, ally.Y); return; }
        if (dy != 0 && CanMove(map, ally.X, ally.Y + dy))
        { map.MoveEntity(ally, ally.X, ally.Y + dy); return; }
    }

    private static bool CanMove(GameMap map, int x, int y) =>
        map.InBounds(x, y) && !map.GetTile(x, y).BlocksMovement && map.GetTile(x, y).Occupant == null;

    // Scale allies to match player level (called on level up or floor change).
    public static void ScaleToPlayer(int playerLevel)
    {
        foreach (var ally in Members)
        {
            ally.Level = Math.Max(1, playerLevel - 1);
            ally.BaseAttack = 5 + playerLevel * 2;
            ally.BaseDefense = 3 + playerLevel;
            ally.MaxHealth = 80 + playerLevel * 8;
            if (ally.CurrentHealth > ally.MaxHealth) ally.CurrentHealth = ally.MaxHealth;
        }
    }

    // Clear party (new game).
    public static void Clear() => Members.Clear();
}
