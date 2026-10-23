using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

internal sealed class DocumentBuilder : ITextOutput
{
    private const int MaxSignatureLines = 8;
    private const string DefaultFoldText = "...";

    private readonly DecompilationLanguage _language;
    private readonly StringBuilder _line = new();
    private readonly List<DocumentSpan> _spans = new();
    private readonly List<(string Text, int First, int Count)> _lines = new();
    private readonly Dictionary<CodeReference, int> _definitions = new();
    private readonly Dictionary<int, (CodeReference Method, int Offset)> _statements = new();
    private readonly List<DocumentSpan> _lexed = new();
    private readonly Dictionary<object, int> _localKeys = new();
    private readonly Stack<OpenFold> _openFolds = new();
    private readonly List<(OpenFold Start, int EndLine)> _folds = new();
    private readonly LoadedAssembly? _assembly;
    private int _lineFirstSpan;
    private int _indent;
    private bool _lineStarted;
    private CodeReference? _scope;

    public DocumentBuilder(DecompilationLanguage language, LoadedAssembly? assembly = null)
    {
        _language = language;
        _assembly = assembly;
    }

    public string IndentationString { get; set; } = "    ";

    public SpanKind? Kind { get; set; }

    public DecompiledDocument Build(string title, bool isError = false)
    {
        if (_line.Length > 0 || _lineFirstSpan < _spans.Count || _lines.Count == 0)
            WriteLine();
        var spans = _spans.ToArray();
        var lines = ImmutableArray.CreateBuilder<DocumentLine>(_lines.Count);
        foreach (var (text, first, count) in _lines)
            lines.Add(new DocumentLine(text, spans, first, count));
        var built = lines.MoveToImmutable();
        return new DecompiledDocument(title, _language, isError, built, _definitions, Statements(), FoldRegions(built));
    }

    public void Write(string text, SpanKind kind, CodeReference? reference = null, bool isDefinition = false)
    {
        var start = 0;
        while (true)
        {
            var newline = text.IndexOf('\n', start);
            var end = newline < 0 ? text.Length : newline;
            var segmentEnd = end > start && text[end - 1] == '\r' ? end - 1 : end;
            Append(text.AsSpan(start, segmentEnd - start), kind, reference, isDefinition);
            if (newline < 0) return;
            WriteLine();
            start = newline + 1;
        }
    }

    public void Indent() => _indent++;

    public void Unindent() => _indent = Math.Max(0, _indent - 1);

    public void Write(char ch)
    {
        if (ch == '\n')
            WriteLine();
        else if (ch != '\r')
            Append(new ReadOnlySpan<char>(in ch), Kind ?? SpanKind.Plain, null, false);
    }

    public void Write(string text) => Write(text, Kind ?? SpanKind.Plain);

    public void WriteLine()
    {
        if (_language == DecompilationLanguage.IL)
            LexILLine();
        _lines.Add((_line.ToString(), _lineFirstSpan, _spans.Count - _lineFirstSpan));
        _line.Clear();
        _lineFirstSpan = _spans.Count;
        _lineStarted = false;
    }

    public void WriteReference(OpCodeInfo opCode, bool omitSuffix = false)
    {
        var name = opCode.Name;
        if (omitSuffix)
        {
            var lastDot = name.LastIndexOf('.');
            if (lastDot > 0) name = name[..(lastDot + 1)];
        }
        Write(name, SpanKind.ILOpcode);
    }

    public void WriteReference(MetadataFile metadata, Handle handle, string text, string protocol = "decompile", bool isDefinition = false)
    {
        var reference = ReferenceTo(metadata, handle, text);
        var kind = Kind ?? (reference is null ? SpanKind.Plain : isDefinition && text.StartsWith('.') ? SpanKind.Keyword : SpanKindOf(reference.Kind));
        WriteReferenceSpan(text, kind, reference, isDefinition);
    }

    public void WriteReference(IType type, string text, bool isDefinition = false)
    {
        var reference = type.GetDefinition() is { } definition ? CodeReference.To(definition, text) : null;
        WriteReferenceSpan(text, Kind ?? SpanKind.Type, reference, isDefinition);
    }

    public void WriteReference(IMember member, string text, bool isDefinition = false)
    {
        var definition = member.MemberDefinition ?? member;
        var kind = Kind ?? SpanKindOf(CodeReference.KindOf(definition.SymbolKind));
        WriteReferenceSpan(text, kind, CodeReference.To(definition, text), isDefinition);
    }

