using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Dramatic floor transition screen — replaces the plain MessageBox.Query
// with a centered overlay showing the floor number and recap stats.
public static class FloorTransitionOverlay
{
    // Dialog width — sized to fit recap stat lines comfortably.
    private const int OverlayWidth  = 44;
    // Dialog height — enough for floor title, recap stats, and buttons.
    private const int OverlayHeight = 21;

    // Aincrad region names by floor range — displayed as the floor subtitle.
    // Sorted ascending; first match whose MaxFloor >= nextFloor wins.
    // Add new regions here when expanding the floor count.
    private static readonly (int MaxFloor, string Region)[] FloorRegions =
    {
        (5,   "The Outer Reaches"),
        (10,  "The Middle Levels"),
        (25,  "The Deep Strata"),
        (50,  "The Upper Sanctum"),
        (75,  "The Ruby Palace"),
        (100, "The Final Frontier"),
    };

    // Shows a floor transition dialog with recap stats from the previous floor.
    // Returns true if the player confirms the ascent.
    public static bool Show(int nextFloor, TurnManager.FloorRecapData? recap, int playerLevel = 1)
    {
        bool confirmed = false;

        var dialog = DialogHelper.Create("", OverlayWidth, OverlayHeight);

        int row = 0;

        // ── Divider top ───────────────────────────────────────────
        var topDiv = new Label
        {
            Text = new string('-', OverlayWidth - 6),
            X = Pos.Center(), Y = row++,
            ColorScheme = ColorSchemes.Gold
        };

        // ── "Teleporting to..." flavor ────────────────────────────
        var flavorLabel = new Label
        {
            Text = "Teleporting to the next floor...",
            X = Pos.Center(), Y = row++,
            ColorScheme = ColorSchemes.Dim
        };

        row++; // spacer

        // ── Floor number (large, centered) ────────────────────────
        var floorLabel = new Label
        {
            Text = $"Floor {nextFloor}",
            X = Pos.Center(), Y = row++,
            ColorScheme = ColorSchemes.Gold
        };

        // ── Floor subtitle (from FloorRegions table) ──────────────
        string subtitle = FloorRegions[^1].Region; // default to last region
        foreach (var (maxFloor, region) in FloorRegions)
        {
            if (nextFloor <= maxFloor) { subtitle = region; break; }
        }
        var subtitleLabel = new Label
        {
            Text = subtitle,
            X = Pos.Center(), Y = row++,
            ColorScheme = ColorSchemes.Dim
        };

        // ── Floor intro flavor text ─────────────────────────────────
        var introLabel = new Label
        {
            Text = $"\"{FlavorText.GetFloorEntryMessage(nextFloor)}\"",
            X = 1, Y = row,
            Width = OverlayWidth - 4, Height = 2,
            ColorScheme = ColorSchemes.Dim
        };
        dialog.Add(introLabel);
        row += 2;

        // ── Recap stats from previous floor ───────────────────────
        if (recap != null)
        {
            var recapHeader = new Label
            {
                Text = $"[ Floor {recap.Floor} Recap ]",
                X = Pos.Center(), Y = row++,
                ColorScheme = ColorSchemes.Body
            };
            dialog.Add(recapHeader);

            var statsLine1 = new Label
            {
                Text = $"Kills: {recap.Kills}    Items: {recap.Items}    DMG taken: {recap.DamageTaken}",
                X = Pos.Center(), Y = row++,
                ColorScheme = ColorSchemes.Body
            };
            string timeStr = recap.RealTime.TotalMinutes >= 1
                ? $"{(int)recap.RealTime.TotalMinutes}m {recap.RealTime.Seconds:D2}s"
                : $"{recap.RealTime.Seconds}s";
            var statsLine2 = new Label
            {
                Text = $"Turns: {recap.Turns}    Explored: {recap.ExplorePercent}%    Col: +{recap.ColEarned}",
                X = Pos.Center(), Y = row++,
                ColorScheme = ColorSchemes.Body
            };
            int floorPar = TurnManager.GetFloorPar(recap.Floor);
            string parTag = recap.Turns <= floorPar ? " FAST!" : "";
            var statsLine3 = new Label
            {
                Text = $"Clear Time: {timeStr}  ({recap.Turns}/{floorPar} turns{parTag})",
                X = Pos.Center(), Y = row++,
                ColorScheme = recap.Turns <= floorPar ? ColorSchemes.Gold : ColorSchemes.Dim
            };
            string floorGrade = RunGradeHelper.Rate(recap.Floor, recap.Kills, recap.Turns);
            var gradeLabel = new Label
            {
                Text = $"Grade: {floorGrade}",
                X = Pos.Center(), Y = row++,
                ColorScheme = floorGrade.StartsWith("S") ? ColorSchemes.Gold
                    : floorGrade.StartsWith("A") ? ColorSchemes.Body
                    : ColorSchemes.Dim
            };
            dialog.Add(statsLine1, statsLine2, statsLine3, gradeLabel);

            // Bounty result (if one was active this floor)
            if (recap.BountyTarget != null)
            {
                string bountyResult = recap.BountyDone
                    ? $"Bounty: {recap.BountyTarget} — COMPLETE!"
                    : $"Bounty: {recap.BountyTarget} — {recap.BountyProgress}/{recap.BountyNeeded}";
                var bountyLabel = new Label
                {
                    Text = bountyResult,
                    X = Pos.Center(), Y = row++,
                    ColorScheme = recap.BountyDone ? ColorSchemes.Gold : ColorSchemes.Body
                };
                dialog.Add(bountyLabel);
            }
        }

        // ── Par time for next floor ────────────────────────────
        int par = TurnManager.GetFloorPar(nextFloor);
        var parLabel = new Label
        {
            Text = $"Speed par: {par} turns",
            X = Pos.Center(), Y = row++,
            ColorScheme = ColorSchemes.Dim
        };
        dialog.Add(parLabel);

        // ── Danger preview — expected monster level vs player ──
        int avgMobLevel = nextFloor; // mobs spawn at floor-1 to floor+2
        int diff = avgMobLevel - playerLevel;
        string danger = diff switch
        {
            >= 5  => "Deadly",
            >= 3  => "Dangerous",
            >= 1  => "Hard",
            0     => "Normal",
            >= -2 => "Safe",
            _     => "Trivial",
        };
        var dangerLabel = new Label
        {
            Text = $"Danger: {danger}",
            X = Pos.Center(), Y = row++,
            ColorScheme = danger is "Deadly" or "Dangerous" ? ColorSchemes.Danger
                : danger == "Hard" ? ColorSchemes.Body
                : ColorSchemes.Dim
        };
        dialog.Add(dangerLabel);

        // ── Weather preview (if not clear) ──────────────────────
        if (WeatherSystem.Current != WeatherType.Clear)
        {
            var weatherLabel = new Label
            {
                Text = $"Weather: {WeatherSystem.GetLabel()}",
                X = Pos.Center(), Y = row++,
                ColorScheme = ColorSchemes.Dim
            };
            dialog.Add(weatherLabel);
        }

        // ── Divider bottom ────────────────────────────────────────
        var bottomDiv = new Label
        {
            Text = new string('-', OverlayWidth - 6),
            X = Pos.Center(), Y = Pos.AnchorEnd(4),
            ColorScheme = ColorSchemes.Gold
        };

        // ── Buttons ───────────────────────────────────────────────
        var ascendBtn = new Button
        {
            Text = " Ascend ",
            X = Pos.Center() - 9, Y = Pos.AnchorEnd(2),
            ColorScheme = ColorSchemes.Button
        };
        var cancelBtn = new Button
        {
            Text = " Stay ",
            X = Pos.Right(ascendBtn) + 1, Y = Pos.AnchorEnd(2),
            IsDefault = true,
            ColorScheme = ColorSchemes.Button
        };

        ascendBtn.Accepting += (s, e) => { confirmed = true; Application.RequestStop(); e.Cancel = true; };
        cancelBtn.Accepting += (s, e) => { Application.RequestStop(); e.Cancel = true; };

        var hintLabel = new Label
        {
            Text = "Enter: continue to next floor",
            X = Pos.Center(), Y = Pos.AnchorEnd(1),
            ColorScheme = ColorSchemes.Dim
        };

        dialog.Add(topDiv, flavorLabel, floorLabel, subtitleLabel,
                   bottomDiv, ascendBtn, cancelBtn, hintLabel);
        DialogHelper.RunModal(dialog);

        return confirmed;
    }
}
