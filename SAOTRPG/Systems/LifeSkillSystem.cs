using SAOTRPG.Items;

namespace SAOTRPG.Systems;

// FB-050 Skill-by-Use Leveling framework. Per-player instance holds one
// LifeSkillState per LifeSkillType. XP curves and milestone bonuses are
// static. Hooked from TurnManager.Movement (rest/sprint/walk) and the
// Inventory ConsumableUsed event (food). Milestone stat bonuses fold into
// Player via Player.LifeSkillHpBonus / LifeSkillSpeedBonus / etc. properties
// which the Player stat pipeline reads additively, similar to how
// proficiency and refinement fold into the existing stat totals.
public enum LifeSkillType
{
    Sleep,
    Walking,
    Running,
    Eating,
    // FB-072 — shopkeep haggling. XP per shop transaction (buy or sell).
    // Milestone bonus is a buy-discount / sell-bonus multiplier that stacks
    // multiplicatively on top of the karma shop price multiplier.
    Bargaining,
    // FB-077 — aquatic traversal. XP per water tile stepped. Level gates
    // shallow vs. deep water passability; below the upper threshold each
    // water step costs an extra turn tick (slow penalty).
    Swimming,
    // (Extensible — add Fishing/Mining/Cooking here in later passes.)
}

// Mutable per-skill state: current level (1-99) and XP toward the next level.
public record LifeSkillState
{
    public int Level { get; set; } = 1;
    public int CurrentXp { get; set; } = 0;
}

public class LifeSkillSystem
{
    public const int MaxLevel = 99;

    // Fires when a skill hits a milestone level (10/25/50/99 only).
    // Signature: (skill type, new level).
    public event Action<LifeSkillType, int>? LifeSkillMilestoneReached;

    public Dictionary<LifeSkillType, LifeSkillState> Skills { get; set; } = new();

    public LifeSkillSystem()
    {
        foreach (var t in Enum.GetValues<LifeSkillType>())
            Skills[t] = new LifeSkillState();
    }

    // Geometric XP curve. Anchor points:
    //   L1    → 0      (implicit)
    //   L5    → 10
    //   L10   → 100
    //   L25   → 1000
    //   L50   → 5000
    //   L99   → 50000
    // Piecewise-linear between anchors so the curve is monotonic and tunable.
    private static readonly (int Level, int Xp)[] Anchors =
    {
        (1, 0), (5, 10), (10, 100), (25, 1000), (50, 5000), (99, 50000),
    };

    // Total XP required to reach a given level from L1. For L≤1 returns 0;
    // for L>99 clamps to the L99 anchor.
    public static int GetXpForLevel(int level)
    {
        if (level <= 1) return 0;
        if (level >= MaxLevel) return Anchors[^1].Xp;

        // Find the bracket containing `level`.
        for (int i = 0; i < Anchors.Length - 1; i++)
        {
            var (la, xa) = Anchors[i];
            var (lb, xb) = Anchors[i + 1];
            if (level >= la && level <= lb)
            {
                if (la == lb) return xa;
                // Linear interpolation inside the bracket.
                double t = (double)(level - la) / (lb - la);
                return (int)Math.Round(xa + t * (xb - xa));
            }
        }
        return Anchors[^1].Xp;
    }

    public int GetLevel(LifeSkillType skill) => Skills[skill].Level;
    public int GetXp(LifeSkillType skill) => Skills[skill].CurrentXp;

    // Returns (current level XP, next level XP) so the UI can render a
    // progress bar within the current level. At max level returns
    // (MaxLevelXp, MaxLevelXp) so the bar renders full.
    public (int Current, int Next) GetLevelProgress(LifeSkillType skill)
    {
        var s = Skills[skill];
        if (s.Level >= MaxLevel)
            return (GetXpForLevel(MaxLevel), GetXpForLevel(MaxLevel));
        int cur = GetXpForLevel(s.Level);
        int nxt = GetXpForLevel(s.Level + 1);
        return (s.CurrentXp - cur, nxt - cur);
    }

