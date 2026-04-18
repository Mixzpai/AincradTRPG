using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Lifetime Records dialog — opened from the TitleScreen "Records" menu.
// Replaces the old MessageBox.Query text dump with a structured 80x30
// Terminal.Gui Dialog. Three main blocks:
//   1. Header banner + two-column Summary / Achievement panels + two
//      progress bars (Floor / Win Rate). Always visible.
//   2. Tabbed table area — Tab / Left-Right arrows cycle between:
//        • Recent Runs (last 10 of any outcome — death or victory)
//        • Victory Leaderboard (ONLY completed victories, sortable)
//   3. Close button at bottom.
//
// Leaderboard sort keys cycle via `[` / `]` or the "Sort by: X" button.
// Sortable on: Col, Turns, Level, Kills, PlayTime, Date, Grade. Keeps
// all-time victory records since a victory means the 100-floor climb
// is done and worth preserving.
public static class RecordsDialog
{
    private const int DialogWidth = 80;
    private const int DialogHeight = 30;

    private enum ViewMode { Recent, Leaderboard }
    private enum SortKey { Col, Turns, Level, Kills, PlayTime, Date, Grade }

    public static void Show()
    {
        var data = LifetimeStats.Load();
        var dialog = DialogHelper.Create("Lifetime Records", DialogWidth, DialogHeight);

        // ── Banner ──────────────────────────────────────────────────────
        var bannerTop = new Label
        {
            Text = "╔══════════════════════ ⚔ AINCRAD CHRONICLE ⚔ ══════════════════════╗",
            X = Pos.Center(), Y = 0, Width = Dim.Auto(), ColorScheme = ColorSchemes.Gold,
        };

        // Empty-state short-circuit.
        if (data.TotalRuns == 0)
        {
            var empty = new Label
            {
                Text = "⚔  The pages await your deeds.  ⚔\n\n  Begin your climb — every run is chronicled here.",
                X = Pos.Center(), Y = Pos.Center(), Width = Dim.Auto(), Height = 3,
                ColorScheme = ColorSchemes.Dim,
            };
            var emptyClose = DialogHelper.CreateMenuButton("Close", isDefault: true);
            emptyClose.X = Pos.Center(); emptyClose.Y = Pos.AnchorEnd(2);
            emptyClose.Accepting += (s, e) => { e.Cancel = true; Application.RequestStop(); };
            dialog.Add(bannerTop, empty, emptyClose);
            DialogHelper.CloseOnEscape(dialog);
            DialogHelper.RunModal(dialog);
            return;
        }

        // ── Summary panel (left column, ~36 wide) ──────────────────────
        int colW = 36;
        int leftColX = 2;
        int rightColX = leftColX + colW + 2;

        var summaryHdr = new Label
        {
            Text = "┌─ Summary ─────────────────────────┐",
            X = leftColX, Y = 2, Width = Dim.Auto(), ColorScheme = ColorSchemes.Gold,
        };
        string totalTime = FormatTime(data.TotalPlayTimeSeconds);
        var summaryBody = new Label
        {
            Text =
                $"│ Runs ............ {data.TotalRuns,-14} │\n" +
                $"│ Victories ....... {data.TotalVictories,-14} │\n" +
                $"│ Deaths .......... {data.TotalDeaths,-14} │\n" +
                $"│ Total Kills ..... {data.TotalKills,-14:N0} │\n" +
                $"│ Play Time ....... {totalTime,-14} │\n" +
                "└───────────────────────────────────┘",
            X = leftColX, Y = 3, Width = Dim.Auto(), Height = 6,
            ColorScheme = ColorSchemes.Body,
        };

        // ── Achievement panel (right column) ────────────────────────────
        var achHdr = new Label
        {
            Text = "┌─ Achievement ─────────────────────┐",
            X = rightColX, Y = 2, Width = Dim.Auto(), ColorScheme = ColorSchemes.Gold,
        };
        int winRate = data.TotalRuns > 0 ? (data.TotalVictories * 100 / data.TotalRuns) : 0;
        var achBody = new Label
        {
            Text =
                $"│ Best Grade ...... {data.BestGrade,-14} │\n" +
                $"│ Highest Floor ... F{data.HighestFloor,-13} │\n" +
                $"│ Highest Level ... Lv {data.HighestLevel,-11} │\n" +
                $"│ Col Earned ...... {data.TotalColEarned,-14:N0} │\n" +
                $"│ Win Rate ........ {winRate,-13}% │\n" +
                "└───────────────────────────────────┘",
            X = rightColX, Y = 3, Width = Dim.Auto(), Height = 6,
            ColorScheme = ColorSchemes.Body,
        };

        // ── Progress bars ───────────────────────────────────────────────
        int barRowY = 10;
        var floorBar = new Label
        {
            Text = $"Floor Progress  {MakeBar(data.HighestFloor, 100, 40)}  {data.HighestFloor} / 100",
            X = leftColX, Y = barRowY, Width = Dim.Auto(),
            ColorScheme = data.HighestFloor >= 100 ? ColorSchemes.Gold : ColorSchemes.Body,
        };
        var winBar = new Label
        {
            Text = $"Win Rate        {MakeBar(data.TotalVictories, data.TotalRuns, 40)}  {data.TotalVictories} / {data.TotalRuns}",
            X = leftColX, Y = barRowY + 1, Width = Dim.Auto(), ColorScheme = ColorSchemes.Body,
        };

        // ── Tab switcher ────────────────────────────────────────────────
        int tabRowY = 13;
        ViewMode mode = ViewMode.Recent;
        SortKey sortKey = SortKey.Col;

        var recentTab = DialogHelper.CreateMenuButton("Recent Runs", isDefault: true);
        recentTab.X = leftColX; recentTab.Y = tabRowY;
        var leaderTab = DialogHelper.CreateMenuButton("Victory Leaderboard");
        leaderTab.X = Pos.Right(recentTab) + 2; leaderTab.Y = tabRowY;
        var sortBtn = DialogHelper.CreateButton($"Sort: {sortKey}");
        // AnchorEnd(18) gives 2 cols clearance from the dialog's right border
        // so the widest Sort label ("Sort: PlayTime" + button padding) doesn't
        // clip the frame edge.
        sortBtn.X = Pos.AnchorEnd(18); sortBtn.Y = tabRowY;
        sortBtn.Visible = false;  // hidden until leaderboard tab active

        // ── Table container (shared, content swapped on tab change) ─────
        int tableY = tabRowY + 2;
        var tableLabel = new Label
        {
            Text = "", X = leftColX, Y = tableY,
            Width = DialogWidth - 4, Height = 12,
            ColorScheme = ColorSchemes.Body,
        };

        void RefreshTable()
        {
            if (mode == ViewMode.Recent)
                tableLabel.Text = BuildRecentTable(data.RecentRuns);
            else
                tableLabel.Text = BuildLeaderboardTable(data.VictoryRuns, sortKey);
            sortBtn.Text = $"Sort: {sortKey}";
            sortBtn.Visible = mode == ViewMode.Leaderboard;
        }

        recentTab.Accepting += (s, e) => { e.Cancel = true; mode = ViewMode.Recent; RefreshTable(); };
        leaderTab.Accepting += (s, e) => { e.Cancel = true; mode = ViewMode.Leaderboard; RefreshTable(); };
        sortBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            sortKey = (SortKey)(((int)sortKey + 1) % Enum.GetValues(typeof(SortKey)).Length);
            RefreshTable();
        };

