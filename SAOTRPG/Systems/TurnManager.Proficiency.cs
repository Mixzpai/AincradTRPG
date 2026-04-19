namespace SAOTRPG.Systems;

// Weapon proficiency system — kills per weapon type grant cumulative
// passive damage bonuses and rank titles.
//
// The legacy 15-tier "Novice → The Black Swordsman" ladder is a cosmetic
// display draped over a numeric 110-level curve. Kill-count anchors
// L1=0, L25=25, L50=100, L75=500, L100=2000, L110=10000 — Kills-per-level
// is interpolated geometrically between anchors. At L25/L50/L75/L100 the
// player picks 1 of 2 passive "fork" bonuses per weapon type
// (Bloodfever/Bulwark, Keen Edge/Guarded Stance, Onslaught/Hardened Core,
// Weapon Master/Immortal Vanguard). Forks fire through the same
// FloorLevelUp-style modal pattern used by talent picks
// (GameScreen.Events.cs TalentPickRequested hook).
public partial class TurnManager
{
    // ── Legacy 15-tier cosmetic rank ladder (display only) ────────────
    // The Bonus column is NO LONGER used for combat — level-based
    // bonuses below supersede it. The Kills column is the kill-count
    // threshold at which the rank NAME flips in the UI.
    private static readonly (int Kills, int Bonus, string Rank)[] ProficiencyRanks =
    {
        (10, 1, "Novice"),       (25, 2, "Apprentice"),     (50, 4, "Journeyman"),
        (100, 7, "Expert"),      (200, 11, "Master"),       (350, 16, "Grandmaster"),
        (500, 22, "Sword Saint"),(750, 29, "Blade Dancer"), (1000, 37, "Weapon Lord"),
        (1500, 46, "Legendary"), (2000, 56, "Mythic"),      (3000, 67, "Transcendent"),
        (4500, 80, "Divine Edge"), (6000, 95, "Aincrad's Chosen"), (9999, 120, "The Black Swordsman"),
    };

    // Fork passives picked by the player at L25/L50/L75/L100. Each entry
    // is 0 (unpicked), 1 (first option), or 2 (second option).
    // Keyed by weapon type.
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

    // Read-only access to weapon kill counts for UI display.
    public IReadOnlyDictionary<string, int> WeaponKills => _weaponKills;

    // Per-weapon fork choices (length 4, 0=unpicked, 1 or 2 = picked option).
    public IReadOnlyDictionary<string, int[]> WeaponProficiencyForks => _weaponProficiencyForks;

    // Kill count for a specific weapon type (used by sword skill unlocks).
    public int GetWeaponKills(string weaponType) =>
        _weaponKills.GetValueOrDefault(weaponType, 0);

    // Fires when the player crosses L25/50/75/100 for a weapon and must pick a fork.
    // UI layer opens ProficiencyForkDialog and calls ApplyProficiencyFork afterwards.
    public event Action<string, int, ProficiencyFork, ProficiencyFork>? ProficiencyForkRequested;

