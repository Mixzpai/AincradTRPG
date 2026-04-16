using Terminal.Gui;

namespace SAOTRPG.Systems.Story;

// Ten authored milestone events for the SAO 100-floor arc. Each is a pure
// data entry: trigger + fire predicate + cutscene builder. StorySystem
// owns firing, persistence, and replay-shortcutting.
public static class StoryEvents
{
    // Color palette for speakers.
    private static readonly Color Kirito     = Color.BrightCyan;
    private static readonly Color Asuna      = Color.White;
    private static readonly Color Kayaba     = Color.BrightRed;
    private static readonly Color Heathcliff = Color.BrightRed;
    private static readonly Color ArgoC      = Color.BrightYellow;
    private static readonly Color KleinC     = Color.BrightMagenta;
    private static readonly Color AgilC      = Color.BrightGreen;
    private static readonly Color KizmelC    = Color.Cyan;
    private static readonly Color SachiC     = Color.BrightCyan;
    private static readonly Color KibaouC    = Color.Yellow;
    private static readonly Color Narration  = Color.Gray;
    private static readonly Color WorldEvent = Color.BrightYellow;

    public static void RegisterAll(List<NarrativeEvent> events)
    {
        events.Add(E01_KayabaReveal());
        events.Add(E02_Beater());
        events.Add(E03_Kizmel());
        events.Add(E04_Midpoint());
        events.Add(E05_Sachi());
        events.Add(E06_Kibaou());
        events.Add(E07_BlackSwordsman());
        events.Add(E08_GleamEyes());
        events.Add(E09_Heathcliff());
        events.Add(E10_RubyPalace());
    }

    // F1 prologue: Kayaba's announcement (unskippable).
    private static NarrativeEvent E01_KayabaReveal() => new(
        Id: "e01_kayaba_reveal",
        Trigger: StoryTrigger.GameStart,
        CanFire: _ => true,
        Build: _ => new CutsceneScript(
            EventId: "e01_kayaba_reveal",
            Title: "November 6, 2022",
            Unskippable: true,
            Beats: new[]
            {
                new CutsceneBeat(null, null, WorldEvent, Letterbox: true,
                    Text: "The sky of the Town of Beginnings ignites crimson. Ten thousand players freeze as a hooded giant descends."),
                new CutsceneBeat("Akihiko Kayaba", "kayaba", Kayaba,
                    "I am the sole person who can control this world. I have disabled the log-out button. This was not a defect."),
                new CutsceneBeat("Akihiko Kayaba", "kayaba", Kayaba,
                    "If your HP reaches zero in-game, your NerveGear will destroy your brain. To be freed, you must clear all one hundred floors of Aincrad."),
                new CutsceneBeat(null, null, Narration, Letterbox: true,
                    Text: "Mirrors materialize. You look down. The face staring back is your own — your real one. The cage has closed."),
            }
        )
    );

    // F1 boss aftermath: the Beater moment.
    private static NarrativeEvent E02_Beater() => new(
        Id: "e02_beater",
        Trigger: StoryTrigger.BossDefeat,
        CanFire: ctx => ctx.Floor == 1,
        Build: _ => new CutsceneScript(
            EventId: "e02_beater",
            Title: "Illfang Has Fallen",
            Beats: new[]
            {
                new CutsceneBeat(null, null, Narration,
                    "Diavel's body dissolves into blue shards. Twenty raiders stand silent over the loot drop. A voice cuts through the hush."),
                new CutsceneBeat("Kibaou", "kibaou", KibaouC,
                    "That strategy guide was wrong about the boss's weapon. Only a beta-tester would have known. One of you hid it. One of you killed him!"),
                new CutsceneBeat(null, null, Narration,
                    "Every eye turns to you. You were a beta-tester. You could have warned them. You chose not to.",
                    Choices: new[]
                    {
                        new CutsceneChoice(
                            "\"A beta-tester? Don't insult me. I'm a BEATER.\"",
                            "The word lands like a slap. You pull up your cloak and walk past them alone. Let them hate a scapegoat — the solo clearers they would have hunted are safer now.",
                            () => { StorySystem.SetFlag(StoryFlag.OwnedBeaterLabel); StorySystem.AdjustRep(Faction.AincradLiberationSquad, -10); }),
                        new CutsceneChoice(
                            "\"I would have warned anyone who asked me.\"",
                            "The raiders mutter but don't disperse. You earn a reputation as honest — and a dozen new acquaintances who will ask you for everything next time.",
                            () => { StorySystem.SetFlag(StoryFlag.DeniedBeaterLabel); StorySystem.AdjustRep(Faction.AincradLiberationSquad, 5); }),
                    }),
            }
        )
    );

