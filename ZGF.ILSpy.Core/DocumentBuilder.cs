using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

internal sealed class DocumentBuilder : ITextOutput
{
    private readonly DecompilationLanguage _language;
    private readonly StringBuilder _line = new();
    private readonly List<DocumentSpan> _spans = new();
    private readonly List<(string Text, int First, int Count)> _lines = new();
    private readonly Dictionary<CodeReference, int> _definitions = new();
    private readonly List<DocumentSpan> _lexed = new();
    private readonly Dictionary<object, int> _localKeys = new();
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
        return new DecompiledDocument(title, _language, isError, lines.MoveToImmutable(), _definitions);
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
        var kind = Kind ?? (reference is null ? SpanKind.Plain : isDefinition && text.StartsWith('.') ? SpanKind.Keyword : SpanKindOf(metadata, handle));
        WriteReferenceSpan(text, kind, reference, isDefinition);
    }

    public void WriteReference(IType type, string text, bool isDefinition = false)
    {
        var definition = type.GetDefinition();
        var reference = definition is null ? null : ReferenceTo(definition, CodeReferenceKind.Type, text);
        WriteReferenceSpan(text, Kind ?? SpanKind.Type, reference, isDefinition);
    }

    public void WriteReference(IMember member, string text, bool isDefinition = false)
    {
        var definition = member.MemberDefinition ?? member;
        var kind = ReferenceKindOf(definition.SymbolKind);
        var reference = ReferenceTo(definition, kind, text);
        WriteReferenceSpan(text, Kind ?? SpanKindOf(kind), reference, isDefinition);
    }

    public void WriteLocalReference(string text, object reference, bool isDefinition = false)
    {
        var (referenceKind, spanKind) = reference switch
        {
            int => (CodeReferenceKind.Label, SpanKind.ILLabel),
            string s when s.StartsWith("param_", StringComparison.Ordinal) => (CodeReferenceKind.Local, SpanKind.Parameter),
            ILVariable { Kind: VariableKind.Parameter } => (CodeReferenceKind.Local, SpanKind.Parameter),
            IParameter => (CodeReferenceKind.Local, SpanKind.Parameter),
            _ => (CodeReferenceKind.Local, SpanKind.Local),
        };
        var scope = reference is ILVariable ? null : _scope;
        if (!_localKeys.TryGetValue(reference, out var key))
            _localKeys.Add(reference, key = _localKeys.Count);
        Write(text, Kind ?? spanKind, CodeReference.ToLocal(referenceKind, text, key, scope), isDefinition);
    }

    public void MarkFoldStart(string collapsedText = "...", bool defaultCollapsed = false, bool isDefinition = false)
    {
    }

    public void MarkFoldEnd()
    {
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

    private static CodeReference? ReferenceTo(IEntity definition, CodeReferenceKind kind, string text)
    {
        var file = definition.ParentModule?.MetadataFile;
        if (file is null || definition.MetadataToken.IsNil) return null;
        return CodeReference.ToEntity(kind, text, file.FileName, definition.MetadataToken);
    }

    private CodeReference? ReferenceTo(MetadataFile metadata, Handle handle, string text)
    {
        if (handle.IsNil) return null;
        if (handle.Kind is HandleKind.TypeReference or HandleKind.TypeSpecification or HandleKind.MemberReference or HandleKind.MethodSpecification
            && ResolveDefinition(metadata, (EntityHandle)handle) is { } definition)
            return ReferenceTo(definition, ReferenceKindOf(definition.SymbolKind), text);
        return handle.Kind switch
        {
            HandleKind.AssemblyReference =>
                CodeReference.ToEntity(CodeReferenceKind.Assembly, text, metadata.FileName, (AssemblyReferenceHandle)handle),
            HandleKind.TypeDefinition or HandleKind.TypeReference or HandleKind.TypeSpecification =>
                CodeReference.ToEntity(CodeReferenceKind.Type, text, metadata.FileName, (EntityHandle)handle),
            HandleKind.MethodDefinition or HandleKind.MethodSpecification =>
                CodeReference.ToEntity(CodeReferenceKind.Method, text, metadata.FileName, (EntityHandle)handle),
            HandleKind.MemberReference =>
                CodeReference.ToEntity(
                    metadata.Metadata.GetMemberReference((MemberReferenceHandle)handle).GetKind() == MemberReferenceKind.Field
                        ? CodeReferenceKind.Field
                        : CodeReferenceKind.Method,
                    text, metadata.FileName, (EntityHandle)handle),
            HandleKind.FieldDefinition => CodeReference.ToEntity(CodeReferenceKind.Field, text, metadata.FileName, (EntityHandle)handle),
            HandleKind.PropertyDefinition => CodeReference.ToEntity(CodeReferenceKind.Property, text, metadata.FileName, (EntityHandle)handle),
            HandleKind.EventDefinition => CodeReference.ToEntity(CodeReferenceKind.Event, text, metadata.FileName, (EntityHandle)handle),
            _ => null,
        };
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

    private static SpanKind SpanKindOf(MetadataFile metadata, Handle handle) =>
        handle.Kind switch
        {
            HandleKind.AssemblyReference => SpanKind.Plain,
            HandleKind.TypeDefinition or HandleKind.TypeReference or HandleKind.TypeSpecification => SpanKind.Type,
            HandleKind.FieldDefinition => SpanKind.Field,
            HandleKind.PropertyDefinition => SpanKind.Property,
            HandleKind.EventDefinition => SpanKind.Event,
            HandleKind.MemberReference when metadata.Metadata.GetMemberReference((MemberReferenceHandle)handle).GetKind() == MemberReferenceKind.Field =>
                SpanKind.Field,
            _ => SpanKind.Method,
        };

    internal static CodeReferenceKind ReferenceKindOf(SymbolKind kind) =>
        kind switch
        {
            SymbolKind.TypeDefinition => CodeReferenceKind.Type,
            SymbolKind.Field => CodeReferenceKind.Field,
            SymbolKind.Property or SymbolKind.Indexer => CodeReferenceKind.Property,
            SymbolKind.Event => CodeReferenceKind.Event,
            _ => CodeReferenceKind.Method,
        };

    internal static SpanKind SpanKindOf(CodeReferenceKind kind) =>
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
