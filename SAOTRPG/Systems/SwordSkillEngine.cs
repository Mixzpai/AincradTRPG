using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Systems;

// Sword Skill execution — validates, calculates damage, applies effects,
// fires visual events, and manages cooldowns + post-motion delay.
public partial class TurnManager
{
    public void ExecuteSwordSkill(int slot)
    {
        if (_player.IsDefeated) return;
        if (slot < 0 || slot >= EquippedSkills.Length) return;
        var skill = EquippedSkills[slot];
        if (skill == null) { _log.Log("No skill equipped in that slot."); return; }

        // ── Validation ───────────────────────────────────────────────
        if (_stunTurnsLeft > 0)
        { _log.LogCombat("You are stunned and cannot use skills!"); return; }

        if (_postMotionDelay > 0)
        { _log.LogCombat("Still recovering from your last skill! Wait for post-motion to end."); return; }

        if (GetSkillCooldown(skill.Id) > 0)
        {
            _log.LogCombat($"{skill.Name} is on cooldown! ({GetSkillCooldown(skill.Id)} turns remaining)");
            return;
        }

        var wpn = _player.Inventory.GetEquipped(EquipmentSlot.Weapon) as Weapon;
        string wtype = wpn?.WeaponType ?? "Unarmed";
        if (skill.WeaponType != wtype && skill.WeaponType != "Any")
        { _log.LogCombat($"{skill.Name} requires a {skill.WeaponType}!"); return; }

        // ── Find target ──────────────────────────────────────────────
        if (skill.Type == SkillType.Counter)
        {
            ExecuteCounterSkill(skill);
            return;
        }

        var targets = FindSkillTargets(skill);
        if (targets.Count == 0)
        {
            string rangeHint = skill.Range > 1 ? $" (range {skill.Range})" : "";
            _log.LogCombat($"No enemy in range for {skill.Name}!{rangeHint}");
            return;
        }

        // ── Execute ──────────────────────────────────────────────────
        _lastCombatTurn = TurnCount;
        TutorialSystem.ShowTip(_log, "first_skill_use");

        // Visual: skill activation flash + text
        Color skillColor = skill.Type switch
        {
            SkillType.Rush => Color.BrightCyan,
            SkillType.AoE => Color.BrightRed,
            SkillType.Power => Color.BrightYellow,
            SkillType.Combo => Color.BrightMagenta,
            SkillType.Projectile => Color.BrightGreen,
            _ => Color.BrightYellow,
        };
        SkillActivated?.Invoke(_player.X, _player.Y, skillColor);
        CombatTextEvent?.Invoke(_player.X, _player.Y, $"*{skill.Name}!", skillColor);
        _log.LogCombat($"*Sword Skill: {skill.Name}! ({skill.Hits}-hit, {skill.Type})");

        // Rush skills: dash to target first
        if (skill.Type == SkillType.Rush && targets[0] is Monster rushTarget)
        {
            // Find tile adjacent to target that's closest to player
            (int x, int y)? best = null;
            int bestDist = int.MaxValue;
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = rushTarget.X + dx, ny = rushTarget.Y + dy;
                if (!_map.InBounds(nx, ny)) continue;
                var t = _map.GetTile(nx, ny);
                if (t.BlocksMovement || (t.Occupant != null && t.Occupant != _player)) continue;
                int d = Math.Abs(nx - _player.X) + Math.Abs(ny - _player.Y);
                if (d < bestDist) { bestDist = d; best = (nx, ny); }
            }
            if (best.HasValue && (best.Value.x != _player.X || best.Value.y != _player.Y))
            {
                _map.MoveEntity(_player, best.Value.x, best.Value.y);
                _log.LogCombat($"  You dash toward {rushTarget.Name}!");
            }
        }

        int profBonus = GetProficiencyBonus(wtype);
        int baseAtk = _player.Attack + profBonus + _shrineBuff + _levelUpBuff + SatietyAtkBonus + FatigueAtkPenalty;
        int totalDamage = (int)(baseAtk * skill.DamageMultiplier);

        // SkillDamage flat bonus (post-multiplier) so gear contributes to all skills.
        // Multi-hit: /4 per extra hit to preserve old scaling.
        totalDamage += _player.SkillDamage;
        if (skill.Hits > 1)
            totalDamage += _player.SkillDamage * (skill.Hits - 1) / 4;

        // AoE: split damage across targets
        bool isAoE = skill.Type == SkillType.AoE && targets.Count > 1;
        int perTargetDmg = isAoE
            ? Math.Max(1, (int)(totalDamage / Math.Sqrt(targets.Count)))
            : totalDamage;

