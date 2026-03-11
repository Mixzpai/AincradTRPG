using System;

public class BossMonster : Monster
{
    public BossMonster()
    {
        Id = 7;
        Name = "Illfang the Kobald Lord";
        Level = 15;
        Vitality = 8;
        Strength = 6;
        Endurance = 5;
        Dexterity = 4;
        Agility = 3;
        Intelligence = 2;
        BaseAttack = 10;
        BaseCriticalRate = 5;
        BaseCriticalHitDamage = 10;
        BaseDefense = 8;
        BaseSpeed = 6;
        BaseSkillDamage = 4;
        ExperienceYield = 1000;
        ColYield = 5000;
        CurrentHealth = MaxHealth;
    }
}
