using System.Collections.ObjectModel;
using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Monument of Swordsmen — kill milestones + title unlocks. Left: fought species w/ kill counts and 10/100/1000 checkpoints.
// Right: selected title desc, requirement, Equip/Unequip. Title worn via title list below species list.
public static class MonumentDialog
{
    private const int DialogWidth  = 92;
    private const int DialogHeight = 32;

    public static void Show(Player player)
    {
        var dialog = DialogHelper.Create("Monument of Swordsmen", DialogWidth, DialogHeight);

        // ── Header ───────────────────────────────────────────────────
        dialog.Add(new Label
        {
            Text = "The black iron remembers every blade raised in its shadow.",
            X = Pos.Center(), Y = 0, ColorScheme = ColorSchemes.Dim,
        });

        // ── Species kill list (left pane) ────────────────────────────
        dialog.Add(new Label
        {
            Text = "[ Kill Log — Species & Milestones ]",
            X = 1, Y = 2, ColorScheme = ColorSchemes.Gold,
        });

        var entries = Bestiary.GetAll();
        var speciesLines = new List<string>();
        if (entries.Count == 0)
        {
            speciesLines.Add("  No kills recorded yet. The monument waits.");
        }
        else
        {
            foreach (var e in entries)
            {
                string m10  = e.TimesKilled >= 10   ? "[10✓]"   : "[10 ]";
                string m100 = e.TimesKilled >= 100  ? "[100✓]"  : "[100 ]";
                string m1k  = e.TimesKilled >= 1000 ? "[1000✓]" : "[1000 ]";
                speciesLines.Add($"  {e.Name,-32} x{e.TimesKilled,-5} {m10} {m100} {m1k}");
            }
        }

        var speciesList = new ListView
        {
            X = 1, Y = 3,
            Width = DialogWidth / 2 - 2,
            Height = DialogHeight - 10,
            ColorScheme = ColorSchemes.ListSelection,
        };
        speciesList.SetSource(new ObservableCollection<string>(speciesLines));
        dialog.Add(speciesList);

        // ── Titles pane (right) ──────────────────────────────────────
        int rightX = DialogWidth / 2 + 1;
        dialog.Add(new Label
        {
            Text = "[ Titles ]",
            X = rightX, Y = 2, ColorScheme = ColorSchemes.Gold,
        });

        // Build title line list with unlocked/locked state and active marker.
        var titleDefs = TitleSystem.Titles.Values.ToList();
        string BuildTitleLine(TitleSystem.TitleDef def)
        {
            bool unlocked = player.UnlockedTitleIds.Contains(def.Id);
            bool active = player.ActiveTitleId == def.Id;
            string marker = active ? "★" : (unlocked ? "◆" : "·");
            string name = unlocked ? def.DisplayName : "???";
            return $"  {marker} {name}";
        }

        var titleLines = titleDefs.Select(BuildTitleLine).ToList();
        var titleList = new ListView
        {
            X = rightX, Y = 3,
            Width = DialogWidth / 2 - 2,
            Height = DialogHeight - 14,
            ColorScheme = ColorSchemes.ListSelection,
        };
        titleList.SetSource(new ObservableCollection<string>(titleLines));
        dialog.Add(titleList);

        // ── Title detail panel ───────────────────────────────────────
        int detailY = DialogHeight - 11;
        var detailHeader = new Label
        {
            Text = "[ Title Detail ]",
            X = rightX, Y = detailY, ColorScheme = ColorSchemes.Gold,
        };
        var detailName = new Label
        {
            Text = "", X = rightX, Y = detailY + 1,
            Width = DialogWidth / 2 - 2,
            ColorScheme = ColorSchemes.Title,
        };
        var detailDesc = new Label
        {
            Text = "", X = rightX, Y = detailY + 2,
            Width = DialogWidth / 2 - 2,
        };
        var detailReq = new Label
        {
            Text = "", X = rightX, Y = detailY + 3,
            Width = DialogWidth / 2 - 2,
            ColorScheme = ColorSchemes.Dim,
        };
        var activeStatus = new Label
        {
            Text = "", X = rightX, Y = detailY + 4,
            Width = DialogWidth / 2 - 2,
            ColorScheme = ColorSchemes.Dim,
        };
        dialog.Add(detailHeader, detailName, detailDesc, detailReq, activeStatus);

        // ── Equip/Unequip button ─────────────────────────────────────
        var equipBtn = new Button
        {
            Text = "[Enter] Equip",
            X = rightX, Y = detailY + 5,
            ColorScheme = ColorSchemes.Button,
        };
        dialog.Add(equipBtn);

        void RefreshDetail()
        {
            int idx = titleList.SelectedItem;
            if (idx < 0 || idx >= titleDefs.Count) return;
            var def = titleDefs[idx];
            bool unlocked = player.UnlockedTitleIds.Contains(def.Id);
            bool active = player.ActiveTitleId == def.Id;

            detailName.Text = unlocked ? def.DisplayName : "??? (Locked)";
            detailDesc.Text = unlocked ? def.Description
                : "This title has not been earned yet.";
            detailReq.Text  = def.RequirementNote != null
                ? $"Requirement: {def.RequirementNote}"
                : "";
            activeStatus.Text = active ? "Currently Equipped." : "";
            equipBtn.Text = !unlocked ? "[—] Locked"
                : active ? "[Enter] Unequip"
                : "[Enter] Equip";
        }

        void RefreshTitleList()
        {
            for (int i = 0; i < titleDefs.Count; i++)
                titleLines[i] = BuildTitleLine(titleDefs[i]);
            titleList.SetSource(new ObservableCollection<string>(titleLines));
        }

        titleList.SelectedItemChanged += (s, e) => RefreshDetail();
        equipBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            int idx = titleList.SelectedItem;
            if (idx < 0 || idx >= titleDefs.Count) return;
            var def = titleDefs[idx];
            if (!player.UnlockedTitleIds.Contains(def.Id)) return;

            if (player.ActiveTitleId == def.Id)
                TitleSystem.SetActiveTitle(player, null);
            else
                TitleSystem.SetActiveTitle(player, def.Id);

            RefreshTitleList();
            RefreshDetail();
        };

        // Pressing Enter on the title list equips/unequips too.
        titleList.OpenSelectedItem += (s, e) =>
        {
            int idx = titleList.SelectedItem;
            if (idx < 0 || idx >= titleDefs.Count) return;
            var def = titleDefs[idx];
            if (!player.UnlockedTitleIds.Contains(def.Id)) return;

            if (player.ActiveTitleId == def.Id)
                TitleSystem.SetActiveTitle(player, null);
            else
                TitleSystem.SetActiveTitle(player, def.Id);

            RefreshTitleList();
            RefreshDetail();
        };

        // Initial render.
        if (titleDefs.Count > 0) RefreshDetail();

        DialogHelper.AddCloseFooter(dialog);
        DialogHelper.RunModal(dialog);
    }
}
