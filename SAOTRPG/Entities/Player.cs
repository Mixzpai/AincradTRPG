using SAOTRPG.UI;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Definitions;
using SAOTRPG.Items.Definitions.Weapons;
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
        public override int MaxHealth => 100 + (Vitality * 10);

        /****************************************************************************************/
        // Currency & Points
        public int ColOnHand { get; set; }
        public int SkillPoints { get; set; }

        /****************************************************************************************/
        // Inventory
        public PlayerInventory Inventory { get; private set; }

        /****************************************************************************************/
        // Base Attribute Allocations (skill-point invested values)
        private int _baseVitality;
        private int _baseStrength;
        private int _baseEndurance;
        private int _baseDexterity;
        private int _baseAgility;
        private int _baseIntelligence;

        /****************************************************************************************/
        // Attributes — base allocation + equipment bonuses (overrides Entity members)
        public override int Vitality
        {
            get => _baseVitality + Inventory.GetTotalEquipmentBonus(StatType.Vitality);
            set => _baseVitality = value;
        }
        public override int Strength
        {
            get => _baseStrength + Inventory.GetTotalEquipmentBonus(StatType.Strength);
            set => _baseStrength = value;
        }
        public override int Endurance
        {
            get => _baseEndurance + Inventory.GetTotalEquipmentBonus(StatType.Endurance);
            set => _baseEndurance = value;
        }
        public override int Dexterity
        {
            get => _baseDexterity + Inventory.GetTotalEquipmentBonus(StatType.Dexterity);
            set => _baseDexterity = value;
        }
        public override int Agility
        {
            get => _baseAgility + Inventory.GetTotalEquipmentBonus(StatType.Agility);
            set => _baseAgility = value;
        }
        public override int Intelligence
        {
            get => _baseIntelligence + Inventory.GetTotalEquipmentBonus(StatType.Intelligence);
            set => _baseIntelligence = value;
        }

        /****************************************************************************************/
        // Combat Stats — base + scaling + equipment
        public int Attack => BaseAttack + (Strength * 2) + Inventory.GetEquippedWeaponDamage() + Inventory.GetTotalEquipmentBonus(StatType.Attack);
        public int Defense => BaseDefense + (Endurance * 2) + Inventory.GetTotalEquipmentBonus(StatType.Defense);
        public int Speed => BaseSpeed + (Agility * 2) + Inventory.GetTotalEquipmentBonus(StatType.Speed);
        public int SkillDamage => BaseSkillDamage + (Intelligence * 2) + Inventory.GetTotalEquipmentBonus(StatType.SkillDamage);
        public override int CriticalRate => BaseCriticalRate + Dexterity;
        public override int CriticalHitDamage => BaseCriticalHitDamage + (Dexterity * 2);

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

            //
            player._log = log;
            player.Inventory = new PlayerInventory(logger: inventoryLogger);
            player.CurrentHealth = player.MaxHealth;

            // Equip starting gear
            player.EquipItem(WeaponDefinitions.CreateIronSword());

            // Initial items (Gift to player)
            player.Inventory.AddItem(PotionDefinitions.CreateHealthPotion().WithQuantity<Potion>(5));

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
                damage += CriticalHitDamage;
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
                case "vitality": _baseVitality += points; break;
                case "strength": _baseStrength += points; break;
                case "endurance": _baseEndurance += points; break;
                case "dexterity": _baseDexterity += points; break;
                case "agility": _baseAgility += points; break;
                case "intelligence": _baseIntelligence += points; break;
                default: return false;
            }

            SkillPoints -= points;
            return true;
        }

        /// <summary>
        /// Resets all stat allocations — reclaims spent points from current base stats
        /// back into SkillPoints. Works at any point in the game regardless of level.
        /// </summary>
        public void ResetStatAllocation()
        {
            // Sum all points currently invested in base stats
            int spent = _baseVitality + _baseStrength + _baseEndurance + _baseDexterity + _baseAgility + _baseIntelligence;

            // Refund spent points
            SkillPoints += spent;

            // Zero out all base stats
            _baseVitality = 0;
            _baseStrength = 0;
            _baseEndurance = 0;
            _baseDexterity = 0;
            _baseAgility = 0;
            _baseIntelligence = 0;

            // Recalculate health with new MaxHealth (Vitality is now 0 + equipment)
            CurrentHealth = MaxHealth;
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
