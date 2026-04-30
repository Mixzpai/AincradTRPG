using System.Runtime.CompilerServices;
using SAOTRPG.UI;

namespace SAOTRPG.Items.Equipment;

// Typed-effect parse cache. Walks EquipmentBase.SpecialEffect once per equipment
// instance, builds the typed record list, caches via ConditionalWeakTable.
// String field on EquipmentBase remains save-format authority; parser is lazy + idempotent.
public static class EquipmentSpecialEffectRegistry
{
    private static readonly ConditionalWeakTable<EquipmentBase, IReadOnlyList<EquipmentSpecialEffect>> _cache = new();
    // Tracks unknown keys per session so warnings don't spam every parse.
    private static readonly HashSet<string> _warnedKeys = new();

    // Parsed view of an equipment's SpecialEffect string. Empty list when null/unset.
    public static IReadOnlyList<EquipmentSpecialEffect> GetParsed(EquipmentBase eq)
        => _cache.GetValue(eq, BuildParsed);

    private static IReadOnlyList<EquipmentSpecialEffect> BuildParsed(EquipmentBase eq)
    {
        var list = new List<EquipmentSpecialEffect>();
        var fx = eq.SpecialEffect;
        if (string.IsNullOrEmpty(fx)) return list;

        // Tokenizer — letter-key + signed int pairs (e.g. "C5B10" → Crit+5, Block+10).
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
}
