namespace SAOTRPG.Systems;

// Weapon proficiency system — kills per weapon type grant cumulative
// passive damage bonuses and rank titles.
public partial class TurnManager
{
    private static readonly (int Kills, int Bonus, string Rank)[] ProficiencyRanks =
    {
        (10, 1, "Novice"),       (25, 2, "Apprentice"),     (50, 4, "Journeyman"),
        (100, 7, "Expert"),      (200, 11, "Master"),       (350, 16, "Grandmaster"),
        (500, 22, "Sword Saint"),(750, 29, "Blade Dancer"), (1000, 37, "Weapon Lord"),
        (1500, 46, "Legendary"), (2000, 56, "Mythic"),      (3000, 67, "Transcendent"),
        (4500, 80, "Divine Edge"), (6000, 95, "Aincrad's Chosen"), (9999, 120, "The Black Swordsman"),
    };

    private readonly Dictionary<string, int> _weaponKills = new();

    // Current proficiency bonus damage for the given weapon type.
    public int GetProficiencyBonus(string weaponType)
    {
        int kills = _weaponKills.GetValueOrDefault(weaponType, 0);
        int bonus = 0;
        foreach (var rank in ProficiencyRanks)
            if (kills >= rank.Kills) bonus = rank.Bonus;
        return bonus;
    }

    // Read-only access to weapon kill counts for UI display.
    public IReadOnlyDictionary<string, int> WeaponKills => _weaponKills;

    // Kill count for a specific weapon type (used by sword skill unlocks).
    public int GetWeaponKills(string weaponType) =>
        _weaponKills.GetValueOrDefault(weaponType, 0);

    // Returns current rank name and next rank info for a weapon type.
    public (string Rank, int Kills, int NextAt, string NextRank) GetProficiencyInfo(string weaponType)
    {
        int kills = _weaponKills.GetValueOrDefault(weaponType, 0);
        string rank = "Unranked";
        int nextAt = ProficiencyRanks[0].Kills;
        string nextRank = ProficiencyRanks[0].Rank;
        for (int i = 0; i < ProficiencyRanks.Length; i++)
        {
            if (kills >= ProficiencyRanks[i].Kills)
            {
                rank = ProficiencyRanks[i].Rank;
                if (i + 1 < ProficiencyRanks.Length)
                { nextAt = ProficiencyRanks[i + 1].Kills; nextRank = ProficiencyRanks[i + 1].Rank; }
                else
                { nextAt = -1; nextRank = "MAX"; }
            }
        }
        return (rank, kills, nextAt, nextRank);
    }

    // Per-weapon-type rank perks: small bonuses beyond raw damage.
    public (int CritBonus, int ParryBonus, int DodgeBonus) GetProficiencyPerks(string weaponType)
    {
        int kills = _weaponKills.GetValueOrDefault(weaponType, 0);
        int crit = 0, parry = 0, dodge = 0;
        if (kills >= 50)  crit += 2;   // Journeyman: +2% crit
        if (kills >= 200) parry += 3;  // Master: +3% parry
        if (kills >= 500) dodge += 2;  // Sword Saint: +2% dodge
        if (kills >= 1000) crit += 3;  // Weapon Lord: +3% more crit
        if (kills >= 2000) parry += 2; // Mythic: +2% more parry
        return (crit, parry, dodge);
    }

    private static string GetRankUpFlavor(string rank) => rank switch
    {
        "Novice"           => "You're getting the hang of this.",
        "Apprentice"       => "Your strikes grow more confident.",
        "Journeyman"       => "Enemies should start worrying.",
        "Expert"           => "Your technique is razor-sharp.",
        "Master"           => "The weapon feels like an extension of yourself.",
        "Grandmaster"      => "Few in Aincrad can match your skill.",
        "Sword Saint"      => "Your blade sings with every swing.",
        "Blade Dancer"     => "Combat becomes art in your hands.",
        "Weapon Lord"      => "Legends speak of warriors like you.",
        "Legendary"        => "Your name echoes through every floor.",
        "Mythic"           => "Even floor bosses hesitate before you.",
        "Transcendent"     => "You've surpassed what was thought possible.",
        "Divine Edge"      => "The system itself bends to your will.",
        "Aincrad's Chosen" => "There is no one stronger. Not anymore.",
        "The Black Swordsman" => "This world is your domain now.",
        _                  => ""
    };
}
