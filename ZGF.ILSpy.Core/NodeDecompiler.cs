using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Output;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;
using ZGF.ILSpy.Core.Resources;

namespace ZGF.ILSpy.Core;

internal static class NodeDecompiler
{
    private const int MaxResourceBytes = 64 * 1024;
    private const string UsedIndent = "    ";
    private const string ReferenceAssemblyAttribute = "System.Runtime.CompilerServices.ReferenceAssemblyAttribute";
    private const string ReferenceAssemblyWarning =
        "This assembly is marked as 'reference assembly', which means that it only contains metadata and no executable code.";

    /// <summary>Writes the document <paramref name="request"/> asks for, after <paramref name="note"/>
    /// as a comment when given. Throws <see cref="OperationCanceledException"/> when cancelled; any other
    /// failure becomes a document describing it.</summary>
    public static DecompiledDocument Decompile(DocumentRequest request, DecompilationLanguage language, DecompilerOptions options,
        CancellationToken cancellationToken, string? note = null)
    {
        try
        {
            return DecompileOrThrow(request, language, options, cancellationToken, note);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            var error = new DocumentBuilder(language);
            error.Write($"// Error decompiling {request.FullName}", SpanKind.Comment);
            error.WriteLine();
            error.Write(e.ToString(), SpanKind.Comment);
            return error.Build(request.Title, isError: true);
        }
    }

    private static DecompiledDocument DecompileOrThrow(DocumentRequest request, DecompilationLanguage language, DecompilerOptions options,
        CancellationToken cancellationToken, string? note = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is DocumentRequest.Failure failure)
            return DescribeLoadError(failure, language);
        var builder = new DocumentBuilder(language, ModuleOf(request));
        if (note != null)
            WriteComment(builder, note);
        var settings = Settings.Create(options);
        if (language == DecompilationLanguage.CSharp)
            WriteCSharp(request, builder, settings, cancellationToken);
        else
            WriteIL(request, builder, language, settings, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return builder.Build(request.Title);
    }

    /// <summary>Why the file <paramref name="failure"/> names could not be read.</summary>
    private static DecompiledDocument DescribeLoadError(DocumentRequest.Failure failure, DecompilationLanguage language)
    {
        var error = failure.Assembly.Error;
        var builder = new DocumentBuilder(language);
        WriteComment(builder, error.Message);
        if (error.InnerException != null)
            builder.Write(error.InnerException.ToString(), SpanKind.Comment);
        return builder.Build(failure.Title, isError: true);
    }

    /// <summary>The module the document of <paramref name="request"/> is read from; null for a package's.</summary>
    private static LoadedModule? ModuleOf(DocumentRequest request) => request switch
    {
        DocumentRequest.ModulePreview preview => preview.Module,
        DocumentRequest.Type type => type.Module,
        DocumentRequest.Member member => member.Module,
        DocumentRequest.Summary summary => summary.Module,
        DocumentRequest.Resource resource => resource.Module,
        DocumentRequest.Baml baml => baml.Module,
        DocumentRequest.Failure => null,
        _ => throw new UnreachableException(),
    };

