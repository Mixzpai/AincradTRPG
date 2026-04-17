using Terminal.Gui;

namespace SAOTRPG.UI;

// Keyboard input handling for the dungeon map view.
public partial class MapView
{
    protected override bool OnKeyDown(Key keyEvent)
    {
        if (HandleLookModeKey(keyEvent)) return true;

        int dx = 0, dy = 0;
        var bareKey = keyEvent.KeyCode & ~KeyCode.ShiftMask & ~KeyCode.CtrlMask & ~KeyCode.AltMask;

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
            case KeyCode.D1: QuickUseRequested?.Invoke(1); keyEvent.Handled = true; return true;
            case KeyCode.D2: QuickUseRequested?.Invoke(2); keyEvent.Handled = true; return true;
            case KeyCode.D3: QuickUseRequested?.Invoke(3); keyEvent.Handled = true; return true;
            case KeyCode.D4: QuickUseRequested?.Invoke(4); keyEvent.Handled = true; return true;
            case KeyCode.D5: QuickUseRequested?.Invoke(5); keyEvent.Handled = true; return true;
            case KeyCode.PageUp:   return FireEvent(LogScrollUpRequested,   keyEvent);
            case KeyCode.PageDown: return FireEvent(LogScrollDownRequested, keyEvent);
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
