using System.Collections.ObjectModel;
using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Standalone Milestones panel — opened with Shift+M from anywhere on the map.
// Shift+L re-routes here pre-focused on the Collectables tab. Same render core as
// MonumentDialog (which adds Bestiary kill log + NPC framing on top).
public static class MilestonesDialog
{
    public static void Show(Player player, string? initialCategory = null)
    {
        int screenW = Application.Screen.Width;
        int screenH = Application.Screen.Height;
        int dlgW = Math.Min(Math.Max(96, screenW - 6), 130);
        int dlgH = Math.Min(Math.Max(32, screenH - 4), 46);

        var dialog = DialogHelper.Create("Milestones", dlgW, dlgH);

        var headerLabel = new Label
        {
            Text = "[ Milestones ]",
            X = Pos.Center(), Y = 0,
            ColorScheme = ColorSchemes.Gold,
        };
        dialog.Add(headerLabel);

        MilestoneTabbedView.Build(dialog, player, initialCategory);

        DialogHelper.AddCloseFooter(dialog);
        DialogHelper.RunModal(dialog);
    }
}

// Shared internal tabbed renderer used by MilestonesDialog and MonumentDialog.
// 10 category tabs across the top; each tab filters MilestoneRegistry.All by Category
// and renders a sorted list (unlocked first, locked dimmed below). Equippable rows
// support Equip/Unequip via E/U keys when focused. Collectables tab adds a bucket
// dropdown filter (LN/AL/IF/HF/LR/MD/FD/Myth/Non-Canon).
internal static class MilestoneTabbedView
{
    // Canonical 10-tab order. Match strings to Milestone.Category exactly.
    private static readonly string[] TabOrder =
    {
        "Combat", "Floor", "Life Skills", "Story", "Canon",
        "Discovery", "Equipment", "Karma", "Death", "Collectables",
    };

    // Bucket order: lore source / canon era. Matches the ordering players
    // remember from the prior Collectables panel.
    private static readonly string[] BucketOrder =
        { "All", "LN", "AL", "IF", "HF", "LR", "MD", "FD", "Myth", "Non-Canon" };

    private static readonly Dictionary<string, string> BucketLabels = new()
    {
        ["All"] = "All buckets",
        ["LN"] = "Light Novel",
        ["AL"] = "Alicization Lycoris",
        ["IF"] = "Integral Factor",
        ["HF"] = "Hollow Fragment",
        ["LR"] = "Last Recollection / Lost Song",
        ["MD"] = "Memory Defrag",
        ["FD"] = "Fractured Daydream",
        ["Myth"] = "Mythological",
        ["Non-Canon"] = "Non-Canon",
    };

