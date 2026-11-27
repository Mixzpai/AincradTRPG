namespace SAOTRPG.UI.Widgets;

// A Button that never builds Terminal.Gui's drop shadow. Every button in the game has none: on
// its black background the shadow is black on black, yet it overwrites the cells beneath and
// widens the frame. A plain Button builds the shadow's views in its constructor, before an
// initializer can clear it, and tearing them down again leaves all of it as garbage.
public class PlainButton : Button
{
    protected override void OnInitializingShadowStyle(ValueChangingEventArgs<ShadowStyles?> args) => args.NewValue = null;
}
