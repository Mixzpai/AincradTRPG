using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.Systems.Input;
using SAOTRPG.UI.Dialogs;

namespace SAOTRPG.UI;

// Keyboard input handling for the dungeon map view.
public partial class MapView
{
    protected override bool OnKeyDown(Key keyEvent)
    {
        // Any keypress is potential state-change -> invalidate frame cache before dispatch.
        DirtyFrame();
        if (HandleLookModeKey(keyEvent)) return true;
        // Reticle modal swallows keys before any movement/dialog dispatch.
        if (HandleRangedFireKey(keyEvent)) return true;
        // Dev instrumentation, deliberately not rebindable.
        if (HandleDebugKey(keyEvent)) return true;

        // Esc is reserved. Terminal.Gui binds it to Command.Quit at the application level, and a
        // player who rebound it could strand themselves with no way back to the rebind screen.
        // Dialog-on-top case: Terminal.Gui routes KeyDown to the focused dialog first
        // (DialogHelper.CloseOnEscape), so this only fires from map focus - no double-close.
        if (keyEvent.KeyCode == KeyCode.Esc) return FireEvent(PauseRequested, keyEvent);

        // Exact match first, so an explicitly-bound chord (Shift+M) wins over the direction its
        // base key would otherwise resolve to.
        GameAction action = Keybinds.Resolve(InputContext.Map, keyEvent);
        if (action == GameAction.None)
        {
            GameAction direction = Keybinds.ResolveDirectionLoose(InputContext.Map, keyEvent);
            if (direction == GameAction.None) return base.OnKeyDown(keyEvent);

            return Move(direction, keyEvent);
        }

        if (Keybinds.IsDirection(action)) return Move(action, keyEvent);

        switch (action)
        {
            case GameAction.Look:             return FireEvent(LookRequested,           keyEvent);
            case GameAction.OpenInventory:    return FireEvent(InventoryRequested,      keyEvent);
            case GameAction.Pickup:           return FireEvent(PickupRequested,         keyEvent);
            case GameAction.AutoExplore:      return FireEvent(AutoExploreRequested,    keyEvent);
            case GameAction.OpenBestiary:     return FireEvent(BestiaryRequested,       keyEvent);
            case GameAction.OpenStats:        return FireEvent(StatsRequested,          keyEvent);
            case GameAction.OpenHelp:         return FireEvent(HelpRequested,           keyEvent);
            case GameAction.OpenPlayerGuide:  return FireEvent(PlayerGuideRequested,    keyEvent);
            case GameAction.Rest:             return FireEvent(RestRequested,           keyEvent);
            case GameAction.OpenQuestLog:     return FireEvent(QuestLogRequested,       keyEvent);
            case GameAction.OpenKillStats:    return FireEvent(KillStatsRequested,      keyEvent);
            case GameAction.OpenEquipment:    return FireEvent(EquipmentRequested,      keyEvent);
            case GameAction.Counter:          return FireEvent(CounterRequested,        keyEvent);
            case GameAction.OpenSwordSkills:  return FireEvent(SwordSkillMenuRequested, keyEvent);
            case GameAction.RangedFire:       return FireEvent(RangedFireKeyPressed,    keyEvent);
            case GameAction.Wait:             return FireEvent(WaitRequested,           keyEvent);
            case GameAction.QuickSave:        return FireEvent(SaveRequested,           keyEvent);
            case GameAction.LogScrollUp:      return FireEvent(LogScrollUpRequested,    keyEvent);
            case GameAction.LogScrollDown:    return FireEvent(LogScrollDownRequested,  keyEvent);

            case GameAction.ToggleStatusTray:
                StatusTrayVerboseToggleRequested?.Invoke();
                keyEvent.Handled = true;
                return true;

            case GameAction.ToggleHeightmap:
                SAOTRPG.UI.MapView.HeightmapDebugEnabled = !SAOTRPG.UI.MapView.HeightmapDebugEnabled;
                SetNeedsDraw();
                keyEvent.Handled = true;
                return true;

            case GameAction.OpenMilestones:
                ClearDamagePopups();
                MilestonesDialog.Show(_player);
                keyEvent.Handled = true;
                return true;

            // Routes to the same dialog focused on the Collectables tab, so muscle memory from
            // the old Legendary Collectables panel survives the merge.
            case GameAction.OpenCollectables:
                ClearDamagePopups();
                MilestonesDialog.Show(_player, initialCategory: "Collectables");
                keyEvent.Handled = true;
                return true;

            case GameAction.SwordSkill1: return FireSlot(SwordSkillRequested, 0, keyEvent);
            case GameAction.SwordSkill2: return FireSlot(SwordSkillRequested, 1, keyEvent);
            case GameAction.SwordSkill3: return FireSlot(SwordSkillRequested, 2, keyEvent);
            case GameAction.SwordSkill4: return FireSlot(SwordSkillRequested, 3, keyEvent);

            case GameAction.QuickUse1:  return FireSlot(QuickUseRequested, 1,  keyEvent);
            case GameAction.QuickUse2:  return FireSlot(QuickUseRequested, 2,  keyEvent);
            case GameAction.QuickUse3:  return FireSlot(QuickUseRequested, 3,  keyEvent);
            case GameAction.QuickUse4:  return FireSlot(QuickUseRequested, 4,  keyEvent);
            case GameAction.QuickUse5:  return FireSlot(QuickUseRequested, 5,  keyEvent);
            case GameAction.QuickUse6:  return FireSlot(QuickUseRequested, 6,  keyEvent);
            case GameAction.QuickUse7:  return FireSlot(QuickUseRequested, 7,  keyEvent);
            case GameAction.QuickUse8:  return FireSlot(QuickUseRequested, 8,  keyEvent);
            case GameAction.QuickUse9:  return FireSlot(QuickUseRequested, 9,  keyEvent);
            case GameAction.QuickUse10: return FireSlot(QuickUseRequested, 10, keyEvent);

            default: return base.OnKeyDown(keyEvent);
        }
    }

