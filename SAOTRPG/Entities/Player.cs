using SAOTRPG.UI;
using YourGame.Items;
using YourGame.Items.Consumables;
using YourGame.Items.Definitions;
using YourGame.Items.Equipment;
using Inventory.Logging;
using PlayerInventory = Inventory.Core.Inventory;
using EquipmentSlot = Inventory.Core.EquipmentSlot;

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

    private static int GeneratePlayerId()
    {
        return new Random().Next(1, 10000);
    }
}