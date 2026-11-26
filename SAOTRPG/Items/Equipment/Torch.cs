namespace SAOTRPG.Items.Equipment;

// Tool-slot light source. The player emits no light of their own — carrying a lit torch is the
// only way to see past the ambient sky, so this competes with the Pickaxe for the Tool slot:
// you mine or you carry a light, not both.
//
// It shares the OffHand slot with nothing on purpose. A torch in OffHand would displace a shield,
// which silently disables TryShieldBlock and the Holy Sword unique skill's block passive — a
// player would lose a Unique Skill by picking up a light.
public class Torch : EquipmentBase
{

    public Torch()
    {
        EquipmentType = "torch";
        // Torches grant no combat stats — Bonuses left empty by default.
    }
}
