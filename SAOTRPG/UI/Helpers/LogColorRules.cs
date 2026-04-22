using Terminal.Gui;

namespace SAOTRPG.UI.Helpers;

// Keyword → color rules for the game log. ColoredLogView checks in order; first case-insensitive match wins.
// Category palette (research §5): Combat BrightRed, System BrightCyan, Item Gold, Dialog White, General DimGray.
public static class LogColorRules
{
    // Ordered keyword → color pairs. Each log line is tested against these in sequence;
    // the first keyword found in the line determines the line's color.
    public static readonly (string Keyword, Color Color)[] Rules =
    {
        // ── Divine Objects ── ◈ diamond → BrightRed (drop/pickup/chest lines).
        ("◈",               Color.BrightRed),

        // ── FB-452 damage-type tags ── matched before generic rules so the log
        // line picks up the tag color. Covers brackets + chip styles; bare style
        // (unbracketed word at sentence start) is caught by ordering too.
        ("[FIRE]",          Color.BrightRed),
        ("[ICE]",           Color.BrightCyan),
        ("[THUNDER]",       Color.BrightYellow),
        ("[HOLY]",          Color.BrightYellow),
        ("[DARK]",          Color.BrightMagenta),
        ("[POISON]",        Color.Green),
        ("[BLEED]",         Color.White),
        ("[SLASH]",         Color.Gray),
        ("[THRUST]",        Color.Gray),
        ("[BLUNT]",         Color.DarkGray),
        ("[PIERCE]",        Color.White),
        ("[CUT]",           Color.Cyan),
        ("◆FIRE◆",          Color.BrightRed),
        ("◆ICE◆",           Color.BrightCyan),
        ("◆THUNDER◆",       Color.BrightYellow),
        ("◆HOLY◆",          Color.BrightYellow),
        ("◆DARK◆",          Color.BrightMagenta),
        ("◆POISON◆",        Color.Green),
        ("◆BLEED◆",         Color.White),
        ("◆SLASH◆",         Color.Gray),
        ("◆THRUST◆",        Color.Gray),
        ("◆BLUNT◆",         Color.DarkGray),
        ("◆PIERCE◆",        Color.White),
        ("◆CUT◆",           Color.Cyan),

        // ── Critical / special moments ──────────────────────────────
        ("CRITICAL HIT",    Color.BrightMagenta),
        ("LEVEL UP",        Color.BrightMagenta),
        ("UNSTOPPABLE",     Color.BrightMagenta),
        ("Rampage",         Color.BrightMagenta),
        ("PERFECT!",        Color.BrightYellow),
        ("FLOOR BOSS",      Color.BrightMagenta),

        // ── Kill / loot rewards ─────────────────────────────────────
        ("defeated!",       Color.BrightYellow),
        ("dropped a",       Color.BrightYellow),
        ("Picked up",       Color.BrightYellow),
        ("slain!",          Color.BrightYellow),

        // ── Combo / streak ──────────────────────────────────────────
        ("combo!",          Color.BrightCyan),
        ("streak",          Color.BrightCyan),
        ("Double Kill",     Color.BrightCyan),
        ("Triple Kill",     Color.BrightCyan),

        // ── Economy ─────────────────────────────────────────────────
        ("Purchased",       Color.BrightGreen),
        ("Sold",            Color.BrightGreen),

        // ── Combat ──────────────────────────────────────────────────
        ("you dodge",       Color.BrightCyan),

        // ── Healing / campfire ──────────────────────────────────────
        ("restores",        Color.BrightGreen),
        ("healed",          Color.BrightGreen),
        ("warmth restores", Color.BrightGreen),

        // ── Status effects ──────────────────────────────────────────
        ("poisoned",        Color.Green),
        ("bleeding",        Color.Red),
        ("starving",        Color.Yellow),

        // ── Traps ───────────────────────────────────────────────────
        ("spike trap",      Color.BrightRed),
        ("teleport trap",   Color.BrightRed),
        ("lava",            Color.BrightRed),

        // ── Floor transitions ───────────────────────────────────────
        ("Ascending to",    Color.BrightCyan),
        ("Welcome to Floor",Color.BrightCyan),

        // ── Proficiency ─────────────────────────────────────────────
        ("proficiency",     Color.BrightMagenta),

        // ── Sword Skills ────────────────────────────────────────────
        ("Sword Skill:",    Color.BrightYellow),
        ("Skill Unlocked",  Color.BrightCyan),
        ("Post-motion",     Color.Yellow),

        // ── Quests ──────────────────────────────────────────────────
        ("[QUEST]",         Color.BrightCyan),
        ("New quest",       Color.BrightCyan),

        // ── Crafting ────────────────────────────────────────────────
        ("SUCCESS!",        Color.BrightGreen),
        ("FAILURE!",        Color.BrightRed),
        ("Enhancement",     Color.BrightYellow),

        // ── Tutorial tips ───────────────────────────────────────────
        ("[TIP]",           Color.BrightCyan),

        // ── Near death ──────────────────────────────────────────────
        ("near death",      Color.BrightRed),
        ("barely standing", Color.BrightRed),
        ("One more hit",    Color.BrightRed),

        // ── NPC dialogue ────────────────────────────────────────────
        ("Klein:",          Color.BrightRed),
        ("Argo",            Color.BrightYellow),
        ("Agil:",           Color.BrightGreen),

        // ── Death penalty ───────────────────────────────────────────
        ("Death penalty",   Color.BrightRed),
    };
}
