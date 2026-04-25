using System.Collections.ObjectModel;
using Terminal.Gui;
using SAOTRPG.Items;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Bundle 13 (Item 1) — Legendary collectables panel. Reads CollectablesTracker
// and renders the 184 canonical Legendaries grouped by 9 source buckets,
// with collected/uncollected filter + this-floor/all-floors filter.
// Opens via Shift+L from MapView (bare L stays Look mode).
public static class LegendaryCollectablesDialog
{
    private enum CollectFilter { All, Obtained, NotObtained }
    private enum FloorFilter { All, ThisBand }

    private static readonly string[] BucketOrder =
        { "LN", "AL", "IF", "HF", "LR", "MD", "FD", "Myth", "Non-Canon" };
    private static readonly Dictionary<string, string> BucketLabels = new()
    {
        ["LN"] = "Light Novel (canon)",
        ["AL"] = "Alicization Lycoris",
        ["IF"] = "Integral Factor",
        ["HF"] = "Hollow Fragment",
        ["LR"] = "Last Recollection / Lost Song",
        ["MD"] = "Memory Defrag",
        ["FD"] = "Fractured Daydream",
        ["Myth"] = "Mythological",
        ["Non-Canon"] = "Non-Canon (invented)",
    };

    public static void Show(int currentFloor)
    {
        var collectFilter = CollectFilter.All;
        var floorFilter = FloorFilter.All;

        int screenW = Application.Top?.Frame.Width  ?? 100;
        int screenH = Application.Top?.Frame.Height ?? 30;
        int dlgW = Math.Min(Math.Max(60, screenW - 6), 120);
        int dlgH = Math.Min(Math.Max(20, screenH - 4), 45);

        var dialog = DialogHelper.Create("Legendary Collectables", dlgW, dlgH);

        var headerLabel = new Label
        {
            Text = "", X = 1, Y = 0, Width = Dim.Fill(2),
            ColorScheme = ColorSchemes.Gold,
        };
        var filterLabel = new Label
        {
            Text = "", X = 1, Y = 1, Width = Dim.Fill(2),
            ColorScheme = ColorSchemes.Body,
        };

        var entries = new ObservableCollection<string>();
        var listView = new ListView
        {
            X = 1, Y = 3, Width = Dim.Fill(1), Height = Dim.Fill(4),
            Source = new ListWrapper<string>(entries),
            CanFocus = true,
            ColorScheme = ColorSchemes.ListSelection,
        };

        var hint = new Label
        {
            Text = "1: All  2: Obtained  3: Not Yet  | F1: All Floors  F2: This Floor (±8) | Esc: close",
            X = 1, Y = Pos.AnchorEnd(2), Width = Dim.Fill(1),
            ColorScheme = ColorSchemes.Dim,
        };

        // Build the line list given current filters.
        void Refresh()
        {
            entries.Clear();
            int total = CollectablesTracker.TotalCount();
            int got   = CollectablesTracker.CollectedCount();
            string flStr = floorFilter == FloorFilter.ThisBand
                ? $" · floor band F{Math.Max(1, currentFloor - 8)}-F{currentFloor + 8}"
                : "";
            headerLabel.Text = $"Collected: {got} / {total}{flStr}";
            string filtStr = collectFilter switch
            {
                CollectFilter.All         => "[1 All]   2 Obtained    3 Not Yet",
                CollectFilter.Obtained    => " 1 All   [2 Obtained]   3 Not Yet",
                CollectFilter.NotObtained => " 1 All    2 Obtained   [3 Not Yet]",
                _ => "",
            };
            string flFilt = floorFilter == FloorFilter.All
                ? "  | [F1 All Floors]   F2 This Floor"
                : "  |  F1 All Floors   [F2 This Floor]";
            filterLabel.Text = filtStr + flFilt;

            // Optional band restriction.
            HashSet<string>? bandSet = null;
            if (floorFilter == FloorFilter.ThisBand)
                bandSet = new HashSet<string>(CollectablesTracker.ForCurrentFloor(currentFloor));

            int linesAdded = 0;
            foreach (var bucket in BucketOrder)
            {
                if (!CollectablesTracker.AllByBucket.TryGetValue(bucket, out var defIds)) continue;
                if (defIds.Count == 0) continue;

                // Apply filters before printing the header so empty buckets stay collapsed.
                var filteredIds = new List<string>(defIds.Count);
                foreach (var id in defIds)
                {
                    bool isObtained = CollectablesTracker.IsCollected(id);
                    if (collectFilter == CollectFilter.Obtained    && !isObtained) continue;
                    if (collectFilter == CollectFilter.NotObtained &&  isObtained) continue;
                    if (bandSet != null && !bandSet.Contains(id))             continue;
                    filteredIds.Add(id);
                }
                if (filteredIds.Count == 0) continue;

                int bucketGot = filteredIds.Count(id => CollectablesTracker.IsCollected(id));
                string label = BucketLabels.GetValueOrDefault(bucket, bucket);
                entries.Add($"── {label} ─ {bucketGot}/{filteredIds.Count} ──────");
                linesAdded++;

                foreach (var id in filteredIds)
                {
                    bool got2 = CollectablesTracker.IsCollected(id);
                    string mark = got2 ? "[✓]" : "[ ]";
                    string name = ItemRegistry.Create(id)?.Name ?? id;
                    string rarity = "L"; // 184 entries are Legendary by definition (Tracker source).
                    var citation = CanonCitationData.Lookup(id);
                    string anchor = citation?.FloorAnchor ?? "";
                    string raw = $"  {mark} {rarity} {name}  · {anchor}";
                    int max = Math.Max(20, dlgW - 6);
                    entries.Add(raw.Length > max ? raw[..max] : raw);
                    linesAdded++;
                }
                entries.Add("");
                linesAdded++;
            }
            if (linesAdded == 0)
                entries.Add("  (no entries match these filters)");
            listView.SetNeedsDraw();
        }

        // Filter keys captured at dialog level so the list doesn't swallow them.
        dialog.KeyDown += (s, e) =>
        {
            if (e.IsShift || (e.KeyCode & KeyCode.CtrlMask) != 0) return;
            switch (e.KeyCode)
            {
                case KeyCode.D1: collectFilter = CollectFilter.All;         Refresh(); e.Handled = true; break;
                case KeyCode.D2: collectFilter = CollectFilter.Obtained;    Refresh(); e.Handled = true; break;
                case KeyCode.D3: collectFilter = CollectFilter.NotObtained; Refresh(); e.Handled = true; break;
                case KeyCode.F1: floorFilter   = FloorFilter.All;           Refresh(); e.Handled = true; break;
                case KeyCode.F2: floorFilter   = FloorFilter.ThisBand;      Refresh(); e.Handled = true; break;
            }
        };

        dialog.Add(headerLabel, filterLabel, listView, hint);
        DialogHelper.AddCloseFooter(dialog);
        Refresh();
        listView.SetFocus();
        DialogHelper.RunModal(dialog);
    }
}
