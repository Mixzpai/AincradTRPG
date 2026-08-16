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

    // Aincrad region names by floor range (floor subtitle). Sorted ascending; first MaxFloor >= nextFloor wins.
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
            SchemeName = ColorSchemes.GoldName
        };

        // ── "Teleporting to..." flavor ────────────────────────────
        var flavorLabel = new Label
        {
            Text = "Teleporting to the next floor...",
            X = Pos.Center(), Y = row++,
            SchemeName = ColorSchemes.DimName
        };

        row++; // spacer

        // ── Floor number (large, centered) ────────────────────────
        var floorLabel = new Label
        {
            Text = $"Floor {nextFloor}",
            X = Pos.Center(), Y = row++,
            SchemeName = ColorSchemes.GoldName
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
            SchemeName = ColorSchemes.DimName
        };

        // ── Floor intro flavor text ─────────────────────────────────
        var introLabel = new Label
        {
            Text = $"\"{FlavorText.GetFloorEntryMessage(nextFloor)}\"",
            X = 1, Y = row,
            Width = OverlayWidth - 4, Height = 2,
            SchemeName = ColorSchemes.DimName
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
                SchemeName = ColorSchemes.BodyName
            };
            dialog.Add(recapHeader);

            var statsLine1 = new Label
            {
                Text = $"Kills: {recap.Kills}    Items: {recap.Items}    DMG taken: {recap.DamageTaken}",
                X = Pos.Center(), Y = row++,
                SchemeName = ColorSchemes.BodyName
            };
            string timeStr = recap.RealTime.TotalMinutes >= 1
                ? $"{(int)recap.RealTime.TotalMinutes}m {recap.RealTime.Seconds:D2}s"
                : $"{recap.RealTime.Seconds}s";
            var statsLine2 = new Label
            {
                Text = $"Turns: {recap.Turns}    Explored: {recap.ExplorePercent}%    Col: +{recap.ColEarned}",
                X = Pos.Center(), Y = row++,
                SchemeName = ColorSchemes.BodyName
            };
            int floorPar = TurnManager.GetFloorPar(recap.Floor);
            string parTag = recap.Turns <= floorPar ? " FAST!" : "";
            var statsLine3 = new Label
            {
                Text = $"Clear Time: {timeStr}  ({recap.Turns}/{floorPar} turns{parTag})",
                X = Pos.Center(), Y = row++,
                SchemeName = recap.Turns <= floorPar ? ColorSchemes.GoldName : ColorSchemes.DimName
            };
            string floorGrade = RunGradeHelper.Rate(recap.Floor, recap.Kills, recap.Turns);
            var gradeLabel = new Label
            {
                Text = $"Grade: {floorGrade}",
                X = Pos.Center(), Y = row++,
                SchemeName = floorGrade.StartsWith("S") ? ColorSchemes.GoldName
                    : floorGrade.StartsWith("A") ? ColorSchemes.BodyName
                    : ColorSchemes.DimName
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
                    SchemeName = recap.BountyDone ? ColorSchemes.GoldName : ColorSchemes.BodyName
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
            SchemeName = ColorSchemes.DimName
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
            SchemeName = danger is "Deadly" or "Dangerous" ? ColorSchemes.DangerName
                : danger == "Hard" ? ColorSchemes.BodyName
                : ColorSchemes.DimName
        };
        dialog.Add(dangerLabel);

        // ── Weather preview (if not clear) ──────────────────────
        if (WeatherSystem.Current != WeatherType.Clear)
        {
            var weatherLabel = new Label
            {
                Text = $"Weather: {WeatherSystem.GetLabel()}",
                X = Pos.Center(), Y = row++,
                SchemeName = ColorSchemes.DimName
            };
            dialog.Add(weatherLabel);
        }

        // ── Divider bottom ────────────────────────────────────────
        var bottomDiv = new Label
        {
            Text = new string('-', OverlayWidth - 6),
            X = Pos.Center(), Y = Pos.AnchorEnd(4),
            SchemeName = ColorSchemes.GoldName
        };

        // ── Buttons ───────────────────────────────────────────────
        var ascendBtn = new Button
        {
            Text = " Ascend ",
            X = Pos.Center() - 9, Y = Pos.AnchorEnd(2),
            SchemeName = ColorSchemes.ButtonName, ShadowStyle = null
        };
        var cancelBtn = new Button
        {
            Text = " Stay ",
            X = Pos.Right(ascendBtn) + 1, Y = Pos.AnchorEnd(2),
            IsDefault = true,
            SchemeName = ColorSchemes.ButtonName, ShadowStyle = null
        };

        ascendBtn.Accepting += (s, e) => { confirmed = true; AppHost.App.RequestStop(); e.Handled = true; };
        cancelBtn.Accepting += (s, e) => { AppHost.App.RequestStop(); e.Handled = true; };

        var hintLabel = new Label
        {
            Text = "Enter: continue to next floor",
            X = Pos.Center(), Y = Pos.AnchorEnd(1),
            SchemeName = ColorSchemes.DimName
        };

        dialog.Add(topDiv, flavorLabel, floorLabel, subtitleLabel,
                   bottomDiv, ascendBtn, cancelBtn, hintLabel);
        DialogHelper.RunModal(dialog);

        return confirmed;
    }
}
