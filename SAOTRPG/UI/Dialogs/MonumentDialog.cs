using System.Collections.ObjectModel;
using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;
using SAOTRPG.Systems.Input;

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
        // 11 rows reserved below: the kill-log header, its 6-row strip, and the shared
        // detail/close block. The default 6 drew the list's last five rows under the strip.
        MilestoneTabbedView.Build(dialog, player, initialCategory: null, bottomReserve: 11,
            extraKeys: $"{Keybinds.Get(GameAction.MilestoneFocusKillLog).Primary}: kill log");

        // ── Kill log preview at the bottom (unique to Monument) ──────
        // The legacy Monument's species kill log lives here as a compact 6-row
        // strip beneath the milestone area. Full per-species detail still lives
        // on the Bestiary screen.
        ListView killList = AddKillLogStrip(dialog);

        // The kill log is the second focusable list on a dialog that claims Tab for category
        // cycling, so nothing moved focus onto it. Toggles between the two lists.
        dialog.KeyDown += (s, e) =>
        {
            if (!Keybinds.IsPressed(GameAction.MilestoneFocusKillLog, e)) return;
            var lists = new List<ListView>();
            CollectLists(dialog, lists);
            ListView? milestones = lists.Count > 0 ? lists[0] : null;
            if (killList.HasFocus) milestones?.SetFocus();
            else killList.SetFocus();
            e.Handled = true;
        };

        DialogHelper.AddCloseFooter(dialog);
        DialogHelper.RunModal(dialog);
    }

    // Depth-first list of the dialog's ListViews, in add order: [0] milestones, [1] kill log.
    private static void CollectLists(View v, List<ListView> into)
    {
        foreach (View c in v.SubViews)
        {
            if (c is ListView lv) into.Add(lv);
            CollectLists(c, into);
        }
    }

    // 6-row scrolling strip showing top-killed species + 10/100/1000 checkmarks.
    private static ListView AddKillLogStrip(Dialog dialog)
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
            // Type-ahead off — it consumes a printable rune before KeyDown is raised, which ate
            // the dialog-level 1-9/0 tab jumps whenever focus sat on this list.
            KeystrokeNavigator = null,
        };
        killList.SetSource(new ObservableCollection<string>(lines));
        // A read-only strip, but it is still a focus stop, so Enter reaches it — and an
        // unclaimed Command.Accept bubbles to the dialog's default button, which is Close.
        killList.Accepting += (s, e) => e.Handled = true;
        dialog.Add(killList);
        DialogHelper.SelectFirstRow(killList);
        return killList;
    }
}
