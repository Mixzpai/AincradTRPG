using System.Collections.ObjectModel;
using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Cooking screen — select a recipe, check ingredients, cook. Asuna in party
// unlocks Gold/Legendary tiers. Opened from a Campfire tile via "[C]ook".
public static class CookingScreen
{
    private const int DialogWidth = 80, DialogHeight = 26;

    public static void Show(Player player, IGameLog log)
    {
        var dialog = DialogHelper.Create("Campfire Cooking", DialogWidth, DialogHeight);

        bool hasAsuna = PartySystem.Members.Any(a =>
            a.Name.Contains("Asuna", StringComparison.OrdinalIgnoreCase));

        var header = new Label
        {
            Text = hasAsuna
                ? "Asuna is here — Gold + Legendary recipes unlocked."
                : "No one with Cooking Extra Skill. Bronze + Silver only.",
            X = 2, Y = 0, Width = Dim.Fill(2),
            SchemeName = hasAsuna ? ColorSchemes.GoldName : ColorSchemes.DimName,
        };

        var names = new ObservableCollection<string>();
        var recipes = RecipeData.All.ToArray();
        foreach (var r in recipes) names.Add(FormatRecipeLine(r, player, hasAsuna));

        var listView = new ListView
        {
            X = 1, Y = 2, Width = Dim.Fill(1), Height = Dim.Fill(8),
            Source = new ListWrapper<string>(names),
            CanFocus = true,
        };

        var detailLabel = new Label
        {
            Text = "", X = 2, Y = Pos.AnchorEnd(7),
            Width = Dim.Fill(2), Height = 5, SchemeName = ColorSchemes.BodyName,
        };

        var cookBtn = DialogHelper.CreateButton("Cook", isDefault: true);
        cookBtn.X = Pos.Center();
        cookBtn.Y = Pos.AnchorEnd(2);

        var hint = new Label
        {
            Text = "Enter: cook selected  |  Esc: close",
            X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1), SchemeName = ColorSchemes.DimName,
        };

        listView.ValueChanged += (s, e) =>
        {
            int idx = listView.SelectedItem ?? -1;
            if (idx < 0 || idx >= recipes.Length) return;
            var r = recipes[idx];
            detailLabel.Text = BuildDetail(r, player, hasAsuna);
            detailLabel.SchemeName = CookingSystem.CanCook(r, player, out _) ? ColorSchemes.BodyName : ColorSchemes.DimName;
        };

        void TryCook()
        {
            int idx = listView.SelectedItem ?? -1;
            if (idx < 0 || idx >= recipes.Length) return;
            var r = recipes[idx];
            var output = CookingSystem.Cook(r, player, log);
            if (output != null)
            {
                detailLabel.Text = $"Cooked {r.Name}! Added {output.Name} to inventory.";
                detailLabel.SchemeName = ColorSchemes.GoldName;
                names[idx] = FormatRecipeLine(r, player, hasAsuna);
            }
            else
            {
                detailLabel.Text = CookingSystem.CanCook(r, player, out var reason) ? "Cooking failed." : reason;
                detailLabel.SchemeName = ColorSchemes.DimName;
            }
        }

        cookBtn.Accepting += (s, e) => { e.Handled = true; TryCook(); };
        // Enter is Command.Accept and Activated answers Command.Activate (Space); an unclaimed
        // Accept bubbles to a default button, and this dialog has two of them.
        listView.Accepting += (s, e) => { e.Handled = true; TryCook(); };
        listView.Activated += (s, e) => TryCook();

        dialog.Add(header, listView, detailLabel, cookBtn, hint);
        DialogHelper.AddCloseFooter(dialog);
        listView.SetFocus();
        DialogHelper.SelectFirstRow(listView);
        DialogHelper.RunModal(dialog);
    }

    private static string FormatRecipeLine(Recipe r, Player player, bool hasAsuna)
    {
        string tierTag = r.Tier switch
        {
            RecipeTier.Bronze    => "[Bronze]   ",
            RecipeTier.Silver    => "[Silver]   ",
            RecipeTier.Gold      => "[Gold]     ",
            RecipeTier.Legendary => "[Legendary]",
            _                     => "           ",
        };
        bool canCook = CookingSystem.CanCook(r, player, out _);
        string status = canCook ? "✓" : "✗";
        return $" {status} {tierTag} {r.Name}";
    }

    private static string BuildDetail(Recipe r, Player player, bool hasAsuna)
    {
        var ingredients = string.Join(", ", r.Inputs.Select(i =>
        {
            int have = player.Inventory.CountByDefinitionId(i.DefId);
            var sample = ItemRegistry.Create(i.DefId);
            string nm = sample?.Name ?? i.DefId;
            return $"{nm} ({have}/{i.Count})";
        }));
        return $"{r.Description}\nNeeds: {ingredients}";
    }
}
