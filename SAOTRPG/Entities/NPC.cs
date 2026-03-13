namespace SAOTRPG.Entities
{
    public abstract class NPC : Entity
    {
        /****************************************************************************************/
        // NPC-Specific Properties
        public string Dialogue { get; protected set; }
        public bool CanInteract { get; protected set; } = true;
    }
}