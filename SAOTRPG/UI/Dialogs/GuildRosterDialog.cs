using System.Collections.ObjectModel;
using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Systems;
using SAOTRPG.Systems.Story;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// FB-063 Guild Roster. Read-only browser for the 8 SAO-canon guilds +
// a "Found Your Own Guild" action at the bottom. Accessible from
// StatsDialog. Active guild is highlighted; locked guilds show the
// failing requirement. Joining itself happens via the recruiter NPC on
// each guild's HQ floor — this dialog only surfaces the info.
public static class GuildRosterDialog
{
    private const int DialogWidth = 78;
    private const int DialogHeight = 26;

    public static void Show(Player player)
    {
        var dialog = DialogHelper.Create("Guild Roster", DialogWidth, DialogHeight);

        var header = new Label
        {
            Text = $"Karma: {player.Karma} [{KarmaSystem.TierLabel(player.Karma)}]   "
                 + $"Active: {GuildSystem.ActiveGuildDisplayName(player)}",
            X = 1, Y = 0, Width = Dim.Fill(1),
            ColorScheme = ColorSchemes.Gold,
        };

        var names = new ObservableCollection<string>();
        var ids = new List<Faction>();
        foreach (var def in GuildSystem.Guilds.Values)
        {
            string statusTag;
            if (player.ActiveGuildId == def.Id) statusTag = "[MEMBER]";
            else
            {
                var (ok, _) = GuildSystem.CanJoin(player, def);
                statusTag = ok ? "[ELIGIBLE]" : "[LOCKED]";
            }
            string fateTag = def.IsFateSealed ? " (FATE-SEALED)" : "";
            int rep = StorySystem.GetRep(def.Id);
            names.Add($"  {statusTag,-10} {def.DisplayName,-26} HQ: F{def.HqFloor,-3} Rep:{rep,+4}{fateTag}");
            ids.Add(def.Id);
        }

        var listView = new ListView
        {
            X = 0, Y = 2, Width = Dim.Fill(), Height = Dim.Fill(10),
            Source = new ListWrapper<string>(names),
        };

        var detail = new Label
        {
            Text = "",
            X = 1, Y = Pos.AnchorEnd(9),
            Width = Dim.Fill(1), Height = 4,
        };
        var reqLabel = new Label
        {
            Text = "",
            X = 1, Y = Pos.AnchorEnd(5),
            Width = Dim.Fill(1), Height = 2,
            ColorScheme = ColorSchemes.Dim,
        };

        void Refresh()
        {
            int idx = listView.SelectedItem;
            if (idx < 0 || idx >= ids.Count) { detail.Text = ""; reqLabel.Text = ""; return; }
            if (!GuildSystem.Guilds.TryGetValue(ids[idx], out var def)) return;
            detail.Text =
                $"{def.DisplayName} — Leader: {def.CanonLeader}   HQ: {def.HqName}\n"
              + $"Recruiter: {def.RecruiterName}   Description: {def.Description}\n"
              + $"Perk: {def.PerkFlavor}";
            var (ok, reason) = GuildSystem.CanJoin(player, def);
            string memTag = player.ActiveGuildId == def.Id ? "You are a member." : reason;
            reqLabel.Text = $"Requirements: Lv.{def.MinLevel}, karma {def.MinKarma}..{def.MaxKarma}\n{memTag}";
        }
        listView.SelectedItemChanged += (s, e) => Refresh();

        var foundBtn = DialogHelper.CreateButton("Found Your Own Guild");
        foundBtn.X = 1;
        foundBtn.Y = Pos.AnchorEnd(3);
        foundBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            // Already in a guild? Require leaving first.
            if (player.ActiveGuildId == Faction.PlayerGuild)
            {
                MessageBox.Query("Founded Guild", "You already lead a founded guild. Use the Dissolve button to step down first.", "OK");
                return;
            }
            if (player.ActiveGuildId != Faction.None)
            {
                int c = MessageBox.Query("Leave Current Guild?",
                    "Founding a new guild requires leaving your current one (-10 rep, -3 karma). Proceed?",
                    "Yes", "Cancel");
                if (c != 0) return;
            }
            if (player.ColOnHand < GuildSystem.PlayerGuildFoundCost)
            {
                MessageBox.Query("Not Enough Col",
                    $"Founding a guild costs {GuildSystem.PlayerGuildFoundCost} Col. You have {player.ColOnHand}.", "OK");
                return;
            }
            string? name = PromptForName();
            if (string.IsNullOrWhiteSpace(name)) return;
            int preset = PromptForPreset();
            if (preset < 0) return;

            // Deduct cost, set founded metadata, Join via GuildSystem.
            player.ColOnHand -= GuildSystem.PlayerGuildFoundCost;
            player.FoundedGuildName = name!.Trim();
            player.FoundedGuildPerk = preset;
            var silentLog = new StringGameLog(new System.Text.StringBuilder());
            // Leave current guild quietly first (Join does this, but we route
            // via a throwaway string log so the player doesn't see the old-
            // guild -10 rep / -3 karma log lines in a channel they can't see
            // (the modal intercepts the game-log). Actual leave-penalty is
            // still applied to state — only the log output is discarded.
            GuildSystem.Join(player, Faction.PlayerGuild, silentLog);
            MessageBox.Query("Guild Founded",
                $"{name} is born!\nPerk: {GuildSystem.FoundedPresets[preset].Flavor}\nCost: {GuildSystem.PlayerGuildFoundCost} Col",
                "For glory!");
            Application.RequestStop();
        };