        RefreshTable();

        // ── Close button ────────────────────────────────────────────────
        var closeBtn = DialogHelper.CreateMenuButton("Close");
        closeBtn.X = Pos.Center(); closeBtn.Y = Pos.AnchorEnd(2);
        closeBtn.Accepting += (s, e) => { e.Cancel = true; Application.RequestStop(); };

        // ── Nav: Tab + Left/Right cycles tabs; `[`/`]` cycles sort ──────
        dialog.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Tab)
            {
                mode = mode == ViewMode.Recent ? ViewMode.Leaderboard : ViewMode.Recent;
                RefreshTable();
                (mode == ViewMode.Recent ? recentTab : leaderTab).SetFocus();
                e.Handled = true;
            }
            else if (e.AsRune.Value == '[' && mode == ViewMode.Leaderboard)
            {
                int n = Enum.GetValues(typeof(SortKey)).Length;
                sortKey = (SortKey)(((int)sortKey - 1 + n) % n);
                RefreshTable();
                e.Handled = true;
            }
            else if (e.AsRune.Value == ']' && mode == ViewMode.Leaderboard)
            {
                sortKey = (SortKey)(((int)sortKey + 1) % Enum.GetValues(typeof(SortKey)).Length);
                RefreshTable();
                e.Handled = true;
            }
        };

        dialog.Add(bannerTop,
            summaryHdr, summaryBody,
            achHdr, achBody,
            floorBar, winBar,
            recentTab, leaderTab, sortBtn,
            tableLabel,
            closeBtn);
        DialogHelper.CloseOnEscape(dialog);
        DialogHelper.RunModal(dialog);
    }

    // Render 10-wide horizontal block bar — filled chars proportional to
    // (value/max), remainder as dim shaded blocks. Width param controls
    // on-screen cell count; filled uses `█`, empty uses `░`.
    private static string MakeBar(int value, int max, int width)
    {
        if (max <= 0) max = 1;
        int filled = Math.Clamp(value * width / max, 0, width);
        return "[" + new string('█', filled) + new string('░', width - filled) + "]";
    }

    private static string FormatTime(long totalSeconds)
    {
        if (totalSeconds >= 3600)
            return $"{totalSeconds / 3600}h {totalSeconds % 3600 / 60:D2}m";
        return $"{totalSeconds / 60}m";
    }

    // ── Recent Runs table ──────────────────────────────────────────────
    // Columns: # / Date / Name / Floor / Lv / Kills / Grade / Time
    // Fate column dropped — if a run is in this table it ended (victory
    // or death); the death case is implied. Victories naturally show at
    // F100 with a top-grade entry.
    private static string BuildRecentTable(List<LifetimeStats.RunEntry> runs)
    {
        if (runs.Count == 0) return "\n  No recent runs yet.";
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("  # Date          Name             Floor  Lv   Kills  Grade  Time");
        sb.AppendLine("  ───────────────────────────────────────────────────────────────────");
        for (int i = 0; i < runs.Count && i < 10; i++)
        {
            var r = runs[i];
            string name = DisplayName(r.PlayerName).PadRight(15);
            string date = (r.Date.Length >= 10 ? r.Date[..10] : r.Date).PadRight(12);
            string time = FormatTime(r.PlayTimeSeconds).PadRight(6);
            sb.AppendLine($"  {i + 1,2} {date}  {name}  F{r.Floor,-4}  {r.Level,-3}  {r.Kills,-5}  {r.Grade,-5}  {time}");
        }
        return sb.ToString();
    }

    // Map legacy-default "Unknown" placeholder (from RunEntry saved before
    // the PlayerName field was added) to an em-dash so stale rows read as
    // "data predates this field" instead of an error state. Truncates any
    // name longer than 15 chars for column alignment.
    private static string DisplayName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name == "Unknown") return "—";
        return name.Length > 15 ? name[..15] : name;
    }

    // ── Victory Leaderboard table ──────────────────────────────────────
    // ONLY victories. Sort order set by `sortKey`. Shows top 10.
    private static string BuildLeaderboardTable(List<LifetimeStats.RunEntry> runs, SortKey key)
    {
        if (runs.Count == 0)
            return "\n  No victory runs yet.\n\n  Beat the game to record your first entry on the leaderboard.";

        // Apply sort — leaderboard best-first (higher Col/Level/Kills better,
        // lower Turns/PlayTime better, newer Date better, higher Grade better).
        IEnumerable<LifetimeStats.RunEntry> sorted = key switch
        {
            SortKey.Col      => runs.OrderByDescending(r => r.ColEarned),
            SortKey.Turns    => runs.OrderBy(r => r.TurnCount == 0 ? int.MaxValue : r.TurnCount),
            SortKey.Level    => runs.OrderByDescending(r => r.Level),
            SortKey.Kills    => runs.OrderByDescending(r => r.Kills),
            SortKey.PlayTime => runs.OrderBy(r => r.PlayTimeSeconds == 0 ? long.MaxValue : r.PlayTimeSeconds),
            SortKey.Date     => runs.OrderByDescending(r => r.Date),
            SortKey.Grade    => runs.OrderByDescending(r => GradeRank(r.Grade)),
            _                => runs.AsEnumerable(),
        };

        var top = sorted.Take(10).ToList();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("  Rank Name             Col        Turns    Lv   Kills  Grade  Time    Date");
        sb.AppendLine("  ─────────────────────────────────────────────────────────────────────────────");
        for (int i = 0; i < top.Count; i++)
        {
            var r = top[i];
            string name = DisplayName(r.PlayerName).PadRight(15);
            string time = FormatTime(r.PlayTimeSeconds).PadRight(6);
            string date = (r.Date.Length >= 10 ? r.Date[..10] : r.Date).PadRight(10);
            string turns = r.TurnCount > 0 ? r.TurnCount.ToString("N0") : "—";
            sb.AppendLine($"  {i + 1,3}. {name}  {r.ColEarned,-9:N0}  {turns,-7}  {r.Level,-3}  {r.Kills,-5}  {r.Grade,-5}  {time}  {date}");
        }
        if (runs.Count > 10) sb.AppendLine($"  … and {runs.Count - 10} more.");
        return sb.ToString();
    }

    private static int GradeRank(string grade) => grade switch
    {
        _ when grade.StartsWith("S+") => 6,
        _ when grade.StartsWith("S")  => 5,
        _ when grade.StartsWith("A")  => 4,
        _ when grade.StartsWith("B")  => 3,
        _ when grade.StartsWith("C")  => 2,
        _ => 1,
    };
}
