namespace SAOTRPG.Systems;

// Root save data DTO — serialized to JSON.
public class SaveData
{
    // Schema version — bump when save format changes to enable migration.
    public int SaveVersion { get; set; } = 1;
    public DateTime Timestamp { get; set; }
    public long PlayTimeSeconds { get; set; }

    // Player identity
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Gender { get; set; } = "";
    public string Title { get; set; } = "";
    public int PlayerId { get; set; }

    // Player progression
    public int Level { get; set; }
    public int CurrentExperience { get; set; }
    public int CurrentHealth { get; set; }
    public int ColOnHand { get; set; }
    public int SkillPoints { get; set; }

    // Player attributes
    public int Strength { get; set; }
    public int Vitality { get; set; }
    public int Endurance { get; set; }
    public int Dexterity { get; set; }
    public int Agility { get; set; }
    public int Intelligence { get; set; }

    // Player base combat stats (includes baked-in equipment bonuses)
    public int BaseAttack { get; set; }
    public int BaseDefense { get; set; }
    public int BaseSpeed { get; set; }
    public int BaseSkillDamage { get; set; }
    public int BaseCriticalRate { get; set; }
    public int BaseCriticalHitDamage { get; set; }

    // Inventory
    public List<ItemSaveData> InventoryItems { get; set; } = [];
    // Equipped gear keyed by slot name (e.g. "Weapon", "Chest").
    public Dictionary<string, ItemSaveData> EquippedItems { get; set; } = [];

    // Turn manager state
    public int TurnCount { get; set; }
    public int KillCount { get; set; }
    public int TotalColEarned { get; set; }
    public int CurrentFloor { get; set; }
    // Difficulty tier index (0 = Easy … 3 = Nightmare).
    public int Difficulty { get; set; }
    // Current hunger level (decreases over time).
    public int Satiety { get; set; }
    // Consecutive kills without taking damage.
    public int KillStreak { get; set; }
    public Dictionary<string, int> WeaponKills { get; set; } = [];

    // Per-weapon fork picks at L25/50/75/100. int[] length-4: 0=unpicked,
    // 1|2=chosen option. Empty/null on legacy saves.
    public Dictionary<string, int[]> WeaponProficiencyForks { get; set; } = [];

    // Turns since last rest (exhaustion counter).
    public int RestCounter { get; set; }

    // Bounty
    public string? BountyTarget { get; set; }
    public int BountyKillsNeeded { get; set; }
    public int BountyKillsCurrent { get; set; }
    public int BountyRewardCol { get; set; }
    public int BountyRewardXp { get; set; }
    public bool BountyComplete { get; set; }

    // Status effects (active at save time)
    public int PoisonTurnsLeft { get; set; }
    public int BleedTurnsLeft { get; set; }
    public int StunTurnsLeft { get; set; }
    // Remaining turns of slow (halves dodge chance).
    public int SlowTurnsLeft { get; set; }
    public int ShrineBuffTurns { get; set; }
    public int LevelUpBuffTurns { get; set; }

    // Sword Skills
    // IDs of the 4 equipped sword skills (null entries = empty slots).
    public List<string?> EquippedSkillIds { get; set; } = [];
    // Skill cooldowns at save time (skillId → remaining turns).
    public Dictionary<string, int> SkillCooldowns { get; set; } = [];

    // Indices of discovered lore stone entries (0-based into FlavorText.LoreStoneEntries).
    public List<int> DiscoveredLore { get; set; } = [];

    public List<string> UnlockedAchievements { get; set; } = [];

    // IDs of tutorial tips already shown (so they don't repeat on reload).
    public List<string> SeenTutorialTips { get; set; } = [];

    // Quests
    public List<Quest> ActiveQuests { get; set; } = [];
    public List<Quest> CompletedQuests { get; set; } = [];
    public string? PinnedQuestId { get; set; }

    // Party members
    public List<AllySaveData> PartyMembers { get; set; } = [];

    // Story / narrative state
    public List<string> FiredStoryEventIds { get; set; } = [];
    public List<string> StoryFlags { get; set; } = [];
    public Dictionary<string, int> FactionReputation { get; set; } = [];

    // Unique skills (Cardinal-selected abilities, all unlockable)
    public List<string> UnlockedUniqueSkills { get; set; } = [];
    public int TrapsDisarmed { get; set; }

    // Field bosses defeated this run (ids like "frost_dragon_f48")
    public List<string> DefeatedFieldBosses { get; set; } = [];

    // Active Run Modifiers (FB-564) — set at run start, fixed per run
    public List<string> ActiveRunModifiers { get; set; } = [];

    // IM Shop Tiering — highest boss-cleared floor. 0 = base stock only.
    public int HighestFloorBossCleared { get; set; }

    // FB-050 Life Skills — Level + CurrentXp by enum name. Missing = L1/0.
    public Dictionary<string, LifeSkillStateSave> LifeSkills { get; set; } = new();

    // FB-058 Titles — unlocked IDs + active. Null/empty until Monument visit.
    public List<string> UnlockedTitleIds { get; set; } = new();
    public string? ActiveTitleId { get; set; }

    // FB-063 Karma/Guild. Karma [-100,+100]; ActiveGuildId = Faction enum name,
    // "None" = no guild. Founded fields apply only when ActiveGuildId=="PlayerGuild".
    public int Karma { get; set; }
    public string ActiveGuildId { get; set; } = "None";
    public string? FoundedGuildName { get; set; }
    public int FoundedGuildPerk { get; set; }

    // FB-072 Investing — per-vendor Col totals by ShopName.
    // VendorInvestmentSystem clamps to [0, MaxInvestmentPerVendor] on load.
    public Dictionary<string, int> VendorInvestments { get; set; } = new();

    // FB-466 — 10 consumable-quickbar slots (keys 1-0), DefinitionId per index.
    // Null entries = empty. Legacy saves deserialize as nulls and auto-fill
    // from the first pickup thereafter.
    public List<string?> QuickbarSlotDefIds { get; set; } = new();
}

// FB-050 — serialized form of a single life skill's live state.
public class LifeSkillStateSave
{
    public int Level { get; set; } = 1;
    public int CurrentXp { get; set; }
}

// Serialized form of a single item (backpack or equipped).
// Definition items save as a tiny ID reference; procedural items save their full data.
public class ItemSaveData
{
    public string? DefinitionId { get; set; }
    public int Durability { get; set; }
    public int? Quantity { get; set; }
    public string? FullItemJson { get; set; }
    // Enhancement level for equipment (+0 to +10). Persisted so enhanced
    // definition items don't lose their upgrades on reload.
    public int EnhancementLevel { get; set; }
    // 3 slots of socketed Ingot DefIds. Null array = no refinement data (legacy);
    // only saved when a slot is non-null to keep legacy saves clean.
    public List<string?>? RefinementSlots { get; set; }

    // IM Enhancement Ore history (parallel to EnhancementLevel; entry i = ore
    // for level i+1). Null on legacy → auto-fill N × Crimson Flame.
    public List<string>? EnhancementOreHistory { get; set; }
}

// Serialized party member data.
public class AllySaveData
{
    public string Name { get; set; } = "";
    public char Symbol { get; set; }
    public int SymbolColor { get; set; } // Terminal.Gui.Color cast to int
    public string WeaponType { get; set; } = "";
    public string Title { get; set; } = "";
    public int Level { get; set; }
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int Behavior { get; set; } // AllyBehavior enum cast to int
}

// Lightweight summary for the save slot selection UI.
public class SaveSlotSummary
{
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public int Floor { get; set; }
    public string Difficulty { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public TimeSpan PlayTime { get; set; }
}
