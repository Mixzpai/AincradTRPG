namespace SAOTRPG.Systems;

// Data structures for NPC branching dialogue.
// A dialogue is a sequence of lines, each optionally having player choices.

// A single page of NPC dialogue with optional player choices.
public record DialogueLine(string Text, DialogueChoice[]? Choices = null);

// A player response option in a dialogue.
public record DialogueChoice(string Label, string Response);