    public void WriteLocalReference(string text, object reference, bool isDefinition = false, bool isHoverOnly = false)
    {
        var (referenceKind, spanKind) = reference switch
        {
            int => (CodeReferenceKind.Label, SpanKind.ILLabel),
            string s when s.StartsWith("param_", StringComparison.Ordinal) => (CodeReferenceKind.Local, SpanKind.Parameter),
            ILVariable { Kind: VariableKind.Parameter } => (CodeReferenceKind.Local, SpanKind.Parameter),
            IParameter => (CodeReferenceKind.Local, SpanKind.Parameter),
            _ => (CodeReferenceKind.Local, SpanKind.Local),
        };
        if (isDefinition && reference is int offset && _scope is { Kind: CodeReferenceKind.Method } method)
            MarkStatement(method, offset);
        var scope = reference is ILVariable ? null : _scope;
        if (!_localKeys.TryGetValue(reference, out var key))
            _localKeys.Add(reference, key = _localKeys.Count);
        Write(text, Kind ?? spanKind, CodeReference.ToLocal(referenceKind, text, key, scope), isDefinition);
    }

    /// <summary>Marks the line being written as where the statement at <paramref name="ilOffset"/> of
    /// <paramref name="method"/> starts. A line keeps the first method marked on it and its lowest offset.</summary>
    public void MarkStatement(IMethod method, int ilOffset)
    {
        if (CodeReference.To(method) is { } reference)
            MarkStatement(reference, ilOffset);
    }

    private void MarkStatement(CodeReference method, int ilOffset)
    {
        var line = _lines.Count;
        if (_statements.TryGetValue(line, out var marked) && (marked.Method != method || marked.Offset <= ilOffset)) return;
        _statements[line] = (method, ilOffset);
    }

    private Dictionary<int, CodeAnchor> Statements()
    {
        var anchors = new Dictionary<int, CodeAnchor>(_statements.Count);
        foreach (var (line, (method, offset)) in _statements)
        {
            if (method.FilePath is { } file)
                anchors[line] = new CodeAnchor(CodeAnchorKind.Statement, file, MetadataTokens.GetToken(method.Handle), offset);
        }
        return anchors;
    }

    public void MarkDefinitionStart()
    {
    }

    public void MarkFoldStart(string collapsedText = DefaultFoldText, bool defaultCollapsed = false, bool isDefinition = false) =>
        _openFolds.Push(new OpenFold(_lines.Count, _lineStarted ? _line.Length : 0, collapsedText, defaultCollapsed, isDefinition));

    public void MarkFoldEnd()
    {
        if (_openFolds.TryPop(out var start))
            _folds.Add((start, _lineStarted ? _lines.Count : _lines.Count - 1));
    }

    private readonly record struct OpenFold(int Line, int Column, string Text, bool DefaultCollapsed, bool IsDefinition);

    private ImmutableArray<FoldRegion> FoldRegions(ImmutableArray<DocumentLine> lines)
    {
        if (_folds.Count == 0) return ImmutableArray<FoldRegion>.Empty;
        var regions = new List<FoldRegion>(_folds.Count);
        foreach (var (start, endLine) in _folds)
        {
            if (endLine <= start.Line - 1 || endLine >= lines.Length) continue;
            var closes = lines[endLine].Text.AsSpan().TrimStart().StartsWith("}");
            var opening = FirstCharFrom(lines, start.Line, start.Column, endLine, out var openingLine);
            var ownLine = lines[start.Line].Text.AsSpan(0, Math.Min(start.Column, lines[start.Line].Text.Length)).IsWhiteSpace();
            if (opening != '{' && closes)
                openingLine = BodyAfterSignature(lines, start.Line, endLine);
            var brace = closes && openingLine >= 0;
            var header = !brace ? start.Line
                : openingLine > start.Line ? openingLine - 1
                : ownLine && start.Line > 0 ? start.Line - 1
                : start.Line;
            if (endLine <= header) continue;
            var text = HasOwnText(start, lines[header]) ? start.Text
                : !brace ? "…"
                : openingLine > header ? "{ … }"
                : "… " + lines[endLine].Text.Trim();
            regions.Add(new FoldRegion(header, endLine, text, start.DefaultCollapsed, start.IsDefinition));
        }
        regions.Sort(static (a, b) => a.StartLine != b.StartLine ? a.StartLine.CompareTo(b.StartLine) : b.EndLine.CompareTo(a.EndLine));

        var nested = ImmutableArray.CreateBuilder<FoldRegion>(regions.Count);
        var open = new Stack<FoldRegion>();
        foreach (var region in regions)
        {
            while (open.Count > 0 && open.Peek().EndLine < region.StartLine)
                open.Pop();
            if (nested.Count > 0 && nested[^1].StartLine == region.StartLine) continue;
            if (open.Count > 0 && region.EndLine > open.Peek().EndLine) continue;
            nested.Add(region);
            open.Push(region);
        }
        return nested.ToImmutable();
    }

    /// <summary>Whether <paramref name="start"/> names a collapsed text that says more than its
    /// header line, which stays in view; an XML doc comment names its own first line.</summary>
    private static bool HasOwnText(OpenFold start, DocumentLine header)
    {
        if (start.Text == DefaultFoldText)
            return false;
        return start.Text.Trim() != header.Text.Trim();
    }

