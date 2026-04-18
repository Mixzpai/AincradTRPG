using SAOTRPG.Entities;
using SAOTRPG.UI;

namespace SAOTRPG.Systems;

// FB-063 Karma System. Tracks a single signed integer in [-100, +100]
// reflecting the moral weight of player actions. Thresholds drive NPC
// dialogue, shop pricing, guild gating, and the F1 Town Guard spawn.
//
// State lives on Player.Karma (see Player.Stats.cs). This class is the
// central adjust/query API so every call-site logs consistently and the
// threshold-cross banner fires exactly once per transition.
public static class KarmaSystem
{
    public const int Min = -100;
    public const int Max = +100;

    // Gain/loss deltas for canonical events. Keep as consts so quest + combat
    // hooks surface the same numbers in-log and in the guide.
    public const int DeltaPkKill          = +2;  // Killing a PKer (hostile human mob) is lawful.
    public const int DeltaPeacefulKill    = -5;  // Slaying a neutral / non-hostile creature.
    public const int DeltaNpcKill         = -20; // Murdering a named NPC.
    public const int DeltaQuestComplete   = +3;
    public const int DeltaSteal           = -5;
    public const int DeltaLeaveGuild      = -3;  // Abandoning a guild leaves a mark.
    public const int DeltaBlackCatsFall   = -5;  // Moonlit Black Cats dissolution.

    // Karma tier used by dialogue/shop/spawn gating.
    public enum Tier { Honorable, Neutral, Shady, Outlaw }

    // Names of mob LootTag / Name fragments that count as "PKer" — hostile
    // humans who attack players. Kills here GAIN karma.
    private static readonly string[] PkerFragments =
    {
        "Titan's Hand", "Crimson Longsword", "Laughing Coffin PKer", "Fallen Paladin",
    };

    // Peaceful tags — creatures that do not actively hunt the player (boar,
    // beast, insect, plant, aquatic). Killing these drains karma.
    private static readonly string[] PeacefulTags =
    {
        "beast", "insect", "plant", "aquatic",
    };

    // Returns the tier for a karma value. Thresholds are inclusive on the
    // outlaw edge so karma == -50 still gates Town Guard spawn.
    public static Tier GetTier(int karma)
    {
        if (karma >= 50) return Tier.Honorable;
        if (karma >= 0)  return Tier.Neutral;
        if (karma > -50) return Tier.Shady;
        return Tier.Outlaw;
    }

    // Display label for HUD / StatsDialog.
    public static string TierLabel(int karma) => GetTier(karma) switch
    {
        Tier.Honorable => "Honorable",
        Tier.Neutral   => "Neutral",
        Tier.Shady     => "Shady",
        Tier.Outlaw    => "Outlaw",
        _ => "Neutral",
    };

    // Shop price multiplier by tier. Outlaw returns -1f to signal
    // "shops refuse service" — callers check for the negative sentinel.
    public static float ShopPriceMultiplier(int karma) => GetTier(karma) switch
    {
        Tier.Honorable => 0.9f,
        Tier.Neutral   => 1.0f,
        Tier.Shady     => 1.1f,
        Tier.Outlaw    => -1f,
        _ => 1.0f,
    };

    // Adjust karma by delta, clamp to [-100, +100], log the reason, and
    // fire a banner if the tier crossed. Log is optional so silent early
    // hooks (character creation) don't crash.
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

    // Classify a mob by name/tag into the karma-relevant bucket. Called from
    // HandleMonsterKill so every kill funnels through one place.
    public static int DeltaForMobKill(string mobName, string lootTag)
    {
        if (string.IsNullOrEmpty(mobName)) return 0;
        foreach (var frag in PkerFragments)
            if (mobName.Contains(frag)) return DeltaPkKill;
        foreach (var tag in PeacefulTags)
            if (lootTag == tag) return DeltaPeacefulKill;
        // humanoid/undead/construct/kobold/reptile/elemental/dragon/hollow/generic
        // are all "hostile but not human" — neutral for karma purposes.
        return 0;
    }
}
