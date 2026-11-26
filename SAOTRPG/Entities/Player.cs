using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Definitions;
using SAOTRPG.Items.Definitions.Weapons;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Inventory.Logging;
using SAOTRPG.Systems.Persistence;
using SAOTRPG.Systems.RunState;
using SAOTRPG.Systems.Skills;
using SAOTRPG.UI.GameLog;
using PlayerInventory = SAOTRPG.Inventory.Core.Inventory;
using EquipmentSlot = SAOTRPG.Inventory.Core.EquipmentSlot;

namespace SAOTRPG.Entities
{
    // Player: Entity + identity, currency, inventory, XP/level, SP, equipment-aware stats.
    public partial class Player : Entity
    {
        public override char Symbol { get; protected set; } = '@';
        public override Color SymbolColor { get; protected set; } = Color.BrightYellow;

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        public int CurrentExperience { get; set; }
        public int ExperienceRequired => (BaseExperienceRequired + (BaseExperienceRequired * Level));
        private const int BaseExperienceRequired = 100;

        // MaxHP = 100 + Vit*10 + Sleep life-skill milestone bonus (read-path wide, one source of truth).
        public new int MaxHealth => 100 + (Vitality * 10) + LifeSkills.SleepMaxHpBonus();

        public int ColOnHand { get; set; }
        public int SkillPoints { get; set; }

        // Backpack + equipped gear.
        public PlayerInventory Inventory { get; private set; } = null!;

        // Life Skills — per-skill level/XP with milestone bonuses folded into stats. Eager-init so reads skip null checks.
        public LifeSkillSystem LifeSkills { get; private set; } = new();

        // Active equipped title (Milestone Id with RewardType.EquippableTitle).
        // Unlock set itself lives in LifetimeStats.UnlockedMilestones — single
        // bucket, persists across permadeath.
        public string? ActiveTitleId { get; set; }

        // Karma ∈ [-100,+100], default 0. Adjusted via KarmaSystem.Adjust; drives NPC dialogue, shop prices, TOB guard patrol.
        public int Karma { get; set; }

        // 10-slot consumable quickbar bound to keys 1-0 (D1..D0). Per-player,
        // persisted via SaveData. Manual bind via Shift+N; auto-fill on pickup.
        public QuickbarState Quickbar { get; set; } = new();

        // Active guild; single-membership (joining forces leaving current).
        public Systems.Story.Faction ActiveGuildId { get; set; } = Systems.Story.Faction.None;

        // Player-founded guild — only meaningful when ActiveGuildId == Faction.PlayerGuild.
        public string? FoundedGuildName { get; set; }
        public int FoundedGuildPerk { get; set; }

        public int Attack => BaseAttack + (Strength * 2) + Inventory.GetTotalEquipmentBonus(StatType.Attack);
        public int Defense => BaseDefense + (Endurance * 2)
            + Inventory.GetTotalEquipmentBonus(StatType.Defense)
            + (LifeSkills.WalkingEnduranceBonus() * 2);
        public int Speed => BaseSpeed + (Agility * 2)
            + Inventory.GetTotalEquipmentBonus(StatType.Speed)
            + LifeSkills.RunningSpeedBonus();
        public int SkillDamage => BaseSkillDamage + (Intelligence * 2) + Inventory.GetTotalEquipmentBonus(StatType.SkillDamage);

