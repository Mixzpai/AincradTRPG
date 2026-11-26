using System.Text;
using SAOTRPG.Entities;
using SAOTRPG.Systems.Diagnostics;
using SAOTRPG.Systems.RunState;
using SAOTRPG.UI.GameLog;

namespace SAOTRPG.UI.Screens;

// Boots straight onto Floor 1 with a fixed character, skipping the title, difficulty and
// character-creation screens. For benchmarking and for any measurement that wants to reach the
// map without a human driving four screens first.
//
// The CHARACTER is fixed so two runs are comparable; only the world seed varies, and --seed
// pins that too when a benchmark needs the same floor twice.
public static class QuickStart
{
    // Slot 0 is deliberately OUTSIDE the 1..SaveManager.MaxSlots range the slot picker
    // enumerates, so a quick-start run can exercise the save path without any possibility of
    // overwriting a real save. A probe harness has destroyed live player data here before; the
    // cheap guarantee is a slot the rest of the game cannot address.
    public const int DebugSaveSlot = 0;

    // "Normal" — the intended balance, so a measurement taken here describes the real game.
    private const int DebugDifficulty = 3;

    // Ten starting points, spread across all six stats. Fixed rather than rolled: an unspent or
    // varying allocation changes combat maths and would make two benchmark runs incomparable.
    private static readonly (string Stat, int Points)[] Allocation =
    {
        ("Vitality", 2), ("Strength", 2), ("Endurance", 2),
        ("Dexterity", 1), ("Agility", 2), ("Intelligence", 1),
    };

    public static void Launch(Window mainWindow, int? runSeed)
    {
        var tempLog = new StringGameLog(new StringBuilder());
        var player = Player.CreateNewPlayer("Debug", "Runner", "Female",
            tempLog, new TerminalGuiInventoryLogger(tempLog));

        foreach ((string stat, int points) in Allocation)
            player.SpendSkillPoints(stat, points);

        // Announced rather than silent: a quick-start run is not a normal one — it uses a fixed
        // character and a reserved save slot — and a perf number taken from it must not be
        // mistaken for a number taken from real play.
        DebugLogger.LogGame("QUICKSTART",
            $"skipping title/difficulty/creation — difficulty={DifficultyData.GetName(DebugDifficulty)} "
            + $"slot={DebugSaveSlot} (reserved) seed={(runSeed.HasValue ? runSeed.Value.ToString() : "random")} "
            + $"points={string.Join("/", Allocation.Select(a => $"{a.Stat[..3]}{a.Points}"))}");

        GameScreen.Show(mainWindow, player, DebugDifficulty,
            saveSlot: DebugSaveSlot, runSeed: runSeed);
    }
}