    private static int BodyAfterSignature(ImmutableArray<DocumentLine> lines, int start, int endLine)
    {
        for (var line = start + 1; line < endLine && line <= start + MaxSignatureLines; line++)
        {
            var text = lines[line].Text.AsSpan().Trim();
            if (text.StartsWith("{")) return text.Length == 1 ? line : -1;
        }
        return -1;
    }

    private static char FirstCharFrom(ImmutableArray<DocumentLine> lines, int line, int column, int lastLine, out int foundLine)
    {
        for (foundLine = line; foundLine <= lastLine; foundLine++, column = 0)
        {
            var text = lines[foundLine].Text;
            for (var i = column; i < text.Length; i++)
            {
                if (!char.IsWhiteSpace(text[i])) return text[i];
            }
        }
        foundLine = -1;
        return ' ';
    }

    private void WriteReferenceSpan(string text, SpanKind kind, CodeReference? reference, bool isDefinition)
    {
        if (isDefinition && reference is { Kind: CodeReferenceKind.Method })
            _scope = reference;
        Write(text, kind, reference, isDefinition);
    }

    private void Append(ReadOnlySpan<char> text, SpanKind kind, CodeReference? reference, bool isDefinition)
    {
        if (text.IsEmpty) return;
        if (!_lineStarted)
        {
            _lineStarted = true;
            for (var i = 0; i < _indent; i++)
                AppendSpan(IndentationString, SpanKind.Plain, null, false);
        }
        if (isDefinition && reference is { IsNavigable: true })
            _definitions.TryAdd(reference, _lines.Count);
        AppendSpan(text, kind, reference, isDefinition);
    }

    private void AppendSpan(ReadOnlySpan<char> text, SpanKind kind, CodeReference? reference, bool isDefinition)
    {
        if (text.IsEmpty) return;
        var start = _line.Length;
        _line.Append(text);
        if (reference is null && !isDefinition && _spans.Count > _lineFirstSpan)
        {
            var last = _spans[^1];
            if (last.Kind == kind && last.Reference is null && !last.IsDefinition && last.End == start)
            {
                _spans[^1] = last with { Length = last.Length + text.Length };
                return;
            }
        }
        _spans.Add(new DocumentSpan(start, text.Length, kind, reference, isDefinition));
    }

    private void LexILLine()
    {
        var count = _spans.Count - _lineFirstSpan;
        var any = false;
        for (var i = 0; i < count && !any; i++)
            any = _spans[_lineFirstSpan + i] is { Kind: SpanKind.Plain, Reference: null };
        if (!any) return;

        _lexed.Clear();
        var text = _line.ToString();
        var inComment = false;
        for (var i = 0; i < count; i++)
        {
            var span = _spans[_lineFirstSpan + i];
            if (span.Kind != SpanKind.Plain || span.Reference is not null)
            {
                _lexed.Add(inComment && span.Reference is null ? span with { Kind = SpanKind.Comment } : span);
                continue;
            }
            ILLexer.Classify(text, span.Start, span.End, ref inComment, _lexed);
        }
        _spans.RemoveRange(_lineFirstSpan, count);
        _spans.AddRange(_lexed);
    }

    private CodeReference? ReferenceTo(MetadataFile metadata, Handle handle, string text)
    {
        if (handle.IsNil) return null;
        if (handle.Kind is HandleKind.TypeReference or HandleKind.TypeSpecification or HandleKind.MemberReference or HandleKind.MethodSpecification
            && ResolveDefinition(metadata, (EntityHandle)handle) is { } definition)
            return CodeReference.To(definition, text);
        return CodeReference.KindOf(metadata.Metadata, handle) is { } kind
            ? CodeReference.ToEntity(kind, text, metadata.FileName, (EntityHandle)handle)
            : null;
    }

    private IEntity? ResolveDefinition(MetadataFile metadata, EntityHandle handle)
    {
        if (_assembly is null || !ReferenceEquals(metadata, _assembly.File)) return null;
        try
        {
            return Definitions.Resolve(_assembly.Module, handle);
        }
        catch (Exception e) when (e is BadImageFormatException or InvalidOperationException or NotSupportedException)
        {
            return null;
        }
    }

    private static SpanKind SpanKindOf(CodeReferenceKind kind) =>
        kind switch
        {
            CodeReferenceKind.Type => SpanKind.Type,
            CodeReferenceKind.Method => SpanKind.Method,
            CodeReferenceKind.Field => SpanKind.Field,
            CodeReferenceKind.Property => SpanKind.Property,
            CodeReferenceKind.Event => SpanKind.Event,
            CodeReferenceKind.Local => SpanKind.Local,
            CodeReferenceKind.Label => SpanKind.ILLabel,
            _ => SpanKind.Plain,
        };
}
