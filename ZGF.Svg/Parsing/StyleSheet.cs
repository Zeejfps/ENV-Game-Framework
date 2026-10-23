using System.Xml;

namespace ZGF.Svg.Parsing;

/// <summary>
/// The rules of a document's &lt;style&gt; elements, as icon exporters write them: simple
/// selectors only — an element name, <c>*</c>, <c>.class</c> and <c>#id</c>, alone or
/// compounded (<c>path.a</c>), in comma lists. Rules with combinators, attribute selectors or
/// pseudo-classes can't be matched while streaming the tree, so they are skipped, as are
/// at-rules.
/// </summary>
internal sealed class StyleSheet
{
    public static readonly StyleSheet Empty = new([]);

    private readonly List<Rule> _rules;

    private StyleSheet(List<Rule> rules)
    {
        _rules = rules;
    }

    private readonly record struct Selector(string? Element, string? Id, string[] Classes)
    {
        public int Specificity => (Id != null ? 100 : 0) + Classes.Length * 10 + (Element != null ? 1 : 0);

        public bool Matches(string element, string? id, string? classAttribute)
        {
            if (Element != null && Element != element)
                return false;
            if (Id != null && Id != id)
                return false;
            foreach (var c in Classes)
            {
                if (!HasClass(classAttribute, c))
                    return false;
            }
            return true;
        }

        private static bool HasClass(string? classAttribute, string name)
        {
            if (classAttribute == null)
                return false;
            foreach (var range in classAttribute.AsSpan().SplitAny(" \t\r\n"))
            {
                if (classAttribute.AsSpan()[range].SequenceEqual(name))
                    return true;
            }
            return false;
        }
    }

    private readonly record struct Rule(Selector Selector, string Declarations, int Order);

    /// <summary>Reads every &lt;style&gt; element of <paramref name="svgText"/>, wherever it sits.</summary>
    public static StyleSheet Collect(string svgText, XmlReaderSettings settings)
    {
        var rules = new List<Rule>();
        using var reader = XmlReader.Create(new StringReader(svgText), settings);
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "style" || reader.IsEmptyElement)
                continue;
            AddRules(reader.ReadElementContentAsString(), rules);
        }
        return rules.Count == 0 ? Empty : new StyleSheet(rules);
    }

    /// <summary>
    /// The declaration blocks of the rules that match the element, in cascade order: lower
    /// specificity first, then document order, so later blocks win when applied in turn.
    /// </summary>
    public IEnumerable<string> DeclarationsFor(string element, string? id, string? classAttribute)
    {
        if (_rules.Count == 0)
            return [];
        return _rules
            .Where(r => r.Selector.Matches(element, id, classAttribute))
            .OrderBy(r => r.Selector.Specificity)
            .ThenBy(r => r.Order)
            .Select(r => r.Declarations);
    }

    private static void AddRules(string css, List<Rule> rules)
    {
        var text = StripComments(css);
        var i = 0;
        while (i < text.Length)
        {
            var open = text.IndexOf('{', i);
            if (open < 0)
                return;
            var prelude = text[i..open].Trim();
            if (prelude.StartsWith('@'))
            {
                i = SkipAtRule(text, i);
                continue;
            }
            var close = text.IndexOf('}', open + 1);
            if (close < 0)
                close = text.Length;
            var declarations = text[(open + 1)..close];
            foreach (var part in prelude.Split(','))
            {
                if (TryParseSelector(part.Trim(), out var selector))
                    rules.Add(new Rule(selector, declarations, rules.Count));
            }
            i = close + 1;
        }
    }

    private static string StripComments(string css)
    {
        var start = css.IndexOf("/*", StringComparison.Ordinal);
        if (start < 0)
            return css;
        var end = css.IndexOf("*/", start + 2, StringComparison.Ordinal);
        var rest = end < 0 ? "" : css[(end + 2)..];
        return css[..start] + " " + StripComments(rest);
    }

    /// <summary>Skips an at-rule starting at or after <paramref name="from"/>: up to its ';' when
    /// it has no block (@import), else past its block, nested blocks included (@media).</summary>
    private static int SkipAtRule(string text, int from)
    {
        var semicolon = text.IndexOf(';', from);
        var open = text.IndexOf('{', from);
        if (semicolon >= 0 && semicolon < open)
            return semicolon + 1;
        var depth = 0;
        for (var i = open; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                depth++;
                continue;
            }
            if (text[i] != '}')
                continue;
            depth--;
            if (depth == 0)
                return i + 1;
        }
        return text.Length;
    }

    /// <summary>Parses a compound selector such as <c>*</c>, <c>rect</c>, <c>.a.b</c> or
    /// <c>path#x.a</c>; false for anything with combinators, attributes or pseudo-classes.</summary>
    private static bool TryParseSelector(string text, out Selector selector)
    {
        selector = default;
        if (text.Length == 0 || text.AsSpan().IndexOfAny(" \t\r\n>+~[:") >= 0)
            return false;

        string? element = null;
        string? id = null;
        var classes = new List<string>();
        var i = 0;
        var nameEnd = NameEnd(text, 0);
        if (nameEnd > 0)
        {
            element = text[..nameEnd];
            i = nameEnd;
        }
        else if (text[0] == '*')
        {
            i = 1;
        }

        while (i < text.Length)
        {
            var kind = text[i];
            var end = NameEnd(text, i + 1);
            if (end == i + 1)
                return false;
            var name = text[(i + 1)..end];
            if (kind == '.')
                classes.Add(name);
            else if (kind == '#')
                id = name;
            else
                return false;
            i = end;
        }

        selector = new Selector(element, id, classes.ToArray());
        return true;
    }

    private static int NameEnd(string text, int start)
    {
        var i = start;
        while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] is '-' or '_'))
            i++;
        return i;
    }
}
