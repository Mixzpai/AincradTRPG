namespace SAOTRPG.Items.Equipment;

// Bundle 10 (B12) — typed special effect registry. Discriminated record set; each variant
// carries exactly the fields its consumer reads. The string field on EquipmentBase remains
// the save-format authority; this is the parsed view (built lazily on first access).
public abstract record EquipmentSpecialEffect
{
    public abstract string Key { get; }

    // ── Combat damage modifiers (weapon-only consumers in TurnManager.Combat.cs)
    public sealed record HolyDamage(int Percent)        : EquipmentSpecialEffect { public override string Key => "HolyDamage"; }
    public sealed record FrostDamage(int Percent)       : EquipmentSpecialEffect { public override string Key => "FrostDamage"; }
    public sealed record NightDamage(int Percent)       : EquipmentSpecialEffect { public override string Key => "NightDamage"; }
    public sealed record DragonSlayer(int Percent)      : EquipmentSpecialEffect { public override string Key => "DragonSlayer"; }
    public sealed record ArmorPierce(int Percent)       : EquipmentSpecialEffect { public override string Key => "ArmorPierce"; }
    public sealed record PiercingShot(int Percent)      : EquipmentSpecialEffect { public override string Key => "PiercingShot"; }
    public sealed record TrueStrike(int ChancePercent)  : EquipmentSpecialEffect { public override string Key => "TrueStrike"; }
    public sealed record BackstabDmg(int BonusPercent)  : EquipmentSpecialEffect { public override string Key => "BackstabDmg"; }
    public sealed record ExecuteThreshold(int HpPercent): EquipmentSpecialEffect { public override string Key => "ExecuteThreshold"; }
    public sealed record ThrustDmg(int Percent)         : EquipmentSpecialEffect { public override string Key => "ThrustDmg"; }
    public sealed record ComboBonus(int Percent)        : EquipmentSpecialEffect { public override string Key => "ComboBonus"; }
    public sealed record Cleave(int SplashPercent)      : EquipmentSpecialEffect { public override string Key => "Cleave"; }

    // ── On-hit status applies (weapon-only — flavor canon: armor with on-hit-effect would be off-flavor)
    public sealed record BleedOnHit(int ChancePercent)  : EquipmentSpecialEffect { public override string Key => "Bleed"; }
    public sealed record StunOnHit(int ChancePercent)   : EquipmentSpecialEffect { public override string Key => "Stun"; }
    public sealed record PoisonOnHit(int ChancePercent) : EquipmentSpecialEffect { public override string Key => "Poison"; }
    public sealed record BlindOnHit(int ChancePercent)  : EquipmentSpecialEffect { public override string Key => "BlindOnHit"; }
    public sealed record LunacyOnHit(int ChancePercent) : EquipmentSpecialEffect { public override string Key => "Lunacy"; }
    public sealed record SlowOnHit(int ChancePercent)   : EquipmentSpecialEffect { public override string Key => "SlowOnHit"; }   // Bundle 10 (B11) — new consumer

    // ── Crit-triggered modifiers (weapon-only)
    public sealed record CritHeal(int Percent)          : EquipmentSpecialEffect { public override string Key => "CritHeal"; }
    public sealed record Invisibility(int Turns)        : EquipmentSpecialEffect { public override string Key => "Invisibility"; }
    public sealed record CritRateBonus(int Percent)     : EquipmentSpecialEffect { public override string Key => "CritRate"; }

    // ── Defensive (additive across weapon + shield slots)
    public sealed record BlockChanceBonus(int Percent)  : EquipmentSpecialEffect { public override string Key => "BlockChance"; }
    public sealed record ParryChance(int Percent)       : EquipmentSpecialEffect { public override string Key => "ParryChance"; }
    public sealed record EvadeRegen(int Percent)        : EquipmentSpecialEffect { public override string Key => "EvadeRegen"; }
    public sealed record Uninterruptible(int ChancePercent) : EquipmentSpecialEffect { public override string Key => "Uninterruptible"; }
    public sealed record DamageReflect(int Percent)     : EquipmentSpecialEffect { public override string Key => "DamageReflect"; }
    public sealed record CritImmune(int ChancePercent)  : EquipmentSpecialEffect { public override string Key => "CritImmune"; }
    public sealed record HPRegen(int Amount)            : EquipmentSpecialEffect { public override string Key => "HPRegen"; }
    public sealed record Barrier(int Magnitude)         : EquipmentSpecialEffect { public override string Key => "Barrier"; }

