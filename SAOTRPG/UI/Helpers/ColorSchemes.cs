using Terminal.Gui;

namespace SAOTRPG.UI.Helpers;

// Centralized color scheme definitions used across all UI screens.
// Keeps visual consistency and makes palette changes easy.
// Static palettes are registered with Terminal.Gui's SchemeManager at startup (RegisterAll)
// under "SAO."-prefixed names so views can bind via SchemeName; dynamic one-offs (FromColor,
// fade-scaled schemes) are applied directly with View.SetScheme and never registered.
//
// ── Palette design ──────────────────────────────────────────────────────────────
// Colours are named by ROLE, not by hue, and every scheme below is assembled from the
// role constants. Two rules keep it coherent:
//
//   1. Exactly one saturated accent (Accent). Everything else is a value ramp between
//      Muted and Emphasis. Two saturated hues never sit adjacent — status colours are
//      desaturated so they read as meaning rather than as alarm.
//   2. Hierarchy lives in VALUE (brightness), with hue only reinforcing it. Strip the
//      colour out and the screens must still be readable.
//
// Base is the page and everything sits on it — menus, dialogs and the map share one ground.
// Surface lifts a focused row or an input field off it, and those work because they are FILLED
// CONTROLS: the lift and the text belong to the same scheme. A lifted PAGE does not work the same
// way, because a page is mostly other views' content and each of those carries its own background
// (see BuildDialog). Grouping at page level is drawn — a Card's border, a dialog's frame.
public static class ColorSchemes
{
    // ── Role palette ────────────────────────────────────────────────

    // The active theme. Every role below reads off it, and every scheme is assembled from the
    // roles, so switching a theme is one assignment plus a rebuild — there is no second place
    // where a colour is decided.
    public static ColorTheme Theme { get; private set; } = ColorTheme.Default;

    private static Color Base    => Theme.Base;
    private static Color Surface => Theme.Surface;
    // Reserved. Nothing fills with this today — see BuildDialog for why a filled surface layer
    // cannot work until every content scheme has an Overlay variant.
    private static Color Overlay => Theme.Overlay;

    private static Color Muted => Theme.Muted;
    private static Color Body_ => Theme.Body;
    private static Color Emph  => Theme.Emph;

    private static Color Accent    => Theme.Accent;
    private static Color AccentDim => Theme.AccentDim;

    private static Color Ok   => Theme.Ok;
    private static Color Crit => Theme.Crit;

    // Switches theme and republishes every scheme under its existing name.
    //
    // SchemeManager.AddScheme UPDATES an entry that already exists, and a view bound with
    // SchemeName resolves through the manager when it draws — so re-registering repaints
    // everything bound that way without touching a single view. Views bound with SetScheme hold
    // the Scheme object instead and keep the old one until they are rebuilt, which is why the
    // settings screen re-shows itself after a switch.
    public static void ApplyTheme(ThemeId id)
    {
        Theme = ColorTheme.ById(id);
        Rebuild();
        RegisterAll();
    }

    // ── SchemeManager registration names ────────────────────────────

    public const string ButtonName        = "SAO.Button";
    public const string GoldName          = "SAO.Gold";
    public const string DimName           = "SAO.Dim";
    public const string BodyName          = "SAO.Body";
    public const string TitleName         = "SAO.Title";
    public const string DangerName        = "SAO.Danger";
    public const string SuccessName       = "SAO.Success";
    public const string DialogName        = "SAO.Dialog";
    public const string MenuButtonName    = "SAO.MenuButton";
    public const string TierRadioName     = "SAO.TierRadio";
    public const string ListSelectionName = "SAO.ListSelection";
    public const string WindowName        = "SAO.Window";
    public const string CardName          = "SAO.Card";