        // Gear stats live only in the equipment cache (broken pieces skipped, set bonuses folded in).
        // Attributes read and write the EFFECTIVE value; the setter stores it net of gear, so `+=`
        // moves only the base and a saved effective value round-trips once gear is back on.
        private int Gear(StatType stat) => Inventory?.GetTotalEquipmentBonus(stat) ?? 0;
        public new int Strength { get => base.Strength + Gear(StatType.Strength); set => base.Strength = value - Gear(StatType.Strength); }
        public new int Vitality { get => base.Vitality + Gear(StatType.Vitality); set => base.Vitality = value - Gear(StatType.Vitality); }
        public new int Endurance { get => base.Endurance + Gear(StatType.Endurance); set => base.Endurance = value - Gear(StatType.Endurance); }
        public new int Dexterity { get => base.Dexterity + Gear(StatType.Dexterity); set => base.Dexterity = value - Gear(StatType.Dexterity); }
        public new int Agility { get => base.Agility + Gear(StatType.Agility); set => base.Agility = value - Gear(StatType.Agility); }
        public new int Intelligence { get => base.Intelligence + Gear(StatType.Intelligence); set => base.Intelligence = value - Gear(StatType.Intelligence); }
        // Entity's versions read the raw Dexterity, so the player's are restated over the effective one.
        public new int CriticalRate => BaseCriticalRate + Gear(StatType.CritRate) + (Dexterity / 2);
        public new int CriticalHitDamage => BaseCriticalHitDamage + Dexterity;

        // Katana Iaijutsu fork: +25% damage on the first strike against each enemy per floor.
        // Combat layer flips on encounter start.
        public bool KatanaIaijutsuActive { get; set; }
        // Bow Marksman Eye fork: extra effective range overflow on bow shots.
        public int BowRangeOverflow { get; set; }

        // New player: Iron Sword + Health Potion + a lit Wooden Torch, full HP.
        public static Player CreateNewPlayer(string firstName, string lastName, string gender, IGameLog log, IInventoryLogger? inventoryLogger = null)
        {
            var player = new Player
            {
                FirstName = firstName, LastName = lastName, Gender = gender,
                Title = "Adventurer", Id = RunRng.Next(10000, 99999),
                Level = 1, CurrentExperience = 0, ColOnHand = 1000, SkillPoints = 10
            };
            player._log = log;
            player.Inventory = new PlayerInventory(logger: inventoryLogger);
            // Auto-fill quickbar on every new pickup of a consumable type.
            player.Inventory.Events.ItemAdded += (_, e) => player.Quickbar.TryAutoBind(e.Item);
            player.CurrentHealth = player.MaxHealth;
            player.Inventory.AddItem(OneHandedSwordDefinitions.CreateIronSword());
            player.Inventory.AddItem(PotionDefinitions.CreateHealthPotion());
            // Equipped, not merely carried. The player casts no light of their own, so a run that
            // began with the torch sitting in the backpack would open on a dark screen and read as
            // a rendering fault rather than as a mechanic.
            var torch = Items.Definitions.TorchDefinitions.CreateWoodenTorch();
            player.Inventory.AddItem(torch);
            player.Inventory.Equip(torch, player);
            return player;
        }

