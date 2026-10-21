using System.Collections.Immutable;

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

/// <summary>
/// The immutable result of decompiling a node: lines of classified, referencing text. Safe to read
/// from any thread while other documents are being produced.
/// </summary>
public sealed class DecompiledDocument
{
    private readonly IReadOnlyDictionary<CodeReference, int> _definitions;

    internal DecompiledDocument(string title, DecompilationLanguage language, bool isError, ImmutableArray<DocumentLine> lines,
        IReadOnlyDictionary<CodeReference, int> definitions)
    {
        Title = title;
        Language = language;
        IsError = isError;
        Lines = lines;
        _definitions = definitions;
    }

    /// <summary>A document with no lines.</summary>
    public static DecompiledDocument Empty { get; } = FromText("", "");

    /// <summary>A short name for the document, e.g. for a tab.</summary>
    public string Title { get; }

    public DecompilationLanguage Language { get; }

    /// <summary>Whether decompilation failed and the lines describe the error.</summary>
    public bool IsError { get; }

    public ImmutableArray<DocumentLine> Lines { get; }

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
