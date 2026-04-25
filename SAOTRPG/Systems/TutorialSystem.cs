using SAOTRPG.UI;

namespace SAOTRPG.Systems;

// Contextual tutorial system — shows first-time tips triggered by game events.
// Each tip fires ONCE per save file and is logged with a [TIP] prefix.
public static class TutorialSystem
{
    // Tips already shown this session (persisted via SaveData).
    public static HashSet<string> SeenTips { get; set; } = new();

    // Show a tip if it hasn't been seen before. Returns true if shown.
    public static bool ShowTip(IGameLog log, string tipId)
    {
        if (!SeenTips.Add(tipId)) return false;
        var tip = GetTip(tipId);
        if (tip == null) return false;
        log.LogSystem($"  [TIP] {tip}");
        return true;
    }

    // F9 hot-reload latch reset — drops per-floor tip ids (e.g. "floor1_*").
    public static void ClearPerFloorLatches()
    {
        int before = SeenTips.Count;
        SeenTips.RemoveWhere(id => id.StartsWith("floor"));
        DebugLogger.LogGame("RELOAD", $"TutorialSystem.ClearPerFloorLatches removed {before - SeenTips.Count}");
    }

    // All tutorial tips keyed by event ID.
    private static string? GetTip(string id) => id switch
    {
        // ── Movement & exploration ───────────────────────────────────
        "first_move" =>
            "Use WASD or arrow keys to move. Q/E/Z/C for diagonals. Press H for full controls.",

        "first_wall_bump" =>
            "You bumped into a wall. Walls block movement — look for paths and doors.",

        "first_door" =>
            "Walk into doors to open them. Explore rooms for loot and enemies.",

        "first_fog" =>
            "Grey tiles are areas you've explored but can't currently see. Your vision is limited by line of sight.",

        "first_stairs" =>
            "You found the stairs! Defeat the floor boss in the Labyrinth before you can ascend.",

        "first_labyrinth" =>
            "This is the Labyrinth entrance. Inside is a dangerous dungeon with the floor boss. Prepare well before entering!",

        // ── Combat ───────────────────────────────────────────────────
        "first_combat" =>
            "Walk into enemies to attack! Your weapon type and stats determine damage. Watch your HP!",

        "first_kill" =>
            "Enemy defeated! You earn XP and Col. Kill enough with one weapon type to unlock Sword Skills.",

        "first_damage_taken" =>
            "You took damage! Press R to rest (heals 3 HP/turn, costs 3 turns). Use potions with keys 1-5.",

        "first_low_hp" =>
            "Your HP is critically low! Use a Health Potion (key 1) or retreat and rest (R).",

        "first_crit" =>
            "Critical hit! Dexterity increases your crit rate. Crits deal bonus damage based on your crit stat.",

        "first_combo" =>
            "Combo! Hit the same enemy repeatedly for increasing bonus damage. 5 hits = finisher (x2 damage)!",

        "first_backstab" =>
            "Backstab! Attacking an unaware enemy deals double damage. Stealth move (Ctrl+direction) halves detection range.",

        "first_dodge" =>
            "You dodged an attack! Agility increases dodge chance. The Slow debuff halves it.",

        "first_status_poison" =>
            "You've been poisoned! Use an Antidote (key 3) to cure it, or wait for it to wear off.",

        // ── Sword Skills ─────────────────────────────────────────────
        "first_skill_unlock" =>
            "Sword Skill unlocked! Press F to open the Skill menu and equip skills to F1-F4.",

        "first_skill_use" =>
            "Sword Skills deal multiplied damage but have cooldowns. Powerful skills also have post-motion delay — you take extra damage briefly after using them.",

        "first_post_motion" =>
            "Post-motion delay! You're vulnerable for a few turns. Enemies deal +50% damage to you during this window.",

        // ── Items & inventory ────────────────────────────────────────
        "first_item_ground" =>
            "There are items on the ground! Press G to pick them up, or enable auto-pickup in Options.",

        "first_chest" =>
            "A treasure chest! Walk onto it to open. Chests contain equipment, consumables, or Col.",

        "first_inventory_open" =>
            "This is your inventory. Press Enter on an item to Use, Equip, or Drop it. Tab switches to equipment view.",

        "first_equipment_change" =>
            "New equipment equipped! Check the sidebar to see your updated stats. Better gear = stronger character.",

        "first_vendor" =>
            "A shop! Browse the vendor's wares. Selling loot you don't need is a good source of Col.",

        // ── World & NPCs ─────────────────────────────────────────────
        "first_npc_talk" =>
            "NPCs share tips and lore. Walk into them to talk — they'll step aside afterward.",

        "first_campfire" =>
            "Campfires provide light in dark areas. Stand near one to see better at night.",

        "first_shrine" =>
            "Shrines grant temporary stat buffs. Worth seeking out before tough fights!",

        "first_night" =>
            "Night is falling — your vision range shrinks dramatically. Your torch provides a warm bubble of light around you.",

        "first_weather_change" =>
            "The weather has changed! Weather affects combat: Rain reduces crit, Fog reduces vision, Wind boosts throwables.",

        // ── Progression ──────────────────────────────────────────────
        "first_level_up" =>
            "Level up! You gained 5 skill points. Press P to view your stats — allocate points to customize your build.",

        "first_bounty" =>
            "Bounty accepted! Kill the target mob type to earn bonus Col and XP. Check progress in the sidebar.",

        "first_death" =>
            "Death costs you 25% of your Col and 10% of your XP. Prepare better next time — bring potions and watch your HP!",

        // ── Floor 1 intro ────────────────────────────────────────────
        "floor1_welcome" =>
            "Welcome to Aincrad! You're in the Town of Beginnings — a safe zone where no monsters spawn. " +
            "Explore the town, talk to NPCs, and prepare before venturing into the wilderness beyond the gates.",

        "floor1_exit_town" =>
            "You've left the safe zone! Monsters roam the wilderness. Fight carefully — if you die, you lose Col and XP.",

        _ => null,
    };
}