    // ── Skill modifiers (weapon-only)
    public sealed record SkillCooldownReduction(int Turns) : EquipmentSpecialEffect { public override string Key => "SkillCooldown"; }
    public sealed record PostMotionReduction(int Turns) : EquipmentSpecialEffect { public override string Key => "PostMotion"; }
    public sealed record AttackSpeedBonus(int Levels)   : EquipmentSpecialEffect { public override string Key => "AttackSpeed"; }
    public sealed record SPRegen(int Amount)            : EquipmentSpecialEffect { public override string Key => "SPRegen"; }

    // ── Pre-existing-but-unconsumed (parsed, no-op consumer — flagged at registry build time)
    public sealed record DarknessRending(int Percent)   : EquipmentSpecialEffect { public override string Key => "DarknessRending"; }

    // Tries to parse a single "Key+N" / "Key-N" / "KeyN" token (no embedded multiple-pairs).
    // Use EquipmentSpecialEffectRegistry.GetParsed for full strings (multi-pair).
    public static bool TryParse(string raw, out EquipmentSpecialEffect? effect)
    {
        effect = null;
        if (string.IsNullOrEmpty(raw)) return false;
        int i = 0;
        while (i < raw.Length && !char.IsLetter(raw[i])) i++;
        int keyStart = i;
        while (i < raw.Length && char.IsLetter(raw[i])) i++;
        if (i == keyStart) return false;
        string key = raw.Substring(keyStart, i - keyStart);
        int numStart = i;
        while (i < raw.Length)
        {
            char c = raw[i];
            if (c == '+' || c == '-' || char.IsDigit(c)) i++; else break;
        }
        if (i == numStart || !int.TryParse(raw.AsSpan(numStart, i - numStart), out int val)) return false;
        effect = Build(key, val);
        return effect != null;
    }

    // Constructs the typed record for a known key. Returns null for unrecognised keys
    // (caller logs a debug warning; raw int still accessible via SwordSkillEngine.GetSpecialEffectValue).
    internal static EquipmentSpecialEffect? Build(string key, int value) => key switch
    {
        "HolyDamage"       => new HolyDamage(value),
        "FrostDamage"      => new FrostDamage(value),
        "NightDamage"      => new NightDamage(value),
        "DragonSlayer"     => new DragonSlayer(value),
        "ArmorPierce"      => new ArmorPierce(value),
        "PiercingShot"     => new PiercingShot(value),
        "TrueStrike"       => new TrueStrike(value),
        "BackstabDmg"      => new BackstabDmg(value),
        "ExecuteThreshold" => new ExecuteThreshold(value),
        "ThrustDmg"        => new ThrustDmg(value),
        "ComboBonus"       => new ComboBonus(value),
        "Cleave"           => new Cleave(value),
        "Bleed"            => new BleedOnHit(value),
        "Stun"             => new StunOnHit(value),
        "Poison"           => new PoisonOnHit(value),
        "BlindOnHit"       => new BlindOnHit(value),
        "Lunacy"           => new LunacyOnHit(value),
        "SlowOnHit"        => new SlowOnHit(value),
        "CritHeal"         => new CritHeal(value),
        "Invisibility"     => new Invisibility(value),
        "CritRate"         => new CritRateBonus(value),
        "BlockChance"      => new BlockChanceBonus(value),
        "ParryChance"      => new ParryChance(value),
        "EvadeRegen"       => new EvadeRegen(value),
        "Uninterruptible"  => new Uninterruptible(value),
        "DamageReflect"    => new DamageReflect(value),
        "CritImmune"       => new CritImmune(value),
        "HPRegen"          => new HPRegen(value),
        "Barrier"          => new Barrier(value),
        "SkillCooldown"    => new SkillCooldownReduction(value),
        "PostMotion"       => new PostMotionReduction(value),
        "AttackSpeed"      => new AttackSpeedBonus(value),
        "SPRegen"          => new SPRegen(value),
        "DarknessRending"  => new DarknessRending(value),
        _                  => null,
    };
}