    /// <summary>Writes the code <paramref name="export"/> names to the file at <paramref name="path"/>.
    /// Throws when decompiling fails. The file is replaced only once the code is whole.</summary>
    public static void SaveCode(CodeExport export, DecompilationLanguage language, DecompilerOptions options, string path,
        CancellationToken cancellationToken)
    {
        var temporary = path + ".tmp";
        try
        {
            WriteCode(export, language, options, temporary, cancellationToken);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    private static void WriteCode(CodeExport export, DecompilationLanguage language, DecompilerOptions options, string path,
        CancellationToken cancellationToken)
    {
        switch (export)
        {
            case CodeExport.WholeModule or CodeExport.Namespace:
                WriteWholeCode(export, language, options, path, cancellationToken);
                break;
            case CodeExport.Document document:
                var written = DecompileOrThrow(document.Request, language, options, cancellationToken);
                File.WriteAllText(path, written.GetText());
                break;
            default:
                throw new UnreachableException();
        }
    }

    private static void WriteWholeCode(CodeExport export, DecompilationLanguage language, DecompilerOptions options, string path,
        CancellationToken cancellationToken)
    {
        var settings = Settings.Create(options);
        using var writer = new StreamWriter(path);
        var output = new PlainTextOutput(writer) { IndentationString = settings.CSharpFormattingOptions.IndentationString };
        if (language == DecompilationLanguage.CSharp)
            WriteWholeCSharp(export, output, settings, cancellationToken);
        else
            WriteWholeIL(export, output, language, settings, cancellationToken);
    }

    private static void WriteWholeCSharp(CodeExport export, ITextOutput output, DecompilerSettings settings, CancellationToken cancellationToken)
    {
        var syntaxTree = export switch
        {
            CodeExport.WholeModule whole => CreateCSharpDecompiler(whole.Module, settings, cancellationToken).DecompileWholeModuleAsSingleFile(),
            CodeExport.Namespace ns => CreateCSharpDecompiler(ns.Module, settings, cancellationToken).DecompileTypes(TypeHandles(ns)),
            _ => throw new UnreachableException(),
        };
        cancellationToken.ThrowIfCancellationRequested();
        syntaxTree.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
        var writer = new TextTokenWriter(output, settings);
        syntaxTree.AcceptVisitor(new CSharpOutputVisitor(writer, settings.CSharpFormattingOptions));
    }

    private static void WriteWholeIL(CodeExport export, ITextOutput output, DecompilationLanguage language, DecompilerSettings settings,
        CancellationToken cancellationToken)
    {
        switch (export)
        {
            case CodeExport.WholeModule whole:
                var file = whole.Module.File;
                var disassembler = CreateDisassembler(whole.Module, output, language, settings, cancellationToken);
                disassembler.WriteAssemblyReferences(file.Metadata);
                if (file.Metadata.IsAssembly)
                    disassembler.WriteAssemblyHeader(file);
                output.WriteLine();
                disassembler.WriteModuleHeader(file);
                output.WriteLine();
                disassembler.WriteModuleContents(file);
                break;
            case CodeExport.Namespace ns:
                CreateDisassembler(ns.Module, output, language, settings, cancellationToken)
                    .DisassembleNamespace(ns.Name, ns.Module.File, TypeHandles(ns));
                break;
            default:
                throw new UnreachableException();
        }
    }

    private static List<TypeDefinitionHandle> TypeHandles(CodeExport.Namespace ns)
    {
        return ns.Types
            .Select(static t => (TypeDefinitionHandle)t.MetadataToken)
            .ToList();
    }

    private static void WriteCSharp(DocumentRequest request, DocumentBuilder output, DecompilerSettings settings, CancellationToken cancellationToken)
    {
        switch (request)
        {
            case DocumentRequest.ModulePreview preview:
                WriteAssemblySummary(preview.Module, output);
                WriteSyntaxTree(preview.Module, output, settings, d => d.DecompileModuleAndAssemblyAttributes(), cancellationToken);
                break;
            case DocumentRequest.Type type:
                WriteHeader(type.Module, output, type.Definition, ConversionFlags.UseFullyQualifiedTypeNames | ConversionFlags.UseFullyQualifiedEntityNames);
                WriteSyntaxTree(type.Module, output, settings, d => d.Decompile(type.Definition.MetadataToken), cancellationToken);
                break;
            case DocumentRequest.Member member:
                WriteMember(member, output, settings, cancellationToken);
                break;
            case DocumentRequest.Summary or DocumentRequest.Resource or DocumentRequest.Baml:
                WriteSummary(request, output, cancellationToken);
                break;
            default:
                throw new UnreachableException();
        }
    }

    private static void WriteMember(DocumentRequest.Member request, DocumentBuilder output, DecompilerSettings settings, CancellationToken cancellationToken)
    {
        var member = request.Definition;
        var declaring = member.DeclaringTypeDefinition;
        if (declaring == null)
        {
            WriteSyntaxTree(request.Module, output, settings, d => d.Decompile(member.MetadataToken), cancellationToken);
            return;
        }
        WriteHeader(request.Module, output, member.DeclaringType, ConversionFlags.UseFullyQualifiedEntityNames | ConversionFlags.UseFullyQualifiedTypeNames);
        WriteSyntaxTree(request.Module, output, settings, d => DecompileMember(d, member, declaring), cancellationToken);
    }

    private static void WriteIL(DocumentRequest request, DocumentBuilder output, DecompilationLanguage language, DecompilerSettings settings,
        CancellationToken cancellationToken)
    {
        switch (request)
        {
            case DocumentRequest.ModulePreview preview:
                var file = preview.Module.File;
                var disassembler = CreateDisassembler(preview.Module, output, language, settings, cancellationToken);
                WriteAssemblySummary(preview.Module, output);
                output.WriteLine();
                disassembler.WriteAssemblyReferences(file.Metadata);
                if (file.Metadata.IsAssembly)
                    disassembler.WriteAssemblyHeader(file);
                output.WriteLine();
                disassembler.WriteModuleHeader(file);
                break;
            case DocumentRequest.Type type:
                CreateDisassembler(type.Module, output, language, settings, cancellationToken)
                    .DisassembleType(type.Module.File, (TypeDefinitionHandle)type.Definition.MetadataToken);
                break;
            case DocumentRequest.Member member:
                var memberDisassembler = CreateDisassembler(member.Module, output, language, settings, cancellationToken);
                DisassembleMember(memberDisassembler, member.Module.File, member.Definition.MetadataToken);
                break;
            case DocumentRequest.Summary or DocumentRequest.Resource or DocumentRequest.Baml:
                WriteSummary(request, output, cancellationToken);
                break;
            default:
                throw new UnreachableException();
        }
    }

    private static void DisassembleMember(ReflectionDisassembler disassembler, MetadataFile file, EntityHandle handle)
    {
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
            default:
                throw new UnreachableException();
        }
    }

    private static ReflectionDisassembler CreateDisassembler(LoadedModule assembly, ITextOutput output, DecompilationLanguage language,
        DecompilerSettings settings, CancellationToken cancellationToken)
    {
        var body = CreateMethodBodyDisassembler(assembly, output, language, settings, cancellationToken);
        return new ReflectionDisassembler(output, body, cancellationToken)
        {
            DetectControlStructure = true,
            ExpandMemberDefinitions = true,
            AssemblyResolver = assembly.Resolver,
            DebugInfo = DebugSymbolsFor(assembly, settings),
        };
    }

    /// <summary>What disassembles each method body: with the C# above each statement for
    /// <see cref="DecompilationLanguage.ILWithCSharp"/>, else as plain IL.</summary>
    private static MethodBodyDisassembler CreateMethodBodyDisassembler(LoadedModule assembly, ITextOutput output, DecompilationLanguage language,
        DecompilerSettings settings, CancellationToken cancellationToken)
    {
        if (language == DecompilationLanguage.ILWithCSharp)
            return new MethodBodyWithCSharp(output, assembly, settings, cancellationToken);
        return new MethodBodyDisassembler(output, cancellationToken);
    }

    /// <summary>The comments ILSpy opens a type or member with: a warning for a reference assembly,
    /// the assembly's full name, and the type.</summary>
    private static void WriteHeader(LoadedModule assembly, DocumentBuilder output, IType type, ConversionFlags flags)
    {
        if (assembly.MainModule.GetAssemblyAttributes().Any(static a => a.AttributeType.FullName == ReferenceAssemblyAttribute))
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

    private static void WriteSyntaxTree(LoadedModule assembly, DocumentBuilder output, DecompilerSettings settings,
        Func<CSharpDecompiler, SyntaxTree> decompile, CancellationToken cancellationToken)
    {
        var decompiler = CreateCSharpDecompiler(assembly, settings, cancellationToken);
        var syntaxTree = decompile(decompiler);
        cancellationToken.ThrowIfCancellationRequested();
        output.IndentationString = settings.CSharpFormattingOptions.IndentationString;
        var writer = CreateHighlightingWriter(output, settings);
        WriteSyntaxTree(syntaxTree, writer, settings);
    }

    /// <summary>Writes <paramref name="syntaxTree"/> to <paramref name="output"/> as the C# view shows
    /// it, recording in the tree where each node landed, for <see cref="CSharpDecompiler.CreateSequencePoints"/>.</summary>
    internal static void WriteLocatedSyntaxTree(SyntaxTree syntaxTree, DocumentBuilder output, DecompilerSettings settings)
    {
        output.IndentationString = settings.CSharpFormattingOptions.IndentationString;
        var highlighting = CreateHighlightingWriter(output, settings);
        var located = new LocatingTokenWriter(highlighting, output);
        var writer = TokenWriter.WrapInWriterThatSetsLocationsInAST(located);
        WriteSyntaxTree(syntaxTree, writer, settings);
    }

    private static TokenWriter CreateHighlightingWriter(DocumentBuilder output, DecompilerSettings settings)
    {
        var writer = new TextTokenWriter(output, settings);
        return new HighlightingTokenWriter(writer, output);
    }

    private static void WriteSyntaxTree(SyntaxTree syntaxTree, TokenWriter writer, DecompilerSettings settings)
    {
        syntaxTree.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
        syntaxTree.AcceptVisitor(new CSharpOutputVisitor(writer, settings.CSharpFormattingOptions));
    }

    internal static CSharpDecompiler CreateCSharpDecompiler(LoadedModule assembly, DecompilerSettings settings, CancellationToken cancellationToken)
    {
        var typeSystem = assembly.TypeSystemFor(settings);
        return new CSharpDecompiler(typeSystem, settings)
        {
            CancellationToken = cancellationToken,
            DebugInfoProvider = DebugSymbolsFor(assembly, settings),
        };
    }

    internal static IDebugInfoProvider? DebugSymbolsFor(LoadedModule assembly, DecompilerSettings settings)
    {
        if (!settings.UseDebugSymbols)
            return null;
        return assembly.DebugSymbols;
    }

    private static void WriteAssemblySummary(LoadedModule assembly, DocumentBuilder output)
    {
        var file = assembly.File;
        WriteComment(output, assembly.FilePath.Text);
        WriteComment(output, assembly.FullName);
        var corHeader = file.CorHeader;
        if (file.Metadata.IsAssembly && corHeader != null)
        {
            var entryPoint = corHeader.EntryPointTokenOrRelativeVirtualAddress;
            if (entryPoint != 0 && (corHeader.Flags & CorFlags.NativeEntryPoint) == 0)
            {
                WriteEntryPoint(assembly, MetadataTokens.EntityHandle(entryPoint), output);
            }
        }
        if (assembly.PEFile != null)
            WriteComment(output, $"Architecture: {ArchitectureOf(assembly.PEFile.Reader.PEHeaders)}");
        if (assembly.TargetFramework != null)
            WriteComment(output, $"Target framework: {assembly.TargetFramework}");
        output.WriteLine();
    }

    private static void WriteEntryPoint(LoadedModule assembly, EntityHandle handle, DocumentBuilder output)
    {
        if (handle.Kind != HandleKind.MethodDefinition)
            return;
        var method = assembly.MainModule.ResolveEntity(handle);
        if (method == null)
            return;
        var reference = CodeReference.To(method);
        if (reference == null)
            return;
        output.Write("// Entry point: ", SpanKind.Comment);
        output.Write(EntityText.FullName(method), SpanKind.Method, reference);
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

    private static void WriteSummary(DocumentRequest request, DocumentBuilder output, CancellationToken cancellationToken)
    {
        switch (request)
        {
            case DocumentRequest.Summary summary:
                WriteSummary(summary.Info, output, cancellationToken);
                break;
            case DocumentRequest.Baml baml:
                WriteXaml(baml, output, cancellationToken);
                break;
            case DocumentRequest.Resource resource:
                WriteResource(resource, output);
                break;
            default:
                throw new UnreachableException();
        }
    }

    private static void WriteSummary(SummaryInfo summary, DocumentBuilder output, CancellationToken cancellationToken)
    {
        switch (summary)
        {
            case SummaryInfo.Comments comments:
                foreach (var line in comments.Lines)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (line.Length == 0)
                        output.WriteLine();
                    else
                        WriteComment(output, line);
                }
                break;
            case SummaryInfo.Namespace ns:
                output.Write("namespace", SpanKind.Keyword);
                output.Write(" ");
                output.Write(ns.Name, SpanKind.Namespace);
                output.WriteLine();
                output.WriteLine();
                foreach (var entry in ns.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    WriteNamespaceEntry(output, entry);
                }
                break;
            case SummaryInfo.ForwardedType forwarded:
                WriteComment(output, ForwardedToText(forwarded.Destination));
                output.Write(forwarded.FullName, SpanKind.Type, forwarded.Target);
                output.WriteLine();
                break;
            case SummaryInfo.References references:
                foreach (var assembly in references.Assemblies)
                    WriteReferenceLine(output, assembly);
                foreach (var module in references.Modules)
                    WriteComment(output, module);
                break;
            case SummaryInfo.ModuleReference module:
                WriteComment(output, module.Name);
                WriteComment(output, DescribeModule(module.ContainsMetadata));
                WriteImports(output, module.Imports);
                break;
            case SummaryInfo.ImportedMethod import:
                WriteLink(output, import.Method, SpanKind.Method);
                WriteComment(output, $"Calls {import.EntryPoint} in {import.ModuleName}");
                break;
            case SummaryInfo.AssemblyReference reference:
                WriteComment(output, reference.Reference.FullName);
                var path = reference.Module.Resolver.FindFile(reference.Reference);
                WriteComment(output, path is null ? "Not found" : $"Resolves to {path}");
                break;
            case SummaryInfo.Used used:
                WriteUsed(output, used.Entries, "", cancellationToken);
                break;
            case SummaryInfo.Types types:
                foreach (var type in types.Entries)
                    WriteLink(output, type, SpanKind.Type);
                break;
            case SummaryInfo.Error error:
                foreach (var line in error.Exception.ToString().Split('\n'))
                    WriteComment(output, line.TrimEnd('\r'));
                break;
            default:
                throw new UnreachableException();
        }
    }

    private static void WriteNamespaceEntry(DocumentBuilder output, NamespaceEntry entry)
    {
        switch (entry)
        {
            case NamespaceEntry.Declared declared:
                WriteTypeDeclaration(output, declared);
                break;
            case NamespaceEntry.Forwarded forwarded:
                WriteForwardedType(output, forwarded.Line);
                break;
            default:
                throw new UnreachableException();
        }
    }

    /// <summary>A line for each type and member <paramref name="entries"/> lists as used from a reference,
    /// going to where it is defined, with what is used of a type indented under it.</summary>
    private static void WriteUsed(DocumentBuilder output, IReadOnlyList<UsedEntry> entries, string indent, CancellationToken cancellationToken)
    {
        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            output.Write(indent);
            switch (entry)
            {
                case UsedEntry.Forwarded forwarded:
                    WriteForwardedType(output, forwarded.Line);
                    break;
                case UsedEntry.Type type:
                    output.Write(type.FullName, SpanKind.Type, type.Target);
                    output.WriteLine();
                    WriteUsed(output, type.Used, indent + UsedIndent, cancellationToken);
                    break;
                case UsedEntry.Member member:
                    output.Write(member.Label, member.Kind, member.Target);
                    output.WriteLine();
                    break;
                default:
                    throw new UnreachableException();
            }
        }
    }

    /// <summary>A line naming the forwarded type, which goes to where it is defined, and where it is forwarded to.</summary>
    private static void WriteForwardedType(DocumentBuilder output, ForwardedLine line)
    {
        output.Write(line.Label, SpanKind.Type, line.Target);
        output.Write(" // " + ForwardedToText(line.Destination), SpanKind.Comment);
        output.WriteLine();
    }

    private static string ForwardedToText(string? destination) => $"Forwarded to {destination}";

    private static void WriteTypeDeclaration(DocumentBuilder output, NamespaceEntry.Declared type)
    {
        var declaration = EntityText.Declaration(type.Type);
        var label = type.Label;
        var nameAt = declaration.LastIndexOf(label, StringComparison.Ordinal);
        if (nameAt < 0)
        {
            output.Write(declaration, SpanKind.Plain, type.Reference);
        }
        else
        {
            output.Write(declaration[..nameAt], SpanKind.Keyword);
            output.Write(label, SpanKind.Type, type.Reference);
            output.Write(declaration[(nameAt + label.Length)..], SpanKind.Plain);
        }
        output.WriteLine();
    }

    /// <summary>WPF's compiled XAML as the XAML it was compiled from, colored as markup.</summary>
    private static void WriteXaml(DocumentRequest.Baml baml, DocumentBuilder output, CancellationToken cancellationToken)
    {
        var xaml = Baml.DecompileToText(baml.Module, baml.Data, cancellationToken);
        ResourceText.Write(output, xaml, ResourceSyntax.Xml);
        output.WriteLine();
    }

    /// <summary>A text resource as its text, colored as XML or JSON when it is that, as ILSpy shows
    /// it, and indented when written on one line; anything else as a description and a hex dump of
    /// its first bytes.</summary>
    private static void WriteResource(DocumentRequest.Resource resource, DocumentBuilder output)
    {
        var data = resource.Read();
        if (ResourceText.Detect(resource.Title, data) is var (text, syntax))
        {
            var shown = ResourceText.IndentOneLine(text, syntax);
            ResourceText.Write(output, shown, syntax);
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

    /// <summary>An assembly reference's full name, which goes to the assembly, after a comment's marker.</summary>
    private static void WriteReferenceLine(DocumentBuilder output, SummaryLink reference)
    {
        output.Write("// ", SpanKind.Comment);
        output.Write(reference.Text, SpanKind.Plain, reference.Target);
        output.WriteLine();
    }

    private static string DescribeModule(bool containsMetadata)
    {
        if (containsMetadata)
            return "A module of this assembly, holding metadata";
        return "A native library, or a module holding no metadata";
    }

    /// <summary>The methods the assembly imports from a module, after a blank line, a line each.</summary>
    private static void WriteImports(DocumentBuilder output, IReadOnlyList<SummaryLink> imports)
    {
        if (imports.Count == 0)
            return;
        output.WriteLine();
        foreach (var import in imports)
            WriteLink(output, import, SpanKind.Method);
    }

    private static void WriteLink(DocumentBuilder output, SummaryLink link, SpanKind kind)
    {
        output.Write(link.Text, kind, link.Target);
        output.WriteLine();
    }

    private static void WriteComment(DocumentBuilder output, string text)
    {
        output.Write($"// {text}", SpanKind.Comment);
        output.WriteLine();
    }
}
