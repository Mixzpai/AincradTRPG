using System;

public abstract class Monster
{
    /****************************************************************************************/
    // Monster's True Stats
    public int Id { get; protected set; }
    public string Name { get; protected set; }
    public int Level { get; protected set; }
    public int MaxHealth => Vitality * 10;
    public int CurrentHealth { get; protected set; }
    public int ExperienceYield { get; protected set; }
    public int ColYield { get; protected set; }
    public int Attack => BaseAttack + (Strength * 2);
    public int CriticalRate => BaseCriticalRate + (Dexterity * 2);
    public double CriticalHitDamage => BaseCriticalHitDamage + (Attack * 1.5);
    public int Defense => BaseDefense + (Endurance * 2);
    public int Speed => BaseSpeed + (Dexterity * 2);
    public int SkillDamage => BaseSkillDamage + (Intelligence * 2);

    /****************************************************************************************/
    // Monster's Base Stats
    public int BaseAttack { get; protected set; }
    public int BaseCriticalRate { get; protected set; }
    public int BaseCriticalHitDamage { get; protected set; }
    public int BaseDefense { get; protected set; }
    public int BaseSpeed { get; protected set; }
    public int BaseSkillDamage { get; protected set; }

    /****************************************************************************************/
    // Monster's Stats
    public int Vitality { get; protected set; }
    public int Strength { get; protected set; }
    public int Endurance { get; protected set; }
    public int Dexterity { get; protected set; }
    public int Agility { get; protected set; }
    public int Intelligence { get; protected set; }

    public bool IsDefeated { get; private set; }

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
    public DefeatReward TakeDamage(int damage)
    {
        // If already defeated, no further damage can be applied
        if (IsDefeated)
        {
            Console.WriteLine($"{Name} is already defeated. No further damage can be applied.");
            return null;
        }

        int previousHealth = CurrentHealth;
        CurrentHealth -= damage;

        // Check if defeated
        if (CurrentHealth <= 0)
        {
            int overkillDamage = -CurrentHealth;
            CurrentHealth = 0;
            IsDefeated = true;

            // Calculate rewards with overkill bonus
            int experienceReward = ExperienceYield;
            if (overkillDamage > 0)
            {
                // Bonus experience for excessive damage (2x the overkill amount)
                experienceReward += overkillDamage * 2;
            }

            // Return the rewards for defeating the monster
            // Note: Col reward does not increase with overkill
            var reward = new DefeatReward
            {
                Experience = experienceReward,
                Col = ColYield,
                WasOverkill = overkillDamage > 0,
                OverkillDamage = overkillDamage
            };

            // When the monster is defeated, print and return rewards
            PrintReward(reward);
            return reward;
        }

        // Not defeated yet, no rewards
        return null;
    }

    /****************************************************************************************/
    // Method to print rewards when monster is defeated
    public void PrintReward(DefeatReward reward)
    {
        Console.WriteLine($"\n{Name} has been defeated!");
        Console.WriteLine($"Experience Gained: {reward.Experience}");
        Console.WriteLine($"Col Gained: {reward.Col}");
        if (reward.WasOverkill)
        {
            Console.WriteLine($"Overkill Bonus: {reward.OverkillDamage * 2} experience!");
        }
    }

    /****************************************************************************************/
    // Method to print the monster's current status
    public void PrintStatus()
    {
        Console.WriteLine($"{Name} - Level {Level}");
        Console.WriteLine($"Health: {CurrentHealth}/{MaxHealth}");
        Console.WriteLine($"Attack: {Attack}, Defense: {Defense}, Speed: {Speed}");
        Console.WriteLine();
    }

    /****************************************************************************************/
}
