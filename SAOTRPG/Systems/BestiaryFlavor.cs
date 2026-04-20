namespace SAOTRPG.Systems;

// Bestiary flavor. Keyed by mob base Name (strip Elite/Champion affixes).
// Canon: SAO Wiki/Progressive/HF/IF. Non-canon → GenerateGenericBlurb
// (LootTag + floor band).
public static class BestiaryFlavor
{
    // Strip Elite/Champion/affix prefixes to hit the canonical key.
    // Matches MobFactory's naming convention so Bestiary Names with
    // variant/affix prefixes still resolve to base canon flavor.
    public static string Lookup(string name, string? lootTag = null,
        int minFloor = 1, int maxFloor = 100)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";

        // Try exact match first.
        if (ByMobName.TryGetValue(name, out var canon)) return canon;

        // Try stripping up to two known prefixes ("Elite ", "Champion ", affix).
        string stripped = name;
        for (int i = 0; i < 3; i++)
        {
            int sp = stripped.IndexOf(' ');
            if (sp <= 0) break;
            string candidate = stripped[(sp + 1)..];
            if (ByMobName.TryGetValue(candidate, out var c)) return c;
            stripped = candidate;
        }

        return GenerateGenericBlurb(name, lootTag ?? "generic", minFloor, maxFloor);
    }

    // Non-canon mobs receive a short 1-2 sentence blurb based on loot tag
    // and floor band. Keeps tone neutral and varied.
    public static string GenerateGenericBlurb(string name, string lootTag,
        int minFloor, int maxFloor)
    {
        string band = minFloor <= 5 ? "the lowest floors"
            : minFloor <= 10 ? "the early stone tiers"
            : minFloor <= 25 ? "the mid-tier frontier"
            : minFloor <= 50 ? "the industrial heights"
            : minFloor <= 75 ? "the endgame approach"
            : "the Hollow Fragment depths";

        string tagLine = lootTag switch
        {
            "beast"     => "A wild predator that hunts by instinct and scent.",
            "insect"    => "A chitinous swarm-creature; rarely alone for long.",
            "undead"    => "An unliving husk animated by Aincrad's old sorrow.",
            "dragon"    => "A scaled flier — rarer than its kin, and more dangerous.",
            "humanoid"  => "A two-legged adversary; likely armed and coordinated.",
            "construct" => "A stone-or-metal automaton that ignores pain entirely.",
            "plant"     => "Rooted on the surface — until something warm steps close.",
            "reptile"   => "Cold-blooded and patient. It waits for the wrong move.",
            "aquatic"   => "At home in water; fights better there than on land.",
            "elemental" => "A bound element given hostile shape by the Cardinal System.",
            "hollow"    => "A corrupted entity from the Hollow Area's data bleed.",
            "kobold"    => "A canine humanoid of the labyrinth — pack hunter, weapon-user.",
            _           => "A creature of Aincrad whose origins are not well-documented.",
        };

        return $"{tagLine} Seen on {band}.";
    }

    // Canon SAO monster flavor. Keys must match MobFactory and BossFactory
    // Name fields exactly. Adding a new canon mob? Drop a 3-5 sentence entry
    // here and it appears automatically in the Bestiary Lore tab.
    public static readonly Dictionary<string, string> ByMobName = new()
    {
        // ── Floor 1 mobs (Progressive Vol 1) ─────────────────────────────
        ["Frenzy Boar"] =
            "A tusked boar driven mad by the Aincrad air, roaming the grasslands " +
            "of the Town of Beginnings and Horunka. Telegraphs a straight-line charge " +
            "before committing; sidestepping flips the fight into your favor. " +
            "Diavel's famous 'last attack bonus' demo was fought against this species, " +
            "making it the first monster almost every Aincrad prisoner ever faced.",

        ["Little Nepent"] =
            "A squat pitcher-plant mob whose vines lash and drip. The venom is " +
            "rarely lethal but the stun is brutal in packs. Harvested by Horunka " +
            "villagers for alchemical reagents; the Anneal Blade quest requires " +
            "a specific mutant-nepent drop.",

        ["Sharp-Hook Nepent"] =
            "A barbed variant of the Little Nepent. Its hook-vines open shallow " +
            "wounds that bleed through most cloth armor. Most commonly found near " +
            "Horunka village's western tree-line.",

        ["Three-Pronged Nepent"] =
            "An older, larger Nepent with three venomous heads. Can keep three " +
            "attackers poisoned at once. Rare spawn — drops the seed reagent that " +
            "opens the late Floor 1 alchemy quest.",

        ["Dire Wolf"] =
            "A grey pack-beast that patrols the grasslands at night. Leaps across " +
            "several tiles with each attack. Clearers in the first month of Aincrad " +
            "lost more HP to Dire Wolves than to Kobolds — they move faster than " +
            "most beginners expect.",

        ["Ruin Kobold Trooper"] =
            "A canine humanoid of the Floor 1 labyrinth, armed with scavenged " +
            "weapons and crude armor. They summon Sentinels when their pack drops " +
            "below half strength. Illfang's entire army is built from these.",

        ["Ruin Kobold Sentinel"] =
            "The shield-bearing arm of the Kobold host. Sentinels flank Troopers " +
            "and the Kobold Lord, interposing their iron shields to soak hits " +
            "meant for their allies. Slow, but patient.",

        ["Windwasp"] =
            "An insect the size of a hand, aggressive in open grassland. Drops " +
            "the Wind Wasp Needle — a low-tier crafting reagent. Travels in drift-" +
            "patterns; baiting them into a corridor neutralizes their leap.",

        // ── Floor 2 mobs (Progressive Vol 2) ─────────────────────────────
        ["Lesser Taurus"] =
            "A minotaur-kin of the Urbus foothills. Smaller than the Taurus Kings " +
            "but faster on the turn. Charges like a freight train when you drop " +
            "your guard.",

        ["Trembling Ox"] =
            "A massive, dim-witted bull-mob whose stomps crack the earth in a " +
            "cone. The tremor stuns anyone who isn't airborne when the hoof lands. " +
            "Classic 'switch target' lesson for early parties.",

        ["Heavy Hammer Taurus"] =
            "A weaponized Taurus wielding a stone maul. Its overhead smash stuns " +
            "on contact. Isolate from its Ox and Bullbous Bow pack-mates before " +
            "engaging.",

        ["Bullbous Bow"] =
            "A Floor 2 field boss — a red-armored bull that paws the earth before " +
            "charging. Drops the bullbous-horn reagent that unlocks the floor boss " +
            "room in some Progressive timelines. Dangerous to underleveled soloists.",

        ["Plumed Mist Lizard"] =
            "A feathered reptile that breathes a cold mist, slowing anything caught " +
            "in the cloud. Uses terrain to flank; don't fight it in open space.",

        // ── Floor 3 mobs (Progressive Vol 3 — Elf War) ───────────────────
        ["Treant Sapling"] =
            "A young walking-tree of Zumfut. Slow and weak alone, but the " +
            "forest floors spawn them in thickets. Each kill risks pulling the " +
            "Elder Treant from deeper cover.",

        ["Elder Treant"] =
            "A centuries-old treant, twisted by the Elf War. Its roots slow anyone " +
            "adjacent. Kayaba himself is said to have modeled their AI after real " +
            "deep-forest ambushers.",

        ["Forest Elf Scout"] =
            "An elven skirmisher from the Progressive Elf War questline. Ranged, " +
            "agile, and loyal to its faction — attacking a Dark Elf Scout may " +
            "permanently lock you out of Kizmel's questline.",

        ["Kobold Trapper"] =
            "A Floor 3 variant of the lowland kobold. Lays slow-field snares and " +
            "leashes its prey into ambushes. Treat every unfamiliar corridor as " +
            "potentially trapped.",

        ["Cave Bat"] =
            "A nocturnal swarmer of the Zumfut caverns. Individually trivial; " +
            "twelve together can blackout a corridor and gnaw through a party " +
            "healer in seconds.",

        ["Toadstool Walker"] =
            "A bipedal fungus-thing. Releases poison spores on death — finish it " +
            "from two tiles away or accept the dot.",

        // ── Floor 4 mobs (Progressive Vol 4 — Rovia flooded) ─────────────
        ["Water Drake"] =
            "A long-bodied serpent-dragon of Rovia's flooded streets. Swims as well " +
            "as it walks. Ranged bite attack from deep water — positioning beats " +
            "DPS here.",

        ["Lakeshore Crab"] =
            "A reinforced-carapace crab common in Rovia shallows. Low threat " +
            "alone; armor-focused, so piercing weapons clear them fastest.",

        ["Giant Clam"] =
            "A stationary ambush-mob that snaps shut on adjacent feet. Easy XP " +
            "if you pull it with a ranged hit; dangerous if you blunder into one " +
            "while rowing.",

        ["Water Wight"] =
            "A drowned spirit bound to the flooded canals. Slow but persistent. " +
            "Its touch saps stamina; Purification-grade cleansing items break " +
            "the debuff.",

        ["Scavenger Toad"] =
            "A heavy-bodied toad that feeds on Rovia's drift-corpses. Poisonous " +
            "tongue-lash, weak constitution — a clean critical usually one-shots.",

        // ── Floor 5 mobs (Progressive Vol 5 — Pitch-Black Cathedral) ─────
        ["Cathedral Bat"] =
            "A twisted bat that nests in the Karluin cathedral rafters. Hunts " +
            "by sound; being still for a single turn can drop its aggro.",

        ["Cursed Ghoul"] =
            "A stunned-cadaver mob with a rictus grin. Strike-stuns on melee hit. " +
            "The Pitch-Black Cathedral's acolytes become these when the ritual " +
            "fails.",

        ["Shadow Stalker"] =
            "A blur of dark cloth and claws. Leaps multiple tiles and specializes " +
            "in flanking. Turn your back to a corner if possible.",

        ["Vacant Sentinel"] =
            "A stone-armored guardian of the Karluin cathedral doors. Slow, stuns " +
            "on charge. Some players brute-force its DEF; others lure it onto its " +
            "own floor traps.",

        ["Skeleton Warrior"] =
            "A plated skeletal soldier. Straightforward melee opponent — no " +
            "special abilities, just consistent pressure and decent HP.",

        // ── Boss roster (canon) ──────────────────────────────────────────
        ["Illfang the Kobold Lord"] =
            "The Floor 1 boss and the first monster to kill a frontline clearer. " +
            "Four HP bars, talwar-and-buckler until the last bar — when he swaps " +
            "to a nodachi Diavel mistakenly anticipated and paid for with his life. " +
            "Kirito finished the fight with Asuna's help, earning the Coat of " +
            "Midnight trophy and the first real step toward Floor 2.",

        ["Asterius the Taurus King"] =
            "The Floor 2 boss — a Taurus King flanked by Baran the General Taurus " +
            "and Nato the Colonel Taurus. A three-target raid encounter: the " +
            "generals counter if you hit the king first, so switching discipline " +
            "matters more here than raw damage.",

        ["Nerius the Evil Treant"] =
            "The Floor 3 boss — a corrupted ancient tree at the heart of Zumfut's " +
            "lightless grove. Roots the frontline in place while it lobs " +
            "brambles at the backline. Fire-element weapons are canonically " +
            "effective.",

        ["Wythege the Hippocampus"] =
            "The Floor 4 boss, a serpent-seahorse hybrid that fights in the " +
            "flooded labyrinth. Part of the Rovia arc. Dives between phases, " +
            "forcing the raid to track it through underwater tiles before it " +
            "resurfaces to strike.",

        ["Fuscus the Vacant Colossus"] =
            "The Floor 5 boss — a hollow stone golem from Integral Factor canon. " +
            "Enormous HP pool, straightforward attacks; most raids win by " +
            "grinding through its layers of outer armor one plate at a time.",

        ["The Irrational Cube"] =
            "The Floor 6 boss — a geometric puzzle-boss that rolls across the " +
            "raid floor inflicting damage equal to whichever face is up. Canon " +
            "raid comp included a math-minded sub-team whose job was just to " +
            "track face transitions.",

        ["Kagachi the Samurai Lord"] =
            "The Floor 10 boss — a giant reptilian in samurai armor wielding a " +
            "katana almost as tall as the raid leader. Integral Factor canon. " +
            "Sheathes and quick-draws between phases; the draw has no telegraph, " +
            "only a cold blue shimmer.",

        ["The Gleam Eyes"] =
            "The Floor 74 boss and the single most infamous encounter of the " +
            "death game. Blue-skinned, goat-headed, wielding a zanbato the height " +
            "of a man. Purple energy trails every swing. Kirito soloed it in " +
            "canon by combining Dual Blades with Asuna's Switch — an encounter " +
            "that took years off the lives of everyone watching.",

        ["The Skull Reaper"] =
            "The Floor 75 boss. A centipede-skeleton with twin scythe-arms that " +
            "reach across ten tiles. Killed fourteen high-level clearers in the " +
            "first minute of the fight in canon before Kirito and Asuna " +
            "triangulated its weak spot. The last encounter of the original " +
            "death-game clear.",

        // ── Hollow Fragment Floor 76-100 bosses ──────────────────────────
        ["The Ghastlygaze"] =
            "The Floor 76 boss — an all-seeing aberration from Hollow Fragment. " +
            "Paralyzes anyone it makes eye contact with. Raid comp required " +
            "blindfolded tanks in the first kill video.",

        ["The Crystalize Claw"] =
            "The Floor 77 boss. A scorpion-form construct sheathed in prismatic " +
            "crystal plates. Its pincers splinter weapons on block — parry or " +
            "dodge, never hard-block.",

        ["The Horn of Madness"] =
            "The Floor 78 boss — a berserker minotaur-kin that swings faster " +
            "the lower its HP. Standard HF endgame 'enrage' mechanic amplified " +
            "to uncomfortable levels.",

        ["The Tempest of Trihead"] =
            "The Floor 79 boss. A three-headed storm hydra; each head channels " +
            "a different element. Damage distribution across the three heads " +
            "matters — lopsided kills trigger a phase-5 wipe-lash.",

        ["The Guilty Scythe"] =
            "The Floor 80 boss. A reaper-type wielding a two-headed scythe. " +
            "Every third swing applies a stacking 'Guilt' debuff that " +
            "amplifies damage taken. Canon burn window: break the stacks at 3.",

        ["The Knight of Darkness"] =
            "The Floor 81 boss — a black paladin in HF's void keep. Swaps " +
            "between a greatsword and a cross-shield; shield-phase reflects a " +
            "portion of melee damage. Ranged classes shine here.",

        ["The Legacy of Grand"] =
            "The Floor 82 boss. An ancient construct guardian the size of a " +
            "two-story building. Summons smaller 'child' constructs from its " +
            "chest-cavity through the fight.",

        ["The Horn of Furious"] =
            "The Floor 83 boss. A flame-aura minotaur lord. Upgrade of Floor 78's " +
            "Horn of Madness; same tempo, faster swing speed, lava-tile carpet " +
            "over the arena floor.",

        ["The Queen of Ant"] =
            "The Floor 84 boss. A matriarch insectoid that tunnels beneath the " +
            "raid and surfaces mid-formation. The colony-summon phase locks the " +
            "arena until every larva is cleared.",

        ["The Maelstrom of Trihead"] =
            "The Floor 85 boss. The upgraded Tempest. Same three-headed hydra " +
            "base; its 'storm' phase now stacks weather effects across the " +
            "raid floor that persist until dispelled.",

        ["The King of Skeleton"] =
            "The Floor 86 boss — a lich-lord on a bone throne. Cannot be damaged " +
            "until its throne-guard skeletons are cleared. The throne itself is " +
            "the dismissable component; canon kill-order is guards -> throne -> king.",

        ["The Radiance Eater"] =
            "The Floor 87 boss. A light-devouring beast that absorbs damage " +
            "from radiance-element attacks and converts it into a healing aura. " +
            "Dark-element parties clear it in half the time.",

        ["The Rebellious Eyes"] =
            "The Floor 88 boss. A hundred-eyed aberration whose gaze can " +
            "trigger Cardinal System errors at random — a coin-flip status " +
            "lottery every three turns.",

        ["The Murderer Fang"] =
            "The Floor 89 boss. An alpha beast with a bleeding-pack aura. " +
            "Every mob on the floor gains Bleed-inflicting attacks while this " +
            "boss lives; clear order matters more than damage output.",

        ["The Ruler of Blade"] =
            "The Floor 90 boss. An armored swordmaster; every weapon skill it " +
            "uses corresponds to a known player-class unlock. Blade-mirror " +
            "mechanics: it learns the player's sword skills over the course " +
            "of the fight.",

        ["The Absolute Gazer"] =
            "The Floor 91 boss. A massive eye-and-tendril aberration that " +
            "pierces through terrain. There is no 'line of sight' cover on " +
            "this floor — only distance.",

        ["The Chaos Dragon"] =
            "The Floor 92 boss. A multi-headed dragon whose elemental damage " +
            "type rotates every phase. Dragon-slayer consumables are canonically " +
            "stocked for this fight.",

        ["The Lava Creeper"] =
            "The Floor 93 boss. A magma-serpent that tunnels beneath the arena, " +
            "surfacing as molten columns that become permanent hazards.",

        ["The Knight of Blazing"] =
            "The Floor 94 boss. A fire-clad paladin wielding a flamberge. " +
            "Immolation aura deals tick damage to anyone within 2 tiles; " +
            "reach weapons dominate.",

        ["The Genocide Eyes"] =
            "The Floor 95 boss. Rumored to have killed every raid group that " +
            "attempted it in the first HF clear week. Death-gaze range increases " +
            "each phase.",

        ["The Slaughter Fang"] =
            "The Floor 96 boss. Alpha of the bleeding pack, upgraded from F89. " +
            "Same aura; this version stacks bleeds rather than refreshing them.",

        ["The Emperor of Death"] =
            "The Floor 97 boss. A reaper-emperor whose scythe-reach matches the " +
            "entire raid floor diagonal. Positioning is the whole fight.",

        ["The Kaiser Dragon"] =
            "The Floor 98 boss. The culmination of HF's dragon line. Canon kill " +
            "required the full clearing group of the HF endgame, plus every " +
            "dragon-slayer consumable the merchant tree can cook.",

        ["The Ruler of Deities"] =
            "The Floor 99 boss. Flanked by four sub-bosses that must be cleared " +
            "in a specific order or the Ruler's phase-shift triggers an arena-" +
            "wide wipe.",

        ["The Hollow Avatar"] =
            "The Floor 100 boss of Hollow Fragment. A player-model avatar " +
            "flanked by the MHCP programs — Yui, Strea, and their sisters — " +
            "before Heathcliff reveals himself as the true final layer.",

        // ── Notable field bosses / event bosses ──────────────────────────
        ["The Fatal Scythe"] =
            "A hidden-dungeon boss beneath the Black Iron Palace on Floor 1. " +
            "Appears only under specific conditions. Legendarily dangerous for " +
            "its floor bracket — killed more players than Illfang.",

        ["Nephila Regina"] =
            "A Floor 1 field boss — a huge spider-queen whose web covers the " +
            "entire approach corridor. Drop-table contributed to the earliest " +
            "endgame rapier crafts.",

        ["Nicholas the Renegade"] =
            "The fallen Christmas Knight — a seasonal field boss in Aincrad's " +
            "winter event. Guarantees the Divine Stone of Returning Soul on " +
            "kill, the only canon item capable of reviving a dead player " +
            "within ten seconds. Canonically contested between Kirito, the " +
            "Holy Dragon Alliance, and a Laughing Coffin PK squad.",

        ["X'rphan the White Wyrm"] =
            "A pale dragon of the higher Aincrad peaks. Canon field encounter. " +
            "Drops the Crystallite Ingot that Lisbeth forged Dark Repulser from.",

        ["Geocrawler"] =
            "A burrowing menace — a huge worm-type field boss introduced on " +
            "the mid-floors. Tunnels between terrain features, resurfacing in " +
            "the middle of the raid.",

        ["The King of Lakes"] =
            "A Floor 22 field boss — a lake-dwelling leviathan whose presence " +
            "is only felt when the water goes unnaturally still.",

        ["Forest King Stag"] =
            "A Floor 22 field boss from Kirito and Asuna's honeymoon forest. " +
            "A crowned antlered beast. Drops the Kingly Antler crafting reagent.",

        ["Magnatherium"] =
            "The Mammoth of the Memory Hill — a colossal tusked beast said to " +
            "carry the memories of every player who died on its floor in its " +
            "hide. Canon drop: the Mammoth Tusk reagent for late-game greatswords.",

        ["Frost Dragon"] =
            "A Floor 48 field boss. A white wyrm of the Crystallite Cave. Canon " +
            "source of the Crystallite Ingot Lisbeth forged Dark Repulser from.",

        // ── Endgame era mobs (F51-75) — mix of canon + themed ────────────
        ["Fatal Scythe Echo"] =
            "A lesser echo of the Floor 1 Fatal Scythe hidden-boss. Appears " +
            "in the endgame labyrinth as a wandering terror; two-hit combos " +
            "that stun even through guard.",

        ["Laughing Coffin PKer"] =
            "A player-killer from Aincrad's most infamous red guild. Armed, " +
            "ruthless, and almost always encountered in pairs. PoH's doctrine " +
            "of 'one of us dies either way' means they will not retreat.",

        ["Titan's Hand PKer"] =
            "A lesser orange-guild raider from Titan's Hand, Rosalia's crew. " +
            "Bleeds targets, then waits for stragglers to fall behind.",
    };
}
