using SAOTRPG.Items.Consumables;

namespace SAOTRPG.Items.Definitions;

// IF Refinement Ingots — 12 canon-flavored ingots across Common/Rare/Epic/Legendary.
// Balance per Tyler Q4: Common tier halved relative to scout's proposal.
// StatType enum limitations: CritRate/AttackSpeed/BlockChance/HPRegen/
// SkillCooldown/Durability are NOT StatType entries, so scout names are
// remapped onto existing entries (Dexterity for crit-like, Speed for
// attack-speed / durability tradeoff, Vitality for HPRegen/block, Intelligence
// for skill-cooldown effects). Flagged in Agent 3 report for Tyler.
public static class IngotDefinitions
{
    private static Ingot Make(
        string id, string name, int value, string rarity,
        StatType primary, int primaryBonus,
        StatType secondary, int secondaryBonus,
        StatType? third = null, int thirdBonus = 0,
        StatType? fourth = null, int fourthBonus = 0,
        string? effect = null)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            Quantity = 1, MaxStacks = 99,
            MaterialType = "Refinement Ingot", CraftingTier = RarityTier(rarity),
            PrimaryStat = primary, PrimaryBonus = primaryBonus,
            SecondaryStat = secondary, SecondaryBonus = secondaryBonus,
            ThirdStat = third, ThirdBonus = thirdBonus,
            FourthStat = fourth, FourthBonus = fourthBonus,
            EffectDescription = effect ?? BuildEffect(
                primary, primaryBonus, secondary, secondaryBonus,
                third, thirdBonus, fourth, fourthBonus),
        };

    private static int RarityTier(string rarity) => rarity switch
    {
        "Common"    => 1,
        "Uncommon"  => 2,
        "Rare"      => 3,
        "Epic"      => 4,
        "Legendary" => 5,
        _           => 1,
    };

    private static string BuildEffect(
        StatType p, int pv, StatType s, int sv,
        StatType? t, int tv, StatType? f, int fv)
    {
        var parts = new List<string> { Fmt(p, pv), Fmt(s, sv) };
        if (t.HasValue) parts.Add(Fmt(t.Value, tv));
        if (f.HasValue) parts.Add(Fmt(f.Value, fv));
        return string.Join(" / ", parts);
    }

    private static string Fmt(StatType stat, int v) =>
        $"{stat} {(v >= 0 ? "+" : "")}{v}";

    // ── Common (4) — big tradeoff, trivial mat cost (1 Red Hot Ore) ──────
    public static Ingot CreateSharpeningIngot() => Make(
        "sharpening_ingot", "Sharpening Ingot", 80, "Common",
        StatType.Attack, 5, StatType.Speed, -3);

    public static Ingot CreateWardenIngot() => Make(
        "warden_ingot", "Warden Ingot", 80, "Common",
        StatType.Defense, 8, StatType.Agility, -3);

    public static Ingot CreateHunterIngot() => Make(
        "hunter_ingot", "Hunter Ingot", 80, "Common",
        StatType.Dexterity, 3, StatType.Attack, -3);

    public static Ingot CreateLunarIngot() => Make(
        "lunar_ingot", "Lunar Ingot", 80, "Common",
        StatType.SkillDamage, 3, StatType.Defense, -2);

    // ── Rare (4) — balanced tradeoff, modest mat cost (2 Red Hot Ore) ───
    public static Ingot CreateKeenIngot() => Make(
        "keen_ingot", "Keen Ingot", 320, "Rare",
        StatType.Attack, 10, StatType.Speed, -3,
        StatType.Dexterity, 2);

    public static Ingot CreateGuardianIngot() => Make(
        "guardian_ingot", "Guardian Ingot", 320, "Rare",
        StatType.Defense, 15, StatType.Attack, -4,
        StatType.Vitality, 5);

    public static Ingot CreateSwiftstrikeIngot() => Make(
        "swiftstrike_ingot", "Swiftstrike Ingot", 320, "Rare",
        StatType.Speed, 5, StatType.Strength, -3,
        StatType.Dexterity, 4);

    public static Ingot CreateSpellbindIngot() => Make(
        "spellbind_ingot", "Spellbind Ingot", 320, "Rare",
        StatType.SkillDamage, 6, StatType.Defense, -3,
        StatType.Intelligence, 3);

    // ── Epic (3) — small downside (3 Red Hot Ore) ───────────────────────
    public static Ingot CreateChimericIngot() => Make(
        "chimeric_ingot", "Chimeric Ingot", 900, "Epic",
        StatType.Attack, 15, StatType.Speed, -4,
        StatType.Strength, 5, StatType.Dexterity, 3);

    public static Ingot CreateSovereignIngot() => Make(
        "sovereign_ingot", "Sovereign Ingot", 900, "Epic",
        StatType.Defense, 22, StatType.Dexterity, -2,
        StatType.Vitality, 3, StatType.Endurance, 2);

    public static Ingot CreateVanguardIngot() => Make(
        "vanguard_ingot", "Vanguard Ingot", 900, "Epic",
        StatType.Attack, 12, StatType.Dexterity, -2,
        StatType.Vitality, 4);

    // ── Legendary (1) — rare multi-stat, minimal downside (5 mat) ───────
    public static Ingot CreateAstralIngot() => Make(
        "astral_ingot", "Astral Ingot", 3500, "Legendary",
        StatType.Attack, 15, StatType.Defense, -2,
        StatType.Dexterity, 10);
}
