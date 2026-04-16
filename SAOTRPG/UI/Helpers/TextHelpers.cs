namespace SAOTRPG.UI.Helpers;

// Small string utilities used by the HUD and sidebar to keep text
// inside fixed-width column budgets.
public static class TextHelpers
{
    // Truncate a string to  characters, adding
    // an ellipsis "…" if it was shortened. Returns the string unchanged
    // when it already fits (or when  ≤ 1).
    public static string Truncate(string? s, int maxLen)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= maxLen) return s ?? "";
        if (maxLen <= 1) return s[..maxLen];
        return string.Concat(s.AsSpan(0, maxLen - 1), "…");
    }
}
