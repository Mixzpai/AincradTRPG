namespace SAOTRPG.Entities
{
    public class Boss : Monster
    {
        /****************************************************************************************/
        // Boss-Specific Properties
        public string BossTitle { get; protected set; }
        public int Phase { get; protected set; } = 1;
        public int MaxPhases { get; protected set; } = 1;
    }
}