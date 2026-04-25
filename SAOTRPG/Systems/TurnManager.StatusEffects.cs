using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;

namespace SAOTRPG.Systems;

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
        // FB-450 status-inflict particle puff alongside the trail glyph.
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
        _log.LogSystem(FlavorText.DeathFlavors[Random.Shared.Next(FlavorText.DeathFlavors.Length)]);
        PlayerDied?.Invoke();
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
        var ach = !_player.IsDefeated ? Achievements.TryUnlock("survive_hemorrhage") : null;
        if (ach != null)
        {
            _player.ColOnHand += ach.ColReward;
            TotalColEarned += ach.ColReward;
            _log.LogSystem($"  **ACHIEVEMENT: {ach.Name} — {ach.Description} (+{ach.ColReward} Col)");
        }
        CheckDotDeath("bleeding");
    }

    // True if the player is currently bleeding.
    public bool IsBleeding => _bleedTurnsLeft > 0;
    // Remaining turns of bleed damage.
    public int BleedTurnsLeft => _bleedTurnsLeft;
    // True if the player is currently stunned.
    public bool IsStunned => _stunTurnsLeft > 0;
    // True if the player is currently slowed.
    public bool IsSlowed => _slowTurnsLeft > 0;

    private void TickExhaustion()
    {
        if (_restCounter >= ExhaustionThreshold && !_exhaustedWarned)
        {
            _exhaustedWarned = true;
            _log.LogCombat("You are EXHAUSTED! Your body screams for rest. (-4 ATK, -2 SPD, -1 DEF)");
        }
        else if (_restCounter >= FatigueThreshold && !_fatiguedWarned)
        {
            _fatiguedWarned = true;
            _log.LogCombat("Fatigue sets in. Your swings feel sluggish. (-2 ATK, -1 SPD)");
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

    private void AdvanceTurn()
    {
        TurnCount++;
        TickSkillCooldowns();
        TickInvisibility();
        TickConfusedMobs();

        if (TurnCount > 0 && TurnCount % FlavorText.MilestoneInterval == 0)
        {
            string msg = FlavorText.MilestoneMessages[Random.Shared.Next(FlavorText.MilestoneMessages.Length)];
            _log.LogSystem(string.Format(msg, TurnCount));
        }

        // Normal hunger + biome-specific extra drain (desert/volcanic/void).
        // FB-564 Iron Rank modifier doubles hunger drain.
        int drainAmount = 1 + BiomeSystem.SatietyDrainBonus;
        if (RunModifiers.IsActive(RunModifier.IronRank)) drainAmount *= 2;
        if (TurnCount % HungerDrainInterval == 0 && Satiety > 0)
            Satiety = Math.Max(0, Satiety - drainAmount);

        // Biome passive damage (volcanic heat, void corruption).
        var (biomeDmg, bioInterval) = BiomeSystem.EnvironmentDamage;
        if (biomeDmg > 0 && bioInterval > 0 && TurnCount % bioInterval == 0 && !_player.IsDefeated)
        {
            _player.TakeDamage(biomeDmg);
            if (TurnCount % (bioInterval * 3) == 0) // log every 3rd tick to reduce spam
                _log.Log($"The {BiomeSystem.DisplayName} environment wears on you. (-{biomeDmg} HP)");
            if (_player.IsDefeated) { LastKillerName = $"the {BiomeSystem.DisplayName}"; PlayerDied?.Invoke(); }
        }

        if (Satiety <= 0 && !_player.IsDefeated)
        {
            _player.TakeDamage(1);
            if (!_starvingWarned)
            {
                _log.LogCombat("You're starving! Eat something before it's too late!");
                _starvingWarned = true;
            }
            if (_player.IsDefeated)
            {
                LastKillerName = "starvation";
                _log.LogSystem(FlavorText.DeathFlavors[Random.Shared.Next(FlavorText.DeathFlavors.Length)]);
                PlayerDied?.Invoke();
            }
        }
        else if (Satiety == 15 && !_starvingWarned)
        {
            _log.Log("Your stomach growls... you're getting hungry.");
        }

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
    }

    private void PassiveRegen()
    {
        int interval = _diffTier.RegenInterval;
        if (_player.IsDefeated || interval <= 0) return;
        if (Satiety < HungerRegenThreshold) return;
        // FB-051 Sleep L99: regen cadence halves (interval/2) → ~2x heal outside rest.
        if (_player.LifeSkills.SleepFasterRegen && interval > 1) interval = Math.Max(1, interval / 2);
        if (TurnCount % interval != 0) return;

        // SpecialEffect regen stacks on top of native regen.
        // Bundle 10 (B14): HPRegen + SPRegen sum across ALL equipped slots (armor pieces stack).
        var regenWpn = _player.Inventory.GetEquipped(EquipmentSlot.Weapon) as Items.Equipment.Weapon;
        int hpBonus = GetEffectSumAllSlots("HPRegen");
        int spBonus = GetEffectSumAllSlots("SPRegen");

        // Bundle 10 (B1) — active food regen buff folds into the same tick.
        // Consumed once per regen interval; expires when turns reach 0.
        int foodBonus = 0;
        if (_foodRegenTurnsLeft > 0)
        {
            foodBonus = _foodRegenRate;
            _foodRegenTurnsLeft--;
            if (_foodRegenTurnsLeft == 0) { _foodRegenRate = 0; _log.Log("The meal's restorative warmth fades."); }
        }

        if (_player.CurrentHealth < _player.MaxHealth)
        {
            int regenAmount = 1 + _player.Vitality / 3 + WeatherSystem.GetRegenBonus() + hpBonus + foodBonus;
            _player.CurrentHealth = Math.Min(_player.CurrentHealth + regenAmount, _player.MaxHealth);
            // FB-450 healing tick sparkle — subtle +/· pair at player tile.
            ParticleQueue.Emit(ParticleEvent.HealingTick, _player.X, _player.Y);
            if (Random.Shared.Next(100) < 30)
                _log.Log(FlavorText.RegenFlavors[Random.Shared.Next(FlavorText.RegenFlavors.Length)]);
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
    private void TickConfusedMobs()
    {
        if (_confusedMobs.Count == 0) return;
        var expired = new List<int>();
        foreach (var kv in _confusedMobs)
        {
            int left = kv.Value - 1;
            if (left <= 0) expired.Add(kv.Key);
            else _confusedMobs[kv.Key] = left;
        }
        foreach (var id in expired) _confusedMobs.Remove(id);
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
        else if (item.ItemDurability == 5)
            _log.Log($"Your {item.Name} is about to break! ({item.ItemDurability} durability)");
    }
}
