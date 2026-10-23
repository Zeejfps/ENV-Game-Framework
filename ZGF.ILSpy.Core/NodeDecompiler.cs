using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Output;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Core.Resources;

namespace ZGF.ILSpy.Core;

internal static class NodeDecompiler
{
    private const int MaxResourceBytes = 64 * 1024;
    private const string ReferenceAssemblyAttribute = "System.Runtime.CompilerServices.ReferenceAssemblyAttribute";
    private const string ReferenceAssemblyWarning =
        "This assembly is marked as 'reference assembly', which means that it only contains metadata and no executable code.";

    /// <summary>Decompiles <paramref name="node"/>, after <paramref name="note"/> as a comment when given.</summary>
    public static DecompiledDocument Decompile(BrowseNode node, DecompilationLanguage language, DecompilerOptions options, CancellationToken cancellationToken,
        string? note = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var builder = new DocumentBuilder(language, node.Assembly);
        try
        {
            if (note != null)
                WriteComment(builder, note);
            var settings = Settings.Create(options);
            if (language == DecompilationLanguage.CSharp)
                WriteCSharp(node, builder, settings, cancellationToken);
            else
                WriteIL(node, builder, settings, cancellationToken);
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

    private static void WriteCSharp(BrowseNode node, DocumentBuilder output, DecompilerSettings settings, CancellationToken cancellationToken)
    {
        switch (node)
        {
            case AssemblyNode:
                WriteAssemblySummary(node.Assembly, output);
                WriteSyntaxTree(node.Assembly, output, settings, d => d.DecompileModuleAndAssemblyAttributes(), cancellationToken);
                break;
            case TypeNode type:
                WriteHeader(node.Assembly, output, type.Type, ConversionFlags.UseFullyQualifiedTypeNames | ConversionFlags.UseFullyQualifiedEntityNames);
                WriteSyntaxTree(node.Assembly, output, settings, d => d.Decompile(type.Type.MetadataToken), cancellationToken);
                break;
            case MemberNode { Member: { DeclaringTypeDefinition: { } declaring } member }:
                WriteHeader(node.Assembly, output, member.DeclaringType, ConversionFlags.UseFullyQualifiedEntityNames | ConversionFlags.UseFullyQualifiedTypeNames);
                WriteSyntaxTree(node.Assembly, output, settings, d => DecompileMember(d, member, declaring), cancellationToken);
                break;
            case EntityNode entity:
                WriteSyntaxTree(node.Assembly, output, settings, d => d.Decompile(entity.Entity.MetadataToken), cancellationToken);
                break;
            default:
                WriteSummary(node, output, cancellationToken);
                break;
        }
    }

    private static void WriteIL(BrowseNode node, DocumentBuilder output, DecompilerSettings settings, CancellationToken cancellationToken)
    {
        var file = node.Assembly.File;
        var disassembler = new ReflectionDisassembler(output, cancellationToken)
        {
            DetectControlStructure = true,
            ExpandMemberDefinitions = true,
            AssemblyResolver = node.Assembly.Resolver,
            DebugInfo = DebugSymbolsFor(node.Assembly, settings),
        };
        switch (node)
        {
            case AssemblyNode:
                WriteAssemblySummary(node.Assembly, output);
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
                WriteSummary(node, output, cancellationToken);
                break;
        }
    }

    /// <summary>The comments ILSpy opens a type or member with: a warning for a reference assembly,
    /// the assembly's full name, and the type.</summary>
    private static void WriteHeader(LoadedAssembly assembly, DocumentBuilder output, IType type, ConversionFlags flags)
    {
        if (assembly.Module.GetAssemblyAttributes().Any(static a => a.AttributeType.FullName == ReferenceAssemblyAttribute))
            WriteComment(output, ReferenceAssemblyWarning);
        WriteComment(output, assembly.FullName);
        var ambience = new CSharpAmbience { ConversionFlags = ConversionFlags.ShowTypeParameterList | ConversionFlags.PlaceReturnTypeAfterParameterList | flags };
        WriteComment(output, type is ITypeDefinition definition ? ambience.ConvertSymbol(definition) : ambience.ConvertType(type));
    }

    /// <summary>Decompiles <paramref name="member"/> as ILSpy shows it on its own: a constructor of a
    /// class or a field that is not constant with the members whose initializers it shares.</summary>
    private static SyntaxTree DecompileMember(CSharpDecompiler decompiler, IMember member, ITypeDefinition declaring)
    {
        var module = decompiler.TypeSystem.MainModule;
        member = (IMember)module.ResolveEntity(member.MetadataToken);
        declaring = module.GetDefinition((TypeDefinitionHandle)declaring.MetadataToken);
        switch (member)
        {
            case IMethod { IsConstructor: true } ctor when ctor.DeclaringType.IsReferenceType != false:
                decompiler.AstTransforms.Add(new MemberSelection.Constructor(ctor));
                return decompiler.Decompile(MemberSelection.FieldsAndConstructors(declaring, ctor.IsStatic));
            case IField { IsConst: false } field:
                decompiler.AstTransforms.Add(new MemberSelection.Field(field));
                return decompiler.Decompile(MemberSelection.FieldsAndConstructors(declaring, field.IsStatic));
            default:
                return decompiler.Decompile(member.MetadataToken);
        }
    }

    private static void WriteSyntaxTree(LoadedAssembly assembly, DocumentBuilder output, DecompilerSettings settings,
        Func<CSharpDecompiler, SyntaxTree> decompile, CancellationToken cancellationToken)
    {
        var typeSystem = assembly.TypeSystemFor(settings);
        var decompiler = new CSharpDecompiler(typeSystem, settings)
        {
            CancellationToken = cancellationToken,
            DebugInfoProvider = DebugSymbolsFor(assembly, settings),
        };
        var syntaxTree = decompile(decompiler);
        cancellationToken.ThrowIfCancellationRequested();
        syntaxTree.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
        output.IndentationString = settings.CSharpFormattingOptions.IndentationString;
        TokenWriter writer = new TextTokenWriter(output, settings);
        writer = new HighlightingTokenWriter(writer, output);
        syntaxTree.AcceptVisitor(new CSharpOutputVisitor(writer, settings.CSharpFormattingOptions));
    }

    private static IDebugInfoProvider? DebugSymbolsFor(LoadedAssembly assembly, DecompilerSettings settings)
    {
        if (!settings.UseDebugSymbols)
            return null;
        return assembly.DebugSymbols;
    }

    private static void WriteAssemblySummary(LoadedAssembly assembly, DocumentBuilder output)
    {
        var file = assembly.File;
        WriteComment(output, assembly.FilePath);
        WriteComment(output, assembly.FullName);
        if (file.Metadata.IsAssembly && file.Reader.PEHeaders.CorHeader is { } corHeader)
        {
            var entryPoint = corHeader.EntryPointTokenOrRelativeVirtualAddress;
            if (entryPoint != 0 && (corHeader.Flags & CorFlags.NativeEntryPoint) == 0)
            {
                var handle = MetadataTokens.EntityHandle(entryPoint);
                if (handle.Kind == HandleKind.MethodDefinition && assembly.FindNode(handle) is { Reference: { } reference } entry)
                {
                    output.Write("// Entry point: ", SpanKind.Comment);
                    output.Write(entry.FullName, SpanKind.Method, reference);
                    output.WriteLine();
                }
            }
        }
        WriteComment(output, $"Architecture: {ArchitectureOf(file.Reader.PEHeaders)}");
        if (assembly.TargetFramework is { } framework)
            WriteComment(output, $"Target framework: {framework}");
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

    private static void WriteSummary(BrowseNode node, DocumentBuilder output, CancellationToken cancellationToken)
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
                    output.Write("// ", SpanKind.Comment);
                    output.Write(reference.FullName, SpanKind.Plain, reference.Target);
                    output.WriteLine();
                }
                break;
            case AssemblyReferenceNode reference:
                WriteComment(output, reference.FullName);
                var path = node.Assembly.Resolver.FindFile(reference.AssemblyReference);
                WriteComment(output, path is null ? "Not found" : $"Resolves to {path}");
                break;
            case ResourcesFolderNode:
                foreach (var child in node.Children)
                {
                    if (child is IResourceData resource)
                        WriteComment(output, resource.Description);
                }
                break;
            case IResourceData resource:
                WriteResource(node.Text, resource, output);
                break;
            case MetadataTextNode metadata:
                foreach (var line in metadata.Lines())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (line.Length == 0)
                        output.WriteLine();
                    else
                        WriteComment(output, line);
                }
                break;
            case BaseTypesNode or DerivedTypesNode:
                foreach (var child in node.Children)
                {
                    output.Write(child.Text, SpanKind.Type, child.Target);
                    output.WriteLine();
                }
                break;
            case BaseTypeNode or DerivedTypeNode:
                output.Write(node.Text, SpanKind.Type, node.Target);
                output.WriteLine();
                break;
            case ErrorNode error:
                foreach (var line in error.Error.ToString().Split('\n'))
                    WriteComment(output, line.TrimEnd('\r'));
                break;
            default:
                WriteComment(output, node.Text);
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
            output.Write(declaration, SpanKind.Plain, node.Reference);
        }
        else
        {
            output.Write(declaration[..nameAt], SpanKind.Keyword);
            output.Write(label, SpanKind.Type, node.Reference);
            output.Write(declaration[(nameAt + label.Length)..], SpanKind.Plain);
        }
        output.WriteLine();
    }

    /// <summary>A text resource as its text, colored as XML or JSON when it is that, as ILSpy shows
    /// it; anything else as a description and a hex dump of its first bytes.</summary>
    private static void WriteResource(string name, IResourceData resource, DocumentBuilder output)
    {
        var data = resource.ReadData();
        if (ResourceText.Detect(name, data) is var (text, syntax))
        {
            ResourceText.Write(output, text, syntax);
            output.WriteLine();
            return;
        }
        WriteComment(output, resource.Description);
        output.WriteLine();
        WriteHexDump(data.AsSpan(0, Math.Min(data.Length, MaxResourceBytes)), output);
        if (data.Length > MaxResourceBytes)
            WriteComment(output, $"Truncated after {MaxResourceBytes} bytes.");
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

    private static void WriteComment(DocumentBuilder output, string text)
    {
        output.Write($"// {text}", SpanKind.Comment);
        output.WriteLine();
    }
}