        foreach (var target in targets)
        {
            if (target is not Monster monster || monster.IsDefeated) continue;

            int dmg = perTargetDmg;
            bool anyCrit = false;

            // Per-hit crit rolling for multi-hit skills
            int critHits = 0;
            for (int h = 0; h < skill.Hits; h++)
            {
                if (Random.Shared.Next(100) < Math.Max(0, _player.CriticalRate + WeatherSystem.GetCritModifier()))
                    critHits++;
            }
            if (critHits > 0)
            {
                dmg += _player.CriticalHitDamage * critHits;
                anyCrit = true;
            }

            var reward = monster.TakeDamage(dmg);
            string critTag = anyCrit ? $" ({critHits} CRIT!)" : "";
            _log.LogCombat($"  {skill.Name} hits {monster.Name} for {dmg} damage!{critTag}");

            WeaponSwing?.Invoke(_player.X, _player.Y, monster.X, monster.Y,
                GetSwingColor(wpn, anyCrit));
            // Multi-hit cascade (Starburst Stream 16 / Eclipse 27 / MR 11):
            // emit MultiHitStream-flagged popups at 40ms stagger so they
            // bypass the 3-per-tile coalesce and register as a visible storm.
            if (skill.Hits >= 4)
            {
                int perHitDmg = Math.Max(1, dmg / skill.Hits);
                int lastHitDelay = 0;
                for (int h = 0; h < skill.Hits; h++)
                {
                    int delay = h * 40;
                    lastHitDelay = delay;
                    MultiHitStreamRequested?.Invoke(monster.X, monster.Y, perHitDmg, h, delay);
                }
                // Aggregate "×N = total" lands 400ms after the last hit.
                MultiHitAggregateRequested?.Invoke(monster.X, monster.Y, skill.Hits, dmg, lastHitDelay + 400);
                // Fire MultiHitDamageDealt so hit-flash/scorch/border still
                // trigger — but the popup is routed through the cascade, not
                // the single-aggregate path.
                MultiHitDamageDealt?.Invoke(monster.X, monster.Y, dmg, false, anyCrit);
            }
            else
            {
                DamageDealt?.Invoke(monster.X, monster.Y, dmg, false, anyCrit);
            }
            // Sword-arc projectile for ranged or rush skills (player→target).
            if (skill.Range > 1 || skill.Type == SkillType.Projectile || skill.Type == SkillType.Rush)
                ProjectileRequested?.Invoke(_player.X, _player.Y, monster.X, monster.Y,
                    skill.Type == SkillType.Projectile ? '·' : '◇',
                    skillColor, 30, false);
            if (anyCrit)
            {
                CombatTextEvent?.Invoke(monster.X, monster.Y, "CRIT!", Color.BrightRed);
                MapViewShakeRequested?.Invoke(1);
            }

            // Status effect application
            if (skill.StatusEffect != null && skill.StatusChance > 0
                && Random.Shared.NextDouble() < skill.StatusChance)
            {
                ApplySkillStatus(monster, skill.StatusEffect);
            }

            if (monster.IsDefeated && reward != null)
                HandleMonsterKill(monster, reward, wtype, _player.CurrentHealth);

            DegradeEquipment(EquipmentSlot.Weapon);
        }

        // ── Cooldown + post-motion ───────────────────────────────────
        int cdReduction = GetSpecialEffectValue(wpn, "SkillCooldown");
        int cd = Math.Max(0, skill.CooldownTurns + cdReduction); // cdReduction is negative like -1
        if (cd > 0)
            _skillCooldowns[skill.Id] = cd;
        int pmReduction = GetSpecialEffectValue(wpn, "PostMotion");
        int pm = Math.Max(0, skill.PostMotionDelay + pmReduction);
        if (pm > 0)
        {
            _postMotionDelay = pm;
            _log.LogCombat($"  Post-motion delay: {_postMotionDelay} turn(s) of vulnerability!");
        }

