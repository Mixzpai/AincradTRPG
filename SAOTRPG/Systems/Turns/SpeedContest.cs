namespace SAOTRPG.Systems.Turns;

// What Speed does. One source of truth for the number, so the combat roll, the character sheet
// and the Player Guide cannot drift apart.
//
// The rule: the faster combatant presses the advantage. When an attacker's Speed exceeds the
// defender's, each melee exchange carries a chance of one extra strike at reduced damage. The
// follow-up never chains — a follow-up cannot roll a follow-up of its own.
//
// Sizing:
//   * a mob's BaseSpeed is 3 + floor (+2..+5 with Swift or Ethereal), while a player's Speed is
//     investment-driven and grows far larger. A ratio between the two would saturate as soon as
//     the player invests at all and make every Speed item indistinguishable, so the advantage is
//     an absolute difference fed through a saturating curve: every extra point still moves the
//     number and no amount of investment reaches the ceiling.
//   * the ceiling matches the 20% extra auto-attack on a rapier's ExtraAutoAttack tag.
//   * the follow-up's damage share matches the dual-wield off-hand swing (60%).
//   * HalfAdvantage is a tuning constant. The curve must be non-zero at the smallest realistic
//     investment, strictly increasing, and short of the ceiling at the largest advantage a run
//     can reach.
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
    // Rounded, not truncated: early-game advantages are small (a level-1 character with every
    // starting point in Agility outpaces a floor-1 mob by 18), and truncating integer division
    // would turn an advantage of 6 into zero.
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
