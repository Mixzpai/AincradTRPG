using Terminal.Gui;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Player Guide — opened with the B key (mnemonic: "Book").
// Left-pane topic list (grouped by category) + right-pane body text.
//
// Navigation:
//   ↑/↓         move topic selection (headers auto-skip)
//   1–5         jump to category N
//   /           start in-page search (filters topic list live)
//   Tab         toggle focus between topic list and body
//   Esc         close the guide (or cancel an active search)
//
// Key handling note: ListView consumes digit keys internally for row-jump
// type-ahead, so 1-5 and Tab MUST be wired on the topicList.KeyDown event
// directly — not on dialog.KeyDown (which fires after the child handles it).
public static class PlayerGuideDialog
{
    private const int MinWidth = 96, MinHeight = 32;
    private const int LeftPaneWidth = 44;  // 41-char max title + 2 indent + 1 margin

    private static ColorScheme CategoryColor(string category) => category switch
    {
        "Combat & Rarity"  => ColorSchemes.FromColor(Color.BrightRed),
        "Progression"      => ColorSchemes.FromColor(Color.BrightCyan),
        "World"            => ColorSchemes.FromColor(Color.BrightGreen),
        "Items"            => ColorSchemes.FromColor(Color.BrightYellow),
        "Quests & NPCs"    => ColorSchemes.FromColor(Color.BrightMagenta),
        _                  => ColorSchemes.Gold,
    };

    // Fit a topic title into the left-pane row width with a trailing ellipsis
    // if it would clip. Belt-and-braces safety for any future long titles.
    private static string FitRow(string title)
    {
        int budget = LeftPaneWidth - 2;  // two-space indent
        if (title.Length <= budget) return "  " + title;
        return "  " + title.Substring(0, Math.Max(1, budget - 1)) + "…";
    }

