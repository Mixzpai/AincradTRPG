using System.Collections.ObjectModel;
using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Monster compendium (Y from map). Persists selection/tab/sort/filter across opens within a run.
// Size = min(screen-6, 120x45). Left list + right tabbed detail, filter chips under title, footer hint.
public static class BestiaryDialog
{
    private const int MaxWidth = 120;
    private const int MaxHeight = 45;
    private const int ListPaneWidth = 38;

    private static readonly string[] KnownTags =
    {
        "beast", "insect", "undead", "dragon", "humanoid",
        "construct", "plant", "reptile", "aquatic", "elemental",
        "hollow", "kobold",
    };

    public static void Show(Player player, TurnManager turnManager)
    {
        int w = Math.Min(Math.Max(60, Application.Screen.Size.Width  - 6), MaxWidth);
        int h = Math.Min(Math.Max(24, Application.Screen.Size.Height - 6), MaxHeight);

        int total = Bestiary.TotalRosterCount();
        int discovered = Bestiary.DiscoveredCount();
        int pct = total > 0 ? discovered * 100 / total : 0;
        string title = $"Bestiary — {discovered} / {total} discovered ({pct}%)";

        var dialog = DialogHelper.Create(title, w, h);

        var filter = Bestiary.SessionFilterState;
        string sortKey = Bestiary.SessionSort;
        string activeTab = Bestiary.SessionActiveTab;

        // ── Filter chip row ──
        var chipLabel = new Label
        {
            Text = "", X = 1, Y = 0,
            Width = Dim.Fill(2), Height = 1,
            ColorScheme = ColorSchemes.Body,
        };

        // ── List pane ──
        var listLabel = new Label
        {
            Text = "[ Entries ]", X = 1, Y = 2,
            Width = ListPaneWidth, Height = 1,
            ColorScheme = ColorSchemes.Gold,
        };
        var listView = new ListView
        {
            X = 1, Y = 3,
            Width = ListPaneWidth, Height = Dim.Fill(3),
            ColorScheme = ColorSchemes.ListSelection,
            AllowsMarking = false,
        };

        // ── Detail pane ──
        int detailX = ListPaneWidth + 2;
        var tabLabel = new Label
        {
            Text = "", X = detailX, Y = 2,
            Width = Dim.Fill(2), Height = 1,
            ColorScheme = ColorSchemes.Gold,
        };
        var detailView = new TextView
        {
            X = detailX, Y = 3,
            Width = Dim.Fill(2), Height = Dim.Fill(3),
            ReadOnly = true, WordWrap = false,
            ColorScheme = ColorSchemes.Body,
        };

        // ── Footer hint ──
        var hint = new Label
        {
            Text = "[Y/Esc] Close  [↑↓] Nav  [Tab] Tab  [S] Sort  [/] Search  [B] Boss  [U] Undisc  [C] Clear",
            X = 1, Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(1), Height = 1,
            ColorScheme = ColorSchemes.Dim,
        };

        // ── Search box (hidden until /) ──
        var searchLabel = new Label
        {
            Text = "Search:", X = 1, Y = Pos.AnchorEnd(2),
            Width = 8, ColorScheme = ColorSchemes.Gold, Visible = false,
        };
        var searchField = new TextField
        {
            Text = filter.Search,
            X = 9, Y = Pos.AnchorEnd(2), Width = ListPaneWidth - 7,
            ColorScheme = ColorSchemes.Body, Visible = false,
        };

        dialog.Add(chipLabel, listLabel, listView, tabLabel, detailView,
            hint, searchLabel, searchField);

        // ── State snapshot + refresh pipeline ──
        List<(string DisplayName, string? Key, Bestiary.Entry? Entry)> rows = new();

        void RebuildRows()
        {
            rows.Clear();
            var all = Bestiary.GetAll().ToList();
            IEnumerable<Bestiary.Entry> filtered = all;

            if (filter.ActiveTags.Count > 0)
                filtered = filtered.Where(e => filter.ActiveTags.Contains(e.LootTag ?? ""));
            if (filter.BossOnly)
                filtered = filtered.Where(e => e.IsBoss || e.IsFieldBoss);
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string q = filter.Search.ToLowerInvariant();
                filtered = filtered.Where(e => e.Name.ToLowerInvariant().Contains(q));
            }
            filtered = filtered.Where(e =>
                e.FirstFloorEncountered <= filter.FloorMax &&
                (e.LastFloorEncountered == 0 || e.LastFloorEncountered >= filter.FloorMin));

            filtered = sortKey switch
            {
                "A" => filtered.OrderBy(e => e.Name),
                "L" => filtered.OrderByDescending(e => e.Level).ThenBy(e => e.Name),
                "K" => filtered.OrderByDescending(e => e.TimesKilled).ThenBy(e => e.Name),
                "R" => filtered.OrderByDescending(e => e.LastSeenDate).ThenBy(e => e.Name),
                "F" => filtered.OrderBy(e => e.FirstFloorEncountered).ThenBy(e => e.Name),
                _   => filtered.OrderBy(e => e.Name),
            };

            foreach (var e in filtered)
                rows.Add((FormatRow(e), e.Name, e));

            if (filter.ShowUndiscovered)
            {
                int placeholders = Math.Max(0, Bestiary.TotalRosterCount() - Bestiary.DiscoveredCount());
                placeholders = Math.Min(placeholders, 40);  // cap so the list stays usable
                for (int i = 0; i < placeholders; i++)
                    rows.Add((FormatUndiscoveredRow(), null, null));
            }
        }

