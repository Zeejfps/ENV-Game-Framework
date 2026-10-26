// Adapted from ILSpy's CSharpILMixedLanguage (MIT License, Copyright (c) AlphaSierraPapa for the SharpDevelop Team).

using System.Reflection.Metadata;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Metadata;
using SequencePoint = ICSharpCode.Decompiler.DebugInfo.SequencePoint;

namespace ZGF.ILSpy.Core;

/// <summary>
/// Disassembles a method's body with the C# it decompiles to written above the instructions: each
/// statement's lines before the instruction it starts at, as comments. Written to a
/// <see cref="DocumentBuilder"/>, the statement keeps C#'s colors and the rest of its lines are dimmed.
/// A method whose C# cannot be decompiled says so above its first instruction.
/// </summary>
internal sealed class MethodBodyWithCSharp : MethodBodyDisassembler
{
    private const string NoCSharp = "(no C# code)";
    private const string CommentPrefix = "// ";

    private readonly LoadedAssembly _assembly;
    private readonly DecompilerSettings _settings;
    private readonly CancellationToken _cancellationToken;
    private List<SequencePoint> _points = [];
    private DecompiledDocument? _csharp;
    private string? _failure;
    private int _next;

    public MethodBodyWithCSharp(ITextOutput output, LoadedAssembly assembly, DecompilerSettings settings, CancellationToken cancellationToken)
        : base(output, cancellationToken)
    {
        _assembly = assembly;
        _settings = settings;
        _cancellationToken = cancellationToken;
    }

    public override void Disassemble(MetadataFile module, MethodDefinitionHandle handle)
    {
        try
        {
            DecompileToCSharp(handle);
            base.Disassemble(module, handle);
        }
        finally
        {
            Forget();
        }
    }

    /// <summary>Decompiles the method to C# and finds where its statements start in IL; on failure,
    /// keeps why for the first instruction to say.</summary>
    private void DecompileToCSharp(MethodDefinitionHandle handle)
    {
        try
        {
            var decompiler = NodeDecompiler.CreateCSharpDecompiler(_assembly, _settings, _cancellationToken);
            var syntaxTree = decompiler.Decompile(handle);
            var builder = new DocumentBuilder(DecompilationLanguage.CSharp, _assembly);
            NodeDecompiler.WriteLocatedSyntaxTree(syntaxTree, builder, _settings);
            _csharp = builder.Build("");
            _points = FindPoints(decompiler.CreateSequencePoints(syntaxTree), handle);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _failure = $"(C# could not be decompiled: {e.Message})";
        }
    }

    /// <summary>The sequence points of the function <paramref name="handle"/> compiled to: the method
    /// itself, or for an async method or iterator, its state machine's MoveNext.</summary>
    private static List<SequencePoint> FindPoints(Dictionary<ILFunction, List<SequencePoint>> points, MethodDefinitionHandle handle)
    {
        foreach (var (function, list) in points)
        {
            var method = function.MoveNextMethod ?? function.Method;
            if (method != null && method.MetadataToken == handle)
                return list;
        }
        return [];
    }

    private void Forget()
    {
        _points = [];
        _csharp = null;
        _failure = null;
        _next = 0;
    }

    protected override void WriteInstruction(ITextOutput output, MetadataFile metadataFile, MethodDefinitionHandle methodHandle, ref BlobReader blob,
        int methodRva)
    {
        WriteCSharpAt(output, blob.Offset);
        base.WriteInstruction(output, metadataFile, methodHandle, ref blob, methodRva);
    }

    /// <summary>Writes the statements that start at IL offset <paramref name="offset"/>; the
    /// instructions come in order, so the points before it are passed by.</summary>
    private void WriteCSharpAt(ITextOutput output, int offset)
    {
        WriteFailureAt(output, offset);
        while (_next < _points.Count && _points[_next].Offset < offset)
            _next++;
        while (_next < _points.Count && _points[_next].Offset == offset)
        {
            WriteStatement(output, _points[_next]);
            _next++;
        }
    }

    private void WriteFailureAt(ITextOutput output, int offset)
    {
        if (_failure == null)
            return;
        if (offset != 0)
            return;
        WriteComment(output, _failure);
    }

    private void WriteStatement(ITextOutput output, SequencePoint point)
    {
        if (point.IsHidden)
        {
            WriteComment(output, NoCSharp);
            return;
        }
        var lines = _csharp!.Lines;
        var indent = IndentOf(lines[point.StartLine - 1].Text);
        for (var line = point.StartLine; line <= point.EndLine; line++)
            WriteStatementLine(output, point, lines[line - 1], line, indent);
    }

    /// <summary>Writes a line of the statement, less the indentation its first line has: in C#'s colors
    /// to a <see cref="DocumentBuilder"/>, as plain text to any other output.</summary>
    private static void WriteStatementLine(ITextOutput output, SequencePoint point, DocumentLine line, int number, int indent)
    {
        var text = line.Text;
        var from = Math.Min(indent, IndentOf(text));
        var start = StartColumnOn(point, number, text);
        var end = EndColumnOn(point, number, text);
        if (output is DocumentBuilder builder)
        {
            builder.WriteSourceLine(line, from, start, end);
            return;
        }
        WriteComment(output, text.Substring(from));
    }

    /// <summary>Where the statement starts on line <paramref name="number"/>, from column 0: its start
    /// on its first line, else the line's start.</summary>
    private static int StartColumnOn(SequencePoint point, int number, string text)
    {
        if (number != point.StartLine)
            return 0;
        return Math.Clamp(point.StartColumn - 1, 0, text.Length);
    }

    /// <summary>Where the statement ends on line <paramref name="number"/>, from column 0: its end on
    /// its last line, else the line's end.</summary>
    private static int EndColumnOn(SequencePoint point, int number, string text)
    {
        if (number != point.EndLine)
            return text.Length;
        return Math.Clamp(point.EndColumn - 1, 0, text.Length);
    }

    private static int IndentOf(string text)
    {
        var indent = 0;
        while (indent < text.Length && char.IsWhiteSpace(text[indent]))
            indent++;
        return indent;
    }

    private static void WriteComment(ITextOutput output, string text)
    {
        output.WriteLine(CommentPrefix + text);
    }
}
