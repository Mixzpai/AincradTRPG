using Terminal.Gui;

namespace SAOTRPG.Entities
{
    // Floor boss entity -- multi-phase fight with unique abilities.
    // Phase transitions at HP thresholds (75%, 50%, 25%).
    // Each boss can have up to 3 special abilities that activate per phase.
    public class Boss : Monster
    {
        public override char Symbol { get; protected set; } = 'B';
        public override Color SymbolColor { get; protected set; } = Color.BrightRed;

        public string BossTitle { get; protected set; } = string.Empty;

        // Phase system: bosses have up to 4 phases based on HP%.
        // Phase 1: 100-75%, Phase 2: 75-50%, Phase 3: 50-25%, Phase 4: 25-0% (enrage).
        public int CurrentPhase => MaxHealth <= 0 ? 1 : CurrentHealth switch
        {
            var hp when hp > MaxHealth * 0.75 => 1,
            var hp when hp > MaxHealth * 0.50 => 2,
            var hp when hp > MaxHealth * 0.25 => 3,
            _ => 4,
        };
        public int LastAnnouncedPhase { get; set; } = 1;

        // Enrage at phase 4 (25% HP).
        public const double EnrageAtkMultiplier = 1.5;
        public bool IsEnraged => CurrentPhase >= 4;
        public bool EnrageAnnounced { get; set; }

        // Boss abilities -- up to 3 unique mechanics.
        public BossAbility[] Abilities { get; set; } = Array.Empty<BossAbility>();
    }

    // A boss special ability that triggers during combat.
    public class BossAbility
    {
        public string Name { get; set; } = "";
        public BossAbilityType Type { get; set; }
        public int MinPhase { get; set; } = 1;    // earliest phase this can trigger
        public int Cooldown { get; set; } = 3;     // turns between uses
        public int LastUsedTurn { get; set; } = -99;
        public double DamageMultiplier { get; set; } = 1.5;
        public int AoERadius { get; set; } = 1;
        public int SummonCount { get; set; } = 2;
        public string? StatusEffect { get; set; }   // "Poison", "Stun", etc.
        public double StatusChance { get; set; } = 0.3;
    }

    public enum BossAbilityType
    {
        HeavyStrike,   // Big single-target hit with telegraph
        AoESlam,       // Hits all adjacent tiles
        SummonMinions,  // Spawns mobs nearby
        HealSelf,      // Recovers HP
        ChargeAttack,   // Rush from range, high damage
        StatusBreath,   // Cone/AoE status effect
    }
}
