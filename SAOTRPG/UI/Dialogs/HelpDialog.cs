using Terminal.Gui;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Keybinding reference (H). Two-column: controls left, legend right. Cogmind-style essentials first, advanced below.
// Bundle 10 (B10): ReadOnly TextView (was Label) — scrolls with arrow/PgUp/PgDn so content grows without cramping.
public static class HelpDialog
{
    private const int DialogWidth = 78, DialogHeight = 30;

    // Left column: controls (40 chars). Right column: legend (34 chars). Free
    // to grow vertically — TextView paginates beyond the viewport.
    private const string Content = @"
 [ Essential Controls ]                [ Map Legend ]

  WASD / Arrows .. Move                 .  Floor/Path    #  Wall
  Q E Z C ........ Diagonals            ~  Water         =  Deep Water
  Space .......... Wait a turn          T  Tree          ;  Bush
  G .............. Pick up items        &  Campfire      O  Fountain
  I .............. Inventory            +  Door/Shrine   >  Stairs Up
  Bump ........... Attack / Talk        @  You           B  Boss
                                        V  Vendor        N  NPC
 [ Combat ]                             *  Ore vein      ▒  Cracked wall

  Bump enemy ..... Basic attack        [ Items on Ground ]
  F1-F4 .......... Sword Skills
  0-9 ............ Use quickbar slot    .  Common        o  Uncommon
  Shift+0-9 ...... Bind consumable      +  Rare          *  Legendary
                   (in inventory)
  V .............. Counter stance      [ Mob Symbols ]
  Shift+Dir ...... Sprint (2 tiles)
  Ctrl+Dir ....... Stealth move         lowercase  Normal mob
  Combos ......... 5-hit = x2 dmg       UPPERCASE  Elite mob

 [ Menus & Screens ]                   [ Status Tags ]

  F .............. Sword Skills         !PSN  Poison     !BLD  Bleed
  J .............. Quest Journal        !STN  Stun       vSLW  Slow
  P .............. Stats / Levels       +SRG  Surge      +BLS  Shrine
  P (Quest Log) .. Pin / unpin quest    +FED  Well Fed   ~FTG  Fatigue
  T .............. Equipment
  K .............. Kill Stats          Shift+S toggles status-tray
  Y .............. Bestiary            verbose labels (session only).
  H .............. This help
  R .............. Rest (3 turns)      [ Day/Night ]
  X .............. Auto-explore
  L .............. Look mode            Vision shrinks at night.
  F5 ............. Save game            Your torch provides light.
  1-5 ............ Quick-use item       Campfires glow in the dark.
  Tab ............ Cycle log tabs

 [ Life Skills ]                       [ Crafting ]

  Eating ......... Satiety + regen      Step on Anvil to repair or
                   from food            enhance equipment (+1 to +10).
  Walking ........ Stamina; less        Materials come from mob drops.
                   fatigue penalty
  Mining ......... Ore yield + speed   [ Mining ]
                   from veins
  Cooking ........ Better recipes       Equip a Pickaxe in the Tool
                   from raw drops       slot (T menu, second row).

 [ Tool Slot ]                          Bump an ore vein (*) to mine:
                                          Iron     - 3 strikes default
  Pickaxes equip in the Tool slot.       Mithril  - 5 strikes default
  Bump-mine ore veins (*) to gather      Divine   - 8 strikes default
  ingots and shards. Higher MiningPower
  reduces strikes per vein. Pickaxes     MiningPower (gear stat) cuts
  degrade with use — repair at anvils.   strikes; min 1 per node.

 [ Tips ]                               [ Notes ]

  - Weapon proficiency grows as you      Death is permanent — save
    kill with a specific weapon type.    is deleted on game-over.
  - Higher proficiency unlocks new
    Sword Skills (press F to equip).     Talk to NPCs for quests
  - Talk to NPCs for quests and to       and to turn in completed
    turn in completed ones.              ones.
  - Enhanced weapons show +N suffix.
  - Death is permanent — save deleted.
";

    public static void Show()
    {
        var dialog = DialogHelper.Create("Help -- Controls & Reference", DialogWidth, DialogHeight);

        // Bundle 11 — top search bar. Typing filters body to lines containing
        // the substring, plus their parent section header for context. Esc
        // empties the field (and on second Esc closes via DialogHelper).
        var searchPrompt = new Label
        {
            Text = "Search:", X = 1, Y = 0, Width = 8, ColorScheme = ColorSchemes.Gold,
        };
        var searchField = new TextField
        {
            X = 9, Y = 0, Width = Dim.Fill(2),
            ColorScheme = ColorSchemes.Body,
        };

        // ReadOnly TextView so the body scrolls vertically with arrow keys /
        // PageUp / PageDown. Word-wrap off preserves the two-column layout.
        var contentView = new TextView
        {
            Text = Content,
            X = 0, Y = 1,
            Width = Dim.Fill(), Height = Dim.Fill(2),
            ReadOnly = true, WordWrap = false,
            ColorScheme = ColorSchemes.Body,
        };

        var hint = new Label
        {
            Text = "Type: filter   ↑↓ PgUp PgDn: scroll   Esc: clear/close",
            X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1), ColorScheme = ColorSchemes.Dim,
        };

        // Recompute body when search text changes. Empty query restores full Content.
        searchField.TextChanged += (s, e) =>
        {
            string q = (searchField.Text ?? "").Trim();
            contentView.Text = q.Length == 0 ? Content : FilterContent(q);
        };

        // Esc clears the field first; if already empty, fall through to dialog close.
        searchField.KeyDown += (s, e) =>
        {
            if (e.KeyCode != KeyCode.Esc) return;
            string txt = searchField.Text ?? "";
            if (txt.Length == 0) return; // Let DialogHelper.CloseOnEscape handle.
            searchField.Text = "";
            contentView.Text = Content;
            e.Handled = true;
        };

        dialog.Add(searchPrompt, searchField, contentView, hint);
        DialogHelper.AddCloseFooter(dialog);
        // Focus search bar so typing flows immediately. ↓ + Tab move focus to body.
        searchField.SetFocus();
        DialogHelper.RunModal(dialog);
    }

    // Filters Content to lines containing the case-insensitive substring,
    // preserving the most-recent section header (lines starting with " [")
    // so context never gets lost. Empty section → header gets dropped on the
    // way out (no orphan headers).
    private static string FilterContent(string query)
    {
        var lines = Content.Split('\n');
        var output = new List<string>();
        string? pendingHeader = null;
        bool sectionEmitted = false;

        foreach (var line in lines)
        {
            bool isHeader = line.TrimStart().StartsWith("[ ") || line.TrimStart().StartsWith("[ ");
            if (isHeader)
            {
                pendingHeader = line;
                sectionEmitted = false;
                continue;
            }
            if (line.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                if (pendingHeader != null && !sectionEmitted)
                {
                    if (output.Count > 0) output.Add("");
                    output.Add(pendingHeader);
                    sectionEmitted = true;
                }
                output.Add(line);
            }
        }

        if (output.Count == 0) return $"\n  No matches for \"{query}\".";
        return string.Join('\n', output);
    }
}
