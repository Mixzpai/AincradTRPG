using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Systems;

public partial class TurnManager
{
    // Monster IDs with Pair Resonance banner already shown. Clears on target swap.
    private readonly HashSet<int> _pairResonanceLogged = new();

    // Shake request event. int tier: 1 = light (crit/20%+ HP hit),
    // 2 = heavy (boss special / floor-quake). MapView consumes.
    public event Action<int>? MapViewShakeRequested;

    // Animated projectile request: (sx, sy, ex, ey, glyph, color, msPerCell, isArrow).
    // isArrow flag picks the directional arrow glyph in MapView; non-arrow
    // uses the caller-supplied glyph for sword-arc / bolt variants.
    public event Action<int, int, int, int, char, Color, int, bool>? ProjectileRequested;

    // Status-mote trail request: (x, y, kind). kind: "bleed" | "poison" | "burn".
    public event Action<int, int, string>? StatusTrailRequested;

    // Multi-hit cascade popup per hit — (mx, my, damage, hitIndex, delayMs).
    public event Action<int, int, int, int, int>? MultiHitStreamRequested;

    // Multi-hit final aggregate — (mx, my, totalHits, totalDamage, delayMs).
    public event Action<int, int, int, int, int>? MultiHitAggregateRequested;

    // Mirror of DamageDealt for multi-hit skills — fires side-effect flashes
    // (scorch, hit-flash, border) WITHOUT the popup enqueue (cascade owns popups).
    public event Action<int, int, int, bool, bool>? MultiHitDamageDealt;

    // Formats the "You hit X for Y" line per the active DamageBreakdownMode.
    // raw = pre-mitigation, defShown = estimated armor/def absorbed,
    // resistShown = elemental resist (0 when N/A).
    private static string FormatPlayerHitLog(string monsterName, string weaponName,
        int final, int raw, int defShown, int resistShown)
    {
        return UserSettings.Current.DamageBreakdownMode switch
        {
            DamageBreakdownMode.Concise =>
                $"You hit {monsterName} for {final} dmg ({raw} - {defShown} armor)",
            DamageBreakdownMode.Medium =>
                $"You hit {monsterName}: {raw} raw - {defShown} armor = {final}",
            DamageBreakdownMode.Verbose =>
                $"You hit {monsterName} for {final} dmg ({raw} atk - {defShown} def + {resistShown} resist)",
            _ =>
                $"You hit {monsterName} with {weaponName} for {final} damage!",
        };
    }

    // Builds a damage-type tag string based on weapon type + SpecialEffect.
    // Returns empty when DamageBreakdownMode == Off OR the weapon is bare
    // physical with no clear subtype (generic "hit"). Q26=b rule.
    internal static string BuildDamageTypeTag(Weapon? weapon)
    {
        if (UserSettings.Current.DamageBreakdownMode == DamageBreakdownMode.Off) return "";

        string? tag = InferElementalTag(weapon?.SpecialEffect)
                      ?? InferPhysicalTag(weapon?.WeaponType);
        if (string.IsNullOrEmpty(tag)) return "";

        return UserSettings.Current.DamageTagStyle switch
        {
            DamageTagStyle.Bare => tag,
            DamageTagStyle.Chip => $"◆{tag}◆",
            _ => $"[{tag}]",
        };
    }

    // Builds tag for monster→player incoming damage (Q23=a). Source derived
    // from the monster name keywords — wraith/skeleton=DARK, fire/flame=FIRE, etc.
    internal static string BuildIncomingDamageTag(string? monsterName)
    {
        if (UserSettings.Current.DamageBreakdownMode == DamageBreakdownMode.Off) return "";
        string? tag = InferMonsterDamageTag(monsterName);
        if (string.IsNullOrEmpty(tag)) return "";
        return UserSettings.Current.DamageTagStyle switch
        {
            DamageTagStyle.Bare => tag,
            DamageTagStyle.Chip => $"◆{tag}◆",
            _ => $"[{tag}]",
        };
    }

    private static string? InferElementalTag(string? specialEffect)
    {
        if (string.IsNullOrEmpty(specialEffect)) return null;
        if (specialEffect.Contains("Burn", StringComparison.OrdinalIgnoreCase)
            || specialEffect.Contains("Fire", StringComparison.OrdinalIgnoreCase)) return "FIRE";
        if (specialEffect.Contains("Freeze", StringComparison.OrdinalIgnoreCase)
            || specialEffect.Contains("Ice", StringComparison.OrdinalIgnoreCase)) return "ICE";
        if (specialEffect.Contains("Shock", StringComparison.OrdinalIgnoreCase)
            || specialEffect.Contains("Thunder", StringComparison.OrdinalIgnoreCase)) return "THUNDER";
        if (specialEffect.Contains("Holy", StringComparison.OrdinalIgnoreCase)) return "HOLY";
        if (specialEffect.Contains("Dark", StringComparison.OrdinalIgnoreCase)) return "DARK";
        if (specialEffect.Contains("Poison", StringComparison.OrdinalIgnoreCase)) return "POISON";
        if (specialEffect.Contains("Bleed", StringComparison.OrdinalIgnoreCase)) return "BLEED";
        return null;
    }

    private static string? InferPhysicalTag(string? weaponType) => weaponType switch
    {
        "One-Handed Sword" or "Two-Handed Sword" or "Scimitar" or "Katana" or "Claws" => "SLASH",
        "Rapier" or "Spear" => "THRUST",
        "Mace" => "BLUNT",
        "Dagger" or "Bow" => "PIERCE",
        "Scythe" => "CUT",
        _ => null,
    };

