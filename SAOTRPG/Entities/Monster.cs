using SAOTRPG.UI;

namespace SAOTRPG.Entities
{
    // Base class for all hostile entities — mobs, bosses, mini-bosses.
    // Handles damage intake, defeat rewards, and overkill XP bonuses.
    public abstract class Monster : Entity
    {
        // Bonus XP multiplier applied to overkill damage (damage beyond 0 HP).
        private const int OverkillXpMultiplier = 2;

        /****************************************************************************************/
        // Monster-Specific Stats

        // Base experience points awarded on defeat (before overkill bonus).
        public int ExperienceYield { get; set; }
        // Col (gold) awarded on defeat.
        public int ColYield { get; set; }

        // FB-077 — Aquatic mobs can enter Water + WaterDeep tiles. Mirrors
        // the player's Swimming skill gate, but binary (no slow penalty).
        // Set per template in MobFactory; defaults false so land-bound mobs
        // still treat water as a choke point.
        public bool CanSwim { get; set; }

        /****************************************************************************************/
        // Result structure for when monster is defeated

        // Reward data returned when a monster is killed. Includes overkill info.
        public class DefeatReward
        {
            // Total XP earned (base + overkill bonus).
            public int Experience { get; set; }
            // Col earned from the kill.
            public int Col { get; set; }
            // True if the killing blow dealt more damage than remaining HP.
            public bool WasOverkill { get; set; }
            // Excess damage beyond 0 HP (used for overkill display).
            public int OverkillDamage { get; set; }
        }

        /****************************************************************************************/
        // Damage & Defeat

        // Apply damage to this monster. Returns a DefeatReward if the
        // monster dies, or null if it survives. Overkill damage grants bonus XP.
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

        /****************************************************************************************/
        // Status Display

    }
}