    // F3: Kizmel the Dark Elf.
    private static NarrativeEvent E03_Kizmel() => new(
        Id: "e03_kizmel",
        Trigger: StoryTrigger.FloorEntry,
        CanFire: ctx => ctx.Floor == 3,
        Build: _ => new CutsceneScript(
            EventId: "e03_kizmel",
            Title: "The Grove of the Sister",
            Beats: new[]
            {
                new CutsceneBeat(null, null, Narration,
                    "Floor 3 is a green cathedral. Between the black oaks, a wounded knight in dark-blue plate presses a hand to her side. She does not flee."),
                new CutsceneBeat("Kizmel", "kizmel", KizmelC,
                    "A human swordsman… you would help me? My sister Tilnel fell three days ago on this same path. The Forest Elves murdered her for the Jade Key."),
                new CutsceneBeat("Kizmel", "kizmel", KizmelC,
                    "My people remember. So do I. If you will walk with me a while, I can teach you things no NPC ever taught a Player."),
                new CutsceneBeat(null, null, Narration,
                    "She is not supposed to remember. She is not supposed to grieve. The question begins here and does not close for six floors.",
                    Choices: new[]
                    {
                        new CutsceneChoice(
                            "Offer your hand.",
                            "Her fingers close around yours. Her grip is warm, and heavier than any scripted line should feel.",
                            () => StorySystem.SetFlag(StoryFlag.MetKizmel)),
                    }),
            }
        )
    );

    // F25: Midpoint Memorial.
    private static NarrativeEvent E04_Midpoint() => new(
        Id: "e04_midpoint",
        Trigger: StoryTrigger.FloorEntry,
        CanFire: ctx => ctx.Floor == 25,
        Build: _ => new CutsceneScript(
            EventId: "e04_midpoint",
            Title: "The Quarter-Way Memorial",
            Beats: new[]
            {
                new CutsceneBeat(null, null, Narration, Letterbox: true,
                    Text: "A stone obelisk stands in the plaza of the 25th floor. Two thousand and forty-seven names are carved into it. The list is still growing."),
                new CutsceneBeat("Agil", "agil", AgilC,
                    "We hit the quarter mark today. Twenty-five down, seventy-five to go. The numbers don't get better from here."),
                new CutsceneBeat(null, null, Narration,
                    "A small crowd gathers at the base of the memorial. Some are crying. Some are sharpening blades. You have to choose what this floor means to you.",
                    Choices: new[]
                    {
                        new CutsceneChoice(
                            "Kneel and read every name.",
                            "Hours later you rise with a heavier heart and a steadier hand. The dead deserve witnesses. You will be one.",
                            () => StorySystem.SetFlag(StoryFlag.MournedFallen)),
                        new CutsceneChoice(
                            "Walk past. The living need the next floor cleared.",
                            "You feel the memorial's shadow recede behind you. Grief is a resource; you have decided how to spend yours.",
                            () => StorySystem.SetFlag(StoryFlag.SteeledResolve)),
                    }),
            }
        )
    );

    // F27: Sachi's Fall.
    private static NarrativeEvent E05_Sachi() => new(
        Id: "e05_sachi",
        Trigger: StoryTrigger.FloorEntry,
        CanFire: ctx => ctx.Floor == 27,
        Build: _ => new CutsceneScript(
            EventId: "e05_sachi",
            Title: "A Message Across Christmas",
            Beats: new[]
            {
                new CutsceneBeat(null, null, Narration,
                    "A small guild passes you at the floor gate — four laughing boys and one quiet girl with a dented iron shield."),
                new CutsceneBeat("Sachi", "sachi", SachiC,
                    "Oh! You're the Black Swordsman, right? Keita said you saved us from that mob ambush. I just wanted to say… thank you. For letting us keep feeling safe."),
                new CutsceneBeat(null, null, Narration, Letterbox: true,
                    Text: "Seven days later the Moonlit Black Cats will trigger a teleport trap in a level-50 hidden dungeon. Only one of them will walk out."),
                new CutsceneBeat("Sachi", "sachi", SachiC,
                    "If I die… please. Don't forget me. Look for me even if I'm not here anymore."),
            }
        )
    );

