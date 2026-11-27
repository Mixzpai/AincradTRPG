using SAOTRPG.Systems.World;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// The wait menu: fixed durations and waits until the next daylight, noon, night or midnight, with
// the current time in the header. Each row's key picks it; Enter picks the focused row; Esc waits
// for nothing. The player then waits until the time passes or something interrupts them.
public static class WaitDialog
{
    private const int RowWidth = 46;

    // The menu's entries and hotkeys. Daylight, noon, night and midnight are the next time the clock
    // reads 6 AM, 12 PM, 6 PM and 12 AM: this sky's sunrise, zenith, sunset and nadir.
    public static IReadOnlyList<(char Key, string Text, int Turns)> Entries(int turn)
    {
        var rows = new List<(char, string, int)>();
        foreach (var (key, secs) in new[] { ('1', 20), ('2', 60), ('3', 300), ('4', 1800),
                                            ('5', 3600), ('6', 7200), ('7', 10800), ('8', 21600) })
            rows.Add((key, GameClock.Duration(secs), secs));
        foreach (var (key, caption, hour) in new[] { ('d', "Wait till daylight", 6), ('n', "Wait till noon", 12),
                                                     ('k', "Wait till night", 18), ('m', "Wait till midnight", 0) })
        {
            int t = GameClock.TurnsUntil(turn, hour * GameClock.TurnsPerHour);
            rows.Add((key, $"{caption} ({GameClock.Duration(t)})", t));
        }
        return rows;
    }

    // How long to wait, or null when the menu was closed.
    public static int? Show(int turn)
    {
        var entries = Entries(turn);
        int? chosen = null;
        var dialog = DialogHelper.Create("Wait", RowWidth + 4, entries.Count + 6);
        dialog.Add(new Label
        {
            Text = $"It's {GameClock.TimeOfDay(turn)} now.  Wait for how long?",
            X = 1, Y = 0, Width = Dim.Fill(1), Height = 1,
            SchemeName = ColorSchemes.GoldName,
        });
        for (int i = 0; i < entries.Count; i++)
        {
            var (key, text, turns) = entries[i];
            var row = DialogHelper.CreateMenuRow($"{key}  {text}", RowWidth, isDefault: i == 0);
            row.X = Pos.Center();
            row.Y = i + 2;
            row.Accepting += (_, e) => { e.Handled = true; chosen = turns; AppHost.App.RequestStop(); };
            dialog.Add(row);
        }
        var hints = new[] { ("key", "pick"), ("enter", "select"), ("esc", "cancel") };
        foreach (var label in ScreenHeader.KeyHints(Pos.Percent(50) - ScreenHeader.KeyHintsWidth(hints) / 2,
                                                    Pos.AnchorEnd(1), hints))
            dialog.Add(label);
        // A row's hotkey picks it outright.
        dialog.KeyDown += (_, e) =>
        {
            if (e.AsRune.Value == 0) return;
            char c = (char)e.AsRune.Value;
            foreach (var (key, _, turns) in entries)
            {
                if (key != c) continue;
                chosen = turns;
                e.Handled = true;
                AppHost.App.RequestStop();
                return;
            }
        };
        DialogHelper.CloseOnEscape(dialog);
        DialogHelper.RunModal(dialog);
        return chosen;
    }
}
