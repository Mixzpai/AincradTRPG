namespace SAOTRPG.UI.Dialogs;

// Static content for the in-game Player Guide (hotkey B).
// Organized as a flat list of (Category, Title, Body) entries.
// The dialog renders the category headers inline and shows one body at a time.
// Compiled from 5 parallel research agents 2026-04-17 — foundation pass,
// iterate content as gameplay evolves.
public static class PlayerGuideContent
{
    // Tags enable polyhierarchy — a topic appears under its primary Category
    // plus a virtual "Tag: <name>" root for each tag (see PlayerGuideDialog).
    // Default [] keeps existing call sites valid; content agents 3/4 can tag
    // during the content-rewrite pass. Use `with { Tags = [...] }` to add.
    public record GuideEntry(string Category, string Title, string Body)
    {
        public string[] Tags { get; init; } = System.Array.Empty<string>();
    }

    public static readonly GuideEntry[] Entries =
    {
        // ═══════════════════════════════════════════════════════════════
        // ── 1. Combat & Rarity ─────────────────────────────────────────
        // ═══════════════════════════════════════════════════════════════

        new("Combat & Rarity", "Damage Formula",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Damage Formula\n" +
            "│ Applies to: Every melee/ranged swing\n" +
            "│ Inputs: Attack, proficiency, combo, buffs\n" +
            "│ Trigger: Any successful hit\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Final damage sums your Attack, weapon proficiency bonus, combo\n" +
            "bonus, shrine buff, level-up surge, satiety/fatigue, and biome\n" +
            "modifier — then scales by unique-skill multipliers.\n\n" +
            "USAGE\n" +
            "Applies automatically on every swing. The log echoes the final\n" +
            "number; backstabs from unseen enemies double it.\n\n" +
            "EFFECTS\n" +
            "Unique skills like Holy Sword, Martial Arts, and elemental\n" +
            "affinity multiply the summed total. The 5th hit of a combo is a\n" +
            "Combo Finisher and doubles total damage.\n\n" +
            "COSTS\n" +
            "Each swing ticks 1 durability off your weapon. You'll see an\n" +
            "\"about to break\" warning once durability drops to 5.\n\n" +
            "TIPS\n" +
            "Stack inputs that multiply (unique skills) before ones that add\n" +
            "(gear). Keep proficiency high — it feeds both the sum and the\n" +
            "rank bonuses.\n\n" +
            "SEE ALSO\n" +
            "[Critical Hits] · [Combo Attacks] · [Weapon Proficiency Ranks]")
        {
            Tags = new[] { "combat", "stats" }
        },

        new("Combat & Rarity", "Critical Hits",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Critical Hits\n" +
            "│ Base rate: 5% + Dex/2\n" +
            "│ Bonus damage: 10 + Dex\n" +
            "│ Trigger: Per-hit roll\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Crits flash \"CRIT!\" in BrightRed and add flat bonus damage on\n" +
            "top of the swing. Base rate is 5% plus +1% per 2 points of\n" +
            "Dexterity.\n\n" +
            "USAGE\n" +
            "The roll is automatic per hit. Multi-hit sword skills roll crit\n" +
            "separately on every hit.\n\n" +
            "EFFECTS\n" +
            "Weather can shift crit rate up or down. The Proficiency Tree\n" +
            "offers +2% crit at the L25 fork and +3% more at the L50 fork\n" +
            "if you pick the crit branches. Weapons like Lambent Light and\n" +
            "Masamune add flat CritRate. CritHeal restores a percent of\n" +
            "damage on crit.\n\n" +
            "COSTS\n" +
            "None beyond normal swing costs.\n\n" +
            "TIPS\n" +
            "Stack Dex for both rate and bonus damage — it scales both sides\n" +
            "at once. Pair Dex investment with crit-flagged weapons for\n" +
            "compounding returns.\n\n" +
            "SEE ALSO\n" +
            "[Damage Formula] · [The Six Attributes] · [Weapon Proficiency Ranks]")
        {
            Tags = new[] { "combat", "crit", "stats" }
        },

        new("Combat & Rarity", "Combo Attacks",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Combo Attacks\n" +
            "│ Stack: Same target, consecutive turns\n" +
            "│ Bonus: +2 damage per stack past 1\n" +
            "│ Finisher: 5th hit doubles damage\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Hitting the same target on consecutive turns stacks a combo\n" +
            "counter. Each stack beyond the first adds +2 flat damage, and\n" +
            "the 5th consecutive hit is a Combo Finisher.\n\n" +
            "USAGE\n" +
            "Keep swinging the same enemy turn after turn. The log labels\n" +
            "stacks 2/3/4 as Double/Triple/Quad Strike.\n\n" +
            "EFFECTS\n" +
            "Combo Finisher DOUBLES your total damage on that hit and\n" +
            "resets the counter. Weapons with ComboBonus+N scale the combo\n" +
            "bonus by N percent.\n\n" +
            "COSTS\n" +
            "Switching targets resets the combo immediately.\n\n" +
            "TIPS\n" +
            "Plan target focus around the 5-hit cadence — finishing on a\n" +
            "high-HP enemy wastes far less damage than on a near-dead mob.\n\n" +
            "SEE ALSO\n" +
            "[Damage Formula] · [Critical Hits] · [Kill Streaks]")
        {
            Tags = new[] { "combat", "stats" }
        },

        new("Combat & Rarity", "Defense — Block, Parry, Dodge",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Defense — Block, Parry, Dodge\n" +
            "│ Order: Block, then Parry, then Dodge\n" +
            "│ Stats: Shield, Dex, Agi, proficiency\n" +
            "│ Trigger: Incoming monster hit\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "When a monster swings at you, three saves roll in order. Any\n" +
            "that succeeds cancels the hit before armor mitigation.\n\n" +
            "USAGE\n" +
            "Automatic on each incoming attack. Counter Stance forces an\n" +
            "auto-Parry on your next incoming hit.\n\n" +
            "EFFECTS\n" +
            "BLOCK: Shield BlockChance plus weapon BlockChance SpecialEffect\n" +
            "— full negation, and the shield degrades on success.\n" +
            "PARRY: min(Dex, 15)% plus proficiency plus weapon ParryChance;\n" +
            "counter-strikes for 25% of your Attack.\n" +
            "DODGE: min(Agi x 2, 20)% plus proficiency. Slow halves dodge,\n" +
            "Naked Ingress grants +25%. Three consecutive dodges trigger\n" +
            "\"Matrix mode!\"; five trigger \"Untouchable!\".\n\n" +
            "COSTS\n" +
            "Successful blocks eat shield durability.\n\n" +
            "TIPS\n" +
            "Pick one lane and invest — Agi for dodge, Dex for parry, or a\n" +
            "heavy shield for block. Split investment leaves every save\n" +
            "mediocre.\n\n" +
            "SEE ALSO\n" +
            "[Damage Mitigation] · [Look Mode & Counter Stance] · [Equipment Slots & Dual Wield]")
        {
            Tags = new[] { "combat", "stats" }
        },

        new("Combat & Rarity", "Damage Mitigation",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Damage Mitigation\n" +
            "│ Formula: raw - (DEF + buffs + food/fatigue)/3\n" +
            "│ Floor: Minimum 1 damage\n" +
            "│ Trigger: After block/parry/dodge miss\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "If all three defensive rolls miss, armor absorbs part of the\n" +
            "incoming hit. The remaining damage passes to your HP.\n\n" +
            "USAGE\n" +
            "Mitigation runs automatically after Defense rolls fail. The\n" +
            "final number shown in the log is post-mitigation.\n\n" +
            "EFFECTS\n" +
            "Enemy crits add their CriticalHitDamage to the raw value before\n" +
            "mitigation. While in sword-skill post-motion you take +50%\n" +
            "incoming damage. If the attacker has Vampiric, it heals 25% of\n" +
            "the damage dealt.\n\n" +
            "COSTS\n" +
            "None beyond HP loss.\n\n" +
            "TIPS\n" +
            "Stack Defense, shrine buffs, and Well-Fed satiety together —\n" +
            "they all feed the same mitigation bracket. Avoid acting inside\n" +
            "post-motion when mobs are queued on you.\n\n" +
            "SEE ALSO\n" +
            "[Defense — Block, Parry, Dodge] · [Sword Skill Cooldown & Post-Motion] · [Hunger, Satiety & Fatigue]")
        {
            Tags = new[] { "combat", "stats" }
        },

        new("Combat & Rarity", "Heavy Attacks (Winding Up)",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Heavy Attacks (Winding Up)\n" +
            "│ Bosses: 25% chance to telegraph\n" +
            "│ High-level mobs: 15% chance\n" +
            "│ Release: 1.8x normal damage\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Bosses and mobs at least 2 levels above you may shout \"WINDING\n" +
            "UP!\" for one turn before unleashing a 1.8x heavy attack. Charge\n" +
            "mobs are a separate threat that close 2-3 tiles in one hop.\n\n" +
            "USAGE\n" +
            "Watch the log. The moment you see the telegraph, move at least\n" +
            "2 tiles away before the release resolves.\n\n" +
            "EFFECTS\n" +
            "Landing the heavy deals 1.8x normal damage. Charge mobs that\n" +
            "close and connect hit for 1.5x damage.\n\n" +
            "COSTS\n" +
            "Spending the turn moving away forfeits your own attack.\n\n" +
            "TIPS\n" +
            "Counter Stance trades the attack back with a Parry if you can't\n" +
            "reposition. Keep open ground behind you during boss fights so\n" +
            "the whiff-move is always available.\n\n" +
            "SEE ALSO\n" +
            "[Look Mode & Counter Stance] · [Sprint & Stealth Move] · [Floor Boss Roster — Canon Highlights]")
        {
            Tags = new[] { "combat", "bosses" }
        },

        new("Combat & Rarity", "Status: Bleed & Poison",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Status: Bleed & Poison\n" +
            "│ Bleed: 30% proc, 4 turns, 1+floor/turn\n" +
            "│ Poison: 35% proc, 5 turns, 1+floor/turn\n" +
            "│ Trigger: Mob CanBleed / CanPoison flag\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Bleed and Poison are damage-over-time statuses. Running both\n" +
            "simultaneously triggers Hemorrhage, a burst combining the two.\n\n" +
            "USAGE\n" +
            "Procs automatically when a qualifying mob hits you. Weapons\n" +
            "with Bleed+N let you apply 3 turns of bleed on your own hits.\n\n" +
            "EFFECTS\n" +
            "Bleed ticks (1 + floor) per turn over 4 turns. Poison ticks\n" +
            "(1 + floor) per turn over 5 turns and ignores food healing.\n" +
            "Hemorrhage fires (poisonDmg + bleedDmg) x 2 as instant damage.\n\n" +
            "COSTS\n" +
            "Poison only clears with time or an Antidote — food won't help.\n\n" +
            "TIPS\n" +
            "Keep Antidotes in quick-use slot 3. Never let both statuses\n" +
            "overlap on yourself — clear Bleed with food before Poison\n" +
            "lands, or spend the Antidote early.\n\n" +
            "SEE ALSO\n" +
            "[Status: Stun & Slow] · [Quick-Use Slots (1-5)] · [Potions, Crystals & Throwables]")
        {
            Tags = new[] { "combat", "status" }
        },

        new("Combat & Rarity", "Status: Stun & Slow",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Status: Stun & Slow\n" +
            "│ Stun proc: 20% mob / 20-30% skill\n" +
            "│ Slow proc: 25% mob\n" +
            "│ Trigger: CanStun / CanSlow flag\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Stun freezes your action economy; Slow cripples your evasion.\n" +
            "Both statuses land from mob hits and qualifying player skills.\n\n" +
            "USAGE\n" +
            "Procs on hit. Break Time (30% proc) and Savage Fulcrum (20%\n" +
            "proc) let you stun enemies for 2 turns. Glacial Slash applies\n" +
            "Slow.\n\n" +
            "EFFECTS\n" +
            "STUN costs you 1-2 turns and blocks sword-skill casting.\n" +
            "SLOW lasts 3 turns and halves your dodge chance. Slow expires\n" +
            "with a \"no longer slowed\" log line.\n\n" +
            "COSTS\n" +
            "Stun forfeits turns outright; Slow mainly taxes dodge-built\n" +
            "characters.\n\n" +
            "TIPS\n" +
            "Dodge builds should carry a Battle Elixir or repositioning\n" +
            "option to ride out Slow windows. Stack stun skills against\n" +
            "Winding-Up bosses to pre-empt the heavy release.\n\n" +
            "SEE ALSO\n" +
            "[Status: Bleed & Poison] · [Defense — Block, Parry, Dodge] · [Sword Skills — Unlock & Use]")
        {
            Tags = new[] { "combat", "skills" }
        },

        new("Combat & Rarity", "Rarity Tiers & Drop Rates",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Rarity Tiers & Drop Rates\n" +
            "│ Drops: 60/25/11/4% Com/Un/Rare/Epic\n" +
            "│ Legendary/Divine: Never random\n" +
            "│ Trigger: Any item roll\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Random loot rolls across four tiers. Legendary and Divine are\n" +
            "hand-placed only — you never see them from a random drop.\n\n" +
            "USAGE\n" +
            "Applies to standard loot tables. Legendary/Divine come from\n" +
            "specific bosses, field bosses, or quest rewards.\n\n" +
            "EFFECTS\n" +
            "Stat multipliers vs the Common baseline: Uncommon 125% (+10\n" +
            "dur), Rare 160% (+25 dur), Epic 200% (+50 dur). Value\n" +
            "multipliers: 100/150/250/400%.\n\n" +
            "COSTS\n" +
            "None — drop rates are inherent.\n\n" +
            "TIPS\n" +
            "Don't grind random tables for Legendaries — target the named\n" +
            "sources. For raw stats per Col spent, Epic drops from early\n" +
            "floors out-scale baseline Rare gear on later floors.\n\n" +
            "SEE ALSO\n" +
            "[Rarity Colors & Glyphs] · [Divine Objects] · [Named Legendary Highlights] · [Hollow Area Uniques]")
        {
            Tags = new[] { "rarity", "weapons" }
        },

        new("Combat & Rarity", "Rarity Colors & Glyphs",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Rarity Colors & Glyphs\n" +
            "│ Range: Common 1 -> Divine 6\n" +
            "│ Display: Log prefix + inventory tint\n" +
            "│ Trigger: Any tiered item\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Every rarity tier has a distinct color and bracket glyph for\n" +
            "log pickups and inventory sort.\n\n" +
            "USAGE\n" +
            "Sort-by-rarity lists Divine (6) down to Common (1). Log lines\n" +
            "for pickups carry the bracketed prefix shown below.\n\n" +
            "EFFECTS\n" +
            "  Common     Gray           (no tag)\n" +
            "  Uncommon   BrightGreen    [U]\n" +
            "  Rare       BrightCyan     [R]\n" +
            "  Epic       BrightMagenta  [E]\n" +
            "  Legendary  BrightYellow   [L]\n" +
            "  Divine     BrightRed      [diamond]  top tier\n\n" +
            "COSTS\n" +
            "None.\n\n" +
            "TIPS\n" +
            "Scan the log color, not the text — BrightRed with the diamond\n" +
            "glyph is always a Divine drop worth stopping to look at.\n\n" +
            "SEE ALSO\n" +
            "[Rarity Tiers & Drop Rates] · [Divine Objects] · [Divine Object Set — Integrity Knights]")
        {
            Tags = new[] { "rarity", "weapons" }
        },

        new("Combat & Rarity", "Divine Objects",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Divine Objects\n" +
            "│ Count: 16 total\n" +
            "│ Source: 7 Integrity Knights + 9 T4 chain\n" +
            "│ Trigger: Hand-placed, never random\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Peak rarity gear. All 16 Divine weapons are hand-placed — 7\n" +
            "canon Integrity Knight swords and 9 Evolution Chain T4 apex\n" +
            "weapons.\n\n" +
            "USAGE\n" +
            "Earn via specific named encounters or the T4 evolution craft.\n" +
            "Drops log a bespoke BrightRed line with the diamond glyph and\n" +
            "the tag \"Divine Object.\"\n\n" +
            "EFFECTS\n" +
            "Divine weapons are UNBREAKABLE — durability never ticks down.\n" +
            "Canon flavor says Divines bypass enemy block rolls; this flag\n" +
            "is forward-looking and currently Divines still roll normal\n" +
            "block/parry math.\n\n" +
            "COSTS\n" +
            "The T4 craft demands 20 chain catalysts + 1 rare peak material\n" +
            "per weapon.\n\n" +
            "TIPS\n" +
            "Because Divines don't degrade, they escape every Anvil repair\n" +
            "cost — once earned they pay back forever.\n\n" +
            "SEE ALSO\n" +
            "[Divine Object Set — Integrity Knights] · [Weapon Evolution Chains] · [Evolution Chain Table]")
        {
            Tags = new[] { "rarity", "divine", "weapons" }
        },

        new("Combat & Rarity", "Sword Skills — Unlock & Use",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Sword Skills — Unlock & Use\n" +
            "│ Trees: One per weapon type\n" +
            "│ Thresholds: 0,10,25,50,75,100,150,200,300,500\n" +
            "│ Slots: Up to 4 (F1-F4)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Each weapon class has its own skill tree unlocked by kill\n" +
            "thresholds. Hotkeys F1-F4 fire equipped skills; F opens the\n" +
            "skill menu.\n\n" +
            "USAGE\n" +
            "Rack kills with a given weapon until the next threshold trips.\n" +
            "Newly unlocked skills auto-fill your first empty skill slot.\n\n" +
            "EFFECTS\n" +
            "Thresholds at 0, 10, 25, 50, 75, 100, 150, 200, 300, and 500\n" +
            "kills. The Hollow Ingress run modifier doubles all required\n" +
            "kill counts.\n\n" +
            "COSTS\n" +
            "None per unlock — just the kill grind.\n\n" +
            "TIPS\n" +
            "Pick one weapon for the early floors and stick with it — the\n" +
            "500-kill tail unlocks the strongest capstones. Rotate a\n" +
            "secondary only after you've hit 100 on your main.\n\n" +
            "SEE ALSO\n" +
            "[Sword Skill Cooldown & Post-Motion] · [Weapon Proficiency Ranks] · [Run Modifiers (12 Optional Challenges)]")
        {
            Tags = new[] { "combat", "skills", "unlock" }
        },

        new("Combat & Rarity", "Sword Skill Cooldown & Post-Motion",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Sword Skill Cooldown & Post-Motion\n" +
            "│ Formula: (ATK+prof+buffs)*mult + SkillDmg\n" +
            "│ Cooldown: Per-skill turn count\n" +
            "│ Post-motion: +50% incoming for 1-3 turns\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Sword skills multiply your attack through a DamageMultiplier\n" +
            "and add flat SkillDamage from Intelligence and gear. Each skill\n" +
            "has its own cooldown and recovery window.\n\n" +
            "USAGE\n" +
            "Fire a skill via F1-F4, then wait out cooldown before the\n" +
            "next cast. Multi-hit skills roll crit separately per hit.\n\n" +
            "EFFECTS\n" +
            "Example cooldowns: Starburst Stream 15 turns, The Eclipse 30\n" +
            "turns. SkillCooldown-N reduces the cooldown. During post-motion\n" +
            "(1-3 turns) you take +50% incoming damage and cannot chain\n" +
            "another skill; PostMotion-N shrinks the window.\n\n" +
            "COSTS\n" +
            "Post-motion effectively forfeits your defenses for the recovery\n" +
            "turns.\n\n" +
            "TIPS\n" +
            "Cast the biggest skill when an enemy is about to die, not\n" +
            "before — overkill damage is wasted but the post-motion\n" +
            "penalty isn't.\n\n" +
            "SEE ALSO\n" +
            "[Sword Skills — Unlock & Use] · [Damage Mitigation] · [The Six Attributes]")
        {
            Tags = new[] { "combat", "skills" }
        },

        new("Combat & Rarity", "Weapon Proficiency Ranks",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Weapon Proficiency Ranks\n" +
            "│ Currency: Kills with one weapon type\n" +
            "│ Reward: Flat ATK + fork passives\n" +
            "│ Cap: Level 110 per weapon type\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Kills per weapon type feed a 110-level Proficiency Tree. The\n" +
            "existing 15 titles (Novice through The Black Swordsman) still\n" +
            "ride the curve as cosmetic bands — they are now draped over the\n" +
            "numeric levels rather than being the system itself.\n\n" +
            "USAGE\n" +
            "Stick with one weapon class long enough to accrue kills. Your\n" +
            "Proficiency Level increments on a geometric curve, and the\n" +
            "bonus multiplies into the Damage Formula as proficiency bonus.\n\n" +
            "EFFECTS\n" +
            "  L1 Novice           (10 kills)      +1 ATK\n" +
            "  L25 Journeyman      (~100 kills)    +4 ATK, fork: crit/parry\n" +
            "  L50 Weapon Lord     (~500 kills)    +37 ATK, fork: dodge/skill\n" +
            "  L75 Mythic          (~2000 kills)   +56 ATK, fork: combo/stun\n" +
            "  L100 Divine Edge    (~6000 kills)   +95 ATK, capstone fork\n" +
            "  L110 Black Swordsman (9999 kills)   +120 ATK\n" +
            "Each of the four forks offers a 1-of-2 passive pick — see the\n" +
            "Proficiency Forks topic for the branch list.\n\n" +
            "COSTS\n" +
            "None beyond the kill grind. Fork choices persist per save.\n\n" +
            "TIPS\n" +
            "The forks cluster at L25/50/75/100 — plan grind milestones\n" +
            "around those thresholds, not the raw damage numbers. Pick\n" +
            "forks that match your build: crit/dodge for Dex, combo/parry\n" +
            "for tanks.\n\n" +
            "SEE ALSO\n" +
            "[Weapon Proficiency Tree] · [Damage Formula] · [Sword Skills — Unlock & Use]")
        {
            Tags = new[] { "combat", "stats", "proficiency" }
        },

        new("Combat & Rarity", "Kill Streaks",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Kill Streaks\n" +
            "│ Tiers: Double -> GODLIKE\n" +
            "│ Bonus: Per-mob Col yield\n" +
            "│ Trigger: Consecutive fast kills\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Chaining kills in quick succession unlocks banner tiers that\n" +
            "pad Col rewards. Damage doesn't break the chain — only missing\n" +
            "the cadence does.\n\n" +
            "USAGE\n" +
            "Keep finishing mobs without a long break. Moving, resting, and\n" +
            "missing swings all preserve the streak.\n\n" +
            "EFFECTS\n" +
            "  2 kills   Double Kill\n" +
            "  3 kills   Triple Kill\n" +
            "  4 kills   Multi Kill\n" +
            "  5 kills   RAMPAGE\n" +
            "  7 kills   UNSTOPPABLE\n" +
            "  10 kills  GODLIKE\n\n" +
            "Streak tiers contribute to per-mob Col yield (up to 3 tiers\n" +
            "every 5 kills).\n\n" +
            "COSTS\n" +
            "Taking damage breaks Perfect-Kill runs but not the streak.\n\n" +
            "TIPS\n" +
            "Clear clustered rooms in one push before resting — the final\n" +
            "Col payout scales more with streak tier than with individual\n" +
            "mob value.\n\n" +
            "SEE ALSO\n" +
            "[Combo Attacks] · [Col Economy — How You Earn] · [Achievements]")
        {
            Tags = new[] { "combat", "stats" }
        },

        new("Combat & Rarity", "Hunger, Satiety & Fatigue",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Hunger, Satiety & Fatigue\n" +
            "│ Satiety: Drains per turn\n" +
            "│ Fatigue: Ticks from exertion\n" +
            "│ Trigger: Time, sprint, combat\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Satiety is your food clock; fatigue is your exhaustion clock.\n" +
            "Both swing combat stats and need regular maintenance at food\n" +
            "and campfires.\n\n" +
            "USAGE\n" +
            "Eat food, rest at campfires (+20 satiety), and sleep to clear\n" +
            "fatigue. Sprint cycles and heavy combat tick fatigue up.\n\n" +
            "EFFECTS\n" +
            "Well Fed (>=80): +1 HP regen, +3 ATK/DEF. Hungry (<20): -2 ATK.\n" +
            "Starving (0): -5 HP/turn plus STARVING status. Mild Fatigue:\n" +
            "-1 SPD. Heavy Fatigue: -3 SPD, -5% crit. Food restores satiety\n" +
            "and HP over turns.\n\n" +
            "COSTS\n" +
            "Satiety drains 1 point per N turns (biome- and modifier-\n" +
            "dependent). The Iron Rank run modifier doubles drain rate.\n\n" +
            "TIPS\n" +
            "Hover at Well Fed before any boss — the +3 ATK/DEF feeds right\n" +
            "into Damage Mitigation. Carry at least one stack of cheap bread\n" +
            "for emergency top-ups.\n\n" +
            "SEE ALSO\n" +
            "[Food & Cooking] · [Damage Mitigation] · [Run Modifiers (12 Optional Challenges)]")
        {
            Tags = new[] { "combat", "stats" }
        },

        new("Combat & Rarity", "Sprint & Stealth Move",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Sprint & Stealth Move\n" +
            "│ Sprint: Shift + dir, 2 tiles\n" +
            "│ Stealth: Ctrl + dir, 1 tile\n" +
            "│ Trigger: Modifier key on move\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Two movement modifiers beyond the normal step. Sprint\n" +
            "repositions fast; Stealth Move halves aggro for the turn.\n\n" +
            "USAGE\n" +
            "Hold Shift with a direction to sprint 2 tiles. Hold Ctrl with\n" +
            "a direction to take one quiet step.\n\n" +
            "EFFECTS\n" +
            "Sprint cannot cross Lava, Trap, or DangerZone tiles — you'll\n" +
            "stop short. Stealth Move halves aggro range that turn and\n" +
            "hides you from mobs farther than 5 tiles; it breaks on attack.\n" +
            "Neither triggers Extra Search's passive trap-reveal — only\n" +
            "normal step moves do.\n\n" +
            "COSTS\n" +
            "Sprint adds +50% satiety drain for the turn spent.\n\n" +
            "TIPS\n" +
            "Use Sprint to escape post-motion recovery before the follow-up\n" +
            "hit lands. Use Stealth Move when scouting unfamiliar rooms so\n" +
            "you don't pull the whole pack at once.\n\n" +
            "SEE ALSO\n" +
            "[Heavy Attacks (Winding Up)] · [Unique Skill: Extra Skill — Search] · [Traps & Hazards]")
        {
            Tags = new[] { "combat", "movement" }
        },

        new("Combat & Rarity", "Quick-Use Slots (1-5)",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Quick-Use Slots (1-5)\n" +
            "│ Keys: 1-5\n" +
            "│ Bindings: Fixed\n" +
            "│ Trigger: Direct consume, no menu\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Number keys 1-5 fire pre-bound consumables without opening\n" +
            "the inventory. Each key has a fixed item assignment.\n\n" +
            "USAGE\n" +
            "Hit the key mid-combat. One stock is consumed from your pack\n" +
            "if available; nothing happens if empty.\n\n" +
            "EFFECTS\n" +
            "  1   Health Potion         (instant +50 HP)\n" +
            "  2   Greater Health Potion (+150 HP)\n" +
            "  3   Antidote              (cures Poison + Bleed)\n" +
            "  4   Battle Elixir         (+15 ATK, +10 SPD for 60 turns)\n" +
            "  5   Escape Rope           (warp to floor entrance)\n\n" +
            "COSTS\n" +
            "Anti-Crystal Tyranny run modifier disables Crystal-based\n" +
            "consumables (Revive, Teleport, etc.).\n\n" +
            "TIPS\n" +
            "Rebuild stock at every vendor visit — the slots are useless\n" +
            "if empty. Battle Elixir (slot 4) on a boss fight almost always\n" +
            "outperforms saving it.\n\n" +
            "SEE ALSO\n" +
            "[Potions, Crystals & Throwables] · [Status: Bleed & Poison] · [Run Modifiers (12 Optional Challenges)]")
        {
            Tags = new[] { "combat", "potions" }
        },

        new("Combat & Rarity", "Vision & FOV",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Vision & FOV\n" +
            "│ Base radius: 20 tiles (bright day)\n" +
            "│ Night floor: 8 tiles\n" +
            "│ Modifiers: Biome, time, lights\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Your visible area is a shadowcast FOV that shrinks with time\n" +
            "of day and biome penalties and expands around light sources.\n\n" +
            "USAGE\n" +
            "Automatic. Light sources (campfires, shrines, vents) emit warm\n" +
            "bubbles you can stand next to for expanded sight.\n\n" +
            "EFFECTS\n" +
            "  - Base radius:       20 tiles on bright day\n" +
            "  - Time of day:       shrinks to 8 tiles at deep night\n" +
            "  - Biome:              Darkness -20, Forest -8, Swamp -5, Desert -5\n" +
            "  - Weather:           Fog reduces trap detection, not FOV\n" +
            "  - Starless Night:    run modifier locks vision at -20\n\n" +
            "COSTS\n" +
            "Fog-of-war persists — previously seen tiles stay dim but don't\n" +
            "update mob movement until re-seen.\n\n" +
            "TIPS\n" +
            "Hug shrine and campfire bubbles when exploring Darkness or\n" +
            "Forest biomes; the extra radius can reveal traps before you\n" +
            "step on them.\n\n" +
            "SEE ALSO\n" +
            "[Look Mode & Counter Stance] · [Traps & Hazards] · [Biomes]")
        {
            Tags = new[] { "combat", "vision" }
        },

        new("Combat & Rarity", "Look Mode & Counter Stance",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Look Mode & Counter Stance\n" +
            "│ Look: L key, cursor inspect\n" +
            "│ Stance: V key, forces Parry\n" +
            "│ Trigger: Manual hotkey\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Two utility hotkeys: Look Mode inspects tiles and enemies\n" +
            "without moving; Counter Stance trades your turn for an\n" +
            "auto-Parry on the next incoming hit.\n\n" +
            "USAGE\n" +
            "Press L to enter Look Mode. Use arrows to move the cursor,\n" +
            "Tab/Shift+Tab to cycle visible monsters, Esc or L again to\n" +
            "exit. Press V to enter Counter Stance.\n\n" +
            "EFFECTS\n" +
            "Look Mode shows each target's name, HP, stats, and status\n" +
            "without advancing time. Counter Stance auto-Parries the next\n" +
            "incoming hit for a 25% Attack counter-strike.\n\n" +
            "COSTS\n" +
            "Counter Stance consumes 1 turn. The stance ends after one\n" +
            "incoming hit or one turn, whichever comes first.\n\n" +
            "TIPS\n" +
            "Look Mode before engaging is the safest scouting tool in the\n" +
            "game. Stance shines when a boss telegraphs Winding Up and you\n" +
            "can't reposition.\n\n" +
            "SEE ALSO\n" +
            "[Heavy Attacks (Winding Up)] · [Defense — Block, Parry, Dodge] · [Vision & FOV]")
        {
            Tags = new[] { "combat", "vision" }
        },

        new("Combat & Rarity", "Death Penalty & Hardcore",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Death Penalty & Hardcore\n" +
            "│ Normal: -25% Col + XP loss\n" +
            "│ Hardcore: Permanent save wipe\n" +
            "│ Trigger: HP reaches 0\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Two death rules. Normal mode respawns you at last save with\n" +
            "some resource loss; Hardcore ends the run and wipes the slot.\n\n" +
            "USAGE\n" +
            "Applies automatically when HP hits 0. Hardcore mode must be\n" +
            "selected at character creation.\n\n" +
            "EFFECTS\n" +
            "NORMAL: lose 25% Col on hand, XP toward next level is lost\n" +
            "(but levels aren't taken away), equipment stays intact,\n" +
            "respawn at last save with full HP. HARDCORE: one life only,\n" +
            "death wipes the save slot permanently, score converts to a\n" +
            "permanent leaderboard entry.\n\n" +
            "COSTS\n" +
            "Hardcore saves cannot be loaded after the run ends.\n\n" +
            "TIPS\n" +
            "Hardcore mirrors the SAO novel's death-game rule — Normal is a\n" +
            "beta-tester-style run. Keep a Revive Crystal equipped on\n" +
            "Normal runs to avoid the Col penalty entirely.\n\n" +
            "SEE ALSO\n" +
            "[Save System] · [Potions, Crystals & Throwables] · [Achievements]")
        {
            Tags = new[] { "combat", "hardcore" }
        },

        // ═══════════════════════════════════════════════════════════════
        // ── 2. Character Progression ───────────────────────────────────
        // ═══════════════════════════════════════════════════════════════

        new("Progression", "Experience & Leveling",
            "┌─ Progression\n" +
            "│ Topic: Experience & Leveling\n" +
            "│ Formula: 100 + (100 * current_level)\n" +
            "│ Reward: +5 SP + 1-of-3 talent pick\n" +
            "│ Unlock: Every level\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "XP needed for the next level scales linearly: Lv2 = 200,\n" +
            "Lv3 = 300, Lv4 = 400, and so on. Every level-up hands you\n" +
            "stat points and a passive-talent choice.\n\n" +
            "USAGE\n" +
            "Kill mobs and turn in quests. Level-ups fire automatically as\n" +
            "the XP threshold trips.\n\n" +
            "EFFECTS\n" +
            "On level-up: +5 Skill Points, HP fully restored, and a prompt\n" +
            "to pick 1 of 3 random Passive Talent perks. Excess XP rolls\n" +
            "over into the next level. Max HP also scales with Vitality.\n\n" +
            "COSTS\n" +
            "None — leveling is pure gain.\n\n" +
            "TIPS\n" +
            "Don't ignore attributes: Vitality scaling means stat\n" +
            "allocation matters as much as level. Save tough boss attempts\n" +
            "for immediately after a level-up to ride the HP restore.\n\n" +
            "SEE ALSO\n" +
            "[The Six Attributes] · [Passive Talents (Level-Up Perks)] · [Floor Titles]")
        {
            Tags = new[] { "leveling", "stats" }
        },

        new("Progression", "Starting Loadout",
            "┌─ Progression\n" +
            "│ Topic: Starting Loadout\n" +
            "│ Level: 1\n" +
            "│ Purse: 1000 Col, 10 SP\n" +
            "│ Unlock: New character\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Every fresh character starts at Level 1 with the same gear,\n" +
            "purse, and 10 Skill Points to distribute before the first\n" +
            "fight.\n\n" +
            "USAGE\n" +
            "Allocate the starting SP at the character sheet before leaving\n" +
            "the Town of Beginnings.\n\n" +
            "EFFECTS\n" +
            "  - 1000 Col, 10 Skill Points\n" +
            "  - Title: \"Adventurer\"\n" +
            "  - Iron Sword equipped, 1 Health Potion in inventory\n" +
            "Base stats start at 0 for Attack/Defense/Speed/SkillDamage.\n" +
            "Base CritRate 5%, Base CritHit damage 10. All 6 attributes\n" +
            "start at 0.\n\n" +
            "COSTS\n" +
            "None on creation.\n\n" +
            "TIPS\n" +
            "Pick one primary stat (Str for damage, Vit for survival) and\n" +
            "pour most starting SP there. Spread investment leaves every\n" +
            "combat number underwhelming.\n\n" +
            "SEE ALSO\n" +
            "[The Six Attributes] · [Experience & Leveling] · [Town of Beginnings NPCs (F1)]")
        {
            Tags = new[] { "leveling", "stats" }
        },

        new("Progression", "The Six Attributes",
            "┌─ Progression\n" +
            "│ Topic: The Six Attributes\n" +
            "│ Cost: 1 SP per point\n" +
            "│ Scaling: Flat per-point bonuses\n" +
            "│ Unlock: Level 1\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Six attributes translate Skill Points into combat stats.\n" +
            "Every point buys a fixed chunk of one derived stat.\n\n" +
            "USAGE\n" +
            "Spend SP on the character sheet. 1 SP equals 1 attribute\n" +
            "point.\n\n" +
            "EFFECTS\n" +
            "  VITALITY     +10 MaxHP + faster passive regen (1 + VIT/3 HP/turn)\n" +
            "  STRENGTH     +2 ATK per point\n" +
            "  ENDURANCE    +2 DEF per point\n" +
            "  DEXTERITY    +0.5% CritRate + 1 CritDamage; improves accuracy\n" +
            "  AGILITY      +2 SPD (turn priority) + improves dodge\n" +
            "  INTELLIGENCE +2 SkillDamage (boosts Sword Skill multipliers)\n\n" +
            "COSTS\n" +
            "Skill Points are finite — each allocation locks out another.\n\n" +
            "TIPS\n" +
            "Dex double-dips on crit rate and crit damage, so it scales\n" +
            "crit builds twice. Vit is the only defensive stat that also\n" +
            "boosts passive regen.\n\n" +
            "SEE ALSO\n" +
            "[Derived Combat Stats] · [Experience & Leveling] · [Critical Hits]")
        {
            Tags = new[] { "attributes", "stats" }
        },

        new("Progression", "Derived Combat Stats",
            "┌─ Progression\n" +
            "│ Topic: Derived Combat Stats\n" +
            "│ Stats: ATK / DEF / SPD / SD / CRT / CD\n" +
            "│ Scaling: Attribute x 2 + gear\n" +
            "│ Trigger: Automatic recompute\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "The character sheet derives six combat numbers from base\n" +
            "stats, attributes, and gear bonuses.\n\n" +
            "USAGE\n" +
            "Open the sheet to see the current rollup. Numbers refresh\n" +
            "whenever you re-equip, repair, or spend SP.\n\n" +
            "EFFECTS\n" +
            "  ATK = BaseAttack + (STR x 2) + gear bonuses\n" +
            "  DEF = BaseDefense + (END x 2) + gear bonuses\n" +
            "  SPD = BaseSpeed + (AGI x 2) + gear bonuses\n" +
            "  SD  = BaseSkillDamage + (INT x 2) + gear bonuses\n" +
            "  CRT = BaseCriticalRate + DEX/2  (percent)\n" +
            "  CD  = BaseCriticalHitDamage + DEX\n\n" +
            "COSTS\n" +
            "Broken gear (0 durability) contributes nothing until repaired.\n\n" +
            "TIPS\n" +
            "Audit the sheet after repair runs — a dead weapon or armor\n" +
            "slot silently halves your output until you notice.\n\n" +
            "SEE ALSO\n" +
            "[The Six Attributes] · [Damage Formula] · [Anvil — Repair, Enhance, Evolve, Refine]")
        {
            Tags = new[] { "stats", "attributes" }
        },

        new("Progression", "Passive Talents (Level-Up Perks)",
            "┌─ Progression\n" +
            "│ Topic: Passive Talents (Level-Up Perks)\n" +
            "│ Pool: 11 perks\n" +
            "│ Offer: 3 random at each level\n" +
            "│ Unlock: On every level-up\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Every level-up offers 3 random perks from an 11-option pool.\n" +
            "All are permanent and stack.\n\n" +
            "USAGE\n" +
            "Pick one from the 3 offered at the level-up prompt. The other\n" +
            "two are not banked — they're gone for that level.\n\n" +
            "EFFECTS\n" +
            "  Keen Edge (+3% Crit), Brutal Strikes (+5 CritDmg)\n" +
            "  Iron Will (+2 VIT), Power Surge (+3 BaseATK)\n" +
            "  Fortify (+3 BaseDEF), Quick Step (+2 BaseSPD)\n" +
            "  Brute Force (+2 STR), Endurance (+2 END)\n" +
            "  Nimble Fingers (+2 DEX), Fleet Foot (+2 AGI), Sharp Mind (+2 INT)\n\n" +
            "COSTS\n" +
            "None — the pick is free.\n\n" +
            "TIPS\n" +
            "Keen Edge plus Brutal Strikes stacks cleanly on Dex builds.\n" +
            "If all 3 offered perks miss your build, pick the closest\n" +
            "BaseATK/DEF/SPD option — flat bases always contribute.\n\n" +
            "SEE ALSO\n" +
            "[Experience & Leveling] · [The Six Attributes] · [Derived Combat Stats]")
        {
            Tags = new[] { "leveling", "talents", "stats" }
        },

        new("Progression", "Unique Skill: Dual Blades",
            "┌─ Progression\n" +
            "│ Topic: Unique Skill: Dual Blades\n" +
            "│ Stat: Adds offhand swing per main hit\n" +
            "│ Scaling: offhand.BaseDmg*0.6 + prof/2\n" +
            "│ Unlock: F74 Gleam Eyes or 50 1H kills\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Kirito's signature skill. Lets you wield a second 1H Sword in\n" +
            "OffHand and auto-append an off-hand strike to every main hit.\n\n" +
            "USAGE\n" +
            "Equip a second 1H Sword in OffHand after unlock. The off-hand\n" +
            "swing fires automatically — no new hotkey.\n\n" +
            "EFFECTS\n" +
            "Each main-hand swing triggers an off-hand strike for\n" +
            "max(1, offhand.BaseDamage x 0.6 + prof/2) damage. Plus +15%\n" +
            "damage on single-hit skills. Unlocks Double Circular (2.0x),\n" +
            "Starburst Stream (16-hit 8.0x), and The Eclipse (27-hit 12.0x).\n\n" +
            "COSTS\n" +
            "OffHand shield slot is forfeited — no more Block rolls.\n\n" +
            "TIPS\n" +
            "Since off-hand scales on BaseDamage + prof, pick the same\n" +
            "weapon class in both hands to share proficiency grinding\n" +
            "across both swings. Paired canon items (Elucidator/Dark\n" +
            "Repulser, Elucidator Rouge/Flare Pulsar, Black Iron A/B)\n" +
            "bypass the unlock entirely — see Paired Dual-Wield Weapons.\n\n" +
            "SEE ALSO\n" +
            "[Paired Dual-Wield Weapons] · [Unique Skill: Holy Sword] · [Equipment Slots & Dual Wield]")
        {
            Tags = new[] { "combat", "unique-skills", "dual-blades" }
        },

        new("Progression", "Unique Skill: Holy Sword",
            "┌─ Progression\n" +
            "│ Topic: Unique Skill: Holy Sword\n" +
            "│ Stat: +15% Block Chance\n" +
            "│ Requires: 1H Sword + true Shield\n" +
            "│ Unlock: Defeat Heathcliff on F75\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Heathcliff's stance. Active only when wielding a 1H Sword\n" +
            "with a real shield (not a second sword) in OffHand.\n\n" +
            "USAGE\n" +
            "Equip a 1H Sword in the main hand and a Shield in the OffHand\n" +
            "after F75 clears. The stance is passive while the loadout\n" +
            "holds.\n\n" +
            "EFFECTS\n" +
            "+15% Block Chance. Unlocks Sacred Edge (7.0x overhead with\n" +
            "brief damage reduction) and Divine Cross (2-hit 4.5x).\n\n" +
            "COSTS\n" +
            "Loses the off-hand swing and +15% single-hit bonus that Dual\n" +
            "Blades offers — they're mutually exclusive stances.\n\n" +
            "TIPS\n" +
            "Because unlock is gated on beating Heathcliff with no grind\n" +
            "fallback, treat the F75 clear as a one-shot — don't skip the\n" +
            "fight expecting to come back later.\n\n" +
            "SEE ALSO\n" +
            "[Unique Skill: Dual Blades] · [Defense — Block, Parry, Dodge] · [Floor Boss Roster — Canon Highlights]")
        {
            Tags = new[] { "combat", "unique-skills", "holy-sword" }
        },

        new("Progression", "Unique Skill: Martial Arts",
            "┌─ Progression\n" +
            "│ Topic: Unique Skill: Martial Arts\n" +
            "│ Stat: +10% dmg, +20% CritRate unarmed\n" +
            "│ Requires: Empty hands\n" +
            "│ Unlock: Ran's F2 trial or 30 unarmed kills\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Unarmed combat stance. Empty hands flip your crit rate and\n" +
            "damage up and open an unarmed skill tree.\n\n" +
            "USAGE\n" +
            "Unequip your main-hand weapon after unlock. Skills auto-fill\n" +
            "your slots as kill thresholds trip.\n\n" +
            "EFFECTS\n" +
            "+10% damage and +20% CritRate while both hands are empty.\n" +
            "Unarmed skill tree: Smash Knuckle at 0, Gazelle Rush at 10,\n" +
            "Blazing Blow at 100, Turbulence Rush at 200, Deadly Blow at\n" +
            "500 kills.\n\n" +
            "COSTS\n" +
            "No weapon means no weapon-slot bonuses (ATK, crit, sword\n" +
            "skills).\n\n" +
            "TIPS\n" +
            "Grind the early unarmed skills against the same packs you'd\n" +
            "use for 1H Sword — the 20% crit rate pairs neatly with Keen\n" +
            "Edge and Dex.\n\n" +
            "SEE ALSO\n" +
            "[Ran the Brawler (F2)] · [Sword Skills — Unlock & Use] · [Critical Hits]")
        {
            Tags = new[] { "combat", "unique-skills", "martial-arts" }
        },

        new("Progression", "Unique Skill: Katana Mastery",
            "┌─ Progression\n" +
            "│ Topic: Unique Skill: Katana Mastery\n" +
            "│ Stat: +10% dmg, +10% crit, 15% Bleed\n" +
            "│ Requires: Katana equipped\n" +
            "│ Unlock: 100 Katana kills\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Klein's signature. Buffs every Katana strike with flat damage,\n" +
            "crit, and a Bleed proc on hit.\n\n" +
            "USAGE\n" +
            "Keep a Katana in the main hand after unlock — the passive\n" +
            "activates automatically.\n\n" +
            "EFFECTS\n" +
            "+10% damage, +10% CritRate, and a 15% Bleed proc on hit.\n" +
            "Adds Tsumujigaeshi and Zekkuu to the Katana skill tree.\n\n" +
            "COSTS\n" +
            "None beyond the 100-kill grind gate.\n\n" +
            "TIPS\n" +
            "Stack this with a Bleed+N Katana (Karakurenai, Muramasa, or an\n" +
            "Elemental Dark variant) for constant bleed-tick pressure — the\n" +
            "weapon proc adds a separate 3-turn bleed on top of the 15%\n" +
            "passive, which chains into Hemorrhage if Poison is also active.\n\n" +
            "SEE ALSO\n" +
            "[Status: Bleed & Poison] · [Sword Skills — Unlock & Use] · [Named Legendary Highlights] · [Infinity Moment Shop Weapons] · [Elemental Weapon Variants]")
        {
            Tags = new[] { "combat", "unique-skills", "katana" }
        },

        new("Progression", "Unique Skill: Darkness Blade",
            "┌─ Progression\n" +
            "│ Topic: Unique Skill: Darkness Blade\n" +
            "│ Stat: +20% dmg, +10% Dodge (Night only)\n" +
            "│ Requires: Night phase active\n" +
            "│ Unlock: Kill any floor boss at Night\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "\"The Black Swordsman's\" stance. A flat Night-phase buff that\n" +
            "swings combat in your favor after dusk.\n\n" +
            "USAGE\n" +
            "Active only during the Night phase of the day/night cycle.\n" +
            "The Starless Night run modifier pins the world at Night for a\n" +
            "permanent buff window.\n\n" +
            "EFFECTS\n" +
            "+20% damage and +10% Dodge while Night is active. Unlocks\n" +
            "Shadow Cleave (3-hit rush 3.5x, range 3).\n\n" +
            "COSTS\n" +
            "Zero uptime during Day — the stance lies dormant for more than\n" +
            "half the clock.\n\n" +
            "TIPS\n" +
            "Bank hard fights for Night cycles. Combine with the Starless\n" +
            "Night modifier for 100% uptime, at the cost of permanent FOV\n" +
            "penalties.\n\n" +
            "SEE ALSO\n" +
            "[Day/Night Cycle] · [Run Modifiers (12 Optional Challenges)] · [Vision & FOV]")
        {
            Tags = new[] { "combat", "unique-skills", "darkness" }
        },

        new("Progression", "Unique Skill: Blazing & Frozen Edge",
            "┌─ Progression\n" +
            "│ Topic: Unique Skill: Blazing & Frozen Edge\n" +
            "│ Stat: Elemental proc + 25% vs opposite\n" +
            "│ Requires: Any weapon\n" +
            "│ Unlock: Volcanic / Ice biome boss kill\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Two elemental attunements that work with any weapon. Each\n" +
            "carries a proc, an anti-element damage bonus, and a signature\n" +
            "skill.\n\n" +
            "USAGE\n" +
            "Fell the biome-specific floor boss to unlock the matching\n" +
            "edge. Both attunements apply passively once learned.\n\n" +
            "EFFECTS\n" +
            "BLAZING EDGE: +10% Burn proc on hit, +25% damage vs ice/\n" +
            "frost/snow enemies, unlocks Flame Strike (3.5x cone, 50%\n" +
            "Burn).\n" +
            "FROZEN EDGE: +10% Slow proc on hit, +25% damage vs fire/\n" +
            "flame/lava enemies, unlocks Glacial Slash (3.5x line, 50%\n" +
            "Slow).\n\n" +
            "COSTS\n" +
            "None — purely additive.\n\n" +
            "TIPS\n" +
            "Chase both edges: the opposing-element bonus means Blazing\n" +
            "carries you through Ice biomes and Frozen carries you through\n" +
            "Volcanic. One pair, two biome hard-counters.\n\n" +
            "SEE ALSO\n" +
            "[Biomes] · [Status: Stun & Slow] · [Floor Boss Roster — Canon Highlights]")
        {
            Tags = new[] { "combat", "unique-skills", "blazing" }
        },

        new("Progression", "Unique Skill: Extra Skill — Search",
            "┌─ Progression\n" +
            "│ Topic: Unique Skill: Extra Skill — Search\n" +
            "│ Stat: Trap reveal in 3-tile radius\n" +
            "│ Requires: Normal step move\n" +
            "│ Unlock: Disarm 10 traps total\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Argo's scouting art. A passive trap-reveal aura that lights\n" +
            "up hidden hazards around you as you move.\n\n" +
            "USAGE\n" +
            "Move normally (step only — sprint and stealth don't trigger).\n" +
            "Every step refreshes the 3-tile reveal.\n\n" +
            "EFFECTS\n" +
            "Passively reveals hidden traps within a 3-tile radius around\n" +
            "your position every move. No active ability; purely an\n" +
            "exploration aid.\n\n" +
            "COSTS\n" +
            "None after unlock.\n\n" +
            "TIPS\n" +
            "Grind the 10-trap counter early on a Trap-heavy floor; the\n" +
            "total persists across floors, so progress never resets.\n\n" +
            "SEE ALSO\n" +
            "[Traps & Hazards] · [Sprint & Stealth Move] · [Vision & FOV]")
        {
            Tags = new[] { "combat", "unique-skills", "search" }
        },

        new("Progression", "Floor Titles",
            "┌─ Progression\n" +
            "│ Topic: Floor Titles\n" +
            "│ Scaling: Floor milestones\n" +
            "│ Range: Adventurer -> Liberator of Aincrad\n" +
            "│ Unlock: Climb specific floors\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Your title auto-updates as you climb Aincrad. Ten named ranks\n" +
            "span the F1-F100 ascent.\n\n" +
            "USAGE\n" +
            "Ascend a qualifying floor; the promotion fires automatically\n" +
            "and announces \"You have earned the title: <Title>!\"\n\n" +
            "EFFECTS\n" +
            "  F1   Adventurer         F25  Dungeon Scourge\n" +
            "  F2   Blooded             F35  Nightmare Walker\n" +
            "  F5   Survivor            F50  Floor Conqueror\n" +
            "  F10  Seasoned            F75  Clearance Hero\n" +
            "  F15  Proven              F100 Liberator of Aincrad\n\n" +
            "COSTS\n" +
            "None. Purely cosmetic.\n\n" +
            "TIPS\n" +
            "The Clearance Hero rank at F75 aligns with the Heathcliff\n" +
            "fight that unlocks Holy Sword — a natural story beat to look\n" +
            "forward to.\n\n" +
            "SEE ALSO\n" +
            "[Aincrad's 100 Floors & Eras] · [Ascending a Floor] · [Achievements]")
        {
            Tags = new[] { "leveling", "floors" }
        },

        new("Progression", "Weapon Proficiency Tree",
            "┌─ Progression\n" +
            "│ Topic: Weapon Proficiency Tree\n" +
            "│ Levels: 110 per weapon type\n" +
            "│ Milestones: L25 / L50 / L75 / L100 forks\n" +
            "│ Save: Per-save, per-weapon-type\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Every weapon type has its own 110-level Proficiency Tree fed\n" +
            "by kills with that weapon. Kill counts crossed at 25, 100,\n" +
            "500, and 2000 map to Levels 25, 50, 75, and 100 on a\n" +
            "geometric curve — each a fork where you pick one of two\n" +
            "passive branches.\n\n" +
            "USAGE\n" +
            "Kill monsters with the chosen weapon equipped. On hitting a\n" +
            "fork threshold, a prompt asks you to pick one of two passive\n" +
            "upgrades for that weapon type. Picks are permanent per save.\n\n" +
            "EFFECTS\n" +
            "The 15 classic rank titles (Novice through The Black\n" +
            "Swordsman) are retained as cosmetic bands draped over the\n" +
            "numeric levels:\n" +
            "  L1-9    Novice / Apprentice\n" +
            "  L10-24  Journeyman grind\n" +
            "  L25     FORK 1 (crit vs parry focus)\n" +
            "  L50     FORK 2 (dodge vs skill-damage focus)\n" +
            "  L75     FORK 3 (combo vs stun focus)\n" +
            "  L100    FORK 4 (capstone — per-weapon unique)\n" +
            "  L110    The Black Swordsman (cap, +120 ATK)\n\n" +
            "COSTS\n" +
            "None. Fork choices cannot be respec'd without a New Game.\n\n" +
            "TIPS\n" +
            "Don't split early kills across 3 weapons. A single primary\n" +
            "weapon reaches L50 long before a split build, giving you the\n" +
            "second fork faster. Use secondaries only after your primary\n" +
            "crosses L50.\n\n" +
            "SEE ALSO\n" +
            "[Weapon Proficiency Ranks] · [Sword Skills — Unlock & Use] · [Passive Talents (Level-Up Perks)]")
        {
            Tags = new[] { "proficiency", "skills", "weapons" }
        },

        // ═══════════════════════════════════════════════════════════════
        // ── 3. World & Exploration ─────────────────────────────────────
        // ═══════════════════════════════════════════════════════════════

        new("World", "Aincrad's 100 Floors & Eras",
            "┌─ World\n" +
            "│ Topic: Aincrad's 100 Floors & Eras\n" +
            "│ Floors: 1-100\n" +
            "│ Entry: Start at F1 Town of Beginnings\n" +
            "│ Unlock: Ascend by clearing each floor boss\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Aincrad is a 100-floor stacked castle. Every floor has its own\n" +
            "named boss, and clearing that boss unlocks the stairs up. Floors\n" +
            "are grouped into thematic eras that cycle biomes and rosters.\n\n" +
            "USAGE\n" +
            "Climb one floor at a time. Clearing F100 triggers GameWon.\n\n" +
            "EFFECTS\n" +
            "Thematic era bands:\n" +
            "  F1-5    Verdant   (beasts, nature)\n" +
            "  F6-10   Stone     (constructs, puzzles)\n" +
            "  F11-15  Crimson   (fire, demons)\n" +
            "  F16-20  Crystal   (ice)\n" +
            "  F21-25  Twilight  (shadow, undead)\n" +
            "  F26-75  era pattern repeats\n" +
            "  F76-95  Hollow Fragment canon endgame\n" +
            "  F96-100 Ruby Palace - F100 mirrors a clone of you\n\n" +
            "COSTS\n" +
            "None. Ascent is the objective, not a resource sink.\n\n" +
            "TIPS\n" +
            "Era bands predict biomes — pack ice gear before F16-20 and fire\n" +
            "resist before F11-15. The endgame (F76+) is notably harsher.\n\n" +
            "SEE ALSO\n" +
            "[Biomes] · [Floor Boss Roster — Canon Highlights] · [Labyrinth System] · [Ascending a Floor]")
        {
            Tags = new[] { "world", "floors", "bosses" }
        },

        new("World", "Day/Night Cycle",
            "┌─ World\n" +
            "│ Topic: Day/Night Cycle\n" +
            "│ Floors: All\n" +
            "│ Landmark: Global 400-turn clock\n" +
            "│ Unlock: Always on\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "A global clock ticks every turn and cycles through Dawn, Day,\n" +
            "Dusk, and Night on a 400-turn loop. Sun elevation follows a cosine\n" +
            "curve — SunLevel=1.0 at noon, 0.0 at midnight.\n\n" +
            "USAGE\n" +
            "Automatic. Check the HUD or use Look Mode to see current phase.\n\n" +
            "EFFECTS\n" +
            "Vision radius scales with sun level: bright day gives the full\n" +
            "viewport, deep night shrinks FOV to an 18-tile torch bubble.\n" +
            "Ambient tint shifts cool moonlit blue at night, warm off-white by\n" +
            "day. Darkness Blade unique skill only activates at Night. The\n" +
            "Starless Night run modifier pins the cycle at 0 permanently.\n\n" +
            "COSTS\n" +
            "None. Time advances whether you act or idle.\n\n" +
            "TIPS\n" +
            "Save Darkness Blade combat windows for Night; rest at campfires\n" +
            "during dawn to enter Day with a fresh vision radius.\n\n" +
            "SEE ALSO\n" +
            "[Vision & FOV] · [Unique Skill: Darkness Blade] · [Run Modifiers (12 Optional Challenges)] · [Weather]")
        {
            Tags = new[] { "world", "weather", "floors" }
        },

        new("World", "Biomes",
            "┌─ World\n" +
            "│ Topic: Biomes\n" +
            "│ Floors: All (one BiomeType per floor)\n" +
            "│ Landmark: Rolled on floor entry\n" +
            "│ Unlock: Always visible on map\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Every floor has a BiomeType that shapes vision, hazards, and mob\n" +
            "spawn tables. Biomes are rolled on floor entry and stay fixed\n" +
            "until you ascend.\n\n" +
            "USAGE\n" +
            "No action needed — biome effects apply passively while you're on\n" +
            "that floor. Scout the tile legend to spot the biome's hazards.\n\n" +
            "EFFECTS\n" +
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
            "  The Void      1 dmg every 10 turns, reality-warp flavor\n\n" +
            "COSTS\n" +
            "Biome passives can cost HP (Volcanic/Void), satiety (Desert), or\n" +
            "stats (Ice/Aquatic). Plan rest stops accordingly.\n\n" +
            "TIPS\n" +
            "Carry Antidotes into Swamps, fire resist gear into Volcanic, and\n" +
            "a torch or Extra Skill Search into Darkness floors. Settlement\n" +
            "floors are the best time to off-load loot and re-stock potions.\n\n" +
            "SEE ALSO\n" +
            "[Weather] · [Vision & FOV] · [Traps & Hazards] · [Unique Skill: Blazing & Frozen Edge]")
        {
            Tags = new[] { "world", "biomes", "floors" }
        },

        new("World", "Weather",
            "┌─ World\n" +
            "│ Topic: Weather\n" +
            "│ Floors: All\n" +
            "│ Landmark: HUD top-right indicator\n" +
            "│ Unlock: Rolled on each floor ascend\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Weather rolls once on floor entry and holds until you climb\n" +
            "stairs up. Each pattern modifies combat, trap detection, or\n" +
            "passive regen for everyone on the floor.\n\n" +
            "USAGE\n" +
            "Automatic. Read the HUD tag or open Look Mode to confirm the\n" +
            "active pattern before engaging a boss.\n\n" +
            "EFFECTS\n" +
            "  Clear  (40%) +1 passive HP regen\n" +
            "  Rainy  (25%) -3 crit for all, trap detect -10, +1 poison dur.\n" +
            "  Foggy  (15%) trap detection -20 (traps almost invisible)\n" +
            "  Windy  (20%) +5 damage on thrown items\n\n" +
            "Bounty bosses and status math respect the current pattern.\n\n" +
            "COSTS\n" +
            "None directly — but Rainy and Foggy raise the expected damage\n" +
            "you'll take by making traps hard to see.\n\n" +
            "TIPS\n" +
            "Save Fire Bombs and Flash Bombs for Windy floors (+5 each). If\n" +
            "you roll Foggy on a trap-heavy biome, consider skipping side\n" +
            "rooms and going straight to the Labyrinth.\n\n" +
            "SEE ALSO\n" +
            "[Biomes] · [Traps & Hazards] · [Critical Hits] · [Day/Night Cycle]")
        {
            Tags = new[] { "world", "weather", "floors" }
        },

        new("World", "Labyrinth System",
            "┌─ World\n" +
            "│ Topic: Labyrinth Dungeons\n" +
            "│ Floors: 1-99 (every climbed floor)\n" +
            "│ Entry: Labyrinth Entrance tile (cyan Pi)\n" +
            "│ Unlock: Always present; find the archway\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Each floor has an overworld wilderness AND a separate labyrinth\n" +
            "dungeon that houses the floor boss. The two maps are linked by a\n" +
            "Labyrinth Entrance archway (cyan Pi glyph) in a corner of the\n" +
            "overworld, usually revealed on the minimap after exploration.\n\n" +
            "USAGE\n" +
            "Step onto the archway to enter the labyrinth. Overworld state\n" +
            "freezes while you're inside. Step onto the archway again (or\n" +
            "clear the boss) to return. Stairs Up stay sealed until the floor\n" +
            "boss is dead.\n\n" +
            "EFFECTS\n" +
            "Field bosses NEVER spawn inside labyrinths — overworld only.\n" +
            "Labyrinth layouts lean corridor-and-room; overworld is open.\n\n" +
            "COSTS\n" +
            "None to enter. The boss fight itself is the real cost.\n\n" +
            "TIPS\n" +
            "Clear the overworld first — field-boss drops (and Secret Shrines)\n" +
            "only exist outside. Enter the labyrinth fully rested, at 100%\n" +
            "durability, with escape consumables in quick slots.\n\n" +
            "SEE ALSO\n" +
            "[Floor Boss Roster — Canon Highlights] · [Field Bosses — Guaranteed Drops] · [Ascending a Floor] · [Mechanical Tiles]")
        {
            Tags = new[] { "world", "floors", "bosses" }
        },

        new("World", "Safe Rooms & Mechanics",
            "┌─ World\n" +
            "│ Topic: Safe Rooms & Mechanics\n" +
            "│ Floors: All (scattered on overworld)\n" +
            "│ Landmark: Orange &, cyan O, violet cross, gold +\n" +
            "│ Unlock: Always; usually one-shot per tile\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Scattered utility tiles across each floor. Some purge status,\n" +
            "some grant buffs, some open crafting UIs. Most are one-shot and\n" +
            "consume on use; Anvil and Bounty Board re-open freely.\n\n" +
            "USAGE\n" +
            "Walk onto the tile to trigger. Campfires offer a cooking menu;\n" +
            "Anvils open the smithing/repair/evolve screen.\n\n" +
            "EFFECTS\n" +
            "CAMPFIRE (&/*, orange)    Purge Poison/Bleed/Slow, heal\n" +
            "                           15+5*floor HP, reset rest + fatigue,\n" +
            "                           cooking interaction\n" +
            "FOUNTAIN (O, cyan)         Heal 30+10*floor HP, +20 Satiety,\n" +
            "                           clear fatigue (tile consumes)\n" +
            "SHRINE (cross, violet)     +3+floor ATK & DEF for 30 turns\n" +
            "PILLAR (|)                 Reveals 15-tile map radius\n" +
            "ANVIL (+, gold)            Opens smithing/repair/evolve UI\n" +
            "BOUNTY BOARD (diamond)     100+50*floor Col contracts\n\n" +
            "COSTS\n" +
            "None to step on. Anvil Repair/Enhance/Evolve burn Col + mats.\n\n" +
            "TIPS\n" +
            "Save Shrines until right before the labyrinth run — 30 turns\n" +
            "of +ATK/+DEF goes a long way in a boss fight. Never ascend a\n" +
            "floor without visiting the Anvil first.\n\n" +
            "SEE ALSO\n" +
            "[Anvil — Repair, Enhance, Evolve, Refine] · [Hunger, Satiety & Fatigue] · [Col Economy — How You Earn] · [Lore, Journals & Enchant Shrines]")
        {
            Tags = new[] { "world", "economy", "progression" }
        },

        new("World", "Lore, Journals & Enchant Shrines",
            "┌─ World\n" +
            "│ Topic: Lore, Journals & Enchant Shrines\n" +
            "│ Floors: All; rare spawns\n" +
            "│ Landmark: Diamond, =, or gold cross glyphs\n" +
            "│ Unlock: Walk-on; one-shot each\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "A trio of one-shot tiles that reward exploration. Lore Stones and\n" +
            "Journals grant XP + canon flavor; Enchant Shrines stamp permanent\n" +
            "+2 bonuses onto equipped gear.\n\n" +
            "USAGE\n" +
            "Step on the glyph to consume it. Lore text shows in a triple-dot\n" +
            "reveal; the entry is logged to your Lore set for achievement use.\n\n" +
            "EFFECTS\n" +
            "LORE STONE (diamond)       5x floor XP + Lore set entry,\n" +
            "                            one-shot, triple-dot reveal message\n" +
            "JOURNAL (=)                Weathered diary page (in-world tip),\n" +
            "                            3x floor XP, one-shot\n" +
            "ENCHANT SHRINE (gold cross) Rolls one equipped piece, stamps +2\n" +
            "                            to random stat (ATK/DEF/SPD);\n" +
            "                            bonus persists through unequip\n\n" +
            "COSTS\n" +
            "None. All three are free on contact.\n\n" +
            "TIPS\n" +
            "Equip your best piece before stepping on an Enchant Shrine — the\n" +
            "+2 lives on the item and carries between runs. Hunt every Lore\n" +
            "Stone if you want the Loremaster achievement.\n\n" +
            "SEE ALSO\n" +
            "[Safe Rooms & Mechanics] · [Achievements] · [Secret Shrines (T1 Chain Weapons)] · [Experience & Leveling]")
        {
            Tags = new[] { "world", "progression", "xp" }
        },

        new("World", "Secret Shrines (T1 Chain Weapons)",
            "┌─ World\n" +
            "│ Topic: Secret Shrines\n" +
            "│ Floors: 5, 8, 12, 15, 18, 22, 28, 32, 36\n" +
            "│ Landmark: Magenta ! glyph, one per chain\n" +
            "│ Unlock: Walk-on; one-shot, one per floor\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Nine rare shrines scattered across the lower half of Aincrad,\n" +
            "each gifting the T1 starter weapon of one Evolution Chain.\n" +
            "Finding all nine is the fastest path to Divine endgame weapons.\n\n" +
            "USAGE\n" +
            "Walk onto the magenta ! glyph to receive the T1 weapon. Feed it\n" +
            "through the Anvil's Evolve Weapon flow to push T1 -> T2 -> T3 ->\n" +
            "T4 Divine (unbreakable).\n\n" +
            "EFFECTS\n" +
            "  F5   Final Espada      (1H Sword)\n" +
            "  F8   Prima Sabre       (Rapier)\n" +
            "  F12  Moonstruck Saber  (Scimitar)\n" +
            "  F15  Heated Razor      (Dagger)\n" +
            "  F18  Matter Dissolver  (2H Sword)\n" +
            "  F22  Heart Piercer     (Spear)\n" +
            "  F28  Lunatic Press     (Mace)\n" +
            "  F32  Matamon           (Katana)\n" +
            "  F36  Bardiche          (Axe)\n\n" +
            "COSTS\n" +
            "None for the T1 pickup. Later tiers cost catalysts + Col.\n\n" +
            "TIPS\n" +
            "Explore every floor ending in these numbers thoroughly — the\n" +
            "shrine can hide behind a Cracked Wall. Grab the T1 even if\n" +
            "it's not your current weapon; you can evolve it for an ally.\n\n" +
            "SEE ALSO\n" +
            "[Weapon Evolution Chains] · [Evolution Chain Table] · [Chain Catalysts — by Weapon Type] · [Anvil — Repair, Enhance, Evolve, Refine]")
        {
            Tags = new[] { "world", "items", "divine" }
        },

        new("World", "Traps & Hazards",
            "┌─ World\n" +
            "│ Topic: Traps & Hazards\n" +
            "│ Floors: All (density scales with biome)\n" +
            "│ Landmark: Hidden until detect roll succeeds\n" +
            "│ Unlock: Always active; detect needs Dex\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Every floor seeds hidden traps. Dex drives per-step detect rolls,\n" +
            "weather modifies them downward, and Extra Skill: Search flags\n" +
            "traps within 3 tiles passively.\n\n" +
            "USAGE\n" +
            "Traps reveal as you walk adjacent. Step around visible traps or\n" +
            "disarm them (if the trap supports it) for progress toward\n" +
            "Extra Skill: Search.\n\n" +
            "EFFECTS\n" +
            "Detection: up to 30% per-step chance + up to 65% confirm.\n" +
            "Weather modifiers: Rain -10, Fog -20.\n\n" +
            "  Spike          3 + 2*floor damage, one-shot\n" +
            "  Poison Trap    2 + floor dmg/turn for 3 turns\n" +
            "  Teleport Trap  warps you to random walkable tile\n" +
            "  Alarm Trap     alerts every mob within 10 tiles\n" +
            "  Gas Vent       1 + floor/2 dmg/turn for 3 turns, repeats\n" +
            "  Lava (~)       4 + 2*floor dmg per step; can kill outright\n" +
            "  Danger Zone    1 + floor/5 dmg/step (guards monster dens)\n" +
            "  Tall Grass     hides mobs; ambush chance = 25 - Dex\n\n" +
            "COSTS\n" +
            "HP, status durations, and positioning. Lava can end a run.\n\n" +
            "TIPS\n" +
            "Pump Dex early if you plan to run Foggy biomes. Stealth Move\n" +
            "does NOT trigger Extra Skill: Search's passive reveal — walk\n" +
            "normally through trap-heavy rooms.\n\n" +
            "SEE ALSO\n" +
            "[Weather] · [Biomes] · [Unique Skill: Extra Skill — Search] · [Sprint & Stealth Move]")
        {
            Tags = new[] { "world", "biomes", "floors" }
        },

        new("World", "Mechanical Tiles",
            "┌─ World\n" +
            "│ Topic: Mechanical Tiles\n" +
            "│ Floors: All\n" +
            "│ Landmark: Levers, plates, cracked walls, chests\n" +
            "│ Unlock: Walk-on (plate), bump (lever/wall), open (chest)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Interactive overworld tiles that open shortcuts, hide loot, or\n" +
            "gate progression. Most are discovered by bumping or walking onto\n" +
            "them; cracked walls need a hit to break open.\n\n" +
            "USAGE\n" +
            "Bump levers, step on pressure plates, attack cracked walls, and\n" +
            "bump chests to open them. Stairs Up activate after the floor\n" +
            "boss dies.\n\n" +
            "EFFECTS\n" +
            "LEVER & PRESSURE PLATE   Linked to a wall/door pair. Activating\n" +
            "                          toggles the linked wall into a door\n" +
            "                          (or vice versa). Used for dead-end\n" +
            "                          puzzle loops.\n" +
            "CRACKED WALL (shaded)    Hidden passage - break to reveal a\n" +
            "                          safe room with a chest.\n" +
            "CHEST (gold diamond)     Loot container; tier scales with floor.\n" +
            "STAIRS DOWN (<)          Never used (Aincrad climbs up only).\n" +
            "STAIRS UP (>)            Sealed until the floor boss is dead.\n\n" +
            "COSTS\n" +
            "Breaking a cracked wall costs 1 weapon durability per swing.\n\n" +
            "TIPS\n" +
            "Any lever that seems pointless probably has a linked plate\n" +
            "elsewhere — trace the floor systematically. Cracked walls almost\n" +
            "always guard Epic-or-better chests.\n\n" +
            "SEE ALSO\n" +
            "[Labyrinth System] · [Safe Rooms & Mechanics] · [Ascending a Floor] · [Col Economy — How You Earn]")
        {
            Tags = new[] { "world", "floors", "economy" }
        },

        new("World", "Floor Boss Roster — Canon Highlights",
            "┌─ World\n" +
            "│ Topic: Floor Boss Roster\n" +
            "│ Floors: 1-100, one canonical boss each\n" +
            "│ Landmark: Labyrinth boss chamber\n" +
            "│ Unlock: Enter the Labyrinth and survive\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Each floor has one canonical boss whose death unlocks Stairs Up.\n" +
            "A select slice maps to SAO novel / anime canon — the rest follow\n" +
            "the scaling curve below.\n\n" +
            "USAGE\n" +
            "Fought inside the Labyrinth. Allies join; party size caps at 2.\n\n" +
            "EFFECTS\n" +
            "F1  Illfang the Kobold Lord       F50  The Six-Armed Buddha\n" +
            "F2  Asterius the Taurus King      F55  X'rphan the White Wyrm\n" +
            "F3  Nerius the Evil Treant        F60  Armoured Stone Warrior\n" +
            "F4  Wythege the Hippocampus       F74  The Gleam Eyes\n" +
            "F5  Fuscus the Vacant Colossus    F75  The Skull Reaper\n" +
            "F6  The Irrational Cube           F98  Incarnation of the Radius\n" +
            "F10 Kagachi the Samurai Lord      F99  Heathcliff's Shadow\n" +
            "F22 The Witch of the West         F100 ??? (Your clone, 150% HP)\n" +
            "F25 The Two-Headed Giant\n" +
            "F27 The Four-Armed Giant\n" +
            "F35 Nicholas the Renegade\n" +
            "F46 The Ant Queen\n\n" +
            "Scaling curve for unlisted floors:\n" +
            "  Level = 10 + 2*floor\n" +
            "  HP    = 150 + 30*floor + 0.5*floor^2\n" +
            "  Col   = 4000 + 500*floor + 20*floor^2\n\n" +
            "COSTS\n" +
            "Durability, consumables, and allies' HP. Boss deaths cannot be\n" +
            "rolled back; Hardcore runs wipe on a boss-fight death.\n\n" +
            "TIPS\n" +
            "F74 Gleam Eyes unlocks Dual Blades; F75 Heathcliff locks Holy\n" +
            "Sword unless you defeat him. Park major quest turn-ins before\n" +
            "fighting a boss so you don't waste overflow XP.\n\n" +
            "SEE ALSO\n" +
            "[Labyrinth System] · [Unique Skill: Dual Blades] · [Unique Skill: Holy Sword] · [Field Bosses — Guaranteed Drops]")
        {
            Tags = new[] { "world", "bosses", "floors" }
        },

        new("World", "Field Bosses — Guaranteed Drops",
            "┌─ World\n" +
            "│ Topic: Field Bosses\n" +
            "│ Floors: 2, 14, 22, 25, 35, 40, 48, 49, 60, 61, 70, 77+, 80, 83, 85, 86, 87, 90, 93, 95, 97, 98\n" +
            "│ Landmark: Roaming overworld elites\n" +
            "│ Unlock: Floor entry; never spawn in labyrinths\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Field bosses are roaming elites on overworld floors. Each\n" +
            "100%-drops a named item — some Divine, some craft catalysts,\n" +
            "some Hollow-Fragment implements, and (post-FD pass) seven\n" +
            "canonical FD character weapons. Once defeated, a field boss\n" +
            "never respawns on that save.\n\n" +
            "USAGE\n" +
            "Engage in the open world — they appear on the overworld only,\n" +
            "not inside labyrinths. Escape Rope / Teleport crystals work.\n" +
            "F70+ field-boss kills also roll the Avatar Weapon Last-Attack\n" +
            "Bonus (see Avatar Weapons topic).\n\n" +
            "EFFECTS\n" +
            "  F2   Bullbous Bow           -> Bullbous Horn\n" +
            "  F14  Starlight Sentinel     -> Integral Arc Angel (IF Epic)\n" +
            "  F22  Forest King Stag       -> Kingly Antler\n" +
            "  F25  Labyrinth Warden       -> Nox Radgrid (IF Epic)\n" +
            "  F35  Magnatherium           -> Mammoth Tusk\n" +
            "  F40  Ogre Lord              -> Ogre's Cleaver\n" +
            "  F40  Phoenix of Smolder Pk  -> Flame Bow (Divine)\n" +
            "  F48  Frost Dragon           -> Crystallite Ingot\n" +
            "  F49  Nicholas Renegade      -> Returning Soul (Christmas only)\n" +
            "  F60  Kagutsuchi Fire Samurai-> Spirit Sword Kagutsuchi (FD)\n" +
            "  F61  Crimson Forneus        -> Rosso Forneus (IF Legendary)\n" +
            "  F70  Susanoo the Storm Blade-> Spirit Sword Susanoo (FD)\n" +
            "  F80  Soul Binder            -> Arcaneblade: Soul Binder (HF)\n" +
            "  F80  Pyre Lord of Heathcliff-> Flame Lord (FD Legendary)\n" +
            "  F83  Ruinous Herald         -> Fellblade: Ruinous Doom (HF)\n" +
            "  F85  Silent Edge            -> Black Lily Sword (Divine)\n" +
            "  F85  Abased Beast           -> canon HNM (Avatar 10% rate)\n" +
            "  F85  Yuuki's Echo           -> Macafitel (FD Legendary)\n" +
            "  F86  Fellaxe Revenant       -> Fellaxe: Demon's Scythe (HF)\n" +
            "  F87  Yasha the Night Demon  -> Yasha Astaroth (IF Legendary)\n" +
            "  F90  Gaou the Ox-King       -> Gaou Reginleifr (IF Legendary)\n" +
            "  F93  Banishing Ray          -> Glimmerblade: Banishing Ray (HF)\n" +
            "  F94  Ark Knight             -> canon HNM (Avatar 10% rate)\n" +
            "  F95  Warden of Stopped Hrs  -> Time Piercing (Divine)\n" +
            "  F95  Warden of Blooming Rose-> Red Rose Sword (FD Legendary)\n" +
            "  F95  Gaia Breaker           -> canon HNM (Avatar 10% rate)\n" +
            "  F96  Eternal Dragon         -> canon HNM (Avatar 10% rate)\n" +
            "  F97  Administrator's Regent -> Silvery Ruler (FD Legendary)\n" +
            "  F98  Ashen Kirito Simulacrum-> Elucidator Rouge (FD Legendary)\n\n" +
            "COSTS\n" +
            "No time limit, but field bosses scale 1.5x normal HP and deal\n" +
            "heavier telegraphed attacks. FD canon bosses scale harder\n" +
            "(up to 4.5x HP, 1.85x ATK) — see FD Field Bosses topic.\n\n" +
            "TIPS\n" +
            "Clear the overworld fully before stepping on the Labyrinth\n" +
            "entrance — missing a Divine, HF, or FD character drop is\n" +
            "painful. Christmas-only Nicholas on F49 is one of only two\n" +
            "Divine Stone of Returning Soul sources. F98 Ashen Kirito\n" +
            "coexists with Blaze Armor, so budget durability.\n\n" +
            "SEE ALSO\n" +
            "[Integral Factor Field Bosses] · [Fractured Daydream Field Bosses] · [Avatar Weapons & Last-Attack Bonus] · [Divine Object Set — Integrity Knights]")
        {
            Tags = new[] { "world", "bosses", "fractured-daydream" }
        },

        new("World", "Integral Factor Field Bosses",
            "┌─ World\n" +
            "│ Topic: Integral Factor Field Bosses\n" +
            "│ Floors: F14, F25, F61, F87, F90\n" +
            "│ Landmark: Overworld, one per IF series\n" +
            "│ Unlock: Floor entry; never respawn\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Five new field bosses gate the Integral Factor weapon series.\n" +
            "Each guarantees one signature weapon of its series; the series\n" +
            "shield drops as a secondary, and the remaining series weapons\n" +
            "fall into the floor-banded Epic/Legendary loot pool.\n\n" +
            "USAGE\n" +
            "Clear the overworld on the listed floors. These bosses scale\n" +
            "harder than standard field bosses (up to 4.5x HP, 1.85x ATK)\n" +
            "and deal heavier telegraphed attacks.\n\n" +
            "EFFECTS\n" +
            "  F14 Starlight Sentinel      Integral Arc Angel (Epic Bow)\n" +
            "  F25 Labyrinth Warden        Nox Radgrid (Epic 1H Sword)\n" +
            "  F61 Crimson Forneus         Rosso Forneus (Leg. 1H Sword)\n" +
            "  F87 Yasha the Night Demon   Yasha Astaroth (Leg. 1H Sword)\n" +
            "  F90 Gaou the Ox-King        Gaou Reginleifr (Leg. 1H Sword)\n" +
            "Yasha was moved from canon F85 to F87 to avoid collision with\n" +
            "Silent Edge (Black Lily Sword) and Abased Beast already on\n" +
            "that floor. Night Stalker and Yasha coexist on F87.\n\n" +
            "COSTS\n" +
            "No time limit. Each boss never respawns on that save, so\n" +
            "missing a clear means farming the remaining series items\n" +
            "through the floor-banded loot pool.\n\n" +
            "TIPS\n" +
            "Do NOT skip F14 Starlight Sentinel — Integral is the first\n" +
            "tier that carries through every later tier's refinement\n" +
            "upgrades. Yasha/Gaou field bosses hit in the 80-90 range\n" +
            "where an unrefined Celestial weapon is already underpowered.\n\n" +
            "SEE ALSO\n" +
            "[Integral Factor Weapon Series] · [Field Bosses — Guaranteed Drops] · [Fractured Daydream Field Bosses] · [Named Legendary Highlights]")
        {
            Tags = new[] { "world", "bosses", "integral-factor" }
        },

        new("World", "Fractured Daydream Field Bosses",
            "┌─ World\n" +
            "│ Topic: Fractured Daydream Field Bosses\n" +
            "│ Floors: F60, F70, F80, F85, F95, F97, F98\n" +
            "│ Landmark: Overworld, canon FD arc bosses\n" +
            "│ Unlock: Floor entry; never respawn\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Seven canonical field bosses gate Fractured Daydream (FD)\n" +
            "character weapons. Each 100%-drops one FD Legendary on kill,\n" +
            "moving those weapons out of the random banded loot pool into\n" +
            "fixed canon placements.\n\n" +
            "USAGE\n" +
            "Clear the overworld on the listed floors. FD bosses scale\n" +
            "harder than standard field bosses (2.8x-4.5x HP, 1.5x-1.85x\n" +
            "ATK) and deal heavier telegraphed attacks — budget crystals.\n\n" +
            "EFFECTS\n" +
            "  F60 Kagutsuchi the Fire Samurai    (2.8x HP, 1.5x ATK)\n" +
            "                                      -> Spirit Sword Kagutsuchi\n" +
            "  F70 Susanoo the Storm Blade        (3.2x HP, 1.6x ATK)\n" +
            "                                      -> Spirit Sword Susanoo\n" +
            "  F80 Pyre Lord of Heathcliff        (3.8x HP, 1.7x ATK)\n" +
            "                                      -> Flame Lord\n" +
            "  F85 Yuuki's Echo                   (4.0x HP, 1.75x ATK)\n" +
            "                                      -> Macafitel\n" +
            "  F95 Warden of the Blooming Rose    (4.3x HP, 1.8x ATK)\n" +
            "                                      -> Red Rose Sword\n" +
            "  F97 Administrator's Regent         (4.2x HP, 1.8x ATK)\n" +
            "                                      -> Silvery Ruler\n" +
            "  F98 Ashen Kirito Simulacrum        (4.5x HP, 1.85x ATK)\n" +
            "                                      -> Elucidator Rouge\n" +
            "Also canon: F55 Agil's Apprentice (quest NPC, 15 kills) hands\n" +
            "out Ground Gorge — see Fractured Daydream Character Weapons.\n\n" +
            "COSTS\n" +
            "No time limit. Each boss never respawns on that save, so\n" +
            "missing a clear means the weapon never drops on this run.\n" +
            "F97 bumped from canon F95 to space out with Warden of Stopped\n" +
            "Hours and Red Rose Warden. F98 coexists with Blaze Armor.\n\n" +
            "TIPS\n" +
            "Kagutsuchi (F60) and Susanoo (F70) are the easiest pair — Klein\n" +
            "canon kit lands here. Chain F95 → F97 → F98 on a single push\n" +
            "for three Legendary drops in two floors. Ashen Kirito drops\n" +
            "the Elucidator Rouge half of the Rouge/Flare Pulsar pair.\n\n" +
            "SEE ALSO\n" +
            "[Field Bosses — Guaranteed Drops] · [Fractured Daydream Character Weapons] · [Agil's Apprentice (F55)] · [Paired Dual-Wield Weapons]")
        {
            Tags = new[] { "world", "bosses", "fractured-daydream" }
        },

        new("World", "Run Modifiers (12 Optional Challenges)",
            "┌─ World\n" +
            "│ Topic: Run Modifiers\n" +
            "│ Floors: Applied globally at run start\n" +
            "│ Landmark: New Game modifier select screen\n" +
            "│ Unlock: First F100 clear (testing: always on)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Twelve stacked toggles that make a run harder in exchange for\n" +
            "a score multiplier (cap x10). Chosen at run start and frozen\n" +
            "for the life of the save.\n\n" +
            "USAGE\n" +
            "Toggle on the New Game screen. Active modifiers show on the HUD\n" +
            "and save into the slot summary.\n\n" +
            "EFFECTS\n" +
            "EASY (x1.15)\n" +
            "  Starless Night, Iron Rank\n" +
            "MODERATE (x1.25)\n" +
            "  Beater, Solo, Laughing Coffin\n" +
            "HARD (x1.40)\n" +
            "  Heathcliff's Gauntlet, Anti-Crystal Tyranny, Kayaba's Wager,\n" +
            "  Hollow Ingress, Naked Ingress\n" +
            "NIGHTMARE (x1.75)\n" +
            "  Gleam Eyes Echo, Sword Art Only (pure Kirito run)\n\n" +
            "COSTS\n" +
            "Each modifier bites differently: Iron Rank doubles satiety drain,\n" +
            "Hollow Ingress doubles sword-skill unlock kills, Solo forbids\n" +
            "recruits, Anti-Crystal Tyranny disables Crystal consumables.\n\n" +
            "TIPS\n" +
            "Stack compatible modifiers to approach the x10 multiplier cap.\n" +
            "Starless Night pairs well with Darkness Blade builds.\n\n" +
            "SEE ALSO\n" +
            "[Unique Skill: Darkness Blade] · [Recruitable Allies & Party System] · [Death Penalty & Hardcore] · [Save System]")
        {
            Tags = new[] { "world", "progression", "hardcore" }
        },

        new("World", "Seasonal Events",
            "┌─ World\n" +
            "│ Topic: Seasonal Events\n" +
            "│ Floors: Varies (Christmas = F49)\n" +
            "│ Landmark: Triggered by real-world date\n" +
            "│ Unlock: Event-window dates; floor-entry check\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "The game reads the real-world system clock and fires seasonal\n" +
            "events within specific date windows. Only Christmas drives a\n" +
            "full encounter today; the rest are registered hooks.\n\n" +
            "USAGE\n" +
            "Events check on floor entry. Play within the date window to\n" +
            "trigger the encounter.\n\n" +
            "EFFECTS\n" +
            "  Christmas (Dec 20-26)  Nicholas the Renegade on F49\n" +
            "                          -> Divine Stone of Returning Soul\n" +
            "                          (canon LN Vol 2 reward)\n" +
            "  New Year (Jan 1-3)      hook present\n" +
            "  Valentine's (Feb 10-17) hook present\n" +
            "  White Day (Mar 14)      hook present\n" +
            "  Tanabata (Jul 7)        hook present\n" +
            "  Summer Festival         Jul 15 - Aug 31\n" +
            "  Tsukimi (Sep 15-18)     hook present\n" +
            "  Halloween (Oct 20-Nov3) hook present\n\n" +
            "COSTS\n" +
            "None to participate. Missing the window means missing the drop.\n\n" +
            "TIPS\n" +
            "Plan a December run targeting F49 to grab the Divine Stone of\n" +
            "Returning Soul — it auto-revives you within 10 seconds of death\n" +
            "and is a lifesaver on Hardcore.\n\n" +
            "SEE ALSO\n" +
            "[Field Bosses — Guaranteed Drops] · [Potions, Crystals & Throwables] · [Death Penalty & Hardcore] · [Divine Objects]")
        {
            Tags = new[] { "world", "bosses", "divine" }
        },

        new("World", "Lindarth Town (F48)",
            "┌─ World\n" +
            "│ Topic: Lindarth Town (F48)\n" +
            "│ Floors: 48\n" +
            "│ Landmark: Settlement hub on Floor 48\n" +
            "│ Unlock: Reach F48; walk into town tiles\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Lindarth is the canon SAO blacksmithing town on Floor 48 and\n" +
            "the only place Lisbeth's Rarity 6 craft line is available.\n" +
            "Treat it as the mid-endgame crafting hub.\n\n" +
            "USAGE\n" +
            "Ascend to Floor 48 and explore until you find the Lindarth\n" +
            "settlement tiles. Bump the BrightMagenta 'L' NPC (F48 Lisbeth,\n" +
            "distinct from the F1 townsfolk) to open the R6 craft dialog.\n\n" +
            "EFFECTS\n" +
            "Lindarth houses:\n" +
            "  F48 Lisbeth (craft-only, Rarity 6 recipes)\n" +
            "  Standard floor vendor\n" +
            "  Safe-room tiles (Campfire / Anvil)\n" +
            "The Frost Dragon field boss also spawns on F48 — its\n" +
            "Crystallite Ingot drop feeds several R6 recipes.\n\n" +
            "COSTS\n" +
            "None to visit. R6 crafts cost 3M Col + rare mats each.\n\n" +
            "TIPS\n" +
            "Bank Col aggressively before reaching F48 — a single R6 craft\n" +
            "drains 3M Col. Stock Crystallite Ingots by farming the F48\n" +
            "Frost Dragon on your way in.\n\n" +
            "SEE ALSO\n" +
            "[Lisbeth — Rarity 6 Craft Line] · [Field Bosses — Guaranteed Drops] · [Town of Beginnings NPCs (F1)] · [Anvil — Repair, Enhance, Evolve, Refine]")
        {
            Tags = new[] { "world", "lisbeth", "npcs" }
        },

        new("World", "Ascending a Floor",
            "┌─ World\n" +
            "│ Topic: Ascending a Floor\n" +
            "│ Floors: All, on StairsUp step\n" +
            "│ Landmark: Stairs Up (>) after boss death\n" +
            "│ Unlock: Kill the floor boss first\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Stepping on the Stairs Up tile after the floor boss is dead runs\n" +
            "a recap sequence, applies bonuses, auto-saves, and drops you onto\n" +
            "the next floor with fresh biome + weather.\n\n" +
            "USAGE\n" +
            "Kill the floor boss, return to the overworld, walk onto Stairs\n" +
            "Up. The recap screen is automatic.\n\n" +
            "EFFECTS\n" +
            "On ascend, the following fire in order:\n" +
            "  1. Floor Recap: Kills / Items / Damage / Turns / Exploration%\n" +
            "  2. Speed Clear bonus if elapsed <= par: 50 + 30*floor Col\n" +
            "  3. Thorough Exploration (>=90%): +50*floor XP, +100*floor Col\n" +
            "  4. Achievements checked\n" +
            "  5. Floor++, auto-save writes\n" +
            "  6. Weather re-rolls, biome swaps\n" +
            "  7. Allies revive at 50% HP\n\n" +
            "Clearing F100 triggers GameWon. You are free.\n\n" +
            "COSTS\n" +
            "None. Ascent is pure gain — but overflow XP past 90%/par gates\n" +
            "is lost if you leave bonus conditions unmet.\n\n" +
            "TIPS\n" +
            "Chase the 90% exploration flag on every floor — the +XP/+Col is\n" +
            "large and compounds. Speed Clear par doubles the reward window;\n" +
            "aggressive runs are rewarded. Watch the ascend banner for Floor\n" +
            "Title promotions — they fire at F2/5/10/15/25/35/50/75/100.\n\n" +
            "SEE ALSO\n" +
            "[Floor Boss Roster — Canon Highlights] · [Floor Titles] · [Save System] · [Col Economy — How You Earn]")
        {
            Tags = new[] { "world", "progression", "xp" }
        },

        // ═══════════════════════════════════════════════════════════════
        // ── 4. Items & Weapons ─────────────────────────────────────────
        // ═══════════════════════════════════════════════════════════════

        new("Items", "Weapon Types Overview",
            "┌─ Items\n" +
            "│ Topic: Weapon Types Overview\n" +
            "│ Classes: 13 active\n" +
            "│ Weapon type: Melee, ranged, offhand\n" +
            "│ Source: All vendors and drops\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Thirteen weapon classes cover every playstyle, from fast\n" +
            "claw flurries to slow scythe reach. Each lists speed, range,\n" +
            "and the attributes it scales with. Integral Factor adds named\n" +
            "series weapons (Integral/Nox/Rosso/Yasha/Gaou) across most of\n" +
            "these classes, Infinity Moment adds 20 further canon weapons\n" +
            "— 8 LAB floor-boss drops (F85-F99) and 12 enhanceable shop\n" +
            "weapons (F76-F99) — and the Memory Defrag (MD, SAO mobile\n" +
            "game) and Fractured Daydream (FD, Bandai 2024 co-op action\n" +
            "game) imports add another 65 canon weapons across 1H Sword,\n" +
            "2H Sword, Katana, Rapier, Dagger, Mace, Axe, Bow, and Spear.\n\n" +
            "USAGE\n" +
            "Equip any class in the main-hand slot. The OffHand Shield is\n" +
            "the only dedicated offhand until Dual Blades unlocks.\n\n" +
            "EFFECTS\n" +
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
            "  Shield        OffHand — blocks + armor, 3 refine slots\n\n" +
            "COSTS\n" +
            "None directly — cost comes from Col at the vendor.\n\n" +
            "TIPS\n" +
            "Match the class to your attribute bias — Str users chew\n" +
            "through Axe/Mace/2H Sword, Dex users should bee-line to\n" +
            "Rapier or Dagger proficiency. Every weapon and shield gets 3\n" +
            "Refinement slots — see the Refinement System topic.\n\n" +
            "SEE ALSO\n" +
            "[Material Tiers (Baseline)] · [Integral Factor Weapon Series] · [Infinity Moment Last Attack Bonus Weapons] · [Infinity Moment Shop Weapons] · [Memory Defrag Originals] · [Fractured Daydream Character Weapons] · [Weapon Refinement System]")
        {
            Tags = new[] { "weapons", "equipment", "integral-factor" }
        },

        new("Items", "Material Tiers (Baseline)",
            "┌─ Items\n" +
            "│ Topic: Material Tiers (Baseline)\n" +
            "│ Tiers: Iron -> Celestial (5 steps)\n" +
            "│ Weapon type: All mundane classes\n" +
            "│ Source: Vendors, floor drops\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Every weapon class has a 5-tier mundane progression from Iron\n" +
            "up to Celestial. Armor follows the same ladder.\n\n" +
            "USAGE\n" +
            "Upgrade at vendors as you cross the listed floor thresholds.\n" +
            "Material tier sits beneath named Legendaries and Divine gear.\n\n" +
            "EFFECTS\n" +
            "  Common Iron         Lv1    100 Col    +8 ATK\n" +
            "  Uncommon Steel      Lv10   250 Col    +15 ATK\n" +
            "  Rare Mythril        Lv25   800 Col    +25 ATK\n" +
            "  Epic Adamantite     Lv50   2500 Col   +45 ATK\n" +
            "  Legendary Celestial Lv75   6000 Col   +70 ATK\n" +
            "Stats, durability, and value roughly double each tier. Armor\n" +
            "ladder: Leather/Iron/Steel/Mythril/Adamantite/Celestial.\n" +
            "Refinement Ingots (Common/Rare/Epic/Legendary) are a separate\n" +
            "material family — they socket into weapon/shield slots rather\n" +
            "than forging a new item. See Refinement Ingots for the list.\n" +
            "Enhancement Ores (7 types, Uncommon, 85-120 Col) are a third\n" +
            "family — each Anvil Enhance level consumes one ore, and the\n" +
            "ore picked biases the level's bonus into a specific stat. See\n" +
            "Enhancement Ores System for details.\n\n" +
            "COSTS\n" +
            "Col scales sharply at the top — Celestial is 60x the Iron\n" +
            "price.\n\n" +
            "TIPS\n" +
            "Don't skip a tier — the Col spend curve assumes each bracket\n" +
            "gets bought; jumping straight to Adamantite burns your purse\n" +
            "and leaves you under-enhanced.\n\n" +
            "SEE ALSO\n" +
            "[Weapon Types Overview] · [Named Legendary Highlights] · [Refinement Ingots] · [Enhancement Ores System]")
        {
            Tags = new[] { "weapons", "equipment", "rarity" }
        },

        new("Items", "Named Legendary Highlights",
            "┌─ Items\n" +
            "│ Topic: Named Legendary Highlights\n" +
            "│ Tier: Legendary (hand-placed)\n" +
            "│ Weapon type: Various\n" +
            "│ Source: Named bosses and quests\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Legendary weapons are hand-placed drops tied to specific\n" +
            "characters or encounters — never rolled from random loot.\n\n" +
            "USAGE\n" +
            "Earn via the listed source. Each carries a bespoke\n" +
            "SpecialEffect alongside Legendary-tier base stats.\n\n" +
            "EFFECTS\n" +
            "  Elucidator        1H Sword, SkillCooldown-1       Kirito signature\n" +
            "  Dark Repulser     1H Sword, CritHeal+5            Kirito signature\n" +
            "  Remains Heart     1H Sword, SkillCooldown-1       Lisbeth masterwork\n" +
            "  Liberator         1H Sword, BlockChance+15        Heathcliff\n" +
            "  Lambent Light     Rapier                          Asuna\n" +
            "  Radiant Light     Rapier, CritRate+20             Asuna post-game\n" +
            "  Mother's Rosario  Rapier, ComboBonus+50, 11-hit   Yuuki\n" +
            "  Karakurenai       Katana, BackstabDmg+50          Klein\n" +
            "  Kagenui           Katana                          Klein upgrade\n" +
            "  Argo's Claws      Dagger, BackstabDmg+30          Argo quest reward\n" +
            "  Mjolnir           Mace, StunChance+25             Divine apex\n" +
            "  Rosso Forneus     1H Sword (F61)                  IF Rosso series\n" +
            "  Yasha Astaroth    1H Sword (F87)                  IF Yasha series\n" +
            "  Yasha Oratorio    Katana (F87)                    IF Yasha series\n" +
            "  Gaou Reginleifr   1H Sword (F90+)                 IF Gaou series\n" +
            "  Gaou Oratorio     Katana (F90+)                   IF Gaou series\n" +
            "  Avidya Samsara    1H Sword, craft-only            Lisbeth R6\n" +
            "  Marginless Blade  1H Sword, craft-only            Lisbeth R6\n" +
            "  Majestic Lord     Mace (Deliverer), craft-only    Lisbeth R6\n" +
            "  Giga Disaster     Axe (Hecatomb), craft-only      Lisbeth R6\n" +
            "  Saphir Avatar     Scimitar, F70+ Last-Attack      Avatar drop\n" +
            "  Absoludia Avatar  2H Sword, F70+ Last-Attack      Avatar drop\n" +
            "IM LAB highlights (non-enhanceable, F85+ 100% drop):\n" +
            "  Saku              Katana (F94), NightDamage\n" +
            "  Lunatic Roof      Spear (F98), Lunacy\n" +
            "  Artemis           Bow (F99), PiercingShot\n" +
            "IM Shop highlights (enhanceable via Anvil + ores):\n" +
            "  Muramasa          Katana (F86-99), Bleed — Legendary\n" +
            "  Noctis Strasse    Rapier (F86-99), Bleed — Legendary\n" +
            "MD Originals (Memory Defrag mobile canon, see MD Originals topic):\n" +
            "  Sword of Diva     1H Sword, AoE signature         MD exclusive\n" +
            "  Sword of Causality 2H Sword, ArmorPierce          MD exclusive\n" +
            "  Shining Nemesisz  Katana, HolyDamage              MD exclusive\n" +
            "  Espada of Sword Dance Rapier, multi-thrust         MD exclusive\n" +
            "MD Alicization extras (fills canon Underworld gaps):\n" +
            "  Unfolding Truth Fragrant Olive Sword  Alice MD-awakened\n" +
            "  Red Rose Sword    1H Sword, pairs with Night Sky  Kirito\n" +
            "  Black Iron Dual Swords A & B          Underworld Kirito pair\n" +
            "FD Character Core (Fractured Daydream signature drops):\n" +
            "  Elucidator Rouge  1H Sword                        Kirito FD\n" +
            "  Murasama G4       Katana                          Kirito FD\n" +
            "  Spirit Sword Susanoo  Katana                      Klein FD\n" +
            "  Golden Osmanthus Sword 1H Sword                   Alice FD\n" +
            "  Flame Lord        2H Sword                        Heathcliff FD\n" +
            "  Grida Replicant   Mace                            Lisbeth FD\n" +
            "  Excalibur Oberon  1H Sword                        Oberon FD\n" +
            "  Silvery Ruler     1H Sword                        Administrator FD\n" +
            "  Macafitel         Rapier                          Yuuki FD\n\n" +
            "COSTS\n" +
            "None — the encounter itself is the cost. Lisbeth R6 crafts\n" +
            "cost 3M Col + rare mats each (see Lisbeth craft topic).\n" +
            "IM LAB weapons carry the IsEnhanceable=false flag — high base\n" +
            "stats, no scaling. IM Shop weapons enhance normally. MD/FD\n" +
            "originals drop from floor-banded loot pools.\n\n" +
            "TIPS\n" +
            "Mjolnir is flagged as Divine apex; pair it with stun-heavy\n" +
            "skills for lockdown. Dual Blades users should chase both\n" +
            "Elucidator and Dark Repulser (or the FD Elucidator Rouge +\n" +
            "Chaos Raider Dual pair). The three IF Legendary series (Rosso/\n" +
            "Yasha/Gaou) cover F61-F100. With Lisbeth's 18-recipe R6 line,\n" +
            "Avatar Weapons, the 20 Infinity Moment additions, and the new\n" +
            "MD/FD canon drops, the F50+ arsenal spans every SAO game.\n\n" +
            "SEE ALSO\n" +
            "[Memory Defrag Originals] · [MD Alicization Canonical Extras] · [Fractured Daydream Character Weapons] · [Elemental Weapon Variants] · [Integral Factor Weapon Series] · [Infinity Moment Last Attack Bonus Weapons] · [Infinity Moment Shop Weapons] · [Lisbeth — Rarity 6 Craft Line] · [Avatar Weapons & Last-Attack Bonus]")
        {
            Tags = new[] { "weapons", "rarity", "integral-factor" }
        },

        new("Items", "Divine Object Set — Integrity Knights",
            "┌─ Items\n" +
            "│ Topic: Divine Object Set — Integrity Knights\n" +
            "│ Tier: Divine (top of 16 total)\n" +
            "│ Weapon type: Knight-themed set\n" +
            "│ Source: Canon boss / quest hand-placed\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Seven canon Integrity Knight swords make up the non-chain\n" +
            "half of the 16-piece Divine Object roster. Each is locked to\n" +
            "a specific encounter or quest.\n\n" +
            "USAGE\n" +
            "Clear the listed boss or quest to claim the weapon. Drops log\n" +
            "with the Divine BrightRed line and diamond glyph.\n\n" +
            "EFFECTS\n" +
            "  Night Sky Sword       Kirito     F99 Heathcliff's Shadow   ArmorPierce+30\n" +
            "  Blue Rose Sword       Eugeo      F20 Absolut the Monarch   Freeze+20\n" +
            "  Fragrant Olive Sword  Alice      Selka's quest (F65)       HolyAoE+15, SD+15\n" +
            "  Time Piercing Sword   Bercouli   F95 Warden of Stopped Hrs ExecuteThreshold+25\n" +
            "  Black Lily Sword      Sheyta     F85 The Silent Edge       SeveringStrike+50\n" +
            "  Conflagrant Flame Bow Deusolbert F40 Phoenix of Smolder P. Burn+30\n" +
            "  Heaven-Piercing Blade Fanatio    Azariya's quest (F50)     PiercingBeam+30 Rng2\n\n" +
            "COSTS\n" +
            "Divine gear is unbreakable, so no Anvil repair cost applies.\n\n" +
            "TIPS\n" +
            "Alice's Fragrant Olive and Fanatio's Heaven-Piercing Blade\n" +
            "both come from quests rather than boss kills — don't skip\n" +
            "Selka and Azariya's questlines on your climb.\n\n" +
            "SEE ALSO\n" +
            "[Divine Objects] · [Selka the Novice (F65)] · [Sister Azariya (F50)]")
        {
            Tags = new[] { "weapons", "divine", "rarity" }
        },

        new("Items", "Weapon Evolution Chains",
            "┌─ Items\n" +
            "│ Topic: Weapon Evolution Chains\n" +
            "│ Tier: T1 Rare -> T4 Divine\n" +
            "│ Weapon type: 9 of 13 classes\n" +
            "│ Source: Secret Shrine + Anvil Evolve\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Nine of the 13 weapon classes have a 4-tier evolution chain.\n" +
            "T1 is a free Secret Shrine find; each step up is crafted at\n" +
            "the Anvil from accumulated catalysts.\n\n" +
            "USAGE\n" +
            "Loot the T1 seed at a Secret Shrine, then return to the Anvil\n" +
            "with catalysts to step up. The chain weapon must be equipped\n" +
            "to evolve.\n\n" +
            "EFFECTS\n" +
            "  T1 (Rare)       Found at Secret Shrine — free\n" +
            "  T2 (Epic)       Craft: 3 chain catalyst\n" +
            "  T3 (Legendary)  Craft: 8 chain catalyst\n" +
            "  T4 (Divine)     Craft: 20 chain catalyst + 1 rare peak material\n" +
            "Enhancement level (+N) is preserved through evolution. Old\n" +
            "weapons are kept in backpack after evolving, not destroyed.\n\n" +
            "COSTS\n" +
            "Catalysts are farmed from specific mob families (see Chain\n" +
            "Catalysts topic).\n\n" +
            "TIPS\n" +
            "Enhance the T1 to +7 or higher BEFORE the T2 craft — the\n" +
            "enhancement carries forward and saves Anvil attempts later.\n\n" +
            "SEE ALSO\n" +
            "[Evolution Chain Table] · [Chain Catalysts — by Weapon Type] · [Secret Shrines (T1 Chain Weapons)]")
        {
            Tags = new[] { "weapons", "evolution", "crafting" }
        },

        new("Items", "Evolution Chain Table",
            "┌─ Items\n" +
            "│ Topic: Evolution Chain Table\n" +
            "│ Tier: Per-class names T1-T4\n" +
            "│ Weapon type: 9 chained classes\n" +
            "│ Source: Anvil Evolve\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Full name table for every chain weapon, T1 through T4. Each\n" +
            "class has a unique four-name progression ending in a Divine\n" +
            "capstone.\n\n" +
            "USAGE\n" +
            "Use this as a reference sheet when planning catalyst spend —\n" +
            "know which T4 apex you're aiming for before you commit.\n\n" +
            "EFFECTS\n" +
            "Weapon    T1               T2               T3               T4 (Divine)\n" +
            "-----------------------------------------------------------------------\n" +
            "1H Sword  Final Espada     Asmodeus         Final Avalanche  Tyrfing\n" +
            "Rapier    Prima Sabre     Pentagramme      Charadrios       Hexagramme\n" +
            "Scimitar  Moonstruck Saber Diablo Esperanza Iblis            Satanachia\n" +
            "Dagger    Heated Razor     Valkyrie         Misericorde      Iron Maiden\n" +
            "Mace      Lunatic Press    Nemesis          Yggdrasil        Mjolnir\n" +
            "Katana    Matamon          Shishi-Otoshi    Shichishito      Masamune\n" +
            "2H Sword  Matter Dissolver Titan's Blade    Ifrit            Ascalon\n" +
            "Axe       Bardiche         Archaic Murder   Nidhogg's Fang   Ouroboros\n" +
            "Spear     Heart Piercer    Trishula         Vijaya           Caladbolg\n\n" +
            "COSTS\n" +
            "See the chain crafting costs in Weapon Evolution Chains.\n\n" +
            "TIPS\n" +
            "Names repeat from the Named Legendary list where a class\n" +
            "converges (Mjolnir, Masamune). Either source grants the same\n" +
            "weapon statline.\n\n" +
            "SEE ALSO\n" +
            "[Weapon Evolution Chains] · [Chain Catalysts — by Weapon Type] · [Named Legendary Highlights]")
        {
            Tags = new[] { "weapons", "evolution", "rarity" }
        },

        new("Items", "Chain Catalysts — by Weapon Type",
            "┌─ Items\n" +
            "│ Topic: Chain Catalysts — by Weapon Type\n" +
            "│ Tier: Rare crafting material\n" +
            "│ Weapon type: 9 chained classes\n" +
            "│ Source: Mob family drops\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Each chain weapon consumes a unique catalyst farmed from a\n" +
            "specific mob family. T3 -> T4 also requires one Rare Peak\n" +
            "material.\n\n" +
            "USAGE\n" +
            "Farm the listed mob family until you have enough catalysts,\n" +
            "then visit the Anvil to evolve. Peak materials drop from\n" +
            "high-tier boss encounters.\n\n" +
            "EFFECTS\n" +
            "  1H Sword  Demonic Sigil      (humanoid mobs ~3%)\n" +
            "  Rapier    Geometric Shard    (construct mobs)\n" +
            "  Scimitar  Infernal Gem       (dragon mobs)\n" +
            "  Dagger    Valkyrie Feather   (insect mobs)\n" +
            "  Mace      Lunar Core         (undead mobs)\n" +
            "  Katana    Oni Ash            (humanoid mobs)\n" +
            "  2H Sword  Titan Fragment     (construct mobs)\n" +
            "  Axe       Nidhogg Scale      (dragon mobs)\n" +
            "  Spear     Trishula Tip       (elemental mobs)\n" +
            "Rare Peak (T3->T4): Dragon Scale, Crystallite Ingot, Bat\n" +
            "Wing, Flame Core, Venom Gland, Mithril Trace, Ectoplasm.\n\n" +
            "COSTS\n" +
            "T2 = 3, T3 = 8, T4 = 20 catalysts (+ 1 peak material at T4).\n\n" +
            "TIPS\n" +
            "Several catalyst families overlap — farming humanoid mobs\n" +
            "feeds both 1H Sword and Katana chains, making those two\n" +
            "chains efficient co-grinds.\n\n" +
            "SEE ALSO\n" +
            "[Weapon Evolution Chains] · [Evolution Chain Table] · [Field Bosses — Guaranteed Drops]")
        {
            Tags = new[] { "weapons", "evolution", "crafting" }
        },

        new("Items", "Anvil — Repair, Enhance, Evolve, Refine",
            "┌─ Items\n" +
            "│ Topic: Anvil — Repair, Enhance, Evolve, Refine\n" +
            "│ Tier: All equipment\n" +
            "│ Weapon type: All + armor + accessories\n" +
            "│ Source: Anvil workstation\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "The Anvil is the one-stop shop for gear upkeep: restore\n" +
            "durability, push +N enhancement levels, evolve chain weapons,\n" +
            "and socket Refinement Ingots into weapons and shields.\n\n" +
            "USAGE\n" +
            "Interact with an Anvil to open the four services. Enhance,\n" +
            "Evolve, and Refine all confirm via preview before committing.\n\n" +
            "EFFECTS\n" +
            "REPAIR ALL — Restores all equipped gear to cap:\n" +
            "  50 + 10*floor + 5*enhancementLevel durability.\n" +
            "ENHANCE (+0 to +10) — Each level now consumes 1 Enhancement\n" +
            "Ore (7 types) plus the Col cost, and the ore picked biases\n" +
            "that level's BonusPerLevel into a specific stat — Crimson\n" +
            "Flame = Attack, Adamant = Defense, Crust = Vitality, Sharp\n" +
            "Blade = Dexterity, Flowing Water = Speed, Wind Flower =\n" +
            "Agility, Ash White = Intelligence. Baseline: weapons +3 ATK\n" +
            "per level on Crimson Flame, armor +2 DEF on Adamant, other\n" +
            "ores redirect the bump. Success rates: +1 95%, +5 60%, +7\n" +
            "40%, +10 10%. From +7 up, failed attempts have a 30% chance\n" +
            "to DOWNGRADE by 1 (SAO canon). IM LAB weapons cannot be\n" +
            "enhanced — the menu shows [SEALED].\n" +
            "EVOLVE — Swaps the equipped chain weapon, keeps the old in\n" +
            "backpack, and preserves enhancement level AND ore history.\n" +
            "REFINE — Socket an Ingot into one of 3 slots on a weapon or\n" +
            "shield. Override-only: socketing into an occupied slot\n" +
            "destroys the previous ingot. Divines cannot be refined.\n\n" +
            "COSTS\n" +
            "Repair: 50 + 25*floor Col. Enhance attempt: (level+1)*100 +\n" +
            "50*floor Col plus 1 Enhancement Ore of the chosen bias.\n" +
            "Evolve: catalysts per Weapon Evolution Chains. Refine: 1/1/\n" +
            "2/3/5 Red Hot Ore scaling with ingot rarity (Common through\n" +
            "Legendary).\n\n" +
            "TIPS\n" +
            "Stop at +6 on everything cheap — the +7 downgrade risk makes\n" +
            "further pushes economically dangerous without a Divine safety\n" +
            "net. Refine last: ingots are cheaper to swap than enhancement\n" +
            "is to rebuild. Plan ore spend by farming themed mob biomes\n" +
            "(Ash White from hollow mobs F76+, Crimson Flame from fire/demon,\n" +
            "etc.) — see Enhancement Ores System.\n\n" +
            "SEE ALSO\n" +
            "[Weapon Refinement System] · [Weapon Evolution Chains] · [Refinement Ingots] · [Enhancement Ores System] · [Non-Enhanceable LAB Weapons] · [Lisbeth — Rarity 6 Craft Line]")
        {
            Tags = new[] { "crafting", "anvil", "refinement" }
        },

        new("Items", "Integral Factor Weapon Series",
            "┌─ Items\n" +
            "│ Topic: Integral Factor Weapon Series\n" +
            "│ Tier: Epic (F14/25) -> Legendary (F61/87/90+)\n" +
            "│ Count: 23 weapons across 5 series\n" +
            "│ Source: Canon SAO: Integral Factor MMO\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Five named weapon series imported from Bandai Namco's SAO:\n" +
            "Integral Factor. Each series spans multiple weapon types, and\n" +
            "every series' field boss guarantees one signature drop with\n" +
            "the rest banded across the floor's Epic/Legendary loot pool.\n\n" +
            "USAGE\n" +
            "Hunt the series' field boss on the listed floor. Series\n" +
            "shield drops as a secondary from the same boss.\n\n" +
            "EFFECTS\n" +
            "INTEGRAL (F14, Epic) — Arc Angel (Bow), Radgrid (1H Sword),\n" +
            "  Gusion (Rapier), After Glow (2H Sword), Fermat (Shield).\n" +
            "NOX (F25, Epic) — Nocturne (Dagger), Radgrid (1H Sword),\n" +
            "  Gusion (Rapier), Arc Angel (Bow), After Glow (2H Sword),\n" +
            "  Nox Fermat (Shield).\n" +
            "ROSSO (F61, Legendary) — Forneus (1H Sword), Albatross (Bow),\n" +
            "  Sigrun (Spear), Rhapsody (Rapier), Dominion (Axe), Rosso\n" +
            "  Aegis (Shield).\n" +
            "YASHA (F87, Legendary) — Astaroth (1H Sword), Oratorio\n" +
            "  (Katana), Envy (Dagger), Yasha Kavacha (Shield).\n" +
            "GAOU (F90+, Legendary) — Reginleifr (1H Sword), Oratorio\n" +
            "  (Katana), Gaou Tatari (Shield).\n\n" +
            "COSTS\n" +
            "Field-boss fights only. The non-signature drops roll through\n" +
            "normal Epic/Legendary loot odds on their floor band.\n\n" +
            "TIPS\n" +
            "All 23 IF weapons + 5 shields accept Refinement Ingots. An\n" +
            "F14 Integral weapon with 3 Epic ingots can outperform an\n" +
            "unrefined F40 Legendary for many floors.\n\n" +
            "SEE ALSO\n" +
            "[Integral Factor Field Bosses] · [Weapon Refinement System] · [Named Legendary Highlights]")
        {
            Tags = new[] { "weapons", "integral-factor", "rarity" }
        },

        new("Items", "Weapon Refinement System",
            "┌─ Items\n" +
            "│ Topic: Weapon Refinement System\n" +
            "│ Slots: 3 per weapon, shield, or off-hand\n" +
            "│ Rule: Override-only (destroys prior ingot)\n" +
            "│ Source: Anvil Refine service\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Weapons and shields each carry 3 Refinement slots. Socket an\n" +
            "Ingot into a slot to fold its bonuses into the equipped\n" +
            "piece. Socketing into an occupied slot DESTROYS the previous\n" +
            "ingot — plan swaps deliberately.\n\n" +
            "USAGE\n" +
            "Visit the Anvil, pick Refine. Select the target (weapon,\n" +
            "off-hand sword, or shield), pick a slot (1/2/3), choose an\n" +
            "ingot from inventory, preview the +/- effect, confirm.\n\n" +
            "EFFECTS\n" +
            "Slots unlock by spending Red Hot Ore. Socketing consumes one\n" +
            "ingot and scales Ore cost by the ingot's rarity:\n" +
            "  Common ingot     -> 1 Red Hot Ore\n" +
            "  Rare ingot       -> 1 Red Hot Ore (Note: 1st slot free)\n" +
            "  Epic ingot       -> 2-3 Red Hot Ore\n" +
            "  Legendary ingot  -> 5 Red Hot Ore\n" +
            "DIVINE weapons cannot be refined — their unbreakable nature\n" +
            "is the tradeoff for the missing socket surface.\n\n" +
            "COSTS\n" +
            "One Red Hot Ore per Common socket, scaling to 5 for\n" +
            "Legendary. The displaced ingot is destroyed, not returned.\n\n" +
            "TIPS\n" +
            "Build two refinement loadouts per weapon class — a farm\n" +
            "setup (Sharpening/Keen for kill speed) and a boss setup\n" +
            "(Guardian/Sovereign for survival). Carry both ingot sets.\n\n" +
            "SEE ALSO\n" +
            "[Refinement Ingots] · [Anvil — Repair, Enhance, Evolve, Refine] · [Integral Factor Weapon Series]")
        {
            Tags = new[] { "refinement", "crafting", "anvil" }
        },

        new("Items", "Refinement Ingots",
            "┌─ Items\n" +
            "│ Topic: Refinement Ingots\n" +
            "│ Count: 12 across Common/Rare/Epic/Legendary\n" +
            "│ Source: Mob drops (~3%), rare mining yields\n" +
            "│ Use: Socket via Anvil Refine\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Twelve canon-flavored ingots span four rarity tiers. Each\n" +
            "carries a primary stat gain and a tradeoff penalty; Rare and\n" +
            "above add a third or fourth stat line. Legendary ingots are\n" +
            "the only multi-stat picks with a trivial downside.\n\n" +
            "USAGE\n" +
            "Farm from qualifying mobs (low-rate drop) or mine high-tier\n" +
            "veins once the Mining system lands. Socket at the Anvil.\n\n" +
            "EFFECTS\n" +
            "COMMON (big tradeoff)\n" +
            "  Sharpening   ATK +5     / SPD -3\n" +
            "  Warden       DEF +8     / AGI -3\n" +
            "  Hunter       DEX +3     / ATK -3\n" +
            "  Lunar        SkillDmg+3 / DEF -2\n" +
            "RARE (balanced, 3-stat)\n" +
            "  Keen         ATK +10, DEX +2 / SPD -3\n" +
            "  Guardian     DEF +15, VIT +5 / ATK -4\n" +
            "  Swiftstrike  SPD +5, DEX +4  / STR -3\n" +
            "  Spellbind    SkillDmg+6, INT+3 / DEF -3\n" +
            "EPIC (small downside, 4-stat)\n" +
            "  Chimeric     ATK +15, STR+5, DEX+3 / SPD -4\n" +
            "  Sovereign    DEF +22, VIT+3, END+2 / DEX -2\n" +
            "  Vanguard     ATK +12, VIT+4        / DEX -2\n" +
            "LEGENDARY (multi-stat, minimal downside)\n" +
            "  Astral       ATK +15, DEX+10 / DEF -2\n\n" +
            "COSTS\n" +
            "Drop rate is ~3% from qualifying mobs. Legendary Astral\n" +
            "ingots are boss-loot or peak mining yields only.\n\n" +
            "TIPS\n" +
            "Three Guardian Ingots in a shield give +45 DEF — roughly a\n" +
            "full Celestial armor tier of extra defense on top of the\n" +
            "shield itself. Stack identical Commons early, mix Rare+ for\n" +
            "flexible builds.\n\n" +
            "SEE ALSO\n" +
            "[Weapon Refinement System] · [Anvil — Repair, Enhance, Evolve, Refine] · [Material Tiers (Baseline)]")
        {
            Tags = new[] { "refinement", "ingots", "crafting" }
        },

        new("Items", "Lisbeth — Rarity 6 Craft Line",
            "┌─ Items\n" +
            "│ Topic: Lisbeth — Rarity 6 Craft Line\n" +
            "│ Tier: Rarity 6 (craft-only, above Legendary)\n" +
            "│ Weapon type: 18 recipes across most classes\n" +
            "│ Source: F48 Lindarth Lisbeth NPC\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Lisbeth's canon SAO blacksmithing arsenal. Eighteen named\n" +
            "Rarity 6 weapons, each crafted at the F48 Lindarth Lisbeth NPC.\n" +
            "Craft-only — these weapons never drop from mobs or bosses. Each\n" +
            "recipe can be crafted multiple times, self-gated by Col cost.\n\n" +
            "USAGE\n" +
            "Reach F48, find Lindarth, bump the BrightMagenta 'L' NPC to\n" +
            "open the craft dialog. Recipes list AVAILABLE / NEED / CRAFTED\n" +
            "status per row. Select AVAILABLE rows to spend mats + Col.\n\n" +
            "EFFECTS\n" +
            "Eighteen R6 recipes (summary, class in parens):\n" +
            "  Variable V Vice                    Liberator: Astral Legion\n" +
            "  Ogreblade: Over the Cross          Deliverer: Majestic Lord\n" +
            "  Championfoil: Radiant Chariot      Glimmerspine: Silver Bullet\n" +
            "  Crescentblade: Original Sin        Notes' End Trinity\n" +
            "  Godslayer: Tattered Hope           Heavenslance: Elpis Order\n" +
            "  Dictator's Punisher                Hecatomb Axe: Giga Disaster\n" +
            "  Eldark Radius Sigma                Ingurgitator: Belzericht\n" +
            "  Photon Hammer: XP Smasher          Ambitious Juggernaut\n" +
            "  Avidya Samsara Blade               Marginless Blade\n\n" +
            "If inventory is full at craft time, Lisbeth REFUNDS mats + Col\n" +
            "and aborts the craft — no silent loss.\n\n" +
            "COSTS\n" +
            "Each recipe costs 3,000,000 Col plus 3-5 rare materials. Mat\n" +
            "sources cross-cut catalyst drops, field-boss loot, and\n" +
            "Crystallite Ingots — plan a multi-floor farm run per craft.\n\n" +
            "TIPS\n" +
            "Bank 6M+ Col before every Lindarth run so you can craft two in\n" +
            "a session. These weapons slot 3 Refinement ingots each — pair\n" +
            "with Astral or Chimeric for F70+ burst builds.\n\n" +
            "SEE ALSO\n" +
            "[Lindarth Town (F48)] · [Weapon Refinement System] · [Anvil — Repair, Enhance, Evolve, Refine] · [Named Legendary Highlights]")
        {
            Tags = new[] { "lisbeth", "crafting", "weapons" }
        },

        new("Items", "Avatar Weapons & Last-Attack Bonus",
            "┌─ Items\n" +
            "│ Topic: Avatar Weapons & Last-Attack Bonus\n" +
            "│ Tier: Legendary (8 type-locked drops)\n" +
            "│ Weapon type: One per weapon class (OHS excluded)\n" +
            "│ Source: F70+ field-boss last-attack roll\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Avatar Weapons are canon-flavor Hollow Fragment apex drops. One\n" +
            "Avatar exists per weapon type (except 1H Sword, which has no\n" +
            "canon Avatar). The drop is gated by the LAST-ATTACK BONUS: only\n" +
            "the killing blow's weapon type can receive an Avatar.\n\n" +
            "USAGE\n" +
            "Land the killing blow on a F70+ field boss using the weapon\n" +
            "type whose Avatar you want. The drop rolls automatically —\n" +
            "2% on any F70+ field boss, 10% on canon HNM bosses.\n\n" +
            "EFFECTS\n" +
            "Eight Avatars, type-locked:\n" +
            "  Ishvalca Avatar      Rapier\n" +
            "  Genocide Avatar      Dagger\n" +
            "  Saphir Avatar        Scimitar\n" +
            "  Burning Haze Avatar  Katana\n" +
            "  Lord Burster Avatar  2H Axe\n" +
            "  Absoludia Avatar     2H Sword\n" +
            "  Asleigeon Avatar     Spear\n" +
            "  Ijelfur Avatar       Mace\n\n" +
            "Canon HNM (10% rate): F85 Abased Beast, F94 Ark Knight, F95\n" +
            "Gaia Breaker, F96 Eternal Dragon. All other F70+ field bosses\n" +
            "use the 2% rate. 1H Sword has NO canon Avatar — OHS killing\n" +
            "blows skip the roll entirely.\n" +
            "DISTINCT FROM IM LAB: the Infinity Moment Last Attack Bonus\n" +
            "drops on F85+ FLOOR BOSSES at 100% are a separate hook (see\n" +
            "Infinity Moment Last Attack Bonus Weapons). Avatar rolls fire\n" +
            "on FIELD bosses at 2%/10%. Both hooks can trigger on the same\n" +
            "climb — they don't compete.\n\n" +
            "COSTS\n" +
            "None beyond the field-boss fight itself. A wrong-weapon last\n" +
            "hit forfeits the Avatar roll for that encounter forever (field\n" +
            "bosses don't respawn).\n\n" +
            "TIPS\n" +
            "Target a specific Avatar by wielding its weapon type when\n" +
            "closing out canon HNM fights — 10% per HNM is a real chance at\n" +
            "the type you want. Dual Blades users should finish with the\n" +
            "off-hand weapon type they care about.\n\n" +
            "SEE ALSO\n" +
            "[Field Bosses — Guaranteed Drops] · [Named Legendary Highlights] · [Weapon Types Overview] · [Infinity Moment Last Attack Bonus Weapons] · [Hollow Fragment HNM Questgivers (F79-F99)]")
        {
            Tags = new[] { "avatar", "weapons", "hollow-fragment" }
        },

        new("Items", "Hollow Area Uniques",
            "┌─ Items\n" +
            "│ Topic: Hollow Area Uniques\n" +
            "│ Tier: Legendary (5 canon HF items)\n" +
            "│ Weapon type: Spread across classes\n" +
            "│ Source: Floor-banded rare drop pool\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Five canon Hollow Fragment weapons imported from the Hollow\n" +
            "Area subplot (an alt-dimension Aincrad fragment in HF canon).\n" +
            "They roll as ELEVATED-chance rare drops via the floor-banded\n" +
            "registered-loot pool — not guaranteed, but the pool weighting\n" +
            "favors them on the listed floor bands.\n\n" +
            "USAGE\n" +
            "Farm mobs within the listed floor band. Drops echo through the\n" +
            "standard LootGenerator.FloorBandedRegisteredLoot system — you\n" +
            "may see them from chests, kills, or boss loot rolls.\n\n" +
            "EFFECTS\n" +
            "  F30-40 band   Traitorblade: Argute Brand (1H Sword) @ F35\n" +
            "  F50-60 band   Shroudbow: Star Stitcher (Bow)        @ F55\n" +
            "  F65-75 band   Reaper Scythe (Scythe)                 @ F70\n" +
            "  F78-88 band   Fake Sword Velocious Brain (1H Sword)  @ F82\n" +
            "  F92-99 band   Saintblade: Ragnarok (2H Sword)        @ F95\n\n" +
            "COSTS\n" +
            "None guaranteed — the pool is probabilistic. Expect multiple\n" +
            "farm runs per piece.\n\n" +
            "TIPS\n" +
            "Camp the middle of each band (F35/F55/F70/F82/F95) rather than\n" +
            "the edges — the pool weight peaks there. Hollow Area Uniques\n" +
            "fill the gaps between canon HF questgiver rewards and the\n" +
            "Avatar Weapon roster.\n\n" +
            "SEE ALSO\n" +
            "[Hollow Fragment HNM Questgivers (F79-F99)] · [Avatar Weapons & Last-Attack Bonus] · [Named Legendary Highlights] · [Rarity Tiers & Drop Rates]")
        {
            Tags = new[] { "hollow-area", "weapons", "hollow-fragment" }
        },

        new("Items", "Implement System Weapons (F77-F99)",
            "┌─ Items\n" +
            "│ Topic: Implement System Weapons (F77-F99)\n" +
            "│ Tier: Legendary (canon HF arsenal)\n" +
            "│ Weapon type: 25 weapons across most classes\n" +
            "│ Source: HF questgiver NPCs on each floor\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "The Implement System is Hollow Fragment's canonical late-game\n" +
            "weapon-grant framework: each F77-F99 floor hosts an NPC who\n" +
            "hands out a named HF weapon after a floor-locked kill quest.\n" +
            "The system is now content-complete — 25 Implement weapons span\n" +
            "the full F77-F99 stretch.\n\n" +
            "USAGE\n" +
            "Reference the Hollow Fragment HNM Questgivers topic for the\n" +
            "full floor-by-floor NPC + kill-count roster. Bump the NPC on\n" +
            "their floor, rack the kill count, return for the weapon.\n\n" +
            "EFFECTS\n" +
            "Coverage map (new F80-F99 weapons marked *):\n" +
            "  F79  Infinite Ouroboros          F89  (reserved)\n" +
            "  F80  Jato Onikirimaru *          F90  Eurynome's Holy Sword\n" +
            "  F80* Arcaneblade: Soul Binder    F91  Saintspear Rhongomyniad\n" +
            "  F81  Fiendblade Deathbringer     F92* Aurumbrand: Hauteclaire\n" +
            "  F83  Fayblade Tizona             F93* Glimmerblade: Banishing Ray\n" +
            "  F83* Fellblade: Ruinous Doom     F95  Shinto Ama-no-Murakumo\n" +
            "  F84* Spiralblade: Rendering Fail F98  Godspear Gungnir\n" +
            "  F85* Crusher: Bond Cyclone       F99* Deathglutton: Epetamu\n" +
            "  F86* Fellaxe: Demon's Scythe\n" +
            "  F88  Starmace Elysium\n\n" +
            "Some drops are field-boss guaranteed (F80/F83/F86/F93); the\n" +
            "rest are quest-reward grants.\n\n" +
            "COSTS\n" +
            "Kill counts only for questgiver weapons. Field-boss versions\n" +
            "cost one field-boss fight (no respawn).\n\n" +
            "TIPS\n" +
            "Plan one climb that turns in every Implement quest end-to-end\n" +
            "— they don't respec, don't respawn, and missed turn-ins lock\n" +
            "out until a new save. The Implement System roster is the\n" +
            "spine of any post-F75 build diversity.\n\n" +
            "SEE ALSO\n" +
            "[Hollow Fragment HNM Questgivers (F79-F99)] · [Field Bosses — Guaranteed Drops] · [Hollow Area Uniques] · [Avatar Weapons & Last-Attack Bonus]")
        {
            Tags = new[] { "hollow-fragment", "weapons", "quests" }
        },

        new("Items", "Anneal Blade Craft Line",
            "┌─ Items\n" +
            "│ Topic: Anneal Blade Craft Line\n" +
            "│ Tier: Uncommon -> Rare -> Rare\n" +
            "│ Weapon type: 1H Sword, crafted starter\n" +
            "│ Source: Agil's General Store, F1\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Three-step Integral Factor starter craft line for new\n" +
            "players. Each step is a separate 1H sword, not an evolution\n" +
            "chain — the prior blade stays in your pack when you buy the\n" +
            "next tier.\n\n" +
            "USAGE\n" +
            "Visit Agil's General Store on Floor 1 to craft the Anneal\n" +
            "Blade. Tough and Pitch-black variants unlock as you climb\n" +
            "and gather the harder-tempered materials.\n\n" +
            "EFFECTS\n" +
            "  Anneal Blade              Uncommon, F1-2   ATK +10, DEX +2\n" +
            "  Tough Anneal Blade        Rare, F4-5       ATK +18, STR +3\n" +
            "  Pitch-black Anneal Blade  Rare, F8-10      ATK +24, DEX +4,\n" +
            "                                             Bleed+10 effect\n" +
            "Durability ladder: 80 / 120 / 140. Values: 180 / 420 / 780.\n\n" +
            "COSTS\n" +
            "Col + tempering ingredients scale with each step. Pitch-black\n" +
            "requires blackened ore from F8-10 mobs.\n\n" +
            "TIPS\n" +
            "Pitch-black carries you cleanly into the F14 Integral Arc\n" +
            "Angel window. Socket Hunter or Sharpening Ingots into its 3\n" +
            "refinement slots to squeeze another 10-20 floors of use.\n\n" +
            "SEE ALSO\n" +
            "[Integral Factor Weapon Series] · [Weapon Refinement System] · [Town of Beginnings NPCs (F1)]")
        {
            Tags = new[] { "weapons", "integral-factor", "crafting" }
        },

        new("Items", "Equipment Slots & Dual Wield",
            "┌─ Items\n" +
            "│ Topic: Equipment Slots & Dual Wield\n" +
            "│ Tier: Gear layout\n" +
            "│ Weapon type: Main + OffHand rules\n" +
            "│ Source: Character sheet\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Ten equipment slots cover weapon, armor, accessories, and\n" +
            "off-hand. OffHand rules depend on main-hand choice and Dual\n" +
            "Blades unlock state.\n\n" +
            "USAGE\n" +
            "Equip from inventory. Rings can stack two; necklaces and\n" +
            "bracelets are one each.\n\n" +
            "EFFECTS\n" +
            "Slots: Weapon, Head, Chest, Legs, Feet, RightRing, LeftRing,\n" +
            "Bracelet, Necklace, OffHand.\n" +
            "SHIELDS (OffHand): Wooden 10% block, Iron 18% block.\n" +
            "Successful block fully negates the hit and degrades the shield.\n" +
            "DUAL WIELD: 1H Swords become legal in OffHand after unlocking\n" +
            "Dual Blades. EXCEPTION: weapons with IsDualWieldPaired=true\n" +
            "(3 canonical pairs + 3 solo \"Dual\" weapons) equip to OffHand\n" +
            "without the unlock. Otherwise OffHand accepts shields only.\n" +
            "2H Swords/Bows/Scythes block OffHand entirely.\n" +
            "REFINEMENT: weapon, off-hand sword, AND shield each carry\n" +
            "their own 3 Refinement slots. Dual-wielding two IF swords\n" +
            "effectively stacks 6 ingot bonuses.\n\n" +
            "COSTS\n" +
            "Choosing a 2H weapon surrenders the OffHand slot outright.\n\n" +
            "TIPS\n" +
            "Before Dual Blades, an Iron Shield's 18% block pairs well\n" +
            "with Holy Sword's +15% — stacked you reach 33% flat block\n" +
            "against every incoming hit. Socket Guardian Ingots into the\n" +
            "shield for another +15 DEF on top. If you find a paired\n" +
            "canon weapon early, you can start dual-wielding before the\n" +
            "Dual Blades grind completes.\n\n" +
            "SEE ALSO\n" +
            "[Unique Skill: Dual Blades] · [Paired Dual-Wield Weapons] · [Weapon Refinement System]")
        {
            Tags = new[] { "equipment", "weapons", "refinement" }
        },

        new("Items", "Paired Dual-Wield Weapons",
            "┌─ Items\n" +
            "│ Topic: Paired Dual-Wield Weapons\n" +
            "│ Tier: Legendary / Divine (9 flagged weapons)\n" +
            "│ Flag: IsDualWieldPaired=true\n" +
            "│ Source: Canon placements — bosses, quests, drops\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Nine weapons carry the IsDualWieldPaired flag. They equip to\n" +
            "OffHand without the Dual Blades unlock, and three canonical\n" +
            "pairs trigger Pair Resonance when both slots are filled.\n\n" +
            "USAGE\n" +
            "Equip any paired weapon to OffHand at any level — the unlock\n" +
            "gate is bypassed. For Pair Resonance, fill BOTH main and off\n" +
            "with the matched partners; the bonus activates automatically\n" +
            "and logs '◆ Pair Resonance! ... sing together' once on first\n" +
            "swing of each encounter.\n\n" +
            "EFFECTS\n" +
            "CANONICAL PAIRS (auto-offhand + Pair Resonance synergy):\n" +
            "  Elucidator        ↔ Dark Repulser     Kirito SAO canon (F76)\n" +
            "  Elucidator Rouge  ↔ Flare Pulsar      FD fire variant pair\n" +
            "  Black Iron Dual A ↔ Black Iron Dual B Alicization Kirito\n" +
            "SOLO \"DUAL\" FLAVORED (auto-offhand bypass only, no synergy):\n" +
            "  Chaos Raider Dual    Kirito FD\n" +
            "  Lightning Divider Dual Kirito\n" +
            "  Murasama G4 Dual     Kirito\n" +
            "PAIR RESONANCE BONUSES (both slots canonical pair):\n" +
            "  +10% damage on both main-hand and offhand swings\n" +
            "  +5% effective CritRate via 5% re-roll on failed crits\n" +
            "Non-paired 1H Swords still require the Dual Blades unlock\n" +
            "(F74 Gleam Eyes or 50 1H kills).\n\n" +
            "COSTS\n" +
            "None beyond acquiring both halves. Pair Resonance needs an\n" +
            "exact match — mixing Elucidator with Flare Pulsar does not\n" +
            "trigger the synergy.\n\n" +
            "TIPS\n" +
            "Black Iron A/B is the fastest pair to assemble pre-F76 — both\n" +
            "drop through the Alicization-era banded pool. Elucidator/Dark\n" +
            "Repulser is the highest-ceiling canon pair but gates on F76\n" +
            "Gleam Eyes drops. Elucidator Rouge (F98 Ashen Kirito) + Flare\n" +
            "Pulsar is the late-game FD option.\n\n" +
            "SEE ALSO\n" +
            "[Unique Skill: Dual Blades] · [Equipment Slots & Dual Wield] · [Fractured Daydream Character Weapons] · [MD Alicization Canonical Extras]")
        {
            Tags = new[] { "dual-wield", "pair-resonance", "weapons" }
        },

        new("Items", "Food & Cooking",
            "┌─ Items\n" +
            "│ Topic: Food & Cooking\n" +
            "│ Tier: Common through canon rare\n" +
            "│ Weapon type: Consumable\n" +
            "│ Source: Bakeries, vendors, events\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Food items heal HP over a turn count and restore satiety.\n" +
            "Drinks grant smaller, longer regens.\n\n" +
            "USAGE\n" +
            "Eat from inventory or hotkey. Regens tick each turn while the\n" +
            "effect is active.\n\n" +
            "EFFECTS\n" +
            "  Bread               2 HP/turn x 10     bakery\n" +
            "  Grilled Meat        5 x 15              common\n" +
            "  Fish Stew           8 x 8               F3+\n" +
            "  Honey Bread         4 x 12              F2+ bakery mid-tier\n" +
            "  Elven Waybread      3 x 30              F4+\n" +
            "  Cream-Filled Bread  3 x 10              Tolbana 5 Col staple\n" +
            "  Asuna's Sandwich    5 x 15              F74 field lunch canon\n" +
            "  Gingerbread Cookies 3 x 20              Christmas event\n" +
            "  Ragout Rabbit Stew  20 x 40 turns       F35 LN Vol 1 canon, 3000 Col, max 1\n\n" +
            "COSTS\n" +
            "Poison ignores food healing — only time or Antidote clears it.\n\n" +
            "TIPS\n" +
            "Cream-Filled Bread at 5 Col is the best Col-per-HP deal in\n" +
            "the early game. Save Ragout Rabbit Stew for a long dungeon\n" +
            "push where its 40-turn regen can play out fully.\n\n" +
            "SEE ALSO\n" +
            "[Hunger, Satiety & Fatigue] · [Status: Bleed & Poison] · [Seasonal Events]")
        {
            Tags = new[] { "food", "equipment" }
        },

        new("Items", "Potions, Crystals & Throwables",
            "┌─ Items\n" +
            "│ Topic: Potions, Crystals & Throwables\n" +
            "│ Tier: Common through Legendary\n" +
            "│ Weapon type: Consumables\n" +
            "│ Source: Vendors, drops, quests\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Three consumable families: potions for stats and healing,\n" +
            "SAO-iconic voice-commanded crystals for teleport and utility,\n" +
            "and throwables for area effects.\n\n" +
            "USAGE\n" +
            "Use from inventory or bind the common ones to quick-use slots\n" +
            "1-5. Crystals respond to voice commands in canon and fire\n" +
            "instantly in-game.\n\n" +
            "EFFECTS\n" +
            "POTIONS  Health Potion (+50), Greater (+150), Antidote (cures\n" +
            "         poison/bleed), Battle Elixir (+15 ATK/+10 SPD 60t),\n" +
            "         Speed Potion (+10 SPD 30t), Iron Skin Potion (+10\n" +
            "         DEF 30t), Escape Rope, Revive Crystal (auto).\n" +
            "CRYSTALS Teleport Crystal (warp to named city), Corridor\n" +
            "         Crystal (60s portal), Anti-Crystal (suppresses\n" +
            "         teleports — Laughing Coffin tool), Healing (+100),\n" +
            "         High Healing (+300), Antidote Crystal, Paralysis\n" +
            "         Cure, Mirage Sphere (records combat), Pneuma Flower\n" +
            "         (revives ally within 10 turns, Legendary), Divine\n" +
            "         Stone of Returning Soul (revives within 10s —\n" +
            "         Nicholas F49 drop).\n" +
            "THROWABLES  Fire Bomb (30 fire, 3-rad), Poison Vial (10+\n" +
            "            poison), Smoke Bomb (blinds), Flash Bomb (5+stun).\n\n" +
            "COSTS\n" +
            "The Anti-Crystal Tyranny run modifier disables every Crystal\n" +
            "consumable listed above.\n\n" +
            "TIPS\n" +
            "Keep a Teleport Crystal + Escape Rope stacked for panic\n" +
            "exits. The Divine Stone of Returning Soul is a one-shot\n" +
            "revival — don't stash it, deploy it when it matters.\n\n" +
            "SEE ALSO\n" +
            "[Quick-Use Slots (1-5)] · [Status: Bleed & Poison] · [Run Modifiers (12 Optional Challenges)]")
        {
            Tags = new[] { "potions", "throwables", "equipment" }
        },

        new("Items", "Accessories",
            "┌─ Items\n" +
            "│ Topic: Accessories\n" +
            "│ Tier: Six standard pieces\n" +
            "│ Weapon type: Ring / amulet / band\n" +
            "│ Source: Vendors and drops\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Six standard rings, amulets, and bands that plug into the\n" +
            "ring, necklace, and bracelet slots with focused attribute +\n" +
            "derived-stat pairs.\n\n" +
            "USAGE\n" +
            "Equip via the character sheet. Rings stack to 2 simultaneous\n" +
            "copies; necklaces and bracelets are one each.\n\n" +
            "EFFECTS\n" +
            "  Ring of Strength    +10 STR / +5 ATK       max 2 equipped\n" +
            "  Amulet of Agility   +12 AGI / +5 SPD\n" +
            "  Guardian Ring       +8 DEF / +5 VIT\n" +
            "  Scholar's Pendant   +10 INT / +5 SkillDmg\n" +
            "  Swift Band          +8 SPD / +6 AGI\n" +
            "  Vitality Charm      +8 VIT\n\n" +
            "COSTS\n" +
            "Standard Col vendor prices; no special crafting cost.\n\n" +
            "TIPS\n" +
            "Ring of Strength is the only piece that explicitly caps at 2\n" +
            "equipped — slot both for +20 STR / +10 ATK. Scholar's Pendant\n" +
            "is the dedicated slot for Intelligence-scaling sword-skill\n" +
            "builds.\n\n" +
            "SEE ALSO\n" +
            "[Equipment Slots & Dual Wield] · [The Six Attributes] · [Derived Combat Stats]")
        {
            Tags = new[] { "accessories", "equipment", "stats" }
        },

        new("Items", "Infinity Moment Last Attack Bonus Weapons",
            "┌─ Items\n" +
            "│ Topic: Infinity Moment Last Attack Bonus Weapons\n" +
            "│ Tier: Legendary (non-enhanceable, 8 canon IM drops)\n" +
            "│ Weapon type: Bow, 2H Sword, Scimitar, Katana, Dagger, 2H Axe, Spear\n" +
            "│ Source: F85-F99 floor-boss killing blow (100% drop)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Eight canon Infinity Moment weapons that drop at 100% when the\n" +
            "PLAYER lands the killing blow on a designated floor boss from\n" +
            "F85 upward. LAB (Last Attack Bonus) weapons are non-enhanceable\n" +
            "— high base stats are the tradeoff for zero scaling headroom.\n\n" +
            "USAGE\n" +
            "Deliver the killing blow yourself on the listed floor boss —\n" +
            "ally KOs don't count. Drop logs a BrightYellow line alongside\n" +
            "any existing guaranteed boss drop (additive, not replacing).\n\n" +
            "EFFECTS\n" +
            "  F85  Zephyros          Bow\n" +
            "  F92  Sacred Cross      2H Sword, HolyDamage\n" +
            "  F93  Glow Haze         Scimitar, BlindOnHit\n" +
            "  F94  Saku              Katana, NightDamage\n" +
            "  F95  Mirage Knife      Dagger, Invisibility\n" +
            "  F96  Northern Light    2H Axe, FrostDamage\n" +
            "  F98  Lunatic Roof      Spear, Lunacy\n" +
            "  F99  Artemis           Bow, PiercingShot\n" +
            "F99 drops BOTH Night Sky Sword Divine AND Artemis — additive.\n\n" +
            "COSTS\n" +
            "IsEnhanceable=false: Anvil Enhance shows [SEALED]. Durability\n" +
            "ticks normally; repair at the Anvil like any Legendary piece.\n\n" +
            "TIPS\n" +
            "Wield the weapon type you want a later LAB to replace when\n" +
            "closing the boss — LAB drops are floor-locked and won't repeat\n" +
            "on a later climb. Park Saku (Katana, NightDamage) for Darkness\n" +
            "Blade builds; Artemis (Bow) is one of only two F99 apex drops.\n\n" +
            "SEE ALSO\n" +
            "[Non-Enhanceable LAB Weapons] · [Avatar Weapons & Last-Attack Bonus] · [Infinity Moment Shop Weapons] · [Named Legendary Highlights]")
        {
            Tags = new[] { "infinity-moment", "lab-weapon", "weapons" }
        },

        new("Items", "Infinity Moment Shop Weapons",
            "┌─ Items\n" +
            "│ Topic: Infinity Moment Shop Weapons\n" +
            "│ Tier: Epic (F76-85) + Legendary (F86-99), enhanceable\n" +
            "│ Weapon type: Rapier, 2H Sword, 2H Axe, Katana, Spear, Scimitar, Dagger\n" +
            "│ Source: F50+ Dynamic Shop Tiering stock (tier-unlocked)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Twelve canon Infinity Moment weapons purchasable from vendors\n" +
            "once their floor tier unlocks via the Dynamic Shop Tiering\n" +
            "system. Unlike LAB weapons, these ARE enhanceable — each level\n" +
            "consumes an Enhancement Ore of your chosen bias.\n\n" +
            "USAGE\n" +
            "Clear floor bosses at F50+ to push the shop tier counter. When\n" +
            "the listed floor band unlocks, the weapon appears in EVERY\n" +
            "vendor's stock with a [NEW] badge on first visit. Buy once and\n" +
            "it's yours — stock is additive and never shrinks.\n\n" +
            "EFFECTS\n" +
            "EPIC band (F76-85):\n" +
            "  Edelweiss         Rapier, CritRate\n" +
            "  Fasislawine       2H Sword, Cleave\n" +
            "  Foa Stoss         Spear, ThrustDmg\n" +
            "  Poisoned Syringe  Scimitar, Poison\n" +
            "  Flyheight Fang    Dagger, BackstabDmg\n" +
            "LEGENDARY band (F86-99):\n" +
            "  Noctis Strasse    Rapier, Bleed\n" +
            "  Wice Ritter       2H Sword, ArmorPierce\n" +
            "  Schwarzs Blitz    2H Axe, Stun\n" +
            "  Muramasa          Katana, Bleed\n" +
            "  Wave Schneider    Spear, FrostDamage\n" +
            "  Silver Wing       Scimitar, AttackSpeed\n" +
            "  Rue Feuille       Dagger, CritRate\n\n" +
            "COSTS\n" +
            "Standard vendor markup. Enhancement ore cost scales as normal\n" +
            "(see Anvil — Repair, Enhance, Evolve, Refine).\n\n" +
            "TIPS\n" +
            "Muramasa (Katana, Bleed) stacks cleanly with Katana Mastery's\n" +
            "15% bleed passive for constant DoT pressure. Schwarzs Blitz\n" +
            "pairs with stun-skill builds. Enhance with Crimson Flame for\n" +
            "raw ATK or Sharp Blade for crit-focused DPS.\n\n" +
            "SEE ALSO\n" +
            "[Dynamic Shop Tiering (F50+)] · [Enhancement Ores System] · [Anvil — Repair, Enhance, Evolve, Refine] · [Unique Skill: Katana Mastery] · [Vendors — Rotating Stock]")
        {
            Tags = new[] { "infinity-moment", "shop-tiering", "weapons" }
        },

        new("Items", "Enhancement Ores System",
            "┌─ Items\n" +
            "│ Topic: Enhancement Ores System\n" +
            "│ Tier: Uncommon material (7 ore types)\n" +
            "│ Weapon type: Consumed by Anvil Enhance\n" +
            "│ Source: Themed mob drops (~3-5%) + boss drops (15-30%)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Seven themed Enhancement Ores replace the old flat +N Enhance.\n" +
            "Each Anvil Enhance level now consumes exactly one ore, and the\n" +
            "ore chosen biases that level's BonusPerLevel into a specific\n" +
            "stat — you pick the stat by picking the ore.\n\n" +
            "USAGE\n" +
            "Farm ores from themed mob biomes. At the Anvil, pick Enhance,\n" +
            "choose a bias ore from inventory, confirm. The ore is consumed\n" +
            "whether the enhance succeeds or fails. Ore history is stored\n" +
            "per-weapon and preserved through Evolve.\n\n" +
            "EFFECTS\n" +
            "  Crimson Flame Ore  +Attack         fire / demon / volcanic mobs\n" +
            "  Adamant Ore        +Defense        armored / construct / golem mobs\n" +
            "  Crust Ore          +Vitality       earth / giant / undead mobs\n" +
            "  Sharp Blade Ore    +Dexterity      humanoid / bandit / pk mobs\n" +
            "  Flowing Water Ore  +Speed          aquatic / ice mobs\n" +
            "  Wind Flower Ore    +Agility        insect / beast / flying mobs\n" +
            "  Ash White Ore      +Intelligence   hollow mobs F76+\n" +
            "Prices: 85-120 Col each, Uncommon rarity, stack to 20.\n\n" +
            "COSTS\n" +
            "One ore per Enhance attempt (in addition to the normal Col\n" +
            "cost). Legacy save migration: any pre-existing +N weapon gets\n" +
            "N × Crimson Flame Ore entries baked into history, so stats\n" +
            "match pre-session behavior (+Attack-only) — no regression.\n\n" +
            "TIPS\n" +
            "Pre-farm 7-10 ores of your primary bias before a major Enhance\n" +
            "push; running out mid-attempt wastes a trip. Boss drops roll\n" +
            "15-30% so floor-boss farming is the fastest stockpile route.\n" +
            "Humanoid/bandit biomes double-dip as chain-catalyst farms.\n\n" +
            "SEE ALSO\n" +
            "[Anvil — Repair, Enhance, Evolve, Refine] · [Material Tiers (Baseline)] · [Non-Enhanceable LAB Weapons] · [Infinity Moment Shop Weapons]")
        {
            Tags = new[] { "enhancement-ore", "anvil", "crafting" }
        },

        new("Items", "Non-Enhanceable LAB Weapons",
            "┌─ Items\n" +
            "│ Topic: Non-Enhanceable LAB Weapons\n" +
            "│ Tier: Legendary (IsEnhanceable flag = false)\n" +
            "│ Weapon type: All 8 Infinity Moment LAB drops\n" +
            "│ Source: F85-F99 floor-boss LAB hook\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "A subset of late-game weapons carry the IsEnhanceable=false\n" +
            "flag. Canon IM rationale: LAB weapons ship with high base\n" +
            "stats and cannot scale further. The Anvil Enhance menu marks\n" +
            "them [SEALED] and blocks any attempt with a clear message.\n\n" +
            "USAGE\n" +
            "Equip and fight as normal. Repair, Evolve (if on a chain), and\n" +
            "Refine flows all still apply — only Enhance is blocked.\n\n" +
            "EFFECTS\n" +
            "Sealed weapons (all 8 current IM LAB drops):\n" +
            "  Zephyros, Sacred Cross, Glow Haze, Saku, Mirage Knife,\n" +
            "  Northern Light, Lunatic Roof, Artemis.\n" +
            "Remains Heart remains enhanceable (canon Lisbeth masterwork\n" +
            "exception). Any future LAB additions inherit the flag.\n\n" +
            "COSTS\n" +
            "Opportunity cost only: a sealed weapon's +3 ATK / +N bias\n" +
            "ceiling is forfeit. Factor high base stats into the comparison\n" +
            "against an enhanceable Epic/Legendary you could push to +10.\n\n" +
            "TIPS\n" +
            "Pair a sealed LAB with heavy Refinement investment — socket 3\n" +
            "Astral or Chimeric Ingots to recover the ATK you can't enhance\n" +
            "in. Dual Blades users can mix a sealed main-hand with an\n" +
            "enhanceable off-hand for scaling on one side.\n\n" +
            "SEE ALSO\n" +
            "[Infinity Moment Last Attack Bonus Weapons] · [Anvil — Repair, Enhance, Evolve, Refine] · [Enhancement Ores System] · [Weapon Refinement System]")
        {
            Tags = new[] { "infinity-moment", "lab-weapon", "weapons" }
        },

        new("Items", "Dynamic Shop Tiering (F50+)",
            "┌─ Items\n" +
            "│ Topic: Dynamic Shop Tiering (F50+)\n" +
            "│ Tier: 50 stock tiers across F51-F99\n" +
            "│ Weapon type: Shop-wide stock injector\n" +
            "│ Source: Floor-boss clears at F50+ (persistent)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Every floor-boss cleared at F50 or higher unlocks a new stock\n" +
            "entry across EVERY vendor. Fifty tiers span F51-F99. Stock is\n" +
            "additive — once unlocked, an item stays available for the rest\n" +
            "of the save.\n\n" +
            "USAGE\n" +
            "Ascend past F50, then clear each subsequent floor boss. The\n" +
            "shop header shows \"Tier N/50 unlocked\" and the first-visit\n" +
            "items carry a [NEW] badge until inspected.\n\n" +
            "EFFECTS\n" +
            "Tier count persists via SaveData.HighestFloorBossCleared. The\n" +
            "12 Infinity Moment shop weapons slot into canon F76-F96 tier\n" +
            "bands (Epic at F76-85, Legendary at F86-99). Higher tiers also\n" +
            "inject future expansion content into the same pipeline.\n\n" +
            "COSTS\n" +
            "None for unlocks — the floor-boss climb IS the unlock. Vendor\n" +
            "markup (+20%) applies to the priced item as normal.\n\n" +
            "TIPS\n" +
            "Revisit every vendor after each F50+ boss kill to grab the\n" +
            "[NEW] item before it blends into the stock wall. Missing the\n" +
            "badge doesn't lock you out — the item remains in stock — but\n" +
            "the badge is the best way to spot the new drop quickly.\n\n" +
            "SEE ALSO\n" +
            "[Infinity Moment Shop Weapons] · [Vendors — Rotating Stock] · [Ascending a Floor] · [Floor Boss Roster — Canon Highlights]")
        {
            Tags = new[] { "shop-tiering", "infinity-moment", "economy" }
        },

        new("Items", "Memory Defrag Originals",
            "┌─ Items\n" +
            "│ Topic: Memory Defrag Originals\n" +
            "│ Tier: Rare / Epic / Legendary (16 total)\n" +
            "│ Weapon type: 1H Sword, 2H Sword, Katana, Rapier, Dagger, Spear, Bow\n" +
            "│ Source: Floor-banded loot pool (MD canon)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Sixteen MD-exclusive weapons imported from SAO: Memory Defrag\n" +
            "(MD, the canonical SAO mobile game). They are NOT tied to any\n" +
            "named character — MD's event weapons were one-off drops, and\n" +
            "they roll here through the floor-banded loot pool at canon-\n" +
            "appropriate bands.\n\n" +
            "USAGE\n" +
            "Farm mobs and chests within each weapon's listed floor band.\n" +
            "Drops echo through LootGenerator.FloorBandedRegisteredLoot —\n" +
            "you may see them from any kill, chest, or pool roll on the\n" +
            "band.\n\n" +
            "EFFECTS\n" +
            "LEGENDARY (5): Sword of Diva (1H), Espada of Sword Dance\n" +
            "(Rapier), Sword of Causality (2H), Shining Nemesisz (Katana).\n" +
            "EPIC (5): Purple Star Baselard (Dagger), Neo Atlantis (Spear),\n" +
            "Eternal Promise (1H), Aqua Spread (Bow), Chivalrous Rapier.\n" +
            "RARE (7): Cobalt Tristan (1H), Venus Heart (Rapier), Atlantis\n" +
            "Sword (1H), Bloody Rapier, Holy Flower Rapier, Mithril Rapier,\n" +
            "Cheer of Love Bow.\n\n" +
            "COSTS\n" +
            "None guaranteed — the loot pool is probabilistic. MD drops\n" +
            "compete for pool slots with IF/HF/IM/FD entries on each band.\n\n" +
            "TIPS\n" +
            "MD weapons lean flavor-canon (no unique SpecialEffects beyond\n" +
            "their rarity-tier stats) — if you're building for a specific\n" +
            "effect, prefer IM Shop or IF Series weapons. Rare MD rapiers\n" +
            "are a cheap stopgap for Rapier proficiency grinds F10-F30.\n\n" +
            "SEE ALSO\n" +
            "[MD Alicization Canonical Extras] · [Fractured Daydream Character Weapons] · [Elemental Weapon Variants] · [Named Legendary Highlights] · [Weapon Types Overview]")
        {
            Tags = new[] { "memory-defrag", "weapons", "rarity" }
        },

        new("Items", "MD Alicization Canonical Extras",
            "┌─ Items\n" +
            "│ Topic: MD Alicization Canonical Extras\n" +
            "│ Tier: Legendary (4 weapons)\n" +
            "│ Weapon type: 1H Sword (all four)\n" +
            "│ Source: Floor-banded pool, Alicization era bands\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Four Memory Defrag (MD) Alicization canon blades that fill\n" +
            "Underworld-arc gaps alongside the Divine Object Set. They\n" +
            "cover two Kirito pairs and MD's awakened-form variant of\n" +
            "Alice's Fragrant Olive Sword.\n\n" +
            "USAGE\n" +
            "Three farm through the floor-banded loot pool on Alicization-\n" +
            "era floors (F65+). Unfolding Truth Fragrant Olive Sword is\n" +
            "quest-gated via Selka's chained Quest 2 (The Sword's Awakening).\n" +
            "Red Rose Sword also has a canon field-boss source on F95.\n\n" +
            "EFFECTS\n" +
            "  Unfolding Truth Fragrant Olive Sword  Selka Q2 reward\n" +
            "                                         (Alice's awakened form)\n" +
            "  Red Rose Sword                         F95 boss drop + pool\n" +
            "                                         (pairs with Night Sky)\n" +
            "  Black Iron Dual Sword A               Underworld Kirito pair\n" +
            "  Black Iron Dual Sword B               offhand of the pair\n" +
            "Black Iron A/B are IsDualWieldPaired — either slot bypasses\n" +
            "the Dual Blades unlock, and equipping both triggers Pair\n" +
            "Resonance (+10% dmg both swings, +5% crit re-roll).\n\n" +
            "COSTS\n" +
            "None beyond the pool roll or quest. Pair A/B don't auto-drop\n" +
            "together — expect separate rolls for the dual-wield setup.\n\n" +
            "TIPS\n" +
            "Dual Blades users building a canon Underworld Kirito run should\n" +
            "chase Black Iron A/B as the matched Pair Resonance pair.\n" +
            "Unfolding Truth is the MD-canon complement to the Divine\n" +
            "Fragrant Olive from Selka Q1 — both canon, different flavor.\n\n" +
            "SEE ALSO\n" +
            "[The Sword's Awakening (Selka F65)] · [Paired Dual-Wield Weapons] · [Divine Object Set — Integrity Knights] · [Memory Defrag Originals] · [Named Legendary Highlights]")
        {
            Tags = new[] { "memory-defrag", "weapons", "divine" }
        },

        new("Items", "Fractured Daydream Character Weapons",
            "┌─ Items\n" +
            "│ Topic: Fractured Daydream Character Weapons\n" +
            "│ Tier: Legendary (17) + Rare (1)\n" +
            "│ Weapon type: Across 1H, 2H, Katana, Mace, Dagger, Rapier, Axe\n" +
            "│ Source: Floor-banded loot pool (FD canon)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Eighteen character-signature weapons imported from SAO:\n" +
            "Fractured Daydream (FD, the 2024 co-op action game). Each is\n" +
            "canonically wielded by a specific SAO cast member in FD; the\n" +
            "class-lock is flavor only — any player can equip any FD weapon\n" +
            "that fits their loadout.\n\n" +
            "USAGE\n" +
            "Eight are now canon-placed on field bosses or quest NPCs\n" +
            "(see Fractured Daydream Field Bosses). The remainder farm\n" +
            "through the floor-banded loot pool on F50+, competing for\n" +
            "slots alongside IF, HF, IM, and MD entries at their band.\n\n" +
            "EFFECTS\n" +
            "By wielder (★ = canon-placed boss/quest drop):\n" +
            "  Kirito        Elucidator Rouge★ (F98), Chaos Raider Dual,\n" +
            "                Murasama G4 (Katana)\n" +
            "  Klein         Spirit Sword Kagutsuchi★ (F60),\n" +
            "                Spirit Sword Susanoo★ (F70)\n" +
            "  Agil          Ground Gorge★ (F55 quest), Naz (2H Axe)\n" +
            "  Alice         Golden Osmanthus Sword (1H)\n" +
            "  Heathcliff    Flame Lord★ (F80, 2H Sword)\n" +
            "  Lisbeth       Plain Mace (Mace, Rare), Grida Replicant (Mace)\n" +
            "  Leafa         Sweep Saber (Katana)\n" +
            "  Yui           Obsidian Dagger\n" +
            "  Argo          Virt Katze (Dagger), Thunder God's Rift Blade\n" +
            "                (Dagger)\n" +
            "  Oberon        Tanquiem (1H), Excalibur Oberon (1H)\n" +
            "  Administrator Silvery Ruler★ (F97, 1H)\n" +
            "  Yuuki         Macafitel★ (F85, Rapier)\n" +
            "  (also canon★) Red Rose Sword (F95)\n\n" +
            "COSTS\n" +
            "None beyond the pool roll or boss kill. Drops are not gated\n" +
            "on recruiting the matching ally — you can loot Klein's\n" +
            "Spirit Sword Susanoo with or without Klein in your party.\n\n" +
            "TIPS\n" +
            "Pair matching weapons with matching allies for canon flavor\n" +
            "runs (Klein wielding Spirit Sword, Agil with Ground Gorge).\n" +
            "Kirito's FD pair (Elucidator Rouge + Flare Pulsar) forms a\n" +
            "Pair Resonance dual-wield combo; add Murasama G4 as a Katana\n" +
            "sidearm for Katana Mastery's bleed pressure when swapping.\n\n" +
            "SEE ALSO\n" +
            "[Fractured Daydream Field Bosses] · [Agil's Apprentice (F55)] · [Paired Dual-Wield Weapons] · [Elemental Weapon Variants] · [Named Legendary Highlights]")
        {
            Tags = new[] { "fractured-daydream", "weapons", "rarity" }
        },

        new("Items", "Elemental Weapon Variants",
            "┌─ Items\n" +
            "│ Topic: Elemental Weapon Variants\n" +
            "│ Tier: Rare / Epic (27 variants)\n" +
            "│ Weapon type: Character-base flavor drops\n" +
            "│ Source: Floor-banded pool F50+, 80% stats of base\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Twenty-seven elemental variants of FD and legacy character-\n" +
            "signature weapons. Each variant shifts its base's statline to\n" +
            "80% of the character-base version and adds one element-tied\n" +
            "SpecialEffect, trading raw power for utility.\n\n" +
            "USAGE\n" +
            "Farm the floor-banded loot pool on F50+. Variants spawn in the\n" +
            "same bands as their character-base entries, so a farm pass\n" +
            "hunting Alice's Golden Osmanthus Sword may yield Alice's\n" +
            "Thunderclap variant instead.\n\n" +
            "EFFECTS\n" +
            "Element → SpecialEffect mapping:\n" +
            "  Fire      Burn +15 / +20\n" +
            "  Water     Freeze +15\n" +
            "  Wind      Slow +15\n" +
            "  Thunder   Stun +10\n" +
            "  Light     HolyDamage +15\n" +
            "  Dark      Bleed +15\n\n" +
            "Variants exist across (non-exhaustive): Asuna (Ray Grace, Volt\n" +
            "Rapier, Dazzling Blink, Shadow Grace), Klein (White Plum,\n" +
            "Futari Shizuka), Agil (Ignite Bardiche, Tyrant Fall, Sturm\n" +
            "Welt), Alice (Red Peony, Gentle Breeze, Thunderclap, Purple\n" +
            "Bellflower), Heathcliff (Arc Order, Topaz Edge, Saint Guarder,\n" +
            "Abyss Keeper), Lisbeth (Blazing Torch, Elemental Hammer), Leafa\n" +
            "(Icicle Blade, Eradicate Saber), Silica (Defeza), Argo (Hermit\n" +
            "Fang), Oberon (Excalibur Bloodthirst).\n\n" +
            "COSTS\n" +
            "None beyond the pool roll. The 80%-of-base stat tradeoff is\n" +
            "fixed — variants never match the character-base's raw damage.\n\n" +
            "TIPS\n" +
            "Pick the variant whose SpecialEffect matches your build gap:\n" +
            "Thunder/Stun for crowd control, Dark/Bleed to feed Hemorrhage,\n" +
            "Fire/Burn for tick pressure. On Katana builds, Dark variants\n" +
            "double-dip with Katana Mastery's 15% bleed passive.\n\n" +
            "SEE ALSO\n" +
            "[Fractured Daydream Character Weapons] · [Memory Defrag Originals] · [Status: Bleed & Poison] · [Status: Stun & Slow] · [Unique Skill: Katana Mastery]")
        {
            Tags = new[] { "elemental-variants", "weapons", "fractured-daydream" }
        },

        // ═══════════════════════════════════════════════════════════════
        // ── 5. Quests, NPCs & Economy ──────────────────────────────────
        // ═══════════════════════════════════════════════════════════════

        new("Quests & NPCs", "Quest Types & Rewards",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Any random floor NPC\n" +
            "│ Floor: All\n" +
            "│ Quest: Kill / Collect / Explore / Deliver\n" +
            "│ Reward: Scales with floor + quest count\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Four quest archetypes with deterministic scaling formulas. Caps\n" +
            "at 5 active quests; non-persistent quests clear on floor change.\n\n" +
            "USAGE\n" +
            "Bump a random NPC (1-in-3 chance) to be offered a floor-\n" +
            "appropriate quest. Canonical Divine-quest givers never layer\n" +
            "random quests on top.\n\n" +
            "EFFECTS\n" +
            "Types:\n" +
            "  KILL     Defeat N mobs (optionally gated on weapon type)\n" +
            "  COLLECT  Gather N named items from mobs\n" +
            "  EXPLORE  Discover 40-60% of the floor\n" +
            "  DELIVER  Bring a package to any NPC\n\n" +
            "Reward formulas (F = floor, N = count):\n" +
            "  Kill:    100 + F*50 + N*20 Col / 50 + F*30 + N*10 XP\n" +
            "  Collect:  80 + F*40 + N*30 Col / 40 + F*25 + N*15 XP\n" +
            "  Explore: 150 + F*60 Col       / 80 + F*35 XP\n" +
            "  Deliver: 120 + F*45 Col       / 60 + F*30 XP\n\n" +
            "COSTS\n" +
            "Quest progress resets on floor change unless you turn in first.\n\n" +
            "TIPS\n" +
            "Stack 5 kill quests on the same floor before farming mobs — all\n" +
            "5 count in parallel. Turn in right before ascending so you don't\n" +
            "burn floor-locked progress.\n\n" +
            "SEE ALSO\n" +
            "[Accepting & Completing Quests] · [Col Economy — How You Earn] · [Vendors — Rotating Stock] · [Experience & Leveling]")
        {
            Tags = new[] { "quests", "economy", "xp" }
        },

        new("Quests & NPCs", "Accepting & Completing Quests",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Any floor NPC (bump to interact)\n" +
            "│ Floor: All\n" +
            "│ Quest: Acceptance / completion workflow\n" +
            "│ Reward: See Quest Types & Rewards\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Quests are offered on NPC bump (1-in-3 chance), auto-tracked in\n" +
            "the background, and turned in at any NPC once complete. No\n" +
            "quest-log screen needed.\n\n" +
            "USAGE\n" +
            "1. Bump an NPC; accept the offered quest.\n" +
            "2. Play; Kill/Collect/Explore auto-track.\n" +
            "3. When [QUEST] complete! fires, bump ANY NPC to resolve. All\n" +
            "   completed quests pay out in a single pop.\n\n" +
            "EFFECTS\n" +
            "Deliver quests resolve the instant you talk to any NPC after\n" +
            "picking up the package. Weapon-gated kill quests only count\n" +
            "while the required weapon type is equipped. Canonical Divine-\n" +
            "quest givers never layer random quests on top of their own.\n\n" +
            "COSTS\n" +
            "None. Aborting an accepted quest just lets it expire on floor\n" +
            "change (no penalty).\n\n" +
            "TIPS\n" +
            "Keep a Town NPC in reach during long farming runs — turn in\n" +
            "between waves so overflow XP isn't eaten by a level-up mid-quest.\n\n" +
            "SEE ALSO\n" +
            "[Quest Types & Rewards] · [Town of Beginnings NPCs (F1)] · [Vendors — Rotating Stock] · [Save System]")
        {
            Tags = new[] { "quests", "npcs", "economy" }
        },

        new("Quests & NPCs", "Ran the Brawler (F2)",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Ran the Brawler (green 'R')\n" +
            "│ Floor: 2\n" +
            "│ Quest: Ran's Trial (Unique Skill unlock)\n" +
            "│ Reward: Martial Arts + 200 Col + 150 XP\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "An SAO Progressive canon NPC who gates the Martial Arts Unique\n" +
            "Skill behind an unarmed-combat trial on Floor 2.\n\n" +
            "USAGE\n" +
            "Bump Ran on F2. Accept Ran's Trial: defeat 5 beasts on F2 using\n" +
            "Unarmed (no weapon equipped). Return to Ran after completion.\n\n" +
            "EFFECTS\n" +
            "On turn-in:\n" +
            "  - Unlocks Martial Arts Unique Skill\n" +
            "  - +200 Col\n" +
            "  - +150 XP\n" +
            "Alternative unlock: Martial Arts auto-unlocks at 30 unarmed\n" +
            "kills. The trial banner is guarded against double-firing if the\n" +
            "milestone already popped.\n\n" +
            "COSTS\n" +
            "You fight 5 F2 beasts with hands empty. Fists do reduced raw\n" +
            "damage but benefit from Martial Arts' unarmed +10% / +20% crit\n" +
            "once it's active.\n\n" +
            "TIPS\n" +
            "Travel to F2 with a ranged backup (Bow) stashed in backpack so\n" +
            "you can re-equip if a boss ambushes. Feline/kobold beasts on F2\n" +
            "die in 2-3 unarmed hits at Lv4+.\n\n" +
            "SEE ALSO\n" +
            "[Unique Skill: Martial Arts] · [Accepting & Completing Quests] · [Weapon Proficiency Ranks] · [Sword Skills — Unlock & Use]")
        {
            Tags = new[] { "quests", "npcs", "unique-skills" }
        },

        new("Quests & NPCs", "Sister Azariya (F50)",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Sister Azariya (cyan 'A')\n" +
            "│ Floor: 50\n" +
            "│ Quest: Light at the Edge of Sight\n" +
            "│ Reward: Heaven-Piercing Blade (Divine) + 500 Col + 400 XP\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "A Divine Object giver on Floor 50 who hands over Fanatio's\n" +
            "Heaven-Piercing Blade in exchange for clearing monsters on her\n" +
            "floor. Former Fanatio apprentice; left the Integrity Knight\n" +
            "order.\n\n" +
            "USAGE\n" +
            "Bump Sister Azariya on F50. Accept \"Light at the Edge of Sight\":\n" +
            "slay 20 monsters on Floor 50. Return to her with the quest\n" +
            "complete.\n\n" +
            "EFFECTS\n" +
            "On turn-in:\n" +
            "  - Heaven-Piercing Blade (Divine, PiercingBeam+30, Range 2)\n" +
            "  - +500 Col\n" +
            "  - +400 XP\n" +
            "Auto-added to inventory; dropped at your feet if inventory full.\n\n" +
            "COSTS\n" +
            "20 F50 monster kills. She never layers random quests on top, so\n" +
            "don't expect extra side work.\n\n" +
            "TIPS\n" +
            "Double-dip by accepting weapon-gated random Kill quests from\n" +
            "another F50 NPC first — the same 20 kills can pay out twice.\n\n" +
            "SEE ALSO\n" +
            "[Divine Object Set — Integrity Knights] · [Divine Objects] · [Selka the Novice (F65)] · [Quest Types & Rewards]")
        {
            Tags = new[] { "quests", "npcs", "divine" }
        },

        new("Quests & NPCs", "Selka the Novice (F65)",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Selka the Novice (white 'S')\n" +
            "│ Floor: 65\n" +
            "│ Quest 1: The Last Knight's Bequest (25 kills)\n" +
            "│ Quest 2: The Sword's Awakening (30 kills, post-turn-in)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "A Floor 65 Divine Object giver and chained-quest NPC. Selka\n" +
            "(Alice's younger sister) keeps Alice's blade for a worthy\n" +
            "wielder, then returns with a canon MD awakening chain.\n\n" +
            "USAGE\n" +
            "Bump Selka on F65. Accept \"The Last Knight's Bequest\": slay 25\n" +
            "monsters on Floor 65, return for the Divine blade. After turn-\n" +
            "in, bump her again to unlock \"The Sword's Awakening\": slay 30\n" +
            "monsters on F65+ for the MD-awakened variant.\n\n" +
            "EFFECTS\n" +
            "Quest 1 (The Last Knight's Bequest):\n" +
            "  - Fragrant Olive Sword (Divine, HolyAoE+15, SD+15)\n" +
            "  - +500 Col, +400 XP\n" +
            "Quest 2 (The Sword's Awakening, chained):\n" +
            "  - Unfolding Truth Fragrant Olive Sword (MD-awakened, stronger)\n" +
            "  - +800 Col, +600 XP\n" +
            "  - Closes Selka's arc with canon MD \"awakening\" dialogue\n" +
            "Auto-added to inventory; dropped at your feet if full.\n\n" +
            "COSTS\n" +
            "25 + 30 F65+ monster kills. No new save schema — the chain\n" +
            "uses QuestStatus.TurnedIn as the Quest 2 gate.\n\n" +
            "TIPS\n" +
            "If you're running a Holy-Sword build, Fragrant Olive Sword's\n" +
            "HolyAoE+15 stacks with Sacred Edge nicely. Push straight into\n" +
            "Quest 2 for the awakened upgrade — the 30 kills overlap with\n" +
            "any standing F65+ kill quests, so stack your accept list.\n\n" +
            "SEE ALSO\n" +
            "[The Sword's Awakening (Selka F65)] · [Divine Object Set — Integrity Knights] · [MD Alicization Canonical Extras] · [Unique Skill: Holy Sword]")
        {
            Tags = new[] { "quests", "npcs", "unfolding-truth" }
        },

        new("Quests & NPCs", "The Sword's Awakening (Selka F65)",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Selka the Novice (F65)\n" +
            "│ Floor: 65+ (kills count anywhere F65 or above)\n" +
            "│ Quest: selka_unfolding_truth (chained Quest 2)\n" +
            "│ Reward: Unfolding Truth Fragrant Olive Sword + 800 Col + 600 XP\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Selka's chained second quest. Becomes available only after the\n" +
            "base Fragrant Olive Sword quest (The Last Knight's Bequest) is\n" +
            "turned in — no new save schema; uses QuestStatus.TurnedIn as\n" +
            "the unlock gate.\n\n" +
            "USAGE\n" +
            "Turn in Quest 1 for the Divine Fragrant Olive Sword. Return to\n" +
            "Selka on F65 and bump her again — the awakening dialogue opens.\n" +
            "Accept, then slay 30 monsters on F65 or higher and return.\n\n" +
            "EFFECTS\n" +
            "On turn-in:\n" +
            "  - Unfolding Truth Fragrant Olive Sword\n" +
            "    (MD-awakened variant, stronger stats than Divine base)\n" +
            "  - +800 Col\n" +
            "  - +600 XP\n" +
            "  - Post-turn-in flavor line closes Selka's arc\n" +
            "Dialogue uses canon MD phrasing (\"awakening\", \"unfolding\n" +
            "truth\") pulled from the Alicization mobile storyline.\n\n" +
            "COSTS\n" +
            "30 F65+ kills. Kills roll up from any floor at or above 65, so\n" +
            "you can advance the counter while climbing.\n\n" +
            "TIPS\n" +
            "Stack the 30 kills with HF HNM questgiver grinds on F79+ —\n" +
            "those floors count, and you'll pay off two quests at once.\n" +
            "The awakened blade replaces the Divine base in most builds;\n" +
            "salvage or vault the Divine for Holy-Sword flavor runs.\n\n" +
            "SEE ALSO\n" +
            "[Selka the Novice (F65)] · [MD Alicization Canonical Extras] · [Divine Object Set — Integrity Knights] · [Quest Types & Rewards]")
        {
            Tags = new[] { "quests", "unfolding-truth", "npcs" }
        },

        new("Quests & NPCs", "Agil's Apprentice (F55)",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Agil's Apprentice (BrightYellow 'G' glyph)\n" +
            "│ Floor: 55\n" +
            "│ Quest: Ground Gorge reclamation (15 kills)\n" +
            "│ Reward: Ground Gorge (FD 2H Axe, Agil canon)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "A quest NPC on F55 who gate-keeps Agil's signature FD weapon\n" +
            "Ground Gorge. Moves the drop out of the random banded pool\n" +
            "into a fixed canon placement — no more banded RNG for the axe.\n\n" +
            "USAGE\n" +
            "Bump the BrightYellow 'G' on F55. Accept the kill quest: 15\n" +
            "monsters on Floor 55. Return on completion for Ground Gorge.\n\n" +
            "EFFECTS\n" +
            "On turn-in:\n" +
            "  - Ground Gorge (Fractured Daydream, 2H Axe, Agil signature)\n" +
            "Auto-added to inventory; dropped at your feet if full.\n\n" +
            "COSTS\n" +
            "15 F55 kills. Axe proficiency recommended if you plan to\n" +
            "wield the reward — otherwise save it for an Agil ally run.\n\n" +
            "TIPS\n" +
            "Perfect canon loadout if you're recruiting Agil — hand the\n" +
            "axe to him via party gear. Double-dip with standing F55\n" +
            "weapon-gated Kill quests; the same 15 can resolve both.\n\n" +
            "SEE ALSO\n" +
            "[Fractured Daydream Character Weapons] · [Fractured Daydream Field Bosses] · [Recruitable Allies & Party System] · [Quest Types & Rewards]")
        {
            Tags = new[] { "quests", "npcs", "fractured-daydream" }
        },

        new("Quests & NPCs", "Hollow Fragment HNM Questgivers (F79-F99)",
            "┌─ Quests & NPCs\n" +
            "│ NPC: 13 Hollow-Fragment HNM givers\n" +
            "│ Floor: 79, 80, 81, 83, 84, 85, 88, 90, 91, 92, 95, 98, 99\n" +
            "│ Quest: Kill-count-on-this-floor chain\n" +
            "│ Reward: Named HNM weapon + Col/XP\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Thirteen Hollow Fragment HNM (High-Notorious-Monster, endgame\n" +
            "elite) weapon NPCs gate the late-endgame arsenal behind floor-\n" +
            "locked kill quests. All grant a canon HF weapon. With the F84/\n" +
            "F85/F92/F99 gap-fillers the Implement System (the canon HF\n" +
            "weapon-grant framework) is now content-complete F77-F99.\n\n" +
            "USAGE\n" +
            "Bump the NPC on their floor, complete the kill count on that\n" +
            "same floor, return for the weapon.\n\n" +
            "EFFECTS\n" +
            "  F79 Scholar Ellroy        15 kills  Infinite Ouroboros    400/300\n" +
            "  F80 Hunter Kojiro         15        Jato Onikirimaru      400/300\n" +
            "  F81 Ranger Torva          15        Fiendblade Deathbringr 450/320\n" +
            "  F83 Apiarist Nell         15        Fayblade Tizona       450/320\n" +
            "  F84 Spiralist Vey         10        Spiralblade:Rendering Fail\n" +
            "  F85 Crusher Drago         10        Crusher:Bond Cyclone (2H Axe)\n" +
            "  F88 Watcher Kael          20        Starmace Elysium      600/450\n" +
            "  F90 High Priestess Sola   20        Eurynome's Holy Sword 650/480\n" +
            "  F91 Torchbearer Meir      20        Saintspear Rhongomyniad 700/520\n" +
            "  F92 Auric Knight Halric   15        Aurumbrand: Hauteclaire\n" +
            "  F95 Elder Beastkeeper     25        Shinto Ama-no-Murakumo 800/600\n" +
            "  F98 Sentinel Captain      25        Godspear Gungnir      800/600\n" +
            "  F99 Last Herald Xiv       20        Deathglutton: Epetamu\n\n" +
            "COSTS\n" +
            "Kill-count is the only cost. Progress resets if you ascend\n" +
            "before turning in — no exceptions.\n\n" +
            "TIPS\n" +
            "Farm all thirteen on a single climb; build variety is the\n" +
            "reward. Crusher Drago (F85) shares the floor with Silent Edge\n" +
            "and Abased Beast — budget durability accordingly.\n\n" +
            "SEE ALSO\n" +
            "[Implement System Weapons (F77-F99)] · [Named Legendary Highlights] · [Divine Object Set — Integrity Knights] · [Quest Types & Rewards]")
        {
            Tags = new[] { "quests", "npcs", "hollow-fragment" }
        },

        new("Quests & NPCs", "Town of Beginnings NPCs (F1)",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Agil, Klein, Argo, Lisbeth, Silica, +3\n" +
            "│ Floor: 1 (hub)\n" +
            "│ Quest: Tutorial + vendor + info-broker\n" +
            "│ Reward: Dialogue, shop access, tips\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "F1's hand-built Town of Beginnings hosts eight fixed canon NPCs\n" +
            "— the core SAO cast plus supporting staff. They anchor the\n" +
            "tutorial flow and the early recruitment pool.\n\n" +
            "USAGE\n" +
            "Bump to trigger dialogue or a shop. Klein and Argo also wander\n" +
            "beyond F1 once the player climbs.\n\n" +
            "EFFECTS\n" +
            "  Agil            Vendor, \"Agil's General Store\"\n" +
            "  Klein           Tutorial dialogue (combat / progression /\n" +
            "                  survival)\n" +
            "  Argo the Rat    Information broker; tips on bosses,\n" +
            "                  proficiency, death\n" +
            "  Priest Tadashi  Flavor\n" +
            "  Nezha           Smith; points to anvil\n" +
            "  Lisbeth         Short canon dialogue\n" +
            "  Silica          Short canon dialogue\n" +
            "  Diavel          Short canon dialogue\n\n" +
            "Klein also appears on F2-F3; Argo on F3+ as wandering NPCs.\n\n" +
            "COSTS\n" +
            "None for dialogue. Agil's shop prices follow standard vendor\n" +
            "markup (+20%).\n\n" +
            "TIPS\n" +
            "Talk to Argo before every new era — her tips rotate with your\n" +
            "current floor. Klein and the F1 Lisbeth townsfolk become\n" +
            "recruitable once bumped outside F1. Agil's shop offers the\n" +
            "Anneal Blade craft line. NOTE: the F1 Lisbeth here is the\n" +
            "flavor/recruit NPC — the F48 Lindarth Lisbeth is a separate\n" +
            "crafting NPC that gates the Rarity 6 craft line.\n\n" +
            "SEE ALSO\n" +
            "[Anneal Blade Craft Line] · [Lindarth Town (F48)] · [Starting Loadout] · [Vendors — Rotating Stock] · [SAO Switch (Party)]")
        {
            Tags = new[] { "quests", "npcs", "shops" }
        },

        new("Quests & NPCs", "SAO Switch (Party)",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Any recruited ally\n" +
            "│ Floor: All (any party size >= 1)\n" +
            "│ Quest: Canonical SAO tactical move\n" +
            "│ Reward: Free position swap\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "The canonical SAO Switch — bump into a friendly ally to swap\n" +
            "positions with them instantly. The signature tactical move of\n" +
            "SAO's front-line parties.\n\n" +
            "USAGE\n" +
            "Walk into an ally's tile. Positions swap; the ally's current\n" +
            "status effects, cooldowns, and facing carry over intact.\n\n" +
            "EFFECTS\n" +
            "Useful for:\n" +
            "  - Pulling a wounded ally out of the front line\n" +
            "  - Putting a tank (Agil, Strea) into the path of a heavy attack\n" +
            "  - Repositioning around narrow corridors\n\n" +
            "Party size caps at 2 allies.\n\n" +
            "COSTS\n" +
            "None. The swap is free — no action point, no turn spent.\n\n" +
            "TIPS\n" +
            "Chain Switch with a Heavy-Attack telegraph: swap the tank\n" +
            "forward on the WINDING UP turn, let them Block/Parry, then\n" +
            "counter-swap back in for your combo window.\n\n" +
            "SEE ALSO\n" +
            "[Recruitable Allies & Party System] · [Heavy Attacks (Winding Up)] · [Defense — Block, Parry, Dodge] · [Combo Attacks]")
        {
            Tags = new[] { "quests", "npcs", "combat" }
        },

        new("Quests & NPCs", "Recruitable Allies & Party System",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Klein, Asuna, Agil, Silica, Lisbeth\n" +
            "│ Floor: Any (bump NPC outside F1)\n" +
            "│ Quest: Recruit dialog\n" +
            "│ Reward: Party slot (max 2 allies)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Recruit up to 2 SAO canon allies to fight alongside you. Each\n" +
            "scales to your level, carries a weapon-typed combat role, and\n" +
            "can be commanded via behavior flags.\n\n" +
            "USAGE\n" +
            "Bump Klein, Asuna, Agil, Silica, or Lisbeth OUTSIDE Floor 1 to\n" +
            "open the recruit dialog. Set behavior (Aggressive / Defensive /\n" +
            "Follow) from the party UI.\n\n" +
            "EFFECTS\n" +
            "  Klein     Katana, Samurai\n" +
            "  Asuna     Rapier, The Flash\n" +
            "  Agil      Axe\n" +
            "  Silica    Dagger, Dragon Tamer\n" +
            "  Lisbeth   Mace, Blacksmith\n\n" +
            "Ally stats scale to playerLevel-1 with 80 + playerLevel*8 HP.\n" +
            "Bump an ally to perform an SAO Switch (position swap). KO'd\n" +
            "allies auto-revive at 50% HP on floor change.\n\n" +
            "COSTS\n" +
            "No Col or XP. The Solo run modifier forbids recruits entirely.\n\n" +
            "TIPS\n" +
            "Pair Asuna (ranged Rapier pressure) with Agil (Axe tank) for a\n" +
            "front/back composition. Silica's Dragon Tamer extra-hit stacks\n" +
            "with Combo Attacks for fast takedowns.\n\n" +
            "SEE ALSO\n" +
            "[SAO Switch (Party)] · [Town of Beginnings NPCs (F1)] · [Run Modifiers (12 Optional Challenges)] · [Combo Attacks]")
        {
            Tags = new[] { "quests", "npcs", "combat" }
        },

        new("Quests & NPCs", "Vendors — Rotating Stock",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Floor Vendor (green 'V')\n" +
            "│ Floor: 2+ (every non-town floor)\n" +
            "│ Quest: Rotating shop stock\n" +
            "│ Reward: Consumables, gear, accessories\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "One vendor per non-town floor. Stock rotates per run and scales\n" +
            "with floor — the shop's name even renames as you climb.\n\n" +
            "USAGE\n" +
            "Bump the green 'V'. Shop name scales:\n" +
            "  F<=5   \"General Store\"\n" +
            "  F<=10  \"Adventurer's Supply\"\n" +
            "  F>10   \"Elite Outfitters\"\n\n" +
            "EFFECTS\n" +
            "Always stocked:\n" +
            "  2x Health Potion, Antidote, Bread, Grilled Meat\n" +
            "Tier unlocks:\n" +
            "  F2+  Greater Health Potion, Honey Bread, Spiced Jerky,\n" +
            "        Fire Bomb\n" +
            "  F3+  Fish Stew, Speed Potion, Iron Skin Potion, Smoke Bomb,\n" +
            "        Poison Vial, Escape Rope\n" +
            "  F4+  Elven Waybread, Flash Bomb, Revive Crystal\n" +
            "  F5+  1 random accessory\n\n" +
            "Plus 3-4 random floor-scaled weapons, 1-2 armors.\n\n" +
            "COSTS\n" +
            "All prices marked up +20% over base.\n\n" +
            "TIPS\n" +
            "Stock Revive Crystals and Escape Ropes before entering a\n" +
            "Labyrinth — they're cheaper on the floor you find them than\n" +
            "carrying them from F1. Accessories at F5+ are random per run,\n" +
            "so revisit shops if you're hunting a specific slot.\n\n" +
            "SEE ALSO\n" +
            "[Col Economy — How You Earn] · [Potions, Crystals & Throwables] · [Accessories] · [Food & Cooking]")
        {
            Tags = new[] { "npcs", "shops", "economy" }
        },

        new("Quests & NPCs", "Col Economy — How You Earn",
            "┌─ Quests & NPCs\n" +
            "│ NPC: N/A (systemic)\n" +
            "│ Floor: All\n" +
            "│ Quest: Col source reference\n" +
            "│ Reward: Col faucets; 25% sink on death\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Col is earned from mobs, chests, speed and exploration bonuses,\n" +
            "quests, and achievements. Dying on non-Hardcore loses 25%;\n" +
            "Hardcore wipes the save entirely.\n\n" +
            "USAGE\n" +
            "Passive — every income source logs Col to your wallet as it\n" +
            "fires. Open the inventory screen to see totals.\n\n" +
            "EFFECTS\n" +
            "  Mob kills       ColYield + Perfect Kill +50% (no damage taken)\n" +
            "                  + streak tier (up to 3 tiers every 5 kills)\n" +
            "  Floor boss      ColYield + guaranteed drop\n" +
            "  Chests          20 + 15*floor + 0-19 Col; x1.5 for \"far\" chests\n" +
            "  Speed clear     50 + 30*floor Col if elapsed <= par\n" +
            "                  Par: {200,220,250,280,320,360,400,450,500,550}\n" +
            "                  capped at F10 value\n" +
            "  Exploration     +50*floor XP, +100*floor Col if >=90% explored\n" +
            "                  on ascend\n" +
            "  Quests          Per quest type formula\n" +
            "  Achievements    50-10000 Col per milestone\n" +
            "  Bounty          100 + 50*floor Col\n\n" +
            "COSTS\n" +
            "DEATH PENALTY: lose 25% Col on non-Hardcore death. Hardcore wipes\n" +
            "the save.\n\n" +
            "TIPS\n" +
            "Perfect Kill streaks stack on mob yield — don't break cover if\n" +
            "you're farming a floor. Deposit Col into gear before risky\n" +
            "fights; equipped gear isn't a Col holding and can't be stolen by\n" +
            "the death penalty.\n\n" +
            "SEE ALSO\n" +
            "[Kill Streaks] · [Achievements] · [Ascending a Floor] · [Death Penalty & Hardcore]")
        {
            Tags = new[] { "economy", "progression", "hardcore" }
        },

        new("Quests & NPCs", "Achievements",
            "┌─ Quests & NPCs\n" +
            "│ NPC: N/A (milestone system)\n" +
            "│ Floor: All\n" +
            "│ Quest: Auto-unlocked milestones\n" +
            "│ Reward: 50-10000 Col per milestone\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Auto-tracked milestones that pay Col on unlock. No grinding UI —\n" +
            "the game watches your kill log, floor tally, and streak state\n" +
            "and fires rewards on the spot.\n\n" +
            "USAGE\n" +
            "No action required. Milestones pop on the ascend screen or the\n" +
            "instant the criterion is met mid-run.\n\n" +
            "EFFECTS\n" +
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
            "  Iron Will           survive a hemorrhage        400\n\n" +
            "COSTS\n" +
            "None. Achievements are pure upside.\n\n" +
            "TIPS\n" +
            "Loremaster is the best-hidden payout (+1500 Col) — comb every\n" +
            "floor for Lore Stones. Iron Will requires surviving a Bleed +\n" +
            "Poison hemorrhage burst; pack an Antidote Crystal on biomes\n" +
            "that proc both.\n\n" +
            "SEE ALSO\n" +
            "[Kill Streaks] · [Status: Bleed & Poison] · [Lore, Journals & Enchant Shrines] · [Col Economy — How You Earn]")
        {
            Tags = new[] { "economy", "progression", "xp" }
        },

        new("Quests & NPCs", "Save System",
            "┌─ Quests & NPCs\n" +
            "│ NPC: N/A (system)\n" +
            "│ Floor: All (auto-save on ascend)\n" +
            "│ Quest: Save / load / migration flow\n" +
            "│ Reward: Persistence across sessions\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Three save slots, auto-save on every floor ascend, and a legacy-\n" +
            "save auto-migration path. Hardcore deletes the save on death.\n\n" +
            "USAGE\n" +
            "Files live in %LOCALAPPDATA%/AincradTRPG/save_N.json. Legacy\n" +
            "save.json auto-migrates to slot 1. Auto-save fires on every\n" +
            "floor ascend; manual save from the pause menu works any time.\n\n" +
            "EFFECTS\n" +
            "Saved state includes:\n" +
            "  - Player stats / inventory / equipment\n" +
            "  - Kill counts / turn counter\n" +
            "  - Active + completed quests\n" +
            "  - Achievements\n" +
            "  - Unique skills\n" +
            "  - Party members (with behaviors)\n" +
            "  - Tutorial tips seen\n" +
            "  - Discovered lore\n" +
            "  - Faction reputation\n" +
            "  - Defeated field bosses\n" +
            "  - ACTIVE RUN MODIFIERS\n" +
            "  - Equipped skills + cooldowns\n" +
            "  - Story flags / events\n\n" +
            "Slot summary shows name, level, floor, difficulty, hardcore\n" +
            "flag, timestamp, and playtime.\n\n" +
            "COSTS\n" +
            "None for normal saves. Hardcore saves cost a save slot on death.\n\n" +
            "TIPS\n" +
            "Back up %LOCALAPPDATA%/AincradTRPG manually before a big\n" +
            "Hardcore push if you want insurance. The auto-save is\n" +
            "overwrite-in-place, so you can't roll back a bad ascend.\n\n" +
            "SEE ALSO\n" +
            "[Death Penalty & Hardcore] · [Run Modifiers (12 Optional Challenges)] · [Ascending a Floor] · [Achievements]")
        {
            Tags = new[] { "save", "hardcore", "progression" }
        },
    };
}
