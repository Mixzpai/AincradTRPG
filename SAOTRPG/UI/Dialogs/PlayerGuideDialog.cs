using System.Text.RegularExpressions;
using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Systems;
using SAOTRPG.Systems.Story;
using SAOTRPG.UI.Helpers;

using SAOTRPG.Systems.Input;

namespace SAOTRPG.UI.Dialogs;

// Player Guide — opened with B ("Book"). Two-pane Wikipedia-style reference.
//
// Left pane (sidebar): pinned "/" search field at top, then 5 collapsible
// category headers ([+]/[-] with (N/M) or (N/??) counts), child topics
// indented underneath when expanded. Categories collapsed by default;
// expand-state persists across sessions via ProfileData.GuideExpandedCategories.
//
// Right pane (body): formatted entry with TITLE in caps, lead paragraph,
// section rules ("--- Name ---"), bullets, [[double-bracket]] cross-refs,
// auto-extracted See also list, auto-computed Referenced by footer.
//
// Key dispatch: child widgets consume keys before dialog.KeyDown can see
// them, so ALL navigation is wired on the focused widget directly.
public static partial class PlayerGuideDialog
{
    // Backlinks are informative in small numbers and noise in large ones — a hub topic
    // accumulates one per referring page and would otherwise bury its own content.
    private const int MaxBacklinksShown = 10;

    private const int MinWidth = 110, MinHeight = 40;
    // MaxWidth gives the body pane 90 cols on wide terminals so wide table
    // rows (e.g. Mining "+4 Iron / +9 Mith / +18 Div L10 ...") fit on one line
    // without the wrap fragmenting their column structure.
    private const int MaxWidth = 150, MaxHeight = 60;
    private const int LeftPaneWidth = 52;

    // Hidden control rows in the sidebar above the topic list (search field + spacer).
    private const int SidebarHeaderRows = 2;

    // Keep <details:Title>...</details> per (topicKey, blockTitle) expansion state.
    private static readonly HashSet<(string topic, string block)> _expanded = new();

    // Captured by Show() for stat-dump; null when opened outside an active run.
    private static TurnManager? _activeTm;
    private static Player? _activePlayer;

    // Sidebar categories in canonical category-jump 1..N order.
    // Also the digit-jump order, and the order the sidebar reads top to bottom — so it doubles as
    // the Guide's implicit reading order. Getting Started leads for that reason.
    private static readonly string[] CategoryOrder =
    {
        "Getting Started", "Combat & Rarity", "Progression", "World", "Items", "Quests & NPCs",
        "Floors",
    };

    // Per-category sidebar tint. Body header borrows the same scheme.
    private static Scheme CategoryColor(string category) => category switch
    {
        "Getting Started"  => ColorSchemes.Gold,
        "Combat & Rarity"  => ColorSchemes.FromColor(Color.BrightRed),
        "Progression"      => ColorSchemes.FromColor(Color.BrightCyan),
        "World"            => ColorSchemes.FromColor(Color.BrightGreen),
        "Items"            => ColorSchemes.FromColor(Color.BrightYellow),
        "Quests & NPCs"    => ColorSchemes.FromColor(Color.BrightMagenta),
        "Floors"           => ColorSchemes.FromColor(Color.BrightBlue),
        _                  => ColorSchemes.Gold,
    };

    // Spoiler-tag check; entries tagged "spoiler" mask their title until
    // ProfileData.GuideKnownTopics contains the literal title.
    private static bool IsSpoilerEntry(PlayerGuideContent.GuideEntry e) =>
        e.Tags != null && Array.IndexOf(e.Tags, "spoiler") >= 0;

    private static bool IsRevealed(PlayerGuideContent.GuideEntry e) =>
        !IsSpoilerEntry(e) || ProfileData.GuideKnownTopics.Contains(e.Title);

    private static string TopicKey(PlayerGuideContent.GuideEntry e) => $"{e.Category}|{e.Title}";

    // ── Search match ──
    // Case-insensitive substring over title, body and tags, plus the alias table. Tags are what
    // let a subject word reach pages that never use it. Static rather than a local function so the
    // audit tool can run the real matcher instead of a copy of it — the Guide states search
    // examples in its own prose, and those are only true if something checks them.
    private static bool Matches(PlayerGuideContent.GuideEntry e, string query)
    {
        if (string.IsNullOrEmpty(query)) return true;
        string q = query.ToLowerInvariant();
        if (e.Title.ToLowerInvariant().Contains(q)) return true;
        if (e.Body.ToLowerInvariant().Contains(q)) return true;
        if (e.Tags is not null)
            foreach (string t in e.Tags)
                if (t.Contains(q, StringComparison.OrdinalIgnoreCase)) return true;
        return PlayerGuideContent.AliasMatches(e.Title, q);
    }

    // Atomic [[Topic]] token regex. Double-bracket; non-greedy; titles lack ']'.
    private static readonly Regex DoubleBracketRegex =
        new(@"\[\[([^\]]+)\]\]", RegexOptions.Compiled);

    // Sidebar row model. Header rows are clickable; topic rows jump to body.
    private enum RowKind { Header, Topic, Spoiler }
    private sealed class Row
    {
        public RowKind Kind;
        public string Category = "";
        public PlayerGuideContent.GuideEntry? Entry;
        public bool Expanded;
        public int Visible, Total;
        public bool CategoryHasSpoilers;
    }


    // Both params nullable for main-menu callers (no active run).
    public static void Show(TurnManager? turnManager = null, Player? player = null)
    {
        ProfileData.EnsureLoaded();
        _expanded.Clear();
        _activeTm = turnManager;
        _activePlayer = player;

        int screenW = AppHost.App.Screen.Width;
        int screenH = AppHost.App.Screen.Height;
        // Clamp shape: at least MinWidth/MinHeight, at most MaxWidth/MaxHeight, and
        // always small enough to leave a 2-col/row terminal margin.
        int w = Math.Min(Math.Min(MaxWidth, Math.Max(MinWidth, (int)(screenW * 0.7))),
                         Math.Max(MinWidth, screenW - 2));
        int h = Math.Min(Math.Min(MaxHeight, Math.Max(MinHeight, (int)(screenH * 0.8))),
                         Math.Max(MinHeight, screenH - 2));
        var dialog = DialogHelper.Create("Player Guide", w, h);

        var entries = PlayerGuideContent.Entries;

        // Bucket entries by category (only the 5 known categories — anything
        // outside falls through to a "spillover" bucket bound to the original key).
        var entriesByCat = new Dictionary<string, List<PlayerGuideContent.GuideEntry>>(StringComparer.OrdinalIgnoreCase);
        foreach (var cat in CategoryOrder)
            entriesByCat[cat] = new List<PlayerGuideContent.GuideEntry>();
        foreach (var e in entries)
        {
            if (!entriesByCat.TryGetValue(e.Category, out var list))
            {
                list = new List<PlayerGuideContent.GuideEntry>();
                entriesByCat[e.Category] = list;
            }
            list.Add(e);
        }

        // Title → entry map for [[bracket]] jumps; first-seen wins on collision.
        var entriesByTitle = new Dictionary<string, PlayerGuideContent.GuideEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries) entriesByTitle.TryAdd(e.Title, e);

        // Reverse map: title → list of titles whose body references it via [[bracket]].
        var referencedBy = BuildReferencedByMap(entries);

        // Sidebar expand-state — restore from profile.
        var expanded = new HashSet<string>(ProfileData.GuideExpandedCategories, StringComparer.OrdinalIgnoreCase);

        // ── Sidebar widgets ───────────────────────────────────────────
        // Search-prefix label sits at column 1 so the field can host a longer
        // query without losing its leading "/" affordance.
        var searchPrefix = new Label
        {
            Text = "/",
            X = 1, Y = 0,
            Width = 2,
            SchemeName = ColorSchemes.GoldName,
        };
        var searchField = new TextField
        {
            Text = "",
            X = 3, Y = 0,
            Width = LeftPaneWidth - 4,
            SchemeName = ColorSchemes.BodyName,
        };
        var searchHint = new Label
        {
            Text = "type to filter",
            X = 3, Y = 1,
            Width = LeftPaneWidth - 4,
            SchemeName = ColorSchemes.DimName,
        };

        // List of sidebar rows. Items are display strings; Row metadata kept in parallel array.
        var sidebar = new ListView
        {
            X = 1, Y = SidebarHeaderRows,
            Width = LeftPaneWidth - 2,
            Height = Dim.Fill(3),
            SchemeName = ColorSchemes.ListSelectionName,
            // Type-ahead off — it swallows the search key and the 1-6 category jumps.
            // ListView.OnKeyDown eats a printable rune before KeyDown is raised, so one arrow
            // key would kill every letter and digit binding for the rest of the session.
            KeystrokeNavigator = null,
        };

        // Vertical separator between sidebar and body.
        var sep = new View
        {
            X = LeftPaneWidth, Y = 0,
            Width = 1, Height = Dim.Fill(2),
            SchemeName = ColorSchemes.DimName,
        };
        sep.DrawingContent += (s, e) =>
        {
            for (int yy = 0; yy < sep.Frame.Height; yy++)
                sep.AddRune(0, yy, (System.Text.Rune)'│');
        };