    // Grants XP to a skill and rolls up levels as thresholds are met.
    // Fires LifeSkillMilestoneReached exactly once per milestone crossed
    // at L10, L25, L50, L99.
    public void GrantXp(LifeSkillType skill, int amount)
    {
        if (amount <= 0) return;
        var s = Skills[skill];
        if (s.Level >= MaxLevel) return;

        s.CurrentXp += amount;
        int oldLevel = s.Level;

        // Roll forward through any level thresholds we cross. Each
        // iteration advances exactly one level so we don't miss milestone
        // bands on a very large single grant.
        while (s.Level < MaxLevel && s.CurrentXp >= GetXpForLevel(s.Level + 1))
        {
            s.Level++;
        }

        // Fire milestone notifications for any crossings in the 10/25/50/99 set.
        if (s.Level > oldLevel)
        {
            foreach (int ms in new[] { 10, 25, 50, MaxLevel })
            {
                if (oldLevel < ms && s.Level >= ms)
                    LifeSkillMilestoneReached?.Invoke(skill, ms);
            }
        }
    }

    // Milestone stat bonus at a given level. Returns the CUMULATIVE bonus
    // the player currently has for `skill` at `level`. (Tiers stack — e.g.
    // a L60 Sleep player gets L10+L25+L50 Sleep bonuses.)
    // Returns a list of (StatType, flatValue) tuples. Multiplier-style
    // bonuses (Running sprint stamina, Eating food potency) are surfaced
    // via EatingFoodMultiplierPercent / RunningSprintStaminaReductionPercent.
    public IEnumerable<(StatType Stat, int Value)> GetFlatStatBonuses(LifeSkillType skill)
    {
        int lvl = Skills[skill].Level;
        switch (skill)
        {
            case LifeSkillType.Sleep:
                if (lvl >= 10) yield return (StatType.Health, 5);
                if (lvl >= 25) yield return (StatType.Health, 15);
                if (lvl >= 50) yield return (StatType.Health, 35);
                if (lvl >= 99) yield return (StatType.Health, 80);
                break;
            case LifeSkillType.Walking:
                if (lvl >= 10) yield return (StatType.Endurance, 2);
                if (lvl >= 25) yield return (StatType.Endurance, 5);
                if (lvl >= 50) yield return (StatType.Endurance, 10);
                if (lvl >= 99) yield return (StatType.Endurance, 20);
                break;
            case LifeSkillType.Running:
                if (lvl >= 10) yield return (StatType.Speed, 2);
                if (lvl >= 25) yield return (StatType.Speed, 5);
                if (lvl >= 50) yield return (StatType.Speed, 10);
                if (lvl >= 99) yield return (StatType.Speed, 20);
                break;
            case LifeSkillType.Eating:
                // Eating's bonus is a multiplier — no flat stat bonus here.
                break;
            case LifeSkillType.Bargaining:
                // Bargaining's bonus is a price multiplier — see BargainingDiscount().
                break;
            case LifeSkillType.Swimming:
                // Swimming is purely a traversal gate — no flat stat bonus.
                break;
        }
    }

    // FB-072 — Bargaining buy-price multiplier (< 1.0 = discount).
    //   L10  0.97   (−3%)
    //   L25  0.94   (−6%)
    //   L50  0.90  (−10%)
    //   L99  0.85  (−15% cap)
    // Returns 1.0 at L<10 (no effect). Stacks multiplicatively on top of
    // the karma shop price multiplier at the ShopDialog call sites.
    public float BargainingBuyMultiplier()
    {
        int lvl = Skills[LifeSkillType.Bargaining].Level;
        if (lvl >= MaxLevel) return 0.85f;
        if (lvl >= 50) return 0.90f;
        if (lvl >= 25) return 0.94f;
        if (lvl >= 10) return 0.97f;
        return 1.0f;
    }

    // Convenience helper for callers that have a Player reference. Null-safe
    // so early boot paths (character creation, test harnesses) can't crash.
    public static float BargainingDiscount(SAOTRPG.Entities.Player? player)
        => player?.LifeSkills.BargainingBuyMultiplier() ?? 1.0f;

    // FB-077 — Swimming level. Water (shallow) passable at L1+; WaterDeep
    // passable at L25+. Below the upper threshold the move costs an extra
    // turn tick (L<10 slow on shallow, L<50 slow on deep).
    public int SwimmingLevel => Skills[LifeSkillType.Swimming].Level;

    // Cumulative flat MaxHP bonus from Sleep skill tiers.
    public int SleepMaxHpBonus()
    {
        int total = 0;
        foreach (var (stat, v) in GetFlatStatBonuses(LifeSkillType.Sleep))
            if (stat == StatType.Health) total += v;
        return total;
    }

