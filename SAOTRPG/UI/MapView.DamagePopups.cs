using Terminal.Gui;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Subtle damage popups: float 1 cell up over ~400ms, fade in last 150ms.
// Dim-first palette (crits add brightness + polygon glyph); frame-coalesce + DoT + chip filters keep it quiet.
public partial class MapView
{
    private sealed class DamagePopup
    {
        public int X, Y;
        public string Text;
        public Color Color;
        public int ElapsedMs;
        public bool IsCrit;
        // MultiHitStream popups bypass the 3-per-tile coalesce rule so the
        // 16-hit Starburst Stream cascade renders as 16 distinct numbers.
        public bool MultiHitStream;
        // Pre-render delay — skill cascade staggers popups at 40ms intervals
        // so they don't all collide on the same frame.
        public int DelayMs;

        public DamagePopup(int x, int y, string text, Color color, bool crit,
            bool multiHit = false, int delayMs = 0)
        { X = x; Y = y; Text = text; Color = color; IsCrit = crit;
          MultiHitStream = multiHit; DelayMs = delayMs; }
    }

    private const int PopupLifetimeMs = 400;
    private const int PopupFadeOutMs  = 150;
    // Max concurrent popups per (x,y) frame before coalesce.
    private const int PopupMaxPerTile = 3;
    private readonly List<DamagePopup> _popups = new();
    private readonly HashSet<int> _dotTickFirstSeen = new();

    // Called by TurnManager.Combat via the DamageDealt event. Caller-driven targeting
    // (monster tile for player hits); maxTargetHp drives <2% chip-damage suppression.
    public void EnqueueDamagePopup(int mx, int my, int damage, bool isCrit,
        Color tintColor, int maxTargetHp)
    {
        if (damage <= 0) return;
        DirtyFrame();
        // Chip-damage suppression (research §5): sub-2% max HP reads as noise.
        if (maxTargetHp > 0 && damage * 100 < maxTargetHp * 2 && !isCrit) return;

        string text = damage.ToString();
        Color c = isCrit ? Color.BrightYellow : tintColor;
        if (isCrit) text = "◇" + text;

        // Same-frame coalesce: when 3+ popups already sit on this tile in the
        // spawn frame, replace the oldest with an aggregate entry.
        var sameTile = new List<DamagePopup>();
        foreach (var p in _popups)
            if (p.X == mx && p.Y == my && p.ElapsedMs <= 32 && !p.MultiHitStream) sameTile.Add(p);

        if (sameTile.Count >= PopupMaxPerTile - 1)
        {
            int total = damage;
            foreach (var p in sameTile)
                if (int.TryParse(p.Text.TrimStart('◇'), out int v)) total += v;
            foreach (var p in sameTile) _popups.Remove(p);
            _popups.Add(new DamagePopup(mx, my, $"x{sameTile.Count + 1} {total}", c, isCrit));
            return;
        }

        // Stack offset: if another popup is active on this tile, bump Y up by
        // 1 so the two numbers don't overlap.
        int yOffset = 0;
        foreach (var p in _popups)
            if (p.X == mx && p.Y == my - yOffset) yOffset++;

        _popups.Add(new DamagePopup(mx, my - yOffset, text, c, isCrit));
    }

    // Multi-hit cascade popup (Starburst Stream, Eclipse, Mother's Rosario).
    // Bypasses coalesce; ±1 cell x-jitter prevents single-column stack; delayMs staggers per hitIndex.
    public void EnqueueMultiHitPopup(int mx, int my, int damage, int hitIndex, int delayMs)
    {
        if (damage <= 0) return;
        DirtyFrame();
        // Alternate BrightYellow / White per hit for visual pulse.
        Color c = (hitIndex & 1) == 0 ? Color.BrightYellow : Color.White;
        int jitter = (hitIndex % 3) - 1;
        string text = damage.ToString();
        _popups.Add(new DamagePopup(mx + jitter, my, text, c,
            crit: false, multiHit: true, delayMs: delayMs));
    }

