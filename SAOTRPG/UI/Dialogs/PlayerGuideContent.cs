namespace SAOTRPG.UI.Dialogs;

// Static content for the in-game Player Guide (hotkey B).
// Organized as a flat list of (Category, Title, Body) entries.
// The dialog renders the category headers inline and shows one body at a time.
// Compiled from 5 parallel research agents 2026-04-17 — foundation pass,
// iterate content as gameplay evolves.
public static class PlayerGuideContent
{
    public record GuideEntry(string Category, string Title, string Body);

    public static readonly GuideEntry[] Entries =
    {
        // ═══════════════════════════════════════════════════════════════
        // ── 1. Combat & Rarity ─────────────────────────────────────────
        // ═══════════════════════════════════════════════════════════════

        new("Combat & Rarity", "Damage Formula",
            "Final damage = your Attack + weapon proficiency bonus + combo bonus + shrine buff\n" +
            "+ level-up surge + satiety/fatigue + biome modifier, all multiplied by unique-skill\n" +
            "percentages (Holy Sword, Martial Arts, elemental affinity, etc.).\n\n" +
            "Backstabs from unseen enemies deal x2 damage. The 5th hit of a combo doubles damage\n" +
            "as a Combo Finisher. Your weapon loses 1 durability per swing — watch for the\n" +
            "\"about to break\" warning at 5 durability."),

        new("Combat & Rarity", "Critical Hits",
            "Crits deal bonus damage and flash \"CRIT!\" in BrightRed. Base crit chance is 5%, plus\n" +
            "+1% per 2 Dexterity (formula: 5 + Dex/2). Crit bonus damage = 10 + Dex.\n\n" +
            "Weather can shift crit rate up or down. Weapon proficiency gives +2% crit at 50 kills\n" +
            "(Journeyman) and +3% more at 1000 kills (Weapon Lord). Some weapons (Lambent Light,\n" +
            "Masamune) add flat CritRate via SpecialEffect. CritHeal effects heal a % on crit."),

        new("Combat & Rarity", "Combo Attacks",
            "Hitting the same target on consecutive turns stacks combos. +2 bonus damage per stack\n" +
            "beyond the first. At 2/3/4 stacks you see Double/Triple/Quad Strike in the log.\n\n" +
            "The 5th consecutive hit is a Combo Finisher — DOUBLES your total damage and resets\n" +
            "the counter. Switching targets resets the combo. Weapons with ComboBonus+N scale\n" +
            "the combo bonus by N%."),

        new("Combat & Rarity", "Defense — Block, Parry, Dodge",
            "When a monster hits you, three checks roll in order:\n\n" +
            "• BLOCK: Shield BlockChance + weapon BlockChance SpecialEffect. Full negation.\n" +
            "  Degrades the shield on success.\n" +
            "• PARRY: min(Dex, 15)% + proficiency + weapon ParryChance. Counter-strike for 25%\n" +
            "  of your Attack.\n" +
            "• DODGE: min(Agi × 2, 20)% + proficiency. Slow halves dodge. Naked Ingress gives\n" +
            "  +25%. Three dodges in a row triggers \"Matrix mode!\", five triggers \"Untouchable!\"."),

        new("Combat & Rarity", "Damage Mitigation",
            "If block/parry/dodge all miss, armor reduces the incoming hit:\n\n" +
            "   final = raw - (Defense + shrine buff + satiety/fatigue) / 3\n\n" +
            "Minimum 1 damage. Enemy crits add their CriticalHitDamage to the raw value before\n" +
            "mitigation. While in sword-skill post-motion you take +50% incoming damage. If the\n" +
            "attacking monster has Vampiric, it heals 25% of the damage dealt."),

        new("Combat & Rarity", "Heavy Attacks (Winding Up)",
            "Bosses (25% chance) and mobs at least 2 levels above you (15% chance) may telegraph\n" +
            "a heavy attack by shouting \"WINDING UP!\" for one turn. The release deals 1.8x normal\n" +
            "damage. Move at least 2 tiles away during that turn to make the attack whiff.\n\n" +
            "Charge mobs close 2-3 tiles in one hop and hit for 1.5x damage if they land the move."),

        new("Combat & Rarity", "Status: Bleed & Poison",
            "BLEED: mobs with CanBleed have a 30% chance on hit to open a wound. Ticks for\n" +
            "(1 + floor) damage per turn over 4 turns. Weapons with Bleed+N apply 3 turns on hit.\n\n" +
            "POISON: CanPoison mobs have a 35% chance; ticks (1 + floor) per turn over 5 turns.\n" +
            "Poison ignores food healing — only time or Antidote clears it.\n\n" +
            "HEMORRHAGE: If bleed and poison are active on you at once, they react into a burst\n" +
            "of (poisonDmg + bleedDmg) x 2 instant damage."),

        new("Combat & Rarity", "Status: Stun & Slow",
            "STUN: CanStun mobs (20% proc) take 1-2 of your turns. You can't cast sword skills.\n" +
            "Player skills like Break Time (30% proc) and Savage Fulcrum (20% proc) stun for 2 turns.\n\n" +
            "SLOW: CanSlow mobs (25% proc) lasts 3 turns, halves your dodge. Player skills like\n" +
            "Glacial Slash also apply Slow. Expires with a \"no longer slowed\" log line."),

        new("Combat & Rarity", "Rarity Tiers & Drop Rates",
            "Common 60% / Uncommon 25% / Rare 11% / Epic 4%. Legendary and Divine NEVER drop\n" +
            "from random rolls — they are hand-placed on specific bosses, field bosses, or quest\n" +
            "rewards.\n\n" +
            "Stat multipliers vs Common baseline: Uncommon 125% (+10 dur), Rare 160% (+25 dur),\n" +
            "Epic 200% (+50 dur). Value multipliers: 100/150/250/400%."),

        new("Combat & Rarity", "Rarity Colors & Glyphs",
            "  Common     Gray           (no tag)\n" +
            "  Uncommon   BrightGreen    [U]\n" +
            "  Rare       BrightCyan     [R]\n" +
            "  Epic       BrightMagenta  [E]\n" +
            "  Legendary  BrightYellow   [L]\n" +
            "  Divine     BrightRed      [◈]  — top tier\n\n" +
            "Sort-by-rarity goes Divine 6 -> Common 1. Log pickups show bracketed prefix."),

        new("Combat & Rarity", "Divine Objects",
            "Peak rarity — hand-placed, never random. 16 total: 7 canon Integrity Knight weapons\n" +
            "+ 9 Evolution Chain T4 apex weapons.\n\n" +
            "Divine weapons are UNBREAKABLE — durability never ticks down. Drops log with bespoke\n" +
            "BrightRed line: \"◈ {source} drops {name} — Divine Object.\"\n\n" +
            "Canon flavor also notes Divine weapons bypass enemy block rolls — this is a\n" +
            "forward-looking flag; currently Divines still roll normal block/parry math."),

        new("Combat & Rarity", "Sword Skills — Unlock & Use",
            "Each weapon type has its own skill tree. Skills unlock by kill thresholds:\n" +
            "0, 10, 25, 50, 75, 100, 150, 200, 300, 500 kills with that weapon.\n\n" +
            "Hollow Ingress modifier doubles all required kills. Newly unlocked skills auto-fill\n" +
            "your first empty skill slot. You can equip up to 4 skills at once (F1-F4 hotkeys).\n" +
            "F opens the skill menu."),

        new("Combat & Rarity", "Sword Skill Cooldown & Post-Motion",
            "Skill damage = (Attack + proficiency + buffs) x DamageMultiplier + flat SkillDamage\n" +
            "from Intelligence and gear. Multi-hit skills roll crit separately per hit.\n\n" +
            "COOLDOWN: Starburst Stream = 15 turns, The Eclipse = 30 turns, etc. SkillCooldown-N\n" +
            "reduces this.\n\n" +
            "POST-MOTION: you take +50% damage for 1-3 turns recovering. Bigger skills have\n" +
            "longer windows. PostMotion-N reduces it. Can't chain skills during post-motion."),

        new("Combat & Rarity", "Weapon Proficiency Ranks",
            "Kills with a weapon type grant cumulative flat damage + rank perks:\n\n" +
            "  Novice 10k/+1    Apprentice 25k/+2    Journeyman 50k/+4 (+2% crit)\n" +
            "  Expert 100k/+7   Master 200k/+11 (+3% parry)   Grandmaster 350k/+16\n" +
            "  Sword Saint 500k/+22 (+2% dodge)   Blade Dancer 750k/+29\n" +
            "  Weapon Lord 1000k/+37 (+3% crit)   Legendary 1500k/+46\n" +
            "  Mythic 2000k/+56 (+2% parry)   Transcendent 3000k/+67\n" +
            "  Divine Edge 4500k/+80   Aincrad's Chosen 6000k/+95\n" +
            "  The Black Swordsman 9999k/+120\n\n" +
            "(see: Damage Formula, Sword Skills — Unlock & Use)"),

        new("Combat & Rarity", "Kill Streaks",
            "Consecutive kills in quick succession trigger escalating banners:\n\n" +
            "  2 kills   Double Kill\n" +
            "  3 kills   Triple Kill\n" +
            "  4 kills   Multi Kill\n" +
            "  5 kills   RAMPAGE\n" +
            "  7 kills   UNSTOPPABLE\n" +
            "  10 kills  GODLIKE\n\n" +
            "Streak tiers contribute to per-mob Col yield (up to 3 tiers every 5 kills).\n" +
            "Taking any damage breaks the Perfect-Kill run, NOT the streak itself — moving,\n" +
            "resting, and missing swings all preserve the kill chain."),

        new("Combat & Rarity", "Hunger, Satiety & Fatigue",
            "SATIETY drains passively over time (1 point per N turns, varies by biome and\n" +
            "run modifiers). At Well Fed (>=80): +1 HP regen, +3 ATK/DEF.\n" +
            "At Hungry (<20): -2 ATK. At Starving (0): -5 HP/turn and +STARVING status.\n\n" +
            "Food items restore satiety AND HP over turns. Campfires restore +20 satiety.\n\n" +
            "FATIGUE ticks up from long rest/sprint cycles and heavy combat. Mild Fatigue:\n" +
            "-1 SPD. Heavy Fatigue: -3 SPD, -5% crit. Sleeping at campfires clears it.\n\n" +
            "The Iron Rank run modifier doubles satiety drain rate (the ultimate survivalist\n" +
            "challenge)."),

        new("Combat & Rarity", "Sprint & Stealth Move",
            "SPRINT (Shift + direction): moves 2 tiles in the direction pressed. Costs +50%\n" +
            "satiety drain for that turn and cannot cross Lava, Trap, or DangerZone tiles —\n" +
            "you'll stop short. Useful for repositioning out of post-motion or escaping\n" +
            "ambushes.\n\n" +
            "STEALTH MOVE (Ctrl + direction): moves 1 tile but mobs further than 5 tiles\n" +
            "away won't notice you. Halves your aggro range for the turn. Breaks on attack.\n\n" +
            "Neither sprint nor stealth triggers Extra Search's passive trap-reveal —\n" +
            "only normal step moves do."),

        new("Combat & Rarity", "Quick-Use Slots (1-5)",
            "Number keys 1-5 fire pre-bound consumables without opening inventory:\n\n" +
            "  1   Health Potion         (instant +50 HP)\n" +
            "  2   Greater Health Potion (+150 HP)\n" +
            "  3   Antidote              (cures Poison + Bleed)\n" +
            "  4   Battle Elixir         (+15 ATK, +10 SPD for 60 turns)\n" +
            "  5   Escape Rope           (warp to floor entrance)\n\n" +
            "Each consumes one from stock if available. Bindings are fixed. Anti-Crystal\n" +
            "Tyranny run modifier disables Crystal-based consumables (Revive, Teleport, etc.)."),

        new("Combat & Rarity", "Vision & FOV",
            "Your visible area (\"shadowcast FOV\") depends on:\n\n" +
            "  - Base radius:       20 tiles on bright day\n" +
            "  - Time of day:       shrinks to 8 tiles at deep night\n" +
            "  - Biome:             Darkness -20, Forest -8, Swamp -5, Desert -5\n" +
            "  - Weather:           Fog reduces trap detection, not FOV\n" +
            "  - Light sources:     campfires/shrines/vents emit warm bubbles\n" +
            "  - Starless Night:    run modifier locks vision at -20 permanently\n\n" +
            "Fog-of-war persists: previously seen tiles stay visible in dim tones but don't\n" +
            "update with mob movement until re-seen."),

        new("Combat & Rarity", "Look Mode & Counter Stance",
            "LOOK MODE (L): pauses the game and lets you move a cursor to inspect any tile,\n" +
            "monster, or item without moving the player. Shows name, HP, stats, status.\n" +
            "Press Tab/Shift+Tab to cycle between visible monsters. Esc / L again to exit.\n\n" +
            "COUNTER STANCE (V): skip your turn and set a counter flag — if a monster hits\n" +
            "you next turn, you automatically Parry (25% Attack counter-strike). Consumes 1\n" +
            "turn. Useful vs telegraphed Heavy Attacks. Stance ends after one incoming hit\n" +
            "or one turn, whichever comes first."),

        new("Combat & Rarity", "Death Penalty & Hardcore",
            "NORMAL DEATH: lose 25% Col on hand. XP toward next level is lost (but levels\n" +
            "aren't taken away). Equipment stays intact. Respawn at last save (floor start)\n" +
            "with full HP.\n\n" +
            "HARDCORE MODE: one life only. Death wipes the save slot permanently. Score\n" +
            "earned converts to a permanent leaderboard entry in your profile. Save cannot\n" +
            "be loaded after the run ends.\n\n" +
            "SAO CANON FLAVOR: Hardcore mirrors the novel's death-game rule. Normal mode\n" +
            "is a \"beta-tester-style\" run."),

        // ═══════════════════════════════════════════════════════════════
        // ── 2. Character Progression ───────────────────────────────────
        // ═══════════════════════════════════════════════════════════════

        new("Progression", "Experience & Leveling",
            "XP needed for next level = 100 + (100 x current_level).\n" +
            "So: Lv2 = 200, Lv3 = 300, Lv4 = 400, and so on linearly.\n\n" +
            "On level-up: +5 Skill Points, HP fully restored, and you're prompted to pick 1 of 3\n" +
            "random Passive Talent perks. Excess XP rolls over into the next level.\n" +
            "Max HP also scales with Vitality, so stat allocation matters as much as level."),

        new("Progression", "Starting Loadout",
            "Fresh character at Level 1 has:\n" +
            "  - 1000 Col, 10 Skill Points\n" +
            "  - Title: \"Adventurer\"\n" +
            "  - Iron Sword equipped, 1 Health Potion in inventory\n\n" +
            "Base stats start at 0 for Attack/Defense/Speed/SkillDamage. Base CritRate 5%,\n" +
            "Base CritHit damage 10. All 6 attributes start at 0 — your first 10 SP are yours\n" +
            "to distribute."),

        new("Progression", "The Six Attributes",
            "Spend Skill Points 1-for-1:\n\n" +
            "  VITALITY     +10 MaxHP + faster passive regen (1 + VIT/3 HP/turn)\n" +
            "  STRENGTH     +2 ATK per point\n" +
            "  ENDURANCE    +2 DEF per point\n" +
            "  DEXTERITY    +0.5% CritRate + 1 CritDamage; improves accuracy\n" +
            "  AGILITY      +2 SPD (turn priority) + improves dodge\n" +
            "  INTELLIGENCE +2 SkillDamage (boosts Sword Skill multipliers)"),

        new("Progression", "Derived Combat Stats",
            "Your character sheet shows five derived numbers:\n\n" +
            "  ATK = BaseAttack + (STR x 2) + gear bonuses\n" +
            "  DEF = BaseDefense + (END x 2) + gear bonuses\n" +
            "  SPD = BaseSpeed + (AGI x 2) + gear bonuses\n" +
            "  SD  = BaseSkillDamage + (INT x 2) + gear bonuses\n" +
            "  CRT = BaseCriticalRate + DEX/2  (percent)\n" +
            "  CD  = BaseCriticalHitDamage + DEX\n\n" +
            "Broken gear (0 durability) contributes nothing until repaired."),

        new("Progression", "Passive Talents (Level-Up Perks)",
            "Every level-up picks 3 random perks from 11. All are permanent and stack:\n\n" +
            "  Keen Edge (+3% Crit), Brutal Strikes (+5 CritDmg)\n" +
            "  Iron Will (+2 VIT), Power Surge (+3 BaseATK)\n" +
            "  Fortify (+3 BaseDEF), Quick Step (+2 BaseSPD)\n" +
            "  Brute Force (+2 STR), Endurance (+2 END)\n" +
            "  Nimble Fingers (+2 DEX), Fleet Foot (+2 AGI), Sharp Mind (+2 INT)\n\n" +
            "Pick what shores up your build."),

        new("Progression", "Unique Skill: Dual Blades",
            "Wield a second 1H Sword in OffHand. Unlocks at F74 Gleam Eyes story event OR\n" +
            "at 50 one-handed-sword kills (whichever first).\n\n" +
            "Effect: every main-hand swing fires a follow-up off-hand strike dealing\n" +
            "max(1, offhand.BaseDamage x 0.6 + prof/2). Plus +15% damage on single-hit skills.\n\n" +
            "Unlocks signature skills: Double Circular (2.0x), Starburst Stream (16-hit 8.0x),\n" +
            "The Eclipse (27-hit 12.0x)."),

        new("Progression", "Unique Skill: Holy Sword",
            "Heathcliff's stance. Active only when wielding a 1H Sword with a true SHIELD\n" +
            "(not a second sword) in OffHand.\n\n" +
            "Effect: +15% Block Chance. Unlocks Sacred Edge (7.0x overhead with brief damage\n" +
            "reduction) and Divine Cross (2-hit 4.5x).\n\n" +
            "Unlocks only by defeating Heathcliff on Floor 75 — no grind fallback."),

        new("Progression", "Unique Skill: Martial Arts",
            "Unarmed combat. While hands are empty: +10% damage, +20% CritRate. Unarmed sword\n" +
            "skill tree unlocks (Smash Knuckle at 0, Gazelle Rush at 10, Blazing Blow at 100,\n" +
            "Turbulence Rush at 200, Deadly Blow at 500 kills).\n\n" +
            "Unlocks at Ran the Brawler's F2 trial (Progressive canon) OR at 30 unarmed kills."),

        new("Progression", "Unique Skill: Katana Mastery",
            "Katana strikes gain +10% damage, +10% CritRate, and a 15% Bleed proc on hit.\n" +
            "Adds Tsumujigaeshi and Zekkuu to the Katana skill tree.\n\n" +
            "Unlocks at exactly 100 Katana kills."),

        new("Progression", "Unique Skill: Darkness Blade",
            "\"The Black Swordsman's\" stance. Active ONLY during Night phase.\n\n" +
            "Effect: +20% damage, +10% Dodge while Night is active. Unlocks Shadow Cleave\n" +
            "(3-hit rush 3.5x, range 3).\n\n" +
            "Unlock by killing any floor boss during Night."),

        new("Progression", "Unique Skill: Blazing & Frozen Edge",
            "Elemental attunements that work with any weapon.\n\n" +
            "BLAZING EDGE: +10% Burn proc on hit, +25% damage vs ice/frost/snow enemies,\n" +
            "unlocks Flame Strike (3.5x cone, 50% Burn). Unlocked by felling a Volcanic-biome\n" +
            "floor boss.\n\n" +
            "FROZEN EDGE: +10% Slow proc on hit, +25% damage vs fire/flame/lava enemies,\n" +
            "unlocks Glacial Slash (3.5x line, 50% Slow). Unlocked by felling an Ice-biome\n" +
            "floor boss."),

        new("Progression", "Unique Skill: Extra Skill — Search",
            "Argo's scouting art. Passively reveals hidden traps within a 3-tile radius around\n" +
            "your position every move.\n\n" +
            "Unlocks after disarming 10 traps total (counter persists across floors).\n" +
            "No active ability; purely an exploration aid."),

        new("Progression", "Floor Titles",
            "Your title auto-updates as you climb:\n\n" +
            "  F1   Adventurer         F25  Dungeon Scourge\n" +
            "  F2   Blooded             F35  Nightmare Walker\n" +
            "  F5   Survivor            F50  Floor Conqueror\n" +
            "  F10  Seasoned            F75  Clearance Hero\n" +
            "  F15  Proven              F100 Liberator of Aincrad\n\n" +
            "Each promotion announces: \"You have earned the title: <Title>!\""),

        // ═══════════════════════════════════════════════════════════════
        // ── 3. World & Exploration ─────────────────────────────────────
        // ═══════════════════════════════════════════════════════════════

        new("World", "Aincrad's 100 Floors & Eras",
            "100 stacked floors. Each floor has a unique named boss; clearing unlocks stairs up.\n" +
            "Floors grouped into thematic eras:\n\n" +
            "  F1-5   Verdant   (beasts, nature)\n" +
            "  F6-10  Stone     (constructs, puzzles)\n" +
            "  F11-15 Crimson   (fire, demons)\n" +
            "  F16-20 Crystal   (ice)\n" +
            "  F21-25 Twilight  (shadow, undead)\n" +
            "  F26-75 era pattern repeats\n" +
            "  F76-95 Hollow Fragment canon endgame\n" +
            "  F96-100 Ruby Palace — F100 is a mirror-clone of you."),

        new("World", "Day/Night Cycle",
            "A global clock advances every turn. Full cycle = 400 turns. Sun elevation follows\n" +
            "cosine: noon SunLevel=1.0, midnight 0.0. Phases: Dawn -> Day -> Dusk -> Night.\n\n" +
            "Vision radius scales with sun level — bright day gives full viewport, deep night\n" +
            "shrinks FOV to an 18-tile torch bubble. Ambient light tints cool moonlit blue at\n" +
            "night, warm off-white during day.\n\n" +
            "Darkness Blade unique skill activates only at Night. Starless Night run modifier\n" +
            "pins the cycle at 0 forever."),

        new("World", "Biomes",
            "Every floor has a BiomeType with passive effects:\n\n" +
            "  Grassland     safe\n" +
            "  Forest        -8 vision, ambush in tall grass\n" +
            "  Toxic Swamp   -5 vision, 8% step poison\n" +
            "  Desert        -5 vision, +1 thirst/turn\n" +
            "  Volcanic      2 dmg every 8 turns, +1 thirst\n" +
            "  Frozen (Ice)  12% slip, -2 ATK\n" +
            "  Aquatic       5% slip, -3 ATK\n" +
            "  Darkness      -20 vision (severe)\n" +
            "  Ancient Ruins trap/chest density bumped\n" +
            "  Settlement    vendors/NPCs common\n" +
            "  The Void      1 dmg every 10 turns, reality-warp flavor"),

        new("World", "Weather",
            "Rolled on floor entry:\n\n" +
            "  Clear  (40%) +1 passive HP regen\n" +
            "  Rainy  (25%) -3 crit for all, trap detect -10, +1 poison duration\n" +
            "  Foggy  (15%) trap detection -20 (traps almost invisible)\n" +
            "  Windy  (20%) +5 damage on thrown items\n\n" +
            "Weather only changes on floor ascend. Bounty bosses and status math respect\n" +
            "current weather."),

        new("World", "Labyrinth System",
            "Every floor is an overworld wilderness PLUS a separate labyrinth dungeon holding\n" +
            "the floor boss. The overworld has an archway tile (LabyrinthEntrance, cyan Pi\n" +
            "glyph) in a corner — usually visible on the minimap after exploration.\n\n" +
            "Stepping on it transports you into the labyrinth. The overworld state freezes.\n" +
            "Stepping on it again returns you. Field bosses NEVER spawn inside labyrinths.\n" +
            "To climb: kill the floor boss (in labyrinth), return to overworld, climb stairs up.\n" +
            "Stairs stay sealed until the boss is dead."),

        new("World", "Safe Rooms & Mechanics",
            "CAMPFIRE (&/*, orange)     Purges Poison/Bleed/Slow, heals 15+5*floor HP,\n" +
            "                            resets rest counter & fatigue. Cooking interaction.\n" +
            "FOUNTAIN (O, cyan)          One-time: heal 30+10*floor HP, +20 Satiety,\n" +
            "                            clears fatigue. Tile consumes on use.\n" +
            "SHRINE (cross, violet)      +3+floor ATK & DEF for 30 turns. One-shot.\n" +
            "PILLAR (|)                  Reveals 15-tile map radius on minimap. One-shot.\n" +
            "ANVIL (+, gold)             Opens smithing/repair/evolve UI.\n" +
            "BOUNTY BOARD (diamond)      100+50*floor Col bounty contracts."),

        new("World", "Lore, Journals & Enchant Shrines",
            "LORE STONE (diamond)        Grants 5x floor XP, marks entry in your Lore set.\n" +
            "                            One-shot. Triple-dot reveal message.\n" +
            "JOURNAL (=)                 Weathered diary page, in-world tip. 3x floor XP.\n" +
            "                            One-shot.\n" +
            "ENCHANT SHRINE (cross, gold) Rolls a random equipped piece, stamps +2 to random\n" +
            "                            stat (ATK/DEF/SPD). Bonus persists through unequip."),

        new("World", "Secret Shrines (T1 Chain Weapons)",
            "Rare magenta ! glyph on specific floors — one per chain:\n\n" +
            "  F5   Final Espada      (1H Sword)\n" +
            "  F8   Prima Sabre       (Rapier)\n" +
            "  F12  Moonstruck Saber  (Scimitar)\n" +
            "  F15  Heated Razor      (Dagger)\n" +
            "  F18  Matter Dissolver  (2H Sword)\n" +
            "  F22  Heart Piercer     (Spear)\n" +
            "  F28  Lunatic Press     (Mace)\n" +
            "  F32  Matamon           (Katana)\n" +
            "  F36  Bardiche          (Axe)\n\n" +
            "Walk onto the shrine to receive the T1 weapon. Feed it through the Anvil's\n" +
            "Evolve Weapon flow to push T1 -> T2 -> T3 -> T4 Divine."),

        new("World", "Traps & Hazards",
            "Traps start hidden. Dex gives up to 30% per-step detect chance + up to 65% confirm.\n" +
            "Weather modifies: Rain -10, Fog -20.\n\n" +
            "  Spike          3 + 2*floor damage, one-shot\n" +
            "  Poison Trap    2 + floor dmg/turn for 3 turns\n" +
            "  Teleport Trap  warps you to random walkable tile\n" +
            "  Alarm Trap     alerts every mob within 10 tiles\n" +
            "  Gas Vent       1 + floor/2 dmg/turn for 3 turns, repeats\n" +
            "  Lava (~)       4 + 2*floor dmg per step; can kill outright\n" +
            "  Danger Zone    1 + floor/5 dmg/step (guards monster dens)\n" +
            "  Tall Grass     hides mobs; ambush chance = 25 - Dex"),

        new("World", "Mechanical Tiles",
            "LEVER & PRESSURE PLATE      Each linked to a wall/door pair. Activating toggles\n" +
            "                             the linked wall into a door (or vice versa). Used for\n" +
            "                             dead-end puzzle loops.\n" +
            "CRACKED WALL (shaded)       Hidden passage — can be broken to reveal a safe room\n" +
            "                             with a chest.\n" +
            "CHEST (gold diamond)        Loot container; tier scales with floor.\n" +
            "STAIRS DOWN (<)             Never used (Aincrad climbs up only).\n" +
            "STAIRS UP (>)               Sealed until the floor boss is dead."),

        new("World", "Floor Boss Roster — Canon Highlights",
            "F1  Illfang the Kobold Lord          F50  The Six-Armed Buddha\n" +
            "F2  Asterius the Taurus King         F55  X'rphan the White Wyrm\n" +
            "F3  Nerius the Evil Treant           F60  The Armoured Stone Warrior\n" +
            "F4  Wythege the Hippocampus          F74  The Gleam Eyes\n" +
            "F5  Fuscus the Vacant Colossus       F75  The Skull Reaper\n" +
            "F6  The Irrational Cube              F98  Incarnation of the Radius\n" +
            "F10 Kagachi the Samurai Lord         F99  Heathcliff's Shadow\n" +
            "F22 The Witch of the West            F100 ??? (Your clone, 150% HP)\n" +
            "F25 The Two-Headed Giant\n" +
            "F27 The Four-Armed Giant             Stat curve: Level = 10 + 2*floor,\n" +
            "F35 Nicholas the Renegade            HP = 150 + 30*floor + 0.5*floor^2,\n" +
            "F46 The Ant Queen                    Col = 4000 + 500*floor + 20*floor^2."),

        new("World", "Field Bosses — Guaranteed Drops",
            "Roaming elites on overworld floors (not labyrinth). Each 100%-drops a named item.\n\n" +
            "  F2  Bullbous Bow -> Bullbous Horn       F40 Phoenix of Smolder Peak -> Flame Bow (D)\n" +
            "  F22 Forest King Stag -> Kingly Antler   F48 Frost Dragon -> Crystallite Ingot\n" +
            "  F35 Magnatherium -> Mammoth Tusk        F49 Nicholas Renegade (Christmas only)\n" +
            "  F40 Ogre Lord -> Ogre's Cleaver         F85 Silent Edge -> Black Lily Sword (D)\n" +
            "  F77-98 Hollow Fragment roster           F95 Warden of Stopped Hours -> Time Piercing (D)\n\n" +
            "(D) = Divine Object. Defeated field bosses don't respawn."),

        new("World", "Run Modifiers (12 Optional Challenges)",
            "Stacked toggles at run start, score-multiplied (cap x10):\n\n" +
            "EASY (x1.15)      Starless Night, Iron Rank\n" +
            "MODERATE (x1.25)  Beater, Solo, Laughing Coffin\n" +
            "HARD (x1.40)      Heathcliff's Gauntlet, Anti-Crystal Tyranny, Kayaba's Wager,\n" +
            "                   Hollow Ingress, Naked Ingress\n" +
            "NIGHTMARE (x1.75) Gleam Eyes Echo, Sword Art Only (pure Kirito run)\n\n" +
            "Canon unlock: first F100 clear. Currently always available (testing)."),

        new("World", "Seasonal Events",
            "Real-world date triggers in-world events:\n\n" +
            "  Christmas (Dec 20-26)  Nicholas the Renegade on F49 -> Divine Stone of\n" +
            "                          Returning Soul (canon LN Vol 2 reward)\n" +
            "  New Year (Jan 1-3)      hook present\n" +
            "  Valentine's (Feb 10-17) hook present\n" +
            "  White Day (Mar 14)      hook present\n" +
            "  Tanabata (Jul 7)        hook present\n" +
            "  Summer Festival         Jul 15 - Aug 31\n" +
            "  Tsukimi (Sep 15-18)     hook present\n" +
            "  Halloween (Oct 20-Nov3) hook present\n\n" +
            "Only Christmas drives a full encounter currently. Events check on floor entry."),

        new("World", "Ascending a Floor",
            "When you step on StairsUp after killing the boss:\n\n" +
            "  1. Floor Recap: Kills / Items / Damage / Turns / Exploration%\n" +
            "  2. Speed Clear bonus if elapsed <= par: 50 + 30*floor Col\n" +
            "  3. Thorough Exploration bonus if >=90%: +50*floor XP, +100*floor Col\n" +
            "  4. Achievements checked\n" +
            "  5. Floor++, auto-save writes\n" +
            "  6. Weather re-rolls, biome swaps\n" +
            "  7. Allies revive at 50% HP\n\n" +
            "Clearing F100 triggers GameWon. You are free."),

        // ═══════════════════════════════════════════════════════════════
        // ── 4. Items & Weapons ─────────────────────────────────────────
        // ═══════════════════════════════════════════════════════════════

        new("Items", "Weapon Types Overview",
            "13 active weapon classes:\n\n" +
            "  1H Sword      spd 1, rng 1, Attack/Str/Agi — balanced\n" +
            "  2H Sword      spd 3, rng 1, Str — heavy\n" +
            "  Rapier        spd 0, rng 1, Dex/Spd — precise thrust\n" +
            "  Dagger        spd 0, rng 1, Agi/Dex — backstab/bleed\n" +
            "  Katana        spd 0-1, rng 1, Agi/Spd — SAO signature\n" +
            "  Axe           spd 2, rng 1, Str/Vit — high damage\n" +
            "  Mace          spd 2, rng 1, Str/Vit — stun-heavy\n" +
            "  Spear         spd 1, rng 2, Dex/Str — reach thrusts\n" +
            "  Bow           spd 1-2, rng 2-4, Dex/Agi — ranged\n" +
            "  Scimitar      spd 2, rng 1, Dex/Agi — curved-slash bleed\n" +
            "  Claws         spd 3, rng 1, Agi/Dex — dual-fist flurry\n" +
            "  Scythe        spd 0, rng 2, Str — long-reach reaper\n" +
            "  Shield        OffHand — blocks + armor"),

        new("Items", "Material Tiers (Baseline)",
            "Every weapon class has a 5-tier mundane progression:\n\n" +
            "  Common Iron        Lv1    100 Col    +8 ATK\n" +
            "  Uncommon Steel     Lv10   250 Col    +15 ATK\n" +
            "  Rare Mythril       Lv25   800 Col    +25 ATK\n" +
            "  Epic Adamantite    Lv50   2500 Col   +45 ATK\n" +
            "  Legendary Celestial Lv75  6000 Col   +70 ATK\n\n" +
            "Stats, durability, and value roughly double each tier. Armor follows the same\n" +
            "Leather/Iron/Steel/Mythril/Adamantite/Celestial ladder."),

        new("Items", "Named Legendary Highlights",
            "Hand-placed beyond baseline:\n\n" +
            "  Elucidator     1H Sword, SkillCooldown-1         Kirito signature\n" +
            "  Dark Repulser  1H Sword, CritHeal+5              Kirito signature\n" +
            "  Remains Heart  1H Sword, SkillCooldown-1         Lisbeth masterwork\n" +
            "  Liberator      1H Sword, BlockChance+15          Heathcliff\n" +
            "  Lambent Light  Rapier                            Asuna\n" +
            "  Radiant Light  Rapier, CritRate+20               Asuna post-game\n" +
            "  Mother's Rosario Rapier, ComboBonus+50, 11-hit   Yuuki\n" +
            "  Karakurenai    Katana, BackstabDmg+50            Klein\n" +
            "  Kagenui        Katana                            Klein upgrade\n" +
            "  Argo's Claws   Dagger, BackstabDmg+30            Argo quest reward\n" +
            "  Mjolnir        Mace, StunChance+25               Divine apex"),

        new("Items", "Divine Object Set — Integrity Knights",
            "Night Sky Sword     Kirito        F99 Heathcliff's Shadow   ArmorPierce+30\n" +
            "Blue Rose Sword     Eugeo         F20 Absolut the Monarch   Freeze+20\n" +
            "Fragrant Olive Sword Alice        Selka's quest (F65)       HolyAoE+15, SD+15\n" +
            "Time Piercing Sword Bercouli      F95 Warden of Stopped Hrs ExecuteThreshold+25\n" +
            "Black Lily Sword    Sheyta        F85 The Silent Edge       SeveringStrike+50\n" +
            "Conflagrant Flame Bow Deusolbert  F40 Phoenix of Smolder P. Burn+30\n" +
            "Heaven-Piercing Blade Fanatio     Azariya's quest (F50)     PiercingBeam+30 Rng2"),

        new("Items", "Weapon Evolution Chains",
            "9 of 13 weapon types have a 4-tier chain. Each chain:\n\n" +
            "  T1 (Rare)        Found at Secret Shrine — free\n" +
            "  T2 (Epic)        Craft: 3 chain catalyst at Anvil Evolve\n" +
            "  T3 (Legendary)   Craft: 8 chain catalyst\n" +
            "  T4 (Divine)      Craft: 20 chain catalyst + 1 rare peak material\n\n" +
            "T4 weapons are Divine tier: unbreakable, ◈ glyph, peak stats.\n" +
            "Enhancement level (+N) is preserved through evolution.\n" +
            "Old weapons are kept in backpack after evolving (not destroyed)."),

        new("Items", "Evolution Chain Table",
            "Weapon    T1               T2               T3               T4 (Divine)\n" +
            "-----------------------------------------------------------------------\n" +
            "1H Sword  Final Espada     Asmodeus         Final Avalanche  Tyrfing\n" +
            "Rapier    Prima Sabre      Pentagramme      Charadrios       Hexagramme\n" +
            "Scimitar  Moonstruck Saber Diablo Esperanza Iblis            Satanachia\n" +
            "Dagger    Heated Razor     Valkyrie         Misericorde      Iron Maiden\n" +
            "Mace      Lunatic Press    Nemesis          Yggdrasil        Mjolnir\n" +
            "Katana    Matamon          Shishi-Otoshi    Shichishito      Masamune\n" +
            "2H Sword  Matter Dissolver Titan's Blade    Ifrit            Ascalon\n" +
            "Axe       Bardiche         Archaic Murder   Nidhogg's Fang   Ouroboros\n" +
            "Spear     Heart Piercer    Trishula         Vijaya           Caladbolg"),

        new("Items", "Chain Catalysts — by Weapon Type",
            "  1H Sword  Demonic Sigil      (humanoid mobs ~3%)\n" +
            "  Rapier    Geometric Shard    (construct mobs)\n" +
            "  Scimitar  Infernal Gem       (dragon mobs)\n" +
            "  Dagger    Valkyrie Feather   (insect mobs)\n" +
            "  Mace      Lunar Core         (undead mobs)\n" +
            "  Katana    Oni Ash            (humanoid mobs)\n" +
            "  2H Sword  Titan Fragment     (construct mobs)\n" +
            "  Axe       Nidhogg Scale      (dragon mobs)\n" +
            "  Spear     Trishula Tip       (elemental mobs)\n\n" +
            "Rare Peak materials (T3->T4): Dragon Scale, Crystallite Ingot, Bat Wing, Flame Core,\n" +
            "Venom Gland, Mithril Trace, Ectoplasm — from high-tier boss drops."),

        new("Items", "Anvil — Repair, Enhance, Evolve",
            "REPAIR ALL\n" +
            "  Cost: 50 + 25*floor Col. Restores all equipped gear to cap:\n" +
            "  50 + 10*floor + 5*enhancementLevel durability.\n\n" +
            "ENHANCE (+0 to +10)\n" +
            "  Cost per attempt: (level+1)*100 + 50*floor Col, plus 1 + level/3 materials.\n" +
            "  Weapons: +3 ATK per level. Armor: +2 DEF. Accessories: +1 primary.\n" +
            "  Success rates: +1 95% -> +5 60% -> +7 40% -> +10 10%.\n" +
            "  From +7 up, failed attempts have 30% chance to DOWNGRADE by 1 (SAO canon).\n\n" +
            "EVOLVE WEAPON\n" +
            "  Requires an equipped Chain weapon. Consumes catalysts, confirms with preview,\n" +
            "  preserves enhancement level. Swaps weapon + keeps old in backpack."),

        new("Items", "Equipment Slots & Dual Wield",
            "Ten slots: Weapon, Head, Chest, Legs, Feet, RightRing, LeftRing, Bracelet, Necklace,\n" +
            "OffHand.\n\n" +
            "Rings: 2 simultaneously. Necklaces/Bracelets: 1 each.\n\n" +
            "SHIELDS (OffHand): Wooden 10% block, Iron 18% block. Successful block fully negates\n" +
            "the hit and degrades the shield.\n\n" +
            "DUAL WIELD: 1H Swords become legal in OffHand ONLY after unlocking Dual Blades.\n" +
            "Otherwise OffHand accepts shields only. 2H Swords/Bows/Scythes block OffHand entirely."),

        new("Items", "Food & Cooking",
            "HP regen over a turn count. Highlights:\n\n" +
            "  Bread              2 HP/turn x 10     bakery\n" +
            "  Grilled Meat       5 x 15              common\n" +
            "  Fish Stew          8 x 8               F3+\n" +
            "  Honey Bread        mid\n" +
            "  Elven Waybread     3 x 30              F4+\n" +
            "  Cream-Filled Bread 3 x 10              Tolbana 5 Col staple\n" +
            "  Asuna's Sandwich   5 x 15              F74 field lunch canon\n" +
            "  Gingerbread Cookies 3 x 20             Christmas event\n" +
            "  Ragout Rabbit Stew 20 x 40 turns       F35 LN Vol 1 canon. 3000 Col. Max 1.\n\n" +
            "Drinks (Ale, Herbal Tea, Warm Milk, etc.) grant smaller, longer regens."),

        new("Items", "Potions, Crystals & Throwables",
            "POTIONS  Health Potion (+50), Greater (+150), Antidote (cures poison/bleed),\n" +
            "          Battle Elixir (+15 ATK/+10 SPD 60 turns), Speed Potion (+10 SPD 30t),\n" +
            "          Iron Skin Potion (+10 DEF 30t), Escape Rope, Revive Crystal (auto).\n\n" +
            "CRYSTALS (SAO-iconic, voice-commanded):\n" +
            "          Teleport Crystal (warp to named city), Corridor Crystal (60s portal),\n" +
            "          Anti-Crystal (suppresses teleports — Laughing Coffin tool),\n" +
            "          Healing Crystal (+100), High Healing Crystal (+300), Antidote Crystal,\n" +
            "          Paralysis Cure Crystal, Mirage Sphere (records combat),\n" +
            "          Pneuma Flower (revives ally within 10 turns, Legendary),\n" +
            "          Divine Stone of Returning Soul (revives within 10s — Nicholas F49 drop).\n\n" +
            "THROWABLES  Fire Bomb (30 fire, 3-rad), Poison Vial (10+poison),\n" +
            "             Smoke Bomb (blinds), Flash Bomb (5+stun)."),

        new("Items", "Accessories",
            "Six standard rings/necklaces:\n\n" +
            "  Ring of Strength    +10 STR / +5 ATK       max 2 equipped\n" +
            "  Amulet of Agility   +12 AGI / +5 SPD\n" +
            "  Guardian Ring       +8 DEF / +5 VIT\n" +
            "  Scholar's Pendant   +10 INT / +5 SkillDmg\n" +
            "  Swift Band          +8 SPD / +6 AGI\n" +
            "  Vitality Charm      +8 VIT"),

        // ═══════════════════════════════════════════════════════════════
        // ── 5. Quests, NPCs & Economy ──────────────────────────────────
        // ═══════════════════════════════════════════════════════════════

        new("Quests & NPCs", "Quest Types & Rewards",
            "Four types:\n" +
            "  KILL     Defeat N mobs (optionally gated on weapon type)\n" +
            "  COLLECT  Gather N named items from mobs\n" +
            "  EXPLORE  Discover 40-60% of the floor\n" +
            "  DELIVER  Bring a package to any NPC\n\n" +
            "Reward formula (scales with floor):\n" +
            "  Kill:     100 + floor*50 + count*20 Col / 50 + floor*30 + count*10 XP\n" +
            "  Collect:  80 + floor*40 + count*30 Col / 40 + floor*25 + count*15 XP\n" +
            "  Explore:  150 + floor*60 Col / 80 + floor*35 XP\n" +
            "  Deliver:  120 + floor*45 Col / 60 + floor*30 XP\n\n" +
            "Max 5 active quests. Non-persistent quests clear on floor change."),

        new("Quests & NPCs", "Accepting & Completing Quests",
            "Bump any NPC. 1-in-3 chance they offer a random floor-appropriate quest (unless\n" +
            "they're a canonical Divine-quest giver who never layers random quests).\n\n" +
            "Kill/Collect/Explore auto-track as you play. To turn in: talk to ANY NPC after the\n" +
            "[QUEST] complete! message appears — all completed quests resolve in one pop.\n\n" +
            "Deliver quests complete the instant you talk to any NPC after getting the package.\n" +
            "Weapon-gated kills only count when that weapon type is equipped."),

        new("Quests & NPCs", "Ran the Brawler (F2)",
            "Bump Ran the Brawler on Floor 2 (green 'R'). Offers Ran's Trial:\n" +
            "Defeat 5 beasts on F2 using Unarmed (no weapon).\n\n" +
            "Return after completion. Unlocks Martial Arts Unique Skill + 200 Col + 150 XP.\n\n" +
            "Alternative: Martial Arts auto-unlocks at 30 unarmed kills. The trial banner is\n" +
            "guarded against double-firing if you already milestoned the skill.\n\n" +
            "Dialogue quotes SAO Progressive canon."),

        new("Quests & NPCs", "Sister Azariya (F50)",
            "F50 Divine Object giver (cyan 'A'). Quest: \"Light at the Edge of Sight\" —\n" +
            "slay 20 monsters on Floor 50.\n\n" +
            "Return with quest complete; she hands over the Heaven-Piercing Blade (Divine) plus\n" +
            "500 Col + 400 XP. Auto-added to inventory, or dropped at your feet if full.\n\n" +
            "Canon: former Fanatio apprentice who left the Integrity Knight order."),

        new("Quests & NPCs", "Selka the Novice (F65)",
            "F65 Divine Object giver (white 'S'). Quest: \"The Last Knight's Bequest\" —\n" +
            "slay 25 monsters on Floor 65.\n\n" +
            "Return to receive Alice's blade — Fragrant Olive Sword (Divine) plus\n" +
            "500 Col + 400 XP.\n\n" +
            "Canon: Alice's younger sister, keeps the blade until a worthy wielder proves\n" +
            "themselves."),

        new("Quests & NPCs", "Hollow Fragment HNM Questgivers (F79-F98)",
            "Nine canonical HNM weapon NPCs, all kill-count-on-this-floor quests:\n\n" +
            "  F79 Scholar Ellroy         15 kills  Infinite Ouroboros      400/300\n" +
            "  F80 Hunter Kojiro          15        Jato Onikirimaru        400/300\n" +
            "  F81 Ranger Torva           15        Fiendblade Deathbringer 450/320\n" +
            "  F83 Apiarist Nell          15        Fayblade Tizona         450/320\n" +
            "  F88 Watcher Kael           20        Starmace Elysium        600/450\n" +
            "  F90 High Priestess Sola    20        Eurynome's Holy Sword   650/480\n" +
            "  F91 Torchbearer Meir       20        Saintspear Rhongomyniad 700/520\n" +
            "  F95 Elder Beastkeeper      25        Shinto Ama-no-Murakumo  800/600\n" +
            "  F98 Sentinel Captain       25        Godspear Gungnir        800/600"),

        new("Quests & NPCs", "Town of Beginnings NPCs (F1)",
            "Floor 1 is a hand-built hub with fixed NPCs:\n\n" +
            "  Agil            Vendor, \"Agil's General Store\"\n" +
            "  Klein           Tutorial dialogue on combat/progression/survival\n" +
            "  Argo the Rat    Information broker; tips on bosses/proficiency/death\n" +
            "  Priest Tadashi  Flavor\n" +
            "  Nezha           Smith; points to anvil\n" +
            "  Lisbeth         Short canon dialogue\n" +
            "  Silica          Short canon dialogue\n" +
            "  Diavel          Short canon dialogue\n\n" +
            "Klein also appears on F2-F3; Argo on F3+ as wandering NPCs."),

        new("Quests & NPCs", "SAO Switch (Party)",
            "Bump into a friendly ally to swap positions with them — this is the canonical\n" +
            "SAO Switch move. Useful for:\n\n" +
            "  - Pulling a wounded ally out of the front line\n" +
            "  - Putting a tank (Agil, Strea) into the path of a heavy attack\n" +
            "  - Repositioning around narrow corridors\n\n" +
            "No action cost — swap is free. Ally keeps their current status effects,\n" +
            "cooldowns, and facing. Party size caps at 2 allies.\n\n" +
            "(see: Recruitable Allies & Party System)"),

        new("Quests & NPCs", "Recruitable Allies & Party System",
            "Max 2 allies. Talking to Klein, Asuna, Agil, Silica, or Lisbeth outside F1 prompts\n" +
            "recruit dialog.\n\n" +
            "  Klein     Katana, Samurai\n" +
            "  Asuna     Rapier, The Flash\n" +
            "  Agil      Axe\n" +
            "  Silica    Dagger, Dragon Tamer\n" +
            "  Lisbeth   Mace, Blacksmith\n\n" +
            "Ally stats scale to playerLevel-1 with 80 + playerLevel*8 HP. Behaviors: Aggressive,\n" +
            "Defensive, Follow. Bump an ally to perform an SAO Switch (position swap). KO'd allies\n" +
            "auto-revive at 50% HP on floor change.\n\n" +
            "The Solo run modifier forbids recruits."),

        new("Quests & NPCs", "Vendors — Rotating Stock",
            "Every non-town floor spawns 1 vendor (green 'V'). Shop name scales:\n" +
            "\"General Store\" (F<=5), \"Adventurer's Supply\" (F<=10), \"Elite Outfitters\" (F>10).\n\n" +
            "Always: 2x Health Potion, Antidote, Bread, Grilled Meat\n" +
            "F2+    Greater Health Potion, Honey Bread, Spiced Jerky, Fire Bomb\n" +
            "F3+    Fish Stew, Speed Potion, Iron Skin Potion, Smoke Bomb, Poison Vial, Escape Rope\n" +
            "F4+    Elven Waybread, Flash Bomb, Revive Crystal\n" +
            "F5+    1 random accessory\n\n" +
            "Plus 3-4 random floor-scaled weapons, 1-2 armors. All prices marked up +20%."),

        new("Quests & NPCs", "Col Economy — How You Earn",
            "  Mob kills       ColYield + Perfect Kill +50% (no damage taken) + streak tier\n" +
            "                   (up to 3 tiers every 5 kills)\n" +
            "  Floor boss      ColYield + guaranteed drop\n" +
            "  Chests          20 + 15*floor + 0-19 Col. x1.5 for \"far\" chests.\n" +
            "  Speed clear     50 + 30*floor Col if elapsed <= par\n" +
            "                   Par: {200,220,250,280,320,360,400,450,500,550} capped at F10 value\n" +
            "  Exploration     +50*floor XP, +100*floor Col if >=90% explored on ascend\n" +
            "  Quests          Per quest type formula\n" +
            "  Achievements    50-10000 Col per milestone\n" +
            "  Bounty          100 + 50*floor Col\n\n" +
            "DEATH PENALTY: lose 25% Col on non-Hardcore death. Hardcore wipes the save."),

        new("Quests & NPCs", "Achievements",
            "  First Blood         first monster kill           50\n" +
            "  Boss Slayer         first floor boss            200\n" +
            "  Centurion           100 monsters                500\n" +
            "  Walking Calamity    500 monsters               2500\n" +
            "  Untouchable         5-kill perfect streak       300\n" +
            "  Ascending           reach F5                    150\n" +
            "  Into the Unknown    reach F10                   300\n" +
            "  Veteran Climber     reach F25                  1000\n" +
            "  Halfway There       reach F50                  3000\n" +
            "  Apex of Aincrad     reach F100                10000\n" +
            "  Speed Demon         speed-clear a floor         200\n" +
            "  Loremaster          all lore stones discovered 1500\n" +
            "  Bounty Hunter       complete a bounty           250\n" +
            "  Iron Will           survive a hemorrhage        400"),

        new("Quests & NPCs", "Save System",
            "3 save slots in %LOCALAPPDATA%/AincradTRPG/save_N.json. Legacy save.json auto-\n" +
            "migrates to slot 1. Auto-save fires on every floor ascend.\n\n" +
            "Saved state: player stats/inv/equipment, kills/turns, active+completed quests,\n" +
            "achievements, unique skills, party members (with behaviors), tutorial tips seen,\n" +
            "discovered lore, faction reputation, defeated field bosses, ACTIVE RUN MODIFIERS,\n" +
            "equipped skills + cooldowns, story flags/events.\n\n" +
            "Slot summary shows name, level, floor, difficulty, hardcore flag, timestamp, playtime.\n" +
            "HARDCORE mode deletes the save on death."),
    };
}
