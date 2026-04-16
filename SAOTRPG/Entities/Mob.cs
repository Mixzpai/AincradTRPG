using Terminal.Gui;

namespace SAOTRPG.Entities
{
    public class Mob : Monster
    {
        public override char Symbol { get; protected set; } = 'M';
        public override Color SymbolColor { get; protected set; } = Color.Red;

        // How far this mob detects the player (Chebyshev distance).
        // Default 6; set per-mob-type for variety.
        public int AggroRange { get; set; } = 6;

        // Whether this mob's attacks can inflict poison.
        // Set per-mob-type in MobFactory. Add new poisonous mobs there.
        public bool CanPoison { get; set; }

        // Whether this mob's attacks can inflict bleed.
        // Set per-mob-type in MobFactory. Add new bleeding mobs there.
        public bool CanBleed { get; set; }

        // Whether this mob's attacks can inflict stun (skip turn).
        public bool CanStun { get; set; }

        // Whether this mob's attacks can inflict slow (halved dodge).
        public bool CanSlow { get; set; }

        // Random affix for elite/champion mobs (null for normal mobs).
        public string? Affix { get; set; }

        // Variant prefix: "", "Elite", or "Champion". Set by MobFactory.
        // Elite/Champion mobs have boosted stats and better rewards.
        public string Variant { get; set; } = "";

        // Loot category tag — determines themed drops on kill.
        // Set per-mob-type in MobFactory. Add new loot tables in TurnManager.MobLootTable.
        public string LootTag { get; set; } = "generic";

        // Spawn position — used for aggro leashing. Mob gives up chase when
        // too far from spawn. Set automatically when placed on the map.
        public int SpawnX { get; set; }
        public int SpawnY { get; set; }

        // Max distance from spawn before giving up chase. Default 12.
        public int LeashRange { get; set; } = 12;

        // Attack range in Chebyshev distance. 1 = melee, 2-3 = ranged.
        public int AttackRange { get; set; } = 1;

        // Named special ability (e.g. "Charge", "Leap"). Null for normal mobs.
        public string? SpecialAbility { get; set; }

        // Override the default map symbol and color. Called by MobFactory per template.
        public void SetAppearance(char symbol, Color color)
        {
            Symbol = symbol;
            SymbolColor = color;
        }
    }
}
