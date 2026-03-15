using System.Text.Json;
using System.Text.Json.Serialization;
using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Items.Materials;
using SAOTRPG.UI;

namespace SAOTRPG.Systems;

/// <summary>
/// Handles saving/loading game state to JSON files.
/// Save location: %LocalAppData%/AincradTRPG/save.json
/// </summary>
public static class SaveManager
{
    private static readonly string SaveDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AincradTRPG");

    private static readonly string SavePath = Path.Combine(SaveDir, "save.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // ── Public API ─────────────────────────────────────────────────

    public static bool SaveExists() => File.Exists(SavePath);

    public static bool SaveGame(Player player, TurnManager turnManager)
    {
        try
        {
            Directory.CreateDirectory(SaveDir);

            var data = BuildSaveData(player, turnManager);
            string json = JsonSerializer.Serialize(data, JsonOpts);

            // Atomic write: write to .tmp then rename
            string tmpPath = SavePath + ".tmp";
            File.WriteAllText(tmpPath, json);
            File.Move(tmpPath, SavePath, overwrite: true);

            return true;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SaveManager.Save", ex);
            return false;
        }
    }

    public static SaveData? LoadGame()
    {
        try
        {
            if (!File.Exists(SavePath)) return null;
            string json = File.ReadAllText(SavePath);
            return JsonSerializer.Deserialize<SaveData>(json, JsonOpts);
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SaveManager.Load", ex);
            return null;
        }
    }

    public static void DeleteSave()
    {
        try
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SaveManager.Delete", ex);
        }
    }

    // ── Build save data from live game state ───────────────────────

    private static SaveData BuildSaveData(Player player, TurnManager tm)
    {
        var data = new SaveData
        {
            Timestamp = DateTime.Now,

            // Player identity
            FirstName = player.FirstName,
            LastName = player.LastName,
            Gender = player.Gender,
            Title = player.Title,
            PlayerId = player.Id,

            // Progression
            Level = player.Level,
            CurrentExperience = player.CurrentExperience,
            CurrentHealth = player.CurrentHealth,
            ColOnHand = player.ColOnHand,
            SkillPoints = player.SkillPoints,

            // Attributes
            Strength = player.Strength,
            Vitality = player.Vitality,
            Endurance = player.Endurance,
            Dexterity = player.Dexterity,
            Agility = player.Agility,
            Intelligence = player.Intelligence,

            // Base combat stats (includes baked-in equipment bonuses)
            BaseAttack = player.BaseAttack,
            BaseDefense = player.BaseDefense,
            BaseSpeed = player.BaseSpeed,
            BaseSkillDamage = player.BaseSkillDamage,
            BaseCriticalRate = player.BaseCriticalRate,
            BaseCriticalHitDamage = player.BaseCriticalHitDamage,

            // TurnManager state
            TurnCount = tm.TurnCount,
            KillCount = tm.KillCount,
            CurrentFloor = tm.CurrentFloor,
            Difficulty = tm.Difficulty,
            IsHardcore = tm.IsHardcore,
            Satiety = tm.Satiety,
            KillStreak = tm.KillStreak,
            WeaponKills = new Dictionary<string, int>(tm.WeaponKills),

            // Status effects
            PoisonTurnsLeft = tm.PoisonTurnsLeft,
            BleedTurnsLeft = tm.BleedTurnsLeft,
            ShrineBuffTurns = tm.ShrineBuffTurns,
            LevelUpBuffTurns = tm.LevelUpBuffTurns,
        };

        // Serialize backpack items
        foreach (var item in player.Inventory.Items)
        {
            data.InventoryItems.Add(SerializeItem(item));
        }

        // Serialize equipped items
        foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
        {
            var equipped = player.Inventory.GetEquipped(slot);
            if (equipped != null)
            {
                data.EquippedItems[slot.ToString()] = SerializeItem(equipped);
            }
        }

        return data;
    }

    // ── Item serialization ─────────────────────────────────────────

    private static ItemSaveData SerializeItem(BaseItem item)
    {
        var save = new ItemSaveData
        {
            DefinitionId = item.DefinitionId,
            Durability = item.ItemDurability,
        };

        // Save quantity for stackable items
        if (item is StackableItem stackable)
        {
            save.Quantity = stackable.Quantity;
        }

        // Procedural items (no DefinitionId) need full serialization
        if (item.DefinitionId == null)
        {
            save.FullItemJson = SerializeFullItem(item);
        }

        return save;
    }

