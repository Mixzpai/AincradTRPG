using System.Text.RegularExpressions;
using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Systems;
using SAOTRPG.Systems.Story;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Player Guide — opened with B ("Book"). Left column: TreeView over Recent/Bookmarks panels; right: topic body.
// Keys: ↑↓ select, ←→ collapse/expand, Enter jump via first [bracket], Bksp/Ctrl+O pop stack, 1–5 category, / search, b bookmark, Tab cycle, Esc close.
//
// Key-handling gotcha: ListView/TreeView widgets consume many keys internally
// (digit type-ahead, letter nav, Enter activation). Shortcuts MUST be wired
// on the widget itself, not on dialog.KeyDown — by the time dialog sees the
// event, e.Handled is already true. HandleGuideKey() centralizes the
// dispatch so ?, e, and other overlay hooks can inject cleanly.
public static class PlayerGuideDialog
{
    private const int MinWidth = 96, MinHeight = 32;
    private const int LeftPaneWidth = 44;
    // Hard body-wrap column — man-page convention, width-independent.
    private const int BodyWrapCols = 72;

    // Session-only; persistent mirrors in ProfileData.GuideVisitedTopics/Bookmarks.
    private static readonly HashSet<string> _visitedThisSession = new();
    private static readonly HashSet<(string topic, string block)> _expanded = new();

    // Captured by Show() for stat-dump; null when opened outside an active run.
    private static TurnManager? _activeTm;
    private static Player? _activePlayer;

    // Footer hint segments, joined with " · " so extras append cleanly.
    private static readonly string[] FooterHint =
    {
        "↑↓: select",
        "1-5: category",
        "/: search",
        "b: bookmark",
        "Enter: jump",
        "Bksp: back",
        "Tab: pane",
        "Esc: close",
    };

    private static ColorScheme CategoryColor(string category) => category switch
    {
        "Combat & Rarity"  => ColorSchemes.FromColor(Color.BrightRed),
        "Progression"      => ColorSchemes.FromColor(Color.BrightCyan),
        "World"            => ColorSchemes.FromColor(Color.BrightGreen),
        "Items"            => ColorSchemes.FromColor(Color.BrightYellow),
        "Quests & NPCs"    => ColorSchemes.FromColor(Color.BrightMagenta),
        _                  => ColorSchemes.Gold,
    };

    // ── Node model ──
    // CategoryNode: root+children. TagNode: polyhierarchy root. TopicNode: leaf → GuideEntry by index.
    private abstract record GuideNode;
    private sealed record CategoryNode(string Name, List<TopicNode> Children) : GuideNode;
    private sealed record TagNode(string Tag, List<TopicNode> Children) : GuideNode;
    private sealed record TopicNode(int EntryIndex, PlayerGuideContent.GuideEntry Entry) : GuideNode;

    private static string TopicKey(PlayerGuideContent.GuideEntry e) => $"{e.Category}|{e.Title}";

    // SEE ALSO token regex "[Topic Title]". Non-greedy; titles lack ']'.
    private static readonly Regex BracketRegex = new(@"\[([^\]]+)\]", RegexOptions.Compiled);

    // Both params nullable for main-menu callers (no active run).
    public static void Show(TurnManager? turnManager = null, Player? player = null)
    {
        ProfileData.EnsureLoaded();
        _visitedThisSession.Clear();
        _expanded.Clear();
        _activeTm = turnManager;
        _activePlayer = player;

        int w = Math.Max(MinWidth, (int)(Application.Screen.Width * 0.7));
        int h = Math.Max(MinHeight, (int)(Application.Screen.Height * 0.8));
        var dialog = DialogHelper.Create("Player Guide", w, h);

        var entries = PlayerGuideContent.Entries;

        // ── Build node roots ── Category roots in Entries order, then alphabetized Tag roots.
        // Tagged topics appear under both their category and each Tag root (polyhierarchy).
        var categoryRoots = new List<CategoryNode>();
        var categoryByName = new Dictionary<string, CategoryNode>();
        var topicByKey = new Dictionary<string, TopicNode>(StringComparer.OrdinalIgnoreCase);
        var topicByTitle = new Dictionary<string, TopicNode>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            if (!categoryByName.TryGetValue(e.Category, out var cat))
            {
                cat = new CategoryNode(e.Category, new List<TopicNode>());
                categoryByName[e.Category] = cat;
                categoryRoots.Add(cat);
            }
            var topic = new TopicNode(i, e);
            cat.Children.Add(topic);
            topicByKey[TopicKey(e)] = topic;
            // Title-only map for [Bracket] jumps; first-seen wins on collision.
            topicByTitle.TryAdd(e.Title, topic);
        }

