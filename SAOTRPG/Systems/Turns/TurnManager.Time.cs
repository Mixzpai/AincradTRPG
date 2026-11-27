using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Map;
using SAOTRPG.Systems.World;

namespace SAOTRPG.Systems.Turns;

// The flow of time. A turn is one second. Every
// turn each creature gains its speed in move points, 100 for the player and an ordinary monster,
// and acts while it has points left; every action spends some. The world advances one turn each
// time the player's points run out, so a 250-point step through shallow water lets every monster
// act two and a half times.
public partial class TurnManager
{
    public const int NormalSpeed = 100;

    // The player's move points. Persisted: a save is a moment, and that includes the middle of a turn.
    public int PlayerMoves { get; private set; } = NormalSpeed;
    public int PlayerSpeed => NormalSpeed;

    // Spend an action's cost, passing a turn each time the points run out (the player then gains
    // their speed). False if the player died in the time that passed.
    private bool SpendMoves(int cost, bool regen = true)
    {
        PlayerMoves -= cost;
        while (PlayerMoves <= 0)
        {
            if (!PassTurn(regen)) return false;
            PlayerMoves += PlayerSpeed;
        }
        return true;
    }

    // Pause: the rest of this turn is spent doing nothing.
    private bool Pause(bool regen = true)
    {
        PlayerMoves = 0;
        return SpendMoves(0, regen);
    }

    // One second of world time: the clock, every timed effect, every other creature, regen.
    private bool PassTurn(bool regen = true)
    {
        AdvanceTurn();
        TickPoison(); TickBleed(); TickSlow(); TickPerTurnTimers();
        if (_player.IsDefeated) return false;
        ProcessEntityTurns();
        if (_player.IsDefeated) return false;
        if (regen) PassiveRegen();
        return true;
    }

    // ── What actions cost ─────────────────────────────────────────────────────────────────────

    // Terrain move cost: 2 is flat ground, 100 points a tile. Soft ground (reeds, mud, dune sand,
    // snow) costs 3.
    public static int TerrainMoveCost(TileType t) => t switch
    {
        TileType.GrassTall => 5,
        TileType.Bush => 8,
        TileType.Water => 5,
        TileType.WaterDeep => 8,
        TileType.Lava => 4,
        TileType.Reeds or TileType.Mud or TileType.DuneSand or TileType.Snow => 3,
        _ => 2,
    };

    // A step costs the mean of the two tiles' costs, 50 points a unit. Diagonals cost the same as
    // orthogonal steps.
    public int StepCost(int fromX, int fromY, int toX, int toY) =>
        25 * (TerrainMoveCost(_map.RefTile(fromX, fromY).Type) + TerrainMoveCost(_map.RefTile(toX, toY).Type));

    // A monster's step: the same mean, except a swimmer crosses water as flat ground.
    private int MonsterStepCost(Monster m, int fromX, int fromY)
    {
        int a = TerrainMoveCost(_map.RefTile(fromX, fromY).Type);
        int b = TerrainMoveCost(_map.RefTile(m.X, m.Y).Type);
        if (m.CanSwim)
        {
            if (IsWater(_map.RefTile(fromX, fromY).Type)) a = 2;
            if (IsWater(_map.RefTile(m.X, m.Y).Type)) b = 2;
        }
        return 25 * (a + b);
    }

    private static bool IsWater(TileType t) => t is TileType.Water or TileType.WaterDeep;

    // Move modes: running doubles the speed of a step, crouching halves it.
    public const int RunDivisor = 2, CrouchMultiplier = 2;

    // Attack time is 65 + volume / 62.5 mL + weight / 60 g. Weapons here carry no mass, so each
    // type takes the figures of a typical weapon of its kind (combat knife, rapier, arming sword,
    // katana, mace, steel spear, battle axe, scythe, zweihander); empty hands are the bare 65.
    public static int AttackTime(string? weaponType) => weaponType switch
    {
        null => 65,
        "Dagger" => 74,
        "Claws" => 76,
        "Rapier" => 103,
        "Scimitar" => 105,
        "One-Handed Sword" => 108,
        "Katana" => 113,
        "Mace" => 116,
        "Spear" => 138,
        "Axe" => 142,
        "Scythe" => 167,
        "Two-Handed Sword" => 173,
        _ => 100,
    };

    // The attack-speed formula is sized for stats of 4 to 20; this game's go higher, so past 20
    // dexterity buys nothing.
    private const int CddaDexCap = 20;

