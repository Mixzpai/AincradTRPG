using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Systems.Story;
using SAOTRPG.UI;

namespace SAOTRPG.Systems;

// FB-063 Guild System. Eight SAO-canon guilds + one player-founded guild.
// Each guild has a canon leader, HQ floor, join requirements (level + karma
// window), and a flat passive bonus applied at join-time via Player base
// stat pokes (mirroring TitleSystem). Membership is single-guild — joining
// a new guild forces leaving the current one with a karma + rep penalty.
//
// Recruitment NPCs on guild HQ floors gate the "Prove yourself" quest. On
// successful turn-in the player is inducted (bonus applied, active id set,
// rep seeded at +10). Signature quests become available post-join.
public static class GuildSystem
{
    // Stat bonus entry applied by ApplyGuildPerk. Weapon-conditional bonuses
    // are represented as a separate WeaponTypeBonus tuple so apply-time can
    // skip them (the combat hook inspects ActiveGuildId instead).
    public record GuildDef(
        Faction Id,
        string DisplayName,
        string CanonLeader,
        int HqFloor,
        string HqName,
        int MinLevel,
        int MinKarma,
        int MaxKarma,
        (StatType Stat, int Value)[] PerkBonuses,
        string PerkFlavor,
        bool IsFateSealed,
        int? DissolvesOnFloor,
        string Description,
        string RecruiterName);