        var tagRoots = new List<TagNode>();
        var topicsByTag = new Dictionary<string, List<TopicNode>>(StringComparer.OrdinalIgnoreCase);
        foreach (var cat in categoryRoots)
        {
            foreach (var topic in cat.Children)
            {
                foreach (var tag in topic.Entry.Tags)
                {
                    if (!topicsByTag.TryGetValue(tag, out var list))
                    {
                        list = new List<TopicNode>();
                        topicsByTag[tag] = list;
                    }
                    list.Add(topic);
                }
            }
        }
        foreach (var kv in topicsByTag.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            tagRoots.Add(new TagNode(kv.Key, kv.Value));

        var allRoots = new List<GuideNode>();
        allRoots.AddRange(categoryRoots);
        allRoots.AddRange(tagRoots);

        // ── Tree widget ──
        var listHeader = new Label
        {
            Text = "Topics", X = 1, Y = 0, Width = LeftPaneWidth,
            ColorScheme = ColorSchemes.Gold,
        };

        // Layout from bottom: 1 close-footer + 1 hint + 2 search + 5 bookmarks + 5 recent; tree fills rest.
        const int RecentRows = 4;
        const int BookmarkRows = 4;
        const int PanelBlockHeight = RecentRows + BookmarkRows + 2;  // headers

        var tree = new TreeView<GuideNode>
        {
            X = 1, Y = 1,
            Width = LeftPaneWidth, Height = Dim.Fill(4 + PanelBlockHeight),
            ColorScheme = ColorSchemes.ListSelection,
        };
        tree.TreeBuilder = new DelegateTreeBuilder<GuideNode>(
            childGetter: n => n switch
            {
                CategoryNode c => c.Children.Cast<GuideNode>(),
                TagNode t => t.Children.Cast<GuideNode>(),
                _ => Enumerable.Empty<GuideNode>(),
            },
            canExpand: n => n is CategoryNode or TagNode);
        tree.MultiSelect = false;
        tree.AllowLetterBasedNavigation = false;  // let 1-5 and / work
        tree.AspectGetter = TreeAspect;
        tree.ColorGetter = TreeColor;
        tree.AddObjects(allRoots);

        // Start with category roots expanded so topics are visible.
        foreach (var root in categoryRoots)
            tree.Expand(root);

        // ── Recent / Bookmarks panels ──
        // Anchored above the 4-row search/hint/footer tail; Pos.AnchorEnd offsets are from dialog bottom.
        var recentLabel = new Label
        {
            Text = "Recent", X = 1, Y = Pos.AnchorEnd(4 + BookmarkRows + RecentRows + 2),
            Width = LeftPaneWidth, ColorScheme = ColorSchemes.Gold,
        };
        var recentList = new ListView
        {
            X = 1, Y = Pos.AnchorEnd(4 + BookmarkRows + RecentRows + 1),
            Width = LeftPaneWidth, Height = RecentRows,
            ColorScheme = ColorSchemes.ListSelection,
        };
        var bookmarkLabel = new Label
        {
            Text = "Bookmarks", X = 1, Y = Pos.AnchorEnd(4 + BookmarkRows + 1),
            Width = LeftPaneWidth, ColorScheme = ColorSchemes.Gold,
        };
        var bookmarkList = new ListView
        {
            X = 1, Y = Pos.AnchorEnd(4 + BookmarkRows),
            Width = LeftPaneWidth, Height = BookmarkRows,
            ColorScheme = ColorSchemes.ListSelection,
        };

        // ── Right pane ────────────────────────────────────────────────
        var bodyHeader = new Label
        {
            Text = "", X = LeftPaneWidth + 2, Y = 0,
            Width = Dim.Fill(2), ColorScheme = ColorSchemes.Gold,
        };
        var bodyRule = new Label
        {
            Text = new string('─', Math.Max(0, w - LeftPaneWidth - 5)),
            X = LeftPaneWidth + 2, Y = 1,
            Width = Dim.Fill(2), ColorScheme = ColorSchemes.Dim,
        };
        var bodyText = new TextView
        {
            X = LeftPaneWidth + 2, Y = 2,
            Width = Dim.Fill(2), Height = Dim.Fill(4),
            ColorScheme = ColorSchemes.Body,
            // Pre-wrapped via WrapTo(BodyWrapCols); auto-wrap off preserves [bracket] tokens.
            ReadOnly = true, WordWrap = false,
        };

        var searchLabel = new Label
        {
            Text = "/", X = 1, Y = Pos.AnchorEnd(2), Width = 2,
            ColorScheme = ColorSchemes.Gold,
            Visible = false,
        };
        var searchField = new TextField
        {
            X = 3, Y = Pos.AnchorEnd(2), Width = LeftPaneWidth - 2,
            ColorScheme = ColorSchemes.Body,
            Visible = false,
        };

        var hint = new Label
        {
            // "?: help" and "e: export" appended to the footer hints.
            Text = string.Join("   ", FooterHint.Concat(new[] { "?: help", "e: export" })),
            X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1),
            ColorScheme = ColorSchemes.Dim,
        };

