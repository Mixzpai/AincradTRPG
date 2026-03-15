using SAOTRPG.Map;

namespace SAOTRPG.Systems;

/// <summary>
/// Centralized flavor text arrays and atmospheric data.
/// All static string arrays live here to keep TurnManager focused on logic.
/// To add new flavor lines, just append strings to the relevant array.
/// </summary>
public static class FlavorText
{
    // ── Combat ──────────────────────────────────────────────────────────

    /// <summary>Dodge/miss flavor (use {0} for monster name).</summary>
    public static readonly string[] DodgeFlavors =
    {
        "{0} swings wide — you sidestep!",
        "You duck under {0}'s attack!",
        "{0} lunges — you twist away!",
        "A narrow miss! {0}'s strike cuts only air.",
        "You parry {0}'s blow and slip aside!",
        "{0} overextends — you dodge effortlessly!",
    };

    /// <summary>Monster defeat (use {0} for monster name).</summary>
    public static readonly string[] DefeatFlavors =
    {
        ">> {0} defeated! <<",
        ">> {0} crumbles to dust! <<",
        ">> {0} falls! <<",
        ">> {0} shatters into light! <<",
        ">> You vanquish {0}! <<",
    };

    /// <summary>Overkill (use {0} for bonus EXP).</summary>
    public static readonly string[] OverkillFlavors =
    {
        "  Overkill! +{0} bonus EXP",
        "  Obliterated! +{0} bonus EXP",
        "  Annihilated! +{0} bonus EXP",
        "  Destroyed! That was excessive — +{0} bonus EXP",
    };

    /// <summary>Monster aggro alerts (use {0} for monster name).</summary>
    public static readonly string[] AggroAlerts =
    {
        "{0} has spotted you!",
        "{0} locks eyes with you and advances!",
        "{0} snarls and begins to chase!",
        "You've caught {0}'s attention!",
        "{0} turns toward you with hostile intent!",
    };

    /// <summary>Monster flee alerts (use {0} for monster name).</summary>
    public static readonly string[] FleeFlavors =
    {
        "{0} panics and tries to flee!",
        "{0} turns tail and runs!",
        "{0} staggers away in fear!",
        "{0} is retreating — finish it off!",
    };

    /// <summary>Strong block (defense absorbs 50%+ damage).</summary>
    public static readonly string[] BlockFlavors =
    {
        "Your armor absorbs the brunt of the blow!",
        "A solid block! Your defense holds strong.",
        "The hit glances off your gear — barely felt it.",
        "Your equipment takes the hit for you. Good investment.",
        "Steel meets steel. Your armor earns its keep.",
    };

    /// <summary>Low HP survival encouragement.</summary>
    public static readonly string[] LowHpEncouragements =
    {
        "Your vision blurs... but you stand firm.",
        "Pain courses through you. Don't give up.",
        "You're on your last legs. Find a campfire!",
        "Your hands tremble, but your grip holds.",
        "Every step hurts. Keep fighting.",
        "The world spins. You need healing — fast.",
    };

    /// <summary>Player death lines.</summary>
    public static readonly string[] DeathFlavors =
    {
        "You have been defeated...",
        "Your vision fades to black...",
        "The world grows dark. This is the end.",
        "You collapse. Aincrad claims another soul.",
        "Everything goes silent. Game over.",
    };

    // ── Items & Interaction ─────────────────────────────────────────────

    /// <summary>Rare loot fanfare (use {0}=rarity, {1}=item name).</summary>
    public static readonly string[] RareLootFlavors =
    {
        "  ★ {0} find! {1} glows with power!",
        "  ★ A {0} {1} — this is a valuable drop!",
        "  ★ {0} loot! {1} hums with energy.",
        "  ★ Your luck pays off — a {0} {1}!",
    };

    /// <summary>Item pickup (use {0} for item name).</summary>
    public static readonly string[] PickupFlavors =
    {
        "Picked up {0}.",
        "You grab {0} from the ground.",
        "{0} added to your pack.",
        "You pocket {0}. Could be useful.",
    };

    /// <summary>Inventory full warnings.</summary>
    public static readonly string[] InventoryFullFlavors =
    {
        "Inventory is full! Drop or sell something first.",
        "You can't carry any more. Your bags are bursting!",
        "No room! Visit a vendor or drop unwanted items.",
        "Your pack is stuffed. Make some space!",
    };

    /// <summary>Empty ground (G key with no items).</summary>
    public static readonly string[] EmptyGroundFlavors =
    {
        "Nothing to pick up here.",
        "The ground is bare. Nothing useful.",
        "You search around — nothing here.",
        "Just dust and stone. No items.",
    };

    /// <summary>Vendor greetings (use {0} for shop name).</summary>
    public static readonly string[] VendorGreetings =
    {
        "Welcome to {0}! Take a look.",
        "Ah, a customer! Browse {0} at your leisure.",
        "Come in, come in! {0} has what you need.",
        "Another adventurer! {0} is open for business.",
        "Looking for supplies? {0} has the best deals on this floor.",
    };

