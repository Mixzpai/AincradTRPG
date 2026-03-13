using SAOTRPG.UI;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Definitions;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Inventory.Logging;
using PlayerInventory = SAOTRPG.Inventory.Core.Inventory;
using EquipmentSlot = SAOTRPG.Inventory.Core.EquipmentSlot;

namespace SAOTRPG.Entities
{
    public class Player : Entity
    {
        /****************************************************************************************/
        // Player Details
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
        // Health — computed from Vitality, overrides Entity's flat MaxHealth
        public new int MaxHealth => 100 + (Vitality * 10);

        /****************************************************************************************/
        // Currency & Points
        public int ColOnHand { get; set; }
        public int SkillPoints { get; set; }

        /****************************************************************************************/
        // Inventory
        public PlayerInventory Inventory { get; private set; }

        /****************************************************************************************/
        // Combat Stats — base + scaling + equipment
        public int Attack => BaseAttack + (Strength * 2) + Inventory.GetTotalEquipmentBonus(StatType.Attack);
        public int Defense => BaseDefense + (Endurance * 2) + Inventory.GetTotalEquipmentBonus(StatType.Defense);
        public int Speed => BaseSpeed + (Agility * 2) + Inventory.GetTotalEquipmentBonus(StatType.Speed);
        public int SkillDamage => BaseSkillDamage + (Intelligence * 2) + Inventory.GetTotalEquipmentBonus(StatType.SkillDamage);

        /****************************************************************************************/
        // Factory
        public static Player CreateNewPlayer(string firstName, string lastName, string gender, IGameLog log, IInventoryLogger? inventoryLogger = null)
        {
            var player = new Player
            {
                FirstName = firstName,
                LastName = lastName,
                Gender = gender,
                Title = "Adventurer",
                Id = Random.Shared.Next(10000, 99999),
                Level = 1,
                CurrentExperience = 0,
                ColOnHand = 1000,
                SkillPoints = 10
            };
            player._log = log;
            player.Inventory = new PlayerInventory(logger: inventoryLogger);
            player.CurrentHealth = player.MaxHealth;

            player.Inventory.AddItem(WeaponDefinitions.CreateIronSword());
            player.Inventory.AddItem(PotionDefinitions.CreateHealthPotion());

            return player;
        }

        /****************************************************************************************/
        // Equipment
        public bool EquipItem(EquipmentBase equipment) => Inventory.Equip(equipment, this);
        public bool UnequipItem(EquipmentSlot slot) => Inventory.Unequip(slot, this);
        public void UseItem(Consumable consumable) => Inventory.UseConsumable(consumable, this);

        /****************************************************************************************/
        // Combat
        public void TakeDamage(int damage)
        {
            if (IsDefeated) { _log?.LogCombat($"{FirstName} {LastName} is already defeated."); return; }

            CurrentHealth -= damage;
            _log?.LogCombat($"{FirstName} {LastName} takes {damage} damage. HP: {CurrentHealth}/{MaxHealth}");
            DebugLogger.LogState($"Player \"{FirstName}\" took {damage} dmg", $"HP:{CurrentHealth}/{MaxHealth}");

            if (CurrentHealth <= 0)
            {
                IsDefeated = true;
                _log?.LogCombat($"{FirstName} {LastName} has been defeated!");
            }
        }

        public int AttackMonster(Monster monster)
        {
            int damage = Attack;
            bool isCrit = Random.Shared.Next(0, 100) < CriticalRate;
            if (isCrit)
            {
                damage += (int)CriticalHitDamage;
                _log?.LogCombat("Critical Hit!");
            }
            _log?.LogCombat($"{FirstName} attacks {monster.Name} for {damage} damage.");
            return damage;
        }

        /****************************************************************************************/
        // Leveling
        public void LevelUp()
        {
            Level++;
            SkillPoints += 5;
            _log?.LogSystem($"LEVEL UP! {FirstName} has Acquired LVL:{Level}! Skill points available: {SkillPoints}");
            DebugLogger.LogState($"Player \"{FirstName}\" leveled up", $"LVL:{Level} HP:{CurrentHealth}/{MaxHealth} ATK:{Attack} DEF:{Defense} SP:{SkillPoints}");
        }

        public void GainExperience(int experience)
        {
            CurrentExperience += experience;
            _log?.LogSystem($"{FirstName} gains {experience} EXP. Current: {CurrentExperience}/{ExperienceRequired}");
            while (CurrentExperience >= ExperienceRequired)
            {
                CurrentExperience -= ExperienceRequired;
                LevelUp();
            }
        }

        /****************************************************************************************/
        // Skill Point Allocation
        public bool SpendSkillPoints(string statName, int points)
        {
            if (points <= 0 || points > SkillPoints) return false;

            switch (statName.ToLower())
            {
                case "vitality": Vitality += points; break;
                case "strength": Strength += points; break;
                case "endurance": Endurance += points; break;
                case "dexterity": Dexterity += points; break;
                case "agility": Agility += points; break;
                case "intelligence": Intelligence += points; break;
                default: return false;
            }

            SkillPoints -= points;
            return true;
        }

        /****************************************************************************************/
        // Display
        public string GetStatsDisplay() =>
$@"{FirstName} {LastName}
Title: {Title}
Level: {Level}
EXP: {CurrentExperience}/{ExperienceRequired}
HP: {CurrentHealth}/{MaxHealth}
Col: {ColOnHand}
Skill Points: {SkillPoints}

── Combat ──
ATK: {Attack}  DEF: {Defense}
SPD: {Speed}  SDMG: {SkillDamage}
CRIT: {CriticalRate}%  CDMG: +{CriticalHitDamage}

── Attributes ──
VIT: {Vitality}  STR: {Strength}
END: {Endurance}  DEX: {Dexterity}
AGI: {Agility}  INT: {Intelligence}";
    }
}
