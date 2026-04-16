namespace SAOTRPG.Systems;

// Quest system -- procedurally generated NPC quests with four SAO-canon types:
// Kill (elimination), Collect (fetch materials), Explore (discover a tile/area),
// and Deliver (bring an item to an NPC). Quests are given by NPCs on each floor,
// tracked in a quest log, and reward Col + XP on completion.

public enum QuestType { Kill, Collect, Explore, Deliver }
public enum QuestStatus { Active, Complete, TurnedIn }

public class Quest
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string GiverName { get; set; } = "";
    public int Floor { get; set; }
    public QuestType Type { get; set; }
    public QuestStatus Status { get; set; } = QuestStatus.Active;

    // Kill quest: target mob name and count
    public string? TargetMob { get; set; }
    public int TargetCount { get; set; }
    public int CurrentCount { get; set; }

    // Collect quest: target item name and count
    public string? TargetItem { get; set; }

    // Explore quest: target tile type to discover
    public string? TargetTileType { get; set; }

    // Rewards
    public int RewardCol { get; set; }
    public int RewardXp { get; set; }

    // Kill quest: optional weapon-type gate. If set, only kills made with that
    // weapon type count toward the quest. Used by the F2 Martial Arts trial.
    public string? RequiresWeaponType { get; set; }

    // If true, the quest survives floor changes (e.g. multi-floor story quests).
    public bool Persistent { get; set; }

    public bool IsComplete => Status == QuestStatus.Complete || Status == QuestStatus.TurnedIn;

    public string ProgressText => Type switch
    {
        QuestType.Kill => $"Slay {TargetMob}: {CurrentCount}/{TargetCount}",
        QuestType.Collect => $"Collect {TargetItem}: {CurrentCount}/{TargetCount}",
        QuestType.Explore => CurrentCount >= TargetCount ? "Area discovered!" : "Explore the target area",
        QuestType.Deliver => CurrentCount >= TargetCount ? "Ready to deliver!" : $"Obtain {TargetItem}",
        _ => "",
    };
}

// Manages active quests, generates new ones per floor, and checks completion.
public static class QuestSystem
{
    public static List<Quest> ActiveQuests { get; set; } = new();
    public static List<Quest> CompletedQuests { get; set; } = new();
    public const int MaxActiveQuests = 5;

    // Generate floor-appropriate quests. Called when entering a new floor
    // or talking to quest-giving NPCs.
    public static Quest GenerateQuest(int floor, string giverName)
        => Generate((QuestType)Random.Shared.Next(4), floor, giverName);

    private static readonly string[] KillTargets =
    {
        "Kobold", "Wolf", "Slime", "Treant", "Spider", "Lizardman", "Skeleton",
        "Golem", "Bat", "Beetle", "Serpent", "Boar", "Wasp", "Wraith", "Drake",
    };

    private static readonly string[] CollectItems =
    {
        "Beast Fang", "Raw Hide", "Chitin Shard", "Herb Bundle", "Iron Ring",
        "Scale Plate", "Bone Fragment", "Crystal Core", "Fire Crystal", "Venom Sac",
    };

    // Templates: { "Title", "Description" }. Format args vary per type -- see Generate below.
    private static readonly string[][] KillQuestTemplates =
    {
        new[] { "Pest Control", "The {0}s near the road are scaring travelers. Clear out {1} of them." },
        new[] { "Threat Elimination", "A pack of {0}s has been spotted. Eliminate {1} before they spread." },
        new[] { "Hunter's Mark", "I'll pay well for {1} {0} kills. Bring proof when you're done." },
        new[] { "Bounty Hunt", "There's a bounty on {0}s. Slay {1} for the reward." },
        new[] { "Safety Patrol", "Make the roads safe -- kill {1} {0}s lurking in the wilderness." },
    };

    private static readonly string[][] CollectQuestTemplates =
    {
        new[] { "Material Request", "I need {1} {0}(s) for my work. Can you gather them from monsters?" },
        new[] { "Supply Run", "We're running low on {0}s. Collect {1} from the field." },
        new[] { "Research Materials", "I'm studying {0}s. Bring me {1} samples from defeated creatures." },
    };

    private static readonly string[][] ExploreQuestTemplates =
    {
        new[] { "Scouting Mission", "Scout the floor -- explore at least {0}% of the map." },
        new[] { "Cartographer's Request", "I'm mapping this floor. Help me by exploring {0}% of the area." },
        new[] { "Recon Duty", "Command needs intel. Explore {0}% of this floor's terrain." },
    };

    private static readonly string[][] DeliverQuestTemplates =
    {
        new[] { "Special Delivery", "Take this package to the next vendor you find. They'll know what to do." },
        new[] { "Urgent Message", "Deliver this urgent message -- find the nearest NPC and pass it along." },
        new[] { "Supply Shipment", "I packed supplies for the frontlines. Deliver them to the next NPC." },
    };

    // Single dispatcher -- parameterizes over per-type target pool, count formula,
    // template set, id tag, and reward formula. Exact per-type reward math preserved.
    public static Quest Generate(QuestType type, int floor)
        => Generate(type, floor, giver: "");

