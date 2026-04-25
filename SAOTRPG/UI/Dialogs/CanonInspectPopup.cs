using Terminal.Gui;
using SAOTRPG.Items;
using SAOTRPG.Items.Equipment;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Lore-citation inspect popup (Bundle 11). Triggered by 'L' on a selected
// inventory item — shows canon source, floor anchor, and detail blurb for
// Legendary weapons. Falls back to "Invented for AincradTRPG" / generic note
// when no entry is registered in CanonCitationData.
public static class CanonInspectPopup
{
    private const int DialogWidth = 60, DialogHeight = 14;

    public static void Show(BaseItem item)
    {
        var dialog = DialogHelper.Create("Lore — Canon Citation", DialogWidth, DialogHeight);

        string nameLine = item.Rarity is "Legendary" or "Divine"
            ? $"{item.Name}  [{item.Rarity}]" : (item.Name ?? "Unknown");

        var nameLabel = new Label
        {
            Text = nameLine, X = 1, Y = 0,
            Width = Dim.Fill(1), ColorScheme = RarityScheme(item.Rarity),
        };

        var rule = new Label
        {
            Text = DialogHelper.Separator(DialogWidth, 4),
            X = 0, Y = 1, Width = Dim.Fill(), ColorScheme = ColorSchemes.Dim,
        };

        var citation = CanonCitationData.Lookup(item.DefinitionId);
        string sourceText, anchorText, detailText;
        ColorScheme sourceScheme;

        if (citation.HasValue)
        {
            var c = citation.Value;
            sourceText = $"Source:  {c.Source}";
            anchorText = $"Anchor:  {c.FloorAnchor}";
            detailText = c.Detail;
            sourceScheme = c.Source.Contains("Invented", StringComparison.OrdinalIgnoreCase)
                ? ColorSchemes.Dim : ColorSchemes.Gold;
        }
        else if (item is EquipmentBase && item.Rarity is "Legendary" or "Divine")
        {
            sourceText = "Source:  Invented for AincradTRPG";
            anchorText = "Anchor:  (no canon citation registered)";
            detailText = "This Legendary is original to AincradTRPG — no SAO source.";
            sourceScheme = ColorSchemes.Dim;
        }
        else
        {
            sourceText = "Source:  No canon citation available";
            anchorText = "";
            detailText = "Citations are tracked for high-profile Legendary weapons.";
            sourceScheme = ColorSchemes.Dim;
        }

        var sourceLabel = new Label
        {
            Text = sourceText, X = 1, Y = 3,
            Width = Dim.Fill(1), ColorScheme = sourceScheme,
        };
        var anchorLabel = new Label
        {
            Text = anchorText, X = 1, Y = 4,
            Width = Dim.Fill(1), ColorScheme = ColorSchemes.Body,
        };

        // Detail: word-wrapped via TextView (no scroll — fits 4 lines at width 56).
        var detailView = new TextView
        {
            Text = detailText, X = 1, Y = 6,
            Width = Dim.Fill(1), Height = 4,
            ReadOnly = true, WordWrap = true,
            ColorScheme = ColorSchemes.Body,
        };

        dialog.Add(nameLabel, rule, sourceLabel, anchorLabel, detailView);
        DialogHelper.AddCloseFooter(dialog);
        DialogHelper.RunModal(dialog);
    }

    // Rarity-tinted name color so the popup header reads at a glance.
    private static ColorScheme RarityScheme(string? rarity) => rarity switch
    {
        "Legendary" => ColorSchemes.FromColor(Color.BrightMagenta),
        "Divine"    => ColorSchemes.FromColor(Color.BrightYellow),
        "Epic"      => ColorSchemes.FromColor(Color.BrightYellow),
        "Rare"      => ColorSchemes.FromColor(Color.BrightCyan),
        "Uncommon"  => ColorSchemes.FromColor(Color.BrightGreen),
        _           => ColorSchemes.Title,
    };
}
