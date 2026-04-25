using System.Reflection;
using System.Text.RegularExpressions;
using SAOTRPG.Items;

namespace SAOTRPG.Systems;

// Bundle 13 (Item 1) — tracks which Legendary DefIds the player has collected
// across the run. Hooked from TurnManager via Inventory.Events.ItemAdded.
public static class CollectablesTracker
{
    private static readonly HashSet<string> _collected = new();
    private static readonly Dictionary<string, string> _bucketByDefId;
    private static readonly Dictionary<string, IReadOnlyList<string>> _byBucket;
    private static readonly Dictionary<string, (int Lo, int Hi)> _floorRangeByDefId;
    private static readonly List<string> _allDefIds;

    public static IReadOnlySet<string> Collected => _collected;

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> AllByBucket => _byBucket;

    public static bool IsCollected(string defId) =>
        !string.IsNullOrEmpty(defId) && _collected.Contains(defId);

    public static int CollectedCount() => _collected.Count;

    public static int TotalCount() => _allDefIds.Count;

    // Returns DefIds whose floor anchor falls within ±8 floors of currentFloor.
    // Range entries are included when [Lo,Hi] overlaps [currentFloor-8,currentFloor+8].
    public static IReadOnlyList<string> ForCurrentFloor(int currentFloor)
    {
        const int Band = 8;
        int lo = currentFloor - Band, hi = currentFloor + Band;
        var result = new List<string>();
        foreach (var defId in _allDefIds)
        {
            if (!_floorRangeByDefId.TryGetValue(defId, out var fr)) continue;
            if (fr.Hi < lo || fr.Lo > hi) continue;
            result.Add(defId);
        }
        return result;
    }

    internal static void HydrateFromSave(IEnumerable<string>? saved)
    {
        _collected.Clear();
        if (saved == null) return;
        foreach (var id in saved)
            if (!string.IsNullOrEmpty(id)) _collected.Add(id);
    }

    internal static IEnumerable<string> Snapshot() => _collected.ToArray();

    internal static void MarkCollected(string defId)
    {
        if (string.IsNullOrEmpty(defId)) return;
        _collected.Add(defId);
    }

    static CollectablesTracker()
    {
        var entries = ReadCanonEntriesViaReflection();
        _allDefIds = entries.Keys.ToList();
        _bucketByDefId = new Dictionary<string, string>(entries.Count);
        _floorRangeByDefId = new Dictionary<string, (int, int)>(entries.Count);
        var bucketLists = new Dictionary<string, List<string>>
        {
            ["LN"] = new(), ["AL"] = new(), ["IF"] = new(), ["HF"] = new(),
            ["LR"] = new(), ["MD"] = new(), ["FD"] = new(), ["Myth"] = new(),
            ["Non-Canon"] = new(),
        };
        foreach (var (defId, citation) in entries)
        {
            string bucket = ParseBucket(citation.Source);
            _bucketByDefId[defId] = bucket;
            bucketLists[bucket].Add(defId);
            _floorRangeByDefId[defId] = ParseFloorRange(citation.FloorAnchor);
        }
        _byBucket = bucketLists.ToDictionary(
            kv => kv.Key, kv => (IReadOnlyList<string>)kv.Value);
    }

    // Reads CanonCitationData._entries via reflection — keeps Items/CanonCitationData.cs
    // R-only per Bundle 13 file ownership.
    private static Dictionary<string, CanonCitationData.Citation> ReadCanonEntriesViaReflection()
    {
        var field = typeof(CanonCitationData).GetField("_entries",
            BindingFlags.NonPublic | BindingFlags.Static);
        if (field == null)
            throw new InvalidOperationException("CanonCitationData._entries field not found");
        var dict = field.GetValue(null) as Dictionary<string, CanonCitationData.Citation>;
        if (dict == null)
            throw new InvalidOperationException("CanonCitationData._entries is null or wrong type");
        return new Dictionary<string, CanonCitationData.Citation>(dict);
    }

