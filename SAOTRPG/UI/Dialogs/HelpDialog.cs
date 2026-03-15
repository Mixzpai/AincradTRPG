using Terminal.Gui;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

/// <summary>
/// Full-screen keybinding reference opened with H key.
/// Shows all controls organized by category.
/// </summary>
public static class HelpDialog
{
    // ── Help text content ────────────────────────────────────────────
    // Organized into sections for easy scanning.
    // Add new keybinds here as features are added.

    private const string HelpText =
        "  ── Movement ──────────────────────────────\n" +
        "  W / ↑          Move North\n" +
        "  S / ↓          Move South\n" +
        "  A / ←          Move West\n" +
        "  D / →          Move East\n" +
        "  Q              Move NW (diagonal)\n" +
        "  E              Move NE (diagonal)\n" +
        "  Z              Move SW (diagonal)\n" +
        "  C              Move SE (diagonal)\n" +
        "  Shift+dir      Sprint (move 2 tiles)\n" +
        "  Ctrl+dir       Stealth move (halves aggro)\n" +
        "\n" +
        "  ── Actions ───────────────────────────────\n" +
        "  Space          Wait a turn\n" +
        "  G              Pick up item\n" +
        "  I              Open inventory\n" +
        "  P              Allocate skill points\n" +
        "  L              Look around\n" +
        "  R              Rest (skip 3 turns, heal)\n" +
        "  T              Equipment overview\n" +
        "  X              Auto-explore\n" +
        "  H              This help screen\n" +
        "\n" +
        "  ── Interaction ───────────────────────────\n" +
        "  Bump           Walk into NPCs/vendors to\n" +
        "                 talk or open their shop\n" +
        "\n" +
        "  ── Map Legend ─────────────────────────────\n" +
        "  @  You          k/w/b  Monsters\n" +
        "  V  Vendor       N  NPC\n" +
        "  >  Stairs up    &  Campfire\n" +
        "  ~  Water        T  Tree\n" +
        "  !  Item(s)      F  Fountain\n" +
        "  S  Shrine       P  Pillar";

    // ── Dialog dimensions ────────────────────────────────────────────
    private const int DialogWidth  = 52;
    private const int DialogHeight = 34;

    public static void Show()
    {
        var dialog = new Dialog
        {
            Title = "Keybindings & Legend",
            Width = DialogWidth,
            Height = DialogHeight,
            ColorScheme = ColorSchemes.Dialog
        };

        var helpLabel = new Label
        {
            Text = HelpText,
            X = 0, Y = 0,
            Width = Dim.Fill(), Height = Dim.Fill(2),
            ColorScheme = ColorSchemes.Body
        };

        var closeBtn = new Button
        {
            Text = "Close",
            X = Pos.Center(),
            Y = Pos.AnchorEnd(1),
            IsDefault = true,
            ColorScheme = ColorSchemes.Button
        };
        closeBtn.Accepting += (s, e) => { e.Cancel = true; Application.RequestStop(); };

        dialog.Add(helpLabel, closeBtn);
        closeBtn.SetFocus();
        Application.Run(dialog);
    }
}
