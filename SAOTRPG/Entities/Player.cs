using SAOTRPG.UI;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Definitions;
using SAOTRPG.Items.Equipment;
using Inventory.Logging;
using PlayerInventory = Inventory.Core.Inventory;
using EquipmentSlot = Inventory.Core.EquipmentSlot;

namespace SAOTRPG.Entities
{
    public class Player : Entity
    {
        /****************************************************************************************/
        // Player-Specific Details
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Gender { get; set; }
        public string Title { get; set; }

        /****************************************************************************************/
        // Experience
        public int CurrentExperience { get; set; }
        public int ExperienceRequired => (BaseExperienceRequired + (BaseExperienceRequired * Level));
        public int BaseExperienceRequired { get; set; } = 100;

        /****************************************************************************************/
        // Currency
        public int ColOnHand { get; set; }
        public int SkillPoints { get; set; }

        /****************************************************************************************/
        // Inventory and Equipment
        public PlayerInventory Inventory { get; private set; }

        /****************************************************************************************/
        // Override stats to include equipment bonuses
        public new int Attack => BaseAttack + (Strength * 2) + Inventory.GetTotalEquipmentBonus(StatType.Attack);
        public new int Defense => BaseDefense + (Endurance * 2) + Inventory.GetTotalEquipmentBonus(StatType.Defense);
        public new int Speed => BaseSpeed + (Agility * 2) + Inventory.GetTotalEquipmentBonus(StatType.Speed);
        public new int SkillDamage => BaseSkillDamage + (Intelligence * 2) + Inventory.GetTotalEquipmentBonus(StatType.SkillDamage);

        /****************************************************************************************/
        // Create a new player with basic information
        public static Player CreateNewPlayer(string firstName, string lastName, string gender, IGameLog log, IInventoryLogger? inventoryLogger = null)
        {
            var player = new Player();
            player._log = log;
            player.Inventory = new PlayerInventory(logger: inventoryLogger);

            player.Id = GeneratePlayerId();
            player.FirstName = firstName;
            player.LastName = lastName;
            player.Gender = gender;
            player.Title = "Adventurer";

            player.Level = 1;
            player.CurrentExperience = 0;
            player.CurrentHealth = player.MaxHealth;
            player.ColOnHand = 1000;
            player.SkillPoints = 10;

            player.Inventory.AddItem(WeaponDefinitions.IronSword);
            player.Inventory.AddItem(PotionDefinitions.HealthPotion);

            return player;
        }

        /****************************************************************************************/
        // Generate a random player ID
        private static int GeneratePlayerId()
        {
            return Random.Shared.Next(10000, 99999);
        }

        /****************************************************************************************/
        // Equipment Methods
        public bool EquipItem(Equipment equipment)
        {
            return Inventory.Equip(equipment, this);
        }

        public bool UnequipItem(EquipmentSlot slot)
        {
            return Inventory.Unequip(slot, this);
        }

        public void UseItem(Consumable consumable)
        {
            Inventory.UseConsumable(consumable, this);
        }

        /****************************************************************************************/
        // Method to apply damage to the player
        public void TakeDamage(int damage)
        {
            if (IsDefeated)
            {
                Console.WriteLine($"{FirstName} {LastName} is already defeated. No further damage can be applied.");
            }
            else
            {
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
            if (isCriticalHit)
            {
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
}