    // Aggregate "×N = total" popup for end-of-cascade summary. BrightYellow,
    // fires 400ms after the last hit per research §7.3.
    public void EnqueueCascadeAggregate(int mx, int my, int hits, int total, int delayMs)
    {
        _popups.Add(new DamagePopup(mx, my - 1, $"×{hits} = {total}",
            Color.BrightYellow, crit: true, multiHit: true, delayMs: delayMs));
        DirtyFrame();
    }

    // DoT tick filter — first tick per mob id passes, the rest suppress.
    // Caller (TurnManager) passes a stable key like the mob Id.
    public bool ShouldShowDotTick(int mobId)
    {
        if (_dotTickFirstSeen.Add(mobId)) return true;
        return false;
    }

    public void ResetDotTracking() => _dotTickFirstSeen.Clear();

    // Clears pending popups + projectiles — called when a modal dialog opens
    // so overlays don't leak past the z-order boundary.
    public void ClearDamagePopups()
    { _popups.Clear(); ClearProjectiles(); DirtyFrame(); }

    public bool HasActivePopups => _popups.Count > 0 || HasActiveProjectiles;

    // Resolves the element tint from a weapon SpecialEffect string. Returns
    // Color.White for physical or unknown effects — caller decides.
    public static Color ResolveElementalTint(string? specialEffect)
    {
        if (string.IsNullOrEmpty(specialEffect)) return Color.White;
        if (specialEffect.Contains("Burn", StringComparison.OrdinalIgnoreCase))
            return Color.Red;
        if (specialEffect.Contains("Freeze", StringComparison.OrdinalIgnoreCase)
            || specialEffect.Contains("Slow", StringComparison.OrdinalIgnoreCase))
            return Color.Cyan;
        if (specialEffect.Contains("Shock", StringComparison.OrdinalIgnoreCase)
            || specialEffect.Contains("Thunder", StringComparison.OrdinalIgnoreCase))
            return Color.Yellow;
        if (specialEffect.Contains("Holy", StringComparison.OrdinalIgnoreCase))
            return Color.BrightYellow;
        if (specialEffect.Contains("Dark", StringComparison.OrdinalIgnoreCase))
            return Color.Magenta;
        return Color.White;
    }

    // Per-frame render + tick. Integrates into the existing overlay pass.
    private void RenderDamagePopups(int w, int h, int dtMs)
    {
        for (int i = _popups.Count - 1; i >= 0; i--)
        {
            var p = _popups[i];
            // Delayed popups count down without ageing — invisible until due.
            if (p.DelayMs > 0) { p.DelayMs -= dtMs; continue; }
            p.ElapsedMs += dtMs;
            if (p.ElapsedMs >= PopupLifetimeMs)
            {
                _popups.RemoveAt(i);
                continue;
            }
            if (!_map.InBounds(p.X, p.Y) || !_map.IsVisible(p.X, p.Y)) continue;

            // Tween: stay at (x, y) first third, then rise 1 cell.
            int vy = MapToVy(p.Y - 1);
            if (p.ElapsedMs < PopupLifetimeMs / 3) vy = MapToVy(p.Y);
            int vx = MapToVx(p.X);

            // Alpha-emulation: dim the foreground during the last 150ms.
            Color draw = p.Color;
            int fadeStart = PopupLifetimeMs - PopupFadeOutMs;
            if (p.ElapsedMs > fadeStart)
            {
                float t = (p.ElapsedMs - fadeStart) / (float)PopupFadeOutMs;
                draw = Dim(p.Color, t);
            }

            DrawTextAtView(vx, vy, p.Text, Gfx.Attr(draw, Color.Black), w, h);
        }
    }

    // Linear RGB dim toward black. t=0 → full color, t=1 → black.
    private static Color Dim(Color c, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        float k = 1f - t;
        return new Color((byte)(c.R * k), (byte)(c.G * k), (byte)(c.B * k));
    }
}
