using System.Collections.ObjectModel;
using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Monument of Swordsmen — canonical NPC view of the unified milestone system.
// Top: NPC flavor. Middle: shared 10-tab milestone view (same renderer as MilestonesDialog).
// Bottom: per-species kill log preserved from the legacy Monument layout.
public static class MonumentDialog
{
    public static void Show(Player player)
    {
        int screenW = AppHost.App.Screen.Width;
        int screenH = AppHost.App.Screen.Height;
        int dlgW = Math.Min(Math.Max(96, screenW - 6), 130);
        int dlgH = Math.Min(Math.Max(36, screenH - 4), 50);

        var dialog = DialogHelper.Create("Monument of Swordsmen", dlgW, dlgH);

        // ── NPC framing ──────────────────────────────────────────────
        var flavorLabel = new Label
        {
            Text = "The black iron remembers every blade raised in its shadow.",
            X = Pos.Center(), Y = 0,
            SchemeName = ColorSchemes.DimName,
        };
        dialog.Add(flavorLabel);

        // ── Shared milestone tab view ────────────────────────────────
        MilestoneTabbedView.Build(dialog, player, initialCategory: null);

        // ── Kill log preview at the bottom (unique to Monument) ──────
        // The legacy Monument's species kill log lives here as a compact 6-row
        // strip beneath the milestone area. Full per-species detail still lives
        // on the Bestiary screen.
        AddKillLogStrip(dialog);

        DialogHelper.AddCloseFooter(dialog);
        DialogHelper.RunModal(dialog);
    }

    // 6-row scrolling strip showing top-killed species + 10/100/1000 checkmarks.
    private static void AddKillLogStrip(Dialog dialog)
    {
        var header = new Label
        {
            Text = "[ Kill Log — Species & Milestones ]",
            X = 1, Y = Pos.AnchorEnd(11),
            SchemeName = ColorSchemes.GoldName,
        };
        dialog.Add(header);

        var entries = Bestiary.GetAll();
        var lines = new List<string>();
        if (entries.Count == 0)
        {
            lines.Add("  No kills recorded yet. The monument waits.");
        }
        else
        {
            // Sort by kills descending — Monument is canonical kill log surface.
            var ordered = entries.OrderByDescending(e => e.TimesKilled).Take(64);
            foreach (var e in ordered)
            {
                string m10  = e.TimesKilled >= 10   ? "[10✓]"   : "[10 ]";
                string m100 = e.TimesKilled >= 100  ? "[100✓]"  : "[100 ]";
                string m1k  = e.TimesKilled >= 1000 ? "[1000✓]" : "[1000 ]";
                lines.Add($"  {e.Name,-32} x{e.TimesKilled,-5} {m10} {m100} {m1k}");
            }
        }

        var killList = new ListView
        {
            X = 1, Y = Pos.AnchorEnd(10), Width = Dim.Fill(2), Height = 6,
            SchemeName = ColorSchemes.ListSelectionName,
            CanFocus = true,
        };
        killList.SetSource(new ObservableCollection<string>(lines));
        dialog.Add(killList);
    }
}
