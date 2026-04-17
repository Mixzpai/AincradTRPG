using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Map;

namespace SAOTRPG.UI;

// Main dungeon map view -- state, events, and visual effect tracking.
// Split across partial files: Rendering, Overlays, Input, LookMode, Helpers.
public partial class MapView : View
{
    private GameMap _map;
    private readonly Camera _camera;
    private readonly Player _player;

    private const int FootstepTrailLength = 10;
    private readonly Queue<(int X, int Y)> _footsteps = new();
    private (int X, int Y) _lastPlayerPos = (-1, -1);

    private readonly List<(int X, int Y, string Text, Color Color, int FramesLeft, bool IsCrit)> _damageFlashes = new();
    private const int DamageFlashFrames = 3;
    private const int CritDamageFlashFrames = 5;

    public void AddDamageFlash(int mx, int my, int damage, bool isPlayerDamage)
    {
        string text = damage.ToString();
        Color color = isPlayerDamage ? Color.BrightRed : Color.BrightYellow;
        _damageFlashes.Add((mx, my, text, color, DamageFlashFrames, false));
    }

    public void AddCritDamageFlash(int mx, int my, int damage, bool isPlayerDamage)
    {
        string text = $"«{damage}»";
        Color color = isPlayerDamage ? Color.BrightRed : Color.BrightCyan;
        _damageFlashes.Add((mx, my, text, color, CritDamageFlashFrames, true));
    }

    public void AddTextFlash(int mx, int my, string text, Color color)
        => _damageFlashes.Add((mx, my, text, color, DamageFlashFrames, false));

    // ── Border edge flash (crit/damage/levelup) ──────────────────────
    private int _borderFlashFrames;
    private Color _borderFlashColor = Color.Red;
    public void FlashBorder(Color color, int frames)
    {
        if (frames <= _borderFlashFrames && color == _borderFlashColor) return;
        _borderFlashColor = color;
        _borderFlashFrames = Math.Max(_borderFlashFrames, frames);
    }

    // ── Weapon swing arcs ────────────────────────────────────────────
    private readonly List<(int FromX, int FromY, int ToX, int ToY, Color Color, int FramesLeft)> _weaponSwings = new();
    private const int WeaponSwingFrames = 2;
    public void AddWeaponSwing(int fx, int fy, int tx, int ty, Color color)
        => _weaponSwings.Add((fx, fy, tx, ty, color, WeaponSwingFrames));

    private readonly List<(int X, int Y, int FramesLeft)> _corpseMarkers = new();
    private const int CorpseMarkerFrames = 40;
    public void AddCorpseMarker(int mx, int my) => _corpseMarkers.Add((mx, my, CorpseMarkerFrames));

    private readonly List<(int X, int Y, int FramesLeft)> _scorchMarks = new();
    private const int ScorchMarkFrames = 60;

    public void AddScorchMark(int mx, int my)
    {
        if (!_scorchMarks.Exists(s => s.X == mx && s.Y == my))
            _scorchMarks.Add((mx, my, ScorchMarkFrames));
    }

    private readonly Dictionary<int, Queue<(int X, int Y)>> _mobTrails = new();
    private const int MobTrailLength = 4;

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

    public void ClearMobTrail(int entityId) => _mobTrails.Remove(entityId);

    // SAO-style polygon dissolution -- fragments scatter outward from kill
    // point with cyan flash, cycling through polygon glyphs as they expand.
    private readonly List<PolygonBurst> _polyBursts = new();
    private const int PolyBurstFrames = 6;

    // Each fragment: offset direction, speed multiplier, glyph index
    private static readonly (int dx, int dy, int speed, int glyphIdx)[] PolyFragments =
    {
        // Inner ring (speed 1)
        (-1, -1, 1, 0), ( 0, -1, 1, 1), ( 1, -1, 1, 2),
        (-1,  0, 1, 3),                  ( 1,  0, 1, 0),
        (-1,  1, 1, 1), ( 0,  1, 1, 2), ( 1,  1, 1, 3),
        // Outer ring (speed 2) -- more fragments for density
        (-2, -1, 2, 0), (-1, -2, 2, 1), ( 1, -2, 2, 2), ( 2, -1, 2, 3),
        (-2,  1, 2, 0), (-1,  2, 2, 1), ( 1,  2, 2, 2), ( 2,  1, 2, 3),
        // Far scatter (speed 3)
        ( 0, -3, 3, 0), ( 3,  0, 3, 1), ( 0,  3, 3, 2), (-3,  0, 3, 3),
        (-2, -2, 3, 0), ( 2, -2, 3, 1), ( 2,  2, 3, 2), (-2,  2, 3, 3),
    };

    private static readonly char[] PolyGlyphs = { '>', '<', '^', 'v', '/', '\\', '+', 'x' };

    private record struct PolygonBurst(int X, int Y, int Frame, bool IsBoss);

    public void AddShatterParticle(int mx, int my) =>
        _polyBursts.Add(new PolygonBurst(mx, my, PolyBurstFrames, false));

    public void AddBossShatter(int mx, int my) =>
        _polyBursts.Add(new PolygonBurst(mx, my, PolyBurstFrames + 3, true));

    // Sword skill activation burst -- ring of light around player
    private readonly List<(int X, int Y, int Frame, Color Color)> _skillFlashes = new();
    private const int SkillFlashFrames = 3;

