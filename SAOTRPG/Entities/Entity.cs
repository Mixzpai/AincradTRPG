using Terminal.Gui;
using SAOTRPG.UI;

namespace SAOTRPG.Entities
{
    // Base for players/monsters/NPCs/bosses — position, stats, HP, rendering.
    public abstract class Entity : IStatModifiable
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public int X { get; set; }
        public int Y { get; set; }
        public virtual char Symbol { get; protected set; } = '?';
        public virtual Color SymbolColor { get; protected set; } = Color.White;

        public int Level { get; set; } = 1;
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }

        // Base stats (pre-equipment/buffs). Attributes: STR +2 ATK, VIT +10 HP, END +2 DEF, AGI +2 SPD, INT +2 SD.
        public int BaseAttack { get; set; }
        public int BaseDefense { get; set; }
        public int BaseSpeed { get; set; }
        public int BaseSkillDamage { get; set; }
        public int BaseCriticalRate { get; set; } = 5;
        public int BaseCriticalHitDamage { get; set; } = 10;

        // Derived crit: rate = Base + DEX/2, damage = Base + DEX.
        public int CriticalRate => BaseCriticalRate + (Dexterity / 2);
        public int CriticalHitDamage => BaseCriticalHitDamage + Dexterity;

        public int Strength { get; set; }
        public int Vitality { get; set; }
        public int Endurance { get; set; }
        public int Dexterity { get; set; }
        public int Agility { get; set; }
        public int Intelligence { get; set; }

        public bool IsDefeated { get; protected set; }

        protected IGameLog? _log;
        public void SetLog(IGameLog log) => _log = log;
    }
}
