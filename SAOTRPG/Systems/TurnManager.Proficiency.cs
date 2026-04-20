namespace SAOTRPG.Systems;

// Weapon proficiency — per-type kills grant passive dmg + rank titles.
// Kill-count anchors L1=0, L25=25, L50=100, L75=500, L100=2000, L110=10000;
// geometric interpolation. At L25/50/75/100 player picks 1-of-2 fork passive.
// Forks use the FloorLevelUp modal pattern (GameScreen.Events.TalentPickRequested).
public partial class TurnManager
{
    // ── Legacy 15-tier rank ladder (display only) ─────────────────────
    // Bonus column unused; Kills is the UI rank-flip threshold.
    private static readonly (int Kills, int Bonus, string Rank)[] ProficiencyRanks =
    {
        (10, 1, "Novice"),       (25, 2, "Apprentice"),     (50, 4, "Journeyman"),
        (100, 7, "Expert"),      (200, 11, "Master"),       (350, 16, "Grandmaster"),
        (500, 22, "Sword Saint"),(750, 29, "Blade Dancer"), (1000, 37, "Weapon Lord"),
        (1500, 46, "Legendary"), (2000, 56, "Mythic"),      (3000, 67, "Transcendent"),
        (4500, 80, "Divine Edge"), (6000, 95, "Aincrad's Chosen"), (9999, 120, "The Black Swordsman"),
    };

    // Fork picks by weapon type: arr[0..3] = L25/50/75/100; 0=unpicked, 1/2=option.
    private readonly Dictionary<string, int[]> _weaponProficiencyForks = new();
    public const int ForkCount = 4; // L25, L50, L75, L100

    // Max level and kill-count anchors (geometric curve).
    public const int MaxProfLevel = 110;
    private static readonly (int Level, int Kills)[] CurveAnchors =
    {
        (1, 0), (25, 25), (50, 100), (75, 500), (100, 2000), (110, 10000),
    };
    private static readonly int[] ForkLevels = { 25, 50, 75, 100 };

    private readonly Dictionary<string, int> _weaponKills = new();

    // Pending fork picks: weapon type → list of levels (25/50/75/100) awaiting choice.
    private readonly Queue<(string WpnType, int ForkLevel)> _pendingForkPicks = new();

    // ── Public API ────────────────────────────────────────────────────

    public IReadOnlyDictionary<string, int> WeaponKills => _weaponKills;
    public IReadOnlyDictionary<string, int[]> WeaponProficiencyForks => _weaponProficiencyForks;

    public int GetWeaponKills(string weaponType) =>
        _weaponKills.GetValueOrDefault(weaponType, 0);

    // Fires on fork crossing (L25/50/75/100). UI opens ProficiencyForkDialog
    // then calls ApplyProficiencyFork.
    public event Action<string, int, ProficiencyFork, ProficiencyFork>? ProficiencyForkRequested;

    public static int ComputeLevel(int kills)
    {
        if (kills <= 0) return 1;
        // Linear interpolate kills→level inside anchor brackets.
        for (int i = 1; i < CurveAnchors.Length; i++)
        {
            var lo = CurveAnchors[i - 1];
            var hi = CurveAnchors[i];
            if (kills < hi.Kills)
            {
                double t = (double)(kills - lo.Kills) / (hi.Kills - lo.Kills);
                int lvl = lo.Level + (int)Math.Floor(t * (hi.Level - lo.Level));
                return Math.Clamp(lvl, 1, MaxProfLevel);
            }
        }
        return MaxProfLevel;
    }

    // Inverse of ComputeLevel on anchor points.
    public static int KillsForLevel(int level)
    {
        if (level <= 1) return 0;
        if (level >= MaxProfLevel) return CurveAnchors[^1].Kills;
        for (int i = 1; i < CurveAnchors.Length; i++)
        {
            var lo = CurveAnchors[i - 1];
            var hi = CurveAnchors[i];
            if (level <= hi.Level)
            {
                double t = (double)(level - lo.Level) / (hi.Level - lo.Level);
                return lo.Kills + (int)Math.Ceiling(t * (hi.Kills - lo.Kills));
            }
        }
        return CurveAnchors[^1].Kills;
    }