        // ── Body widgets ──────────────────────────────────────────────
        var bodyHeader = new Label
        {
            Text = "",
            X = LeftPaneWidth + 2, Y = 0,
            Width = Dim.Fill(2),
            SchemeName = ColorSchemes.GoldName,
        };
        // Custom render surface — paints body content rune-by-rune with
        // per-token color, tracks See-also bullets for yellow-highlight focus.
        // Replaces a TextView so we get inline cyan [[brackets]], Gold section
        // rules, and per-row focus tinting (TextView shares one Scheme for all content).
        // bodyHeader sits at Y=0; the body fills directly under it (no rule
        // separator — header carries its own category color as the divider).
        // Bottom 2 rows reserved for footerFlash + hint (Close button removed —
        // [Esc] Close corner hint suffices).
        var bodyText = new BodyRenderView
        {
            X = LeftPaneWidth + 2, Y = 1,
            Width = Dim.Fill(2),
            Height = Dim.Fill(2),
        };

        // Footer trim — 6 segments separated by " · ", target ≤100 chars.
        var hint = new Label
        {
            Text = "[/] search · [←→] panes · [↑↓] move · [Enter] follow · [1-5] category · [?] help · [Esc] close",
            X = 1, Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(1),
            SchemeName = ColorSchemes.DimName,
        };

        // Transient flash for stat-dump confirm / search status. Floats one
        // row above the footer hint; Close button is gone so AnchorEnd(2) is free.
        var footerFlash = new Label
        {
            Text = "",
            X = LeftPaneWidth + 2, Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(2),
            SchemeName = ColorSchemes.SuccessName,
            Visible = false,
        };
        // Generation counter, because Flash is called from KEY HANDLERS and so can fire again
        // before the previous 3s window closes. Without it the older timeout clears the NEWER
        // message early — flash at t=0 and t=2s, and the second one vanishes at t=3s instead of
        // t=5s. IsOpen alone is a retire condition, not a re-entrancy guard.
        int flashGen = 0;
        void Flash(string msg, Scheme? scheme = null)
        {
            footerFlash.Text = msg;
            footerFlash.SetScheme(scheme ?? ColorSchemes.Success);
            footerFlash.Visible = true;
            int myGen = ++flashGen;
            AppHost.App.AddTimeout(TimeSpan.FromSeconds(3), () =>
            {
                // A newer flash owns the row now; this one must not clear it.
                if (myGen != flashGen) return false;
                // Outlives the guide if it closes inside the 3s window.
                if (!DialogHelper.IsOpen(dialog)) return false;
                footerFlash.Visible = false;
                footerFlash.Text = "";
                return false;
            });
        }

        // ── Sidebar row state ─────────────────────────────────────────
        var rows = new List<Row>();
        var rowDisplay = new List<string>();

        // Filter: substring search (lowercase), null/empty = no filter.
        string searchQuery = "";

        // Cached body wrap col (layout-aware). Recomputed on layout.
        int bodyWrapCols = ComputeBodyWrapCols(w);