        dialog.Add(header, listView, detail, reqLabel, foundBtn);

        // Dissolve button — visible only when player leads a founded guild.
        // Calls GuildSystem.Leave (which has a clean PlayerGuild branch:
        // removes perk, clears ActiveGuildId, no karma/rep penalty since
        // there's nothing for the player to betray by disbanding their
        // own crew). Clears the stored guild name so the player can re-
        // found later with a fresh name.
        if (player.ActiveGuildId == Faction.PlayerGuild)
        {
            var dissolveBtn = DialogHelper.CreateButton("Dissolve Guild");
            dissolveBtn.X = Pos.Right(foundBtn) + 2;
            dissolveBtn.Y = Pos.AnchorEnd(3);
            dissolveBtn.Accepting += (s, e) =>
            {
                e.Cancel = true;
                string guildName = player.FoundedGuildName ?? "your guild";
                int confirm = MessageBox.Query("Dissolve Guild",
                    $"Disband {guildName}? The perk is lost. No rep or karma penalty.",
                    "Dissolve", "Cancel");
                if (confirm != 0) return;
                var silentLog = new StringGameLog(new System.Text.StringBuilder());
                GuildSystem.Leave(player, silentLog, silent: true);
                player.FoundedGuildName = null;
                player.FoundedGuildPerk = 0;
                MessageBox.Query("Guild Dissolved",
                    $"{guildName} is no more. The banner falls.", "OK");
                Application.RequestStop();
            };
            dialog.Add(dissolveBtn);
        }

        DialogHelper.AddCloseFooter(dialog);
        if (names.Count > 0) { listView.SelectedItem = 0; Refresh(); }
        DialogHelper.RunModal(dialog);
    }

    // Simple modal text prompt for the founded-guild name. Enforces 1..20
    // chars, alphanumeric + space only (per FB-063 rule 7). Returns null on
    // cancel or invalid input.
    private static string? PromptForName()
    {
        var d = DialogHelper.Create("Name Your Guild", 52, 9);
        var hint = new Label
        {
            Text = $"1-{GuildSystem.PlayerGuildNameMaxLen} chars, letters/digits/space only.",
            X = 1, Y = 1, ColorScheme = ColorSchemes.Dim,
        };
        var field = new TextField
        {
            X = 1, Y = 3, Width = Dim.Fill(2),
        };
        string? result = null;
        var okBtn = DialogHelper.CreateButton("Create", isDefault: true);
        okBtn.X = Pos.Center() - 8; okBtn.Y = Pos.AnchorEnd(2);
        okBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            string raw = field.Text?.ToString()?.Trim() ?? "";
            if (raw.Length == 0 || raw.Length > GuildSystem.PlayerGuildNameMaxLen) return;
            foreach (char c in raw)
                if (!char.IsLetterOrDigit(c) && c != ' ') return;
            result = raw;
            Application.RequestStop();
        };
        var cancelBtn = DialogHelper.CreateButton("Cancel");
        cancelBtn.X = Pos.Center() + 2; cancelBtn.Y = Pos.AnchorEnd(2);
        cancelBtn.Accepting += (s, e) => { e.Cancel = true; Application.RequestStop(); };
        d.Add(hint, field, okBtn, cancelBtn);
        DialogHelper.CloseOnEscape(d);
        DialogHelper.RunModal(d);
        return result;
    }

    // Radio-list picker for the 5 player-guild presets. Returns -1 on cancel.
    private static int PromptForPreset()
    {
        var d = DialogHelper.Create("Choose a Perk", 58, 13);
        var hint = new Label { Text = "Your founded guild's signature bonus:", X = 1, Y = 0 };
        var presets = GuildSystem.FoundedPresets;
        var rg = new RadioGroup
        {
            X = 1, Y = 2,
            RadioLabels = presets.Select(p => $"{p.Name} — {p.Flavor}").ToArray(),
        };
        int result = -1;
        var okBtn = DialogHelper.CreateButton("Select", isDefault: true);
        okBtn.X = Pos.Center() - 8; okBtn.Y = Pos.AnchorEnd(2);
        okBtn.Accepting += (s, e) => { e.Cancel = true; result = rg.SelectedItem; Application.RequestStop(); };
        var cancelBtn = DialogHelper.CreateButton("Cancel");
        cancelBtn.X = Pos.Center() + 2; cancelBtn.Y = Pos.AnchorEnd(2);
        cancelBtn.Accepting += (s, e) => { e.Cancel = true; Application.RequestStop(); };
        d.Add(hint, rg, okBtn, cancelBtn);
        DialogHelper.CloseOnEscape(d);
        DialogHelper.RunModal(d);
        return result;
    }
}
