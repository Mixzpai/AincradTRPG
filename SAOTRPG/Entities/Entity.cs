using SAOTRPG.UI;

namespace SAOTRPG.Entities
{
    public abstract class Entity
    {
        /****************************************************************************************/
        // General Entity Details
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        
        public int Level { get; internal set; } = 1;
        public int CurrentHealth { get; internal set; }
        public int MaxHealth { get; set; }

        /****************************************************************************************/
        // Base Stats
        public int BaseAttack { get; internal set; }
        public int BaseDefense { get; internal set; }
        public int BaseSpeed { get; internal set; }

        /****************************************************************************************/
        // Entity Stats
        public int Strength { get; internal set; }
        public int Vitality { get; internal set; }
        public int Endurance { get; internal set; }
        public int Dexterity { get; internal set; }
        public int Agility { get; internal set; }
        public int Intelligence { get; internal set; }

        /****************************************************************************************/
        // State
        public bool IsDefeated { get; protected set; }

        /****************************************************************************************/
        // Game Log
        protected IGameLog? _log;

        public void SetLog(IGameLog log) => _log = log;
    }
}