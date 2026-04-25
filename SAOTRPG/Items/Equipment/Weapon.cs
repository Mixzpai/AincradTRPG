namespace SAOTRPG.Items.Equipment;

// Equipment that increases attack power.
public class Weapon : EquipmentBase
{
    // Raw damage value before stat scaling and bonuses.
    public int BaseDamage { get; set; }
    // Weapon category (e.g. "One-Handed Sword", "Two-Handed Axe").
    public string? WeaponType { get; set; }
    // Attack speed modifier — higher values allow faster consecutive strikes.
    public int AttackSpeed { get; set; }
    // Tile range for attacks. 1 = melee, 2+ = ranged.
    public int Range { get; set; }

    // IM Last-Attack-Bonus weapons trade enhance-ability for higher flat stats; false blocks CraftingDialog Enhance.
    public bool IsEnhanceable { get; set; } = true;

    // IM Enhancement Ores (System 3): per-level ore DefId history (len == EnhancementLevel).
    // Legacy saves auto-populate with Crimson Flame (Attack) so existing +N still reads +N Attack.
    public List<string> EnhancementOreHistory { get; set; } = new();

    // FD Paired — pre-tuned dual-wield, OffHand without DualBlades unlock. Systems.DualWieldPairs lists canon partners → Pair Resonance synergy.
    public bool IsDualWieldPaired { get; set; } = false;

    // Bundle 9 — Divine Awakening level (0-◈3). Selka F65 bumps this via
    // DivineAwakening.Awaken; bonus folds into Bonuses.Attack additively w/ Refinement.
    public int AwakeningLevel { get; set; } = 0;

    // True when weapon is Divine-rarity and below the awakening cap.
    public bool CanAwaken => Rarity == "Divine" && AwakeningLevel < SAOTRPG.Systems.DivineAwakening.MaxLevel;
}
