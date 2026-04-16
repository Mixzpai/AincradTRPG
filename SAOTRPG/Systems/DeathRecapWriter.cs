using SAOTRPG.Entities;

namespace SAOTRPG.Systems;

// Writes a human-readable death recap .txt file to %LocalAppData%/AincradTRPG/deaths/.
// Silent on I/O failure — never crashes the game.
public static class DeathRecapWriter
{
    private static readonly string DeathDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AincradTRPG", "deaths");

    // Write a death recap text file. Returns the file path on success, null on failure.
    public static string? WriteRecap(Player player, int floor, int kills, int turns,
        string? killedBy, string grade, bool hardcore, int totalColEarned = 0,
        TimeSpan? playTime = null)
    {
        try
        {
            Directory.CreateDirectory(DeathDir);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"death_{timestamp}.txt";
            string filePath = Path.Combine(DeathDir, fileName);

            string mode = hardcore ? "HARDCORE" : "Normal";
            string killer = killedBy ?? "Unknown";

            string content =
                $"═══════════════════════════════════\n" +
                $"  DEATH RECAP — {DateTime.Now:yyyy-MM-dd HH:mm}\n" +
                $"═══════════════════════════════════\n" +
                $"\n" +
                $"  Player:     {player.FirstName} {player.LastName}\n" +
                $"  Level:      {player.Level}\n" +
                $"  Title:      {player.Title}\n" +
                $"  Mode:       {mode}\n" +
                $"\n" +
                $"  Floor:      {floor}\n" +
                $"  Killed by:  {killer}\n" +
                $"  Kills:      {kills}\n" +
                $"  Turns:      {turns}\n" +
                $"  Col Earned: {totalColEarned}\n" +
                $"  Col on Hand:{player.ColOnHand}\n" +
                $"  Rating:     {grade}\n" +
                (playTime != null ? $"  Play Time:  {playTime.Value.Hours}h {playTime.Value.Minutes:D2}m {playTime.Value.Seconds:D2}s\n" : "") +
                $"\n" +
                $"═══════════════════════════════════\n";

            File.WriteAllText(filePath, content);
            return filePath;
        }
        catch
        {
            return null;
        }
    }
}
