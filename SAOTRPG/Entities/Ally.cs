using Terminal.Gui;

namespace SAOTRPG.Entities;

// Party member ally -- fights alongside the player using simple AI.
// Follows the player, attacks nearby enemies, and uses the SAO Switch mechanic.
// Each ally has a weapon type that determines their combat style.
public class Ally : Entity
{
    public override char Symbol { get; protected set; }
    public override Color SymbolColor { get; protected set; }

    public string WeaponType { get; set; } = "One-Handed Sword";
    public string Title { get; set; } = "";

    // AI behavior mode.
    public AllyBehavior Behavior { get; set; } = AllyBehavior.Aggressive;

    public int AttackDamage => Math.Max(1, BaseAttack + Strength * 2 + Level);

    public const int FollowDistance = 3;
    public const int AggroRange = 5;

    public Ally(char symbol, Color color)
    {
        Symbol = symbol;
        SymbolColor = color;
    }

    // Reduce HP and mark defeated at 0. Returns true if KO'd.
    public bool TakeDamage(int damage)
    {
        CurrentHealth = Math.Max(0, CurrentHealth - damage);
        if (CurrentHealth <= 0) IsDefeated = true;
        return IsDefeated;
    }

    // Revive with partial HP (used between floors).
    public void Revive(int hp)
    {
        IsDefeated = false;
        CurrentHealth = Math.Clamp(hp, 1, MaxHealth);
    }
}

public enum AllyBehavior
{
    Aggressive, // Attack nearest enemy in range.
    Defensive,  // Only attack enemies adjacent to player.
    Follow,     // Stay near player, don't initiate combat.
}
