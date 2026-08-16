namespace SAOTRPG.UI.Dialogs;

// Read-only seam onto the reader's render pipeline, for Tools/GuideAudit.
//
// The pipeline is private because nothing in the game calls it from outside, but the Guide's
// invariants are properties of the RENDERED page, not of the C# string literals in
// PlayerGuideContent — measuring the source instead of the render has produced a wrong answer five
// times across this workstream. So the checker runs the real pipeline rather than reimplementing it.
//
// Every member here forwards; none reimplements. Adding a transform to RenderGatedBody therefore
// reaches the checker automatically, which is the point of the split.
public static partial class PlayerGuideDialog
{
    public static int AuditWrapCols(int dialogWidth) => ComputeBodyWrapCols(dialogWidth);

    // The sidebar's categories, in digit-jump order. A topic in a category not on this list has no
    // sidebar row and is unreachable.
    public static IReadOnlyList<string> AuditCategoryOrder => CategoryOrder;

    public static Dictionary<string, List<string>> AuditBacklinkMap(
        PlayerGuideContent.GuideEntry[] entries) => BuildReferencedByMap(entries);

    // The page with the floor-unlock and boss-drop gates skipped. Structural checks use this: the
    // gates are subtractive and depend on how far this machine's save has climbed, so gated renders
    // would make the same corpus pass on one machine and fail on another.
    public static string AuditRenderUngated(PlayerGuideContent.GuideEntry e, int wrapCols,
        Dictionary<string, List<string>> backlinks) =>
        RenderGatedBody(e, e.Body, wrapCols, backlinks);

    // Outgoing cross-references, read the way RenderGatedBody reads them (single brackets migrated
    // to double first, so legacy "SEE ALSO" paragraphs contribute).
    public static List<string> AuditOutgoingLinks(string body)
    {
        var migrated = string.Join("\n",
            body.Replace("\r\n", "\n").Split('\n').Select(MigrateBracketsToDouble));
        return ExtractInlineLinks(migrated);
    }

    // The header box's label/value rows, exactly as the rendered stat block reads them.
    public static IReadOnlyList<(string Label, string Value)> AuditStatFields(string body) =>
        ParseStatFields(body);

    // The reader's own search predicate. The Guide states search examples in its prose, and an
    // example is only true if something runs it.
    public static PlayerGuideContent.GuideEntry[] AuditSearch(string query) =>
        PlayerGuideContent.Entries.Where(e => Matches(e, query)).ToArray();
}
