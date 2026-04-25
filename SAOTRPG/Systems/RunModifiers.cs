namespace SAOTRPG.Systems;

// Run Modifiers: run-start toggles that alter gameplay (stackable, score mul is
// multiplicative). Unlocked after F100 clear (FB-564, gated via ProfileData.HasCompletedGame).
public enum RunModifier
{
    StarlessNight,        // Easy   — perpetual night, +spawn density
    IronRank,             // Easy   — hunger ×2
    Beater,               // Mod    — faction rep -50, shops +25%
    Solo,                 // Mod    — no party recruits, +10% XP
    LaughingCoffin,       // Mod    — PK ambush every 5 floors
    HeathcliffsGauntlet,  // Hard   — boss HP ×2, boss dmg ×1.3
    AntiCrystalTyranny,   // Hard   — crystals disabled
    KayabasWager,         // Hard   — teleport/flee disabled
    HollowIngress,        // Hard   — start with no sword skills
    NakedIngress,         // Hard   — no armor equippable, +25% evasion
    GleamEyesEcho,        // N-mare — bosses gain 25%-HP enrage phase
    SwordArtOnly,         // N-mare — only 1H swords equippable
}

public enum ModifierTier { Easy, Moderate, Hard, Nightmare }

public record ModifierDef(
    RunModifier Id,
    string Name,
    string Description,
    ModifierTier Tier,
    double ScoreMultiplier
);

public static class RunModifiers
{
    // Active set for the current run. Loaded from SaveData / picker UI.
    public static HashSet<RunModifier> Active { get; set; } = new();

    public static readonly Dictionary<RunModifier, ModifierDef> Definitions = new()
    {
        [RunModifier.StarlessNight] = new(RunModifier.StarlessNight,
            "Starless Night",
            "The sun never rises. Vision -20%, mob spawns +30%, enemy damage +10%.",
            ModifierTier.Easy, 1.15),

        [RunModifier.IronRank] = new(RunModifier.IronRank,
            "Iron Rank",
            "Hunger drains twice as fast. Food or famine.",
            ModifierTier.Easy, 1.15),

        [RunModifier.Beater] = new(RunModifier.Beater,
            "Beater",
            "All factions scorn you. Rep -50, shop prices +25%.",
            ModifierTier.Moderate, 1.25),

        [RunModifier.Solo] = new(RunModifier.Solo,
            "Solo",
            "No party recruits allowed. Compensation: +10% XP.",
            ModifierTier.Moderate, 1.25),

        [RunModifier.LaughingCoffin] = new(RunModifier.LaughingCoffin,
            "Laughing Coffin",
            "PK-squad ambush every 5 floors. Rare drops on defeat.",
            ModifierTier.Moderate, 1.25),

        [RunModifier.HeathcliffsGauntlet] = new(RunModifier.HeathcliffsGauntlet,
            "Heathcliff's Gauntlet",
            "All boss HP ×2 and boss damage ×1.3.",
            ModifierTier.Hard, 1.40),

        [RunModifier.AntiCrystalTyranny] = new(RunModifier.AntiCrystalTyranny,
            "Anti-Crystal Tyranny",
            "All Crystal consumables are disabled (Healing / Teleport / Antidote / Revive / Mirage).",
            ModifierTier.Hard, 1.40),

        [RunModifier.KayabasWager] = new(RunModifier.KayabasWager,
            "Kayaba's Wager",
            "Teleport and Corridor crystals inert. Cannot flee mid-combat.",
            ModifierTier.Hard, 1.40),

        [RunModifier.HollowIngress] = new(RunModifier.HollowIngress,
            "Hollow Ingress",
            "Start with zero Sword Skills unlocked. Unlock only via proficiency kills.",
            ModifierTier.Hard, 1.40),

        [RunModifier.NakedIngress] = new(RunModifier.NakedIngress,
            "Naked Ingress",
            "No armor equippable in any slot. Compensation: +25% evasion.",
            ModifierTier.Hard, 1.40),

        [RunModifier.GleamEyesEcho] = new(RunModifier.GleamEyesEcho,
            "Gleam Eyes Echo",
            "At 25% HP, every boss enrages — attack speed +50% + new AoE breath.",
            ModifierTier.Nightmare, 1.75),

        [RunModifier.SwordArtOnly] = new(RunModifier.SwordArtOnly,
            "Sword Art Only",
            "Only One-Handed Swords equippable. Pure Kirito run.",
            ModifierTier.Nightmare, 1.75),
    };

    public static bool IsActive(RunModifier mod) => Active.Contains(mod);

    public static double TotalScoreMultiplier()
    {
        double mul = 1.0;
        foreach (var mod in Active)
            mul *= Definitions[mod].ScoreMultiplier;
        return Math.Min(mul, 10.0);
    }

    public static void Reset() => Active.Clear();

    public static void LoadFromSave(IEnumerable<string>? ids)
    {
        Active.Clear();
        if (ids == null) return;
        foreach (var id in ids)
            if (Enum.TryParse<RunModifier>(id, out var mod))
                Active.Add(mod);
    }

    public static List<string> ToSaveList() => Active.Select(m => m.ToString()).ToList();
}