    /// <summary>NPC fallback dialogue (when NPC has no custom line).</summary>
    public static readonly string[] NpcFallbackDialogue =
    {
        "Be careful on this floor, adventurer.",
        "I've heard strange noises coming from the floor above...",
        "Another brave soul. Good luck out there.",
        "The monsters here are getting stronger. Watch yourself.",
        "Have you found the stairs yet? Keep exploring.",
        "I'm just passing through. Don't mind me.",
    };

    // ── Movement & Environment ──────────────────────────────────────────

    /// <summary>Terrain footstep flavor chance (% per step on special terrain).</summary>
    public const int FootstepChance = 12;

    /// <summary>Terrain-specific footstep flavor text.</summary>
    public static readonly Dictionary<TileType, string[]> TerrainFlavors = new()
    {
        { TileType.Water,       new[] { "Your boots splash through shallow water.", "Cool water laps at your ankles." } },
        { TileType.WaterDeep,   new[] { "You wade through knee-deep water. Something brushes your leg.", "The water here is dark and cold." } },
        { TileType.GrassTall,   new[] { "Tall grass rustles as you push through.", "Something slithers away in the tall grass." } },
        { TileType.Flowers,     new[] { "Wildflowers crunch softly underfoot.", "A faint floral scent fills the air." } },
        { TileType.Path,        new[] { "Your boots crunch on the dirt path.", "Footprints mark this well-traveled road." } },
        { TileType.Bush,        new[] { "Branches scratch at your armor as you pass.", "Leaves rustle in your wake." } },
        { TileType.Door,        new[] { "The heavy door creaks as you pass through.", "You duck through the doorway.", "Old hinges groan as the door swings.", "You slip through the narrow doorway." } },
        { TileType.Mountain,    new[] { "Loose gravel crunches under your weight.", "The rocky terrain slows your pace.", "You scramble over jagged stone." } },
        { TileType.Rock,        new[] { "You squeeze past a weathered boulder.", "The rock face is cold and damp to the touch.", "Pebbles scatter as you brush past the stone." } },
    };

    /// <summary>Campfire rest quotes.</summary>
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

    /// <summary>Campfire at full HP.</summary>
    public static readonly string[] CampfireFullHpFlavors =
    {
        "The campfire crackles warmly. You're already at full health.",
        "You rest by the fire. No wounds to mend, just a moment of peace.",
        "The flames offer comfort, though you need no healing.",
        "Full health. You warm your hands and enjoy the glow.",
        "A brief pause by the fire. You're in fighting shape.",
    };

    /// <summary>Spike trap (use {0} for damage).</summary>
    public static readonly string[] SpikeTrapFlavors =
    {
        "You step on a spike trap! {0} damage!",
        "Spikes shoot up from the floor! {0} damage!",
        "A hidden trap pierces your boots! {0} damage!",
        "CRUNCH — iron spikes bite into your legs! {0} damage!",
        "The ground gives way to cruel spikes! {0} damage!",
    };

    /// <summary>Teleport trap landing.</summary>
    public static readonly string[] TeleportLandingFlavors =
    {
        "You materialize in an unfamiliar part of the floor.",
        "The world snaps back into focus. Where are you?",
        "You land hard on cold stone. Somewhere... else.",
        "A flash of light, and you're elsewhere entirely.",
        "Disoriented, you look around. Nothing here looks familiar.",
    };

    /// <summary>Trap proximity sense.</summary>
    public static readonly string[] TrapSenseFlavors =
    {
        "Your instincts tingle — something dangerous is nearby...",
        "You sense a trap lurking ahead. Tread carefully!",
        "The hairs on your neck stand up. Danger close.",
        "A faint click echoes nearby. Watch your step!",
    };

    /// <summary>Wall bump (10% chance).</summary>
    public static readonly string[] WallBumpFlavors =
    {
        "You bump into the wall. Solid stone.",
        "The wall doesn't budge. Obviously.",
        "Nope. That's a wall.",
        "You press against the stone. Nothing happens.",
    };

    // ── Exploration & Auto-Explore ──────────────────────────────────────

    /// <summary>Auto-explore stop: enemy nearby.</summary>
    public static readonly string[] AutoExploreEnemyFlavors =
    {
        "Auto-explore interrupted — enemy nearby!",
        "You halt — something hostile ahead!",
        "Danger detected! Auto-explore stopped.",
        "Your instincts scream STOP. An enemy lurks close.",
    };

    /// <summary>Auto-explore stop: items found.</summary>
    public static readonly string[] AutoExploreItemFlavors =
    {
        "Auto-explore paused — items here. Press G to pick up.",
        "You stop — something on the ground catches your eye. Press G.",
        "Loot underfoot! Auto-explore paused. Press G to grab.",
        "Your foot nudges something. Items here! Press G.",
    };

    /// <summary>Auto-explore stop: fully explored.</summary>
    public static readonly string[] AutoExploreDoneFlavors =
    {
        "Nothing left to explore on this floor.",
        "Every corner mapped. The floor holds no more secrets.",
        "Exploration complete — nowhere new to go.",
        "You've seen it all. Time to move on.",
    };

