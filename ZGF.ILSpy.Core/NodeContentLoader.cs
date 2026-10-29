using System.Diagnostics;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Core.Resources;

namespace ZGF.ILSpy.Core;

/// <summary>What a <see cref="BrowseNode"/> shows: a <see cref="DocumentContent"/>, a
/// <see cref="GridContent"/> or an <see cref="ImageContent"/>.</summary>
public abstract record NodeContent;

public sealed record DocumentContent(DecompiledDocument Document) : NodeContent;

public sealed record GridContent(MetadataGrid Grid) : NodeContent;

/// <summary>A picture, and for one stored as markup, such as SVG, that markup as a document.</summary>
public sealed record ImageContent(ResourceImage Image, DecompiledDocument? Source = null) : NodeContent;

/// <summary>
/// What a node shows and what saving its code writes: a grid, a picture, or a document, worked out
/// from what the node stands for and handed to the decompiler as a request that holds no node.
/// Safe to call concurrently.
/// </summary>
public static class NodeContentLoader
{
    /// <summary>What <paramref name="node"/> shows, made on the thread pool: for metadata its grid of
    /// rows, read the first time and kept; for a resource named as a PNG, JPEG, BMP, GIF, ICO, CUR or
    /// SVG file its picture, or when that cannot be decoded the resource as a document with why first;
    /// else the node decompiled with <paramref name="options"/>, which only C# reads.</summary>
    public static Task<NodeContent> LoadAsync(BrowseNode node, DecompilationLanguage language, DecompilerOptions options,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => Load(node, language, options, cancellationToken), cancellationToken);

    /// <summary>Decompiles <paramref name="node"/> with <paramref name="options"/>, which only C# reads.
    /// Throws <see cref="OperationCanceledException"/> when cancelled; any other failure becomes a
    /// document describing it.</summary>
    public static DecompiledDocument Decompile(BrowseNode node, DecompilationLanguage language, DecompilerOptions options,
        CancellationToken cancellationToken = default) =>
        NodeDecompiler.Decompile(RequestOf(node), language, options, cancellationToken);

    /// <summary>Whether <see cref="LoadAsync"/> gives decompiled code, which changes with the language
    /// and options, rather than a grid, a picture or why the file failed to load, which do not.</summary>
    public static bool DependsOnLanguage(BrowseNode node) => node is not MetadataGridNode and not FailedAssemblyNode && !IsImage(node);

    /// <summary>Writes <paramref name="node"/>'s code in <paramref name="language"/> to the file at
    /// <paramref name="path"/>: for an assembly its whole module, for a namespace all its types, else
    /// what the node shows. Throws when decompiling or writing fails.</summary>
    public static void SaveCode(BrowseNode node, DecompilationLanguage language, DecompilerOptions options, string path,
        CancellationToken cancellationToken = default) =>
        NodeDecompiler.SaveCode(ExportOf(node), language, options, path, cancellationToken);

    /// <summary>The name a file of <paramref name="node"/>'s code in <paramref name="language"/> is
    /// offered under: the node's name, made safe for a file name, with the language's extension.</summary>
    public static string SuggestCodeFileName(BrowseNode node, DecompilationLanguage language)
    {
        var name = node switch
        {
            AssemblyNode => node.Assembly.Name,
            EntityNode entity => entity.Entity.Name,
            _ => node.Text,
        };
        var extension = DecompilationLanguages.IsIL(language) ? ".il" : ".cs";
        return WholeProjectDecompiler.CleanUpFileName(name, extension);
    }

