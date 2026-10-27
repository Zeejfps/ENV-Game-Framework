namespace ZGF.ILSpy.Core;

/// <summary>How namespace names nest: <c>System.Collections.Generic</c> in <c>System.Collections</c>,
/// and that in <c>System</c>.</summary>
public static class NamespaceNames
{
    private const char Separator = '.';

    /// <summary>The last segment of <paramref name="name"/>, e.g. <c>Generic</c> for
    /// <c>System.Collections.Generic</c>; the whole name for an outermost one.</summary>
    public static string LastSegment(string name)
    {
        var last = name.LastIndexOf(Separator);
        return name.Substring(last + 1);
    }

    /// <summary><paramref name="name"/> and the names of the namespaces it nests in, the name first.</summary>
    internal static IEnumerable<string> AndEnclosing(string name)
    {
        for (string? enclosing = name; enclosing != null; enclosing = EnclosingName(enclosing))
            yield return enclosing;
    }

    /// <summary>The name <paramref name="name"/> extends by a segment; null for an outermost name.</summary>
    internal static string? EnclosingName(string name)
    {
        var last = name.LastIndexOf(Separator);
        if (last < 0)
            return null;
        return name.Substring(0, last);
    }
}