        // ── Advance turn ─────────────────────────────────────────────
        AdvanceTurn();
        TickPoison(); TickBleed(); TickSlow();
        if (_player.IsDefeated) return;
        ProcessEntityTurns();
        PassiveRegen();
        UpdateVisibility();
        TurnCompleted?.Invoke();
    }

    private void ExecuteCounterSkill(SwordSkill skill)
    {
        _counterStance = true;
        _log.LogCombat($"*{skill.Name}! You enter a counter stance with {skill.DamageMultiplier:F1}x riposte power!");
        CombatTextEvent?.Invoke(_player.X, _player.Y, $"*{skill.Name}!", Color.BrightYellow);

        if (skill.CooldownTurns > 0)
            _skillCooldowns[skill.Id] = skill.CooldownTurns;

        AdvanceTurn();
        TickPoison(); TickBleed(); TickSlow();
        if (_player.IsDefeated) return;
        ProcessEntityTurns();

        // Counter riposte applied 1.5x default; skill multiplier override
        // handled in ProcessMonsterAttack via _counterStance flag.
        _counterStance = false;
        PassiveRegen();
        TurnCompleted?.Invoke();
    }

    // AoE skills hit all adjacent enemies; single-target skills pick the nearest
    // enemy within the skill's range (Chebyshev distance).
    private List<Entity> FindSkillTargets(SwordSkill skill)
    {
        var targets = new List<Entity>();
        int range = skill.Range;

        if (skill.Type == SkillType.AoE)
        {
            // All adjacent enemies
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = _player.X + dx, ny = _player.Y + dy;
                if (_map.InBounds(nx, ny) && _map.GetTile(nx, ny).Occupant is Monster m && !m.IsDefeated)
                    targets.Add(m);
            }
            return targets;
        }

        // Find nearest enemy within range
        Monster? nearest = null;
        int nearDist = int.MaxValue;
        foreach (var e in _map.Entities)
        {
            if (e == _player || e.IsDefeated || e is not Monster m) continue;
            int dist = Math.Max(Math.Abs(m.X - _player.X), Math.Abs(m.Y - _player.Y));
            if (dist <= range && dist < nearDist) { nearest = m; nearDist = dist; }
        }

        if (nearest != null) targets.Add(nearest);
        return targets;
    }

    // Inflicts a debuff on the monster based on the skill's StatusEffect string.
    // Duration and damage scale with floor to stay relevant at higher levels.
    private void ApplySkillStatus(Monster monster, string status)
    {
        switch (status)
        {
            case "Poison":
                _poisonedMobs[monster.Id] = (4, 1 + CurrentFloor);
                _log.LogCombat($"  {monster.Name} is poisoned!");
                break;
            case "Stun":
                _stunnedMobs[monster.Id] = 2;
                _log.LogCombat($"  {monster.Name} is stunned!");
                break;
            case "Bleed":
                _burningMobs[monster.Id] = (3, 1 + CurrentFloor);
                _log.LogCombat($"  {monster.Name} is bleeding!");
                break;
        }
    }

    // Per-weapon cache of parsed SpecialEffect key->value. Lazy populate.
    // ConditionalWeakTable = entries GC'd with the weapon.
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Weapon, Dictionary<string, int>>
        _specialFxCache = new();

    // Parse SpecialEffect: "SkillCooldown-1", "CritRate+20" → signed int. 0 if missing.
    internal static int GetSpecialEffectValue(Weapon? wpn, string effectName)
    {
        if (wpn?.SpecialEffect == null) return 0;

        // Fast path: look up (or build) the parsed table for this weapon.
        var table = _specialFxCache.GetValue(wpn, BuildSpecialFxTable);
        return table.TryGetValue(effectName, out int val) ? val : 0;
    }

    // Parse SpecialEffect once: "KeyN"/"Key+N"/"Key-N" pairs (letter key, signed int value).
    private static Dictionary<string, int> BuildSpecialFxTable(Weapon wpn)
    {
        var dict = new Dictionary<string, int>();
        var fx = wpn.SpecialEffect;
        if (string.IsNullOrEmpty(fx)) return dict;

        int i = 0;
        while (i < fx.Length)
        {
            // Skip until we hit a letter — start of a key.
            if (!char.IsLetter(fx[i])) { i++; continue; }

            int keyStart = i;
            while (i < fx.Length && char.IsLetter(fx[i])) i++;
            string key = fx.Substring(keyStart, i - keyStart);

            // Collect sign + digits that form the value.
            int numStart = i;
            while (i < fx.Length)
            {
                char c = fx[i];
                if (c == '+' || c == '-' || char.IsDigit(c)) i++;
                else break;
            }
            if (i > numStart && int.TryParse(fx.AsSpan(numStart, i - numStart), out int val))
                dict[key] = val;
        }
        return dict;
    }

    // Called from AdvanceTurn to tick down skill cooldowns and post-motion.
    private void TickSkillCooldowns()
    {
        if (_postMotionDelay > 0)
        {
            _postMotionDelay--;
            if (_postMotionDelay == 0) _log.LogCombat("Post-motion delay has ended.");
        }

        var expired = new List<string>();
        foreach (var kvp in _skillCooldowns)
        {
            if (kvp.Value <= 1) expired.Add(kvp.Key);
            else _skillCooldowns[kvp.Key] = kvp.Value - 1;
        }
        foreach (var id in expired) _skillCooldowns.Remove(id);
    }
}
