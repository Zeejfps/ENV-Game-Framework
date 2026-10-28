using System.Text;
using ZGF.Markdown;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>Selected markdown as an HTML fragment, for pasting where formatting is kept: each leaf the
/// element it is — a paragraph, a heading, code, a table cell — inside the lists, items, quotes,
/// tables and rows it sits in, with its bold, italic, code, struck and linked text marked up. Only
/// web and mail links keep their address.</summary>
internal static class MarkdownHtml
{
    public static string Of(IReadOnlyList<MarkdownLeaf> leaves, MarkdownSelection selection)
    {
        var html = new StringBuilder();
        var open = new List<HtmlContainer>();
        var last = Math.Min(selection.End.Leaf, leaves.Count - 1);
        for (var leaf = selection.Start.Leaf; leaf <= last; leaf++)
        {
            var shown = leaves[leaf];
            var (from, to) = selection.SpanIn(leaf, shown.Text.Length);
            if (from >= to && shown.Tag is not LeafTag.Cell)
                continue;
            Enter(html, open, shown.Path);
            Element(html, shown, from, to);
        }
        Enter(html, open, []);
        return html.ToString();
    }

    /// <summary>Closes the containers open past those <paramref name="path"/> shares with them, and
    /// opens the rest of <paramref name="path"/>.</summary>
    private static void Enter(StringBuilder html, List<HtmlContainer> open, IReadOnlyList<HtmlContainer> path)
    {
        var shared = 0;
        while (shared < open.Count && shared < path.Count && ReferenceEquals(open[shared], path[shared]))
            shared++;
        for (var i = open.Count - 1; i >= shared; i--)
            html.Append(CloseTag(open[i]));
        open.RemoveRange(shared, open.Count - shared);
        for (var i = shared; i < path.Count; i++)
        {
            html.Append(OpenTag(path[i]));
            open.Add(path[i]);
        }
    }

    private static string OpenTag(HtmlContainer container) => container switch
    {
        HtmlContainer.List { Ordered: false } => "<ul>",
        HtmlContainer.List { Start: 1 } => "<ol>",
        HtmlContainer.List list => $"<ol start=\"{list.Start}\">",
        HtmlContainer.Item => "<li>",
        HtmlContainer.Quote => "<blockquote>",
        HtmlContainer.Table => "<table>",
        HtmlContainer.Row => "<tr>",
        _ => throw new InvalidOperationException($"No tag for a {container.GetType().Name}."),
    };

    private static string CloseTag(HtmlContainer container) => container switch
    {
        HtmlContainer.List { Ordered: false } => "</ul>",
        HtmlContainer.List => "</ol>",
        HtmlContainer.Item => "</li>",
        HtmlContainer.Quote => "</blockquote>",
        HtmlContainer.Table => "</table>",
        HtmlContainer.Row => "</tr>",
        _ => throw new InvalidOperationException($"No tag for a {container.GetType().Name}."),
    };

    private static void Element(StringBuilder html, MarkdownLeaf leaf, int from, int to)
    {
        switch (leaf.Tag)
        {
            case LeafTag.Paragraph:
                html.Append("<p>");
                Inline(html, leaf, from, to);
                html.Append("</p>");
                break;
            case LeafTag.Heading heading:
                html.Append("<h").Append(heading.Level).Append('>');
                Inline(html, leaf, from, to);
                html.Append("</h").Append(heading.Level).Append('>');
                break;
            case LeafTag.Code:
                html.Append("<pre><code>");
                Escape(html, leaf.Text.AsSpan(from, to - from), breaks: false);
                html.Append("</code></pre>");
                break;
            case LeafTag.Cell cell:
                html.Append(cell.Header ? "<th>" : "<td>");
                Inline(html, leaf, from, to);
                html.Append(cell.Header ? "</th>" : "</td>");
                break;
            default:
                throw new InvalidOperationException($"No element for a {leaf.Tag.GetType().Name}.");
        }
    }

    /// <summary>The leaf's text from <paramref name="from"/> to <paramref name="to"/>, each run's
    /// part marked up as the run it was styled from.</summary>
    private static void Inline(StringBuilder html, MarkdownLeaf leaf, int from, int to)
    {
        var offset = 0;
        for (var i = 0; i < leaf.Runs.Count; i++)
        {
            var text = leaf.Runs[i].Text;
            var start = Math.Max(from, offset);
            var end = Math.Min(to, offset + text.Length);
            if (start < end)
                Run(html, leaf.Source[i], text.AsSpan(start - offset, end - start));
            offset += text.Length;
        }
    }

    private static void Run(StringBuilder html, InlineRun run, ReadOnlySpan<char> text)
    {
        var link = run.LinkUrl ?? run.ImageSrc;
        var linked = link != null && IsWebOrMail(link);
        if (linked)
            html.Append("<a href=\"").Append(EscapeAttribute(link!)).Append("\">");
        if (run.Strikethrough) html.Append("<s>");
        if (run.Bold) html.Append("<b>");
        if (run.Italic) html.Append("<i>");
        if (run.Code) html.Append("<code>");
        Escape(html, text, breaks: true);
        if (run.Code) html.Append("</code>");
        if (run.Italic) html.Append("</i>");
        if (run.Bold) html.Append("</b>");
        if (run.Strikethrough) html.Append("</s>");
        if (linked)
            html.Append("</a>");
    }

    private static bool IsWebOrMail(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeMailto);

    /// <summary>Appends <paramref name="text"/> with the characters HTML gives meaning escaped, and
    /// with <paramref name="breaks"/> each line break as a break element.</summary>
    private static void Escape(StringBuilder html, ReadOnlySpan<char> text, bool breaks)
    {
        foreach (var c in text)
        {
            switch (c)
            {
                case '&': html.Append("&amp;"); break;
                case '<': html.Append("&lt;"); break;
                case '>': html.Append("&gt;"); break;
                case '"': html.Append("&quot;"); break;
                case '\n' when breaks: html.Append("<br>"); break;
                default: html.Append(c); break;
            }
        }
    }

    private static string EscapeAttribute(string value)
    {
        var html = new StringBuilder(value.Length);
        Escape(html, value, breaks: false);
        return html.ToString();
    }
}
