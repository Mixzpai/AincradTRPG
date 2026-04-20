using SAOTRPG.UI;

namespace SAOTRPG.Entities
{
    // Hostile base class — mobs/bosses/mini-bosses. Damage intake, defeat rewards, overkill XP.
    public abstract class Monster : Entity
    {
        // XP multiplier for damage dealt beyond 0 HP.
        private const int OverkillXpMultiplier = 2;

        public int ExperienceYield { get; set; }
        public int ColYield { get; set; }

        // Aquatic mobs — binary Water/WaterDeep gate (no slow penalty). Default false → water is a choke.
        public bool CanSwim { get; set; }

        // Reward data returned on kill; includes overkill info.
        public class DefeatReward
        {
            public int Experience { get; set; }
            public int Col { get; set; }
            public bool WasOverkill { get; set; }
            public int OverkillDamage { get; set; }
        }

        // Returns DefeatReward on kill, null on survive. Overkill grants bonus XP.
        public DefeatReward? TakeDamage(int damage)
        {
            if (IsDefeated)
            {
                _log?.LogCombat($"{Name} is already defeated. No further damage can be applied.");
                return null;
            }

            CurrentHealth -= damage;

            if (CurrentHealth <= 0)
            {
                int overkillDamage = -CurrentHealth;
                CurrentHealth = 0;
                IsDefeated = true;

                int experienceReward = ExperienceYield;
                if (overkillDamage > 0)
                {
                    experienceReward += overkillDamage * OverkillXpMultiplier;
                }

                var reward = new DefeatReward
                {
                    Experience = experienceReward,
                    Col = ColYield,
                    WasOverkill = overkillDamage > 0,
                    OverkillDamage = overkillDamage
                };

                return reward;
            }

            return null;
        }
    }
}
