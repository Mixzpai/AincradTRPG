using SAOTRPG.Entities;
using Terminal.Gui;

namespace SAOTRPG.Map;

// Named field bosses that spawn in the wilderness of specific floors.
// Distinct from floor bosses (labyrinth room) — these are roaming elites with
// canon drops (Crystallite Ingot, Mammoth Tusk, Divine Stone of Returning Soul).
// Stats are softer than labyrinth bosses: 2.0-3.0x mob HP, 1.3-1.6x mob ATK.
public static class FieldBossFactory
{
    private record FieldBossEntry(
        string Id,
        string Name,
        string Title,
        int Floor,
        float HpScale,
        float AtkScale,
        char Symbol,
        Color BodyColor,
        string GuaranteedDropId,
        string EncounterFlavor,
        bool IsSeasonal = false,
        string? SeasonalEventId = null
    );

    private static readonly FieldBossEntry[] Roster =
    {
        new("bullbous_bow_f2",      "Bullbous Bow",      "Armored Terror of the Plains",  2,  2.0f, 1.3f, 'O', Color.Red,           "bullbous_horn",
            "A massive armored bull paws the earth, steam rising from its nostrils."),
        new("forest_king_stag_f22", "Forest King Stag",  "Crowned Beast of Wandering",    22, 2.3f, 1.4f, 'K', Color.BrightYellow,  "kingly_antler",
            "Antlers like a crown crest the ridge. A king of this forest is near."),
        new("magnatherium_f35",     "Magnatherium",      "Mammoth of the Memory Hill",    35, 2.8f, 1.4f, 'M', Color.White,         "mammoth_tusk",
            "The ground shakes with heavy footfalls. Something enormous hunts the hill."),
        new("ogre_lord_f40",        "Ogre Lord",         "Chieftain of the Broken Valley", 40, 2.6f, 1.5f, 'O', Color.BrightMagenta, "ogres_cleaver",
            "A guttural roar rolls across the valley. The ogre lord claims this floor."),
        new("frost_dragon_f48",     "Frost Dragon",      "Wyrm of the Crystallite Cave",  48, 3.0f, 1.6f, 'D', Color.BrightCyan,    "crystallite_ingot",
            "A freezing wind howls from the ice cavern. A white wyrm watches the valley."),
        new("nicholas_f49",         "Nicholas the Renegade", "Fallen Saint of the Winter Fir", 49, 2.5f, 1.5f, 'N', Color.BrightRed, "divine_stone_of_returning_soul",
            "Jingling bells echo through the dead pines. A hulking red-coated giant waits beneath the fir tree.",
            IsSeasonal: true, SeasonalEventId: "christmas"),
    };

    // Returns all field bosses that should spawn on this floor right now.
    // Skips seasonal bosses when their event isn't active, and skips bosses
    // already defeated in this run.
    public static IEnumerable<FieldBoss> GetActiveForFloor(int floor, HashSet<string> defeatedIds,
        Systems.SeasonalEvent activeEvent)
    {
        foreach (var entry in Roster)
        {
            if (entry.Floor != floor) continue;
            if (defeatedIds.Contains(entry.Id)) continue;
            if (entry.IsSeasonal && !SeasonalMatches(entry.SeasonalEventId, activeEvent)) continue;
            yield return Build(entry, floor);
        }
    }

    private static bool SeasonalMatches(string? eventId, Systems.SeasonalEvent active)
        => eventId switch
        {
            "christmas"  => active == Systems.SeasonalEvent.Christmas,
            "halloween"  => active == Systems.SeasonalEvent.Halloween,
            "valentine"  => active == Systems.SeasonalEvent.Valentine,
            "new_year"   => active == Systems.SeasonalEvent.NewYear,
            "summer"     => active == Systems.SeasonalEvent.Summer,
            _            => false,
        };

    private static FieldBoss Build(FieldBossEntry e, int floor)
    {
        // Scale off the floor mob's baseline rather than the labyrinth boss
        // curve: softer than a floor boss, tougher than a normal mob.
        int level = 8 + floor * 2;
        int mobHp = 15 + floor * 10;
        int mobAtk = 3 + floor * 2;
        int hp  = (int)(mobHp * e.HpScale * 6);  // field-boss HP pool
        int atk = (int)(mobAtk * e.AtkScale * 2);
        int def = 4 + floor;

        var boss = new FieldBoss
        {
            Id = 8000 + floor,
            Name = e.Name,
            Level = level,
            BaseAttack = atk,
            BaseDefense = def,
            BaseSpeed = 4 + floor / 2,
            BaseCriticalRate = 5 + floor / 8,
            BaseCriticalHitDamage = 15 + floor / 4,
            Vitality = 4 + floor,
            Strength = 3 + floor,
            Endurance = 3 + floor / 2,
            Dexterity = 2 + floor / 2,
            Agility = 2 + floor / 2,
            Intelligence = 2,
            MaxHealth = hp,
            CurrentHealth = hp,
            ExperienceYield = 400 + floor * 100,
            ColYield = 2000 + floor * 250,
            FieldBossId = e.Id,
            GuaranteedDropId = e.GuaranteedDropId,
            EncounterFlavor = e.EncounterFlavor,
            IsSeasonal = e.IsSeasonal,
            SeasonalEventId = e.SeasonalEventId,
        };
        boss.SetBossTitle(e.Title);
        boss.SetSymbol(e.Symbol);
        boss.SetColor(e.BodyColor);
        boss.Abilities = new[]
        {
            new BossAbility { Name = "Signature Strike", Type = BossAbilityType.HeavyStrike,
                MinPhase = 2, Cooldown = 5, DamageMultiplier = 1.6 },
        };
        return boss;
    }
}