    private static string? InferMonsterDamageTag(string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        string n = name.ToLowerInvariant();
        if (n.Contains("wraith") || n.Contains("shadow") || n.Contains("dark")) return "DARK";
        if (n.Contains("flame") || n.Contains("fire") || n.Contains("lava")
            || n.Contains("drake") || n.Contains("dragon")) return "FIRE";
        if (n.Contains("frost") || n.Contains("ice") || n.Contains("snow")) return "ICE";
        if (n.Contains("thunder") || n.Contains("spark") || n.Contains("storm")) return "THUNDER";
        if (n.Contains("holy") || n.Contains("angel") || n.Contains("light")) return "HOLY";
        if (n.Contains("spider") || n.Contains("serpent") || n.Contains("wasp")) return "POISON";
        if (n.Contains("skeleton") || n.Contains("zombie")) return "DARK";
        if (n.Contains("wolf") || n.Contains("beast") || n.Contains("bear")) return "SLASH";
        return null;
    }

    // Glues a damage-type tag onto a log line in the configured position.
    internal static string ApplyDamageTag(string baseLine, string tag)
    {
        if (string.IsNullOrEmpty(tag)) return baseLine;
        return UserSettings.Current.DamageTagPosition switch
        {
            DamageTagPosition.Suffix => $"{baseLine} {tag}",
            DamageTagPosition.Inline => InlineInsert(baseLine, tag),
            _ => $"{tag} {baseLine}",
        };
    }

    // Inline insertion: before the final "damage"/"dmg" word so the tag reads
    // "…for 8 [SLASH] dmg" per the research spec.
    private static string InlineInsert(string line, string tag)
    {
        int idx = line.LastIndexOf(" dmg", StringComparison.Ordinal);
        if (idx < 0) idx = line.LastIndexOf(" damage", StringComparison.Ordinal);
        if (idx < 0) return $"{line} {tag}";
        return line.Substring(0, idx) + " " + tag + line.Substring(idx);
    }

    // Returns the Barrier+N value from the currently-equipped main weapon + OH shield.
    // Called on floor entry + ReplaceMap to refill the per-floor pool.
    private int GetBarrierCapacity()
    {
        return Math.Max(0, GetEffectSum("Barrier"));
    }

    // Bundle 8: aggregate MH weapon + OH shield SpecialEffect value.
    // Additive by default (sums); callers use GetEffectMax for cap-like effects (CritImmune).
    internal int GetEffectSum(string effectName)
    {
        var wpn = _player.Inventory.GetEquipped(EquipmentSlot.Weapon) as EquipmentBase;
        int v = GetSpecialEffectValue(wpn, effectName);
        if (_player.Inventory.GetEquipped(EquipmentSlot.OffHand) is Armor shield)
            v += GetSpecialEffectValue(shield, effectName);
        return v;
    }

    // Bundle 8: MH + OH cap-style effect — picks the higher of the two (non-stacking).
    internal int GetEffectMax(string effectName)
    {
        var wpn = _player.Inventory.GetEquipped(EquipmentSlot.Weapon) as EquipmentBase;
        int a = GetSpecialEffectValue(wpn, effectName);
        int b = 0;
        if (_player.Inventory.GetEquipped(EquipmentSlot.OffHand) is Armor shield)
            b = GetSpecialEffectValue(shield, effectName);
        return Math.Max(a, b);
    }

    // Bundle 10 (B14) — additive defensive keys (BlockChance, ParryChance, EvadeRegen, HPRegen, SPRegen)
    // sum across every equipped slot. On-hit procs (Bleed/Stun/SlowOnHit) stay weapon-only by canon.
    private static readonly EquipmentSlot[] _allEquippedSlots =
    {
        EquipmentSlot.Weapon, EquipmentSlot.OffHand, EquipmentSlot.Head, EquipmentSlot.Chest,
        EquipmentSlot.Legs, EquipmentSlot.Feet, EquipmentSlot.RightRing, EquipmentSlot.LeftRing,
        EquipmentSlot.Bracelet, EquipmentSlot.Necklace,
    };
    internal int GetEffectSumAllSlots(string effectName)
    {
        int total = 0;
        foreach (var slot in _allEquippedSlots)
        {
            if (_player.Inventory.GetEquipped(slot) is EquipmentBase eq)
                total += GetSpecialEffectValue(eq, effectName);
        }
        return total;
    }

    // DragonSlayer+N target check. True if LootTag=="dragon" or the name contains
    // a draconic keyword (dragon/wyrm/wyvern/drake). Fatal Scythe is excluded (undead).
    private static bool IsDragonType(Monster monster)
    {
        if (monster is Mob mob && mob.LootTag == "dragon") return true;
        string n = monster.Name;
        if (string.IsNullOrEmpty(n)) return false;
        string lower = n.ToLowerInvariant();
        if (lower.Contains("dragon") || lower.Contains("wyrm")
            || lower.Contains("wyvern") || lower.Contains("drake")) return true;
        return false;
    }

