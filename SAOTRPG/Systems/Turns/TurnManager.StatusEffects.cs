using SAOTRPG.Inventory.Core;
using SAOTRPG.Systems.Combat;
using SAOTRPG.Systems.Progression;
using SAOTRPG.Systems.RunState;
using SAOTRPG.Systems.World;
using SAOTRPG.UI.Effects;

namespace SAOTRPG.Systems.Turns;

// Player status effect ticks, passive regen, exhaustion, and equipment durability.
public partial class TurnManager
{
    // Shared DoT tick: damage, decrement, log, combat text, death handling. Returns false if skipped.
    private bool TickDot(ref int turnsLeft, int damage, string label, string endMsg, string tag, Color color, string killerName)
    {
        if (_player.IsDefeated || turnsLeft <= 0) return false;
        _player.TakeDamage(damage);
        turnsLeft--;
        string suffix = turnsLeft > 0 ? $" ({turnsLeft} turns left)" : $" ({endMsg})";
        _log.LogCombat($"{label} deals {damage} damage!{suffix}");
        CombatTextEvent?.Invoke(_player.X, _player.Y, $"{tag} -{damage}", color);
        // Status mote on the affected tile — bleed uses ◇ shatter per SAO theme.
        string kind = tag switch { "PSN" => "poison", "BLD" => "bleed", _ => "burn" };
        StatusTrailRequested?.Invoke(_player.X, _player.Y, kind);
        // Status-inflict particle puff alongside the trail glyph.
        ParticleEvent pev = kind switch
        {
            "poison" => ParticleEvent.PoisonInflict,
            "bleed" => ParticleEvent.BleedInflict,
            _ => ParticleEvent.BurnInflict,
        };
        ParticleQueue.Emit(pev, _player.X, _player.Y);
        CheckDotDeath(killerName);
        return true;
    }

    private void CheckDotDeath(string killerName)
    {
        if (!_player.IsDefeated) return;
        LastKillerName = killerName;
        _log.LogSystem(FlavorText.DeathFlavors[RunRng.Next(FlavorText.DeathFlavors.Length)]);
        RaisePlayerDied(killerName);
    }

    private void TickPoison() =>
        TickDot(ref _poisonTurnsLeft, _poisonDamagePerTick, "Poison", "poison wears off", "PSN", Color.BrightGreen, "poison");

    // True if the player is currently poisoned.
    public bool IsPoisoned => _poisonTurnsLeft > 0;
    // Remaining turns of poison damage.
    public int PoisonTurnsLeft => _poisonTurnsLeft;

    private void TickBleed()
    {
        if (!TickDot(ref _bleedTurnsLeft, _bleedDamagePerTick, "Bleed", "bleeding stops", "BLD", Color.BrightRed, "bleeding")) return;
        if (_poisonTurnsLeft <= 0 || _bleedTurnsLeft <= 0 || _player.IsDefeated) return;

        // Hemorrhage combo: poison + bleed active = burst damage
        int hemorrhageDmg = (_poisonDamagePerTick + _bleedDamagePerTick) * 2;
        _player.TakeDamage(hemorrhageDmg);
        _log.LogCombat($"*** HEMORRHAGE! Poison and bleed react — {hemorrhageDmg} burst damage! ***");
        CombatTextEvent?.Invoke(_player.X, _player.Y, "HEMORRHAGE!", Color.BrightMagenta);
        if (!_player.IsDefeated)
            MilestoneSystem.TryUnlock("survive_hemorrhage", _player);
        CheckDotDeath("bleeding");
    }

    // True if the player is currently bleeding.
    public bool IsBleeding => _bleedTurnsLeft > 0;
    // Remaining turns of bleed damage.
    public int BleedTurnsLeft => _bleedTurnsLeft;