        // Reconstructs a player from a save file, restoring inventory and equipped items.
        public static Player LoadFromSave(SaveData save, IGameLog log, IInventoryLogger? inventoryLogger = null)
        {
            var player = new Player
            {
                FirstName = save.FirstName, LastName = save.LastName,
                Gender = save.Gender, Title = save.Title,
                Id = save.PlayerId, Level = save.Level,
                CurrentExperience = save.CurrentExperience,
                CurrentHealth = save.CurrentHealth,
                ColOnHand = save.ColOnHand, SkillPoints = save.SkillPoints,
                BaseAttack = save.BaseAttack, BaseDefense = save.BaseDefense,
                BaseSpeed = save.BaseSpeed, BaseSkillDamage = save.BaseSkillDamage,
                BaseCriticalRate = save.BaseCriticalRate,
                BaseCriticalHitDamage = save.BaseCriticalHitDamage,
            };
            player._log = log;
            player.Inventory = new PlayerInventory(logger: inventoryLogger);
            player.Inventory.Events.ItemAdded += (_, e) => player.Quickbar.TryAutoBind(e.Item);

            foreach (var itemData in save.InventoryItems)
            {
                var item = SAOTRPG.Systems.Persistence.SaveManager.DeserializeItem(itemData);
                if (item != null) player.Inventory.AddItem(item);
            }

            foreach (var kvp in save.EquippedItems)
            {
                if (Enum.TryParse<EquipmentSlot>(kvp.Key, out var slot))
                {
                    var item = SAOTRPG.Systems.Persistence.SaveManager.DeserializeItem(kvp.Value);
                    if (item is Items.Equipment.EquipmentBase equipment)
                        player.Inventory.ForceEquipForLoad(slot, equipment);
                }
            }

            // Saved attributes are effective values, so they are set only once the gear is worn.
            player.Strength = save.Strength; player.Vitality = save.Vitality;
            player.Endurance = save.Endurance; player.Dexterity = save.Dexterity;
            player.Agility = save.Agility; player.Intelligence = save.Intelligence;

            // Life Skills: hydrate Level + CurrentXp; missing entries keep default L1/0 (legacy saves load cleanly).
            if (save.LifeSkills != null)
            {
                foreach (var kvp in save.LifeSkills)
                {
                    if (Enum.TryParse<LifeSkillType>(kvp.Key, out var skillType)
                        && player.LifeSkills.Skills.TryGetValue(skillType, out var state))
                    {
                        state.Level = Math.Clamp(kvp.Value.Level, 1, SAOTRPG.Systems.Skills.LifeSkillSystem.MaxLevel);
                        state.CurrentXp = Math.Max(0, kvp.Value.CurrentXp);
                    }
                }
            }

            // Active title id rehydrated; base stats already contain the baked-in
            // bonus from prior session, so storing the id only avoids double-apply.
            // The unlock set itself lives in LifetimeStats.UnlockedMilestones.
            if (!string.IsNullOrEmpty(save.ActiveTitleId)
                && SAOTRPG.Systems.Persistence.LifetimeStats.IsMilestoneUnlocked(save.ActiveTitleId))
            {
                player.ActiveTitleId = save.ActiveTitleId;
            }

            // Karma+Guild hydration; "AincradLiberationSquad" migrates to AincradLiberationForce.
            player.Karma = Math.Clamp(save.Karma, SAOTRPG.Systems.Social.KarmaSystem.Min, SAOTRPG.Systems.Social.KarmaSystem.Max);
            string guildName = save.ActiveGuildId ?? "None";
            if (guildName == "AincradLiberationSquad") guildName = "AincradLiberationForce";
            if (Enum.TryParse<Systems.Story.Faction>(guildName, out var fac))
                player.ActiveGuildId = fac;
            else
                player.ActiveGuildId = Systems.Story.Faction.None;
            player.FoundedGuildName = save.FoundedGuildName;
            player.FoundedGuildPerk = save.FoundedGuildPerk;
            // Guild perk already baked in at prior-session Join-time; no re-apply (mirrors Title hydration).

            // Quickbar — hydrate 10-slot DefinitionId array. Legacy saves
            // with empty list stay at defaults (all null = empty).
            if (save.QuickbarSlotDefIds != null)
            {
                for (int qi = 0; qi < SAOTRPG.Systems.RunState.QuickbarState.SlotCount
                    && qi < save.QuickbarSlotDefIds.Count; qi++)
                {
                    player.Quickbar.SlotItemDefIds[qi] = save.QuickbarSlotDefIds[qi];
                }
            }

            return player;
        }

        public bool EquipItem(EquipmentBase equipment) => Inventory.Equip(equipment, this);
        public void UseItem(Consumable consumable) => Inventory.UseConsumable(consumable, this);

        // Active-title display name (fallback to Player.Title).
        public string EffectiveTitleName()
        {
            if (ActiveTitleId != null
                && SAOTRPG.Systems.Progression.MilestoneRegistry.ById.TryGetValue(ActiveTitleId, out var ms)
                && ms.Reward == SAOTRPG.Systems.Progression.RewardType.EquippableTitle)
                return ms.Name;
            return Title;
        }
    }
}