    private static string SerializeFullItem(BaseItem item)
    {
        var dict = new Dictionary<string, object?>
        {
            ["Type"] = item.GetType().Name,
            ["Name"] = item.Name,
            ["Value"] = item.Value,
            ["Rarity"] = item.Rarity,
            ["ItemDurability"] = item.ItemDurability,
            ["Weight"] = item.Weight,
        };

        switch (item)
        {
            case Weapon w:
                dict["BaseDamage"] = w.BaseDamage;
                dict["WeaponType"] = w.WeaponType;
                dict["AttackSpeed"] = w.AttackSpeed;
                dict["Range"] = w.Range;
                dict["RequiredLevel"] = w.RequiredLevel;
                dict["EquipmentType"] = w.EquipmentType;
                dict["Bonuses"] = SerializeBonuses(w.Bonuses);
                break;
            case Armor a:
                dict["BaseDefense"] = a.BaseDefense;
                dict["ArmorSlot"] = a.ArmorSlot;
                dict["Weight"] = a.Weight;
                dict["BlockChance"] = a.BlockChance;
                dict["RequiredLevel"] = a.RequiredLevel;
                dict["EquipmentType"] = a.EquipmentType;
                dict["Bonuses"] = SerializeBonuses(a.Bonuses);
                break;
            case Accessory acc:
                dict["AccessorySlot"] = acc.AccessorySlot;
                dict["MaxEquipped"] = acc.MaxEquipped;
                dict["RequiredLevel"] = acc.RequiredLevel;
                dict["EquipmentType"] = acc.EquipmentType;
                dict["Bonuses"] = SerializeBonuses(acc.Bonuses);
                break;
            case DamageItem di:
                dict["BaseDamage"] = di.BaseDamage;
                dict["DamageType"] = di.DamageType;
                dict["AreaOfEffect"] = di.AreaOfEffect;
                dict["ConsumableType"] = di.ConsumableType;
                dict["EffectDescription"] = di.EffectDescription;
                dict["Quantity"] = di.Quantity;
                dict["MaxStacks"] = di.MaxStacks;
                break;
            case Food f:
                dict["RegenerationRate"] = f.RegenerationRate;
                dict["RegenerationDuration"] = f.RegenerationDuration;
                dict["FoodType"] = f.FoodType;
                dict["ConsumableType"] = f.ConsumableType;
                dict["EffectDescription"] = f.EffectDescription;
                dict["Quantity"] = f.Quantity;
                dict["MaxStacks"] = f.MaxStacks;
                break;
            case Potion p:
                dict["PotionType"] = p.PotionType;
                dict["Cooldown"] = p.Cooldown;
                dict["ConsumableType"] = p.ConsumableType;
                dict["EffectDescription"] = p.EffectDescription;
                dict["Quantity"] = p.Quantity;
                dict["MaxStacks"] = p.MaxStacks;
                dict["Bonuses"] = SerializeBonuses(p.Effects);
                break;
            case MobDrop md:
                dict["SourceMonster"] = md.SourceMonster;
                dict["DropRate"] = md.DropRate;
                dict["IsBossDrop"] = md.IsBossDrop;
                dict["MaterialType"] = md.MaterialType;
                dict["CraftingTier"] = md.CraftingTier;
                dict["Quantity"] = md.Quantity;
                dict["MaxStacks"] = md.MaxStacks;
                break;
        }

        return JsonSerializer.Serialize(dict, JsonOpts);
    }

    private static List<Dictionary<string, object>> SerializeBonuses(StatModifierCollection bonuses)
    {
        var list = new List<Dictionary<string, object>>();
        foreach (var effect in bonuses.Effects)
        {
            list.Add(new Dictionary<string, object>
            {
                ["Type"] = effect.Type.ToString(),
                ["Potency"] = effect.Potency,
                ["Duration"] = effect.Duration,
                ["IsPercentage"] = effect.IsPercentage,
            });
        }
        return list;
    }

    // ── Item deserialization ───────────────────────────────────────

    /// <summary>
    /// Recreates a BaseItem from save data. Uses ItemRegistry for definition items,
    /// full deserialization for procedural items.
    /// </summary>
    public static BaseItem? DeserializeItem(ItemSaveData save)
    {
        BaseItem? item;

        if (save.DefinitionId != null)
        {
            // Definition item — recreate from registry, then apply saved state
            item = ItemRegistry.Create(save.DefinitionId);
            if (item == null) return null;

            item.ItemDurability = save.Durability;
            if (item is StackableItem stackable && save.Quantity.HasValue)
            {
                stackable.Quantity = save.Quantity.Value;
            }
        }
        else if (save.FullItemJson != null)
        {
            // Procedural item — full deserialization
            item = DeserializeFullItem(save.FullItemJson);
        }
        else
        {
            return null;
        }

        return item;
    }

