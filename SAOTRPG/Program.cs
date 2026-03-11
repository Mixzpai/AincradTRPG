using System;


namespace REDACTED_PROJECT_NAME
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Test Player Creation
            Player PlayerInstance = new Player();
            PlayerInstance = Player.CreateNewPlayer();

            Console.WriteLine();
            PlayerInstance.DisplayPlayerStats();

            Console.WriteLine();
            PlayerInstance.SpendSkillPoints();

            Console.WriteLine();
            PlayerInstance.DisplayPlayerStats();

            Console.WriteLine();


            // Test monster
            Console.WriteLine();
            BossMonster illfang = new BossMonster();

            Console.WriteLine($"<<{illfang.Name}>> Encountered!");
            Console.WriteLine($"| LVL:{illfang.Level} | HP:({illfang.MaxHealth}/{illfang.CurrentHealth}) | " +
                $"STR:{illfang.Strength} | END:{illfang.Endurance} | DEX:{illfang.Dexterity} | AGI:{illfang.Agility} | INT:{illfang.Intelligence} |");
            Console.WriteLine();

            while (!illfang.IsDefeated)
            {
                int damage = PlayerInstance.AttackMonster(illfang);
                Console.WriteLine($"{PlayerInstance.FirstName} attacked {illfang.Name} with {damage} damage...");
                Console.WriteLine();

                var reward = illfang.TakeDamage(damage);
                Console.WriteLine();
                
                if (illfang.IsDefeated && reward != null)
                {
                    PlayerInstance.GainExperience(reward.Experience);
                    break;
                }
            }
        }
    }
}