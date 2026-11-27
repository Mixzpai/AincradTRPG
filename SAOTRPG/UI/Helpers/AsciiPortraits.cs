namespace SAOTRPG.UI.Helpers;

// Small ASCII portraits keyed by a short identifier. Humans are 8×5; bosses upgrade to 8×7.
// Glyph-safe palette — no CJK, no emoji, no combining marks.
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

    // Display name to portrait key, for dialogs and boss banners. A name that only refers to a
    // character ("Crystal Wyrm of Lisbeth's Forge") gets no portrait.
    public static string? KeyForName(string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        string n = name.ToLowerInvariant();
        bool Has(string key) => n.Contains(key) && !n.Contains("of " + key);
        if (Has("kirito")) return "kirito";
        if (Has("asuna")) return "asuna";
        if (Has("klein")) return "klein";
        if (Has("silica")) return "silica";
        if (Has("agil")) return "agil";
        if (Has("lisbeth")) return "lisbeth";
        if (Has("argo")) return "argo";
        if (Has("kizmel")) return "kizmel";
        if (Has("sachi")) return "sachi";
        if (Has("kibaou")) return "kibaou";
        if (Has("illfang")) return "illfang";
        if (Has("asterius")) return "asterius";
        if (Has("gleam eyes")) return "gleam_eyes";
        if (Has("skull reaper")) return "skull_reaper";
        if (Has("fatal scythe")) return "fatal_scythe";
        if (Has("heathcliff")) return "heathcliff_boss";
        if (Has("kayaba")) return "kayaba";
        return null;
    }
}
