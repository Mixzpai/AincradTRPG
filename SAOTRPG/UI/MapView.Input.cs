using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Dialogs;

namespace SAOTRPG.UI;

// Keyboard input handling for the dungeon map view.
public partial class MapView
{
    protected override bool OnKeyDown(Key keyEvent)
    {
        // Any keypress is potential state-change → invalidate frame cache before dispatch.
        DirtyFrame();
        if (HandleLookModeKey(keyEvent)) return true;
        // Reticle modal swallows keys before any movement/dialog dispatch.
        if (HandleRangedFireKey(keyEvent)) return true;

        int dx = 0, dy = 0;
        var bareKey = keyEvent.KeyCode & ~KeyCode.ShiftMask & ~KeyCode.CtrlMask & ~KeyCode.AltMask;

        // Shift+S — status tray verbose toggle. Captured before directional
        // dispatch so Shift+S-as-sprint-south is preserved via Shift+Down arrow.
        if (bareKey == KeyCode.S && keyEvent.IsShift
            && (keyEvent.KeyCode & KeyCode.CtrlMask) == 0)
        {
            StatusTrayVerboseToggleRequested?.Invoke();
            keyEvent.Handled = true;
            return true;
        }

        // Shift+G — toggle heightmap debug overlay. Captured before switch so
        // bare G (pickup) stays intact.
        if (bareKey == KeyCode.G && keyEvent.IsShift
            && (keyEvent.KeyCode & KeyCode.CtrlMask) == 0)
        {
            SAOTRPG.UI.MapView.HeightmapDebugEnabled = !SAOTRPG.UI.MapView.HeightmapDebugEnabled;
            SetNeedsDraw();
            keyEvent.Handled = true;
            return true;
        }

        // Shift+F10 — toggle profiler on/off (default off; session-only).
        // Shift+F11 — reset buckets.
        // Shift+F12 — dump current buckets to a timestamped file.
        if (bareKey == KeyCode.F10 && keyEvent.IsShift)
        {
            Profiler.Enabled = !Profiler.Enabled;
            Log?.LogSystem(Profiler.Enabled ? "[PROF] Profiler enabled." : "[PROF] Profiler disabled.");
            keyEvent.Handled = true;
            return true;
        }
        if (bareKey == KeyCode.F12 && keyEvent.IsShift)
        {
            string path = Profiler.DumpToFile();
            if (Log != null && !string.IsNullOrEmpty(path))
                Log.Log($"[PROF] Profiler dumped to {path}");
            keyEvent.Handled = true;
            return true;
        }
        if (bareKey == KeyCode.F11 && keyEvent.IsShift)
        {
            Profiler.Reset();
            Log?.LogSystem("[PROF] Profiler reset.");
            keyEvent.Handled = true;
            return true;
        }

        // Shift+M opens the unified Milestones dialog from anywhere on the map.
        if (bareKey == KeyCode.M && keyEvent.IsShift
            && (keyEvent.KeyCode & KeyCode.CtrlMask) == 0)
        {
            ClearDamagePopups();
            MilestonesDialog.Show(_player);
            keyEvent.Handled = true;
            return true;
        }

        // Shift+L re-routes to MilestonesDialog focused on the Collectables tab so
        // muscle memory from the old Legendary Collectables panel survives the merge.
        // Bare L still routes to Look mode via the switch below.
        if (bareKey == KeyCode.L && keyEvent.IsShift
            && (keyEvent.KeyCode & KeyCode.CtrlMask) == 0)
        {
            ClearDamagePopups();
            MilestonesDialog.Show(_player, initialCategory: "Collectables");
            keyEvent.Handled = true;
            return true;
        }

        // `\` (backslash) opens the ranged-fire reticle. KeyCode has no Backslash
        // member in v2 — match via the typed rune so the binding works regardless
        // of host keyboard layout.
        if (keyEvent.AsRune.Value == '\\') return FireEvent(RangedFireKeyPressed, keyEvent);

        switch (bareKey)
        {
            case KeyCode.W: case KeyCode.CursorUp:    dy = -1; break;
            case KeyCode.S: case KeyCode.CursorDown:   dy = 1;  break;
            case KeyCode.A: case KeyCode.CursorLeft:   dx = -1; break;
            case KeyCode.D: case KeyCode.CursorRight:  dx = 1;  break;
            case KeyCode.Q: dx = -1; dy = -1; break;
            case KeyCode.E: dx =  1; dy = -1; break;
            case KeyCode.Z: dx = -1; dy =  1; break;
            case KeyCode.C: dx =  1; dy =  1; break;
            case KeyCode.L:     return FireEvent(LookRequested,        keyEvent);
            case KeyCode.I:     return FireEvent(InventoryRequested,   keyEvent);
            case KeyCode.G:     return FireEvent(PickupRequested,      keyEvent);
            case KeyCode.X:     return FireEvent(AutoExploreRequested, keyEvent);
            case KeyCode.Y:     return FireEvent(BestiaryRequested,    keyEvent);
            case KeyCode.P:     return FireEvent(StatsRequested,       keyEvent);
            case KeyCode.H:     return FireEvent(HelpRequested,        keyEvent);
            case KeyCode.B:     return FireEvent(PlayerGuideRequested, keyEvent);
            case KeyCode.R:     return FireEvent(RestRequested,        keyEvent);
            case KeyCode.J:     return FireEvent(QuestLogRequested,     keyEvent);
            case KeyCode.K:     return FireEvent(KillStatsRequested,   keyEvent);
            case KeyCode.T:     return FireEvent(EquipmentRequested,   keyEvent);
            case KeyCode.V:     return FireEvent(CounterRequested,     keyEvent);
            case KeyCode.F:     return FireEvent(SwordSkillMenuRequested, keyEvent);
            case KeyCode.Space: return FireEvent(WaitRequested,        keyEvent);
            case KeyCode.F1: SwordSkillRequested?.Invoke(0); keyEvent.Handled = true; return true;
            case KeyCode.F2: SwordSkillRequested?.Invoke(1); keyEvent.Handled = true; return true;
            case KeyCode.F3: SwordSkillRequested?.Invoke(2); keyEvent.Handled = true; return true;
            case KeyCode.F4: SwordSkillRequested?.Invoke(3); keyEvent.Handled = true; return true;
            case KeyCode.F5:    return FireEvent(SaveRequested,        keyEvent);
            case KeyCode.F9:    return FireEvent(BiomeReloadRequested, keyEvent);
            case KeyCode.D1: QuickUseRequested?.Invoke(1); keyEvent.Handled = true; return true;
            case KeyCode.D2: QuickUseRequested?.Invoke(2); keyEvent.Handled = true; return true;
            case KeyCode.D3: QuickUseRequested?.Invoke(3); keyEvent.Handled = true; return true;
            case KeyCode.D4: QuickUseRequested?.Invoke(4); keyEvent.Handled = true; return true;
            case KeyCode.D5: QuickUseRequested?.Invoke(5); keyEvent.Handled = true; return true;
            case KeyCode.D6: QuickUseRequested?.Invoke(6); keyEvent.Handled = true; return true;
            case KeyCode.D7: QuickUseRequested?.Invoke(7); keyEvent.Handled = true; return true;
            case KeyCode.D8: QuickUseRequested?.Invoke(8); keyEvent.Handled = true; return true;
            case KeyCode.D9: QuickUseRequested?.Invoke(9); keyEvent.Handled = true; return true;
            case KeyCode.D0: QuickUseRequested?.Invoke(10); keyEvent.Handled = true; return true;
            case KeyCode.PageUp:   return FireEvent(LogScrollUpRequested,   keyEvent);
            case KeyCode.PageDown: return FireEvent(LogScrollDownRequested, keyEvent);
            // Esc → pause menu. Dialog-on-top case: Terminal.Gui routes KeyDown to focused dialog first
            // (DialogHelper.CloseOnEscape), so this only fires from map focus — no double-close.
            case KeyCode.Esc:      return FireEvent(PauseRequested,          keyEvent);
            default: return base.OnKeyDown(keyEvent);
        }

        if (keyEvent.IsShift) SprintRequested?.Invoke(dx, dy);
        else if ((keyEvent.KeyCode & KeyCode.CtrlMask) != 0) StealthMoveRequested?.Invoke(dx, dy);
        else PlayerMoveRequested?.Invoke(dx, dy);
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
