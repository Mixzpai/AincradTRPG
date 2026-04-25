namespace SAOTRPG.UI.Dialogs;

// Static Player Guide content (hotkey B) — flat (Category, Title, Body) list.
public static class PlayerGuideContent
{
    // Tags enable polyhierarchy — topic shows under its Category + each "Tag:" root.
    public record GuideEntry(string Category, string Title, string Body)
    {
        public string[] Tags { get; init; } = System.Array.Empty<string>();
    }

    public static readonly GuideEntry[] Entries =
    {
        // ── 1. Combat & Rarity ──

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
            "[Critical Hits] · [Combo Attacks] · [Weapon Proficiency Ranks] · [Rarity Colors & Glyphs] · [Damage & Toast Feedback] · [Damage Breakdown Format] · [Combat Visual Feedback] · [Damage Type Tags] · [Status Icon Tray] · [Particle Effects]")
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
            "│ Count: 17 total\n" +
            "│ Source: 8 NPC-quest Divines + 9 T4 chain\n" +
            "│ Trigger: Hand-placed, never random\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Peak rarity gear. All 17 Divine weapons are hand-placed — 7\n" +
            "canon Integrity Knight swords, Dorothy's Starlight Banner\n" +
            "(Last Recollection, F78), and 9 Evolution Chain T4 apex\n" +
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
            "and HP over turns. The Eating life skill scales food potency by\n" +
            "+10% / +25% / +50% / +100% at L10 / L25 / L50 / L99. At L99\n" +
            "Eating also scales food's HP REGEN RATE — the per-turn heal\n" +
            "ticks higher in addition to the existing Satiety duration boost,\n" +
            "so Roast Boar with high Eating turns into a passive heal stream\n" +
            "during exploration rather than a one-shot top-up.\n\n" +
            "COSTS\n" +
            "Satiety drains 1 point per N turns (biome- and modifier-\n" +
            "dependent). The Iron Rank run modifier doubles drain rate.\n\n" +
            "TIPS\n" +
            "Hover at Well Fed before any boss — the +3 ATK/DEF feeds right\n" +
            "into Damage Mitigation. Carry at least one stack of cheap bread\n" +
            "for emergency top-ups; each bread also banks Eating XP toward\n" +
            "the food-potency milestones. Once Eating hits L99, swap your\n" +
            "between-fight grazing food from cheap bread up to Roast Boar or\n" +
            "stew for the duration-PLUS-regen-rate stack.\n\n" +
            "SEE ALSO\n" +
            "[Food & Cooking] · [Damage Mitigation] · [Run Modifiers (12 Optional Challenges)] · [Life Skills] · [Campfires — Rest & Sleep XP]")
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
            "normal step moves do. Each sprint step banks +2 Running XP;\n" +
            "each normal step banks +1 Walking XP (sprint and stealth skip\n" +
            "the Walking track).\n\n" +
            "COSTS\n" +
            "Sprint adds +50% satiety drain for the turn spent.\n\n" +
            "TIPS\n" +
            "Use Sprint to escape post-motion recovery before the follow-up\n" +
            "hit lands. Use Stealth Move when scouting unfamiliar rooms so\n" +
            "you don't pull the whole pack at once. Alternate sprint and\n" +
            "normal steps on long treks — both life skills bank XP on the\n" +
            "same trip.\n\n" +
            "SEE ALSO\n" +
            "[Controls & Keybindings] · [Heavy Attacks (Winding Up)] · [Unique Skill: Extra Skill — Search] · [Traps & Hazards] · [Life Skills]")
        {
            Tags = new[] { "combat", "movement", "controls" }
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
            "[Quickbar & Consumables] · [Controls & Keybindings] · [Potions, Crystals & Throwables] · [Status: Bleed & Poison] · [Run Modifiers (12 Optional Challenges)] · [Categorized Combat Log]")
        {
            Tags = new[] { "combat", "potions", "controls" }
        },

        new("Combat & Rarity", "Quickbar & Consumables",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Quickbar & Consumables\n" +
            "│ Slots: 10 hotbar slots rendered in the bottom HUD row\n" +
            "│ Keys: 0-9 use · Shift+0-9 bind (in inventory)\n" +
            "│ Trigger: Direct consume, no menu\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "The quickbar is a ten-slot consumable hotbar rendered inline on\n" +
            "the bottom HUD row, sitting alongside the F1-F4 sword-skill\n" +
            "slots. Digit keys 0-9 use the bound consumable directly; no\n" +
            "menu, no turn drop-in, no inventory round-trip.\n\n" +
            "USAGE\n" +
            "Number keys 0-9 fire the bound slot mid-combat. One stock is\n" +
            "consumed from your pack if available; nothing happens if empty.\n" +
            "Slots auto-fill on first pickup of a new consumable type, so a\n" +
            "fresh character with an empty bar fills out organically as you\n" +
            "pick drops up. To rebind a slot manually, open the inventory\n" +
            "(I), highlight the consumable you want, and press Shift+N — the\n" +
            "dialog prompts for a slot 0-9. Chest peeks show the pickup\n" +
            "destination slot inline so you can see where a picked-up item\n" +
            "will land before you grab it.\n\n" +
            "EFFECTS\n" +
            "The bottom HUD row lays out like this:\n" +
            "  [F1][F2][F3][F4]  [0][1][2][3][4][5][6][7][8][9]\n" +
            "   sword skills      consumable quickbar\n" +
            "F1-F4 are sword-skill slots; 0-9 are consumable slots. The two\n" +
            "groups are never overloaded — no key does double duty. Empty\n" +
            "slots render dim with no glyph; filled slots show the item\n" +
            "glyph and a stock count. The active quickbar persists across\n" +
            "saves, so rebinds carry between sessions of the same run.\n\n" +
            "COSTS\n" +
            "Quickbar use consumes one stock per press — same as using the\n" +
            "item from the inventory dialog. The Anti-Crystal Tyranny run\n" +
            "modifier still disables Crystal-based consumables; bound slots\n" +
            "for those items flash a brief \"blocked\" tint on press.\n\n" +
            "TIPS\n" +
            "Auto-fill picks the first empty slot, so the order you pick\n" +
            "items up becomes your muscle-memory order by default. For a\n" +
            "tight boss loadout, bind Shift+N: healing to 0-2, buffs to 3-5,\n" +
            "utility/escape to 6-9 — that way the most-used keys are closest\n" +
            "to the resting position. A Crystal on the top row is cheap\n" +
            "insurance; forgetting to bind one is the #1 avoidable death.\n\n" +
            "SEE ALSO\n" +
            "[Quick-Use Slots (1-5)] · [Potions, Crystals & Throwables] · [Controls & Keybindings] · [Damage & Toast Feedback] · [Gear Compare]")
        {
            Tags = new[] { "combat", "potions", "controls", "ui" }
        },

        new("Combat & Rarity", "Damage & Toast Feedback",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Damage & Toast Feedback\n" +
            "│ Popups: 0.4s fade, 1-cell travel, element tint\n" +
            "│ Toasts: Center banner, 3s TTL, 1 at a time\n" +
            "│ Trigger: Every hit · milestone events\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Two subtle visual layers keep combat legible without cluttering\n" +
            "the map. Damage popups float a single cell upward and fade in\n" +
            "0.4s at the hit tile, tinted by element. Toast banners appear\n" +
            "center-screen for 3 seconds on milestone events (level ups,\n" +
            "titles, achievements, etc).\n\n" +
            "USAGE\n" +
            "Both are automatic — nothing to bind. Popups show on every\n" +
            "meaningful swing (yours and theirs); toasts fire on the milestone\n" +
            "trigger list below. Only one toast is on-screen at a time; a new\n" +
            "toast of the same category within 500ms merges into the current\n" +
            "banner instead of queueing behind it.\n\n" +
            "EFFECTS\n" +
            "ELEMENT COLOR KEY (popup tint):\n" +
            "  Physical   White        baseline melee/ranged\n" +
            "  Fire       Red          burn, flame, ember hits\n" +
            "  Thunder    Yellow       shock, lightning, storm\n" +
            "  Water      Blue         ice, frost, tidal\n" +
            "  Holy       Gold         divine, sacred, Integrity\n" +
            "  Dark       Magenta      shadow, void, corruption\n" +
            "CRIT DIFFERENTIATOR:\n" +
            "  A crit keeps its element tint but brightens one shade AND\n" +
            "  prefixes the number with a diamond glyph: ◇123 instead of\n" +
            "  the plain 123. You see the colour shift from across the map.\n" +
            "SUPPRESSION (reduces visual noise):\n" +
            "  - Tiny chip damage (< 1% of the defender's max HP) is\n" +
            "    omitted entirely — no popup fires.\n" +
            "  - DoT effects (Bleed / Poison / Burn) show ONLY the\n" +
            "    first-tick popup per application; subsequent ticks are\n" +
            "    silent.\n" +
            "TOAST CATEGORIES (center-screen banner triggers):\n" +
            "  - Level up\n" +
            "  - Title earned / equipped\n" +
            "  - Stat-up milestone\n" +
            "  - Sword skill unlock\n" +
            "  - Achievement unlocked\n" +
            "  - Quest complete\n" +
            "  - Floor boss cleared\n" +
            "  - Shop tier unlocked (F50+)\n" +
            "  - Bestiary species-first sighting\n\n" +
            "COSTS\n" +
            "Zero gameplay cost — both layers are presentation only. No\n" +
            "turns pass for either popup or toast; they animate in parallel\n" +
            "with the normal turn clock.\n\n" +
            "TIPS\n" +
            "Use the element tint as a quick sanity check that your weapon's\n" +
            "element is actually applying — if a fire-enchant sword draws\n" +
            "white numbers, something stripped the element (wet biome, water\n" +
            "immunity, etc.). The diamond crit glyph is the fastest read for\n" +
            "\"was that swing actually a crit?\" without scrolling the log.\n\n" +
            "SEE ALSO\n" +
            "[Critical Hits] · [Damage Formula] · [Combo Attacks] · [Quickbar & Consumables] · [Gear Compare] · [Achievements] · [Look Mode & Counter Stance] · [Combat Visual Feedback] · [Damage Breakdown Format]")
        {
            Tags = new[] { "combat", "ui", "crit", "toast" }
        },

        new("Combat & Rarity", "Combat Visual Feedback",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Combat Visual Feedback\n" +
            "│ Shake: 100ms / 3 frames on high-impact hits\n" +
            "│ Projectiles: Arrows · skill arcs · status trails\n" +
            "│ Multi-hit: Per-hit popups + aggregate summary\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "A trio of layered effects sells the weight of a swing without\n" +
            "slowing the turn clock. Screen shake punches crits, boss heavy\n" +
            "attacks, explosions, and floor-quakes. Animated projectiles\n" +
            "trace arrows, multi-hit sword-skill arcs, and status trails\n" +
            "across the map. Multi-hit cascades (e.g. Starburst Stream's 16\n" +
            "hits) fire a fast per-popup stream plus a single aggregate\n" +
            "damage summary at the end.\n\n" +
            "USAGE\n" +
            "All three layers are automatic. Each one can be toggled\n" +
            "independently in Options > Accessibility if motion sensitivity\n" +
            "or visual noise is a concern.\n\n" +
            "EFFECTS\n" +
            "SCREEN SHAKE (trigger list):\n" +
            "  - Critical hits (yours or theirs)\n" +
            "  - Boss heavy attacks (winding-up release)\n" +
            "  - Explosion tiles and fire-bomb detonations\n" +
            "  - Floor-wide quake events (labyrinth collapse, boss phase)\n" +
            "PROJECTILE ANIMATIONS:\n" +
            "  - Arrows trace a single-frame glyph tile-to-tile\n" +
            "  - Sword-skill arcs sweep along the hit path\n" +
            "  - Status trails (poison mist, burn wisps) dot the route of\n" +
            "    any status-inflicting swing\n" +
            "MULTI-HIT CASCADE:\n" +
            "  - Each hit of a multi-hit skill fires its own damage popup\n" +
            "  - A single bolder aggregate summary prints at the end so the\n" +
            "    total is legible at a glance\n\n" +
            "COSTS\n" +
            "Zero gameplay cost — presentation only. Screen shake is bounded\n" +
            "to 100ms / 3 frames to stay under motion-sickness thresholds.\n\n" +
            "TIPS\n" +
            "Turn shake off in Options > Accessibility if it causes\n" +
            "discomfort; the same menu hosts projectile and trail toggles.\n" +
            "Watching the arc color of a multi-hit skill is the fastest way\n" +
            "to confirm which element actually landed.\n\n" +
            "SEE ALSO\n" +
            "[Damage & Toast Feedback] · [Critical Hits] · [Heavy Attacks (Winding Up)] · [Sword Skills — Unlock & Use] · [Damage Breakdown Format] · [Controls & Keybindings] · [Particle Effects]")
        {
            Tags = new[] { "combat", "ui", "accessibility" }
        },

        new("Combat & Rarity", "Damage Breakdown Format",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Damage Breakdown Format\n" +
            "│ Modes: Off · Concise · Medium · Verbose\n" +
            "│ Default: Concise\n" +
            "│ Set via: Options > Accessibility (or Gameplay)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "A four-mode toggle controls how much math the log shows after\n" +
            "each hit. Off hides the breakdown entirely; Concise shows only\n" +
            "the final number; Medium adds the major components; Verbose\n" +
            "exposes every multiplier for theory-crafting.\n\n" +
            "USAGE\n" +
            "Cycle the mode in Options > Accessibility or Gameplay. The\n" +
            "selected format renders in the combat log immediately — no\n" +
            "restart needed. A live preview of each format sits next to the\n" +
            "toggle so you can compare before committing.\n\n" +
            "EFFECTS\n" +
            "SAMPLE OUTPUT (same 87-damage swing in each mode):\n" +
            "  Off      — no breakdown line, just the kill/hit message\n" +
            "  Concise  — \"87 dmg\"\n" +
            "  Medium   — \"87 dmg (ATK 42 + prof 12 + combo 8, x1.3 crit)\"\n" +
            "  Verbose  — \"87 dmg = (ATK 42 + prof 12 + combo 8 + shrine 4\n" +
            "             + surge 3) x 1.3 crit x 1.15 biome x 1.0 unique\"\n\n" +
            "COSTS\n" +
            "None — log verbosity does not affect combat math. Verbose mode\n" +
            "does push more log lines per swing, which is the main trade-off.\n\n" +
            "TIPS\n" +
            "Concise is ideal for fast casual play — the number is all you\n" +
            "need. Medium is the best default for learning the combat system\n" +
            "without drowning in digits. Verbose is the theory-crafting mode:\n" +
            "leave it on when testing gear, stat allocations, or biome\n" +
            "synergies so you can trace exactly which multiplier moved.\n\n" +
            "SEE ALSO\n" +
            "[Damage Formula] · [Damage & Toast Feedback] · [Combat Visual Feedback] · [Critical Hits] · [The Six Attributes] · [Damage Type Tags]")
        {
            Tags = new[] { "combat", "ui", "accessibility", "log" }
        },

        new("Combat & Rarity", "Damage Type Tags",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Damage Type Tags\n" +
            "│ Physical: SLASH · THRUST · BLUNT · PIERCE · CUT\n" +
            "│ Elemental: FIRE · ICE · THUNDER · HOLY · DARK · POISON · BLEED\n" +
            "│ Gate: Breakdown mode ≠ Off\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Every combat-log line can tag which damage channel a hit used,\n" +
            "so [SLASH] reads differently from [PIERCE] and [FIRE] stands out\n" +
            "from [HOLY]. Tags appear only while Damage Breakdown Format is\n" +
            "something other than Off.\n\n" +
            "USAGE\n" +
            "The tag renders in color next to the damage number — position\n" +
            "(before vs after the number) and style (bracketed [SLASH] vs\n" +
            "bare SLASH) are both toggleable in Options. Turn Breakdown Format\n" +
            "back to Off to hide the tags entirely.\n\n" +
            "EFFECTS\n" +
            "PHYSICAL SUBTYPES (derived from weapon class):\n" +
            "  SLASH   White       longswords, sabers, katana cuts\n" +
            "  THRUST  Light-gray  rapiers, spears, estoc\n" +
            "  BLUNT   Dark-gray   maces, warhammers, brawler strikes\n" +
            "  PIERCE  White       arrows, crossbow bolts, daggers\n" +
            "  CUT     White       twin-blade reverse edges, hollow tears\n" +
            "ELEMENTAL KEY:\n" +
            "  FIRE    Red         burn, flame, ember hits\n" +
            "  ICE     Blue        frost, freeze, chill\n" +
            "  THUNDER Yellow      shock, lightning, storm\n" +
            "  HOLY    Gold        divine, sacred, Integrity Knights\n" +
            "  DARK    Magenta     shadow, void, corrupted weapons\n" +
            "  POISON  Green       toxin, venom, spore DoT\n" +
            "  BLEED   DarkRed     laceration DoT\n\n" +
            "COSTS\n" +
            "Presentation only — tags do not alter damage math. Verbose\n" +
            "breakdown with tags enabled is the densest log mode; switch to\n" +
            "Concise if the log feels noisy.\n\n" +
            "TIPS\n" +
            "Tags are the fastest sanity check that a weapon enchant is\n" +
            "landing — a fire-enchanted katana should show [SLASH][FIRE];\n" +
            "only [SLASH] means the element was stripped by immunity or\n" +
            "biome. Flip position to after-the-number if your eye tracks the\n" +
            "damage digits first.\n\n" +
            "SEE ALSO\n" +
            "[Damage Formula] · [Damage Breakdown Format] · [Damage & Toast Feedback] · [Status: Bleed & Poison] · [Weapon Types Overview] · [Elemental Weapon Variants]")
        {
            Tags = new[] { "combat", "ui", "log", "accessibility" }
        },

        new("Combat & Rarity", "Status Icon Tray",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Status Icon Tray\n" +
            "│ Location: Bottom HUD row, below HP bar\n" +
            "│ Format: Letter + color, stack×duration (P×3:4)\n" +
            "│ Verbose toggle: Shift+S (session-local)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "A compact tray beneath the HP bar aggregates every active\n" +
            "status source — debuffs, buffs, and passive stances — into\n" +
            "one-letter color-coded icons. Stacks and remaining duration\n" +
            "render as `P×3:4` meaning 3 poison stacks with 4 turns left.\n\n" +
            "USAGE\n" +
            "Automatic — icons appear/expire with their underlying effects.\n" +
            "Shift+S toggles verbose mode, which swaps the single letters\n" +
            "for short labels (POISON·3 4t) until you reload the save.\n" +
            "Ordering is debuff-first severity, so the scariest effect sits\n" +
            "leftmost and can't hide behind a cosmetic buff.\n\n" +
            "EFFECTS\n" +
            "ICON KEY (letter · color · source):\n" +
            "  P  Green      Poison DoT\n" +
            "  B  Red        Bleed DoT\n" +
            "  S  Yellow     Stun (skip turn)\n" +
            "  L  Cyan       Slow (halves dodge)\n" +
            "  Z  LightBlue  Freeze (immobilize)\n" +
            "  X  Magenta    Blind (miss chance)\n" +
            "  F  Gold       Well Fed\n" +
            "  H  Gray       Hunger\n" +
            "  E  Cyan       Pair Resonance (dual-wield)\n" +
            "  K  Bronze     Counter Stance (queued parry)\n" +
            "  W  LightBlue  Winter weather affinity\n" +
            "  V  Red        Volcano biome affinity\n" +
            "  G  Green      Toxic biome affinity\n" +
            "  R  Green      Regen tick buff\n" +
            "  C  Yellow     Crit-Up buff\n" +
            "  D  Cyan       Dodge-Up buff\n\n" +
            "COSTS\n" +
            "None — display only. The tray never hides behind popups or\n" +
            "projectiles; it updates in-line each frame.\n\n" +
            "TIPS\n" +
            "Turn verbose on with Shift+S the first few runs to memorize the\n" +
            "single-letter key; flip back to compact once the shapes read at\n" +
            "a glance. If the tray fills, the leftmost slot is always the\n" +
            "thing about to kill you — clear that first.\n\n" +
            "SEE ALSO\n" +
            "[Status: Bleed & Poison] · [Status: Stun & Slow] · [Hunger, Satiety & Fatigue] · [Look Mode & Counter Stance] · [Paired Dual-Wield Weapons] · [Controls & Keybindings]")
        {
            Tags = new[] { "combat", "ui", "status", "accessibility" }
        },

        new("Combat & Rarity", "Particle Effects",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Particle Effects\n" +
            "│ Events: 10 combat / world triggers\n" +
            "│ Density: Off · Subtle · Moderate · Pronounced\n" +
            "│ Default: Pronounced\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Short-lived tile particles punctuate combat and world events —\n" +
            "sparks on metal-on-metal parries, blood specks on a cut, embers\n" +
            "off a fire hit. Ten event types are wired up; the density slider\n" +
            "in Options > Accessibility scales how many particles each event\n" +
            "spawns, or disables them outright.\n\n" +
            "USAGE\n" +
            "Automatic. Adjust the slider to taste — Pronounced is the\n" +
            "default; Moderate trims counts ~40%; Subtle is near-silent;\n" +
            "Off skips spawning entirely. The setting hot-applies with no\n" +
            "restart needed.\n\n" +
            "EFFECTS\n" +
            "TEN TRIGGER EVENTS:\n" +
            "  1. Critical-hit spark burst\n" +
            "  2. Block / parry spark trail\n" +
            "  3. Cut / bleed blood specks\n" +
            "  4. Fire hit ember puff\n" +
            "  5. Ice hit frost shards\n" +
            "  6. Thunder hit static arcs\n" +
            "  7. Mob death dust plume\n" +
            "  8. Level-up rising motes\n" +
            "  9. Biome ambient drift (volcano ash, winter flakes)\n" +
            " 10. Item-pickup sparkle\n" +
            "Z-ORDER (bottom to top):\n" +
            "  tile glyphs → particles → projectiles → damage popups →\n" +
            "  toast banner. Particles always yield to the popup stream and\n" +
            "  projectiles so damage numbers stay legible.\n\n" +
            "COSTS\n" +
            "Zero gameplay cost. Motion-sensitive players should drop the\n" +
            "slider to Subtle or Off — particle motion is the first layer to\n" +
            "strip before touching screen shake.\n\n" +
            "TIPS\n" +
            "Pronounced is tuned to read clearly on a 24x80 viewport; on\n" +
            "very dense maps (labyrinth interiors, boss rooms) Moderate is\n" +
            "easier on the eye without losing the feedback. Biome drift is\n" +
            "the one category you can mute by standing under roofed tiles.\n\n" +
            "SEE ALSO\n" +
            "[Combat Visual Feedback] · [Damage & Toast Feedback] · [Critical Hits] · [Biomes] · [Weather] · [Controls & Keybindings]")
        {
            Tags = new[] { "combat", "ui", "accessibility", "particles" }
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
            "[Look Mode & Counter Stance] · [Traps & Hazards] · [Biomes] · [Ambient World Animation]")
        {
            Tags = new[] { "combat", "vision" }
        },

        new("Combat & Rarity", "Look Mode & Counter Stance",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Look Mode & Counter Stance\n" +
            "│ Look: L key, cursor inspect + target list panel\n" +
            "│ Stance: V key, forces Parry\n" +
            "│ Trigger: Manual hotkey\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Two utility hotkeys: Look Mode inspects tiles and enemies\n" +
            "without moving; Counter Stance trades your turn for an\n" +
            "auto-Parry on the next incoming hit.\n\n" +
            "USAGE\n" +
            "Press L to enter Look Mode. Yellow brackets wrap the selected\n" +
            "target; a right-side Target List panel shows all visible\n" +
            "hostiles with HP bars, distance, and threat color. Tab/Arrows\n" +
            "cycle, T cycles the sort (Dist -> HP -> Threat -> Level ->\n" +
            "Index), and 1-9 jump directly to a numbered target. Esc or\n" +
            "L again exits. Press V to enter Counter Stance.\n\n" +
            "EFFECTS\n" +
            "Look Mode shows each target's name, HP, stats, skills, and\n" +
            "status without advancing time. Threat color reflects the\n" +
            "target's level versus yours (grey/green/white/yellow/red).\n" +
            "Counter Stance auto-Parries the next incoming hit for a 25%\n" +
            "Attack counter-strike.\n\n" +
            "COSTS\n" +
            "Look Mode is free (no turn consumed). Counter Stance consumes\n" +
            "1 turn. The stance ends after one incoming hit or one turn,\n" +
            "whichever comes first.\n\n" +
            "TIPS\n" +
            "Sort by HP to finish off low-HP stragglers first. Sort by\n" +
            "Threat before engaging a mixed group to identify the deadliest\n" +
            "target. Look Mode before any fight is the safest scouting tool\n" +
            "in the game. Stance shines when a boss telegraphs Winding Up\n" +
            "and you can't reposition.\n\n" +
            "SEE ALSO\n" +
            "[Controls & Keybindings] · [Heavy Attacks (Winding Up)] · [Defense — Block, Parry, Dodge] · [Vision & FOV] · [Bestiary — Monster Compendium] · [Damage & Toast Feedback] · [Combat Visual Feedback]")
        {
            Tags = new[] { "combat", "vision", "controls" }
        },

        new("Combat & Rarity", "Bestiary — Monster Compendium",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Bestiary — Monster Compendium\n" +
            "│ Hotkey: Y (closes with Y or Esc)\n" +
            "│ Scope: 199 entries — 66 mobs + 100 bosses + 33 field bosses\n" +
            "│ Persistence: Survives permadeath via lifetime_stats.json\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "A browsable compendium of every mob you've fought or even just\n" +
            "glimpsed, cross-referenced with kill counts, drop hints, threat\n" +
            "rating, and canon SAO flavor lore. Bundle 10 locks the roster\n" +
            "count at 199 — 66 standard mobs across the floor pool, 100\n" +
            "floor bosses (one per floor), and 33 field bosses scattered\n" +
            "across the eras. Entries unlock on first sighting and persist\n" +
            "permanently across runs — permadeath wipes the save, not the\n" +
            "Bestiary.\n\n" +
            "USAGE\n" +
            "Press Y on the map to open. Navigate with:\n" +
            "  Up / Down         Select entry in the left-hand list\n" +
            "  Tab               Cycle detail sub-tabs (Stats / Drops /\n" +
            "                    Lore / Records)\n" +
            "  S                 Cycle sort (Name -> Kills -> Floor ->\n" +
            "                    Threat -> First Seen)\n" +
            "  F                 Cycle filter mode\n" +
            "  /                 Type-ahead search by name\n" +
            "  B                 Toggle boss-only view\n" +
            "  U                 Toggle show-undiscovered placeholders\n" +
            "  C                 Clear all filters and search\n" +
            "  Y / Esc           Close\n\n" +
            "EFFECTS\n" +
            "Each entry tracks:\n" +
            "  - Floor range where the mob appears\n" +
            "  - Lifetime kills and total encounters\n" +
            "  - Cross-run deaths-caused (how often this mob has killed\n" +
            "    a character of yours)\n" +
            "  - Known drops and their rarity tiers (as you observe them)\n" +
            "  - Threat rating vs. your current level\n" +
            "  - Canon SAO lore flavor text (unlocked with the sighting)\n\n" +
            "COSTS\n" +
            "None. The Bestiary is a pause-style overlay — no turns pass\n" +
            "while it is open.\n\n" +
            "BUNDLE 10 IMPROVEMENTS\n" +
            "  - Loot tags now PERSIST across runs (carried in lifetime_\n" +
            "    stats.json alongside kills/sightings) — drops you've\n" +
            "    confirmed in run 3 still show in run 7's Bestiary.\n" +
            "  - Boss / field-boss / elite flags now stamp on FIRST DAMAGE\n" +
            "    instead of first aggro. Insta-kill openers (Holy Sword\n" +
            "    burst, Iaijutsu first-strike, sword-skill alpha on a\n" +
            "    surprised target) used to skip the flag — these no longer\n" +
            "    leak past your compendium.\n\n" +
            "TIPS\n" +
            "Sort by kills to see your favorite targets at a glance.\n" +
            "Boss-only filter shows your canon SAO bosses — useful for\n" +
            "canon completion hunting and planning floor-by-floor pushes.\n" +
            "Bestiary knowledge persists across runs — you keep what\n" +
            "you've seen even through permadeath, so every run chips\n" +
            "away at the undiscovered list. Track the 199 total against\n" +
            "your seen-count: at 199/199 you've completed the canonical\n" +
            "encounter sweep across all 100 floors.\n\n" +
            "SEE ALSO\n" +
            "[Look Mode & Counter Stance] · [Controls & Keybindings] · [Permadeath & Save Deletion] · [Floor Boss Roster] · [Field Bosses]")
        {
            Tags = new[] { "combat", "knowledge", "controls", "persistence" }
        },

        new("Combat & Rarity", "Permadeath & Save Deletion",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Permadeath & Save Deletion\n" +
            "│ Rule: One life per save — all deaths are final\n" +
            "│ Consequence: Save file deleted immediately\n" +
            "│ Trigger: HP reaches 0\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Every run is a death-game run. When HP hits 0 the DeathScreen\n" +
            "shows your run stats and the save slot is wiped — there is no\n" +
            "partial penalty, no respawn, and no reload.\n\n" +
            "USAGE\n" +
            "Applies automatically on any death, anywhere on the floor. The\n" +
            "DeathScreen asks you to confirm a return to the main menu; by\n" +
            "the time you see it, the save file is already gone from disk.\n\n" +
            "EFFECTS\n" +
            "Death deletes save_N.json from %LOCALAPPDATA%/AincradTRPG. The\n" +
            "run's score still posts to achievements/leaderboard state, but\n" +
            "the slot itself cannot be reloaded. Equipment, Col, XP, party,\n" +
            "and quests are all lost with the save. No 25% Col penalty or\n" +
            "XP rollback — the previous soft-death fallback has been\n" +
            "replaced by universal permadeath.\n\n" +
            "COSTS\n" +
            "The entire run. There is no middle ground.\n\n" +
            "TIPS\n" +
            "Keep a Revive Crystal in a quick-use slot — it intercepts the\n" +
            "fatal hit before the save is deleted. Use Safe Rooms between\n" +
            "labyrinth pushes, and F5 quick-save before a boss pull so the\n" +
            "auto-save on ascend doesn't overwrite a bad position.\n\n" +
            "SEE ALSO\n" +
            "[Save System] · [Pause Menu (Esc)] · [Potions, Crystals & Throwables] · [Safe Rooms & Mechanics] · [Bestiary — Monster Compendium]")
        {
            Tags = new[] { "combat", "permadeath", "save" }
        },

        // ── 2. Character Progression ──

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
            "[The Six Attributes] · [Passive Talents (Level-Up Perks)] · [Floor Titles] · [Starting Loadout]")
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
            "boosts passive regen. Life-Skill milestones add flat bonuses\n" +
            "on top of your SP spend — Walking feeds END, Running feeds\n" +
            "SPD, and Sleep lifts MaxHP at L10/25/50/99 thresholds.\n\n" +
            "SEE ALSO\n" +
            "[Derived Combat Stats] · [Experience & Leveling] · [Critical Hits] · [Life Skills]")
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
            "BUNDLE 10 — NEW DIRECT STAT GRANTS:\n" +
            "Five new StatType entries let equipment grant the following\n" +
            "stats DIRECTLY (no flavor-string parsing required):\n" +
            "  CritRate         +N% to CRT — stacks with Dex contribution\n" +
            "  AttackSpeed      +N to weapon swing cadence (faster turns)\n" +
            "  BlockChance      +N% to shield block roll\n" +
            "  HPRegen          +N HP per passive-regen pulse\n" +
            "  SkillCooldown    -N turns on Sword Skill cooldowns\n" +
            "Previously these effects only existed via SpecialEffect tag\n" +
            "strings on weapons (CritRate+N, HPRegen+N, etc.). Now any\n" +
            "equipment piece — armor, ring, bracelet, necklace — can roll\n" +
            "them as a direct StatBonus, and the values stack additively\n" +
            "with any equivalent SpecialEffect tags from other slots.\n\n" +
            "COSTS\n" +
            "Broken gear (0 durability) contributes nothing until repaired.\n\n" +
            "TIPS\n" +
            "Audit the sheet after repair runs — a dead weapon or armor\n" +
            "slot silently halves your output until you notice. Direct\n" +
            "StatBonus grants for CritRate/AttackSpeed/HPRegen are visible\n" +
            "on the sheet diff line; SpecialEffect tag strings show in the\n" +
            "tooltip — both sources sum.\n\n" +
            "SEE ALSO\n" +
            "[The Six Attributes] · [Damage Formula] · [Anvil — Repair, Enhance, Evolve, Refine] · [Advanced Weapon Effects]")
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
            "[Paired Dual-Wield Weapons] · [Unique Skill: Holy Sword] · [Unique Skill: Martial Arts] · [Equipment Slots & Dual Wield]")
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
            "[Aincrad's 100 Floors & Eras] · [Ascending a Floor] · [Achievements] · [Titles & the Active Title Slot]")
        {
            Tags = new[] { "leveling", "floors" }
        },

        new("Progression", "Karma & Alignment",
            "┌─ Progression\n" +
            "│ Topic: Karma & Alignment\n" +
            "│ Range: -100 (Outlaw) to +100 (Honorable)\n" +
            "│ Default: 0 (Neutral)\n" +
            "│ Unlock: Always active from character creation\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Karma is a signed alignment score from -100 to +100 that tracks\n" +
            "how the world sees you. It gates guild membership, shifts shop\n" +
            "prices, and at the extremes triggers Town Guard aggro or unlocks\n" +
            "the Laughing Coffin (LC) hideout.\n\n" +
            "USAGE\n" +
            "Karma changes automatically as you play. No menu to spend — the\n" +
            "value lives on your Player sheet and the StatsDialog shows the\n" +
            "current score and tier.\n\n" +
            "EFFECTS\n" +
            "Gain / loss events:\n" +
            "  +3   Quest completion\n" +
            "  +2   Kill a PK mob (Titan's Hand, Crimson Longsword,\n" +
            "       LC PKer, Fallen Paladin)\n" +
            "  -3   Leave a guild voluntarily\n" +
            "  -5   Kill a peaceful / neutral mob (beast, insect, non-aggressor)\n" +
            "  -5   Moonlit Black Cats fate event on F27 entry\n" +
            "  -20  Kill a named NPC\n\n" +
            "Thresholds:\n" +
            "  +50 to +100  Honorable — NPC dialogue respectful, shop x0.90\n" +
            "    0 to  +50  Neutral — default baseline\n" +
            "  -50 to    0  Shady — NPC warnings, shop x1.10\n" +
            " -100 to  -50  Outlaw — Town Guards spawn + aggro in F1 plaza,\n" +
            "               LC F75 gate unlocks, shops refuse service\n\n" +
            "Bargaining Life Skill milestones STACK MULTIPLICATIVELY with\n" +
            "karma: Honorable (x0.90) × Bargaining L99 (x0.85) = x0.765 →\n" +
            "23.5% off buys, the maximum discount in the game. Outlaw still\n" +
            "blocks shop entry BEFORE Bargaining math runs, so no amount of\n" +
            "Life Skill grinding rescues a -50 karma slot.\n\n" +
            "COSTS\n" +
            "Farming karma in either direction has opportunity cost — Honorable\n" +
            "gets cheaper shops but cannot join LC; Outlaw gains the LC guild\n" +
            "path but loses all civilian shop access.\n\n" +
            "TIPS\n" +
            "Plan the swing deliberately. If you want the KoB or Sleeping\n" +
            "Knights end-game guilds (karma >=+30 / +50) grind quest turn-ins\n" +
            "early; if you want LC stockpile PK-mob kills and avoid guild\n" +
            "joins. Never kill peaceful mobs casually — a -5 swing undoes\n" +
            "almost two quest turn-ins. Pair Honorable with L99 Bargaining\n" +
            "before a big shop run for the full -23.5% stack.\n\n" +
            "SEE ALSO\n" +
            "[Guild System Overview] · [Town Guard (Outlaw Mode)] · [Laughing Coffin (F75 Hidden)] · [Sleeping Knights (F60)] · [Bargaining (Life Skill)] · [Vendors — Rotating Stock]")
        {
            Tags = new[] { "karma", "alignment", "progression" }
        },

        new("Progression", "Life Skills",
            "┌─ Progression\n" +
            "│ Topic: Life Skills\n" +
            "│ Skills: Sleep · Walking · Running · Eating · Bargaining · Swimming · Mining\n" +
            "│ Cap: Level 99 per skill\n" +
            "│ Save: Per-player, in SaveData.LifeSkills\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Seven non-combat skills level from everyday play — Sleep,\n" +
            "Walking, Running, Eating, Bargaining, Swimming, and Mining\n" +
            "(Bundle 10). They sit parallel to Weapon Proficiency:\n" +
            "proficiency rewards combat with one weapon type, Life Skills\n" +
            "reward the travel, trade, rest, and resource-extraction loop.\n\n" +
            "USAGE\n" +
            "No menu — each skill banks XP from the matching in-world action.\n" +
            "Milestones at L10/25/50/99 fire a banner and stamp a permanent\n" +
            "stat passive (or traversal gate) onto your character.\n\n" +
            "EFFECTS\n" +
            "XP curve (piecewise geometric): L5=10, L10=100, L25=1000,\n" +
            "L50=5000, L99=50000.\n\n" +
            "  SKILL       XP SOURCE                       L10/25/50/99\n" +
            "  SLEEP       +20 ProcessRest / +10 campfire  MaxHP +5/+15/+35/+80 +regen\n" +
            "  WALKING     +1 per normal step              END  +2/+5/+10/+20\n" +
            "              (suppressed on Water tiles — Swim takes over)\n" +
            "  RUNNING     +2 per sprint step              SPD  +2/+5/+10/+20 +cost\n" +
            "  EATING      +10 per food consumed           Potency +10/25/50/100%\n" +
            "  BARGAINING  +1 per shop transaction         Buy x0.97/0.94/0.90/0.85\n" +
            "              (Sell Junk = 1 XP total)        Sell x1.03/1.06/1.10/1.15\n" +
            "  SWIMMING    +2 Water step / +3 WaterDeep    L10 Water full-speed\n" +
            "                                              L25 WaterDeep passable\n" +
            "                                              L50 WaterDeep full-speed\n" +
            "  MINING      +4 Iron / +9 Mith / +18 Div     L10 +5% drops + free dur every other strike\n" +
            "              (per vein strike)               L25 +10% drops + 20% bonus ore roll\n" +
            "                                              L50 +15% drops + Mith trace +1 + Iron -1 strike\n" +
            "                                              L99 +25% drops + 20% Divine boost + dur halved\n\n" +
            "COSTS\n" +
            "None. Life Skills cost only the time spent doing the activity;\n" +
            "they never block or gate other progression. Swimming below L10\n" +
            "charges 2 turn ticks per water step (mobs get a free turn).\n\n" +
            "TIPS\n" +
            "Running and Walking level in parallel — a mix of sprint bursts\n" +
            "and normal steps feeds both XP pools on the same trip. Rest at\n" +
            "campfires whenever idle; each step-on is Sleep XP even before\n" +
            "you trigger the cook menu. Eating cheap bread between fights\n" +
            "grinds Eating XP without burning rare consumables. Bargaining\n" +
            "ticks on EVERY shop buy and sell — split a bulk sell into\n" +
            "individual transactions early if you want the XP. Swimming\n" +
            "stacks +2/+3 XP per water tile crossed, so scenic routes through\n" +
            "rivers beat dryland detours once you're willing to take the hit.\n\n" +
            "SEE ALSO\n" +
            "[Bargaining (Life Skill)] · [Swimming (Life Skill)] · [Mining (Life Skill)] · [Mining — Tool Slot & Ore Veins] · [Weapon Proficiency Tree] · [Hunger, Satiety & Fatigue] · [Sprint & Stealth Move]")
        {
            Tags = new[] { "life-skills", "progression" }
        },

        new("Progression", "Bargaining (Life Skill)",
            "┌─ Progression\n" +
            "│ Topic: Bargaining (Life Skill)\n" +
            "│ XP: +1 per shop transaction (buy or sell)\n" +
            "│ Cap: Level 99 (-15% buy / +15% sell)\n" +
            "│ Save: Per-player, in SaveData.LifeSkills\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Bargaining is a Life Skill that tilts shop math in your favor.\n" +
            "Every buy, sell, or Sell Junk transaction banks +1 XP; milestones\n" +
            "at L10/25/50/99 STACK MULTIPLICATIVELY with Karma tier and any\n" +
            "floor/vendor markup to shave Col off buys and pad sells.\n\n" +
            "USAGE\n" +
            "Automatic — any ShopDialog transaction feeds XP. Sell Junk grants\n" +
            "1 XP for the bulk action, not 1-per-item, so split high-volume\n" +
            "sells across individual transactions if you're grinding the\n" +
            "curve. Milestone banners fire on level-up.\n\n" +
            "EFFECTS\n" +
            "  LEVEL   BUY PRICE   SELL PRICE   NOTE\n" +
            "  L1-9    x1.00       x1.00        No bonus — grind window\n" +
            "  L10+    x0.97       x1.03        -3% / +3%\n" +
            "  L25+    x0.94       x1.06        -6% / +6%\n" +
            "  L50+    x0.90       x1.10        -10% / +10%\n" +
            "  L99     x0.85       x1.15        -15% / +15% (cap)\n\n" +
            "Stacking: Honorable karma (x0.9 buy) × L99 Bargaining (x0.85)\n" +
            "= x0.765 final → 23.5% off. Shady karma (+10% markup) × L99\n" +
            "bargain still nets ≈x0.935 → 6.5% off. Outlaw karma refuses\n" +
            "service entirely BEFORE the Bargaining math runs — no discount\n" +
            "rescues a -50 karma slot.\n\n" +
            "COSTS\n" +
            "None. Bargaining XP is pure upside; every shop visit feeds it.\n\n" +
            "TIPS\n" +
            "Hit L10 early — the first -3%/+3% swing pays back the inventory\n" +
            "time cost inside two shop runs. If you're Outlaw-drifting for\n" +
            "Laughing Coffin access, Bargaining XP freezes (shops refuse\n" +
            "you) — bank transactions before the karma flip. The multiplier\n" +
            "also applies to Vendor Investing deposits, so L99 + Honorable\n" +
            "shaves nearly a quarter off every tier-boost payment.\n\n" +
            "SEE ALSO\n" +
            "[Life Skills] · [Vendor Investing] · [Karma & Alignment] · [Vendors — Rotating Stock] · [Col Economy — How You Earn]")
        {
            Tags = new[] { "life-skills", "bargaining", "economy" }
        },

        new("Progression", "Swimming (Life Skill)",
            "┌─ Progression\n" +
            "│ Topic: Swimming (Life Skill)\n" +
            "│ XP: +2 per Water step / +3 per WaterDeep step\n" +
            "│ Gates: L1 Water / L25 WaterDeep\n" +
            "│ Save: Per-player, in SaveData.LifeSkills\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Swimming is a Life Skill that turns water tiles from impassable\n" +
            "walls into traversable terrain. Level gates control WHICH water\n" +
            "you can enter, and milestone thresholds drop the slow penalty\n" +
            "that otherwise gives aquatic mobs a free turn on every step.\n\n" +
            "USAGE\n" +
            "Step onto a Water or WaterDeep tile to bank XP. Walking XP is\n" +
            "SUPPRESSED on water steps — no double-dip with the Walking\n" +
            "skill. Below the speed threshold for a water type, each step\n" +
            "costs 2 turn ticks instead of 1 (the mob acts between your\n" +
            "frames).\n\n" +
            "EFFECTS\n" +
            "  LEVEL   WATER (shallow)       WATERDEEP            XP/STEP\n" +
            "  L1-9    Passable, slow (2t)   Blocked              +2 / n/a\n" +
            "  L10-24  Full speed            Blocked              +2 / n/a\n" +
            "  L25-49  Full speed            Passable, slow (2t)  +2 / +3\n" +
            "  L50-98  Full speed            Full speed           +2 / +3\n" +
            "  L99     Master swimmer (flavor-only capstone)      +2 / +3\n\n" +
            "RequiresSwimmingLevel on Tile: Water = 1, WaterDeep = 25.\n\n" +
            "COSTS\n" +
            "Below-threshold water steps burn 2 turn ticks — a slow tax that\n" +
            "lets aquatic mobs (CanSwim = true) reposition or attack between\n" +
            "your frames. Mob AI IsWalkable is unchanged, so dryland mobs\n" +
            "still treat water as a wall — rivers remain a choke point.\n\n" +
            "TIPS\n" +
            "Grind the first 10 levels on shallow rivers before pushing F4 —\n" +
            "the slow penalty alone gives Water Drakes or Lakeshore Crabs\n" +
            "two free swings per crossing. Use water as a MOAT against\n" +
            "non-CanSwim pursuers even before L10; they can't follow, and\n" +
            "the 2-tick cost is cheaper than a long detour when you're\n" +
            "already wounded. L99 is flavor — the real cliffs are L10 and\n" +
            "L50.\n\n" +
            "SEE ALSO\n" +
            "[Life Skills] · [River Crossing & Aquatic Mobs] · [Mechanical Tiles] · [Biomes] · [Hunger, Satiety & Fatigue]")
        {
            Tags = new[] { "life-skills", "swimming", "world" }
        },

        new("Progression", "Mining (Life Skill)",
            "┌─ Progression\n" +
            "│ Topic: Mining (Life Skill)\n" +
            "│ XP: +4 Iron / +9 Mithril / +18 Divine per strike\n" +
            "│ Cap: Level 99 (+25% drops, dur damage halved)\n" +
            "│ Save: Per-player, in SaveData.LifeSkills\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Mining is Bundle 10's seventh Life Skill — a non-combat\n" +
            "track that levels every time you swing a Pickaxe at an ore\n" +
            "vein. Each strike banks XP regardless of whether the vein\n" +
            "depletes that turn, so even a Wooden Pickaxe whittling away\n" +
            "at a stubborn Mithril vein contributes 9 XP per swing. The\n" +
            "milestones at L10/25/50/99 progressively boost ore drops,\n" +
            "save your pickaxe durability, and crank Divine vein yields\n" +
            "into endgame-relevant territory.\n\n" +
            "USAGE\n" +
            "No menu — strike any vein with a Pickaxe equipped to bank XP.\n" +
            "Unlike Walking and Swimming, Mining is opt-in: you have to\n" +
            "carry the tool. The Life Skills XP curve is shared with the\n" +
            "other six skills (L5=10, L10=100, L25=1000, L50=5000,\n" +
            "L99=50000), so 100 Iron strikes lands you at L10 in a single\n" +
            "vein-rich floor.\n\n" +
            "EFFECTS\n" +
            "XP PER STRIKE:\n" +
            "  Iron      +4 XP\n" +
            "  Mithril   +9 XP\n" +
            "  Divine    +18 XP\n" +
            "MILESTONES (stack — each level inherits prior bonuses):\n" +
            "  L1   Base — strikes work, no bonuses\n" +
            "  L10  +5% drop chance on every vein\n" +
            "       Every-other-strike costs 0 durability (free swing)\n" +
            "  L25  +10% drop chance (replaces L10 +5%)\n" +
            "       +1 bonus ore roll @ 20% per depletion\n" +
            "  L50  +15% drop chance\n" +
            "       Mithril veins drop +1 mithril_trace on depletion\n" +
            "       Iron veins cost -1 strike (combines with MiningPower)\n" +
            "  L99  +25% drop chance\n" +
            "       Divine vein drop rate +20%\n" +
            "       All durability damage HALVED — every strike now ticks\n" +
            "       0.5 durability (rounded), effectively doubling pickaxe\n" +
            "       lifespan on top of L10's free-strike effect\n\n" +
            "COSTS\n" +
            "Mining XP costs only durability and turns — no Col, no\n" +
            "stamina drain. Pickaxes SHATTER at 0 durability, so the L10\n" +
            "every-other-free-strike and L99 dur-halved milestones aren't\n" +
            "just nice-to-haves; they materially extend the per-pickaxe\n" +
            "vein count.\n\n" +
            "TIPS\n" +
            "Power-level on Iron veins through F10-F25 — they're cheap to\n" +
            "strike, the XP is steady, and L10's free-every-other-strike\n" +
            "kicks in fast. Once L25 lands, swing on Mithril for the +1\n" +
            "trace bonus + 20% bonus ore roll combo. L50 → L99 is the\n" +
            "long curve; bank Divine strikes once F75+ access opens to\n" +
            "make the climb manageable. The L99 dur-halved + L10 free-\n" +
            "strike combo means a Mithril Pickaxe (200 dur) can chip\n" +
            "DOZENS of Divine veins per repair cycle.\n\n" +
            "SEE ALSO\n" +
            "[Life Skills] · [Mining — Tool Slot & Ore Veins] · [Pickaxe Tiers] · [Refinement Ingots] · [Anvil — Repair, Enhance, Evolve, Refine]")
        {
            Tags = new[] { "life-skills", "mining", "progression" }
        },

        new("Progression", "Titles & the Active Title Slot",
            "┌─ Progression\n" +
            "│ Topic: Titles & the Active Title Slot\n" +
            "│ Unlocks: Species, tag, total, floor-clear triggers\n" +
            "│ Slot: One equipped title at a time\n" +
            "│ Manage: Monument of Swordsmen (F1) or StatsDialog\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "15 starter titles unlock from kill milestones and floor clears.\n" +
            "You equip ONE into the Active Title slot for a flat passive\n" +
            "bonus; swapping is free and can be done any time.\n\n" +
            "USAGE\n" +
            "Unlock fires automatically when the trigger resolves — a banner\n" +
            "notification announces the new title. Equip/unequip at the\n" +
            "Monument of Swordsmen on F1 or through the Life Skills / Active\n" +
            "Title section of the character sheet.\n\n" +
            "EFFECTS\n" +
            "  Boar Slayer          10 boar kills\n" +
            "  Nepent Cutter        10 nepent kills\n" +
            "  Kobold Crusher       10 kobold kills\n" +
            "  Titan Breaker        10 taurus kills\n" +
            "  Cardinal Breaker     10 Cardinal Error kills\n" +
            "  Beast Hunter         100 beast-tag kills\n" +
            "  Beast Lord           1000 beast-tag kills\n" +
            "  Dragon Slayer        100 dragon-tag kills\n" +
            "  Dragon Lord          1000 dragon-tag kills\n" +
            "  Hollow Walker        100 hollow-tag kills\n" +
            "  Undead Exorcist      100 undead-tag kills\n" +
            "  Insect Crusher       100 insect-tag kills\n" +
            "  Beginner Slayer      100 total kills\n" +
            "  The Black Swordsman  9999 total kills\n" +
            "  Survivor             F50 cleared\n\n" +
            "COSTS\n" +
            "None. Single-slot only — equipping a new title replaces the old.\n\n" +
            "TIPS\n" +
            "The Black Swordsman title is distinct from the L110 per-weapon\n" +
            "proficiency rank of the same name — the title unlocks at 9999\n" +
            "TOTAL kills across all weapons. Swap titles to match the next\n" +
            "leg: species-specific passives for floor grinds, then a\n" +
            "tag-aggregate like Beast Lord once you move on.\n\n" +
            "SEE ALSO\n" +
            "[Monument of Swordsmen (F1)] · [Life Skills] · [Weapon Proficiency Ranks] · [Floor Titles]")
        {
            Tags = new[] { "titles", "progression" }
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
            "  L50     FORK 2 (dodge vs skill-damage focus,\n" +
            "          weapon-specific for select types — see below)\n" +
            "  L75     FORK 3 (combo vs stun focus)\n" +
            "  L100    FORK 4 (capstone — per-weapon unique)\n" +
            "  L110    The Black Swordsman (cap, +120 ATK)\n\n" +
            "BUNDLE 10 — WEAPON-SPECIFIC L50 FORKS:\n" +
            "Three weapon types now offer flavored forks at the L50 mark\n" +
            "instead of the generic dodge/skill-damage pick:\n" +
            "  ONE-HANDED SWORD  Vorpal Edge (+3 CritRate)\n" +
            "                    OR Saber Step (+1 AttackSpeed)\n" +
            "  KATANA            Iaijutsu (+5% damage on first strike of\n" +
            "                    every encounter — pairs with Drawing\n" +
            "                    Stance flavor in canon)\n" +
            "                    OR Drawing Stance (+2 CritRate)\n" +
            "  BOW               Marksman Eye (+2 CritRate, +5 effective\n" +
            "                    bow range overflow)\n" +
            "                    OR Quickdraw (+1 AttackSpeed)\n" +
            "The other 9 weapon types use the unchanged generic L50 fork.\n" +
            "Choices use the new B13 StatType grants (CritRate, Attack-\n" +
            "Speed) — they show up directly on the character sheet rather\n" +
            "than as flavor-string riders.\n\n" +
            "COSTS\n" +
            "None. Fork choices cannot be respec'd without a New Game.\n\n" +
            "TIPS\n" +
            "Don't split early kills across 3 weapons. A single primary\n" +
            "weapon reaches L50 long before a split build, giving you the\n" +
            "second fork faster. Use secondaries only after your primary\n" +
            "crosses L50. Life Skills are the non-combat parallel track —\n" +
            "they level from travel and rest, not kills.\n\n" +
            "SEE ALSO\n" +
            "[Weapon Proficiency Ranks] · [Sword Skills — Unlock & Use] · [Passive Talents (Level-Up Perks)] · [Life Skills]")
        {
            Tags = new[] { "proficiency", "skills", "weapons" }
        },

        // ── 3. World & Exploration ──

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
            "floors are the best time to off-load loot and re-stock potions.\n" +
            "On Aquatic floors (and F4 Rovia) ensure Swimming L10+ before\n" +
            "engaging — below the threshold the slow tax on water tiles\n" +
            "compounds the -3 ATK biome debuff.\n\n" +
            "SEE ALSO\n" +
            "[Weather] · [Vision & FOV] · [Traps & Hazards] · [Swimming (Life Skill)] · [River Crossing & Aquatic Mobs] · [Unique Skill: Blazing & Frozen Edge] · [Ambient World Animation]")
        {
            Tags = new[] { "world", "biomes", "floors" }
        },

        new("World", "Ambient World Animation",
            "┌─ World\n" +
            "│ Topic: Ambient World Animation\n" +
            "│ Tiles: Vents · terminals · fans · hearths · torches\n" +
            "│        · water ripples · door creaks · chest sparkles\n" +
            "│ Density cap: Max 3 concurrent ambient tiles\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Select world tiles animate subtly to give the map a heartbeat.\n" +
            "Each animation is an environmental cue — vents mean airflow,\n" +
            "sparkling chests mean unlooted treasure, and torches gutter when\n" +
            "the floor has an active draft or quake event.\n\n" +
            "USAGE\n" +
            "Ambient animation runs automatically when tiles enter your FOV.\n" +
            "Tiles outside the current field-of-view never animate — they\n" +
            "only come alive when you can actually see them.\n\n" +
            "EFFECTS\n" +
            "ANIMATED TILE CATALOGUE:\n" +
            "  Vents          steam plume every few frames\n" +
            "  Terminals      cursor blink / glyph cycle\n" +
            "  Fans           rotating blade glyph\n" +
            "  Hearths        flame flicker (pulses brighter in wind)\n" +
            "  Torches        flame flicker (same palette as hearths)\n" +
            "  Water ripples  surface tiles cycle a ~ glyph\n" +
            "  Door creaks    one-shot when you pass adjacent\n" +
            "  Chest sparkles unlooted chests shimmer gold\n" +
            "FOV GATING:\n" +
            "  Nothing animates outside your vision cone — ambient never\n" +
            "  reveals mob position or hidden tiles.\n" +
            "DENSITY CAP:\n" +
            "  Engine hard-caps the map at 3 concurrent ambient tiles so\n" +
            "  busy floors never flood the eye with motion.\n" +
            "COMBAT SUPPRESSION:\n" +
            "  Hearth and torch flames auto-disable while combat is active\n" +
            "  so the flicker does not compete with damage popups.\n\n" +
            "COSTS\n" +
            "Zero gameplay cost. Ambient animation is presentation only and\n" +
            "never changes mob AI, FOV radius, or trap-detection rolls.\n\n" +
            "TIPS\n" +
            "Read ambient motion as a scouting layer: a sparkling chest means\n" +
            "unlooted, a fluttering torch means the floor has active weather\n" +
            "or a nearby vent draft, and a steam plume pinpoints the vent\n" +
            "tile itself in a Darkness biome when the base glyph is hard to\n" +
            "read. Disable in Options if animation distracts you.\n\n" +
            "SEE ALSO\n" +
            "[Biomes] · [Vision & FOV] · [Mechanical Tiles] · [Labyrinth System] · [Weather] · [Campfires — Rest & Sleep XP] · [Combat Visual Feedback]")
        {
            Tags = new[] { "world", "ui", "accessibility" }
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
            "[Floor Boss Roster — Canon Highlights] · [Field Bosses — Guaranteed Drops] · [Ascending a Floor] · [Mechanical Tiles] · [Ambient World Animation]")
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
            "[Anvil — Repair, Enhance, Evolve, Refine] · [Hunger, Satiety & Fatigue] · [Col Economy — How You Earn] · [Lore, Journals & Enchant Shrines] · [Campfires — Rest & Sleep XP]")
        {
            Tags = new[] { "world", "economy", "progression" }
        },

        new("World", "Campfires — Rest & Sleep XP",
            "┌─ World\n" +
            "│ Topic: Campfires — Rest & Sleep XP\n" +
            "│ Glyph: Orange &/*\n" +
            "│ Floors: All (scattered overworld)\n" +
            "│ Unlock: Walk-on, cooking interaction\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "The standard Safe Room campfire doubles as a Sleep-skill farm.\n" +
            "Each step onto a campfire tile banks +10 Sleep XP, and the\n" +
            "ProcessRest action (cook/sleep from the campfire menu) banks\n" +
            "another +20.\n\n" +
            "USAGE\n" +
            "Walk onto an orange & or * tile. The status purge and heal fire\n" +
            "as with any campfire; the cooking menu opens on bump. Resting\n" +
            "from the menu counts as ProcessRest for the +20 XP drop.\n\n" +
            "EFFECTS\n" +
            "Stacked with the Safe Rooms effect package:\n" +
            "  +10 Sleep XP          on step (campfire tile)\n" +
            "  +20 Sleep XP          on ProcessRest action\n" +
            "  Purge Poison / Bleed / Slow\n" +
            "  Heal 15 + 5*floor HP\n" +
            "  Reset rest + fatigue timers\n\n" +
            "COSTS\n" +
            "None. Campfire tiles are one-shot per tile (consume on use), but\n" +
            "Sleep XP banks before the tile is spent.\n\n" +
            "TIPS\n" +
            "Route through every campfire you pass even when not injured —\n" +
            "the Sleep XP compounds toward L10/25/50/99 MaxHP milestones. The\n" +
            "Eating skill levels from the cook menu, so campfires pull double\n" +
            "duty if you roast food between fights.\n\n" +
            "SEE ALSO\n" +
            "[Safe Rooms & Mechanics] · [Life Skills] · [Food & Cooking] · [Hunger, Satiety & Fatigue]")
        {
            Tags = new[] { "sleep", "world", "life-skills" }
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
            "[Weather] · [Biomes] · [Unique Skill: Extra Skill — Search] · [Sprint & Stealth Move] · [Mechanical Tiles]")
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
            "MONUMENT (M, yellow)     F1 Town of Beginnings only. Opens kill\n" +
            "                          log + Active Title picker; never\n" +
            "                          consumes.\n" +
            "WATER (shallow, blue ~)  Swim gate L1+. Below L10, step costs\n" +
            "                          2 turn ticks (mobs get a free turn).\n" +
            "                          Walking XP suppressed; Swimming +2 XP.\n" +
            "WATERDEEP (dark blue ≈)  Swim gate L25+. Below L50, 2-tick slow\n" +
            "                          penalty. Swimming +3 XP per step.\n" +
            "STAIRS DOWN (<)          Never used (Aincrad climbs up only).\n" +
            "STAIRS UP (>)            Sealed until the floor boss is dead.\n\n" +
            "COSTS\n" +
            "Breaking a cracked wall costs 1 weapon durability per swing.\n" +
            "Water tiles below the speed threshold cost 2 turn ticks each.\n\n" +
            "TIPS\n" +
            "Any lever that seems pointless probably has a linked plate\n" +
            "elsewhere — trace the floor systematically. Cracked walls almost\n" +
            "always guard Epic-or-better chests. Water is a moat against\n" +
            "dryland mobs (their IsWalkable refuses it) but porous to any\n" +
            "aquatic mob flagged CanSwim.\n\n" +
            "SEE ALSO\n" +
            "[Labyrinth System] · [Safe Rooms & Mechanics] · [River Crossing & Aquatic Mobs] · [Swimming (Life Skill)] · [Ascending a Floor] · [Monument of Swordsmen (F1)]")
        {
            Tags = new[] { "world", "floors", "economy" }
        },

        new("World", "River Crossing & Aquatic Mobs",
            "┌─ World\n" +
            "│ Topic: River Crossing & Aquatic Mobs\n" +
            "│ Floors: Water tiles on many overworlds; F4 Rovia hub\n" +
            "│ Landmark: Blue ~ (shallow) / dark ≈ (deep)\n" +
            "│ Unlock: Swimming Life Skill L1+ (shallow), L25+ (deep)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Water tiles are passable terrain gated by the Swimming Life Skill\n" +
            "— a strategic choke point for most mobs, but an open highway for\n" +
            "the five aquatic mobs flagged Monster.CanSwim = true. F4 Rovia is\n" +
            "where the design shows up most clearly, with a river system and\n" +
            "a roster of swim-capable predators.\n\n" +
            "USAGE\n" +
            "Walk onto a Water or WaterDeep tile to cross. Your Swimming level\n" +
            "sets BOTH the pass gate (L1 shallow, L25 deep) and the speed gate\n" +
            "(L10 shallow full-speed, L50 deep full-speed). Mob AI checks each\n" +
            "mob's CanSwim flag against the tile's water type.\n\n" +
            "EFFECTS\n" +
            "CanSwim = TRUE (Tier 3 / F4 Rovia):\n" +
            "  Water Drake        Draconic aquatic predator\n" +
            "  Lakeshore Crab     Coastal crustacean\n" +
            "  Giant Clam         Sessile ambusher\n" +
            "  Water Wight        Undead drifter\n" +
            "  Scavenger Toad     Amphibian brawler\n\n" +
            "These mobs traverse BOTH Water and WaterDeep — they'll pursue\n" +
            "you into a river and close the gap you thought was a moat.\n" +
            "Both SimpleAI and TurnManager.AI honor the flag.\n\n" +
            "CanSwim = FALSE (everything else, including F2 Plumed Mist\n" +
            "Lizard which is flagged as reptile but intentionally land-bound):\n" +
            "  Water is a hard wall — the mob routes around or pulls up\n" +
            "  short at the bank. Use this as a kiting aid.\n\n" +
            "COSTS\n" +
            "Slow-tick water steps (below swim-speed threshold) cost you a\n" +
            "free turn to any CanSwim pursuer in range. Waste water crossings\n" +
            "on low-Swimming chars are the single biggest mid-river death\n" +
            "vector.\n\n" +
            "TIPS\n" +
            "Scout the overworld before crossing — a Water Drake on the far\n" +
            "bank turns a 2-tick-per-step swim into a gauntlet. Bank Swimming\n" +
            "XP to L10 before pushing F4 Rovia so your shallow crossings are\n" +
            "at normal speed. For non-aquatic mob chases (wolves, kobolds,\n" +
            "Plumed Mist Lizard), rivers remain a clean escape route at any\n" +
            "Swimming level — the slow tax is cheaper than a fight.\n\n" +
            "SEE ALSO\n" +
            "[Swimming (Life Skill)] · [Mechanical Tiles] · [Biomes] · [Sprint & Stealth Move]")
        {
            Tags = new[] { "world", "aquatic", "swimming" }
        },

        new("World", "Monument of Swordsmen (F1)",
            "┌─ World\n" +
            "│ Topic: Monument of Swordsmen (F1)\n" +
            "│ Glyph: BrightYellow M\n" +
            "│ Floor: 1 — Town of Beginnings, south plaza grass park\n" +
            "│ Unlock: Walk onto the tile\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "A canonical Town of Beginnings landmark. Stepping on it opens a\n" +
            "dialog showing your species kill log with milestone checkmarks\n" +
            "and the equip/unequip picker for every title you've unlocked.\n\n" +
            "USAGE\n" +
            "Walk onto the M tile in the southern plaza park of F1. The\n" +
            "MonumentDialog pops with two sections: species kill list\n" +
            "(checkmarks at 10 / 100 / 1000) and the active title picker.\n\n" +
            "EFFECTS\n" +
            "  KILL LOG    Per-species tally vs milestone thresholds; check\n" +
            "              marks show which title triggers have fired.\n" +
            "  TITLE SLOT  Equip / unequip any unlocked title — same single-\n" +
            "              slot passive available from StatsDialog.\n" +
            "  LANDMARK    Does not consume on step; revisit freely.\n\n" +
            "COSTS\n" +
            "None. Zero-durability, zero-Col interaction.\n\n" +
            "TIPS\n" +
            "Check in whenever you return to F1 — the kill log is the fastest\n" +
            "way to spot which species title you're closest to unlocking.\n" +
            "Swap titles here before heading to the next floor so the right\n" +
            "passive rides with you up the stairs.\n\n" +
            "SEE ALSO\n" +
            "[Titles & the Active Title Slot] · [Town of Beginnings NPCs (F1)] · [Mechanical Tiles] · [Life Skills]")
        {
            Tags = new[] { "monument", "titles", "world" }
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
            "rolled back — a wipe in a boss fight deletes the save slot.\n\n" +
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
            "coexists with Blaze Armor, so budget durability. F95+ field\n" +
            "bosses also roll a 10% Corruption Stone drop (random Night or\n" +
            "Shadow), independent of the Avatar last-attack roll.\n\n" +
            "SEE ALSO\n" +
            "[Integral Factor Field Bosses] · [Fractured Daydream Field Bosses] · [Avatar Weapons & Last-Attack Bonus] · [Corruption Stones & Corrupted Weapons] · [Divine Object Set — Integrity Knights] · [Seasonal Events]")
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
            "│ Unlock: First F100 clear (the only route in)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Twelve stacked toggles that make a run harder in exchange for\n" +
            "a score multiplier (cap x10). Chosen at run start and frozen\n" +
            "for the life of the save. Run Modifiers unlock after your\n" +
            "first F100 clear — before then the screen slot shows [locked].\n\n" +
            "USAGE\n" +
            "Finish a full F1-F100 run first; the modifier select screen is\n" +
            "gated behind that victory. After the unlock, toggle modifiers\n" +
            "on the New Game screen. Active modifiers show on the HUD and\n" +
            "save into the slot summary.\n\n" +
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
            "Pre-unlock cost: you can't use any modifier until you clear\n" +
            "F100 once. Post-unlock, each modifier bites differently: Iron\n" +
            "Rank doubles satiety drain, Hollow Ingress doubles sword-skill\n" +
            "unlock kills, Solo forbids recruits, Anti-Crystal Tyranny\n" +
            "disables Crystal consumables.\n\n" +
            "TIPS\n" +
            "You will see [locked] in the menu slot until you complete your\n" +
            "first F100 run — that clear is the ticket. After that, stack\n" +
            "compatible modifiers to approach the x10 multiplier cap.\n" +
            "Starless Night pairs well with Darkness Blade builds. The\n" +
            "Laughing Coffin modifier synergizes with the LC guild path —\n" +
            "more LC mob spawns on one side, +20% BackstabDmg passive on\n" +
            "the other — and with Legend Braves' +15 Attack vs LC-tagged\n" +
            "mobs if you're running the anti-PK track instead.\n\n" +
            "SEE ALSO\n" +
            "[Unique Skill: Darkness Blade] · [Laughing Coffin (F75 Hidden)] · [Legend Braves (F25)] · [Permadeath & Save Deletion] · [Save System]")
        {
            Tags = new[] { "world", "progression", "permadeath" }
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
            "and is a literal save-from-deletion since every run is permadeath.\n\n" +
            "SEE ALSO\n" +
            "[Field Bosses — Guaranteed Drops] · [Potions, Crystals & Throwables] · [Permadeath & Save Deletion] · [Divine Objects]")
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

        new("World", "Town Guard (Outlaw Mode)",
            "┌─ World\n" +
            "│ Topic: Town Guard (Outlaw Mode)\n" +
            "│ Floors: 1 (Town of Beginnings plaza only)\n" +
            "│ Landmark: Spawns on entry when karma <= -50\n" +
            "│ Unlock: Player karma reaches Outlaw tier\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Town Guards are the F1 plaza's enforcement response to Outlaw-\n" +
            "tier karma. They only exist when your karma is at or below -50\n" +
            "and they spawn specifically in the Town of Beginnings (TOB)\n" +
            "plaza — not on any other floor.\n\n" +
            "USAGE\n" +
            "Drop to karma <= -50, then step into the F1 TOB plaza. 3-5\n" +
            "guards spawn on entry and aggro immediately. Leave the plaza\n" +
            "(or raise karma back above -50) to stop the spawn trigger.\n\n" +
            "EFFECTS\n" +
            "  Glyph     'G' in BrightBlue\n" +
            "  Scaling   ~Level 20 (100 HP / 25 ATK baseline)\n" +
            "  Spawn     3-5 per plaza entry, only while karma <= -50\n" +
            "  LC bonus  Laughing Coffin members gain +20 guild rep per\n" +
            "            Town Guard killed\n\n" +
            "COSTS\n" +
            "Killing Town Guards is a named-NPC-class hit in everyone else's\n" +
            "eyes but not a karma drop (they are PK-flag enemies, not peaceful\n" +
            "NPCs). The fight eats durability and consumables on every F1\n" +
            "visit until you either raise karma or stay off the plaza.\n\n" +
            "TIPS\n" +
            "If you're running the LC path, farm Town Guards deliberately —\n" +
            "+20 LC rep per kill is the fastest reputation faucet in the\n" +
            "game. If you're NOT running LC, push karma above -50 with\n" +
            "quest turn-ins before setting foot in TOB again.\n\n" +
            "SEE ALSO\n" +
            "[Karma & Alignment] · [Laughing Coffin (F75 Hidden)] · [Town of Beginnings NPCs (F1)] · [Guild System Overview]")
        {
            Tags = new[] { "outlaw", "karma", "world" }
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
            "Title promotions — they fire at F2/5/10/15/25/35/50/75/100.\n" +
            "Clearing F50 also unlocks the equippable Survivor title (see\n" +
            "Titles & the Active Title Slot).\n\n" +
            "SEE ALSO\n" +
            "[Floor Boss Roster — Canon Highlights] · [Floor Titles] · [Save System] · [Col Economy — How You Earn] · [Titles & the Active Title Slot]")
        {
            Tags = new[] { "world", "progression", "xp" }
        },

        new("World", "Terrain Hazards",
            "┌─ World\n" +
            "│ Topic: Terrain Hazards\n" +
            "│ Tiles: Mud · Bog Water · Cracked Ice\n" +
            "│ Biomes: Swamp fringe, ice edges\n" +
            "│ Trigger: Step onto the tile\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Three walkable hazard tiles lurk in biome-native terrain: Mud\n" +
            "(Swamp fringe — slows for one turn per step), Bog Water (Swamp\n" +
            "pools — one Poison stack per step, Swim L1 to enter), and\n" +
            "Cracked Ice (Ice edges — occasional slip, random Slow tick).\n" +
            "None are lethal alone; most cost a turn and a stack of\n" +
            "tolerance.\n\n" +
            "USAGE\n" +
            "Walk normally. The log line tells you when a tick triggers.\n" +
            "Antidote clears Poison from Bog Water; Agi-built characters read\n" +
            "Cracked Ice slips as wasted dodge rolls.\n\n" +
            "EFFECTS\n" +
            "  Mud           Slow 2 turns on entry, top-up guarded so\n" +
            "                repeat steps do not stack beyond the cap\n" +
            "  Bog Water     Poison on entry, stacks scale with floor/2\n" +
            "                (F10 ticks 5 dmg, F40 ticks 20); Swim L1\n" +
            "                required or you refuse to enter\n" +
            "  Cracked Ice   25% slip chance per step; a slip applies\n" +
            "                Stun 1 turn and wastes the move\n\n" +
            "COSTS\n" +
            "Mud eats one free action while you dig out. Bog Water bleeds\n" +
            "HP over the next 3-5 turns per Poison stack. Cracked Ice\n" +
            "slips waste the turn AND surrender your counter-attack\n" +
            "window.\n\n" +
            "TIPS\n" +
            "Keep one Antidote per Swamp floor. A torch or Lantern reveals\n" +
            "Bog Water pools before you wade in — listen for the\n" +
            "\"something gurgles\" log line. On Ice floors, hug thick-drawn\n" +
            "tiles; hairline-cracked glyphs are the slip risk.\n\n" +
            "SEE ALSO\n" +
            "[Biomes] · [Ambient World Animation] · [Status: Bleed & Poison] · [Status: Stun & Slow] · [Swimming (Life Skill)] · [Traps & Hazards]")
        {
            Tags = new[] { "world", "terrain", "hazard" }
        },

        new("World", "Prefab Rooms — What They Are",
            "┌─ World\n" +
            "│ Topic: Prefab Rooms — What They Are\n" +
            "│ Categories: Shrines · Vaults · Trap Corridors\n" +
            "│             Merchant Stalls · Boss Arenas · Vignettes\n" +
            "│ Placement: Dropped into procedural floors\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Some rooms on a floor aren't generated tile-by-tile — they're\n" +
            "hand-authored templates dropped into the map. Shrines, vaults,\n" +
            "trap corridors, merchant stalls, and every boss arena are\n" +
            "prefab rooms. You'll recognize them by their deliberate layout\n" +
            "and signature decoration.\n\n" +
            "USAGE\n" +
            "Explore normally. Prefab rooms intermix with procedural ones.\n" +
            "A prefab shrine in a Forest floor will look the same as that\n" +
            "prefab shrine on any other Forest floor (modulo rotation and\n" +
            "mirroring).\n\n" +
            "EFFECTS\n" +
            "Each prefab carries a biome tag and a role tag (shrine, vault,\n" +
            "trap, merchant, boss, vignette). The generator picks prefabs\n" +
            "whose biome matches the current floor, rotates them to fit an\n" +
            "open room, and stamps them. Some prefabs flag MAX_PER_GAME=1\n" +
            "so you will see them only once across a whole campaign.\n" +
            "Template-specified mobs and items now spawn at placement:\n" +
            "MONS slot glyphs (1-7) fill with floor-appropriate monsters\n" +
            "and ITEM slots (a-d) drop ground items when the prefab lands.\n" +
            "Earlier builds left these slots empty — they are now wired\n" +
            "live, so a shrine with altar guards or a stall with stocked\n" +
            "crates arrives fully populated.\n\n" +
            "COSTS\n" +
            "None passively. Interacting with a prefab's contents (altar,\n" +
            "trap, vendor, chest) costs turns and resources per that\n" +
            "feature's own rules.\n\n" +
            "TIPS\n" +
            "Scan every floor for the signature silhouettes: a clean\n" +
            "rectangle of decoration flags a prefab. Search Mode clears\n" +
            "hidden traps in trap-corridor prefabs. Once-per-game prefabs\n" +
            "(secret shrines, deep vaults) are worth the detour.\n\n" +
            "SEE ALSO\n" +
            "[Biomes] · [Labyrinth System] · [Secret Shrines (T1 Chain Weapons)] · [Prefab Rooms — Shrine Vaults] · [Prefab Rooms — Trap Corridors] · [Prefab Rooms — Merchant Stalls] · [Prefab Rooms — Boss Arenas]")
        {
            Tags = new[] { "world", "terrain", "prefab" }
        },

        new("World", "Prefab Rooms — Shrine Vaults",
            "┌─ World\n" +
            "│ Topic: Prefab Rooms — Shrine Vaults\n" +
            "│ Centerpiece: Altar tile (Shrine / Enchant / Secret)\n" +
            "│ Biome variants: Frost, Forge, Grove, Dune, Void\n" +
            "│ Interaction: Walk onto the altar\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Shrine vault prefabs center on an altar tile — often Shrine\n" +
            "(temporary buff), EnchantShrine (one-shot enhance), or the\n" +
            "floor-specific SecretShrine (chain weapon). Biome-themed\n" +
            "decoration signals what kind: frost spikes in Ice, lava-forge\n" +
            "anvils in Volcanic, bone rings in the Void.\n\n" +
            "USAGE\n" +
            "Walk onto the altar. Buff shrines trigger instantly; enchant\n" +
            "shrines prompt before consuming a gear slot; secret shrines\n" +
            "grant the chain weapon and vanish.\n\n" +
            "EFFECTS\n" +
            "  Shrine          Temporary biome-themed buff, ~50 turns\n" +
            "  EnchantShrine   One-shot +1 enhance on chosen gear\n" +
            "  SecretShrine    Tier-1 chain weapon (floor-locked list)\n" +
            "  Altar (ritual)  Rare sacrifice variant — consumes an\n" +
            "                  item, grants a stat-line bonus\n\n" +
            "COSTS\n" +
            "EnchantShrines burn one enhancement on one gear piece. Altar\n" +
            "sacrifices consume an inventory slot permanently. Buff\n" +
            "shrines are free.\n\n" +
            "TIPS\n" +
            "Save enchant shrines for a mid-tier piece you'll keep for 10+\n" +
            "floors — the +1 doesn't refund when you upgrade. Secret\n" +
            "shrines are MAX_PER_GAME; don't skip one because you're in a\n" +
            "hurry. Altar sacrifices favor low-rarity items — don't feed a\n" +
            "Legendary.\n\n" +
            "SEE ALSO\n" +
            "[Secret Shrines (T1 Chain Weapons)] · [Anvil — Repair, Enhance, Evolve, Refine] · [Weapon Evolution Chains] · [Prefab Rooms — What They Are] · [Lore, Journals & Enchant Shrines]")
        {
            Tags = new[] { "world", "terrain", "prefab", "shrine" }
        },

        new("World", "Prefab Rooms — Trap Corridors",
            "┌─ World\n" +
            "│ Topic: Prefab Rooms — Trap Corridors\n" +
            "│ Shape: 3-tile-wide passage, 3-5 traps in sequence\n" +
            "│ Trap mix: Spike · Poison · Teleport · Alarm\n" +
            "│ Reveal: Search Mode (Dex check)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Some corridors are authored trap gauntlets: 3-5 sequential\n" +
            "traps in a 3-tile-wide passage. Spike, Poison, Teleport, and\n" +
            "Alarm each have their own prefab variants. Traps here are\n" +
            "clustered by design — not the scattered randoms.\n\n" +
            "USAGE\n" +
            "Search Mode (Dex check) reveals hidden traps. Two thirds of\n" +
            "trap-corridor traps are hidden; one third is telegraphed\n" +
            "pressure plates you can see and avoid.\n\n" +
            "EFFECTS\n" +
            "  Spike       Flat physical damage on step\n" +
            "  Poison      Poison stacks + small damage\n" +
            "  Teleport    Relocates you elsewhere on the floor\n" +
            "  Alarm       Aggros a wave of nearby monsters\n\n" +
            "Corridor prefabs never mix Teleport with Alarm in the same\n" +
            "run — the mixed aggro/relocate combo was too lethal.\n\n" +
            "COSTS\n" +
            "Each triggered trap costs a turn and burns HP or state. A\n" +
            "failed Search Mode Dex check still costs the action.\n\n" +
            "TIPS\n" +
            "Run Search Mode the moment the corridor silhouette shows — a\n" +
            "straight 3-wide hallway with no room branches is the tell.\n" +
            "Extra Skill — Search boosts reveal odds. Keep a stack of\n" +
            "Antidote on Poison-heavy floors.\n\n" +
            "SEE ALSO\n" +
            "[Traps & Hazards] · [Unique Skill: Extra Skill — Search] · [Mechanical Tiles] · [Prefab Rooms — What They Are] · [Status: Bleed & Poison]")
        {
            Tags = new[] { "world", "terrain", "prefab", "trap" }
        },

        new("World", "Prefab Rooms — Merchant Stalls",
            "┌─ World\n" +
            "│ Topic: Prefab Rooms — Merchant Stalls\n" +
            "│ Occupant: Vendor NPC + generated stock\n" +
            "│ Signals: Counter layout, BountyBoard, campfire\n" +
            "│ Stock: Scales with floor; layout scales with biome\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Wilderness merchant prefabs house a Vendor NPC with generated\n" +
            "stock. Look for counter-layouts, BountyBoards, and campfires —\n" +
            "visual cues that a merchant is near. Stock depth follows\n" +
            "floor; stall layout follows biome.\n\n" +
            "USAGE\n" +
            "Talk to the vendor. Prices match standard shop rules;\n" +
            "investing stacks across visits, even if the stall is a\n" +
            "one-off wilderness prefab.\n\n" +
            "EFFECTS\n" +
            "Vendor stock rolls from the floor-tier table, so a F20 stall\n" +
            "shows F20-tier goods. A BountyBoard inside the stall grants\n" +
            "the same quest options as a town board. Campfires inside the\n" +
            "stall are rest-legal.\n\n" +
            "COSTS\n" +
            "Standard Col prices. Investing locks Col for a stock boost at\n" +
            "the NEXT visit — not this one.\n\n" +
            "TIPS\n" +
            "Wilderness stalls are a chance to off-load heavy junk between\n" +
            "towns. Rest at the campfire before the next pull — the\n" +
            "stall-adjacent Safe Room flag is not guaranteed, so sleep\n" +
            "while it's free. Invest early on floors you'll revisit.\n\n" +
            "SEE ALSO\n" +
            "[Vendors — Rotating Stock] · [Vendor Investing] · [Col Economy — How You Earn] · [Prefab Rooms — What They Are] · [Campfires — Rest & Sleep XP]")
        {
            Tags = new[] { "world", "terrain", "prefab", "merchant" }
        },

        new("World", "Prefab Rooms — Boss Arenas",
            "┌─ World\n" +
            "│ Topic: Prefab Rooms — Boss Arenas\n" +
            "│ Canon bosses: Unique hand-authored arena\n" +
            "│ Non-canon: One of three generic sizes\n" +
            "│ Entry: Via Labyrinth Entrance stairs\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Every canon-named boss fights you in a unique hand-authored\n" +
            "arena keyed to their theme: Illfang in a Kobold Lord throne\n" +
            "room, Wythege in a flooded colonnade, Skull Reaper in a bone\n" +
            "dungeon. Non-canon floors use one of three generic arena\n" +
            "sizes (small 17×13, medium 25×19, large 33×25).\n\n" +
            "USAGE\n" +
            "Reached via the Labyrinth Entrance. Arena layout is\n" +
            "deterministic per boss — repeat runs see the same\n" +
            "architecture; only boss rolls (HP, ATK mods) vary with run\n" +
            "modifiers.\n\n" +
            "EFFECTS\n" +
            "Canon arenas may include keyed features: Wythege's colonnade\n" +
            "has Bog Water pools, Skull Reaper's hall has bone spike\n" +
            "clusters, Gleam Eyes' lair has lava cracks. These are part of\n" +
            "the arena, not the boss stats, so prep accordingly.\n\n" +
            "COSTS\n" +
            "Standard boss-fight stakes. Ascending returns you to the\n" +
            "post-boss overworld; losing drops you back at the last save.\n\n" +
            "TIPS\n" +
            "Memorize the arena after a scouting death — repeat layouts\n" +
            "mean the second attempt is a different fight. On canon\n" +
            "floors, pack the themed counter (Antidote for Wythege, fire\n" +
            "resist for Gleam Eyes). Generic-arena fights lean on boss\n" +
            "stats alone; run modifiers matter more than terrain.\n\n" +
            "SEE ALSO\n" +
            "[Floor Boss Roster — Canon Highlights] · [Labyrinth System] · [Floor Canon] · [Prefab Rooms — What They Are] · [Ascending a Floor]")
        {
            Tags = new[] { "world", "terrain", "prefab", "boss" }
        },

        new("World", "Biome Feel",
            "┌─ World\n" +
            "│ Topic: Biome Feel\n" +
            "│ Layers: Tile palette · global tint · entry text\n" +
            "│         · ambient particles · tree glyph variants\n" +
            "│ Goal: Read a biome in five seconds, no tag peek\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "A floor's biome shows through four layered cues: the base\n" +
            "tile palette, a global color tint that washes visible tiles,\n" +
            "entry-line flavor text on arrival, and ambient particles\n" +
            "that drift across walkable space. Put together, you should\n" +
            "read a biome in five seconds without looking at any tag.\n\n" +
            "USAGE\n" +
            "Automatic on floor entry. Use the Particles setting to tune\n" +
            "ambient density, or Off to drop the overlay entirely.\n\n" +
            "EFFECTS\n" +
            "  Forest     Green tint, drifting leaves, bushy tree glyphs\n" +
            "  Swamp      Sickly-green tint, gnat motes, drooping glyphs\n" +
            "  Desert     Amber tint, sand grains, scrubby glyphs\n" +
            "  Volcanic   Red tint, ember sparks, charred glyphs\n" +
            "  Ice        Pale-blue tint, snowflakes, frost-rimed glyphs\n" +
            "  Aquatic    Blue tint, bubble motes, kelp glyphs\n" +
            "  Ruins      Grey tint, dust, broken-pillar silhouettes\n" +
            "  Darkness   Near-black, no ambient, eye-glint glyphs\n" +
            "  Void       Purple tint, reality-warp motes, glitch glyphs\n\n" +
            "COSTS\n" +
            "Ambient particles cost a small amount of render time. Turning\n" +
            "Particles down to Low or Off is the fix on slow terminals.\n\n" +
            "TIPS\n" +
            "Trust the tint before the tag. If the floor LOOKS like\n" +
            "Swamp, pack Antidote even if you haven't seen a hazard yet.\n" +
            "The entry-line flavor text is lore — skim it once per new\n" +
            "biome, then trust your eyes.\n\n" +
            "SEE ALSO\n" +
            "[Biomes] · [Ambient World Animation] · [Weather] · [Labyrinth System] · [Terrain Hazards]")
        {
            Tags = new[] { "world", "terrain", "biome" }
        },

        new("World", "Feature Quotas",
            "┌─ World\n" +
            "│ Topic: Feature Quotas\n" +
            "│ Floor guarantees: 1+ shrine, 1+ chest, 1+ lore\n" +
            "│ Biome extras: Anvils (Urban), vents (Volcanic/Swamp)\n" +
            "│ Caps: Max counts prevent noise-y stacking\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Every floor guarantees at least one shrine, one chest, and\n" +
            "one piece of lore (LoreStone or Journal). Biome-specific\n" +
            "quotas may also guarantee anvils (Urban), gas vents\n" +
            "(Volcanic/Swamp), or pillars (Ruins). Maximums cap noise-y\n" +
            "scatter from stacking too thick.\n\n" +
            "USAGE\n" +
            "Transparent. Walk the floor; you'll meet quotas without\n" +
            "trying. Missing a guarantee after exploration >= 80% is a\n" +
            "bug — report it.\n\n" +
            "EFFECTS\n" +
            "  Shrine          min 1 per floor, max 3\n" +
            "  Chest           min 1 per floor, max 6 (scales w/ floor)\n" +
            "  LoreStone       min 1 of {LoreStone, Journal} per floor\n" +
            "  Anvil           guaranteed on Urban / Ruins floors\n" +
            "  Vent            guaranteed on Volcanic / Swamp floors\n" +
            "  Trap cluster    quota scales with floor tier\n\n" +
            "COSTS\n" +
            "None to the player. Quotas are a generator-side contract.\n\n" +
            "TIPS\n" +
            "If you cleared a floor and never saw a shrine, check hidden\n" +
            "rooms — Search Mode can reveal sealed prefab vaults. The\n" +
            "lore guarantee is easy to miss: a single Journal behind a\n" +
            "trapped corridor counts.\n\n" +
            "SEE ALSO\n" +
            "[Biomes] · [Lore, Journals & Enchant Shrines] · [Traps & Hazards] · [Prefab Rooms — What They Are] · [Ascending a Floor]")
        {
            Tags = new[] { "world", "terrain", "meta" }
        },

        new("World", "Floor Canon",
            "┌─ World\n" +
            "│ Topic: Floor Canon\n" +
            "│ Sources: Anime · LN · Progressive · Hollow Fragment\n" +
            "│          · Integral Factor · Fractured Daydream\n" +
            "│ Canon floors: Get unique arenas + town overlays\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Aincrad's 100 floors trace canon sources (anime, LN,\n" +
            "Progressive, Hollow Fragment, Integral Factor). Canon-named\n" +
            "bosses get unique arena prefabs; canon towns get unique\n" +
            "F1-style overlays. Non-canon floors are era-themed\n" +
            "interpolations.\n\n" +
            "USAGE\n" +
            "Read FLOOR_CANON.md for the full chart. F1 Town of\n" +
            "Beginnings, F22 Coral Village, F48 Lindarth, F55 Granzam,\n" +
            "F100 Ruby Palace all have full canon treatment.\n\n" +
            "EFFECTS\n" +
            "  Canon boss floors    Named boss, unique arena prefab\n" +
            "  Canon town floors    Hand-authored settlement overlay\n" +
            "  Era-themed floors    Canon biome + mob table, generic\n" +
            "                       arena and town prefabs\n" +
            "  Fully procedural     No canon hook; random biome roll\n\n" +
            "Roughly 16 of 100 floors have fully canon boss arenas;\n" +
            "another 6-8 have canon town overlays. The rest draw from\n" +
            "era-appropriate biome and mob pools.\n\n" +
            "COSTS\n" +
            "None. Canon content is additive — never gates progress.\n\n" +
            "TIPS\n" +
            "Watch the ascend banner: a canon boss title means you're\n" +
            "stepping into a unique arena with themed terrain. Canon\n" +
            "towns (F1, F22, F48, F55, F100) are the best stock-up\n" +
            "points — deeper vendor tables than generic settlements.\n\n" +
            "SEE ALSO\n" +
            "[Aincrad's 100 Floors & Eras] · [Lindarth Town (F48)] · [Floor Boss Roster — Canon Highlights] · [Prefab Rooms — Boss Arenas] · [Monument of Swordsmen (F1)]")
        {
            Tags = new[] { "world", "terrain", "canon", "boss" }
        },

        new("World", "Narrative Vignettes",
            "┌─ World\n" +
            "│ Topic: Narrative Vignettes\n" +
            "│ Size: 3×3 to 5×5 prefab clusters\n" +
            "│ Placement: Wilderness, off main paths\n" +
            "│ Reward: LoreStone or Journal (no combat)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Small 3×3 to 5×5 prefabs scattered across wilderness tell\n" +
            "micro-stories: a toppled cart with a spilled journal, a\n" +
            "hermit's shack with a cold campfire, a grave circle ringed\n" +
            "by pillars. They're ambient worldbuilding — no mandatory\n" +
            "interaction.\n\n" +
            "USAGE\n" +
            "Walk the wilderness. Vignettes appear as unusual clusters\n" +
            "off main paths. Loot in them is usually a LoreStone or\n" +
            "Journal, not combat rewards.\n\n" +
            "EFFECTS\n" +
            "  Toppled cart      Spilled Journal + 1-2 low-tier items\n" +
            "  Hermit's shack    Cold campfire (re-lightable), 1 Journal\n" +
            "  Grave circle      Ring of Pillars, 1 LoreStone center\n" +
            "  Traveler rest     Bedroll + ash pile + 1 note\n" +
            "  Wreckage          Broken gear + 1 LoreStone\n\n" +
            "Vignettes do NOT count toward Feature Quotas — they're a\n" +
            "bonus layer on top of the guaranteed lore count.\n\n" +
            "COSTS\n" +
            "None. No traps, no aggro, no turn penalties beyond the walk.\n\n" +
            "TIPS\n" +
            "Vignettes are a cheap exploration-percent boost on floors\n" +
            "where the 90% threshold matters. Re-light the hermit's\n" +
            "campfire for a safe rest spot far from town.\n\n" +
            "SEE ALSO\n" +
            "[Lore, Journals & Enchant Shrines] · [Day/Night Cycle] · [Biomes] · [Prefab Rooms — What They Are] · [Campfires — Rest & Sleep XP]")
        {
            Tags = new[] { "world", "terrain", "flavor" }
        },

        new("World", "Pocket Biomes",
            "┌─ World\n" +
            "│ Topic: Pocket Biomes\n" +
            "│ Size: 10-30 tiles, irregular patch\n" +
            "│ Floors: Band-edge floors (F2, F5, F10, F15, ...)\n" +
            "│ Trigger: Walk onto the palette swap\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "On the last floor of each biome band, 1-2 small pocket biomes\n" +
            "intrude into the dominant terrain. Expect a frozen pond on a\n" +
            "Grassland floor, an overgrown Forest patch cutting through\n" +
            "Ruins, or a lava vent near Desert dunes. Pockets are always\n" +
            "climatically-close to the host biome — you will not find Ice\n" +
            "in a Forest — and they exist only on band-edge floors.\n\n" +
            "USAGE\n" +
            "Walk the edges of the floor. Spot the palette swap: a patch of\n" +
            "sand in your grassland is a Desert pocket, a frozen pond on a\n" +
            "grass field is an Ice pocket. Borders blend with noise-\n" +
            "perturbed transitions so the seam is organic rather than\n" +
            "rectangular. Pocket tiles use the full intruding-biome tileset.\n\n" +
            "EFFECTS\n" +
            "  Tileset      Pocket tiles render from the intruding biome\n" +
            "               (ice, dunes, forest canopy, lava) while the\n" +
            "               rest of the floor keeps the host palette.\n" +
            "  Hazards      Pocket hazards apply — a volcanic pocket on a\n" +
            "               Desert floor still burns; an Ice pocket on a\n" +
            "               Grassland floor still slips. Hazards scale to\n" +
            "               the current floor, not the pocket biome's\n" +
            "               canonical band.\n" +
            "  Enemies/Loot Follow the dominant floor biome, not the pocket\n" +
            "               — mob spawns and drop pools do not swap inside\n" +
            "               the patch.\n" +
            "  Placement    1-2 pockets per eligible floor; interior band\n" +
            "               floors (e.g. F3, F4 in a Grassland band) have\n" +
            "               none.\n\n" +
            "COSTS\n" +
            "None directly. Pockets add variety without penalty — you pay\n" +
            "only the normal hazard cost if you step into one (a slip, a\n" +
            "burn tick, a poison stack).\n\n" +
            "TIPS\n" +
            "Band-edge floors (F2, F5, F10, F15, F25, F35, F50, F75, F100)\n" +
            "are the only ones that roll pockets; scout them deliberately\n" +
            "for unique terrain and hazard mixes. An Ice pocket is a free\n" +
            "cold-biome test chamber on a warm floor — useful for learning\n" +
            "slip timing before the next band forces the lesson. Pack one\n" +
            "hazard counter (Antidote for Swamp pockets, Heat Resist for\n" +
            "Volcanic) when ascending into a band edge.\n\n" +
            "SEE ALSO\n" +
            "[Biomes] · [Biome Feel] · [Feature Quotas] · [Floor Canon] · [Terrain Hazards] · [Ascending a Floor]")
        {
            Tags = new[] { "world", "terrain", "biome", "pocket" }
        },

        // ── 4. Items & Weapons ──

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
            "game) imports add 65 canon weapons. The cross-game sweep\n" +
            "further adds 55 named weapons from Alicization Lycoris (AL),\n" +
            "Lost Song (LS), and Last Recollection (LR) — putting the\n" +
            "named roster at roughly 190 weapons across all 13 classes.\n\n" +
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
            "[Material Tiers (Baseline)] · [Integral Factor Weapon Series] · [Infinity Moment Last Attack Bonus Weapons] · [Infinity Moment Shop Weapons] · [Memory Defrag Originals] · [Fractured Daydream Character Weapons] · [Alicization Lycoris Raid Weapons] · [SAO Lost Song Named Weapons] · [SAO Last Recollection Weapons] · [Weapon Refinement System]")
        {
            Tags = new[] { "weapons", "equipment" }
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
            "Enhancement Ores System for details.\n" +
            "Corruption Stones (Epic, F95+ field-boss drop @ 10%) are a\n" +
            "fourth family — they consume on use to transform a specific\n" +
            "target weapon into its Corrupted variant. See Corruption\n" +
            "Stones & Corrupted Weapons.\n\n" +
            "COSTS\n" +
            "Col scales sharply at the top — Celestial is 60x the Iron\n" +
            "price.\n\n" +
            "TIPS\n" +
            "Don't skip a tier — the Col spend curve assumes each bracket\n" +
            "gets bought; jumping straight to Adamantite burns your purse\n" +
            "and leaves you under-enhanced.\n\n" +
            "SEE ALSO\n" +
            "[Weapon Types Overview] · [Named Legendary Highlights] · [Refinement Ingots] · [Enhancement Ores System] · [Corruption Stones & Corrupted Weapons]")
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
            "  Macafitel         Rapier                          Yuuki FD\n" +
            "Cross-game sweep (AL/LS/LR, F70-F99):\n" +
            "  Demon Blade Muramasa    Katana, LS apex           LS top-tier\n" +
            "  Lang                    2H Axe, highest LS ATK    LS top-tier\n" +
            "  Futsu no Mitama         Katana, canon myth        LS mytho\n" +
            "  Divine Laevateinn       Spear, canon myth         LS mytho\n" +
            "  Object Eraser           2H Sword, canon myth      LS mytho\n" +
            "  Darkness Rending Blade  Katana, Eydis canon       LR canon\n" +
            "  Rainbow Blade Ex Eterna 1H Sword, LR DLC skin     LR DLC\n" +
            "  Corrupted Elucidator    1H Sword, +Bleed/-Holy    Corruption Stone\n" +
            "  Corrupted Dark Repulser 1H Sword, +Freeze/-Holy   Corruption Stone\n\n" +
            "COSTS\n" +
            "None — the encounter itself is the cost. Lisbeth R6 crafts\n" +
            "cost 3M Col + rare mats each (see Lisbeth craft topic).\n" +
            "IM LAB weapons carry the IsEnhanceable=false flag — high base\n" +
            "stats, no scaling. IM Shop weapons enhance normally. MD/FD\n" +
            "originals drop from floor-banded loot pools.\n\n" +
            "TIPS\n" +
            "Mjolnir is flagged as Divine apex; pair it with stun-heavy\n" +
            "skills for lockdown. Dual Blades users should chase both\n" +
            "Elucidator and Dark Repulser (or their Corrupted variants via\n" +
            "Corruption Stones, which preserve IsDualWieldPaired). The\n" +
            "three IF Legendary series (Rosso/Yasha/Gaou) cover F61-F100.\n" +
            "With Lisbeth's 18-recipe R6 line, Avatar Weapons, Infinity\n" +
            "Moment additions, MD/FD canon drops, and the AL/LS/LR cross-\n" +
            "game sweep, the F50+ arsenal spans every SAO game.\n\n" +
            "SEE ALSO\n" +
            "[Memory Defrag Originals] · [MD Alicization Canonical Extras] · [Fractured Daydream Character Weapons] · [Elemental Weapon Variants] · [Integral Factor Weapon Series] · [Infinity Moment Last Attack Bonus Weapons] · [Infinity Moment Shop Weapons] · [Lisbeth — Rarity 6 Craft Line] · [Avatar Weapons & Last-Attack Bonus] · [Alicization Lycoris Raid Weapons] · [SAO Lost Song Named Weapons] · [SAO Last Recollection Weapons] · [Corruption Stones & Corrupted Weapons]")
        {
            Tags = new[] { "weapons", "rarity", "cross-game" }
        },

        new("Items", "Divine Object Set — Integrity Knights",
            "┌─ Items\n" +
            "│ Topic: Divine Object Set — Integrity Knights\n" +
            "│ Tier: Divine (8 NPC-quest Divines of 17 total)\n" +
            "│ Weapon type: Knight-themed + Dorothy's scythe\n" +
            "│ Source: Canon boss / quest hand-placed\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Seven canon Integrity Knight swords plus Dorothy's Starlight\n" +
            "Banner (Last Recollection, F78) make up the non-chain half of\n" +
            "the 17-piece Divine Object roster. Each is locked to a\n" +
            "specific encounter or quest.\n\n" +
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
            "  Heaven-Piercing Blade Fanatio    Azariya's quest (F50)     PiercingBeam+30 Rng2\n" +
            "  Starlight Banner      Dorothy    Dorothy's quest (F78)     HolyAoE+20, Scythe\n\n" +
            "COSTS\n" +
            "Divine gear is unbreakable, so no Anvil repair cost applies.\n\n" +
            "TIPS\n" +
            "Four of the eight are quest-locked — Alice (Selka F65), Fanatio\n" +
            "(Azariya F50), and Dorothy's Starlight Banner (F78) each\n" +
            "require the associated NPC chain. Starlight Banner is the only\n" +
            "Divine Scythe; pair it with Scythe proficiency for the 2-range\n" +
            "reaper build.\n\n" +
            "SEE ALSO\n" +
            "[Divine Objects] · [Dorothy (F78)] · [Selka the Novice (F65)] · [Sister Azariya (F50)]")
        {
            Tags = new[] { "weapons", "divine", "last-recollection" }
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
            "climb — they don't compete.\n" +
            "DISTINCT FROM CORRUPTION STONES: F95+ field bosses also roll\n" +
            "a 10% Corruption Stone drop. Avatar (2%/10%) and Corruption\n" +
            "Stone (10%) rolls are independent — a single F95+ field-boss\n" +
            "kill can yield both.\n\n" +
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
            "[Field Bosses — Guaranteed Drops] · [Named Legendary Highlights] · [Weapon Types Overview] · [Infinity Moment Last Attack Bonus Weapons] · [Hollow Fragment HNM Questgivers (F79-F99)] · [Corruption Stones & Corrupted Weapons]")
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
            "Eleven equipment slots cover weapon, armor, accessories, off-\n" +
            "hand, and the new Tool slot. OffHand rules depend on main-hand\n" +
            "choice and Dual Blades unlock state.\n\n" +
            "USAGE\n" +
            "Equip from inventory. Rings can stack two; necklaces and\n" +
            "bracelets are one each. The Tool slot is single-occupancy and\n" +
            "currently accepts pickaxes.\n\n" +
            "EFFECTS\n" +
            "Slots: Weapon, Head, Chest, Legs, Feet, RightRing, LeftRing,\n" +
            "Bracelet, Necklace, OffHand, Tool.\n" +
            "TOOL: Holds non-combat utility gear. Bundle 10 ships pickaxes\n" +
            "(Wooden / Iron / Mithril) — equipping one enables the bump-\n" +
            "action mining swing on ore-vein tiles. Tool slot is independent\n" +
            "of OffHand, so a sword + shield + pickaxe loadout is legal\n" +
            "(unlike weapon-vs-shield, the Tool slot never trades against\n" +
            "your combat slots). Future bundles may add other tool types.\n" +
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
            "[Gear Compare] · [Unique Skill: Dual Blades] · [Paired Dual-Wield Weapons] · [Weapon Refinement System] · [Accessories] · [Mining — Tool Slot & Ore Veins]")
        {
            Tags = new[] { "equipment", "weapons", "refinement" }
        },

        new("Items", "Mining — Tool Slot & Ore Veins",
            "┌─ Items\n" +
            "│ Topic: Mining — Tool Slot & Ore Veins\n" +
            "│ Slot: Tool (new — equip a Pickaxe)\n" +
            "│ Tiles: OreVeinIron · OreVeinMithril · OreVeinDivine\n" +
            "│ Action: Bump a vein with a Pickaxe equipped\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Mining is Bundle 10's headline non-combat loop. Equip a\n" +
            "Pickaxe in the new Tool slot, walk up to an ore vein on the\n" +
            "map, and bump into it the way you'd bump an enemy — your\n" +
            "swing strikes the vein, costs 1 turn, ticks 1 durability off\n" +
            "the pickaxe, and chips down the vein's strike counter. When\n" +
            "the vein hits 0 strikes it converts to a walkable Depleted\n" +
            "tile and drops its loot stack on you.\n\n" +
            "USAGE\n" +
            "1. Find a Pickaxe (vendor in Town of Beginnings sells the\n" +
            "   Wooden tier; harder tiers gate by floor).\n" +
            "2. Equip it via Inventory → Tool slot.\n" +
            "3. Explore until you spot an ore glyph on the map:\n" +
            "      ◊  OreVeinIron      gray (RGB ~170,170,180)\n" +
            "      ◊  OreVeinMithril   pale blue (RGB ~190,210,255)\n" +
            "      ◈  OreVeinDivine    pulsing gold (RGB ~255,200,80)\n" +
            "4. Move into the vein tile. Each bump = 1 strike, 1 turn,\n" +
            "   1 durability tick on the pickaxe.\n" +
            "5. Keep striking until the vein depletes — it switches to a\n" +
            "   '·' Depleted tile (walkable) and drops the ore stack.\n\n" +
            "EFFECTS\n" +
            "STRIKES TO DEPLETE (subtracted by pickaxe MiningPower, min 1):\n" +
            "  Iron     3 strikes\n" +
            "  Mithril  5 strikes\n" +
            "  Divine   8 strikes\n" +
            "Higher-tier pickaxes (MiningPower > 0) shorten these counts.\n" +
            "DROPS ON DEPLETION:\n" +
            "  Iron     iron_ingot + iron_ore\n" +
            "  Mithril  mithril_ingot + mithril_trace\n" +
            "  Divine   divine_fragment + (rare) primordial_shard\n" +
            "BIOME DENSITY (relative — generation pass weights veins by\n" +
            "biome richness):\n" +
            "  Volcanic + Void              richest tiles\n" +
            "  Plains / Forest / Cave       moderate\n" +
            "  Aquatic + Swamp              barren — rivers and bogs hide\n" +
            "                               little ore\n" +
            "FLOOR EXCLUSIONS:\n" +
            "  F1 (Town of Beginnings)      no veins — civic floor\n" +
            "  F100 (Ruby Palace)           no veins — final boss arena\n\n" +
            "COSTS\n" +
            "Each strike spends 1 durability. A pickaxe at 0 durability\n" +
            "SHATTERS — the item is destroyed, not just disabled, and\n" +
            "you lose the Tool slot until you equip another. On long\n" +
            "expeditions to Mithril or Divine bands, carry a SPARE\n" +
            "pickaxe — chipping a Divine vein with 8 strikes costs at\n" +
            "minimum 8 of the 30 durability on a Wooden Pickaxe, so\n" +
            "rookie-tier kit doesn't survive a Divine farming run.\n\n" +
            "TIPS\n" +
            "Veins cluster — generation places them in the same room or\n" +
            "the same lake-edge corridor as part of the same pass. If\n" +
            "you spot one, sweep the surrounding 5x5 before pressing on.\n" +
            "Iron veins are the fastest XP-per-durability ratio for\n" +
            "leveling Mining; Divine veins are the highest XP per strike\n" +
            "but eat durability and gate behind F75+ access. The Tool\n" +
            "slot does NOT compete with combat slots — keep your sword,\n" +
            "shield, AND a pickaxe equipped at all times once you have\n" +
            "the Tool slot unlocked.\n\n" +
            "SEE ALSO\n" +
            "[Pickaxe Tiers] · [Mining (Life Skill)] · [Equipment Slots & Dual Wield] · [Biomes] · [Refinement Ingots]")
        {
            Tags = new[] { "items", "mining", "tool", "life-skills" }
        },

        new("Items", "Pickaxe Tiers",
            "┌─ Items\n" +
            "│ Topic: Pickaxe Tiers\n" +
            "│ Slot: Tool\n" +
            "│ Tiers: Wooden · Iron · Mithril (B10 ship) — more in future\n" +
            "│ Source: Town of Beginnings vendor + floor-gated shops\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Pickaxes are Tool-slot equipment that enable mining. Bundle\n" +
            "10 ships three tiers, gated by floor and Col, each with\n" +
            "different MaxDurability and MiningPower (the strike-cost\n" +
            "reducer). Higher tiers chip veins faster AND last longer\n" +
            "before shattering.\n\n" +
            "USAGE\n" +
            "Buy from the Town of Beginnings vendor (Wooden, no level\n" +
            "gate) or from floor-gated shops at F10+ (Iron) and F50+\n" +
            "(Mithril). Equip via Inventory → Tool slot. Repair at the\n" +
            "Anvil like any equipment piece — pickaxes follow normal\n" +
            "durability/repair rules.\n\n" +
            "EFFECTS\n" +
            "  TIER       FLOOR   COL    MAXDUR   MININGPOWER   NOTE\n" +
            "  Wooden     F1      80     30       0             Starter — buy in TOB\n" +
            "  Iron       F10+    320    80       1             -1 strike per vein, min 1\n" +
            "  Mithril    F50+    1800   200      2             -2 strikes, +10% OreQuality\n" +
            "MININGPOWER applies as: actual_strikes = max(1, base_strikes\n" +
            "- MiningPower). Iron Pickaxe vs an Iron vein (3 base) needs\n" +
            "2 strikes; Mithril Pickaxe vs an Iron vein needs 1 strike.\n" +
            "Mithril's +10% OreQualityBonus rolls higher rarity tiers\n" +
            "of the dropped ingots — silently improves Anvil refinement\n" +
            "yield over the run.\n\n" +
            "COSTS\n" +
            "Pickaxes SHATTER at 0 durability — destroyed, not disabled.\n" +
            "A Wooden Pickaxe lasts 30 strikes (≈10 Iron veins or 6\n" +
            "Mithril, less for Divine). Iron lasts 80, Mithril 200. Plan\n" +
            "your Col reserve so a long Mithril/Divine farming push\n" +
            "doesn't strand you toolless mid-floor.\n\n" +
            "TIPS\n" +
            "Buy the Iron Pickaxe at F10 — the strike-cost reduction\n" +
            "alone pays back the 320 Col within ~5 veins, and the durability\n" +
            "tripling means you stop respawning Wooden replacements every\n" +
            "shop visit. Mithril is the late-game investment — its +10%\n" +
            "OreQuality compounds across the F50-F99 push, where you'll\n" +
            "be chipping Mithril and Divine veins by the dozen for end-\n" +
            "game refinement ingots.\n\n" +
            "SEE ALSO\n" +
            "[Mining — Tool Slot & Ore Veins] · [Mining (Life Skill)] · [Anvil — Repair, Enhance, Evolve, Refine] · [Refinement Ingots]")
        {
            Tags = new[] { "items", "mining", "tool", "weapons" }
        },

        new("Items", "Gear Compare",
            "┌─ Items\n" +
            "│ Topic: Gear Compare\n" +
            "│ Tier: Any equippable item\n" +
            "│ Weapon type: All slots\n" +
            "│ Source: Inventory, Shop, Chest peek\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "A side-by-side stat diff that appears whenever you hover an\n" +
            "equippable item. The panel shows the hovered item next to what\n" +
            "you currently have in the matching slot, with green +N gains\n" +
            "and red -N losses per stat line — no mental math required.\n\n" +
            "USAGE\n" +
            "The diff shows up automatically in three places:\n" +
            "  - Inventory (I): hover any weapon, armor, or accessory\n" +
            "  - Shop dialog: hover any stocked item before buying\n" +
            "  - Chest peek: hover the chest contents before picking up\n" +
            "Your currently-equipped item is shown on the left; the hovered\n" +
            "candidate is shown on the right. If you're dual-wielding, the\n" +
            "panel compares against the main-hand slot by default — press\n" +
            "the slot-swap key shown at the bottom of the panel to compare\n" +
            "against the off-hand instead.\n\n" +
            "EFFECTS\n" +
            "Per-stat deltas are color-coded:\n" +
            "  Green  +N     strict upgrade on that line\n" +
            "  Red    -N     strict downgrade on that line\n" +
            "  Gray    =     no change\n" +
            "Overall \"is this better\" is not summarised into a single\n" +
            "number — two builds can read the same diff differently (Atk\n" +
            "vs CritRate, DEF vs weight). Shown stats include Attack,\n" +
            "Defense, CritRate, CritDmg, durability, and any unique\n" +
            "modifiers (Holy, elemental, ComboBonus, refinement bonuses).\n" +
            "WEAPON-TYPE MISMATCH BANNER:\n" +
            "When the hovered weapon is a different class than your current\n" +
            "(1H Sword vs Katana, etc.) the panel flashes a banner:\n" +
            "  \"Different weapon class — proficiency will not transfer\"\n" +
            "This is a WARNING only — you can still equip it, but your\n" +
            "weapon-proficiency rank resets the clock on the new class.\n\n" +
            "COSTS\n" +
            "Pure presentation — no cost, no turn consumed, no side effect.\n" +
            "The panel simply appears while an item is hovered.\n\n" +
            "TIPS\n" +
            "In the shop, the diff is your best defense against overpaying:\n" +
            "a Rare stock weapon often reads as flat sidegrade vs the drop\n" +
            "you picked up twenty turns ago, and the vendor's +20% markup\n" +
            "makes the sidegrade a losing trade. Use the chest-peek diff to\n" +
            "decide BEFORE you pick up — picking up a strictly-worse item\n" +
            "just to stash it still eats an inventory slot. The weapon-type\n" +
            "mismatch banner matters most mid-run: resetting proficiency for\n" +
            "a +3 Attack sidegrade almost never pays back the kills lost.\n\n" +
            "SEE ALSO\n" +
            "[Equipment Slots & Dual Wield] · [Weapon Proficiency Ranks] · [Rarity Tiers & Drop Rates] · [Dynamic Shop Tiering (F50+)] · [Damage & Toast Feedback] · [Quickbar & Consumables]")
        {
            Tags = new[] { "equipment", "weapons", "ui", "shop" }
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
            "[Infinity Moment Shop Weapons] · [Vendors — Rotating Stock] · [Vendor Investing] · [Ascending a Floor] · [Floor Boss Roster — Canon Highlights] · [Gear Compare]")
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

        new("Items", "Alicization Lycoris Raid Weapons",
            "┌─ Items\n" +
            "│ Topic: Alicization Lycoris Raid Weapons\n" +
            "│ Tier: Epic + Legendary (29 weapons)\n" +
            "│ Weapon type: Spread across classes (F70-F99)\n" +
            "│ Source: AL raid tiers + Relic bosses + DLC\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Twenty-nine canon weapons imported from Alicization Lycoris\n" +
            "(AL), the 2020 action-RPG Underworld entry. Split across four\n" +
            "subgroups: Normal Raid (7 Epic), Extreme Raid (7 Legendary),\n" +
            "Relic Boss drops (9 Legendary), and DLC (6 Legendary).\n\n" +
            "USAGE\n" +
            "Drop from the listed floor-banded canon source. All 29 slot\n" +
            "into standard loot pools and enhance normally at the Anvil.\n\n" +
            "EFFECTS\n" +
            "NORMAL RAID (Epic, F70-85):\n" +
            "  Skyrend (2H Axe), Timestream (Rapier), Veinshredder (Dagger),\n" +
            "  Dragonstar (1H), Heavenstriker (Spear), Superior Blade (1H),\n" +
            "  Sacred Inferno (2H)\n" +
            "EXTREME RAID (Legendary, F85-95):\n" +
            "  Blade of the Lightwolf (1H), Graceful Needle (Rapier),\n" +
            "  Whitespark (Dagger), Demonslayer (2H), Blazewyrm Greatsword\n" +
            "  (2H), Arctic Pillar (Spear), Starshatter (Mace)\n" +
            "RELIC BOSS (Legendary, F80-95):\n" +
            "  Scorching Blade (1H), Beasthowl (Mace), Double-Edged Blade\n" +
            "  (2H), Whirlpool Hammer (Mace), Cinder Bow, Dragoncrest (1H),\n" +
            "  Deathbringer (2H Axe), Frostpeak (Spear), Snowsunder (Axe)\n" +
            "DLC (Legendary, F85-99):\n" +
            "  Loveblight Bow, Purgatorial Greatsword, Savage Sandstorm\n" +
            "  (Scimitar), Illustrious Sword, Lifestream Greatsword,\n" +
            "  Glitzwood Bow\n\n" +
            "COSTS\n" +
            "None beyond the encounter. Epic Normal-Raid set tops out at\n" +
            "F85, while the Relic/Extreme/DLC Legendaries cover F85-F99.\n\n" +
            "TIPS\n" +
            "If you're deep into a specific class, skim the subgroups for\n" +
            "the right weapon type rather than farming every raid tier —\n" +
            "the Normal Raid 7 together cover every major melee class at\n" +
            "Epic, so they're often a mid-climb upgrade before the F85\n" +
            "Legendary wave lands.\n\n" +
            "SEE ALSO\n" +
            "[Named Legendary Highlights] · [SAO Lost Song Named Weapons] · [SAO Last Recollection Weapons] · [Weapon Types Overview]")
        {
            Tags = new[] { "alicization-lycoris", "weapons", "cross-game" }
        },

        new("Items", "SAO Lost Song Named Weapons",
            "┌─ Items\n" +
            "│ Topic: SAO Lost Song Named Weapons\n" +
            "│ Tier: Epic + Legendary (21 weapons)\n" +
            "│ Weapon type: Spread across classes (F50-F99)\n" +
            "│ Source: LS canon drops / pool rolls\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Twenty-one canon weapons imported from Sword Art Online: Lost\n" +
            "Song (LS), the 2015 ALfheim-set entry with flight combat and\n" +
            "race-gated magic. Split into top-tier-per-type (9 Legendary),\n" +
            "canon mythological (10 Legendary), and 2 Easter-egg Epics.\n\n" +
            "USAGE\n" +
            "Drop from floor-banded loot pools and canon named sources.\n" +
            "All 21 enhance normally at the Anvil.\n\n" +
            "EFFECTS\n" +
            "TOP-TIER PER TYPE (Legendary, F80-95):\n" +
            "  Blazing Sword (1H), Glaring Light (Rapier), Fragarach\n" +
            "  (Dagger), Demon Blade Muramasa (Katana), Lang (2H Axe,\n" +
            "  highest LS ATK), Caduceus (Mace), Brave Song (Spear),\n" +
            "  Silvan Bow, Iron Fist Oguma (Claws)\n" +
            "CANON MYTHOLOGICAL (Legendary, F75-99):\n" +
            "  Futsu no Mitama (Katana), Nadr (Axe), Divine Laevateinn\n" +
            "  (Spear), Elder's Trident (Spear), Holy L'arc Qui ne Faut\n" +
            "  (Bow), Artemis' Fult (Bow), Paopei (Claws), Object Eraser\n" +
            "  (2H), Giardino (Dagger), Ancile (Shield)\n" +
            "EASTER EGGS (Epic, F50-70):\n" +
            "  Kagetsu-4 (1H), Laser Sword HG (1H)\n\n" +
            "COSTS\n" +
            "None beyond the encounter or floor-pool roll.\n\n" +
            "TIPS\n" +
            "Lang is the highest-ATK canonical LS axe — worth the dedicated\n" +
            "farm on F90+ if you main 2H Axe. Artemis' Fult (LS) and\n" +
            "Artemis (IM LAB F99 bow) are separate weapons with distinct\n" +
            "DefIds; both can live in the same inventory.\n\n" +
            "SEE ALSO\n" +
            "[Named Legendary Highlights] · [Alicization Lycoris Raid Weapons] · [SAO Last Recollection Weapons] · [Weapon Types Overview]")
        {
            Tags = new[] { "lost-song", "weapons", "cross-game" }
        },

        new("Items", "SAO Last Recollection Weapons",
            "┌─ Items\n" +
            "│ Topic: SAO Last Recollection Weapons\n" +
            "│ Tier: Epic / Legendary / Divine (5 weapons)\n" +
            "│ Weapon type: Scythes + 1H Swords + Katana\n" +
            "│ Source: LR canon / Dorothy F78 quest / DLC\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Five canon weapons imported from Sword Art Online: Last\n" +
            "Recollection (LR), the 2023 entry that caps the Alicization\n" +
            "Aocean arc. Includes the 8th Divine Object (Starlight Banner)\n" +
            "and Eydis's canon katana.\n\n" +
            "USAGE\n" +
            "Dorothy's Starlight Banner is a fixed F78 quest reward. The\n" +
            "other four drop from floor-banded pools or canon sources.\n\n" +
            "EFFECTS\n" +
            "  Starlight Banner       DIVINE Scythe, F78 Dorothy quest  HolyAoE+20\n" +
            "  Azuretear Scythe       Epic Scythe, Dorothy's base, F50-65\n" +
            "  Darkness Rending Blade Legendary Katana, Eydis canon, F80-95\n" +
            "  Rainbow Blade Ex Eterna Legendary 1H Sword, LR DLC skin, F90-99\n" +
            "  Aetherial Glow         Epic 1H Sword, LR DLC skin, F60-80\n\n" +
            "COSTS\n" +
            "Starlight Banner costs the Dorothy quest (22 kills on F78);\n" +
            "the rest cost nothing beyond the encounter or pool roll.\n\n" +
            "TIPS\n" +
            "Starlight Banner is the ONLY Divine Scythe in the game and\n" +
            "the 8th non-chain Divine Object overall — don't skip Dorothy's\n" +
            "questline if you main Scythe. Azuretear pairs thematically as\n" +
            "Dorothy's pre-purification base and works as an F50-65 bridge\n" +
            "toward the Divine upgrade.\n\n" +
            "SEE ALSO\n" +
            "[Dorothy (F78)] · [Divine Object Set — Integrity Knights] · [Alicization Lycoris Raid Weapons] · [SAO Lost Song Named Weapons]")
        {
            Tags = new[] { "last-recollection", "weapons", "divine" }
        },

        new("Items", "Corruption Stones & Corrupted Weapons",
            "┌─ Items\n" +
            "│ Topic: Corruption Stones & Corrupted Weapons\n" +
            "│ Tier: Epic Stone -> Legendary Corrupted weapon\n" +
            "│ Weapon type: Consumable + transformation hook\n" +
            "│ Source: F95+ field-boss 10% drop, random stone\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "A new consumable family that transforms a specific target\n" +
            "weapon into its Corrupted variant — higher base stats with a\n" +
            "HolyDamage penalty. Two stones, two Corrupted weapons, all\n" +
            "canon trade-off flavor.\n\n" +
            "USAGE\n" +
            "Kill any F95+ field boss; 10% chance to drop a stone (randomly\n" +
            "Night or Shadow). Use the stone from inventory — the game\n" +
            "checks equipped slot, off-hand, and backpack for the matching\n" +
            "target weapon, prompts for confirmation, consumes one stone,\n" +
            "removes the target, and grants the Corrupted variant.\n\n" +
            "EFFECTS\n" +
            "  Night Corruption Stone  Elucidator     -> Corrupted Elucidator\n" +
            "  Shadow Corruption Stone Dark Repulser  -> Corrupted Dark Repulser\n" +
            "Corrupted Elucidator: Legendary 1H Sword, baseDmg 175,\n" +
            "Attack+92, Bleed+25, CritRate+20, HolyDamage-10.\n" +
            "Corrupted Dark Repulser: Legendary 1H Sword, baseDmg 175,\n" +
            "Attack+92, Freeze+25, CritRate+20, HolyDamage-10.\n" +
            "EnhancementLevel, EnhancementOreHistory, and RefinementSlots\n" +
            "are preserved on transformation. IsDualWieldPaired stays true\n" +
            "— Corrupted variants still pair with non-corrupted partners.\n" +
            "If the target weapon is missing, the stone is refunded rather\n" +
            "than silently consumed.\n\n" +
            "COSTS\n" +
            "One stone per transformation. F100 ends the game, so there's\n" +
            "no post-F100 canon source — F95+ field bosses are the only\n" +
            "stone tap. Canon trade-off: Corrupted pays a HolyDamage-10\n" +
            "penalty for its stat boost.\n\n" +
            "TIPS\n" +
            "Enhance Elucidator / Dark Repulser to +7 or higher BEFORE you\n" +
            "apply a Corruption Stone — levels carry forward and you avoid\n" +
            "re-grinding Anvil attempts on the higher-base Corrupted form.\n" +
            "Dual Blades users can keep one corrupted and one clean (e.g.\n" +
            "Corrupted Elucidator + Dark Repulser) to soften the HolyDamage\n" +
            "hit on the pair.\n\n" +
            "SEE ALSO\n" +
            "[Named Legendary Highlights] · [Field Bosses — Guaranteed Drops] · [Avatar Weapons & Last-Attack Bonus] · [Material Tiers (Baseline)]")
        {
            Tags = new[] { "corruption-stone", "corrupted-weapon", "weapons" }
        },

        new("Items", "Advanced Weapon Effects",
            "┌─ Items\n" +
            "│ Topic: Advanced Weapon Effects\n" +
            "│ Tier: T3+ rarity weapons\n" +
            "│ Weapon type: Any — tag string on SpecialEffect\n" +
            "│ Source: Tooltip SpecialEffect line\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Several weapon SpecialEffect strings now fire live mechanics\n" +
            "beyond the flavor text. Weapons at T3+ rarity carry tags like\n" +
            "DragonSlayer, CritImmune, TrueStrike, Barrier, and friends —\n" +
            "each reads \"Name+N\" where N is the magnitude and the mechanic\n" +
            "fires automatically in combat. These used to be cosmetic; they\n" +
            "are now wired.\n\n" +
            "USAGE\n" +
            "Equip a weapon, open Inventory, and read the SpecialEffect\n" +
            "line in the tooltip. Effects apply while the weapon is held —\n" +
            "no toggle, no activation. Multiple equipped pieces (main-hand,\n" +
            "off-hand, accessories with effect strings) stack their tags.\n\n" +
            "EFFECTS\n" +
            "  HPRegen+N         Heal N HP on the passive-regen tick\n" +
            "  SPRegen+N         Restore N SP on the passive-regen tick\n" +
            "  CritImmune+N      N% chance to downgrade incoming crits\n" +
            "  TrueStrike+N      N% chance to bypass evade/block per swing\n" +
            "  HolyDamage+N      +N damage vs undead/demon-tagged mobs\n" +
            "  Barrier+N         Absorb up to N damage before HP; refreshes\n" +
            "                    per floor\n" +
            "  EvadeRegen+N      HP regen on every successful dodge\n" +
            "  DragonSlayer+N    +N damage vs dragon-tagged mobs (Asterius,\n" +
            "                    X'rphan, Frost Dragon, and similar)\n" +
            "  ExecuteThreshold+N 2x damage vs targets below N% HP\n" +
            "  Uninterruptible+N N% chance to ignore stagger/stun on self\n" +
            "  BlindOnHit+N      N% on-hit Blind proc\n" +
            "  Stun+N            N% on-hit Stun proc\n" +
            "  Poison+N          N% on-hit Poison proc\n" +
            "  SlowOnHit+N       N% on-hit Slow proc — target acts every\n" +
            "                    other turn for 3 turns (Bundle 10 wired;\n" +
            "                    Guilty Thorn's SlowOnHit+30 is the canon\n" +
            "                    test bench). Mob-ID parity desyncs the\n" +
            "                    skip-turn cadence, so two slowed mobs in\n" +
            "                    the same pull won't act on the same frame.\n" +
            "  Cleave+N          Multi-target adjacent damage, +N magnitude\n" +
            "  ArmorPierce+N     Ignore N armor on the struck target\n" +
            "  PiercingShot+N    Ranged variant — pierces N armor/ranks\n" +
            "  FrostDamage+N     Cold-element rider on each hit\n" +
            "  ThrustDmg+N       Modifier on thrust-class sword skills\n" +
            "  NightDamage+N     +N damage in Dark biome / Night phase\n" +
            "  Invisibility+N    Exotic — brief stealth window on trigger\n" +
            "  Lunacy+N          Exotic — chance-based erratic proc suite\n\n" +
            "BUNDLE 10 — CROSS-SLOT PARSING:\n" +
            "SpecialEffect strings now parse on Armor and Shield as well as\n" +
            "Weapon (previously weapon-only). Defensive additive keys SUM\n" +
            "across every equipped slot: BlockChance, ParryChance,\n" +
            "EvadeRegen, HPRegen, SPRegen all add together — a HPRegen+2\n" +
            "ring + Yasha Kavacha's HPRegen+3 shield + a HPRegen+1 chest\n" +
            "ticks for +6 HP per regen pulse. ON-HIT procs (Bleed, Stun,\n" +
            "Slow, Poison, Blind, Lunacy) remain weapon-only by design —\n" +
            "they're swing-themed in canon, and armor-side on-hit procs\n" +
            "would be off-flavor for the death-game tone.\n\n" +
            "COSTS\n" +
            "None. SpecialEffects are passive on the equipped weapon and\n" +
            "do not consume durability, SP, or stamina beyond the normal\n" +
            "swing cost.\n\n" +
            "TIPS\n" +
            "Always check the SpecialEffect line before selling a T3+ drop\n" +
            "— an unassuming weapon with Barrier+40 is a tank anchor.\n" +
            "Stack complementary tags: Barrier + HPRegen turns a mace into\n" +
            "a wall, TrueStrike + Poison guarantees DoT uptime, DragonSlayer\n" +
            "is dead weight outside dragon-tagged floors but carries the\n" +
            "fight on F48 (Frost Dragon) and F100 approaches. NightDamage\n" +
            "weapons pair with Darkness Blade for compound Dark-biome bursts.\n\n" +
            "SEE ALSO\n" +
            "[Damage Formula] · [Critical Hits] · [Status: Bleed & Poison] · [Status: Stun & Slow] · [Weapon Proficiency Ranks] · [Weapon Types Overview] · [Floor Boss Roster — Canon Highlights]")
        {
            Tags = new[] { "items", "weapon", "combat", "special-effect" }
        },

        // ── 5. Quests, NPCs & Economy ──

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
            "[Quest Types & Rewards] · [Town of Beginnings NPCs (F1)] · [Vendors — Rotating Stock] · [Save System] · [Quest Tracker]")
        {
            Tags = new[] { "quests", "npcs", "economy" }
        },

        new("Quests & NPCs", "Quest Tracker",
            "┌─ Quests & NPCs\n" +
            "│ NPC: N/A (HUD widget)\n" +
            "│ Floor: All\n" +
            "│ Quest: Pinned-quest progress readout\n" +
            "│ Reward: Always-visible objective + turn-in hint\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "A small HUD widget parked top-right (directly below the minimap)\n" +
            "that shows your currently pinned quest: title, short objective,\n" +
            "and live progress counter. On completion it flips to a green\n" +
            "accent and prints \"COMPLETE → return to NPC\".\n\n" +
            "USAGE\n" +
            "The first quest you accept is auto-pinned. Open the Quest Log\n" +
            "(J) and press P on any other quest to pin/unpin it manually —\n" +
            "only one quest is pinned at a time, so pinning a new one\n" +
            "replaces the previous. The widget hides itself when no quest\n" +
            "is pinned.\n\n" +
            "EFFECTS\n" +
            "Widget layout (top-right, below minimap):\n" +
            "  line 1 — quest title (truncated to fit)\n" +
            "  line 2 — objective text\n" +
            "  line 3 — progress counter or COMPLETE banner\n" +
            "On completion the entire frame takes a green accent so peripheral\n" +
            "vision catches the state change without having to read the text.\n\n" +
            "COSTS\n" +
            "None — the widget reserves ~4 rows of the right margin and is\n" +
            "skipped when empty. It never steals input; P only binds inside\n" +
            "the Quest Log dialog.\n\n" +
            "TIPS\n" +
            "Pin the quest whose progress you actually want to track at a\n" +
            "glance — usually a Kill or Collect quest on the current floor.\n" +
            "Leave the auto-pin alone if you only ever keep one quest at a\n" +
            "time; unpin to reclaim the HUD space for a big-map session.\n\n" +
            "SEE ALSO\n" +
            "[Accepting & Completing Quests] · [Quest Types & Rewards] · [Town of Beginnings NPCs (F1)] · [Controls & Keybindings]")
        {
            Tags = new[] { "quests", "ui", "hud" }
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
            "[Divine Object Set — Integrity Knights] · [Divine Objects] · [Selka the Novice (F65)] · [Dorothy (F78)] · [Quest Types & Rewards]")
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
            "[The Sword's Awakening (Selka F65)] · [Divine Object Set — Integrity Knights] · [Dorothy (F78)] · [MD Alicization Canonical Extras] · [Unique Skill: Holy Sword]")
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

        new("Quests & NPCs", "Dorothy (F78)",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Dorothy (BrightCyan 'D' glyph)\n" +
            "│ Floor: 78\n" +
            "│ Quest: Purify the Darkness (22 kills)\n" +
            "│ Reward: Starlight Banner (Divine) + 700 Col + 550 XP\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "A Divine Object giver on Floor 78, canon Last Recollection\n" +
            "character. Dorothy hands over the Starlight Banner — the 8th\n" +
            "Divine Object and the only Divine Scythe in the game — in\n" +
            "exchange for purifying her floor.\n\n" +
            "USAGE\n" +
            "Bump the BrightCyan 'D' on F78. Accept \"Purify the Darkness\":\n" +
            "slay 22 monsters on Floor 78. Return to Dorothy complete.\n\n" +
            "EFFECTS\n" +
            "On turn-in:\n" +
            "  - Starlight Banner (Divine Scythe, HolyAoE+20, range 2)\n" +
            "  - +700 Col\n" +
            "  - +550 XP\n" +
            "Drop logs with a bespoke diamond (◈) Divine line to mark the\n" +
            "8th Divine Object placement. Auto-added to inventory; dropped\n" +
            "at your feet if full.\n\n" +
            "COSTS\n" +
            "22 F78 monster kills. Single-quest NPC — no chained follow-up\n" +
            "like Selka's awakening arc.\n\n" +
            "TIPS\n" +
            "Bring Scythe proficiency into the F78 push if you want to\n" +
            "wield Starlight Banner on receipt — its 2-range reach shines\n" +
            "on Scythe builds. Stack the 22 kills with any standing F78\n" +
            "weapon-gated Kill quests before turn-in; the counter overlaps.\n" +
            "Dorothy pairs neatly with Sister Azariya (F50) and Selka\n" +
            "(F65) to complete the NPC-quest Divine trio before F80.\n\n" +
            "SEE ALSO\n" +
            "[Divine Object Set — Integrity Knights] · [Divine Objects] · [SAO Last Recollection Weapons] · [Sister Azariya (F50)] · [Selka the Novice (F65)] · [Quest Types & Rewards]")
        {
            Tags = new[] { "quests", "npcs", "last-recollection" }
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
            "│ NPC: Agil, Klein, Argo, Kibaou, Lisbeth, Silica, +3\n" +
            "│ Floor: 1 (hub)\n" +
            "│ Quest: Tutorial + vendor + ALF recruiter\n" +
            "│ Reward: Dialogue, shop access, tips, guild join\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "F1's hand-built Town of Beginnings hosts fixed canon NPCs — the\n" +
            "core SAO cast plus supporting staff and the Aincrad Liberation\n" +
            "Force (ALF) recruiter. Anchors the tutorial flow, the early\n" +
            "recruitment pool, and the ALF guild onramp.\n\n" +
            "USAGE\n" +
            "Bump to trigger dialogue or a shop. Klein and Argo also wander\n" +
            "beyond F1 once the player climbs. Kibaou stays in the plaza as\n" +
            "ALF's permanent recruiter.\n\n" +
            "EFFECTS\n" +
            "  Agil            Vendor, \"Agil's General Store\"\n" +
            "  Klein           Tutorial dialogue (combat / progression /\n" +
            "                  survival)\n" +
            "  Argo the Rat    Information broker; tips on bosses,\n" +
            "                  proficiency, death\n" +
            "  Kibaou          ALF recruiter (see Aincrad Liberation Force)\n" +
            "  Priest Tadashi  Flavor\n" +
            "  Nezha           Smith; points to anvil\n" +
            "  Lisbeth         Short canon dialogue\n" +
            "  Silica          Short canon dialogue\n" +
            "  Diavel          Short canon dialogue\n\n" +
            "Klein also appears on F2-F3; Argo on F3+ as wandering NPCs.\n" +
            "The Monument of Swordsmen (BrightYellow M) stands in the south\n" +
            "plaza park — step on to review species kill milestones and\n" +
            "swap your Active Title.\n\n" +
            "COSTS\n" +
            "None for dialogue. Agil's shop prices follow standard vendor\n" +
            "markup (+20%).\n\n" +
            "TIPS\n" +
            "Talk to Argo before every new era — her tips rotate with your\n" +
            "current floor. Klein and the F1 Lisbeth townsfolk become\n" +
            "recruitable once bumped outside F1. Agil's shop offers the\n" +
            "Anneal Blade craft line. Kibaou is the lowest-gate guild\n" +
            "recruit in the game (ALF, Lv1, karma >=-30). NOTE: the F1\n" +
            "Lisbeth here is the flavor/recruit NPC — the F48 Lindarth\n" +
            "Lisbeth is a separate crafting NPC that gates the Rarity 6\n" +
            "craft line. If your karma has dropped to <=-50, Town Guards\n" +
            "spawn in this plaza on entry — raise karma first or bring a\n" +
            "fight.\n\n" +
            "SEE ALSO\n" +
            "[Aincrad Liberation Force (F1)] · [Town Guard (Outlaw Mode)] · [Guild System Overview] · [Anneal Blade Craft Line] · [Ran the Brawler (F2)] · [Lindarth Town (F48)] · [Starting Loadout] · [Monument of Swordsmen (F1)]")
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
            "[SAO Switch (Party)] · [Town of Beginnings NPCs (F1)] · [Guild System Overview] · [Run Modifiers (12 Optional Challenges)] · [Combo Attacks]")
        {
            Tags = new[] { "quests", "npcs", "combat" }
        },

        new("Quests & NPCs", "Guild System Overview",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Guild recruiter per faction\n" +
            "│ Floor: Varies (F1 to F75)\n" +
            "│ Quest: Recruitment + signature per guild\n" +
            "│ Reward: Passive perk, guild rep, quest line\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Eight canon guilds plus a Player-Founded option make up Aincrad's\n" +
            "social spine. You can hold ONE active guild at a time — leaving a\n" +
            "guild costs -3 karma and wipes that guild's passive perk. Each\n" +
            "guild has a level + karma gate, a recruiter NPC, a passive perk,\n" +
            "and two quests: a 10-kill recruitment and a canon-themed signature.\n\n" +
            "USAGE\n" +
            "Open StatsDialog, click \"View All Guilds\" to open the Guild\n" +
            "Roster. Travel to the guild's HQ floor, bump the recruiter, and\n" +
            "accept if you meet the level + karma gate. The single-active-\n" +
            "guild rule is enforced: join a new one and the old one drops you.\n\n" +
            "EFFECTS\n" +
            "Eight canon guilds (see individual entries for detail):\n" +
            "  KoB (F55)    Knights of the Blood Oath — Heathcliff\n" +
            "  ALF (F1)     Aincrad Liberation Force  — Kibaou\n" +
            "  DDA (F40)    Divine Dragon Alliance    — Lind\n" +
            "  Fuurinkazan  (F20)                     — Klein\n" +
            "  Legend Braves (F25)                    — Schmitt\n" +
            "  Sleeping Knights (F60)                 — Siune / Yuuki\n" +
            "  LC  (F75)    Laughing Coffin (hidden)  — PoH's Herald\n" +
            "  Moonlit Black Cats (F10)               — Keita\n\n" +
            "Plus Player-Founded Guild — 5000 Col to start your own.\n\n" +
            "COSTS\n" +
            "Leaving a guild: -3 karma. Player-Founded creation: 5000 Col.\n" +
            "No Col cost to join a canon guild — only the karma/level gate.\n\n" +
            "TIPS\n" +
            "Pick your alignment target before F10 — the Moonlit Black Cats\n" +
            "join window closes once you take fate-sealed damage on F27, and\n" +
            "LC karma requirements (<=-50) take deliberate grinding. Stack\n" +
            "signature-quest rewards with the guild's passive perk for the\n" +
            "biggest power spike per floor.\n\n" +
            "SEE ALSO\n" +
            "[Karma & Alignment] · [Knights of the Blood Oath (F55)] · [Aincrad Liberation Force (F1)] · [Laughing Coffin (F75 Hidden)] · [Player-Founded Guild]")
        {
            Tags = new[] { "guild", "quests", "npcs" }
        },

        new("Quests & NPCs", "Knights of the Blood Oath (F55)",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Heathcliff (leader), Godfree (recruiter)\n" +
            "│ Floor: 55 Granzam HQ\n" +
            "│ Quest: Recruitment + \"Defend the Frontline\"\n" +
            "│ Reward: +8 Defense + BlockChance via Vitality\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Knights of the Blood Oath (KoB) is Heathcliff's elite frontline\n" +
            "order headquartered at F55 Granzam. Steep gate — Level 25 with\n" +
            "karma >= +30 — but the passive is one of the strongest in the\n" +
            "canon roster.\n\n" +
            "USAGE\n" +
            "Climb to F55, find Granzam, bump Godfree to open the recruit\n" +
            "dialog. Decline the prompt if you haven't decided yet; Godfree\n" +
            "stays available while your gate is met.\n\n" +
            "EFFECTS\n" +
            "  Gate      Level 25, Karma >= +30\n" +
            "  Passive   +8 Defense + BlockChance scaling on Vitality\n" +
            "  Recruit   10 labyrinth mob kills (frontline-tagged)\n" +
            "  Signature \"Defend the Frontline\" — hold a KoB-coded\n" +
            "            labyrinth push\n\n" +
            "COSTS\n" +
            "The karma gate locks out anyone running the LC path. Leaving KoB\n" +
            "drops the Defense passive and costs -3 karma — don't flip guilds\n" +
            "casually once you're tanking on the +8.\n\n" +
            "TIPS\n" +
            "Vitality-heavy builds benefit the most — the BlockChance bonus\n" +
            "scales with VIT, so KoB compounds with a shield / 1H build. Pair\n" +
            "with DDA-style tank gear for the highest survivability ceiling.\n\n" +
            "SEE ALSO\n" +
            "[Guild System Overview] · [Divine Dragon Alliance (F40)] · [Defense — Block, Parry, Dodge] · [Karma & Alignment]")
        {
            Tags = new[] { "guild", "npcs", "quests" }
        },

        new("Quests & NPCs", "Aincrad Liberation Force (F1)",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Kibaou (leader + recruiter)\n" +
            "│ Floor: 1 Town of Beginnings plaza\n" +
            "│ Quest: Recruitment + \"Raid the Frontlines\"\n" +
            "│ Reward: +5% XP + 2 all stats\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Aincrad Liberation Force (ALF) is Kibaou's mass-recruitment guild\n" +
            "— the largest faction in the game, headquartered in the F1 TOB\n" +
            "plaza. Lowest gate of any canon guild: Level 1, karma >= -30.\n\n" +
            "USAGE\n" +
            "Bump Kibaou in the F1 plaza to open the recruit dialog. The\n" +
            "gate is loose enough that any early-floor build can sign up\n" +
            "on Day One.\n\n" +
            "EFFECTS\n" +
            "  Gate      Level 1, Karma >= -30\n" +
            "  Passive   +5% XP gain, +2 to all stats\n" +
            "  Recruit   10 low-floor mob kills\n" +
            "  Signature \"Raid the Frontlines\" — canon-themed push quest\n\n" +
            "COSTS\n" +
            "ALF's mass-recruitment identity means the passive is generalist\n" +
            "rather than specialist — other guilds hit harder in single\n" +
            "stats. Leaving costs -3 karma like any guild drop.\n\n" +
            "TIPS\n" +
            "ALF is the best fallback guild while you climb toward a gated\n" +
            "option (KoB +30, Sleeping Knights +50). The +5% XP rider\n" +
            "stacks cleanly with quest turn-in XP — use ALF as your\n" +
            "early-game engine, then swap once you hit the level thresholds.\n\n" +
            "SEE ALSO\n" +
            "[Guild System Overview] · [Town of Beginnings NPCs (F1)] · [Experience & Leveling] · [Karma & Alignment]")
        {
            Tags = new[] { "guild", "npcs", "quests" }
        },

        new("Quests & NPCs", "Fuurinkazan (F20)",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Klein (leader + recruiter)\n" +
            "│ Floor: 20 HQ\n" +
            "│ Quest: Recruitment + \"Blades of Friendship\"\n" +
            "│ Reward: +5% CritRate, +10 Attack with Katana\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Fuurinkazan is Klein's samurai-themed crew, headquartered on\n" +
            "Floor 20. The passive is the single best Katana buff in the\n" +
            "game — +10 flat Attack on every Katana swing on top of +5%\n" +
            "CritRate across all weapons.\n\n" +
            "USAGE\n" +
            "Reach F20, find the Fuurinkazan HQ, bump Klein to open the\n" +
            "recruit dialog. Any karma is acceptable — no alignment gate.\n\n" +
            "EFFECTS\n" +
            "  Gate      Level 10, any karma\n" +
            "  Passive   +5% CritRate + +10 Attack when wielding a Katana\n" +
            "  Recruit   10 mob kills (Katana-tagged theme)\n" +
            "  Signature \"Blades of Friendship\" — canon Klein-crew quest\n\n" +
            "COSTS\n" +
            "Non-Katana weapons still get the +5% crit — but the +10 Attack\n" +
            "rider is the real draw. If you're not committing to Katana the\n" +
            "guild is underutilized.\n\n" +
            "TIPS\n" +
            "Stack Fuurinkazan with Katana Mastery and a Klein-recruit ally\n" +
            "for a full samurai composition. Karakurenai (Klein's canon\n" +
            "Katana) hits hardest when its BackstabDmg+50 rider lands on\n" +
            "top of the Fuurinkazan +10.\n\n" +
            "SEE ALSO\n" +
            "[Guild System Overview] · [Unique Skill: Katana Mastery] · [Recruitable Allies & Party System] · [Critical Hits]")
        {
            Tags = new[] { "guild", "katana", "quests" }
        },

        new("Quests & NPCs", "Legend Braves (F25)",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Schmitt (recruiter)\n" +
            "│ Floor: 25 HQ\n" +
            "│ Quest: Recruitment + \"Hunt the Coffin\"\n" +
            "│ Reward: +5% Attack, +15 Atk vs LC-tagged mobs\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Legend Braves is the player-organized anti-Laughing Coffin\n" +
            "(LC) guild on Floor 25. Its perk directly counters LC — +15\n" +
            "flat Attack specifically against LC-tagged enemies on top of\n" +
            "a generalist +5% Attack.\n\n" +
            "USAGE\n" +
            "Climb to F25, find the Legend Braves HQ, bump Schmitt to\n" +
            "recruit. Karma >= 0 is the gate; you cannot be Shady or Outlaw\n" +
            "and wear the tabard.\n\n" +
            "EFFECTS\n" +
            "  Gate      Level 15, Karma >= 0\n" +
            "  Passive   +5% Attack + +15 Attack vs LC-flagged mobs\n" +
            "  Recruit   10 LC-theme mob kills\n" +
            "  Signature \"Hunt the Coffin\" — LC-themed push\n\n" +
            "COSTS\n" +
            "Mutually exclusive with LC itself (karma gates don't overlap).\n" +
            "Leaving is -3 karma, and Schmitt will not re-admit you without\n" +
            "a karma rebuild.\n\n" +
            "TIPS\n" +
            "Stack with the Laughing Coffin run modifier — modifier spawns\n" +
            "more LC mobs, and Legend Braves' +15 Attack rider applies to\n" +
            "every one of them. A canon synergy with a real damage gain.\n\n" +
            "SEE ALSO\n" +
            "[Guild System Overview] · [Laughing Coffin (F75 Hidden)] · [Run Modifiers (12 Optional Challenges)] · [Karma & Alignment]")
        {
            Tags = new[] { "guild", "quests", "npcs" }
        },

        new("Quests & NPCs", "Divine Dragon Alliance (F40)",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Lind (leader + recruiter)\n" +
            "│ Floor: 40 HQ\n" +
            "│ Quest: Recruitment + \"Drake Hunt\"\n" +
            "│ Reward: +10 Vitality, +5 Defense\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Divine Dragon Alliance (DDA) is Lind's rival-tank guild to KoB,\n" +
            "headquartered on Floor 40. The passive is the raw-tank package:\n" +
            "+10 VIT and +5 Defense on every equipped loadout.\n\n" +
            "USAGE\n" +
            "Ascend to F40, find the DDA HQ, bump Lind to recruit. Gate is\n" +
            "Level 15 and karma >= 0 — a middleweight gate between ALF\n" +
            "(easy) and KoB (strict).\n\n" +
            "EFFECTS\n" +
            "  Gate      Level 15, Karma >= 0\n" +
            "  Passive   +10 Vitality + +5 Defense\n" +
            "  Recruit   10 labyrinth-drake-tagged mob kills\n" +
            "  Signature \"Drake Hunt\" — canon dragon-themed push\n\n" +
            "COSTS\n" +
            "DDA and KoB are mutually exclusive active guilds — pick one\n" +
            "tank identity. Leaving is the standard -3 karma.\n\n" +
            "TIPS\n" +
            "Raw +10 VIT feeds HP, stamina, and (via BlockChance scaling on\n" +
            "Vitality) defense at the same time — DDA is the better pick\n" +
            "for pure survivability, while KoB edges ahead for shield-\n" +
            "focused builds.\n\n" +
            "SEE ALSO\n" +
            "[Guild System Overview] · [Knights of the Blood Oath (F55)] · [The Six Attributes] · [Defense — Block, Parry, Dodge]")
        {
            Tags = new[] { "guild", "quests", "npcs" }
        },

        new("Quests & NPCs", "Sleeping Knights (F60)",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Siune (recruiter), Yuuki (leader)\n" +
            "│ Floor: 60 HQ\n" +
            "│ Quest: Recruitment + \"The Moon's Rest\"\n" +
            "│ Reward: +3 all stats, +5% CritRate\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Sleeping Knights is Yuuki's late-game elite order on Floor 60.\n" +
            "The steepest positive-karma gate in the game (Level 50, karma\n" +
            ">= +50) but a clean all-stats passive plus crit.\n\n" +
            "USAGE\n" +
            "Reach F60, find the Sleeping Knights HQ, bump Siune to open\n" +
            "the recruit dialog. Karma floor (+50) means you're likely\n" +
            "already in Honorable tier by the time you qualify.\n\n" +
            "EFFECTS\n" +
            "  Gate      Level 50, Karma >= +50\n" +
            "  Passive   +3 to all stats + +5% CritRate\n" +
            "  Recruit   10 late-floor mob kills\n" +
            "  Signature \"The Moon's Rest\" — canon Yuuki-crew quest\n\n" +
            "COSTS\n" +
            "The karma + level gate locks this out of speedruns. Leaving is\n" +
            "-3 karma — but you've earned enough to survive the drop.\n\n" +
            "TIPS\n" +
            "Treat Sleeping Knights as the endgame honorable-path capstone\n" +
            "— swap in from ALF or KoB once you clear the +50 karma + L50\n" +
            "gate and ride the +3 all-stats through the F75 Heathcliff\n" +
            "fight.\n\n" +
            "SEE ALSO\n" +
            "[Guild System Overview] · [Karma & Alignment] · [Knights of the Blood Oath (F55)] · [Critical Hits]")
        {
            Tags = new[] { "guild", "quests", "npcs" }
        },

        new("Quests & NPCs", "Laughing Coffin (F75 Hidden)",
            "┌─ Quests & NPCs\n" +
            "│ NPC: PoH's Herald (recruiter)\n" +
            "│ Floor: 75 (hidden hideout)\n" +
            "│ Quest: Recruitment + \"Crimson Letter\"\n" +
            "│ Reward: +20% BackstabDmg, Town Guard hostility\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Laughing Coffin (LC) is PoH's PKer guild, hidden on Floor 75\n" +
            "and gated behind karma <= -50 (Outlaw tier). The signature\n" +
            "quest \"Crimson Letter\" demands 5 NPC kills — canon atrocity\n" +
            "as written.\n\n" +
            "USAGE\n" +
            "Drop to karma <= -50, climb to F75, find the hidden hideout,\n" +
            "bump PoH's Herald. The gate opens only while you're Outlaw —\n" +
            "bounce above -50 and the entrance closes again.\n\n" +
            "EFFECTS\n" +
            "  Gate      Level 30, Karma <= -50\n" +
            "  Passive   +20% BackstabDmg\n" +
            "  Recruit   10 PK-mob / LC-mob kills\n" +
            "  Signature \"Crimson Letter\" — 5 NPC kills\n" +
            "  Side      Town Guards hostile at F1 plaza while in LC\n\n" +
            "COSTS\n" +
            "Shops refuse service (karma tier). Town Guards aggro on F1\n" +
            "entry. Named NPC kills cost -20 karma each — the signature\n" +
            "quest alone will bottom your score.\n\n" +
            "TIPS\n" +
            "Farm Town Guards on F1 for +20 LC rep each — fastest rep\n" +
            "faucet in the game. Pair with the Laughing Coffin run modifier\n" +
            "for a themed, self-consistent PKer run. The BackstabDmg +20%\n" +
            "stacks multiplicatively with weapons like Karakurenai.\n\n" +
            "SEE ALSO\n" +
            "[Guild System Overview] · [Karma & Alignment] · [Town Guard (Outlaw Mode)] · [Run Modifiers (12 Optional Challenges)]")
        {
            Tags = new[] { "guild", "outlaw", "karma" }
        },

        new("Quests & NPCs", "Moonlit Black Cats (F10)",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Keita (leader + recruiter)\n" +
            "│ Floor: 10 HQ (quest turn-in stays F10)\n" +
            "│ Quest: Recruitment + \"One More Floor\"\n" +
            "│ Reward: +5 Vitality, +3 Defense (fate-sealed)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Moonlit Black Cats is Keita's tragic early-game guild on\n" +
            "Floor 10. The passive is modest but the canon fate is the\n" +
            "hook — the guild dissolves on F27 entry regardless of your\n" +
            "play, losing -5 karma along with the perk.\n\n" +
            "USAGE\n" +
            "Climb to F10, find the Moonlit Black Cats HQ, bump Keita.\n" +
            "The signature \"One More Floor\" auto-completes when you\n" +
            "step onto F25 — turn it in back at Keita on F10.\n\n" +
            "EFFECTS\n" +
            "  Gate      Level 5, Karma >= -20\n" +
            "  Passive   +5 Vitality + +3 Defense\n" +
            "  Recruit   10 F10-tier mob kills\n" +
            "  Signature \"One More Floor\" — auto-complete on F25 entry,\n" +
            "            turn in at Keita (F10)\n" +
            "  FATE      Guild dissolves on F27 entry (-5 karma, perk lost)\n\n" +
            "COSTS\n" +
            "The F27 fate is canonical and unavoidable. Plan your F27 ascend\n" +
            "knowing the perk drops and the karma dings — banked guild rep\n" +
            "does not carry into a replacement guild.\n\n" +
            "TIPS\n" +
            "Use Moonlit Black Cats as a known-expiry stepping stone between\n" +
            "F5 and F27 — collect the signature turn-in reward on F25 and\n" +
            "bank the XP / Col before the fate event fires. Line up the\n" +
            "replacement guild recruit (Fuurinkazan F20 is a good bridge)\n" +
            "before you step onto F27.\n\n" +
            "SEE ALSO\n" +
            "[Guild System Overview] · [Karma & Alignment] · [Fuurinkazan (F20)] · [Ascending a Floor]")
        {
            Tags = new[] { "guild", "npcs", "quests" }
        },

        new("Quests & NPCs", "Player-Founded Guild",
            "┌─ Quests & NPCs\n" +
            "│ NPC: N/A (created from StatsDialog)\n" +
            "│ Floor: Any\n" +
            "│ Quest: Pick name + perk preset\n" +
            "│ Reward: 1 of 5 passive perk presets\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Player-Founded Guild lets you skip the canon roster and roll\n" +
            "your own. Spend 5000 Col, pick a name, choose 1 of 5 perk\n" +
            "presets — your guild counts as your single active guild for\n" +
            "every downstream rule.\n\n" +
            "USAGE\n" +
            "Open StatsDialog, click \"View All Guilds\" to open the Guild\n" +
            "Roster, pick the Player-Founded option, pay 5000 Col, enter\n" +
            "a guild name, pick a perk preset. The guild is created in-\n" +
            "place and counts immediately.\n\n" +
            "EFFECTS\n" +
            "  Cost      5000 Col\n" +
            "  Naming    Freeform string\n" +
            "  Presets   1 of 5 (attack / tank / crit / XP / utility flavor)\n" +
            "  Rule      Counts as your single active guild — joining any\n" +
            "            canon guild drops the Player-Founded one\n\n" +
            "COSTS\n" +
            "5000 Col upfront. The perk presets are all weaker than the\n" +
            "strongest canon options (KoB, Sleeping Knights), so the\n" +
            "trade-off is identity + flexibility vs raw power.\n\n" +
            "TIPS\n" +
            "Good early-game option if your karma or level locks you out\n" +
            "of the canon guild you want — roll Player-Founded as a\n" +
            "stepping stone, then swap to the real one once you qualify.\n" +
            "Pick the preset that matches your build's weakest column\n" +
            "for the biggest marginal gain.\n\n" +
            "SEE ALSO\n" +
            "[Guild System Overview] · [Karma & Alignment] · [Aincrad Liberation Force (F1)] · [Col Economy — How You Earn]")
        {
            Tags = new[] { "guild", "economy", "quests" }
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
            "The Invest button (in ShopDialog) deposits Col per-vendor to\n" +
            "unlock bonus stock tiers beyond the global ShopTierSystem — see\n" +
            "Vendor Investing for thresholds and the \"Invested +N\" header\n" +
            "badge. Bargaining XP ticks +1 per buy/sell/Sell Junk action.\n\n" +
            "COSTS\n" +
            "All prices marked up +20% over base. Karma tier and Bargaining\n" +
            "milestones STACK MULTIPLICATIVELY on top:\n" +
            "  Honorable (+50..+100)  x0.90 final price\n" +
            "  Neutral / Shady        x1.00 / x1.10\n" +
            "  Outlaw   (-100..-50)   shops refuse service\n" +
            "  Bargaining L99         x0.85 on top (max -23.5% combined)\n\n" +
            "TIPS\n" +
            "Stock Revive Crystals and Escape Ropes before entering a\n" +
            "Labyrinth — they're cheaper on the floor you find them than\n" +
            "carrying them from F1. Accessories at F5+ are random per run,\n" +
            "so revisit shops if you're hunting a specific slot. Push karma\n" +
            "to Honorable AND grind Bargaining to L99 for the compounded\n" +
            "-23.5% price cut before a big shop run.\n\n" +
            "SEE ALSO\n" +
            "[Vendor Investing] · [Bargaining (Life Skill)] · [Col Economy — How You Earn] · [Karma & Alignment] · [Potions, Crystals & Throwables] · [Accessories]")
        {
            Tags = new[] { "npcs", "shops", "economy" }
        },

        new("Quests & NPCs", "Vendor Investing",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Any Vendor (green 'V')\n" +
            "│ Floor: 2+ (any vendor shop)\n" +
            "│ Quest: Deposit Col to boost that vendor's stock tier\n" +
            "│ Reward: +1 / +2 / +3 bonus stock tiers, per-vendor\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Per-vendor Col deposits that layer on top of the global\n" +
            "ShopTierSystem. Each vendor tracks its OWN cumulative investment\n" +
            "keyed by ShopName — a boost at Lindarth's Elite Outfitters\n" +
            "doesn't carry over to F5 General Store. Cap is 20,000 Col\n" +
            "invested per vendor (+3 tiers).\n\n" +
            "USAGE\n" +
            "Open ShopDialog → click \"Invest\" → MessageBox picker: 500 /\n" +
            "1,000 / 5,000 / 20,000 / Cancel. The deposit is clamped to\n" +
            "your Col on hand and to the 20,000-per-vendor cap. Header\n" +
            "badge \"Invested +N\" appears once the vendor has earned any\n" +
            "bonus tiers. Persists in SaveData.VendorInvestments.\n\n" +
            "EFFECTS\n" +
            "  CUMULATIVE COL   BONUS TIERS   RESULT\n" +
            "      1,000 Col    +1 tier       Next unlocked tier added\n" +
            "      5,000 Col    +2 tiers      Two tiers added\n" +
            "     20,000 Col    +3 tiers      Three tiers (CAP)\n\n" +
            "Bonus tiers pull from ShopTierSystem tiers that the RUN hasn't\n" +
            "globally unlocked yet (floors the player hasn't cleared bosses\n" +
            "on). If every tier is already globally unlocked, investment\n" +
            "produces no visible stock — the Col is effectively wasted,\n" +
            "which the UI flags in the Invest dialog. Extra stock is marked\n" +
            "up +20% on top of the base Value.\n\n" +
            "COSTS\n" +
            "Col. Investments do NOT refund — once deposited, the Col is\n" +
            "converted to permanent per-vendor tier progress. Dies with the\n" +
            "save on permadeath like any other Col sink.\n\n" +
            "TIPS\n" +
            "Front-load the 1,000 Col threshold at a vendor you'll actually\n" +
            "revisit — the +1 tier is a 10x return if the tier unlocks a\n" +
            "weapon you need. Don't chase 20,000 unless you're post-F50 and\n" +
            "the global ShopTierSystem has stalled. Bargaining L99 stacks on\n" +
            "deposits — grind Bargaining first for a -15% invest cost.\n\n" +
            "SEE ALSO\n" +
            "[Vendors — Rotating Stock] · [Dynamic Shop Tiering (F50+)] · [Bargaining (Life Skill)] · [Col Economy — How You Earn] · [Save System]")
        {
            Tags = new[] { "npcs", "shops", "investing" }
        },

        new("Quests & NPCs", "Col Economy — How You Earn",
            "┌─ Quests & NPCs\n" +
            "│ NPC: N/A (systemic)\n" +
            "│ Floor: All\n" +
            "│ Quest: Col source reference\n" +
            "│ Reward: Col faucets; save wipes on death\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Col is earned from mobs, chests, speed and exploration bonuses,\n" +
            "quests, and achievements. Death deletes the entire save slot,\n" +
            "so every Col you bank is backed by the run staying alive.\n\n" +
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
            "DEATH: the save file is deleted. Every Col carried is forfeit\n" +
            "along with the rest of the run.\n\n" +
            "TIPS\n" +
            "Perfect Kill streaks stack on mob yield — don't break cover if\n" +
            "you're farming a floor. Sink Col into Anvil enhance/refine\n" +
            "before a risky push; the gear still dies with the save, but\n" +
            "invested Col beats banked Col on a speed-clear floor where\n" +
            "every swing counts.\n\n" +
            "SEE ALSO\n" +
            "[Kill Streaks] · [Achievements] · [Ascending a Floor] · [Permadeath & Save Deletion]")
        {
            Tags = new[] { "economy", "progression", "permadeath" }
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
            "[Kill Streaks] · [Status: Bleed & Poison] · [Lore, Journals & Enchant Shrines] · [Col Economy — How You Earn] · [Damage & Toast Feedback]")
        {
            Tags = new[] { "economy", "progression", "xp" }
        },

        new("Quests & NPCs", "Pause Menu (Esc)",
            "┌─ Quests & NPCs\n" +
            "│ NPC: N/A (system)\n" +
            "│ Floor: All (map view only)\n" +
            "│ Quest: Mid-run system menu\n" +
            "│ Reward: Save / Load / Options / Exit with confirmations\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Esc on the map view opens a four-option Pause Menu: Save game,\n" +
            "Load game, Options, Exit game. Each action asks for a yes/no\n" +
            "confirmation before it runs. Esc again closes the menu and\n" +
            "returns you to the same turn.\n\n" +
            "USAGE\n" +
            "Press Esc in the map view to open. Press Esc inside the menu\n" +
            "to close it. Pick an option with arrow keys + Enter, or click.\n" +
            "Esc closes other dialogs (Player Guide, Stats, Inventory) as\n" +
            "normal — it only opens the Pause Menu from the map itself.\n\n" +
            "EFFECTS\n" +
            "  Save game   Confirmation prompt, then writes the current slot\n" +
            "  Load game   Confirmation prompt, then reloads the last save\n" +
            "  Options     Opens the existing OptionsScreen\n" +
            "  Exit game   Confirmation prompt, then quits the app\n\n" +
            "F5 quick-save still works from the map and bypasses the menu\n" +
            "with no confirmation prompt.\n\n" +
            "COSTS\n" +
            "No in-game time passes while the menu is open — this is a true\n" +
            "pause, not a turn-consuming action.\n\n" +
            "TIPS\n" +
            "Use F5 for fast checkpoints mid-floor, and the Pause Menu's\n" +
            "Save option when you want the confirmation safety net before\n" +
            "a boss pull. Load game from the Pause Menu is the fastest\n" +
            "roll-back when you mispicked a Passive Talent on level-up\n" +
            "and haven't saved since.\n\n" +
            "SEE ALSO\n" +
            "[Controls & Keybindings] · [Save System] · [Permadeath & Save Deletion] · [Passive Talents (Level-Up Perks)] · [Categorized Combat Log]")
        {
            Tags = new[] { "save", "pause-menu", "controls" }
        },

        new("Quests & NPCs", "Categorized Combat Log",
            "┌─ Quests & NPCs\n" +
            "│ Topic: Categorized Combat Log\n" +
            "│ Tabs: All · Combat · System · Item · Dialog\n" +
            "│ Cycle: Tab key (when log focused)\n" +
            "│ History: 500-entry ring buffer (preserved across turns)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "The combat log splits its feed into five filtered tabs so you\n" +
            "can read just the slice you care about. Color per category keeps\n" +
            "the All view scannable, and a 500-entry ring buffer preserves\n" +
            "recent history across turns, floor changes, and dialog opens.\n\n" +
            "USAGE\n" +
            "Focus the log pane, then press Tab to cycle forward through the\n" +
            "tabs. Focus returns to the map with Tab or Esc. PageUp/PageDown\n" +
            "scroll history within the active tab.\n\n" +
            "EFFECTS\n" +
            "TAB CONTENTS:\n" +
            "  All      Every message, uncategorized, original colors\n" +
            "  Combat   Hits, crits, dodges, parries, status applications\n" +
            "  System   Saves, level-ups, floor changes, game state events\n" +
            "  Item     Pickups, drops, vendor trades, crafting, equipment\n" +
            "  Dialog   NPC barks, boss lines, quest text, scripted scenes\n" +
            "COLOR KEY (same hue in All as in its home tab):\n" +
            "  Combat   red/yellow damage tones\n" +
            "  System   muted gray/gold for meta events\n" +
            "  Item     cyan/green loot tones\n" +
            "  Dialog   magenta for character voice\n" +
            "HISTORY BUFFER:\n" +
            "  Ring-buffer of the most recent 500 entries; older entries drop\n" +
            "  off silently so the scroll stays responsive even on long runs.\n\n" +
            "COSTS\n" +
            "No gameplay impact — the log never swallows turns, and category\n" +
            "filtering is pure presentation. Dialogs still pause the game as\n" +
            "normal when opened.\n\n" +
            "TIPS\n" +
            "Flip to Combat when auditing a boss fight and to Item after\n" +
            "clearing a vault — the noise-to-signal ratio is dramatically\n" +
            "better than the All view. Dialog tab doubles as a quick recap\n" +
            "of the last NPC you talked to if you missed a quest hook.\n\n" +
            "SEE ALSO\n" +
            "[Damage & Toast Feedback] · [Damage Breakdown Format] · [Controls & Keybindings] · [Quick-Use Slots (1-5)] · [Achievements]")
        {
            Tags = new[] { "ui", "log", "controls" }
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
            "save auto-migration path. Every death deletes the active save.\n\n" +
            "USAGE\n" +
            "Files live in %LOCALAPPDATA%/AincradTRPG/save_N.json. Legacy\n" +
            "save.json auto-migrates to slot 1. Auto-save fires on every\n" +
            "floor ascend; F5 is a no-prompt quick-save, and the Esc Pause\n" +
            "Menu offers Save / Load / Options / Exit with confirmations.\n\n" +
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
            "  - Faction reputation (10-faction enum incl. guild + player-\n" +
            "    founded sentinels; legacy saves auto-migrate)\n" +
            "  - Karma score + active guild membership\n" +
            "  - Defeated field bosses\n" +
            "  - ACTIVE RUN MODIFIERS\n" +
            "  - Equipped skills + cooldowns\n" +
            "  - Story flags / events\n\n" +
            "Slot summary shows name, level, floor, difficulty, timestamp,\n" +
            "and playtime.\n\n" +
            "COSTS\n" +
            "Every save is permadeath — on death the slot file is deleted\n" +
            "from disk before the DeathScreen's confirmation prompt returns\n" +
            "you to the main menu.\n\n" +
            "TIPS\n" +
            "Back up %LOCALAPPDATA%/AincradTRPG manually before a big push\n" +
            "if you want insurance against the permadeath wipe. The auto-\n" +
            "save on ascend is overwrite-in-place, so you can't roll back a\n" +
            "bad ascend — F5 quick-save before boss pulls instead.\n\n" +
            "SEE ALSO\n" +
            "[Controls & Keybindings] · [Permadeath & Save Deletion] · [Pause Menu (Esc)] · [Run Modifiers (12 Optional Challenges)] · [Ascending a Floor] · [Achievements]")
        {
            Tags = new[] { "save", "permadeath", "progression" }
        },

        new("Quests & NPCs", "Controls & Keybindings",
            "┌─ Quests & NPCs\n" +
            "│ NPC: N/A (system)\n" +
            "│ Floor: All\n" +
            "│ Quest: In-game keyboard reference\n" +
            "│ Reward: One screen, every binding\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Every in-game key in one place. The map view is modal — dialogs\n" +
            "swallow keys while open, so these bindings assume the map has\n" +
            "focus unless noted. The title screen has no rotating hints;\n" +
            "this topic is the single source of truth for controls.\n\n" +
            "USAGE\n" +
            "Open this page any time with B. Hold Shift + dir to sprint,\n" +
            "Ctrl + dir to stealth-move. Esc closes any open dialog; Esc\n" +
            "from the map itself opens the Pause Menu.\n\n" +
            "EFFECTS\n" +
            "MOVEMENT\n" +
            "  W A S D / Arrows  Step one tile (cardinal)\n" +
            "  Q E Z C           Step one tile (diagonals)\n" +
            "  Shift + dir       Sprint 2 tiles (+50% satiety drain)\n" +
            "  Ctrl + dir        Stealth Move 1 tile (halves aggro)\n" +
            "  Space             Wait / skip turn\n\n" +
            "ACTIONS\n" +
            "  G                 Pick up item on your tile\n" +
            "  L                 Enter Look Mode\n" +
            "  V                 Counter Stance (forces next Parry)\n" +
            "  R                 Rest (regen, advances turns)\n" +
            "  X                 Auto-explore\n" +
            "  F                 Open Sword Skill menu\n" +
            "  F1-F4             Fire equipped skills 1-4\n" +
            "  1-5               Quick-Use consumable slots\n\n" +
            "UI\n" +
            "  I                 Inventory\n" +
            "  T                 Equipment\n" +
            "  P                 Player stats\n" +
            "  J                 Quest log\n" +
            "  K                 Kill stats\n" +
            "  Y                 Bestiary (monster compendium)\n" +
            "  B                 Player Guide (this dialog)\n" +
            "  H                 Quick help overlay (scrollable —\n" +
            "                    arrows / PgUp / PgDn navigate sections;\n" +
            "                    new sections cover Mining, Tool slot,\n" +
            "                    and Life Skills)\n" +
            "  P (in Quest Log)  Pin / unpin the selected quest\n" +
            "  Shift+S           Toggle status-tray verbose labels\n" +
            "  PageUp / PageDown Scroll the combat log\n" +
            "  Tab               Cycle log category tabs (log focused)\n\n" +
            "SYSTEM\n" +
            "  F5                Quick-save (no prompt)\n" +
            "  Esc               Close dialog — or open Pause Menu from map\n\n" +
            "TITLE SCREEN\n" +
            "  Arrows / Enter    Navigate + pick (Continue / New Game /\n" +
            "                    Records / Options / Exit)\n" +
            "  Esc               Quit with confirmation\n\n" +
            "COSTS\n" +
            "Sprint, Stealth, and every action key consume a turn in the\n" +
            "same way a normal step does. UI keys (I, T, P, J, K, B, H) and\n" +
            "the Pause Menu pause the game — no turns pass while they are\n" +
            "open.\n\n" +
            "TIPS\n" +
            "Bind your brain to the shape, not the letters — movement is\n" +
            "the WASD + QEZC 3x3, utilities cluster on the left home row\n" +
            "(G, R, X, F), and data panels are on the right (I, J, K, T).\n" +
            "F5 before any risky pull; the Pause Menu's Load is your only\n" +
            "roll-back if you haven't saved since.\n\n" +
            "SEE ALSO\n" +
            "[Pause Menu (Esc)] · [Save System] · [Sprint & Stealth Move] · [Look Mode & Counter Stance] · [Quick-Use Slots (1-5)] · [Quickbar & Consumables] · [Sword Skills — Unlock & Use] · [Bestiary — Monster Compendium] · [Damage & Toast Feedback] · [Gear Compare] · [Combat Visual Feedback] · [Damage Breakdown Format] · [Ambient World Animation] · [Categorized Combat Log] · [Quest Tracker] · [Status Icon Tray] · [Particle Effects] · [Damage Type Tags]")
        {
            Tags = new[] { "controls", "keybindings", "ui" }
        },

        new("Items", "Divine Weapons — Roster & Acquisition",
            "┌─ Items\n" +
            "│ Topic: Divine Weapons — Roster & Acquisition\n" +
            "│ Tier: Divine (17 total — peak rarity)\n" +
            "│ Cap: One Divine per run (hard lock)\n" +
            "│ Sources: Floor bosses · Quests · Hidden vault · T4 craft\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Seventeen Divine weapons span the 13 weapon classes. Every\n" +
            "Divine is hand-placed — never rolled on a random chest. Across\n" +
            "a single run you may obtain AT MOST one Divine; after the first\n" +
            "enters inventory, remaining Divine drops substitute to a banded\n" +
            "Legendary fallback until next run. This cap preserves the\n" +
            "\"chosen blade\" flavor and stops late-run Divine stacking.\n\n" +
            "USAGE\n" +
            "Clear a listed floor boss, complete the named quest, find the\n" +
            "hidden Conflagrant Vault, or finish the T4 evolution craft.\n" +
            "Divines equip like any weapon — unbreakable, no durability\n" +
            "tick, no Anvil repair cost. The drop logs a BrightRed line\n" +
            "with a diamond glyph and triggers the Divine Obtain Banner.\n\n" +
            "EFFECTS\n" +
            "FLOOR-BOSS GUARANTEED DROPS (10 Divines, F75-F98):\n" +
            "  F75  Masamune              Skull Reaper\n" +
            "  F82  Hexagramme             Legacy of Grand\n" +
            "  F84  Caladbolg              Queen of Ant\n" +
            "  F86  Tyrfing                King of Skeleton\n" +
            "  F87  Iron Maiden Dagger     Radiance Eater\n" +
            "  F88  Ouroboros              Rebellious Eyes\n" +
            "  F91  Mjolnir                Seraphiel the Fallen\n" +
            "  F93  Ascalon                Ragnarok the Final Beast\n" +
            "  F97  Time Piercing Sword    Cardinal the System Error\n" +
            "  F98  Black Lily Sword       Incarnation of the Radius\n" +
            "PRE-EXISTING FLOOR-BOSS DROPS (2):\n" +
            "  F20  Blue Rose Sword        Absolut the Winter Monarch\n" +
            "  F99  Night Sky Sword        Heathcliff's Shadow\n" +
            "QUEST REWARDS (4):\n" +
            "  F50  Heaven-Piercing Blade  Sister Azariya quest\n" +
            "  F65  Fragrant Olive Sword   Selka the Novice quest\n" +
            "  F78  Starlight Banner       Dorothy quest\n" +
            "  F89  Satanachia             Scholar Vesper quest\n" +
            "HIDDEN VAULT (1):\n" +
            "  F77-F79  Conflagrant Flame Bow  Conflagrant Vault prefab\n" +
            "         (Volcanic/Ruins/Dark biomes, once-per-game)\n\n" +
            "COSTS\n" +
            "The one-per-run cap is absolute. A Divine entering inventory\n" +
            "(pickup, quest turn-in, vault chest, T4 craft) sets a run-wide\n" +
            "flag that survives save/load. Subsequent floor bosses with a\n" +
            "Divine in their guaranteed slot drop a banded Legendary of the\n" +
            "appropriate tier instead. The flag resets on new run only.\n\n" +
            "TIPS\n" +
            "Plan which Divine you want BEFORE F75 — you only get one.\n" +
            "Canon-minded Katana mains sprint for F75 Masamune; 2H mains\n" +
            "hold for F93 Ascalon's dragon-slayer flavor; rapier duelists\n" +
            "can grab Hexagramme at F82 or hold for F50 Heaven-Piercing\n" +
            "Blade via the Sister Azariya chain. The Conflagrant Vault is\n" +
            "the only bow Divine outside T4 craft — prioritize if Archery\n" +
            "is your main weapon track. F98 Black Lily Sword and F97 Time\n" +
            "Piercing Sword are the mythic-tier pair; skip them only if\n" +
            "you want a specific thematic Divine instead. Sister Selka on\n" +
            "F65 can awaken Divines up to Lv3 for +45% base damage — see\n" +
            "entry 'Divine Awakening'.\n\n" +
            "SEE ALSO\n" +
            "[Divine Objects] · [Divine Object Set — Integrity Knights] · [Named Legendary Highlights] · [Floor Boss Roster — Canon Highlights] · [Weapon Evolution Chains] · [Rarity Tiers & Drop Rates] · [Divine Awakening]")
        {
            Tags = new[] { "items", "weapon", "divine", "rarity", "endgame" }
        },

        new("Items", "Divine Awakening",
            "┌─ Items\n" +
            "│ Topic: Divine Awakening\n" +
            "│ Applies to: Any Divine-rarity weapon\n" +
            "│ Levels: ◈1 / ◈2 / ◈3 (+15% / +30% / +45% base damage)\n" +
            "│ NPC: Sister Selka the Novice (F65)\n" +
            "│ Materials: Mithril Ingot · Divine Fragment · Primordial Shard\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Once you hold a Divine weapon, Sister Selka on F65 can awaken it\n" +
            "up to three times. Each awakening adds a flat base-damage bonus\n" +
            "(+15%, +30%, +45% cumulative) that stacks ADDITIVELY with\n" +
            "Refinement sockets and Enhancement levels — all three systems\n" +
            "fold into the weapon's Attack contribution separately. Awakening\n" +
            "is forward-only and persists across save/load.\n\n" +
            "USAGE\n" +
            "Carry a Divine weapon and the required materials to Sister\n" +
            "Selka on F65. Talk to her; her dialogue now offers an extra\n" +
            "option — \"Awaken a Divine weapon.\" Select the option, pick\n" +
            "the Divine from the preview pane (usually one, since the run\n" +
            "cap allows a single Divine), review the material cost, and\n" +
            "confirm. The banner fires a golden awakening variant on success.\n\n" +
            "EFFECTS\n" +
            "  ◈1  +15% base damage (flat, added to Attack)\n" +
            "  ◈2  +30% base damage (flat, added to Attack)\n" +
            "  ◈3  +45% base damage (flat, added to Attack)\n" +
            "Stacks additively with Refinement (e.g. a fully Refined +10\n" +
            "weapon reaching +100% Attack from sockets; Awakening Lv3 then\n" +
            "adds another +45% of base damage on top via Bonuses.Attack).\n" +
            "The awakening bonus flows through Player.Attack alongside\n" +
            "Strength, Enhancement, and Refinement — all additive.\n\n" +
            "COSTS\n" +
            "  Lv0→Lv1  3× Mithril Ingot       (mithril_ingot)\n" +
            "  Lv1→Lv2  1× Divine Fragment      (divine_fragment)\n" +
            "  Lv2→Lv3  1× Primordial Shard     (primordial_shard)\n" +
            "Divine Fragment drops ~5% from F75+ canon floor bosses (Skull\n" +
            "Reaper, Ghastlygaze, Legacy of Grand, and the rest of the\n" +
            "F75-F99 lineup). Primordial Shard drops one-per-run, guaranteed,\n" +
            "from clearing the F100 throne fight (Your Shadow — The Final\n" +
            "Trial). You cannot obtain two Shards in the same run.\n\n" +
            "TIPS\n" +
            "Lv3 is a post-F100 state by design — the Primordial Shard only\n" +
            "drops from the throne clear, so the third awakening is a\n" +
            "victory-lap upgrade unless you reload from a pre-clear save.\n" +
            "Plan Lv2 mid-run: if a Divine enters inventory at F75, you have\n" +
            "the entire F75-F99 climb to farm a Divine Fragment (~0.8 expected\n" +
            "drops across 16 canon bosses at 5%).\n" +
            "Awakened weapon in the MAIN hand applies the full bonus. In the\n" +
            "OFFHAND slot, the bonus does NOT apply — offhand damage reads\n" +
            "the weapon's raw BaseDamage directly (the same limitation that\n" +
            "applies to Refinement today). Keep your awakened Divine in the\n" +
            "main hand.\n" +
            "The ◈N suffix on the weapon's inventory name (e.g. \"Night Sky\n" +
            "Sword ◈2\") tells you the current awakening level at a glance.\n\n" +
            "SEE ALSO\n" +
            "[Divine Weapons — Roster & Acquisition] · [Weapon Refinement System] · [Refinement Ingots] · [Enhancement Ores System] · [Floor Boss Roster — Canon Highlights] · [Advanced Weapon Effects]")
        {
            Tags = new[] { "items", "divine", "awakening", "endgame", "selka" }
        },

        new("Items", "Shield Special Effects",
            "┌─ Items\n" +
            "│ Topic: Shield Special Effects\n" +
            "│ Slot: OffHand (ArmorSlot=Shield)\n" +
            "│ Shields: Nox Fermat · Rosso Aegis · Yasha Kavacha · Gaou Tatari\n" +
            "│ Effect field: SpecialEffect (shared with weapons)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Four endgame shields carry live SpecialEffect strings matching\n" +
            "the same tag grammar weapons use. Previously flavor-only, the\n" +
            "SpecialEffect field now lives on EquipmentBase — both weapons\n" +
            "and shields parse it through the shared reader, so a shield in\n" +
            "the OffHand adds its tag on top of the main-hand weapon's tag.\n\n" +
            "USAGE\n" +
            "Equip as OffHand (shields go to the off-hand slot, not the\n" +
            "weapon slot). The effect applies automatically while equipped.\n" +
            "Each shield's tag sums with any matching weapon tag for\n" +
            "additive effects; cap-style effects (CritImmune) take the max\n" +
            "of the two sources rather than stacking.\n\n" +
            "EFFECTS\n" +
            "  Nox Fermat      DamageReflect+5  Returns 5% of incoming damage\n" +
            "                                   to the attacker as flat dmg;\n" +
            "                                   minimum 1 per reflected hit.\n" +
            "  Rosso Aegis     CritImmune+5     5% chance per incoming hit to\n" +
            "                                   downgrade a monster crit to a\n" +
            "                                   normal hit (no CritHitDmg).\n" +
            "  Yasha Kavacha   HPRegen+3        +3 HP on every passive-regen\n" +
            "                                   tick; sums with MH HPRegen.\n" +
            "  Gaou Tatari     Barrier+10       Refreshing 10-HP absorb buffer,\n" +
            "                                   recharges once per floor ascend,\n" +
            "                                   absorbs before HP takes damage.\n\n" +
            "STACKING RULES:\n" +
            "  Additive (sum MH + OH): HPRegen · Barrier · DamageReflect\n" +
            "  Cap-style (max MH vs OH): CritImmune\n\n" +
            "COSTS\n" +
            "Shield-slot opportunity cost — you forgo a dual-wield OffHand\n" +
            "weapon (no +10% Pair Resonance damage, no offhand swing\n" +
            "multiplier). Block rolls still consume shield durability on\n" +
            "successful blocks; Divine gear is unbreakable but these four\n" +
            "shields are not Divine-tier.\n\n" +
            "TIPS\n" +
            "Pair Yasha Kavacha (HPRegen+3) with a weapon that also carries\n" +
            "HPRegen+N to double-dip the regen tick — HPRegen is additive.\n" +
            "Gaou Tatari's per-floor Barrier refresh rewards short pulls\n" +
            "between floor ascents; its 10-HP absorb is tiny per-hit but\n" +
            "compounding across a boss pull. Nox Fermat's reflect punishes\n" +
            "machine-gun attackers (lots of small hits) harder than one\n" +
            "big-swing boss — read the monster's hit cadence first.\n\n" +
            "SEE ALSO\n" +
            "[Advanced Weapon Effects] · [Defense — Block, Parry, Dodge] · [Equipment Slots & Dual Wield] · [Damage Mitigation] · [Named Legendary Highlights]")
        {
            Tags = new[] { "items", "armor", "shield", "special-effect" }
        },

        new("Items", "Pair Resonance — Mechanics Clarified",
            "┌─ Items\n" +
            "│ Topic: Pair Resonance — Mechanics Clarified\n" +
            "│ Trigger: Canonical MH+OH pair equipped\n" +
            "│ Damage: +10% on BOTH main-hand and offhand swings\n" +
            "│ Crit: +5% re-roll, FIRST hit per encounter only\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Pair Resonance is the synergy bonus that fires when you equip\n" +
            "a canonical dual-wield pair (e.g. Elucidator + Dark Repulser).\n" +
            "Two effects stack together: a persistent +10% damage rider on\n" +
            "every swing of both hands, and a one-shot 5% crit re-roll on\n" +
            "the FIRST hit of each new encounter. The crit re-roll used to\n" +
            "fire on every swing, which turned resonance into a raw +5%\n" +
            "crit buff — it is now capped to a \"sync burst\" on first hit\n" +
            "only, matching canon FD resonance flavor.\n\n" +
            "USAGE\n" +
            "Fill MH and OH slots with a matched canonical pair (see the\n" +
            "Paired Dual-Wield Weapons topic for the 3 canon pairs). On\n" +
            "first swing against a new monster, the log prints '◆ Pair\n" +
            "Resonance! ... sing together' ONCE per target. The damage\n" +
            "bonus continues silently on every subsequent swing.\n\n" +
            "EFFECTS\n" +
            "DAMAGE BONUS (persistent, every swing):\n" +
            "  Main-hand damage  *= 1.1\n" +
            "  Offhand damage    *= 1.1\n" +
            "CRIT RE-ROLL (FIRST-HIT ONLY per encounter):\n" +
            "  If MH did NOT crit on the opening swing against a new\n" +
            "  monster, roll d100 < 5 to convert the hit to a crit. Gate\n" +
            "  shares the same set that gates the banner log, so the\n" +
            "  re-roll and the banner fire once per target together.\n" +
            "BUNDLE 8 FIX: previously the 5% re-roll was unchecked and\n" +
            "fired every hit, effectively adding a permanent +5% crit rate\n" +
            "on top of the +10% damage mult — compounding crit damage with\n" +
            "the damage mult on lucky swings. The fix gates the re-roll\n" +
            "behind the existing per-encounter banner set; the damage\n" +
            "multiplier stays unchanged on all hits.\n\n" +
            "BUNDLE 10 — INDEPENDENT OFFHAND CRIT:\n" +
            "Off-hand swings from a paired weapon now roll critical hits\n" +
            "INDEPENDENTLY from the main-hand swing on the same turn. Both\n" +
            "rolling crit displays a CRITICAL! tag on each swing line in\n" +
            "the combat log, and the burst damage shows up immediately —\n" +
            "previously the offhand piggybacked on the main-hand crit\n" +
            "result, capping pair-burst ceiling. Independent rolls mean a\n" +
            "high-Dex Elucidator/Dark Repulser build can land double-CRIT\n" +
            "openers; expect occasional triple-digit single-turn bursts.\n\n" +
            "COSTS\n" +
            "The canonical pair pool is narrow — only 3 pairs exist\n" +
            "(Elucidator/Dark Repulser, Elucidator Rouge/Flare Pulsar,\n" +
            "Black Iron Dual A/B). Mixing halves across pairs forfeits the\n" +
            "synergy — the game matches by exact DefId pair lookup.\n\n" +
            "TIPS\n" +
            "Open every pull with a heavy-hitter sword skill — that first\n" +
            "swing is now the single crit-re-roll window. On long boss\n" +
            "fights, the +10% damage rider still carries the majority of\n" +
            "the resonance payoff; the crit burst is opening flavor, not\n" +
            "the main economic driver. If you were relying on the old\n" +
            "every-hit re-roll for crit uptime, swap to a weapon with flat\n" +
            "CritRate (Lambent Light, Masamune) or invest Dex.\n\n" +
            "SEE ALSO\n" +
            "[Paired Dual-Wield Weapons] · [Equipment Slots & Dual Wield] · [Critical Hits] · [Unique Skill: Dual Blades]")
        {
            Tags = new[] { "items", "dual-wield", "pair-resonance", "combat" }
        },

        new("Items", "Legendary Distribution (F75-F99)",
            "┌─ Items\n" +
            "│ Topic: Legendary Distribution (F75-F99)\n" +
            "│ Peak band: F94-F95 (~23 entries per pool)\n" +
            "│ Bundle 11 cap: peak ≤25 per floor (down from 62 at F90)\n" +
            "│ Edge bands lifted: F75-F79 + F96-F99\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Legendary-tier loot registration clusters by floor band in the\n" +
            "loot generator. Pre-Bundle 11, floors F88-F90 each carried\n" +
            "~62-69 Legendary candidates per roll. The Bundle 11 sweep\n" +
            "redistributed every Legendary across F1-F99 (see the new\n" +
            "Legendary Redistribution Overview topic for the full picture);\n" +
            "this section covers the F75-F99 endgame slice specifically.\n\n" +
            "USAGE\n" +
            "Nothing to do — the redistribution happens at chest-roll time.\n" +
            "Endgame chests now carry a flatter per-floor roster: F75-F79\n" +
            "is no longer a thin band, and F94-F95 caps at ~23 entries\n" +
            "instead of the old 62-spike at F90.\n\n" +
            "EFFECTS\n" +
            "BUNDLE 11 ENDGAME MOVES (not exhaustive):\n" +
            "  Fire-themed Relics/LS (Cinder Bow, Blazing Sword, Muramasa\n" +
            "  Demon Blade) shifted down to F65-F79, matching Volcanic ramp.\n" +
            "  Ice/cursed Relics (Snowsunder, Loveblight Bow) shifted to\n" +
            "  F66-F86, matching ice-plain and dark-cursed flavor.\n" +
            "  Mythic-finisher Relics (Dragoncrest, Deathbringer, Frostpeak)\n" +
            "  shifted up to F84-F94.\n" +
            "  AL Extreme Raid mythics (Demonslayer, Graceful Needle)\n" +
            "  shifted up to F82-F92.\n" +
            "  Black Iron Dual A/B (Underworld Kirito pair) anchored F78-F84.\n" +
            "  Red Rose Sword now F95 field-boss lock (Warden of Blooming Rose).\n" +
            "PRESERVED (NOT moved): IF series boss-anchors, IM Shop tiers,\n" +
            "MD Originals, HF Implement weapons, FD elemental variants,\n" +
            "and the F75/F82/F84/F86/F87/F88/F91/F93/F97/F98/F99 Divine\n" +
            "boss anchors.\n\n" +
            "COSTS\n" +
            "None — no weapons removed, no rarity downgrades. Pure band\n" +
            "redistribution. The total Legendary count stays at 185 across\n" +
            "F1-F99.\n\n" +
            "TIPS\n" +
            "If you were farming F88-F90 for a specific fire-themed Relic,\n" +
            "check F65-F79 instead — the pool shifted downward. F96-F99\n" +
            "runs still carry mythic-tier Legendaries, just at thinner\n" +
            "per-pool counts (~17 at F99). Named Legendaries anchored to\n" +
            "specific floor bosses (IF series, HF Implements, the new\n" +
            "F50 Elucidator LAB and F55 Crystal Wyrm Dark Repulser) are\n" +
            "unchanged — the canon hooks are untouched.\n\n" +
            "SEE ALSO\n" +
            "[Legendary Redistribution Overview] · [Mid-Game Legendary Lifts (F12-F44)] · [Boss Drop Reference] · [Rarity Tiers & Drop Rates] · [Named Legendary Highlights] · [Alicization Lycoris Raid Weapons] · [SAO Lost Song Named Weapons] · [Floor Boss Roster — Canon Highlights]")
        {
            Tags = new[] { "items", "rarity", "legendary", "loot" }
        },

        // ── Bundle 11 — Legendary Redistribution Player Guide entries ──

        new("Items", "Legendary Redistribution Overview",
            "┌─ Items\n" +
            "│ Topic: Legendary Redistribution Overview (Bundle 11)\n" +
            "│ Span: F1-F99 (was F50+ only)\n" +
            "│ Total Legendaries: 185\n" +
            "│ Per-floor cap: ≤25 (currently peaks at ~18 at F80)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Bundle 11 redistributed all 185 Legendary weapons across the\n" +
            "full F1-F99 range. Before this sweep, every Legendary lived\n" +
            "at F50 or higher — thirty mid-game floors (F1-F49) carried\n" +
            "exactly zero Legendary chest entries. The new distribution\n" +
            "spreads Legendaries from F1 onward, anchors canon weapons to\n" +
            "their LN/anime/IF/AL floors, and caps the late-game spike\n" +
            "(F90 dropped from 62 to ~20 entries per pool).\n\n" +
            "USAGE\n" +
            "Nothing to do — the redistribution happens automatically at\n" +
            "chest-roll time. The change is visible in your day-to-day:\n" +
            "early floors now occasionally drop a Legendary, and mid-game\n" +
            "F12-F44 is no longer a Legendary wasteland.\n\n" +
            "EFFECTS\n" +
            "DESIGN PHILOSOPHY:\n" +
            "  - Strict tier gating: each non-canon Legendary lives in a\n" +
            "    narrow ~5-10 floor band, not a 25-floor smear.\n" +
            "  - Canon-anchored weapons stay at canon floors:\n" +
            "      Elucidator        F50 floor-boss Last-Attack\n" +
            "      Dark Repulser     F55 Crystal Wyrm + Lisbeth gift\n" +
            "      Black Lily Sword  F85 Silent Edge field-boss\n" +
            "      Night Sky Sword   F99 Heathcliff's Shadow boss\n" +
            "      Mother's Rosario  F76 Jun NPC quest\n" +
            "      Lambent Light     F40 Yulier NPC quest + F1-F8 chest\n" +
            "  - Non-canon weapons fill the curve smoothly between locks.\n" +
            "  - F1 always carries at least 2 Legendaries (Mate Chopper\n" +
            "    F1-F5 + Lambent Light F1-F8) so first-floor discovery is\n" +
            "    a real possibility.\n" +
            "  - Per-floor peak ≤25 Legendaries (currently ~18 at F80).\n" +
            "PRE/POST FLOOR PROFILE:\n" +
            "  Floor    Pre-Bundle-11   Bundle 11\n" +
            "  F1       0               2     (was empty)\n" +
            "  F12-44   0               1-3   (lifts seed mid-game)\n" +
            "  F50      0               4     (Elucidator + chains)\n" +
            "  F75      9-11            9     (preserved)\n" +
            "  F90      62              ~20   (peak broken up)\n" +
            "  F99      ~24             ~17   (smoothed)\n\n" +
            "COSTS\n" +
            "None. No weapons removed, no rarity changes. Pure band sweep.\n\n" +
            "TIPS\n" +
            "If you used to farm F90 for high Legendary density, the math\n" +
            "now favors F80-F88 (still 13-18 entries per pool with better\n" +
            "spread of canon-themed drops). Early-floor runs are no longer\n" +
            "Legendary-empty: a lucky F1-F8 chest can roll Mate Chopper\n" +
            "or Lambent Light. See the per-floor counts in the spec doc\n" +
            "(LEGENDARY_REDISTRIBUTION_PROPOSAL.md section 6) for the full\n" +
            "table.\n\n" +
            "SEE ALSO\n" +
            "[Legendary Distribution (F75-F99)] · [Mid-Game Legendary Lifts (F12-F44)] · [Boss Drop Reference] · [Rarity Tiers & Drop Rates] · [Named Legendary Highlights] · [Lambent Light & Asuna's Memory] · [Sleeping Knights' Tribute & Mother's Rosario] · [Crystal Wyrm of Lisbeth's Forge (F55)]")
        {
            Tags = new[] { "items", "rarity", "legendary", "loot", "bundle-11" }
        },

        new("Items", "Mid-Game Legendary Lifts (F12-F44)",
            "┌─ Items\n" +
            "│ Topic: Mid-Game Legendary Lifts (F12-F44)\n" +
            "│ Span: F12 to F44 (previously 0 Legendaries)\n" +
            "│ New entries: 7 LR-myth weapons + 4 floor-boss locks\n" +
            "│ Source: Chest pool + invented Divine Beast bosses\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Pre-Bundle-11, floors F12 through F44 carried zero Legendary\n" +
            "chest entries — thirty floors of pure Epic-or-lower loot. The\n" +
            "Bundle 11 redistribution lifted seven Last-Recollection /\n" +
            "Lost-Song mythological Legendaries down into this band, and\n" +
            "kept the four Alicization-Lycoris invented Divine Beast\n" +
            "bosses (F11, F17, F24, F30, F38, F40, F43) as guaranteed\n" +
            "Legendary drops. Players exploring early floors now have a\n" +
            "real chance at Legendary discovery.\n\n" +
            "USAGE\n" +
            "Roll chests on the listed floors; clear the floor boss for\n" +
            "the lock. No special action needed.\n\n" +
            "EFFECTS\n" +
            "CHEST-POOL LIFTS (7 new mid-game Legendaries):\n" +
            "  F12-F18  Nadr (Axe, LR myth)\n" +
            "  F18-F25  Futsu no Mitama (Katana, Lost Song shinto)\n" +
            "  F22-F29  Artemis Fult (Bow, LR myth)\n" +
            "  F25-F32  Giardino (Dagger, LR myth)\n" +
            "  F30-F36  Caduceus (Mace, Lost Song healer staff)\n" +
            "  F30-F37  Paopei (Claws, LR myth)\n" +
            "  F35-F42  Elder's Trident (Spear, LR myth)\n" +
            "FLOOR-BOSS LOCKS (preserved, AL Lycoris invented bosses):\n" +
            "  F11 Felos the Ember Drake     -> Starfall (Bow)\n" +
            "  F17 Gelidus the Frozen Colossus -> Savage Squall (1H Sword)\n" +
            "  F24 Grimhollow the Phantom    -> Phantasmagoria (Dagger)\n" +
            "  F30 Primos the World Serpent  -> Void Eater (1H Sword)\n" +
            "  F38 Obsidian the Black Knight -> Cactus Bludgeon (Mace)\n" +
            "  F40 Dracoflame the Elder Wyrm -> Crimson Stream (2H Sword)\n" +
            "  F43 Undine the Water Maiden   -> Midnight Rain (Rapier)\n" +
            "QUEST-LOCKS (Bundle 11 NPCs):\n" +
            "  F40 Yulier   -> Lambent Light (10 kills, see Lambent Light topic)\n" +
            "  F40 chest    -> Lambent Light tail (F1-F8 anchor extends)\n\n" +
            "COSTS\n" +
            "None beyond standard chest/boss costs. The lifts replaced no\n" +
            "existing weapons — they were pulled from the bloated F75-F99\n" +
            "pool, where their per-pool weight was diluted anyway.\n\n" +
            "TIPS\n" +
            "If you're rolling a fresh save, push to F11 and clear the\n" +
            "boss for the first guaranteed Legendary (Starfall). Each\n" +
            "AL invented boss between F11 and F49 carries one more\n" +
            "guaranteed Legendary on top of any chest roll. The seven\n" +
            "LR-myth chest lifts are tier-coherent with their floor — a\n" +
            "F25 Giardino isn't a numerical god (its base stats scale at\n" +
            "the floor's tier), but it carries a Legendary special effect\n" +
            "and a refinement slot count above any Epic-or-lower drop.\n\n" +
            "SEE ALSO\n" +
            "[Legendary Redistribution Overview] · [Boss Drop Reference] · [Lambent Light & Asuna's Memory] · [Floor Boss Roster — Canon Highlights] · [SAO Last Recollection Weapons] · [SAO Lost Song Named Weapons]")
        {
            Tags = new[] { "items", "rarity", "legendary", "mid-game", "bundle-11" }
        },

        new("Items", "Boss Drop Reference",
            "┌─ Items\n" +
            "│ Topic: Boss Drop Reference\n" +
            "│ Sources: Floor-boss + field-boss + NPC + LAB\n" +
            "│ Includes: Bundle 11 F50 Elucidator LAB + F55 Crystal Wyrm\n" +
            "│ Use: One-stop list of every guaranteed boss drop\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Single-page reference of every guaranteed weapon/item drop\n" +
            "from a named boss in the game — floor-boss kills, floor-boss\n" +
            "Last-Attack Bonus (LAB) drops (must be killing-blow),\n" +
            "field-boss kills, and the new Bundle 11 anchors at F40, F50,\n" +
            "F55, and F76. NPC-quest weapon rewards live in the Quests &\n" +
            "NPCs category — this entry covers the boss-side drops.\n\n" +
            "USAGE\n" +
            "Cross-reference before pushing a floor: know which fights\n" +
            "give a guaranteed Legendary, which require Last-Attack, and\n" +
            "which bosses gate Divine objects.\n\n" +
            "EFFECTS\n" +
            "FLOOR-BOSS GUARANTEED DROPS (any party member kills):\n" +
            "  F11 Felos the Ember Drake       -> Starfall (Legendary Bow)\n" +
            "  F17 Gelidus the Frozen Colossus -> Savage Squall (Legendary 1HS)\n" +
            "  F20 Absolut the Winter Monarch  -> Blue Rose Sword (Divine)\n" +
            "  F24 Grimhollow the Phantom      -> Phantasmagoria (Leg. Dag)\n" +
            "  F30 Primos the World Serpent    -> Void Eater (Leg. 1HS)\n" +
            "  F38 Obsidian the Black Knight   -> Cactus Bludgeon (Leg. Mace)\n" +
            "  F40 Dracoflame the Elder Wyrm   -> Crimson Stream (Leg. 2HS)\n" +
            "  F43 Undine the Water Maiden     -> Midnight Rain (Leg. Rapier)\n" +
            "  F49 Shadowstep Assassin         -> Midnight Sun (Leg. Katana)\n" +
            "  F75 Skull Reaper                -> Masamune (Divine Katana)\n" +
            "  F82 Legacy of Grand             -> Hexagramme (Divine Rapier)\n" +
            "  F84 Queen of Ant                -> Caladbolg (Divine Spear)\n" +
            "  F86 King of Skeleton            -> Tyrfing (Divine 1HS)\n" +
            "  F87 Radiance Eater              -> Iron Maiden Dagger (Divine)\n" +
            "  F88 Rebellious Eyes             -> Ouroboros (Divine Axe)\n" +
            "  F91 Seraphiel the Fallen        -> Mjolnir (Divine Mace)\n" +
            "  F93 Ragnarok Final Beast        -> Ascalon (Divine 2HS)\n" +
            "  F97 Cardinal System Error       -> Time Piercing Sword (Divine)\n" +
            "  F98 Incarnation of the Radius   -> Black Lily Sword (Divine)\n" +
            "  F99 Heathcliff's Shadow         -> Night Sky Sword (Divine)\n\n" +
            "FLOOR-BOSS LAST-ATTACK BONUS (must be the killing blow):\n" +
            "  F50 Tier of Sin                 -> Elucidator (Legendary 1HS, NEW)\n" +
            "  F85 floor boss                  -> Bow Zephyros (Leg. Bow)\n" +
            "  F92 floor boss                  -> Sacred Cross (Leg. 2HS)\n" +
            "  F93 floor boss                  -> Glow Haze (Leg. Scimitar)\n" +
            "  F94 floor boss                  -> Saku (Leg. Katana)\n" +
            "  F95 floor boss                  -> Mirage Knife (Leg. Dagger)\n" +
            "  F96 floor boss                  -> Northern Light (Leg. Axe)\n" +
            "  F98 floor boss                  -> Lunatic Roof (Leg. Spear)\n" +
            "  F99 Heathcliff's Shadow         -> Artemis (Leg. Bow, alongside Night Sky)\n\n" +
            "FIELD-BOSS GUARANTEED DROPS (overworld, never respawn):\n" +
            "  F2  Bullbous Bow             -> Bullbous Horn (mat)\n" +
            "  F14 Starlight Sentinel       -> Integral Arc Angel (Epic Bow)\n" +
            "  F22 Forest King Stag         -> Kingly Antler (mat)\n" +
            "  F25 Labyrinth Warden         -> Nox Radgrid (Epic 1HS)\n" +
            "  F35 Magnatherium             -> Mammoth Tusk (mat)\n" +
            "  F40 Ogre Lord                -> Ogre's Cleaver (mat)\n" +
            "  F40 Phoenix of Smolder Peak  -> Conflagrant Flame Bow (Divine)\n" +
            "  F48 Frost Dragon             -> Crystallite Ingot (mat)\n" +
            "  F49 Nicholas the Renegade    -> Returning Soul (Christmas only)\n" +
            "  F55 Crystal Wyrm of Lisbeth's Forge -> Dark Repulser (Leg., NEW)\n" +
            "  F60 Kagutsuchi Fire Samurai  -> Spirit Sword Kagutsuchi (FD Leg.)\n" +
            "  F61 Crimson Forneus          -> Rosso Forneus (IF Leg. 1HS)\n" +
            "  F70 Susanoo the Storm Blade  -> Spirit Sword Susanoo (FD Leg.)\n" +
            "  F77 Goblin Leader            -> Mace of Asclepius (HF Leg.)\n" +
            "  F80 Soul Binder              -> Arcaneblade Soul Binder (HF Leg.)\n" +
            "  F80 Pyre Lord of Heathcliff  -> Flame Lord (FD Leg. 2HS)\n" +
            "  F83 Arboreal Fear            -> Demonspear Gae Bolg (HF Leg.)\n" +
            "  F83 Ruinous Herald           -> Fellblade Ruinous Doom (HF Leg.)\n" +
            "  F85 Silent Edge              -> Black Lily Sword (Divine)\n" +
            "  F85 Abased Beast             -> Godblade Dragonslayer (HF Leg.)\n" +
            "  F85 Yuuki's Echo             -> Macafitel (FD Leg. Rapier)\n" +
            "  F86 Fellaxe Revenant         -> Fellaxe Demon's Scythe (HF Leg.)\n" +
            "  F87 Yasha the Night Demon    -> Yasha Astaroth (IF Leg.)\n" +
            "  F87 Night Stalker            -> Saintblade Durandal (HF Leg.)\n" +
            "  F90 Gaou the Ox-King         -> Gaou Reginleifr (IF Leg.)\n" +
            "  F93 Banishing Ray            -> Glimmerblade Banishing Ray (HF Leg.)\n" +
            "  F94 Ark Knight               -> Ragnarok's Bane Headsman (HF Leg.)\n" +
            "  F95 Gaia Breaker             -> Stigmablade Arondight (HF Leg.)\n" +
            "  F95 Warden of Stopped Hours  -> Time Piercing Sword (Divine)\n" +
            "  F95 Warden of Blooming Rose  -> Red Rose Sword (FD Leg.)\n" +
            "  F96 Eternal Dragon           -> Demonblade Gram (HF Leg.)\n" +
            "  F97 Administrator's Regent   -> Silvery Ruler (FD Leg.)\n" +
            "  F98 Blaze Armor              -> Yato Masamune (HF Leg.)\n" +
            "  F98 Ashen Kirito Simulacrum  -> Elucidator Rouge (FD Leg.)\n\n" +
            "FIELD-BOSS SECONDARY DROPS (paired shield):\n" +
            "  F14 Starlight Sentinel  -> Shield Fermat (Epic)\n" +
            "  F25 Labyrinth Warden    -> Shield Nox Fermat (Epic)\n" +
            "  F61 Crimson Forneus     -> Rosso Aegis (Leg.)\n" +
            "  F87 Yasha               -> Yasha Kavacha (Leg.)\n" +
            "  F90 Gaou                -> Gaou Tatari (Leg.)\n\n" +
            "COSTS\n" +
            "Each field-boss never respawns once defeated; missing the\n" +
            "kill means farming the chest pool (which carries the same\n" +
            "DefId in many cases). LAB drops require the killing blow —\n" +
            "if your ally lands the final hit, the LAB Legendary does NOT\n" +
            "drop.\n\n" +
            "TIPS\n" +
            "F50 Elucidator and F99 Artemis are the two LAB drops most\n" +
            "easily missed — both are paired with bigger Divine fights\n" +
            "(F50 Six-Armed Buddha + F99 Heathcliff's Shadow). Save burst\n" +
            "skills for the final HP slice. F95 carries three guaranteed\n" +
            "Legendary/Divine drops in one floor (Time Piercing + Red Rose\n" +
            "Sword + Stigmablade) — budget durability for three boss kills\n" +
            "before ascending. F85 and F87 are similar triple-drop floors.\n\n" +
            "SEE ALSO\n" +
            "[Floor Boss Roster — Canon Highlights] · [Field Bosses — Guaranteed Drops] · [Integral Factor Field Bosses] · [Fractured Daydream Field Bosses] · [Avatar Weapons & Last-Attack Bonus] · [Crystal Wyrm of Lisbeth's Forge (F55)] · [Lambent Light & Asuna's Memory] · [Sleeping Knights' Tribute & Mother's Rosario] · [Divine Object Set — Integrity Knights]")
        {
            Tags = new[] { "reference", "bosses", "drops", "lab", "bundle-11" }
        },

        new("Quests & NPCs", "Lambent Light & Asuna's Memory",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Yulier (F40) — KoB-era Asuna friend\n" +
            "│ Floor: 40 (quest); F1-F8 (chest tease)\n" +
            "│ Quest: The Lightning Flash's Memory (10 kills)\n" +
            "│ Reward: Lambent Light (Legendary Rapier)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Lambent Light is Asuna's signature rapier from the KoB era —\n" +
            "the canon weapon she carried during the F50-F75 push in the\n" +
            "novels. Bundle 11 wires it through three sources: an early\n" +
            "F1-F8 chest tease (very rare), the F40 Yulier NPC quest\n" +
            "(guaranteed reward), and the existing F88-F94 Radiant Light\n" +
            "(her post-game upgrade). Mother's Rosario lives separately at\n" +
            "F76 — see that topic.\n\n" +
            "USAGE\n" +
            "Three paths to Lambent Light:\n" +
            "  1. F1-F8 chest pool — small chance per chest (Bundle 11 F1\n" +
            "     anchor pair with Mate Chopper).\n" +
            "  2. F40 Yulier (BrightYellow 'Y' or NPC glyph) — accept\n" +
            "     'The Lightning Flash's Memory', slay 10 monsters on F40,\n" +
            "     return for the rapier + 450 Col + 350 XP.\n" +
            "  3. F88-F94 chest band — Radiant Light (Asuna post-game\n" +
            "     upgrade, separate DefId, CritRate+20).\n\n" +
            "EFFECTS\n" +
            "  Lambent Light    Legendary Rapier, flat CritRate bonus\n" +
            "  Radiant Light    Legendary Rapier, CritRate+20 (post-game)\n" +
            "  Mother's Rosario Legendary Rapier, ComboBonus+50, 11-hit\n" +
            "                   (F76 Jun NPC, see Sleeping Knights topic)\n" +
            "Yulier's quest is one-shot per save. Inventory full at turn-in\n" +
            "drops the rapier at your feet — never silently lost.\n\n" +
            "COSTS\n" +
            "10 F40 monster kills for the guaranteed path. The chest tease\n" +
            "is pure RNG; do not rely on it.\n\n" +
            "TIPS\n" +
            "Asuna's three rapiers form a complete Asuna-themed loadout\n" +
            "across the run. If you're playing a Rapier build, plant the\n" +
            "F40 Yulier turn-in BEFORE fighting the F40 floor boss\n" +
            "(Dracoflame the Elder Wyrm) — the quest kills can overlap\n" +
            "with the boss approach. The KoB-era flavor pairs well with\n" +
            "joining Knights of the Blood Oath at F55.\n\n" +
            "CANON\n" +
            "SAO LN vol 4-7 (Aincrad arc): Lambent Light is Asuna's\n" +
            "personalized rapier as Vice-Commander of the Knights of the\n" +
            "Blood Oath, used through the F50-F75 floor push. Yulier is a\n" +
            "Liberation Army officer who fought alongside Asuna on F59.\n\n" +
            "SEE ALSO\n" +
            "[Sleeping Knights' Tribute & Mother's Rosario] · [Knights of the Blood Oath (F55)] · [Named Legendary Highlights] · [Quest Types & Rewards] · [Critical Hits]")
        {
            Tags = new[] { "quests", "npcs", "asuna", "rapier", "bundle-11" }
        },

        new("Quests & NPCs", "Sleeping Knights' Tribute & Mother's Rosario",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Jun (F76) — Sleeping Knights memorial\n" +
            "│ Floor: 76\n" +
            "│ Quest: The Sleeping Knights' Tribute (15 kills)\n" +
            "│ Reward: Mother's Rosario (Legendary Rapier, 11-hit OSS)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Mother's Rosario is Yuuki Konno's signature rapier — the\n" +
            "blade that carries her 11-hit Original Sword Skill of the\n" +
            "same name. Bundle 11 wires it as a one-shot quest reward\n" +
            "from Jun, a Sleeping Knights survivor on F76 (the memorial\n" +
            "floor for Yuuki and the Sleeping Knights guild).\n\n" +
            "USAGE\n" +
            "Reach F76. Bump Jun (NPC glyph in floor town/safe room).\n" +
            "Accept 'The Sleeping Knights' Tribute': slay 15 monsters on\n" +
            "F76, return to Jun for the rapier + 700 Col + 550 XP.\n\n" +
            "EFFECTS\n" +
            "  Mother's Rosario    Legendary Rapier\n" +
            "                      ComboBonus+50, 11-hit Original Sword Skill\n" +
            "                      Inventory-full -> dropped at feet\n" +
            "Quest is one-shot per save. The 15 kills overlap with any\n" +
            "standing F76 weapon-gated Kill quests.\n\n" +
            "COSTS\n" +
            "15 F76 monster kills. F76 is also the Hollow-Fragment HNM\n" +
            "questgiver band (F79+ NPCs nearby) — budget durability for\n" +
            "the broader floor.\n\n" +
            "TIPS\n" +
            "If you're already chasing Lambent Light at F40 and Radiant\n" +
            "Light in the F88-F94 chest band, Mother's Rosario completes\n" +
            "the Asuna/Yuuki rapier triptych. Mother's Rosario's 11-hit\n" +
            "OSS is the longest combo in the game — pair with Combo\n" +
            "Finisher mechanics for double-damage on the final strike.\n\n" +
            "CANON\n" +
            "SAO LN vol 7 (Mother's Rosario arc): Yuuki Konno (Zekken,\n" +
            "leader of the Sleeping Knights, terminal AIDS patient) wields\n" +
            "Mother's Rosario as the only Original Sword Skill in canon\n" +
            "ALO. The Sleeping Knights guild memorialize her after her\n" +
            "death. Jun is a Sleeping Knights member who survived to\n" +
            "tend the memorial.\n\n" +
            "SEE ALSO\n" +
            "[Lambent Light & Asuna's Memory] · [Sleeping Knights (F60)] · [Named Legendary Highlights] · [Combo Attacks] · [Sword Skills — Unlock & Use] · [Quest Types & Rewards]")
        {
            Tags = new[] { "quests", "npcs", "yuuki", "rapier", "bundle-11" }
        },

        new("World", "Crystal Wyrm of Lisbeth's Forge (F55)",
            "┌─ World\n" +
            "│ Topic: Crystal Wyrm of Lisbeth's Forge\n" +
            "│ Floor: 55 (overworld field-boss)\n" +
            "│ Glyph: BrightCyan 'W'\n" +
            "│ Drop: Dark Repulser (Legendary 1H Sword)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "A canon LN field-boss new in Bundle 11. The Crystal Wyrm\n" +
            "lives in a crystallite hollow on F55 — its scales chime when\n" +
            "it breathes. Defeating it grants Dark Repulser, the second\n" +
            "half of Kirito's iconic dual-blade pair (Elucidator is the\n" +
            "F50 Last-Attack Bonus). The Wyrm is the field-boss source;\n" +
            "Lisbeth's F48 handover (after F55 boss clear) is a separate\n" +
            "guaranteed gift — see Lisbeth's Dark Repulser Gift topic.\n\n" +
            "USAGE\n" +
            "Reach F55 overworld. The wyrm wanders the overworld floor\n" +
            "(does NOT spawn inside the Labyrinth). Bump to engage. HP\n" +
            "scales 3.2x normal, ATK 1.6x. Once defeated, never respawns.\n\n" +
            "EFFECTS\n" +
            "  Drop          Dark Repulser (Legendary 1H Sword)\n" +
            "  Effect        CritHeal+5 — restores HP on crit hit\n" +
            "  Pair          Canonical pair with Elucidator (F50 LAB)\n" +
            "                Pair Resonance: +10% damage MH+OH, +5% crit\n" +
            "                re-roll on first hit per encounter\n" +
            "  Lisbeth gift  Talking to Lisbeth at F48 AFTER clearing the\n" +
            "                F55 boss triggers a one-time second Dark\n" +
            "                Repulser handover (canon Lisbeth-craft flavor)\n\n" +
            "COSTS\n" +
            "Field-boss durability cost. Field-boss drops do NOT block\n" +
            "Avatar Last-Attack Bonus rolls — a Crystal Wyrm kill with a\n" +
            "matching weapon class still rolls the avatar pool.\n\n" +
            "TIPS\n" +
            "Push F50 floor boss for Elucidator (Last-Attack Bonus, must\n" +
            "be killing blow), then push to F55 and find the wyrm BEFORE\n" +
            "the floor boss for the Dark Repulser pair. Return to F48\n" +
            "Lindarth after F55 boss clear for Lisbeth's gift — you can\n" +
            "end up with TWO Dark Repulsers (one for the active dual-wield\n" +
            "set, one for an ally or storage).\n\n" +
            "CANON\n" +
            "SAO LN vol 2 (the Lisbeth side-story): Lisbeth and Kirito\n" +
            "descend into a crystal cavern on F55 to harvest the breath-\n" +
            "frozen ingot of a crystal dragon. Lisbeth forges Dark Repulser\n" +
            "from the ingot back at her Lindarth shop. The wyrm's scales-\n" +
            "chime detail comes directly from the LN description.\n\n" +
            "SEE ALSO\n" +
            "[Lisbeth's Dark Repulser Gift (F48)] · [Field Bosses — Guaranteed Drops] · [Boss Drop Reference] · [Pair Resonance — Mechanics Clarified] · [Paired Dual-Wield Weapons] · [Lindarth Town (F48)] · [F50 Boss & the Elucidator Drop]")
        {
            Tags = new[] { "world", "field-boss", "kirito", "dual-blades", "bundle-11" }
        },

        new("Quests & NPCs", "Lisbeth's Dark Repulser Gift (F48)",
            "┌─ Quests & NPCs\n" +
            "│ NPC: Lisbeth (BrightMagenta 'L', Lindarth F48)\n" +
            "│ Floor: 48 (Lindarth town)\n" +
            "│ Trigger: Talk to Lisbeth AFTER F55 boss is cleared\n" +
            "│ Reward: Dark Repulser (Legendary 1H Sword), one-time\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Bundle 11 wires Lisbeth's canon LN dialogue: after you bring\n" +
            "back F55 dragon ore (modeled here as 'clear the F55 floor\n" +
            "boss'), Lisbeth crafts Dark Repulser and gifts it to you on\n" +
            "your next visit. This is a separate, parallel source from\n" +
            "the F55 Crystal Wyrm field-boss drop — both can fire on the\n" +
            "same save, so a player who clears both ends up with two Dark\n" +
            "Repulsers.\n\n" +
            "USAGE\n" +
            "  1. Reach F55 and clear the floor boss (any Five-Eyed Wraith\n" +
            "     or your floor's labyrinth boss — the trigger is\n" +
            "     HighestFloorBossCleared >= 55).\n" +
            "  2. Return to F48 Lindarth.\n" +
            "  3. Bump Lisbeth (BrightMagenta 'L') to open the forge\n" +
            "     dialog. The gift fires before the standard craft menu.\n" +
            "  4. The forge menu opens normally afterward — you can craft\n" +
            "     R6 weapons in the same visit.\n\n" +
            "EFFECTS\n" +
            "  Dialog        \"Hey — remember that crystallite ore from the\n" +
            "                F55 dragon? I finally finished it. Here. I\n" +
            "                made it for you.\"\n" +
            "  Drop          Dark Repulser (Legendary 1H Sword)\n" +
            "  Effect        CritHeal+5 — restores HP on crit hit\n" +
            "  Inventory     Auto-added; full -> dropped at your feet\n" +
            "  One-time      Quest 'lisbeth_dark_repulser_gift' marks\n" +
            "                turned-in; never fires again on the save\n\n" +
            "COSTS\n" +
            "None — pure gift. Does NOT consume Col, mats, or your forge\n" +
            "craft slots. Fully additive to her R6 craft line.\n\n" +
            "TIPS\n" +
            "Time the F48 visit to chain with a planned R6 craft so you\n" +
            "bank the Lindarth trip. If you also kill the F55 Crystal\n" +
            "Wyrm, you can build a dual-wield kit AND give a backup Dark\n" +
            "Repulser to a Kirito ally for a near-canon party loadout.\n\n" +
            "CANON\n" +
            "SAO LN vol 2 (Lisbeth side-story): after the F55 dragon-ore\n" +
            "expedition, Lisbeth gifts Dark Repulser to Kirito as a\n" +
            "personal blacksmith gesture — both because the ingot was\n" +
            "extraordinary and because of the bond formed during the dive.\n\n" +
            "SEE ALSO\n" +
            "[Crystal Wyrm of Lisbeth's Forge (F55)] · [Lindarth Town (F48)] · [Lisbeth — Rarity 6 Craft Line] · [Pair Resonance — Mechanics Clarified] · [F50 Boss & the Elucidator Drop]")
        {
            Tags = new[] { "quests", "npcs", "lisbeth", "kirito", "bundle-11" }
        },

        new("World", "F50 Boss & the Elucidator Drop",
            "┌─ World\n" +
            "│ Topic: F50 Boss & the Elucidator Drop\n" +
            "│ Floor: 50 (labyrinth boss chamber)\n" +
            "│ Boss: The Six-Armed Buddha / Tier of Sin\n" +
            "│ LAB drop: Elucidator (Legendary 1H Sword) — must killing-blow\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "The F50 floor boss now drops Elucidator on Last-Attack Bonus\n" +
            "(killing-blow only). This is a canon LN anchor: Kirito takes\n" +
            "Elucidator from the F50 boss in SAO LN vol 4. It is the only\n" +
            "Bundle 11 LAB drop that is paired with a non-IM weapon (the\n" +
            "rest of the LAB pool is IM canon at F85+). Pre-Bundle-11\n" +
            "Elucidator was orphaned to the chest pool; it now lives at\n" +
            "its canon floor.\n\n" +
            "USAGE\n" +
            "Reach F50, find the labyrinth, fight the Six-Armed Buddha\n" +
            "(Tier of Sin). YOU must land the killing blow — if your\n" +
            "ally lands the final hit, the LAB drop does NOT fire.\n" +
            "Elucidator drops alongside any standard floor-boss loot.\n\n" +
            "EFFECTS\n" +
            "  Boss          Six-Armed Buddha (canon F50 boss)\n" +
            "  Codename      Tier of Sin (LN designation)\n" +
            "  LAB drop      Elucidator (Legendary 1H Sword)\n" +
            "  Effect        SkillCooldown-1 — sword skills come off\n" +
            "                cooldown one turn faster\n" +
            "  Pair          Canonical pair with Dark Repulser (F55 wyrm\n" +
            "                + F48 Lisbeth gift). See Pair Resonance.\n" +
            "  Last-Attack   Killing blow required. If allies finish, the\n" +
            "                Legendary does NOT drop.\n\n" +
            "COSTS\n" +
            "Boss durability cost + the LAB risk: micro-managing damage\n" +
            "to ensure your hit ends the fight. Burst skills and crit\n" +
            "weapons are best for landing the killing blow predictably.\n\n" +
            "TIPS\n" +
            "Open the fight with Look Mode to scout the boss HP. Save\n" +
            "your highest-burst sword skill (Vorpal Strike, Star Splash,\n" +
            "etc.) for the final HP slice. If allies are on the field,\n" +
            "consider rotating them to non-attack roles via SAO Switch\n" +
            "before the killing blow window — or pre-pull aggro so the\n" +
            "boss faces YOU. Pair this run with the F55 Crystal Wyrm\n" +
            "and F48 Lisbeth gift for a same-session Dual-Blades kit.\n\n" +
            "CANON\n" +
            "SAO LN vol 4 (Aincrad arc): Kirito takes Elucidator from\n" +
            "the F50 floor boss as Last-Attack Bonus — the LN explicitly\n" +
            "calls out the LAB mechanic. The blade becomes Kirito's main-\n" +
            "hand for the rest of the F50-F75 push, and pairs with Dark\n" +
            "Repulser (F55 Lisbeth craft) to form his iconic dual-wield\n" +
            "loadout once Dual Blades unlocks at F74.\n\n" +
            "SEE ALSO\n" +
            "[Floor Boss Roster — Canon Highlights] · [Boss Drop Reference] · [Crystal Wyrm of Lisbeth's Forge (F55)] · [Lisbeth's Dark Repulser Gift (F48)] · [Pair Resonance — Mechanics Clarified] · [Unique Skill: Dual Blades] · [Avatar Weapons & Last-Attack Bonus]")
        {
            Tags = new[] { "world", "bosses", "kirito", "elucidator", "lab", "bundle-11" }
        },
    };
}
