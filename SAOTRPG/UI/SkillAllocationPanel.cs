using Terminal.Gui;
using SAOTRPG.Entities;

namespace SAOTRPG.UI;

/// <summary>
/// Reusable skill point allocation UI — used in both CharacterCreation (phase 2)
/// and the in-game Skills dialog. Builds +/- buttons for each stat with a live
/// stats preview and allocate/reset controls.
/// </summary>
public static class SkillAllocationPanel
{
    private static readonly string[] StatNames = { "Vitality", "Strength", "Endurance", "Dexterity", "Agility", "Intelligence" };
    private static readonly char[] StatGlyphs = { '♥', '⚔', '⛊', '★', '▶', '✦' };

    /// <summary>
    /// Builds the full skill allocation UI into the given container view.
    /// Returns the statsView so the caller can position/size it, and the
    /// messageLabel for observing feedback.
    /// </summary>
    public static SkillAllocationResult Build(View container, Player player, int statsX, int statsY, int controlsX, int controlsStartY)
    {
        // ── Stats preview (left side) ──
        var statsView = new TextView
        {
            X = statsX, Y = statsY,
            Width = 38, Height = 22,
            ReadOnly = true,
            Text = player.GetStatsDisplay(),
            ColorScheme = Theme.Body
        };

        // ── Points available header ──
        var pointsLabel = new Label
        {
            Text = FormatPointsHeader(player.SkillPoints, 0),
            X = controlsX, Y = controlsStartY,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.StatHeader
        };

        var divider1 = new Label
        {
            Text = Theme.LightRule(30),
            X = controlsX, Y = controlsStartY + 1,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Dim
        };

        // ── Per-stat +/- rows ──
        var pending = new int[StatNames.Length];
        var pendingLabels = new Label[StatNames.Length];

        int TotalPending() { int sum = 0; foreach (var p in pending) sum += p; return sum; }

        void RefreshPendingDisplay()
        {
            for (int i = 0; i < StatNames.Length; i++)
            {
                pendingLabels[i].Text = pending[i] > 0 ? $"+{pending[i]}" : " 0";
                pendingLabels[i].ColorScheme = pending[i] > 0 ? Theme.StatPositive : Theme.Subtitle;
            }
            pointsLabel.Text = FormatPointsHeader(player.SkillPoints, TotalPending());
        }

        int rowStart = controlsStartY + 2;

        for (int i = 0; i < StatNames.Length; i++)
        {
            int row = rowStart + i * 2;
            int idx = i;

            var glyphLabel = new Label
            {
                Text = $" {StatGlyphs[i]}",
                X = controlsX, Y = row,
                Width = 3, Height = 1,
                ColorScheme = Theme.Gold
            };

            var nameLabel = new Label
            {
                Text = $"{StatNames[i]}:",
                X = controlsX + 3, Y = row,
                Width = 14, Height = 1,
                ColorScheme = Theme.StatLabel
            };

            var minusBtn = new Button
            {
                Text = " - ",
                X = controlsX + 17, Y = row,
                ColorScheme = Theme.SmallButton
            };

            pendingLabels[i] = new Label
            {
                Text = " 0",
                X = Pos.Right(minusBtn) + 1, Y = row,
                Width = 4, Height = 1,
                ColorScheme = Theme.Subtitle
            };

            var plusBtn = new Button
            {
                Text = " + ",
                X = Pos.Right(pendingLabels[i]) + 1, Y = row,
                ColorScheme = Theme.SmallButton
            };

            minusBtn.Accepting += (s, e) =>
            {
                e.Cancel = true;
                if (pending[idx] > 0)
                {
                    pending[idx]--;
                    RefreshPendingDisplay();
                }
            };

            plusBtn.Accepting += (s, e) =>
            {
                e.Cancel = true;
                if (TotalPending() < player.SkillPoints)
                {
                    pending[idx]++;
                    RefreshPendingDisplay();
                }
            };

            container.Add(glyphLabel, nameLabel, minusBtn, pendingLabels[i], plusBtn);
        }

        // ── Divider before controls ──
        int controlsY = rowStart + StatNames.Length * 2;

        var divider2 = new Label
        {
            Text = Theme.LightRule(30),
            X = controlsX, Y = controlsY,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Dim
        };

        // ── Allocate / Reset buttons ──
        int btnRow = controlsY + 1;

        var allocateBtn = new Button
        {
            Text = $" {Theme.Sparkle} Allocate ",
            X = controlsX, Y = btnRow,
            ColorScheme = Theme.MenuButton
        };

        var resetBtn = new Button
        {
            Text = $" {Theme.Cross} Reset ",
            X = Pos.Right(allocateBtn) + 2, Y = btnRow,
            ColorScheme = Theme.MenuButton
        };

        // ── Feedback label ──
        var messageLabel = new Label
        {
            Text = "",
            X = controlsX, Y = btnRow + 2,
            Width = 35, Height = 2,
            ColorScheme = Theme.Info
        };

        allocateBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            if (TotalPending() == 0)
            {
                messageLabel.Text = "No points queued.";
                messageLabel.ColorScheme = Theme.Subtitle;
                return;
            }

            var results = new System.Text.StringBuilder();
            for (int i = 0; i < StatNames.Length; i++)
            {
                if (pending[i] <= 0) continue;
                if (player.SpendSkillPoints(StatNames[i], pending[i]))
                    results.Append($"+{pending[i]} {StatNames[i]}  ");
                else
                    results.Append($"{StatNames[i]}: not enough  ");
                pending[i] = 0;
            }

            RefreshPendingDisplay();
            statsView.Text = player.GetStatsDisplay();
            messageLabel.Text = results.ToString();
            messageLabel.ColorScheme = Theme.Success;
        };

        resetBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            player.ResetStatAllocation();
            for (int i = 0; i < pending.Length; i++)
                pending[i] = 0;
            RefreshPendingDisplay();
            statsView.Text = player.GetStatsDisplay();
            messageLabel.Text = "Stats reset! Points refunded.";
            messageLabel.ColorScheme = Theme.Info;
        };

        container.Add(statsView, pointsLabel, divider1, divider2, allocateBtn, resetBtn, messageLabel);

        return new SkillAllocationResult
        {
            StatsView = statsView,
            AllocateButton = allocateBtn,
            ResetButton = resetBtn,
            MessageLabel = messageLabel,
            RefreshStats = () => { statsView.Text = player.GetStatsDisplay(); }
        };
    }

    private static string FormatPointsHeader(int available, int queued)
    {
        string header = $" ✧ Skill Points: {available}";
        if (queued > 0)
            header += $"  (Queued: {queued})";
        return header;
    }

    /// <summary>Result container so callers can wire up additional behavior.</summary>
    public class SkillAllocationResult
    {
        public TextView StatsView { get; init; } = null!;
        public Button AllocateButton { get; init; } = null!;
        public Button ResetButton { get; init; } = null!;
        public Label MessageLabel { get; init; } = null!;
        public Action RefreshStats { get; set; } = null!;
    }
}
