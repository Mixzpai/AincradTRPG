using SAOTRPG.Map;

namespace SAOTRPG.Systems;

public static class FlavorText
{
    // ── Combat ──────────────────────────────────────────────────────────

    public static readonly string[] DodgeFlavors =
    {
        "{0} swings wide — you sidestep!", "You duck under {0}'s attack!",
        "{0} lunges — you twist away!", "A narrow miss! {0}'s strike cuts only air.",
        "You parry {0}'s blow and slip aside!", "{0} overextends — you dodge effortlessly!",
    };

    public static readonly string[] DefeatFlavors =
    {
        ">> {0} defeated! <<", ">> {0} crumbles to dust! <<", ">> {0} falls! <<",
        ">> {0} shatters into light! <<", ">> You vanquish {0}! <<",
    };

    public static readonly string[] OverkillFlavors =
    {
        "  Overkill! +{0} bonus EXP", "  Obliterated! +{0} bonus EXP",
        "  Annihilated! +{0} bonus EXP", "  Destroyed! That was excessive — +{0} bonus EXP",
    };

    public static readonly string[] BlockFlavors =
    {
        "Your armor absorbs the brunt of the blow!", "A solid block! Your defense holds strong.",
        "The hit glances off your gear — barely felt it.", "Your equipment takes the hit for you. Good investment.",
        "Steel meets steel. Your armor earns its keep.",
    };

    public static readonly string[] LowHpEncouragements =
    {
        "Your vision blurs... but you stand firm.", "Pain courses through you. Don't give up.",
        "You're on your last legs. Find a campfire!", "Your hands tremble, but your grip holds.",
        "Every step hurts. Keep fighting.", "The world spins. You need healing — fast.",
    };

    public static readonly string[] DeathFlavors =
    {
        "You have been defeated...", "Your vision fades to black...",
        "The world grows dark. This is the end.", "You collapse. Aincrad claims another soul.",
        "Everything goes silent. Game over.",
    };

    public static readonly string[] DeathTips =
    {
        "Use potions before you're desperate — don't wait until 1 HP.",
        "Campfires fully heal you. Look for the warm glow.",
        "Higher floors have stronger monsters but better loot.",
        "Dodge chance scales with Agility — consider investing points.",
        "Shields can fully block attacks. Check the shop for one.",
        "Combo attacks deal increasing bonus damage — focus one target.",
        "Auto-explore (X) stops when enemies or items are nearby.",
        "Sell unused gear at shops for Col to buy upgrades.",
        "Weapon proficiency grows with kills — stick with one type.",
        "Rest (R) skips turns and regenerates HP outside of combat.",
        "Check your equipment durability — broken gear gives no stats.",
        "The danger compass points toward nearby threats.",
        "Eating food restores satiety, which boosts ATK and DEF.",
    };

    // ── Items & Interaction ─────────────────────────────────────────────

    public static readonly string[] RareLootFlavors =
    {
        "  *{0} find! {1} glows with power!", "  *A {0} {1} — this is a valuable drop!",
        "  *{0} loot! {1} hums with energy.", "  *Your luck pays off — a {0} {1}!",
    };

    public static readonly string[] PickupFlavors =
    {
        "Picked up {0}.", "You grab {0} from the ground.",
        "{0} added to your pack.", "You pocket {0}. Could be useful.",
    };

    public static readonly string[] InventoryFullFlavors =
    {
        "Inventory is full! Drop or sell something first.", "You can't carry any more. Your bags are bursting!",
        "No room! Visit a vendor or drop unwanted items.", "Your pack is stuffed. Make some space!",
    };

    public static readonly string[] EmptyGroundFlavors =
    {
        "Nothing to pick up here.", "The ground is bare. Nothing useful.",
        "You search around — nothing here.", "Just dust and stone. No items.",
    };

    public static readonly string[] VendorGreetings =
    {
        "Welcome to {0}! Take a look.", "Ah, a customer! Browse {0} at your leisure.",
        "Come in, come in! {0} has what you need.", "Another adventurer! {0} is open for business.",
        "Looking for supplies? {0} has the best deals on this floor.",
    };

    public static readonly string[] NpcFallbackDialogue =
    {
        "Be careful on this floor, adventurer.", "I've heard strange noises coming from the floor above...",
        "Another brave soul. Good luck out there.", "The monsters here are getting stronger. Watch yourself.",
        "Have you found the stairs yet? Keep exploring.", "I'm just passing through. Don't mind me.",
    };

    // ── Movement & Environment ──────────────────────────────────────────

    public const int FootstepChance = 4;  // reduced from 12 -- less log spam

