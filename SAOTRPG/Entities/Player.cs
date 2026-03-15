using Terminal.Gui;
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
        public override char Symbol { get; protected set; } = '@';
        public override Color SymbolColor { get; protected set; } = Color.BrightYellow;

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

        public static Player LoadFromSave(Systems.SaveData save, IGameLog log, IInventoryLogger? inventoryLogger = null)
        {
            var player = new Player
            {
                FirstName = save.FirstName,
                LastName = save.LastName,
                Gender = save.Gender,
                Title = save.Title,
                Id = save.PlayerId,
                Level = save.Level,
                CurrentExperience = save.CurrentExperience,
                CurrentHealth = save.CurrentHealth,
                ColOnHand = save.ColOnHand,
                SkillPoints = save.SkillPoints,

                // Attributes
                Strength = save.Strength,
                Vitality = save.Vitality,
                Endurance = save.Endurance,
                Dexterity = save.Dexterity,
                Agility = save.Agility,
                Intelligence = save.Intelligence,

                // Base combat stats (includes baked-in equipment bonuses)
                BaseAttack = save.BaseAttack,
                BaseDefense = save.BaseDefense,
                BaseSpeed = save.BaseSpeed,
                BaseSkillDamage = save.BaseSkillDamage,
                BaseCriticalRate = save.BaseCriticalRate,
                BaseCriticalHitDamage = save.BaseCriticalHitDamage,
            };
            player._log = log;
            player.Inventory = new PlayerInventory(logger: inventoryLogger);

            // Restore backpack items
            foreach (var itemData in save.InventoryItems)
            {
                var item = Systems.SaveManager.DeserializeItem(itemData);
                if (item != null) player.Inventory.AddItem(item);
            }

            // Restore equipped items (bypass stat application — stats already baked into base)
            foreach (var kvp in save.EquippedItems)
            {
                if (Enum.TryParse<EquipmentSlot>(kvp.Key, out var slot))
                {
                    var item = Systems.SaveManager.DeserializeItem(kvp.Value);
                    if (item is Items.Equipment.EquipmentBase equipment)
                    {
                        player.Inventory.ForceEquipForLoad(slot, equipment);
                    }
                }
            }

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

        // ── Level-up flavor text ──────────────────────────────────────
        // Add new lines by adding a string to this array
        private static readonly string[] LevelUpFlavors =
        {
            "  You feel power surge through your veins!",
            "  The world sharpens. You're getting stronger.",
            "  Aincrad's challenges forge you into something greater.",
            "  New strength flows into your limbs. Press onward!",
            "  A warm glow envelops you. You've grown.",
        };

        // ── Normal attack flavor text ──────────────────────────────────
        // Add new lines by adding a string (use {0}=player, {1}=monster, {2}=damage)
        private static readonly string[] AttackFlavors =
        {
            "{0} attacks {1} for {2} damage.",
            "{0} slashes at {1} — {2} damage!",
            "{0} swings at {1}, dealing {2} damage.",
            "{0} strikes {1} squarely for {2} damage.",
            "{0} lands a hit on {1} — {2} damage.",
        };

        // ── Critical hit flavor text ────────────────────────────────────
        // Add new crit lines by adding a string (use {0}=player, {1}=monster, {2}=damage)
        private static readonly string[] CritFlavors =
        {
            "*** CRITICAL HIT! *** {0} strikes {1} for {2} damage!",
            "*** DEVASTATING BLOW! *** {0} cleaves {1} for {2} damage!",
            "*** PERFECT STRIKE! *** {0} finds {1}'s weak point — {2} damage!",
            "*** BRUTAL HIT! *** {0} smashes into {1} for {2} damage!",
            "*** PRECISION STRIKE! *** {0} pierces {1}'s guard — {2} damage!",
        };

        public int AttackMonster(Monster monster)
        {
            int damage = Attack;
            bool isCrit = Random.Shared.Next(0, 100) < CriticalRate;
            if (isCrit)
            {
                damage += (int)CriticalHitDamage;
                string critMsg = string.Format(
                    CritFlavors[Random.Shared.Next(CritFlavors.Length)],
                    FirstName, monster.Name, damage);
                _log?.LogCombat(critMsg);
            }
            else
            {
                string atkMsg = string.Format(
                    AttackFlavors[Random.Shared.Next(AttackFlavors.Length)],
                    FirstName, monster.Name, damage);
                _log?.LogCombat(atkMsg);
            }
            return damage;
        }

        /****************************************************************************************/
        // Leveling
        public void LevelUp()
        {
            Level++;
            SkillPoints += 5;

            // Full HP restore on level up
            CurrentHealth = MaxHealth;

            // Fanfare log
            _log?.LogSystem("════════════════════════════════════");
            _log?.LogSystem($"  ★ LEVEL UP! ★  {FirstName} is now Level {Level}!");
            _log?.LogSystem($"  +5 Skill Points | HP fully restored");
            _log?.LogSystem(LevelUpFlavors[Random.Shared.Next(LevelUpFlavors.Length)]);
            _log?.LogSystem("════════════════════════════════════");
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
