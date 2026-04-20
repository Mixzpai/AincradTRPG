using Terminal.Gui;

namespace SAOTRPG.Systems.Skills;

// Unique Skills state + dispatcher. All skills stack; passives apply in combat.
public static class UniqueSkillSystem
{
    public static HashSet<UniqueSkill> Unlocked { get; set; } = new();

    public static readonly Dictionary<UniqueSkill, UniqueSkillDef> Definitions = new()
    {
        [UniqueSkill.DualBlades] = new(UniqueSkill.DualBlades,
            "Dual Blades",
            "Wield two one-handed swords. Grants bonus damage and unlocks signature dual-blade sword skills.",
            "Unlocked when the System selects you on Floor 74.",
            Color.BrightCyan,
            new[] { "dual_double_circular", "dual_starburst_stream", "dual_the_eclipse" }),

        [UniqueSkill.HolySword] = new(UniqueSkill.HolySword,
            "Holy Sword",
            "Sword + shield mastery. Massive block chance and reflective counterstrike.",
            "Unlocked by defeating Heathcliff on Floor 75.",
            Color.BrightYellow,
            new[] { "holy_sacred_edge" }),

        [UniqueSkill.MartialArts] = new(UniqueSkill.MartialArts,
            "Martial Arts",
            "Unarmed combat mastery. Unlocks crushing hand-to-hand techniques.",
            "Learned from a hidden trial on Floor 2 (Progressive lore).",
            Color.White,
            new[] { "ma_embracer", "ma_flashing_palm" }),

        [UniqueSkill.KatanaMastery] = new(UniqueSkill.KatanaMastery,
            "Katana Mastery",
            "Your katana strikes carry elevated crit chance and reliable bleed.",
            "Earned through 100 katana kills.",
            Color.BrightMagenta,
            new[] { "kat_tsumujigaeshi", "kat_zekkuu" }),

        [UniqueSkill.DarknessBlade] = new(UniqueSkill.DarknessBlade,
            "Darkness Blade",
            "The Black Swordsman's stance. During Night, deal more damage and dodge more easily.",
            "Unlocked by defeating a floor boss during the Night phase.",
            Color.BrightMagenta,
            new[] { "dark_shadow_cleave" }),

        [UniqueSkill.BlazingEdge] = new(UniqueSkill.BlazingEdge,
            "Blazing Edge",
            "Your weapon strikes ignite. Bonus damage against Ice-affinity enemies.",
            "Attuned after felling a Volcanic-biome floor boss.",
            Color.BrightRed,
            new[] { "fire_flame_strike" }),

        [UniqueSkill.FrozenEdge] = new(UniqueSkill.FrozenEdge,
            "Frozen Edge",
            "Your weapon chills. Bonus damage against Fire-affinity enemies.",
            "Attuned after felling an Ice-biome floor boss.",
            Color.BrightBlue,
            new[] { "ice_glacial_slash" }),

        [UniqueSkill.ExtraSearch] = new(UniqueSkill.ExtraSearch,
            "Extra Skill: Search",
            "Argo's scouting art. Reveals nearby traps within a 3-tile radius.",
            "Earned after disarming 10 traps — the instinct sharpens with practice.",
            Color.BrightYellow,
            Array.Empty<string>()),
    };

    public static void Reset()
    {
        Unlocked.Clear();
    }

    public static bool Has(UniqueSkill skill) => Unlocked.Contains(skill);

    public static bool TryUnlock(UniqueSkill skill) => Unlocked.Add(skill);

    // Convenience: true when Dual Blades is unlocked. Used to gate OffHand
    // one-handed-sword equipping + the bonus offhand swing in combat.
    public static bool HasDualBlades() => Has(UniqueSkill.DualBlades);

    // Convenience: true when Martial Arts is unlocked.
    public static bool HasMartialArts() => Has(UniqueSkill.MartialArts);

    // ── Active-state predicates (checked at combat time) ──────────────
    public static bool IsDualBladesActive(string wpnType)
        => Has(UniqueSkill.DualBlades) && wpnType == "One-Handed Sword";

    public static bool IsHolySwordActive(string wpnType, bool hasShield)
        => Has(UniqueSkill.HolySword) && wpnType == "One-Handed Sword" && hasShield;

    public static bool IsMartialArtsActive(string wpnType)
        => Has(UniqueSkill.MartialArts) && wpnType == "Unarmed";

    public static bool IsKatanaMasteryActive(string wpnType)
        => Has(UniqueSkill.KatanaMastery) && wpnType == "Katana";

