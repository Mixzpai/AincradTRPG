namespace SAOTRPG.Systems.Story;

// Thin helper around ProfileData.GuideKnownTopics — gameplay sites call
// MarkKnown(...) on monster/boss defeat and the Player Guide's TreeAspect
// lifts the "??? (Unknown)" mask for those topics.
//
// Title convention mirrors the Phase B scout §9 content schema:
//   "Monster: <base name>"     — any non-boss mob kill
//   "Boss: <name>"             — floor boss defeat
//   "Field Boss: <name>"       — roaming elite defeat (FieldBoss subclass)
//
// Titles must match PlayerGuideContent.GuideEntry.Title verbatim; content
// agents 3/4 will add the corresponding entries using these exact prefixes.
public static class PlayerGuideKnowledge
{
    // Record a discovery. Idempotent; persists on first write.
    public static void MarkKnown(string topicTitle)
    {
        if (string.IsNullOrWhiteSpace(topicTitle)) return;
        ProfileData.MarkKnown(topicTitle);
    }

    // Strip elite/champion affix from a monster display name so "Elite Wolf"
    // and "Champion Wolf" both unlock the single "Monster: Wolf" topic.
    // Mirrors Mob.Variant = "Elite" | "Champion" | "" convention.
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