    private void HandleCombat(Monster monster, int hpBefore)
    {
        var wpn = _player.Inventory.GetEquipped(EquipmentSlot.Weapon) as Weapon;
        string wpnType = wpn?.WeaponType ?? "Unarmed";
        int profBonus = GetProficiencyBonus(wpnType);

        if (_comboTarget == monster.Id) _comboCount++;
        else { _comboTarget = monster.Id; _comboCount = 1; _pairResonanceLogged.Clear(); }
        int comboBonus = Math.Max(0, (_comboCount - 1) * 2);
        // SpecialEffect: ComboBonus+N increases combo damage by N%
        int comboMulPct = GetSpecialEffectValue(wpn, "ComboBonus");
        if (comboMulPct > 0 && comboBonus > 0)
            comboBonus = comboBonus * (100 + comboMulPct) / 100;
        bool isFinisher = _comboCount == 5;

        _lastCombatTurn = TurnCount;
        bool backstab = !_aggroAlerted.Contains(monster.Id);
        // Bundle 10 (B8) — first-hit bestiary capture (insta-kill safety) + mirror
        // danger warning. Idempotent with AI.cs aggro-side capture.
        if (_aggroAlerted.Add(monster.Id))
        {
            Bestiary.RecordMonsterEncounter(monster, CurrentFloor);
            if (monster.Level >= _player.Level + 3 && _dangerWarned.Add(monster.Id))
            {
                int diff = monster.Level - _player.Level;
                string warn = diff >= 6 ? "!! EXTREME DANGER !!" : "!! DANGER !!";
                _log.LogCombat($"{warn} {monster.Name} (Lv{monster.Level}) -- {diff} levels above you!");
            }
        }
        var (baseDmg, playerCrit) = _player.AttackMonster(monster);

        // FD Pair Resonance: canonical MH+OH pair → +10% total dmg (per hit), +5% crit
        // re-roll on FIRST hit only (Bundle 8 fix: was per-hit, stacked with +10% compound).
        var offHandWeapon = _player.Inventory.GetEquipped(EquipmentSlot.OffHand) as Weapon;
        bool pairResonance = wpn != null && offHandWeapon != null
            && DualWieldPairs.IsCanonicalPair(wpn.DefinitionId, offHandWeapon.DefinitionId);
        // Gate the 5% re-roll by the same banner-hash-set: single-shot per encounter.
        if (pairResonance && !playerCrit && !_pairResonanceLogged.Contains(monster.Id)
            && Random.Shared.Next(100) < 5)
        {
            playerCrit = true;
        }

        int damage = baseDmg + profBonus + comboBonus + _shrineBuff + _levelUpBuff
            + SatietyAtkBonus + FatigueAtkPenalty + BiomeSystem.AttackModifier;

        // FB-063 Guild combat — Fuurinkazan: +10 ATK (Katana); LB: +15 vs LC PKers.
        damage += GuildSystem.KatanaAttackBonus(_player, wpnType);
        damage += GuildSystem.LegendBravesVsLcBonus(_player, monster.Name);
        if (pairResonance)
            damage = damage * 110 / 100;

        // Unique-skill passive dmg mods. Dual-Blades OH sword is NOT a shield —
        // Holy Sword's shield bonus only applies for true shields.
        var offHandItem = _player.Inventory.GetEquipped(Inventory.Core.EquipmentSlot.OffHand);
        bool hasShield = offHandItem != null && offHandItem is not Weapon;
        int uniqueBonusPct = Skills.UniqueSkillSystem.DamageBonusPercent(wpnType, hasShield)
                           + Skills.UniqueSkillSystem.ElementalBonusPercent(monster.Name);
        if (uniqueBonusPct > 0)
            damage = damage * (100 + uniqueBonusPct) / 100;

        // SpecialEffect damage multipliers — HolyDamage vs undead/demon,
        // DragonSlayer vs dragons, FrostDamage generic elemental bonus.
        int holyPct = GetSpecialEffectValue(wpn, "HolyDamage");
        if (holyPct > 0 && monster is Mob hm
            && (hm.LootTag == "undead" || hm.LootTag == "demon"))
            damage = damage * (100 + holyPct) / 100;
        int dragonPct = GetSpecialEffectValue(wpn, "DragonSlayer");
        if (dragonPct > 0 && IsDragonType(monster))
            damage = damage * (100 + dragonPct) / 100;
        int frostPct = GetSpecialEffectValue(wpn, "FrostDamage");
        if (frostPct > 0) damage = damage * (100 + frostPct) / 100;
        // NightDamage+N — only applies during night (SunLevel < 0.35).
        int nightPct = GetSpecialEffectValue(wpn, "NightDamage");
        if (nightPct > 0 && SAOTRPG.Map.DayNightCycle.SunLevel < 0.35f)
            damage = damage * (100 + nightPct) / 100;
        // ArmorPierce+N — ignore N% of monster defense (bonus damage add).
        int armorPiercePct = GetSpecialEffectValue(wpn, "ArmorPierce");
        if (armorPiercePct > 0)
            damage += Math.Max(0, monster.BaseDefense) * armorPiercePct / 100;
        // PiercingShot+N — bow-only variant. Stacks with ArmorPierce on bows.
        int piercePct = GetSpecialEffectValue(wpn, "PiercingShot");
        if (piercePct > 0 && wpnType == "Bow")
            damage += Math.Max(0, monster.BaseDefense) * piercePct / 100;
        // TrueStrike+N — on proc, bypass any mitigation: raw+profBonus,
        // crit-quality clean hit. Current engine has no monster evade on
        // player swings, so we honor intent by ensuring no-floor damage.
        int trueStrikeChance = GetSpecialEffectValue(wpn, "TrueStrike");
        bool trueStrikeProc = trueStrikeChance > 0
            && Random.Shared.Next(100) < trueStrikeChance;
        if (trueStrikeProc)
        {
            damage = Math.Max(damage, baseDmg + profBonus + comboBonus);
            _log.LogCombat($"  True Strike! {wpn?.Name ?? "Your blow"} ignores all guard.");
        }
        if (backstab)
        {
            int backstabMul = 2 + GetSpecialEffectValue(wpn, "BackstabDmg") / 50; // +50% → x3
            damage *= backstabMul;
        }

        if (isFinisher)
        {
            damage *= 2;
            string finisher = FlavorText.ComboFinisherFlavors[Random.Shared.Next(FlavorText.ComboFinisherFlavors.Length)];
            _log.LogCombat(finisher);
            _log.LogCombat($"  5-hit combo finisher! (x2 damage = {damage})");
            _comboCount = 0;
            _comboTarget = -1;
        }
        else if (_comboCount >= 2)
        {
            TutorialSystem.ShowTip(_log, "first_combo");
            string comboFlavor = _comboCount switch { 2 => "Double strike!", 3 => "Triple strike!", 4 => "Quad strike!", _ => "" };
            _log.LogCombat($"  {_comboCount}-hit combo! {comboFlavor} (+{comboBonus} bonus damage)");
        }

        if (backstab)
        {
            _log.LogCombat($"You strike {monster.Name} from the shadows! BACKSTAB! (x2 damage)");
            CombatTextEvent?.Invoke(monster.X, monster.Y, "BACKSTAB!", Color.BrightMagenta);
        }

        var reward = monster.TakeDamage(damage);
        // ExecuteThreshold+N — if surviving HP% is below N, force-kill. Applies
        // post-damage, pre-defeat-check so loot/xp flow through HandleMonsterKill.
        int executePct = GetSpecialEffectValue(wpn, "ExecuteThreshold");
        if (executePct > 0 && !monster.IsDefeated && monster.MaxHealth > 0
            && monster.CurrentHealth * 100 / monster.MaxHealth < executePct)
        {
            _log.LogCombat($"  EXECUTE! {wpn?.Name ?? "Your weapon"} finishes {monster.Name}!");
            CombatTextEvent?.Invoke(monster.X, monster.Y, "EXECUTE!", Color.BrightRed);
            var execReward = monster.TakeDamage(monster.CurrentHealth);
            if (execReward != null) reward = execReward;
        }
        string wpnName = wpn?.Name ?? "Fists";
        string critTag = playerCrit ? " CRITICAL!" : "";
        if (pairResonance && _pairResonanceLogged.Add(monster.Id))
            _log.LogCombat($"  ◆ Pair Resonance! {wpn!.Name} and {offHandWeapon!.Name} sing together (+10% damage, +5% crit).");
        // Damage breakdown (FB-463): Off/Concise/Medium/Verbose chosen in Options.
        // Raw = pre-defense; defShown = monster.Defense mitigation estimate.
        int rawDmg = baseDmg + profBonus + comboBonus + _shrineBuff + _levelUpBuff + SatietyAtkBonus;
        int defShown = Math.Max(0, rawDmg - damage);
        string hitLine = FormatPlayerHitLog(monster.Name, wpnName, damage, rawDmg, defShown, 0);
        string tag = BuildDamageTypeTag(wpn);
        hitLine = ApplyDamageTag(hitLine, tag);
        _log.LogCombat(hitLine + critTag);
        WeaponSwing?.Invoke(_player.X, _player.Y, monster.X, monster.Y, GetSwingColor(wpn, playerCrit));
        // Bow/ranged: emit animated arrow projectile from player→target.
        if (wpnType == "Bow")
            ProjectileRequested?.Invoke(_player.X, _player.Y, monster.X, monster.Y,
                '·', Color.BrightYellow, 40, true);
        // FB-450 sword slash particles — 3-arc spray in the swing direction.
        int dxSwing = Math.Sign(monster.X - _player.X);
        int dySwing = Math.Sign(monster.Y - _player.Y);
        ParticleQueue.Emit(ParticleEvent.SwordSlash, monster.X, monster.Y, dxSwing, dySwing);
        if (playerCrit) ParticleQueue.Emit(ParticleEvent.CritShatter, monster.X, monster.Y);
        DamageDealt?.Invoke(monster.X, monster.Y, damage, false, playerCrit);
        // Screen shake (FB-453): crit = tier 1, boss heavies handled in BossAI.
        if (playerCrit) MapViewShakeRequested?.Invoke(1);
        if (playerCrit)
        {
            CombatTextEvent?.Invoke(monster.X, monster.Y, "CRIT!", Color.BrightRed);
            TutorialSystem.ShowTip(_log, "first_crit");
        }
        DegradeEquipment(EquipmentSlot.Weapon);

        // Dual Blades / FD Paired OH swing at 60% dmg. Paired weapons bypass
        // DualBlades unlock. Pair Resonance +10% applies to OH too.
        if (!monster.IsDefeated
            && _player.Inventory.GetEquipped(EquipmentSlot.OffHand) is Weapon offhand
            && (Skills.UniqueSkillSystem.HasDualBlades() || offhand.IsDualWieldPaired))
        {
            int offhandDmg = Math.Max(1, offhand.BaseDamage * 60 / 100 + profBonus / 2);
            if (pairResonance) offhandDmg = offhandDmg * 110 / 100;
            // Bundle 10 (B3) — OH rolls crit independently. CriticalHitDamage adds
            // ONCE to the OH damage component (no double-stacking with MH crit).
            bool offhandCrit = Random.Shared.Next(100)
                < Math.Max(0, _player.CriticalRate + WeatherSystem.GetCritModifier());
            if (offhandCrit) offhandDmg += _player.CriticalHitDamage;
            monster.TakeDamage(offhandDmg);
            string ohCritTag = offhandCrit ? " CRITICAL!" : "";
            _log.LogCombat($"You strike again with {offhand.Name} for {offhandDmg} damage!{ohCritTag}");
            WeaponSwing?.Invoke(_player.X, _player.Y, monster.X, monster.Y, GetSwingColor(offhand, offhandCrit));
            if (offhandCrit) ParticleQueue.Emit(ParticleEvent.CritShatter, monster.X, monster.Y);
            DamageDealt?.Invoke(monster.X, monster.Y, offhandDmg, false, offhandCrit);
            DegradeEquipment(EquipmentSlot.OffHand);
        }

        // SpecialEffect: CritHeal — heal a % of damage on crit
        int critHealPct = GetSpecialEffectValue(wpn, "CritHeal");
        if (playerCrit && critHealPct > 0)
        {
            int heal = Math.Max(1, damage * critHealPct / 100);
            _player.CurrentHealth = Math.Min(_player.CurrentHealth + heal, _player.MaxHealth);
            _log.LogCombat($"  {wpn!.Name} drains {heal} HP from the critical hit!");
        }

        // SpecialEffect: Invisibility+N — crit conceals player for N turns.
        // Consumed by AI.cs via _invisibilityTurnsLeft > 0 cutting aggro range.
        int invisTurns = GetSpecialEffectValue(wpn, "Invisibility");
        if (playerCrit && invisTurns > 0)
        {
            _invisibilityTurnsLeft = Math.Max(_invisibilityTurnsLeft, invisTurns);
            _log.LogCombat($"  Phantom! {wpn!.Name} conceals you for {invisTurns} turns.");
        }

        // SpecialEffect: Bleed — chance to apply bleed on normal attacks
        int bleedChance = GetSpecialEffectValue(wpn, "Bleed");
        if (bleedChance > 0 && !monster.IsDefeated && Random.Shared.Next(100) < bleedChance)
        {
            _burningMobs[monster.Id] = (3, 1 + CurrentFloor);
            _log.LogCombat($"  {monster.Name} is bleeding from {wpn!.Name}!");
        }

        // SpecialEffect: Stun+N — chance to stun on normal attacks (2-turn).
        int stunChance = GetSpecialEffectValue(wpn, "Stun");
        if (stunChance > 0 && !monster.IsDefeated && Random.Shared.Next(100) < stunChance)
        {
            _stunnedMobs[monster.Id] = 2;
            _log.LogCombat($"  {monster.Name} is stunned by {wpn!.Name}!");
        }
        // SpecialEffect: Poison+N — chance to poison on hit (floor-scaled dmg).
        int poisonChance = GetSpecialEffectValue(wpn, "Poison");
        if (poisonChance > 0 && !monster.IsDefeated && Random.Shared.Next(100) < poisonChance)
        {
            _poisonedMobs[monster.Id] = (4, 1 + CurrentFloor);
            _log.LogCombat($"  {monster.Name} is poisoned by {wpn!.Name}!");
        }
        // SpecialEffect: BlindOnHit+N — chance to blind on hit (halves atk).
        int blindChance = GetSpecialEffectValue(wpn, "BlindOnHit");
        if (blindChance > 0 && !monster.IsDefeated && Random.Shared.Next(100) < blindChance)
        {
            _blindedMobs[monster.Id] = 3;
            _log.LogCombat($"  {monster.Name} is blinded by {wpn!.Name}!");
        }
        // SpecialEffect: Lunacy+N — N% chance to confuse target AI for 2 turns.
        // Consumed by AI.cs: confused mobs pick a random adjacent tile.
        int lunacyChance = GetSpecialEffectValue(wpn, "Lunacy");
        if (lunacyChance > 0 && !monster.IsDefeated && Random.Shared.Next(100) < lunacyChance)
        {
            _confusedMobs[monster.Id] = 2;
            _log.LogCombat($"  {monster.Name} reels with lunacy from {wpn!.Name}!");
        }
        // Bundle 10 (B11) — SlowOnHit+N: N% chance to slow target for 3 turns.
        // Consumed by AI.cs: slowed mobs act every other turn (skip alternate turns).
        int slowChance = GetSpecialEffectValue(wpn, "SlowOnHit");
        if (slowChance > 0 && !monster.IsDefeated && Random.Shared.Next(100) < slowChance)
        {
            _slowedMobs[monster.Id] = 3;
            _log.LogCombat($"  {monster.Name} is slowed by {wpn!.Name}!");
        }
        // SpecialEffect: Cleave+N — splash N% damage to up-to-2 adjacent foes.
        int cleavePct = GetSpecialEffectValue(wpn, "Cleave");
        if (cleavePct > 0 && damage > 0)
        {
            int splashDmg = Math.Max(1, damage * cleavePct / 100);
            int splashHits = 0;
            foreach (var e in _map.Entities)
            {
                if (splashHits >= 2) break;
                if (e == monster || e == _player || e is not Monster near) continue;
                if (near.IsDefeated) continue;
                int d = Math.Max(Math.Abs(near.X - monster.X), Math.Abs(near.Y - monster.Y));
                if (d != 1) continue;
                near.TakeDamage(splashDmg);
                _log.LogCombat($"  Cleave! {wpn!.Name} splashes {near.Name} for {splashDmg}.");
                DamageDealt?.Invoke(near.X, near.Y, splashDmg, false, false);
                splashHits++;
            }
        }

        if (!monster.IsDefeated)
        {
            int hpPct = monster.CurrentHealth * 100 / monster.MaxHealth;
            _log.LogCombat($"  {monster.Name}: {hpPct}% HP remaining");
        }

        if (monster.IsDefeated && reward != null)
        {
            TutorialSystem.ShowTip(_log, "first_kill");
            HandleMonsterKill(monster, reward, wpnType, hpBefore);
        }
        else if (!monster.IsDefeated) ShowMonsterCondition(monster);
    }

