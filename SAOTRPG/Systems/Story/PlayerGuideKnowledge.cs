namespace SAOTRPG.Systems.Story;

// Wraps ProfileData.GuideKnownTopics. MarkKnown on kill lifts ??? mask; Title
// convention "Monster: <name>" / "Boss: <name>" (must match GuideEntry.Title verbatim).
public static class PlayerGuideKnowledge
{
    // Record a discovery. Idempotent; persists on first write.
    public static void MarkKnown(string topicTitle)
    {
        if (string.IsNullOrWhiteSpace(topicTitle)) return;
        ProfileData.MarkKnown(topicTitle);
    }

    // Strip Elite/Champion affix so variant prefixes unlock the base "Monster: <name>"
    // topic. Mirrors Mob.Variant = "Elite" | "Champion" | "".
    public static string StripAffix(string rawName)
    {
        if (string.IsNullOrEmpty(rawName)) return rawName;
        // Handle the bracketed "[Elite] Wolf" formatting used in some logs.
        if (rawName.StartsWith("[") && rawName.Contains("] "))
        {
            int close = rawName.IndexOf("] ");
            if (close >= 0) return rawName[(close + 2)..].Trim();
        }
        // Handle plain "Elite Wolf" / "Champion Wolf" prefixes.
        foreach (var prefix in new[] { "Elite ", "Champion ", "Greater ", "Lesser " })
        {
            if (rawName.StartsWith(prefix))
                return rawName[prefix.Length..].Trim();
        }
        return rawName.Trim();
    }
}
