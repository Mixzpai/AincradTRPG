using Terminal.Gui;

namespace SAOTRPG.Systems;

// Categories drive banner colors + coalesce-eligibility.
public enum ToastCategory
{
    LevelUp,
    TitleAcquired,
    StatUp,
    SwordSkillUnlocked,
    AchievementUnlocked,
    QuestComplete,
    FloorBossCleared,
    ShopTierUnlocked,
    BestiarySpeciesFirst,
    Info,
}

// Center notif queue: 1 visible, same-cat within CoalesceWindowMs merges.
// MapView.Toasts reads Peek() each frame + calls AdvanceFrame() to tick TTL.
public static class ToastQueue
{
    // Timing budget (ms): 300 fade-in + 2400 hold + 300 fade-out = 3000.
    public const int FadeInMs = 300;
    public const int HoldMs = 2400;
    public const int FadeOutMs = 300;
    public const int LifetimeMs = FadeInMs + HoldMs + FadeOutMs;
    private const int CoalesceWindowMs = 500;

    // Cap queue to keep post-boss salvos tolerable (per research §6 anti-pattern).
    private const int MaxQueued = 5;

    public class Toast
    {
        public string Message;
        public Color Accent;
        public ToastCategory Category;
        // Monotonic ms since added (for coalesce window) and since activated
        // (for fade timeline). Activation = the moment Peek() first exposes it.
        public long AddedAtMs;
        public long ActivatedAtMs = -1;
        public int ElapsedMs;

        public Toast(string msg, Color accent, ToastCategory cat, long addedAt)
        {
            Message = msg; Accent = accent; Category = cat; AddedAtMs = addedAt;
        }
    }

    private static readonly List<Toast> _queue = new();
    private static readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();

    public static void Enqueue(string message, Color accent, ToastCategory category)
    {
        long now = _clock.ElapsedMilliseconds;

        // Coalesce: if tail is same-cat, still within 500ms window, and not
        // yet activated (text still safe to edit), merge into it.
        if (_queue.Count > 0)
        {
            var tail = _queue[^1];
            if (tail.Category == category
                && tail.ActivatedAtMs < 0
                && now - tail.AddedAtMs <= CoalesceWindowMs)
            {
                tail.Message = Merge(tail.Message, message);
                tail.AddedAtMs = now;
                return;
            }
        }

        if (_queue.Count >= MaxQueued) return;
        _queue.Add(new Toast(message, accent, category, now));
    }

    // Returns the currently-rendering toast, activating it if this is its
    // first exposure. Returns null when queue is empty.
    public static Toast? Peek()
    {
        if (_queue.Count == 0) return null;
        var head = _queue[0];
        if (head.ActivatedAtMs < 0) head.ActivatedAtMs = _clock.ElapsedMilliseconds;
        head.ElapsedMs = (int)(_clock.ElapsedMilliseconds - head.ActivatedAtMs);
        if (head.ElapsedMs >= LifetimeMs)
        {
            _queue.RemoveAt(0);
            return Peek();
        }
        return head;
    }

    public static void Clear() => _queue.Clear();

    // Stat-up compact merge: "STAT up: STR +1" + "Stat up: VIT +1" →
    // "Stat up: STR +1, VIT +1". Generic fallback = newline-join.
    private static string Merge(string existing, string incoming)
    {
        const string StatUpPrefix = "Stat up: ";
        if (existing.StartsWith(StatUpPrefix) && incoming.StartsWith(StatUpPrefix))
            return existing + ", " + incoming[StatUpPrefix.Length..];
        return existing + " / " + incoming;
    }

    // Convenience wrappers for common categories — keeps callsites slim.
    public static void EnqueueLevelUp(int level) =>
        Enqueue($"LEVEL UP — {level}", Color.BrightYellow, ToastCategory.LevelUp);

    public static void EnqueueTitle(string title) =>
        Enqueue($"Title: {title}", Color.BrightYellow, ToastCategory.TitleAcquired);

    public static void EnqueueStatUp(string statShort, int delta) =>
        Enqueue($"Stat up: {statShort} +{delta}", Color.Cyan, ToastCategory.StatUp);

    public static void EnqueueSwordSkill(string name) =>
        Enqueue($"Sword Skill: {name}", Color.BrightMagenta, ToastCategory.SwordSkillUnlocked);

    public static void EnqueueAchievement(string name) =>
        Enqueue($"Achievement: {name}", Color.BrightYellow, ToastCategory.AchievementUnlocked);

    public static void EnqueueQuest(string title) =>
        Enqueue($"Quest: {title}", Color.BrightGreen, ToastCategory.QuestComplete);

    public static void EnqueueFloorBoss(string bossName) =>
        Enqueue($"Floor Boss Cleared — {bossName}", Color.BrightYellow, ToastCategory.FloorBossCleared);

    public static void EnqueueShopTier(int tiersGained) =>
        Enqueue($"Shop tier +{tiersGained}", Color.DarkGray, ToastCategory.ShopTierUnlocked);

    public static void EnqueueBestiaryFirst(string speciesName) =>
        Enqueue($"New species: {speciesName}", Color.Cyan, ToastCategory.BestiarySpeciesFirst);
}