    private static BaseItem? DeserializeFullItem(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string typeName = root.GetProperty("Type").GetString() ?? "";
            string? name = root.TryGetProperty("Name", out var n) ? n.GetString() : null;
            int value = root.TryGetProperty("Value", out var v) ? v.GetInt32() : 0;
            string? rarity = root.TryGetProperty("Rarity", out var r) ? r.GetString() : null;
            int durability = root.TryGetProperty("ItemDurability", out var d) ? d.GetInt32() : 0;
            int weight = root.TryGetProperty("Weight", out var w) ? w.GetInt32() : 1;

            BaseItem? item = typeName switch
            {
                "Weapon" => DeserializeWeapon(root, name, value, rarity, durability, weight),
                "Armor" => DeserializeArmor(root, name, value, rarity, durability, weight),
                "Accessory" => DeserializeAccessory(root, name, value, rarity, durability, weight),
                "DamageItem" => DeserializeDamageItem(root, name, value, rarity, durability, weight),
                "Food" => DeserializeFood(root, name, value, rarity, durability, weight),
                "Potion" => DeserializePotion(root, name, value, rarity, durability, weight),
                "MobDrop" => DeserializeMobDrop(root, name, value, rarity, durability, weight),
                _ => null
            };

            return item;
        }
        catch
        {
            return null;
        }
    }

    private static Weapon DeserializeWeapon(JsonElement root, string? name, int value, string? rarity, int durability, int weight) => new()
    {
        Name = name, Value = value, Rarity = rarity, ItemDurability = durability, Weight = weight,
        BaseDamage = root.TryGetProperty("BaseDamage", out var bd) ? bd.GetInt32() : 0,
        WeaponType = root.TryGetProperty("WeaponType", out var wt) ? wt.GetString() : null,
        AttackSpeed = root.TryGetProperty("AttackSpeed", out var a) ? a.GetInt32() : 1,
        Range = root.TryGetProperty("Range", out var rng) ? rng.GetInt32() : 1,
        RequiredLevel = root.TryGetProperty("RequiredLevel", out var rl) ? rl.GetInt32() : 1,
        EquipmentType = root.TryGetProperty("EquipmentType", out var et) ? et.GetString() : "Weapon",
        Bonuses = DeserializeBonuses(root),
    };

    private static Armor DeserializeArmor(JsonElement root, string? name, int value, string? rarity, int durability, int weight) => new()
    {
        Name = name, Value = value, Rarity = rarity, ItemDurability = durability,
        BaseDefense = root.TryGetProperty("BaseDefense", out var bd) ? bd.GetInt32() : 0,
        ArmorSlot = root.TryGetProperty("ArmorSlot", out var slot) ? slot.GetString() : null,
        Weight = root.TryGetProperty("Weight", out var w) ? w.GetInt32() : weight,
        BlockChance = root.TryGetProperty("BlockChance", out var bc) ? bc.GetInt32() : 0,
        RequiredLevel = root.TryGetProperty("RequiredLevel", out var rl) ? rl.GetInt32() : 1,
        EquipmentType = root.TryGetProperty("EquipmentType", out var et) ? et.GetString() : "Armor",
        Bonuses = DeserializeBonuses(root),
    };

    private static Accessory DeserializeAccessory(JsonElement root, string? name, int value, string? rarity, int durability, int weight) => new()
    {
        Name = name, Value = value, Rarity = rarity, ItemDurability = durability, Weight = weight,
        AccessorySlot = root.TryGetProperty("AccessorySlot", out var slot) ? slot.GetString() : null,
        MaxEquipped = root.TryGetProperty("MaxEquipped", out var me) ? me.GetInt32() : 1,
        RequiredLevel = root.TryGetProperty("RequiredLevel", out var rl) ? rl.GetInt32() : 1,
        EquipmentType = root.TryGetProperty("EquipmentType", out var et) ? et.GetString() : "Accessory",
        Bonuses = DeserializeBonuses(root),
    };

    private static DamageItem DeserializeDamageItem(JsonElement root, string? name, int value, string? rarity, int durability, int weight) => new()
    {
        Name = name, Value = value, Rarity = rarity, ItemDurability = durability, Weight = weight,
        BaseDamage = root.TryGetProperty("BaseDamage", out var bd) ? bd.GetInt32() : 0,
        DamageType = root.TryGetProperty("DamageType", out var dt) ? dt.GetString() : null,
        AreaOfEffect = root.TryGetProperty("AreaOfEffect", out var aoe) ? aoe.GetInt32() : 0,
        ConsumableType = root.TryGetProperty("ConsumableType", out var ct) ? ct.GetString() : null,
        EffectDescription = root.TryGetProperty("EffectDescription", out var ed) ? ed.GetString() : null,
        Quantity = root.TryGetProperty("Quantity", out var q) ? q.GetInt32() : 1,
        MaxStacks = root.TryGetProperty("MaxStacks", out var ms) ? ms.GetInt32() : 20,
    };

    private static Food DeserializeFood(JsonElement root, string? name, int value, string? rarity, int durability, int weight) => new()
    {
        Name = name, Value = value, Rarity = rarity, ItemDurability = durability, Weight = weight,
        RegenerationRate = root.TryGetProperty("RegenerationRate", out var rr) ? rr.GetInt32() : 0,
        RegenerationDuration = root.TryGetProperty("RegenerationDuration", out var rd) ? rd.GetInt32() : 0,
        FoodType = root.TryGetProperty("FoodType", out var ft) ? ft.GetString() : null,
        ConsumableType = root.TryGetProperty("ConsumableType", out var ct) ? ct.GetString() : null,
        EffectDescription = root.TryGetProperty("EffectDescription", out var ed) ? ed.GetString() : null,
        Quantity = root.TryGetProperty("Quantity", out var q) ? q.GetInt32() : 1,
        MaxStacks = root.TryGetProperty("MaxStacks", out var ms) ? ms.GetInt32() : 99,
    };

    private static Potion DeserializePotion(JsonElement root, string? name, int value, string? rarity, int durability, int weight) => new()
    {
        Name = name, Value = value, Rarity = rarity, ItemDurability = durability, Weight = weight,
        PotionType = root.TryGetProperty("PotionType", out var pt) ? pt.GetString() : null,
        Cooldown = root.TryGetProperty("Cooldown", out var cd) ? cd.GetInt32() : 0,
        ConsumableType = root.TryGetProperty("ConsumableType", out var ct) ? ct.GetString() : null,
        EffectDescription = root.TryGetProperty("EffectDescription", out var ed) ? ed.GetString() : null,
        Quantity = root.TryGetProperty("Quantity", out var q) ? q.GetInt32() : 1,
        MaxStacks = root.TryGetProperty("MaxStacks", out var ms) ? ms.GetInt32() : 99,
        Effects = DeserializeConsumableEffects(root),
    };

    private static MobDrop DeserializeMobDrop(JsonElement root, string? name, int value, string? rarity, int durability, int weight) => new()
    {
        Name = name, Value = value, Rarity = rarity, ItemDurability = durability, Weight = weight,
        SourceMonster = root.TryGetProperty("SourceMonster", out var sm) ? sm.GetString() : null,
        DropRate = root.TryGetProperty("DropRate", out var dr) ? dr.GetSingle() : 0,
        IsBossDrop = root.TryGetProperty("IsBossDrop", out var ibd) && ibd.GetBoolean(),
        MaterialType = root.TryGetProperty("MaterialType", out var mt) ? mt.GetString() : null,
        CraftingTier = root.TryGetProperty("CraftingTier", out var ct) ? ct.GetInt32() : 1,
        Quantity = root.TryGetProperty("Quantity", out var q) ? q.GetInt32() : 1,
        MaxStacks = root.TryGetProperty("MaxStacks", out var ms) ? ms.GetInt32() : 99,
    };

    private static StatModifierCollection DeserializeBonuses(JsonElement root)
    {
        var collection = new StatModifierCollection();
        if (!root.TryGetProperty("Bonuses", out var bonuses)) return collection;

        foreach (var effect in bonuses.EnumerateArray())
        {
            if (Enum.TryParse<StatType>(effect.GetProperty("Type").GetString(), out var type))
            {
                collection.Add(
                    type,
                    effect.GetProperty("Potency").GetInt32(),
                    effect.TryGetProperty("Duration", out var dur) ? dur.GetInt32() : 0,
                    effect.TryGetProperty("IsPercentage", out var pct) && pct.GetBoolean()
                );
            }
        }
        return collection;
    }

    private static StatModifierCollection DeserializeConsumableEffects(JsonElement root)
    {
        var collection = new StatModifierCollection();
        if (!root.TryGetProperty("Bonuses", out var bonuses)) return collection;

        foreach (var effect in bonuses.EnumerateArray())
        {
            if (Enum.TryParse<StatType>(effect.GetProperty("Type").GetString(), out var type))
            {
                collection.Add(
                    type,
                    effect.GetProperty("Potency").GetInt32(),
                    effect.TryGetProperty("Duration", out var dur) ? dur.GetInt32() : 0,
                    effect.TryGetProperty("IsPercentage", out var pct) && pct.GetBoolean()
                );
            }
        }
        return collection;
    }
}