    // Register every static palette with SchemeManager. Call once at startup,
    // after AppHost.Start() and before any UI construction.
    public static void RegisterAll()
    {
        SchemeManager.AddScheme(ButtonName,        Button);
        SchemeManager.AddScheme(GoldName,          Gold);
        SchemeManager.AddScheme(DimName,           Dim);
        SchemeManager.AddScheme(BodyName,          Body);
        SchemeManager.AddScheme(TitleName,         Title);
        SchemeManager.AddScheme(DangerName,        Danger);
        SchemeManager.AddScheme(SuccessName,       Success);
        SchemeManager.AddScheme(DialogName,        Dialog);
        SchemeManager.AddScheme(MenuButtonName,    MenuButton);
        SchemeManager.AddScheme(TierRadioName,     TierRadio);
        SchemeManager.AddScheme(ListSelectionName, ListSelection);
        SchemeManager.AddScheme(WindowName,        Window);
        SchemeManager.AddScheme(CardName,          Card);
    }

    // Replaces Terminal.Gui's default marker glyphs. Call once at startup, next to
    // RegisterAll — Terminal.Gui scopes Glyphs and Scheme under the same theme, so the
    // game's visual vocabulary is set in one place.
    //
    // The diamonds are the polygon-shatter motif the combat visuals already use, so the
    // menus speak the same language as the game. CheckStateNone is not merely a restyle:
    // its default U+2B1B is an EMOJI-presentation glyph that measures two columns wide
    // (Terminal.Gui's own GetColumns confirms), which would tear the character grid the
    // first time a three-state checkbox appeared. Nothing sets AllowNone today, so this
    // is pre-emptive.
    public static void ApplyGlyphs()
    {
        Glyphs.Selected           = new System.Text.Rune('◆');
        Glyphs.UnSelected         = new System.Text.Rune('◇');
        Glyphs.CheckStateChecked  = new System.Text.Rune('◼');
        Glyphs.CheckStateUnChecked = new System.Text.Rune('◻');
        Glyphs.CheckStateNone     = new System.Text.Rune('▪');
    }

    // ── Core UI palettes ─────────────────────────────────────────────

    // Standard button: body text at rest, accent on a lifted surface when focused.
    private static Scheme _button = BuildButton();

    private static Scheme BuildButton() => new()
    {
        Normal    = Gfx.Attr(Body_,  Base),
        Focus     = Gfx.Attr(Accent, Surface),
        HotNormal = Gfx.Attr(Body_,  Base),
        HotFocus  = Gfx.Attr(Accent, Surface),
        Disabled  = Gfx.Attr(Muted,  Base),
    };

    // The accent, for highlights, dividers and section headings.
    private static Scheme _gold = BuildGold();

    private static Scheme BuildGold() => new()
    {
        Normal    = Gfx.Attr(Accent, Base),
        Focus     = Gfx.Attr(Accent, Surface),
        HotNormal = Gfx.Attr(Accent, Base),
        HotFocus  = Gfx.Attr(Accent, Surface),
        Disabled  = Gfx.Attr(AccentDim, Base),
    };

    // Secondary text: hints, footers, disabled rows.
    private static Scheme _dim = BuildDim();

    private static Scheme BuildDim() => new()
    {
        Normal    = Gfx.Attr(Muted, Base),
        Focus     = Gfx.Attr(Muted, Base),
        HotNormal = Gfx.Attr(Muted, Base),
        HotFocus  = Gfx.Attr(Muted, Base),
        Disabled  = Gfx.Attr(Muted, Base),
    };

    // Standard body text.
    private static Scheme _body = BuildBody();

    private static Scheme BuildBody() => new()
    {
        Normal    = Gfx.Attr(Body_, Base),
        Focus     = Gfx.Attr(Body_, Base),
        HotNormal = Gfx.Attr(Body_, Base),
        HotFocus  = Gfx.Attr(Body_, Base),
        Disabled  = Gfx.Attr(Muted, Base),
    };

    // Emphasis — screen titles and headings. Bold reinforces the value step; it is not
    // load-bearing on its own, since the colour already separates it from Body.
    private static Scheme _title = BuildTitle();

