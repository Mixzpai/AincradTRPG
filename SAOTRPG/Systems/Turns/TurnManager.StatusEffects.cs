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

    // The parenthetical is built from the penalties, never written out, so the warning names
    // exactly what fatigue charges: attack and defence.
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

    // Every timed duration and the world clock (hunger, biome damage, starvation) tick from
    // TickPerTurnTimers, which PassTurn calls once per turn beside TickPoison/TickBleed/TickSlow,
    // so every turn-consuming path reaches it exactly once.
    //
    // Every attrition interval is multiplied by AttritionTurnScale, which sets the overall pace
    // of hunger, biome damage and starvation identically for every playstyle.
    //
    // TickExhaustion is not here: fatigue is an exertion clock, and its single call site is the
    // walking path, so it builds only while walking.
    private const int AttritionTurnScale = 4;

    // How much hungrier the night is, as a multiple of the base drain interval rather than as a
    // turn count of its own — so it stays a fixed fraction of the base rate if that rate is
    // re-tuned, instead of silently becoming a different difficulty.
    //
    // Four means one extra point per four ordinary drains: +25% while the sun is down. The Night
    // phase is about 7 hours of the day, so the surcharge costs about 25 satiety over a whole
    // night — one ordinary ration (Bread restores 20, Grilled Meat 30). Across a full day it is
    // +7%. Satiety is already the fastest clock in the game, so a much larger surcharge would
    // make food the only thing the player is doing.
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

        // Starvation has no interval of its own: one HP every AttritionTurnScale turns.
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

    // ── A carried light burns down ─────────────────────────────────────────────────────────
    // Fuel, not wear. DegradeEquipment models a weapon chipping on impact and leaves the broken
    // item equipped "until repaired"; a light that has burned out is spent, so it is destroyed
    // and the next one lit automatically. Combat never touches it — the Tool slot appears in
    // none of DegradeEquipment's call sites.
    //
    // This rides TickPerTurnTimers, the per-turn hook every turn path shares, and is not scaled
    // by AttritionTurnScale: LightBurnTurns is the real interval.
    //
    // A light burns whenever it is equipped, at every hour. DayNightCycle.TorchInfluence floors at
    // 0.08 rather than reaching zero, so a carried flame is never doing nothing, and burning only
    // after dark would make the rate depend on something the player cannot see. The Tool slot
    // holds a pickaxe or a light, so unequipping at dawn to mine is what makes a torch last the
    // night it is rated for.
    //
    // Rate: a day is DayNightCycle.CycleLength = 86,400 turns (seconds) and the sun is up for
    // half of it, so a night is 43,200; the 60-durability starter is rated to cover one.
    // 43,200 / 60 = 720 — Wooden Torch 43,200 turns (0.50 day), Iron Lantern 100,800 (1.17),
    // Crystal Lantern 216,000 (2.50).
    // Public so the Player Guide's lifetime table is derived from it rather than transcribed.
    public const int LightBurnTurns = 720;

    // Warning thresholds as a share of the light's own capacity, so a Crystal Lantern warns with
    // proportionally as much left as a Wooden Torch does rather than at a flat count that would
    // mean five times less to it.
    public const int LightLowFuelPercent = 25, LightCriticalFuelPercent = 10;

    // Turns spent with a light equipped. Per-run and not persisted, on the same
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
        // mid-interval. Swapping one lit torch for another deliberately does not reset it: the
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

    // Order matters. DestroyEquipped clears the slot without returning the item to the
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

        // Quiet while waiting or asleep: an eight-hour sleep would print eight of them.
        if (TurnCount % FlavorText.MilestoneInterval == 0 && CurrentActivity == null)
        {
            string msg = FlavorText.MilestoneMessages[RunRng.Next(FlavorText.MilestoneMessages.Length)];
            _log.LogSystem(string.Format(msg, SAOTRPG.Systems.World.GameClock.Clipped(TurnCount)));
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
        // HPRegen + SPRegen sum across all equipped slots (armor pieces stack).
        int hpBonus = SumAllSlots<Items.Equipment.EquipmentSpecialEffect.HPRegen>(h => h.Amount);
        int spBonus = SumAllSlots<Items.Equipment.EquipmentSpecialEffect.SPRegen>(s => s.Amount);

        // Active food regen buff folds into the same tick. The countdown lives in
        // TickPerTurnTimers, once per turn; this only reads the rate.
        int foodBonus = _foodRegenTurnsLeft > 0 ? _foodRegenRate : 0;

        if (_player.CurrentHealth < _player.MaxHealth)
        {
            int regenAmount = 1 + _player.Vitality / 3 + WeatherSystem.GetRegenBonus() + hpBonus + foodBonus
                              + _player.BaseHpRegenPerTick;
            _player.CurrentHealth = Math.Min(_player.CurrentHealth + regenAmount, _player.MaxHealth);
            // Healing tick sparkle — subtle +/· pair at player tile.
            ParticleQueue.Emit(ParticleEvent.HealingTick, _player.X, _player.Y);
            if (RunRng.Next(100) < 30)
                _log.Log(FlavorText.RegenFlavors[RunRng.Next(FlavorText.RegenFlavors.Length)]);
        }

        // SPRegen applies regardless of HP cap, on the same tick cadence.
        if (spBonus > 0) _player.SkillPoints += spBonus;
    }

    // Invisibility+N tick — one decrement per turn; logs on expiry.
    // Called from TickPerTurnTimers, so the cadence is independent of the regen interval.
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
    // warning ladder reads it rather than transcribing the number.
    public const int DurabilityWarnAt = 5;

    private void DegradeEquipment(EquipmentSlot slot)
    {
        var item = _player.Inventory.GetEquipped(slot);
        if (item == null || item.ItemDurability <= 0) return;
        // Divine Objects are unbreakable: their durability never ticks down.
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
