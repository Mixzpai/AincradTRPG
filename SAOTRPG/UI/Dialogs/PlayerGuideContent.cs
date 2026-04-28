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

        new("Combat & Rarity", "Floor Scaling Formulas",
            "┌─ Combat & Rarity\n" +
            "│ Topic: Floor Scaling Formulas\n" +
            "│ Source: Map/BossFactory.cs\n" +
            "│ Applies to: Procedurally-named floor bosses (F2-F99)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Floor bosses scale linearly+quadratically with floor number.\n" +
            "Canon-anchored bosses (Illfang F1, Asterius F2, etc.) override\n" +
            "the scaling values — see their Floor entry. The curve below\n" +
            "applies to procedurally-named bosses that fill unspecified\n" +
            "floors.\n\n" +
            "MECHANICS\n" +
            "Level = 10 + 2 * floor\n" +
            "HP    = 150 + 30 * floor + 0.5 * floor^2\n" +
            "Col   = 4000 + 500 * floor + 20 * floor^2\n\n" +
            "TIPS\n" +
            "Use these to estimate prep gear before stepping on a stair tile.\n" +
            "If the boss room is shrouded, the formulas predict approximate\n" +
            "HP.\n\n" +
            "SEE ALSO\n" +
            "[Damage Formula] · [Critical Hits] · [Floor 50] · [Floor 75] · [Floor 99]")
        {
            Tags = new[] { "combat", "scaling", "bosses" }
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
            "[Look Mode & Counter Stance] · [Sprint & Stealth Move] · [Floor Scaling Formulas]")
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
            "rating, and canon SAO flavor lore. The roster is locked\n" +
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
            "[Look Mode & Counter Stance] · [Controls & Keybindings] · [Permadeath & Save Deletion]")
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
            "[The Six Attributes] · [Experience & Leveling] · [Floor 1]")
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
            "[Unique Skill: Dual Blades] · [Defense — Block, Parry, Dodge] · [Floor Scaling Formulas]")
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
            "[Floor 2] · [Sword Skills — Unlock & Use] · [Critical Hits]")
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
            "[Biomes] · [Status: Stun & Slow] · [Floor Scaling Formulas]")
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
            "[Floor 1] · [Ascending a Floor] · [Achievements] · [Titles & the Active Title Slot]")
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
            "[Guild System Overview] · [Town Guard (Outlaw Mode)] · [Floor 75] · [Floor 60] · [Bargaining (Life Skill)] · [Vendors — Rotating Stock]")
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
            ". They sit parallel to Weapon Proficiency:\n" +
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
            "Mining is the seventh Life Skill — a non-combat\n" +
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
            "MID-MINING SAVE PERSISTENCE:\n" +
            "Partially-mined ore veins now preserve their strike-count\n" +
            "across save/load — quit mid-vein and the chip-counter is\n" +
            "exactly where you left it. Limited to the CURRENT FLOOR:\n" +
            "ascending stairs regenerates the next floor and discards\n" +
            "any prior-floor vein state along with the rest of the map.\n" +
            "Backtracking down stairs to an earlier floor also regenerates\n" +
            "that floor — the persistence is live-session-on-floor only.\n\n" +
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
            "[Floor 1] · [Life Skills] · [Weapon Proficiency Ranks] · [Floor Titles]")
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
            "  KATANA            Iaijutsu (see wiring below)\n" +
            "                    OR Drawing Stance (+2 CritRate)\n" +
            "  BOW               Marksman Eye (see wiring below)\n" +
            "                    OR Quickdraw (+1 AttackSpeed)\n" +
            "The other 9 weapon types use the unchanged generic L50 fork.\n" +
            "Choices use the new B13 StatType grants (CritRate, Attack-\n" +
            "Speed) — they show up directly on the character sheet rather\n" +
            "than as flavor-string riders.\n\n" +
            "BUNDLE 12 — IAIJUTSU & MARKSMAN EYE CONSUMERS WIRED:\n" +
            "The L25 fork prompts and L50 wiring complete the\n" +
            "loop by wiring the actual gameplay consumers:\n" +
            "  KATANA L25 IAIJUTSU\n" +
            "    +25% damage on the FIRST strike against each enemy per\n" +
            "    floor. Tracked per-enemy, not per-encounter — once you\n" +
            "    hit a mob once on a floor, subsequent hits on that mob\n" +
            "    revert to baseline. Resets on floor change. Mutually\n" +
            "    exclusive with Backstab (sneak attack already grants\n" +
            "    x2-x3 damage; Iaijutsu does NOT layer on top of a\n" +
            "    backstab to prevent compounding). Combat log shows:\n" +
            "    \"Iaijutsu strike! +25% damage on first contact.\"\n" +
            "  BOW L25 MARKSMAN EYE\n" +
            "    +5 tile range on Bow sword-skills (in addition to the\n" +
            "    existing +2 CritRate). Affects skills only — basic-attack\n" +
            "    ranged-fire is a separate system. The range overflow\n" +
            "    stacks with weapon-line skill range; e.g. a 6-tile skill\n" +
            "    becomes 11 tiles with Marksman Eye selected.\n" +
            "Picks remain permanent per save — no respec.\n\n" +
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
            "[Floor Scaling Formulas] · [Ascending a Floor] · [Mechanical Tiles] · [Ambient World Animation]")
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
            "[Labyrinth System] · [Safe Rooms & Mechanics] · [River Crossing & Aquatic Mobs] · [Swimming (Life Skill)] · [Ascending a Floor] · [Floor 1]")
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
            "[Unique Skill: Darkness Blade] · [Floor 75] · [Floor 25] · [Permadeath & Save Deletion] · [Save System]")
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
            "[Potions, Crystals & Throwables] · [Permadeath & Save Deletion] · [Divine Objects]")
        {
            Tags = new[] { "world", "bosses", "divine" }
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
            "[Karma & Alignment] · [Floor 75] · [Floor 1] · [Guild System Overview]")
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
            "[Floor Scaling Formulas] · [Floor Titles] · [Save System] · [Col Economy — How You Earn] · [Titles & the Active Title Slot]")
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
            "[Floor Scaling Formulas] · [Labyrinth System] · [Floor Canon] · [Prefab Rooms — What They Are] · [Ascending a Floor]")
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
            "[Floor 1] · [Floor 48] · [Floor Scaling Formulas] · [Prefab Rooms — Boss Arenas] · [Floor 1]")
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
            Tags = new[] { "weapons", "rarity", "cross-game", "spoiler" }
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
            "[Divine Objects] · [Floor 78] · [Floor 65] · [Floor 50]")
        {
            Tags = new[] { "weapons", "divine", "last-recollection", "spoiler" }
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
            "[Weapon Evolution Chains] · [Evolution Chain Table]")
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
            "[Weapon Refinement System] · [Weapon Evolution Chains] · [Refinement Ingots] · [Enhancement Ores System] · [Sealed Weapons] · [Lisbeth — Rarity 6 Craft Line]")
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
            "[Weapon Refinement System] · [Named Legendary Highlights]")
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
            "BUNDLE 13 — FORGE TABS\n" +
            "Lisbeth's forge dialog now exposes five tabs (F1-F5):\n" +
            "  F1 R6 Crafts — this entry's content.\n" +
            "  F2 Iron Ingot Enhance (Common/Uncommon, +1..+5).\n" +
            "  F3 Mithril Ingot Enhance (Rare/Epic, +1..+7).\n" +
            "  F4 Reforge — re-roll random Bonuses.\n" +
            "  F5 Crystallite Ingot Enhance (Epic/Legendary, +1..+10).\n" +
            "The +10 ceiling lifts the legacy +6 Anvil cap, but only on\n" +
            "Epic/Legendary tier weapons via crystallite. Common-Rare gear\n" +
            "still tops out at +5 (iron) / +7 (mithril).\n\n" +
            "SEE ALSO\n" +
            "[Floor 48] · [Weapon Refinement System] · [Anvil — Repair, Enhance, Evolve, Refine] · [Named Legendary Highlights] · [Mithril Ingot Enhance (Rare/Epic, +1..+7)] · [Crystallite Ingot Enhance (Epic/Legendary, +1..+10)] · [Reforge — Re-roll Random Bonuses]")
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
            "[Named Legendary Highlights] · [Weapon Types Overview] · [Infinity Moment Last Attack Bonus Weapons] · [Floor 79] · [Corruption Stones & Corrupted Weapons]")
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
            "[Floor 79] · [Avatar Weapons & Last-Attack Bonus] · [Named Legendary Highlights] · [Rarity Tiers & Drop Rates]")
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
            "[Floor 79] · [Hollow Area Uniques] · [Avatar Weapons & Last-Attack Bonus]")
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
            "[Integral Factor Weapon Series] · [Weapon Refinement System] · [Floor 1]")
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
            "TOOL: Holds non-combat utility gear. Pickaxes\n" +
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
            "Mining is the headline non-combat loop. Equip a\n" +
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
            "Buy Wooden at every vendor F1-F9 (~96 Col post-markup) and\n" +
            "Iron at every vendor F10-F50 (~384 Col post-markup). Mithril\n" +
            "is WORLD-FIND ONLY — no vendor stocks it; loot\n" +
            "it from chests, ore-cluster rooms, or quest rewards on F50+.\n" +
            "Equip via Inventory → Tool slot. Repair at the Anvil like\n" +
            "any equipment piece — pickaxes follow normal durability/repair\n" +
            "rules.\n\n" +
            "EFFECTS\n" +
            "  TIER       VENDOR  COL    MAXDUR   MININGPOWER   NOTE\n" +
            "  Wooden     F1-F9   80     30       0             Starter — vendor stock\n" +
            "  Iron       F10-F50 320    80       1             -1 strike per vein, min 1\n" +
            "  Mithril    none    1800   200      2             find-only, +10% OreQuality\n" +
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
            "Upgrade to Iron the moment you reach F10 — the strike-cost\n" +
            "reduction alone pays back the ~384 Col within ~5 veins, and\n" +
            "the durability tripling means you stop respawning Wooden\n" +
            "replacements every shop visit. Mithril is a hunt, not a\n" +
            "purchase: ore-cluster rooms on F50+ floors are the most\n" +
            "reliable source. Its +10% OreQuality compounds across the\n" +
            "F50-F99 push, where you'll be chipping Mithril and Divine\n" +
            "veins by the dozen for endgame refinement ingots.\n\n" +
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
            "[Sealed Weapons] · [Avatar Weapons & Last-Attack Bonus] · [Infinity Moment Shop Weapons] · [Named Legendary Highlights]")
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
            "[Anvil — Repair, Enhance, Evolve, Refine] · [Material Tiers (Baseline)] · [Sealed Weapons] · [Infinity Moment Shop Weapons]")
        {
            Tags = new[] { "enhancement-ore", "anvil", "crafting" }
        },

        new("Items", "Sealed Weapons",
            "┌─ Items\n" +
            "│ Topic: Sealed Weapons\n" +
            "│ Tier: Legendary (sealed flag set)\n" +
            "│ Weapon type: All 8 Infinity Moment LAB drops\n" +
            "│ Source: F85-F99 floor-boss LAB hook\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "A subset of late-game LAB weapons ship sealed: their stats\n" +
            "cannot be scaled further. The Anvil and Reforge menus mark\n" +
            "them [SEALED] and block any attempt with a clear message.\n\n" +
            "MECHANICS\n" +
            "Enhance and Reforge are blocked. Repair and Evolve (if on a\n" +
            "chain) still work. Refinement sockets still accept ingots.\n" +
            "Sealed roster (all 8 current IM LAB drops):\n" +
            "  Zephyros, Sacred Cross, Glow Haze, Saku, Mirage Knife,\n" +
            "  Northern Light, Lunatic Roof, Artemis.\n" +
            "Remains Heart stays enhanceable (canon Lisbeth masterwork\n" +
            "exception).\n\n" +
            "TIPS\n" +
            "Pair a sealed LAB with heavy Refinement — socket 3 Astral or\n" +
            "Chimeric Ingots to recover the ATK you can't enhance in. Dual\n" +
            "Blades users can mix a sealed main-hand with an enhanceable\n" +
            "off-hand for scaling on one side.\n\n" +
            "SEE ALSO\n" +
            "[Infinity Moment Last Attack Bonus Weapons] · [Anvil — Repair, Enhance, Evolve, Refine] · [Reforge — Re-roll Random Bonuses] · [Weapon Refinement System]")
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
            "[Infinity Moment Shop Weapons] · [Vendors — Rotating Stock] · [Vendor Investing] · [Ascending a Floor] · [Floor Scaling Formulas] · [Gear Compare]")
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
            "[Floor 65] · [Paired Dual-Wield Weapons] · [Divine Object Set — Integrity Knights] · [Memory Defrag Originals] · [Named Legendary Highlights]")
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
            "[Floor 55] · [Paired Dual-Wield Weapons] · [Elemental Weapon Variants] · [Named Legendary Highlights]")
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
            "[Floor 78] · [Divine Object Set — Integrity Knights] · [Alicization Lycoris Raid Weapons] · [SAO Lost Song Named Weapons]")
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
            "[Named Legendary Highlights] · [Avatar Weapons & Last-Attack Bonus] · [Material Tiers (Baseline)]")
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
            "                    other turn for 3 turns (wired;\n" +
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
            "[Damage Formula] · [Critical Hits] · [Status: Bleed & Poison] · [Status: Stun & Slow] · [Weapon Proficiency Ranks] · [Weapon Types Overview] · [Floor Scaling Formulas]")
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
            "[Quest Types & Rewards] · [Floor 1] · [Vendors — Rotating Stock] · [Save System] · [Quest Tracker]")
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
            "[Accepting & Completing Quests] · [Quest Types & Rewards] · [Floor 1] · [Controls & Keybindings]")
        {
            Tags = new[] { "quests", "ui", "hud" }
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
            "[SAO Switch (Party)] · [Floor 1] · [Guild System Overview] · [Run Modifiers (12 Optional Challenges)] · [Combo Attacks]")
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
            "[Karma & Alignment] · [Floor 55] · [Floor 1] · [Floor 75] · [Player-Founded Guild]")
        {
            Tags = new[] { "guild", "quests", "npcs" }
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
            "[Guild System Overview] · [Karma & Alignment] · [Floor 1] · [Col Economy — How You Earn]")
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
            "PICKAXE STOCK:\n" +
            "  F1-F9    Wooden Pickaxe   ~96 Col post-markup (base 80)\n" +
            "  F10-F50  Iron Pickaxe     ~384 Col post-markup (base 320)\n" +
            "  F51+     no pickaxe slot  Mithril is world-find only\n" +
            "Mithril Pickaxes do NOT appear in any vendor stock — you must\n" +
            "loot them from chests, ore-cluster rooms, or quest rewards on\n" +
            "F50+ floors. Wooden + Iron always slot in the per-floor stock\n" +
            "regardless of the random weapon/armor rolls.\n\n" +
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
            "│ Cap: None — collect any/all Divines per run\n" +
            "│ Sources: Floor bosses · Quests · Hidden vault · T4 craft\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Seventeen Divine weapons span the 13 weapon classes. Every\n" +
            "Divine is hand-placed — never rolled on a random chest. There\n" +
            "is no per-run cap on Divine drops; clear every gating boss and\n" +
            "complete every named quest to assemble the full set in a single\n" +
            "run if you can.\n\n" +
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
            "Each Divine demands a high-tier boss kill, a long quest chain,\n" +
            "or a deep T4 craft pipeline — there is no per-run lockout, but\n" +
            "the time/durability/consumable cost of farming the full 17 in\n" +
            "one run is steep on its own.\n\n" +
            "TIPS\n" +
            "Plan a Divine route by floor band and weapon class. Canon-\n" +
            "minded Katana mains sprint for F75 Masamune; 2H mains hold for\n" +
            "F93 Ascalon's dragon-slayer flavor; rapier duelists can grab\n" +
            "Hexagramme at F82 or hold for F50 Heaven-Piercing Blade via\n" +
            "the Sister Azariya chain. The Conflagrant Vault is the only\n" +
            "bow Divine outside T4 craft — prioritize if Archery is your\n" +
            "main weapon track. F98 Black Lily Sword and F97 Time Piercing\n" +
            "Sword are the mythic-tier pair. Sister Selka on F65 can awaken\n" +
            "Divines up to Lv3 for +45% base damage — see entry 'Divine\n" +
            "Awakening'.\n\n" +
            "SEE ALSO\n" +
            "[Divine Objects] · [Divine Object Set — Integrity Knights] · [Named Legendary Highlights] · [Floor Scaling Formulas] · [Weapon Evolution Chains] · [Rarity Tiers & Drop Rates] · [Divine Awakening]")
        {
            Tags = new[] { "items", "weapon", "divine", "rarity", "endgame", "spoiler" }
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
            "Sword ◈2\") tells you the current awakening level at a glance.\n" +
            "Awakening fires a particle burst from the player tile keyed to\n" +
            "the new level — Lv1 = 3 particles / 600ms, Lv2 = 6 / 900ms,\n" +
            "Lv3 = 12 / 1200ms. Particles emit only on the awakening event\n" +
            "(not ambient) and respect the OptionsScreen Particle Density\n" +
            "setting (Off cancels the burst entirely).\n\n" +
            "SEE ALSO\n" +
            "[Divine Weapons — Roster & Acquisition] · [Weapon Refinement System] · [Refinement Ingots] · [Enhancement Ores System] · [Floor Scaling Formulas] · [Advanced Weapon Effects]")
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

        new("Items", "Iron Ingot Enhancement (Lisbeth)",
            "┌─ Items\n" +
            "│ Topic: Iron Ingot Enhancement (Lisbeth)\n" +
            "│ NPC: F48 Lindarth Lisbeth (BrightMagenta 'L')\n" +
            "│ Recipe: 3x iron_ingot + 200 Col → +1 EnhancementLevel\n" +
            "│ Cap: +5 on Common/Uncommon weapons only\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "A low-cost enhancement path for early/mid-game weapons at F48\n" +
            "Lindarth Lisbeth, separate from her endgame Rarity 6 craft\n" +
            "line. Spend 3 iron_ingot + 200 Col per +1 enhancement on a\n" +
            "Common or Uncommon weapon, up to +5. The recipe sidesteps the\n" +
            "Anvil's Enhancement Ore requirement entirely — useful when\n" +
            "you're sitting on iron from Mining but haven't farmed an Ore\n" +
            "biome.\n\n" +
            "MECHANICS\n" +
            "  Cost per +1     3 iron_ingot + 200 Col (flat — no floor scaling)\n" +
            "  Eligibility     Common (T0/T1) and Uncommon (T2) weapons only\n" +
            "  Cap             +5 EnhancementLevel via this recipe\n" +
            "  Stacks with     Anvil Enhance up to the global +10 cap; the\n" +
            "                  Anvil resumes the +6 → +10 push from whatever\n" +
            "                  Lisbeth left it at\n" +
            "  Failure         None — guaranteed +1 per craft (no downgrade\n" +
            "                  risk like the Anvil's +7+ band)\n" +
            "Distinct from the R6 craft line: that's 3M Col + rare mats per\n" +
            "recipe; this is 200 Col + 3 iron_ingot per +1. Different menus,\n" +
            "different flow. High-tier weapons (Rare/Epic/Legendary) still\n" +
            "route through the Anvil + Enhancement Ore path.\n\n" +
            "TIPS\n" +
            "Push Common/Uncommon starter weapons to +5 cheaply on first\n" +
            "F48 visit — a +5 Common weapon with the iron-ingot bumps\n" +
            "carries through F50-F60 longer than a +0 Rare drop. Bank\n" +
            "iron_ingot during Mining grinds: 15 ingots = a +5 push for\n" +
            "1000 Col total, vs ~3500-5000 Col on the Anvil with ores.\n\n" +
            "SEE ALSO\n" +
            "[Lisbeth — Rarity 6 Craft Line] · [Anvil — Repair, Enhance, Evolve, Refine] · [Enhancement Ores System] · [Mining (Life Skill)] · [Floor 48]")
        {
            Tags = new[] { "lisbeth", "crafting", "enhancement", "mining" }
        },

        new("Items", "Legendary Collectables Panel (Shift+L)",
            "┌─ Items\n" +
            "│ Topic: Legendary Collectables — completion tracker\n" +
            "│ Hotkey: Shift+L from the map\n" +
            "│ Total: 184 Legendaries across 9 source buckets\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "A run-long completion tracker for the 184 named Legendary\n" +
            "weapons. Press Shift+L on the map to open the panel; the list\n" +
            "is grouped by canon source bucket (LN, Hollow Fragment,\n" +
            "Integral Factor, etc.) with a per-bucket completion count.\n" +
            "Pickups are recorded automatically — every Legendary that\n" +
            "ever lands in your inventory is marked collected for the rest\n" +
            "of the save.\n\n" +
            "USAGE\n" +
            "Filter keys (inside the panel):\n" +
            "  1   Show all entries\n" +
            "  2   Show only collected entries\n" +
            "  3   Show only not-yet entries\n" +
            "  F1  All floors (default)\n" +
            "  F2  This floor band — current floor ±8\n" +
            "  Esc Close\n\n" +
            "Header counter reads \"Collected: N / 184\" with the same\n" +
            "scope the filters apply.\n\n" +
            "EFFECTS\n" +
            "Source buckets (9):\n" +
            "  LN          Light Novel — canon Aincrad arc\n" +
            "  AL          Alicization Lycoris\n" +
            "  IF          Integral Factor\n" +
            "  HF          Hollow Fragment\n" +
            "  LR          Last Recollection / Lost Song\n" +
            "  MD          Memory Defrag\n" +
            "  FD          Fractured Daydream\n" +
            "  Myth        Mythological (non-SAO references)\n" +
            "  Non-Canon   AincradTRPG-invented blades\n\n" +
            "TIPS\n" +
            "Use the This-Floor filter (F2) before pushing a new floor —\n" +
            "the band ±8 surfaces every Legendary anchored anywhere near\n" +
            "your current depth, so you can plan for chest hunts and\n" +
            "field-boss farms before ascending.\n\n" +
            "SEE ALSO\n" +
            "[Named Legendary Highlights]")
        {
            Tags = new[] { "items", "ui", "collectables" }
        },

        new("Items", "Mithril Ingot Enhance (Rare/Epic, +1..+7)",
            "┌─ Crafting\n" +
            "│ Topic: Mid-tier Lisbeth enhance lane\n" +
            "│ NPC: Lisbeth (Lindarth F48)\n" +
            "│ Cost: 1,000 Col + 3x Mithril Ingot per +1\n" +
            "│ Cap: +7 (this lane); higher tier requires Crystallite\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Mid-tier weapon enhancement at Lisbeth's Lindarth forge.\n" +
            "Accepts Rare and Epic weapons, pushes them up to +7. Sits\n" +
            "between the Common/Uncommon iron lane (+5 cap) and the Epic/\n" +
            "Legendary crystallite lane (+10 cap).\n\n" +
            "USAGE\n" +
            "Open Lisbeth (F48 Lindarth) and press F3 to switch to the\n" +
            "Mithril Ingot Enhance tab. Pick a Rare or Epic weapon from\n" +
            "your inventory; press Confirm to spend mats. Sealed (LAB)\n" +
            "weapons are rejected.\n\n" +
            "COSTS\n" +
            "  1,000 Col + 3x Mithril Ingot per +1 attempt.\n" +
            "  Cap: +7 (this tab; higher requires Crystallite tab F5).\n\n" +
            "TIPS\n" +
            "Mithril is the F30s+ chest/drop ore — push your Rares to +5\n" +
            "with iron first, then transition to mithril for the +5→+7\n" +
            "stretch. Reforge can re-roll the bonuses without changing\n" +
            "the enhance level — see the Reforge entry.\n\n" +
            "SEE ALSO\n" +
            "[Lisbeth — Rarity 6 Craft Line] · [Crystallite Ingot Enhance (Epic/Legendary)] · [Reforge — Re-roll Random Bonuses]")
        {
            Tags = new[] { "crafting", "lisbeth", "enhancement" }
        },

        new("Items", "Crystallite Ingot Enhance (Epic/Legendary, +1..+10)",
            "┌─ Crafting\n" +
            "│ Topic: High-tier Lisbeth enhance lane\n" +
            "│ NPC: Lisbeth (Lindarth F48)\n" +
            "│ Cost: 5,000 Col + 3x Crystallite Ingot per +1\n" +
            "│ Cap: +10 (the new game-wide ceiling)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Top-tier weapon enhancement using Crystallite Ingot — the\n" +
            "F48 Frost Dragon signature drop. Accepts Epic and Legendary\n" +
            "weapons; raises the enhancement cap from the legacy +6 limit\n" +
            "to a new game-wide +10 ceiling.\n\n" +
            "USAGE\n" +
            "Open Lisbeth (F48 Lindarth) and press F5 to switch to the\n" +
            "Crystallite Ingot Enhance tab. Pick an Epic or Legendary\n" +
            "weapon. Sealed (LAB) and Divine weapons are rejected.\n\n" +
            "COSTS\n" +
            "  5,000 Col + 3x Crystallite Ingot per +1 attempt.\n" +
            "  Cap: +10 (game-wide).\n\n" +
            "TIPS\n" +
            "Crystallite Ingot is the F48 Frost Dragon field-boss drop —\n" +
            "you'll bank a stack from the dragon hunt itself. R6 craft\n" +
            "recipes also list crystallite as a high-tier component, so\n" +
            "budget the stack across enhance + R6 craft + reforge.\n\n" +
            "SEE ALSO\n" +
            "[Mithril Ingot Enhance (Rare/Epic)] · [Lisbeth — Rarity 6 Craft Line] · [Reforge — Re-roll Random Bonuses]")
        {
            Tags = new[] { "crafting", "lisbeth", "enhancement" }
        },

        new("Items", "Reforge — Re-roll Random Bonuses",
            "┌─ Crafting\n" +
            "│ Topic: Reforge verb (Lisbeth F4 tab)\n" +
            "│ NPC: Lisbeth (Lindarth F48)\n" +
            "│ Cost: scales by rarity — see table\n" +
            "│ Effect: re-rolls Bonuses; preserves enhance + awakening\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Reforge re-rolls a weapon's random Bonuses within its rarity\n" +
            "tier band. Enhance level, Awakening level, refinement slots,\n" +
            "and sockets are all preserved — only the random stat lines\n" +
            "change. The new roll is independent of the preview shown\n" +
            "before confirmation; accepting locks in whatever the seed\n" +
            "produces.\n\n" +
            "USAGE\n" +
            "Open Lisbeth (F48 Lindarth), press F4 to switch to Reforge.\n" +
            "Pick an eligible weapon. Detail panel shows current bonuses\n" +
            "and a deterministic preview roll. Press Confirm; a second\n" +
            "modal restates before/after and asks for explicit consent.\n\n" +
            "COSTS\n" +
            "  Common      5,000 Col   + 3x Mithril Ingot\n" +
            "  Uncommon   25,000 Col   + 3x Mithril Ingot\n" +
            "  Rare      100,000 Col   + 3x Mithril Ingot\n" +
            "  Epic      250,000 Col   + 5x Crystallite Ingot\n" +
            "  Legendary 1,000,000 Col + 5x Crystallite Ingot\n\n" +
            "INELIGIBLE\n" +
            "  Divine weapons          — bonuses are sealed (canon)\n" +
            "  LAB-sealed weapons      — Last-Attack-Bonus drops\n\n" +
            "TIPS\n" +
            "The preview is informational only — random rolls can wipe\n" +
            "good lines, so think twice before reforging a well-rolled\n" +
            "blade. The cost ladder makes Legendary reforges expensive\n" +
            "by design; spend mithril cheaply on Common-Rare blades to\n" +
            "find the bonus profile you want, then graduate the kit.\n\n" +
            "SEE ALSO\n" +
            "[Mithril Ingot Enhance (Rare/Epic)] · [Crystallite Ingot Enhance (Epic/Legendary)] · [Lisbeth — Rarity 6 Craft Line]")
        {
            Tags = new[] { "crafting", "lisbeth", "reforge" }
        },

        new("Combat & Rarity", "Ranged Fire & the Reticle (\\)",
            "┌─ Combat\n" +
            "│ Topic: Ranged-fire reticle\n" +
            "│ Hotkey: \\ (backslash)\n" +
            "│ Scope: Bow basic-attack + sword skills with Range > 1\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Bows and long-range sword skills now route through a\n" +
            "dedicated reticle — press \\ on the map to enter aim mode,\n" +
            "move the reticle with arrow keys, press Enter to fire. A\n" +
            "right-edge sidebar shows target coords, distance, and\n" +
            "in-range status while aiming.\n\n" +
            "USAGE\n" +
            "  \\ (backslash)   Enter reticle (Bow basic only).\n" +
            "  F1-F4           Activate sword skill — auto-opens reticle\n" +
            "                  if the skill's Range > 1; bump-targets\n" +
            "                  otherwise.\n" +
            "  Arrows / WASD   Move the reticle.\n" +
            "  Enter           Fire on the targeted tile.\n" +
            "  Esc             Exit aim without firing.\n\n" +
            "EFFECTS\n" +
            "  Bow basic — same damage formula as melee, gated by FOV /\n" +
            "  line-of-sight and weapon range. AoE and Counter sword\n" +
            "  skills skip the reticle (no single-target).\n\n" +
            "TIPS\n" +
            "Stunned, mid-motion, or on-cooldown skills auto-cancel reticle\n" +
            "entry — eligibility is rechecked at fire. If the target falls\n" +
            "out of FOV/LOS during aim, fire is rejected at confirm time.\n\n" +
            "SEE ALSO\n" +
            "[Look Mode & Counter Stance] · [Sword Skills — Per-Weapon Lists] · [Critical Hits]")
        {
            Tags = new[] { "combat", "ranged", "controls" }
        },

        new("Items", "Slicing Stones — Alt Evolution Paths",
            "┌─ Items\n" +
            "│ Topic: Slicing Stones (Lesser / Greater / Perfect)\n" +
            "│ Use: re-route a chain weapon to its canonical alternate tier\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Slicing Stones unlock alternate evolution paths for chain\n" +
            "weapons — three tiers (Lesser, Greater, Perfect) corresponding\n" +
            "to the three canon T1/T2/T3 chain tiers. Use a Slicing Stone\n" +
            "during evolution to swap the upgrade target from the default\n" +
            "canon path to the alt-canon variant.\n\n" +
            "TIERS\n" +
            "  Lesser  ◊ BrightCyan      — T1 alt-route trigger (Rare)\n" +
            "  Greater ◈ BrightMagenta   — T2 alt-route trigger (Epic)\n" +
            "  Perfect ✦ BrightYellow    — T3 alt-route trigger (Legendary)\n\n" +
            "USAGE\n" +
            "When evolving an eligible chain weapon, the evolve dialog\n" +
            "offers two confirm paths if a matching Slicing Stone is in\n" +
            "inventory: [Confirm Canon] for the default chain target, or\n" +
            "[Confirm Slicing Stone Alt] for the alt-canon variant.\n\n" +
            "TIPS\n" +
            "Coverage spans all 9 canon chains (1HS/2HS/Rapier/Scimitar/\n" +
            "Dagger/Katana/Spear/Mace/2H Axe) plus the Anneal Blade extension\n" +
            "line. Slicing Stones don't bypass tier requirements — the\n" +
            "input weapon must already qualify for evolution.\n\n" +
            "SEE ALSO\n" +
            "[Weapon Evolution Chains] · [Lisbeth — Rarity 6 Craft Line]")
        {
            Tags = new[] { "items", "evolution", "stones" }
        },

        new("Items", "Footstep Trail Settings",
            "┌─ Controls & Keybindings\n" +
            "│ Topic: Footstep trail customization\n" +
            "│ Settings: Style + Length + Opacity\n" +
            "│ Toggle: OptionsScreen (Display section)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "The footstep trail behind your avatar is now fully tunable:\n" +
            "pick a glyph style, a trail length (in turns), and an opacity\n" +
            "tier. All three settings persist across runs in the global\n" +
            "settings.json.\n\n" +
            "OPTIONS\n" +
            "  Style    Off / Dots · / Dashes - / Paws \" / Boots : / Chevrons ^\n" +
            "  Length   Off / 5 / 10 / 20 / 50 / Unlimited (1000-turn ceiling)\n" +
            "  Opacity  Subtle (DarkGray) / Medium (Gray) / Bold (White)\n\n" +
            "TIPS\n" +
            "\"Unlimited\" is capped at 1000 turns to keep render cost flat\n" +
            "even on very long sessions. If you find the trail dominates\n" +
            "the map, drop opacity to Subtle and length to 5 — visible\n" +
            "during back-tracking decisions, invisible during combat.\n\n" +
            "SEE ALSO\n" +
            "[Look Mode & Counter Stance]")
        {
            Tags = new[] { "controls", "footsteps", "options" }
        },

        new("Combat & Rarity", "Status Effect Abbreviations",
            "┌─ Combat\n" +
            "│ Topic: Sidebar status row — 3-4 letter codes\n" +
            "│ Update: multi-glyph status icons\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "The sidebar status row now uses 3-4 letter abbreviations in\n" +
            "place of single-character icons — clearer at a glance and\n" +
            "consistent with the in-game log tags. Each cell shows\n" +
            "[ABBR] in the effect's color, with the remaining-turn count\n" +
            "directly below.\n\n" +
            "ABBREVIATIONS\n" +
            "  BLD   Bleed             (BrightRed)\n" +
            "  PSN   Poison            (BrightGreen)\n" +
            "  STN   Stun              (BrightYellow)\n" +
            "  SLW   Slow              (BrightCyan)\n" +
            "  SHRN  Shrine Buff       (BrightYellow)\n" +
            "  SRG   Level-Up Surge    (BrightGreen)\n" +
            "  REGN  Food Regen        (BrightGreen)\n" +
            "  INV   Invisibility      (White)\n\n" +
            "TIPS\n" +
            "On narrow sidebars (under 24 cells), the row falls back to\n" +
            "single-letter form (B/P/S/etc.) — same color, less width. The\n" +
            "log tag style remains [BLD]/[PSN]/etc. for consistency with\n" +
            "the sidebar.\n\n" +
            "SEE ALSO\n" +
            "[Bleed Effect] · [Poison Effect] · [Stun Effect]")
        {
            Tags = new[] { "combat", "status", "ui" }
        },

        new("Items", "Equipment Compare Panel",
            "┌─ Items\n" +
            "│ Topic: Inventory equipment-compare panel\n" +
            "│ Trigger: arrow-keys on equipment in Inventory\n" +
            "│ Footprint: bottom 4-row panel\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "When you highlight a weapon or armor in the inventory list,\n" +
            "a 4-row compare panel auto-renders at the bottom of the\n" +
            "dialog showing the diff against your currently-equipped\n" +
            "counterpart. The panel hides itself if no item is equipped\n" +
            "in the relevant slot.\n\n" +
            "PANEL CONTENT\n" +
            "  Row 1   \"vs equipped: <name>\" header\n" +
            "  Row 2   Stat deltas: DMG +6  ATK +2  DEX -1\n" +
            "  Row 3   Special-effect diff: +CritHeal  -Lifesteal\n" +
            "  Row 4   Net verdict: [UPGRADE] / [DOWNGRADE] / [SIDEGRADE]\n\n" +
            "TIPS\n" +
            "Type-mismatch (different weapon class or armor slot) shows\n" +
            "a single-line banner instead of stat deltas — that's the\n" +
            "compare engine telling you the apples-to-oranges comparison\n" +
            "would be misleading. Move to a same-slot piece for a real\n" +
            "diff.\n\n" +
            "SEE ALSO\n" +
            "[Equipment Slots & Type] · [Weapon Type & Affinity]")
        {
            Tags = new[] { "items", "ui", "compare" }
        },

        new("Items", "Player Guide Search & Navigation",
            "┌─ Controls & Keybindings\n" +
            "│ Topic: Search box + keybind reference\n" +
            "│ Hotkey: / opens live search\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Press / to open the search bar at the top of the Player\n" +
            "Guide. Type to filter the topic tree live — title hits\n" +
            "weighted higher than body hits. The bar stays visible after\n" +
            "first activation; first Esc clears the query, second Esc\n" +
            "closes the dialog.\n\n" +
            "USAGE\n" +
            "  /         Open search bar (autofocuses input)\n" +
            "  type      Filter live as you type\n" +
            "  Enter     Keep results, hand focus back to topic tree\n" +
            "  Esc       Clear query (1st press) / close dialog (2nd)\n" +
            "  ↑↓        Navigate topics\n" +
            "  Tab       Cycle focus (tree / recent / bookmarks / body)\n" +
            "  1-5       Jump to category\n" +
            "  b         Bookmark / unbookmark current topic\n" +
            "  Bksp      History back\n" +
            "  ?         Show keybind overlay\n" +
            "  e         Export run summary to clipboard\n\n" +
            "TIPS\n" +
            "Search is fuzzy: \"refor\" matches Reforge; \"liz\" matches\n" +
            "Lisbeth. Title hits dominate body hits 3:1 in the score so\n" +
            "the most relevant topic typically lands at top.\n\n" +
            "SEE ALSO\n" +
            "[Look Mode & Counter Stance]")
        {
            Tags = new[] { "controls", "ui", "search" }
        },

        // ── Floors (per-floor entries; gated by LifetimeStats.MaxFloorReached) ──

        new("Floors", "Floor 1",
            "┌─ Floors\n" +
            "│ Topic: Floor 1\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Grassland\n" +
            "│ Boss: Illfang the Kobold Lord (Tyrant of the First Floor)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "The opening floor and Aincrad's largest. A walled hub city, the\n" +
            "Town of Beginnings, occupies the south plaza; everything outside\n" +
            "is open grassland sloping into kobold-haunted hills. Map size is\n" +
            "1000x1000 — every later floor shrinks toward the F99 100x100.\n" +
            "\n" +
            "BOSS\n" +
            "Illfang the Kobold Lord — single Power Strike phase 2 ability\n" +
            "(1.8x). Lv12, ~180 HP at this floor's curve. Bring slash damage\n" +
            "and a positioning skill; the arena is wide enough to kite.\n" +
            "\n" +
            "DROPS\n" +
            "No guaranteed drop. No Last-Attack Bonus weapon. Chest pool is\n" +
            "Tier-1 commons + the occasional uncommon. The Mate Chopper\n" +
            "Legendary can roll from F1 elite kobolds — F1 carries two\n" +
            "Legendaries' worth of luck-band loot, so don't skip the open\n" +
            "fields.\n" +
            "\n" +
            "NPCS / QUESTS\n" +
            "Town of Beginnings is the entire ground floor of the game's\n" +
            "social layer:\n" +
            "  - Klein (samurai, future Fuurinkazan founder)\n" +
            "  - Argo the Rat (info broker, cheapest tips in Aincrad)\n" +
            "  - Agil (vendor, becomes the F50 trader later)\n" +
            "  - Lisbeth & Silica (flavor — they level up before you meet\n" +
            "    them at F48 / F35)\n" +
            "  - Diavel (raid commander, Illfang strategy)\n" +
            "  - Kibaou (recruits the Aincrad Liberation Force at the plaza\n" +
            "    fountain — Lv1+ and karma >= -30 to join, +5% XP and +2 to\n" +
            "    every stat after sign-up)\n" +
            "  - Priest Tadashi, Nezha (side flavor)\n" +
            "Quest hooks: First Day chain, ALF recruitment, Anneal Blade\n" +
            "craft line out of Horunka village.\n" +
            "\n" +
            "Monument of Swordsmen sits in the south plaza park — a tall\n" +
            "BrightYellow M tile. Step on it to open the species kill log\n" +
            "and pick the active title shown on your name plate.\n" +
            "\n" +
            "Town Guard outlaw response: drop your karma to -50 or below\n" +
            "and crossing back into the plaza spawns 3-5 Lv20 BrightBlue G\n" +
            "guards. They are not optional and they hit hard.\n" +
            "\n" +
            "CANON\n" +
            "SAO LN vol 1 / anime S1E1-2 / Progressive vol 1: Diavel's raid\n" +
            "kills Illfang at the cost of his life when the LN-canon four-bar\n" +
            "boss switches from talwar to nodachi at the last bar. The Town\n" +
            "of Beginnings, Black Iron Palace, and Monument of Life are all\n" +
            "first-floor canon landmarks; the Monument of Swordsmen is this\n" +
            "build's hub-tile cousin of the Monument of Life.\n" +
            "\n" +
            "TIPS\n" +
            "Do the ALF recruit early — that +5% XP banks for every later\n" +
            "floor. Stand on the Monument of Swordsmen before each run to\n" +
            "lock the title that fits your build. F1 is the only floor\n" +
            "where prefab placement is fully suppressed (Town of Beginnings\n" +
            "is the prefab); explore the open fields, not the labyrinth\n" +
            "wing, for early loot.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor 2] · [Floor 50] · [Floor Scaling Formulas] · [Karma & Alignment]")
        {
            Tags = new[] { "floors", "f1", "canon", "town", "monument", "guild" }
        },

        new("Floors", "Floor 2",
            "┌─ Floors\n" +
            "│ Topic: Floor 2\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Grassland (savanna / mesa)\n" +
            "│ Boss: Asterius the Taurus King (Sovereign of the Labyrinth)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Wind-stripped plateau, the Taurus plains. The cylindrical stone\n" +
            "town of Urbus crowns a mesa; below, Bullbous Bow herds drift\n" +
            "the savanna. The first floor where unarmed builds get a real\n" +
            "playground.\n" +
            "\n" +
            "BOSS\n" +
            "Asterius the Taurus King (Lv14, ~212 HP). Progressive canon\n" +
            "fight — Asterius is flanked by Baran the General Taurus and\n" +
            "Nato the Colonel Taurus in lore; in-engine he is the single\n" +
            "boss entity. Standard Power Strike phase-2 ability. Bring\n" +
            "blunt or slash; the boss room is open enough to circle.\n" +
            "\n" +
            "DROPS\n" +
            "Field boss Bullbous Bow (Armored Terror of the Plains) roams\n" +
            "the plateau and gates the labyrinth entrance — kill drops the\n" +
            "Bullbous Horn material guaranteed. No labyrinth-boss LAB drop;\n" +
            "chest pool stays Tier-1.\n" +
            "\n" +
            "NPCS / QUESTS\n" +
            "Ran the Brawler — Progressive canon, hub of Urbus. Ran's Trial\n" +
            "is the Martial Arts unique-skill quest: 5 unarmed kills against\n" +
            "F2 beasts. Reward: Martial Arts unique skill flag + 200 Col +\n" +
            "150 XP. The actual unlock condition for Martial Arts is 30\n" +
            "lifetime unarmed kills — Ran's quest just frames the canon.\n" +
            "\n" +
            "CANON\n" +
            "Progressive vol 2: Asterius is the second-floor boss of the\n" +
            "PV beta tester era; Ran is the canon Martial Arts trial giver,\n" +
            "the only NPC in PV who teaches you to fight without a weapon.\n" +
            "\n" +
            "TIPS\n" +
            "Don't skip Ran's Trial even on a sword build — the quest XP\n" +
            "and Col are the easiest of any F2 NPC. Bullbous Bow drops the\n" +
            "horn whether you finish or an ally does, but you want the\n" +
            "killing blow on Asterius for the boss-room loot ring.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor 1] · [Floor 3] · [Unique Skill: Martial Arts] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f2", "canon", "unique-skills" }
        },

        new("Floors", "Floor 3",
            "┌─ Floors\n" +
            "│ Topic: Floor 3\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Forest (deep, twilight canopy)\n" +
            "│ Boss: Nerius the Evil Treant (The Rotting Ancient)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Perpetual twilight under the canopy. The hollowed trio of\n" +
            "baobabs known as Zumfut shelter the trade road; out past it,\n" +
            "the Forest Elf and Dark Elf war begins. Plant-type aggression\n" +
            "is up across the floor.\n" +
            "\n" +
            "BOSS\n" +
            "Nerius the Evil Treant (Lv16, ~244 HP). Plant-type — fire and\n" +
            "edged weapons cut best. Standard scaling-curve ability set\n" +
            "(Power Strike phase 2).\n" +
            "\n" +
            "DROPS\n" +
            "No guaranteed labyrinth-boss drop. No Last-Attack Bonus weapon.\n" +
            "Tier-1 chest pool with low rare chance.\n" +
            "\n" +
            "NPCS / QUESTS\n" +
            "Kizmel the Dark Elf — Progressive canon companion, found at\n" +
            "an elf camp west of the trade road. Her questline carries\n" +
            "across F3 and F4 in canon; in this build she is flavor.\n" +
            "\n" +
            "CANON\n" +
            "Progressive vols 2-3: F3 opens the Elf War quest chain. Zumfut\n" +
            "(town hollowed into three baobab trees) and Yofel Castle frame\n" +
            "the floor. Kizmel is the player's first Dark Elf ally and a\n" +
            "fixture of Progressive's early arc.\n" +
            "\n" +
            "TIPS\n" +
            "Vision is shorter under the canopy — keep a torch or fire\n" +
            "Crystal in inventory. Plant mobs ignite if you stack burn from\n" +
            "an enchanted blade.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor 2] · [Floor 4] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f3", "canon" }
        },

        new("Floors", "Floor 4",
            "┌─ Floors\n" +
            "│ Topic: Floor 4\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Aquatic (lake / canals)\n" +
            "│ Boss: Wythege the Hippocampus (Serpent of the Drowned Cavern)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Rovia the canal town. Most of the floor is flooded — gondolas\n" +
            "are the canon way around. Step into open water and the biome\n" +
            "rules apply: 5% slip per move and -3 ATK while wet.\n" +
            "\n" +
            "BOSS\n" +
            "Wythege the Hippocampus (Lv18, ~278 HP). Aquatic seahorse-form\n" +
            "boss in a water-themed arena studded with Bog Water tiles.\n" +
            "Lightning carries through the wet floor — bring it.\n" +
            "\n" +
            "DROPS\n" +
            "No guaranteed labyrinth-boss drop. Aquatic mob pool (Water\n" +
            "Drake, Lakeshore Crab, Giant Clam, Water Wight, Scavenger\n" +
            "Toad) has CanSwim set on this floor — they'll cross water you\n" +
            "wouldn't expect to.\n" +
            "\n" +
            "NPCS / QUESTS\n" +
            "Romolo the Shipwright of Yore — canon Rovia gondolier. The\n" +
            "dam-breaking quest chain starts in the canal district.\n" +
            "\n" +
            "CANON\n" +
            "Progressive vol 3: Rovia is canonically a Venice-styled water\n" +
            "town. Beta-test memory has it as a sandy canyon, but the live\n" +
            "version is the lake floor.\n" +
            "\n" +
            "TIPS\n" +
            "Don't fight in standing water unless your kit handles slip\n" +
            "rolls. The Water Wight's drain attack ignores armor — kill it\n" +
            "first in any mixed pack.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor 3] · [Floor 5] · [Biomes] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f4", "canon", "aquatic", "swimming" }
        },

        new("Floors", "Floor 5",
            "┌─ Floors\n" +
            "│ Topic: Floor 5\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Ruins\n" +
            "│ Boss: Fuscus the Vacant Colossus (The Hollow Giant)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Karluin and the Pitch-Black Cathedral. The Ruins biome's\n" +
            "decay pass eats walls and grows scrub through the floor —\n" +
            "open cathedral nave one minute, broken nave with bushes next.\n" +
            "Undead show up here for the first time.\n" +
            "\n" +
            "BOSS\n" +
            "Fuscus the Vacant Colossus (Lv20, ~312 HP). Stone golem;\n" +
            "blunt damage does best, edged glances. F5 is also the band\n" +
            "where bosses pick up Call Reinforcements as a second ability —\n" +
            "expect adds in phase 2.\n" +
            "\n" +
            "DROPS\n" +
            "No guaranteed labyrinth-boss drop. Tier-1/2 chest pool.\n" +
            "\n" +
            "CANON\n" +
            "Progressive vol 4 / Integral Factor: Fuscus is the IF F5\n" +
            "boss; the Pitch-Black Cathedral and catacombs are PV4 canon.\n" +
            "Halloween Haunted Maze (Hollow Realization) reskins this\n" +
            "floor in seasonal events.\n" +
            "\n" +
            "TIPS\n" +
            "Kill the summoned adds before they reach you — Fuscus's adds\n" +
            "stack damage faster than the boss does. The decay pass is\n" +
            "deterministic per seed, so a remembered shortcut from a prior\n" +
            "run still exists.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor 4] · [Floor 6] · [Biomes] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f5", "canon" }
        },

        new("Floors", "Floor 6",
            "┌─ Floors\n" +
            "│ Topic: Floor 6\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Swamp\n" +
            "│ Boss: The Irrational Cube (Enigma of the Sixth Floor)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Marsh, fog, kobold mining shafts. The Swamp biome takes 5 off\n" +
            "your sight range and rolls 8% step poison through the muck.\n" +
            "The boss is a puzzle, not a slugfest.\n" +
            "\n" +
            "BOSS\n" +
            "The Irrational Cube (Lv22, ~348 HP). Progressive canon — six\n" +
            "faces, randomized damage type per face it shows. Read the\n" +
            "presented face, swap to the resist or weakness it implies, and\n" +
            "go again. Bull-rushing the cube punishes most kits hard.\n" +
            "\n" +
            "DROPS\n" +
            "No guaranteed labyrinth-boss drop. Cube fight rewards Tier-1/2\n" +
            "chest pulls.\n" +
            "\n" +
            "CANON\n" +
            "Progressive: F6 is the canon puzzle floor; the Cube's\n" +
            "shifting-face mechanic is the lore-anchor for elemental\n" +
            "swap-coverage builds.\n" +
            "\n" +
            "TIPS\n" +
            "Carry one weapon of every common element you can stomach —\n" +
            "this is the floor that pays for that breadth. Stockpile\n" +
            "antidote crystals before the descent; the swamp's poison\n" +
            "ticks add up.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor 5] · [Floor 7] · [Biomes] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f6", "canon" }
        },

        new("Floors", "Floor 7",
            "┌─ Floors\n" +
            "│ Topic: Floor 7\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Forest (rainforest)\n" +
            "│ Boss: The Storm Magnus Duo (Twin Sentinels of the Stone Keep)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Lectea — the canon sky floor of cloud seas and stone\n" +
            "platforms; in this build it renders as a dense rainforest.\n" +
            "The Dogma thieves' lair sits somewhere in the canopy. PKer\n" +
            "guild presence is up; carry a teleport crystal.\n" +
            "\n" +
            "BOSS\n" +
            "The Storm Magnus Duo (Lv24, ~384 HP). Twin sentinels —\n" +
            "Progressive canon two-body fight. Splitting damage between\n" +
            "them keeps both at half HP and wastes burst; focus one down\n" +
            "first while the other repositions.\n" +
            "\n" +
            "DROPS\n" +
            "No guaranteed labyrinth-boss drop.\n" +
            "\n" +
            "CANON\n" +
            "Progressive vols 5-6: Lectea is sky platforms over a cloud\n" +
            "sea; Dogma is the floor's PKer arc.\n" +
            "\n" +
            "TIPS\n" +
            "Park the second Magnus in a corridor while you finish the\n" +
            "first. AoE feels efficient — it isn't, you'll just heal-race\n" +
            "the duo.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor 6] · [Floor 8] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f7", "canon" }
        },

        new("Floors", "Floor 8",
            "┌─ Floors\n" +
            "│ Topic: Floor 8\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Ice (winter / flooded forest)\n" +
            "│ Boss: Wadjet the Flaming Serpent (Inferno of the Eighth Floor)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Frieven is canonically a coastal-forest village with salt fog\n" +
            "mornings; this build's biome system runs F8 as ice — winter\n" +
            "labyrinths, frozen pine, 12% slip per move. The boss bucks the\n" +
            "biome and arrives on fire.\n" +
            "\n" +
            "BOSS\n" +
            "Wadjet the Flaming Serpent (Lv26, ~422 HP). Fire-element\n" +
            "snake on ice tiles — the floor's terrain works with you, not\n" +
            "against you. Cold or water damage cuts best. The boss heat\n" +
            "thaws ice tiles into water mid-fight; repositioning matters.\n" +
            "\n" +
            "DROPS\n" +
            "No guaranteed labyrinth-boss drop.\n" +
            "\n" +
            "CANON\n" +
            "Progressive vol 6: Frieven is the post-Dogma quiet floor —\n" +
            "low player presence, recovery tone. The fire-on-ice flavor is\n" +
            "this build's invention.\n" +
            "\n" +
            "TIPS\n" +
            "Stand on the unmelted ice — Wadjet's tail-sweep crit window\n" +
            "shrinks if it has to slide.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor 7] · [Floor 9] · [Biomes] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f8", "canon" }
        },

        new("Floors", "Floor 9",
            "┌─ Floors\n" +
            "│ Topic: Floor 9\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Forest (enchanted)\n" +
            "│ Boss: Cagnazzo the Toad Demon (Tyrant of the Drowned Vault)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Stachion the puzzle-town. NPC doors lock behind riddles,\n" +
            "stone-golem labyrinths weave through the underbrush, and a\n" +
            "cursed-mayor questline runs the social plot. The boss waits\n" +
            "in the drowned vault below the town.\n" +
            "\n" +
            "BOSS\n" +
            "Cagnazzo the Toad Demon (Lv28, ~460 HP). Progressive canon —\n" +
            "swallow attack with a long tell, jump-pound that water-stuns\n" +
            "in radius 1. Strike from melee range while it's recovering\n" +
            "from a swallow miss.\n" +
            "\n" +
            "DROPS\n" +
            "No guaranteed labyrinth-boss drop.\n" +
            "\n" +
            "CANON\n" +
            "Progressive vol 7: Stachion's cursed mayor and the riddle\n" +
            "doors are the canon plot beats; Cagnazzo is the vault boss.\n" +
            "\n" +
            "TIPS\n" +
            "Don't refuse the riddle quests — they unlock the boss-room\n" +
            "stairs without a long backtrack. Stand-still skills go badly\n" +
            "during the swallow attack; have a movement skill cued.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor 8] · [Floor 10] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f9", "canon" }
        },

        new("Floors", "Floor 10",
            "┌─ Floors\n" +
            "│ Topic: Floor 10\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Urban (traditional Japanese village)\n" +
            "│ Boss: Kagachi the Samurai Lord (Blade of the Rising Moon)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Taft — Aincrad's Edo-flavored mountain town. Wooden alleys,\n" +
            "lantern stalls, no natural floor hazards. F10 is the band\n" +
            "where bosses replace Power Strike with Ground Slam (radius 2,\n" +
            "1.3x AoE). Big swing-room labyrinth.\n" +
            "\n" +
            "BOSS\n" +
            "Kagachi the Samurai Lord (Lv30, ~500 HP). Integral Factor\n" +
            "canon — giant reptilian in samurai armor with a katana.\n" +
            "Wide horizontal slashes and a Ground Slam AoE; piercing or\n" +
            "lightning damage cuts armor. Don't bunch the party — the AoE\n" +
            "hits radius 2.\n" +
            "\n" +
            "DROPS\n" +
            "No guaranteed labyrinth-boss drop. Tier-2 chest pool opens up.\n" +
            "\n" +
            "NPCS / QUESTS\n" +
            "The Moonlit Black Cats — Keita's small guild, headquartered\n" +
            "on F10. Recruit gate: Lv5+ and karma >= -20. Bonuses on join:\n" +
            "+5 VIT and +3 DEF, plus a guild-buff aura when partied with\n" +
            "Black Cats members. Sachi is the soft-voiced spear member you\n" +
            "are most likely to befriend. The 'One More Floor' signature\n" +
            "quest from Keita resolves at this floor on F25 entry — the\n" +
            "clear is the canon promise the guild is celebrating.\n" +
            "\n" +
            "Open the chat with Keita early in your run. The Black Cats\n" +
            "fate triggers on F27 entry, force-leaves you, and costs -5\n" +
            "karma. There is no save-state path that prevents it; the\n" +
            "tragedy is canon-mandatory.\n" +
            "\n" +
            "CANON\n" +
            "SAO LN vol 2 / anime ep 3 — Black Cats arc origin floor.\n" +
            "Keita and the Black Cats are the start of Kirito's grief\n" +
            "thread. Kagachi the Samurai Lord is Integral Factor's F10 boss.\n" +
            "\n" +
            "TIPS\n" +
            "Recruit the Black Cats. Take the F10-F26 guild buff and the\n" +
            "+5 VIT for the entire mid-tier climb. Save your strongest\n" +
            "burst skill for Kagachi's Ground Slam wind-down — that is\n" +
            "the one consistent crit window in the fight.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor 9] · [Floor 11] · [Floor 25] · [Floor 27] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f10", "canon", "guild" }
        },

        new("Floors", "Floor 11",
            "┌─ Floors\n" +
            "│ Topic: Floor 11\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Desert\n" +
            "│ Boss: Felos the Ember Drake (Wings of Living Flame)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "First desert in the climb. Heat and grit raise your satiety\n" +
            "drain by 1 per turn — pack water. The first floor that\n" +
            "guarantees a Divine Beast drop on the labyrinth boss.\n" +
            "\n" +
            "BOSS\n" +
            "Felos the Ember Drake (Lv32, ~540 HP). Flying fire-breath\n" +
            "drake, ranged threat at the top of the boss arena. Stay close\n" +
            "or take cover — the breath cone has long range and ignores\n" +
            "low walls. Cold or water cuts best.\n" +
            "\n" +
            "DROPS\n" +
            "- Guaranteed: Starfall — Divine Beast bow. Drops on any kill,\n" +
            "  not Last-Attack-locked. Bow users lock the floor for the\n" +
            "  Divine slot here.\n" +
            "- Tier-2 chest pool.\n" +
            "\n" +
            "TIPS\n" +
            "Bring water flasks; the satiety drain stacks with hot-tile\n" +
            "ticks if any walls of the labyrinth carry residual fire from\n" +
            "Felos's breath. Starfall pairs well with a melee carry —\n" +
            "consider a two-character party.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor 10] · [Floor 12] · [Divine Weapons — Roster & Acquisition] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f11", "canon", "divine", "loot" }
        },

        new("Floors", "Floor 12",
            "┌─ Floors\n" +
            "│ Topic: Floor 12\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Aquatic (seashore)\n" +
            "│ Boss: Volcanus the Molten King (Lord of the Slag Throne)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-1 scaling band, no canon anchor — the floor follows the\n" +
            "scaling curve. Biome: aquatic seashore (see [Biomes]).\n" +
            "\n" +
            "BOSS\n" +
            "Volcanus the Molten King (Lv34, ~582 HP). Standard\n" +
            "scaling-curve boss; no canonical fight mechanics.\n" +
            "\n" +
            "TIPS\n" +
            "Wet sand reduces footing — slow walks where possible.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor Scaling Formulas] · [Biomes] · [Floor 11] · [Floor 13]")
        {
            Tags = new[] { "floors", "f12" }
        },

        new("Floors", "Floor 13",
            "┌─ Floors\n" +
            "│ Topic: Floor 13\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Volcanic\n" +
            "│ Boss: Charion the Ash Wraith (Specter of Cinder)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-1 scaling band, no canon anchor. Volcanic biome — 2 dmg\n" +
            "per 8 turns environment tick (see [Biomes]).\n" +
            "\n" +
            "BOSS\n" +
            "Charion the Ash Wraith (Lv36, ~624 HP). Standard scaling-curve\n" +
            "boss; no canonical fight mechanics.\n" +
            "\n" +
            "TIPS\n" +
            "Carry burn-resist Crystals. The volcanic tick stacks on top of\n" +
            "any direct fire damage in the fight.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor Scaling Formulas] · [Biomes] · [Floor 12] · [Floor 14]")
        {
            Tags = new[] { "floors", "f13" }
        },

        new("Floors", "Floor 14",
            "┌─ Floors\n" +
            "│ Topic: Floor 14\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Forest (dense)\n" +
            "│ Boss: Ignaroth the Infernal (Demon of the Burning Halls)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-1 scaling band, no canon labyrinth-boss anchor. Forest\n" +
            "biome — dense canopy, short sight. The floor's distinctive\n" +
            "feature is the Integral Factor field boss.\n" +
            "\n" +
            "BOSS\n" +
            "Ignaroth the Infernal (Lv38, ~668 HP). Standard scaling-curve\n" +
            "boss; no canonical fight mechanics.\n" +
            "\n" +
            "DROPS\n" +
            "Field boss Starlight Sentinel (Guardian of the Integral Dawn)\n" +
            "roams the floor and is the IF Integral series anchor —\n" +
            "guaranteed Integral Arc Angel (Epic bow) plus Shield Fermat\n" +
            "(Epic). The IF Integral chest band runs F14-F25.\n" +
            "\n" +
            "TIPS\n" +
            "Skip the labyrinth boss-rush on a fresh run; clear Starlight\n" +
            "Sentinel first for the Epic bow, then descend.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor Scaling Formulas] · [Floor 13] · [Floor 15] · [Floor 25] · [Integral Factor Weapon Series]")
        {
            Tags = new[] { "floors", "f14", "integral-factor", "field-boss" }
        },

        new("Floors", "Floor 15",
            "┌─ Floors\n" +
            "│ Topic: Floor 15\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Volcanic\n" +
            "│ Boss: Surtr the Flame Giant (The Floor-Scorcher)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-1 scaling band, no canon anchor. Volcanic biome at its\n" +
            "crimson-era peak — long fissures, ash drifts, and the same\n" +
            "tick-damage rules as F13 (see [Biomes]).\n" +
            "\n" +
            "BOSS\n" +
            "Surtr the Flame Giant (Lv40, ~712 HP). Standard scaling-curve\n" +
            "boss; no canonical fight mechanics.\n" +
            "\n" +
            "TIPS\n" +
            "Burn-resist Crystals carry the floor; the giant's swing arc\n" +
            "is wide but slow.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor Scaling Formulas] · [Biomes] · [Floor 14] · [Floor 16]")
        {
            Tags = new[] { "floors", "f15" }
        },

        new("Floors", "Floor 16",
            "┌─ Floors\n" +
            "│ Topic: Floor 16\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Ice (crystal era)\n" +
            "│ Boss: Crystalis the Frost Weaver (Architect of the Ice Labyrinth)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-1 scaling band, no canon anchor. The ice band opens —\n" +
            "F16 through F20 all run cold biomes with slip checks and\n" +
            "shorter combat ranges.\n" +
            "\n" +
            "BOSS\n" +
            "Crystalis the Frost Weaver (Lv42, ~758 HP). Standard\n" +
            "scaling-curve boss; no canonical fight mechanics.\n" +
            "\n" +
            "TIPS\n" +
            "The ice biome rewards patience — let melee come to you, kite\n" +
            "across thawed water lanes when the boss tries to charge.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor Scaling Formulas] · [Biomes] · [Floor 15] · [Floor 17]")
        {
            Tags = new[] { "floors", "f16" }
        },

        new("Floors", "Floor 17",
            "┌─ Floors\n" +
            "│ Topic: Floor 17\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Ice\n" +
            "│ Boss: Gelidus the Frozen Colossus (The Rime-Bound Titan)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-1 scaling band, no canon anchor. Ice biome continues.\n" +
            "Second guaranteed Divine Beast drop in the climb.\n" +
            "\n" +
            "BOSS\n" +
            "Gelidus the Frozen Colossus (Lv44, ~805 HP). Standard\n" +
            "scaling-curve boss; no canonical fight mechanics.\n" +
            "\n" +
            "DROPS\n" +
            "- Guaranteed: Savage Squall — Divine Beast 1H sword. Drops\n" +
            "  on any kill, not Last-Attack-locked.\n" +
            "- Tier-2 chest pool.\n" +
            "\n" +
            "TIPS\n" +
            "Don't cheese the boss in a corridor — the Divine roll\n" +
            "guarantees regardless, but you want clean line of sight for\n" +
            "the loot pull radius.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor Scaling Formulas] · [Floor 16] · [Floor 18] · [Divine Weapons — Roster & Acquisition]")
        {
            Tags = new[] { "floors", "f17", "divine", "loot" }
        },

        new("Floors", "Floor 18",
            "┌─ Floors\n" +
            "│ Topic: Floor 18\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Ice\n" +
            "│ Boss: Prismalynx the Shardcat (Predator of Refracted Light)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-1 scaling band, no canon anchor. Ice biome.\n" +
            "\n" +
            "BOSS\n" +
            "Prismalynx the Shardcat (Lv46, ~852 HP). Standard scaling-curve\n" +
            "boss; no canonical fight mechanics.\n" +
            "\n" +
            "TIPS\n" +
            "Refracted-light flavor — the Shardcat dazzles after a hit\n" +
            "lands; close the distance to deny range.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor Scaling Formulas] · [Biomes] · [Floor 17] · [Floor 19]")
        {
            Tags = new[] { "floors", "f18" }
        },

        new("Floors", "Floor 19",
            "┌─ Floors\n" +
            "│ Topic: Floor 19\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Ice\n" +
            "│ Boss: Gelmyre the Crystal Hydra (Three Heads of Living Ice)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-1 scaling band, no canon anchor. Ice biome. Multi-head\n" +
            "fight gives the floor more shape than the surrounding\n" +
            "scaling-curve floors.\n" +
            "\n" +
            "BOSS\n" +
            "Gelmyre the Crystal Hydra (Lv48, ~901 HP). Three heads share\n" +
            "an HP pool but not their cooldowns — a head down doesn't\n" +
            "remove its slot from the rotation. Treat it as one boss with\n" +
            "stacked ability windows.\n" +
            "\n" +
            "TIPS\n" +
            "Edged or piercing damage cuts. Cold builds get no biome\n" +
            "advantage on Ice mobs that aren't using cold attacks.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor Scaling Formulas] · [Floor 18] · [Floor 20]")
        {
            Tags = new[] { "floors", "f19" }
        },

        new("Floors", "Floor 20",
            "┌─ Floors\n" +
            "│ Topic: Floor 20\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Ice (mountain summit)\n" +
            "│ Boss: Absolut the Winter Monarch (Sovereign of Eternal Frost)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Era-I closing floor. Mountain summit, eternal frost, and the\n" +
            "first canonically Alicization-flavored Divine drop in the\n" +
            "climb. Tier 1 ends here in lore; the scaling curve doesn't\n" +
            "agree, but the boss roster does.\n" +
            "\n" +
            "BOSS\n" +
            "Absolut the Winter Monarch (Lv50, ~950 HP). Frost-element\n" +
            "boss. The arena freezes water tiles into walkable ice\n" +
            "between phases — your usual path may close mid-fight.\n" +
            "\n" +
            "DROPS\n" +
            "- Guaranteed: Blue Rose Sword — Divine Object, Eugeo's blade.\n" +
            "  Drops on the floor-boss kill (any party member's blow\n" +
            "  qualifies). Carries Freeze on hit.\n" +
            "- Tier-2/3 chest pool.\n" +
            "\n" +
            "NPCS / QUESTS\n" +
            "Klein and his samurai cohort headquarter Fuurinkazan on F20\n" +
            "(implementation; not strict canon). Recruit gate: Lv10+, any\n" +
            "karma. Bonuses on join: +5% CritRate and +10 ATK with Katana.\n" +
            "Best fit for any kit running Katana or relying on crit\n" +
            "scaling.\n" +
            "\n" +
            "CANON\n" +
            "Alicization: the Blue Rose Sword is Eugeo's signature blade,\n" +
            "the Underworld Divine Object that frosts a target on impact.\n" +
            "Klein's Fuurinkazan is Aincrad-era canon (LN vol 1+); F20 HQ\n" +
            "is this build's invention.\n" +
            "\n" +
            "TIPS\n" +
            "Land the killing blow yourself if you want to stack the\n" +
            "title-board kill on top of the Divine drop. The Blue Rose\n" +
            "Sword's Freeze procs work best on melee mob density — bank\n" +
            "it for F21-F25 dungeon clears.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor 19] · [Floor 21] · [Floor 25] · [Divine Weapons — Roster & Acquisition] · [Guild System Overview]")
        {
            Tags = new[] { "floors", "f20", "canon", "divine", "guild", "loot" }
        },

        new("Floors", "Floor 21",
            "┌─ Floors\n" +
            "│ Topic: Floor 21\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Dark (twilight era)\n" +
            "│ Boss: Morgath the Lich King (Deathless Lord of Floor 21)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-1 scaling band, no canon anchor. Dark biome opens — sight\n" +
            "drops by 20 across the floor (see [Biomes]).\n" +
            "\n" +
            "BOSS\n" +
            "Morgath the Lich King (Lv52, ~1000 HP). Standard scaling-curve\n" +
            "boss; no canonical fight mechanics.\n" +
            "\n" +
            "TIPS\n" +
            "Pack a torch or pre-stash fire crystals — the Dark biome's\n" +
            "vision penalty applies to mobs too, so stealth pulls work.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor Scaling Formulas] · [Biomes] · [Floor 20] · [Floor 22]")
        {
            Tags = new[] { "floors", "f21" }
        },

        new("Floors", "Floor 22",
            "┌─ Floors\n" +
            "│ Topic: Floor 22\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Dark (lakeside coniferous, golden hour)\n" +
            "│ Boss: The Witch of the West (Mistress of the Black Marsh)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Coral Village and the Forest of Wandering. Aincrad's gentlest\n" +
            "floor in canon — golden-hour woodland over a still lake, the\n" +
            "fog rolls thin, and a small log cabin sits where Kirito and\n" +
            "Asuna canonically retire to live as a married couple.\n" +
            "\n" +
            "BOSS\n" +
            "The Witch of the West (Lv54, ~1052 HP). Some lore docs call\n" +
            "this fight the King of Lakes; in this build the labyrinth\n" +
            "boss is the Witch. Marsh arena, summons that root the\n" +
            "approach. Edged or fire damage cuts.\n" +
            "\n" +
            "DROPS\n" +
            "Field boss Forest King Stag (Crowned Beast of Wandering)\n" +
            "roams the Forest of Wandering — guaranteed Kingly Antler\n" +
            "material on kill (any party member's blow qualifies). The\n" +
            "antler unlocks recipes at Lisbeth on F48.\n" +
            "\n" +
            "CANON\n" +
            "SAO LN vol 1 / anime ep 11-12: F22 is the Yui arc floor.\n" +
            "Kirito and Asuna's honeymoon cabin, the AI child Yui found\n" +
            "wandering the forest, the soft beat between Aincrad's harder\n" +
            "stretches. The Forest of Wandering's name carries through the\n" +
            "entire arc.\n" +
            "\n" +
            "TIPS\n" +
            "Don't sprint through the Forest of Wandering — its visibility\n" +
            "model is built for slow exploration. The Stag's antler is\n" +
            "your earliest recipe-unlock material; even non-craft kits\n" +
            "should bank one.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor 21] · [Floor 23] · [Floor 48]")
        {
            Tags = new[] { "floors", "f22", "canon", "asuna", "kirito" }
        },

        new("Floors", "Floor 23",
            "┌─ Floors\n" +
            "│ Topic: Floor 23\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Dark (meadow / pastoral)\n" +
            "│ Boss: Skullvane the Bone Dragon (The Rattling Sky)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-1 scaling band, no canon anchor. Dark biome, pastoral\n" +
            "meadow flavor — Lore.txt notes a Japanese-inspired farming\n" +
            "town for color, but no NPCs hook quests here.\n" +
            "\n" +
            "BOSS\n" +
            "Skullvane the Bone Dragon (Lv56, ~1104 HP). Standard\n" +
            "scaling-curve boss; no canonical fight mechanics.\n" +
            "\n" +
            "TIPS\n" +
            "Open meadow approach — bring ranged or expect a long charge\n" +
            "into Skullvane's breath cone.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor Scaling Formulas] · [Floor 22] · [Floor 24]")
        {
            Tags = new[] { "floors", "f23" }
        },

        new("Floors", "Floor 24",
            "┌─ Floors\n" +
            "│ Topic: Floor 24\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Dark (lake resort)\n" +
            "│ Boss: Grimhollow the Phantom (The Shape That Haunts)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Panareze — the lake resort. Wooden bridges between sun-drenched\n" +
            "islands in Integral Factor canon; in this build the dark biome\n" +
            "wash dims it to dusk. Vacation-town flavor surfaces in\n" +
            "Hollow Realization summer-beach event reskins.\n" +
            "\n" +
            "BOSS\n" +
            "Grimhollow the Phantom (Lv58, ~1158 HP). Phase-shifter — turns\n" +
            "intangible mid-fight, ignores incoming damage during the tell.\n" +
            "Wait out the phase before committing burst.\n" +
            "\n" +
            "DROPS\n" +
            "- Guaranteed: Phantasmagoria — Divine Beast dagger. Drops on\n" +
            "  any kill, not Last-Attack-locked.\n" +
            "- Tier-2/3 chest pool.\n" +
            "\n" +
            "CANON\n" +
            "SAO Material Edition / Integral Factor: Panareze is canonically\n" +
            "the floor's hub town. Stratos the St. Centaur and An Invictus\n" +
            "of the Cherub are IF's named bosses; this build runs Grimhollow\n" +
            "as the labyrinth boss with the Divine drop attached.\n" +
            "\n" +
            "TIPS\n" +
            "Phantasmagoria is a Tier-1 Divine — keep it through F40+\n" +
            "if you're a dagger kit. Don't burn cooldowns into Grimhollow's\n" +
            "intangible phase; the AI tell is consistent.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor 23] · [Floor 25] · [Divine Weapons — Roster & Acquisition]")
        {
            Tags = new[] { "floors", "f24", "canon", "divine", "loot" }
        },

        new("Floors", "Floor 25",
            "┌─ Floors\n" +
            "│ Topic: Floor 25\n" +
            "│ Tier: 1 (newbie)\n" +
            "│ Biome: Dark (forest ridge)\n" +
            "│ Boss: The Two-Headed Giant (Terror of the Twin Peaks)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Quarter-floor canon — Aincrad's multiples-of-25 spike the\n" +
            "boss difficulty. Forested ridge between two peaks, narrow\n" +
            "approach, single corridor into the labyrinth. F25 is the\n" +
            "band where bosses gain Toxic Breath — a status ability that\n" +
            "rotates Poison/Bleed/Slow on a floor%3 cycle.\n" +
            "\n" +
            "BOSS\n" +
            "The Two-Headed Giant (Lv60, ~1212 HP). Twin-headed brute,\n" +
            "two attack timelines. The Toxic Breath gimmick layers a\n" +
            "DoT on top of melee pressure — Poison rotation here. Bring\n" +
            "antidote crystals or a cleanse skill.\n" +
            "\n" +
            "DROPS\n" +
            "Field boss Labyrinth Warden (Keeper of the Underground Vault)\n" +
            "roams the labyrinth approach — IF Nox series anchor.\n" +
            "Guaranteed Nox Radgrid (Epic 1H sword) plus Shield Nox Fermat\n" +
            "(Epic). The IF Nox chest band runs F25-F40.\n" +
            "\n" +
            "NPCS / QUESTS\n" +
            "The Legend Braves — Schmitt's anti-Laughing-Coffin guild,\n" +
            "F25 HQ. Recruit gate: Lv15+ and karma >= 0. Bonuses on join:\n" +
            "+5% ATK plus +15 ATK against Laughing Coffin opponents. The\n" +
            "F25 boss clear is also the trigger that resolves the Black\n" +
            "Cats 'One More Floor' signature quest left over from F10.\n" +
            "\n" +
            "CANON\n" +
            "Pre-tragedy buffer floor in canon. The Legend Braves and the\n" +
            "anti-LC tone foreshadow F27's Black Cats catastrophe.\n" +
            "\n" +
            "TIPS\n" +
            "Bring an antidote stack and one cleanse-on-friendly skill.\n" +
            "If you recruited the Black Cats on F10, hand in 'One More\n" +
            "Floor' before descending — the bonus XP is wasted if you\n" +
            "trigger the F27 fate event with the quest still open.\n" +
            "\n" +
            "SEE ALSO\n" +
            "[Floor 10] · [Floor 24] · [Floor 27] · [Guild System Overview] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f25", "canon", "guild", "field-boss", "integral-factor" }
        },

        new("Floors", "Floor 26",
            "┌─ Floors\n" +
            "│ Topic: Floor 26\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Forest\n" +
            "│ Boss: Venomfang the Basilisk (Gaze of Petrification)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "First floor of the Tier-2 jungle-ruins band. Greener and\n" +
            "denser than the F21-F25 dark-forest stretch, with overgrown\n" +
            "stonework breaking up the corridors. No specific canon\n" +
            "anchor — follow the scaling curve.\n\n" +
            "BOSS\n" +
            "Venomfang the Basilisk (Gaze of Petrification). Status-\n" +
            "heavy: petrify-style stalls layer onto direct damage.\n" +
            "Antidote stack and a buffer ally help.\n\n" +
            "TIPS\n" +
            "F25+ bosses gain a Toxic-Breath status ability that rotates\n" +
            "Poison/Bleed/Slow on a 3-floor cycle — pre-stock cures so\n" +
            "the rotation never costs you a clear.\n\n" +
            "SEE ALSO\n" +
            "[Floor 25] · [Floor 27] · [Floor Scaling Formulas] · [Status Effects]")
        {
            Tags = new[] { "floors", "f26" }
        },

        new("Floors", "Floor 27",
            "┌─ Floors\n" +
            "│ Topic: Floor 27\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Forest\n" +
            "│ Boss: The Four-Armed Giant (Colossus of the Jungle Ruins)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "The trap-dungeon floor. Overgrown jungle ruins choke the\n" +
            "labyrinth corridors and the air goes still in a way the\n" +
            "earlier floors never did. F27 is a narrative checkpoint as\n" +
            "much as a fight: the Moonlit Black Cats fate event fires\n" +
            "the moment you arrive on the floor — the guild dissolves\n" +
            "and your karma takes a -5 hit you cannot prevent.\n\n" +
            "BOSS\n" +
            "The Four-Armed Giant (Colossus of the Jungle Ruins). Four\n" +
            "weapon arcs in a single round; lateral spacing matters\n" +
            "more than burst. Slash damage is reliable; a shield-stance\n" +
            "ally helps absorb the multi-arm volley.\n\n" +
            "CANON\n" +
            "SAO LN vol 2: Kirito's guild the Moonlit Black Cats wipes\n" +
            "in a F27 trap dungeon and Sachi dies. The fate event is\n" +
            "the in-game tribute — the guild force-disbands on F27\n" +
            "entry regardless of your standing with them, mirroring the\n" +
            "LN's fixed tragedy.\n\n" +
            "TIPS\n" +
            "If you joined the Black Cats earlier, do not bank goals on\n" +
            "their roster lasting past F26. Take the karma hit, push\n" +
            "through the boss, treat F28-F30 as recovery floors.\n\n" +
            "SEE ALSO\n" +
            "[Floor 26] · [Floor 28] · [Karma & Alignment] · [Guild System Overview]")
        {
            Tags = new[] { "floors", "f27", "canon" }
        },

        new("Floors", "Floor 28",
            "┌─ Floors\n" +
            "│ Topic: Floor 28\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Forest\n" +
            "│ Boss: Thornqueen Selvaria (Empress of the Briar Maze)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-2 jungle band, no specific canon anchor — follow the\n" +
            "scaling curve. Briar walls thicken the labyrinth and side\n" +
            "rooms tend to dead-end into thorn pockets.\n\n" +
            "BOSS\n" +
            "Thornqueen Selvaria (Empress of the Briar Maze). Roots\n" +
            "and snares; mobility loss compounds her direct hits.\n" +
            "Bring a cleanse or a dash skill.\n\n" +
            "TIPS\n" +
            "Don't burn pots clearing thorn rooms — push the labyrinth\n" +
            "and skip cosmetic dead-ends.\n\n" +
            "SEE ALSO\n" +
            "[Floor 27] · [Floor 29] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f28" }
        },

        new("Floors", "Floor 29",
            "┌─ Floors\n" +
            "│ Topic: Floor 29\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Forest\n" +
            "│ Boss: Raptoros the Apex Hunter (The Unseen Strike)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-2 jungle band, no specific canon anchor — follow the\n" +
            "scaling curve. Sightlines tighten under heavier canopy and\n" +
            "ambush-prone mobs cluster around the chokepoints.\n\n" +
            "BOSS\n" +
            "Raptoros the Apex Hunter (The Unseen Strike). Stealth +\n" +
            "single-target burst opener. Keep your back to a wall and\n" +
            "save a hard CC for the first appearance.\n\n" +
            "TIPS\n" +
            "Look-Mode the room before committing — Raptoros' approach\n" +
            "telegraph is a fold in the lighting, not a tile glyph.\n\n" +
            "SEE ALSO\n" +
            "[Floor 28] · [Floor 30] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f29" }
        },

        new("Floors", "Floor 30",
            "┌─ Floors\n" +
            "│ Topic: Floor 30\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Forest\n" +
            "│ Boss: Primos the World Serpent (Coil That Encircles the Floor)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-2 multiples-of-10 floor — no canon hub. Closes the\n" +
            "F26-F30 jungle band; the labyrinth opens out into one large\n" +
            "boss-arena hollow rather than the usual corridor stack.\n\n" +
            "BOSS\n" +
            "Primos the World Serpent (Coil That Encircles the Floor).\n" +
            "Wide-arena serpent fight. Drops a guaranteed Void Eater\n" +
            "(Divine Beast). Mobile rotation around a central coil —\n" +
            "stay outside its wrap range or eat the squeeze.\n\n" +
            "TIPS\n" +
            "Last clean break before the F31-F35 ruins band. Re-stock\n" +
            "consumables and stash anything you do not want to risk in\n" +
            "the F35 Christmas-event window.\n\n" +
            "SEE ALSO\n" +
            "[Floor 29] · [Floor 31] · [Floor 35] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f30" }
        },

        new("Floors", "Floor 31",
            "┌─ Floors\n" +
            "│ Topic: Floor 31\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Ruins\n" +
            "│ Boss: Garrison the Living Wall (The Unbreakable Bulwark)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Opens the F31-F35 fortress-ruins band. Crumbling outer\n" +
            "battlements, decay-flagged tiles, and the labyrinth wraps\n" +
            "around old siege-engine emplacements. No canon anchor.\n\n" +
            "BOSS\n" +
            "Garrison the Living Wall (The Unbreakable Bulwark). High-\n" +
            "Defense bulwark frame — ArmorPierce and crit-stack damage\n" +
            "outpace flat ATK. Patient fight, not a burst race.\n\n" +
            "TIPS\n" +
            "Ruins-biome decay drains structure HP on contact tiles.\n" +
            "Lean on edge-of-wall pulls so the boss eats the decay,\n" +
            "not you.\n\n" +
            "SEE ALSO\n" +
            "[Floor 30] · [Floor 32] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f31" }
        },

        new("Floors", "Floor 32",
            "┌─ Floors\n" +
            "│ Topic: Floor 32\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Ruins\n" +
            "│ Boss: Ironhyde the Siege Golem (Breaker of Gates)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-2 fortress-ruins band, no canon anchor. Wider corridors\n" +
            "framed by collapsed siege engines; line-of-sight is unusually\n" +
            "long for a ruins floor.\n\n" +
            "BOSS\n" +
            "Ironhyde the Siege Golem (Breaker of Gates). Heavy single-\n" +
            "target slams; predictable telegraph but punishing if you\n" +
            "stand-and-trade. Move on the wind-up.\n\n" +
            "TIPS\n" +
            "Long sightlines reward Bow openers. Fire from outside the\n" +
            "boss aggro radius before bumping the room.\n\n" +
            "SEE ALSO\n" +
            "[Floor 31] · [Floor 33] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f32" }
        },

        new("Floors", "Floor 33",
            "┌─ Floors\n" +
            "│ Topic: Floor 33\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Ruins\n" +
            "│ Boss: Ballistor the War Machine (Construct of Endless Arrows)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-2 fortress-ruins band, no canon anchor — follow the\n" +
            "scaling curve. Old artillery emplacements scatter the open\n" +
            "rooms; the walking is straightforward.\n\n" +
            "BOSS\n" +
            "Ballistor the War Machine (Construct of Endless Arrows).\n" +
            "Sustained ranged volleys with no real stop. Close the gap\n" +
            "fast or eat a dozen arrows on the approach.\n\n" +
            "TIPS\n" +
            "Block / Parry shines here — ranged frames have a better\n" +
            "average mitigation profile than dodge-stacking against\n" +
            "Ballistor's volume.\n\n" +
            "SEE ALSO\n" +
            "[Floor 32] · [Floor 34] · [Defense — Block, Parry, Dodge]")
        {
            Tags = new[] { "floors", "f33" }
        },

        new("Floors", "Floor 34",
            "┌─ Floors\n" +
            "│ Topic: Floor 34\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Ruins\n" +
            "│ Boss: Warden Keloth (Jailer of the Deep Dungeon)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-2 fortress-ruins band, no canon anchor. Last quiet\n" +
            "floor before the F35 Christmas-event spike — Keloth's\n" +
            "warden cells dominate the Labyrinth interior.\n\n" +
            "BOSS\n" +
            "Warden Keloth (Jailer of the Deep Dungeon). Stalls and\n" +
            "snares; a typical sustained-fight frame. Cleanse stack\n" +
            "and bring patience.\n\n" +
            "TIPS\n" +
            "Treat F34 as the staging floor for a F35 Nicholas attempt:\n" +
            "if the in-game date sits in the Dec 20-26 window, the F35\n" +
            "labyrinth approach overlaps with the seasonal Forest of\n" +
            "Wandering field event.\n\n" +
            "SEE ALSO\n" +
            "[Floor 33] · [Floor 35] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f34" }
        },

        new("Floors", "Floor 35",
            "┌─ Floors\n" +
            "│ Topic: Floor 35\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Ruins\n" +
            "│ Boss: Nicholas The Renegade (The Fallen Christmas Knight)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "The Christmas floor. Canon name Mishe — gentle hill-and-\n" +
            "village setting in the lore, fortress-ruins biome in code.\n" +
            "F35 wires Nicholas as the labyrinth boss directly,\n" +
            "anchoring the canonical Christmas-event association at the\n" +
            "floor it belongs to.\n\n" +
            "BOSS\n" +
            "Nicholas The Renegade (The Fallen Christmas Knight). Heavy\n" +
            "two-hand frame, slow swings, predictable rotation. The\n" +
            "Magnatherium field boss also roams F35 with a guaranteed\n" +
            "Mammoth Tusk drop.\n\n" +
            "CANON\n" +
            "SAO LN vol 2 / Anime Ep3: Nicholas the Renegade is the\n" +
            "Christmas Eve event boss in the Forest of Wandering, dropping\n" +
            "the Divine Stone of Returning Soul (a one-shot resurrection\n" +
            "item). Kirito chases the rumor solo and the duel is one of\n" +
            "the canon arc's emotional pivots. In this codebase Nicholas\n" +
            "stands as the regular F35 labyrinth boss; the seasonal\n" +
            "Forest-of-Wandering event still fires at F49 in a\n" +
            "Dec 20-26 window with the Returning-Soul drop.\n\n" +
            "TIPS\n" +
            "If you are diving F35 in the December window, plan a side\n" +
            "trip down to F49 for the seasonal field encounter — the\n" +
            "Returning Soul is a permadeath insurance policy worth the\n" +
            "detour.\n\n" +
            "SEE ALSO\n" +
            "[Floor 34] · [Floor 36] · [Floor 49] · [Seasonal Events]")
        {
            Tags = new[] { "floors", "f35", "canon" }
        },

        new("Floors", "Floor 36",
            "┌─ Floors\n" +
            "│ Topic: Floor 36\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Volcanic\n" +
            "│ Boss: Pyroclast the Lava Titan (Born of the Caldera)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Opens the F36-F40 volcanic band. Heat shimmer, ash-fall,\n" +
            "magma-channel hazards lining the labyrinth interior. No\n" +
            "specific canon anchor — follow the scaling curve.\n\n" +
            "BOSS\n" +
            "Pyroclast the Lava Titan (Born of the Caldera). Fire\n" +
            "rotation and ground-damage AoE; expect chip damage every\n" +
            "turn near its mass. Cold or Holy elements help.\n\n" +
            "TIPS\n" +
            "Stack a fire-resist accessory before stepping onto F36 —\n" +
            "the volcanic biome layers ambient damage over the boss\n" +
            "fight on top of mob aggro.\n\n" +
            "SEE ALSO\n" +
            "[Floor 35] · [Floor 37] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f36" }
        },

        new("Floors", "Floor 37",
            "┌─ Floors\n" +
            "│ Topic: Floor 37\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Volcanic\n" +
            "│ Boss: Infernus the Red Wyvern (Scourge of the Ashen Skies)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-2 volcanic band, no specific canon anchor — follow\n" +
            "the scaling curve. Open arenas and short corridors,\n" +
            "punctuated by hazard flows.\n\n" +
            "BOSS\n" +
            "Infernus the Red Wyvern (Scourge of the Ashen Skies).\n" +
            "Aerial-pattern fire-breath frame; pulls aggro early and\n" +
            "stays mobile. Bow openers at range trade well.\n\n" +
            "TIPS\n" +
            "Bring a fire-resist potion stash — the wyvern's breath\n" +
            "stacks on top of biome ambient.\n\n" +
            "SEE ALSO\n" +
            "[Floor 36] · [Floor 38] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f37" }
        },

        new("Floors", "Floor 38",
            "┌─ Floors\n" +
            "│ Topic: Floor 38\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Volcanic\n" +
            "│ Boss: Obsidian the Black Knight (Forged in Volcanic Glass)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-2 volcanic band, no canon anchor. Floor boss carries\n" +
            "a guaranteed Cactus Bludgeon (Divine Beast) drop on a\n" +
            "regular clear — worth the kit-up detour.\n\n" +
            "BOSS\n" +
            "Obsidian the Black Knight (Forged in Volcanic Glass).\n" +
            "Heavy single-blade frame, parries cleanly into\n" +
            "counter-strikes. Sword-skill cooldown management matters.\n\n" +
            "TIPS\n" +
            "Equip Elucidator (-1 SkillCooldown) if you have it — this\n" +
            "fight rewards skill rotation density.\n\n" +
            "SEE ALSO\n" +
            "[Floor 37] · [Floor 39] · [Floor 50]")
        {
            Tags = new[] { "floors", "f38" }
        },

        new("Floors", "Floor 39",
            "┌─ Floors\n" +
            "│ Topic: Floor 39\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Volcanic\n" +
            "│ Boss: Magmaron the Core Beast (The Living Eruption)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-2 volcanic band, no canon anchor. Quiet penultimate\n" +
            "floor before the F40 mid-game pivot — last room to bank\n" +
            "consumables before Dracoflame and the DDA recruit window.\n\n" +
            "BOSS\n" +
            "Magmaron the Core Beast (The Living Eruption). Pulses AoE\n" +
            "on a fixed cadence. Watch the count, time your skill\n" +
            "windows between pulses.\n\n" +
            "TIPS\n" +
            "Ship gear back to Lindarth (F48) after this clear if you\n" +
            "want a Mithril enhance pass before F40 — it slots cleanly\n" +
            "into the descent route.\n\n" +
            "SEE ALSO\n" +
            "[Floor 38] · [Floor 40] · [Floor 48]")
        {
            Tags = new[] { "floors", "f39" }
        },

        new("Floors", "Floor 40",
            "┌─ Floors\n" +
            "│ Topic: Floor 40\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Volcanic\n" +
            "│ Boss: Dracoflame the Elder Wyrm (Grandfather of Fire)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Quarter-floor mid-game pivot. F40 packs three Legendary\n" +
            "seeds, a guild HQ, and a canon NPC into one volcanic\n" +
            "stretch. Plan the descent so you do not skip any of them.\n\n" +
            "BOSS\n" +
            "Dracoflame the Elder Wyrm (Grandfather of Fire). Drops\n" +
            "the Crimson Stream Demonblade (Legendary 2H Sword) on a\n" +
            "regular clear. Heavy fire-rotation wyrm; cold and Holy\n" +
            "elements help, fire-resist accessory mandatory.\n\n" +
            "DROPS\n" +
            "- Floor boss: Crimson Stream (Legendary 2H Sword,\n" +
            "  guaranteed Divine Beast drop).\n" +
            "- Field boss Ogre Lord (Chieftain of the Broken Valley):\n" +
            "  Ogre's Cleaver.\n" +
            "- Field boss Phoenix of the Smolder Peak: Conflagrant\n" +
            "  Flame Bow (Divine — Deusolbert's bow, Alicization canon).\n\n" +
            "NPCS / QUESTS\n" +
            "Yulier (BrightYellow 'Y') — KoB-era Asuna friend. Quest\n" +
            "'The Lightning Flash's Memory': 10 F40 kills returns\n" +
            "Lambent Light (Asuna's Legendary Rapier) plus 450 Col +\n" +
            "350 XP. One-shot per save.\n" +
            "Lind (Divine Dragon Alliance HQ) — recruiter for the rival\n" +
            "tank guild to KoB. Gate Lv15 + karma >= 0; passive +10\n" +
            "Vitality + 5 Defense. DDA and KoB are mutually exclusive\n" +
            "active guilds.\n\n" +
            "CANON\n" +
            "SAO LN vol 4-7: Lambent Light is Asuna's signature rapier\n" +
            "as Vice-Commander of KoB; she carries it through the\n" +
            "F50-F75 push. Yulier (Liberation Army) fought beside Asuna\n" +
            "on F59, modeled here as the F40 questgiver. The Conflagrant\n" +
            "Flame Bow is Deusolbert's blade in Alicization canon —\n" +
            "Aincrad-invented placement on the Smolder Peak phoenix.\n\n" +
            "TIPS\n" +
            "Plant the Yulier turn-in BEFORE the floor boss so the 10\n" +
            "F40 kills overlap with the boss approach. If you intend to\n" +
            "join DDA, complete recruitment before stepping to F55,\n" +
            "where KoB recruitment opens (Godfree, Granzam HQ).\n\n" +
            "SEE ALSO\n" +
            "[Floor 39] · [Floor 41] · [Floor 50] · [Floor 55] · [Floor 55]")
        {
            Tags = new[] { "floors", "f40", "canon" }
        },

        new("Floors", "Floor 41",
            "┌─ Floors\n" +
            "│ Topic: Floor 41\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Aquatic\n" +
            "│ Boss: Leviathan the Depth Lord (Terror of the Sunken Halls)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Opens the F41-F45 aquatic band. Flooded corridors,\n" +
            "swimming-zone hazards, and a long stretch of Tier-2\n" +
            "scaling-curve boss roster with no specific canon anchors.\n\n" +
            "BOSS\n" +
            "Leviathan the Depth Lord (Terror of the Sunken Halls).\n" +
            "Mass single-target frame; sustained pressure rather than\n" +
            "burst telegraph. Lightning damage scales unusually well\n" +
            "in submerged rooms.\n\n" +
            "TIPS\n" +
            "Water tiles slow basic movement — use a dash or swap to\n" +
            "Bow before stepping into the boss arena.\n\n" +
            "SEE ALSO\n" +
            "[Floor 40] · [Floor 42] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f41" }
        },

        new("Floors", "Floor 42",
            "┌─ Floors\n" +
            "│ Topic: Floor 42\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Aquatic\n" +
            "│ Boss: Coralith the Reef Titan (The Living Atoll)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-2 aquatic band, no canon anchor. Reef-floor visuals;\n" +
            "the labyrinth is unusually open with reef pillars\n" +
            "interrupting otherwise straight lanes.\n\n" +
            "BOSS\n" +
            "Coralith the Reef Titan (The Living Atoll). Static-\n" +
            "platform frame: low mobility, high regional damage. Bring\n" +
            "ranged damage and stay outside its passive aura.\n\n" +
            "TIPS\n" +
            "If you stalled out kit-wise on F40, F42 is the cheapest\n" +
            "farm window — wide rooms, predictable monster rotation.\n\n" +
            "SEE ALSO\n" +
            "[Floor 41] · [Floor 43] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f42" }
        },

        new("Floors", "Floor 43",
            "┌─ Floors\n" +
            "│ Topic: Floor 43\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Aquatic\n" +
            "│ Boss: Undine the Water Maiden (Siren of the Deep)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-2 aquatic band, no canon anchor. Floor boss carries\n" +
            "a guaranteed Midnight Rain (Divine Beast Legendary Rapier)\n" +
            "drop on a regular clear.\n\n" +
            "BOSS\n" +
            "Undine the Water Maiden (Siren of the Deep). Charm /\n" +
            "stall rotation that punishes single-target focus. Bring\n" +
            "a cleanse and rotate damage targets when she summons.\n\n" +
            "TIPS\n" +
            "Rapier players: this is your kit-up window. Midnight Rain\n" +
            "carries a Rapier build cleanly to F50 and beyond.\n\n" +
            "SEE ALSO\n" +
            "[Floor 42] · [Floor 44] · [Floor 40]")
        {
            Tags = new[] { "floors", "f43" }
        },

        new("Floors", "Floor 44",
            "┌─ Floors\n" +
            "│ Topic: Floor 44\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Aquatic\n" +
            "│ Boss: Abyssal Kraken (Tentacles of the Drowned Floor)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-2 aquatic band, no canon anchor — follow the scaling\n" +
            "curve. Open-room labyrinth; the floor leans into wide\n" +
            "boss-arena fights instead of corridor stacks.\n\n" +
            "BOSS\n" +
            "Abyssal Kraken (Tentacles of the Drowned Floor). Multi-\n" +
            "tentacle frame; cleave damage shines, single-target\n" +
            "burst loses tempo to summons.\n\n" +
            "TIPS\n" +
            "Stay mobile — the Kraken's tentacle pattern punishes\n" +
            "stand-and-trade. AoE consumables (Fire Bombs, Lightning\n" +
            "Crystals) trade up here.\n\n" +
            "SEE ALSO\n" +
            "[Floor 43] · [Floor 45] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f44" }
        },

        new("Floors", "Floor 45",
            "┌─ Floors\n" +
            "│ Topic: Floor 45\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Aquatic\n" +
            "│ Boss: Tidecaller Nereus (Herald of the Endless Flood)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Tier-2 aquatic band, no canon anchor. Closes the aquatic\n" +
            "stretch before the F46-F50 dark-band shift; floor boss\n" +
            "is a multiples-of-5 milestone but not a canon hub.\n\n" +
            "BOSS\n" +
            "Tidecaller Nereus (Herald of the Endless Flood). Summons\n" +
            "and tide-pulse AoE; treat it like a wave-based fight\n" +
            "rather than a duel.\n\n" +
            "TIPS\n" +
            "Stash a Holy or Light item — the dark biome on F46-F50\n" +
            "shifts the elemental landscape. Last cheap restock floor\n" +
            "before the run intensifies.\n\n" +
            "SEE ALSO\n" +
            "[Floor 44] · [Floor 46] · [Floor 50]")
        {
            Tags = new[] { "floors", "f45" }
        },

        new("Floors", "Floor 46",
            "┌─ Floors\n" +
            "│ Topic: Floor 46\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Dark\n" +
            "│ Boss: The Ant Queen (Matriarch of the Hive Floor)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Opens the F46-F50 dark band. Hive-density labyrinth, mob\n" +
            "swarms, restricted sightlines. The Ant Queen lands here\n" +
            "by canon Floor Boss Roster — same boss reappears in a\n" +
            "Tier-4 form on F84.\n\n" +
            "BOSS\n" +
            "The Ant Queen (Matriarch of the Hive Floor). Summon-\n" +
            "and-swarm frame: multi-target damage cleans her broods\n" +
            "faster than single-target burst will. Bring AoE.\n\n" +
            "TIPS\n" +
            "If you cleared the F40 Crimson Stream Demonblade, this\n" +
            "is a clean cleave-damage room to settle the kit. The Ant\n" +
            "Queen herself goes down quickly once the swarm thins.\n\n" +
            "SEE ALSO\n" +
            "[Floor 45] · [Floor 47] · [Floor 84]")
        {
            Tags = new[] { "floors", "f46" }
        },

        new("Floors", "Floor 47",
            "┌─ Floors\n" +
            "│ Topic: Floor 47\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Dark\n" +
            "│ Boss: Mindflayer Zethos (The Thought Devourer)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "The canon Floria floor — Aincrad's flower-garden hub in\n" +
            "the lore. The dark biome dominates code-side, but the\n" +
            "spawn-area generator scatters flower tiles into the local\n" +
            "7×7 box at 1/5 density as a flavor nod.\n\n" +
            "BOSS\n" +
            "Mindflayer Zethos (The Thought Devourer). Charm /\n" +
            "psychic-damage frame; punishes single-target burst\n" +
            "rotations and rewards cleanse stack.\n\n" +
            "CANON\n" +
            "SAO Aincrad arc / Material Edition: Floria is the\n" +
            "canonical flower-garden floor — a quiet town with no\n" +
            "labyrinth-defining canon boss. The flower-tile sprinkle\n" +
            "around the spawn arena is the in-game tribute.\n\n" +
            "TIPS\n" +
            "Treat F47 as a controlled stretch before the F48 Lindarth\n" +
            "side-trip. Anti-charm potion helps Zethos go cleanly.\n\n" +
            "SEE ALSO\n" +
            "[Floor 46] · [Floor 48] · [Floor 50] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f47", "canon" }
        },

        new("Floors", "Floor 48",
            "┌─ Floors\n" +
            "│ Topic: Floor 48\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Dark\n" +
            "│ Boss: Nightweaver Morrigan (Spinner of Bad Dreams)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Lindarth — Lisbeth's forge town. The mid-endgame crafting\n" +
            "hub: a hand-laid 61×35 hub with seven named buildings\n" +
            "carved out of the Labyrinth interior. Snowstorm weather\n" +
            "is canon. Plan the run so F48 is a multi-purpose stop.\n\n" +
            "BOSS\n" +
            "Nightweaver Morrigan (Spinner of Bad Dreams). Standard\n" +
            "scaling-curve labyrinth boss; the floor's interest is in\n" +
            "the town and the field encounter, not the boss room.\n\n" +
            "DROPS\n" +
            "- Field boss Frost Dragon (Wyrm of the Crystallite Cave):\n" +
            "  Crystallite Ingot — feeds Lisbeth's Crystallite enhance\n" +
            "  tab and several R6 recipes.\n" +
            "- Floor boss carries the standard Tier-2 chest pool.\n\n" +
            "NPCS / QUESTS\n" +
            "F48 Lisbeth (BrightMagenta 'L', distinct from the F1\n" +
            "townsfolk) spawns deterministically inside the central\n" +
            "Forge. Her dialog opens five tabs:\n" +
            "  R6 craft       Rarity-6 Legendary recipes, 3M Col +\n" +
            "                 rare mats per craft.\n" +
            "  Iron enhance   Common/Uncommon, +5 cap, 200 Col + 3\n" +
            "                 iron per +1.\n" +
            "  Mithril enhance Rare/Epic, +7 cap, 1000 Col + 3 mithril\n" +
            "                 per +1.\n" +
            "  Crystallite    Epic/Legendary, +10 cap, 5000 Col + 3\n" +
            "                 crystallite per +1.\n" +
            "  Reforge        Re-roll random bonuses.\n" +
            "Dark Repulser gift line: clear the F55 floor boss, return\n" +
            "to F48, bump Lisbeth — she gifts Dark Repulser (Legendary\n" +
            "1H Sword, CritHeal+5) before the standard forge dialog.\n" +
            "One-shot per save; pure gift, no Col / mat cost.\n\n" +
            "BUILDINGS\n" +
            "  Forge                Lisbeth's primary work-floor.\n" +
            "  Anvil cluster (4)    Auxiliary enhance stations.\n" +
            "  Crystallite Refinery High-tier ingot processing.\n" +
            "  Mithril Smelter      Mid-tier ingot processing.\n" +
            "  Material Vendor      Mat top-up between crafts.\n" +
            "  Lisbeth's quarters   Flavor interior.\n" +
            "  Lindarth Inn         Rest checkpoint.\n\n" +
            "CANON\n" +
            "SAO LN vol 2 / Anime Ep7 (Lisbeth side-story): Lisbeth's\n" +
            "smithing town sits on a Lindarth-equivalent floor. After\n" +
            "the F55 dragon-ore expedition (modeled here as 'clear F55\n" +
            "floor boss'), Lisbeth crafts Dark Repulser from the\n" +
            "Crystallite Ingot and gifts it to Kirito. The same\n" +
            "Crystallite Ingot harvested from the F48 Frost Dragon is\n" +
            "the canon material thread.\n\n" +
            "TIPS\n" +
            "Stack a Lindarth visit with one R6 craft + one reforge +\n" +
            "an enhance run. Bank Col aggressively before F48 — a\n" +
            "single R6 craft drains 3M. Two Dark Repulsers per save\n" +
            "are possible: the F55 Crystal Wyrm field-boss drop is\n" +
            "fully additive to Lisbeth's gift line.\n\n" +
            "SEE ALSO\n" +
            "[Floor 47] · [Floor 49] · [Floor 50] · [Floor 55] · [Lisbeth — Rarity 6 Craft Line] · [Pair Resonance]")
        {
            Tags = new[] { "floors", "f48", "canon" }
        },

        new("Floors", "Floor 49",
            "┌─ Floors\n" +
            "│ Topic: Floor 49\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Dark\n" +
            "│ Boss: Shadowstep Assassin (The Boss You Never See Coming)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Canon name Myujen — a white-stone city in the Aincrad\n" +
            "lore, dark-dungeon flavor in code. The seasonal Nicholas\n" +
            "the Renegade event drops here as the field-boss layer in\n" +
            "the Forest of Wandering during a Dec 20-26 window.\n\n" +
            "BOSS\n" +
            "Shadowstep Assassin (The Boss You Never See Coming).\n" +
            "Tier-2 scaling-curve labyrinth boss with stealth-opener\n" +
            "framing. Drops a guaranteed Midnight Sun (Divine Beast,\n" +
            "Legendary Katana). Watch the first appearance — its\n" +
            "burst window opens the fight.\n\n" +
            "DROPS\n" +
            "- Floor boss: Midnight Sun (Legendary Katana, guaranteed).\n" +
            "- Seasonal field boss Nicholas the Renegade (Fallen Saint\n" +
            "  of the Winter Fir): Divine Stone of Returning Soul. Only\n" +
            "  fires on Dec 20-26 in-game.\n\n" +
            "CANON\n" +
            "SAO LN vol 2 / Anime Ep3: the Christmas-Eve Nicholas duel\n" +
            "drops the Returning Soul, a one-shot resurrection item.\n" +
            "F49 is the seasonal placement; F35 carries the regular\n" +
            "labyrinth-boss appearance year-round.\n\n" +
            "TIPS\n" +
            "Drop in during the December window if you can — Returning\n" +
            "Soul is permadeath insurance. Otherwise, treat F49 like\n" +
            "any Tier-2 floor and ride the curve to F50.\n\n" +
            "SEE ALSO\n" +
            "[Floor 48] · [Floor 50] · [Floor 35] · [Seasonal Events]")
        {
            Tags = new[] { "floors", "f49", "canon" }
        },

        new("Floors", "Floor 50",
            "┌─ Floors\n" +
            "│ Topic: Floor 50\n" +
            "│ Tier: 2 (mid-game)\n" +
            "│ Biome: Dark\n" +
            "│ Boss: The Six-Armed Buddha (Tier of Sin)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Mid-game pivot. The labyrinth shifts from corridor-heavy\n" +
            "to open-arena and the boss is the canonical Elucidator\n" +
            "drop floor. Canon name Algade — major commercial hub in\n" +
            "the Aincrad arc, KoB ascension era. F50+ also unlocks\n" +
            "additive shop tiers as boss clears feed forward.\n\n" +
            "BOSS\n" +
            "The Six-Armed Buddha (Tier of Sin). Multi-arm flurries;\n" +
            "Holy / Light cuts through best. F50+ bosses gain a\n" +
            "Regeneration self-heal (10% MaxHealth, phase 3, cooldown\n" +
            "10) — burst the final HP slice or the heal denies the\n" +
            "kill window.\n\n" +
            "DROPS\n" +
            "- Last-Attack Bonus (your killing blow only): Elucidator\n" +
            "  — Legendary 1H Sword, SkillCooldown-1. Ally finishers\n" +
            "  forfeit it.\n" +
            "- Chest pool: Tier-2/3 Legendaries, mid-tier ingots.\n\n" +
            "NPCS / QUESTS\n" +
            "Sister Azariya (cyan 'A', former Fanatio apprentice,\n" +
            "left the Integrity Knight order). Quest 'Light at the\n" +
            "Edge of Sight': 20 F50 kills returns Heaven-Piercing\n" +
            "Blade (Divine Rapier, PiercingBeam+30, Range 2) plus\n" +
            "500 Col + 400 XP. She never layers random side quests.\n\n" +
            "CANON\n" +
            "SAO LN vol 4 (Aincrad arc): Kirito takes Elucidator from\n" +
            "the F50 boss as Last-Attack Bonus — the LN explicitly\n" +
            "calls out the LAB mechanic. The blade carries his\n" +
            "F50-F75 push and pairs with Dark Repulser at F74 once\n" +
            "Dual Blades unlocks. Heaven-Piercing Blade is Fanatio's\n" +
            "Alicization Integrity-Knight blade; the Sister Azariya\n" +
            "questline is implementation-canon placement.\n\n" +
            "TIPS\n" +
            "Save your highest-burst skill for the final HP slice.\n" +
            "Pre-pull aggro so the boss faces you in the LAB window;\n" +
            "rotate allies to non-attack stance via SAO Switch if you\n" +
            "want absolute LAB safety. Run F50 → F55 → F48 in one\n" +
            "session for a same-run Dual Blades kit.\n\n" +
            "SEE ALSO\n" +
            "[Floor 48] · [Floor 49] · [Floor 55] · [Pair Resonance] · [Unique Skill: Dual Blades]")
        {
            Tags = new[] { "floors", "f50", "canon" }
        },

        new("Floors", "Floor 51",
            "┌─ Floors\n" +
            "│ Topic: Floor 51\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Forest\n" +
            "│ Boss: Yggdrath the World Tree (The Rooted Colossus)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "First step into the expert tier. Ancient-forest biome opens\n" +
            "the F51-F55 band — no canon hub on this floor itself, just\n" +
            "deeper canopy and wider boss arenas. Follow the scaling curve.\n\n" +
            "BOSS\n" +
            "Yggdrath the World Tree (The Rooted Colossus). Scaling-curve\n" +
            "labyrinth boss with no canon-anchored mechanic. Treant frame —\n" +
            "Holy / Light damage cuts through best; fire risks setting the\n" +
            "arena alight without measurable bonus.\n\n" +
            "TIPS\n" +
            "Bank a Lindarth (F48) crafting stop on the way up — Iron and\n" +
            "Mithril enhancements are the cheap kit-up window before the\n" +
            "F55 canon spike. Expert-tier mob HP outpaces flat damage; lean\n" +
            "on crit and elemental riders.\n\n" +
            "SEE ALSO\n" +
            "[Floor 48] · [Floor 55] · [Floor Scaling Formulas] · [Critical Hits]")
        {
            Tags = new[] { "floors", "f51" }
        },

        new("Floors", "Floor 52",
            "┌─ Floors\n" +
            "│ Topic: Floor 52\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Forest\n" +
            "│ Boss: Fenrir the Dread Wolf (The Unchained Beast)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Mid-band ancient forest. No canon anchor; scaling-curve floor\n" +
            "between F51's tree-boss and F55's Pale Dragon. Beast packs\n" +
            "lean fast and lupine.\n\n" +
            "BOSS\n" +
            "Fenrir the Dread Wolf (The Unchained Beast). Scaling-curve\n" +
            "labyrinth boss. High-mobility brawler — kite into chokepoints\n" +
            "to deny its lunge windows.\n\n" +
            "TIPS\n" +
            "F52-F54 reward straight-line climbing toward the F55 Lisbeth\n" +
            "canon spike. Don't burn finite consumables here; conserve for\n" +
            "the F55 dragon and floor boss.\n\n" +
            "SEE ALSO\n" +
            "[Floor 51] · [Floor 55] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f52" }
        },

        new("Floors", "Floor 53",
            "┌─ Floors\n" +
            "│ Topic: Floor 53\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Forest\n" +
            "│ Boss: Sylphiel the Storm Dryad (Wrath of the Ancient Wood)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Forest band continues. No canon hub. Storm-touched canopy:\n" +
            "expect lightning-flavored mob ability rotations on top of the\n" +
            "expert-tier scaling curve.\n\n" +
            "BOSS\n" +
            "Sylphiel the Storm Dryad (Wrath of the Ancient Wood). Scaling-\n" +
            "curve labyrinth boss. Wind / lightning theming; bring conductive\n" +
            "armor at your own risk and prefer Holy / Light cleansing kits.\n\n" +
            "TIPS\n" +
            "Same advice as F51-F52: ride the curve, save burn for F55.\n" +
            "Stack any standing F51-F55 weapon-gated quest counters here\n" +
            "rather than backtracking later.\n\n" +
            "SEE ALSO\n" +
            "[Floor 52] · [Floor 55] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f53" }
        },

        new("Floors", "Floor 54",
            "┌─ Floors\n" +
            "│ Topic: Floor 54\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Forest\n" +
            "│ Boss: Titanoak the Living Fortress (Where the Forest Fights Back)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Last forest floor before Granzam. No canon anchor; the labyrinth\n" +
            "biome thickens into siege-style oaken bulwarks. Treat as the\n" +
            "final breath-take before the F55 spike.\n\n" +
            "BOSS\n" +
            "Titanoak the Living Fortress (Where the Forest Fights Back).\n" +
            "Scaling-curve labyrinth boss. High-Defense bark frame —\n" +
            "ArmorPierce and crit-stack damage scale better here than raw\n" +
            "flat ATK.\n\n" +
            "TIPS\n" +
            "Verify your kit before stepping to F55: Elucidator (F50 Last-\n" +
            "Attack Bonus) in your main hand, weapon-class ally if you want\n" +
            "to land the Crystal Wyrm killing blow personally. F54 is the\n" +
            "last cheap-revive floor before canon density rises.\n\n" +
            "SEE ALSO\n" +
            "[Floor 53] · [Floor 55] · [Floor 50] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f54" }
        },

        new("Floors", "Floor 55",
            "┌─ Floors\n" +
            "│ Topic: Floor 55\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Forest (Granzam fortress city)\n" +
            "│ Boss: X'rphan the White Wyrm (The Pale Dragon of Floor 55)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Granzam — the iron fortress-city, Knights of the Blood Oath\n" +
            "headquarters, and the floor where Lisbeth's dragon-cave canon\n" +
            "anchors. Steel-grey weather, militaristic streets, the West\n" +
            "Mountain in the distance. Three guaranteed canon hooks land\n" +
            "here: KoB recruitment, the Crystal Wyrm field boss, and Agil's\n" +
            "Apprentice quest.\n\n" +
            "BOSS\n" +
            "X'rphan the White Wyrm (The Pale Dragon of Floor 55). Canon\n" +
            "labyrinth boss, distinct from the field-boss Crystal Wyrm.\n" +
            "Clearing X'rphan flips the lifetime flag that arms Lisbeth's\n" +
            "Dark Repulser gift on your next F48 visit.\n\n" +
            "DROPS\n" +
            "- Field boss: Crystal Wyrm of Lisbeth's Forge (BrightCyan W,\n" +
            "  overworld only — never spawns inside the Labyrinth). HP 3.2x,\n" +
            "  ATK 1.6x. Guaranteed Dark Repulser (Legendary 1H Sword,\n" +
            "  CritHeal+5). Once defeated, never respawns.\n" +
            "- Lisbeth gift (post-clear): return to F48 Lindarth after\n" +
            "  X'rphan is dead — Lisbeth crafts and gifts a second Dark\n" +
            "  Repulser, one-time per save. A player who kills the wyrm\n" +
            "  AND clears X'rphan AND returns to Lindarth ends up with two.\n" +
            "- Floor-boss chest pool: standard expert-tier Legendaries.\n\n" +
            "NPCS / QUESTS\n" +
            "- Knights of the Blood Oath HQ — Godfree (recruiter), Heathcliff\n" +
            "  (leader). Gate Lv25, karma >= +30. Passive: +8 Defense and\n" +
            "  BlockChance scaling on Vitality. Recruit task: 10 frontline\n" +
            "  labyrinth kills.\n" +
            "- Agil's Apprentice (BrightYellow G). Quest: 15 F55 kills →\n" +
            "  Ground Gorge (Fractured Daydream 2H Axe, Agil's signature).\n\n" +
            "CANON\n" +
            "SAO LN vol 2 — the Lisbeth side-story. Lisbeth and Kirito dive\n" +
            "into a crystallite hollow on F55 to harvest the breath-frozen\n" +
            "ingot of a crystal dragon; her scales chime when she breathes.\n" +
            "Lisbeth forges Dark Repulser from the ingot back at her Lindarth\n" +
            "shop. Granzam is also Knights of the Blood Oath HQ in the LN —\n" +
            "Heathcliff's elite frontline order operates from the West\n" +
            "Mountain fortress.\n\n" +
            "TIPS\n" +
            "Path: kill F50 Six-Armed Buddha for Elucidator (Last-Attack\n" +
            "Bonus, killing blow only) → push F51-F54 → on F55, find the\n" +
            "Crystal Wyrm in the overworld BEFORE engaging X'rphan, kill it\n" +
            "for the field-boss Dark Repulser → clear X'rphan → return to\n" +
            "F48 Lindarth for Lisbeth's gift Dark Repulser. Same-session\n" +
            "Dual Blades kit: Elucidator + two Dark Repulsers (one for the\n" +
            "active dual-wield set, one to vault or hand to a Kirito ally\n" +
            "for a near-canon party loadout). Vitality builds gain the most\n" +
            "from KoB recruitment — the BlockChance rider scales with VIT,\n" +
            "compounding with shield kits.\n\n" +
            "SEE ALSO\n" +
            "[Floor 48] · [Floor 50] · [Floor 60] · [Floor 74] · [Pair Resonance] · [Unique Skill: Dual Blades] · [Guild System Overview] · [Karma & Alignment]")
        {
            Tags = new[] { "floors", "f55", "canon" }
        },

        new("Floors", "Floor 56",
            "┌─ Floors\n" +
            "│ Topic: Floor 56\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Ruins (Pani forest outpost)\n" +
            "│ Boss: Geocrawler (The Burrowing Menace)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Pani — Hollow Fragment-era forest outpost and canon Divine\n" +
            "Dragon Alliance frontier post. Picnic safe-zone respite floor\n" +
            "after the Granzam push. Quieter biome, but the labyrinth boss\n" +
            "is a canon HF-adjacent burrower.\n\n" +
            "BOSS\n" +
            "Geocrawler (The Burrowing Menace). Canon HF-adjacent floor\n" +
            "boss. Tunnels under the arena floor and surfaces under your\n" +
            "feet — keep moving and avoid tile-camping.\n\n" +
            "DROPS\n" +
            "Floor-boss chest pool: standard expert-tier Legendaries; no\n" +
            "guaranteed unique anchor.\n\n" +
            "NPCS / QUESTS\n" +
            "Forest House (purchasable safe-zone cottage, HF canon) —\n" +
            "F56's quiet hub asset for parties looking to bank a long\n" +
            "rest near the DDA frontier post.\n\n" +
            "CANON\n" +
            "Hollow Fragment — Pani is the canon DDA forward outpost in\n" +
            "the post-Aincrad-arc HF storyline; the Geocrawler is canon\n" +
            "HF / LN-adjacent in the same post-floor band.\n\n" +
            "TIPS\n" +
            "Stop at the Forest House to bank rest before pushing F60.\n" +
            "Geocrawler's burrow telegraphs as a tile shimmer — step off\n" +
            "the affected tile the turn before it surfaces.\n\n" +
            "SEE ALSO\n" +
            "[Floor 55] · [Floor 60] · [Floor 40]")
        {
            Tags = new[] { "floors", "f56", "canon" }
        },

        new("Floors", "Floor 57",
            "┌─ Floors\n" +
            "│ Topic: Floor 57\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Ruins (Marten mountain trade)\n" +
            "│ Boss: Atlas the Mountainbreaker (He Who Carries the Ceiling)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Marten — canon mountain-trade town. Caravan escorts run the\n" +
            "switchbacks between Granzam and the F60 frontier. Open-sky\n" +
            "ruins biome with steep ridge labyrinths.\n\n" +
            "BOSS\n" +
            "Atlas the Mountainbreaker (He Who Carries the Ceiling).\n" +
            "Scaling-curve floor boss. Heavy stone titan; long wind-up on\n" +
            "ceiling-cracker AoE — bait the swing, then cut from his arc.\n\n" +
            "DROPS\n" +
            "Floor-boss chest pool: standard expert-tier Legendaries.\n\n" +
            "NPCS / QUESTS\n" +
            "Caravan-escort flavor quests in Marten — small Col / XP\n" +
            "rewards, nothing canon-anchored to a Divine drop.\n\n" +
            "CANON\n" +
            "LN V1 adjacent — Marten is named in canon as a mountain-trade\n" +
            "town along the F55-F60 ridge; in implementation it stays\n" +
            "flavor-only (no canon NPC anchored here).\n\n" +
            "TIPS\n" +
            "Use Marten as a staging stop for F60 — the caravan-escort\n" +
            "kill counters double as F57 weapon-gated quest progress.\n\n" +
            "SEE ALSO\n" +
            "[Floor 55] · [Floor 60] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f57", "canon" }
        },

        new("Floors", "Floor 58",
            "┌─ Floors\n" +
            "│ Topic: Floor 58\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Ruins\n" +
            "│ Boss: Gravelthorn the Earth Elemental (Heart of the Mountain)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Mid-band rugged frontier between Marten and the F60 spike.\n" +
            "No canon hub; ridge ruins continue. Follow the scaling curve.\n\n" +
            "BOSS\n" +
            "Gravelthorn the Earth Elemental (Heart of the Mountain).\n" +
            "Scaling-curve floor boss. Earth elemental; bleed and pierce\n" +
            "outperform blunt damage.\n\n" +
            "TIPS\n" +
            "Stage at Marten or push to F59 sandstorm canon. F58-F59 are\n" +
            "the cheap kit-up window before the Sleeping Knights gate at\n" +
            "F60.\n\n" +
            "SEE ALSO\n" +
            "[Floor 57] · [Floor 60] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f58" }
        },

        new("Floors", "Floor 59",
            "┌─ Floors\n" +
            "│ Topic: Floor 59\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Ruins (Danac, sandstorm)\n" +
            "│ Boss: Stonewyrm Basileus (Petrified Dragon King)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Danac — canon sandstorm town. Reduced visibility on the\n" +
            "overworld, calmer inside the labyrinth. Last frontier floor\n" +
            "before F60 Sleeping Knights HQ.\n\n" +
            "BOSS\n" +
            "Stonewyrm Basileus (Petrified Dragon King). Scaling-curve\n" +
            "floor boss. High DEF stone-scaled wyrm; ArmorPierce builds\n" +
            "shine here.\n\n" +
            "TIPS\n" +
            "Sandstorm reduces overworld vision — close-quarter combat\n" +
            "the field-boss spawns to keep them in your detection cone.\n" +
            "Stage karma to >= +50 before F60 if you want to recruit\n" +
            "Sleeping Knights on the next ascent.\n\n" +
            "SEE ALSO\n" +
            "[Floor 58] · [Floor 60] · [Karma & Alignment]")
        {
            Tags = new[] { "floors", "f59" }
        },

        new("Floors", "Floor 60",
            "┌─ Floors\n" +
            "│ Topic: Floor 60\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Ruins (mountain frontier)\n" +
            "│ Boss: The Armoured Stone Warrior (Golem of the Granite Throne)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "F60 is the canon multiples-of-ten quarter spike — second-\n" +
            "hardest tier inside Era III. Sleeping Knights HQ lives here\n" +
            "(Yuuki's elite order in canon Mother's Rosario), and Klein's\n" +
            "Kagutsuchi field boss roams the overworld. Stat-gate floor\n" +
            "for the steepest positive-karma guild in the game.\n\n" +
            "BOSS\n" +
            "The Armoured Stone Warrior (Golem of the Granite Throne).\n" +
            "Canon labyrinth boss. Granite plate frame — slow turn radius\n" +
            "but heavy uninterruptible swings. Reposition between phases.\n\n" +
            "DROPS\n" +
            "- Field boss: Kagutsuchi the Fire Samurai (Ember-Wielder of\n" +
            "  the Sixtieth Summit). Guaranteed drop: Spirit Sword\n" +
            "  Kagutsuchi (Fractured Daydream Legendary Katana, Klein's\n" +
            "  signature FD weapon).\n" +
            "- Floor-boss chest pool: standard expert-tier Legendaries.\n\n" +
            "NPCS / QUESTS\n" +
            "- Sleeping Knights HQ — Siune (recruiter), Yuuki (leader).\n" +
            "  Gate Lv50, karma >= +50 (highest positive-karma threshold\n" +
            "  in the game). Passive: +3 to all stats, +5% CritRate.\n" +
            "  Recruit: 10 late-floor mob kills. Signature: \"The Moon's\n" +
            "  Rest\" — Yuuki-crew themed push.\n\n" +
            "CANON\n" +
            "Sleeping Knights are Yuuki's guild from SAO LN vol 7 (the\n" +
            "Mother's Rosario arc). The arc is canonically post-Aincrad\n" +
            "in ALO; the implementation seats them here as the late-game\n" +
            "honorable-path capstone. Kagutsuchi is Fractured Daydream\n" +
            "canon: Klein's flame-named blade from the FD spinoff.\n\n" +
            "TIPS\n" +
            "Time the recruit pickup carefully — F60 is the FIRST floor\n" +
            "where the Lv50 + karma +50 gate can resolve, so plan an\n" +
            "honorable-path build to step in cleanly. Kagutsuchi roams\n" +
            "the overworld, never the Labyrinth — sweep the field before\n" +
            "engaging the floor boss. Klein-recruit allies pair perfectly\n" +
            "with Spirit Sword Kagutsuchi.\n\n" +
            "SEE ALSO\n" +
            "[Floor 55] · [Floor 65] · [Floor 70] · [Floor 76] · [Karma & Alignment] · [Guild System Overview] · [Recruitable Allies & Party System]")
        {
            Tags = new[] { "floors", "f60", "canon" }
        },

        new("Floors", "Floor 61",
            "┌─ Floors\n" +
            "│ Topic: Floor 61\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Volcanic (Selmburg fog lake)\n" +
            "│ Boss: Belzeroth the Pit Fiend (Duke of the Infernal Court)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Selmburg — canon perpetual-fog lake town. Volcanic biome\n" +
            "opens at the F61-F65 band. Anchors the Integral Factor Rosso\n" +
            "Series field-boss ladder; the Crimson Forneus encounter on\n" +
            "this floor seeds the F58-F64 banded Rosso chest pool.\n\n" +
            "BOSS\n" +
            "Belzeroth the Pit Fiend (Duke of the Infernal Court). Scaling-\n" +
            "curve floor boss. Fire-resistant fiend; Holy / Light damage\n" +
            "and bleed riders cut through best.\n\n" +
            "DROPS\n" +
            "- Field boss: Crimson Forneus (Demon of the Scarlet Depths).\n" +
            "  HP 4.0x, ATK 1.7x. Guaranteed drop: Rosso Forneus (Legendary\n" +
            "  1H Sword) plus Rosso Aegis (Legendary shield) on the\n" +
            "  secondary roll.\n" +
            "- Banded chest pool: F58-F64 chests roll the four IF Rosso\n" +
            "  series weapons (Rosso Albatross / Sigrun / Rhapsody /\n" +
            "  Dominion).\n\n" +
            "CANON\n" +
            "Integral Factor — Rosso Series. Selmburg's perpetual fog is\n" +
            "noted in the SAO Wiki canon entry; Crimson Forneus is the IF\n" +
            "field-boss anchor for the four-weapon Rosso chest band.\n\n" +
            "TIPS\n" +
            "Engage Crimson Forneus only with shield-up loadouts — its\n" +
            "ATK 1.7x rider crushes light builds. The Rosso Aegis shield\n" +
            "drop pairs naturally with KoB Vitality scaling. Fog reduces\n" +
            "overworld vision; pre-scout from the labyrinth entrance.\n\n" +
            "SEE ALSO\n" +
            "[Floor 60] · [Floor 65] · [Integral Factor Weapon Series] · [Floor 55]")
        {
            Tags = new[] { "floors", "f61", "canon" }
        },

        new("Floors", "Floor 62",
            "┌─ Floors\n" +
            "│ Topic: Floor 62\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Volcanic\n" +
            "│ Boss: Hellion the Chaos Dancer (Madness Made Manifest)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Mid-band volcanic ridge inside the IF Rosso chest band. No\n" +
            "canon hub. Follow the scaling curve.\n\n" +
            "BOSS\n" +
            "Hellion the Chaos Dancer (Madness Made Manifest). Scaling-\n" +
            "curve floor boss. Erratic move pattern; lock the room down\n" +
            "with a Slow status to predict its dance.\n\n" +
            "TIPS\n" +
            "F62-F64 chests roll the IF Rosso series weapons — clear\n" +
            "extra rooms for the banded drops. Volcanic environment ticks\n" +
            "2 dmg / 8 turns; pack heat-resist gear or fire potions.\n\n" +
            "SEE ALSO\n" +
            "[Floor 61] · [Floor 65] · [Integral Factor Weapon Series]")
        {
            Tags = new[] { "floors", "f62" }
        },

        new("Floors", "Floor 63",
            "┌─ Floors\n" +
            "│ Topic: Floor 63\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Volcanic\n" +
            "│ Boss: Ashborn the Ember Lich (Death That Burns)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Volcanic mid-band continues. No canon hub. Inside the IF\n" +
            "Rosso chest band; ride the scaling curve.\n\n" +
            "BOSS\n" +
            "Ashborn the Ember Lich (Death That Burns). Scaling-curve\n" +
            "floor boss. Undead caster; silence and stun riders shut down\n" +
            "its longest cast windows.\n\n" +
            "TIPS\n" +
            "Holy / Light cuts through undead lich frames. Continue\n" +
            "rolling chests for the Rosso banded drops.\n\n" +
            "SEE ALSO\n" +
            "[Floor 62] · [Floor 65] · [Integral Factor Weapon Series]")
        {
            Tags = new[] { "floors", "f63" }
        },

        new("Floors", "Floor 64",
            "┌─ Floors\n" +
            "│ Topic: Floor 64\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Volcanic\n" +
            "│ Boss: Moloch the Soul Furnace (The Hunger That Never Ends)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Last floor of the IF Rosso chest band. No canon hub. Volcanic\n" +
            "ridge; F65 Selka canon waits one floor up.\n\n" +
            "BOSS\n" +
            "Moloch the Soul Furnace (The Hunger That Never Ends). Scaling-\n" +
            "curve floor boss. Drains MP / SP on hit — bring potions and\n" +
            "avoid extended skill chains under his leech aura.\n\n" +
            "TIPS\n" +
            "Final floor for IF Rosso chest rolls — clear extra rooms\n" +
            "before pushing F65. Pre-stage karma for any Selka-side\n" +
            "considerations on the next floor (no karma gate, but the\n" +
            "Holy-Sword build path benefits from honorable rep).\n\n" +
            "SEE ALSO\n" +
            "[Floor 63] · [Floor 65] · [Integral Factor Weapon Series]")
        {
            Tags = new[] { "floors", "f64" }
        },

        new("Floors", "Floor 65",
            "┌─ Floors\n" +
            "│ Topic: Floor 65\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Volcanic (active lava cone)\n" +
            "│ Boss: Abaddon the Destroyer (Annihilation Incarnate)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Volcanic crater floor — active lava cone, obsidian fields,\n" +
            "ashfall, heat shimmer. Anchors Selka the Novice's chained\n" +
            "Alicization-canon quest line: two Divines back-to-back, base\n" +
            "Fragrant Olive Sword and the Memory-Defrag awakened variant.\n\n" +
            "BOSS\n" +
            "Abaddon the Destroyer (Annihilation Incarnate). Scaling-curve\n" +
            "floor boss with no canon-anchored mechanic. Heavy AoE, low\n" +
            "single-target precision — burst-damage builds outpace its\n" +
            "phase windows.\n\n" +
            "DROPS\n" +
            "Floor-boss chest pool: standard expert-tier Legendaries. The\n" +
            "Divine drops on this floor live in Selka's NPC quests, not\n" +
            "the boss.\n\n" +
            "NPCS / QUESTS\n" +
            "- Selka the Novice (white S). Quest 1 \"The Last Knight's\n" +
            "  Bequest\": 25 F65 kills → Fragrant Olive Sword (Divine 1H\n" +
            "  Sword, HolyAoE+15, SD+15) + 500 Col + 400 XP.\n" +
            "- Selka chained Quest 2 \"The Sword's Awakening\" (unlocks on\n" +
            "  Q1 turn-in): 30 F65+ kills → Unfolding Truth Fragrant\n" +
            "  Olive Sword (MD-awakened variant, stronger) + 800 Col +\n" +
            "  600 XP. Kills from any F65+ floor count toward the second\n" +
            "  counter, so push and farm in parallel.\n\n" +
            "CANON\n" +
            "Alicization — Selka is Alice's younger sister; Fragrant Olive\n" +
            "Sword is Alice's blade in the Underworld arc. The Unfolding\n" +
            "Truth awakening is Memory Defrag mobile-canon, where Selka\n" +
            "carries the awakening dialogue (\"awakening\", \"unfolding\n" +
            "truth\") used in the chained quest.\n\n" +
            "TIPS\n" +
            "Holy-Sword builds gain the most — Fragrant Olive Sword's\n" +
            "HolyAoE+15 stacks with Sacred Edge. Push straight into Q2\n" +
            "after Q1 turn-in; the 30 kills overlap with any standing\n" +
            "F65+ weapon-gated quest counters and HF HNM grinds at F79+.\n" +
            "Volcanic environment damage; pack fire-resist gear.\n\n" +
            "SEE ALSO\n" +
            "[Floor 60] · [Floor 70] · [Floor 78] · [Unique Skill: Holy Sword] · [MD Alicization Canonical Extras] · [Divine Object Set — Integrity Knights]")
        {
            Tags = new[] { "floors", "f65", "canon" }
        },

        new("Floors", "Floor 66",
            "┌─ Floors\n" +
            "│ Topic: Floor 66\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Void (cosmic era)\n" +
            "│ Boss: Void Sentinel Nyx (Watcher of the Starless Dark)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Void biome opens — F66-F70 cosmic era. +1 satiety drain,\n" +
            "1 dmg / 10 turns environment. No canon hub on F66 itself;\n" +
            "ride the curve.\n\n" +
            "BOSS\n" +
            "Void Sentinel Nyx (Watcher of the Starless Dark). Scaling-\n" +
            "curve floor boss. Reduced visibility in arena; rely on sound\n" +
            "and tile-feel rather than line-of-sight.\n\n" +
            "TIPS\n" +
            "Pack extra rations for the satiety tick. Light sources don't\n" +
            "cut the Void biome — bring Holy weapons that emit their own\n" +
            "ambient glow.\n\n" +
            "SEE ALSO\n" +
            "[Floor 65] · [Floor 70] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f66" }
        },

        new("Floors", "Floor 67",
            "┌─ Floors\n" +
            "│ Topic: Floor 67\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Void\n" +
            "│ Boss: Cosmolith the Star Eater (The Gravity That Devours)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Void mid-band. No canon hub. Continued environment tick.\n\n" +
            "BOSS\n" +
            "Cosmolith the Star Eater (The Gravity That Devours). Scaling-\n" +
            "curve floor boss. Pulls you toward a center tile each phase\n" +
            "transition — kite outward to deny the gravity well.\n\n" +
            "TIPS\n" +
            "Movement speed counters the gravity pull. Sticky-foot effects\n" +
            "from ground hazards compound badly here; clear the arena of\n" +
            "tar / ice / web before engaging.\n\n" +
            "SEE ALSO\n" +
            "[Floor 66] · [Floor 70] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f67" }
        },

        new("Floors", "Floor 68",
            "┌─ Floors\n" +
            "│ Topic: Floor 68\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Void (IF event-floor flavor)\n" +
            "│ Boss: Etheron the Phase Shifter (The Boss Between Dimensions)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Void biome. Canon-flavored as an Integral Factor event-floor\n" +
            "underground cave — implementation keeps the Void atmosphere\n" +
            "intact. No canon NPC anchor.\n\n" +
            "BOSS\n" +
            "Etheron the Phase Shifter (The Boss Between Dimensions).\n" +
            "Scaling-curve floor boss. Phase-skip ability — windows where\n" +
            "it's untargetable; bait the un-phase, then commit burst.\n\n" +
            "TIPS\n" +
            "Track the phase rhythm — Etheron drops out of view on a\n" +
            "predictable cooldown. Save your highest-burst skill for the\n" +
            "rephase window.\n\n" +
            "SEE ALSO\n" +
            "[Floor 67] · [Floor 70] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f68" }
        },

        new("Floors", "Floor 69",
            "┌─ Floors\n" +
            "│ Topic: Floor 69\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Void (reptilian-tribe flavor)\n" +
            "│ Boss: Nebulord Vortex (Storm of Collapsing Stars)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Void biome continues. Canon-flavored as a reptilian-tribe\n" +
            "desert per the lore docs; implementation keeps the Void\n" +
            "skyline as the dominant tone. No canon NPC anchor.\n\n" +
            "BOSS\n" +
            "Nebulord Vortex (Storm of Collapsing Stars). Scaling-curve\n" +
            "floor boss. Spiral AoE with rotating safe-tiles — read the\n" +
            "rotation, step into the sweet spot, swing on the off-beat.\n\n" +
            "TIPS\n" +
            "Last Void floor before F70 Susanoo field-boss canon. Bank\n" +
            "consumables for the Klein-canon spike.\n\n" +
            "SEE ALSO\n" +
            "[Floor 68] · [Floor 70] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f69" }
        },

        new("Floors", "Floor 70",
            "┌─ Floors\n" +
            "│ Topic: Floor 70\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Void (cosmic era closes)\n" +
            "│ Boss: Celestine the Radiant (Light That Blinds and Burns)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Canon multiples-of-ten quarter floor. Closes the cosmic-Void\n" +
            "band; the Ice band opens at F71. No canon labyrinth-boss\n" +
            "anchor, but Klein's second Spirit Sword field-boss roams\n" +
            "the overworld here.\n\n" +
            "BOSS\n" +
            "Celestine the Radiant (Light That Blinds and Burns). Scaling-\n" +
            "curve floor boss. Inflicts Blind on phase transitions — pre-\n" +
            "stage cleansing potions or status-immune armor.\n\n" +
            "DROPS\n" +
            "- Field boss: Susanoo the Storm Blade (Thunder-Wielder of the\n" +
            "  Seventieth Vault). HP 3.2x, ATK 1.6x. Guaranteed: Spirit\n" +
            "  Sword Susanoo (Fractured Daydream Legendary Katana — second\n" +
            "  half of Klein's FD pair, complementing Kagutsuchi at F60).\n" +
            "- Floor-boss chest pool: standard expert-tier Legendaries.\n\n" +
            "CANON\n" +
            "Susanoo is Fractured Daydream canon — Klein's storm-cloud-\n" +
            "splitter blade from FD, paired with his Kagutsuchi flame\n" +
            "drop on F60. The mythological reference is Susanoo's cloud-\n" +
            "splitting sword from LN-canon Japanese myth.\n\n" +
            "TIPS\n" +
            "Klein FD-canon completion run: kill Kagutsuchi on F60, then\n" +
            "sweep F70 for Susanoo, hand both to a Klein-recruit ally for\n" +
            "the canon-paired loadout. Sweep before engaging Celestine —\n" +
            "the Blind status makes wandering field encounters unsafe\n" +
            "post-fight.\n\n" +
            "SEE ALSO\n" +
            "[Floor 60] · [Floor 75] · [Fractured Daydream Character Weapons] · [Recruitable Allies & Party System]")
        {
            Tags = new[] { "floors", "f70" }
        },

        new("Floors", "Floor 71",
            "┌─ Floors\n" +
            "│ Topic: Floor 71\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Ice (legendary era opens)\n" +
            "│ Boss: Stormbringer Raijin (The Thunder God's Wrath)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Ice band opens — F71-F75 legendary era. Slip hazards return,\n" +
            "vision penalties intensify. No canon hub on F71; ride the\n" +
            "scaling curve toward Gleam Eyes at F74.\n\n" +
            "BOSS\n" +
            "Stormbringer Raijin (The Thunder God's Wrath). Scaling-curve\n" +
            "floor boss. Lightning AoE; conductive armor still risky.\n\n" +
            "TIPS\n" +
            "Stage F71-F73 conservatively — F74 Gleam Eyes is the canon\n" +
            "Aincrad-arc spike before F75 Skull Reaper. Bank consumables;\n" +
            "respect the slip hazard on overworld ice.\n\n" +
            "SEE ALSO\n" +
            "[Floor 70] · [Floor 74] · [Floor 75]")
        {
            Tags = new[] { "floors", "f71" }
        },

        new("Floors", "Floor 72",
            "┌─ Floors\n" +
            "│ Topic: Floor 72\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Ice (Kales'Oh, remote elven ruins)\n" +
            "│ Boss: Bloodfang the Vampire Lord (Eternal Night's Master)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Kales'Oh — canon-flavored remote-island elven ruins. No canon\n" +
            "NPC anchor; ride the curve.\n\n" +
            "BOSS\n" +
            "Bloodfang the Vampire Lord (Eternal Night's Master). Scaling-\n" +
            "curve floor boss. Life-drain riders heal him on hit — burst\n" +
            "windows beat sustained DPS.\n\n" +
            "TIPS\n" +
            "Holy / Light damage cuts hardest. Avoid bleed-on-self riders\n" +
            "(thorns, retaliation procs) — Bloodfang's life-drain feeds on\n" +
            "those.\n\n" +
            "SEE ALSO\n" +
            "[Floor 71] · [Floor 74]")
        {
            Tags = new[] { "floors", "f72" }
        },

        new("Floors", "Floor 73",
            "┌─ Floors\n" +
            "│ Topic: Floor 73\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Ice\n" +
            "│ Boss: Deathweaver Arachne (Mother of All Spiders)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Last unmarked floor before F74 Gleam Eyes canon. Ice band\n" +
            "continues. No canon hub; bank kit and consumables for the\n" +
            "two-floor canon spike.\n\n" +
            "BOSS\n" +
            "Deathweaver Arachne (Mother of All Spiders). Scaling-curve\n" +
            "floor boss. Web tiles slow movement — clear them between\n" +
            "phases or pre-stack movement-speed boosts.\n\n" +
            "TIPS\n" +
            "Kit check before F74: 1H Sword OHS-kill counter at 50 if\n" +
            "you're chasing the Dual Blades unique-skill unlock; the\n" +
            "milestone fires on the next OHS kill regardless of floor.\n\n" +
            "SEE ALSO\n" +
            "[Floor 72] · [Floor 74] · [Unique Skill: Dual Blades]")
        {
            Tags = new[] { "floors", "f73" }
        },

        new("Floors", "Floor 74",
            "┌─ Floors\n" +
            "│ Topic: Floor 74\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Ice (Collinia plains, overcast)\n" +
            "│ Boss: The Gleam Eyes (The Blue Demon of Floor 74)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Collinia / Kamdet — canon overcast cliffs and plains. Tense\n" +
            "weather, narrow ridges. F74 is the canon Gleam Eyes floor:\n" +
            "the blue-skinned, goat-headed demon with a zanbato that the\n" +
            "Aincrad arc remembers as the moment Kirito reveals Dual\n" +
            "Blades.\n\n" +
            "BOSS\n" +
            "The Gleam Eyes (The Blue Demon of Floor 74). Canon labyrinth\n" +
            "boss. Goat-headed humanoid demon, zanbato two-hander, purple-\n" +
            "energy aura. Heavy single-target combo pressure — interrupt\n" +
            "the wind-up windows, kite the cleave.\n\n" +
            "DROPS\n" +
            "Floor-boss chest pool: standard expert-tier Legendaries.\n\n" +
            "CANON\n" +
            "SAO LN vol 1 / Anime Ep 9 — the Gleam Eyes raid is the canon\n" +
            "moment Kirito reveals his Dual Blades unique skill. In our\n" +
            "codebase, the unique-skill flavor string anchors Dual Blades\n" +
            "to F74, but the actual unlock is kill-count milestone driven:\n" +
            "50 1H-Sword (OHS) kills, regardless of floor. F74 is canon-\n" +
            "anchor flavor, not a coded gate. If your OHS counter hits 50\n" +
            "anywhere from F1 onward, the System flag fires.\n\n" +
            "TIPS\n" +
            "Stack the OHS kill counter on the way up — by F74 most\n" +
            "single-handed builds clear the milestone naturally. Bring\n" +
            "Elucidator (F50 LAB drop) + Dark Repulser (F55 wyrm or F48\n" +
            "Lisbeth gift) as your canon dual-wield kit; if Dual Blades\n" +
            "unlocks during the Gleam Eyes fight, swap to dual-wield mid-\n" +
            "phase for the canon-perfect kill. Save your highest-burst\n" +
            "skill for the zanbato wind-up window.\n\n" +
            "SEE ALSO\n" +
            "[Floor 50] · [Floor 55] · [Floor 75] · [Unique Skill: Dual Blades] · [Pair Resonance]")
        {
            Tags = new[] { "floors", "f74", "canon" }
        },

        new("Floors", "Floor 75",
            "┌─ Floors\n" +
            "│ Topic: Floor 75\n" +
            "│ Tier: 3 (expert)\n" +
            "│ Biome: Ice (Collinia Labyrinth, bone arena)\n" +
            "│ Boss: The Skull Reaper (Death's Scythe)\n" +
            "└─\n" +
            "\n" +
            "SUMMARY\n" +
            "Collinia Labyrinth — the last frontline town in canon, the\n" +
            "bone-dungeon arena, the dread red gloom. F75 is the canon\n" +
            "endpoint of the Aincrad arc proper: in the LN/anime this is\n" +
            "where Heathcliff reveals himself as Kayaba, in our codebase\n" +
            "this is the Skull Reaper raid that breaks the frontline.\n" +
            "Quarter-floor canon spike — second-hardest tier inside Era\n" +
            "III. Hidden Laughing Coffin hideout sits on this floor for\n" +
            "Outlaw-tier players.\n\n" +
            "BOSS\n" +
            "The Skull Reaper (Death's Scythe). Canon labyrinth boss. In\n" +
            "the LN, Skull Reaper killed 14 high-level players before the\n" +
            "raid won — lethal cleave windows, two-arm scythe, centipede\n" +
            "frame. Phase-1 Devastating Charge ability rolls in here\n" +
            "(2.5x multiplier, cooldown 5) on top of the standard expert-\n" +
            "tier scaling.\n\n" +
            "DROPS\n" +
            "- Floor-boss guaranteed: Masamune (Divine Katana — Hollow\n" +
            "  Realization mythological apex blade). Drops on the kill\n" +
            "  regardless of last-attack.\n" +
            "- Floor-boss chest pool: standard expert-tier Legendaries.\n\n" +
            "NPCS / QUESTS\n" +
            "Laughing Coffin hidden hideout (PoH's Herald). Gate Lv30,\n" +
            "karma <= -50 (Outlaw tier only — entrance closes if you\n" +
            "bounce above -50). Passive: +20% BackstabDmg. Signature\n" +
            "\"Crimson Letter\" demands 5 NPC kills. Town Guards aggro at\n" +
            "F1 plaza while LC is active.\n\n" +
            "CANON\n" +
            "SAO LN vol 1 / Anime Ep 14. F75 is the arc-climax floor —\n" +
            "the boss raid where the canon assault clears Aincrad's lethal\n" +
            "centipede and Heathcliff (Kayaba) reveals himself as the\n" +
            "creator. Important reconciliation: in this codebase, the\n" +
            "Heathcliff fight does not happen here. F75's labyrinth boss\n" +
            "is The Skull Reaper, and the player-clone Heathcliff's Shadow\n" +
            "fight lives at F99 instead. The canon Heathcliff narrative\n" +
            "beat anchors F75 spiritually — the Holy Sword unique-skill\n" +
            "flavor string still references F75 — but the unique-skill\n" +
            "unlock is kill-count milestone driven (no F75 code branch),\n" +
            "and the literal Heathcliff confrontation is moved to the\n" +
            "F99 ascent. Laughing Coffin is canon LN — PoH's player-killer\n" +
            "guild whose hidden hub in canon sits on a high floor; the\n" +
            "implementation seats it here.\n\n" +
            "TIPS\n" +
            "Bring Dual Blades online before this fight if you possibly\n" +
            "can — the Elucidator + Dark Repulser pair with the Pair\n" +
            "Resonance bonus is the canon kit. Skull Reaper's centipede\n" +
            "cleave demands a wide arena — pre-clear minions before\n" +
            "engaging. Bank Masamune for a Klein-recruit ally if your\n" +
            "build is already saturated on Katana. The Laughing Coffin\n" +
            "hideout only opens at karma <= -50 — Honorable-path runs\n" +
            "will not see the entrance at all (it shows as locked terrain).\n\n" +
            "SEE ALSO\n" +
            "[Floor 50] · [Floor 55] · [Floor 74] · [Floor 99] · [Unique Skill: Dual Blades] · [Pair Resonance] · [Karma & Alignment] · [Run Modifiers (12 Optional Challenges)]")
        {
            Tags = new[] { "floors", "f75", "canon" }
        },

        new("Floors", "Floor 76",
            "┌─ Floors\n" +
            "│ Topic: Floor 76\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Dark\n" +
            "│ Boss: The Ghastlygaze (The All-Seeing Abomination)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "First floor past the canon endpoint — the climb survivors who\n" +
            "kept going after Heathcliff. Algade revisited as the Hollow-\n" +
            "Fragment casino district, all neon and dark glass over a Dark-\n" +
            "biome demigod-tier scaling band. Lv 162, 5316 HP boss, 215 ATK.\n\n" +
            "BOSS\n" +
            "The Ghastlygaze (HF canon). Eye-studded horror. By F76 the\n" +
            "boss kit is fully loaded: Devastating Charge opener, Ground\n" +
            "Slam phase 2, Toxic Breath status, and 10% Regeneration on\n" +
            "phase 3. No floor-boss guaranteed Divine.\n\n" +
            "DROPS\n" +
            "- Floor-boss: standard chest pool — Tier-4 Legendaries, high-\n" +
            "  tier ingots, no guaranteed Divine.\n" +
            "- No field bosses rostered for F76.\n\n" +
            "NPCS / QUESTS\n" +
            "Jun (Sleeping Knights memorial) — quest 'The Sleeping Knights'\n" +
            "Tribute', 15 F76 kills returns Mother's Rosario (Legendary\n" +
            "Rapier, ComboBonus+50, 11-hit Original Sword Skill) plus 700\n" +
            "Col / 550 XP. One-shot per save.\n\n" +
            "CANON\n" +
            "Mother's Rosario (LN vol 7): Yuuki Konno's blade and the only\n" +
            "Original Sword Skill in canon ALO. F76 placement is\n" +
            "implementation canon — the MR arc takes place post-Aincrad in\n" +
            "the original LN. Jun is a Sleeping Knights survivor tending\n" +
            "the memorial.\n\n" +
            "TIPS\n" +
            "Stack the 15 Jun kills with any standing F76 weapon-Kill\n" +
            "quests — counters overlap. The 11-hit OSS is the longest\n" +
            "combo in the game; pair with Combo Finisher for double-damage\n" +
            "on the final strike. F76 is also where the F79+ HF questgiver\n" +
            "chain begins — pace the climb so you don't burn the rapier\n" +
            "on a single push.\n\n" +
            "SEE ALSO\n" +
            "[Floor 60] · [Floor 79] · [Combo Attacks] · [Pair Resonance]")
        {
            Tags = new[] { "floors", "f76", "canon" }
        },

        new("Floors", "Floor 77",
            "┌─ Floors\n" +
            "│ Topic: Floor 77\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Dark\n" +
            "│ Boss: The Crystalize Claw (Prismatic Scorpion of Floor 77)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Hollow-Fragment hill-zone floor. Dark biome, demigod scaling.\n" +
            "Lv 164, 5424 HP boss, 219 ATK. Implement-System questgiver\n" +
            "band starts here in spirit, formal NPCs from F79.\n\n" +
            "BOSS\n" +
            "The Crystalize Claw (HF canon). Prismatic scorpion — refractive\n" +
            "carapace, claw cleaves. Standard F75+ kit (Devastating Charge\n" +
            "+ Ground Slam + Toxic Breath + Regen).\n\n" +
            "DROPS\n" +
            "- Floor-boss: standard chest pool, no guaranteed Divine.\n" +
            "- Field boss: Goblin Leader (Warchief of the Kobold Host) →\n" +
            "  Mace of Asclepius (HF Legendary Mace, HPRegen+3).\n\n" +
            "TIPS\n" +
            "Field-boss spawn is overworld; Goblin Leader plays cleanly into\n" +
            "any caster build that wants HPRegen. Bring AoE for the kobold\n" +
            "host adds.\n\n" +
            "SEE ALSO\n" +
            "[Floor 76] · [Floor 79] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f77" }
        },

        new("Floors", "Floor 78",
            "┌─ Floors\n" +
            "│ Topic: Floor 78\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Dark\n" +
            "│ Boss: The Horn of Madness (Berserker of the Endless Maze)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Dark-biome maze floor. Lv 166, 5532 HP boss, 222 ATK. Anchor\n" +
            "for Dorothy's Last-Recollection canon line — the only Divine\n" +
            "Scythe in the game lives here.\n\n" +
            "BOSS\n" +
            "The Horn of Madness (HF canon). Berserker of the Endless Maze\n" +
            "— minotaur-frame berserker, charge attacks chain in tight\n" +
            "labyrinth corridors. Full F75+ kit on top of standard scaling.\n\n" +
            "DROPS\n" +
            "- Floor-boss: standard chest pool, no guaranteed Divine.\n" +
            "- No field bosses rostered for F78.\n\n" +
            "NPCS / QUESTS\n" +
            "Dorothy (BrightCyan 'D') — quest 'Purify the Darkness',\n" +
            "22 F78 kills returns Starlight Banner (Divine Scythe,\n" +
            "HolyAoE+20, range 2) plus 700 Col / 550 XP. One-shot per save;\n" +
            "no chained follow-up.\n\n" +
            "CANON\n" +
            "Last Recollection: Dorothy is a canon LR character, scythe-\n" +
            "wielder. Starlight Banner is her purification scythe in LR.\n" +
            "Her base scythe Azuretear (also LR) is not in implementation\n" +
            "as a named drop.\n\n" +
            "TIPS\n" +
            "Bring Scythe proficiency into the F78 push if you intend to\n" +
            "wield Starlight Banner on receipt — its 2-range reach shines\n" +
            "on Scythe builds. Stack the 22 kills with any standing F78\n" +
            "weapon-gated Kill quest. Dorothy completes the NPC-quest\n" +
            "Divine trio with Sister Azariya (F50) and Selka (F65) before\n" +
            "F80.\n\n" +
            "SEE ALSO\n" +
            "[Floor 50] · [Floor 65] · [Divine Object Set — Integrity Knights]")
        {
            Tags = new[] { "floors", "f78", "canon" }
        },

        new("Floors", "Floor 79",
            "┌─ Floors\n" +
            "│ Topic: Floor 79\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Dark\n" +
            "│ Boss: The Tempest of Trihead (Three-Headed Storm Hydra)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Dark-biome ice-plain frontier per HF canon. Lv 168, 5640 HP\n" +
            "boss, 226 ATK. First formal Hollow-Fragment Implement-System\n" +
            "questgiver floor.\n\n" +
            "BOSS\n" +
            "The Tempest of Trihead (HF canon). Three-headed storm hydra —\n" +
            "lightning attacks alongside the F75+ kit. Spread melee,\n" +
            "the heads share a hitbox on a 2-tile cone.\n\n" +
            "NPCS / QUESTS\n" +
            "Scholar Ellroy (HF HNM questgiver) — 15 F79 kills returns\n" +
            "Infinite Ouroboros (HF Mace, Barrier+20). Standard one-shot\n" +
            "per save.\n\n" +
            "TIPS\n" +
            "Lightning weapons drop F79+ chests on the standard pool — pre-\n" +
            "stage one before the boss for the multi-head pressure. Pack\n" +
            "antidotes for Toxic Breath spillover.\n\n" +
            "SEE ALSO\n" +
            "[Floor 80] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f79" }
        },

        new("Floors", "Floor 80",
            "┌─ Floors\n" +
            "│ Topic: Floor 80\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Dark\n" +
            "│ Boss: The Guilty Scythe (Reaper of the Eightieth Floor)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Sky-sanctum floor in HF canon — Dark biome, demigod scaling.\n" +
            "Lv 170, 5750 HP boss, 229 ATK. Multiples-of-10 spike floor.\n" +
            "Heavy field-boss roster: two Legendaries plus an HF questgiver.\n\n" +
            "BOSS\n" +
            "The Guilty Scythe (HF canon). Reaper of the Eightieth Floor —\n" +
            "scythe-wielding revenant. Standard F75+ kit; the scythe's\n" +
            "phase-2 cleave doubles as the Ground Slam radius cone, so\n" +
            "spacing collapses fast in the boss room.\n\n" +
            "DROPS\n" +
            "- Floor-boss: standard chest pool, no guaranteed Divine.\n" +
            "- Field boss: Soul Binder (Wraith of the Gathered Hymns) →\n" +
            "  Arcaneblade: Soul Binder (HF Legendary Scimitar, SP gain on\n" +
            "  hit).\n" +
            "- Field boss: Pyre Lord of Heathcliff (Red-Hand of the\n" +
            "  Eightieth Flame) → Flame Lord (FD Heathcliff Legendary\n" +
            "  2H Sword). HP 3.8x, ATK 1.7x.\n" +
            "- Chest band: Black Iron Dual Sword A & B (Underworld Kirito\n" +
            "  early-arc pair, AL canon F78-F84 chest band).\n\n" +
            "NPCS / QUESTS\n" +
            "Hunter Kojiro (HF HNM questgiver) — 15 F80 kills returns Jato:\n" +
            "Onikiri-maru (HF Katana, Bleed+20).\n\n" +
            "CANON\n" +
            "The Guilty Scythe is HF F80 boss canon. Pyre Lord references\n" +
            "Heathcliff via Fractured Daydream — the FD line is post-canon\n" +
            "Aincrad-arc scenario filler. Black Iron Dual Swords are Kirito's\n" +
            "early Underworld pair from Alicization.\n\n" +
            "TIPS\n" +
            "F80 is a chest-pool jackpot floor — clear Soul Binder before\n" +
            "the boss for SP-on-hit fuel into the Pyre Lord fight, then\n" +
            "stack the Pyre Lord 2H Sword for the floor-boss kill. The\n" +
            "Black Iron Dual Swords are the cleanest pre-Dual-Blades\n" +
            "training weapons in the band.\n\n" +
            "SEE ALSO\n" +
            "[Floor 79] · [Floor 85] · [Fractured Daydream Character Weapons]")
        {
            Tags = new[] { "floors", "f80", "canon" }
        },

        new("Floors", "Floor 81",
            "┌─ Floors\n" +
            "│ Topic: Floor 81\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Swamp\n" +
            "│ Boss: The Knight of Darkness (Black Paladin of the Void Keep)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Swamp biome — 8% step-poison hazard. Lv 172, 5862 HP boss,\n" +
            "233 ATK. Hollow-Fragment nightmare-difficulty band opens here.\n\n" +
            "BOSS\n" +
            "The Knight of Darkness (HF canon). Black-armor paladin with\n" +
            "shadow-aura strikes. Standard F75+ kit. Watch the boss's phase-2\n" +
            "Devastating Charge while you're losing HP to swamp poison.\n\n" +
            "NPCS / QUESTS\n" +
            "Ranger Torva (HF HNM questgiver) — 15 F81 kills returns\n" +
            "Fiendblade: Deathbringer (HF 1H Sword).\n\n" +
            "TIPS\n" +
            "Antidote tier-up before the climb — F81 swamp + Toxic Breath\n" +
            "phase-2 stacks twice. The 8% step-poison is the actual killer\n" +
            "on long traversal.\n\n" +
            "SEE ALSO\n" +
            "[Floor 80] · [Floor 82] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f81" }
        },

        new("Floors", "Floor 82",
            "┌─ Floors\n" +
            "│ Topic: Floor 82\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Swamp\n" +
            "│ Boss: The Legacy of Grand (The Ancient Construct Guardian)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Swamp biome, HF nightmare band. Lv 174, 5974 HP boss, 236 ATK.\n" +
            "Guaranteed Divine drop floor — Hexagramme.\n\n" +
            "BOSS\n" +
            "The Legacy of Grand (HF canon). Ancient construct — heavy DEF\n" +
            "(153), so DPS-per-tick matters more than burst. Standard F75+\n" +
            "kit. The construct's Regeneration phase is the timing window\n" +
            "the fight is built around.\n\n" +
            "DROPS\n" +
            "- Floor-boss guaranteed: Hexagramme — Divine Rapier, hex-\n" +
            "  pattern arcane piercing.\n" +
            "- No field bosses rostered for F82.\n\n" +
            "TIPS\n" +
            "Hexagramme is one of the few Divine Rapiers in the run — bring\n" +
            "Rapier proficiency if you intend to wield it on pickup. ATK-\n" +
            "burst comps can race the construct's Regeneration tick by\n" +
            "killing during phase-1 before it stabilizes.\n\n" +
            "SEE ALSO\n" +
            "[Floor 81] · [Floor 83] · [Divine Weapons — Roster & Acquisition]")
        {
            Tags = new[] { "floors", "f82" }
        },

        new("Floors", "Floor 83",
            "┌─ Floors\n" +
            "│ Topic: Floor 83\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Swamp\n" +
            "│ Boss: The Horn of Furious (Flame-Aura Minotaur Lord)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Swamp biome, HF nightmare band. Lv 176, 6088 HP boss, 240 ATK.\n" +
            "Two HF field bosses anchor the chest pool.\n\n" +
            "BOSS\n" +
            "The Horn of Furious (HF canon). Flame-aura minotaur lord —\n" +
            "the F78 Horn of Madness's bigger brother. Charge-attacks chain\n" +
            "alongside the F75+ kit; the flame aura adds passive burn on\n" +
            "adjacency.\n\n" +
            "DROPS\n" +
            "- Field boss: Arboreal Fear (Horror of the Hanging Grove) →\n" +
            "  Demonspear: Gae Bolg (HF Spear, Cú Chulainn myth).\n" +
            "- Field boss: Ruinous Herald (Doom-Prophet of the Falling\n" +
            "  Tower) → Fellblade: Ruinous Doom (HF Scimitar).\n\n" +
            "NPCS / QUESTS\n" +
            "Apiarist Nell (HF HNM questgiver) — 15 F83 kills returns\n" +
            "Fayblade: Tizona (HF 1H Sword).\n\n" +
            "TIPS\n" +
            "Stand off the burn aura — Spear or Scimitar reach pays for\n" +
            "itself here. Take Gae Bolg early; its myth-pierce nullifies a\n" +
            "lot of the boss's DEF.\n\n" +
            "SEE ALSO\n" +
            "[Floor 82] · [Floor 84] · [Floor Scaling Formulas]")
        {
            Tags = new[] { "floors", "f83" }
        },

        new("Floors", "Floor 84",
            "┌─ Floors\n" +
            "│ Topic: Floor 84\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Swamp\n" +
            "│ Boss: The Queen of Ant (Matriarch of the Insect Deeps)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Swamp biome, HF nightmare band. Lv 178, 6202 HP boss, 243 ATK.\n" +
            "Guaranteed Divine drop — Caladbolg. The Maelstrom (F85) waits\n" +
            "right after.\n\n" +
            "BOSS\n" +
            "The Queen of Ant (HF canon). Matriarch — summons swarms via\n" +
            "Call Reinforcements every cooldown, then the F75+ kit on top.\n" +
            "The reinforcement count scales by floor — expect 4+ minions\n" +
            "per summon at this depth.\n\n" +
            "DROPS\n" +
            "- Floor-boss guaranteed: Caladbolg — Divine, Irish mythic\n" +
            "  spear (Fergus mac Roich's blade in Irish myth).\n" +
            "- No field bosses rostered for F84.\n\n" +
            "NPCS / QUESTS\n" +
            "Spiralist Vey (HF HNM questgiver) — 10 F84 kills returns\n" +
            "Spiralblade: Rendering Fail.\n\n" +
            "TIPS\n" +
            "AoE is mandatory — the swarm trickle outpaces single-target\n" +
            "kills before the Queen's HP bar moves. Caladbolg pickup is the\n" +
            "second Divine Spear in the run; pair with Spear-proficiency\n" +
            "build for guaranteed wield.\n\n" +
            "SEE ALSO\n" +
            "[Floor 83] · [Floor 85] · [Divine Weapons — Roster & Acquisition]")
        {
            Tags = new[] { "floors", "f84" }
        },

        new("Floors", "Floor 85",
            "┌─ Floors\n" +
            "│ Topic: Floor 85\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Swamp\n" +
            "│ Boss: The Maelstrom of Trihead (Upgraded Storm Hydra)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Volcanic-ruin swamp, Hollow-Fragment nightmare band. Lv 180,\n" +
            "6318 HP boss, 247 ATK. Triple-loot floor — Black Lily Sword\n" +
            "(Divine), Macafitel (FD Yuuki rapier), Godblade Dragonslayer,\n" +
            "Bow Zephyros LAB. Heaviest haul before F95.\n\n" +
            "BOSS\n" +
            "The Maelstrom of Trihead (HF canon). Upgraded storm hydra —\n" +
            "the F79 Tempest with another phase and faster head respawns.\n" +
            "F75+ kit on top of the storm-cone signature.\n\n" +
            "DROPS\n" +
            "- Last-Attack Bonus (your killing blow only): Bow Zephyros\n" +
            "  (Legendary Bow, non-enhanceable — Infinity Moment canon).\n" +
            "  Ally finisher forfeits it.\n" +
            "- Field boss: The Silent Edge (Wielder of the Severing Blade)\n" +
            "  → Black Lily Sword (Divine 1H Sword — Sheyta's blade,\n" +
            "  Alicization canon, severing strike).\n" +
            "- Field boss: Abased Beast (Fallen Wyrm of the Crimson Cliff)\n" +
            "  → Godblade: Dragonslayer (HF, +damage vs dragons, 10% LAB).\n" +
            "- Field boss: Yuuki's Echo (Absolute-Sword Revenant of the\n" +
            "  Eighty-Fifth) → Macafitel (FD Yuuki rapier, HP 4.0x ATK 1.75x).\n\n" +
            "NPCS / QUESTS\n" +
            "Crusher Drago (HF HNM questgiver) — 10 F85 kills returns\n" +
            "Crusher: Bond Cyclone (HF 2H Axe).\n\n" +
            "CANON\n" +
            "Black Lily Sword (Alicization): Sheyta the Silent's blade —\n" +
            "the Integrity Knight whose name no one says above a whisper.\n" +
            "F85 Silent Edge is the canonical drop path; F98's floor-boss\n" +
            "guaranteed Black Lily is a duplicate that exists in code, but\n" +
            "Silent Edge is the canonical kill. Yuuki's Echo references\n" +
            "LN vol 7 (Mother's Rosario / Sleeping Knights); Macafitel is\n" +
            "Yuuki's FD rapier in canon.\n\n" +
            "TIPS\n" +
            "Don't try to clear all four field-boss-tier kills in one\n" +
            "delve — Silent Edge alone is a stat-check fight at HP 4.0x\n" +
            "scaling. Land the Maelstrom killing blow yourself for\n" +
            "Zephyros, then circle back for Silent Edge after a town reset.\n" +
            "Black Lily's severing strike pairs with the F95 Time Piercing\n" +
            "Sword for a full Sheyta/Bercouli Integrity-Knight sweep.\n\n" +
            "SEE ALSO\n" +
            "[Floor 78] · [Floor 95] · [Divine Weapons — Roster & Acquisition] · [Fractured Daydream Character Weapons]")
        {
            Tags = new[] { "floors", "f85", "canon", "divine" }
        },

        new("Floors", "Floor 86",
            "┌─ Floors\n" +
            "│ Topic: Floor 86\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Void\n" +
            "│ Boss: The King of Skeleton (Lich-Lord of the Bone Throne)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Void biome — random status warps + reality flicker. Lv 182,\n" +
            "6434 HP boss, 251 ATK. Guaranteed Divine drop floor — Tyrfing.\n\n" +
            "BOSS\n" +
            "The King of Skeleton (HF canon). Lich-Lord — necromancer kit\n" +
            "stacks Call Reinforcements undead with the floor's Void status\n" +
            "warps. Standard F75+ on top.\n\n" +
            "DROPS\n" +
            "- Floor-boss guaranteed: Tyrfing — Divine 1H Sword, Norse-myth\n" +
            "  cursed blade.\n" +
            "- Field boss: Fellaxe Revenant (Demon-Shade of the Crimson\n" +
            "  Reap) → Fellaxe: Demon's Scythe (HF 2H Axe).\n\n" +
            "TIPS\n" +
            "Void warps rotate your statuses unpredictably — pack broad-\n" +
            "spectrum cures rather than the F81-specific antidote stack.\n" +
            "Tyrfing's curse stat plays well with corruption-build comps.\n\n" +
            "SEE ALSO\n" +
            "[Floor 85] · [Floor 88] · [Divine Weapons — Roster & Acquisition]")
        {
            Tags = new[] { "floors", "f86" }
        },

        new("Floors", "Floor 87",
            "┌─ Floors\n" +
            "│ Topic: Floor 87\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Void\n" +
            "│ Boss: The Radiance Eater (The Light-Devouring Beast)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Void biome, godlike-tier scaling. Lv 184, 6552 HP boss, 254\n" +
            "ATK. Guaranteed Iron Maiden Dagger Divine, plus the Integral\n" +
            "Factor Yasha series anchor.\n\n" +
            "BOSS\n" +
            "The Radiance Eater (HF canon). Light-devouring beast — turns\n" +
            "Holy/Light damage into self-heal during phase 2. Bring non-\n" +
            "Holy backup or eat the Regeneration tick fully timed.\n\n" +
            "DROPS\n" +
            "- Floor-boss guaranteed: Iron Maiden Dagger — Divine, caged-\n" +
            "  pain dagger.\n" +
            "- Field boss: Yasha the Night Demon (Demon-Warrior of the\n" +
            "  Moonless Path) → Yasha Astaroth (IF Legendary 1H Sword) +\n" +
            "  Yasha Kavacha (Legendary shield). IF Yasha Series anchor.\n" +
            "- Field boss: Night Stalker (Predator of the Moonless Halls)\n" +
            "  → Saintblade: Durandal (HF 2H Sword).\n\n" +
            "CANON\n" +
            "Yasha series (Integral Factor): five-weapon banded chest pool\n" +
            "across F84-F90 unlocks once Yasha falls — the F87 kill is the\n" +
            "anchor that opens the rest. Saintblade Durandal is HF / Roland\n" +
            "myth.\n\n" +
            "TIPS\n" +
            "Skip Holy weapons in this fight — feeding the Eater extends\n" +
            "the kill window past the Regeneration cycle. Iron Maiden\n" +
            "Dagger is one of the few Divine Daggers in the run; pair with\n" +
            "backstab comp for guaranteed wield. Cleared Yasha unlocks the\n" +
            "F84-F90 IF chest band.\n\n" +
            "SEE ALSO\n" +
            "[Floor 86] · [Floor 90] · [Integral Factor Weapon Series] · [Divine Weapons — Roster & Acquisition]")
        {
            Tags = new[] { "floors", "f87", "canon", "divine" }
        },

        new("Floors", "Floor 88",
            "┌─ Floors\n" +
            "│ Topic: Floor 88\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Void\n" +
            "│ Boss: The Rebellious Eyes (Hundred-Eyed Aberration)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Void biome, godlike scaling. Lv 186, 6670 HP boss, 258 ATK.\n" +
            "Guaranteed Divine — Ouroboros. F88 anchors the Lambent/Radiant\n" +
            "Light Asuna chest band that runs F88-F94.\n\n" +
            "BOSS\n" +
            "The Rebellious Eyes (HF canon). Hundred-eyed aberration —\n" +
            "every eye is a potential ranged attacker. Status pressure is\n" +
            "the entire fight. Standard F75+ kit on top.\n\n" +
            "DROPS\n" +
            "- Floor-boss guaranteed: Ouroboros — Divine Axe, self-\n" +
            "  consuming serpent (Norse-myth chain).\n" +
            "- Chest band: Radiant Light (Legendary, Asuna F88-F94 chest\n" +
            "  band, pairs with F40 Lambent Light).\n\n" +
            "NPCS / QUESTS\n" +
            "Watcher Kael (HF HNM questgiver) — 20 F88 kills returns\n" +
            "Starmace: Elysium (HF Mace, Uninterruptible+50).\n\n" +
            "TIPS\n" +
            "Uninterruptible+50 from Starmace Elysium is the cleanest\n" +
            "anti-status answer for the rest of the climb — don't skip\n" +
            "Watcher Kael. Stack the 20 kills with Radiant Light chest farms.\n\n" +
            "SEE ALSO\n" +
            "[Floor 87] · [Floor 89] · [Floor 40] · [Divine Weapons — Roster & Acquisition]")
        {
            Tags = new[] { "floors", "f88" }
        },

        new("Floors", "Floor 89",
            "┌─ Floors\n" +
            "│ Topic: Floor 89\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Void\n" +
            "│ Boss: The Murderer Fang (Alpha of the Bleeding Pack)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Void biome, godlike scaling. Lv 188, 6788 HP boss, 262 ATK.\n" +
            "Scholar Vesper anchors a Divine quest here.\n\n" +
            "BOSS\n" +
            "The Murderer Fang (HF canon). Alpha-werebeast pack leader.\n" +
            "Bleed pressure stacks — bring active healing or Bleed-cure.\n" +
            "Standard F75+ kit on top.\n\n" +
            "NPCS / QUESTS\n" +
            "Scholar Vesper (Satanachia Divine quest giver) — chained kill\n" +
            "quest returns Satanachia (Divine Scimitar, infernal-tier T4\n" +
            "chain).\n\n" +
            "TIPS\n" +
            "Bleed-resist gear pays here. Satanachia's infernal tier slots\n" +
            "into the F86 Tyrfing / F88 Ouroboros / F91 Mjolnir Norse-and-\n" +
            "myth Divine spine — collect them as a chain set if you're\n" +
            "running a curse-build.\n\n" +
            "SEE ALSO\n" +
            "[Floor 88] · [Floor 90] · [Divine Weapons — Roster & Acquisition]")
        {
            Tags = new[] { "floors", "f89" }
        },

        new("Floors", "Floor 90",
            "┌─ Floors\n" +
            "│ Topic: Floor 90\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Void\n" +
            "│ Boss: Colossus of Aincrad (The Living Floor)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Void biome, godlike-tier multiples-of-10 spike. Lv 190, 6908\n" +
            "HP boss, 265 ATK. Celestial-garden HF canon flavor — the\n" +
            "Integral Factor Gaou series anchors here, plus a High-Priestess\n" +
            "HF questgiver.\n\n" +
            "BOSS\n" +
            "Colossus of Aincrad — invented (no canon HF F90 boss). 'The\n" +
            "Living Floor' is the in-fiction read: every wall and tile is\n" +
            "the boss. Standard F75+ kit on top of map-scale presence.\n\n" +
            "DROPS\n" +
            "- Floor-boss: standard chest pool, no guaranteed Divine.\n" +
            "- Field boss: Gaou the Ox-King (Demon-King of the Horned\n" +
            "  Vanguard) → Gaou Reginleifr (IF Legendary 1H Sword) + Gaou\n" +
            "  Tatari (Legendary shield). IF Gaou Series anchor.\n\n" +
            "NPCS / QUESTS\n" +
            "High Priestess Sola (HF HNM questgiver) — 20 F90 kills returns\n" +
            "Eurynome's Holy Sword (HF/IM 1H Sword, HolyDamage+20).\n\n" +
            "CANON\n" +
            "Gaou series (Integral Factor): IF anchor for the F90 chest\n" +
            "band. Eurynome's Holy Sword bridges HF and Infinity Moment\n" +
            "canon. The HF original calls F90 'The Ruler of Blade';\n" +
            "implementation renames to Colossus of Aincrad to fit the\n" +
            "in-game theme.\n\n" +
            "TIPS\n" +
            "F90 closes the Yasha-Gaou IF series window — clear both Yasha\n" +
            "(F87) and Gaou for the full IF chest unlock. Sola's holy sword\n" +
            "stacks with Eugeo's Blue Rose Sword (F20) for a holy-damage\n" +
            "burst comp into F91 volcanic.\n\n" +
            "SEE ALSO\n" +
            "[Floor 87] · [Floor 91] · [Integral Factor Weapon Series]")
        {
            Tags = new[] { "floors", "f90", "canon" }
        },

        new("Floors", "Floor 91",
            "┌─ Floors\n" +
            "│ Topic: Floor 91\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Volcanic\n" +
            "│ Boss: Seraphiel the Fallen (Angel of the Burning Sword)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Volcanic biome, impossible-tier scaling. Lv 192, 7028 HP boss,\n" +
            "269 ATK. Guaranteed Divine — Mjolnir.\n\n" +
            "BOSS\n" +
            "Seraphiel the Fallen — invented. Burning-sword angel: fire and\n" +
            "holy combo damage. Standard F75+ kit on top.\n\n" +
            "DROPS\n" +
            "- Floor-boss guaranteed: Mjolnir — Divine, thunderbolt hammer\n" +
            "  (Norse-myth chain).\n\n" +
            "NPCS / QUESTS\n" +
            "Torchbearer Meir (HF HNM questgiver) — 20 F91 kills returns\n" +
            "Saintspear: Rhongomyniad (HF Spear, HPRegen+5).\n\n" +
            "TIPS\n" +
            "Mjolnir is the second of four Norse-myth Divines (Tyrfing F86,\n" +
            "Ouroboros F88, Mjolnir F91, Ascalon F93). Build a Mace comp\n" +
            "here if chasing the chain — the storm-axis play extends to F94.\n\n" +
            "SEE ALSO\n" +
            "[Floor 90] · [Floor 93] · [Divine Weapons — Roster & Acquisition]")
        {
            Tags = new[] { "floors", "f91" }
        },

        new("Floors", "Floor 92",
            "┌─ Floors\n" +
            "│ Topic: Floor 92\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Volcanic\n" +
            "│ Boss: Apollyon the World-Ender (The Seventy-Second Demon)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Volcanic biome, impossible-tier scaling. Lv 194, 7150 HP boss,\n" +
            "273 ATK. Last-Attack drop floor — Sacred Cross.\n\n" +
            "BOSS\n" +
            "Apollyon the World-Ender — invented, 'Seventy-Second Demon'\n" +
            "title plays into the Goetia framing. Heavy ATK + Devastating\n" +
            "Charge phase 1 — pre-position before pull.\n\n" +
            "DROPS\n" +
            "- Last-Attack Bonus (your killing blow only): Sacred Cross\n" +
            "  (IM Legendary 2H Sword). Ally finisher forfeits it.\n\n" +
            "NPCS / QUESTS\n" +
            "Auric Knight Halric (HF HNM questgiver) — 15 F92 kills returns\n" +
            "Aurumbrand: Hauteclaire (HF 1H Sword).\n\n" +
            "TIPS\n" +
            "Save your highest-burst skill for the final HP slice — Sacred\n" +
            "Cross is LAB-only. Halric's 15-kill quest doubles up cleanly\n" +
            "with the boss approach.\n\n" +
            "SEE ALSO\n" +
            "[Floor 91] · [Floor 93] · [Avatar Weapons & Last-Attack Bonus]")
        {
            Tags = new[] { "floors", "f92" }
        },

        new("Floors", "Floor 93",
            "┌─ Floors\n" +
            "│ Topic: Floor 93\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Volcanic\n" +
            "│ Boss: Ragnarok the Final Beast (End of All Things)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Volcanic biome, impossible-tier scaling. Lv 196, 7272 HP boss,\n" +
            "277 ATK. Both a guaranteed Divine (Ascalon) and a LAB drop\n" +
            "(Glow Haze) — pure-loot floor.\n\n" +
            "BOSS\n" +
            "Ragnarok the Final Beast — invented. 'End of All Things'\n" +
            "framing. Standard F75+ kit; the Devastating Charge opener\n" +
            "deals enough that an all-in trade often loses you the LAB.\n\n" +
            "DROPS\n" +
            "- Floor-boss guaranteed: Ascalon — Divine 2H Sword, dragon-\n" +
            "  slayer (Norse-myth chain closer).\n" +
            "- Last-Attack Bonus (your killing blow only): Glow Haze (IM\n" +
            "  Scimitar).\n" +
            "- Field boss: Banishing Ray (Sentinel of the White Horizon)\n" +
            "  → Glimmerblade: Banishing Ray (HF Rapier).\n\n" +
            "TIPS\n" +
            "If you want both drops, win the Ragnarok DPS race solo — Glow\n" +
            "Haze is LAB-only. Ascalon completes the F86/F88/F91/F93 Norse-\n" +
            "myth chain. Pre-clear Banishing Ray for Glimmerblade if you\n" +
            "want a Rapier alongside Ascalon's 2H Sword.\n\n" +
            "SEE ALSO\n" +
            "[Floor 91] · [Floor 95] · [Divine Weapons — Roster & Acquisition]")
        {
            Tags = new[] { "floors", "f93" }
        },

        new("Floors", "Floor 94",
            "┌─ Floors\n" +
            "│ Topic: Floor 94\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Volcanic\n" +
            "│ Boss: Immortal Phoenix (The Boss That Won't Stay Dead)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Volcanic biome, impossible-tier scaling. Lv 198, 7396 HP boss,\n" +
            "280 ATK. LAB drop floor — Saku katana with Night damage.\n\n" +
            "BOSS\n" +
            "Immortal Phoenix — invented. 'The Boss That Won't Stay Dead' —\n" +
            "phoenix Regeneration cycle is the entire encounter. Burn the\n" +
            "boss past the Regen threshold or it'll outlast your skill\n" +
            "rotation.\n\n" +
            "DROPS\n" +
            "- Last-Attack Bonus (your killing blow only): Saku (IM Katana,\n" +
            "  NightDamage). Ally finisher forfeits it.\n" +
            "- Field boss: Ark Knight (Executioner of the Final Ark) →\n" +
            "  Ragnarok's Bane: Headsman (HF 2H Axe, HNM).\n\n" +
            "TIPS\n" +
            "If you can't kill the Phoenix in two rotations, the fight\n" +
            "turns infinite — front-load damage. Saku's Night damage line\n" +
            "is the cleanest carry for the Void biome that opens at F96.\n\n" +
            "SEE ALSO\n" +
            "[Floor 93] · [Floor 95] · [Avatar Weapons & Last-Attack Bonus]")
        {
            Tags = new[] { "floors", "f94" }
        },

        new("Floors", "Floor 95",
            "┌─ Floors\n" +
            "│ Topic: Floor 95\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Volcanic\n" +
            "│ Boss: Abyss Walker (The Darkness Between Floors)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Volcanic biome, divine-ascension flavor (cloud palaces, golden\n" +
            "halls, ethereal-light weather canon). Lv 200, 7522 HP boss,\n" +
            "284 ATK. Heaviest field-boss roster before F99 — Time\n" +
            "Piercing Sword (Divine), Red Rose Sword (FD Legendary),\n" +
            "Stigmablade Arondight, Mirage Knife LAB, plus an HF Shinto\n" +
            "questgiver.\n\n" +
            "BOSS\n" +
            "Abyss Walker — invented. 'The Darkness Between Floors' is the\n" +
            "in-fiction read. Standard F75+ kit on top of impossible-tier\n" +
            "scaling — phase-1 Devastating Charge alone clears 30% of an\n" +
            "unprepared frontline.\n\n" +
            "DROPS\n" +
            "- Last-Attack Bonus (your killing blow only): Mirage Knife\n" +
            "  (IM Dagger). Ally finisher forfeits it.\n" +
            "- Field boss: Warden of Stopped Hours (Sentinel of the Broken\n" +
            "  Clock) → Time Piercing Sword (Divine 1H Sword — Bercouli's\n" +
            "  blade, Alicization canon).\n" +
            "- Field boss: Gaia Breaker (Titan of the Cracked Earth) →\n" +
            "  Stigmablade: Arondight (HF 2H Sword, HNM).\n" +
            "- Field boss: Warden of the Blooming Rose (Petal-Wreathed\n" +
            "  Sentinel of the Ninety-Fifth) → Red Rose Sword (FD Kirito\n" +
            "  red-edge Legendary, Last Recollection canon — pairs with\n" +
            "  Night Sky Sword from F99).\n\n" +
            "NPCS / QUESTS\n" +
            "Elder Beastkeeper (HF HNM questgiver) — 25 F95 kills returns\n" +
            "Shinto: Ama-no-Murakumo (HF Katana, Susanoo's cloud-splitter\n" +
            "from LN myth).\n\n" +
            "CANON\n" +
            "Time Piercing Sword (Alicization): Bercouli Synthesis One's\n" +
            "blade — the First Knight, the one who broke time. F95 Warden\n" +
            "of Stopped Hours is the canonical drop path; the F97 Cardinal\n" +
            "floor-boss guaranteed Time Piercing also exists in code, but\n" +
            "Warden of Stopped Hours is the canonical kill (the Stopped\n" +
            "Hours name maps directly to Bercouli's clock-piercer).\n" +
            "Red Rose Sword pairs with Night Sky Sword (F99) as Kirito's\n" +
            "Last-Recollection red/black duo.\n\n" +
            "TIPS\n" +
            "F95 is a one-day-of-grinding floor — you cannot clear all\n" +
            "three field bosses plus the floor in a single delve. Plan two\n" +
            "or three trips. Land the Abyss Walker killing blow yourself\n" +
            "for Mirage Knife. Take Warden of Stopped Hours first if\n" +
            "you've skipped F85 Black Lily — pair the two Integrity-\n" +
            "Knight blades for the cleanest endgame Sheyta/Bercouli kit.\n\n" +
            "SEE ALSO\n" +
            "[Floor 85] · [Floor 99] · [Divine Weapons — Roster & Acquisition] · [Fractured Daydream Character Weapons]")
        {
            Tags = new[] { "floors", "f95", "canon", "divine" }
        },

        new("Floors", "Floor 96",
            "┌─ Floors\n" +
            "│ Topic: Floor 96\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Void\n" +
            "│ Boss: Herald of the Ruby Palace (The Last Gatekeeper)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Void biome — Ruby Palace approach begins. Lv 202, 7648 HP\n" +
            "boss, 287 ATK. LAB drop (Northern Light) + an HF field boss\n" +
            "carrying Sigurd's mythic blade.\n\n" +
            "BOSS\n" +
            "Herald of the Ruby Palace — invented. 'The Last Gatekeeper'\n" +
            "framing — the floor reads as the doorway to the F100 final\n" +
            "arena. Standard F75+ kit; Void warps add status pressure.\n\n" +
            "DROPS\n" +
            "- Last-Attack Bonus (your killing blow only): Northern Light\n" +
            "  (IM Axe). Ally finisher forfeits it.\n" +
            "- Field boss: Eternal Dragon (The Wyrm That Refuses to Die)\n" +
            "  → Demonblade: Gram (HF 2H Sword, Sigurd's blade in Norse\n" +
            "  myth, HNM).\n\n" +
            "TIPS\n" +
            "Eternal Dragon's HP-recovery cycle mirrors the F94 Phoenix —\n" +
            "burst it down or accept the long fight. Northern Light is the\n" +
            "last Axe LAB before F99; if you're running an Axe build, this\n" +
            "is your last chance to claim the LAB-only line.\n\n" +
            "SEE ALSO\n" +
            "[Floor 95] · [Floor 99] · [Avatar Weapons & Last-Attack Bonus]")
        {
            Tags = new[] { "floors", "f96" }
        },

        new("Floors", "Floor 97",
            "┌─ Floors\n" +
            "│ Topic: Floor 97\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Void\n" +
            "│ Boss: Cardinal the System Error (When the Game Fights Back)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Void biome, Ruby Palace approach. Lv 204, 7776 HP boss, 291\n" +
            "ATK. The fourth-wall floor — Cardinal is the system itself\n" +
            "objecting to your climb. Administrator's Regent waits as the\n" +
            "field boss.\n\n" +
            "BOSS\n" +
            "Cardinal the System Error (HF canon, 'The Emperor of Death'\n" +
            "in HF). 'When the Game Fights Back' is the in-fiction read —\n" +
            "Cardinal is the Underworld's regulator system, weaponized.\n" +
            "Standard F75+ kit on top of impossible-tier scaling, with\n" +
            "Void status warps overlapping every phase.\n\n" +
            "DROPS\n" +
            "- Floor-boss: standard chest pool — Tier-4 Legendaries, no\n" +
            "  guaranteed Divine. (Time Piercing Sword's canonical drop\n" +
            "  is F95 Warden of Stopped Hours; the F97 floor-boss code-\n" +
            "  side duplicate is left unused per locked decision.)\n" +
            "- Field boss: Administrator's Regent (Pontifex Echo of the\n" +
            "  Ninety-Seventh Cathedral) → Silvery Ruler (FD Administrator\n" +
            "  Legendary 1H Sword).\n\n" +
            "CANON\n" +
            "Cardinal (Alicization): the autonomous system Quinella/the\n" +
            "Administrator subverted — the original program meant to keep\n" +
            "Underworld coherent. Administrator's Regent is the Pontifex\n" +
            "echo Quinella left behind after her Alicization defeat;\n" +
            "Silvery Ruler is her FD-canon blade. F97 references the HF\n" +
            "'Emperor of Death' boss slot remixed into AincradTRPG's\n" +
            "Cardinal/Administrator framing.\n\n" +
            "TIPS\n" +
            "Don't try to outlast Cardinal — the boss heals through Void\n" +
            "warps. Burst windows are the entire encounter. Pre-clear\n" +
            "Administrator's Regent for Silvery Ruler before the floor-\n" +
            "boss approach; the Regent fight tunes the same status-warp\n" +
            "pressure you'll see in Cardinal.\n\n" +
            "SEE ALSO\n" +
            "[Floor 95] · [Floor 98] · [Fractured Daydream Character Weapons]")
        {
            Tags = new[] { "floors", "f97", "canon" }
        },

        new("Floors", "Floor 98",
            "┌─ Floors\n" +
            "│ Topic: Floor 98\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Void\n" +
            "│ Boss: Incarnation of the Radius (An Impossible Geometry)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Void biome, Ruby Palace approach. Lv 206, 7904 HP boss, 295\n" +
            "ATK. Heaviest pre-F99 floor — LAB drop, two field-boss\n" +
            "Legendaries, an HF questgiver. Original LN canon final boss\n" +
            "name surfaces here.\n\n" +
            "BOSS\n" +
            "Incarnation of the Radius (HF canon, 'The Kaiser Dragon' in\n" +
            "HF). LN canon — the original programmed F100 final boss\n" +
            "before Heathcliff revealed himself in Aincrad arc. Here\n" +
            "Incarnation gets a F98 placement as the geometry-boss filler\n" +
            "for the Ruby Palace approach. Standard F75+ kit on top of\n" +
            "impossible-tier scaling. The Devastating Charge phase 1 is\n" +
            "the kill check.\n\n" +
            "DROPS\n" +
            "- Floor-boss: standard chest pool — Tier-4 Legendaries. (Black\n" +
            "  Lily Sword's canonical drop is F85 Silent Edge; the F98\n" +
            "  floor-boss code-side duplicate is left unused per locked\n" +
            "  decision.)\n" +
            "- Last-Attack Bonus (your killing blow only): Lunatic Roof\n" +
            "  (IM Spear, Lunacy effect). Ally finisher forfeits it.\n" +
            "- Field boss: Blaze Armor (Living Armor of the Hollow Forge)\n" +
            "  → Yato: Masamune (HF Katana, Hollow Area canon).\n" +
            "- Field boss: Ashen Kirito Simulacrum (Red-Edge Echo of the\n" +
            "  Ninety-Eighth) → Elucidator Rouge (FD Legendary 1H Sword,\n" +
            "  Kirito FD red variant — pairs with Flare Pulsar).\n\n" +
            "NPCS / QUESTS\n" +
            "Sentinel Captain (HF HNM questgiver) — 25 F98 kills returns\n" +
            "Godspear: Gungnir (HF Spear, Odin's spear in LN myth).\n\n" +
            "CANON\n" +
            "Incarnation of the Radius is the original programmed F100\n" +
            "final boss in LN vol 1 — the boss Heathcliff replaced with\n" +
            "himself when he masqueraded as a player. Implementation moves\n" +
            "Incarnation to F98 so F99 can carry the Heathcliff narrative\n" +
            "thread directly. Yato Masamune is HF Hollow Area canon. Ashen\n" +
            "Kirito Simulacrum is FD Last-Recollection-canon Kirito.\n\n" +
            "TIPS\n" +
            "Lunatic Roof's Lunacy effect plays into the Void biome's\n" +
            "status-warp meta — the spear is the cleanest LAB choice into\n" +
            "F99. Ashen Kirito's Elucidator Rouge completes the Kirito\n" +
            "weapon line if you've been collecting (F50 Elucidator + F55\n" +
            "Dark Repulser + F95 Red Rose Sword + this). Save Sentinel\n" +
            "Captain's 25 kills for the Incarnation approach run — the\n" +
            "counter overlaps.\n\n" +
            "SEE ALSO\n" +
            "[Floor 50] · [Floor 95] · [Floor 99] · [Fractured Daydream Character Weapons]")
        {
            Tags = new[] { "floors", "f98", "canon" }
        },

        new("Floors", "Floor 99",
            "┌─ Floors\n" +
            "│ Topic: Floor 99\n" +
            "│ Tier: 4 (endgame)\n" +
            "│ Biome: Void\n" +
            "│ Boss: Heathcliff's Shadow (Echo of the Creator)\n" +
            "└─\n\n" +
            "SUMMARY\n" +
            "Final guide-covered floor. Void biome, divine-ascension flavor.\n" +
            "Lv 208, 8034 HP boss, 298 ATK. The last gate before the Ruby\n" +
            "Palace. Two endgame Divines drop here — Night Sky Sword\n" +
            "(guaranteed) and Artemis (LAB).\n\n" +
            "BOSS\n" +
            "Heathcliff's Shadow — 'Echo of the Creator'. The shade of\n" +
            "Akihiko Kayaba's avatar, looped onto F99 as the climb's last\n" +
            "trial. The full F75+ kit fires every cooldown: Devastating\n" +
            "Charge phase 1, Ground Slam phase 2, Toxic Breath, 10%\n" +
            "Regeneration on phase 3. The Shadow knows your loadout —\n" +
            "expect mirror-style read on your highest-burst skill.\n\n" +
            "DROPS\n" +
            "- Floor-boss guaranteed: Night Sky Sword — Divine 1H Sword,\n" +
            "  ArmorPierce+30. Kirito's Alicization-canon blade. Priority\n" +
            "  46 — the highest-priority Divine in the run.\n" +
            "- Last-Attack Bonus (your killing blow only): Artemis (IM\n" +
            "  Legendary Bow, F99 canon). Ally finisher forfeits it.\n\n" +
            "NPCS / QUESTS\n" +
            "Last Herald Xiv (HF HNM questgiver) — 20 F99 kills returns\n" +
            "Deathglutton: Epetamu (HF Scimitar). The final HF questgiver\n" +
            "in the chain.\n\n" +
            "CANON\n" +
            "F99's Heathcliff's Shadow is the in-code echo of the canonical\n" +
            "F75 Heathcliff arc climax — in the original LN/anime Aincrad\n" +
            "arc, Kirito defeats Heathcliff (Akihiko Kayaba) on F75 and\n" +
            "ends the death game. Implementation moves the Skull Reaper to\n" +
            "F75 (LN-faithful) and routes the Heathcliff narrative thread\n" +
            "to F99 as the endgame mirror — the Shadow is the climb's\n" +
            "memory of the canonical F75 duel, fought again at the top.\n" +
            "Night Sky Sword is Kirito's Alicization-canon blade (Underworld\n" +
            "endgame). Artemis is Infinity Moment's F99 LAB anchor — the\n" +
            "two pair as Kirito's combined Aincrad-survivor / Underworld-\n" +
            "knight kit.\n\n" +
            "TIPS\n" +
            "Land the Shadow's killing blow yourself or you forfeit\n" +
            "Artemis. The Shadow's mirror-read targets your highest-burst\n" +
            "skill — bait it with a feint slot, then commit your real\n" +
            "burst on its phase-3 Regeneration window. Pair Night Sky\n" +
            "Sword with the F95 Red Rose Sword for Kirito's Last-Recollection\n" +
            "red/black duo. F99 closes the Player Guide; the F100 Ruby\n" +
            "Palace fight has no guide entry — what's beyond is yours.\n\n" +
            "SEE ALSO\n" +
            "[Floor 75] · [Floor 95] · [Floor 98] · [Divine Weapons — Roster & Acquisition] · [Avatar Weapons & Last-Attack Bonus]")
        {
            Tags = new[] { "floors", "f99", "canon", "divine" }
        },
    };

    // Gate boss-drop names with "???" until the boss is killed.
    // Floor bosses: per-floor HashSet (Q16). Field bosses: best-effort gating by floor reach
    // (floor <= turnManager.CurrentFloor) — exact field-boss-name → FieldBossId map TBD.
    // LAB section gates same as floor bosses — clearing the floor reveals the LAB drop too.
    public static string GateBossDropReferenceBody(string body, SAOTRPG.Systems.TurnManager? tm)
    {
        if (tm == null || string.IsNullOrEmpty(body)) return body;
        const string GATED = "???";

        var lines = body.Split('\n');
        // Section state machine — section names match the static body's headers.
        // 0=other, 1=floor-boss, 2=LAB, 3=field-boss, 4=field-boss-secondary.
        int section = 0;
        var sb = new System.Text.StringBuilder(body.Length + 64);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.StartsWith("FLOOR-BOSS GUARANTEED DROPS")) section = 1;
            else if (line.StartsWith("FLOOR-BOSS LAST-ATTACK BONUS")) section = 2;
            else if (line.StartsWith("FIELD-BOSS GUARANTEED DROPS")) section = 3;
            else if (line.StartsWith("FIELD-BOSS SECONDARY DROPS")) section = 4;
            else if (line.Length > 0 && line[0] >= 'A' && line[0] <= 'Z'
                     && !line.StartsWith("  ")) section = 0;

            // Drop-line gating only inside boss sections; format "  Fxx <name> -> <drop>".
            if (section >= 1 && line.StartsWith("  F"))
            {
                int arrow = line.IndexOf("->", StringComparison.Ordinal);
                int floorEnd = -1;
                for (int j = 3; j < line.Length && j < 8; j++)
                    if (line[j] == ' ') { floorEnd = j; break; }
                if (arrow > 0 && floorEnd > 3 && int.TryParse(line.AsSpan(3, floorEnd - 3), out int floor))
                {
                    bool revealed = section switch
                    {
                        1 or 2 => tm.DefeatedFloorBosses.Contains(floor),
                        3 or 4 => tm.CurrentFloor >= floor, // best-effort field-boss gate
                        _      => true,
                    };
                    if (!revealed)
                    {
                        // Replace everything after "->" with "???".
                        string head = line.Substring(0, arrow + 2);
                        sb.Append(head).Append(' ').Append(GATED);
                        if (i < lines.Length - 1) sb.Append('\n');
                        continue;
                    }
                }
            }
            sb.Append(line);
            if (i < lines.Length - 1) sb.Append('\n');
        }
        return sb.ToString();
    }
}
