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

    // Bundle 10 (B13) — derived from CritRate / HPRegen / SkillCooldown StatType entries.
    // Mob/Monster fall back to default 0 implementation; Player owns concrete state on Entity.
    int BaseCriticalRate { get; set; }
    int BaseHpRegenPerTick { get; set; }
    int SkillCooldownReduction { get; set; }
}
