using System;
using System.Security.Cryptography.X509Certificates;

public class Player
{
    /****************************************************************************************/
    // General Player Creation Details
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Gender { get; set; }
    public string Title { get; set; }

    /****************************************************************************************/
    // General Stats
    public int Level { get; set; }
    public int CurrentExperience { get; set; }
    public int ExperienceRequired => (BaseExperienceRequired + (BaseExperienceRequired * Level));
    public int CurrentHealth { get; set; }
    public int ColOnHand { get; set; }
    public int SkillPoints { get; set; }

    /****************************************************************************************/
    // True Stats
    public int MaxHealth => Vitality * 10;
    public int Attack => BaseAttack + (Strength * 2);
    public int Defense => BaseDefense + (Endurance * 2);
    public int CriticalRate => BaseCriticalRate + (Dexterity * 2);
    public int CriticalHitDamage => BaseCriticalHitDamage + (Attack * 2);
    public int Speed => BaseSpeed + (Agility * 2);
    public int SkillDamage => BaseSkillDamage + (Intelligence * 2);

    /****************************************************************************************/
    // Base Stats
    public int BaseExperienceRequired { get; set; } = 100;
    public int BaseAttack { get; set; } = 10;
    public int BaseDefense { get; set; } = 10;
    public int BaseCriticalRate { get; set; } = 5;
    public int BaseCriticalHitDamage { get; set; } = 10;
    public int BaseSpeed { get; set; } = 10;
    public int BaseSkillDamage { get; set; } = 10;

    /****************************************************************************************/
    // Player Stats
    public int Vitality { get; set; } = 5;
    public int Strength { get; set; } = 5;
    public int Endurance { get; set; } = 5;
    public int Dexterity { get; set; } = 5;
    public int Agility { get; set; } = 5;
    public int Intelligence { get; set; } = 5;

    /****************************************************************************************/
    // Game Over Condition
    public bool IsDefeated { get; private set; }

    /****************************************************************************************/
    // Create a new player with basic information
    public static Player CreateNewPlayer()
    {
        var player = new Player();

        // User input for player creation
        player.Id = GeneratePlayerId();
        
        Console.Write("Enter your character's first name: ");
        player.FirstName = Console.ReadLine();
        
        Console.Write("Enter your character's last name: ");
        player.LastName = Console.ReadLine();
        
        Console.Write("Enter your character's gender: ");
        player.Gender = Console.ReadLine();
        
        player.Title = "Adventurer";

        //Initial Stats
        player.Level = 1;
        player.CurrentExperience = 0;
        player.CurrentHealth = player.MaxHealth;
        player.ColOnHand = 1000;
        player.SkillPoints = 10;

        return player;
    }

    /****************************************************************************************/
    // Generate a random player ID
    private static int GeneratePlayerId()
    {
        return Random.Shared.Next(10000, 99999);
    }

    /****************************************************************************************/
    // Method to apply damage to the player
    public void TakeDamage(int damage)
    {
        if (IsDefeated)
        {
            Console.WriteLine($"{FirstName} {LastName} is already defeated. No further damage can be applied.");
        }
        else {
            CurrentHealth -= damage;
            Console.WriteLine($"{FirstName} {LastName} takes {damage} damage. Current HP: {CurrentHealth}/{MaxHealth}");
            if (CurrentHealth <= 0)
            {
                IsDefeated = true;
                Console.WriteLine($"{FirstName} {LastName} has been defeated!");
            }
        }
    }
    /****************************************************************************************/
    // Method to let the player attack a monster and calculate damage
    public int AttackMonster(Monster monster)
    {
        int damage = Attack;
        bool isCriticalHit = Random.Shared.Next(0, 100) < CriticalRate;
        if (isCriticalHit) {
            damage += (int)CriticalHitDamage;
            Console.WriteLine("Critical Hit!");
        }
        Console.WriteLine($"{FirstName} attacks {monster.Name} for {damage} damage.");
        return damage;
    }
    /****************************************************************************************/
    // Methods to gain experience and handle leveling up
    public void LevelUp()
    {
        Level++;
        SkillPoints += 5; // Award skill points on level up
        Console.WriteLine($"LEVEL UP! {FirstName} has Aquired LVL:{Level}! Skill points available: {SkillPoints}");
    }

