using SAOTRPG.Systems;
using Terminal.Gui;

namespace SAOTRPG.Entities;

public static class WandererFactory
{
    private static readonly string[] Names = { "Argo", "Klein", "Agil", "Lisbeth", "Silica", "Sachi", "Diabel", "Rosalia" };

    private static readonly string[][] TipSets =
    {
        new[] { "Watch your step on this floor...", "I heard there are hidden rooms behind cracked walls.", "Good luck out there!" },
        new[] { "The monsters here are tougher than they look.", "Have you tried using throwables? They can turn the tide.", "Stay safe!" },
        new[] { "I've been mapping this floor for days.", "If you see a shrine, use it — the buff is worth it.", "May your blade stay sharp." },
        new[] { "The bounty board always has work if you need Col.", "Don't forget to eat — starving in a dungeon is no joke.", "See you on the next floor." },
        new[] { "I used to be an adventurer like you...", "Press V to enter counter stance — riposte incoming attacks!", "Keep fighting!" },
    };

    public static WorldSpawn CreateWanderer(int floor)
    {
        string name = Names[Random.Shared.Next(Names.Length)];
        var tips = TipSets[Random.Shared.Next(TipSets.Length)];
        var dialogueLines = tips.Select(t => new DialogueLine(t)).ToArray();
        var npc = new WorldSpawn('N', Color.BrightCyan)
        {
            Name = name,
            Level = Math.Max(1, floor),
            DialogueLines = dialogueLines,
        };
        return npc;
    }
}
