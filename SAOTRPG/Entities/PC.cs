namespace SAOTRPG.Entities
{
    public class PC : NPC
    {
        /****************************************************************************************/
        // Player Character (Recruitable) Properties
        public bool IsRecruitable { get; protected set; }
        public bool IsRecruited { get; protected set; }
    }
}