    // Cumulative flat Endurance bonus from Walking skill tiers.
    public int WalkingEnduranceBonus()
    {
        int total = 0;
        foreach (var (stat, v) in GetFlatStatBonuses(LifeSkillType.Walking))
            if (stat == StatType.Endurance) total += v;
        return total;
    }

    // Cumulative flat Speed bonus from Running skill tiers.
    public int RunningSpeedBonus()
    {
        int total = 0;
        foreach (var (stat, v) in GetFlatStatBonuses(LifeSkillType.Running))
            if (stat == StatType.Speed) total += v;
        return total;
    }

    // Eating multiplier — food heal/duration scales by (100 + bonus) / 100.
    //   L10  +10%
    //   L25  +25% (replaces L10 — i.e. +25 total, not +35)
    //   L50  +50%
    //   L99 +100% duration
    // Returns 0 at L<10.
    public int EatingFoodPotencyPercent()
    {
        int lvl = Skills[LifeSkillType.Eating].Level;
        if (lvl >= MaxLevel) return 100;
        if (lvl >= 50) return 50;
        if (lvl >= 25) return 25;
        if (lvl >= 10) return 10;
        return 0;
    }

    // Running L99 capstone: sprint stamina cost reduced by 30%. Since
    // sprint doesn't currently cost stamina in the codebase, this is a
    // display-only bonus for now. Wire it up when stamina lands.
    public bool RunningSprintStaminaReduced =>
        Skills[LifeSkillType.Running].Level >= MaxLevel;

    // Sleep L99 capstone: faster HP regen. Consumed by TurnManager passive
    // regen logic to tick the regen-every-N-turns counter more often.
    public bool SleepFasterRegen =>
        Skills[LifeSkillType.Sleep].Level >= MaxLevel;

    // Pretty label for UI rendering.
    public static string Label(LifeSkillType skill) => skill switch
    {
        LifeSkillType.Sleep      => "Sleep",
        LifeSkillType.Walking    => "Walking",
        LifeSkillType.Running    => "Running",
        LifeSkillType.Eating     => "Eating",
        LifeSkillType.Bargaining => "Bargain",
        LifeSkillType.Swimming   => "Swim",
        _                        => skill.ToString(),
    };

    // One-line milestone description for the log banner.
    public static string MilestoneBonusDescription(LifeSkillType skill, int level) => (skill, level) switch
    {
        (LifeSkillType.Sleep,   10) => "+5 MaxHP",
        (LifeSkillType.Sleep,   25) => "+15 MaxHP",
        (LifeSkillType.Sleep,   50) => "+35 MaxHP",
        (LifeSkillType.Sleep,   99) => "+80 MaxHP, faster HP regen",
        (LifeSkillType.Walking, 10) => "+2 Endurance",
        (LifeSkillType.Walking, 25) => "+5 Endurance",
        (LifeSkillType.Walking, 50) => "+10 Endurance",
        (LifeSkillType.Walking, 99) => "+20 Endurance",
        (LifeSkillType.Running, 10) => "+2 Speed",
        (LifeSkillType.Running, 25) => "+5 Speed",
        (LifeSkillType.Running, 50) => "+10 Speed",
        (LifeSkillType.Running, 99) => "+20 Speed, -30% sprint cost",
        (LifeSkillType.Eating,  10) => "+10% food potency",
        (LifeSkillType.Eating,  25) => "+25% food potency",
        (LifeSkillType.Eating,  50) => "+50% food potency",
        (LifeSkillType.Eating,  99) => "+100% food duration",
        (LifeSkillType.Bargaining, 10) => "-3% buy / +3% sell",
        (LifeSkillType.Bargaining, 25) => "-6% buy / +6% sell",
        (LifeSkillType.Bargaining, 50) => "-10% buy / +10% sell",
        (LifeSkillType.Bargaining, 99) => "-15% buy / +15% sell (cap)",
        (LifeSkillType.Swimming,   10) => "Water at full speed",
        (LifeSkillType.Swimming,   25) => "Deep water passable (slow)",
        (LifeSkillType.Swimming,   50) => "Deep water at full speed",
        (LifeSkillType.Swimming,   99) => "Master swimmer",
        _                           => "",
    };
}