    // F50: Kibaou's ALS rises.
    private static NarrativeEvent E06_Kibaou() => new(
        Id: "e06_kibaou",
        Trigger: StoryTrigger.FloorEntry,
        CanFire: ctx => ctx.Floor == 50,
        Build: _ => new CutsceneScript(
            EventId: "e06_kibaou",
            Title: "Two Banners Over Algade",
            Beats: new[]
            {
                new CutsceneBeat(null, null, Narration,
                    "The Algade guild plaza is split in two. Red-and-white Knights of the Blood banners face the green Aincrad Liberation Squad flags across a drawn line."),
                new CutsceneBeat("Heathcliff", "heathcliff", Heathcliff,
                    "The clearing must be disciplined. Organized. Sacrifice where sacrifice is necessary, and no player left behind where it is not. Stand with the Knights."),
                new CutsceneBeat("Kibaou", "kibaou", KibaouC,
                    "Discipline? That's a word the beta-testers use while they hoard the easy kills. The ALS fights for the REAL players. The ones who bleed together."),
                new CutsceneBeat(null, null, Narration,
                    "Rumor has it a third faction has begun to move in the shadows — a red-cloaked guild that kills Players the way Players kill mobs. They call themselves the Laughing Coffin.",
                    Choices: new[]
                    {
                        new CutsceneChoice(
                            "Shake Heathcliff's gauntleted hand.",
                            "The KoB commander nods once. You are not a Knight, but you are welcome at their table. Something about his grip feels rehearsed.",
                            () => { StorySystem.AdjustRep(Faction.KnightsOfBlood, 15); StorySystem.SetFlag(StoryFlag.KnowsLaughingCoffin); }),
                        new CutsceneChoice(
                            "Clasp Kibaou's forearm.",
                            "Kibaou grins. \"Good. The Liberation remembers its friends.\" Later you will wonder if it remembers its enemies better.",
                            () => { StorySystem.AdjustRep(Faction.AincradLiberationSquad, 15); StorySystem.SetFlag(StoryFlag.KnowsLaughingCoffin); }),
                        new CutsceneChoice(
                            "Walk between them without speaking.",
                            "Both leaders watch you pass. You can feel Heathcliff's calculation and Kibaou's contempt in equal measure. Solo suits you.",
                            () => StorySystem.SetFlag(StoryFlag.KnowsLaughingCoffin)),
                    }),
            }
        )
    );

    // F67: The Black Swordsman title at 100 kills.
    private static NarrativeEvent E07_BlackSwordsman() => new(
        Id: "e07_black_swordsman",
        Trigger: StoryTrigger.KillCount,
        CanFire: ctx => ctx.KillCount >= 100 && ctx.Floor >= 50,
        Build: _ => new CutsceneScript(
            EventId: "e07_black_swordsman",
            Title: "A Title Spreads",
            Beats: new[]
            {
                new CutsceneBeat("Argo", "argo", ArgoC,
                    "Hey, hey. The whisper network's got a new name for ya. Black cloak, black blade, black reputation. Costs you nothing — I'm giving this one away."),
                new CutsceneBeat("Argo", "argo", ArgoC,
                    "'The Black Swordsman.' Sounds scary. Good. Scary solo-players live longer. Wear it, kid."),
                new CutsceneBeat(null, null, Narration,
                    "The title is yours now. Monsters will aggro from further away. Vendors will charge a little more. Solo players will step aside to let you pass.",
                    Choices: new[]
                    {
                        new CutsceneChoice(
                            "Accept the title.",
                            "It suits you more than you'd like to admit.",
                            () => StorySystem.SetFlag(StoryFlag.BlackSwordsman)),
                    }),
            }
        )
    );

    // F74: Gleam Eyes and Dual Blades.
    private static NarrativeEvent E08_GleamEyes() => new(
        Id: "e08_gleam_eyes",
        Trigger: StoryTrigger.BossDefeat,
        CanFire: ctx => ctx.Floor == 74,
        Build: _ => { Skills.UniqueSkillSystem.TryUnlock(Skills.UniqueSkill.DualBlades); return BuildGleamEyesScript(); }
    );

    private static CutsceneScript BuildGleamEyesScript() => new CutsceneScript(
            EventId: "e08_gleam_eyes",
            Title: "A Unique Skill Awakens",
            Beats: new[]
            {
                new CutsceneBeat(null, null, Narration, Letterbox: true,
                    Text: "The Gleam Eyes stood eight meters tall. It had killed four of Klein's Fuurinkazan in the first thirty seconds."),
                new CutsceneBeat("Klein", "klein", KleinC,
                    "You — how the HELL — you've got TWO swords. That's not a Skill anyone has. There's only one Dual Blades user in the game…"),
                new CutsceneBeat(null, null, Narration,
                    "The raid party stares. Heathcliff stares longest. You have been seen in a way you did not plan to be seen."),
                new CutsceneBeat("Heathcliff", "heathcliff", Heathcliff,
                    "The system has chosen you. The Knights of the Blood have need of such a chosen one. We will speak again on floor seventy-five."),
            }
        );

