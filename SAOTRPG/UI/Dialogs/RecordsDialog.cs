using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Lifetime Records (TitleScreen → Records). 80x30: banner + Summary/Career panels + Floor/Win bars, tabbed table (Recent Runs | Victory Leaderboard), Close.
// Tab or ←/→ cycle tabs; leaderboard sort via [/] or Sort button (Col/Turns/Level/Kills/PlayTime/Date/Grade).
public static class RecordsDialog
{
    private const int DialogWidth = 80;
    private const int DialogHeight = 30;

    private enum ViewMode { Recent, Leaderboard }
    private enum SortKey { Col, Turns, Level, Kills, PlayTime, Date, Grade }

    // Columns between the two │ edges of the Summary / Career Stats panels. Every row has to
    // agree on this or the box renders ragged, which it did: the borders came out 37 columns
    // wide against body rows of 36, so the right edge of all twelve data rows sat one column
    // inside the corners. Rows are built by the three helpers below rather than hand-spaced.
    private const int BoxInner = 35;

    // Columns the label plus its dot leader occupy inside a row.
    private const int LeaderWidth = 18;

    public static void Show()
    {
        var data = LifetimeStats.Load();
        var dialog = DialogHelper.Create("Lifetime Records", DialogWidth, DialogHeight);

        // ── Banner ──────────────────────────────────────────────────────
        var bannerTop = new Label
        {
            Text = "╔══════════════════════ ⚔ AINCRAD CHRONICLE ⚔ ══════════════════════╗",
            X = Pos.Center(), Y = 0, Width = Dim.Auto(), SchemeName = ColorSchemes.GoldName,
        };

        // Empty-state short-circuit.
        if (data.TotalRuns == 0)
        {
            var empty = new Label
            {
                Text = "⚔  The pages await your deeds.  ⚔\n\n  Begin your climb — every run is chronicled here.",
                X = Pos.Center(), Y = Pos.Center(), Width = Dim.Auto(), Height = 3,
                SchemeName = ColorSchemes.DimName,
            };
            var emptyClose = DialogHelper.CreateMenuButton("Close", isDefault: true);
            emptyClose.X = Pos.Center(); emptyClose.Y = Pos.AnchorEnd(2);
            emptyClose.Accepting += (s, e) => { e.Handled = true; AppHost.App.RequestStop(); };
            dialog.Add(bannerTop, empty, emptyClose);
            DialogHelper.CloseOnEscape(dialog);
            DialogHelper.RunModal(dialog);
            return;
        }

        // ── Summary panel (left column) ────────────────────────────────
        // Box outer width is BoxInner + the two │ edges; the right column clears it by one.
        int leftColX = 2;
        int rightColX = leftColX + BoxInner + 2 + 1;

        var summaryHdr = new Label
        {
            Text = BoxTop("Summary"),
            X = leftColX, Y = 2, Width = Dim.Auto(), SchemeName = ColorSchemes.GoldName,
        };
        string totalTime = FormatTime(data.TotalPlayTimeSeconds);
        var summaryBody = new Label
        {
            Text = string.Join("\n",
                BoxRow("Runs",        $"{data.TotalRuns}"),
                BoxRow("Victories",   $"{data.TotalVictories}"),
                BoxRow("Deaths",      $"{data.TotalDeaths}"),
                BoxRow("Total Kills", $"{data.TotalKills:N0}"),
                BoxRow("Play Time",   totalTime),
                BoxBottom()),
            X = leftColX, Y = 3, Width = Dim.Auto(), Height = 6,
            SchemeName = ColorSchemes.BodyName,
        };

        // ── Career-stats panel (right column) ───────────────────────────
        var achHdr = new Label
        {
            Text = BoxTop("Career Stats"),
            X = rightColX, Y = 2, Width = Dim.Auto(), SchemeName = ColorSchemes.GoldName,
        };
        int winRate = data.TotalRuns > 0 ? (data.TotalVictories * 100 / data.TotalRuns) : 0;
        var achBody = new Label
        {
            Text = string.Join("\n",
                BoxRow("Best Grade",    data.BestGrade),
                BoxRow("Highest Floor", $"F{data.HighestFloor}"),
                BoxRow("Highest Level", $"Lv {data.HighestLevel}"),
                BoxRow("Col Earned",    $"{data.TotalColEarned:N0}"),
                BoxRow("Win Rate",      $"{winRate}%"),
                BoxRow("IF Implements", $"{data.IfImplementHighWaterMark}"),
                BoxRow("HF Missions",   $"{data.HfMissionHighWaterMark}"),
                BoxBottom()),
            X = rightColX, Y = 3, Width = Dim.Auto(), Height = 8,
            SchemeName = ColorSchemes.BodyName,
        };

        // ── Progress bars ───────────────────────────────────────────────
        // Career-stats panel grew by 2 rows (IF/HF lifetime hi-water) so the
        // bar strip + tab strip drop down by 2 to keep clearance.
        int barRowY = 12;
        var floorBar = new Label
        {
            Text = $"Floor Progress  {MakeBar(data.HighestFloor, 100, 40)}  {data.HighestFloor} / 100",
            X = leftColX, Y = barRowY, Width = Dim.Auto(),
            SchemeName = data.HighestFloor >= 100 ? ColorSchemes.GoldName : ColorSchemes.BodyName,
        };
        var winBar = new Label
        {
            Text = $"Win Rate        {MakeBar(data.TotalVictories, data.TotalRuns, 40)}  {data.TotalVictories} / {data.TotalRuns}",
            X = leftColX, Y = barRowY + 1, Width = Dim.Auto(), SchemeName = ColorSchemes.BodyName,
        };

        // ── Tab switcher ────────────────────────────────────────────────
        int tabRowY = 15;
        ViewMode mode = ViewMode.Recent;
        SortKey sortKey = SortKey.Col;

        var recentTab = DialogHelper.CreateMenuButton("Recent Runs", isDefault: true);
        recentTab.X = leftColX; recentTab.Y = tabRowY;
        var leaderTab = DialogHelper.CreateMenuButton("Victory Leaderboard");
        leaderTab.X = Pos.Right(recentTab) + 2; leaderTab.Y = tabRowY;
        var sortBtn = DialogHelper.CreateButton($"Sort: {sortKey}");
        // AnchorEnd(18) gives 2-col clearance — "Sort: PlayTime" + padding won't clip frame.
        sortBtn.X = Pos.AnchorEnd(18); sortBtn.Y = tabRowY;
        sortBtn.Visible = false;  // hidden until leaderboard tab active

        // ── Table container (shared, content swapped on tab change) ─────
        int tableY = tabRowY + 2;
        var tableLabel = new Label
        {
            Text = "", X = leftColX, Y = tableY,
            Width = DialogWidth - 4, Height = 10,
            SchemeName = ColorSchemes.BodyName,
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

        recentTab.Accepting += (s, e) => { e.Handled = true; mode = ViewMode.Recent; RefreshTable(); };
        leaderTab.Accepting += (s, e) => { e.Handled = true; mode = ViewMode.Leaderboard; RefreshTable(); };
        sortBtn.Accepting += (s, e) =>
        {
            e.Handled = true;
            sortKey = (SortKey)(((int)sortKey + 1) % Enum.GetValues(typeof(SortKey)).Length);
            RefreshTable();
        };

        RefreshTable();

        // ── Close button ────────────────────────────────────────────────
        var closeBtn = DialogHelper.CreateMenuButton("Close");
        closeBtn.X = Pos.Center(); closeBtn.Y = Pos.AnchorEnd(2);
        closeBtn.Accepting += (s, e) => { e.Handled = true; AppHost.App.RequestStop(); };

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

    // "┌─ Summary ─────────────────────────┐"
    private static string BoxTop(string title) =>
        $"┌─ {title} ".PadRight(BoxInner + 1, '─') + "┐";

    // "└───────────────────────────────────┘"
    private static string BoxBottom() => "└" + new string('─', BoxInner) + "┘";

    // "│ Runs ............. 42             │" — value clipped rather than allowed to
    // push the right edge out, which is how the death/victory card used to burst its box.
    private static string BoxRow(string label, string value)
    {
        string cell = " " + (label + " ").PadRight(LeaderWidth, '.') + " " + value;
        if (cell.Length > BoxInner) cell = cell[..BoxInner];
        return "│" + cell.PadRight(BoxInner) + "│";
    }

    // Horizontal block bar: filled = ⌊value·width/max⌋ cells of █, rest ░.
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

    // ── Recent Runs table ──
    // Cols: # / Date / Name / Floor / Lv / Kills / Grade / Time. Fate implied (row exists → run ended).
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

    // Legacy "Unknown" (pre-PlayerName saves) → em-dash; truncates >15 chars for column width.
    private static string DisplayName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name == "Unknown") return "—";
        return name.Length > 15 ? name[..15] : name;
    }

    // ── Victory Leaderboard ── Victories only; top 10 per `sortKey`.
    private static string BuildLeaderboardTable(List<LifetimeStats.RunEntry> runs, SortKey key)
    {
        if (runs.Count == 0)
            return "\n  No victory runs yet.\n\n  Beat the game to record your first entry on the leaderboard.";

        // Best-first: Col/Level/Kills desc, Turns/PlayTime asc, Date/Grade desc.
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
