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
    // Named weapon special effect. Null = normal weapon.
    // Examples: "SkillCooldown-1", "CritHeal+5", "ParryChance+10", "Bleed+20"
    public string? SpecialEffect { get; set; }

    // IM Last-Attack-Bonus weapons trade enhance-ability for higher flat stats; false blocks CraftingDialog Enhance.
    public bool IsEnhanceable { get; set; } = true;

    // IM Enhancement Ores (System 3): per-level ore DefId history (len == EnhancementLevel).
    // Legacy saves auto-populate with Crimson Flame (Attack) so existing +N still reads +N Attack.
    public List<string> EnhancementOreHistory { get; set; } = new();

    // FD Paired — pre-tuned dual-wield, OffHand without DualBlades unlock. Systems.DualWieldPairs lists canon partners → Pair Resonance synergy.
    public bool IsDualWieldPaired { get; set; } = false;
}
