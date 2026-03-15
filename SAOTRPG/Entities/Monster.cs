using SAOTRPG.UI;

namespace SAOTRPG.Entities
{
    public abstract class Monster : Entity
    {
        /****************************************************************************************/
        // Monster-Specific Stats
        public int ExperienceYield { get; set; }
        public int ColYield { get; set; }

        /****************************************************************************************/
        // Result structure for when monster is defeated
        public class DefeatReward
        {
            public int Experience { get; set; }
            public int Col { get; set; }
            public bool WasOverkill { get; set; }
            public int OverkillDamage { get; set; }
        }

        /****************************************************************************************/
        // Method to apply damage to the monster
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
                    experienceReward += overkillDamage * 2;
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
        // Get a formatted string of the monster's status (for UI display)
        public string GetStatusDisplay()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(Name);
            sb.AppendLine($"Level: {Level}");
            sb.AppendLine($"HP: {CurrentHealth}/{MaxHealth}");
            sb.AppendLine();
            sb.AppendLine($"ATK: {BaseAttack}  DEF: {BaseDefense}");
            sb.AppendLine($"SPD: {BaseSpeed}");
            if (IsDefeated)
                sb.AppendLine("\n** DEFEATED **");
            return sb.ToString();
        }
    }
}