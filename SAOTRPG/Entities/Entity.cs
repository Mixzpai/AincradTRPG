using SAOTRPG.UI;
using SAOTRPG.Items;

namespace SAOTRPG.Entities
{
    public abstract class Entity : IStatModifiable
    {
        /****************************************************************************************/
        // General Entity Details
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public int Level { get; set; } = 1;
        public int CurrentHealth { get; set; }
        public virtual int MaxHealth { get; set; }

        /****************************************************************************************/
        // Base Stats
        public int BaseAttack { get; set; }
        public int BaseDefense { get; set; }
        public int BaseSpeed { get; set; }
        public int BaseSkillDamage { get; set; }
        public int BaseCriticalRate { get; set; } = 5;
        public int BaseCriticalHitDamage { get; set; } = 10;

        /****************************************************************************************/
        // Derived Combat Stats
        public virtual int CriticalRate => BaseCriticalRate + (Dexterity / 2);
        public virtual int CriticalHitDamage => BaseCriticalHitDamage + Dexterity;

        /****************************************************************************************/
        // Entity Stats
        public virtual int Strength { get; set; }
        public virtual int Vitality { get; set; }
        public virtual int Endurance { get; set; }
        public virtual int Dexterity { get; set; }
        public virtual int Agility { get; set; }
        public virtual int Intelligence { get; set; }

        /****************************************************************************************/
        // State
        public bool IsDefeated { get; protected set; }

        /****************************************************************************************/
        // Game Log
        protected IGameLog? _log;

        public void SetLog(IGameLog log) => _log = log;
    }
}