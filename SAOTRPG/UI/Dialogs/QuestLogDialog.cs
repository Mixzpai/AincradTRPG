using System.Collections.ObjectModel;
using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Quest Journal — full-screen with active/completed tabs, progress, detail panel, reward preview.
// Layout: left-list / right-detail split with bracket-header sections.
public static class QuestLogDialog
{
    private const int DialogWidth = 76, DialogHeight = 26;

    public static void Show(Player player)
    {
        var dialog = DialogHelper.Create("Quest Journal", DialogWidth, DialogHeight);

        bool showCompleted = false;

        // ── Tab buttons ──────────────────────────────────────────────
        var activeTab = new Button { Text = " Active ", X = 2, Y = 0, NoPadding = true, SchemeName = ColorSchemes.GoldName };
        var completedTab = new Button { Text = " Completed ", X = 14, Y = 0, NoPadding = true, SchemeName = ColorSchemes.DimName };
        var countLabel = new Label
        {
            Text = $"{QuestSystem.ActiveQuests.Count}/{QuestSystem.MaxActiveQuests} active",
            X = Pos.AnchorEnd(18), Y = 0, Width = 16, SchemeName = ColorSchemes.DimName,
        };

        // ── Quest list (left panel) ──────────────────────────────────
        var names = new ObservableCollection<string>();

        var listView = new ListView
        {
            X = 1, Y = 2, Width = 38, Height = Dim.Fill(4),
            Source = new ListWrapper<string>(names),
            CanFocus = true,
        };

        // ── Detail panel (right side) ────────────────────────────────
        var detailHeader = new Label
        {
            Text = "[ Details ]", X = 41, Y = 2,
            Width = Dim.Fill(2), SchemeName = ColorSchemes.GoldName,
        };
        var detailTitle = new Label
        {
            Text = "", X = 41, Y = 4,
            Width = Dim.Fill(2), Height = 1, SchemeName = ColorSchemes.TitleName,
        };
        // Word-wrapped over Height=6 so long HF quest descriptions don't clip
        // mid-sentence. CanFocus=false so Tab cycling stays on the listView.
        var detailDesc = new Label
        {
            Text = "Select a quest to view details.", X = 41, Y = 6,
            Width = Dim.Fill(2), Height = 6, SchemeName = ColorSchemes.BodyName,
            CanFocus = false,
        };
        detailDesc.TextFormatter.WordWrap = true;
        var detailProgress = new Label
        {
            Text = "", X = 41, Y = 11,
            Width = Dim.Fill(2), Height = 1, SchemeName = ColorSchemes.GoldName,
        };
        var detailReward = new Label
        {
            Text = "", X = 41, Y = 13,
            Width = Dim.Fill(2), Height = 1, SchemeName = ColorSchemes.DimName,
        };
        var detailGiver = new Label
        {
            Text = "", X = 41, Y = 14,
            Width = Dim.Fill(2), Height = 1, SchemeName = ColorSchemes.DimName,
        };
        var detailStatus = new Label
        {
            Text = "", X = 41, Y = 16,
            Width = Dim.Fill(2), Height = 1, SchemeName = ColorSchemes.BodyName,
        };

        // Terminal.Gui Label with Width=1 only renders the FIRST char of Text. Build
        // a multi-line newline-joined string so each row gets its own '│' glyph.
        var separator = new Label
        {
            Text = string.Join("\n", Enumerable.Repeat("│", 20)),
            X = 40, Y = 2,
            Width = 1, Height = Dim.Fill(4), SchemeName = ColorSchemes.DimName,
        };

        var hint = new Label
        {
            Text = "Tab: switch | P: pin/unpin tracker | Talk to NPCs for quests | Esc: close",
            X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1), SchemeName = ColorSchemes.DimName,
        };