    private static Scheme BuildTitle() => new()
    {
        Normal    = Gfx.Attr(Emph, Base, TextStyle.Bold),
        Focus     = Gfx.Attr(Emph, Base, TextStyle.Bold),
        HotNormal = Gfx.Attr(Emph, Base, TextStyle.Bold),
        HotFocus  = Gfx.Attr(Emph, Base, TextStyle.Bold),
        Disabled  = Gfx.Attr(Muted, Base),
    };

    // Danger/death red for alerts.
    private static Scheme _danger = BuildDanger();

    private static Scheme BuildDanger() => new()
    {
        Normal    = Gfx.Attr(Crit, Base),
        Focus     = Gfx.Attr(Crit, Surface),
        HotNormal = Gfx.Attr(Crit, Base),
        HotFocus  = Gfx.Attr(Crit, Surface),
        Disabled  = Gfx.Attr(Muted, Base),
    };

    // Success green for positive feedback — equip, purchase, heal.
    private static Scheme _success = BuildSuccess();

    private static Scheme BuildSuccess() => new()
    {
        Normal    = Gfx.Attr(Ok,    Base),
        Focus     = Gfx.Attr(Ok,    Surface),
        HotNormal = Gfx.Attr(Ok,    Base),
        HotFocus  = Gfx.Attr(Ok,    Surface),
        Disabled  = Gfx.Attr(Muted, Base),
    };

    // ── Dialog palettes ───────────────────────────────────────────────

    // Dialogs share the page's black ground, and get their lift from their BORDER rather than
    // from a fill — the same way a Card does on the menus.
    //
    // They were filled with Overlay for one session. That does not work while the twelve content
    // schemes hardcode Base: a dialog is mostly its content, so every label and button inside it
    // painted a black rectangle onto the lifted fill, which reads as a block around each piece of
    // text. A filled surface layer needs an Overlay variant of every content scheme before it can
    // be used, and picking the right one at each call site is a mistake waiting to happen.
    //
    // Editable is set explicitly: Terminal.Gui derives it from Normal by dimming the FOREGROUND
    // into a background, which on a black page produces a field with no visible extent at all.
    private static Scheme _dialog = BuildDialog();

    private static Scheme BuildDialog() => new()
    {
        Normal    = Gfx.Attr(Body_, Base),
        Focus     = Gfx.Attr(Emph,  Base),
        HotNormal = Gfx.Attr(Body_, Base),
        HotFocus  = Gfx.Attr(Emph,  Base),
        Disabled  = Gfx.Attr(Muted, Base),
        Editable  = Gfx.Attr(Emph,  Surface),
        Active    = Gfx.Attr(Base,  Accent),
    };

    // ── Menu-specific palettes ───────────────────────────────────────

    // Menu button — body text at rest, accent on a lifted row when focused.
    private static Scheme _menuButton = BuildMenuButton();

    private static Scheme BuildMenuButton() => new()
    {
        Normal    = Gfx.Attr(Body_,  Base),
        Focus     = Gfx.Attr(Accent, Surface),
        HotNormal = Gfx.Attr(Body_,  Base),
        HotFocus  = Gfx.Attr(Accent, Surface),
        Disabled  = Gfx.Attr(Muted,  Base),
    };

    // ── Selection palettes ────────────────────────────────────────────

    // Tier / radio list. Focus is accent-on-surface rather than the old full inversion
    // (black on saturated yellow): a 54-column inverted slab is the loudest thing that
    // can appear on a screen, and it destroys any per-token colour inside the row.
    private static Scheme _tierRadio = BuildTierRadio();

    private static Scheme BuildTierRadio() => new()
    {
        Normal    = Gfx.Attr(Body_,  Base),
        Focus     = Gfx.Attr(Accent, Surface),
        HotNormal = Gfx.Attr(Body_,  Base),
        HotFocus  = Gfx.Attr(Accent, Surface),
        Disabled  = Gfx.Attr(Muted,  Base),
    };

