namespace SAOTRPG.Systems.World;

// Game calendar: one turn is one second, a day is 86,400 turns, a season is 91 days and a year
// four seasons. The run starts when the death game began, at 1:00 PM on 6 November 2022, which
// on this calendar is Year 1, Autumn, day 67.
public static class GameClock
{
    public const int TurnsPerMinute = 60, TurnsPerHour = 3600, TurnsPerDay = 86400;
    public const int SeasonLengthDays = 91;
    public const int StartSecondOfDay = 13 * TurnsPerHour;

    // Autumn is the third season; 6 November is its 67th day.
    private const int StartDayOfYear = 2 * SeasonLengthDays + 66;
    private const int YearLengthDays = 4 * SeasonLengthDays;
    private static readonly string[] Seasons = { "Spring", "Summer", "Autumn", "Winter" };

    // The sun is modelled with noon at phase 0, so the day/night clock runs this far ahead of
    // the turn counter for its noon to fall at 12:00 on this calendar.
    public const int SunPhaseOffset = StartSecondOfDay - 12 * TurnsPerHour;

    // Seconds since midnight on the first day, at this turn.
    public static long Since(long turn) => StartSecondOfDay + turn;

    public static int SecondOfDay(long turn) => (int)(Since(turn) % TurnsPerDay);

    // Turns until the clock next reads this second of the day; a full day when it reads it now.
    public static int TurnsUntil(long turn, int secondOfDay)
    {
        int d = (secondOfDay - SecondOfDay(turn) + TurnsPerDay) % TurnsPerDay;
        return d == 0 ? TurnsPerDay : d;
    }

    // "1:00:00 PM", padded with a space before AM/PM only on a one-digit hour.
    public static string TimeOfDay(long turn)
    {
        Span<char> buf = stackalloc char[TimeOfDayMaxLength];
        return new string(buf[..WriteTimeOfDay(turn, buf)]);
    }

    public const int TimeOfDayMaxLength = 16;

    // The same text written into `dest`, which must hold TimeOfDayMaxLength; returns its length.
    public static int WriteTimeOfDay(long turn, Span<char> dest)
    {
        int s = SecondOfDay(turn);
        int hour = s / TurnsPerHour, minute = s / TurnsPerMinute % 60, second = s % 60;
        int h12 = hour % 12 == 0 ? 12 : hour % 12;
        string padding = h12 < 10 ? " " : "";
        if (!dest.TryWrite($"{h12}:{minute:00}:{second:00}{padding}{(hour < 12 ? "AM" : "PM")}", out int written))
            throw new ArgumentException($"needs {TimeOfDayMaxLength} chars", nameof(dest));
        return written;
    }

    // "Year 1, Autumn, day 67".
    public static string Date(long turn)
    {
        Span<char> buf = stackalloc char[DateMaxLength];
        return new string(buf[..WriteDate(turn, buf)]);
    }

    public const int DateMaxLength = 48;

    // The same text written into `dest`, which must hold DateMaxLength; returns its length.
    public static int WriteDate(long turn, Span<char> dest)
    {
        long days = Since(turn) / TurnsPerDay + StartDayOfYear;
        long year = days / YearLengthDays + 1;
        int dayOfYear = (int)(days % YearLengthDays);
        if (!dest.TryWrite($"Year {year}, {Seasons[dayOfYear / SeasonLengthDays]}, day {dayOfYear % SeasonLengthDays + 1}", out int written))
            throw new ArgumentException($"needs {DateMaxLength} chars", nameof(dest));
        return written;
    }

    // Clipped time: the largest unit the duration fills, and how many of it.
    private enum Unit { Second, Minute, Hour, Day, Week, Season, Year }

    private static (int N, Unit U) Clip(long seconds)
    {
        seconds = Math.Max(0, seconds);
        if (seconds < TurnsPerMinute) return ((int)seconds, Unit.Second);
        if (seconds < TurnsPerHour) return ((int)(seconds / TurnsPerMinute), Unit.Minute);
        if (seconds < TurnsPerDay) return ((int)(seconds / TurnsPerHour), Unit.Hour);
        if (seconds < 7L * TurnsPerDay) return ((int)(seconds / TurnsPerDay), Unit.Day);
        if (seconds < (long)SeasonLengthDays * TurnsPerDay) return ((int)(seconds / (7L * TurnsPerDay)), Unit.Week);
        if (seconds < (long)YearLengthDays * TurnsPerDay)
            return ((int)(seconds / ((long)SeasonLengthDays * TurnsPerDay)), Unit.Season);
        return ((int)(seconds / ((long)YearLengthDays * TurnsPerDay)), Unit.Year);
    }

    private static readonly string[] Singular = { "second", "minute", "hour", "day", "week", "season", "year" };

    // "5 minutes", "1 hour".
    public static string Clipped(long seconds)
    {
        var (n, u) = Clip(seconds);
        return $"{n} {Singular[(int)u]}{(n == 1 ? "" : "s")}";
    }

    // The right-aligned form, 11 columns: "  5 minutes", "  1  minute", "  2   hours".
    public static string ClippedRight(long seconds)
    {
        var (n, u) = Clip(seconds);
        string word = n == 1 ? Singular[(int)u] : Singular[(int)u] + "s";
        return $"{n,3} {word,7}";
    }

    // A duration in words, largest unit plus remainder: "1 hour and 30 minutes".
    public static string Duration(long seconds)
    {
        if (seconds <= TurnsPerMinute) return Clipped(seconds);
        long divider = seconds < TurnsPerHour ? TurnsPerMinute
            : seconds < TurnsPerDay ? TurnsPerHour
            : seconds < 7L * TurnsPerDay ? TurnsPerDay
            : seconds < (long)SeasonLengthDays * TurnsPerDay ? 7L * TurnsPerDay
            : seconds < (long)YearLengthDays * TurnsPerDay ? (long)SeasonLengthDays * TurnsPerDay
            : (long)YearLengthDays * TurnsPerDay;
        long rest = seconds % divider;
        return rest == 0 ? Clipped(seconds) : $"{Clipped(seconds)} and {Clipped(rest)}";
    }
}