    private void HandleMonsterKill(Monster monster, Monster.DefeatReward reward, string wpnType, int hpBefore)
    {
        // Bestiary read BEFORE RecordKill so TimesKilled==0 identifies a
        // species first-kill. Post-record fire routes to the toast queue.
        bool speciesFirst = Bestiary.Get(monster.Name)?.TimesKilled == 0;
        Bestiary.RecordKill(monster.Name);
        if (speciesFirst) SpeciesFirstKilled?.Invoke(monster.Name);
        QuestSystem.OnMobKilled(monster.Name, _log, wpnType);

        // FB-063 Karma — PKer humans +, peaceful -, hostile non-human neutral.
        // Town Guard kills also grant +20 LC rep.
        string lootTag = monster is Mob mob ? mob.LootTag : "generic";
        int karmaDelta = KarmaSystem.DeltaForMobKill(monster.Name, lootTag);
        if (karmaDelta != 0)
            KarmaSystem.Adjust(_player, karmaDelta, $"slew {monster.Name}", _log);
        if (monster.Name == "Town Guard" && _player.ActiveGuildId == Story.Faction.LaughingCoffin)
        {
            Story.StorySystem.AdjustRep(Story.Faction.LaughingCoffin, 20);
            _log.Log("  Laughing Coffin notes your handiwork. (+20 LC rep)");
        }
        // FB-058 Title — check unlocks per kill for immediate milestone banner.
        CheckTitleUnlocksAfterKill(monster);
        // Player Guide: unmask "Monster/Boss/Field Boss: <name>" entry.
        if (monster is FieldBoss fb)
            Story.PlayerGuideKnowledge.MarkKnown("Field Boss: " + fb.Name);
        else if (monster is Boss b)
            Story.PlayerGuideKnowledge.MarkKnown("Boss: " + b.Name);
        else
            Story.PlayerGuideKnowledge.MarkKnown(
                "Monster: " + Story.PlayerGuideKnowledge.StripAffix(monster.Name));
        KillCount++;
        _killStreak++;
        _killsByName[monster.Name] = _killsByName.GetValueOrDefault(monster.Name) + 1;

        if (_bountyTarget != null && !_bountyComplete && monster.Name.Contains(_bountyTarget))
        {
            _bountyKillsCurrent++;
            if (_bountyKillsCurrent >= _bountyKillsNeeded)
            {
                _bountyComplete = true;
                _player.ColOnHand += _bountyRewardCol;
                TotalColEarned += _bountyRewardCol;
                int bLvl = _player.Level;
                _player.GainExperience(_bountyRewardXp);
                if (_player.Level > bLvl) LeveledUp?.Invoke();
                _log.LogSystem($"  **BOUNTY COMPLETE! +{_bountyRewardCol} Col, +{_bountyRewardXp} EXP!");
            }
            else
                _log.Log($"  Bounty progress: {_bountyKillsCurrent}/{_bountyKillsNeeded} {_bountyTarget}");
        }

        int wkBefore = _weaponKills.GetValueOrDefault(wpnType, 0);
        int profLvlBefore = ComputeLevel(wkBefore);
        _weaponKills[wpnType] = wkBefore + 1;
        int wk = _weaponKills[wpnType];
        int profLvlAfter = ComputeLevel(wk);

        // Weapon-milestone unique-skill unlocks (Katana Mastery @ 100, Martial Arts @ 30, etc).
        var milestoneUnlock = Skills.UniqueSkillSystem.CheckWeaponKillMilestone(wpnType, wk);
        if (milestoneUnlock != null) NotifyUniqueSkillUnlock(milestoneUnlock.Value);
        foreach (var rank in ProficiencyRanks)
        {
            if (wk == rank.Kills)
            {
                _log.LogSystem($"  {wpnType} proficiency: {rank.Rank}! (L{profLvlAfter}/{MaxProfLevel})");
                string flavor = GetRankUpFlavor(rank.Rank);
                if (flavor.Length > 0) _log.LogSystem($"  \"{flavor}\"");
            }
        }
        // Numeric level-ups between cosmetic rank thresholds.
        if (profLvlAfter > profLvlBefore
            && !ProficiencyRanks.Any(r => r.Kills == wk))
        {
            _log.Log($"  {wpnType} proficiency L{profLvlAfter}/{MaxProfLevel}");
        }
        // Fork threshold crossing (L25/50/75/100) — fires the picker event.
        CheckForkThresholdOnKill(wpnType, profLvlBefore, profLvlAfter);

        // Sword skill unlocks at this kill count. FB-564 Hollow Ingress doubles reqs.
        int killMult = RunModifiers.IsActive(RunModifier.HollowIngress) ? 2 : 1;
        foreach (var skill in SwordSkillDatabase.ForWeapon(wpnType))
        {
            if (skill.RequiredProfKills * killMult == wk)
            {
                _log.LogSystem($"  **Sword Skill Unlocked: {skill.Name}! Press F to equip it.");
                TutorialSystem.ShowTip(_log, "first_skill_unlock");
                CombatTextEvent?.Invoke(_player.X, _player.Y, $"NEW SKILL!", Color.BrightCyan);
                ToastQueue.EnqueueSwordSkill(skill.Name);
                // Auto-equip to first empty slot
                for (int i = 0; i < EquippedSkills.Length; i++)
                {
                    if (EquippedSkills[i] == null) { EquippedSkills[i] = skill; break; }
                }
            }
        }

        string defeatMsg = string.Format(
            FlavorText.DefeatFlavors[Random.Shared.Next(FlavorText.DefeatFlavors.Length)], monster.Name);
        _log.LogCombat(defeatMsg);

        if (KillCount == 1) _log.LogSystem("Your first kill in Aincrad! The journey begins.");

        HandleKillMilestone();
        HandleKillStreakCallout();

        int xp = CalcXpReward(monster, reward);
        int col = reward.Col;
        int streakTier = Math.Min(_killStreak / 5, 3);
        int streakPct = streakTier * _diffTier.ColStreakBonus;
        int streakBonus = col * streakPct / 100;
        bool perfectKill = _player.CurrentHealth == hpBefore;
        int perfectBonus = perfectKill ? col / 2 : 0;

        int lvlBefore = _player.Level;
        _player.GainExperience(xp);
        if (_player.Level > lvlBefore)
        {
            _levelUpBuff = 3 + _player.Level;
            _levelUpBuffTurns = 10;
            _log.LogSystem($"  Level-up surge! +{_levelUpBuff} ATK for 10 turns!");
            TutorialSystem.ShowTip(_log, "first_level_up");
            LeveledUp?.Invoke();
        }

        int totalCol = col + perfectBonus + streakBonus;
        _player.ColOnHand += totalCol;
        TotalColEarned += totalCol;

        string tierTag = monster is Mob { Variant: not "" } vm ? $"[{vm.Variant}] " : "";
        _log.LogCombat($"  >> {tierTag}{monster.Name} slain! +{xp} XP  +{totalCol} Col");
        if (perfectKill) _log.LogCombat("     PERFECT KILL! (+50% Col)");
        if (streakBonus > 0) _log.LogCombat($"     Streak bonus: +{streakBonus} Col");
        if (reward.WasOverkill)
        {
            string okMsg = string.Format(
                FlavorText.OverkillFlavors[Random.Shared.Next(FlavorText.OverkillFlavors.Length)],
                reward.OverkillDamage * 2);
            _log.LogCombat(okMsg);
        }

        if (monster is FieldBoss fieldBoss)
        {
            _log.LogSystem("════════════════════════════════════");
            _log.LogSystem($"  FIELD BOSS DEFEATED: {fieldBoss.Name}");
            _log.LogSystem("════════════════════════════════════");
            DefeatedFieldBosses.Add(fieldBoss.FieldBossId);
            if (!string.IsNullOrEmpty(fieldBoss.GuaranteedDropId))
            {
                var drop = Items.ItemRegistry.Create(fieldBoss.GuaranteedDropId);
                if (drop != null)
                {
                    _map.AddItem(fieldBoss.X, fieldBoss.Y, drop);
                    _log.LogLoot($"  {fieldBoss.Name} drops: {drop.Name}!");
                    // Bundle 8: field-boss Divine couriers (F40/F85/F95) also trip the cap + banner.
                    if (drop is Items.Equipment.Weapon fbWpn && drop.Rarity == "Divine")
                        NotifyDivineObtained(fbWpn);
                }
            }
            // IF series field bosses drop matching series shield alongside primary.
            if (!string.IsNullOrEmpty(fieldBoss.FieldBossId)
                && LootGenerator.FieldBossSecondaryDrops.TryGetValue(fieldBoss.FieldBossId, out var secondaryId))
            {
                var secondary = Items.ItemRegistry.Create(secondaryId);
                if (secondary != null)
                {
                    _map.AddItem(fieldBoss.X, fieldBoss.Y, secondary);
                    _log.LogLoot($"  {fieldBoss.Name} also drops: {secondary.Name}!");
                }
            }
            // HF Last-Attack Bonus — F70+ field boss, matching weapon type:
            // 2% base / 10% on canon HNM. OHS has no canon Avatar.
            if (CurrentFloor >= 70
                && LootGenerator.AvatarWeaponByWeaponType.TryGetValue(wpnType, out var avatarDefId))
            {
                bool isHnm = !string.IsNullOrEmpty(fieldBoss.FieldBossId)
                    && LootGenerator.CanonHnmBosses.Contains(fieldBoss.FieldBossId);
                int rollThreshold = isHnm ? 10 : 2;
                if (Random.Shared.Next(100) < rollThreshold)
                {
                    var avatar = Items.ItemRegistry.Create(avatarDefId);
                    if (avatar != null)
                    {
                        _map.AddItem(fieldBoss.X, fieldBoss.Y, avatar);
                        _log.LogLoot($"  ◈ Last-Attack Bonus! {fieldBoss.Name} drops: {avatar.Name}!");
                    }
                }
            }
            // Nicholas beaten → mark this year's seasonal flag.
            if (fieldBoss.IsSeasonal && fieldBoss.SeasonalEventId == "christmas")
                Story.ProfileData.MarkSeen($"nicholas_christmas_{DateTime.Now.Year}");

            RollBossOreDrops(fieldBoss.X, fieldBoss.Y, fieldBoss.Name);

            // F95+ field boss: 10% chance to drop a Corruption Stone (HF
            // workaround — post-F100 boss unavailable, stones route corruption).
            if (CurrentFloor >= 95 && Random.Shared.Next(100) < 10)
            {
                string stoneId = Random.Shared.Next(2) == 0
                    ? "night_corruption_stone" : "shadow_corruption_stone";
                var stoneDrop = Items.ItemRegistry.Create(stoneId);
                if (stoneDrop != null)
                {
                    _map.AddItem(fieldBoss.X, fieldBoss.Y, stoneDrop);
                    _log.LogLoot($"  ◆ {fieldBoss.Name} drops: {stoneDrop.Name}!");
                }
            }
        }
        else if (monster is Boss boss)
        {
            _log.LogSystem("====================================");
            _log.LogSystem($"  FLOOR BOSS DEFEATED: {boss.Name}!");
            _log.LogSystem("  The stairs to the next floor are now open!");
            _log.LogSystem("====================================");
            for (int rx = 0; rx < _map.Width; rx++)
            for (int ry = 0; ry < _map.Height; ry++)
                _map.SetExplored(rx, ry);
            _log.LogSystem("  The floor's layout is revealed on the minimap!");
            Story.StorySystem.TryFire(Story.StoryTrigger.BossDefeat,
                new Story.StoryContext(CurrentFloor, KillCount, _player, boss));

            // Guaranteed drop (Divine + P4 AL Divine Beast on non-canon bosses). DropItem formats Divine
            // with ◈; Bundle 8: ResolveFloorBossDropDefId substitutes a Legendary fallback if cap fired.
            var resolvedDropId = LootGenerator.ResolveFloorBossDropDefId(CurrentFloor);
            if (resolvedDropId != null)
            {
                var drop = Items.ItemRegistry.Create(resolvedDropId);
                if (drop != null)
                {
                    DropItem(boss.X, boss.Y, drop, boss.Name);
                    // Fire DivineObtained event + set cap when a Divine actually dropped.
                    if (drop is Items.Equipment.Weapon divineWpn && drop.Rarity == "Divine")
                        NotifyDivineObtained(divineWpn);
                }
            }

            // IM Last-Attack Bonus — F85/F92-F96/F98/F99 guaranteed non-enhanceable
            // Legendary on player's killing blow; stacks with FloorBossGuaranteedDrops.
            if (LootGenerator.FloorBossLastAttackDrops.TryGetValue(CurrentFloor, out var labDropId))
            {
                var labDrop = Items.ItemRegistry.Create(labDropId);
                if (labDrop != null)
                {
                    _map.AddItem(boss.X, boss.Y, labDrop);
                    _log.LogLoot($"  ◈ Last-Attack Bonus! {boss.Name} drops: {labDrop.Name}!");
                }
            }

            // Bundle 9: Divine Fragment — F75-F99 canon boss, ~5% drop (Divine Awakening Lv2 material).
            if (CurrentFloor >= 75 && CurrentFloor < 100 && Random.Shared.Next(100) < 5)
            {
                var frag = Items.ItemRegistry.Create("divine_fragment");
                if (frag != null)
                {
                    _map.AddItem(boss.X, boss.Y, frag);
                    _log.LogLoot($"  ◈ {boss.Name} drops: {frag.Name}!");
                }
            }

            // Bundle 9: F100 Primordial Shard — guaranteed one-per-run (Divine Awakening Lv3 material).
            // Bypasses FloorBossGuaranteedDrops to avoid the Divine-cap substitution path.
            if (CurrentFloor >= 100)
            {
                var shard = Items.ItemRegistry.Create("primordial_shard");
                if (shard != null)
                {
                    _map.AddItem(boss.X, boss.Y, shard);
                    _log.LogLoot($"  ◈ {boss.Name} drops: {shard.Name}!");
                }
            }

            // F50+ boss clear → next ShopTierSystem tier. Additive only.
            int tiersGained = ShopTierSystem.RegisterFloorBossClear(CurrentFloor, _log);
            if (tiersGained > 0) ToastQueue.EnqueueShopTier(tiersGained);
            FloorBossCleared?.Invoke(boss.Name);

            RollBossOreDrops(boss.X, boss.Y, boss.Name);

            // Boss-kill unique skills (Darkness Blade at Night, Blazing/Frozen by biome).
            var bossUnlock = Skills.UniqueSkillSystem.CheckBossKillUnlock(BiomeSystem.DisplayName);
            if (bossUnlock != null) NotifyUniqueSkillUnlock(bossUnlock.Value);
        }

        Story.StorySystem.TryFire(Story.StoryTrigger.KillCount,
            new Story.StoryContext(CurrentFloor, KillCount, _player, monster));

        DropLoot(monster);
        // FB-450 supplementary polygon ring — fires alongside the existing
        // shatter burst. Count scales via ParticleDensity.
        ParticleQueue.Emit(ParticleEvent.MonsterDeath, monster.X, monster.Y);
        MonsterKilled?.Invoke(monster.X, monster.Y);
        CleanupMobStatus(monster.Id);
        _map.RemoveEntity(monster);

        foreach (var ach in Achievements.CheckCombat(this, _player, monster))
        {
            _player.ColOnHand += ach.ColReward;
            TotalColEarned += ach.ColReward;
            _log.LogSystem($"  **ACHIEVEMENT: {ach.Name} — {ach.Description} (+{ach.ColReward} Col)");
            ToastQueue.EnqueueAchievement(ach.Name);
        }

        if (GetMonsterCount() == 0)
        {
            _log.LogSystem(FlavorText.FloorClearedMessages[Random.Shared.Next(FlavorText.FloorClearedMessages.Length)]);
            RevealStairs();
        }
    }

