using Terminal.Gui;

namespace SAOTRPG.Entities
{
    public class Mob : Monster
    {
        public override char Symbol { get; protected set; } = 'M';
        public override Color SymbolColor { get; protected set; } = Color.Red;

        /// <summary>
        /// How far this mob detects the player (Chebyshev distance).
        /// Default 6; set per-mob-type for variety.
        /// </summary>
        public int AggroRange { get; set; } = 6;

        /// <summary>
        /// Whether this mob's attacks can inflict poison.
        /// Set per-mob-type in MobFactory. Add new poisonous mobs there.
        /// </summary>
        public bool CanPoison { get; set; }

        /// <summary>
        /// Whether this mob's attacks can inflict bleed.
        /// Set per-mob-type in MobFactory. Add new bleeding mobs there.
        /// </summary>
        public bool CanBleed { get; set; }

        /// <summary>
        /// Variant prefix: "", "Elite", or "Champion". Set by MobFactory.
        /// Elite/Champion mobs have boosted stats and better rewards.
        /// </summary>
        public string Variant { get; set; } = "";

        /// <summary>
        /// Loot category tag — determines themed drops on kill.
        /// Set per-mob-type in MobFactory. Add new loot tables in TurnManager.MobLootTable.
        /// </summary>
        public string LootTag { get; set; } = "generic";

        /// <summary>
        /// Spawn position — used for aggro leashing. Mob gives up chase when
        /// too far from spawn. Set automatically when placed on the map.
        /// </summary>
        public int SpawnX { get; set; }
        public int SpawnY { get; set; }

        /// <summary>Max distance from spawn before giving up chase. Default 12.</summary>
        public int LeashRange { get; set; } = 12;

        public void SetAppearance(char symbol, Color color)
        {
            Symbol = symbol;
            SymbolColor = color;
        }
    }
}