    // The 8 canon guilds + player-founded placeholder. Order roughly follows
    // HQ floor ascending so the roster dialog reads naturally.
    public static readonly Dictionary<Faction, GuildDef> Guilds = new()
    {
        [Faction.AincradLiberationForce] = new(
            Id: Faction.AincradLiberationForce,
            DisplayName: "Aincrad Liberation Force",
            CanonLeader: "Kibaou",
            HqFloor: 1,
            HqName: "Black Iron Palace (TOB)",
            MinLevel: 1,
            MinKarma: -30,
            MaxKarma: 100,
            PerkBonuses: new[]
            {
                (StatType.Vitality, 2), (StatType.Strength, 2), (StatType.Endurance, 2),
                (StatType.Dexterity, 2), (StatType.Agility, 2), (StatType.Intelligence, 2),
            },
            PerkFlavor: "+5% XP, +2 all attributes (ALF Frontline doctrine).",
            IsFateSealed: false,
            DissolvesOnFloor: null,
            Description: "The Aincrad Liberation Force — Kibaou's people's army. Low bar to join, focus on numbers and mutual aid.",
            RecruiterName: "Kibaou"),

        [Faction.MoonlitBlackCats] = new(
            Id: Faction.MoonlitBlackCats,
            DisplayName: "Moonlit Black Cats",
            CanonLeader: "Keita",
            HqFloor: 10,
            HqName: "Sunshine Forest (F10)",
            MinLevel: 5,
            MinKarma: -20,
            MaxKarma: 100,
            PerkBonuses: new[] { (StatType.Vitality, 5), (StatType.Defense, 3) },
            PerkFlavor: "+5 Vitality, +3 Defense (friends close, ranks closer).",
            IsFateSealed: true,
            DissolvesOnFloor: 27,
            Description: "A small, cheerful guild of friends. (FATE-SEALED: disbands on F27 by canon tragedy.)",
            RecruiterName: "Keita"),

        [Faction.Fuurinkazan] = new(
            Id: Faction.Fuurinkazan,
            DisplayName: "Fuurinkazan",
            CanonLeader: "Klein",
            HqFloor: 20,
            HqName: "Taft (F20)",
            MinLevel: 10,
            MinKarma: -100,
            MaxKarma: 100,
            PerkBonuses: new[] { (StatType.Dexterity, 5) },
            PerkFlavor: "+5 Dexterity (+crit); +10 Attack when Katana is equipped.",
            IsFateSealed: false,
            DissolvesOnFloor: null,
            Description: "Klein's samurai brotherhood. Katana-focused. Any karma welcome — Klein trusts gut over paperwork.",
            RecruiterName: "Klein's Second"),

        [Faction.LegendBraves] = new(
            Id: Faction.LegendBraves,
            DisplayName: "Legend Braves",
            CanonLeader: "Schmitt",
            HqFloor: 25,
            HqName: "F25 midway camp",
            MinLevel: 15,
            MinKarma: 0,
            MaxKarma: 100,
            PerkBonuses: new[] { (StatType.Attack, 5) },
            PerkFlavor: "+5 Attack; +15 Attack vs Laughing Coffin mobs.",
            IsFateSealed: false,
            DissolvesOnFloor: null,
            Description: "An up-and-coming clearing guild out to make a name by hunting PKers.",
            RecruiterName: "Schmitt"),

        [Faction.DivineDragonAlliance] = new(
            Id: Faction.DivineDragonAlliance,
            DisplayName: "Divine Dragon Alliance",
            CanonLeader: "Lind",
            HqFloor: 40,
            HqName: "Frontline Camp (F40)",
            MinLevel: 15,
            MinKarma: 0,
            MaxKarma: 100,
            PerkBonuses: new[] { (StatType.Vitality, 10), (StatType.Defense, 5) },
            PerkFlavor: "+10 Vitality (+100 MaxHP), +5 Defense.",
            IsFateSealed: false,
            DissolvesOnFloor: null,
            Description: "Lind's rival clearing guild. Heavy armor, disciplined formations.",
            RecruiterName: "Lind"),

        [Faction.KnightsOfBlood] = new(
            Id: Faction.KnightsOfBlood,
            DisplayName: "Knights of the Blood Oath",
            CanonLeader: "Heathcliff",
            HqFloor: 55,
            HqName: "Granzam (F55)",
            MinLevel: 25,
            MinKarma: 30,
            MaxKarma: 100,
            PerkBonuses: new[] { (StatType.Defense, 8), (StatType.Vitality, 3) },
            PerkFlavor: "+8 Defense, +3 Vitality (Heathcliff's tempered steel).",
            IsFateSealed: false,
            DissolvesOnFloor: null,
            Description: "The premier clearing guild under Heathcliff. Honor-gated — shady applicants turned away.",
            RecruiterName: "Godfree"),

        [Faction.SleepingKnights] = new(
            Id: Faction.SleepingKnights,
            DisplayName: "Sleeping Knights",
            CanonLeader: "Yuuki",
            HqFloor: 60,
            HqName: "Hideout Garden (F60)",
            MinLevel: 50,
            MinKarma: 50,
            MaxKarma: 100,
            PerkBonuses: new[]
            {
                (StatType.Vitality, 3), (StatType.Strength, 3), (StatType.Endurance, 3),
                (StatType.Dexterity, 3), (StatType.Agility, 3), (StatType.Intelligence, 3),
            },
            PerkFlavor: "+3 all attributes, +5 Dexterity (Yuuki's legacy).",
            IsFateSealed: false,
            DissolvesOnFloor: null,
            Description: "Yuuki's elite band — invites extended only to high-karma veterans.",
            RecruiterName: "Siune"),

        [Faction.LaughingCoffin] = new(
            Id: Faction.LaughingCoffin,
            DisplayName: "Laughing Coffin",
            CanonLeader: "PoH",
            HqFloor: 75,
            HqName: "Hidden grotto (F75)",
            MinLevel: 30,
            MinKarma: -100,
            MaxKarma: -50,
            PerkBonuses: new[] { (StatType.Attack, 10), (StatType.Agility, 5) },
            PerkFlavor: "+10 Attack, +5 Agility, +20% Backstab (PoH's blessing).",
            IsFateSealed: false,
            DissolvesOnFloor: null,
            Description: "The PK guild. Only the truly outlawed may find its door. Town Guards become permanently hostile.",
            RecruiterName: "PoH's Herald"),
    };

    // Player-founded guild perk presets. Index stored on Player.FoundedGuildPerk.
    public record PlayerGuildPreset(string Name, string Flavor, (StatType Stat, int Value)[] Bonuses);

