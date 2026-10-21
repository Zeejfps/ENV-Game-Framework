using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ZGF.ILSpy.Core;

internal static class NodeDecompiler
{
    private const int MaxResourceBytes = 64 * 1024;

    public static DecompiledDocument Decompile(BrowseNode node, DecompilationLanguage language, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var builder = new DocumentBuilder(language, node.Assembly);
        try
        {
            if (language == DecompilationLanguage.CSharp)
                WriteCSharp(node, builder, cancellationToken);
            else
                WriteIL(node, builder, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return builder.Build(node.Text);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            var error = new DocumentBuilder(language);
            error.Write($"// Error decompiling {node.FullName}", SpanKind.Comment);
            error.WriteLine();
            error.Write(e.ToString(), SpanKind.Comment);
            return error.Build(node.Text, isError: true);
        }
    }

    private static void WriteCSharp(BrowseNode node, DocumentBuilder output, CancellationToken cancellationToken)
    {
        switch (node)
        {
            case AssemblyNode:
                WriteAssemblySummary(node.Assembly, output, "//");
                WriteSyntaxTree(node.Assembly, output, d => d.DecompileModuleAndAssemblyAttributes(), cancellationToken);
                break;
            case TypeNode type:
                WriteSyntaxTree(node.Assembly, output, d => d.DecompileTypes([(TypeDefinitionHandle)type.Type.MetadataToken]), cancellationToken);
                break;
            case EntityNode entity:
                WriteSyntaxTree(node.Assembly, output, d => d.Decompile(entity.Entity.MetadataToken), cancellationToken);
                break;
            default:
                WriteSummary(node, output, "//", cancellationToken);
                break;
        }
    }

    private static void WriteIL(BrowseNode node, DocumentBuilder output, CancellationToken cancellationToken)
    {
        var file = node.Assembly.File;
        var disassembler = new ReflectionDisassembler(output, cancellationToken)
        {
            DetectControlStructure = true,
            AssemblyResolver = node.Assembly.Resolver,
        };
        switch (node)
        {
            case AssemblyNode:
                WriteAssemblySummary(node.Assembly, output, "//");
                output.WriteLine();
                disassembler.WriteAssemblyReferences(file.Metadata);
                if (file.Metadata.IsAssembly)
                    disassembler.WriteAssemblyHeader(file);
                output.WriteLine();
                disassembler.WriteModuleHeader(file);
                break;
            case TypeNode type:
                disassembler.DisassembleType(file, (TypeDefinitionHandle)type.Type.MetadataToken);
                break;
            case EntityNode entity:
                var handle = entity.Entity.MetadataToken;
                switch (handle.Kind)
                {
                    case HandleKind.MethodDefinition:
                        disassembler.DisassembleMethod(file, (MethodDefinitionHandle)handle);
                        break;
                    case HandleKind.FieldDefinition:
                        disassembler.DisassembleField(file, (FieldDefinitionHandle)handle);
                        break;
                    case HandleKind.PropertyDefinition:
                        disassembler.DisassembleProperty(file, (PropertyDefinitionHandle)handle);
                        break;
                    case HandleKind.EventDefinition:
                        disassembler.DisassembleEvent(file, (EventDefinitionHandle)handle);
                        break;
                }
                break;
            default:
                WriteSummary(node, output, "//", cancellationToken);
                break;
        }
    }

    private static void WriteSyntaxTree(LoadedAssembly assembly, DocumentBuilder output, Func<CSharpDecompiler, SyntaxTree> decompile,
        CancellationToken cancellationToken)
    {
        var settings = Settings.Create();
        var typeSystem = assembly.TypeSystem;
        var decompiler = new CSharpDecompiler(typeSystem, settings) { CancellationToken = cancellationToken };
        var syntaxTree = decompile(decompiler);
        cancellationToken.ThrowIfCancellationRequested();
        syntaxTree.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
        output.IndentationString = settings.CSharpFormattingOptions.IndentationString;
        TokenWriter writer = new TextTokenWriter(output, settings, typeSystem);
        writer = new HighlightingTokenWriter(writer, output);
        syntaxTree.AcceptVisitor(new CSharpOutputVisitor(writer, settings.CSharpFormattingOptions));
    }

    private static void WriteAssemblySummary(LoadedAssembly assembly, DocumentBuilder output, string comment)
    {
        var file = assembly.File;
        WriteComment(output, comment, assembly.FilePath);
        WriteComment(output, comment, assembly.FullName);
        if (file.Metadata.IsAssembly && file.Reader.PEHeaders.CorHeader is { } corHeader)
        {
            var entryPoint = corHeader.EntryPointTokenOrRelativeVirtualAddress;
            if (entryPoint != 0 && (corHeader.Flags & CorFlags.NativeEntryPoint) == 0)
            {
                var handle = MetadataTokens.EntityHandle(entryPoint);
                if (handle.Kind == HandleKind.MethodDefinition && assembly.FindNode(handle) is { Reference: { } reference } entry)
                {
                    output.Write(comment + " Entry point: ", SpanKind.Comment);
                    output.Write(entry.FullName, SpanKind.Method, reference);
                    output.WriteLine();
                }
            }
        }
        WriteComment(output, comment, $"Architecture: {ArchitectureOf(file.Reader.PEHeaders)}");
        if (assembly.TargetFramework is { } framework)
            WriteComment(output, comment, $"Target framework: {framework}");
        output.WriteLine();
    }

    private static string ArchitectureOf(PEHeaders headers)
    {
        var machine = headers.CoffHeader.Machine;
        var flags = headers.CorHeader?.Flags ?? 0;
        if (machine != Machine.I386 || (flags & CorFlags.ILOnly) == 0)
            return machine.ToString();
        if ((flags & CorFlags.Requires32Bit) == 0)
            return "AnyCPU (64-bit preferred)";
        return (flags & CorFlags.Prefers32Bit) != 0 ? "AnyCPU (32-bit preferred)" : "x86";
    }

    private static void WriteSummary(BrowseNode node, DocumentBuilder output, string comment, CancellationToken cancellationToken)
    {
        switch (node)
        {
            case NamespaceNode ns:
                output.Write("namespace", SpanKind.Keyword);
                output.Write(" ");
                output.Write(ns.Text, SpanKind.Namespace);
                output.WriteLine();
                output.WriteLine();
                foreach (var child in node.Children)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (child is TypeNode type)
                        WriteTypeDeclaration(output, type);
                }
                break;
            case ReferencesFolderNode:
                foreach (var child in node.Children)
                {
                    if (child is not AssemblyReferenceNode reference) continue;
                    output.Write(comment + " ", SpanKind.Comment);
                    output.Write(reference.FullName, SpanKind.Plain, reference.Target);
                    output.WriteLine();
                }
                break;
            case AssemblyReferenceNode reference:
                WriteComment(output, comment, reference.FullName);
                var path = node.Assembly.Resolver.FindFile(reference.AssemblyReference);
                WriteComment(output, comment, path is null ? "Not found" : $"Resolves to {path}");
                break;
            case ResourcesFolderNode:
                foreach (var child in node.Children)
                {
                    if (child is ResourceNode resource)
                        WriteComment(output, comment, Describe(resource.Resource));
                }
                break;
            case ResourceNode resource:
                WriteResource(resource.Resource, output, comment, cancellationToken);
                break;
            case BaseTypesNode:
                foreach (var child in node.Children)
                {
                    output.Write(child.Text, SpanKind.Type, child.Target);
                    output.WriteLine();
                }
                break;
            case BaseTypeNode baseType:
                output.Write(baseType.Text, SpanKind.Type, baseType.Target);
                output.WriteLine();
                break;
            case ErrorNode error:
                foreach (var line in error.Error.ToString().Split('\n'))
                    WriteComment(output, comment, line.TrimEnd('\r'));
                break;
            default:
                WriteComment(output, comment, node.Text);
                break;
        }
    }