        // Transient footer flash for stat-dump copy confirm etc. Shown 3s via Flash().
        var footerFlash = new Label
        {
            Text = "", X = LeftPaneWidth + 2, Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(2),
            ColorScheme = ColorSchemes.Success,
            Visible = false,
        };
        void Flash(string msg, ColorScheme? scheme = null)
        {
            footerFlash.Text = msg;
            footerFlash.ColorScheme = scheme ?? ColorSchemes.Success;
            footerFlash.Visible = true;
            Application.AddTimeout(TimeSpan.FromSeconds(3), () =>
            {
                footerFlash.Visible = false;
                footerFlash.Text = "";
                return false;
            });
        }

        // ── State ─────────────────────────────────────────────────────
        var navStack = new Stack<string>();            // SEE ALSO jump history (topic keys)
        var sessionRecent = new List<string>(ProfileData.GuideRecentlyViewed);
        var bookmarkView = new List<string>(ProfileData.GuideBookmarks);

        void RefreshRecentView()
        {
            // Display newest-first (user-facing), list storage is oldest-first.
            var display = sessionRecent
                .AsEnumerable().Reverse()
                .Select(k => KeyToDisplay(k))
                .ToList();
            recentList.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(
                display.Count == 0 ? new List<string> { "  (none yet)" } : display));
        }
        void RefreshBookmarkView()
        {
            var display = bookmarkView.Select(KeyToDisplay).ToList();
            bookmarkList.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(
                display.Count == 0 ? new List<string> { "  (none)" } : display));
        }
        static string KeyToDisplay(string topicKey)
        {
            var bar = topicKey.IndexOf('|');
            return "  " + (bar < 0 ? topicKey : topicKey[(bar + 1)..]);
        }
        RefreshRecentView();
        RefreshBookmarkView();

        // ── Select / show helpers ─────────────────────────────────────
        void ShowSelected()
        {
            var node = tree.SelectedObject;
            if (node is TopicNode t)
            {
                // Gated monster/boss topics masked until PlayerGuideKnowledge.MarkKnown fires.
                bool known = !IsGatedTitle(t.Entry.Title)
                          || ProfileData.GuideKnownTopics.Contains(t.Entry.Title);
                if (!known)
                {
                    bodyHeader.Text = $"{t.Entry.Category} › ??? (Unknown)";
                    bodyHeader.ColorScheme = ColorSchemes.Dim;
                    bodyText.Text = WrapTo(
                        "This topic is still hidden.\n\n" +
                        "You have not yet encountered the subject of this entry.\n" +
                        "Defeat the relevant monster or boss to unlock its page.",
                        BodyWrapCols);
                    return;
                }
                bodyHeader.Text = $"{t.Entry.Category} › {t.Entry.Title}";
                bodyHeader.ColorScheme = CategoryColor(t.Entry.Category);
                // Render <details:TITLE>…</details> per expansion state, then wrap.
                string rendered = RenderBodyWithDetails(TopicKey(t.Entry), t.Entry.Body);
                bodyText.Text = WrapTo(rendered, BodyWrapCols);
                _visitedThisSession.Add(TopicKey(t.Entry));  // clears "· " unread dot
            }
            else if (node is CategoryNode c)
            {
                bodyHeader.Text = c.Name;
                bodyHeader.ColorScheme = CategoryColor(c.Name);
                bodyText.Text = $"{c.Children.Count} topic(s). Expand with → or Enter.";
            }
            else if (node is TagNode tag)
            {
                bodyHeader.Text = $"Tag: {tag.Tag}";
                bodyHeader.ColorScheme = ColorSchemes.Gold;
                bodyText.Text = $"{tag.Children.Count} topic(s) tagged \"{tag.Tag}\".";
            }
            else
            {
                bodyHeader.Text = "";
                bodyText.Text = "Select a topic.";
            }
        }
        void MarkViewed(TopicNode t)
        {
            var key = TopicKey(t.Entry);
            ProfileData.MarkRecentlyViewed(key);
            // Mirror in session state — ProfileData already mutates its list.
            sessionRecent.Remove(key);
            sessionRecent.Add(key);
            while (sessionRecent.Count > 10) sessionRecent.RemoveAt(0);
            RefreshRecentView();
        }

        tree.SelectionChanged += (s, e) =>
        {
            ShowSelected();
            if (e.NewValue is TopicNode t) MarkViewed(t);
        };

        // ── Navigate to a topic by title (case-insensitive) ───────────
        bool TryNavigateByTitle(string title, bool pushStack)
        {
            if (!topicByTitle.TryGetValue(title.Trim(), out var topic)) return false;
            var oldNode = tree.SelectedObject;
            if (pushStack && oldNode is TopicNode oldTopic)
                navStack.Push(TopicKey(oldTopic.Entry));
            // Expand containing category so topic is visible before select.
            foreach (var cat in categoryRoots)
                if (cat.Children.Contains(topic)) { tree.Expand(cat); break; }
            tree.SelectedObject = topic;
            tree.SetNeedsDraw();
            return true;
        }

        bool TryNavigateByKey(string topicKey, bool pushStack)
        {
            if (!topicByKey.TryGetValue(topicKey, out var topic)) return false;
            var oldNode = tree.SelectedObject;
            if (pushStack && oldNode is TopicNode oldTopic)
                navStack.Push(TopicKey(oldTopic.Entry));
            foreach (var cat in categoryRoots)
                if (cat.Children.Contains(topic)) { tree.Expand(cat); break; }
            tree.SelectedObject = topic;
            tree.SetNeedsDraw();
            return true;
        }

        // ── [Bracket] jump — pick first match from the current body ───
        bool JumpToFirstBracketedReference()
        {
            var body = bodyText.Text?.ToString() ?? "";
            foreach (Match m in BracketRegex.Matches(body))
            {
                var title = m.Groups[1].Value;
                if (TryNavigateByTitle(title, pushStack: true))
                    return true;
            }
            return false;
        }

        // ── Stack pop ─────────────────────────────────────────────────
        bool PopNavStack()
        {
            while (navStack.Count > 0)
            {
                var key = navStack.Pop();
                if (TryNavigateByKey(key, pushStack: false)) return true;
            }
            return false;
        }

        // ── Fuzzy filter ── Subseq match: +3·streak/run, −1/gap, +5 prefix. Null = no match.
        // Title weighted 3x vs body; sort by max of the two.
        static int? FuzzyScore(string text, string query)
        {
            if (string.IsNullOrEmpty(query)) return 0;
            if (string.IsNullOrEmpty(text)) return null;
            int ti = 0, qi = 0, score = 0, streak = 0, firstMatch = -1;
            string tl = text.ToLowerInvariant();
            string ql = query.ToLowerInvariant();
            while (ti < tl.Length && qi < ql.Length)
            {
                if (tl[ti] == ql[qi])
                {
                    if (firstMatch < 0) firstMatch = ti;
                    streak++;
                    score += 3 * streak;  // streak bonus ramps quickly
                    qi++;
                }
                else
                {
                    if (streak > 0) score -= 1;  // penalize breaking a streak
                    streak = 0;
                }
                ti++;
            }
            if (qi < ql.Length) return null;  // didn't match all query chars
            if (firstMatch == 0) score += 5;  // prefix bonus
            score -= firstMatch;              // leading-gap penalty
            return score;
        }

        // TreeView<T>.Filter hook: Category/Tag roots visible if any child matches;
        // topics match on non-null fuzzy score (title primary, body fallback).
        var filter = new GuideFilter(FuzzyScore);
        tree.Filter = filter;

        void ApplyFilter(string? query)
        {
            filter.Query = query ?? "";
            if (!string.IsNullOrWhiteSpace(query))
            {
                // Score topics; snap selection to top scorer (can't reorder tree itself).
                // Defensive reselect below covers TreeView empty-objects crash.
                var scored = new List<(TopicNode topic, int score)>();
                foreach (var cat in categoryRoots)
                    foreach (var topic in cat.Children)
                    {
                        var s = filter.BestScore(topic);
                        if (s.HasValue) scored.Add((topic, s.Value));
                    }
                scored.Sort((a, b) => b.score.CompareTo(a.score));
                if (scored.Count > 0)
                {
                    // Expand its category so the row is visible.
                    foreach (var cat in categoryRoots)
                        if (cat.Children.Contains(scored[0].topic)) { tree.Expand(cat); break; }
                    tree.SelectedObject = scored[0].topic;
                }
            }
            tree.RebuildTree();
            // Defensive reselect if filter cleared selection.
            if (tree.SelectedObject == null && categoryRoots.Count > 0)
                tree.SelectedObject = categoryRoots[0];
            tree.SetNeedsDraw();
            ShowSelected();
        }

        void StartSearch()
        {
            searchLabel.Visible = true;
            searchField.Visible = true;
            searchField.Text = "";
            searchField.SetFocus();
        }
        void EndSearch(bool keepResults)
        {
            searchField.Visible = false;
            searchLabel.Visible = false;
            if (!keepResults) ApplyFilter("");
            tree.SetFocus();
        }
        searchField.TextChanged += (s, e) => ApplyFilter(searchField.Text ?? "");
        searchField.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Esc) { EndSearch(keepResults: false); e.Handled = true; }
            else if (e.KeyCode == KeyCode.Enter) { EndSearch(keepResults: true); e.Handled = true; }
        };

        // ── Category jump ──
        string[] categoryOrder = { "Combat & Rarity", "Progression", "World", "Items", "Quests & NPCs" };
        void JumpToCategory(int n)
        {
            if (n < 1 || n > categoryOrder.Length) return;
            string cat = categoryOrder[n - 1];
            if (!categoryByName.TryGetValue(cat, out var node)) return;
            if (!string.IsNullOrWhiteSpace(filter.Query)) ApplyFilter("");  // clear filter
            tree.Expand(node);
            tree.SelectedObject = node.Children.Count > 0 ? node.Children[0] : (GuideNode)node;
        }

        // ── Bookmark toggle ───────────────────────────────────────────
        void ToggleBookmarkForSelection()
        {
            if (tree.SelectedObject is not TopicNode t) return;
            var key = TopicKey(t.Entry);
            ProfileData.ToggleBookmark(key);
            bookmarkView.Clear();
            bookmarkView.AddRange(ProfileData.GuideBookmarks);
            RefreshBookmarkView();
            tree.SetNeedsDraw();  // ColorGetter may depend on bookmark state
        }

        // ── Focus cycle: Tree → Recent → Bookmarks → Body → Tree ─────
        void CycleFocusForward(View current)
        {
            if (current == tree) recentList.SetFocus();
            else if (current == recentList) bookmarkList.SetFocus();
            else if (current == bookmarkList) bodyText.SetFocus();
            else if (current == bodyText) tree.SetFocus();
            else tree.SetFocus();
        }

        // ── Central key dispatch ──
        // Returns true if handled. Overlay hooks (?, e, stat-dump) inject here.
        bool HandleGuideKey(KeyCode key, System.Text.Rune rune)
        {
            switch (key)
            {
                case KeyCode.D1: JumpToCategory(1); return true;
                case KeyCode.D2: JumpToCategory(2); return true;
                case KeyCode.D3: JumpToCategory(3); return true;
                case KeyCode.D4: JumpToCategory(4); return true;
                case KeyCode.D5: JumpToCategory(5); return true;
            }
            if (rune.Value == '/') { StartSearch(); return true; }

            // ? overlay
            if (rune.Value == '?') { ShowKeybindOverlay(); return true; }
            // stat dump
            if (rune.Value == 'e' || rune.Value == 'E')
            {
                ShowStatDump(Flash);
                return true;
            }
            return false;
        }

        // ── Wire tree KeyDown ─────────────────────────────────────────
        tree.KeyDown += (s, e) =>
        {
            if (HandleGuideKey(e.KeyCode, e.AsRune)) { e.Handled = true; return; }

            switch (e.KeyCode)
            {
                case KeyCode.Tab:
                    CycleFocusForward(tree);
                    e.Handled = true; return;
                case KeyCode.Backspace:
                    if (PopNavStack()) { e.Handled = true; return; }
                    break;
                case KeyCode.O | KeyCode.CtrlMask:
                    if (PopNavStack()) { e.Handled = true; return; }
                    break;
                case KeyCode.Enter:
                    // Topic: try bracket jump. Category: let tree handle expand/collapse.
                    if (tree.SelectedObject is TopicNode &&
                        JumpToFirstBracketedReference())
                    { e.Handled = true; return; }
                    break;
            }
            if (e.AsRune.Value == 'b')
            {
                ToggleBookmarkForSelection();
                e.Handled = true;
            }
        };

        // ── Wire body KeyDown ─────────────────────────────────────────
        bodyText.KeyDown += (s, e) =>
        {
            if (HandleGuideKey(e.KeyCode, e.AsRune)) { e.Handled = true; return; }
            switch (e.KeyCode)
            {
                case KeyCode.Tab:
                    CycleFocusForward(bodyText);
                    e.Handled = true; return;
                case KeyCode.Backspace:
                    if (PopNavStack()) { e.Handled = true; return; }
                    break;
                case KeyCode.O | KeyCode.CtrlMask:
                    if (PopNavStack()) { e.Handled = true; return; }
                    break;
                case KeyCode.Enter:
                    // [Topic] xref on cursor line wins; else ▸/▾ toggles progressive-disclosure block.
                    if (tree.SelectedObject is TopicNode currentTopic)
                    {
                        string cursorLine = GetCursorLine(bodyText);
                        bool lineHasXref = cursorLine.Contains('[') && cursorLine.Contains(']');
                        if (!lineHasXref)
                        {
                            string key = TopicKey(currentTopic.Entry);
                            if (ToggleDetailsOnLine(key, cursorLine)
                                || ToggleFirstCollapsed(key, currentTopic.Entry.Body))
                            {
                                ShowSelected();
                                e.Handled = true;
                                return;
                            }
                        }
                    }
                    if (JumpToFirstBracketedReference()) { e.Handled = true; return; }
                    break;
            }
        };

        // ── Wire Recent / Bookmark panel KeyDown ──────────────────────
        void HookPanel(ListView panel, Func<int, string?> resolveKey)
        {
            panel.KeyDown += (s, e) =>
            {
                if (HandleGuideKey(e.KeyCode, e.AsRune)) { e.Handled = true; return; }
                switch (e.KeyCode)
                {
                    case KeyCode.Tab:
                        CycleFocusForward(panel);
                        e.Handled = true; return;
                    case KeyCode.Enter:
                        var key = resolveKey(panel.SelectedItem);
                        if (key != null && TryNavigateByKey(key, pushStack: true))
                        {
                            tree.SetFocus();
                            e.Handled = true;
                            return;
                        }
                        break;
                }
            };
        }
        HookPanel(recentList, idx =>
        {
            // sessionRecent oldest-first; display newest-first.
            if (idx < 0 || idx >= sessionRecent.Count) return null;
            return sessionRecent[sessionRecent.Count - 1 - idx];
        });
        HookPanel(bookmarkList, idx =>
        {
            if (idx < 0 || idx >= bookmarkView.Count) return null;
            return bookmarkView[idx];
        });

        // ── Assemble dialog ───────────────────────────────────────────
        dialog.Add(listHeader, tree,
                   recentLabel, recentList, bookmarkLabel, bookmarkList,
                   bodyHeader, bodyRule, bodyText,
                   searchLabel, searchField, footerFlash, hint);
        DialogHelper.AddCloseFooter(dialog);

        // Initial selection — first topic of first category.
        if (categoryRoots.Count > 0 && categoryRoots[0].Children.Count > 0)
            tree.SelectedObject = categoryRoots[0].Children[0];
        else if (categoryRoots.Count > 0)
            tree.SelectedObject = categoryRoots[0];
        ShowSelected();

        tree.SetFocus();
        DialogHelper.RunModal(dialog);
    }

    // ── Tree row rendering ──
    // "· " prefix = unread, "??? (Unknown)" = gated monster/boss, trailing "*" = bookmarked.
    private static string TreeAspect(object? node)
    {
        switch (node)
        {
            case CategoryNode c:
                return c.Name;
            case TagNode t:
                return $"Tag: {t.Tag}";
            case TopicNode t:
            {
                var e = t.Entry;
                string key = TopicKey(e);
                bool known = !IsGatedTitle(e.Title) || ProfileData.GuideKnownTopics.Contains(e.Title);
                string display = known ? e.Title : "??? (Unknown)";
                bool visited = ProfileData.GuideVisitedTopics.Contains(key)
                            || _visitedThisSession.Contains(key);
                string prefix = visited ? "" : "· ";
                string suffix = ProfileData.GuideBookmarks.Contains(key) ? " *" : "";
                return prefix + display + suffix;
            }
            default:
                return node?.ToString() ?? "";
        }
    }

    // Dim unread/gated; bookmarks Gold; categories/tags keep baseline tint.
    private static ColorScheme? TreeColor(object? node)
    {
        switch (node)
        {
            case CategoryNode c:
                return CategoryColor(c.Name);
            case TagNode:
                return ColorSchemes.Gold;
            case TopicNode t:
            {
                var e = t.Entry;
                string key = TopicKey(e);
                if (ProfileData.GuideBookmarks.Contains(key)) return ColorSchemes.Gold;
                if (IsGatedTitle(e.Title) && !ProfileData.GuideKnownTopics.Contains(e.Title))
                    return ColorSchemes.Dim;
                // Leading "· " prefix is the primary unread signal — no extra tint.
                return null;
            }
            default:
                return null;
        }
    }

    // ── ??? gating helper ──
    // Prefixes mirror PlayerGuideKnowledge.MarkKnown conventions.
    private static bool IsGatedTitle(string title) =>
        title.StartsWith("Monster: ") || title.StartsWith("Boss: ") || title.StartsWith("Field Boss: ");

    // ── Progressive disclosure ── <details:TITLE>…</details>; collapsed "▸ TITLE (Enter)", expanded "▾ TITLE" + inner.
    // Session state in _expanded, keyed by (topicKey, block title).
    private static string RenderBodyWithDetails(string topicKey, string body)
    {
        if (string.IsNullOrEmpty(body)) return body;
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
            bool expanded = _expanded.Contains((topicKey, title));
            if (expanded)
                sb.Append("▾ ").Append(title).Append('\n').Append(inner.TrimEnd('\n'));
            else
                sb.Append("▸ ").Append(title).Append(" (Enter to expand)");
            i = close + "</details>".Length;
        }
        return sb.ToString();
    }

    // Returns the line of text under the TextView cursor, or "" on error.
    private static string GetCursorLine(TextView tv)
    {
        try
        {
            string? all = tv.Text?.ToString();
            if (string.IsNullOrEmpty(all)) return "";
            int row = tv.CursorPosition.Y;
            var lines = all.Split('\n');
            if (row >= 0 && row < lines.Length) return lines[row];
        }
        catch { /* Terminal.Gui internal quirk → empty */ }
        return "";
    }

    private static bool ToggleDetailsOnLine(string topicKey, string line)
    {
        string trimmed = line.TrimStart();
        if (trimmed.StartsWith("▸ ") || trimmed.StartsWith("▾ "))
        {
            string rest = trimmed.Substring(2);
            int cut = rest.IndexOf(" (Enter to expand)", StringComparison.Ordinal);
            string title = (cut >= 0 ? rest.Substring(0, cut) : rest).Trim();
            if (title.Length == 0) return false;
            var key = (topicKey, title);
            if (!_expanded.Add(key)) _expanded.Remove(key);
            return true;
        }
        return false;
    }

    // Fallback (cursor not on ▸/▾ line): toggle first collapsed block.
    private static bool ToggleFirstCollapsed(string topicKey, string body)
    {
        if (string.IsNullOrEmpty(body)) return false;
        int i = 0;
        while (i < body.Length)
        {
            int open = body.IndexOf("<details:", i, StringComparison.Ordinal);
            if (open < 0) return false;
            int titleEnd = body.IndexOf('>', open);
            if (titleEnd < 0) return false;
            string title = body.Substring(open + "<details:".Length, titleEnd - (open + "<details:".Length));
            if (!_expanded.Contains((topicKey, title)))
            {
                _expanded.Add((topicKey, title));
                return true;
            }
            int close = body.IndexOf("</details>", titleEnd, StringComparison.Ordinal);
            if (close < 0) return false;
            i = close + "</details>".Length;
        }
        return false;
    }

    // ── 72-col paragraph-preserving wrap ── Blank-line-separated paragraphs wrap independently; leading whitespace repeats on continuation.
    // [bracketed-links] are atomic — never broken across lines.
    private static string WrapTo(string text, int cols)
    {
        if (string.IsNullOrEmpty(text)) return text;
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

    // Whitespace tokenizer that keeps "[bracketed]" tokens atomic.
    private static List<string> TokenizeAtomic(string s)
    {
        var result = new List<string>();
        int i = 0;
        while (i < s.Length)
        {
            if (s[i] == ' ' || s[i] == '\t') { i++; continue; }
            if (s[i] == '[')
            {
                int close = s.IndexOf(']', i);
                if (close >= 0)
                {
                    // Absorb trailing punctuation to preserve sentence flow.
                    int end = close + 1;
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

    // ── ? keybindings overlay ── Modal 60x20; any key dismisses.
    private static void ShowKeybindOverlay()
    {
        const int W = 60, H = 20;
        var overlay = DialogHelper.Create("Player Guide — Keybindings", W, H);

        string[] lines = {
            "NAVIGATION",
            "  Up Down     select topic",
            "  Tab         cycle focus (tree / recent / bookmarks / body)",
            "  1 2 3 4 5   jump to category",
            "  Enter       expand detail / follow cross-ref",
            "  Backspace   history back (Ctrl+O also)",
            "",
            "SEARCH",
            "  /           start live filter",
            "  Esc         cancel search",
            "",
            "ACTIONS",
            "  b           toggle bookmark on selected topic",
            "  e           export run summary to clipboard",
            "  ?           show this help",
            "",
            "EXIT",
            "  Esc         close the guide",
        };

        int y = 1;
        foreach (var s in lines)
        {
            bool isHeader = s.Length > 0 && s[0] >= 'A' && s[0] <= 'Z' && !s.StartsWith("  ");
            var lbl = new Label
            {
                Text = s, X = 2, Y = y, Width = Dim.Fill(2),
                ColorScheme = isHeader ? ColorSchemes.Gold : ColorSchemes.Body,
            };
            overlay.Add(lbl);
            y++;
        }

        var hintLabel = new Label
        {
            Text = "Press any key to dismiss",
            X = Pos.Center(), Y = Pos.AnchorEnd(1),
            ColorScheme = ColorSchemes.Dim,
        };
        overlay.Add(hintLabel);

        overlay.KeyDown += (s, e) =>
        {
            Application.RequestStop();
            e.Handled = true;
        };

        Application.Run(overlay);
        overlay.Dispose();
    }

    // ── Run-summary clipboard dump ──
    // Pulls from TurnManager+Player captured by Show(); flashes "No active run" when null.
    private static void ShowStatDump(Action<string, ColorScheme?> flash)
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

        // Active unique skills (display-name list).
        var skills = SAOTRPG.Systems.Skills.UniqueSkillSystem.Unlocked
            .Select(sk => SAOTRPG.Systems.Skills.UniqueSkillSystem.Definitions[sk].Name)
            .ToList();
        string skillList = skills.Count == 0 ? "None" : string.Join(", ", skills);

        // Divine objects from inventory + equipped slots; "N/7" matches lore's 7-object goal.
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
            ? "0/7"
            : $"{Math.Min(divineNames.Count, 7)}/7 ({string.Join(", ", divineNames)})";

        // Bookmarks — strip "Category|" prefix for readability.
        var bookmarkTitles = ProfileData.GuideBookmarks
            .Select(k => { int bar = k.IndexOf('|'); return bar < 0 ? k : k.Substring(bar + 1); })
            .ToList();
        string bookmarks = bookmarkTitles.Count == 0 ? "None" : string.Join(", ", bookmarkTitles);

        var mods = RunModifiers.Active
            .Select(m => RunModifiers.Definitions[m].Name)
            .ToList();
        string modStr = mods.Count == 0 ? "None" : string.Join(", ", mods);

        // SP field in the scout schema = SkillPoints (allocation budget).
        var dump = new System.Text.StringBuilder();
        dump.AppendLine("AINCRAD TRPG — RUN SUMMARY");
        dump.AppendLine("============================");
        dump.AppendLine($"Character: {name} · Level {p.Level} · Floor {tm.CurrentFloor}");
        dump.AppendLine($"HP: {p.CurrentHealth}/{p.MaxHealth} · SP: {p.SkillPoints}");
        dump.AppendLine($"Kills: {tm.KillCount} · Turns: {tm.TurnCount} · Col: {p.ColOnHand}");
        dump.AppendLine($"Active Unique Skills: {skillList}");
        dump.AppendLine($"Divine Objects Found: {divineStr}");
        dump.AppendLine($"Field Bosses Defeated: {tm.DefeatedFieldBosses.Count}");
        dump.AppendLine($"Bookmarks: {bookmarks}");
        dump.AppendLine($"Run Modifiers: {modStr}");
        dump.AppendLine("============================");

        string text = dump.ToString();

        try
        {
            if (Clipboard.TrySetClipboardData(text))
                flash("Stat dump copied to clipboard", ColorSchemes.Success);
            else
                flash("Clipboard unavailable", ColorSchemes.Danger);
        }
        catch
        {
            flash("Clipboard unavailable", ColorSchemes.Danger);
        }
    }

    // ── Filter impl ── Roots match if any child matches; topics match on non-null score.
    // Title full-weight, body half-weight as fallback for body-only hits.
    private sealed class GuideFilter : ITreeViewFilter<GuideNode>
    {
        private readonly Func<string, string, int?> _scoreFn;
        public string Query { get; set; } = "";

        public GuideFilter(Func<string, string, int?> scoreFn) { _scoreFn = scoreFn; }

        public int? BestScore(TopicNode topic)
        {
            if (string.IsNullOrWhiteSpace(Query)) return 0;
            int? titleScore = _scoreFn(topic.Entry.Title, Query);
            int? bodyScore = _scoreFn(topic.Entry.Body, Query);
            int? t = titleScore.HasValue ? titleScore.Value * 3 : (int?)null;  // 3x title boost
            int? b = bodyScore.HasValue ? bodyScore.Value / 2 : (int?)null;
            if (!t.HasValue && !b.HasValue) return null;
            if (!t.HasValue) return b;
            if (!b.HasValue) return t;
            return Math.Max(t.Value, b.Value);
        }

        public bool IsMatch(GuideNode model)
        {
            if (string.IsNullOrWhiteSpace(Query)) return true;
            return model switch
            {
                TopicNode t => BestScore(t).HasValue,
                CategoryNode c => c.Children.Any(ch => BestScore(ch).HasValue),
                TagNode tag => tag.Children.Any(ch => BestScore(ch).HasValue),
                _ => true,
            };
        }
    }
}
