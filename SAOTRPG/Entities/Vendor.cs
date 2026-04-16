using Terminal.Gui;
using SAOTRPG.Items;
using SAOTRPG.Items.Definitions;
using SAOTRPG.Items.Definitions.Weapons;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Systems;

namespace SAOTRPG.Entities
{
    // Shop NPC — sells potions, food, and floor-scaled equipment.
    // Rendered as 'V' in green. Stock is regenerated per floor via GenerateStock.
    public class Vendor : NPC
    {
        public override char Symbol { get; protected set; } = 'V';
        public override Color SymbolColor { get; protected set; } = Color.BrightGreen;

        /****************************************************************************************/
        // Vendor-Specific Properties

        // Shop display name shown in the shop dialog title bar.
        public string ShopName { get; set; } = string.Empty;
        // Current item stock available for purchase.
        public List<BaseItem> ShopStock { get; set; } = new();

        // Generate floor-appropriate shop inventory. Clears existing stock
        // and adds consumables + floor-scaled weapon and armor.
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
            {
                ShopStock.Add(PotionDefinitions.CreateGreaterHealthPotion());
                ShopStock.Add(FoodDefinitions.CreateHoneyBread());
                ShopStock.Add(FoodDefinitions.CreateSpicedJerky());
                ShopStock.Add(DamageItemDefinitions.CreateFireBomb());
            }

            if (floor >= 3)
            {
                ShopStock.Add(FoodDefinitions.CreateFishStew());
                ShopStock.Add(PotionDefinitions.CreateSpeedPotion());
                ShopStock.Add(PotionDefinitions.CreateIronSkinPotion());
                ShopStock.Add(DamageItemDefinitions.CreateSmokeBomb());
                ShopStock.Add(DamageItemDefinitions.CreatePoisonVial());
                ShopStock.Add(PotionDefinitions.CreateEscapeRope());
            }

            if (floor >= 4)
            {
                ShopStock.Add(FoodDefinitions.CreateElvenWaybread());
                ShopStock.Add(DamageItemDefinitions.CreateFlashBomb());
                ShopStock.Add(PotionDefinitions.CreateReviveCrystal());
            }

            // Floor 5+: random accessory
            if (floor >= 5)
            {
                var accPool = new Func<Accessory>[]
                {
                    AccessoryDefinitions.CreateRingOfStrength,
                    AccessoryDefinitions.CreateGuardianRing,
                    AccessoryDefinitions.CreateSwiftBand,
                    AccessoryDefinitions.CreateAgilityNecklace,
                    AccessoryDefinitions.CreateScholarsPendant,
                    AccessoryDefinitions.CreateVitalityCharm,
                };
                ShopStock.Add(accPool[Random.Shared.Next(accPool.Length)]());
            }

            // Floor-scaled equipment — 3-4 random weapons + 1-2 armor.
            int weaponsAdded = 0, weaponTarget = 3 + Random.Shared.Next(0, 2);
            for (int tries = 0; tries < 30 && weaponsAdded < weaponTarget; tries++)
            {
                var item = LootGenerator.CreateRandomEquipment(floor);
                if (item is Weapon w) { ShopStock.Add(w); weaponsAdded++; }
            }

            int armorAdded = 0, armorTarget = 1 + Random.Shared.Next(0, 2);
            for (int tries = 0; tries < 30 && armorAdded < armorTarget; tries++)
            {
                var item = LootGenerator.CreateRandomEquipment(floor);
                if (item is Armor a) { ShopStock.Add(a); armorAdded++; }
            }

            // Mark up vendor prices by 20% over loot value
            foreach (var item in ShopStock)
                item.Value = (int)(item.Value * 1.2);
        }
    }
}