    // ListView selection — same treatment as TierRadio.
    // Used by Player Guide topic list + any vertically-scrolling picker.
    private static Scheme _listSelection = BuildListSelection();

    private static Scheme BuildListSelection() => new()
    {
        Normal    = Gfx.Attr(Body_,  Base),
        Focus     = Gfx.Attr(Accent, Surface),
        HotNormal = Gfx.Attr(Body_,  Base),
        HotFocus  = Gfx.Attr(Accent, Surface),
        Disabled  = Gfx.Attr(Muted,  Base),
        Active    = Gfx.Attr(Emph,   Surface),
    };

    // ── Containers ───────────────────────────────────────────────────

    // Card frame: a muted border so the panel reads as structure rather than as content, with the
    // caption picked out in the emphasis tone. Normal is the border colour — the card's children
    // carry their own schemes, so this only ever paints chrome and the gaps between them.
    private static Scheme _card = BuildCard();

    private static Scheme BuildCard() => new()
    {
        Normal    = Gfx.Attr(Muted, Base),
        Focus     = Gfx.Attr(Accent, Base),
        HotNormal = Gfx.Attr(Emph,  Base, TextStyle.Bold),
        HotFocus  = Gfx.Attr(Accent, Base, TextStyle.Bold),
        Disabled  = Gfx.Attr(Muted, Base),
    };

    // ── Host window ──────────────────────────────────────────────────

    // The full-screen host window every screen renders inside. Views that set no scheme
    // of their own inherit this one, which is why Editable is set here too — an unstyled
    // TextField draws its whole rectangle with the Editable role.
    private static Scheme _window = BuildWindow();

    private static Scheme BuildWindow() => new()
    {
        Normal    = Gfx.Attr(Muted, Base),
        Focus     = Gfx.Attr(Body_, Base),
        HotNormal = Gfx.Attr(Body_, Base),
        HotFocus  = Gfx.Attr(Emph,  Base),
        Disabled  = Gfx.Attr(Muted, Base),
        Editable  = Gfx.Attr(Emph,  Surface),
        Active    = Gfx.Attr(Base,  Accent),
    };

    // ── Accessors ────────────────────────────────────────────────────
    // Properties rather than fields so a theme switch can replace them wholesale.
    public static Scheme Button => _button;
    public static Scheme Gold => _gold;
    public static Scheme Dim => _dim;
    public static Scheme Body => _body;
    public static Scheme Title => _title;
    public static Scheme Danger => _danger;
    public static Scheme Success => _success;
    public static Scheme Dialog => _dialog;
    public static Scheme MenuButton => _menuButton;
    public static Scheme TierRadio => _tierRadio;
    public static Scheme ListSelection => _listSelection;
    public static Scheme Card => _card;
    public static Scheme Window => _window;

    // Reassembles every scheme from the active theme's roles.
    private static void Rebuild()
    {
        _button = BuildButton();
        _gold = BuildGold();
        _dim = BuildDim();
        _body = BuildBody();
        _title = BuildTitle();
        _danger = BuildDanger();
        _success = BuildSuccess();
        _dialog = BuildDialog();
        _menuButton = BuildMenuButton();
        _tierRadio = BuildTierRadio();
        _listSelection = BuildListSelection();
        _card = BuildCard();
        _window = BuildWindow();
    }

    // ── Dynamic palette helpers ─────────────────────────────────────

    // Non-interactive scheme with `fg` on the page background — for one-off labels.
    // Dynamic: apply with View.SetScheme, never registered with SchemeManager.
    public static Scheme FromColor(Color fg) => new()
    {
        Normal    = Gfx.Attr(fg,    Base),
        Focus     = Gfx.Attr(fg,    Base),
        HotNormal = Gfx.Attr(fg,    Base),
        HotFocus  = Gfx.Attr(fg,    Base),
        Disabled  = Gfx.Attr(Muted, Base),
    };
}
