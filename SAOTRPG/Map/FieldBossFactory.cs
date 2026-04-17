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

        // ── Divine Object couriers ────────────────────────────────────
        // Canon-themed field bosses that guarantee-drop Divine Objects.
        // Stronger HP/ATK scaling than normal field bosses — they're meant
        // to be memorable encounters that the player prepares for.

        // F40 — Deusolbert Synthesis Seven's Divine Object (Conflagrant Flame Bow).
        new("phoenix_of_smolder_peak_f40", "Phoenix of the Smolder Peak", "Guardian of the Flame Bow",
            40, 3.5f, 1.6f, 'P', Color.BrightRed, "conflagrant_flame_bow",
            "A hollow-eyed phoenix perches on the smolder cliff. Its gaze is older than fire itself."),

        // F85 — Sheyta Synthesis Twelve's Divine Object (Black Lily Sword).
        new("silent_edge_f85",       "The Silent Edge",   "Wielder of the Severing Blade", 85, 4.0f, 1.7f, 'S', Color.BrightMagenta, "black_lily_sword",
            "A silent figure stands in the gloom, one hand resting on a blade that seems to drink the light."),

        // F95 — Bercouli Synthesis One's Divine Object (Time Piercing Sword).
        new("warden_of_stopped_hours_f95", "Warden of Stopped Hours", "Sentinel of the Broken Clock",
            95, 4.5f, 1.8f, 'W', Color.BrightCyan, "time_piercing_sword",
            "A warden in ancient armor stands beneath a clock that has stopped. Time feels heavier here."),

        // ── Hollow Fragment canon NM/HNM field bosses ──────────────────
        // Canon-accurate placements for HF endgame Legendary weapons.
        // NM = Named Monster (free spawn), HNM = Hollow Named Monster
        // (promoted to field boss tier for "worldboss" feel).

        // F77 NM Goblin Leader drops Mace of Asclepius (HF canon).
        new("goblin_leader_f77",    "Goblin Leader",    "Warchief of the Kobold Host", 77, 3.0f, 1.5f, 'G', Color.Green,        "mace_of_asclepius",
            "A hulking kobold in iron plate looms over its horde. Its mace drips with old blood."),

        // F83 NM Arboreal Fear drops Demonspear: Gae Bolg (HF canon).
        new("arboreal_fear_f83",    "Arboreal Fear",    "Horror of the Hanging Grove",  83, 3.5f, 1.6f, 'T', Color.BrightMagenta,"demonspear_gae_bolg",
            "The forest watches with too many eyes. A twisted dryad steps from the bark, spear in hand."),

        // F85 HNM Abased Beast (promoted) drops Godblade: Dragonslayer (HF canon).
        new("abased_beast_f85",     "Abased Beast",     "Fallen Wyrm of the Crimson Cliff", 85, 4.0f, 1.7f, 'B', Color.BrightRed,"godblade_dragonslayer",
            "A draconic beast drags its broken wings across the stone. Still, it hungers."),

        // F87 NM Night Stalker drops Saintblade: Durandal (HF canon).
        new("night_stalker_f87",    "Night Stalker",    "Predator of the Moonless Halls", 87, 4.0f, 1.7f, 'N', Color.DarkGray,   "saintblade_durandal",
            "A silent predator stalks the corridor. Light dims where it passes."),

        // F94 HNM Ark Knight (promoted) drops Ragnarok's Bane: Headsman (HF canon).
        new("ark_knight_f94",       "Ark Knight",       "Executioner of the Final Ark",    94, 4.8f, 1.8f, 'K', Color.White,      "ragnaroks_bane_headsman",
            "A headsman in ceremonial armor stands ready. His axe has seen a thousand endings."),

        // F95 NM Gaia Breaker drops Stigmablade: Arondight (HF canon).
        new("gaia_breaker_f95",     "Gaia Breaker",     "Titan of the Cracked Earth",      95, 4.5f, 1.8f, 'G', Color.Yellow,     "stigmablade_arondight",
            "The ground shudders. A stone colossus rises, its fists the size of your torso."),

        // F96 HNM Eternal Dragon (promoted) drops Demonblade: Gram (HF canon).
        new("eternal_dragon_f96",   "Eternal Dragon",   "The Wyrm That Refuses to Die",    96, 5.0f, 1.9f, 'D', Color.BrightMagenta,"demonblade_gram",
            "A scarred wyrm coiled on ancient bones. Gram was forged for this."),

        // F98 NM Blaze Armor (Hollow Area relocated to F98) drops Yato: Masamune (HF canon).
        new("blaze_armor_f98",      "Blaze Armor",      "Living Armor of the Hollow Forge",98, 5.2f, 2.0f, 'A', Color.BrightRed,  "yato_masamune",
            "Empty armor glowing with inner flame. The soul inside remembers every sword it has wielded."),

        // ── Hollow Fragment Implement System field bosses (5) ─────────
        // Canon HF F80-F99 "Implement" slots that drop via roaming HNM bosses.

        // F80 — drops Arcaneblade: Soul Binder.
        new("soul_binder_f80",      "Soul Binder",      "Wraith of the Gathered Hymns",    80, 3.8f, 1.65f, 'S', Color.BrightMagenta,"sci_arcaneblade_soul_binder",
            "A figure of layered voices stands in the corridor, scimitar humming with bound soul-tones."),

        // F83 — drops Fellblade: Ruinous Doom (replaces Arboreal Fear slot? No — both F83 possible; arboreal already occupies. Moved to F84 slot for clarity).
        // Actually F83 is Arboreal Fear for Gae Bolg. Fellblade Ruinous Doom is Legendary-scale — place on F83 alongside.
        // But placement table says field boss F83 for Fellblade. Add a second F83 boss.
        new("ruinous_herald_f83",   "Ruinous Herald",   "Doom-Prophet of the Falling Tower",83, 3.9f, 1.7f, 'R', Color.BrightRed,   "sci_fellblade_ruinous_doom",
            "A scimitar-wielder cloaked in unmaking. Every step erases the one before."),

        // F86 — drops Fellaxe: Demon's Scythe.
        new("fellaxe_revenant_f86", "Fellaxe Revenant", "Demon-Shade of the Crimson Reap",  86, 4.0f, 1.72f, 'F', Color.BrightRed,  "axe_fellaxe_demons_scythe",
            "A headsman-shade hauls a scythe-bladed axe. The air around it carries the scent of old harvest."),

        // F93 — drops Glimmerblade: Banishing Ray.
        new("banishing_ray_f93",    "Banishing Ray",    "Sentinel of the White Horizon",   93, 4.3f, 1.75f, 'B', Color.BrightYellow,"rap_glimmerblade_banishing_ray",
            "A rapier-duellist in searing white mail. Its thrust seems to erase what it pierces."),

        // ── Integral Factor series field bosses (IF canon) ─────────────
        // Each boss guarantees one signature series weapon. The matching
        // series shield drops as a secondary via LootGenerator.FieldBossSecondaryDrops.
        // The remaining series weapons appear in the floor-banded Epic loot
        // pool (LootGenerator.FloorBandedRegisteredLoot).

        // F14 — Integral Series (first IF endgame tier). Canon F14 shield
        // is Fermat; canon F14 weapons include Arc Angel, Radgrid, Gusion,
        // After Glow.
        new("starlight_sentinel_f14", "Starlight Sentinel", "Guardian of the Integral Dawn", 14, 3.0f, 1.5f, 'S', Color.BrightYellow, "bow_integral_arc_angel",
            "A luminous figure paces the ridge, a bow of starlight drawn across its back."),

        // F25 — Nox Series (Underground Labyrinth B5F origin, IF canon).
        new("labyrinth_warden_f25",  "Labyrinth Warden",  "Keeper of the Underground Vault",  25, 3.2f, 1.55f, 'L', Color.BrightMagenta, "ohs_nox_radgrid",
            "A hooded warden strides out of the dark, a heavy blade resting across both shoulders."),

        // F61 — Rosso Series. Name borrows from Rosso Forneus weapon.
        new("crimson_forneus_f61",   "Crimson Forneus",   "Demon of the Scarlet Depths",      61, 4.0f, 1.7f, 'F', Color.BrightRed, "ohs_rosso_forneus",
            "The air reddens. A demon-knight in crimson plate levels a heavy sword and advances."),

        // F87 — Yasha Series (moved from F85 due to Silent Edge + Abased
        // Beast collision per scout §9).
        new("yasha_night_demon_f87", "Yasha the Night Demon", "Demon-Warrior of the Moonless Path", 87, 4.2f, 1.75f, 'Y', Color.BrightMagenta, "ohs_yasha_astaroth",
            "A horned silhouette waits at the corridor's end. Its blade drinks the moonlight."),

        // F90+ — Gaou Series. Japanese ox-king demon theme.
        new("gaou_ox_king_f90",      "Gaou the Ox-King",  "Demon-King of the Horned Vanguard", 90, 4.5f, 1.85f, 'G', Color.BrightYellow, "ohs_gaou_reginleifr",
            "A towering ox-headed king stamps the stone. The ground splits beneath its hooves."),
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