    public static bool IsDarknessBladeActive()
        => Has(UniqueSkill.DarknessBlade) && Map.DayNightCycle.PhaseName == "Night";

    // ── Damage / stat modifiers ───────────────────────────────────────
    public static int DamageBonusPercent(string wpnType, bool hasShield)
    {
        int bonus = 0;
        if (IsDualBladesActive(wpnType)) bonus += 15;
        if (IsMartialArtsActive(wpnType)) bonus += 10;
        if (IsKatanaMasteryActive(wpnType)) bonus += 10;
        if (IsDarknessBladeActive()) bonus += 20;
        return bonus;
    }

    public static int CritChanceBonusPercent(string wpnType)
    {
        int bonus = 0;
        if (IsMartialArtsActive(wpnType)) bonus += 20;
        if (IsKatanaMasteryActive(wpnType)) bonus += 10;
        return bonus;
    }

    public static int BlockChanceBonusPercent(string wpnType, bool hasShield)
        => IsHolySwordActive(wpnType, hasShield) ? 15 : 0;

    public static int DodgeBonusPercent()
        => IsDarknessBladeActive() ? 10 : 0;

    // On-hit proc chances (percent out of 100)
    public static int BleedProcChance(string wpnType)
        => IsKatanaMasteryActive(wpnType) ? 15 : 0;

    public static int BurnProcChance()
        => Has(UniqueSkill.BlazingEdge) ? 10 : 0;

    public static int SlowProcChance()
        => Has(UniqueSkill.FrozenEdge) ? 10 : 0;

    // Elemental multipliers against tagged mobs. Mob's Name/Tag is passed in.
    public static int ElementalBonusPercent(string monsterName)
    {
        string name = monsterName.ToLowerInvariant();
        int bonus = 0;
        if (Has(UniqueSkill.BlazingEdge) && (name.Contains("ice") || name.Contains("frost") || name.Contains("snow")))
            bonus += 25;
        if (Has(UniqueSkill.FrozenEdge) && (name.Contains("fire") || name.Contains("flame") || name.Contains("lava") || name.Contains("magma")))
            bonus += 25;
        return bonus;
    }

    // Reveals traps/hidden items in a radius if the player has Extra: Search.
    public static int SearchRadius() => Has(UniqueSkill.ExtraSearch) ? 3 : 0;

    // ── Unlock-check hooks (called from TurnManager at relevant events) ──

    // Weapon-kill milestones: KatanaMastery@100; Dual/Holy fallbacks for non-narrative paths.
    public static UniqueSkill? CheckWeaponKillMilestone(string wpnType, int weaponKills)
    {
        if (wpnType == "Katana" && weaponKills == 100 && TryUnlock(UniqueSkill.KatanaMastery))
            return UniqueSkill.KatanaMastery;
        if (wpnType == "Unarmed" && weaponKills == 30 && TryUnlock(UniqueSkill.MartialArts))
            return UniqueSkill.MartialArts;
        if (wpnType == "One-Handed Sword" && weaponKills == 50 && TryUnlock(UniqueSkill.DualBlades))
            return UniqueSkill.DualBlades;
        return null;
    }

    // Boss-kill-based unlocks: Darkness (Night), Blazing (Volcanic), Frozen (Ice).
    public static UniqueSkill? CheckBossKillUnlock(string biomeName)
    {
        if (Map.DayNightCycle.PhaseName == "Night" && TryUnlock(UniqueSkill.DarknessBlade))
            return UniqueSkill.DarknessBlade;
        string b = biomeName.ToLowerInvariant();
        if ((b.Contains("volcan") || b.Contains("lava") || b.Contains("fire")) && TryUnlock(UniqueSkill.BlazingEdge))
            return UniqueSkill.BlazingEdge;
        if ((b.Contains("ice") || b.Contains("frost") || b.Contains("snow") || b.Contains("tundra")) && TryUnlock(UniqueSkill.FrozenEdge))
            return UniqueSkill.FrozenEdge;
        return null;
    }

    // Trap-disarm tally drives ExtraSearch unlock.
    private static int _trapsDisarmed;
    public static UniqueSkill? OnTrapDisarmed()
    {
        _trapsDisarmed++;
        if (_trapsDisarmed >= 10 && TryUnlock(UniqueSkill.ExtraSearch))
            return UniqueSkill.ExtraSearch;
        return null;
    }
    public static int TrapsDisarmed { get => _trapsDisarmed; set => _trapsDisarmed = value; }
}