    // Build the tab strip + content area inside `parent`. Placement:
    //   y=2..3   tab buttons (two rows of 5)
    //   y=5      tally line
    //   y=6      bucket filter row (Collectables only) OR IF progress bar (Discovery only)
    //   y=7      HF progress bar (Discovery only)
    //   y=8..-5  list view
    //   y=-5..-3 detail panel + equip hint
    public static void Build(View parent, Player player, string? initialCategory)
    {
        var state = new TabState
        {
            Player = player,
            Current = ResolveInitialTab(initialCategory),
        };

        // ── Tab strip (2 rows of 5) ───────────────────────────────────
        // Buttons placed in a flow row; CreateMenuButton gives the ► X ◄ focus marker.
        var tabButtons = new Button[TabOrder.Length];
        int row1Y = 2;
        int row2Y = 3;
        int xCursor1 = 1;
        int xCursor2 = 1;
        for (int i = 0; i < TabOrder.Length; i++)
        {
            string cat = TabOrder[i];
            int idx = i;
            var btn = DialogHelper.CreateMenuButton(cat, isDefault: cat == state.Current);
            if (i < 5)
            {
                btn.X = xCursor1;
                btn.Y = row1Y;
                xCursor1 += cat.Length + 6;
            }
            else
            {
                btn.X = xCursor2;
                btn.Y = row2Y;
                xCursor2 += cat.Length + 6;
            }
            btn.Accepting += (s, e) =>
            {
                e.Cancel = true;
                SwitchTab(state, TabOrder[idx]);
            };
            tabButtons[i] = btn;
            parent.Add(btn);
        }
        state.TabButtons = tabButtons;

        // ── Tally line ────────────────────────────────────────────────
        var tallyLabel = new Label
        {
            Text = "", X = 1, Y = 5, Width = Dim.Fill(2),
            ColorScheme = ColorSchemes.Gold,
        };
        parent.Add(tallyLabel);
        state.TallyLabel = tallyLabel;

        // ── Bucket filter row (Collectables only) ─────────────────────
        // Row of buttons; selected button gets the ► ◄ marker. Hidden when
        // not on Collectables tab (set Visible=false).
        var bucketRow = new View
        {
            X = 1, Y = 6, Width = Dim.Fill(2), Height = 1,
            CanFocus = false,
        };
        var bucketButtons = new Button[BucketOrder.Length];
        int bx = 0;
        for (int i = 0; i < BucketOrder.Length; i++)
        {
            string b = BucketOrder[i];
            int idx = i;
            var bb = DialogHelper.CreateMenuButton(b, isDefault: b == state.Bucket);
            bb.X = bx;
            bb.Y = 0;
            bb.Accepting += (s, e) =>
            {
                e.Cancel = true;
                state.Bucket = BucketOrder[idx];
                RefreshBucketRow(state);
                RefreshList(state);
            };
            bucketButtons[i] = bb;
            bucketRow.Add(bb);
            bx += b.Length + 4;
        }
        state.BucketRow = bucketRow;
        state.BucketButtons = bucketButtons;
        parent.Add(bucketRow);

        // ── Discovery tab progress bars (IF Implement / HF Mission) ────
        // Visible only when the Discovery tab is active. Sits in the same
        // y=6/y=7 strip the Collectables bucket row uses; the two are mutually
        // exclusive because they live on different tabs.
        // PATH-D-PORT: pure Label text — block-character bars (█/░) draw via
        // standard ColorScheme paint; no custom AddRune calls.
        var ifProgress = new Label
        {
            Text = "", X = 1, Y = 6, Width = Dim.Fill(2),
            ColorScheme = ColorSchemes.FromColor(Color.BrightCyan),
            Visible = false,
        };
        var hfProgress = new Label
        {
            Text = "", X = 1, Y = 7, Width = Dim.Fill(2),
            ColorScheme = ColorSchemes.FromColor(Color.BrightYellow),
            Visible = false,
        };
        parent.Add(ifProgress, hfProgress);
        state.IfProgressBar = ifProgress;
        state.HfProgressBar = hfProgress;

        // ── List view ─────────────────────────────────────────────────
        var listView = new ListView
        {
            X = 1, Y = 8, Width = Dim.Fill(2), Height = Dim.Fill(6),
            ColorScheme = ColorSchemes.ListSelection,
            CanFocus = true,
        };
        // PATH-D-PORT: ListView uses ObservableCollection<string> source; replace each refresh.
        parent.Add(listView);
        state.ListView = listView;

        // ── Detail / equip hint ───────────────────────────────────────
        var detailLine = new Label
        {
            Text = "", X = 1, Y = Pos.AnchorEnd(5), Width = Dim.Fill(2),
            ColorScheme = ColorSchemes.Body,
        };
        var rewardLine = new Label
        {
            Text = "", X = 1, Y = Pos.AnchorEnd(4), Width = Dim.Fill(2),
            ColorScheme = ColorSchemes.Dim,
        };
        var equipHint = new Label
        {
            Text = "", X = 1, Y = Pos.AnchorEnd(3), Width = Dim.Fill(2),
            ColorScheme = ColorSchemes.Gold,
        };
        parent.Add(detailLine, rewardLine, equipHint);
        state.DetailLine = detailLine;
        state.RewardLine = rewardLine;
        state.EquipHint = equipHint;

        // ── List interactions ─────────────────────────────────────────
        listView.SelectedItemChanged += (s, e) => RefreshDetail(state);

        // PATH-D-PORT: KeyDown handler on ListView for Equip/Unequip hotkeys (E / U).
        // Tab/Shift+Tab cycles tabs; 1-9/0 jumps directly to a tab.
        listView.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.E)
            {
                TryEquipFocused(state);
                e.Handled = true;
            }
            else if (e.KeyCode == KeyCode.U)
            {
                TryUnequipFocused(state);
                e.Handled = true;
            }
        };

        // Dialog-level key dispatch — Tab cycles tabs, digits jump.
        parent.KeyDown += (s, e) => HandleHotKey(state, e);

        // Initial render.
        RefreshBucketRow(state);
        UpdateTabButtons(state);
        RefreshList(state);
    }

    // ── State ──────────────────────────────────────────────────────────

    private sealed class TabState
    {
        public Player Player = null!;
        public string Current = "Combat";
        public string Bucket = "All";
        public Button[] TabButtons = Array.Empty<Button>();
        public Button[] BucketButtons = Array.Empty<Button>();
        public View BucketRow = null!;
        public ListView ListView = null!;
        public Label TallyLabel = null!;
        public Label DetailLine = null!;
        public Label RewardLine = null!;
        public Label EquipHint = null!;
        public Label IfProgressBar = null!;
        public Label HfProgressBar = null!;
        public List<Milestone> CurrentRows = new();
    }

    private static string ResolveInitialTab(string? initialCategory)
    {
        if (initialCategory != null && Array.IndexOf(TabOrder, initialCategory) >= 0)
            return initialCategory;
        return "Combat";
    }

    private static void SwitchTab(TabState state, string newTab)
    {
        if (state.Current == newTab) return;
        state.Current = newTab;
        UpdateTabButtons(state);
        RefreshBucketRow(state);
        RefreshList(state);
    }

    private static void UpdateTabButtons(TabState state)
    {
        for (int i = 0; i < TabOrder.Length; i++)
        {
            bool isActive = TabOrder[i] == state.Current;
            var b = state.TabButtons[i];
            // Trigger CreateMenuButton's own focus marker logic by setting IsDefault.
            b.IsDefault = isActive;
            // Manually rewrite text so the inactive ones lose the ► ◄ markers.
            // PATH-D-PORT: button text rewrite mimics CreateMenuButton's HasFocusChanged hook.
            string core = TabOrder[i];
            b.Text = isActive ? $"► {core} ◄" : $"  {core}  ";
        }
    }

    private static void RefreshBucketRow(TabState state)
    {
        bool show = state.Current == "Collectables";
        state.BucketRow.Visible = show;
        if (show)
        {
            for (int i = 0; i < BucketOrder.Length; i++)
            {
                bool active = BucketOrder[i] == state.Bucket;
                var b = state.BucketButtons[i];
                b.IsDefault = active;
                string core = BucketOrder[i];
                b.Text = active ? $"► {core} ◄" : $"  {core}  ";
            }
        }

        RefreshDiscoveryProgress(state);
    }

    // Update the IF/HF Discovery-tab progress bars. Visible only on Discovery.
    // Pulls per-run counters from MilestoneSystem.ActiveTurnManager when a run
    // is loaded; falls back to "--" when called from a non-run context (e.g.
    // Monument viewed before the run-state hook lands).
    private static void RefreshDiscoveryProgress(TabState state)
    {
        bool show = state.Current == "Discovery";
        state.IfProgressBar.Visible = show;
        state.HfProgressBar.Visible = show;
        if (!show) return;

        var tm = MilestoneSystem.ActiveTurnManager;
        int? ifCount = tm?.IfImplementQuestsCompletedThisRun;
        int? hfCount = tm?.HfMissionsCompletedThisRun;

        state.IfProgressBar.Text = FormatProgressBar("IF Implement Research", ifCount, threshold: 100);
        state.HfProgressBar.Text = FormatProgressBar("HF Missions          ", hfCount, threshold: 80);
    }

    // 20-cell block-character bar. `count` null when no active run → "--".
    private static string FormatProgressBar(string label, int? count, int threshold)
    {
        const int barWidth = 20;
        int c = count ?? 0;
        int filled = Math.Clamp(c * barWidth / Math.Max(1, threshold), 0, barWidth);
        string bar = new string('█', filled) + new string('░', barWidth - filled);
        string countText = count.HasValue ? $"{c,3}/{threshold,-3}" : $" --/{threshold,-3}";
        return $"  {label}  [{bar}]  {countText}";
    }

    // Build the visible list given the current tab + bucket. Sort: unlocked first,
    // locked dimmed below. SpoilerHidden + locked → "???" rows.
    private static void RefreshList(TabState state)
    {
        var registry = MilestoneRegistry.All;
        var rows = new List<Milestone>();
        foreach (var m in registry)
        {
            if (m.Category != state.Current) continue;
            if (state.Current == "Collectables" && state.Bucket != "All")
            {
                // Threshold milestones (CollectableCount; SubCategory == null) show in every bucket.
                if (m.SubCategory != null && m.SubCategory != state.Bucket) continue;
            }
            rows.Add(m);
        }

        // Sort: unlocked first; within each group preserve registry order.
        rows.Sort((a, b) =>
        {
            bool ua = LifetimeStats.IsMilestoneUnlocked(a.Id);
            bool ub = LifetimeStats.IsMilestoneUnlocked(b.Id);
            if (ua != ub) return ua ? -1 : 1;
            return 0;
        });

        state.CurrentRows = rows;

        var lines = new List<string>();
        int unlockedCount = 0;
        foreach (var m in rows)
        {
            bool unlocked = LifetimeStats.IsMilestoneUnlocked(m.Id);
            if (unlocked) unlockedCount++;
            lines.Add(FormatRow(m, unlocked, state.Player));
        }

        state.ListView.SetSource(new ObservableCollection<string>(lines));
        UpdateTallyLabel(state, unlockedCount, rows.Count);
        RefreshDetail(state);
    }

    private static void UpdateTallyLabel(TabState state, int unlocked, int total)
    {
        // Total-tally across all categories, plus current-tab tally + active title.
        int globalUnlocked = 0;
        int globalTotal = MilestoneRegistry.All.Count;
        foreach (var m in MilestoneRegistry.All)
            if (LifetimeStats.IsMilestoneUnlocked(m.Id)) globalUnlocked++;

        int pct = globalTotal == 0 ? 0 : (int)(100.0 * globalUnlocked / globalTotal);
        string activeTitle = "(none equipped)";
        if (state.Player.ActiveTitleId != null
            && MilestoneRegistry.ById.TryGetValue(state.Player.ActiveTitleId, out var act))
        {
            activeTitle = act.Name;
        }

        state.TallyLabel.Text =
            $"{state.Current}: {unlocked}/{total}   |   Total {globalUnlocked}/{globalTotal} ({pct}%)   |   Active title: {activeTitle}";
    }

    // Single-line formatter for the list view. Marker glyph chosen by reward type.
    // Spoiler-hidden + locked → "???".
    private static string FormatRow(Milestone m, bool unlocked, Player player)
    {
        bool active = unlocked && player.ActiveTitleId == m.Id;
        bool maskName = !unlocked && m.SpoilerHidden;
        string marker = (m.Reward, active) switch
        {
            (_, true)                            => "★",  // active equipped title
            (RewardType.EquippableTitle, _)      => unlocked ? "[E]" : " · ",
            (RewardType.Col, _)                  => unlocked ? "✦"  : " · ",
            (RewardType.AutoPassive, _)          => unlocked ? "—"  : " · ",
            (RewardType.DisplayOnly, _)          => unlocked ? "◆"  : " · ",
            _                                    => " · ",
        };
        string name = maskName ? "???" : m.Name;
        string desc = maskName
            ? "??? — keep playing to discover this milestone"
            : m.Description;
        // Truncate to keep one-line layout stable.
        if (desc.Length > 60) desc = desc.Substring(0, 57) + "...";
        return $"  {marker} {name,-32} {desc}";
    }

    private static void RefreshDetail(TabState state)
    {
        int idx = state.ListView.SelectedItem;
        if (idx < 0 || idx >= state.CurrentRows.Count)
        {
            state.DetailLine.Text = "";
            state.RewardLine.Text = "";
            state.EquipHint.Text = "";
            return;
        }
        var m = state.CurrentRows[idx];
        bool unlocked = LifetimeStats.IsMilestoneUnlocked(m.Id);
        bool maskName = !unlocked && m.SpoilerHidden;
        bool active = unlocked && state.Player.ActiveTitleId == m.Id;

        state.DetailLine.Text = maskName
            ? "???"
            : $"{m.Name} — {m.Description}";
        state.RewardLine.Text = unlocked
            ? FormatReward(m)
            : (maskName ? "Locked." : $"Locked. Requirement: {FormatRequirement(m)}");

        if (m.Reward == RewardType.EquippableTitle && unlocked)
        {
            state.EquipHint.Text = active
                ? "[U] Unequip this title"
                : "[E] Equip this title";
        }
        else
        {
            state.EquipHint.Text = "";
        }
    }

    private static string FormatReward(Milestone m) => m.Reward switch
    {
        RewardType.Col              => $"Reward: +{m.ColReward} Col",
        RewardType.EquippableTitle  => m.BonusStat is StatType s
            ? $"Title — +{m.BonusValue} {s}{(m.VsTag != null ? $" vs {m.VsTag}" : "")}"
            : "Title (bonus pending)",
        RewardType.AutoPassive      => "Passive bonus active",
        RewardType.DisplayOnly      => "Display only",
        _                           => "",
    };

    private static string FormatRequirement(Milestone m) => m.Trigger switch
    {
        TriggerType.KillCountTotal      => $"{m.TriggerThreshold} total kills",
        TriggerType.KillCountByTag      => $"{m.TriggerThreshold} {m.TriggerArg} kills",
        TriggerType.KillCountBySpecies  => $"{m.TriggerThreshold} {m.TriggerArg} kills",
        TriggerType.FloorReached        => $"Reach floor {m.TriggerThreshold}",
        TriggerType.LifeSkillLevel      => $"{m.TriggerArg} skill level {m.TriggerThreshold}",
        TriggerType.LegendaryCollected  => $"Collect {m.TriggerArg}",
        TriggerType.CollectableCount    => $"Collect {m.TriggerThreshold} Legendaries",
        TriggerType.LoreCollected       => "Discover all lore stones",
        TriggerType.BountyCompleted     => "Complete a bounty",
        TriggerType.QuestCompleted      => $"Complete quest: {m.TriggerArg}",
        TriggerType.GuildJoined         => $"Join guild: {m.TriggerArg}",
        TriggerType.AllyRecruited       => $"Recruit ally: {m.TriggerArg}",
        TriggerType.UniqueSkillUnlocked => $"Unlock unique skill: {m.TriggerArg}",
        TriggerType.DivineAwakened      => "Awaken a Divine weapon",
        TriggerType.KarmaThreshold      => $"Karma {m.TriggerThreshold}",
        TriggerType.BestiaryCount       => $"Discover {m.TriggerThreshold} bestiary entries",
        TriggerType.FirstEvent          => "First-time event",
        TriggerType.Conditional         => "Conditional unlock",
        _                               => $"{m.Trigger}",
    };

    private static void TryEquipFocused(TabState state)
    {
        int idx = state.ListView.SelectedItem;
        if (idx < 0 || idx >= state.CurrentRows.Count) return;
        var m = state.CurrentRows[idx];
        if (m.Reward != RewardType.EquippableTitle) return;
        if (!LifetimeStats.IsMilestoneUnlocked(m.Id)) return;
        if (state.Player.ActiveTitleId == m.Id) return;
        MilestoneSystem.SetActiveTitle(state.Player, m.Id);
        RefreshList(state);
    }

    private static void TryUnequipFocused(TabState state)
    {
        int idx = state.ListView.SelectedItem;
        if (idx < 0 || idx >= state.CurrentRows.Count) return;
        var m = state.CurrentRows[idx];
        if (m.Reward != RewardType.EquippableTitle) return;
        if (state.Player.ActiveTitleId != m.Id) return;
        MilestoneSystem.ClearActiveTitle(state.Player);
        RefreshList(state);
    }

    // Tab cycle (Tab / Shift+Tab) and digit jump (1-9, 0=tab10).
    private static void HandleHotKey(TabState state, Key e)
    {
        var bareKey = e.KeyCode & ~KeyCode.ShiftMask & ~KeyCode.CtrlMask & ~KeyCode.AltMask;

        // Tab forward / Shift+Tab back across the 10 tabs.
        if (bareKey == KeyCode.Tab)
        {
            int curIdx = Array.IndexOf(TabOrder, state.Current);
            if (curIdx < 0) curIdx = 0;
            int next = e.IsShift
                ? (curIdx - 1 + TabOrder.Length) % TabOrder.Length
                : (curIdx + 1) % TabOrder.Length;
            SwitchTab(state, TabOrder[next]);
            e.Handled = true;
            return;
        }

        // Digit 1..9 → tabs 0..8. Digit 0 → tab 9 (Collectables).
        int? jump = bareKey switch
        {
            KeyCode.D1 => 0,
            KeyCode.D2 => 1,
            KeyCode.D3 => 2,
            KeyCode.D4 => 3,
            KeyCode.D5 => 4,
            KeyCode.D6 => 5,
            KeyCode.D7 => 6,
            KeyCode.D8 => 7,
            KeyCode.D9 => 8,
            KeyCode.D0 => 9,
            _ => (int?)null,
        };
        if (jump is int j)
        {
            SwitchTab(state, TabOrder[j]);
            e.Handled = true;
        }
    }
}