    public void GainExperience(int experience)
    {
        CurrentExperience += experience;
        Console.WriteLine($"{FirstName} gains {experience} experience points. Current EXP: {CurrentExperience}/{ExperienceRequired}");
        while (CurrentExperience >= ExperienceRequired)
        {
            // Player Level Up!
            LevelUp();
            // After leveling up, check if there's still enough experience to level up again (carryover)
            if (CurrentExperience >= ExperienceRequired)
            {
                var carryoverExp = CurrentExperience - ExperienceRequired;
                CurrentExperience = carryoverExp;
            }
            else
            {
                CurrentExperience = 0;
            }
        }

    }

    /****************************************************************************************/
    // Method to spend skill points on improving stats
    public void SpendSkillPoints()
    {
        if (SkillPoints <= 0)
        {
            Console.WriteLine($"{FirstName} has no skill points available to spend.");
        }
        else
        {
            // Select what stat to increase
            Console.WriteLine($"Available skill points: {SkillPoints}");
            Console.Write("Enter the stat you want to improve (Vitality, Strength, Endurance, Dexterity, Agility, Intelligence): ");
            var stat = Console.ReadLine();
            if (stat is null)
            {
                Console.WriteLine("Null Input Error!");
                return;
            }

            // Select how many skill points to allocate
            Console.Write($"How many skill points do you want to allocate to {stat}?: ");
            var n = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(n))
            {
                Console.WriteLine("Null Input Error!");
                return;
            }
            if (!int.TryParse(n, out int pointsToAllocate))
            {
                Console.WriteLine("Invalid input! Please enter a valid number.");
                return;
            }
            if (pointsToAllocate <= 0)
            {
                Console.WriteLine("You must allocate at least 1 skill point.");
                return;
            }
            if (pointsToAllocate > SkillPoints)
            {
                Console.WriteLine($"Not enough skill points! You only have {SkillPoints} available.");
                return;
            }

            switch (stat.ToLower())
            {
                case "vitality":
                    Vitality += pointsToAllocate;
                    break;
                case "strength":
                    Strength += pointsToAllocate;
                    break;
                case "endurance":
                    Endurance += pointsToAllocate;
                    break;
                case "dexterity":
                    Dexterity += pointsToAllocate;
                    break;
                case "agility":
                    Agility += pointsToAllocate;
                    break;
                case "intelligence":
                    Intelligence += pointsToAllocate;
                    break;
                default:
                    Console.WriteLine("Invalid stat. Please choose from: Vitality, Strength, Endurance, Dexterity, Agility, Intelligence.");
                    return;
            }
            SkillPoints -= pointsToAllocate;
            Console.WriteLine($"{FirstName} has increased {stat} by {pointsToAllocate}. Remaining skill points: {SkillPoints}");
        }
    }

    /****************************************************************************************/
    // Method to display player's current stats
    public void DisplayPlayerStats()
    {
        Console.WriteLine("=== Player Stats ===");
        Console.WriteLine($"Name: {FirstName} {LastName}");
        Console.WriteLine($"Title: {Title}");
        Console.WriteLine($"Gender: {Gender}");
        Console.WriteLine($"Level: {Level}");
        Console.WriteLine($"Experience: {CurrentExperience}/{ExperienceRequired}");
        Console.WriteLine($"Health: {CurrentHealth}/{MaxHealth}");
        Console.WriteLine($"Col on Hand: {ColOnHand}");
        Console.WriteLine($"Skill Points: {SkillPoints}");
        Console.WriteLine();
        Console.WriteLine("=== Combat Stats ===");
        Console.WriteLine($"Attack: {Attack}");
        Console.WriteLine($"Defense: {Defense}");
        Console.WriteLine($"Critical Rate: {CriticalRate}");
        Console.WriteLine($"Critical Hit Damage: {CriticalHitDamage}");
        Console.WriteLine($"Speed: {Speed}");
        Console.WriteLine($"Skill Damage: {SkillDamage}");
        Console.WriteLine();
        Console.WriteLine("=== Attributes ===");
        Console.WriteLine($"Vitality: {Vitality}");
        Console.WriteLine($"Strength: {Strength}");
        Console.WriteLine($"Endurance: {Endurance}");
        Console.WriteLine($"Dexterity: {Dexterity}");
        Console.WriteLine($"Agility: {Agility}");
        Console.WriteLine($"Intelligence: {Intelligence}");
    }

    /****************************************************************************************/
    //
}