    // F75: Heathcliff is Kayaba.
    private static NarrativeEvent E09_Heathcliff() => new(
        Id: "e09_heathcliff",
        Trigger: StoryTrigger.BossDefeat,
        CanFire: ctx => ctx.Floor == 75,
        Build: _ => { Skills.UniqueSkillSystem.TryUnlock(Skills.UniqueSkill.HolySword); return BuildHeathcliffScript(); }
    );

    private static CutsceneScript BuildHeathcliffScript() => new CutsceneScript(
            EventId: "e09_heathcliff",
            Title: "The Man Behind the Shield",
            Beats: new[]
            {
                new CutsceneBeat(null, null, Narration,
                    "Fourteen raiders died on the Skull Reaper. The survivors are sitting among their friends' vanishing motes when you turn slowly to face Heathcliff."),
                new CutsceneBeat("Kirito", "kirito", Kirito,
                    "On floor seventy-four, when the Gleam Eyes hit you, your HP meter flickered into the immortal band for a tenth of a second. I saw it. No Player character has that flag."),
                new CutsceneBeat("Heathcliff", "heathcliff", Heathcliff,
                    "You surprise me, Kirito-kun. You surprise me for the first time in twenty-one months."),
                new CutsceneBeat(null, null, Narration,
                    "The red-and-white armor dissolves. The grey robes return. Akihiko Kayaba stands where the Paladin stood."),
                new CutsceneBeat("Kayaba", "kayaba", Kayaba,
                    "A duel. Solo combat, no items, no party. If you win — every player logs out tonight. If you lose — you fall with the rest on one hundred.",
                    Choices: new[]
                    {
                        new CutsceneChoice(
                            "Draw both swords. \"I accept.\"",
                            "His eyes narrow in something like approval. \"Then it ends here. Or it doesn't.\"",
                            () => { StorySystem.SetFlag(StoryFlag.KnowsHeathcliff); StorySystem.SetFlag(StoryFlag.AcceptedDuel); }),
                        new CutsceneChoice(
                            "Refuse. \"Not like this. I'll climb to you.\"",
                            "He bows shallowly. \"Then I will wait at the Ruby Palace. Do not keep me too long — six thousand souls keep counting the days.\"",
                            () => { StorySystem.SetFlag(StoryFlag.KnowsHeathcliff); StorySystem.SetFlag(StoryFlag.RefusedDuel); }),
                    }),
            }
        );

    // F100: Ruby Palace finale (branched by prior flags).
    private static NarrativeEvent E10_RubyPalace() => new(
        Id: "e10_ruby_palace",
        Trigger: StoryTrigger.FloorEntry,
        CanFire: ctx => ctx.Floor == 100,
        Build: _ =>
        {
            bool accepted = StorySystem.HasFlag(StoryFlag.AcceptedDuel);
            bool refused  = StorySystem.HasFlag(StoryFlag.RefusedDuel);
            bool mourned  = StorySystem.HasFlag(StoryFlag.MournedFallen);

            var beats = new List<CutsceneBeat>
            {
                new(null, null, Narration, Letterbox: true,
                    Text: "Floor one hundred. The Ruby Palace. The air has the weight of a held breath. Six thousand one hundred and forty-seven names flicker faintly on every crystal wall."),
            };

            if (accepted)
            {
                beats.Add(new("Kayaba", "kayaba", Kayaba,
                    "You came as promised, Kirito-kun. The duel ended on seventy-five. This is only the epilogue we owe each other."));
            }
            else if (refused)
            {
                beats.Add(new("Kayaba", "kayaba", Kayaba,
                    "You climbed all one hundred. I admit: I did not weight the probabilities for this branch. Extraordinary."));
            }
            else
            {
                beats.Add(new("Kayaba", "kayaba", Kayaba,
                    "You never suspected me. You never had to. The Cardinal System selected you all the same."));
            }

            if (mourned)
            {
                beats.Add(new(null, null, Narration,
                    "You remember the Quarter-Way Memorial. Two thousand names. Four thousand more since. You are here for every one of them."));
            }

            beats.Add(new(null, null, Narration, Letterbox: true,
                Text: "The final boss arena opens. The last climb of Aincrad begins. Whatever the ending writes, you wrote half of it yourself."));

            return new CutsceneScript(
                EventId: "e10_ruby_palace",
                Title: "The Ruby Palace",
                Unskippable: true,
                Beats: beats.ToArray()
            );
        }
    );
}
