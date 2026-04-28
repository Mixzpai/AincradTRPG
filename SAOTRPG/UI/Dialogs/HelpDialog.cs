using Terminal.Gui;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Keybinding + glyph reference (H). Single-popup, no scroll, no search —
// pure reference: keybinds left, glyph + status legends right.
// Status abbreviations are overlaid as colored Labels (per quirks §5 — Terminal.Gui v2
// doesn't support inline color markup, so per-keyword tinting needs separate Labels).
// Status section uses blank-space placeholders (not underscores — those clip per font).
public static class HelpDialog
{
    private const int DialogWidth = 80, DialogHeight = 60;

    // Status section uses 4 leading spaces as the abbreviation slot (cols 44-47).
    // Labels at X=44 overlay these. Format: `<abbrev> = <description>`.
    private const string Content =
@" [ Movement ]                            [ Map Legend ]

   WASD / Arrows .. Move                    @  You         #  Wall
   QEZC ........... Diagonals               .  Floor       ,  Path
   Shift+Dir ...... Sprint                  ~  Water       =  Deep Water
   Ctrl+Dir ....... Stealth move            T  Tree        ;  Bush
   Space .......... Wait                    &  Campfire    O  Fountain
                                            +  Door        A  Shrine
 [ Combat & Actions ]                       ≫  Stairs Up   ≪  Stairs Down
                                            ▒  Cracked     Π  Labyrinth
   Bump ........... Attack / Talk           V  Vendor      N  NPC
   F .............. Sword Skill menu        *  Ore Vein    †  Corpse
   F1-F4 .......... Skill slots 1-4         B  Boss
   V .............. Counter stance
   \ .............. Ranged fire (Bow)     [ Items on Ground ]
   R .............. Rest (3 turns)
   X .............. Auto-explore            .  Common      o  Uncommon
   L .............. Look mode               +  Rare        *  Legendary
   G .............. Pickup                  ◈  Divine

 [ Menus & Screens ]                      [ Mob Tier ]

   I .............. Inventory               lowercase  Normal mob
   T .............. Equipment               UPPERCASE  Elite / Champion
   P .............. Stats / Levels          named      Boss / unique
   J .............. Quest Log
   K .............. Kill Stats            [ Status Tags ]
   Y .............. Bestiary
   H .............. Help (this)                  = Poisoned
   B .............. Player Guide                 = Bleeding
   Shift+L ........ Legendary panel              = Stunned
   Esc ............ Pause menu                   = Slowed
                                                 = Shrine buff
 [ Save / Quickbar / Log ]                       = Level Surge
                                                 = Food Regen
   F5 ............. Save game                    = Invisible
   1-9, 0 ......... Use quickbar slot
   Shift+0-9 ...... Bind item to slot      [ Toggles ]

                                              Shift+S ... Status tray verbose
                                              Tab ....... Cycle log tabs
                                              PgUp/PgDn . Log scroll
";

    // Abbreviation, color, X column, Y row (relative to dialog body).
    // X=44 places the label at the start of the abbreviation slot (matches the
    // 4-space placeholder in Content). Y rows correspond to the 8 status entries
    // in TextView line indexing (0-based from start of Content).
    private static readonly (string Abbrev, Color Fg, int X, int Y)[] StatusLabels =
    {
        ("PSN",  Color.BrightGreen,  44, 28),
        ("BLD",  Color.BrightRed,    44, 29),
        ("STN",  Color.BrightYellow, 44, 30),
        ("SLW",  Color.BrightCyan,   44, 31),
        ("SHRN", Color.BrightYellow, 44, 32),
        ("SRG",  Color.BrightGreen,  44, 33),
        ("REGN", Color.BrightGreen,  44, 34),
        ("INV",  Color.White,        44, 35),
    };

    public static void Show()
    {
        var dialog = DialogHelper.Create("Help -- Controls & Legend", DialogWidth, DialogHeight);

        // Static reference text — no scroll, no search. CanFocus=false so Tab
        // doesn't steal focus from the Close button (per quirks.md §2).
        var contentView = new TextView
        {
            Text = Content,
            X = 0, Y = 0,
            Width = Dim.Fill(), Height = Dim.Fill(2),
            ReadOnly = true,
            WordWrap = false,
            CanFocus = false,
            ColorScheme = ColorSchemes.Body,
        };

        dialog.Add(contentView);

        // Overlay colored status-tag labels on top of the TextView at known
        // (col, row) positions. Body text shows blank space placeholders.
        foreach (var (abbrev, fg, x, y) in StatusLabels)
        {
            var attr = new Terminal.Gui.Attribute(fg, Color.Black);
            dialog.Add(new Label
            {
                Text = abbrev,
                X = x,
                Y = y,
                ColorScheme = new ColorScheme
                {
                    Normal = attr,
                    HotNormal = attr,
                    Focus = attr,
                    HotFocus = attr,
                    Disabled = attr,
                },
            });
        }

        DialogHelper.AddCloseFooter(dialog);
        DialogHelper.RunModal(dialog);
    }
}
