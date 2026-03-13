namespace SAOTRPG.Entities
{
    public class IllfangTheKobaldLord : Boss
    {
        public IllfangTheKobaldLord()
        {
            Id = 7;
            Name = "Illfang the Kobald Lord";
            BossTitle = "Floor 1 Boss";
            Level = 15;
            MaxPhases = 2;

            // Stats
            Vitality = 8;
            Strength = 6;
            Endurance = 5;
            Dexterity = 4;
            Agility = 3;
            Intelligence = 2;

            // Base Stats
            BaseAttack = 10;
            BaseCriticalRate = 5;
            BaseCriticalHitDamage = 10;
            BaseDefense = 8;
            BaseSpeed = 6;
            BaseSkillDamage = 4;

            // Rewards
            ExperienceYield = 1000;
            ColYield = 5000;

            MaxHealth = 100 + (Vitality * 10);
            CurrentHealth = MaxHealth;
        }
    }
}
