namespace ZGF.ILSpy.Core;

/// <summary>NuGet's version ranges, as <c>[1.0, 2.0)</c> writes "from 1.0 up to but not including 2.0".</summary>
public static class VersionRange
{
    /// <summary>
    /// <paramref name="range"/> as comparisons a reader takes in at a glance: <c>[8.0.1, )</c> as
    /// "≥ 8.0.1", <c>[1.0]</c> as "= 1.0", <c>[1.0, 2.0)</c> as "≥ 1.0, &lt; 2.0", and a bare version, which
    /// NuGet reads as a minimum, as "≥" it. Empty for no range, which allows any version; a range this
    /// cannot read is given back as it is.
    /// </summary>
    public static string Describe(string range)
    {
        var trimmed = range.Trim();
        if (trimmed.Length == 0)
            return "";
        if (!IsBracketed(trimmed))
            return "≥ " + trimmed;
        var inner = trimmed.Substring(1, trimmed.Length - 2);
        var comma = inner.IndexOf(',');
        if (comma < 0)
            return "= " + inner.Trim();
        var lower = inner.Substring(0, comma).Trim();
        var upper = inner.Substring(comma + 1).Trim();
        return JoinBounds(LowerBound(trimmed[0], lower), UpperBound(trimmed[^1], upper));
    }

    private static bool IsBracketed(string range)
    {
        if (range.Length < 2)
            return false;
        var opens = range[0] is '[' or '(';
        var closes = range[^1] is ']' or ')';
        return opens && closes;
    }

    private static string LowerBound(char bracket, string version)
    {
        if (version.Length == 0)
            return "";
        var sign = bracket == '[' ? "≥ " : "> ";
        return sign + version;
    }

    private static string UpperBound(char bracket, string version)
    {
        if (version.Length == 0)
            return "";
        var sign = bracket == ']' ? "≤ " : "< ";
        return sign + version;
    }

    private static string JoinBounds(string lower, string upper)
    {
        if (lower.Length == 0)
            return upper;
        if (upper.Length == 0)
            return lower;
        return lower + ", " + upper;
    }
}