        // ── Refresh logic ────────────────────────────────────────────
        void RefreshList()
        {
            names.Clear();
            var source = showCompleted ? QuestSystem.CompletedQuests : QuestSystem.ActiveQuests;
            if (source.Count == 0)
            {
                names.Add(showCompleted
                    ? "  No completed quests yet."
                    : "  No active quests. Talk to NPCs!");
                return;
            }
            foreach (var q in source)
            {
                string status = q.Status switch
                {
                    QuestStatus.Complete => "[DONE]",
                    QuestStatus.TurnedIn => "[OK]  ",
                    _ => "      ",
                };
                string typeTag = q.Type switch
                {
                    QuestType.Kill => "KILL",
                    QuestType.Collect => "COLL",
                    QuestType.Explore => "EXPL",
                    QuestType.Deliver => "DLVR",
                    _ => "    ",
                };
                // Pin marker prefixes tracked quest so players can see at a glance.
                string pinTag = (!showCompleted && QuestSystem.PinnedQuestId == q.Id) ? "* " : "  ";
                names.Add($"{pinTag}{status} {typeTag} {TextHelpers.Truncate(q.Title, 20)}");
            }
            listView.Source = new ListWrapper<string>(names);
            countLabel.Text = showCompleted
                ? $"{QuestSystem.CompletedQuests.Count} done"
                : $"{QuestSystem.ActiveQuests.Count}/{QuestSystem.MaxActiveQuests} active";
        }

        void ShowDetail(int idx)
        {
            var source = showCompleted ? QuestSystem.CompletedQuests : QuestSystem.ActiveQuests;
            if (idx < 0 || idx >= source.Count)
            {
                detailTitle.Text = "";
                detailDesc.Text = "Select a quest to view details.";
                detailProgress.Text = "";
                detailReward.Text = "";
                detailGiver.Text = "";
                detailStatus.Text = "";
                return;
            }
            var q = source[idx];
            detailTitle.Text = q.Title;
            detailDesc.Text = q.Description;
            detailProgress.Text = q.ProgressText;
            detailReward.Text = $"Reward: {q.RewardCol} Col, {q.RewardXp} XP";
            detailGiver.Text = $"From: {q.GiverName} (Floor {q.Floor})";
            detailStatus.Text = q.Status switch
            {
                QuestStatus.Active => "-- IN PROGRESS --",
                QuestStatus.Complete => "-- READY TO TURN IN --",
                QuestStatus.TurnedIn => "-- COMPLETED --",
                _ => "",
            };
            detailStatus.SchemeName = q.Status == QuestStatus.Complete
                ? ColorSchemes.GoldName : ColorSchemes.DimName;
        }

        RefreshList();

        // ── Event wiring ─────────────────────────────────────────────
        listView.ValueChanged += (s, e) => ShowDetail(listView.SelectedItem ?? -1);

        activeTab.Accepting += (s, e) =>
        {
            e.Handled = true;
            showCompleted = false;
            activeTab.SchemeName = ColorSchemes.GoldName;
            completedTab.SchemeName = ColorSchemes.DimName;
            RefreshList();
            listView.SetFocus();
        };

        completedTab.Accepting += (s, e) =>
        {
            e.Handled = true;
            showCompleted = true;
            activeTab.SchemeName = ColorSchemes.DimName;
            completedTab.SchemeName = ColorSchemes.GoldName;
            RefreshList();
            listView.SetFocus();
        };

        // Tab key switches between active/completed; P toggles tracker pin.
        listView.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Tab)
            {
                showCompleted = !showCompleted;
                activeTab.SchemeName = showCompleted ? ColorSchemes.DimName : ColorSchemes.GoldName;
                completedTab.SchemeName = showCompleted ? ColorSchemes.GoldName : ColorSchemes.DimName;
                RefreshList();
                e.Handled = true;
            }
            else if ((e.KeyCode & ~KeyCode.ShiftMask) == KeyCode.P && !showCompleted)
            {
                var src = QuestSystem.ActiveQuests;
                int idx = listView.SelectedItem ?? -1;
                if (idx >= 0 && idx < src.Count)
                {
                    QuestSystem.TogglePin(src[idx].Id);
                    RefreshList();
                    listView.SelectedItem = idx;
                }
                e.Handled = true;
            }
        };

        dialog.Add(activeTab, completedTab, countLabel,
            listView, separator,
            detailHeader, detailTitle, detailDesc, detailProgress,
            detailReward, detailGiver, detailStatus,
            hint);
        DialogHelper.AddCloseFooter(dialog);
        listView.SetFocus();
        DialogHelper.RunModal(dialog);
    }
}
