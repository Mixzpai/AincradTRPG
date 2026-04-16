using System.Collections.ObjectModel;
using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Quest Journal -- full-screen quest management with active/completed tabs,
// progress tracking, detail panel, and reward preview.
// Inspired by: Cogmind's bifurcated help pages, modern RPG quest logs with
// left-list / right-detail split, and clean bracket-header sections.
public static class QuestLogDialog
{
    private const int DialogWidth = 76, DialogHeight = 26;

    public static void Show(Player player)
    {
        var dialog = DialogHelper.Create("Quest Journal", DialogWidth, DialogHeight);

        bool showCompleted = false;

        // ── Tab buttons ──────────────────────────────────────────────
        var activeTab = new Button { Text = " Active ", X = 2, Y = 0, NoPadding = true, ColorScheme = ColorSchemes.Gold };
        var completedTab = new Button { Text = " Completed ", X = 14, Y = 0, NoPadding = true, ColorScheme = ColorSchemes.Dim };
        var countLabel = new Label
        {
            Text = $"{QuestSystem.ActiveQuests.Count}/{QuestSystem.MaxActiveQuests} active",
            X = Pos.AnchorEnd(18), Y = 0, Width = 16, ColorScheme = ColorSchemes.Dim,
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
            Width = Dim.Fill(2), ColorScheme = ColorSchemes.Gold,
        };
        var detailTitle = new Label
        {
            Text = "", X = 41, Y = 4,
            Width = Dim.Fill(2), Height = 1, ColorScheme = ColorSchemes.Title,
        };
        var detailDesc = new Label
        {
            Text = "Select a quest to view details.", X = 41, Y = 6,
            Width = Dim.Fill(2), Height = 4, ColorScheme = ColorSchemes.Body,
        };
        var detailProgress = new Label
        {
            Text = "", X = 41, Y = 11,
            Width = Dim.Fill(2), Height = 1, ColorScheme = ColorSchemes.Gold,
        };
        var detailReward = new Label
        {
            Text = "", X = 41, Y = 13,
            Width = Dim.Fill(2), Height = 1, ColorScheme = ColorSchemes.Dim,
        };
        var detailGiver = new Label
        {
            Text = "", X = 41, Y = 14,
            Width = Dim.Fill(2), Height = 1, ColorScheme = ColorSchemes.Dim,
        };
        var detailStatus = new Label
        {
            Text = "", X = 41, Y = 16,
            Width = Dim.Fill(2), Height = 1, ColorScheme = ColorSchemes.Body,
        };

        // ── Separator between list and detail ────────────────────────
        var separator = new Label
        {
            Text = new string('|', 20), X = 40, Y = 2,
            Width = 1, Height = Dim.Fill(4), ColorScheme = ColorSchemes.Dim,
        };

        var hint = new Label
        {
            Text = "Tab: switch tabs | Up/Down: browse | Talk to NPCs for quests | Esc: close",
            X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1), ColorScheme = ColorSchemes.Dim,
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
                names.Add($"{status} {typeTag} {TextHelpers.Truncate(q.Title, 22)}");
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
            detailStatus.ColorScheme = q.Status == QuestStatus.Complete
                ? ColorSchemes.Gold : ColorSchemes.Dim;
        }

        RefreshList();

        // ── Event wiring ─────────────────────────────────────────────
        listView.SelectedItemChanged += (s, e) => ShowDetail(listView.SelectedItem);

        activeTab.Accepting += (s, e) =>
        {
            e.Cancel = true;
            showCompleted = false;
            activeTab.ColorScheme = ColorSchemes.Gold;
            completedTab.ColorScheme = ColorSchemes.Dim;
            RefreshList();
            listView.SetFocus();
        };

        completedTab.Accepting += (s, e) =>
        {
            e.Cancel = true;
            showCompleted = true;
            activeTab.ColorScheme = ColorSchemes.Dim;
            completedTab.ColorScheme = ColorSchemes.Gold;
            RefreshList();
            listView.SetFocus();
        };

        // Tab key switches between active/completed
        listView.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Tab)
            {
                showCompleted = !showCompleted;
                activeTab.ColorScheme = showCompleted ? ColorSchemes.Dim : ColorSchemes.Gold;
                completedTab.ColorScheme = showCompleted ? ColorSchemes.Gold : ColorSchemes.Dim;
                RefreshList();
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
