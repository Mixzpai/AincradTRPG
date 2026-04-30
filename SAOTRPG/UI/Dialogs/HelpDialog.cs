using Terminal.Gui;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Keybinding + glyph reference (H). Single-popup, no scroll, no search —
// pure reference: keybinds left, glyph + status legends right.
// Status abbreviations are overlaid as colored Labels — Terminal.Gui v2
// doesn't support inline color markup, so per-keyword tinting needs separate Labels.
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
   Ctrl+Dir ....... Stealth move            ♣  Tree        ※  Bush
   Space .......... Wait                    &  Campfire    ⊙  Fountain
                                            +  Door        ☥  Shrine
 [ Combat & Actions ]                       >  Stairs Up   <  Stairs Down
                                            ▒  Cracked     Π  Labyrinth
   Bump ........... Attack / Talk           V  Vendor      N  NPC
   F .............. Sword Skill menu        ◊  Ore Vein    †  Corpse
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

                                          [ Debug ]

                                              Shift+F10 . Profiler toggle
                                              Shift+F11 . Profiler reset
                                              Shift+F12 . Profiler dump
";

    // Abbreviation, color, X column, Y row (relative to dialog body).
    // X=44 places the label at the start of the abbreviation slot. Y rows
    // correspond to the line index in Content (0-based).
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

    // Map Legend / Items / Mob Tier overlays. Each token gets a colored Label
    // matching its in-game appearance. RGB values mirror Map/TileDefinitions.cs
    // and Items/RarityHelper.cs so the legend stays in sync visually.
    private static readonly (string Token, Color Fg, int X, int Y)[] MapLegendOverlays =
    {
        // Map Legend — left column (X=44), right column (X=59)
        ("@", Color.BrightYellow,         44, 2),   // You
        ("#", new Color(180, 175, 170),   59, 2),   // Wall
        (".", new Color(160, 155, 145),   44, 3),   // Floor
        (",", new Color(200, 180, 120),   59, 3),   // Path
        ("~", new Color(70,  140, 220),   44, 4),   // Water
        ("=", new Color(40,  80,  180),   59, 4),   // Deep Water
        ("♣", new Color(30,  130, 50),    44, 5),   // Tree
        ("※", new Color(50,  160, 60),    59, 5),   // Bush
        ("&", new Color(255, 200, 80),    44, 6),   // Campfire (FireYellow)
        ("⊙", new Color(100, 220, 255),   59, 6),   // Fountain
        ("+", new Color(255, 220, 80),    44, 7),   // Door
        ("☥", new Color(200, 120, 255),   59, 7),   // Shrine
        (">", new Color(100, 220, 255),   44, 8),   // Stairs Up
        ("<", new Color(100, 95,  90),    59, 8),   // Stairs Down
        ("▒", new Color(120, 115, 110),   44, 9),   // Cracked
        ("Π", new Color(100, 220, 255),   59, 9),   // Labyrinth
        ("V", Color.BrightGreen,          44, 10),  // Vendor
        ("N", Color.BrightCyan,           59, 10),  // NPC
        ("◊", new Color(170, 170, 180),   44, 11),  // Ore Vein (iron tone)
        ("†", Color.Gray,                 59, 11),  // Corpse
        ("B", Color.BrightRed,            44, 12),  // Boss

        // Items on Ground (rarity colors per RarityHelper.GetColor)
        (".", Color.Gray,         44, 16),  // Common
        ("o", Color.BrightGreen,  59, 16),  // Uncommon
        ("+", Color.BrightCyan,   44, 17),  // Rare
        ("*", Color.BrightYellow, 59, 17),  // Legendary
        ("◈", Color.BrightRed,    44, 18),  // Divine

        // Mob Tier — casing convention indicators
        ("lowercase", Color.Gray,         44, 22),
        ("UPPERCASE", Color.BrightYellow, 44, 23),
        ("named",     Color.BrightRed,    44, 24),
    };

    public static void Show()
    {
        var dialog = DialogHelper.Create("", DialogWidth, DialogHeight);

        // Build per-cell color schemes once.
        var whiteScheme = SolidScheme(Color.White);
        var goldScheme  = SolidScheme(Color.BrightYellow);

        // Render the Content as a stack of per-line Labels. Static read-only
        // reference — Labels give per-token color (TG v2 TextView shares one
        // ColorScheme across all its content, which is the wrong primitive here).
        var lines = Content.Replace("\r\n", "\n").Split('\n');
        for (int row = 0; row < lines.Length; row++)
        {
            string line = lines[row];
            if (line.Length == 0) continue;  // blank row — no widget needed

            // Base white line.
            dialog.Add(new Label
            {
                Text = line,
                X = 0, Y = row,
                ColorScheme = whiteScheme,
            });

            // Gold overlay for every "[...]" bracket title on the line.
            int scan = 0;
            while (scan < line.Length)
            {
                int br1 = line.IndexOf('[', scan);
                if (br1 < 0) break;
                int br2 = line.IndexOf(']', br1);
                if (br2 < 0) break;
                dialog.Add(new Label
                {
                    Text = line.Substring(br1, br2 - br1 + 1),
                    X = br1, Y = row,
                    ColorScheme = goldScheme,
                });
                scan = br2 + 1;
            }

            // Gold overlay for left-column keybind: first non-space text up to
            // the " .." dot sequence. Skip bracket-title rows.
            int firstChar = 0;
            while (firstChar < line.Length && char.IsWhiteSpace(line[firstChar])) firstChar++;
            if (firstChar >= line.Length || line[firstChar] == '[') continue;

            int dotStart = line.IndexOf(" .", firstChar);
            if (dotStart < 0) continue;
            int chk = dotStart + 1;
            while (chk < line.Length && line[chk] == '.') chk++;
            if (chk - dotStart - 1 < 1) continue;  // need ≥1 dot to qualify (single-dot separators ok)

            string keybind = line.Substring(firstChar, dotStart - firstChar);
            if (string.IsNullOrWhiteSpace(keybind)) continue;
            dialog.Add(new Label
            {
                Text = keybind,
                X = firstChar, Y = row,
                ColorScheme = goldScheme,
            });
        }

        // Status-tag colored overlays — appear over the white base label rows
        // at the (col, row) positions the body text reserves for them.
        foreach (var (abbrev, fg, x, y) in StatusLabels)
        {
            dialog.Add(new Label
            {
                Text = abbrev,
                X = x, Y = y,
                ColorScheme = SolidScheme(fg),
            });
        }

        // Map Legend / Items / Mob Tier glyph + word overlays — colored to
        // match each token's in-game appearance.
        foreach (var (token, fg, x, y) in MapLegendOverlays)
        {
            dialog.Add(new Label
            {
                Text = token,
                X = x, Y = y,
                ColorScheme = SolidScheme(fg),
            });
        }

        DialogHelper.AddCloseFooter(dialog);
        DialogHelper.RunModal(dialog);
    }

    // Build a ColorScheme where every state (Normal / Focus / HotNormal /
    // HotFocus / Disabled) renders fg-on-Black. Used for static-content Labels
    // so TG v2 doesn't fall back to a Disabled-state DarkGray.
    private static ColorScheme SolidScheme(Color fg)
    {
        var attr = Gfx.Attr(fg, Color.Black);
        return new ColorScheme
        {
            Normal    = attr,
            Focus     = attr,
            HotNormal = attr,
            HotFocus  = attr,
            Disabled  = attr,
        };
    }
}
