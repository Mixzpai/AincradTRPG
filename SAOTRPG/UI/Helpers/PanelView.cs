using Terminal.Gui;

namespace SAOTRPG.UI.Helpers;

// Layout container that never clears its own viewport.
//
// The framework clears a view's viewport whenever the view itself is marked for redraw,
// and a container spanning the map or sidebar therefore blanks that whole region. Its
// children repaint every cell they own, so the clear is redundant while they are drawing —
// and actively destructive while they are not: the views beneath a dialog skip painting
// (see AppHost.IsBeneathTopSession), so a container clear during that window wipes the
// background and nothing restores it until the dialog closes.
//
// Gaps between children are static once drawn, and genuine screen swaps still wipe via
// GameWindow.RequestFullClear.
public class PanelView : View
{
    protected override bool OnClearingViewport() => true;
}