    public int GetProficiencyLevel(string weaponType) =>
        ComputeLevel(_weaponKills.GetValueOrDefault(weaponType, 0));

    // +Level/2 dmg (L25=+12, L100=+50, L110=+55) + fork bumps.
    public int GetProficiencyBonus(string weaponType)
    {
        int lvl = GetProficiencyLevel(weaponType);
        int bonus = lvl / 2;
        // Dmg forks folded here; stat-ramp forks apply via ApplyProficiencyFork.
        var picks = GetForkChoices(weaponType);
        if (picks[0] == 1) bonus += 2;  // Bloodfever flat dmg bump
        if (picks[3] == 1) bonus += 5;  // Weapon Master flat dmg bump
        return bonus;
    }

    // Rank name (cosmetic) + current kills + next threshold.
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

    // Non-dmg perks driven by level + fork picks.
    public (int CritBonus, int ParryBonus, int DodgeBonus) GetProficiencyPerks(string weaponType)
    {
        int lvl = GetProficiencyLevel(weaponType);
        int crit = 0, parry = 0, dodge = 0;
        if (lvl >= 50)  crit  += 2;
        if (lvl >= 60)  parry += 3;
        if (lvl >= 75)  dodge += 2;
        if (lvl >= 90)  crit  += 3;
        if (lvl >= 100) parry += 2;
        var picks = GetForkChoices(weaponType);
        if (picks[1] == 1) crit  += 3; // Keen Edge
        if (picks[1] == 2) parry += 3; // Guarded Stance
        return (crit, parry, dodge);
    }

    // Fork picks (length 4). Treat as read-only.
    public int[] GetForkChoices(string weaponType)
    {
        if (!_weaponProficiencyForks.TryGetValue(weaponType, out var arr))
            return new int[ForkCount];
        return arr;
    }

    // Fork index (0..3) for level 25/50/75/100; -1 otherwise.
    public static int ForkIndexForLevel(int level)
    {
        for (int i = 0; i < ForkLevels.Length; i++)
            if (ForkLevels[i] == level) return i;
        return -1;
    }

    public static int LevelForForkIndex(int forkIdx) =>
        forkIdx >= 0 && forkIdx < ForkLevels.Length ? ForkLevels[forkIdx] : -1;

    // True if weapon has unpicked forks (level ≥ fork threshold).
    public bool HasPendingFork(string weaponType)
    {
        int lvl = GetProficiencyLevel(weaponType);
        var picks = GetForkChoices(weaponType);
        for (int i = 0; i < ForkCount; i++)
        {
            if (lvl >= ForkLevels[i] && picks[i] == 0) return true;
        }
        return false;
    }

    // All pending (weapon, forkLevel) pairs. StatsDialog calls on open.
    public List<(string WpnType, int ForkLevel)> EnumeratePendingForks()
    {
        var list = new List<(string, int)>();
        foreach (var kvp in _weaponKills)
        {
            int lvl = ComputeLevel(kvp.Value);
            var picks = GetForkChoices(kvp.Key);
            for (int i = 0; i < ForkCount; i++)
                if (lvl >= ForkLevels[i] && picks[i] == 0)
                    list.Add((kvp.Key, ForkLevels[i]));
        }
        return list;
    }

    public void ApplyProficiencyFork(string weaponType, int forkLevel, int pickedOption)
    {
        int idx = ForkIndexForLevel(forkLevel);
        if (idx < 0) return;
        if (pickedOption != 1 && pickedOption != 2) return;
        if (!_weaponProficiencyForks.TryGetValue(weaponType, out var arr))
        {
            arr = new int[ForkCount];
            _weaponProficiencyForks[weaponType] = arr;
        }
        if (arr[idx] != 0) return; // already picked
        arr[idx] = pickedOption;
        ApplyForkPassive(weaponType, forkLevel, pickedOption);
    }

