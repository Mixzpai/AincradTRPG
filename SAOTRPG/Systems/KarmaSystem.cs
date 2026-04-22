using SAOTRPG.Entities;
using SAOTRPG.UI;

namespace SAOTRPG.Systems;

// Karma: signed int [-100,+100] gating NPC dialog, shop pricing, guild entry,
// F1 Guard spawn. Centralizes adjust/query so tier-cross banner fires once.
public static class KarmaSystem
{
    public const int Min = -100;
    public const int Max = +100;

    // Canonical event deltas (consts so quest/combat hooks match guide).
    public const int DeltaPkKill          = +2;  // PKer (hostile human)
    public const int DeltaPeacefulKill    = -5;  // non-hostile creature
    public const int DeltaNpcKill         = -20; // named NPC
    public const int DeltaQuestComplete   = +3;
    public const int DeltaSteal           = -5;
    public const int DeltaLeaveGuild      = -3;
    public const int DeltaBlackCatsFall   = -5;  // Moonlit Black Cats dissolution

    // Karma tier used by dialogue/shop/spawn gating.
    public enum Tier { Honorable, Neutral, Shady, Outlaw }

    // Hostile-human mob name fragments. Kills here GAIN karma.
    private static readonly string[] PkerFragments =
    {
        "Titan's Hand", "Crimson Longsword", "Laughing Coffin PKer", "Fallen Paladin",
    };

    // Peaceful LootTags — kills drain karma.
    private static readonly string[] PeacefulTags =
    {
        "beast", "insect", "plant", "aquatic",
    };

    // Thresholds: karma == -50 inclusive for Outlaw (gates Town Guard spawn).
    public static Tier GetTier(int karma)
    {
        if (karma >= 50) return Tier.Honorable;
        if (karma >= 0)  return Tier.Neutral;
        if (karma > -50) return Tier.Shady;
        return Tier.Outlaw;
    }

    public static string TierLabel(int karma) => GetTier(karma) switch
    {
        Tier.Honorable => "Honorable",
        Tier.Neutral   => "Neutral",
        Tier.Shady     => "Shady",
        Tier.Outlaw    => "Outlaw",
        _ => "Neutral",
    };

    // Shop price mult. Outlaw = -1f sentinel (shops refuse service).
    public static float ShopPriceMultiplier(int karma) => GetTier(karma) switch
    {
        Tier.Honorable => 0.9f,
        Tier.Neutral   => 1.0f,
        Tier.Shady     => 1.1f,
        Tier.Outlaw    => -1f,
        _ => 1.0f,
    };

    // Adjust + clamp + log + tier-cross banner. Log null-safe for boot paths.
    public static void Adjust(Player player, int delta, string reason, IGameLog? log = null)
    {
        if (delta == 0) return;
        var beforeTier = GetTier(player.Karma);
        int before = player.Karma;
        player.Karma = Math.Clamp(player.Karma + delta, Min, Max);
        var afterTier = GetTier(player.Karma);
        int actualDelta = player.Karma - before;
        if (actualDelta == 0) return; // Clamped at the boundary — no change.

        string sign = actualDelta > 0 ? $"+{actualDelta}" : actualDelta.ToString();
        log?.Log($"  [KARMA] {sign} ({reason}). Karma: {player.Karma} [{TierLabel(player.Karma)}]");

        if (beforeTier != afterTier)
            log?.LogSystem($"  ** Karma threshold crossed: you are now {TierLabel(player.Karma)}. **");
    }

    // Mob → karma delta. Called from HandleMonsterKill.
    public static int DeltaForMobKill(string mobName, string lootTag)
    {
        if (string.IsNullOrEmpty(mobName)) return 0;
        foreach (var frag in PkerFragments)
            if (mobName.Contains(frag)) return DeltaPkKill;
        foreach (var tag in PeacefulTags)
            if (lootTag == tag) return DeltaPeacefulKill;
        // Other hostile-non-human tags = neutral for karma.
        return 0;
    }
}
