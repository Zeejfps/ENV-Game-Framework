using System.Collections.Immutable;
using System.Reflection.Metadata.Ecma335;

namespace ZGF.ILSpy.Core;

/// <summary>The language a node is decompiled to.</summary>
public enum DecompilationLanguage
{
    CSharp,
    IL,
}

/// <summary>How a <see cref="DocumentSpan"/> is colored.</summary>
public enum SpanKind : byte
{
    Plain,
    Keyword,
    Type,
    Namespace,
    Method,
    Field,
    Property,
    Event,
    Parameter,
    Local,
    String,
    Number,
    Comment,
    Preprocessor,
    ILOpcode,
    ILLabel,
}

/// <summary>A run of a <see cref="DocumentLine"/>'s text: characters <see cref="Start"/> up to
/// <see cref="End"/>, optionally pointing at a <see cref="Reference"/>, which this run defines when
/// <see cref="IsDefinition"/> is set.</summary>
public readonly record struct DocumentSpan(int Start, int Length, SpanKind Kind, CodeReference? Reference, bool IsDefinition)
{
    public int End => Start + Length;
}

/// <summary>One line of a <see cref="DecompiledDocument"/>. Its <see cref="Spans"/> are in order and
/// cover <see cref="Text"/> exactly, with no gaps.</summary>
public readonly struct DocumentLine
{
    private readonly DocumentSpan[] _spans;
    private readonly int _first;
    private readonly int _count;

    internal DocumentLine(string text, DocumentSpan[] spans, int first, int count)
    {
        Text = text;
        _spans = spans;
        _first = first;
        _count = count;
    }

    public string Text { get; }

    public ReadOnlySpan<DocumentSpan> Spans => new(_spans, _first, _count);

    public override string ToString() => Text;
}

/// <summary>A foldable range of a <see cref="DecompiledDocument"/>. Collapsed, line
/// <see cref="StartLine"/> stays in view followed by <see cref="CollapsedText"/>, and the lines after
/// it up to and including <see cref="EndLine"/> are hidden.</summary>
/// <param name="DefaultCollapsed">Whether the region starts out collapsed, as the using directives do.</param>
/// <param name="IsDefinition">Whether the region is the body of a type or member definition.</param>
public readonly record struct FoldRegion(int StartLine, int EndLine, string CollapsedText, bool DefaultCollapsed, bool IsDefinition);

/// <summary>
/// The immutable result of decompiling a node: lines of classified, referencing text. Safe to read
/// from any thread while other documents are being produced.
/// </summary>
public sealed class DecompiledDocument
{
    private readonly IReadOnlyDictionary<CodeReference, int> _definitions;
    private readonly IReadOnlyDictionary<int, CodeAnchor> _statements;
    private Dictionary<CodeAnchor, int>? _definitionLines;
    private Dictionary<(string File, int Token), (int Offset, int Line)[]>? _statementLines;

    internal DecompiledDocument(string title, DecompilationLanguage language, bool isError, ImmutableArray<DocumentLine> lines,
        IReadOnlyDictionary<CodeReference, int> definitions, IReadOnlyDictionary<int, CodeAnchor> statements, ImmutableArray<FoldRegion> folds)
    {
        Title = title;
        Language = language;
        IsError = isError;
        Lines = lines;
        _definitions = definitions;
        _statements = statements;
        Folds = folds;
    }

    /// <summary>A document with no lines.</summary>
    public static DecompiledDocument Empty { get; } = FromText("", "");

    /// <summary>A short name for the document, e.g. for a tab.</summary>
    public string Title { get; }

    public DecompilationLanguage Language { get; }

    /// <summary>Whether decompilation failed and the lines describe the error.</summary>
    public bool IsError { get; }

    public ImmutableArray<DocumentLine> Lines { get; }

    /// <summary>The foldable regions, ordered by <see cref="FoldRegion.StartLine"/>, each starting on
    /// a line of its own and nested whole inside any region it starts in.</summary>
    public ImmutableArray<FoldRegion> Folds { get; }

