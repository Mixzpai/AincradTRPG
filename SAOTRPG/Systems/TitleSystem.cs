using SAOTRPG.Entities;
using SAOTRPG.Items;

namespace SAOTRPG.Systems;

// FB-058 Titles — passive nameplates unlocked via Bestiary kills + flags.
// One active at a time; equip = flat base-stat delta. Save persists IDs only.
// TurnManager.CheckTitleUnlocks runs post-kill; TitleUnlocked banner fires.
public static class TitleSystem
{
    // Flat passive. VsTag = "vs Tag" conditional (reserved; currently applied flat).
    public record TitleDef(
        string Id,
        string DisplayName,
        string Description,
        StatType BonusStat,
        int BonusValue,
        string? VsTag = null,
        string? RequirementNote = null);

    // Canonical 15-title starter roster (FB-058 batch 1).
    public static readonly Dictionary<string, TitleDef> Titles = new()
    {
        ["title_boar_slayer"]        = new("title_boar_slayer",        "Boar Slayer",
            "+5% Attack vs beast (10 Frenzy Boar kills).",
            StatType.Attack, 2, VsTag: "beast",
            RequirementNote: "Defeat 10 Frenzy Boars."),

        ["title_beast_hunter"]       = new("title_beast_hunter",       "Beast Hunter",
            "+5% Attack vs beast (100 beast kills).",
            StatType.Attack, 3, VsTag: "beast",
            RequirementNote: "Defeat 100 beast-type enemies."),

        ["title_beast_lord"]         = new("title_beast_lord",         "Beast Lord",
            "+10% Attack vs beast (1000 beast kills).",
            StatType.Attack, 6, VsTag: "beast",
            RequirementNote: "Defeat 1000 beast-type enemies."),

        ["title_nepent_cutter"]      = new("title_nepent_cutter",      "Nepent Cutter",
            "+3 Dexterity (10 Nepent kills).",
            StatType.Dexterity, 3,
            RequirementNote: "Defeat 10 Nepents."),

        ["title_kobold_crusher"]     = new("title_kobold_crusher",     "Kobold Crusher",
            "+5% Attack vs humanoid (10 Kobold kills).",
            StatType.Attack, 2, VsTag: "humanoid",
            RequirementNote: "Defeat 10 Kobolds."),

        ["title_titan_breaker"]      = new("title_titan_breaker",      "Titan Breaker",
            "+3 Strength (10 Taurus kills).",
            StatType.Strength, 3,
            RequirementNote: "Defeat 10 Taurus-class enemies."),

        ["title_dragon_slayer"]      = new("title_dragon_slayer",      "Dragon Slayer",
            "+10% Attack vs dragon (100 dragon kills).",
            StatType.Attack, 5, VsTag: "dragon",
            RequirementNote: "Defeat 100 dragon-type enemies."),

        ["title_dragon_lord"]        = new("title_dragon_lord",        "Dragon Lord",
            "+20% Attack vs dragon (1000 dragon kills).",
            StatType.Attack, 10, VsTag: "dragon",
            RequirementNote: "Defeat 1000 dragon-type enemies."),

        ["title_cardinal_breaker"]   = new("title_cardinal_breaker",   "Cardinal Breaker",
            "+5 Intelligence (10 Cardinal Error kills).",
            StatType.Intelligence, 5,
            RequirementNote: "Defeat 10 Cardinal Errors."),

        ["title_hollow_walker"]      = new("title_hollow_walker",      "Hollow Walker",
            "+5% Attack vs hollow (100 hollow kills).",
            StatType.Attack, 3, VsTag: "hollow",
            RequirementNote: "Defeat 100 hollow-type enemies."),

        ["title_undead_exorcist"]    = new("title_undead_exorcist",    "Undead Exorcist",
            "+5% Attack vs undead (100 undead kills).",
            StatType.Attack, 3, VsTag: "undead",
            RequirementNote: "Defeat 100 undead-type enemies."),

        ["title_insect_crusher"]     = new("title_insect_crusher",     "Insect Crusher",
            "+3 Agility (100 insect kills).",
            StatType.Agility, 3,
            RequirementNote: "Defeat 100 insect-type enemies."),

        ["title_the_black_swordsman"] = new("title_the_black_swordsman", "The Black Swordsman",
            "+10 Attack (9999 total kills). The title that needs no introduction.",
            StatType.Attack, 10,
            RequirementNote: "Achieve 9999 total kills."),

        ["title_survivor"]           = new("title_survivor",           "Survivor",
            "+10 MaxHP (reached Floor 50 without death).",
            StatType.Health, 10,
            RequirementNote: "Reach Floor 50 without dying (floor clear milestone)."),

        ["title_beginner_slayer"]    = new("title_beginner_slayer",    "Beginner Slayer",
            "+1 to all core attributes (100 total kills).",
            StatType.Strength, 1,
            RequirementNote: "Achieve 100 total kills."),
    };

    // Banner fires when a new title is unlocked so the UI/log can log it.
    public static event Action<TitleDef>? TitleUnlocked;