    // Derive the current proficiency level from kills via the geometric anchor curve.
    public static int ComputeLevel(int kills)
    {
        if (kills <= 0) return 1;
        // Walk anchors; inside a bracket, linearly interpolate kills→level.
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

    // Kills required to reach a given level (inverse of ComputeLevel on anchor points).
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

    // Returns the proficiency level for a weapon type.
    public int GetProficiencyLevel(string weaponType) =>
        ComputeLevel(_weaponKills.GetValueOrDefault(weaponType, 0));

    // Current proficiency bonus damage for the given weapon type.
    // Replaces the old 15-tier Bonus column with a smooth L1→L110 curve:
    // +Level/2 damage, with a small bonus from each picked fork passive.
    public int GetProficiencyBonus(string weaponType)
    {
        int lvl = GetProficiencyLevel(weaponType);
        int bonus = lvl / 2; // L25=+12, L50=+25, L75=+37, L100=+50, L110=+55
        // Fork damage components (Bloodfever / Weapon Master) — folded into
        // the raw damage number here; per-stat ramps (Defense, BlockChance)
        // apply via ApplyProficiencyFork directly to Player stats.
        var picks = GetForkChoices(weaponType);
        if (picks[0] == 1) bonus += 2;  // Bloodfever flat dmg bump
        if (picks[3] == 1) bonus += 5;  // Weapon Master flat dmg bump
        return bonus;
    }

    // Returns current rank name and next rank info for a weapon type.
    // Rank name is the legacy cosmetic string; progress values are the
    // kill count and next-rank kill threshold.
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
    // Now driven off proficiency level + fork picks rather than raw kill counts.
    public (int CritBonus, int ParryBonus, int DodgeBonus) GetProficiencyPerks(string weaponType)
    {
        int lvl = GetProficiencyLevel(weaponType);
        int crit = 0, parry = 0, dodge = 0;
        // Level-interpolated checkpoints roughly mirror the old kill-count perks.
        if (lvl >= 50)  crit  += 2;    // was "Journeyman" at 50 kills
        if (lvl >= 60)  parry += 3;    // was "Master" at 200 kills
        if (lvl >= 75)  dodge += 2;    // was "Sword Saint" at 500 kills
        if (lvl >= 90)  crit  += 3;    // was "Weapon Lord" at 1000 kills
        if (lvl >= 100) parry += 2;    // was "Mythic" at 2000 kills
        // Fork perks
        var picks = GetForkChoices(weaponType);
        if (picks[1] == 1) crit  += 3; // Keen Edge
        if (picks[1] == 2) parry += 3; // Guarded Stance (repurposed as parry, remapped from BlockChance)
        return (crit, parry, dodge);
    }

    // Fork choices (length 4) for a weapon type; always returns a fresh-ish
    // array — callers treat it as read-only.
    public int[] GetForkChoices(string weaponType)
    {
        if (!_weaponProficiencyForks.TryGetValue(weaponType, out var arr))
            return new int[ForkCount];
        return arr;
    }

    // Fork index (0..3) for a given level (25→0, 50→1, 75→2, 100→3).
    // Returns -1 if the level is not a fork level.
    public static int ForkIndexForLevel(int level)
    {
        for (int i = 0; i < ForkLevels.Length; i++)
            if (ForkLevels[i] == level) return i;
        return -1;
    }

    public static int LevelForForkIndex(int forkIdx) =>
        forkIdx >= 0 && forkIdx < ForkLevels.Length ? ForkLevels[forkIdx] : -1;

    // True if a weapon has pending forks owed to the player
    // (level ≥ fork level but choice == 0).
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

    // Enumerate any pending forks (weapon type + fork level) that the player
    // has passed but not yet picked. UI layer calls this on StatsDialog open.
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

    // Record the player's fork pick (pickedOption: 1 or 2) and apply it.
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

    // Apply the stat impact of a fork passive directly to the player.
    // Placeholder numbers — rebalance later. Fork damage components
    // (opt 1 of L25, opt 1 of L100) fold into GetProficiencyBonus above;
    // the stat-based components land here.
    private void ApplyForkPassive(string weaponType, int forkLevel, int option)
    {
        switch (forkLevel)
        {
            case 25:
                // Bloodfever (1): +5% Attack — flat ATK bump scales with current base.
                // Bulwark    (2): +5 Defense.
                if (option == 1) _player.BaseAttack += Math.Max(1, _player.BaseAttack * 5 / 100);
                else             _player.BaseDefense += 5;
                break;
            case 50:
                // Keen Edge       (1): +3% CritRate via Dexterity remap (+3 Dex).
                // Guarded Stance  (2): +3 parry — already in GetProficiencyPerks.
                if (option == 1) _player.Dexterity += 3;
                else             _player.Vitality  += 2;
                break;
            case 75:
                // Onslaught     (1): -1 SkillCooldown → Intelligence remap (+5 INT).
                // Hardened Core (2): +10 MaxHP → Vitality remap (+1 VIT ≈ +10 HP).
                if (option == 1) _player.Intelligence += 5;
                else             _player.Vitality     += 1;
                break;
            case 100:
                // Weapon Master     (1): +10% Attack, +3% CritRate → STR +5, DEX +3.
                //                       (flat +5 dmg also in GetProficiencyBonus)
                // Immortal Vanguard (2): +15 Defense, +5% BlockChance → END +6, VIT +2.
                if (option == 1) { _player.Strength += 5; _player.Dexterity += 3; }
                else             { _player.Endurance += 6; _player.Vitality  += 2; }
                break;
        }
        _player.CurrentHealth = Math.Min(_player.CurrentHealth, _player.MaxHealth);
    }

    // Replay all fork effects on load (does NOT invoke the stat deltas — the
    // save already contains the adjusted Base* / attribute values, so we just
    // hydrate the dictionary). Called from LoadFromSave.
    internal void RehydrateForkChoices(Dictionary<string, int[]>? saved)
    {
        if (saved == null) return;
        foreach (var kvp in saved)
        {
            if (kvp.Value == null || kvp.Value.Length != ForkCount) continue;
            _weaponProficiencyForks[kvp.Key] = (int[])kvp.Value.Clone();
        }
    }

    // Serialize current fork state for SaveData.
    internal Dictionary<string, int[]> SnapshotForkChoices()
    {
        var snap = new Dictionary<string, int[]>();
        foreach (var kvp in _weaponProficiencyForks)
            snap[kvp.Key] = (int[])kvp.Value.Clone();
        return snap;
    }

    // Called by Combat.HandleMonsterKill when a kill crosses a fork threshold.
    // Emits the ProficiencyForkRequested event — UI opens picker modal.
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

    // Authoring table for the fork options shown in the picker modal.
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

    // Map a proficiency level to a display rank title by walking the 15-tier
    // ladder at its kill-count anchors (cosmetic — the numeric level is the
    // real source of truth).
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

// Lightweight record for fork-option description; kept in the Systems
// namespace so the UI layer (ProficiencyForkDialog) can consume it without
// reaching into TurnManager internals.
public record ProficiencyFork(string Name, string Description);
