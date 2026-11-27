
using SAOTRPG.Systems.Diagnostics;

namespace SAOTRPG.Items.Equipment;

// Typed-effect parser. Walks EquipmentBase.SpecialEffect once and builds the typed record list.
// The string field on EquipmentBase remains save-format authority; the parse is lazy and its
// result is held by the item itself (EquipmentBase._parsedEffects), so this type is stateless
// apart from the warn-once set.
public static class EquipmentSpecialEffectRegistry
{
    // Tracks unknown keys so each one warns once rather than on every parse.
    private static readonly HashSet<string> _warnedKeys = new();

    // Parsed view of an equipment's SpecialEffect string. Empty list when null/unset.
    // Callers should read EquipmentBase.ParsedEffects, which caches; this always re-parses.
    public static List<EquipmentSpecialEffect> Parse(EquipmentBase eq)
    {
        var list = new List<EquipmentSpecialEffect>();
        var fx = eq.SpecialEffect;
        if (string.IsNullOrEmpty(fx)) return list;

        // Tokenizer — letter-key + signed int pairs (e.g. "Bleed+25,CritRate+20" → Bleed 25, CritRate 20).
        int i = 0;
        while (i < fx.Length)
        {
            if (!char.IsLetter(fx[i])) { i++; continue; }
            int keyStart = i;
            while (i < fx.Length && char.IsLetter(fx[i])) i++;
            string key = fx.Substring(keyStart, i - keyStart);

            int numStart = i;
            while (i < fx.Length)
            {
                char c = fx[i];
                if (c == '+' || c == '-' || char.IsDigit(c)) i++;
                else break;
            }
            if (i > numStart && int.TryParse(fx.AsSpan(numStart, i - numStart), out int val))
            {
                var rec = EquipmentSpecialEffect.Build(key, val);
                if (rec != null) list.Add(rec);
                else if (_warnedKeys.Add(key))
                    DebugLogger.LogGame("SPECIALEFFECT", $"Unknown SpecialEffect key '{key}' on '{eq.Name}' parses to int but has no typed record (legacy/modded).");
            }
        }
        return list;
    }

    // Cached parsed view. Kept so a caller holding only the registry can still ask.
    public static IReadOnlyList<EquipmentSpecialEffect> GetParsed(EquipmentBase eq) => eq.ParsedEffects;
}
