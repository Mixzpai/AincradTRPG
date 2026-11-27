using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Widgets;

// A titled panel: rounded border, caption carried in the border, one column of padding inside.
//
// It gives a screen visible structure: the space outside a card reads as deliberate margin
// rather than leftover void, even on a very wide terminal.
//
// Deliberately a plain View, not a PanelView. PanelView cancels its viewport clear, which is right
// for the map (its children repaint every cell every frame) and wrong here: a card's content is
// Labels whose text changes, and without the clear a shorter string leaves the tail of the previous
// one behind. Letting the framework clear costs nothing on screens that repaint only on a keypress.
//
// A container between a guard-carrying view and GameWindow must cancel its clear. A card
// contains nothing, so on GameScreen, where the sidebar's guarded widgets sit inside the MAP,
// STATUS and LOG cards as siblings, it is not in that chain: they live in the PanelView column,
// and each excludes its own frame before the card draws.
public class Card : View
{
    public Card(string title, Pos x, Pos y, Dim width, Dim height)
    {
        Title = title;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        BorderStyle = LineStyle.Rounded;
        SchemeName = ColorSchemes.CardName;

        // A card is a frame, not a parent. Its content is added to the screen as siblings,
        // positioned inside its bounds with InsideX / InsideY, and the card is added first so it
        // paints underneath.
        //
        // Terminal.Gui traps AdvanceFocus inside a focusable container (with TabStop NoStop or
        // TabGroup, Tab never leaves it for a sibling), and a CanFocus = false container makes
        // the controls inside it unreachable. Only a flat hierarchy lets Tab reach every control,
        // and Tab is how the modifier picker is driven.
        CanFocus = false;
    }

    // Columns a card spends on chrome: two border edges plus one column of breathing room each
    // side. Content is inset by hand because it is not parented to the card.
    public const int ChromeWidth = 4;

    // Rows a card spends on chrome: the top and bottom border.
    public const int ChromeHeight = 2;

    // Left edge of a card's content area, given the card's own left edge.
    public static Pos InsideX(Pos cardX) => cardX + 2;

    // First content row of a card, given the card's own top edge.
    public static int InsideY(int cardY) => cardY + 1;

    // As above, for a card whose top edge is itself computed at layout time.
    public static Pos InsideY(Pos cardY) => cardY + 1;
}
