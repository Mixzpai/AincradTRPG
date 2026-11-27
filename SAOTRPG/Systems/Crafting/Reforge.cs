using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Systems.RunState;
using SAOTRPG.UI.GameLog;

namespace SAOTRPG.Systems.Crafting;

// Reforge verb. Re-rolls a weapon's base Bonuses within its rarity tier band;
// preserves EnhancementLevel/AwakeningLevel/RefinementSlots/sockets. LisbethCraftDialog
// F4 mode tab binds to the record signatures below.
public static class Reforge
{
    public record CostSpec(int ColCost, IReadOnlyList<LisbethRecipes.MaterialRequirement> Mats);
    public record PreviewResult(string CurrentBonusesDescription, string PreviewBonusesDescription, bool IsUpgrade);

    // Per-rarity reforge cost. Mat is mithril for ≤Rare, crystallite for ≥Epic.
    public static CostSpec GetCost(Weapon weapon)
    {
        return weapon.Rarity switch
        {
            "Common"    => new CostSpec(5_000,    new[] { new LisbethRecipes.MaterialRequirement("mithril_ingot",     3) }),
            "Uncommon"  => new CostSpec(25_000,   new[] { new LisbethRecipes.MaterialRequirement("mithril_ingot",     3) }),
            "Rare"      => new CostSpec(100_000,  new[] { new LisbethRecipes.MaterialRequirement("mithril_ingot",     3) }),
            "Epic"      => new CostSpec(250_000,  new[] { new LisbethRecipes.MaterialRequirement("crystallite_ingot", 5) }),
            "Legendary" => new CostSpec(1_000_000,new[] { new LisbethRecipes.MaterialRequirement("crystallite_ingot", 5) }),
            _           => new CostSpec(int.MaxValue, Array.Empty<LisbethRecipes.MaterialRequirement>()),
        };
    }

    // Eligibility: Common-Legendary, IsEnhanceable (LAB-sealed weapons rejected), Divine ineligible.
    public static bool IsEligible(Weapon weapon, out string? reason)
    {
        string rarity = weapon.Rarity ?? "";
        if (rarity == "Divine")
        {
            reason = "Divine weapons cannot be reforged — their bonuses are sealed.";
            return false;
        }
        if (!IsKnownRarity(rarity))
        {
            reason = $"Unknown rarity '{rarity}' — cannot reforge.";
            return false;
        }
        if (!weapon.IsEnhanceable)
        {
            reason = "Last-Attack-Bonus weapon — sealed against Reforge.";
            return false;
        }
        reason = null;
        return true;
    }

    // Preview a roll for the given seed without mutating the weapon. Caller renders
    // both before/after in the confirmation modal.
    public static PreviewResult PeekRoll(Weapon weapon, int seed)
    {
        string before = DescribeBonuses(weapon.Bonuses);
        var rolled = RollBonuses(weapon, weapon.Rarity ?? "Common", new Random(seed));
        string after = DescribeBonuses(rolled);
        bool upgrade = SumPotency(rolled) > SumPotency(weapon.Bonuses);
        return new PreviewResult(before, after, upgrade);
    }

    // Apply: consume Col + mats, mutate weapon.Bonuses, invalidate stat cache, log.
    // Returns false if eligibility/Col/mats fail; true on success.
    public static bool Apply(Weapon weapon, Player player, IGameLog log)
    {
        if (!IsEligible(weapon, out var reason))
        {
            log.LogSystem($"  Reforge denied: {reason}");
            return false;
        }
        var cost = GetCost(weapon);
        if (player.ColOnHand < cost.ColCost)
        {
            log.LogSystem($"  Reforge denied: need {cost.ColCost:N0} Col (have {player.ColOnHand:N0}).");
            return false;
        }
        foreach (var mat in cost.Mats)
        {
            int have = player.Inventory.CountByDefinitionId(mat.DefId);
            if (have < mat.Qty)
            {
                string name = ItemRegistry.Create(mat.DefId)?.Name ?? mat.DefId;
                log.LogSystem($"  Reforge denied: need {mat.Qty}x {name} (have {have}).");
                return false;
            }
        }

        foreach (var mat in cost.Mats) player.Inventory.ConsumeByDefinitionId(mat.DefId, mat.Qty);
        player.ColOnHand -= cost.ColCost;

        // The gameplay stream, not a fresh Random: this is a stat-mutating roll, so it must
        // reproduce from the run's seed like any other gameplay draw.
        var newBonuses = RollBonuses(weapon, weapon.Rarity ?? "Common", RunRng.Stream);
        weapon.Bonuses = newBonuses;
        player.Inventory.InvalidateStatCache();

        log.LogLoot($"  ◈ Reforged {weapon.Name}: {DescribeBonuses(newBonuses)} (-{cost.ColCost:N0} Col)");
        return true;
    }

