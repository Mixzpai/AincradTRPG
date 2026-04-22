using Terminal.Gui;
using SAOTRPG.UI;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Definitions;
using SAOTRPG.Items.Definitions.Weapons;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Inventory.Logging;
using SAOTRPG.Systems;
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
        public int BaseExperienceRequired { get; set; } = 100;

        // MaxHP = 100 + Vit*10 + Sleep life-skill milestone bonus (read-path wide, one source of truth).
        public new int MaxHealth => 100 + (Vitality * 10) + LifeSkills.SleepMaxHpBonus();

        public int ColOnHand { get; set; }
        public int SkillPoints { get; set; }

        // Backpack + equipped gear.
        public PlayerInventory Inventory { get; private set; } = null!;

        // Life Skills — per-skill level/XP with milestone bonuses folded into stats. Eager-init so reads skip null checks.
        public LifeSkillSystem LifeSkills { get; private set; } = new();

        // Titles — SetActiveTitle applies bonuses via base-stat pokes; unequip reverses.
        public HashSet<string> UnlockedTitleIds { get; set; } = new();
        public string? ActiveTitleId { get; set; }

        // Karma ∈ [-100,+100], default 0. Adjusted via KarmaSystem.Adjust; drives NPC dialogue, shop prices, TOB guard patrol.
        public int Karma { get; set; }

        // 10-slot consumable quickbar bound to keys 1-0 (D1..D0). Per-player,
        // persisted via SaveData. Manual bind via Shift+N; auto-fill on pickup.
        public Systems.QuickbarState Quickbar { get; set; } = new();

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

        // New player: Iron Sword + Health Potion, full HP.
        public static Player CreateNewPlayer(string firstName, string lastName, string gender, IGameLog log, IInventoryLogger? inventoryLogger = null)
        {
            var player = new Player
            {
                FirstName = firstName, LastName = lastName, Gender = gender,
                Title = "Adventurer", Id = Random.Shared.Next(10000, 99999),
                Level = 1, CurrentExperience = 0, ColOnHand = 1000, SkillPoints = 10
            };
            player._log = log;
            player.Inventory = new PlayerInventory(logger: inventoryLogger);
            // Auto-fill quickbar on every new pickup of a consumable type.
            player.Inventory.Events.ItemAdded += (_, e) => player.Quickbar.TryAutoBind(e.Item);
            player.CurrentHealth = player.MaxHealth;
            player.Inventory.AddItem(OneHandedSwordDefinitions.CreateIronSword());
            player.Inventory.AddItem(PotionDefinitions.CreateHealthPotion());
            return player;
        }

        // Reconstructs a player from a save file, restoring inventory and equipped items.
        public static Player LoadFromSave(Systems.SaveData save, IGameLog log, IInventoryLogger? inventoryLogger = null)
        {
            var player = new Player
            {
                FirstName = save.FirstName, LastName = save.LastName,
                Gender = save.Gender, Title = save.Title,
                Id = save.PlayerId, Level = save.Level,
                CurrentExperience = save.CurrentExperience,
                CurrentHealth = save.CurrentHealth,
                ColOnHand = save.ColOnHand, SkillPoints = save.SkillPoints,
                Strength = save.Strength, Vitality = save.Vitality,
                Endurance = save.Endurance, Dexterity = save.Dexterity,
                Agility = save.Agility, Intelligence = save.Intelligence,
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
                var item = Systems.SaveManager.DeserializeItem(itemData);
                if (item != null) player.Inventory.AddItem(item);
            }

            foreach (var kvp in save.EquippedItems)
            {
                if (Enum.TryParse<EquipmentSlot>(kvp.Key, out var slot))
                {
                    var item = Systems.SaveManager.DeserializeItem(kvp.Value);
                    if (item is Items.Equipment.EquipmentBase equipment)
                        player.Inventory.ForceEquipForLoad(slot, equipment);
                }
            }

            // Life Skills: hydrate Level + CurrentXp; missing entries keep default L1/0 (legacy saves load cleanly).
            if (save.LifeSkills != null)
            {
                foreach (var kvp in save.LifeSkills)
                {
                    if (Enum.TryParse<Systems.LifeSkillType>(kvp.Key, out var skillType)
                        && player.LifeSkills.Skills.TryGetValue(skillType, out var state))
                    {
                        state.Level = Math.Clamp(kvp.Value.Level, 1, Systems.LifeSkillSystem.MaxLevel);
                        state.CurrentXp = Math.Max(0, kvp.Value.CurrentXp);
                    }
                }
            }

            // Titles: rebuild unlocked set; base stats already include prior session's active title bonus.
            if (save.UnlockedTitleIds != null)
                player.UnlockedTitleIds = new HashSet<string>(save.UnlockedTitleIds);
            if (!string.IsNullOrEmpty(save.ActiveTitleId)
                && player.UnlockedTitleIds.Contains(save.ActiveTitleId))
            {
                // Base stats already contain baked-in title bonus — storing the id only avoids double-apply.
                player.ActiveTitleId = save.ActiveTitleId;
            }

            // Karma+Guild hydration; "AincradLiberationSquad" migrates to AincradLiberationForce.
            player.Karma = Math.Clamp(save.Karma, Systems.KarmaSystem.Min, Systems.KarmaSystem.Max);
            string guildName = save.ActiveGuildId ?? "None";
            if (guildName == "AincradLiberationSquad") guildName = "AincradLiberationForce";
            if (Enum.TryParse<Systems.Story.Faction>(guildName, out var fac))
                player.ActiveGuildId = fac;
            else
                player.ActiveGuildId = Systems.Story.Faction.None;
            player.FoundedGuildName = save.FoundedGuildName;
            player.FoundedGuildPerk = save.FoundedGuildPerk;
            // Guild perk already baked in at prior-session Join-time; no re-apply (mirrors Title hydration).

            // FB-466 Quickbar — hydrate 10-slot DefinitionId array. Legacy
            // saves with empty list stay at defaults (all null = empty).
            if (save.QuickbarSlotDefIds != null)
            {
                for (int qi = 0; qi < Systems.QuickbarState.SlotCount
                    && qi < save.QuickbarSlotDefIds.Count; qi++)
                {
                    player.Quickbar.SlotItemDefIds[qi] = save.QuickbarSlotDefIds[qi];
                }
            }

            return player;
        }

        public bool EquipItem(EquipmentBase equipment) => Inventory.Equip(equipment, this);
        public bool UnequipItem(EquipmentSlot slot) => Inventory.Unequip(slot, this);
        public void UseItem(Consumable consumable) => Inventory.UseConsumable(consumable, this);

        // Active-title display name (fallback to Player.Title). Sidebar HUD formatted for 48-col panel.
        public string EffectiveTitleName()
        {
            if (ActiveTitleId != null
                && Systems.TitleSystem.Titles.TryGetValue(ActiveTitleId, out var def))
                return def.DisplayName;
            return Title;
        }

        public string GetStatsDisplay()
        {
            string sp = SkillPoints > 0 ? $"  ({SkillPoints} SP)" : "";
            return
$@"{FirstName} {LastName}
{EffectiveTitleName()}  Lv.{Level}{sp}

ATK {Attack,-4} DEF {Defense,-4} SPD {Speed}
CRT {CriticalRate}%   CD +{CriticalHitDamage,-3} SD {SkillDamage}

VIT {Vitality,-3} STR {Strength,-3} END {Endurance,-3}
DEX {Dexterity,-3} AGI {Agility,-3} INT {Intelligence,-3}";
        }
    }
}