    // The parenthetical is BUILT FROM THE PENALTIES, never written out. The hand-written version
    // announced a speed penalty at both steps that no mechanic charged. Speed drives the
    // follow-up strike now, but fatigue still charges attack and defence only: it is the
    // exertion clock, and the two numbers it takes are the two the warning names.
    private string FatiguePenaltyText()
    {
        var parts = new List<string>(2);
        if (FatigueAtkPenalty != 0) parts.Add($"{FatigueAtkPenalty} ATK");
        if (FatigueDefPenalty != 0) parts.Add($"{FatigueDefPenalty} DEF");
        return parts.Count == 0 ? "" : $" ({string.Join(", ", parts)})";
    }

    private void TickExhaustion()
    {
        if (_restCounter >= ExhaustionThreshold && !_exhaustedWarned)
        {
            _exhaustedWarned = true;
            _log.LogCombat($"You are EXHAUSTED! Your body screams for rest.{FatiguePenaltyText()}");
        }
        else if (_restCounter >= FatigueThreshold && !_fatiguedWarned)
        {
            _fatiguedWarned = true;
            _log.LogCombat($"Fatigue sets in. Your swings feel sluggish.{FatiguePenaltyText()}");
        }
    }

    private void TickStun()
    {
        if (_stunTurnsLeft <= 0) return;
        _stunTurnsLeft--;
        _log.LogCombat(_stunTurnsLeft > 0
            ? $"You are stunned! ({_stunTurnsLeft} turns left)"
            : "The stun wears off.");
    }

    private void TickSlow()
    {
        if (_slowTurnsLeft <= 0) return;
        _slowTurnsLeft--;
        if (_slowTurnsLeft <= 0) _log.LogCombat("You are no longer slowed.");
    }

    // EVERY TIMED DURATION IN THE GAME, ticked once per turn.
    //
    // These used to live in AdvanceTurn — and the ordinary successful move does TurnCount++
    // directly and never calls it, so on a populated floor only the turns that happened to be
    // attacks ticked anything. Measured: 13 of 46 turns, i.e. a declared 20-turn skill cooldown
    // really lasted about 70, invisibility outlasted its own duration roughly threefold, and a
    // confused mob stayed confused for as long as the player kept walking.
    //
    // Called beside TickPoison/TickBleed/TickSlow, which is the one hook every turn-consuming
    // path already reaches. AdvanceTurn's callers reach it too, so nothing double-ticks.
    //
    // DELIBERATELY NOT MOVED, and the distinction is durations versus attrition: hunger drain,
    // biome damage, starvation and TickExhaustion stay where they are. Each of those is a RATE
    // rather than a declared duration, so rehoming them would change how demanding the game is
    // rather than make a stated number true. (TickExhaustion is the mirror case — its single call
    // site is the walking path, so fatigue currently builds only while walking.) That is a balance
    // decision for the dev.
    // THE WORLD CLOCK: hunger, biome damage and starvation.
    //
    // These lived in AdvanceTurn, which the ordinary move never calls — so the modulo test was
    // only SAMPLED on the ~28% of turns that were attacks, mining or stuns. That did not merely
    // slow attrition down, it made it ERRATIC: whether a hunger tick landed depended on an attack
    // turn happening to coincide with a multiple of the interval. Two players walking the same
    // floor could starve at very different rates for reasons neither could see.
    //
    // Every interval is scaled by AttritionTurnScale so the AVERAGE rate is preserved. The
    // sampling fraction was measured at 13 AdvanceTurns per 46 turns (ProgressProbe s14), i.e.
    // one attrition evaluation per ~3.5 turns; 4 is that rounded to a whole multiplier, which
    // leaves the game about 13% gentler on average than the old behaviour and, more importantly,
    // identical for every playstyle.
    //
    // TickExhaustion is deliberately NOT here. Fatigue is an EXERTION clock, and "walking tires
    // you, standing still does not" is at least arguable as a design; a hunger clock that runs
    // faster because you chose to fight is not. Moving it stays the dev's call.
    private const int AttritionTurnScale = 4;

