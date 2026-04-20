using Terminal.Gui;

namespace SAOTRPG.Entities
{
    public class Mob : Monster
    {
        public override char Symbol { get; protected set; } = 'M';
        public override Color SymbolColor { get; protected set; } = Color.Red;

        // Chebyshev detection range. Per-mob in MobFactory.
        public int AggroRange { get; set; } = 6;

        // Status-inflict flags (set per template in MobFactory).
        public bool CanPoison { get; set; }
        public bool CanBleed { get; set; }
        public bool CanStun { get; set; }
        public bool CanSlow { get; set; }

        // Elite/Champion affix (null for normal).
        public string? Affix { get; set; }

        // Variant: "", "Elite", or "Champion" — boosted stats + better rewards.
        public string Variant { get; set; } = "";

        // Themed drops on kill (TurnManager.MobLootTable).
        public string LootTag { get; set; } = "generic";

        // Aggro leash: spawn pos auto-set on placement; chase drops if beyond LeashRange.
        public int SpawnX { get; set; }
        public int SpawnY { get; set; }
        public int LeashRange { get; set; } = 12;

        // Chebyshev attack range: 1 melee, 2-3 ranged.
        public int AttackRange { get; set; } = 1;

        // Named ability (e.g. "Charge", "Leap").
        public string? SpecialAbility { get; set; }

        public void SetAppearance(char symbol, Color color)
        {
            Symbol = symbol;
            SymbolColor = color;
        }
    }
}