    private int CalcXpReward(Monster monster, Monster.DefeatReward reward)
    {
        int levelDiff = _player.Level - monster.Level;
        int xp = reward.Experience;
        if (levelDiff >= 5) xp /= 4;
        else if (levelDiff >= 3) xp /= 2;
        xp = xp * _diffTier.XpPercent / 100;
        // FB-564 Solo modifier +10% XP (compensation for no party).
        if (RunModifiers.IsActive(RunModifier.Solo)) xp = xp * 110 / 100;
        return Math.Max(1, xp);
    }

    private void HandleKillMilestone()
    {
        (int threshold, int bonus, string msg) milestone = KillCount switch
        {
            25  => (25,  100,  "25 monsters felled! Word spreads of your deeds."),
            50  => (50,  250,  "50 kills! The clearing guilds take notice."),
            100 => (100, 500,  "100 monsters slain! You are a force to be reckoned with."),
            250 => (250, 1000, "250 kills! Your name echoes through Aincrad."),
            500 => (500, 2500, "500 kills! A legend walks these halls."),
            _   => (0, 0, "")
        };
        if (milestone.threshold > 0)
        {
            _player.ColOnHand += milestone.bonus;
            TotalColEarned += milestone.bonus;
            _log.LogSystem($"  **MILESTONE: {milestone.msg} +{milestone.bonus} Col!");
        }
    }