    /// <summary>The index in <see cref="Folds"/> of the region starting on <paramref name="line"/>, or -1.</summary>
    public int FoldStartingAt(int line)
    {
        int low = 0, high = Folds.Length - 1;
        while (low <= high)
        {
            var mid = (low + high) >> 1;
            var start = Folds[mid].StartLine;
            if (start < line) low = mid + 1;
            else if (start > line) high = mid - 1;
            else return mid;
        }
        return -1;
    }

    /// <summary>A document of plain lines, e.g. a placeholder while decompiling.</summary>
    public static DecompiledDocument FromText(string title, string text, DecompilationLanguage language = DecompilationLanguage.CSharp)
    {
        var builder = new DocumentBuilder(language);
        builder.Write(text, SpanKind.Plain);
        return builder.Build(title);
    }

    /// <summary>The index of the line that defines <paramref name="reference"/>, or -1.</summary>
    public int FindDefinitionLine(CodeReference reference) => _definitions.GetValueOrDefault(reference, -1);

    /// <summary>The index of the line that defines the entity <paramref name="node"/> stands for, or -1
    /// if it has none or this document does not define it.</summary>
    public int FindDefinitionLine(BrowseNode node) => node.Reference is { } reference ? FindDefinitionLine(reference) : -1;

    /// <summary>What names <paramref name="line"/> so it can be found again: the statement starting on
    /// it, else the entity it defines; null for a line with neither, such as a brace or a blank line.</summary>
    public CodeAnchor? AnchorAt(int line)
    {
        if (line < 0 || line >= Lines.Length) return null;
        if (_statements.TryGetValue(line, out var statement)) return statement;
        foreach (var span in Lines[line].Spans)
        {
            if (span is { IsDefinition: true, Reference: { IsNavigable: true, Kind: not CodeReferenceKind.Assembly } reference })
                return DefinitionOf(reference);
        }
        return null;
    }

    /// <summary>The line <paramref name="anchor"/> names in this document, or -1 if it shows no such
    /// line. A statement no longer starting a line of its own finds the last one before it in its
    /// method, or the method's first.</summary>
    public int FindLine(CodeAnchor anchor)
    {
        if (anchor.Kind == CodeAnchorKind.Definition)
        {
            if (_definitionLines == null)
            {
                var lines = new Dictionary<CodeAnchor, int>(_definitions.Count);
                foreach (var (reference, line) in _definitions)
                {
                    if (reference.Kind != CodeReferenceKind.Assembly)
                        lines.TryAdd(DefinitionOf(reference), line);
                }
                _definitionLines = lines;
            }
            return _definitionLines.GetValueOrDefault(anchor, -1);
        }

        _statementLines ??= _statements
            .GroupBy(static s => (s.Value.FilePath, s.Value.Token))
            .ToDictionary(static g => g.Key, static g => g.Select(static s => (s.Value.ILOffset, s.Key)).Order().ToArray());
        if (!_statementLines.TryGetValue((anchor.FilePath, anchor.Token), out var points)) return -1;
        var found = points[0].Line;
        foreach (var (offset, line) in points)
        {
            if (offset > anchor.ILOffset) break;
            found = line;
        }
        return found;
    }

    private static CodeAnchor DefinitionOf(CodeReference reference) =>
        new(CodeAnchorKind.Definition, reference.FilePath!, MetadataTokens.GetToken(reference.Handle));

    /// <summary>The whole text, lines joined by '\n'.</summary>
    public string GetText() => GetText(0, Lines.Length);

    /// <summary>The text of lines <paramref name="first"/> up to but not including <paramref name="end"/>,
    /// joined by '\n'.</summary>
    public string GetText(int first, int end)
    {
        first = Math.Clamp(first, 0, Lines.Length);
        end = Math.Clamp(end, first, Lines.Length);
        var builder = new System.Text.StringBuilder();
        for (var i = first; i < end; i++)
        {
            if (i > first) builder.Append('\n');
            builder.Append(Lines[i].Text);
        }
        return builder.ToString();
    }

    public override string ToString() => Title;
}
