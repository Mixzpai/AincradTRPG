namespace SAOTRPG.UI;

// Small ASCII portraits keyed by a short identifier. Humans are 8×5; bosses upgrade to 8×7.
// Glyph-safe per research §3 list — no CJK, no emoji, no combining marks.
public static class AsciiPortraits
{
    public static readonly Dictionary<string, string[]> All = new()
    {
        ["kirito"] = new[]
        {
            "┌──────┐",
            "│ /\\/\\ │",
            "│ o  o │",
            "│  ──  │",
            "└──────┘",
        },
        ["asuna"] = new[]
        {
            "┌──────┐",
            "│╲│══│╱│",
            "│ ○  ○ │",
            "│  ⌣⌣  │",
            "└──────┘",
        },
        ["heathcliff"] = new[]
        {
            "┌──────┐",
            "│ ╒══╕ │",
            "│ │▲│  │",
            "│  ──  │",
            "│ ╳══╳ │",
            "└──────┘",
        },
        ["kayaba"] = new[]
        {
            "┌──────┐",
            "│ ▓▓▓▓ │",
            "│ ·  · │",
            "│  ▔▔  │",
            "└──────┘",
        },
        ["argo"] = new[]
        {
            "┌──────┐",
            "│ ^··^ │",
            "│ ´oo` │",
            "│ ─ww─ │",
            "└──────┘",
        },
        ["klein"] = new[]
        {
            "┌──────┐",
            "│ ≋≋≋≋ │",
            "│ ^  ^ │",
            "│ o  o │",
            "│  ═══ │",
            "└──────┘",
        },
        ["agil"] = new[]
        {
            "┌──────┐",
            "│ ▁▁▁▁ │",
            "│ ●  ● │",
            "│  ──  │",
            "│ ╱══╲ │",
            "└──────┘",
        },
        ["kizmel"] = new[]
        {
            "┌──────┐",
            "│ ▲  ▲ │",
            "│ *  * │",
            "│  ──  │",
            "└──────┘",
        },
        ["sachi"] = new[]
        {
            "┌──────┐",
            "│ ∩∩∩∩ │",
            "│ ·  · │",
            "│  ‿‿  │",
            "└──────┘",
        },
        ["kibaou"] = new[]
        {
            "┌──────┐",
            "│ ╱╲╱╲ │",
            "│ >  < │",
            "│ ══── │",
            "└──────┘",
        },
        // ── Vendors ──
        ["lisbeth"] = new[]
        {
            "┌──────┐",
            "│ ~ww~ │",
            "│ ●  ● │",
            "│  ‿‿  │",
            "│ ╤╤╤╤ │",
            "└──────┘",
        },
        // ── Party / allies ──
        ["silica"] = new[]
        {
            "┌──────┐",
            "│══∩∩══│",
            "│ o  o │",
            "│  ‿‿  │",
            "│ ╲╱╲╱ │",
            "└──────┘",
        },
        // ── Quest giver / re-skinned Argo handled above ──
        // ── Bosses (8×7) ──
        ["illfang"] = new[]
        {
            "┌──────┐",
            "│ ▼▲▲▼ │",
            "│ ░░░░ │",
            "│ ●  ● │",
            "│  ══  │",
            "│ ╱══╲ │",
            "└──────┘",
        },
        ["asterius"] = new[]
        {
            "┌──────┐",
            "│ ╱▲▲╲ │",
            "│ ░░░░ │",
            "│ ●  ● │",
            "│  ▼▼  │",
            "│ ╲══╱ │",
            "└──────┘",
        },
        ["gleam_eyes"] = new[]
        {
            "┌──────┐",
            "│ ╱▲▲╲ │",
            "│  ░░  │",
            "│ ◉  ◉ │",
            "│  ▼▼  │",
            "│ ╳══╳ │",
            "└──────┘",
        },
        ["skull_reaper"] = new[]
        {
            "┌──────┐",
            "│  ▲▲  │",
            "│ ░▓▓░ │",
            "│ ○  ○ │",
            "│  ╳╳  │",
            "│ ╱──╲ │",
            "└──────┘",
        },
        ["heathcliff_boss"] = new[]
        {
            "┌──────┐",
            "│ ╒══╕ │",
            "│ │▲│  │",
            "│ ●  ● │",
            "│  ══  │",
            "│ ╳══╳ │",
            "└──────┘",
        },
        ["fatal_scythe"] = new[]
        {
            "┌──────┐",
            "│ ╱▔▔╲ │",
            "│ ░░░░ │",
            "│  ╳╳  │",
            "│ ╱══╲ │",
            "└──────┘",
        },
    };

    public static string[] Get(string key)
        => All.TryGetValue(key, out var p) ? p : Array.Empty<string>();

    // Name → portrait key for common NPC/boss injection.
    // Used by dialogs/boss banners to map a display name to a registered portrait.
    public static string? KeyForName(string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        string n = name.ToLowerInvariant();
        if (n.Contains("kirito")) return "kirito";
        if (n.Contains("asuna")) return "asuna";
        if (n.Contains("klein")) return "klein";
        if (n.Contains("silica")) return "silica";
        if (n.Contains("agil")) return "agil";
        if (n.Contains("lisbeth")) return "lisbeth";
        if (n.Contains("argo")) return "argo";
        if (n.Contains("kizmel")) return "kizmel";
        if (n.Contains("sachi")) return "sachi";
        if (n.Contains("kibaou")) return "kibaou";
        if (n.Contains("illfang")) return "illfang";
        if (n.Contains("asterius")) return "asterius";
        if (n.Contains("gleam eyes")) return "gleam_eyes";
        if (n.Contains("skull reaper")) return "skull_reaper";
        if (n.Contains("fatal scythe")) return "fatal_scythe";
        if (n.Contains("heathcliff")) return "heathcliff_boss";
        if (n.Contains("kayaba")) return "kayaba";
        return null;
    }
}