    public static readonly Dictionary<TileType, string[]> TerrainFlavors = new()
    {
        { TileType.Water,     new[] { "Your boots splash through shallow water.", "Cool water laps at your ankles." } },
        { TileType.WaterDeep, new[] { "You wade through knee-deep water. Something brushes your leg.", "The water here is dark and cold." } },
        { TileType.GrassTall, new[] { "Tall grass rustles as you push through.", "Something slithers away in the tall grass." } },
        { TileType.Flowers,   new[] { "Wildflowers crunch softly underfoot.", "A faint floral scent fills the air." } },
        { TileType.Path,      new[] { "Your boots crunch on the dirt path.", "Footprints mark this well-traveled road." } },
        { TileType.Bush,      new[] { "Branches scratch at your armor as you pass.", "Leaves rustle in your wake." } },
        { TileType.Door,      new[] { "The heavy door creaks as you pass through.", "You duck through the doorway.", "Old hinges groan as the door swings.", "You slip through the narrow doorway." } },
        { TileType.Mountain,  new[] { "Loose gravel crunches under your weight.", "The rocky terrain slows your pace.", "You scramble over jagged stone." } },
        { TileType.Rock,      new[] { "You squeeze past a weathered boulder.", "The rock face is cold and damp to the touch.", "Pebbles scatter as you brush past the stone." } },
    };

    public static readonly string[] CampfireQuotes =
    {
        "You sit by the fire. For a moment, the dungeon feels almost peaceful.",
        "The flames dance gently. You feel your strength returning.",
        "A brief respite in the warmth. Even heroes need to rest.",
        "The crackling fire reminds you of home... before all this.",
        "You warm your hands. The shadows seem to retreat from the light.",
        "Rest now. The floors ahead won't wait forever.",
        "The campfire's glow steadies your nerves. Onward.",
        "Embers drift upward like tiny stars. A quiet moment in Aincrad.",
    };

    public static readonly string[] CampfireFullHpFlavors =
    {
        "The campfire crackles warmly. You're already at full health.",
        "You rest by the fire. No wounds to mend, just a moment of peace.",
        "The flames offer comfort, though you need no healing.",
        "Full health. You warm your hands and enjoy the glow.",
        "A brief pause by the fire. You're in fighting shape.",
    };

    public static readonly string[] SpikeTrapFlavors =
    {
        "You step on a spike trap! {0} damage!", "Spikes shoot up from the floor! {0} damage!",
        "A hidden trap pierces your boots! {0} damage!", "CRUNCH — iron spikes bite into your legs! {0} damage!",
        "The ground gives way to cruel spikes! {0} damage!",
    };

    public static readonly string[] TeleportLandingFlavors =
    {
        "You materialize in an unfamiliar part of the floor.", "The world snaps back into focus. Where are you?",
        "You land hard on cold stone. Somewhere... else.", "A flash of light, and you're elsewhere entirely.",
        "Disoriented, you look around. Nothing here looks familiar.",
    };

    public static readonly string[] TrapSenseFlavors =
    {
        "Your instincts tingle — something dangerous is nearby...", "You sense a trap lurking ahead. Tread carefully!",
        "The hairs on your neck stand up. Danger close.", "A faint click echoes nearby. Watch your step!",
    };

    public static readonly string[] WallBumpFlavors =
    {
        "You bump into the wall. Solid stone.", "The wall doesn't budge. Obviously.",
        "Nope. That's a wall.", "You press against the stone. Nothing happens.",
    };

    // ── Exploration & Auto-Explore ──────────────────────────────────────

    public static readonly string[] AutoExploreEnemyFlavors =
    {
        "Auto-explore interrupted — enemy nearby!", "You halt — something hostile ahead!",
        "Danger detected! Auto-explore stopped.", "Your instincts scream STOP. An enemy lurks close.",
    };

    public static readonly string[] AutoExploreItemFlavors =
    {
        "Auto-explore paused — items here. Press G to pick up.",
        "You stop — something on the ground catches your eye. Press G.",
        "Loot underfoot! Auto-explore paused. Press G to grab.",
        "Your foot nudges something. Items here! Press G.",
    };

    public static readonly string[] AutoExploreDoneFlavors =
    {
        "Nothing left to explore on this floor.", "Every corner mapped. The floor holds no more secrets.",
        "Exploration complete — nowhere new to go.", "You've seen it all. Time to move on.",
    };

    public static readonly string[] FloorCompleteMessages =
    {
        "You've mapped the entire floor. Well done, adventurer.",
        "Every corner of this floor has been explored. Nothing hidden remains.",
        "Floor fully explored! The stairs await when you're ready.",
        "100% mapped. Aincrad reveals its secrets to the persistent.",
    };