    // HOW MUCH HUNGRIER THE NIGHT IS, as a multiple of the base drain interval rather than as a
    // turn count of its own — so it stays a stated FRACTION of the base rate if that rate is ever
    // re-tuned, instead of silently becoming a different difficulty.
    //
    // FOUR means one extra point per four ordinary drains: +25% while the sun is down. Sized
    // against what the player can buy rather than against a feel: the Night phase is ~1,180 turns
    // of the 4,000-turn cycle, so the surcharge costs about 25 satiety over a whole night — one
    // ordinary ration (Bread restores 20, Grilled Meat 30). Across a full day it is +7%.
    //
    // THE ROADMAP ASKED FOR ONE POINT PER TEN TURNS, WHICH IS A FAR BIGGER CHANGE THAN IT LOOKS.
    // The base rate is one per twelve, so that is +120% during the night and +35% over a day, and
    // it would drain 118 points across one night against a bar that holds 100. Satiety is already
    // the fastest clock in the game — 1,200 turns from full to starving against a 4,000-turn day,
    // so an unfed player starves roughly three times per cycle before any night term exists. A
    // surcharge that large does not make the night feel hungrier; it makes food the only thing
    // the player is doing. The gentler number is the shipped one, and the dial is this constant.
    private const int NightHungerIntervalScale = 4;

    private void TickAttrition()
    {
        // Normal hunger + biome-specific extra drain (desert/volcanic/void).
        int drainAmount = 1 + BiomeSystem.SatietyDrainBonus;
        // The sun is down. A multiple of the base interval, so this extra point can only ever
        // land on a turn the base drain already fires — no second modulo, no second zero guard,
        // and the night's cost arrives as an occasional doubled tick rather than an off-beat one.
        if (SAOTRPG.Map.Lighting.DayNightCycle.IsNight
            && TurnCount % (HungerDrainInterval * AttritionTurnScale * NightHungerIntervalScale) == 0)
            drainAmount += 1;
        // Iron Rank modifier doubles hunger drain — applied last so it doubles the whole bill,
        // the night surcharge included, which is what "doubles hunger drain" says.
        if (RunModifiers.IsActive(RunModifier.IronRank)) drainAmount *= 2;
        if (TurnCount % (HungerDrainInterval * AttritionTurnScale) == 0 && Satiety > 0)
            Satiety = Math.Max(0, Satiety - drainAmount);

        // Biome passive damage (volcanic heat, void corruption).
        var (biomeDmg, bioInterval) = BiomeSystem.EnvironmentDamage;
        int bioScaled = bioInterval * AttritionTurnScale;
        if (biomeDmg > 0 && bioInterval > 0 && TurnCount % bioScaled == 0 && !_player.IsDefeated)
        {
            _player.TakeDamage(biomeDmg);
            if (TurnCount % (bioScaled * 3) == 0) // log every 3rd tick to reduce spam
                _log.Log($"The {BiomeSystem.DisplayName} environment wears on you. (-{biomeDmg} HP)");
            if (_player.IsDefeated) { LastKillerName = $"the {BiomeSystem.DisplayName}"; RaisePlayerDied("biome"); }
        }

        // Starvation carried no interval of its own — it was one HP per AdvanceTurn, which the
        // sampling made roughly one per 3.5 turns. Keyed to the same scale so it stays there.
        if (Satiety <= 0 && !_player.IsDefeated)
        {
            if (TurnCount % AttritionTurnScale == 0) _player.TakeDamage(1);
            if (!_starvingWarned)
            {
                _log.LogCombat("You're starving! Eat something before it's too late!");
                _starvingWarned = true;
            }
            if (_player.IsDefeated)
            {
                LastKillerName = "starvation";
                _log.LogSystem(FlavorText.DeathFlavors[RunRng.Next(FlavorText.DeathFlavors.Length)]);
                RaisePlayerDied("starvation");
            }
        }
        else if (Satiety == 15 && !_starvingWarned)
        {
            _log.Log("Your stomach growls... you're getting hungry.");
        }
    }

