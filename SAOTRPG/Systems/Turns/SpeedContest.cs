namespace SAOTRPG.Systems.Turns;

// What Speed DOES. One source of truth for the number, so the combat roll, the character sheet
// and the Player Guide cannot drift apart.
//
// The rule: the faster combatant presses the advantage. When an attacker's Speed exceeds the
// defender's, each melee exchange carries a chance of one extra strike at reduced damage. The
// follow-up never chains — a follow-up cannot roll a follow-up of its own.
//
// SIZING, from the measured distributions rather than from feel:
//   * a mob's BaseSpeed is 3 + floor exactly (+2..+5 on the ~13% that carry Swift or Ethereal),
//     while a player's Speed is investment-driven and spans 0 to roughly 1,000 across a run.
//     A RATIO between the two therefore saturates the moment the player invests at all: every
//     archetype but "no Agility, no Speed gear" pins at any cap from about floor 5 onward, which
//     makes the 68 item definitions granting +2..+26 Speed indistinguishable from one another.
//     The advantage is used as an ABSOLUTE difference fed through a saturating curve instead, so
//     every extra point still moves the number and no amount of investment reaches the ceiling.
//   * the ceiling is DERIVED, not chosen: the game already advertises an extra auto-attack at
//     20% on a rapier's ExtraAutoAttack tag, so 20% is the rate this game has already decided an
//     extra swing is worth.
//   * the follow-up's damage share matches the dual-wield off-hand swing (60%), for the same
//     reason — the codebase has already priced a second swing in one exchange.
//   * HalfAdvantage is a tuning constant and is labelled as one. What is checked rather than
//     asserted is the SHAPE it has to produce: non-zero at the smallest realistic investment,
//     strictly increasing, and short of the ceiling at the largest advantage a run can reach.
public static class SpeedContest
{
    // Asymptote. Approached, never reached — see FollowUpChance.
    public const int FollowUpCeilingPercent = 20;
    // Speed advantage at which the chance is half the ceiling.
    public const int HalfAdvantage = 150;
    // Damage the follow-up strike carries, as a percent of the blow that earned it.
    public const int FollowUpDamagePercent = 60;
    // Total chance cap once an ExtraAutoAttack tag is added on top.
    public const int FollowUpHardCapPercent = 50;

    // How far the attacker outpaces the defender. Never negative: being slower does not hand the
    // defender anything here, it simply buys the attacker nothing.
    public static int Advantage(int attackerSpeed, int defenderSpeed)
        => Math.Max(0, attackerSpeed - defenderSpeed);

    // Percent chance of one extra strike this exchange.
    //
    // ROUNDED, NOT TRUNCATED, and that is load-bearing. The advantages the early game actually
    // produces are small — a level-1 character with every starting point in Agility outpaces a
    // floor-1 mob by 18 — and integer division floors 18 to 2% but floors an advantage of 6 to
    // zero, which would exempt the whole opening act while a large-value test reported the knob
    // live.
    public static int FollowUpChance(int attackerSpeed, int defenderSpeed)
    {
        int advantage = Advantage(attackerSpeed, defenderSpeed);
        if (advantage <= 0) return 0;
        long denominator = advantage + (long)HalfAdvantage;
        return (int)((FollowUpCeilingPercent * (long)advantage + denominator / 2) / denominator);
    }

    // Damage the follow-up carries, floored at 1 so a landed strike is never a cosmetic one.
    public static int FollowUpDamage(int strikeDamage)
        => Math.Max(1, strikeDamage * FollowUpDamagePercent / 100);
}