    /// <summary>Floor 100% explored.</summary>
    public static readonly string[] FloorCompleteMessages =
    {
        "You've mapped the entire floor. Well done, adventurer.",
        "Every corner of this floor has been explored. Nothing hidden remains.",
        "Floor fully explored! The stairs await when you're ready.",
        "100% mapped. Aincrad reveals its secrets to the persistent.",
    };

    /// <summary>Floor cleared (all monsters dead).</summary>
    public static readonly string[] FloorClearedMessages =
    {
        "The last enemy falls. This floor is clear.",
        "Silence. Every monster on this floor has been slain.",
        "Floor cleared! Nothing left to fight here.",
        "The dungeon grows quiet — all threats eliminated.",
    };

    /// <summary>Staircase discovery (one-time per floor).</summary>
    public static readonly string[] StairsDiscoveryMessages =
    {
        "You spot stairs leading upward through Aincrad!",
        "A staircase ascends toward the next floor.",
        "The way up — stairs to the next floor!",
        "You've found the exit! Stairs ascend nearby.",
    };

    /// <summary>Floor mob count announcement (use {0} for count).</summary>
    public static readonly string[] FloorMobCountFlavors =
    {
        "You sense {0} hostile presence(s) on this floor.",
        "{0} enemies lurk in the shadows ahead.",
        "Your instincts warn you: {0} threats on this floor.",
        "The dungeon stirs. {0} creature(s) await.",
    };

    // ── Progression ─────────────────────────────────────────────────────

    /// <summary>Passive regen notification (30% chance per tick).</summary>
    public static readonly string[] RegenFlavors =
    {
        "Your body mends slowly. +1 HP.",
        "A faint warmth spreads through you. +1 HP.",
        "Your wounds begin to close on their own. +1 HP.",
        "Natural recovery kicks in. +1 HP.",
        "You feel a little better. +1 HP.",
    };

    // ── Atmosphere ──────────────────────────────────────────────────────

    /// <summary>Ambient atmosphere chance (% per movement turn).</summary>
    public const int AmbientChance = 4;

    /// <summary>Ambient atmosphere messages.</summary>
    public static readonly string[] AmbientMessages =
    {
        "A distant bell tolls somewhere above.",
        "The dungeon walls groan under the weight of Aincrad.",
        "You catch a faint glimmer in the corner of your eye — nothing there.",
        "The air shifts. A draft from somewhere above.",
        "Stone dust trickles from the ceiling above you.",
        "A faint hum resonates through the floor beneath your feet.",
        "The shadows dance as your torchlight flickers.",
        "You hear the echo of your own footsteps, delayed and distorted.",
    };

    /// <summary>Ambient sound cues per LootTag for unseen nearby monsters.</summary>
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

    /// <summary>Generic sound cues when LootTag not found.</summary>
    public static readonly string[] GenericSoundCues =
    {
        "You sense something lurking nearby...",
        "The hairs on your neck stand up. Something is close.",
    };

    /// <summary>Turn milestone callout interval.</summary>
    public const int MilestoneInterval = 100;

    /// <summary>Turn milestone messages (use {0} for turn count).</summary>
    public static readonly string[] MilestoneMessages =
    {
        "You've survived {0} turns in Aincrad. Keep pushing forward.",
        "{0} turns in. Aincrad hasn't broken you yet.",
        "Turn {0}. Lesser adventurers would have given up by now.",
        "{0} turns and counting. Aincrad remembers the persistent.",
    };

    /// <summary>Idle flavor threshold and repeat interval.</summary>
    public const int IdleThreshold = 10;
    public const int IdleRepeatInterval = 5;

    /// <summary>Idle flavor text (after standing still 10+ turns).</summary>
    public static readonly string[] IdleFlavors =
    {
        "A cold wind sweeps through the corridor...",
        "You hear distant footsteps echoing off stone walls.",
        "Water drips somewhere in the darkness.",
        "The torchlight flickers, casting long shadows.",
        "An eerie silence settles over the floor.",
        "You catch the faint scent of something burning.",
        "A low rumble trembles through the ground beneath you.",
        "Something skitters in the shadows nearby.",
        "The air feels heavier here. You sense danger.",
        "Faint whispers drift from the floors above.",
        "Your sword arm twitches. The quiet is unnerving.",
        "A distant roar echoes through Aincrad's halls.",
    };

    // ── Floor Titles ────────────────────────────────────────────────────

    /// <summary>Floor-based title progression (sorted descending — first match wins).</summary>
    public static readonly (int Floor, string Title)[] TitleThresholds =
    {
        (100, "Liberator of Aincrad"),
        (75,  "Clearance Hero"),
        (50,  "Floor Conqueror"),
        (35,  "Nightmare Walker"),
        (25,  "Dungeon Scourge"),
        (15,  "Proven"),
        (10,  "Seasoned"),
        (5,   "Survivor"),
        (2,   "Blooded"),
        (1,   "Adventurer"),
    };
}