    public static readonly PlayerGuildPreset[] FoundedPresets =
    {
        new("Warrior Focus",  "+8 Attack",
            new[] { (StatType.Attack, 8) }),
        new("Defender Focus", "+10 Defense, +3 Vitality",
            new[] { (StatType.Defense, 10), (StatType.Vitality, 3) }),
        new("Rogue Focus",    "+5 Dexterity (+crit), +5 Speed",
            new[] { (StatType.Dexterity, 5), (StatType.Speed, 5) }),
        new("Explorer Focus", "+10 Endurance, +5 Agility",
            new[] { (StatType.Endurance, 10), (StatType.Agility, 5) }),
        new("Balanced",       "+3 all attributes",
            new[]
            {
                (StatType.Vitality, 3), (StatType.Strength, 3), (StatType.Endurance, 3),
                (StatType.Dexterity, 3), (StatType.Agility, 3), (StatType.Intelligence, 3),
            }),
    };

    public const int PlayerGuildFoundCost = 5000;
    public const int PlayerGuildNameMaxLen = 20;

    // Apply / reverse a guild's flat passive bonus. Mirrors TitleSystem's
    // base-stat poke pattern so all derived-stat pipelines pick it up.
    public static void ApplyGuildPerk(Player player, GuildDef guild, int sign = +1)
    {
        foreach (var (stat, value) in guild.PerkBonuses)
            PokeStat(player, stat, value * sign);
    }

    public static void ApplyPlayerGuildPerk(Player player, int presetIdx, int sign = +1)
    {
        if (presetIdx < 0 || presetIdx >= FoundedPresets.Length) return;
        foreach (var (stat, value) in FoundedPresets[presetIdx].Bonuses)
            PokeStat(player, stat, value * sign);
    }

    private static void PokeStat(Player player, StatType stat, int v)
    {
        switch (stat)
        {
            case StatType.Attack:       player.BaseAttack       += v; break;
            case StatType.Defense:      player.BaseDefense      += v; break;
            case StatType.Speed:        player.BaseSpeed        += v; break;
            case StatType.SkillDamage:  player.BaseSkillDamage  += v; break;
            case StatType.Strength:     player.Strength         += v; break;
            case StatType.Vitality:     player.Vitality         += v; break;
            case StatType.Endurance:    player.Endurance        += v; break;
            case StatType.Dexterity:    player.Dexterity        += v; break;
            case StatType.Agility:      player.Agility          += v; break;
            case StatType.Intelligence: player.Intelligence     += v; break;
            case StatType.Health:
                player.Vitality += Math.Max(1, v / 10);
                if (v > 0)
                    player.CurrentHealth = Math.Min(player.CurrentHealth + v, player.MaxHealth);
                break;
        }
    }

    // Join a guild. Assumes requirements already checked by the caller.
    // Removes any existing guild first (with penalty), then applies the new
    // perk and seeds rep at +10.
    public static void Join(Player player, Faction newGuild, IGameLog log)
    {
        if (player.ActiveGuildId == newGuild) return;

        // Leave current guild first (with penalty) if any.
        if (player.ActiveGuildId != Faction.None)
            Leave(player, log, silent: false);

        player.ActiveGuildId = newGuild;
        if (newGuild == Faction.PlayerGuild)
        {
            ApplyPlayerGuildPerk(player, player.FoundedGuildPerk, sign: +1);
            string name = string.IsNullOrEmpty(player.FoundedGuildName) ? "your guild" : player.FoundedGuildName!;
            log.LogSystem($"  ** You are now the founder of {name}. **");
            return;
        }

        if (!Guilds.TryGetValue(newGuild, out var def)) return;
        ApplyGuildPerk(player, def, sign: +1);
        StorySystem.AdjustRep(newGuild, 10);
        log.LogSystem($"  ** You are now a member of {def.DisplayName}. **");
        log.Log($"  Perk: {def.PerkFlavor}");
    }