    // Maps Source freeform strings to one of 9 fixed buckets.
    // Priority: Invented → Non-Canon. Else specific game tags first, then Myth, then LN.
    public static string ParseBucket(string source)
    {
        if (string.IsNullOrEmpty(source)) return "Non-Canon";
        // Invented always wins — even when paired with style-of game references.
        if (source.Contains("Invented", StringComparison.OrdinalIgnoreCase))
            return "Non-Canon";

        // Specific game tags. Priority order: HF > IF > MD > FD > AL > LR.
        // Rationale: more specific game-of-origin / re-implementation wins over arc/lit references.
        if (ContainsAny(source, "Hollow Fragment", "Hollow Realization", "Hollow Area")
            || Regex.IsMatch(source, @"\bHF\b"))
            return "HF";
        if (source.Contains("Integral Factor", StringComparison.OrdinalIgnoreCase)
            || Regex.IsMatch(source, @"\bIF\b"))
            return "IF";
        if (source.Contains("Memory Defrag", StringComparison.OrdinalIgnoreCase)
            || source.Contains("SAO MD", StringComparison.OrdinalIgnoreCase)
            || Regex.IsMatch(source, @"\bMD\b"))
            return "MD";
        if (source.Contains("Fractured Daydream", StringComparison.OrdinalIgnoreCase)
            || Regex.IsMatch(source, @"\bFD\b"))
            return "FD";
        if (ContainsAny(source, "Alicization", "Underworld", "Lycoris"))
            return "AL";
        if (source.Contains("Last Recollection", StringComparison.OrdinalIgnoreCase)
            || source.Contains("Lost Song", StringComparison.OrdinalIgnoreCase)
            || Regex.IsMatch(source, @"\bLR\b") || Regex.IsMatch(source, @"\bLS\b"))
            return "LR";

        // Plain-LN canon — fires before Myth so "Infinity Moment (canon) / Greek myth"
        // resolves to LN (canon game-tag wins over Myth, even when IM has no bucket).
        if (source.Contains("SAO LN", StringComparison.OrdinalIgnoreCase)
            || source.Contains("LN ", StringComparison.OrdinalIgnoreCase)
            || source.StartsWith("LN/", StringComparison.OrdinalIgnoreCase)
            || source.Contains("(canon)", StringComparison.OrdinalIgnoreCase))
            return "LN";

        // Myth tags — only when no canon game-tag present.
        if (ContainsAny(source, "Norse", "Greek", "Celtic", "Indian", "Shinto",
                "Arabic", "Arthurian", "Roman", "Japanese", "Islamic", "Medieval",
                "Chinese"))
            return "Myth";
        if (Regex.IsMatch(source, @"\bmyth\b", RegexOptions.IgnoreCase))
            return "Myth";

        return "Non-Canon";
    }

    private static bool ContainsAny(string source, params string[] needles)
    {
        foreach (var n in needles)
            if (source.Contains(n, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    // Parse FloorAnchor strings like "F50 floor-boss", "F1–F5 chest", "F70+ avatar pool",
    // "R6 craft" → (Lo, Hi). R6 craft → Lindarth F48 access.
    private static (int Lo, int Hi) ParseFloorRange(string anchor)
    {
        if (string.IsNullOrEmpty(anchor)) return (0, 0);
        // R6 craft = Lindarth F48 access (per CanonCitationData detail strings).
        if (anchor.StartsWith("R6 craft", StringComparison.OrdinalIgnoreCase))
            return (48, 48);

        // Range form: "F44–F48 chest" or "F1–F5 chest". En-dash U+2013 used throughout.
        var rangeMatch = Regex.Match(anchor, @"F(\d+)\s*[–\-]\s*F(\d+)");
        if (rangeMatch.Success)
        {
            int lo = int.Parse(rangeMatch.Groups[1].Value);
            int hi = int.Parse(rangeMatch.Groups[2].Value);
            return (Math.Min(lo, hi), Math.Max(lo, hi));
        }

        // Open-ended: "F70+ avatar pool" → [70, 100].
        var plusMatch = Regex.Match(anchor, @"F(\d+)\+");
        if (plusMatch.Success)
        {
            int lo = int.Parse(plusMatch.Groups[1].Value);
            return (lo, 100);
        }

        // Single floor: "F50 floor-boss".
        var singleMatch = Regex.Match(anchor, @"F(\d+)");
        if (singleMatch.Success)
        {
            int f = int.Parse(singleMatch.Groups[1].Value);
            return (f, f);
        }

        return (0, 0);
    }
}
