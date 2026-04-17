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

    // IM canon: a small set of Last-Attack-Bonus floor-boss weapons trade
    // enhance-ability for higher flat stats. When false, CraftingDialog Enhance
    // is blocked on this weapon.
    public bool IsEnhanceable { get; set; } = true;

    // IM Enhancement Ores (System 3): parallel history of which ore defId
    // contributed each enhance level (length == EnhancementLevel). On legacy
    // saves where the history is missing, SaveManager.DeserializeItem
    // auto-populates with Crimson Flame (Attack) so existing +N still reads
    // as +N Attack — no retroactive stat change.
    public List<string> EnhancementOreHistory { get; set; } = new();
}