    // Leave the currently-active guild. Applies the -10 rep / -3 karma penalty
    // unless silent=true (used during Moonlit Black Cats force-dissolve, which
    // has its own flavor-specific karma hit).
    public static void Leave(Player player, IGameLog log, bool silent = false)
    {
        var cur = player.ActiveGuildId;
        if (cur == Faction.None) return;

        if (cur == Faction.PlayerGuild)
        {
            ApplyPlayerGuildPerk(player, player.FoundedGuildPerk, sign: -1);
            player.ActiveGuildId = Faction.None;
            if (!silent) log.Log("  You dissolve your founded guild. The banner falls.");
            return;
        }

        if (Guilds.TryGetValue(cur, out var def))
        {
            ApplyGuildPerk(player, def, sign: -1);
            if (!silent)
            {
                StorySystem.AdjustRep(cur, -10);
                KarmaSystem.Adjust(player, KarmaSystem.DeltaLeaveGuild, $"left {def.DisplayName}", log);
                log.LogSystem($"  You leave {def.DisplayName}. (-10 rep, -3 karma)");
            }
        }
        player.ActiveGuildId = Faction.None;
    }

    // Requirement check for join. Returns (ok, reason) so the UI can surface
    // the specific gate the player is failing.
    public static (bool Ok, string Reason) CanJoin(Player player, GuildDef def)
    {
        if (player.Level < def.MinLevel)
            return (false, $"Requires Level {def.MinLevel} (you are Lv.{player.Level}).");
        if (player.Karma < def.MinKarma)
            return (false, $"Requires karma >= {def.MinKarma} (you have {player.Karma}).");
        if (player.Karma > def.MaxKarma)
            return (false, $"Requires karma <= {def.MaxKarma} (you have {player.Karma}).");
        return (true, "Requirements met.");
    }

    // ── Moonlit Black Cats force-dissolve (F27 entry) ───────────────────
    // Called from TurnManager.AscendFloor after CurrentFloor advances.
    // Only fires once — removes the guild, pays out the Sachi-flavored line,
    // -5 karma, and a thematic "Survivor" title unlock hint.
    public static void CheckBlackCatsFate(Player player, int newFloor, IGameLog log)
    {
        if (newFloor != 27) return;
        if (player.ActiveGuildId != Faction.MoonlitBlackCats) return;

        log.LogSystem("══════════════════════════════════════");
        log.LogSystem("  The Moonlit Black Cats are no more.");
        log.Log("  The chest was a mimic. The trap took them all. Only you walk out.");
        log.LogSystem("══════════════════════════════════════");

        // Silent leave (skip generic -10 rep / -3 karma), then apply the
        // canon -5 karma and drop the active guild.
        Leave(player, log, silent: true);
        KarmaSystem.Adjust(player, KarmaSystem.DeltaBlackCatsFall, "Moonlit Black Cats fell on F27", log);

        // Survivor title — matches TitleSystem.CheckFloor50Survivor but fires
        // early on this specific dissolution path.
        TitleSystem.TryUnlock(player, "title_survivor");
    }

    // Convenience accessor for the active guild's display name (for HUD / stats).
    public static string ActiveGuildDisplayName(Player player)
    {
        if (player.ActiveGuildId == Faction.None) return "(none)";
        if (player.ActiveGuildId == Faction.PlayerGuild)
            return string.IsNullOrEmpty(player.FoundedGuildName) ? "Founded Guild" : player.FoundedGuildName!;
        return Guilds.TryGetValue(player.ActiveGuildId, out var def) ? def.DisplayName : "(unknown)";
    }

    // Active guild perk flavor — for StatsDialog display.
    public static string ActiveGuildPerkFlavor(Player player)
    {
        if (player.ActiveGuildId == Faction.None) return "";
        if (player.ActiveGuildId == Faction.PlayerGuild)
        {
            if (player.FoundedGuildPerk < 0 || player.FoundedGuildPerk >= FoundedPresets.Length)
                return "";
            return FoundedPresets[player.FoundedGuildPerk].Flavor;
        }
        return Guilds.TryGetValue(player.ActiveGuildId, out var def) ? def.PerkFlavor : "";
    }

    // Combat hook: Fuurinkazan grants +10 Attack when the player has a Katana
    // equipped. TurnManager.Combat calls this to fold the bonus into the
    // damage calc. Returns 0 for non-Katana or non-Fuurinkazan players.
    public static int KatanaAttackBonus(Player player, string? weaponType)
    {
        if (player.ActiveGuildId != Faction.Fuurinkazan) return 0;
        return weaponType == "Katana" ? 10 : 0;
    }