    private void HandleKillStreakCallout()
    {
        string? streakMsg = _killStreak switch
        {
            2 => "Double Kill!", 3 => "Triple Kill!", 4 => "Quad Kill!",
            5 => "Rampage! (Col bonus active)", 10 => "GODLIKE! (Col bonus x2)",
            15 => "LEGENDARY! (Col bonus x3)", >= 6 => $"UNSTOPPABLE! ({_killStreak} streak)",
            _ => null
        };
        if (streakMsg != null) _log.LogCombat($"*** {streakMsg} ***");
    }

    // Signature swing color per weapon type.
    private static Color GetSwingColor(Weapon? wpn, bool crit)
    {
        if (crit) return Color.BrightCyan;
        if (wpn == null) return Color.Gray;
        return wpn.WeaponType switch
        {
            "One-Handed Sword" => Color.BrightYellow,
            "Two-Handed Sword" => Color.BrightRed,
            "Rapier"           => Color.BrightCyan,
            "Dagger"           => Color.BrightGreen,
            "Katana"           => Color.BrightMagenta,
            "Axe"              => Color.BrightRed,
            "Mace"             => Color.White,
            "Spear"            => Color.BrightCyan,
            "Bow"              => Color.BrightYellow,
            "Scimitar"         => Color.Yellow,
            "Claws"            => Color.BrightRed,
            "Scythe"           => Color.BrightMagenta,
            _                  => Color.White,
        };
    }