    private static NodeContent Load(BrowseNode node, DecompilationLanguage language, DecompilerOptions options, CancellationToken cancellationToken)
    {
        if (node is MetadataGridNode grid)
            return new GridContent(grid.Grid);
        if (!IsImage(node))
            return new DocumentContent(Decompile(node, language, options, cancellationToken));
        try
        {
            return LoadImage(node, language, options, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return new DocumentContent(NodeDecompiler.Decompile(RequestOf(node), language, options, cancellationToken, e.Message));
        }
    }

    private static bool IsImage(BrowseNode node) => node.Kind == NodeKind.ResourceImage && node is IResourceData;

    /// <summary>The picture, with the markup it is stored as when it is drawn from shapes.</summary>
    private static ImageContent LoadImage(BrowseNode node, DecompilationLanguage language, DecompilerOptions options, CancellationToken cancellationToken)
    {
        var image = ResourceImage.Decode(node.ReadResource());
        if (!image.IsVector)
            return new ImageContent(image);
        var source = Decompile(node, language, options, cancellationToken);
        return new ImageContent(image, source);
    }

    private static CodeExport ExportOf(BrowseNode node) => node switch
    {
        AssemblyNode assembly => new CodeExport.WholeModule(assembly.Module),
        NamespaceNode ns => new CodeExport.Namespace(ns.Module, ns.Name, ns.Types.ToList()),
        _ => new CodeExport.Document(RequestOf(node)),
    };

    private static DocumentRequest RequestOf(BrowseNode node)
    {
        switch (node)
        {
            case FailedAssemblyNode failed:
                return new DocumentRequest.Failure(failed.Failed, node.Text, node.FullName);
            case AssemblyNode assembly:
                return new DocumentRequest.ModulePreview(assembly.Module, node.Text, node.FullName);
            case TypeNode type:
                return new DocumentRequest.Type(type.Module, type.Type, node.Text, node.FullName);
            case MemberNode member:
                return new DocumentRequest.Member(member.Module, member.Member, node.Text, node.FullName);
            case ResourcesFolderNode or AvaloniaResourcesNode:
                return Summary(node);
            case BamlEntryNode baml:
                return new DocumentRequest.Baml(baml.Module, baml.Data, node.Text, node.FullName);
            case IResourceData resource:
                return new DocumentRequest.Resource(ModuleOf(node), resource.Description, resource.ReadData, node.Text, node.FullName);
            default:
                return Summary(node);
        }
    }

    private static DocumentRequest.Summary Summary(BrowseNode node) => new(SummaryOf(node), ModuleOf(node), node.Text, node.FullName);

    /// <summary>The module <paramref name="node"/> is a node of; null for one of a package or a file that failed.</summary>
    private static LoadedModule? ModuleOf(BrowseNode node)
    {
        if (node is not IModuleNode module)
            return null;
        return module.Module;
    }

    /// <summary>What the summary of <paramref name="node"/> lists, read from its children.</summary>
    private static SummaryInfo SummaryOf(BrowseNode node) => node switch
    {
        PackageNode or PackageFolderNode => new SummaryInfo.Comments(node.Children.Select(DescribePackageChild).ToList()),
        NamespaceNode => new SummaryInfo.Namespace(node.Text, ListNamespaceEntries(node)),
        ForwardedTypeNode forwarded => new SummaryInfo.ForwardedType(forwarded.FullName, forwarded.Target, forwarded.ForwardedTo),
        ReferencesFolderNode => new SummaryInfo.References(
            node.Children.OfType<AssemblyReferenceNode>().Select(static r => new SummaryLink(r.FullName, r.Target)).ToList(),
            node.Children.OfType<ModuleReferenceNode>().Select(static m => m.Text).ToList()),
        ModuleReferenceNode module => new SummaryInfo.ModuleReference(module.Text, module.ContainsMetadata,
            module.Children.OfType<ImportedMethodNode>().Select(static i => new SummaryLink(i.FullName, i.Target)).ToList()),
        ImportedMethodNode import => new SummaryInfo.ImportedMethod(new SummaryLink(import.FullName, import.Target), import.EntryPoint, import.ModuleName),
        AssemblyReferenceNode reference => new SummaryInfo.AssemblyReference(reference.Module, reference.AssemblyReference),
        ReferencedTypesNode => new SummaryInfo.Used(ListUsed(node)),
        TypeReferenceNode or MemberReferenceNode => new SummaryInfo.Used([UsedOf(node)]),
        ResourcesFolderNode or AvaloniaResourcesNode => new SummaryInfo.Comments(
            node.Children.OfType<IResourceData>().Select(static r => r.Description).ToList()),
        MetadataTextNode metadata => new SummaryInfo.Comments(metadata.Lines()),
        BaseTypesNode or DerivedTypesNode => new SummaryInfo.Types(node.Children.Select(static c => new SummaryLink(c.Text, c.Target)).ToList()),
        BaseTypeNode or DerivedTypeNode => new SummaryInfo.Types([new SummaryLink(node.Text, node.Target)]),
        ErrorNode error => new SummaryInfo.Error(error.Error),
        LoadingNode or MetadataGridNode => new SummaryInfo.Comments([node.Text]),
        _ => throw new UnreachableException(),
    };

    private static string DescribePackageChild(BrowseNode child)
    {
        if (child is IResourceData file)
            return file.Description;
        if (child is PackageFolderNode)
            return child.Text + "/";
        return child.Text;
    }

    /// <summary>The types the namespace defines and forwards, but those the compiler made up.</summary>
    private static List<NamespaceEntry> ListNamespaceEntries(BrowseNode ns)
    {
        var entries = new List<NamespaceEntry>();
        foreach (var child in ns.Children)
        {
            if (child.IsCompilerGenerated) continue;
            if (child is TypeNode type)
                entries.Add(new NamespaceEntry.Declared(type.Type, type.Text, type.Reference));
            if (child is ForwardedTypeNode forwarded)
                entries.Add(new NamespaceEntry.Forwarded(ForwardedLineOf(forwarded)));
        }
        return entries;
    }

    /// <summary>The types and members <paramref name="node"/> lists as used from a reference.</summary>
    private static List<UsedEntry> ListUsed(BrowseNode node)
    {
        return node.Children
            .Where(static c => c is not ErrorNode)
            .Select(UsedOf)
            .ToList();
    }

    private static UsedEntry UsedOf(BrowseNode node) => node switch
    {
        ForwardedTypeNode forwarded => new UsedEntry.Forwarded(ForwardedLineOf(forwarded)),
        TypeReferenceNode type => new UsedEntry.Type(type.FullName, type.Target, ListUsed(type)),
        MemberReferenceNode member => new UsedEntry.Member(member.Text, UsedMemberSpan(member.Kind), member.Target),
        _ => throw new UnreachableException(),
    };

    private static ForwardedLine ForwardedLineOf(ForwardedTypeNode node) => new(node.Text, node.Target, node.ForwardedTo);

    private static SpanKind UsedMemberSpan(NodeKind kind) => kind switch
    {
        NodeKind.FieldReference => SpanKind.Field,
        NodeKind.PropertyReference => SpanKind.Property,
        NodeKind.EventReference => SpanKind.Event,
        NodeKind.MethodReference => SpanKind.Method,
        _ => throw new UnreachableException(),
    };
}
