namespace SAOTRPG.Systems;

/// <summary>
/// Root save data DTO — serialized to JSON.
/// </summary>
public class SaveData
{
    public int SaveVersion { get; set; } = 1;
    public DateTime Timestamp { get; set; }
    public long PlayTimeSeconds { get; set; }

    // ── Player identity ────────────────────────────────────────────
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Gender { get; set; } = "";
    public string Title { get; set; } = "";
    public int PlayerId { get; set; }

    // ── Player progression ─────────────────────────────────────────
    public int Level { get; set; }
    public int CurrentExperience { get; set; }
    public int CurrentHealth { get; set; }
    public int ColOnHand { get; set; }
    public int SkillPoints { get; set; }

    // ── Player attributes ──────────────────────────────────────────
    public int Strength { get; set; }
    public int Vitality { get; set; }
    public int Endurance { get; set; }
    public int Dexterity { get; set; }
    public int Agility { get; set; }
    public int Intelligence { get; set; }

    // ── Player base combat stats (includes baked-in equipment bonuses) ──
    public int BaseAttack { get; set; }
    public int BaseDefense { get; set; }
    public int BaseSpeed { get; set; }
    public int BaseSkillDamage { get; set; }
    public int BaseCriticalRate { get; set; }
    public int BaseCriticalHitDamage { get; set; }

    // ── Inventory ──────────────────────────────────────────────────
    public List<ItemSaveData> InventoryItems { get; set; } = [];
    public Dictionary<string, ItemSaveData> EquippedItems { get; set; } = [];

    // ── Turn manager state ─────────────────────────────────────────
    public int TurnCount { get; set; }
    public int KillCount { get; set; }
    public int CurrentFloor { get; set; }
    public int Difficulty { get; set; }
    public bool IsHardcore { get; set; }
    public int Satiety { get; set; }
    public int KillStreak { get; set; }
    public Dictionary<string, int> WeaponKills { get; set; } = [];

    // ── Status effects (active at save time) ───────────────────────
    public int PoisonTurnsLeft { get; set; }
    public int BleedTurnsLeft { get; set; }
    public int ShrineBuffTurns { get; set; }
    public int LevelUpBuffTurns { get; set; }
}

/// <summary>
/// Serialized form of a single item (backpack or equipped).
/// Definition items save as a tiny ID reference; procedural items save their full data.
/// </summary>
public class ItemSaveData
{
    public string? DefinitionId { get; set; }
    public int Durability { get; set; }
    public int? Quantity { get; set; }
    public string? FullItemJson { get; set; }
}

/// <summary>
/// Lightweight summary for the save slot selection UI.
/// </summary>
public class SaveSlotSummary
{
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public int Floor { get; set; }
    public string Difficulty { get; set; } = "";
    public bool IsHardcore { get; set; }
    public DateTime Timestamp { get; set; }
    public TimeSpan PlayTime { get; set; }
}