    // Shift sprints and Ctrl moves stealthily; both are modifiers ON a direction rather than
    // separate bindings, which is why direction resolution ignores them.
    private bool Move(GameAction direction, Key keyEvent)
    {
        (int dx, int dy) = Keybinds.Delta(direction);

        if (keyEvent.IsShift) SprintRequested?.Invoke(dx, dy);
        else if (keyEvent.IsCtrl) StealthMoveRequested?.Invoke(dx, dy);
        else PlayerMoveRequested?.Invoke(dx, dy);

        keyEvent.Handled = true;
        return true;
    }

    // Profiler and biome hot-reload. Instrumentation, not player keys - kept out of the binding
    // table so a rebind cannot take a debugging tool away mid-session.
    private bool HandleDebugKey(Key keyEvent)
    {
        KeyCode bare = keyEvent.KeyCode & ~KeyCode.ShiftMask & ~KeyCode.CtrlMask & ~KeyCode.AltMask;

        if (bare == KeyCode.F9 && !keyEvent.IsShift) return FireEvent(BiomeReloadRequested, keyEvent);
        if (!keyEvent.IsShift) return false;

        switch (bare)
        {
            case KeyCode.F8:
                LightDebugMode lm = CycleLightDebug();
                SetNeedsDraw();
                Log?.LogSystem($"[LIGHT] Lighting debug overlay: {lm}.");
                keyEvent.Handled = true;
                return true;

            case KeyCode.F10:
                Profiler.Enabled = !Profiler.Enabled;
                Log?.LogSystem(Profiler.Enabled ? "[PROF] Profiler enabled." : "[PROF] Profiler disabled.");
                keyEvent.Handled = true;
                return true;

            case KeyCode.F11:
                Profiler.Reset();
                Log?.LogSystem("[PROF] Profiler reset.");
                keyEvent.Handled = true;
                return true;

            case KeyCode.F12:
                string path = Profiler.DumpToFile();
                if (Log != null && !string.IsNullOrEmpty(path)) Log.Log($"[PROF] Profiler dumped to {path}");
                keyEvent.Handled = true;
                return true;

            default: return false;
        }
    }

    private static bool FireSlot(Action<int>? handler, int slot, Key keyEvent)
    {
        handler?.Invoke(slot);
        keyEvent.Handled = true;
        return true;
    }

    private static bool FireEvent(Action? handler, Key keyEvent)
    {
        handler?.Invoke();
        keyEvent.Handled = true;
        return true;
    }
}