    public void AddSkillFlash(int mx, int my, Color color) =>
        _skillFlashes.Add((mx, my, SkillFlashFrames, color));

    private int _killStreakFlashFrames;
    private int _killStreakLevel;
    private string _killStreakText = "";
    private Color _killStreakColor = Color.BrightYellow;

    public void TriggerKillStreakFlash(int streakLevel)
    {
        _killStreakLevel = streakLevel;
        (_killStreakText, _killStreakColor, _killStreakFlashFrames) = streakLevel switch
        {
            2  => ("DOUBLE KILL",   Color.BrightYellow,  5),
            3  => ("TRIPLE KILL",   Color.Yellow,        6),
            4  => ("QUAD KILL",     Color.BrightRed,     7),
            5  => ("RAMPAGE",       Color.BrightMagenta, 8),
            6  => ("UNSTOPPABLE",   Color.BrightMagenta, 8),
            7  => ("DOMINATING",    Color.BrightCyan,    9),
            10 => ("GODLIKE",       Color.BrightRed,    12),
            15 => ("LEGENDARY",     Color.BrightYellow, 14),
            >= 20 => ("MYTHIC",     Color.White,        16),
            _ => ("KILL STREAK",    Color.BrightYellow,  4),
        };
    }

    private int _levelUpFlashFrames;
    private const int LevelUpFlashDuration = 6;
    public void TriggerLevelUpFlash() => _levelUpFlashFrames = LevelUpFlashDuration;

    private Color? _statusTintColor;
    public void SetStatusTint(Color? color) => _statusTintColor = color;

    private readonly HashSet<(int X, int Y)> _openedDoors = new();
    private readonly List<(int X, int Y, int FramesLeft)> _doorFlashes = new();
    private const int DoorFlashFrames = 2;
    public void MarkDoorOpened(int x, int y)
    {
        _openedDoors.Add((x, y));
        _doorFlashes.Add((x, y, DoorFlashFrames));
    }

    // Brief red background flash on a tile that just took damage.
    // Paired structures: the list tracks per-entry frame counters (needed for
    // independent tick-down) while the HashSet powers O(1) membership tests
    // from the per-tile rendering hot path (thousands of lookups/frame).
    private readonly List<(int X, int Y, int FramesLeft)> _hitFlashes = new();
    private readonly HashSet<(int X, int Y)> _hitFlashSet = new();
    private const int HitFlashFrames = 2;
    public void AddHitFlash(int mx, int my)
    {
        _hitFlashes.Add((mx, my, HitFlashFrames));
        _hitFlashSet.Add((mx, my));
    }
    public bool IsHitFlashed(int mx, int my) => _hitFlashSet.Contains((mx, my));

    // Single-frame screen-wide brightness boost on player crit.
    private int _critScreenFlashFrames;
    public void TriggerCritScreenFlash() => _critScreenFlashFrames = Math.Max(_critScreenFlashFrames, 1);

    // Boss-first-seen tracking: draws a centered banner on first sight.
    private readonly HashSet<int> _bossesSeen = new();
    private int _bossEntranceFrames;
    private string _bossEntranceName = "";
    // Precomputed banner text ("‹ NAME ›"). Cached once at trigger so the
    // per-frame overlay render doesn't re-run ToUpper + string concat.
    private string _bossEntranceBanner = "";
    private const int BossEntranceFrames = 10;
    public void TriggerBossEntrance(string name)
    {
        _bossEntranceName = name;
        _bossEntranceBanner = $"‹ {name.ToUpper()} ›";
        _bossEntranceFrames = BossEntranceFrames;
    }

    // ── Events ───────────────────────────────────────────────────────
    public event Action<int, int>? PlayerMoveRequested;
    public event Action? LookRequested;
    public event Action? WaitRequested;
    public event Action? InventoryRequested;
    public event Action? PickupRequested;
    public event Action? AutoExploreRequested;
    public event Action? StatsRequested;
    public event Action? HelpRequested;
    public event Action? PlayerGuideRequested;
    public event Action? RestRequested;
    public event Action? CounterRequested;
    public event Action? KillStatsRequested;
    public event Action? EquipmentRequested;
    public event Action? LogScrollUpRequested;
    public event Action? LogScrollDownRequested;
    public event Action<int, int>? SprintRequested;
    public event Action<int, int>? StealthMoveRequested;
    public event Action? SaveRequested;
    public event Action<int>? QuickUseRequested;
    public event Action<int>? SwordSkillRequested;
    public event Action? SwordSkillMenuRequested;
    public event Action? QuestLogRequested;

    public void SetMap(GameMap map)
    {
        _map = map;
        _footsteps.Clear();
        _lastPlayerPos = (-1, -1);
        _openedDoors.Clear();
        _levelUpFlashFrames = 0;
        _scorchMarks.Clear();
        _mobTrails.Clear();
        _polyBursts.Clear();
        _skillFlashes.Clear();
        _killStreakFlashFrames = 0;
        _statusTintColor = null;
        _weaponSwings.Clear();
        _borderFlashFrames = 0;
        _damageFlashes.Clear();
        _hitFlashes.Clear();
        _hitFlashSet.Clear();
        _doorFlashes.Clear();
        _critScreenFlashFrames = 0;
        _bossesSeen.Clear();
        _bossEntranceFrames = 0;
    }

    public MapView(GameMap map, Camera camera, Player player)
    {
        _map = map;
        _camera = camera;
        _player = player;
        CanFocus = true;
    }
}