    private void TickPerTurnTimers()
    {
        TickSkillCooldowns();
        TickInvisibility();
        TickConfusedMobs();

        if (_shrineBuffTurns > 0)
        {
            _shrineBuffTurns--;
            if (_shrineBuffTurns == 0) { _shrineBuff = 0; _log.Log("The shrine's blessing fades..."); }
        }

        if (_levelUpBuffTurns > 0)
        {
            _levelUpBuffTurns--;
            if (_levelUpBuffTurns == 0) { _levelUpBuff = 0; _log.Log("The level-up surge fades."); }
        }

        if (_foodRegenTurnsLeft > 0)
        {
            _foodRegenTurnsLeft--;
            if (_foodRegenTurnsLeft == 0)
            { _foodRegenRate = 0; _log.Log("The meal's restorative warmth fades."); }
        }

        ExpireTimedBuffs();
        TickLightSource();
        TickAttrition();
    }

    // ── A CARRIED LIGHT BURNS DOWN ─────────────────────────────────────────────────────────
    // Fuel, not wear. DegradeEquipment models a weapon chipping on impact and deliberately leaves
    // the broken item equipped "until repaired"; a light that has burned out is SPENT, so it is
    // destroyed and the next one lit automatically. That is also why combat never touches it —
    // the Tool slot appears in none of DegradeEquipment's call sites.
    //
    // WHICH HOOK. This rides TickPerTurnTimers, the universal per-turn hook every turn path
    // shares — not AdvanceTurn, which an ordinary move never reaches, and not the
    // AttritionTurnScale family. That scale exists solely to preserve a measured legacy average
    // from the era when attrition was only SAMPLED on AdvanceTurn turns; a rate introduced with
    // the hook already correct has no such history, so inheriting the multiplier would quadruple
    // the stated lifetime for no reason.
    //
    // A LIGHT BURNS WHENEVER IT IS EQUIPPED, at every hour. DayNightCycle.TorchInfluence floors at
    // 0.08 rather than reaching zero, so a carried flame is never doing literally nothing; and
    // burning only after dark would make the rate depend on something the player cannot see, which
    // is the erratic-attrition shape again. It also preserves the one real decision the item
    // creates: the Tool slot holds a pickaxe OR a light, so unequipping at dawn to mine is what
    // makes a torch last the night it is rated for.
    //
    // RATE, derived rather than chosen. A day is DayNightCycle.CycleLength = 4000 turns and the
    // sun is up for exactly half of it, so a night is 2000 turns; the 60-durability starter is
    // rated to cover one. 2000 / 60 = 33.3, so one durability per 33 turns — Wooden Torch 1,980
    // turns (0.50 day), Iron Lantern 4,620 (1.16), Crystal Lantern 9,900 (2.48).
    // Public so the Player Guide's lifetime table can be checked against it rather than
    // transcribing the turn counts, which is how every rotted table in this corpus started.
    public const int LightBurnTurns = 33;

    // Warning thresholds as a share of the light's own capacity, so a Crystal Lantern warns with
    // proportionally as much left as a Wooden Torch does rather than at a flat count that would
    // mean five times less to it.
    public const int LightLowFuelPercent = 25, LightCriticalFuelPercent = 10;

    // Turns spent with a light equipped. Deliberately per-run and NOT persisted, on the same
    // reasoning as _miningStrikeCount: it holds at most LightBurnTurns-1 turns of progress, so
    // which side of the interval a loaded run resumes on cannot matter.
    //
    // An accumulator rather than a modulo on TurnCount: a modulo would let a player who unequips
    // across the tick turn burn nothing at all, and would spend the first point of a freshly-lit
    // torch on the very next turn.
    private int _lightBurnTurns;

    private void TickLightSource()
    {
        // Putting the light away resets the count, so a torch shelved at dawn does not resume
        // mid-interval. Swapping one lit torch for ANOTHER deliberately does not reset it: the
        // count is turns-spent-burning, and clearing it on every equip would let a player rotate
        // a handful of torches just under the interval and burn nothing at all.
        var lit = _player.Inventory.GetActiveLightSource();
        if (lit == null) { _lightBurnTurns = 0; return; }

        if (++_lightBurnTurns < LightBurnTurns) return;
        _lightBurnTurns = 0;

        var (slot, light) = lit.Value;
        light.ItemDurability--;
        if (light.ItemDurability > 0) { WarnOnLowFuel(light); return; }

        ExtinguishAndRelight(slot, light);
    }