        // Build/refresh sidebar rows from category map + expand-state + filter.
        void RefreshSidebar()
        {
            rows.Clear();
            rowDisplay.Clear();

            string q = searchQuery.Trim().ToLowerInvariant();
            bool filterActive = !string.IsNullOrEmpty(q);

            foreach (var cat in CategoryOrder)
            {
                if (!entriesByCat.TryGetValue(cat, out var list)) list = new();

                // Pre-compute spoiler counts and filter passes.
                bool catHasSpoilers = false;
                int total = 0;
                int visible = 0;
                int matched = 0;
                var matchedEntries = new List<PlayerGuideContent.GuideEntry>();
                foreach (var e in list)
                {
                    bool spoiler = IsSpoilerEntry(e);
                    if (spoiler) catHasSpoilers = true;
                    bool revealed = IsRevealed(e);
                    if (revealed) visible++;
                    if (!spoiler) total++;

                    if (filterActive)
                    {
                        // Search masked spoilers don't match (their title is hidden).
                        if (!revealed) continue;
                        if (!EntryMatches(e, q)) continue;
                        matched++;
                        matchedEntries.Add(e);
                    }
                    else
                    {
                        matchedEntries.Add(e);
                    }
                }

                if (filterActive && matched == 0) continue;

                // A title match outranks a body match. Without this, searching "reforge" listed
                // "Lisbeth — Rarity 6 Craft Line" and "Sealed Weapons" — which merely mention the
                // word — above the Reforge topic itself. OrderByDescending is stable, so entries
                // keep their declaration order within each group, and categories keep the fixed
                // order the unfiltered sidebar uses.
                if (filterActive)
                    matchedEntries = matchedEntries
                        .OrderByDescending(x => x.Title.ToLowerInvariant().Contains(q))
                        .ToList();

                bool isOpen = filterActive || expanded.Contains(cat);
                string countStr = catHasSpoilers ? $"({visible}/??)" : $"({visible}/{total})";
                if (filterActive) countStr = $"({matched} match)";

                rows.Add(new Row
                {
                    Kind = RowKind.Header,
                    Category = cat,
                    Expanded = isOpen,
                    Visible = visible,
                    Total = total,
                    CategoryHasSpoilers = catHasSpoilers,
                });
                rowDisplay.Add(FormatHeaderRow(cat, isOpen, countStr));

                if (isOpen)
                {
                    foreach (var e in matchedEntries)
                    {
                        bool revealed = IsRevealed(e);
                        rows.Add(new Row
                        {
                            Kind = revealed ? RowKind.Topic : RowKind.Spoiler,
                            Category = cat,
                            Entry = e,
                        });
                        rowDisplay.Add(FormatEntryRow(e, revealed));
                    }
                }
            }

            sidebar.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(rowDisplay));
            // Defensive selection clamp.
            if (sidebar.SelectedItem >= rows.Count)
                sidebar.SelectedItem = Math.Max(0, rows.Count - 1);
            sidebar.SetNeedsDraw();
        }

        // ── Body rendering ────────────────────────────────────────────
        void ShowSelectedSidebar()
        {
            int idx = sidebar.SelectedItem ?? -1;
            if (idx < 0 || idx >= rows.Count)
            {
                bodyHeader.Text = "";
                bodyText.SetMessage("Select a topic.");
                return;
            }
            var row = rows[idx];
            switch (row.Kind)
            {
                case RowKind.Header:
                    bodyHeader.Text = row.Category;
                    bodyHeader.SetScheme(CategoryColor(row.Category));
                    bodyText.SetMessage(row.Expanded
                        ? $"{row.Visible} topic(s) shown. Use Up/Down to browse, Enter to view a topic."
                        : $"Press Enter or Right to expand. ({row.Visible}{(row.CategoryHasSpoilers ? "/??" : $"/{row.Total}")} topics)");
                    return;
                case RowKind.Spoiler:
                    bodyHeader.Text = $"{row.Category} > ??? (Unknown)";
                    bodyHeader.SchemeName = ColorSchemes.DimName;
                    bodyText.SetMessage(WrapTo(
                        "This topic is still hidden.\n\n" +
                        "You have not yet encountered the subject of this entry.\n" +
                        "Defeat the relevant monster, clear the floor boss, or\n" +
                        "complete the gating quest to unlock its page.",
                        Math.Max(20, bodyWrapCols)));
                    return;
                case RowKind.Topic:
                    var e = row.Entry!;
                    bodyHeader.Text = $"{e.Category} > {e.Title}";
                    bodyHeader.SetScheme(CategoryColor(e.Category));
                    bodyText.SetContent(RenderEntryBody(e, bodyWrapCols, referencedBy));
                    return;
            }
        }

        // Opens the first still-collapsed <details:> block on the topic the sidebar has selected.
        // Shared by the sidebar and the body: the Guide opens with focus on the SIDEBAR, so a key
        // wired only inside BodyRenderView is one the player cannot reach from where they start —
        // while the collapsed row naming that key is already on screen beside them.
        bool ToggleDisclosureOnSelected()
        {
            int idx = sidebar.SelectedItem ?? -1;
            if (idx < 0 || idx >= rows.Count) return false;
            if (rows[idx].Kind != RowKind.Topic || rows[idx].Entry == null) return false;
            var entry = rows[idx].Entry!;
            if (!ToggleFirstCollapsed(TopicKey(entry), entry.Body)) return false;
            ShowSelectedSidebar();
            return true;
        }

        // ── Sidebar row formatting ────────────────────────────────────
        string FormatHeaderRow(string cat, bool open, string count)
        {
            string mark = open ? "[-]" : "[+]";
            // Shorten if the count + mark + name overflows the sidebar.
            int budget = LeftPaneWidth - 2 - 2;  // pane minus chrome
            string left = $"{mark} {cat}";
            int gap = budget - left.Length - count.Length;
            if (gap < 1) gap = 1;
            return left + new string(' ', gap) + count;
        }

        string FormatEntryRow(PlayerGuideContent.GuideEntry e, bool revealed)
        {
            string title = revealed ? e.Title : "??? (Unknown)";
            int budget = LeftPaneWidth - 2 - 4;  // pane minus indent
            string display = "    " + title;
            if (display.Length > budget) display = display.Substring(0, Math.Max(4, budget - 3)) + "...";
            return display;
        }

        bool EntryMatches(PlayerGuideContent.GuideEntry e, string q) => Matches(e, q);

        // ── Activation ────────────────────────────────────────────────
        void ToggleHeaderAt(int idx)
        {
            if (idx < 0 || idx >= rows.Count) return;
            var row = rows[idx];
            if (row.Kind != RowKind.Header) return;
            if (expanded.Contains(row.Category))
            {
                expanded.Remove(row.Category);
                ProfileData.SetCategoryExpanded(row.Category, false);
            }
            else
            {
                expanded.Add(row.Category);
                ProfileData.SetCategoryExpanded(row.Category, true);
            }
            // Try to keep the same header row selected after refresh.
            string prevCat = row.Category;
            RefreshSidebar();
            for (int i = 0; i < rows.Count; i++)
                if (rows[i].Kind == RowKind.Header && rows[i].Category == prevCat)
                {
                    sidebar.SelectedItem = i;
                    break;
                }
            ShowSelectedSidebar();
        }

        void JumpToCategory(int n)
        {
            if (n < 1 || n > CategoryOrder.Length) return;
            string cat = CategoryOrder[n - 1];
            if (!expanded.Contains(cat))
            {
                expanded.Add(cat);
                ProfileData.SetCategoryExpanded(cat, true);
            }
            // Clear search filter so the jump lands deterministically.
            searchField.Text = "";
            searchQuery = "";
            RefreshSidebar();
            for (int i = 0; i < rows.Count; i++)
                if (rows[i].Kind == RowKind.Header && rows[i].Category == cat)
                {
                    sidebar.SelectedItem = i;
                    break;
                }
            ShowSelectedSidebar();
            sidebar.SetFocus();
        }

        // Navigate to a topic by exact title (case-insensitive). Pushes nav stack.
        var navStack = new Stack<(string cat, string title)>();
        bool TryNavigateByTitle(string title, bool pushStack)
        {
            title = title.Trim();
            if (!entriesByTitle.TryGetValue(title, out var e)) return false;
            // Spoiler check — block navigation to masked entries.
            if (!IsRevealed(e)) return false;

            // Capture current selection for nav stack.
            if (pushStack)
            {
                int cur = sidebar.SelectedItem ?? -1;
                if (cur >= 0 && cur < rows.Count && rows[cur].Kind == RowKind.Topic && rows[cur].Entry != null)
                    navStack.Push((rows[cur].Entry!.Category, rows[cur].Entry!.Title));
            }

            // Make sure the target category is expanded.
            if (!expanded.Contains(e.Category))
            {
                expanded.Add(e.Category);
                ProfileData.SetCategoryExpanded(e.Category, true);
            }
            // Clear filter so the row is in view.
            searchField.Text = "";
            searchQuery = "";
            RefreshSidebar();
            for (int i = 0; i < rows.Count; i++)
                if (rows[i].Kind == RowKind.Topic && rows[i].Entry != null
                    && string.Equals(rows[i].Entry!.Title, e.Title, StringComparison.OrdinalIgnoreCase))
                {
                    sidebar.SelectedItem = i;
                    break;
                }
            ShowSelectedSidebar();
            return true;
        }

        bool PopNavStack()
        {
            while (navStack.Count > 0)
            {
                var (cat, title) = navStack.Pop();
                if (TryNavigateByTitle(title, pushStack: false)) return true;
            }
            return false;
        }

        // ── Cross-references focus / activation ──────────────────────
        // Right arrow from sidebar hands focus to the body and highlights
        // the first See-also bullet in yellow. Up/Down inside the body
        // walks between bullets; Enter follows.
        void FocusBodyAtFirstSeeAlso()
        {
            if (!bodyText.HasSeeAlsoLinks)
            {
                Flash("No cross-refs in this entry.", ColorSchemes.Dim);
                return;
            }
            bodyText.SetFocus();
            bodyText.FocusFirstSeeAlso();
        }

        // ── Search ────────────────────────────────────────────────────
        // '/' from any focusable widget hands focus to the searchField. If the
        // user is already in the field, '/' types as a literal — same as any
        // text input. Down arrow from the field moves to the sidebar so the
        // user can immediately walk the filtered results.
        void StartSearch()
        {
            if (searchField.HasFocus) return;
            searchField.SetFocus();
            dialog.SetNeedsDraw();
        }
        searchField.TextChanged += (s, e) =>
        {
            searchQuery = (searchField.Text ?? "")?.ToString() ?? "";
            RefreshSidebar();
            // Snap to first matched topic, if any.
            for (int i = 0; i < rows.Count; i++)
                if (rows[i].Kind == RowKind.Topic) { sidebar.SelectedItem = i; break; }
            ShowSelectedSidebar();
        };
        searchField.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Esc)
            {
                if (!string.IsNullOrEmpty(searchField.Text?.ToString()))
                {
                    searchField.Text = "";
                    searchQuery = "";
                    RefreshSidebar();
                    ShowSelectedSidebar();
                    e.Handled = true;
                }
                else
                {
                    sidebar.SetFocus();
                    e.Handled = true;
                }
            }
            else if (e.KeyCode == KeyCode.Enter || e.KeyCode == KeyCode.CursorDown)
            {
                // Down arrow / Enter: hand focus to sidebar so the first
                // filtered topic is auto-selected and the user can arrow
                // through results immediately.
                for (int i = 0; i < rows.Count; i++)
                    if (rows[i].Kind == RowKind.Topic) { sidebar.SelectedItem = i; break; }
                sidebar.SetFocus();
                e.Handled = true;
            }
        };

        // Dialog-level fallback for '/', covering focus targets with no handler of their own.
        // It cannot rescue a rune a focused widget has already eaten — a container's KeyDown runs
        // last, not first — which is why the sidebar list disables its type-ahead navigator.
        dialog.KeyDown += (s, e) =>
        {
            if (e.Handled) return;
            if (Keybinds.IsPressed(GameAction.GuideSearch, e) && !searchField.HasFocus)
            {
                StartSearch();
                e.Handled = true;
            }
        };

        // ── Sidebar selection / activation wiring ─────────────────────
        sidebar.ValueChanged += (s, e) => { ShowSelectedSidebar(); };

        sidebar.KeyDown += (s, e) =>
        {
            // Tested ahead of everything else, mirroring BodyRenderView, so a rebind onto a key
            // the global handler also claims resolves the same way from both panes.
            if (Keybinds.IsPressed(GameAction.GuideExpand, e) && ToggleDisclosureOnSelected())
            {
                e.Handled = true;
                return;
            }

            if (HandleGlobalGuideKey(e.KeyCode, e.AsRune,
                StartSearch, JumpToCategory, () => ShowKeybindOverlay(),
                () => ShowStatDump(Flash)))
            {
                e.Handled = true;
                return;
            }

            int idx = sidebar.SelectedItem ?? -1;
            switch (e.KeyCode)
            {
                case KeyCode.Tab:
                    FocusBodyAtFirstSeeAlso();
                    e.Handled = true;
                    return;
                case KeyCode.CursorRight:
                    if (idx >= 0 && idx < rows.Count)
                    {
                        // Collapsed header: expand (preserves tree muscle memory).
                        if (rows[idx].Kind == RowKind.Header && !rows[idx].Expanded)
                        {
                            ToggleHeaderAt(idx);
                            e.Handled = true;
                            return;
                        }
                        // Topic row or already-expanded header: cross over to the
                        // body and highlight the entry's first See-also bullet.
                        if (rows[idx].Kind == RowKind.Topic
                            || (rows[idx].Kind == RowKind.Header && rows[idx].Expanded))
                        {
                            FocusBodyAtFirstSeeAlso();
                            e.Handled = true;
                            return;
                        }
                    }
                    break;
                case KeyCode.CursorLeft:
                    if (idx >= 0 && idx < rows.Count)
                    {
                        if (rows[idx].Kind == RowKind.Header && rows[idx].Expanded)
                        {
                            ToggleHeaderAt(idx);
                            e.Handled = true;
                            return;
                        }
                        // On a topic row, Left jumps focus up to its category header.
                        if (rows[idx].Kind == RowKind.Topic || rows[idx].Kind == RowKind.Spoiler)
                        {
                            for (int i = idx - 1; i >= 0; i--)
                                if (rows[i].Kind == RowKind.Header)
                                {
                                    sidebar.SelectedItem = i;
                                    ShowSelectedSidebar();
                                    e.Handled = true;
                                    return;
                                }
                        }
                    }
                    break;
                case KeyCode.Enter:
                    if (idx >= 0 && idx < rows.Count)
                    {
                        if (rows[idx].Kind == RowKind.Header)
                        {
                            ToggleHeaderAt(idx);
                            e.Handled = true;
                            return;
                        }
                        if (rows[idx].Kind == RowKind.Topic)
                        {
                            // Topic already showing — Enter crosses to body and
                            // highlights the first See-also bullet (alias for Right).
                            FocusBodyAtFirstSeeAlso();
                            e.Handled = true;
                            return;
                        }
                    }
                    break;
                case KeyCode.Backspace when Keybinds.IsPressed(GameAction.GuideBack, e):
                    if (PopNavStack()) { e.Handled = true; return; }
                    break;
                case KeyCode.O | KeyCode.CtrlMask:
                    if (PopNavStack()) { e.Handled = true; return; }
                    break;
            }
        };

        // ── Body wiring (BodyRenderView) ──────────────────────────────
        // Up/Down inside the body cycles See-also bullets (yellow highlight)
        // OR scrolls if the entry has no cross-refs. Left returns to sidebar.
        // Enter on a focused bullet follows the link; otherwise toggles the
        // ▸/▾ disclosure block on the rendered cursor row (handled here so we
        // can read sidebar selection state for the topic key).
        bodyText.LinkActivated += target => TryNavigateByTitle(target, pushStack: true);
        bodyText.RequestSidebarFocus += () => sidebar.SetFocus();
        bodyText.RequestPopNavStack += () =>
        {
            if (!PopNavStack()) Flash("history empty", ColorSchemes.Dim);
        };
        // BodyRenderView has no cursor concept (it's a paint surface), so <details:> toggling is
        // best-effort: the first collapsed block on the selected topic.
        bodyText.OnEnterToggleDisclosure = ToggleDisclosureOnSelected;
        bodyText.KeyDown += (s, e) =>
        {
            if (HandleGlobalGuideKey(e.KeyCode, e.AsRune,
                StartSearch, JumpToCategory, () => ShowKeybindOverlay(),
                () => ShowStatDump(Flash)))
            {
                e.Handled = true;
                return;
            }
            // Tab from body returns to sidebar (mirrors Left arrow). All other
            // keys (Up/Down/Enter/Left/Backspace) are handled inside
            // BodyRenderView.OnKeyDown via the callbacks above.
            if (e.KeyCode == KeyCode.Tab)
            {
                bodyText.ClearLinkFocus();
                sidebar.SetFocus();
                e.Handled = true;
            }
        };

        // ── Layout-driven wrap recompute ──────────────────────────────
        dialog.SubViewsLaidOut += (s, e) =>
        {
            int newWrap = ComputeBodyWrapCols(dialog.Frame.Width);
            if (newWrap != bodyWrapCols)
            {
                bodyWrapCols = newWrap;
                ShowSelectedSidebar();
            }
        };

        // ── Assemble ──────────────────────────────────────────────────
        dialog.Add(searchPrefix, searchField, searchHint, sidebar, sep,
                   bodyHeader, bodyText, footerFlash, hint);
        // No centered Close button — [Esc] Close corner hint at AnchorEnd(13)
        // handles affordance, frees 2 rows of body real estate.
        var escHint = new Label
        {
            Text = "[Esc] Close",
            X = Pos.AnchorEnd(13), Y = Pos.AnchorEnd(1),
            SchemeName = ColorSchemes.DimName,
        };
        dialog.Add(escHint);
        DialogHelper.CloseOnEscape(dialog);

        // First-open shape: respect persisted expand-state. If none persisted,
        // start fully collapsed and select the first category header.
        RefreshSidebar();
        if (rows.Count > 0)
        {
            sidebar.SelectedItem = 0;
            ShowSelectedSidebar();
        }

        sidebar.SetFocus();
        DialogHelper.RunModal(dialog);
    }

    // ── Global key dispatch ──
    // Returns true if handled. Wired on every focusable widget that can receive keys.
    private static bool HandleGlobalGuideKey(KeyCode key, System.Text.Rune rune,
        Action onSearch, Action<int> onCategory, Action onHelp, Action onExport)
    {
        // Range rather than one case per digit: this was D1-D5 when there were five categories,
        // and adding Floors as a sixth left it with no shortcut. JumpToCategory range-checks
        // against CategoryOrder, so an out-of-range digit is a no-op rather than a crash.
        if (key >= KeyCode.D1 && key <= KeyCode.D9)
        {
            onCategory((int)(key - KeyCode.D0));
            return true;
        }
        if (rune.Value == '/') { onSearch(); return true; }
        if (rune.Value == '?') { onHelp(); return true; }
        if (rune.Value == 'e' || rune.Value == 'E') { onExport(); return true; }
        return false;
    }

    // ── Body wrap column computation ──
    // Pane width minus a 2-col safety margin; capped at 90 (still inside the
    // generous-prose readability range while letting wide table rows fit on
    // one line at MaxWidth=150).
    private static int ComputeBodyWrapCols(int dialogWidth)
    {
        int paneCols = Math.Max(20, dialogWidth - LeftPaneWidth - 5);
        return Math.Min(90, paneCols - 2);
    }

    // Floors-category lock gate. Returns the locked variant when the player
    // hasn't reached the floor yet OR the entry body is still the writer stub.
    // Locked: drop Biome + Boss lines from the box header, replace body with
    // "???\n\n(Reach Floor N to unlock this entry.)".
    private static string MaybeLockFloorEntry(PlayerGuideContent.GuideEntry e, string body)
    {
        // Floor number lives at the end of the title, e.g. "Floor 50".
        int spaceIdx = e.Title.LastIndexOf(' ');
        if (spaceIdx < 0) return body;
        if (!int.TryParse(e.Title.AsSpan(spaceIdx + 1), out int n)) return body;

        bool reached = SAOTRPG.Systems.LifetimeStats.Load().MaxFloorReached >= n;
        bool isStub = body.Contains("___UNREACHED___", StringComparison.Ordinal);
        if (reached && !isStub) return body;

        var lines = body.Replace("\r\n", "\n").Split('\n');
        var header = new System.Text.StringBuilder();
        bool inHeader = false;
        foreach (var line in lines)
        {
            if (line.StartsWith("┌─", StringComparison.Ordinal))
            {
                inHeader = true;
                header.Append(line).Append('\n');
                continue;
            }
            if (!inHeader) continue;
            if (line.StartsWith("└─", StringComparison.Ordinal))
            {
                header.Append(line).Append('\n');
                break;
            }
            // Drop Biome + Boss rows; keep Topic + Tier.
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("│ Biome:", StringComparison.Ordinal)) continue;
            if (trimmed.StartsWith("│ Boss:", StringComparison.Ordinal)) continue;
            header.Append(line).Append('\n');
        }
        header.Append('\n');
        header.Append("???\n\n(Reach Floor ").Append(n).Append(" to unlock this entry.)");
        return header.ToString();
    }

    // ── Wikipedia-style body renderer ──
    // Pipeline: NormalizeLegacyBody → progressive-disclosure expand → append
    // unified See-also block → wrap. The See-also block merges outgoing
    // references (extracted from RAW body so legacy SEE ALSO paragraphs still
    // contribute) with incoming references (the reverse map), deduped. All
    // refs render as focusable bullets — BodyRenderView tints the focused row
    // yellow and the user navigates with Up/Down then follows with Enter.
    private static string RenderEntryBody(PlayerGuideContent.GuideEntry e, int wrapCols,
        Dictionary<string, List<string>> referencedBy)
    {
        string srcBody = e.Body;
        if (e.Category == "Floors")
            srcBody = MaybeLockFloorEntry(e, srcBody);
        if (e.Title == "Boss Drop Reference")
            srcBody = PlayerGuideContent.GateBossDropReferenceBody(srcBody, _activeTm);
        return RenderGatedBody(e, srcBody, wrapCols, referencedBy);
    }

    // The pipeline downstream of the two player-state gates. Split out so it can be run over an
    // ungated body without duplicating the pipeline — see PlayerGuideDialog.Audit.cs.
    private static string RenderGatedBody(PlayerGuideContent.GuideEntry e, string srcBody,
        int wrapCols, Dictionary<string, List<string>> referencedBy)
    {
        // Substituted per render, not at type-init, so a rebind made mid-session is reflected the
        // next time the page is opened. Key tokens resolve before the stat block and the body
        // normaliser run, so both see real keys.
        srcBody = PlayerGuideContent.BuildControlsBody(srcBody);
        srcBody = PlayerGuideContent.ResolveStatusCodes(srcBody);
        srcBody = PlayerGuideContent.ResolveStatusLetters(srcBody);
        srcBody = PlayerGuideContent.ResolveMinimapLegend(srcBody);
        srcBody = PlayerGuideContent.ResolveOreDensity(srcBody);
        srcBody = PlayerGuideContent.ResolveLandmarkKinds(srcBody);
        srcBody = PlayerGuideContent.ResolveMapLegend(srcBody);
        srcBody = PlayerGuideContent.ResolveGlyphTokens(srcBody);
        srcBody = PlayerGuideContent.ResolveCampfireQuota(srcBody);
        srcBody = PlayerGuideContent.ResolveBiomeEffects(srcBody);
        srcBody = PlayerGuideContent.ResolveDayClock(srcBody);
        srcBody = PlayerGuideContent.ResolveKeyTokens(srcBody);
        srcBody = ResolveCategoryTokens(srcBody);

        string normalized = NormalizeLegacyBody(srcBody, wrapCols);
        string disclosureRendered = RenderBodyWithDetails(TopicKey(e), normalized);

        // Outgoing refs: extract from RAW body (after MigrateBracketsToDouble)
        // so links inside legacy SEE ALSO paragraphs — which NormalizeLegacyBody
        // strips — still feed the unified block.
        var migratedRawLines = srcBody.Replace("\r\n", "\n").Split('\n');
        var migratedRaw = string.Join("\n", migratedRawLines.Select(MigrateBracketsToDouble));
        var outgoing = ExtractInlineLinks(migratedRaw);

        // Two sections, not one. Merging backlinks into "See also" buried the handful of
        // cross-references the author chose under everything that happens to point here:
        // Floor Scaling Formulas is linked by all 99 floors, so its page was 65 links and 60%
        // link-list. Authored links stay first and complete; backlinks get their own capped
        // footer, which is what the file header has always described.
        var seen = new HashSet<string>(outgoing, StringComparer.OrdinalIgnoreCase) { e.Title };
        var backlinks = new List<string>();
        if (referencedBy.TryGetValue(e.Title, out var incoming))
            foreach (var t in incoming)
                if (seen.Add(t)) backlinks.Add(t);

        var sb = new System.Text.StringBuilder();
        sb.Append(e.Title.ToUpperInvariant()).Append('\n').Append('\n');
        sb.Append(new string('-', wrapCols)).Append('\n').Append('\n');

        // Built from srcBody, so it is downstream of the floor-lock gate — a locked floor has
        // already had its Biome and Boss lines removed and simply shows fewer rows.
        string stats = BuildStatBlock(srcBody, wrapCols);
        if (stats.Length > 0) sb.Append(stats).Append('\n');

        sb.Append(disclosureRendered.TrimEnd());

        if (outgoing.Count > 0)
        {
            sb.Append('\n').Append('\n');
            sb.Append(SectionRule("See also", wrapCols)).Append('\n').Append('\n');
            foreach (var t in outgoing)
                sb.Append("  * [[").Append(t).Append("]]\n");
            sb.Length--;
        }

        if (backlinks.Count > 0)
        {
            sb.Append('\n').Append('\n');
            sb.Append(SectionRule("Referenced by", wrapCols)).Append('\n').Append('\n');
            foreach (string t in backlinks.Take(MaxBacklinksShown))
                sb.Append("  * [[").Append(t).Append("]]\n");
            if (backlinks.Count > MaxBacklinksShown)
                sb.Append("  ").Append(backlinks.Count - MaxBacklinksShown)
                  .Append(" more topics link here.\n");
            sb.Length--;
        }

        return WrapTo(sb.ToString(), wrapCols);
    }

    // The navigation topic describes the sidebar, so it must read the sidebar rather than a
    // transcription of it — a sixth category appeared once and left the page claiming five.
    // The digit form is padded to a fixed width so the surrounding key table stays in column.
    private static string ResolveCategoryTokens(string body)
    {
        if (!body.Contains("{{CATEGORY_", StringComparison.Ordinal)) return body;
        int n = Math.Min(CategoryOrder.Length, 9);   // the sidebar binds D1-D9
        return body
            .Replace("{{CATEGORY_COUNT}}", CategoryOrder.Length.ToString())
            .Replace("{{CATEGORY_DIGITS}}", $"1 - {n}");
    }

    // ── Topic stat block ──
    // Every entry carries a "┌─ … └─" metadata box that NormalizeLegacyBody strips, so it was
    // invisible for the game's whole life — searchable (EntryMatches reads the raw Body) but never
    // read by a player. Surfaced here for every category.
    //
    // The labels are deliberately NOT reduced to a shared vocabulary. They are per-topic infobox
    // rows — "Base radius" on Vision & FOV, "Cooldown" on sword-skill timing, "Bleed"/"Poison" on
    // the status topic — so the variety IS the content, and flattening it would delete facts. Only
    // true synonyms were merged.
    //
    // The title is already the heading directly above, so a "Topic:" row would just repeat it.
    private const string StatBoxSkippedField = "Topic";

    // Reads the "┌─ … └─" header box into its label/value rows. Separate from the rendering below
    // because the rows are also the topic's structured metadata — a checker that wants the Boss or
    // Biome a Floors topic claims must read them exactly as the block does.
    private static List<(string Label, string Value)> ParseStatFields(string body)
    {
        var fields = new List<(string Label, string Value)>();
        if (!body.StartsWith("┌─")) return fields;

        foreach (string line in body.Split('\n'))
        {
            if (line.StartsWith("└─")) break;
            if (!line.StartsWith("│ ")) continue;

            string content = line[2..];
            int colon = content.IndexOf(':');

            // A box row with no colon continues the previous row's value — four World topics wrap
            // a long list that way, and treating the wrap as its own field renders it as garbage.
            if (colon < 0)
            {
                string cont = content.Trim();
                if (cont.Length > 0 && fields.Count > 0)
                    fields[^1] = (fields[^1].Label, fields[^1].Value + " " + cont);
                continue;
            }

            string label = content[..colon].Trim();
            string value = content[(colon + 1)..].Trim();
            if (label.Length == 0 || value.Length == 0) continue;
            if (label == StatBoxSkippedField) continue;
            fields.Add((label, value));
        }
        return fields;
    }

    private static string BuildStatBlock(string body, int wrapCols)
    {
        var fields = ParseStatFields(body);
        if (fields.Count == 0) return "";

        int pad = fields.Max(f => f.Label.Length);
        int valueCol = 2 + pad + 3;
        int room = Math.Max(20, wrapCols - valueCol);

        var sb = new System.Text.StringBuilder();
        foreach ((string label, string value) in fields)
        {
            // Hanging indent: a folded row can outrun the pane (the longest is 107 cols against a
            // 90 wrap), and letting the generic wrapper break it would drop the continuation to
            // column 0 and lose the label/value alignment the block exists for.
            string[] wrapped = WrapValue(value, room);
            sb.Append("  ").Append(label.PadRight(pad)).Append("   ").Append(wrapped[0]).Append('\n');
            for (int i = 1; i < wrapped.Length; i++)
                sb.Append(new string(' ', valueCol)).Append(wrapped[i]).Append('\n');
        }
        return sb.ToString();
    }

    private static string[] WrapValue(string value, int room)
    {
        if (value.Length <= room) return new[] { value };

        var lines = new List<string>();
        var current = new System.Text.StringBuilder();
        foreach (string word in value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current.Length > 0 && current.Length + 1 + word.Length > room)
            {
                lines.Add(current.ToString());
                current.Clear();
            }
            if (current.Length > 0) current.Append(' ');
            current.Append(word);
        }
        if (current.Length > 0) lines.Add(current.ToString());
        return lines.ToArray();
    }

    // ── Legacy body normalization ──
    // Strips the "┌─ ... └─" topic-meta block at the top of every legacy entry.
    // Converts SECTION-CAPS lines to "--- Section ---" rules.
    // Drops the trailing "SEE ALSO" paragraph (auto-rebuilt from inline brackets).
    // Migrates [bracket] tokens (single) to [[bracket]] (double); leaves existing
    // double-brackets untouched.
    // Normalizes leading "·" / "-" / "•" bullets to "  * ".
    private static string NormalizeLegacyBody(string body, int wrapCols)
    {
        if (string.IsNullOrEmpty(body)) return body;
        var lines = body.Replace("\r\n", "\n").Split('\n');
        var outLines = new List<string>(lines.Length);

        // Scan past the leading box-drawing meta block "┌─" ... "└─".
        int start = 0;
        if (lines.Length > 0 && lines[0].StartsWith("┌─"))
        {
            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("└─"))
                {
                    start = i + 1;
                    // Eat any blank line that immediately follows the meta block.
                    while (start < lines.Length && lines[start].Trim().Length == 0) start++;
                    break;
                }
            }
        }

        // Legacy "SEE ALSO" / "REFERENCED BY" sections are dropped here — both are
        // auto-rebuilt at the end of RenderEntryBody from inline [[brackets]] and
        // the cross-entry reverse map. Keeping the legacy paragraphs would
        // double-render the same content.
        bool skipUntilNextSection = false;
        for (int i = start; i < lines.Length; i++)
        {
            string line = lines[i];

            // Migrate [bracket] -> [[bracket]] on this line. Skip already-doubled.
            line = MigrateBracketsToDouble(line);

            // Section header detection: bare ALL-CAPS word at line start, not indented.
            string trimmed = line.TrimEnd();
            if (IsSectionHeaderLine(trimmed))
            {
                string upper = trimmed.ToUpperInvariant();
                if (upper.StartsWith("SEE ALSO") || upper.StartsWith("REFERENCED BY"))
                {
                    skipUntilNextSection = true;
                    continue;
                }
                skipUntilNextSection = false;
                string section = ToTitleCase(trimmed);
                outLines.Add(SectionRule(section, wrapCols));
                outLines.Add("");
                continue;
            }

            if (skipUntilNextSection) continue;

            // Bullet normalization: leading "·" or "-" or "•" + space → "  * ".
            string bulletNormalized = NormalizeBullets(line);
            outLines.Add(bulletNormalized);
        }

        // Squash 3+ consecutive blank lines down to 2.
        var squashed = new List<string>(outLines.Count);
        int blankRun = 0;
        foreach (var l in outLines)
        {
            if (l.Trim().Length == 0)
            {
                blankRun++;
                if (blankRun > 2) continue;
            }
            else blankRun = 0;
            squashed.Add(l);
        }

        // Detect table-row blocks (consecutive indented multi-field rows) and
        // re-align them to a uniform first-field width with 2-space inter-field
        // gaps. Continuations (deep-indent or single-field rows that follow a
        // table row) flow under their parent without breaking the block.
        // Rows that don't form a 2+ row block pass through unchanged.
        squashed = AlignTableBlocks(squashed);

        // Join hand-wrapped zero-indent prose into single paragraphs so the
        // final WrapTo can reflow them to the body width (authors typically
        // hand-wrap source at ~60 chars; on wider panes that leaves dead
        // space). Indented content (bullets, table rows) is left untouched.
        squashed = ReflowProseParagraphs(squashed);

        // Drop trivial sections (Costs: None., Tips: (coming soon), etc).
        squashed = DropTrivialSections(squashed);

        return string.Join("\n", squashed);
    }

    // Reflow contiguous zero-indent non-section prose into single-line paragraphs
    // separated by blank lines. WrapTo at the pipeline tail re-wraps each joined
    // paragraph to the actual body width.
    private static List<string> ReflowProseParagraphs(List<string> lines)
    {
        var result = new List<string>();
        int i = 0;
        while (i < lines.Count)
        {
            string ln = lines[i];
            bool isBlank = ln.Length == 0 || ln.Trim().Length == 0;
            bool isIndented = ln.Length > 0 && (ln[0] == ' ' || ln[0] == '\t');
            bool isSectionRule = ln.TrimEnd().StartsWith("---");

            if (isBlank || isIndented || isSectionRule)
            {
                result.Add(ln);
                i++;
                continue;
            }

            // Collect contiguous zero-indent prose lines into one paragraph.
            var sb = new System.Text.StringBuilder(ln);
            int j = i + 1;
            while (j < lines.Count)
            {
                string nxt = lines[j];
                if (nxt.Length == 0 || nxt.Trim().Length == 0) break;
                if (nxt.TrimEnd().StartsWith("---")) break;
                if (nxt[0] == ' ' || nxt[0] == '\t') break;
                // A hyphenated compound hand-wrapped at its hyphen ("hand-\nauthored") must rejoin
                // with no space, or the reflow prints "hand- authored". The hyphen has to sit
                // directly after a letter and the next line has to start with a lowercase one, so
                // a trailing dash used as punctuation still takes its space.
                bool joinsHyphen = sb.Length >= 2 && sb[^1] == '-' && char.IsLetter(sb[^2])
                                   && nxt.Length > 0 && char.IsLower(nxt[0]);
                if (!joinsHyphen) sb.Append(' ');
                sb.Append(nxt);
                j++;
            }
            result.Add(sb.ToString());
            i = j;
        }
        return result;
    }

    // C4 (smart): detect consecutive indented rows that look like table rows,
    // treat them as one block, and re-emit each row with the first field
    // padded to the block's max first-field width. Continuations (deeper
    // indent than the block's base, OR same-indent single-field rows) flow
    // under their parent without breaking the block.
    //
    // The split is done at the FIRST 2+ space gap on each row — that's the
    // boundary between the skill/label and "everything else". The "rest" is
    // emitted verbatim so any column-2/3 alignment authored in the source is
    // preserved (e.g., aligned XP-source / L10-effect columns).
    //
    // Without this approach, an inconsistent author gap (e.g., a 2-space gap
    // after a long skill name like BARGAINING vs. the typical 7-space gap
    // after shorter names) would either over-split or under-split rows and
    // distort the alignment.
    // Hard ceiling on the first column's pad, guarding against one pathological row pushing every
    // other column across the pane. It has to be at least as wide as the widest first cell any
    // real table uses, because a cell wider than the pad keeps its own width and breaks the column
    // on that row — 32 covers the corpus (the guild roster's 31-character names are the widest).
    // Raising it is safe; layout/wrap-90 catches the point where a table stops fitting.
    private const int MaxFirstFieldPad = 32;

    private static List<string> AlignTableBlocks(List<string> lines)
    {
        var result = new List<string>();
        int i = 0;
        while (i < lines.Count)
        {
            var (indent, firstField, rest) = ParseFirstFieldAndRest(lines[i]);
            bool isTableRow = indent > 0 && firstField != null;
            if (!isTableRow)
            {
                result.Add(lines[i]);
                i++;
                continue;
            }

            int blockStart = i;
            int blockIndent = indent;
            var blockRows = new List<(string first, string rest)> { (firstField!, rest!) };
            var continuations = new List<(int parentIdx, string content)>();

            int j = i + 1;
            while (j < lines.Count)
            {
                string ln = lines[j];
                if (ln.Trim().Length == 0) break;            // blank line ends block
                string trimEnd = ln.TrimEnd();
                if (trimEnd.StartsWith("---")) break;        // section rule ends block

                var (jIndent, jFirst, jRest) = ParseFirstFieldAndRest(ln);
                if (jIndent < blockIndent) break;            // outdent ends block

                if (jIndent == blockIndent && jFirst != null)
                {
                    blockRows.Add((jFirst, jRest!));
                    j++;
                    continue;
                }

                // Deeper indent OR same-indent-but-no-2+-gap = continuation.
                continuations.Add((blockRows.Count - 1, ln));
                j++;
            }

            if (blockRows.Count >= 2)
            {
                // Pad to the widest first field, so every row's second column starts in the same
                // place. A row wider than the pad is NOT truncated — it keeps its own width and
                // breaks the column on that row, which is why this has to be the real maximum:
                // a percentile was tried and it guarantees the outlier rows come out misaligned.
                // MaxFirstFieldPad is the only guard needed against one runaway row dragging the
                // whole block across the pane.
                // Columns beyond the first stay hand-aligned in the source; padding them here is
                // not safe, because a padded row that crosses the wrap is re-flowed by WrapTo,
                // which collapses its internal runs of spaces and destroys the block.
                int maxFirst = Math.Min(MaxFirstFieldPad, blockRows.Max(r => r.first.Length));

                string indentStr = new string(' ', blockIndent);
                string contIndent = new string(' ', blockIndent + maxFirst + 2);

                for (int k = 0; k < blockRows.Count; k++)
                {
                    var (first, rowRest) = blockRows[k];
                    var sb = new System.Text.StringBuilder();
                    sb.Append(indentStr);
                    sb.Append(first.PadRight(maxFirst));
                    sb.Append("  ");
                    sb.Append(rowRest);
                    result.Add(sb.ToString());

                    foreach (var (parentIdx, content) in continuations)
                    {
                        if (parentIdx != k) continue;
                        result.Add(contIndent + content.TrimStart());
                    }
                }
                i = j;
            }
            else
            {
                // Single-row "table" — pass through original lines unchanged.
                for (int k = blockStart; k < j; k++) result.Add(lines[k]);
                i = j;
            }
        }
        return result;
    }

    // Split a line into (indent, firstField, rest) at the FIRST 2+ space gap.
    // Returns (indent, null, null) when no 2+ gap exists (single-token line,
    // bullet, prose). Single-space gaps inside firstField — rare for table
    // rows — are kept as part of firstField, so the split point is the
    // first column boundary the author intended.
    private static (int Indent, string? FirstField, string? Remainder) ParseFirstFieldAndRest(string line)
    {
        if (string.IsNullOrEmpty(line)) return (0, null, null);
        int indent = 0;
        while (indent < line.Length && line[indent] == ' ') indent++;
        if (indent == line.Length) return (indent, null, null);

        int i = indent;
        while (i < line.Length)
        {
            if (line[i] == ' ')
            {
                int gap = i;
                while (gap < line.Length && line[gap] == ' ') gap++;
                if (gap - i >= 2)
                {
                    string firstField = line.Substring(indent, i - indent);
                    string rest = gap < line.Length ? line.Substring(gap) : "";
                    return (indent, firstField, rest);
                }
                i = gap;
            }
            else i++;
        }
        return (indent, null, null);
    }

    // C3: drop sections whose content is empty or trivial markers like "None.",
    // "N/A", "(none yet)", "(coming soon)". Walks the rule-delimited blocks and
    // skips ones with under 30 non-whitespace chars OR starting with a known
    // empty marker. Section header rule itself is dropped along with its body.
    private static List<string> DropTrivialSections(List<string> lines)
    {
        var result = new List<string>();
        int i = 0;
        while (i < lines.Count)
        {
            if (IsRenderedSectionRule(lines[i]))
            {
                int sectionStart = i;
                int sectionEnd = i + 1;
                while (sectionEnd < lines.Count && !IsRenderedSectionRule(lines[sectionEnd]))
                    sectionEnd++;

                var bodySb = new System.Text.StringBuilder();
                for (int j = sectionStart + 1; j < sectionEnd; j++)
                    bodySb.Append(lines[j].Trim()).Append(' ');
                string body = bodySb.ToString().Trim();

                bool trivial = body.Length == 0
                            || (body.Length < 30 && !body.Contains("[["))
                            || body.StartsWith("None", StringComparison.OrdinalIgnoreCase)
                            || body.StartsWith("N/A", StringComparison.OrdinalIgnoreCase)
                            || body.StartsWith("(no", StringComparison.OrdinalIgnoreCase)
                            || body.StartsWith("(coming", StringComparison.OrdinalIgnoreCase)
                            || body.StartsWith("(empty", StringComparison.OrdinalIgnoreCase)
                            || body.StartsWith("(tbd", StringComparison.OrdinalIgnoreCase);

                if (!trivial)
                {
                    for (int j = sectionStart; j < sectionEnd; j++) result.Add(lines[j]);
                }
                else
                {
                    // Drop trailing blank line we just emitted before the rule.
                    while (result.Count > 0 && result[^1].Trim().Length == 0)
                        result.RemoveAt(result.Count - 1);
                }
                i = sectionEnd;
            }
            else
            {
                result.Add(lines[i]);
                i++;
            }
        }
        return result;
    }

    private static bool IsRenderedSectionRule(string line)
    {
        // SectionRule emits "--- Title -----..."; loose match: starts with "---"
        // and has a space + word + dashes shape.
        var t = line.TrimEnd();
        return t.StartsWith("--- ") && t.EndsWith("-") && t.Length >= 8;
    }

    // Section rule: "--- Name ---" with dashes filling out to body wrap width.
    // Trailing dashes auto-extend so the rule visually reaches the right margin
    // at any pane width (mockup contract).
    private static string SectionRule(string title, int cols)
    {
        string head = $"--- {title} ";
        int remaining = Math.Max(3, cols - head.Length);
        return head + new string('-', remaining);
    }

    // ALL-CAPS section header: line is non-empty, starts with letter, not indented,
    // every alphabetic character is upper-case, length ≤ 28. Excludes lines that
    // contain box-drawing or quoted strings.
    private static bool IsSectionHeaderLine(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        if (s.Length > 28) return false;
        if (s[0] == ' ' || s[0] == '\t') return false;
        if (!char.IsLetter(s[0])) return false;
        bool hasLetter = false;
        foreach (char c in s)
        {
            if (char.IsLetter(c)) { hasLetter = true; if (char.IsLower(c)) return false; }
            // Allow space, ampersand, hyphen, parentheses, &, digits, /, :, ',', ?
            else if (!" -&()/:,?".Contains(c) && !char.IsDigit(c)) return false;
        }
        return hasLetter;
    }

    // "DAMAGE FORMULA" → "Damage Formula"; preserves & / -.
    private static string ToTitleCase(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        bool capNext = true;
        foreach (char c in s.ToLowerInvariant())
        {
            if (char.IsLetter(c))
            {
                sb.Append(capNext ? char.ToUpperInvariant(c) : c);
                capNext = false;
            }
            else
            {
                sb.Append(c);
                capNext = !char.IsDigit(c);
            }
        }
        return sb.ToString();
    }

    // Every topic title, for the single-bracket promotion below.
    private static readonly HashSet<string> LinkTargets =
        new(PlayerGuideContent.Entries.Select(e => e.Title), StringComparer.OrdinalIgnoreCase);

    // Promote single-bracket tokens to double, so a legacy "SEE ALSO" paragraph written as
    // "[Critical Hits] · [Combo Attacks]" still feeds the cross-reference block.
    //
    // A token is only promoted when it NAMES A TOPIC. The Guide quotes the game's own UI strings in
    // prose — [SEALED], [NEW], [locked], [UPGRADE], the [F1][F2][F3][F4] quickbar diagram — and
    // promoting those turned them into links to topics that do not exist, visible both inline and as
    // dead bullets in See also. The old rule promoted everything and tried to except keybind tokens
    // by shape, which cannot distinguish a link from a screen label.
    private static string MigrateBracketsToDouble(string line)
    {
        if (string.IsNullOrEmpty(line)) return line;
        if (!line.Contains('[')) return line;

        var sb = new System.Text.StringBuilder(line.Length + 16);
        int i = 0;
        while (i < line.Length)
        {
            // Already doubled? Pass through verbatim.
            if (i + 1 < line.Length && line[i] == '[' && line[i + 1] == '[')
            {
                int close = line.IndexOf("]]", i + 2, StringComparison.Ordinal);
                if (close < 0) { sb.Append(line, i, line.Length - i); break; }
                sb.Append(line, i, (close + 2) - i);
                i = close + 2;
                continue;
            }
            // Single-bracket token: "[X]" where X has no nested brackets.
            if (line[i] == '[')
            {
                int close = line.IndexOf(']', i + 1);
                if (close > i)
                {
                    string inner = line.Substring(i + 1, close - i - 1);
                    if (!LinkTargets.Contains(inner.Trim()))
                    {
                        sb.Append(line, i, (close + 1) - i);
                        i = close + 1;
                        continue;
                    }
                    sb.Append("[[").Append(inner).Append("]]");
                    i = close + 1;
                    continue;
                }
            }
            sb.Append(line[i]);
            i++;
        }
        return sb.ToString();
    }

    // Bullet normalization: "·" / "-" / "•" / "*" at the start of an indented or
    // non-indented line (with space after) → "  * ".
    private static string NormalizeBullets(string line)
    {
        if (string.IsNullOrEmpty(line)) return line;
        // Detect leading whitespace.
        int i = 0;
        while (i < line.Length && (line[i] == ' ' || line[i] == '\t')) i++;
        if (i >= line.Length) return line;
        char c = line[i];
        if ((c == '·' || c == '-' || c == '•' || c == '*')
            && i + 1 < line.Length && line[i + 1] == ' ')
        {
            return "  * " + line.Substring(i + 2);
        }
        return line;
    }

    // ── Inline link extraction ──
    private static List<string> ExtractInlineLinks(string body)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ordered = new List<string>();
        foreach (Match m in DoubleBracketRegex.Matches(body ?? ""))
        {
            string title = m.Groups[1].Value.Trim();
            if (string.IsNullOrEmpty(title)) continue;
            if (seen.Add(title)) ordered.Add(title);
        }
        return ordered;
    }

    // Build reverse map: title → list of titles whose body references it via [[bracket]].
    private static Dictionary<string, List<string>> BuildReferencedByMap(PlayerGuideContent.GuideEntry[] entries)
    {
        var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries)
        {
            // Pre-migrate single-brackets to capture both forms uniformly.
            string body = MigrateBracketsToDouble(e.Body ?? "");
            foreach (Match m in DoubleBracketRegex.Matches(body))
            {
                string target = m.Groups[1].Value.Trim();
                if (string.IsNullOrEmpty(target)) continue;
                if (string.Equals(target, e.Title, StringComparison.OrdinalIgnoreCase)) continue;
                if (!map.TryGetValue(target, out var list))
                {
                    list = new List<string>();
                    map[target] = list;
                }
                if (!list.Contains(e.Title, StringComparer.OrdinalIgnoreCase))
                    list.Add(e.Title);
            }
        }
        return map;
    }

    // ── Progressive disclosure ──
    // <details:TITLE>...</details> → "▸ TITLE (Enter to expand)" collapsed,
    // "▾ TITLE\n<inner>" expanded. Falls back to ASCII '>' / 'v' when toggle on.
    // Read at render time, so a rebind shows up on the collapsed row the next time it is drawn.
    private static string ExpandKeyLabel
    {
        get
        {
            var b = Keybinds.Get(GameAction.GuideExpand);
            return b.Primary.IsBound ? b.Primary.ToString() : "unbound";
        }
    }

    private static string RenderBodyWithDetails(string topicKey, string body)
    {
        if (string.IsNullOrEmpty(body)) return body;
        bool ascii = UserSettings.Current.UseAsciiDisclosureGlyphs;
        string collapsedGlyph = ascii ? ">" : "▸";
        string expandedGlyph = ascii ? "v" : "▾";

        int i = 0;
        var sb = new System.Text.StringBuilder(body.Length);
        while (i < body.Length)
        {
            int open = body.IndexOf("<details:", i, StringComparison.Ordinal);
            if (open < 0) { sb.Append(body, i, body.Length - i); break; }
            sb.Append(body, i, open - i);
            int titleEnd = body.IndexOf('>', open);
            if (titleEnd < 0) { sb.Append(body, open, body.Length - open); break; }
            string title = body.Substring(open + "<details:".Length, titleEnd - (open + "<details:".Length));
            int close = body.IndexOf("</details>", titleEnd, StringComparison.Ordinal);
            if (close < 0) { sb.Append(body, open, body.Length - open); break; }
            string inner = body.Substring(titleEnd + 1, close - titleEnd - 1);
            bool isExpanded = _expanded.Contains((topicKey, title));
            if (isExpanded)
                sb.Append(expandedGlyph).Append(' ').Append(title).Append('\n').Append(inner.TrimEnd('\n'));
            else
                sb.Append(collapsedGlyph).Append(' ').Append(title)
                  .Append(" (").Append(ExpandKeyLabel).Append(" to expand)");
            i = close + "</details>".Length;
        }
        return sb.ToString();
    }

    // BodyRenderView paints rune-by-rune so there's no TextView cursor concept; the expand key
    // opens the next still-collapsed block on this topic. Once they are all open it collapses them
    // all again, so the key is a round trip rather than one-way — a player who expanded a long
    // table can get the compact page back without leaving the topic.
    private static bool ToggleFirstCollapsed(string topicKey, string body)
    {
        if (string.IsNullOrEmpty(body)) return false;
        var titles = new List<string>();
        int i = 0;
        while (i < body.Length)
        {
            int open = body.IndexOf("<details:", i, StringComparison.Ordinal);
            if (open < 0) break;
            int titleEnd = body.IndexOf('>', open);
            if (titleEnd < 0) break;
            string title = body.Substring(open + "<details:".Length, titleEnd - (open + "<details:".Length));
            titles.Add(title);
            int close = body.IndexOf("</details>", titleEnd, StringComparison.Ordinal);
            if (close < 0) break;
            i = close + "</details>".Length;
        }
        if (titles.Count == 0) return false;

        foreach (string t in titles)
            if (_expanded.Add((topicKey, t))) return true;

        foreach (string t in titles) _expanded.Remove((topicKey, t));
        return true;
    }

    // Nothing but rule characters — an authored table underline, not content.
    private static bool IsRuleLine(string s)
    {
        if (s.Length < 4) return false;
        foreach (char c in s)
            if (c is not ('-' or '─' or '=' or '_')) return false;
        return true;
    }

    // ── Paragraph-preserving wrap ──
    // Blank-line-separated paragraphs wrap independently; leading whitespace
    // (the "lead") repeats on continuation lines so bullets indent cleanly.
    // [[bracketed-links]] are atomic — never broken across lines.
    private static string WrapTo(string text, int cols)
    {
        if (string.IsNullOrEmpty(text)) return text;
        if (cols < 20) cols = 20;
        var outSb = new System.Text.StringBuilder(text.Length + 32);
        var lines = text.Replace("\r\n", "\n").Split('\n');
        for (int li = 0; li < lines.Length; li++)
        {
            string line = lines[li];
            if (line.Length == 0)
            {
                if (li < lines.Length - 1) outSb.Append('\n');
                continue;
            }
            int leadCount = 0;
            while (leadCount < line.Length && (line[leadCount] == ' ' || line[leadCount] == '\t')) leadCount++;
            string lead = line.Substring(0, leadCount);
            string rest = line.Substring(leadCount);

            // Section rules and lines that are already short enough pass through.
            if (line.Length <= cols)
            {
                outSb.Append(line);
                if (li < lines.Length - 1) outSb.Append('\n');
                continue;
            }

            // A horizontal rule authored under a table header is one unbreakable token, so the
            // wrapper would pass it through at its full width. Clipping is the right answer for a
            // rule — it is decoration sized to its table, and a narrower pane simply wants less.
            if (IsRuleLine(rest))
            {
                outSb.Append(line, 0, cols);
                if (li < lines.Length - 1) outSb.Append('\n');
                continue;
            }

            var tokens = TokenizeAtomic(rest);
            if (tokens.Count == 0)
            {
                outSb.Append(line);
                if (li < lines.Length - 1) outSb.Append('\n');
                continue;
            }

            var cur = new System.Text.StringBuilder();
            cur.Append(lead);
            bool first = true;
            foreach (var tok in tokens)
            {
                int addLen = tok.Length + (first ? 0 : 1);
                if (cur.Length + addLen > cols && cur.Length > lead.Length)
                {
                    outSb.Append(cur).Append('\n');
                    cur.Clear();
                    cur.Append(lead);
                    first = true;
                }
                if (!first) cur.Append(' ');
                cur.Append(tok);
                first = false;
            }
            outSb.Append(cur);
            if (li < lines.Length - 1) outSb.Append('\n');
        }
        return outSb.ToString();
    }

    // Whitespace tokenizer that keeps "[[bracketed]]" tokens atomic. Trailing
    // punctuation is absorbed onto the bracket token to preserve sentence flow.
    private static List<string> TokenizeAtomic(string s)
    {
        var result = new List<string>();
        int i = 0;
        while (i < s.Length)
        {
            if (s[i] == ' ' || s[i] == '\t') { i++; continue; }
            if (i + 1 < s.Length && s[i] == '[' && s[i + 1] == '[')
            {
                int close = s.IndexOf("]]", i + 2, StringComparison.Ordinal);
                if (close >= 0)
                {
                    int end = close + 2;
                    while (end < s.Length && ".,;:!?".IndexOf(s[end]) >= 0) end++;
                    result.Add(s.Substring(i, end - i));
                    i = end;
                    continue;
                }
            }
            int j = i;
            while (j < s.Length && s[j] != ' ' && s[j] != '\t') j++;
            result.Add(s.Substring(i, j - i));
            i = j;
        }
        return result;
    }

    // ── ? keybindings overlay ── Modal 60x22; any key dismisses.
    private static void ShowKeybindOverlay()
    {
        const int W = 62, H = 32;
        var overlay = DialogHelper.Create("Player Guide — Keybindings", W, H);

        string[] lines = {
            "SIDEBAR (categories + topics)",
            "  Up Down     select sidebar row",
            "  Right       expand category, OR cross to body and",
            "              highlight the first See-also bullet (yellow)",
            "  Left        collapse category, OR jump to its header",
            "  1 - 6       jump to category (auto-expand)",
            "  Enter       expand header / cross to body See-also",
            "",
            "BODY (right pane)",
            "  Up Down     move yellow highlight between See-also",
            "              bullets (or scroll body when no cross-refs)",
            "  PgUp PgDn   page scroll",
            "  Home End    jump to top / bottom",
            "  Enter       follow highlighted link, OR toggle ▸/▾ block",
            "  Left Tab    return focus to sidebar",
            "  Backspace   history back (Ctrl+O also)",
            "",
            "SEARCH",
            "  /           focus the search bar",
            "  Down/Enter  exit field to first matched topic",
            "  Esc         clear query (1st), close dialog (2nd)",
            "",
            "ACTIONS",
            "  e           export run summary to clipboard",
            "  ?           show this help",
        };

        int y = 1;
        foreach (var s in lines)
        {
            bool isHeader = s.Length > 0 && s[0] >= 'A' && s[0] <= 'Z' && !s.StartsWith("  ");
            var lbl = new Label
            {
                Text = s, X = 2, Y = y, Width = Dim.Fill(2),
                SchemeName = isHeader ? ColorSchemes.GoldName : ColorSchemes.BodyName,
            };
            overlay.Add(lbl);
            y++;
        }

        var hintLabel = new Label
        {
            Text = "Press any key to dismiss",
            X = Pos.Center(), Y = Pos.AnchorEnd(1),
            SchemeName = ColorSchemes.DimName,
        };
        overlay.Add(hintLabel);

        overlay.KeyDown += (s, e) =>
        {
            AppHost.App.RequestStop();
            e.Handled = true;
        };

        // Nested run loop — excluded from profiler scopes for the same reason RunModal is.
        SAOTRPG.Systems.Profiler.BeginExclusion();
        try { AppHost.App.Run(overlay); }
        finally { SAOTRPG.Systems.Profiler.EndExclusion(); }
        overlay.Dispose();
    }

    // ── Run-summary clipboard dump ──
    private static void ShowStatDump(Action<string, Scheme?> flash)
    {
        var tm = _activeTm;
        var p = _activePlayer;
        if (tm == null || p == null)
        {
            flash("No active run — open from the game screen to export stats.", ColorSchemes.Danger);
            return;
        }

        string name = $"{p.FirstName} {p.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(name)) name = "Unnamed";

        var skills = SAOTRPG.Systems.Skills.UniqueSkillSystem.Unlocked
            .Select(sk => SAOTRPG.Systems.Skills.UniqueSkillSystem.Definitions[sk].Name)
            .ToList();
        string skillList = skills.Count == 0 ? "None" : string.Join(", ", skills);

        // Divine objects — aggregate inventory + equipped slots; cap denominator
        // is the lore total (17 Divines), not the legacy 7-canon-knight count.
        var divineNames = new List<string>();
        foreach (var it in p.Inventory.Items)
            if (it.Rarity == "Divine" && !string.IsNullOrEmpty(it.Name))
                divineNames.Add(it.Name!);
        foreach (Inventory.Core.EquipmentSlot slot in Enum.GetValues<Inventory.Core.EquipmentSlot>())
        {
            var eq = p.Inventory.GetEquipped(slot);
            if (eq != null && eq.Rarity == "Divine" && !string.IsNullOrEmpty(eq.Name))
                divineNames.Add(eq.Name!);
        }
        string divineStr = divineNames.Count == 0
            ? "0/17"
            : $"{Math.Min(divineNames.Count, 17)}/17 ({string.Join(", ", divineNames)})";

        var mods = RunModifiers.Active
            .Select(m => RunModifiers.Definitions[m].Name)
            .ToList();
        string modStr = mods.Count == 0 ? "None" : string.Join(", ", mods);

        var dump = new System.Text.StringBuilder();
        dump.AppendLine("AINCRAD TRPG — RUN SUMMARY");
        dump.AppendLine("============================");
        dump.AppendLine($"Character: {name} · Level {p.Level} · Floor {tm.CurrentFloor}");
        dump.AppendLine($"HP: {p.CurrentHealth}/{p.MaxHealth} · SP: {p.SkillPoints}");
        dump.AppendLine($"Kills: {tm.KillCount} · Turns: {tm.TurnCount} · Col: {p.ColOnHand}");
        dump.AppendLine($"Active Unique Skills: {skillList}");
        dump.AppendLine($"Divine Objects Found: {divineStr}");
        dump.AppendLine($"Field Bosses Defeated: {tm.DefeatedFieldBosses.Count}");
        dump.AppendLine($"Run Modifiers: {modStr}");
        dump.AppendLine("============================");

        string text = dump.ToString();
        try
        {
            if (AppHost.App.Clipboard?.TrySetClipboardData(text) == true)
                flash("Stat dump copied to clipboard", ColorSchemes.Success);
            else
                flash("Clipboard unavailable", ColorSchemes.Danger);
        }
        catch
        {
            flash("Clipboard unavailable", ColorSchemes.Danger);
        }
    }
}