    // Rerolls the weapon's base effects with tier-banded magnitude and keeps every entry appended
    // after them (enhancement, ingots, awakening). Pool of stats = currently-set stat types
    // (preserves weapon flavor — sword keeps melee leanings, etc.).
    private static StatModifierCollection RollBonuses(Weapon weapon, string rarity, Random rng)
    {
        int count = BaseEffectCount(weapon);
        var pool = StatPoolForWeapon(weapon);
        var (lo, hi) = MagnitudeBandForRarity(rarity);

        var rolled = new StatModifierCollection();
        var used = new HashSet<StatType>();
        for (int i = 0; i < count; i++)
        {
            StatType pick;
            int guard = 0;
            do { pick = pool[rng.Next(pool.Count)]; guard++; }
            while (used.Contains(pick) && guard < 20);
            used.Add(pick);
            int potency = rng.Next(lo, hi + 1);
            rolled.Add(pick, potency);
        }
        rolled.Effects.AddRange(weapon.Bonuses.Effects.Skip(count));
        return rolled;
    }

    // A weapon's own effects lead Bonuses; every other source appends after them.
    // A procedural weapon has no definition, so it is assumed to carry LootGenerator.MakeWeapon's two.
    private static int BaseEffectCount(Weapon weapon) =>
        Math.Min(weapon.Bonuses.Effects.Count,
            weapon.DefinitionId != null && ItemRegistry.Create(weapon.DefinitionId) is Weapon def
                ? def.Bonuses.Effects.Count : 2);

    // A registered weapon's rerolled base effects, for the save; null while they still match its definition.
    public static List<StatEffect>? RerolledBase(Weapon weapon)
    {
        if (weapon.DefinitionId == null || ItemRegistry.Create(weapon.DefinitionId) is not Weapon def) return null;
        var baseFx = def.Bonuses.Effects;
        var fx = weapon.Bonuses.Effects;
        if (fx.Count < baseFx.Count) return null;
        for (int i = 0; i < baseFx.Count; i++)
            if (fx[i].Type != baseFx[i].Type || fx[i].Potency != baseFx[i].Potency)
                return fx.Take(baseFx.Count).ToList();
        return null;
    }

    // Stat pool per weapon class. Falls back to existing Bonus stat types if
    // the weapon has unknown WeaponType.
    private static List<StatType> StatPoolForWeapon(Weapon weapon)
    {
        var current = weapon.Bonuses.Effects.Select(e => e.Type).Distinct().ToList();
        var pool = new List<StatType>(current);
        // Always allow Attack as a baseline option.
        if (!pool.Contains(StatType.Attack)) pool.Add(StatType.Attack);
        // Wider weapon-class flavoring.
        switch (weapon.WeaponType)
        {
            case "Bow":
                if (!pool.Contains(StatType.Dexterity)) pool.Add(StatType.Dexterity);
                if (!pool.Contains(StatType.CritRate))  pool.Add(StatType.CritRate);
                break;
            case "Dagger":
            case "Rapier":
                if (!pool.Contains(StatType.Agility))  pool.Add(StatType.Agility);
                if (!pool.Contains(StatType.CritRate)) pool.Add(StatType.CritRate);
                break;
            case "Two-Handed Sword":
            case "Two-Handed Axe":
            case "Mace":
                if (!pool.Contains(StatType.Strength)) pool.Add(StatType.Strength);
                if (!pool.Contains(StatType.Vitality)) pool.Add(StatType.Vitality);
                break;
            default:
                if (!pool.Contains(StatType.Strength)) pool.Add(StatType.Strength);
                if (!pool.Contains(StatType.Dexterity)) pool.Add(StatType.Dexterity);
                break;
        }
        return pool;
    }

    // Magnitude bands per rarity — lower bound never zero so a roll always feels meaningful.
    private static (int lo, int hi) MagnitudeBandForRarity(string rarity) => rarity switch
    {
        "Common"    => (3,  9),
        "Uncommon"  => (5, 14),
        "Rare"      => (8, 22),
        "Epic"      => (14, 38),
        "Legendary" => (20, 60),
        _           => (1,  3),
    };

    private static bool IsKnownRarity(string rarity) =>
        rarity == "Common" || rarity == "Uncommon" || rarity == "Rare"
        || rarity == "Epic" || rarity == "Legendary";

    private static string DescribeBonuses(StatModifierCollection coll)
    {
        if (coll.Effects.Count == 0) return "(no bonuses)";
        var parts = coll.Effects.Select(e => $"{e.Type} {(e.Potency >= 0 ? "+" : "")}{e.Potency}");
        return string.Join(" / ", parts);
    }

    private static int SumPotency(StatModifierCollection coll)
    {
        int total = 0;
        foreach (var e in coll.Effects) total += Math.Abs(e.Potency);
        return total;
    }
}
