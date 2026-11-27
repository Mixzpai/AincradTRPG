namespace SAOTRPG.UI.Dialogs;

// Read-only seam onto the reader's render pipeline, for the Guide audit tool.
//
// The pipeline is private because nothing in the game calls it from outside, but the Guide's
// invariants are properties of the rendered page, not of the C# string literals in
// PlayerGuideContent, so the audit runs the real pipeline rather than reimplementing it.
//
// Every member here forwards; none reimplements, so a transform added to RenderGatedBody
// reaches the audit automatically.
public static partial class PlayerGuideDialog
{
    public static int AuditWrapCols(int dialogWidth) => ComputeBodyWrapCols(dialogWidth);

    // The sidebar's categories, in digit-jump order. A topic in a category not on this list has no
    // sidebar row and is unreachable.
    public static IReadOnlyList<string> AuditCategoryOrder => CategoryOrder;

    public static Dictionary<string, List<string>> AuditBacklinkMap(
        PlayerGuideContent.GuideEntry[] entries) => BuildReferencedByMap(entries);

    // The page with the floor-unlock and boss-drop gates skipped, for structural audits: the gates
    // are subtractive and depend on how far the local save has climbed, so a gated render of the
    // same content differs from one install to another.
    public static string AuditRenderUngated(PlayerGuideContent.GuideEntry e, int wrapCols,
        Dictionary<string, List<string>> backlinks) =>
        RenderGatedBody(e, e.Body, wrapCols, backlinks);

    // Outgoing cross-references, read the way RenderGatedBody reads them (single brackets converted
    // to double first, so single-bracket "SEE ALSO" paragraphs contribute).
    public static List<string> AuditOutgoingLinks(string body)
    {
        var migrated = string.Join("\n",
            body.Replace("\r\n", "\n").Split('\n').Select(MigrateBracketsToDouble));
        return ExtractInlineLinks(migrated);
    }

    // The header box's label/value rows, exactly as the rendered stat block reads them.
    public static IReadOnlyList<(string Label, string Value)> AuditStatFields(string body) =>
        ParseStatFields(body);

    // The reader's own search predicate, so the search examples the Guide's prose gives can be run.
    public static PlayerGuideContent.GuideEntry[] AuditSearch(string query) =>
        PlayerGuideContent.Entries.Where(e => Matches(e, query)).ToArray();
}