    private static void WriteTypeDeclaration(DocumentBuilder output, TypeNode node)
    {
        var declaration = EntityText.Declaration(node.Type);
        var label = node.Text;
        var nameAt = declaration.LastIndexOf(label, StringComparison.Ordinal);
        if (nameAt < 0)
        {
            output.Write(declaration, SpanKind.Plain, node.Reference, isDefinition: true);
        }
        else
        {
            output.Write(declaration[..nameAt], SpanKind.Keyword);
            output.Write(label, SpanKind.Type, node.Reference, isDefinition: true);
            output.Write(declaration[(nameAt + label.Length)..], SpanKind.Plain);
        }
        output.WriteLine();
    }

    private static void WriteResource(Resource resource, DocumentBuilder output, string comment, CancellationToken cancellationToken)
    {
        WriteComment(output, comment, Describe(resource));
        output.WriteLine();
        using var stream = resource.TryOpenStream();
        if (stream is null) return;

        if (resource.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var file = new ResourcesFile(stream, leaveOpen: true);
                foreach (var (key, value) in file)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    output.Write(key, SpanKind.Field);
                    switch (value)
                    {
                        case string text:
                            output.Write(" = ");
                            output.Write($"\"{TextWriterTokenWriter.ConvertString(text)}\"", SpanKind.String);
                            break;
                        case ResourceSerializedObject serialized:
                            output.Write(" : ");
                            output.Write(serialized.TypeName ?? "object", SpanKind.Type);
                            break;
                        case byte[] bytes:
                            output.Write(" : ");
                            output.Write($"byte[{bytes.Length}]", SpanKind.Type);
                            break;
                        case null:
                            output.Write(" = ");
                            output.Write("null", SpanKind.Keyword);
                            break;
                        default:
                            output.Write(" = ");
                            output.Write(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "", SpanKind.Plain);
                            output.Write(" : ");
                            output.Write(value.GetType().Name, SpanKind.Type);
                            break;
                    }
                    output.WriteLine();
                }
                return;
            }
            catch (Exception e) when (e is BadImageFormatException or EndOfStreamException or InvalidDataException or ArgumentException)
            {
                stream.Position = 0;
            }
        }

        var buffer = new byte[MaxResourceBytes];
        var length = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
        if (TryDecodeText(buffer.AsSpan(0, length), out var decoded))
        {
            output.Write(decoded, SpanKind.Plain);
            output.WriteLine();
        }
        else
        {
            WriteHexDump(buffer.AsSpan(0, length), output);
        }
        if (length == buffer.Length && stream.ReadByte() >= 0)
            WriteComment(output, comment, $"Truncated after {MaxResourceBytes} bytes.");
    }

    private static bool TryDecodeText(ReadOnlySpan<byte> bytes, out string text)
    {
        text = "";
        if (bytes.IndexOf((byte)0) >= 0) return false;
        try
        {
            text = new UTF8Encoding(false, true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return bytes.Length == MaxResourceBytes && TryDecodeText(bytes[..^4], out text);
        }
        foreach (var c in text)
        {
            if (char.IsControl(c) && c is not ('\r' or '\n' or '\t' or '﻿')) return false;
        }
        text = text.TrimStart('﻿');
        return true;
    }

    private static void WriteHexDump(ReadOnlySpan<byte> bytes, DocumentBuilder output)
    {
        var line = new StringBuilder();
        for (var offset = 0; offset < bytes.Length; offset += 16)
        {
            var row = bytes.Slice(offset, Math.Min(16, bytes.Length - offset));
            output.Write(offset.ToString("X8"), SpanKind.ILLabel);
            output.Write("  ");
            line.Clear();
            for (var i = 0; i < 16; i++)
                line.Append(i < row.Length ? row[i].ToString("X2") + " " : "   ");
            output.Write(line.ToString(), SpanKind.Number);
            line.Clear();
            foreach (var b in row)
                line.Append(b is >= 0x20 and < 0x7F ? (char)b : '.');
            output.Write(" " + line, SpanKind.String);
            output.WriteLine();
        }
    }

    private static string Describe(Resource resource)
    {
        var size = resource.TryGetLength() is { } length ? $", {length} bytes" : "";
        return $"{resource.Name} ({resource.ResourceType}{size})";
    }

    private static void WriteComment(DocumentBuilder output, string comment, string text)
    {
        output.Write($"{comment} {text}", SpanKind.Comment);
        output.WriteLine();
    }
}
