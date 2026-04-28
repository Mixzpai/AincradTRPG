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

    public IGameLog? Log { get; set; }

    // Per-cell visual snapshot, allocated per-map in SetMap. Animated types bypass; type-change invalidates.
    private Map.TileVisualCache _visualCache = null!;
    private Action<int, int, Map.TileType, Map.TileType>? _visualCacheInvalidator;
    private Action<int, int>? _visualCacheTrapInvalidator;

    // Bundle 13 (Item 8) — read from UserSettings.FootstepLength (0..1000).
    private static int FootstepTrailLength =>
        Math.Clamp(SAOTRPG.Systems.UserSettings.Current.FootstepLength, 0, 1000);
    private readonly Queue<(int X, int Y)> _footsteps = new();
    private (int X, int Y) _lastPlayerPos = (-1, -1);

    private record struct DamageFlash(int X, int Y, string Text, Color Color, int RemainingMs, bool IsCrit);
    private readonly List<DamageFlash> _damageFlashes = new();
    private const int DamageFlashMs = 100;
    private const int CritDamageFlashMs = 165;

    public void AddDamageFlash(int mx, int my, int damage, bool isPlayerDamage)
    {
        string text = damage.ToString();
        Color color = isPlayerDamage ? Color.BrightRed : Color.BrightYellow;
        _damageFlashes.Add(new DamageFlash(mx, my, text, color, DamageFlashMs, false));
        DirtyFrame();
    }

    public void AddCritDamageFlash(int mx, int my, int damage, bool isPlayerDamage)
    {
        string text = $"«{damage}»";
        Color color = isPlayerDamage ? Color.BrightRed : Color.BrightCyan;
        _damageFlashes.Add(new DamageFlash(mx, my, text, color, CritDamageFlashMs, true));
        DirtyFrame();
    }

    public void AddTextFlash(int mx, int my, string text, Color color)
    { _damageFlashes.Add(new DamageFlash(mx, my, text, color, DamageFlashMs, false)); DirtyFrame(); }

    // ── Border edge flash (crit/damage/levelup) ──────────────────────
    private int _borderFlashRemainingMs;
    private Color _borderFlashColor = Color.Red;
    // `ms` is duration in real-time milliseconds (caller-provided).
    public void FlashBorder(Color color, int ms)
    {
        if (ms <= _borderFlashRemainingMs && color == _borderFlashColor) return;
        _borderFlashColor = color;
        _borderFlashRemainingMs = Math.Max(_borderFlashRemainingMs, ms);
        DirtyFrame();
    }

    // ── Weapon swing arcs ────────────────────────────────────────────
    private readonly List<(int FromX, int FromY, int ToX, int ToY, Color Color, int RemainingMs)> _weaponSwings = new();
    private const int WeaponSwingMs = 66;
    public void AddWeaponSwing(int fx, int fy, int tx, int ty, Color color)
    { _weaponSwings.Add((fx, fy, tx, ty, color, WeaponSwingMs)); DirtyFrame(); }

    private readonly List<(int X, int Y, int RemainingMs)> _corpseMarkers = new();
    private const int CorpseMarkerMs = 1320;
    public void AddCorpseMarker(int mx, int my) { _corpseMarkers.Add((mx, my, CorpseMarkerMs)); DirtyFrame(); }

    private readonly List<(int X, int Y, int RemainingMs)> _scorchMarks = new();
    private const int ScorchMarkMs = 2000;

    public void AddScorchMark(int mx, int my)
    {
        if (!_scorchMarks.Exists(s => s.X == mx && s.Y == my))
            _scorchMarks.Add((mx, my, ScorchMarkMs));
        DirtyFrame();
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
        DirtyFrame();
    }

    public void ClearMobTrail(int entityId) { _mobTrails.Remove(entityId); DirtyFrame(); }

    // Monster death braille burst — two-phase fracture+spread. Tier scales radius/duration/count.
    public enum MobTier { Standard, Elite, FloorBoss }

    // One shard inside a death burst. Polar (angle, dist) integrates over elapsed ms.
    private struct DeathShard
    {
        public float OriginX;
        public float OriginY;
        public float Angle;       // radians
        public float Speed;       // tiles/ms (≈0.008–0.012)
        public float SpawnRadius; // initial offset from kill point
        public bool IsHero;       // true → ◆/◊ glyph; false → braille
        public byte Seed;         // per-shard rng seed for braille mask churn
        public float LifeBoost;   // extra multiplier on duration (north-side bias)
    }

    private struct DeathBurst
    {
        public int X;
        public int Y;
        public MobTier Tier;
        public float ElapsedMs;
        public float TotalMs;
        public float FlashMs;       // fracture-flash phase length
        public DeathShard[] Shards;
    }

    private readonly List<DeathBurst> _deathBursts = new();

    public void EmitMonsterDeathBurst(int x, int y, MobTier tier)
    {
        if (Systems.UserSettings.Current.ParticleDensity == Systems.ParticleDensity.Off) return;
        var burst = BuildDeathBurst(x, y, tier);
        _deathBursts.Add(burst);
        DirtyFrame();
    }

    private DeathBurst BuildDeathBurst(int x, int y, MobTier tier)
    {
        // Tier scaling: total ms, flash ms, base shard count, radius.
        (float total, float flash, int baseCount, float radius) = tier switch
        {
            MobTier.FloorBoss => (2000f, 200f, 140, 5.5f),
            MobTier.Elite     => ( 800f, 100f,  70, 3.0f),
            _                 => ( 500f,  80f,  40, 2.0f),
        };

        // Density scaling: respects user preference. ScaleCount mirrors ParticleQueue.
        int n = Systems.UserSettings.Current.ParticleDensity switch
        {
            Systems.ParticleDensity.Subtle   => Math.Max(8,  baseCount / 3),
            Systems.ParticleDensity.Moderate => Math.Max(12, baseCount * 2 / 3),
            _                                => baseCount,
        };

        var rng = new Random(unchecked(x * 73856093 ^ y * 19349663 ^ (int)Systems.FrameClock.ElapsedMs));
        var shards = new DeathShard[n];
        // Hero ◆/◊ count: 1-2 standard, 2-3 elite, 3-4 boss.
        int heroCount = tier switch { MobTier.FloorBoss => 4, MobTier.Elite => 3, _ => 2 };
        for (int i = 0; i < n; i++)
        {
            bool isHero = i < heroCount;
            float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
            // Hero shards travel slower (mid-flight while braille spreads ahead).
            float speedRange = isHero ? 0.0008f : 0.0011f;
            float speedFloor = isHero ? 0.0006f : 0.0008f;
            float speed = speedFloor + (float)rng.NextDouble() * speedRange;
            // Spawn at silhouette boundary (≈0.4 tiles for std, scales with size).
            float silhouette = tier switch { MobTier.FloorBoss => 1.4f, MobTier.Elite => 0.9f, _ => 0.4f };
            float spawnR = isHero ? silhouette : (float)rng.NextDouble() * 0.6f;
            // North bias — shards above the kill point live ~20% longer.
            float lifeBoost = (Math.Sin(angle) < 0f) ? 1.20f : 1.00f;
            shards[i] = new DeathShard
            {
                OriginX = x,
                OriginY = y,
                Angle = angle,
                Speed = speed,
                SpawnRadius = spawnR,
                IsHero = isHero,
                Seed = (byte)rng.Next(256),
                LifeBoost = lifeBoost,
            };
        }
        // Cap radius via shard speed × duration ≈ radius tiles.
        return new DeathBurst { X = x, Y = y, Tier = tier, ElapsedMs = 0f,
            TotalMs = total, FlashMs = flash, Shards = shards };
    }

    // Sword skill activation burst -- ring of light around player
    private readonly List<(int X, int Y, int RemainingMs, int TotalMs, Color Color)> _skillFlashes = new();
    private const int SkillFlashMs = 100;
    private const int SkillFlashStepMs = 33;

    public void AddSkillFlash(int mx, int my, Color color)
    { _skillFlashes.Add((mx, my, SkillFlashMs, SkillFlashMs, color)); DirtyFrame(); }

    private int _killStreakFlashRemainingMs;
    private int _killStreakLevel;
    private string _killStreakText = "";
    private Color _killStreakColor = Color.BrightYellow;

    public void TriggerKillStreakFlash(int streakLevel)
    {
        DirtyFrame();
        _killStreakLevel = streakLevel;
        (_killStreakText, _killStreakColor, _killStreakFlashRemainingMs) = streakLevel switch
        {
            2  => ("DOUBLE KILL",   Color.BrightYellow, 165),
            3  => ("TRIPLE KILL",   Color.Yellow,       200),
            4  => ("QUAD KILL",     Color.BrightRed,    230),
            5  => ("RAMPAGE",       Color.BrightMagenta,265),
            6  => ("UNSTOPPABLE",   Color.BrightMagenta,265),
            7  => ("DOMINATING",    Color.BrightCyan,   300),
            10 => ("GODLIKE",       Color.BrightRed,    400),
            15 => ("LEGENDARY",     Color.BrightYellow, 465),
            >= 20 => ("MYTHIC",     Color.White,        530),
            _ => ("KILL STREAK",    Color.BrightYellow, 200),
        };
    }

    private int _levelUpFlashRemainingMs;
    private const int LevelUpFlashMs = 200;
    public void TriggerLevelUpFlash() { _levelUpFlashRemainingMs = LevelUpFlashMs; DirtyFrame(); }

    // Wave 2 — status tint crossfade. _statusTintCurrentColor is what
    // ApplyStatusTint reads; SetStatusTint kicks off a 200ms ease toward target.
    private Color? _statusTintCurrentColor;
    private Color? _statusTintTargetColor;
    private Color? _statusTintSourceColor;
    private int _statusTintTransitionRemainingMs;
    private const int StatusTintTransitionMs = 200;

    public void SetStatusTint(Color? color)
    {
        if (color == _statusTintTargetColor) return;
        _statusTintSourceColor = _statusTintCurrentColor;
        _statusTintTargetColor = color;
        _statusTintTransitionRemainingMs = StatusTintTransitionMs;
        DirtyFrame();
    }

    private readonly HashSet<(int X, int Y)> _openedDoors = new();
    private readonly List<(int X, int Y, int RemainingMs)> _doorFlashes = new();
    private const int DoorFlashMs = 66;
    public void MarkDoorOpened(int x, int y)
    {
        _openedDoors.Add((x, y));
        _doorFlashes.Add((x, y, DoorFlashMs));
        DirtyFrame();
    }

    // Red flash on damaged tile. Paired: List tracks per-entry RemainingMs counters (independent tick-down),
    // HashSet gives O(1) membership for the per-tile render hot path.
    private readonly List<(int X, int Y, int RemainingMs)> _hitFlashes = new();
    private readonly HashSet<(int X, int Y)> _hitFlashSet = new();
    private const int HitFlashMs = 66;
    public void AddHitFlash(int mx, int my)
    {
        _hitFlashes.Add((mx, my, HitFlashMs));
        _hitFlashSet.Add((mx, my));
        DirtyFrame();
    }
    public bool IsHitFlashed(int mx, int my) => _hitFlashSet.Contains((mx, my));

    // One-shot screen-wide brightness boost on player crit (~100ms).
    private int _critScreenFlashRemainingMs;
    private const int CritScreenFlashMs = 100;
    public void TriggerCritScreenFlash() { _critScreenFlashRemainingMs = Math.Max(_critScreenFlashRemainingMs, CritScreenFlashMs); DirtyFrame(); }

    // Boss-first-seen tracking: draws a centered banner on first sight.
    private readonly HashSet<int> _bossesSeen = new();
    private int _bossEntranceRemainingMs;
    private string _bossEntranceName = "";
    // Precomputed banner text ("‹ NAME ›"). Cached once at trigger so the
    // per-frame overlay render doesn't re-run ToUpper + string concat.
    private string _bossEntranceBanner = "";
    private const int BossEntranceMs = 1000;
    public void TriggerBossEntrance(string name)
    {
        _bossEntranceName = name;
        _bossEntranceBanner = $"‹ {name.ToUpper()} ›";
        _bossEntranceRemainingMs = BossEntranceMs;
        DirtyFrame();
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
    // Opens the Bestiary dialog (Y key).
    public event Action? BestiaryRequested;
    // Fired on Esc from the map view — opens the pause menu.
    public event Action? PauseRequested;
    // FB-479 Shift+S — flips the status tray between compact / verbose labels.
    public event Action? StatusTrayVerboseToggleRequested;
    // F9 — hot-reload biome JSONs + regenerate current floor with same seed.
    public event Action? BiomeReloadRequested;
    // Bundle 13 Item 6 — `\` keypress raised from MapView.Input.cs. GameScreen reads the
    // equipped weapon and either opens the Bow reticle or toasts a "no bow" hint.
    public event Action? RangedFireKeyPressed;
    // Bundle 13 Item 1 — Shift+L opens the Legendary Collectables panel.
    public event Action? LegendaryCollectablesRequested;

    // ── Wave 2 HP/XP/SAT tween state ──────────────────────────────────
    // Generic int-tween with start/target/remaining state. ApplyTarget bumps the
    // tween (multi-hit: restart from current displayed value, no jump).
    private struct IntTween
    {
        public int Displayed;
        public int Start;
        public int Target;
        public int RemainingMs;
        public bool ApplyTarget(int newTarget, int durationMs)
        {
            if (newTarget == Target && RemainingMs == 0) return false;
            if (newTarget == Target) return false;
            Start = Displayed;
            Target = newTarget;
            RemainingMs = durationMs;
            return true;
        }
        public void Tick(int dtMs, int durationMs)
        {
            if (RemainingMs <= 0) { Displayed = Target; return; }
            RemainingMs = Math.Max(0, RemainingMs - dtMs);
            if (RemainingMs <= 0) { Displayed = Target; return; }
            float t = 1f - RemainingMs / (float)durationMs;
            float eased = SAOTRPG.Systems.EasingHelper.Ease(t,
                SAOTRPG.Systems.EasingHelper.EasingType.EaseOutQuad);
            Displayed = Start + (int)Math.Round((Target - Start) * eased);
        }
        public bool Active => RemainingMs > 0;
    }

    private const int HpTweenDurationMs = 300;
    private IntTween _playerHpTween;
    private IntTween _playerXpTween;
    private IntTween _playerSatTween;
    private bool _playerBarsInitialized;
    // Per-monster HP tween for boss bar + look-mode enemy display.
    private readonly Dictionary<int, IntTween> _monsterHpTween = new();

    // Fires when any HP/XP/SAT tween advanced this frame so the HUD label
    // text picks up the new value without a turn-tick.
    public event Action? PlayerBarsTweenTick;

    // Player bars: GameScreen.RefreshHud calls each turn to set targets;
    // MapView ticks tweens on OnDrawingContent + raises PlayerBarsTweenTick.
    public int DisplayedPlayerHp  => _playerHpTween.Displayed;
    public int DisplayedPlayerXp  => _playerXpTween.Displayed;
    public int DisplayedPlayerSat => _playerSatTween.Displayed;

    public void SetPlayerBarTargets(int hp, int xp, int sat)
    {
        if (!_playerBarsInitialized)
        {
            _playerHpTween.Displayed = _playerHpTween.Target = _playerHpTween.Start = hp;
            _playerXpTween.Displayed = _playerXpTween.Target = _playerXpTween.Start = xp;
            _playerSatTween.Displayed = _playerSatTween.Target = _playerSatTween.Start = sat;
            _playerBarsInitialized = true;
            return;
        }
        bool a = _playerHpTween.ApplyTarget(hp, HpTweenDurationMs);
        bool b = _playerXpTween.ApplyTarget(xp, HpTweenDurationMs);
        bool c = _playerSatTween.ApplyTarget(sat, HpTweenDurationMs);
        if (a || b || c) DirtyFrame();
    }

    // Boss/look-mode mob HP read path. Caller passes id+target each render;
    // first call seeds, subsequent target changes start a tween from current.
    public int GetDisplayedMonsterHp(int id, int targetHp)
    {
        if (!_monsterHpTween.TryGetValue(id, out var tw))
        {
            tw = new IntTween { Displayed = targetHp, Start = targetHp, Target = targetHp };
            _monsterHpTween[id] = tw;
            return targetHp;
        }
        if (tw.Target != targetHp)
        {
            tw.ApplyTarget(targetHp, HpTweenDurationMs);
            _monsterHpTween[id] = tw;
            DirtyFrame();
        }
        return tw.Displayed;
    }

    // Partial method hook implemented in MapView.TileAnimations.cs.
    partial void RenderAmbientTiles(int vpWidth, int vpHeight);

    public void SetMap(GameMap map)
    {
        if (_visualCacheInvalidator != null && _map != null)
            _map.TileTypeChanged -= _visualCacheInvalidator;
        if (_visualCacheTrapInvalidator != null && _map != null)
            _map.TileVisualInvalidated -= _visualCacheTrapInvalidator;
        _map = map;
        _visualCache = new Map.TileVisualCache(map.Width, map.Height);
        _visualCacheInvalidator = (x, y, _, _) => _visualCache.Invalidate(x, y);
        _visualCacheTrapInvalidator = (x, y) => _visualCache.Invalidate(x, y);
        map.TileTypeChanged += _visualCacheInvalidator;
        map.TileVisualInvalidated += _visualCacheTrapInvalidator;
        _footsteps.Clear();
        _lastPlayerPos = (-1, -1);
        _openedDoors.Clear();
        _levelUpFlashRemainingMs = 0;
        _scorchMarks.Clear();
        _mobTrails.Clear();
        _deathBursts.Clear();
        _skillFlashes.Clear();
        _killStreakFlashRemainingMs = 0;
        _statusTintCurrentColor = null;
        _statusTintTargetColor = null;
        _statusTintSourceColor = null;
        _statusTintTransitionRemainingMs = 0;
        _monsterHpTween.Clear();
        _playerBarsInitialized = false;
        _weaponSwings.Clear();
        _borderFlashRemainingMs = 0;
        _damageFlashes.Clear();
        _hitFlashes.Clear();
        _hitFlashSet.Clear();
        _doorFlashes.Clear();
        _critScreenFlashRemainingMs = 0;
        _bossesSeen.Clear();
        _bossEntranceRemainingMs = 0;
        SAOTRPG.Systems.ParticleQueue.Clear();
        DirtyFrame();
    }

    public MapView(GameMap map, Camera camera, Player player)
    {
        _map = map;
        _camera = camera;
        _player = player;
        _visualCache = new Map.TileVisualCache(map.Width, map.Height);
        _visualCacheInvalidator = (x, y, _, _) => _visualCache.Invalidate(x, y);
        _visualCacheTrapInvalidator = (x, y) => _visualCache.Invalidate(x, y);
        map.TileTypeChanged += _visualCacheInvalidator;
        map.TileVisualInvalidated += _visualCacheTrapInvalidator;
        CanFocus = true;
    }
}
