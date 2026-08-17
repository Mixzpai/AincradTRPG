using Terminal.Gui;
using SAOTRPG.Items;
using SAOTRPG.Items.Definitions;
using SAOTRPG.Items.Definitions.Weapons;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Systems;

namespace SAOTRPG.Entities
{
    // Shop NPC — potions/food + floor-scaled gear. Stock regenerates per floor via GenerateStock.
    public class Vendor : NPC
    {
        public override char Symbol { get; protected set; } = 'V';
        public override Color SymbolColor { get; protected set; } = Color.BrightGreen;

        public string ShopName { get; set; } = string.Empty;
        public List<BaseItem> ShopStock { get; set; } = new();

        // Floor-appropriate inventory: consumables + scaled weapon/armor.
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

            // Pickaxes: Wooden F1-9, Iron F10-50. Mithril stays find-only.
            if (floor >= 1 && floor <= 9)
                ShopStock.Add(PickaxeDefinitions.CreateWoodenPickaxe());
            else if (floor >= 10 && floor <= 50)
                ShopStock.Add(PickaxeDefinitions.CreateIronPickaxe());

            // Floor 5+: random accessory
            if (floor >= 5)
            {
                var accPool = LootGenerator.AccessoryPool;
                ShopStock.Add(accPool[RunRng.Next(accPool.Length)]());
            }

            // The baseline ladder, which the Player Guide tells the player to buy here:
            // "Upgrade at vendors as you cross the listed floor thresholds." Before this the
            // shop sold only PROCEDURAL gear, so every hand-authored starter and mid-tier
            // weapon was unobtainable and a player who came looking for a Steel Rapier found
            // a randomly generated one at best.
            foreach (var (minFloor, defId) in BaselineStock)
            {
                if (floor < minFloor) continue;
                var baseItem = ItemRegistry.Create(defId);
                if (baseItem != null) ShopStock.Add(baseItem);
            }

            // Utility crystals. Canon buys these in town, and every one of them has a live
            // effect in TurnManager.HandleCrystal.
            foreach (var (minFloor, defId) in CrystalStock)
            {
                if (floor < minFloor) continue;
                var crystal = ItemRegistry.Create(defId);
                if (crystal != null) ShopStock.Add(crystal);
            }

            // Floor-scaled equipment — 3-4 random weapons + 1-2 armor.
            int weaponsAdded = 0, weaponTarget = 3 + RunRng.Next(0, 2);
            for (int tries = 0; tries < 30 && weaponsAdded < weaponTarget; tries++)
            {
                var item = LootGenerator.CreateRandomEquipment(floor);
                if (item is Weapon w) { ShopStock.Add(w); weaponsAdded++; }
            }

            int armorAdded = 0, armorTarget = 1 + RunRng.Next(0, 2);
            for (int tries = 0; tries < 30 && armorAdded < armorTarget; tries++)
            {
                var item = LootGenerator.CreateRandomEquipment(floor);
                if (item is Armor a) { ShopStock.Add(a); armorAdded++; }
            }

            // Mark up vendor prices by 20% over loot value
            foreach (var item in ShopStock)
                item.Value = (int)(item.Value * 1.2);
        }

        // Baseline weapons and armour by the floor they unlock at, matching the Guide's
        // Iron Lv1 / Steel Lv10 / Mythril Lv25 ladder. Adamantite and Celestial weapons are
        // NOT here: ShopTierSystem already gates those behind an F50+ boss clear, and listing
        // them twice would let a player buy a tier the clear is supposed to unlock.
        private static readonly (int MinFloor, string DefId)[] BaselineStock =
        {
            // Iron tier — one starter per weapon class, so no class starts with nothing.
            (1,  "rusty_dagger"),   (1,  "wooden_club"),   (1,  "short_bow"),
            (1,  "iron_katana"),    (1,  "copper_rapier"), (1,  "iron_scimitar"),
            (1,  "iron_scythe"),    (1,  "wooden_spear"),  (1,  "iron_greatsword"),
            (1,  "iron_claws"),     (1,  "hand_axe"),
            (1,  "leather_chestplate"), (1, "iron_helmet"), (1, "wooden_shield"),

            // Steel tier.
            (10, "steel_sword"),  (10, "steel_dagger"), (10, "steel_rapier"),
            (10, "steel_claws"),  (10, "steel_scythe"), (10, "samurai_axe"),
            (10, "long_bow"),     (10, "iron_shield"),

            // Mythril tier.
            (25, "mythril_sword"),
        };

        // Utility crystals sold in town. The ten TOWN TELEPORT crystals are deliberately absent:
        // HandleCrystal's Teleport case moves the player to the centre of the CURRENT floor, so
        // a "Teleport Crystal (Tolbana)" bought on F1 and used on F60 would announce Tolbana and
        // move you nowhere near it. Selling them would ship that promise. Modelling a real
        // destination is a design decision, not a table entry.
        private static readonly (int MinFloor, string DefId)[] CrystalStock =
        {
            (3,  "healing_crystal"),
            (3,  "antidote_crystal"),
            (6,  "paralysis_cure_crystal"),
            (18, "high_healing_crystal"),
            (2,  "ale"),
            (2,  "grape_juice"),
            (12, "lindas_wine"),
        };
    }
}
