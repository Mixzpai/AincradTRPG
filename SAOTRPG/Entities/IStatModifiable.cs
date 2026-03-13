namespace SAOTRPG.Entities;

// Decouples stat modification from Player — lets equipment/consumables modify any entity
// Implemented by: Entity (base class for Player, Monster, NPC)
public interface IStatModifiable
{
    int Level { get; }

    // Core combat stats — modified by equipment and consumables
    int CurrentHealth { get; set; }
    int BaseAttack { get; set; }
    int BaseDefense { get; set; }
    int BaseSpeed { get; set; }
    int BaseSkillDamage { get; set; }

    // Attribute stats — used for scaling and stat point allocation
    int Strength { get; set; }
    int Vitality { get; set; }
    int Endurance { get; set; }
    int Dexterity { get; set; }
    int Agility { get; set; }
    int Intelligence { get; set; }
}