    public static void Show()
    {
        int w = Math.Max(MinWidth, (int)(Application.Screen.Width * 0.7));
        int h = Math.Max(MinHeight, (int)(Application.Screen.Height * 0.8));
        var dialog = DialogHelper.Create("Player Guide", w, h);

        var entries = PlayerGuideContent.Entries;

        // Build the full list. Category headers render as "── Name ──" bars;
        // topic rows are "  Title" (ellipsis-truncated if too long).
        var fullLines = new List<string>();
        var fullTopicIdx = new List<int>();  // -1 = header row
        var categoryHeaderIdx = new Dictionary<string, int>();
        string? currentCat = null;
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].Category != currentCat)
            {
                currentCat = entries[i].Category;
                categoryHeaderIdx[currentCat] = fullLines.Count;
                fullLines.Add($"── {currentCat} ──");
                fullTopicIdx.Add(-1);
            }
            fullLines.Add(FitRow(entries[i].Title));
            fullTopicIdx.Add(i);
        }

        var listLines = new List<string>(fullLines);
        var topicIdx = new List<int>(fullTopicIdx);

        // ── Left pane: topics ──────────────────────────────────────────
        var listHeader = new Label
        {
            Text = "Topics", X = 1, Y = 0, Width = LeftPaneWidth,
            ColorScheme = ColorSchemes.Gold,
        };
        var topicList = new ListView
        {
            X = 1, Y = 1,
            Width = LeftPaneWidth, Height = Dim.Fill(4),
            // ListSelection: gray rows, black-on-BrightYellow selected row.
            // Without this the selection is invisible against the body scheme.
            ColorScheme = ColorSchemes.ListSelection,
        };
        topicList.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(listLines));

        // ── Right pane: body ───────────────────────────────────────────
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
            ReadOnly = true, WordWrap = true,
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
            Text = "↑↓: select   1-5: category   /: search   Tab: switch pane   Esc: close",
            X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1),
            ColorScheme = ColorSchemes.Dim,
        };

        // ── Display logic ─────────────────────────────────────────────
        void ShowSelected()
        {
            int idx = topicList.SelectedItem;
            if (idx < 0 || idx >= topicIdx.Count)
            {
                bodyHeader.Text = "";
                bodyText.Text = listLines.Count == 0
                    ? "No topics match your search. Press Esc to clear."
                    : "Select a topic.";
                return;
            }
            int ei = topicIdx[idx];
            if (ei < 0)
            {
                for (int j = idx + 1; j < topicIdx.Count; j++)
                    if (topicIdx[j] >= 0) { topicList.SelectedItem = j; return; }
                bodyHeader.Text = "";
                bodyText.Text = "Select a topic.";
                return;
            }
            var e = entries[ei];
            bodyHeader.Text = $"{e.Category} › {e.Title}";
            bodyHeader.ColorScheme = CategoryColor(e.Category);
            bodyText.Text = e.Body;
        }
        topicList.SelectedItemChanged += (s, e) => ShowSelected();

        // ── Filter logic ──────────────────────────────────────────────
        void ApplyFilter(string? query)
        {
            listLines.Clear();
            topicIdx.Clear();
            if (string.IsNullOrWhiteSpace(query))
            {
                listLines.AddRange(fullLines);
                topicIdx.AddRange(fullTopicIdx);
            }
            else
            {
                string q = query.Trim().ToLowerInvariant();
                string? lastCat = null;
                for (int i = 0; i < entries.Length; i++)
                {
                    var e = entries[i];
                    bool titleHit = e.Title.ToLowerInvariant().Contains(q);
                    bool bodyHit  = e.Body .ToLowerInvariant().Contains(q);
                    if (!titleHit && !bodyHit) continue;
                    if (e.Category != lastCat)
                    {
                        lastCat = e.Category;
                        listLines.Add($"── {e.Category} ──");
                        topicIdx.Add(-1);
                    }
                    listLines.Add(FitRow(e.Title));
                    topicIdx.Add(i);
                }
            }
            topicList.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(listLines));
            int first = FindFirstTopic(topicIdx);
            if (first >= 0 && first < listLines.Count) topicList.SelectedItem = first;
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
            topicList.SetFocus();
        }

        searchField.TextChanged += (s, e) => ApplyFilter(searchField.Text ?? "");
        searchField.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Esc) { EndSearch(keepResults: false); e.Handled = true; }
            else if (e.KeyCode == KeyCode.Enter) { EndSearch(keepResults: true); e.Handled = true; }
        };

        // ── Category jump ──────────────────────────────────────────────
        string[] categoryOrder = { "Combat & Rarity", "Progression", "World", "Items", "Quests & NPCs" };
        void JumpToCategory(int n)
        {
            if (n < 1 || n > categoryOrder.Length) return;
            string cat = categoryOrder[n - 1];
            if (!categoryHeaderIdx.TryGetValue(cat, out int headerRow)) return;
            if (listLines.Count != fullLines.Count) ApplyFilter("");
            int topicRow = headerRow + 1;
            if (topicRow < topicIdx.Count && topicIdx[topicRow] >= 0)
                topicList.SelectedItem = topicRow;
        }

        // ── Shortcuts MUST be on topicList, NOT dialog ────────────────
        // ListView consumes digits for type-ahead, so by the time an unhandled
        // event bubbles to dialog, it's already marked handled. Attaching here
        // lets us intercept before the ListView internal handler runs.
        topicList.KeyDown += (s, e) =>
        {
            switch (e.KeyCode)
            {
                case KeyCode.D1: JumpToCategory(1); e.Handled = true; return;
                case KeyCode.D2: JumpToCategory(2); e.Handled = true; return;
                case KeyCode.D3: JumpToCategory(3); e.Handled = true; return;
                case KeyCode.D4: JumpToCategory(4); e.Handled = true; return;
                case KeyCode.D5: JumpToCategory(5); e.Handled = true; return;
                case KeyCode.Tab:
                    bodyText.SetFocus(); e.Handled = true; return;
            }
            if (e.AsRune.Value == '/') { StartSearch(); e.Handled = true; }
        };

        // Tab from body returns focus to list; / also works from body.
        bodyText.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Tab) { topicList.SetFocus(); e.Handled = true; }
            else if (e.AsRune.Value == '/') { StartSearch(); e.Handled = true; }
        };

        dialog.Add(listHeader, topicList, bodyHeader, bodyRule, bodyText,
                   searchLabel, searchField, hint);
        DialogHelper.AddCloseFooter(dialog);

        int firstTopic = FindFirstTopic(topicIdx);
        if (firstTopic >= 0 && firstTopic < listLines.Count)
            topicList.SelectedItem = firstTopic;
        ShowSelected();

        topicList.SetFocus();
        DialogHelper.RunModal(dialog);
    }

    private static int FindFirstTopic(List<int> topicIdx)
    {
        for (int i = 0; i < topicIdx.Count; i++)
            if (topicIdx[i] >= 0) return i;
        return 0;
    }
}