    // Attack speed: half the attack time, plus a skill cost that shrinks to a third of
    // it at melee 10 (weapon proficiency / 10 here), less half the dexterity; never under 25.
    public int AttackMoves()
    {
        var wpn = _player.Inventory.GetEquipped(EquipmentSlot.Weapon) as Weapon;
        string? type = wpn?.WeaponType;
        if (type == "Bow") return NormalSpeed;
        int baseCost = AttackTime(type) / 2;
        float skill = type != null ? Math.Min(10f, GetProficiencyLevel(type) / 10f) : 0f;
        int skillCost = (int)(baseCost * (15 - skill) / 15);
        int dexBonus = Math.Min(_player.Dexterity, CddaDexCap) / 2;
        return Math.Max(25, baseCost + skillCost - dexBonus);
    }

    // Consume time: eating takes 5 mL a second and drinking 15. Items carry no
    // volume here, so a potion is a 30 mL vial (2 s) and a meal a 250 mL portion (50 s).
    public const int PotionMoves = 200, FoodMoves = 5000, UseItemMoves = NormalSpeed;

    // ── Long actions: wait and sleep activities ───────────────────────────────────────────────
    public sealed class Activity
    {
        public required string Verb { get; init; }          // "waiting", "sleeping"
        public bool Sleeping { get; init; }
        public bool BesideFire { get; init; }
        public int TurnsLeft { get; set; }
        public int TurnsDone { get; set; }
        public int HealthAtStart { get; init; }
        public HashSet<int> SeenHostiles { get; } = new();
    }

    public Activity? CurrentActivity { get; private set; }

    // A sleeper wakes once healed, after at least an hour and at most eight.
    public const int MinSleepTurns = GameClock.TurnsPerHour, MaxSleepTurns = 8 * GameClock.TurnsPerHour;
    // A monster within this many tiles is a "dangerously close" distraction.
    private const int DangerRadius = 5;

    // Wait menu ('|'). The UI then runs ContinueActivity until it returns false.
    public void BeginWait(int turns)
    {
        if (_player.IsDefeated || turns <= 0) return;
        StartActivity(new Activity { Verb = "waiting", TurnsLeft = turns, HealthAtStart = _player.CurrentHealth });
    }

    private void StartActivity(Activity a)
    {
        foreach (var m in _map.Monsters)
            if (!m.IsDefeated && _map.IsVisible(m.X, m.Y)) a.SeenHostiles.Add(m.Id);
        CurrentActivity = a;
    }

    // Run up to maxTurns seconds of the activity. True while it is still going.
    public bool ContinueActivity(int maxTurns)
    {
        var a = CurrentActivity;
        if (a == null) return false;
        for (int i = 0; i < maxTurns && a.TurnsLeft > 0; i++)
        {
            int hp = _player.CurrentHealth;
            if (!Pause()) { CurrentActivity = null; return false; }
            // A fire doubles a sleeper's regen.
            if (a.Sleeping && a.BesideFire) PassiveRegen();
            a.TurnsLeft--;
            a.TurnsDone++;
            // The player stands still, so sight changes only with the sun; refresh it now and then.
            if (a.TurnsDone % 10 == 0) UpdateVisibility();
            string? why = Distraction(a, hp);
            if (why != null) { StopActivity(why); break; }
            if (a.Sleeping && a.TurnsDone >= MinSleepTurns && _player.CurrentHealth >= _player.MaxHealth)
                a.TurnsLeft = 0;
        }
        if (CurrentActivity == a && a.TurnsLeft <= 0) FinishActivity(a);
        UpdateVisibility();
        TurnCompleted?.Invoke();
        return CurrentActivity != null;
    }

    // Distractions: a hostile coming into view, one dangerously close, being hurt.
    private string? Distraction(Activity a, int hpBefore)
    {
        if (_player.CurrentHealth < hpBefore) return "You were hurt!";
        foreach (var m in _map.Monsters)
        {
            if (m.IsDefeated) continue;
            int d = Math.Max(Math.Abs(m.X - _player.X), Math.Abs(m.Y - _player.Y));
            if (d <= DangerRadius && _map.IsVisible(m.X, m.Y)) return $"{m.Name} is dangerously close!";
            if (!a.Sleeping && _map.IsVisible(m.X, m.Y) && a.SeenHostiles.Add(m.Id)) return $"{m.Name} spotted!";
        }
        return null;
    }

    // A key press, or a distraction: the activity ends where it is.
    public void InterruptActivity() => StopActivity(null);

    private void StopActivity(string? why)
    {
        var a = CurrentActivity;
        if (a == null) return;
        CurrentActivity = null;
        string stop = a.Sleeping ? "You wake up." : $"You stop {a.Verb}.";
        _log.Log(why == null ? stop : $"{why} {stop}");
        if (a.Sleeping) WakeUp(a);
    }

    private void FinishActivity(Activity a)
    {
        CurrentActivity = null;
        if (a.Sleeping) { _log.Log("You wake up."); WakeUp(a); }
        else _log.Log("You finish waiting.");
    }
}
