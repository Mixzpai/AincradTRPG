namespace SAOTRPG.UI.Helpers;

// Chainable scheme application for dynamically-built schemes (rarity tints, NPC colors).
// Terminal.Gui views have no settable scheme property, and SetScheme can't be used inside an
// object initializer, so this keeps view construction a single expression.
public static class ViewSchemeExtensions
{
    public static T WithScheme<T>(this T view, Scheme scheme) where T : View
    {
        view.SetScheme(scheme);
        return view;
    }
}