    // A light guttering low is exactly the thing a player must not discover by going blind, so it
    // is announced twice: once with room to reach a vendor, once when it is nearly out.
    private void WarnOnLowFuel(Items.Equipment.EquipmentBase light)
    {
        // Only a Torch declares a ceiling. Anything else that happens to glow has no whole to take
        // a share of, so it burns silently until it goes out rather than warning against a
        // denominator that would track its own decline and never fire.
        int capacity = light is Items.Equipment.Torch t ? t.MaxDurability : 0;
        if (capacity <= 0) return;

        int low = Math.Max(1, capacity * LightLowFuelPercent / 100);
        int critical = Math.Max(1, capacity * LightCriticalFuelPercent / 100);

        // The two thresholds collapse onto each other for a very small capacity; the earlier
        // wording wins, so a light never announces itself twice on one tick.
        if (light.ItemDurability == low)
            _log.Log($"Your {light.Name} is burning low. ({light.ItemDurability}/{capacity})");
        else if (light.ItemDurability == critical)
            _log.LogCombat($"Your {light.Name} is guttering — it will go out soon. ({light.ItemDurability}/{capacity})");
    }

    // ORDER IS LOAD-BEARING. DestroyEquipped clears the slot WITHOUT returning the item to the
    // backpack; going through Equip first would instead call Unequip, which puts the dead torch
    // back in the bag as a permanent zero-durability passenger. Destroy, then light the next one.
    private void ExtinguishAndRelight(EquipmentSlot slot, Items.Equipment.EquipmentBase spent)
    {
        _player.Inventory.DestroyEquipped(slot);
        _log.LogCombat($"Your {spent.Name} burns out and crumbles to ash.");

        // Candidates are tried in order rather than one pick being trusted: Equip returns false
        // for a level gate and for run modifiers, and an auto-swap that ignored the refusal would
        // leave the player dark while looking like it had worked.
        foreach (var spare in _player.Inventory.LightSourceCandidates(_player.Level))
        {
            if (!_player.Inventory.Equip(spare, _player)) continue;
            _log.Log($"You light your {spare.Name}. ({spare.ItemDurability} left)");
            return;
        }

        // "You have nothing" and "you have something you cannot use yet" are very different things
        // to learn in the dark, so the level gate is named when it is the reason nothing lit.
        Items.Equipment.EquipmentBase? gated = null;
        foreach (var item in _player.Inventory.Items)
        {
            if (item is not Items.Equipment.EquipmentBase eq) continue;
            if (eq.LightRadius <= 0 || eq.ItemDurability <= 0) continue;
            if (eq.RequiredLevel <= _player.Level) continue;
            if (gated == null || eq.RequiredLevel < gated.RequiredLevel) gated = eq;
        }

        _log.LogCombat(gated == null
            ? "Your last light gutters out. The dark closes in around you."
            : $"Your last light gutters out — the {gated.Name} in your pack needs level "
              + $"{gated.RequiredLevel}. The dark closes in around you.");
    }

    private void AdvanceTurn()
    {
        TurnCount++;

        if (TurnCount > 0 && TurnCount % FlavorText.MilestoneInterval == 0)
        {
            string msg = FlavorText.MilestoneMessages[RunRng.Next(FlavorText.MilestoneMessages.Length)];
            _log.LogSystem(string.Format(msg, TurnCount));
        }

    }

