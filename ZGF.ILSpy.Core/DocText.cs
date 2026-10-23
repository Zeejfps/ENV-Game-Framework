using System.Text;

namespace ZGF.ILSpy.Core;

/// <summary>
/// Documentation paragraphs as they are read: runs join the open paragraph with their whitespace
/// collapsed, as XML documentation is rendered, and a paragraph is kept only when it has text.
/// </summary>
internal sealed class DocText
{
    private readonly List<DocParagraph> _paragraphs = new();
    private List<DocRun> _runs = new();

    public bool IsEmpty => _paragraphs.Count == 0 && _runs.Count == 0;

    public void Add(string text, DocStyle style, CodeReference? reference = null)
    {
        var collapsed = CollapseWhitespace(text);
        if (EndsWithSpace() && collapsed.StartsWith(' '))
            collapsed = collapsed.Substring(1);
        if (collapsed.Length == 0)
            return;
        _runs.Add(new DocRun(collapsed, style, reference));
    }

    /// <summary>Adds <paramref name="code"/> as a paragraph of its own, lines kept and their
    /// common indentation removed.</summary>
    public void AddCode(string code)
    {
        Break();
        var lines = Dedent(code);
        if (lines.Length == 0)
            return;
        var run = new DocRun(lines, DocStyle.Code);
        _paragraphs.Add(new DocParagraph([run], IsCode: true));
    }

    /// <summary>Ends the open paragraph.</summary>
    public void Break()
    {
        TrimParagraphEnds();
        if (_runs.Count > 0)
            _paragraphs.Add(new DocParagraph(_runs));
        _runs = new List<DocRun>();
    }

    public IReadOnlyList<DocParagraph> Finish()
    {
        Break();
        return _paragraphs;
    }

    private bool EndsWithSpace()
    {
        if (_runs.Count == 0)
            return true;
        return _runs[^1].Text.EndsWith(' ');
    }

    private void TrimParagraphEnds()
    {
        if (_runs.Count == 0)
            return;
        var last = _runs[^1];
        _runs[^1] = last with { Text = last.Text.TrimEnd() };
        _runs.RemoveAll(static r => r.Text.Length == 0);
    }

    private static string CollapseWhitespace(string text)
    {
        var collapsed = new StringBuilder(text.Length);
        var inSpace = false;
        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                inSpace = true;
                continue;
            }
            if (inSpace)
                collapsed.Append(' ');
            inSpace = false;
            collapsed.Append(c);
        }
        if (inSpace)
            collapsed.Append(' ');
        return collapsed.ToString();
    }

    /// <summary>The code's lines without the blank ones around them or the indentation they
    /// share. Its first line arrives with its indentation trimmed, so only the lines after it say
    /// how deep the code sits.</summary>
    private static string Dedent(string code)
    {
        var lines = code.Replace("\r\n", "\n").Split('\n').ToList();
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
            lines.RemoveAt(0);
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
            lines.RemoveAt(lines.Count - 1);
        var indent = CommonIndent(lines.Skip(1));
        var dedented = lines
            .Select(l => RemoveIndent(l, indent))
            .Select(l => l.TrimEnd());
        return string.Join('\n', dedented);
    }

    private static int CommonIndent(IEnumerable<string> lines)
    {
        var indent = int.MaxValue;
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var leading = line.Length - line.TrimStart().Length;
            indent = Math.Min(indent, leading);
        }
        if (indent == int.MaxValue)
            return 0;
        return indent;
    }

    private static string RemoveIndent(string line, int indent)
    {
        var leading = line.Length - line.TrimStart().Length;
        if (leading < indent)
            return line.TrimStart();
        return line.Substring(indent);
    }
}