        void RefreshChips()
        {
            var parts = new List<string>();
            foreach (var t in KnownTags)
            {
                string mark = filter.ActiveTags.Contains(t) ? "[x]" : "[ ]";
                parts.Add($"{mark}{t}");
            }
            string tagLine = string.Join(" ", parts);
            string bossMark = filter.BossOnly ? "[x]" : "[ ]";
            string undiscMark = filter.ShowUndiscovered ? "[x]" : "[ ]";
            chipLabel.Text = $"{tagLine}  B:{bossMark}Boss  U:{undiscMark}Undisc  F{filter.FloorMin}-{filter.FloorMax}  Sort:{sortKey}";
        }

        void RefreshDetail()
        {
            if (rows.Count == 0)
            {
                tabLabel.Text = "[ No matches ]";
                detailView.Text = "  No matches — press C to clear filters.";
                return;
            }

            int idx = Math.Clamp(listView.SelectedItem, 0, rows.Count - 1);
            var (_, key, entry) = rows[idx];
            Bestiary.SessionSelectedName = key;

            tabLabel.Text = TabStrip(activeTab);

            if (entry == null)
            {
                detailView.Text =
                    "  ???\n" +
                    "  Level: ??\n\n" +
                    "  This entry remains undiscovered.\n" +
                    "  Explore more floors to reveal its identity.";
                return;
            }

            detailView.Text = activeTab switch
            {
                "Overview" => BuildOverview(entry, player),
                "Combat"   => BuildCombat(entry, player),
                "Lore"     => BuildLore(entry),
                "History"  => BuildHistory(entry),
                _          => BuildOverview(entry, player),
            };
        }

