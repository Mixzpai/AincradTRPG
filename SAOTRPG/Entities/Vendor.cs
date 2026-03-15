using Terminal.Gui;
using SAOTRPG.Items;
using SAOTRPG.Items.Definitions;
using SAOTRPG.Items.Definitions.Weapons;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Entities
{
    public class Vendor : NPC
    {
        public override char Symbol { get; protected set; } = 'V';
        public override Color SymbolColor { get; protected set; } = Color.BrightGreen;
        /****************************************************************************************/
        // Vendor-Specific Properties
        public string ShopName { get; set; }
        public List<BaseItem> ShopStock { get; set; } = new();

        /// <summary>
        /// Generate floor-appropriate shop inventory.
        /// </summary>
        public void GenerateStock(int floor)
        {
            ShopStock.Clear();

            // Always sell potions + antidotes
            ShopStock.Add(PotionDefinitions.CreateHealthPotion());
            ShopStock.Add(PotionDefinitions.CreateHealthPotion());
            ShopStock.Add(PotionDefinitions.CreateAntidote());
            ShopStock.Add(FoodDefinitions.CreateBread());
            ShopStock.Add(FoodDefinitions.CreateGrilledMeat());

            if (floor >= 2)
                ShopStock.Add(PotionDefinitions.CreateGreaterHealthPotion());

            // Floor-scaled weapon
            int atkBase = 5 + floor * 3;
            ShopStock.Add(new Weapon
            {
                Name = PickShopWeaponName(floor),
                Value = 80 + floor * 40,
                Rarity = "Common",
                ItemDurability = 40 + floor * 10,
                RequiredLevel = Math.Max(1, floor),
                EquipmentType = "Weapon",
                WeaponType = "One-Handed Sword",
                BaseDamage = atkBase,
                AttackSpeed = 1,
                Range = 1,
                Bonuses = new StatModifierCollection().Add(StatType.Attack, atkBase)
            });

            // Floor-scaled armor
            int defBase = 3 + floor * 2;
            ShopStock.Add(new Armor
            {
                Name = PickShopArmorName(floor),
                Value = 60 + floor * 35,
                Rarity = "Common",
                ItemDurability = 50 + floor * 10,
                RequiredLevel = Math.Max(1, floor),
                EquipmentType = "Armor",
                ArmorSlot = "Chest",
                BaseDefense = defBase,
                Weight = 5,
                Bonuses = new StatModifierCollection().Add(StatType.Defense, defBase)
            });
        }

        private static readonly string[] ShopWeaponNames =
            { "Traveler's Blade", "Militia Sword", "Guard's Saber", "Knight's Longsword", "Veteran's Edge" };
        private static readonly string[] ShopArmorNames =
            { "Padded Vest", "Leather Chestplate", "Chainmail Tunic", "Reinforced Cuirass", "Steel Breastplate" };

        private static string PickShopWeaponName(int floor) =>
            ShopWeaponNames[Math.Min(floor - 1, ShopWeaponNames.Length - 1)];

        private static string PickShopArmorName(int floor) =>
            ShopArmorNames[Math.Min(floor - 1, ShopArmorNames.Length - 1)];
    }
}
