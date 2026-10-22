using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ZGF.ILSpy.Core;

/// <summary>What a search looks for; any combination.</summary>
[Flags]
public enum SearchKinds
{
    None = 0,
    Types = 1 << 0,
    Methods = 1 << 1,
    Fields = 1 << 2,
    Properties = 1 << 3,
    Events = 1 << 4,
    /// <summary>Constants, and string and number literals in method bodies.</summary>
    Literals = 1 << 5,
    /// <summary>The definition with a metadata token, in hex.</summary>
    Tokens = 1 << 6,
    Resources = 1 << 7,
    Assemblies = 1 << 8,
    Namespaces = 1 << 9,
    Members = Methods | Fields | Properties | Events,
    TypesAndMembers = Types | Members,
    All = TypesAndMembers | Literals | Tokens | Resources | Assemblies | Namespaces,
}

/// <summary>Which of an assembly's names an <see cref="SearchKinds.Assemblies"/> search matches.</summary>
public enum AssemblyNameKind
{
    NameOrFileName,
    FilePath,
    FullName,
}

/// <summary>
/// A parsed search box query, with ILSpy's syntax. Whitespace separates terms; double quotes keep
/// one together. A term matches a name containing it, ignoring case; every term must match.
/// <list type="bullet">
/// <item><c>-term</c> must not be contained, <c>=term</c> must be the whole name, <c>~term</c>
/// matches letters in order with gaps, <c>+term</c> is a plain term.</item>
/// <item><c>/regex/</c> matches a regular expression instead, ignoring case.</item>
/// <item>A term with a dot matches qualified names, e.g. <c>Generic.List</c>.</item>
/// <item>A term with <c>&lt;</c> matches names with their type parameters, C#-style, e.g.
/// <c>List&lt;T&gt;</c>.</item>
/// <item>Kind prefixes replace the <see cref="Kinds"/> picked, and add up when there are several:
/// <c>tm:</c> <c>t:</c> <c>m:</c> <c>md:</c> <c>f:</c> <c>p:</c> <c>e:</c> <c>c:</c> (literals)
/// <c>r:</c> <c>n:</c>, <c>a:</c> / <c>af:</c> / <c>an:</c> (assembly by name, file path, full
/// name), <c>@</c> (metadata token).</item>
/// <item><c>inassembly:</c> and <c>innamespace:</c> keep results to assemblies or namespaces
/// whose name contains the value.</item>
/// </list>
/// </summary>
public sealed class SearchQuery
{
    private SearchQuery(SearchKinds kinds)
    {
        Kinds = kinds;
    }

    public SearchKinds Kinds { get; private set; }

    public AssemblyNameKind AssemblyName { get; private set; }

    public IReadOnlyList<string> Terms { get; private set; } = [];

    public Regex? Regex { get; private set; }

    /// <summary>Whether names are matched qualified, namespace and declaring types included.</summary>
    public bool FullName { get; private set; }

    /// <summary>Whether a term names generic arity, so names keep their <c>`1</c> suffix.</summary>
    public bool KeepArity { get; private set; }

    /// <summary>Whether a term names type parameters, so generic names are written with them, e.g.
    /// <c>Dictionary&lt;TKey, TValue&gt;</c>.</summary>
    public bool Generics { get; private set; }

    public string? InAssembly { get; private set; }

    public string? InNamespace { get; private set; }

    /// <summary>True when there is nothing to match, so nothing is found.</summary>
    public bool IsEmpty => Terms.Count == 0 && Regex is null;

    /// <summary>Parses <paramref name="text"/>, searching for <paramref name="kinds"/> unless a prefix
    /// says otherwise.</summary>
    public static SearchQuery Parse(string text, SearchKinds kinds = SearchKinds.TypesAndMembers)
    {
        var query = new SearchQuery(kinds);
        var prefixed = false;
        var terms = new List<string>();
        foreach (var part in Split(text))
        {
            var (prefix, term) = SplitPrefix(part);
            if (term.Length >= 2 && term[0] == '"' && term[^1] == '"')
                term = term[1..^1];

            switch (prefix)
            {
                case "inassembly":
                    query.InAssembly ??= term;
                    continue;
                case "innamespace":
                    query.InNamespace ??= term;
                    continue;
                case not null:
                    query.Kinds = (prefixed ? query.Kinds : SearchKinds.None) | KindsOf(prefix);
                    prefixed = true;
                    query.AssemblyName = prefix switch
                    {
                        "af" => AssemblyNameKind.FilePath,
                        "an" => AssemblyNameKind.FullName,
                        _ => query.AssemblyName,
                    };
                    break;
            }

            if (query.Regex is null && term.Length > 1 && term[0] == '/')
            {
                var pattern = term.Length > 2 && term[^1] == '/' ? term[1..^1] : term[1..];
                query.FullName |= pattern.Contains("\\.");
                query.Generics |= pattern.Contains('<');
                query.Regex = TryRegex(pattern);
                continue;
            }
            query.FullName |= term.Contains('.');
            query.KeepArity |= term.Contains('`');
            query.Generics |= term.Contains('<');
            terms.Add(term);
        }
        query.Terms = terms;
        return query;
    }

