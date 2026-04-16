using Terminal.Gui;
using SAOTRPG.UI;

namespace SAOTRPG.Entities
{
    // Base class for all game entities — players, monsters, NPCs, bosses.
    // Holds position, stats, health, and rendering properties.
    public abstract class Entity : IStatModifiable
    {
        // Unique identifier for this entity instance.
        public int Id { get; set; }
        // Display name shown in combat log and UI.
        public string Name { get; set; } = string.Empty;

        // Map Position & Rendering

        // Horizontal tile position on the current floor map.
        public int X { get; set; }
        // Vertical tile position on the current floor map.
        public int Y { get; set; }
        // ASCII character drawn on the map (e.g. '@' for player, 'k' for kobold).
        public virtual char Symbol { get; protected set; } = '?';
        // Foreground color for the map symbol.
        public virtual Color SymbolColor { get; protected set; } = Color.White;

        // Current level — affects stat scaling and displayed threat.
        public int Level { get; set; } = 1;
        // Current hit points. Entity dies when this reaches 0.
        public int CurrentHealth { get; set; }
        // Maximum hit points — determines the HP bar cap.
        public int MaxHealth { get; set; }

        // Base Stats

        // Base physical attack power before equipment and buffs.
        public int BaseAttack { get; set; }
        // Base physical damage reduction before equipment and buffs.
        public int BaseDefense { get; set; }
        // Base speed — affects turn priority and dodge chance.
        public int BaseSpeed { get; set; }
        // Base skill damage bonus (Intelligence-scaling attacks).
        public int BaseSkillDamage { get; set; }
        // Base critical hit chance (%). Default 5%.
        public int BaseCriticalRate { get; set; } = 5;
        // Base critical hit bonus damage (%). Default 10%.
        public int BaseCriticalHitDamage { get; set; } = 10;

        // Derived Combat Stats

        // Effective crit rate: BaseCriticalRate + Dexterity/2.
        public int CriticalRate => BaseCriticalRate + (Dexterity / 2);
        // Effective crit damage bonus: BaseCriticalHitDamage + Dexterity.
        public int CriticalHitDamage => BaseCriticalHitDamage + Dexterity;

        // Attributes

        // Strength — each point adds +2 ATK.
        public int Strength { get; set; }
        // Vitality — each point adds +10 Max HP.
        public int Vitality { get; set; }
        // Endurance — each point adds +2 DEF.
        public int Endurance { get; set; }
        // Dexterity — adds crit rate and accuracy.
        public int Dexterity { get; set; }
        // Agility — each point adds +2 SPD and dodge.
        public int Agility { get; set; }
        // Intelligence — each point adds +2 skill damage.
        public int Intelligence { get; set; }

        // True after the entity's health reaches 0.
        public bool IsDefeated { get; protected set; }

        // Reference to the game log for combat/event messages.
        protected IGameLog? _log;

        // Attach a game log instance for this entity to write messages to.
        public void SetLog(IGameLog log) => _log = log;
    }
}