        void Refresh()
        {
            RebuildRows();
            RefreshChips();
            var sourceList = rows.Select(r => r.DisplayName).ToList();
            if (sourceList.Count == 0) sourceList.Add("  (no entries)");
            listView.SetSource(new ObservableCollection<string>(sourceList));

            // Restore last-selected row if it's still in the filtered view.
            if (!string.IsNullOrEmpty(Bestiary.SessionSelectedName))
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    if (rows[i].Key == Bestiary.SessionSelectedName)
                    { listView.SelectedItem = i; break; }
                }
            }
            RefreshDetail();
        }

        listView.SelectedItemChanged += (s, e) => RefreshDetail();

        // Search-field handler: Enter commits, Esc cancels.
        // Wired separately — listView.KeyDown stops firing once search has focus.
        searchField.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Enter)
            {
                filter.Search = (searchField.Text ?? "").ToString() ?? "";
                searchField.Visible = false;
                searchLabel.Visible = false;
                listView.SetFocus();
                Refresh();
                e.Handled = true;
            }
            else if (e.KeyCode == KeyCode.Esc)
            {
                searchField.Visible = false;
                searchLabel.Visible = false;
                listView.SetFocus();
                e.Handled = true;
            }
        };

        // Primary keybind handler on listView. Letters go via
        // e.AsRune.Value (the typed character); control keys go via
        // e.KeyCode. Mixing the two matches how PlayerGuideDialog wires
        // keys — KeyCode.A-Z match in the OnKeyDown override path but
        // not reliably in the KeyDown event path, which is why every
        // letter binding has to use AsRune.
        listView.KeyDown += (s, e) =>
        {
            char rune = char.ToLowerInvariant((char)e.AsRune.Value);

            // Sort prefix: awaiting sort-suffix input after 's'.
            if (_awaitingSortSuffix)
            {
                _awaitingSortSuffix = false;
                sortKey = rune switch
                {
                    'a' => "A",
                    'l' => "L",
                    'k' => "K",
                    'r' => "R",
                    'f' => "F",
                    _   => sortKey,
                };
                Bestiary.SessionSort = sortKey;
                Refresh();
                e.Handled = true;
                return;
            }

            // Control keys — KeyCode works reliably for non-letter keys.
            switch (e.KeyCode)
            {
                case KeyCode.Esc:
                    Bestiary.SessionSort = sortKey;
                    Bestiary.SessionActiveTab = activeTab;
                    Application.RequestStop();
                    e.Handled = true;
                    return;
                case KeyCode.Tab:
                    activeTab = NextTab(activeTab, forward: !e.IsShift);
                    Bestiary.SessionActiveTab = activeTab;
                    RefreshDetail();
                    e.Handled = true;
                    return;
            }

            // Letter keys — use rune. Case-insensitive via ToLowerInvariant.
            switch (rune)
            {
                case 'y':
                    Bestiary.SessionSort = sortKey;
                    Bestiary.SessionActiveTab = activeTab;
                    Application.RequestStop();
                    e.Handled = true;
                    return;
                case 's':
                    _awaitingSortSuffix = true;
                    e.Handled = true;
                    return;
                case 'b':
                    filter.BossOnly = !filter.BossOnly;
                    Refresh();
                    e.Handled = true;
                    return;
                case 'u':
                    filter.ShowUndiscovered = !filter.ShowUndiscovered;
                    Refresh();
                    e.Handled = true;
                    return;
                case 'c':
                    filter.Clear();
                    Refresh();
                    e.Handled = true;
                    return;
                case '/':
                    searchLabel.Visible = true;
                    searchField.Visible = true;
                    searchField.Text = filter.Search ?? "";
                    searchField.SetFocus();
                    e.Handled = true;
                    return;
            }

            // Tag chips: 1-9, 0, 'a'/'w' map 12 tags. Raw rune (not lowercased) drives digits.
            // 'a' collides with sort-suffix — only reachable when NOT awaiting.
            int tagIdx = TagIndexFor((char)e.AsRune.Value);
            if (tagIdx >= 0 && tagIdx < KnownTags.Length)
            {
                string tag = KnownTags[tagIdx];
                if (!filter.ActiveTags.Add(tag)) filter.ActiveTags.Remove(tag);
                Refresh();
                e.Handled = true;
                return;
            }
        };

        // Esc safety net — catches drifted focus (e.g. after Tab cycling).
        DialogHelper.CloseOnEscape(dialog);

        // Focus list on open so primary keybind handler receives events immediately.
        Refresh();
        listView.SetFocus();
        DialogHelper.RunModal(dialog);
    }

    // Two-key S-prefix sort state; rolls to next KeyDown frame for the suffix.
    private static bool _awaitingSortSuffix;

    // ── List row formatting ──
    // Template: "{prefix}{glyph} {name,-18} L{level,2} x{kills,2}"; prefix ! boss, * elite, · normal, ? undisc.
    private static string FormatRow(Bestiary.Entry e)
    {
        char prefix = e.IsBoss || e.IsFieldBoss ? '!'
            : e.IsElite ? '*'
            : '\u00B7';  // middle dot
        char glyph = e.Glyph == default || e.Glyph == '\0' ? '?' : e.Glyph;
        string name = e.Name.Length > 18 ? e.Name[..17] + "\u2026" : e.Name.PadRight(18);
        string lvl = e.Level > 0 ? e.Level.ToString().PadLeft(2) : "??";
        string kills = e.TimesKilled >= 100 ? "99+" : e.TimesKilled.ToString().PadLeft(2);
        return $" {prefix}{glyph} {name} L{lvl} x{kills}";
    }

    private static string FormatUndiscoveredRow() =>
        " ?? ???                L?? x 0";

    // ── Tab strip rendering ──────────────────────────────────────────────
    private static string TabStrip(string active)
    {
        string Mark(string t) => t == active ? $"[{t}]" : $" {t} ";
        return $"{Mark("Overview")} {Mark("Combat")} {Mark("Lore")} {Mark("History")}";
    }

    private static string NextTab(string cur, bool forward)
    {
        var tabs = new[] { "Overview", "Combat", "Lore", "History" };
        int i = Array.IndexOf(tabs, cur);
        if (i < 0) i = 0;
        i = (i + (forward ? 1 : tabs.Length - 1)) % tabs.Length;
        return tabs[i];
    }

    // ── Detail pane builders ─────────────────────────────────────────────
    private static string BuildOverview(Bestiary.Entry e, Player player)
    {
        string threat = ThreatLabel(e.Level, player.Level);
        string tag = TitleCase(e.LootTag);
        string floors = e.FirstFloorEncountered == e.LastFloorEncountered
            ? $"F{e.FirstFloorEncountered}"
            : $"F{e.FirstFloorEncountered} - F{e.LastFloorEncountered}";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"  {e.Glyph}  {e.Name}");
        sb.AppendLine($"      {tag} · Lv {e.Level} · {threat}");
        sb.AppendLine();
        sb.AppendLine($"  HP   {Bar(e.MaxHp, 300, 12)}  {e.MaxHp}");
        sb.AppendLine($"  ATK  {Bar(e.Atk,   80,  12)}  {e.Atk}");
        sb.AppendLine();
        sb.AppendLine($"  Floors      {floors}");
        sb.AppendLine($"  Encounters  {e.TimesEncountered}");
        sb.AppendLine($"  Kills       {e.TimesKilled}");
        if (!string.IsNullOrEmpty(e.LastSeenDate))
            sb.AppendLine($"  Last seen   {e.LastSeenDate}");
        if (e.DeathsCausedAcrossRuns > 0)
            sb.AppendLine($"  Your deaths {e.DeathsCausedAcrossRuns} (across all runs)");
        return sb.ToString();
    }

    private static string BuildCombat(Bestiary.Entry e, Player player)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"  {e.Name} — Combat notes");
        sb.AppendLine();
        sb.AppendLine($"  Level       {e.Level}   (you: {player.Level}, {ThreatLabel(e.Level, player.Level)})");
        sb.AppendLine($"  Max HP      {e.MaxHp}");
        sb.AppendLine($"  Base ATK    {e.Atk}");
        sb.AppendLine();
        sb.AppendLine("  Can inflict:");
        if (!e.CanPoison && !e.CanBleed && !e.CanStun && !e.CanSlow)
            sb.AppendLine("    (none observed)");
        else
        {
            if (e.CanPoison) sb.AppendLine("    \u00B7 Poison");
            if (e.CanBleed)  sb.AppendLine("    \u00B7 Bleed");
            if (e.CanStun)   sb.AppendLine("    \u00B7 Stun");
            if (e.CanSlow)   sb.AppendLine("    \u00B7 Slow");
        }
        sb.AppendLine();
        if (e.IsBoss)       sb.AppendLine("  Role: Floor Boss");
        else if (e.IsFieldBoss) sb.AppendLine("  Role: Field Boss (roaming elite)");
        else if (e.IsElite) sb.AppendLine("  Role: Elite / Champion variant observed");
        else                sb.AppendLine("  Role: Standard roster");
        sb.AppendLine();
        if (e.DeathsCausedAcrossRuns > 0)
            sb.AppendLine($"  You've died to this {e.DeathsCausedAcrossRuns} time(s).");
        else
            sb.AppendLine("  This mob has never killed you.");
        return sb.ToString();
    }

    private static string BuildLore(Bestiary.Entry e)
    {
        string flavor = BestiaryFlavor.Lookup(
            e.Name, e.LootTag, e.FirstFloorEncountered, e.LastFloorEncountered);
        if (string.IsNullOrEmpty(flavor)) flavor = "(No recorded lore.)";
        return "  " + WrapParagraph(flavor, 70, indent: "  ");
    }

    private static string BuildHistory(Bestiary.Entry e)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"  {e.Name} — History");
        sb.AppendLine();
        sb.AppendLine($"  Kills this run:      {e.TimesKilled}");
        sb.AppendLine($"  Encounters this run: {e.TimesEncountered}");
        sb.AppendLine();
        if (e.DeathsCausedAcrossRuns > 0)
            sb.AppendLine($"  Lifetime deaths caused: {e.DeathsCausedAcrossRuns}");
        sb.AppendLine();
        sb.AppendLine("  (kill-per-run sparkline coming in a future update)");
        return sb.ToString();
    }

    // ── Helpers ──────────────────────────────────────────────────────────
    private static string Bar(int value, int max, int width)
    {
        if (max <= 0) max = 1;
        int filled = Math.Clamp(value * width / max, 0, width);
        return "[" + new string('#', filled) + new string('.', width - filled) + "]";
    }

    private static string ThreatLabel(int mobLevel, int playerLevel)
    {
        int diff = mobLevel - playerLevel;
        if (diff >= 6) return "EXTREME";
        if (diff >= 3) return "dangerous";
        if (diff >= 0) return "challenging";
        if (diff >= -3) return "moderate";
        return "trivial";
    }

    private static string TitleCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return "—";
        return char.ToUpper(s[0]) + s[1..];
    }

    // Map a tag-toggle key to the index in KnownTags. 1-9, 0, a, b cover 12.
    private static int TagIndexFor(char ch) => ch switch
    {
        '1' => 0, '2' => 1, '3' => 2, '4' => 3, '5' => 4,
        '6' => 5, '7' => 6, '8' => 7, '9' => 8, '0' => 9,
        'a' => 10, 'A' => 10,
        'w' => 11, 'W' => 11,  // avoid collision with 'B' boss, 'C' clear, 'U' undisc
        _ => -1,
    };

    // Word-safe wrap at `width`; `indent` prefix repeated on continuation lines.
    private static string WrapParagraph(string text, int width, string indent = "")
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sb = new System.Text.StringBuilder();
        int col = 0;
        foreach (var w in words)
        {
            if (col == 0) { col = w.Length; sb.Append(w); continue; }
            if (col + 1 + w.Length > width)
            {
                sb.AppendLine();
                sb.Append(indent);
                col = w.Length;
                sb.Append(w);
            }
            else
            {
                sb.Append(' '); sb.Append(w); col += 1 + w.Length;
            }
        }
        return sb.ToString();
    }
}