    /// <summary>Whether <paramref name="name"/> matches every term, or the regular expression.</summary>
    public bool IsMatch(string name)
    {
        if (Regex is not null) return Regex.IsMatch(name);
        foreach (var term in Terms)
        {
            if (term.Length == 0) continue;
            var rest = term.AsSpan(1);
            var matches = term[0] switch
            {
                '-' => rest.Length == 0 || !name.AsSpan().Contains(rest, StringComparison.OrdinalIgnoreCase),
                '=' => rest.Length == 0 || name.AsSpan().Equals(rest, StringComparison.OrdinalIgnoreCase),
                '~' => rest.Length == 0 || IsSubsequence(name, rest),
                '+' => name.AsSpan().Contains(rest, StringComparison.OrdinalIgnoreCase),
                _ => name.Contains(term, StringComparison.OrdinalIgnoreCase),
            };
            if (!matches) return false;
        }
        return true;
    }

    /// <summary>The single term as a number or string literal, for <see cref="SearchKinds.Literals"/>:
    /// a <see cref="long"/> for integers (decimal or <c>0x</c> hex), a <see cref="float"/> with an
    /// <c>f</c> suffix, a <see cref="double"/> otherwise; null when the term is not a number.</summary>
    public object? Literal()
    {
        if (Regex is not null || Terms.Count != 1) return null;
        var term = Terms[0];
        if (term.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || term.StartsWith("-0x", StringComparison.OrdinalIgnoreCase))
        {
            var negative = term[0] == '-';
            var digits = term[(negative ? 3 : 2)..].TrimEnd('u', 'U', 'l', 'L');
            if (!ulong.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var hex)) return null;
            return negative ? -(long)hex : (long)hex;
        }
        var integer = term.TrimEnd('u', 'U', 'l', 'L');
        if (long.TryParse(integer, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var signed)) return signed;
        if (ulong.TryParse(integer, NumberStyles.None, CultureInfo.InvariantCulture, out var unsigned)) return (long)unsigned;
        if (term.Length > 1 && term[^1] is 'f' or 'F')
            return float.TryParse(term[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var single) ? single : null;
        var real = term.Length > 1 && term[^1] is 'd' or 'D' ? term[..^1] : term;
        if (real.Contains('.') || real.Contains('e') || real.Contains('E'))
            return double.TryParse(real, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : null;
        return null;
    }

    /// <summary><paramref name="text"/> with its <paramref name="prefix"/> term, if any, replaced by
    /// one for <paramref name="value"/>, placed first.</summary>
    public static string WithScope(string text, string prefix, string value)
    {
        var parts = Split(text).Where(p => !p.StartsWith(prefix + ":", StringComparison.OrdinalIgnoreCase));
        var scoped = prefix + ":" + (value.Length == 0 || value.Contains(' ') ? $"\"{value}\"" : value);
        return string.Join(' ', parts.Prepend(scoped)) + " ";
    }

    private static SearchKinds KindsOf(string prefix) => prefix switch
    {
        "@" => SearchKinds.Tokens,
        "tm" => SearchKinds.TypesAndMembers,
        "t" => SearchKinds.Types,
        "m" => SearchKinds.Members,
        "md" => SearchKinds.Methods,
        "f" => SearchKinds.Fields,
        "p" => SearchKinds.Properties,
        "e" => SearchKinds.Events,
        "c" => SearchKinds.Literals,
        "r" => SearchKinds.Resources,
        "n" => SearchKinds.Namespaces,
        "a" or "af" or "an" => SearchKinds.Assemblies,
        _ => throw new ArgumentOutOfRangeException(nameof(prefix), prefix, null),
    };

    private static readonly HashSet<string> Prefixes =
        ["tm", "t", "m", "md", "f", "p", "e", "c", "r", "n", "a", "af", "an", "inassembly", "innamespace"];

    private static (string? Prefix, string Term) SplitPrefix(string part)
    {
        if (part.Length > 1 && part[0] == '@')
            return ("@", part[1..]);
        var end = part.IndexOfAny(['"', '/']);
        var colon = part.IndexOf(':', 0, end < 0 ? part.Length : end);
        if (colon <= 0 || colon == part.Length - 1) return (null, part);
        var prefix = part[..colon].ToLowerInvariant();
        return Prefixes.Contains(prefix) ? (prefix, part[(colon + 1)..]) : (null, part);
    }

    private static IEnumerable<string> Split(string text)
    {
        var part = new StringBuilder();
        var quoted = false;
        foreach (var c in text)
        {
            if (c == '"')
                quoted = !quoted;
            else if (!quoted && char.IsWhiteSpace(c))
            {
                if (part.Length > 0)
                {
                    yield return part.ToString();
                    part.Clear();
                }
                continue;
            }
            part.Append(c);
        }
        if (part.Length > 0)
            yield return part.ToString();
    }

    private static bool IsSubsequence(string name, ReadOnlySpan<char> letters)
    {
        var next = 0;
        foreach (var c in name)
        {
            if (char.ToLowerInvariant(c) == char.ToLowerInvariant(letters[next]) && ++next == letters.Length)
                return true;
        }
        return false;
    }

    private static Regex? TryRegex(string pattern)
    {
        try
        {
            return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
