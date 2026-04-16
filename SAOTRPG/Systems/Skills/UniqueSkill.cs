using Terminal.Gui;

namespace SAOTRPG.Systems.Skills;

// Canon Cardinal-assigned rare skills. In canon one per player; here every
// skill is unlockable across playthroughs via a different canon-faithful path.
public enum UniqueSkill
{
    DualBlades,      // Kirito: two 1H swords, +1 attack/turn, unlocks Starburst Stream
    HolySword,       // Heathcliff: 1H + shield, +15% block, reflect counter
    MartialArts,     // Progressive F2 quest: no weapon, +20% crit +30% speed
    KatanaMastery,   // 100 katana kills: +10% crit, bleed on hit
    DarknessBlade,   // Kill a boss during Night: +20% dmg / +10% dodge at night
    BlazingEdge,     // Kill Volcanic-biome boss: 10% burn proc, +25% vs Ice
    FrozenEdge,      // Kill Ice-biome boss: 10% slow proc, +25% vs Fire
    ExtraSearch,     // Scout Argo quest chain: reveals hidden traps, +1 loot tier
}

public record UniqueSkillDef(
    UniqueSkill Id,
    string Name,
    string Description,
    string UnlockHint,
    Color DisplayColor,
    string[] GrantedSkillIds  // OSS skill IDs that appear when this is unlocked
);
