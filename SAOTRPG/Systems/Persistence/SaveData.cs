using SAOTRPG.Systems.Social;
using SAOTRPG.Items;

namespace SAOTRPG.Systems.Persistence;

// Root save data DTO — serialized to JSON.
public class SaveData
{
    public DateTime Timestamp { get; set; }
    public long PlayTimeSeconds { get; set; }

    // Persisted master seed so a load regenerates the same world — the save stores the seed,
    // not the map. -1 only ever means "never written"; SaveManager passes the value through
    // unchecked, because every save written carries a real seed.
    public int GlobalSeed { get; set; } = -1;

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

    // Player base combat stats, gear excluded (gear counts from the equipped items)
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
    // Move points left in the current turn (TurnManager.PlayerMoves).
    public int PlayerMoves { get; set; } = 100;
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
    // 1|2=chosen option. Empty when no weapon has a fork.
    public Dictionary<string, int[]> WeaponProficiencyForks { get; set; } = [];

    // Level-up talents offered but not yet taken. Each entry keeps the perk ids it offered so
    // resuming re-presents the same three rather than rolling again.
    public List<PendingTalentSave> PendingTalents { get; set; } = [];

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
    // The magnitude half of each timed effect above. Both halves are saved, or a loaded run
    // would count down a blessing worth nothing and a poison dealing nothing.
    public int ShrineBuffAmount { get; set; }
    public int LevelUpBuffAmount { get; set; }
    public int PoisonDamagePerTick { get; set; }
    public int BleedDamagePerTick { get; set; }
    public int InvisibilityTurnsLeft { get; set; }
    // Active timed potion buffs. Persisted so a save/load cannot launder a buff into a permanent
    // stat.
    public List<TimedBuffSave> TimedBuffs { get; set; } = new();
    // Active food regen buff. 0 = inactive.
    public int FoodRegenRate { get; set; }
    public int FoodRegenTurnsLeft { get; set; }

    // Sword Skills
    // IDs of the 4 equipped sword skills (null entries = empty slots).
    public List<string?> EquippedSkillIds { get; set; } = [];
    // Skill cooldowns at save time (skillId → remaining turns).
    public Dictionary<string, int> SkillCooldowns { get; set; } = [];

    // Indices of discovered lore stone entries (0-based into FlavorText.LoreStoneEntries).
    public List<int> DiscoveredLore { get; set; } = [];

    // IDs of tutorial tips already shown (so they don't repeat on reload).
    public List<string> SeenTutorialTips { get; set; } = [];

    // Quests
    public List<Quest> ActiveQuests { get; set; } = [];
    public List<Quest> CompletedQuests { get; set; } = [];
    public string? PinnedQuestId { get; set; }

    // Per-run completion tallies for QuestSubCategory-tagged turn-ins. Reset on
    // new game (save replaced). Drive progressive toasts and milestone unlocks.
    public int IfImplementQuestsCompletedThisRun { get; set; }
    public int HfMissionsCompletedThisRun { get; set; }

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

    // Active Run Modifiers — set at run start, fixed per run
    public List<string> ActiveRunModifiers { get; set; } = [];

    // Shop tiering — highest boss-cleared floor. 0 = base stock only.
    public int HighestFloorBossCleared { get; set; }

    // Life Skills — Level + CurrentXp by enum name. Missing = L1/0.
    public Dictionary<string, LifeSkillStateSave> LifeSkills { get; set; } = new();

    // Active equipped title (Milestone Id with RewardType.EquippableTitle).
    // Null = no title equipped. Unlock set itself lives in lifetime_stats.json.
    public string? ActiveTitleId { get; set; }

    // Karma/Guild. Karma [-100,+100]; ActiveGuildId = Faction enum name,
    // "None" = no guild. Founded fields apply only when ActiveGuildId=="PlayerGuild".
    public int Karma { get; set; }
    public string ActiveGuildId { get; set; } = "None";
    public string? FoundedGuildName { get; set; }
    public int FoundedGuildPerk { get; set; }

    // Investing — per-vendor Col totals by ShopName.
    // VendorInvestmentSystem clamps to [0, MaxInvestmentPerVendor] on load.
    public Dictionary<string, int> VendorInvestments { get; set; } = new();

    // 10 consumable-quickbar slots (keys 1-0), DefinitionId per index.
    // Null entries = empty; an empty slot auto-fills from a later pickup.
    public List<string?> QuickbarSlotDefIds { get; set; } = new();

    // Per-prefab-name placement counts as of the current floor's start. Enforces MAX_PER_GAME directive.
    // Missing → System.Text.Json yields an empty dict.
    public Dictionary<string, int> PrefabUseCounts { get; set; } = new();

    // Current-floor mining vein strike counters. Null is treated as empty
    // (re-seeds via DefaultStrikesForTile on first strike).
    public List<VeinStrikeEntry>? VeinStrikes { get; set; }

    // Per-floor boss-clear flags so the Player Guide's drop reveal doesn't leak floors the
    // player skipped via teleport. Null = empty set.
    public HashSet<int>? DefeatedFloorBosses { get; set; }
}

// JSON-friendly tile-strike pair. Tuples don't round-trip cleanly via JsonElement;
// named props avoid that pitfall.
public class VeinStrikeEntry
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Strikes { get; set; }
}

// Serialized form of a single life skill's live state.
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
    // 3 slots of socketed Ingot DefIds. Null array = no refinement data;
    // only saved when a slot is non-null.
    public List<string?>? RefinementSlots { get; set; }

    // Enhancement Ore history (parallel to EnhancementLevel; entry i = ore
    // for level i+1). Null → auto-fill N × Crimson Flame.
    public List<string>? EnhancementOreHistory { get; set; }

    // Dual-wield paired flag. Null falls back to the weapon-definition value;
    // runtime mutators (Corruption Stone transforms) round-trip via the saved flag.
    public bool? IsDualWieldPaired { get; set; }

    // Divine Awakening level (◈1-◈3). Null when unawakened,
    // written only when > 0. Deserialize re-folds the ATK bonus via ComputeBonusAttack.
    public int? AwakeningLevel { get; set; }

    // Declared durability ceiling (pickaxe, torch). Null when the item declares none.
    public int? MaxDurability { get; set; }

    // A registered weapon's reforged base effects, replacing the definition's on load. Null if never reforged.
    public List<StatEffect>? ReforgedBonuses { get; set; }

    // True when Bonuses already include enhancement/refinement/awakening.
    // DefId items leave null (runtime replays); FullItemJson items set true (skip replay).
    public bool? BonusesAlreadyBaked { get; set; }
}

// Serialized party member data.
public class AllySaveData
{
    public string Name { get; set; } = "";
    public char Symbol { get; set; }
    public int SymbolColor { get; set; } // Color cast to int
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

// One level-up talent still owed, with the three perk ids that level offered.
public class PendingTalentSave
{
    public int Id { get; set; }
    public int Level { get; set; }
    public List<string> PerkIds { get; set; } = [];
}

// One active timed buff, flattened for the save file.
public class TimedBuffSave
{
    public string Stat { get; set; } = "";
    public int Potency { get; set; }
    public int TurnsLeft { get; set; }
}