    public static readonly string[] FloorClearedMessages =
    {
        "The last enemy falls. This floor is clear.", "Silence. Every monster on this floor has been slain.",
        "Floor cleared! Nothing left to fight here.", "The dungeon grows quiet — all threats eliminated.",
    };

    public static readonly string[] StairsDiscoveryMessages =
    {
        "You spot stairs leading upward through Aincrad!", "A staircase ascends toward the next floor.",
        "The way up — stairs to the next floor!", "You've found the exit! Stairs ascend nearby.",
    };

    public static readonly string[] FloorMobCountFlavors =
    {
        "You sense {0} hostile presence(s) on this floor.", "{0} enemies lurk in the shadows ahead.",
        "Your instincts warn you: {0} threats on this floor.", "The dungeon stirs. {0} creature(s) await.",
    };

    // ── Progression ─────────────────────────────────────────────────────

    public static readonly string[] RegenFlavors =
    {
        "Your body mends slowly. +1 HP.", "A faint warmth spreads through you. +1 HP.",
        "Your wounds begin to close on their own. +1 HP.", "Natural recovery kicks in. +1 HP.",
        "You feel a little better. +1 HP.",
    };

    // ── Atmosphere ──────────────────────────────────────────────────────

    public const int AmbientChance = 2;  // reduced from 4 -- less log spam

    public static readonly string[] AmbientMessages =
    {
        "A distant bell tolls somewhere above.", "The dungeon walls groan under the weight of Aincrad.",
        "You catch a faint glimmer in the corner of your eye — nothing there.",
        "The air shifts. A draft from somewhere above.", "Stone dust trickles from the ceiling above you.",
        "A faint hum resonates through the floor beneath your feet.",
        "The shadows dance as your torchlight flickers.",
        "You hear the echo of your own footsteps, delayed and distorted.",
    };

    public static readonly Dictionary<string, string[]> SoundCues = new()
    {
        { "beast",     new[] { "You hear growling nearby...", "Something sniffs the air in the distance." } },
        { "kobold",    new[] { "Guttural chattering echoes from the shadows...", "You hear crude weapons scraping stone." } },
        { "insect",    new[] { "A faint buzzing reaches your ears...", "Something skitters in the dark." } },
        { "plant",     new[] { "A rustling sound... not the wind.", "You hear creaking vines nearby." } },
        { "humanoid",  new[] { "Footsteps echo ahead...", "You hear muttered speech nearby." } },
        { "reptile",   new[] { "A slithering sound comes from nearby...", "You hear scales scraping stone." } },
        { "undead",    new[] { "A low moan drifts through the corridor...", "Bones rattle somewhere close." } },
        { "construct", new[] { "Grinding gears echo nearby...", "Heavy mechanical steps thud in the distance." } },
        { "dragon",    new[] { "A deep rumble shakes the ground...", "You feel intense heat from ahead." } },
        { "elemental", new[] { "Crackling energy fills the air...", "A strange hum vibrates through the walls." } },
    };

    public static readonly string[] GenericSoundCues =
    {
        "You sense something lurking nearby...", "The hairs on your neck stand up. Something is close.",
    };

    public const int MilestoneInterval = 100;

    public static readonly string[] MilestoneMessages =
    {
        "You've survived {0} turns in Aincrad. Keep pushing forward.",
        "{0} turns in. Aincrad hasn't broken you yet.",
        "Turn {0}. Lesser adventurers would have given up by now.",
        "{0} turns and counting. Aincrad remembers the persistent.",
    };

    public const int IdleThreshold = 15;
    public const int IdleRepeatInterval = 10;

    public static readonly string[] IdleFlavors =
    {
        "A cold wind sweeps through the corridor...", "You hear distant footsteps echoing off stone walls.",
        "Water drips somewhere in the darkness.", "The torchlight flickers, casting long shadows.",
        "An eerie silence settles over the floor.", "You catch the faint scent of something burning.",
        "A low rumble trembles through the ground beneath you.", "Something skitters in the shadows nearby.",
        "The air feels heavier here. You sense danger.", "Faint whispers drift from the floors above.",
        "Your sword arm twitches. The quiet is unnerving.", "A distant roar echoes through Aincrad's halls.",
    };

    // ── Lore Stones ─────────────────────────────────────────────────