    private void NotifyUniqueSkillUnlock(Skills.UniqueSkill skill)
    {
        var def = Skills.UniqueSkillSystem.Definitions[skill];
        _log.LogSystem("══════════════════════════════════════");
        _log.LogSystem($"  ★ UNIQUE SKILL AWAKENED: {def.Name}");
        _log.LogSystem($"    {def.Description}");
        _log.LogSystem("══════════════════════════════════════");
        CombatTextEvent?.Invoke(_player.X, _player.Y, def.Name.ToUpper(), def.DisplayColor);
    }

    private void ShowMonsterCondition(Monster monster)
    {
        int pct = (int)Math.Round(100.0 * monster.CurrentHealth / monster.MaxHealth);
        string hpBar = BuildMobHpBar(monster.CurrentHealth, monster.MaxHealth, 12);
        string condition = pct > 75 ? "healthy" : pct > 50 ? "wounded" : pct > 25 ? "badly hurt" : "near death";
        _log.LogCombat($"  {monster.Name} {hpBar} {monster.CurrentHealth}/{monster.MaxHealth} ({condition})");
        if (pct <= 10) _log.LogCombat($"  >> {monster.Name} staggers! One more hit! <<");
        else if (pct <= 25) _log.LogCombat($"  {monster.Name} is barely standing...");
    }
}
