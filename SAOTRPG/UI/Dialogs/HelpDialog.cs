using Terminal.Gui;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Keybinding reference and game guide -- opened with H key.
// Two-column layout: controls on the left, legend on the right.
// Cogmind-inspired: essential commands first, advanced below.
// Scrollable content area for all sections.
public static class HelpDialog
{
    private const int DialogWidth = 78, DialogHeight = 34;

    // Left column: controls (40 chars). Right column: legend (34 chars).
    private const string Content = @"
 [ Essential Controls ]                [ Map Legend ]

  WASD / Arrows .. Move                 .  Floor/Path    #  Wall
  Q E Z C ........ Diagonals            ~  Water         =  Deep Water
  Space .......... Wait a turn          T  Tree          ;  Bush
  G .............. Pick up items        &  Campfire      O  Fountain
  I .............. Inventory            +  Door/Shrine   >  Stairs Up
  Bump ........... Attack / Talk        @  You           B  Boss
                                        V  Vendor        N  NPC
 [ Combat ]
                                       [ Items on Ground ]
  Bump enemy ..... Basic attack
  F1-F4 ......... Sword Skills          .  Common        o  Uncommon
  V .............. Counter stance       +  Rare          *  Legendary
  Shift+Dir ...... Sprint (2 tiles)
  Ctrl+Dir ....... Stealth move        [ Mob Symbols ]
  Combos ......... 5-hit = x2 dmg
                                        lowercase  Normal mob
 [ Menus & Screens ]                    UPPERCASE  Elite mob

  F .............. Sword Skills
  J .............. Quest Journal       [ Status Tags ]
  P .............. Stats / Levels
  T .............. Equipment            !PSN  Poison     !BLD  Bleed
  K .............. Kill Stats           !STN  Stun       vSLW  Slow
  H .............. This help            +SRG  Surge      +BLS  Shrine
  R .............. Rest (3 turns)       +FED  Well Fed   ~FTG  Fatigue
  X .............. Auto-explore
  L .............. Look mode           [ Crafting ]
  F5 ............. Save game
  1-5 ............ Quick-use item       Step on Anvil to repair or
                                        enhance equipment (+1 to +10).
 [ Tips ]                               Materials come from mob drops.

  - Weapon proficiency grows as you
    kill with a specific weapon type.   [ Day/Night ]
  - Higher proficiency unlocks new
    Sword Skills (press F to equip).    Vision shrinks at night.
  - Talk to NPCs for quests and to      Your torch provides light.
    turn in completed ones.             Campfires glow in the dark.
  - Enhanced weapons show +N suffix.
  - Death is permanent — save deleted.
";

    public static void Show()
    {
        var dialog = DialogHelper.Create("Help -- Controls & Reference", DialogWidth, DialogHeight);

        var contentLabel = new Label
        {
            Text = Content,
            X = 0, Y = 0,
            Width = Dim.Fill(), Height = Dim.Fill(2),
            ColorScheme = ColorSchemes.Body,
        };

        var hint = new Label
        {
            Text = "Esc: close",
            X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1), ColorScheme = ColorSchemes.Dim,
        };

        dialog.Add(contentLabel, hint);
        DialogHelper.AddCloseFooter(dialog);
        DialogHelper.RunModal(dialog);
    }
}
