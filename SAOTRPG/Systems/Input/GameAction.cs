namespace SAOTRPG.Systems.Input;

// Where a keystroke is being interpreted. The same physical key means different things in
// different places — L is Look on the map and Compare in the inventory — so a binding is only
// ever unique within its context, and conflict checking is per context too.
public enum InputContext
{
    Map,
    LookMode,
    RangedFire,
    Log,
    Inventory,
    PlayerGuide,
    Milestones,
    Bestiary,
}

// Every rebindable action.
//
// NOT here, deliberately: Esc, Enter, Tab and the cursor keys where they drive Terminal.Gui's own
// focus and list navigation. Those are the framework's contract, not game keybinds — Terminal.Gui
// binds Esc to Command.Quit at the application level, so a player who rebound it could strand
// themselves in a dialog with no way out and no way back to the rebind screen to undo it. The
// cursor keys stay bound to movement and reticle motion as ALTERNATES, which is rebindable; it is
// only their list-navigation role that is fixed. Debug instrumentation (Shift+F10/F11/F12
// profiler, F9 biome reload) is dev tooling and is not exposed either.
public enum GameAction
{
    None = 0,

    // ── Map: movement ────────────────────────────────────────────────
    MoveNorth, MoveSouth, MoveWest, MoveEast,
    MoveNorthWest, MoveNorthEast, MoveSouthWest, MoveSouthEast,
    Wait,

    // ── Map: actions ─────────────────────────────────────────────────
    Look, OpenInventory, Pickup, AutoExplore, OpenBestiary, OpenStats, OpenHelp,
    OpenPlayerGuide, Rest, OpenQuestLog, OpenKillStats, OpenEquipment, Counter,
    OpenSwordSkills, RangedFire, QuickSave,
    OpenMilestones, OpenCollectables, ToggleStatusTray, ToggleHeightmap,

    // ── Map: sword-skill slots ───────────────────────────────────────
    SwordSkill1, SwordSkill2, SwordSkill3, SwordSkill4,

    // ── Map: quick-use slots ─────────────────────────────────────────
    QuickUse1, QuickUse2, QuickUse3, QuickUse4, QuickUse5,
    QuickUse6, QuickUse7, QuickUse8, QuickUse9, QuickUse10,

    // ── Map: log ─────────────────────────────────────────────────────
    LogScrollUp, LogScrollDown,

    // ── Look mode ────────────────────────────────────────────────────
    LookNextTarget, LookPrevTarget, LookInspect,

    // ── Ranged fire ──────────────────────────────────────────────────
    FireConfirm, FireNextTarget,
    ReticleNorth, ReticleSouth, ReticleWest, ReticleEast,
    ReticleNorthWest, ReticleNorthEast, ReticleSouthWest, ReticleSouthEast,

    // ── Inventory ────────────────────────────────────────────────────
    InvFilterWeapons, InvFilterArmor, InvFilterMaterials, InvFilterConsumables, InvFilterAll,
    InvCompare,

    // ── Player Guide ─────────────────────────────────────────────────
    GuideSearch, GuideBack, GuideExpand,

    // ── Milestones ───────────────────────────────────────────────────
    // CycleBucket drives the Collectables source filter and FocusKillLog moves between the
    // Monument's two lists; both surfaces existed with no key that reached them.
    MilestoneEquipTitle, MilestoneUnequipTitle,
    MilestoneCycleBucket, MilestoneFocusKillLog,
    MilestoneNextTab, MilestonePrevTab,

    // ── Bestiary ─────────────────────────────────────────────────────
    // FloorBand cycles a preset range; the rest of this dialog matches bare runes directly.
    BestiarySearch, BestiaryFloorBand,
    BestiaryBossOnly, BestiaryShowUndiscovered, BestiaryClearFilters, BestiarySortPrefix,
}
