using SAOTRPG.Items;

namespace SAOTRPG.Systems;

// Skill-by-use leveling (per-player LifeSkillState by type). XP hooks in
// TurnManager.Movement + Inventory.ConsumableUsed; milestones additive via LifeSkill*Bonus.
public enum LifeSkillType
{
    Sleep,
    Walking,
    Running,
    Eating,
    // FB-072 — haggling: XP per shop txn; buy-discount/sell-bonus mult
    // stacks multiplicatively over karma shop mult.
    Bargaining,
    // FB-077 — swim: XP per water tile; gates shallow/deep; slow below threshold.
    Swimming,
    // Bundle 10 — pickaxe-strike XP. Iron=4, Mithril=9, Divine=18 per strike.
    // Milestones at 10/25/50/99 ramp drop chance, durability damage reduction, bonus rolls.
    Mining,
}

public record LifeSkillState
{
    public int Level { get; set; } = 1;
    public int CurrentXp { get; set; } = 0;
}

public class LifeSkillSystem
{
    public const int MaxLevel = 99;

    // Fires on milestone crossings (10/25/50/99): (skill, newLevel).
    public event Action<LifeSkillType, int>? LifeSkillMilestoneReached;

    public Dictionary<LifeSkillType, LifeSkillState> Skills { get; set; } = new();

    public LifeSkillSystem()
    {
        foreach (var t in Enum.GetValues<LifeSkillType>())
            Skills[t] = new LifeSkillState();
    }

    // Piecewise-linear XP curve. Anchors: L1→0, L5→10, L10→100, L25→1k, L50→5k, L99→50k.
    private static readonly (int Level, int Xp)[] Anchors =
    {
        (1, 0), (5, 10), (10, 100), (25, 1000), (50, 5000), (99, 50000),
    };

    // Total XP from L1 to reach `level`. L≤1→0; L>99 clamps to L99 anchor.
    public static int GetXpForLevel(int level)
    {
        if (level <= 1) return 0;
        if (level >= MaxLevel) return Anchors[^1].Xp;

        for (int i = 0; i < Anchors.Length - 1; i++)
        {
            var (la, xa) = Anchors[i];
            var (lb, xb) = Anchors[i + 1];
            if (level >= la && level <= lb)
            {
                if (la == lb) return xa;
                double t = (double)(level - la) / (lb - la);
                return (int)Math.Round(xa + t * (xb - xa));
            }
        }
        return Anchors[^1].Xp;
    }

    public int GetLevel(LifeSkillType skill) => Skills[skill].Level;
    public int GetXp(LifeSkillType skill) => Skills[skill].CurrentXp;

    // (XP into level, XP span to next) for progress bar. At max, both = full.
    public (int Current, int Next) GetLevelProgress(LifeSkillType skill)
    {
        var s = Skills[skill];
        if (s.Level >= MaxLevel)
            return (GetXpForLevel(MaxLevel), GetXpForLevel(MaxLevel));
        int cur = GetXpForLevel(s.Level);
        int nxt = GetXpForLevel(s.Level + 1);
        return (s.CurrentXp - cur, nxt - cur);
    }

    // Grants XP, rolls up levels, fires MilestoneReached per 10/25/50/99 crossing.
    public void GrantXp(LifeSkillType skill, int amount)
    {
        if (amount <= 0) return;
        var s = Skills[skill];
        if (s.Level >= MaxLevel) return;

        s.CurrentXp += amount;
        int oldLevel = s.Level;

        // One-level-per-iter so large grants don't skip milestone bands.
        while (s.Level < MaxLevel && s.CurrentXp >= GetXpForLevel(s.Level + 1))
        {
            s.Level++;
        }

        // Fire milestone events for any crossings in 10/25/50/99.
        if (s.Level > oldLevel)
        {
            foreach (int ms in new[] { 10, 25, 50, MaxLevel })
            {
                if (oldLevel < ms && s.Level >= ms)
                    LifeSkillMilestoneReached?.Invoke(skill, ms);
            }
        }
    }

