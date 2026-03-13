using SAOTRPG.UI;

public abstract class Entity
{
    /****************************************************************************************/
    // General Entity Details
    public int Id { get; protected set; }
    public string Name { get; protected set; }
    public int Level { get; protected set; }

    /****************************************************************************************/
    // True Stats
    public int MaxHealth => Vitality * 10;
    public int CurrentHealth { get; protected set; }
    public int Attack => BaseAttack + (Strength * 2);
    public int Defense => BaseDefense + (Endurance * 2);
    public int CriticalRate => BaseCriticalRate + (Dexterity * 2);
    public double CriticalHitDamage => BaseCriticalHitDamage + (Attack * 1.5);
    public int Speed => BaseSpeed + (Agility * 2);
    public int SkillDamage => BaseSkillDamage + (Intelligence * 2);

    /****************************************************************************************/
    // Base Stats
    public int BaseAttack { get; protected set; }
    public int BaseDefense { get; protected set; }
    public int BaseCriticalRate { get; protected set; }
    public int BaseCriticalHitDamage { get; protected set; }
    public int BaseSpeed { get; protected set; }
    public int BaseSkillDamage { get; protected set; }

    /****************************************************************************************/
    // Entity Stats
    public int Vitality { get; protected set; }
    public int Strength { get; protected set; }
    public int Endurance { get; protected set; }
    public int Dexterity { get; protected set; }
    public int Agility { get; protected set; }
    public int Intelligence { get; protected set; }

    /****************************************************************************************/
    // State
    public bool IsDefeated { get; protected set; }

    /****************************************************************************************/
    // Game Log
    protected IGameLog? _log;

    public void SetLog(IGameLog log) => _log = log;
}