    // Applies a title's flat stat bonus to the player. Mirrors the pattern
    // used by the enchant-shrine — it pokes BaseAttack / BaseDefense / etc.
    // so the bonus threads through every derived stat automatically.
    public static void ApplyTitleBonus(Player player, TitleDef title, int sign = +1)
    {
        int v = title.BonusValue * sign;
        switch (title.BonusStat)
        {
            case StatType.Attack:       player.BaseAttack += v; break;
            case StatType.Defense:      player.BaseDefense += v; break;
            case StatType.Speed:        player.BaseSpeed += v; break;
            case StatType.SkillDamage:  player.BaseSkillDamage += v; break;
            case StatType.Strength:     player.Strength += v; break;
            case StatType.Vitality:     player.Vitality += v; break;
            case StatType.Endurance:    player.Endurance += v; break;
            case StatType.Dexterity:    player.Dexterity += v; break;
            case StatType.Agility:      player.Agility += v; break;
            case StatType.Intelligence: player.Intelligence += v; break;
            case StatType.Health:
                // Flat MaxHP is derived from Vitality, so grant +1 Vit per
                // 10 HP requested and round up. For +10 HP → +1 Vit.
                player.Vitality += Math.Max(1, v / 10);
                player.CurrentHealth = Math.Min(player.CurrentHealth + v, player.MaxHealth);
                break;
        }

        // Beginner Slayer flavor bonus: +1 to the other five attributes
        // on top of the registered +1 Strength. Keeps the definition a
        // single StatType while delivering the "all stats" promise.
        if (title.Id == "title_beginner_slayer")
        {
            player.Vitality    += sign;
            player.Endurance   += sign;
            player.Dexterity   += sign;
            player.Agility     += sign;
            player.Intelligence+= sign;
        }
    }

    // Equip / swap / unequip logic. Handles the previous-active bonus removal.
    public static void SetActiveTitle(Player player, string? newTitleId)
    {
        if (player.ActiveTitleId == newTitleId) return;

        // Remove the old title's bonus.
        if (player.ActiveTitleId != null &&
            Titles.TryGetValue(player.ActiveTitleId, out var oldDef))
        {
            ApplyTitleBonus(player, oldDef, sign: -1);
        }

        player.ActiveTitleId = newTitleId;

        // Apply the new title's bonus.
        if (newTitleId != null && Titles.TryGetValue(newTitleId, out var newDef))
        {
            ApplyTitleBonus(player, newDef, sign: +1);
        }
    }

    // Unlock dispatcher — records the title in Player.UnlockedTitleIds and
    // fires the banner event exactly once per title.
    public static bool TryUnlock(Player player, string titleId)
    {
        if (!Titles.TryGetValue(titleId, out var def)) return false;
        if (!player.UnlockedTitleIds.Add(titleId)) return false;
        TitleUnlocked?.Invoke(def);
        return true;
    }

    // Bestiary-driven unlock check. Called after every kill — walks the
    // registry, evaluating each title's requirement against the current
    // Bestiary / total-kill state. Cheap (N titles × 1 lookup each).
    public static void CheckKillUnlocks(Player player, int totalKillCount,
        IReadOnlyDictionary<string, int> speciesKills,
        IReadOnlyDictionary<string, int> tagKills)
    {
        // Species-specific milestones.
        if (speciesKills.GetValueOrDefault("Frenzy Boar") >= 10)
            TryUnlock(player, "title_boar_slayer");
        if (speciesKills.GetValueOrDefault("Little Nepent") >= 10
            || speciesKills.GetValueOrDefault("Nepent") >= 10)
            TryUnlock(player, "title_nepent_cutter");
        if (speciesKills.GetValueOrDefault("Ruin Kobold Trooper") >= 10
            || speciesKills.GetValueOrDefault("Kobold") >= 10
            || speciesKills.GetValueOrDefault("Kobold Sentinel") >= 10)
            TryUnlock(player, "title_kobold_crusher");
        int taurusKills = speciesKills.GetValueOrDefault("Lesser Taurus")
            + speciesKills.GetValueOrDefault("Heavy Hammer Taurus")
            + speciesKills.GetValueOrDefault("Trembling Ox");
        if (taurusKills >= 10)
            TryUnlock(player, "title_titan_breaker");
        if (speciesKills.GetValueOrDefault("Cardinal Error") >= 10)
            TryUnlock(player, "title_cardinal_breaker");

        // Tag-based milestones (loot-tag categories).
        if (tagKills.GetValueOrDefault("beast") >= 100)
            TryUnlock(player, "title_beast_hunter");
        if (tagKills.GetValueOrDefault("beast") >= 1000)
            TryUnlock(player, "title_beast_lord");
        if (tagKills.GetValueOrDefault("dragon") >= 100)
            TryUnlock(player, "title_dragon_slayer");
        if (tagKills.GetValueOrDefault("dragon") >= 1000)
            TryUnlock(player, "title_dragon_lord");
        if (tagKills.GetValueOrDefault("hollow") >= 100)
            TryUnlock(player, "title_hollow_walker");
        if (tagKills.GetValueOrDefault("undead") >= 100)
            TryUnlock(player, "title_undead_exorcist");
        if (tagKills.GetValueOrDefault("insect") >= 100)
            TryUnlock(player, "title_insect_crusher");

        // Total-kill milestones.
        if (totalKillCount >= 100)
            TryUnlock(player, "title_beginner_slayer");
        if (totalKillCount >= 9999)
            TryUnlock(player, "title_the_black_swordsman");
    }

    // Reaching Floor 50 without death unlock — TurnManager fires this from
    // floor-change handler.
    public static void CheckFloor50Survivor(Player player, int floor)
    {
        if (floor >= 50)
            TryUnlock(player, "title_survivor");
    }
}
