using Terminal.Gui;

namespace SAOTRPG.Entities
{
    public class Boss : Monster
    {
        public override char Symbol { get; protected set; } = 'B';
        public override Color SymbolColor { get; protected set; } = Color.BrightRed;
        /****************************************************************************************/
        // Boss-Specific Properties
        public string BossTitle { get; protected set; }
        public int Phase { get; protected set; } = 1;
        public int MaxPhases { get; protected set; } = 1;
    }
}