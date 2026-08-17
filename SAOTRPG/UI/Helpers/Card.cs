using Terminal.Gui;

namespace SAOTRPG.UI.Helpers;

// A titled panel: rounded border, caption carried in the border, one column of padding inside.
//
// This is the container that gives a screen visible architecture — the space outside a card reads
// as deliberate margin rather than leftover void, which is the thing a centred column floating in
// a 316-column terminal cannot do.
//
// Deliberately a plain View, NOT a PanelView. PanelView cancels its viewport clear, which is right
// for the map (its children repaint every cell every frame) and wrong here: a card's content is
// Labels whose text changes, and without the clear a shorter string leaves the tail of the previous
// one behind. Letting the framework clear costs nothing on screens that repaint only on a keypress.
//
// The trade-off has one edge: the paired render invariant says a container between a
// guard-carrying view and GameWindow must cancel its clear. No pre-map view carries that guard, so
// it does not apply — but a Card placed on GameScreen would have to become a PanelView, and its
// content labels would then need fixed widths to stay residue-free.
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

        // A card is a FRAME, not a parent. Its content is added to the screen as siblings,
        // positioned inside its bounds with InsideX / InsideY, and the card is added first so it
        // paints underneath.
        //
        // This is not a stylistic choice. Terminal.Gui traps AdvanceFocus inside a focusable
        // container: measured, a container holding four checkboxes next to one sibling button
        // yielded 4/4 checkboxes and 0/1 buttons with TabStop NoStop or TabGroup, and 0/4
        // checkboxes with CanFocus false. Only a flat hierarchy reaches everything — the same
        // 4/4 and 1/1 the screens had before they were cardified. Nesting the controls cost the
        // modifier picker its Tab navigation, which is how that screen is driven.
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