    private static Quest Generate(QuestType type, int floor, string giver)
    {
        // Per-type target string (mob name, item name, or literal) + target count.
        string target; int count; string idTag; string[][] templates;
        int rewardCol, rewardXp;
        switch (type)
        {
            case QuestType.Kill:
                target = KillTargets[Random.Shared.Next(KillTargets.Length)];
                count = 3 + Random.Shared.Next(0, 3) + floor / 5;
                idTag = $"kill_{floor}_{target}";
                templates = KillQuestTemplates;
                rewardCol = 100 + floor * 50 + count * 20;
                rewardXp = 50 + floor * 30 + count * 10;
                break;
            case QuestType.Collect:
                target = CollectItems[Random.Shared.Next(CollectItems.Length)];
                count = 2 + Random.Shared.Next(0, 3);
                idTag = $"collect_{floor}_{target}";
                templates = CollectQuestTemplates;
                rewardCol = 80 + floor * 40 + count * 30;
                rewardXp = 40 + floor * 25 + count * 15;
                break;
            case QuestType.Explore:
                count = 40 + Random.Shared.Next(0, 20); // percent
                target = count.ToString();
                idTag = $"explore_{floor}_{count}";
                templates = ExploreQuestTemplates;
                rewardCol = 150 + floor * 60;
                rewardXp = 80 + floor * 35;
                break;
            default: // Deliver
                target = "Package";
                count = 1;
                idTag = $"deliver_{floor}";
                templates = DeliverQuestTemplates;
                rewardCol = 120 + floor * 45;
                rewardXp = 60 + floor * 30;
                break;
        }

        var template = templates[Random.Shared.Next(templates.Length)];
        // Explore's description takes a single {0} = percent; Deliver has no format args;
        // Kill/Collect take {0} = target, {1} = count.
        string description = type switch
        {
            QuestType.Explore => string.Format(template[1], count),
            QuestType.Deliver => template[1],
            _ => string.Format(template[1], target, count),
        };

        return new Quest
        {
            Id = $"{idTag}_{Random.Shared.Next(1000)}",
            Title = template[0],
            Description = description,
            GiverName = giver, Floor = floor, Type = type,
            TargetMob = type == QuestType.Kill ? target : null,
            TargetItem = type == QuestType.Collect || type == QuestType.Deliver ? target : null,
            TargetTileType = type == QuestType.Explore ? "exploration" : null,
            TargetCount = count,
            RewardCol = rewardCol,
            RewardXp = rewardXp,
        };
    }

    // -- Progress tracking methods (called from TurnManager) --

    public static void OnMobKilled(string mobName, UI.IGameLog log, string? weaponType = null)
    {
        foreach (var q in ActiveQuests)
        {
            if (q.Type != QuestType.Kill || q.Status != QuestStatus.Active) continue;
            // Weapon-gated quests only count kills made with the required type.
            if (!string.IsNullOrEmpty(q.RequiresWeaponType) && q.RequiresWeaponType != weaponType) continue;
            // Empty TargetMob = any kill counts (used by generic "N kills" trials).
            if (string.IsNullOrEmpty(q.TargetMob) || mobName.Contains(q.TargetMob))
            {
                q.CurrentCount++;
                if (q.CurrentCount >= q.TargetCount)
                {
                    q.Status = QuestStatus.Complete;
                    log.LogSystem($"  [QUEST] '{q.Title}' complete! Return to the quest giver to claim your reward.");
                }
            }
        }
    }

    public static void OnItemPickup(string itemName, UI.IGameLog log)
    {
        foreach (var q in ActiveQuests)
        {
            if (q.Type != QuestType.Collect || q.Status != QuestStatus.Active) continue;
            if (itemName.Contains(q.TargetItem ?? ""))
            {
                q.CurrentCount++;
                if (q.CurrentCount >= q.TargetCount)
                {
                    q.Status = QuestStatus.Complete;
                    log.LogSystem($"  [QUEST] '{q.Title}' complete! Return to any NPC to claim your reward.");
                }
            }
        }
    }

    public static void OnExplorationUpdate(int explorationPercent, UI.IGameLog log)
    {
        foreach (var q in ActiveQuests)
        {
            if (q.Type != QuestType.Explore || q.Status != QuestStatus.Active) continue;
            q.CurrentCount = explorationPercent;
            if (q.CurrentCount >= q.TargetCount)
            {
                q.Status = QuestStatus.Complete;
                log.LogSystem($"  [QUEST] '{q.Title}' complete! Return to any NPC to claim your reward.");
            }
        }
    }

    public static void OnNpcTalk(UI.IGameLog log)
    {
        // Deliver quests complete when talking to any NPC
        foreach (var q in ActiveQuests)
        {
            if (q.Type != QuestType.Deliver || q.Status != QuestStatus.Active) continue;
            q.CurrentCount = 1;
            q.Status = QuestStatus.Complete;
            log.LogSystem($"  [QUEST] '{q.Title}' delivered!");
        }
    }

    // Turn in all completed quests and return total rewards.
    public static (int Col, int Xp) TurnInCompleted()
    {
        int col = 0, xp = 0;
        var toMove = new List<Quest>();
        foreach (var q in ActiveQuests)
        {
            if (q.Status != QuestStatus.Complete) continue;
            q.Status = QuestStatus.TurnedIn;
            col += q.RewardCol;
            xp += q.RewardXp;
            toMove.Add(q);
        }
        foreach (var q in toMove)
        {
            ActiveQuests.Remove(q);
            CompletedQuests.Add(q);
        }
        return (col, xp);
    }

    // Clear quests for a new floor (keep completed history + persistent story quests).
    public static void OnFloorChange()
    {
        var expired = ActiveQuests
            .Where(q => q.Status == QuestStatus.Active && !q.Persistent)
            .ToList();
        foreach (var q in expired) ActiveQuests.Remove(q);
    }

    public static Quest? GetQuest(string id)
        => ActiveQuests.FirstOrDefault(q => q.Id == id)
           ?? CompletedQuests.FirstOrDefault(q => q.Id == id);
}