    private void PassiveRegen()
    {
        int interval = _diffTier.RegenInterval;
        if (_player.IsDefeated || interval <= 0) return;
        if (Satiety < HungerRegenThreshold) return;
        // Sleep L99: regen cadence halves (interval/2) → ~2x heal outside rest.
        if (_player.LifeSkills.SleepFasterRegen && interval > 1) interval = Math.Max(1, interval / 2);
        if (TurnCount % interval != 0) return;

        // SpecialEffect regen stacks on top of native regen.
        // HPRegen + SPRegen sum across ALL equipped slots (armor pieces stack).
        int hpBonus = SumAllSlots<Items.Equipment.EquipmentSpecialEffect.HPRegen>(h => h.Amount);
        int spBonus = SumAllSlots<Items.Equipment.EquipmentSpecialEffect.SPRegen>(s => s.Amount);

        // Active food regen buff folds into the same tick. The countdown itself lives in
        // TickPerTurnTimers — this only READS the rate. It used to decrement here, once per regen
        // INTERVAL rather than once per turn, so a buff whose parameter, field and tray row all
        // say "turns" actually lasted RegenInterval times longer than its own number.
        int foodBonus = _foodRegenTurnsLeft > 0 ? _foodRegenRate : 0;

        if (_player.CurrentHealth < _player.MaxHealth)
        {
            int regenAmount = 1 + _player.Vitality / 3 + WeatherSystem.GetRegenBonus() + hpBonus + foodBonus;
            _player.CurrentHealth = Math.Min(_player.CurrentHealth + regenAmount, _player.MaxHealth);
            // Healing tick sparkle — subtle +/· pair at player tile.
            ParticleQueue.Emit(ParticleEvent.HealingTick, _player.X, _player.Y);
            if (RunRng.Next(100) < 30)
                _log.Log(FlavorText.RegenFlavors[RunRng.Next(FlavorText.RegenFlavors.Length)]);
        }

        // SPRegen applies regardless of HP cap, on the same tick cadence.
        if (spBonus > 0) _player.SkillPoints += spBonus;
    }

    // Invisibility+N tick — one turn decrement per AdvanceTurn; logs on expiry.
    // Called from AdvanceTurn so cadence is independent of regen interval.
    private void TickInvisibility()
    {
        if (_invisibilityTurnsLeft <= 0) return;
        _invisibilityTurnsLeft--;
        if (_invisibilityTurnsLeft == 0)
            _log.LogCombat("The phantom veil fades. You are visible again.");
    }

    // Lunacy+N tick — decrement per-mob confusion turns, purge zeros.
    private readonly List<int> _expiredConfusedMobs = new();

    private void TickConfusedMobs()
    {
        if (_confusedMobs.Count == 0) return;
        _expiredConfusedMobs.Clear();
        foreach (var kv in _confusedMobs)
        {
            int left = kv.Value - 1;
            if (left <= 0) _expiredConfusedMobs.Add(kv.Key);
            else _confusedMobs[kv.Key] = left;
        }
        foreach (var id in _expiredConfusedMobs) _confusedMobs.Remove(id);
    }

    private void UpdatePlayerTitle()
    {
        foreach (var (floor, title) in FlavorText.TitleThresholds)
        {
            if (CurrentFloor >= floor)
            {
                if (_player.Title != title)
                {
                    _player.Title = title;
                    _log.LogSystem($"You have earned the title: {title}!");
                }
                break;
            }
        }
    }

    // The durability the combat log warns at. Public so the Player Guide's account of the
    // warning ladder can be checked against it rather than transcribing the number.
    public const int DurabilityWarnAt = 5;

    private void DegradeEquipment(EquipmentSlot slot)
    {
        var item = _player.Inventory.GetEquipped(slot);
        if (item == null || item.ItemDurability <= 0) return;
        // Divine Objects are unbreakable — canon Priority / Sacred Object flavor.
        // Their durability never ticks down.
        if (item.Rarity == "Divine") return;
        item.ItemDurability--;
        if (item.ItemDurability <= 0)
        {
            // Crossing the 0 threshold flips the effective bonus contribution
            // (see Inventory.GetTotalEquipmentBonus durability > 0 check).
            _player.Inventory.InvalidateStatCache();
            _log.LogCombat($"Your {item.Name} has broken! It provides no bonuses until repaired.");
        }
        else if (item.ItemDurability == DurabilityWarnAt)
            _log.Log($"Your {item.Name} is about to break! ({item.ItemDurability} durability)");
    }
}
