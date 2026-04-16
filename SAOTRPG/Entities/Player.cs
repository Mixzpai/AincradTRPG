using Terminal.Gui;
using SAOTRPG.UI;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Definitions;
using SAOTRPG.Items.Definitions.Weapons;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Inventory.Logging;
using SAOTRPG.Systems;
using PlayerInventory = SAOTRPG.Inventory.Core.Inventory;
using EquipmentSlot = SAOTRPG.Inventory.Core.EquipmentSlot;

namespace SAOTRPG.Entities
{
    // The player character — extends Entity with identity, currency, inventory,
    // experience/leveling, skill point allocation, and equipment-aware combat stats.
    public partial class Player : Entity
    {
        public override char Symbol { get; protected set; } = '@';
        public override Color SymbolColor { get; protected set; } = Color.BrightYellow;

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        public int CurrentExperience { get; set; }
        public int ExperienceRequired => (BaseExperienceRequired + (BaseExperienceRequired * Level));
        public int BaseExperienceRequired { get; set; } = 100;

        // Max HP derived from Vitality: 100 + Vitality * 10.
        public new int MaxHealth => 100 + (Vitality * 10);

        public int ColOnHand { get; set; }
        public int SkillPoints { get; set; }

        // Player's inventory — backpack items and equipped gear.
        public PlayerInventory Inventory { get; private set; } = null!;

        public int Attack => BaseAttack + (Strength * 2) + Inventory.GetTotalEquipmentBonus(StatType.Attack);
        public int Defense => BaseDefense + (Endurance * 2) + Inventory.GetTotalEquipmentBonus(StatType.Defense);
        public int Speed => BaseSpeed + (Agility * 2) + Inventory.GetTotalEquipmentBonus(StatType.Speed);
        public int SkillDamage => BaseSkillDamage + (Intelligence * 2) + Inventory.GetTotalEquipmentBonus(StatType.SkillDamage);

        // Creates a new player with starting gear (Iron Sword + Health Potion) and full HP.
        public static Player CreateNewPlayer(string firstName, string lastName, string gender, IGameLog log, IInventoryLogger? inventoryLogger = null)
        {
            var player = new Player
            {
                FirstName = firstName, LastName = lastName, Gender = gender,
                Title = "Adventurer", Id = Random.Shared.Next(10000, 99999),
                Level = 1, CurrentExperience = 0, ColOnHand = 1000, SkillPoints = 10
            };
            player._log = log;
            player.Inventory = new PlayerInventory(logger: inventoryLogger);
            player.CurrentHealth = player.MaxHealth;
            player.Inventory.AddItem(OneHandedSwordDefinitions.CreateIronSword());
            player.Inventory.AddItem(PotionDefinitions.CreateHealthPotion());
            return player;
        }

        // Reconstructs a player from a save file, restoring inventory and equipped items.
        public static Player LoadFromSave(Systems.SaveData save, IGameLog log, IInventoryLogger? inventoryLogger = null)
        {
            var player = new Player
            {
                FirstName = save.FirstName, LastName = save.LastName,
                Gender = save.Gender, Title = save.Title,
                Id = save.PlayerId, Level = save.Level,
                CurrentExperience = save.CurrentExperience,
                CurrentHealth = save.CurrentHealth,
                ColOnHand = save.ColOnHand, SkillPoints = save.SkillPoints,
                Strength = save.Strength, Vitality = save.Vitality,
                Endurance = save.Endurance, Dexterity = save.Dexterity,
                Agility = save.Agility, Intelligence = save.Intelligence,
                BaseAttack = save.BaseAttack, BaseDefense = save.BaseDefense,
                BaseSpeed = save.BaseSpeed, BaseSkillDamage = save.BaseSkillDamage,
                BaseCriticalRate = save.BaseCriticalRate,
                BaseCriticalHitDamage = save.BaseCriticalHitDamage,
            };
            player._log = log;
            player.Inventory = new PlayerInventory(logger: inventoryLogger);

            foreach (var itemData in save.InventoryItems)
            {
                var item = Systems.SaveManager.DeserializeItem(itemData);
                if (item != null) player.Inventory.AddItem(item);
            }

            foreach (var kvp in save.EquippedItems)
            {
                if (Enum.TryParse<EquipmentSlot>(kvp.Key, out var slot))
                {
                    var item = Systems.SaveManager.DeserializeItem(kvp.Value);
                    if (item is Items.Equipment.EquipmentBase equipment)
                        player.Inventory.ForceEquipForLoad(slot, equipment);
                }
            }

            return player;
        }

        public bool EquipItem(EquipmentBase equipment) => Inventory.Equip(equipment, this);
        public bool UnequipItem(EquipmentSlot slot) => Inventory.Unequip(slot, this);
        public void UseItem(Consumable consumable) => Inventory.UseConsumable(consumable, this);

        // Compact identity + base stat block for the gameplay sidebar.
        // Formatted to fit inside a 48-column panel without wrapping.
        // Additional sections (progress, equipment, status effects, bounty)
        // are appended by GameScreen.RefreshHud.
        public string GetStatsDisplay()
        {
            string sp = SkillPoints > 0 ? $"  ({SkillPoints} SP)" : "";
            return
$@"{FirstName} {LastName}
{Title}  Lv.{Level}{sp}

ATK {Attack,-4} DEF {Defense,-4} SPD {Speed}
CRT {CriticalRate}%   CD +{CriticalHitDamage,-3} SD {SkillDamage}

VIT {Vitality,-3} STR {Strength,-3} END {Endurance,-3}
DEX {Dexterity,-3} AGI {Agility,-3} INT {Intelligence,-3}";
        }
    }
}