    // Cumulative flat bonuses at current level — tiers stack (L60 Sleep = L10+L25+L50).
    // Multiplier bonuses (Eating potency, Running stamina) live in separate helpers.
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
            case LifeSkillType.Eating:      break; // multiplier-only
            case LifeSkillType.Bargaining:  break; // price-mult only (BargainingDiscount)
            case LifeSkillType.Swimming:    break; // traversal gate only
            case LifeSkillType.Mining:      break; // multiplier-only (drop chance, dur reduction)
        }
    }

    // FB-072 — buy multiplier: L10=0.97, L25=0.94, L50=0.90, L99=0.85 (cap).
    // Stacks multiplicatively with karma shop mult in ShopDialog.
    public float BargainingBuyMultiplier()
    {
        int lvl = Skills[LifeSkillType.Bargaining].Level;
        if (lvl >= MaxLevel) return 0.85f;
        if (lvl >= 50) return 0.90f;
        if (lvl >= 25) return 0.94f;
        if (lvl >= 10) return 0.97f;
        return 1.0f;
    }

    // Null-safe helper — safe from boot paths (character creation, test harnesses).
    public static float BargainingDiscount(SAOTRPG.Entities.Player? player)
        => player?.LifeSkills.BargainingBuyMultiplier() ?? 1.0f;

    // FB-077 — Swimming level. Shallow L1+, Deep L25+. Slow tick L<10/L<50.
    public int SwimmingLevel => Skills[LifeSkillType.Swimming].Level;

    public int SleepMaxHpBonus()
    {
        int total = 0;
        foreach (var (stat, v) in GetFlatStatBonuses(LifeSkillType.Sleep))
            if (stat == StatType.Health) total += v;
        return total;
    }

    public int WalkingEnduranceBonus()
    {
        int total = 0;
        foreach (var (stat, v) in GetFlatStatBonuses(LifeSkillType.Walking))
            if (stat == StatType.Endurance) total += v;
        return total;
    }

    public int RunningSpeedBonus()
    {
        int total = 0;
        foreach (var (stat, v) in GetFlatStatBonuses(LifeSkillType.Running))
            if (stat == StatType.Speed) total += v;
        return total;
    }

    // Eating mult: L10=+10%, L25=+25%, L50=+50%, L99=+100% duration. Tiers replace (not stack).
    public int EatingFoodPotencyPercent()
    {
        int lvl = Skills[LifeSkillType.Eating].Level;
        if (lvl >= MaxLevel) return 100;
        if (lvl >= 50) return 50;
        if (lvl >= 25) return 25;
        if (lvl >= 10) return 10;
        return 0;
    }

    // Running L99: -30% sprint stamina. Display-only until stamina system lands.
    public bool RunningSprintStaminaReduced =>
        Skills[LifeSkillType.Running].Level >= MaxLevel;

    // Sleep L99: faster passive regen counter in TurnManager.PassiveRegen.
    public bool SleepFasterRegen =>
        Skills[LifeSkillType.Sleep].Level >= MaxLevel;

    // Bundle 10 — Mining tier helpers. Drop-chance bonus tiered: 0/5/10/15/25.
    public int MiningOreDropBonusPercent()
    {
        int lvl = Skills[LifeSkillType.Mining].Level;
        if (lvl >= MaxLevel) return 25;
        if (lvl >= 50) return 15;
        if (lvl >= 25) return 10;
        if (lvl >= 10) return 5;
        return 0;
    }

    // L99: durability damage halved overall (consumer rounds odd strikes up).
    public bool MiningDurabilityHalved =>
        Skills[LifeSkillType.Mining].Level >= MaxLevel;

    // L10: skip durability tick on every other strike.
    public bool MiningEveryOtherStrikeFree =>
        Skills[LifeSkillType.Mining].Level >= 10;

    // L25: 20% chance for an extra ore roll per vein strike.
    public int MiningBonusOreRollPercent =>
        Skills[LifeSkillType.Mining].Level >= 25 ? 20 : 0;

    // L50: Iron veins cost one fewer strike to deplete; mining content layer reads this.
    public bool MiningIronStrikeDiscount =>
        Skills[LifeSkillType.Mining].Level >= 50;

    // L50: Mithril vein guaranteed +1 Adamant Trace on depletion (content layer reads this).
    public bool MiningMithrilAdamantBonus =>
        Skills[LifeSkillType.Mining].Level >= 50;

    public static string Label(LifeSkillType skill) => skill switch
    {
        LifeSkillType.Sleep      => "Sleep",
        LifeSkillType.Walking    => "Walking",
        LifeSkillType.Running    => "Running",
        LifeSkillType.Eating     => "Eating",
        LifeSkillType.Bargaining => "Bargain",
        LifeSkillType.Swimming   => "Swim",
        LifeSkillType.Mining     => "Mining",
        _                        => skill.ToString(),
    };

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
        (LifeSkillType.Mining,     10) => "+5% ore drop chance",
        (LifeSkillType.Mining,     25) => "+1 bonus ore roll (20%)",
        (LifeSkillType.Mining,     50) => "Mithril +1 Adamant Trace; Iron costs -1 strike",
        (LifeSkillType.Mining,     99) => "+20% Divine vein drops; durability damage halved",
        _                           => "",
    };
}