    // Stat impact of fork picks. Dmg components (L25/L100 opt1) in GetProficiencyBonus;
    // stat ramps land here.
    private void ApplyForkPassive(string weaponType, int forkLevel, int option)
    {
        switch (forkLevel)
        {
            case 25: // 1:Bloodfever +5% ATK / 2:Bulwark +5 DEF
                if (option == 1) _player.BaseAttack += Math.Max(1, _player.BaseAttack * 5 / 100);
                else             _player.BaseDefense += 5;
                break;
            case 50: // 1:Keen Edge +3 DEX / 2:Guarded Stance +2 VIT (parry in Perks)
                if (option == 1) _player.Dexterity += 3;
                else             _player.Vitality  += 2;
                break;
            case 75: // 1:Onslaught +5 INT / 2:Hardened Core +1 VIT
                if (option == 1) _player.Intelligence += 5;
                else             _player.Vitality     += 1;
                break;
            case 100: // 1:Weapon Master +5 STR/+3 DEX / 2:Immortal Vanguard +6 END/+2 VIT
                if (option == 1) { _player.Strength += 5; _player.Dexterity += 3; }
                else             { _player.Endurance += 6; _player.Vitality  += 2; }
                break;
        }
        _player.CurrentHealth = Math.Min(_player.CurrentHealth, _player.MaxHealth);
    }

    // Hydrate fork dict only — save already has adjusted stats. No delta replay.
    internal void RehydrateForkChoices(Dictionary<string, int[]>? saved)
    {
        if (saved == null) return;
        foreach (var kvp in saved)
        {
            if (kvp.Value == null || kvp.Value.Length != ForkCount) continue;
            _weaponProficiencyForks[kvp.Key] = (int[])kvp.Value.Clone();
        }
    }

    internal Dictionary<string, int[]> SnapshotForkChoices()
    {
        var snap = new Dictionary<string, int[]>();
        foreach (var kvp in _weaponProficiencyForks)
            snap[kvp.Key] = (int[])kvp.Value.Clone();
        return snap;
    }

    // Fires ProficiencyForkRequested on fork crossings. Called by HandleMonsterKill.
    internal void CheckForkThresholdOnKill(string weaponType, int oldLevel, int newLevel)
    {
        if (oldLevel == newLevel) return;
        for (int i = 0; i < ForkCount; i++)
        {
            int fl = ForkLevels[i];
            if (oldLevel < fl && newLevel >= fl)
            {
                var picks = GetForkChoices(weaponType);
                if (picks[i] != 0) continue; // already picked somehow
                var (opt1, opt2) = GetForkOptions(fl);
                _log.LogSystem($"  ** {weaponType} proficiency reached L{fl}! Choose a passive ** ");
                ProficiencyForkRequested?.Invoke(weaponType, fl, opt1, opt2);
            }
        }
    }

    public static (ProficiencyFork Opt1, ProficiencyFork Opt2) GetForkOptions(int forkLevel) =>
        forkLevel switch
        {
            25  => (new("Bloodfever",      "+5% Attack (flat +2 weapon dmg)"),
                    new("Bulwark",         "+5 Defense")),
            50  => (new("Keen Edge",       "+3 Dexterity (≈ +3% Crit)"),
                    new("Guarded Stance",  "+2 Vitality, +3% Parry with this weapon")),
            75  => (new("Onslaught",       "+5 Intelligence (skill damage / cooldown)"),
                    new("Hardened Core",   "+1 Vitality (+10 Max HP)")),
            100 => (new("Weapon Master",   "+5 STR, +3 DEX (flat +5 weapon dmg)"),
                    new("Immortal Vanguard", "+6 END, +2 VIT (+15 DEF, +20 HP)")),
            _   => (new("?", ""), new("?", "")),
        };

    // Cosmetic level→rank mapping via kill-count anchors.
    public static string RankTitleForLevel(int level)
    {
        int kills = KillsForLevel(level);
        string rank = "Unranked";
        foreach (var r in ProficiencyRanks)
            if (kills >= r.Kills) rank = r.Rank;
        return rank;
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

// Fork-option description; in Systems so UI (ProficiencyForkDialog) can consume.
public record ProficiencyFork(string Name, string Description);