    public static readonly string[] LoreStoneEntries =
    {
        "\"Aincrad was not built — it was dreamed into being by Cardinal.\"",
        "\"The first floor was once a paradise. Now only the strong survive it.\"",
        "\"They say the 100th floor holds the key to freedom. But at what cost?\"",
        "\"Cardinal watches all. Every sword swing, every footstep. Every death.\"",
        "\"The Hollow Area was sealed long ago. Some say its monsters still grow stronger.\"",
        "\"Before the death game, this was simply a game. The walls remember laughter.\"",
        "\"Floor bosses guard the only path upward. There is no other way.\"",
        "\"Safe zones exist between floors. Rumors say one holds a legendary blacksmith.\"",
        "\"The NerveGear translates pain as data. In here, that data is very real.\"",
        "\"Some players have given up climbing. They live on the lower floors, farming and waiting.\"",
        "\"Dual-wielding was thought impossible. Until someone proved otherwise.\"",
        "\"The clearing guilds push ever higher, but the gap between floors grows wider.\"",
        "\"Crystal items respond to voice commands. The system listens to everything.\"",
        "\"Monster AI adapts over time. What worked yesterday may fail tomorrow.\"",
        "\"Somewhere in Aincrad, a player forges weapons that rival boss drops.\"",
    };

    // ── Floor-Themed Entry Messages ───────────────────────────────────

    public static readonly (int Min, int Max, string[] Messages)[] FloorEntryTiers =
    {
        (1, 10, new[]
        {
            "Sunlight filters through Aincrad's crystal ceiling. The lower floors still feel alive.",
            "Green fields and gentle breezes. Don't let the scenery fool you.",
            "The sounds of nature echo through the first tier. Birds? Or mimics?",
            "Wooden structures and earthen paths. The beginning of a long journey.",
        }),
        (11, 25, new[]
        {
            "The architecture shifts — carved stone replaces natural ground.",
            "Torchlight flickers in long corridors. The middle floors are darker.",
            "You smell iron and old blood. Many have fought here before you.",
            "The walls are scarred with claw marks. Something large passed through.",
        }),
        (26, 50, new[]
        {
            "The air is thin and cold. Ice crystals form on the walls.",
            "Massive gears turn behind the walls. This floor is mechanical.",
            "Strange runes pulse in the stonework. Magic saturates everything here.",
            "The gravity feels heavier. Each step demands more effort.",
        }),
        (51, 75, new[]
        {
            "The floor hums with energy. You feel it in your bones.",
            "Floating platforms and impossible geometry. Aincrad defies reason here.",
            "The sky visible through cracks shows only void. How high are you?",
            "Ancient statues line the halls, their eyes seeming to follow you.",
        }),
        (76, 100, new[]
        {
            "The final tier. The air crackles with raw power.",
            "Reality itself seems thin here. Glitches flicker at the edges of your vision.",
            "You've climbed higher than almost anyone. Freedom is close.",
            "The endgame floors. Every monster here is a death sentence for the unprepared.",
        }),
    };

    public static string GetFloorEntryMessage(int floor)
    {
        foreach (var tier in FloorEntryTiers)
            if (floor >= tier.Min && floor <= tier.Max)
                return tier.Messages[Random.Shared.Next(tier.Messages.Length)];
        return "The dungeon stretches onward. You press deeper into Aincrad.";
    }

    // ── Tall Grass Ambush ──────────────────────────────────────────────

    public static readonly string[] AmbushFlavors =
    {
        "Something lunges from the tall grass — ambush!",
        "You're caught off guard! A creature strikes from the undergrowth!",
        "The grass erupts — a hidden foe attacks before you can react!",
        "A shadow bursts from the foliage — you didn't see it coming!",
    };

    public static readonly string[] AmbushAvoidedFlavors =
    {
        "You sense movement in the grass and sidestep just in time.",
        "Your instincts warn you — you enter the tall grass carefully.",
        "Something shifts nearby, but your reflexes keep you safe.",
    };

    // ── Bounty Board ──────────────────────────────────────────────────

    public static readonly string[] BountyAcceptFlavors =
    {
        "A contract catches your eye. Time to hunt.", "You tear a bounty notice from the board. The hunt begins.",
        "A kill contract — straightforward work for good Col.", "The board offers work. You accept without hesitation.",
    };

    // ── Combo Finisher ──────────────────────────────────────────────

    public static readonly string[] ComboFinisherFlavors =
    {
        "*** SWORD SKILL — STARBURST STREAM! ***", "*** SWORD SKILL — VERTICAL ARC! ***",
        "*** SWORD SKILL — NOVA ASCENSION! ***", "*** SWORD SKILL — SAVAGE FULCRUM! ***",
    };

    // ── Floor Titles ────────────────────────────────────────────────────

    public static readonly (int Floor, string Title)[] TitleThresholds =
    {
        (100, "Liberator of Aincrad"), (75, "Clearance Hero"), (50, "Floor Conqueror"),
        (35, "Nightmare Walker"), (25, "Dungeon Scourge"), (15, "Proven"),
        (10, "Seasoned"), (5, "Survivor"), (2, "Blooded"), (1, "Adventurer"),
    };
}
