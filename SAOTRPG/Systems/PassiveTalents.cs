namespace SAOTRPG.Systems;

// Passive talent system — on level-up, pick 1 of 3 random flat-stat perks.
public static class PassiveTalents
{
    public record Perk(string Id, string Name, string Description, Action<Entities.Player> Apply);

    private static readonly Perk[] AllPerks =
    {
        new("crit5",     "Keen Edge",       "+3% Crit Rate",            p => p.BaseCriticalRate += 3),
        new("critdmg10", "Brutal Strikes",  "+5 Crit Damage",          p => p.BaseCriticalHitDamage += 5),
        new("hp20",      "Iron Will",       "+2 Vitality",             p => p.Vitality += 2),
        new("atk3",      "Power Surge",     "+3 Base ATK",             p => p.BaseAttack += 3),
        new("def3",      "Fortify",         "+3 Base DEF",             p => p.BaseDefense += 3),
        new("spd2",      "Quick Step",      "+2 Base SPD",             p => p.BaseSpeed += 2),
        new("str2",      "Brute Force",     "+2 Strength",             p => p.Strength += 2),
        new("end2",      "Endurance",       "+2 Endurance",            p => p.Endurance += 2),
        new("dex2",      "Nimble Fingers",  "+2 Dexterity",            p => p.Dexterity += 2),
        new("agi2",      "Fleet Foot",      "+2 Agility",              p => p.Agility += 2),
        new("int2",      "Sharp Mind",      "+2 Intelligence",         p => p.Intelligence += 2),
    };

    // Returns 3 random unique perks for the player to choose from.
    public static Perk[] RollChoices(int count = 3)
    {
        var shuffled = AllPerks.OrderBy(_ => Random.Shared.Next()).Take(count).ToArray();
        return shuffled;
    }
}