    // Combat hook: Legend Braves grants +15 Attack vs Laughing Coffin mobs.
    public static int LegendBravesVsLcBonus(Player player, string mobName)
    {
        if (player.ActiveGuildId != Faction.LegendBraves) return 0;
        if (string.IsNullOrEmpty(mobName)) return 0;
        if (mobName.Contains("Laughing Coffin") || mobName.Contains("PKer")) return 15;
        return 0;
    }

    // Recruitment NPC → Faction dispatch table. Centralized so the
    // TurnManager.Movement.HandleOccupantInteraction dispatcher is terse.
    public static readonly Dictionary<string, Faction> RecruiterToGuild = new()
    {
        ["Kibaou"]           = Faction.AincradLiberationForce,
        ["Keita"]            = Faction.MoonlitBlackCats,
        ["Klein's Second"]   = Faction.Fuurinkazan,
        ["Schmitt"]          = Faction.LegendBraves,
        ["Lind"]             = Faction.DivineDragonAlliance,
        ["Godfree"]          = Faction.KnightsOfBlood,
        ["Siune"]            = Faction.SleepingKnights,
        ["PoH's Herald"]     = Faction.LaughingCoffin,
    };

    // Signature (post-join) quests — one per guild. Kill-count quests tied to
    // thematic mob tags or floor ranges. Rewards keyed to guild rank.
    public record SignatureQuest(
        string QuestId, string Title, string Description, int KillCount,
        string? MobNameFragment, string? LootTag, int MinFloor, int MaxFloor,
        int RewardCol, int RewardXp, string? WeaponType = null);

    public static readonly Dictionary<Faction, SignatureQuest> SignatureQuests = new()
    {
        [Faction.KnightsOfBlood] = new(
            "guild_sig_kob_defend_frontline", "Defend the Frontline",
            "Slay 15 monsters on Floor 55 or above while bearing the Blood Oath.",
            15, null, null, 55, 100, 1200, 800),

        [Faction.AincradLiberationForce] = new(
            "guild_sig_alf_raid_frontlines", "Raid the Frontlines",
            "Slay 20 beast-tag creatures on Floors 1-10.",
            20, null, "beast", 1, 10, 500, 300),

        [Faction.DivineDragonAlliance] = new(
            "guild_sig_dda_drake_hunt", "Drake Hunt",
            "Slay 10 dragon-tag creatures on Floors 30-50.",
            10, null, "dragon", 30, 50, 900, 550),

        [Faction.Fuurinkazan] = new(
            "guild_sig_fuurin_blades", "Blades of Friendship",
            "Slay 20 humanoid-tag mobs while wielding a Katana.",
            20, null, "humanoid", 1, 100, 700, 500,
            WeaponType: "Katana"),

        [Faction.LegendBraves] = new(
            "guild_sig_braves_hunt_coffin", "Hunt the Coffin",
            "Slay 10 Laughing Coffin PKers anywhere on Aincrad.",
            10, "Laughing Coffin", null, 1, 100, 1000, 700),

        [Faction.SleepingKnights] = new(
            "guild_sig_sleep_moons_rest", "The Moon's Rest",
            "Slay 15 undead-tag enemies on Floors 50-100.",
            15, null, "undead", 50, 100, 1500, 800),

        [Faction.LaughingCoffin] = new(
            "guild_sig_lc_crimson_letter", "Crimson Letter",
            "Silence 5 Town Guards — the authorities won't miss them. (Available only at karma ≤-50; Town Guards spawn at F1 TOB.)",
            5, "Town Guard", null, 1, 100, 1500, 500),

        [Faction.MoonlitBlackCats] = new(
            "guild_sig_cats_one_more_floor", "One More Floor",
            "Reach Floor 25 as a Moonlit Black Cat. (You will be there soon.)",
            1, "__floor25__", null, 25, 100, 400, 300),
    };
}
