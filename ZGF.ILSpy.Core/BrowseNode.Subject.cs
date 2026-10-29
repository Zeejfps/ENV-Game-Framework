using System.Diagnostics;
using ICSharpCode.Decompiler.TypeSystem;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Core.Resources;

namespace ZGF.ILSpy.Core;

public sealed partial class BrowseNode
{
    private BrowseNode(BrowseNode? parent, NodeSubject subject)
    {
        Assembly = AssemblyOf(subject);
        Parent = parent;
        Kind = KindOf(subject);
        Text = TextOf(subject);
        Subject = subject;
        switch (subject)
        {
            case NodeSubject.TypeDefinition type:
                (Accessibility, IsPublicApi, IsCompilerGenerated) = DescribeDefinition(parent, type.Definition.Symbol);
                break;
            case NodeSubject.MemberDefinition member:
                (Accessibility, IsPublicApi, IsCompilerGenerated) = DescribeDefinition(parent, member.Definition.Symbol);
                break;
            case NodeSubject.TypeReference type:
                Namespace = type.Value.Namespace;
                break;
            case NodeSubject.ImportedMethod import:
                var shown = import.Value.Shown;
                Accessibility = EntityText.Map(shown.Symbol.Accessibility);
                IsPublicApi = EntityText.IsPublicApi(shown.Symbol);
                DeclaringTypeName = import.Value.DeclaringTypeName;
                break;
            case NodeSubject.ForwardedType forwarded:
                ForwardedTo = forwarded.Value.Destination;
                break;
            case NodeSubject.BaseType baseType:
                if (baseType.Value.Target is BaseTypeTarget.Navigable navigable)
                {
                    Accessibility = EntityText.Map(navigable.Definition.Accessibility);
                    IsPublicApi = EntityText.IsPublicApi(navigable.Definition);
                }
                break;
            case NodeSubject.DerivedType derived:
                Accessibility = EntityText.Map(derived.Value.Symbol.Accessibility);
                IsPublicApi = EntityText.IsPublicApi(derived.Value.Symbol);
                break;
            case NodeSubject.ResourceContainer container:
                (IsPublicApi, Accessibility) = ResourceNodes.Visibility(container.Value.Resource);
                break;
            case NodeSubject.ResourceEntry entry:
                if (entry.Value.Source is ResourceSource.Manifest manifest)
                    (IsPublicApi, Accessibility) = ResourceNodes.Visibility(manifest.Resource);
                break;
            case NodeSubject.MetadataTable table:
                IsEmptyTable = table.Value.IsEmptyTable;
                break;
            case NodeSubject.Namespace ns:
                IsEnclosingOnly = ns.Value.IsEnclosingOnly;
                IsPublicApi = ns.Value.IsPublicApi;
                IsCompilerGenerated = ns.Value.IsCompilerGenerated;
                break;
            case NodeSubject.AssemblyReference or NodeSubject.MemberReference or NodeSubject.References or NodeSubject.ReferencedTypes
                or NodeSubject.ModuleReference or NodeSubject.BaseTypes
                or NodeSubject.DerivedTypes or NodeSubject.Resources or NodeSubject.Metadata or NodeSubject.MetadataTables
                or NodeSubject.Package or NodeSubject.PackageFile or NodeSubject.Module or NodeSubject.FailedAssembly:
                break;
            default:
                throw new UnreachableException();
        }
    }

    /// <summary>What the node of a definition says of it: its accessibility, whether it is seen outside its
    /// assembly, and whether the compiler made it or what holds it up.</summary>
    private static (NodeAccessibility, bool, bool) DescribeDefinition(BrowseNode? parent, IEntity entity) =>
        (EntityText.Map(entity.Accessibility), EntityText.IsPublicApi(entity),
            (parent != null && parent.IsCompilerGenerated) || Members.IsCompilerGenerated(entity));

    /// <summary>What the node stands for: its label, flags and children all follow from that.</summary>
    public NodeSubject Subject { get; }

    /// <summary>The module the node is read from; null for a node of a package's folders and files.</summary>
    internal LoadedModule? SourceModule => Assembly as LoadedModule;

    private static LoadedAssembly AssemblyOf(NodeSubject subject) => subject switch
    {
        NodeSubject.TypeDefinition type => type.Definition.Module,
        NodeSubject.MemberDefinition member => member.Definition.Module,
        NodeSubject.References references => references.Owner,
        NodeSubject.AssemblyReference reference => reference.Value.Module,
        NodeSubject.ReferencedTypes types => types.Reference.Module,
        NodeSubject.TypeReference type => type.Value.Module,
        NodeSubject.MemberReference member => member.Value.Module,
        NodeSubject.ModuleReference module => module.Value.Module,
        NodeSubject.ImportedMethod import => import.Value.Shown.Module,
        NodeSubject.ForwardedType forwarded => forwarded.Value.Module,
        NodeSubject.BaseTypes baseTypes => baseTypes.Type.Module,
        NodeSubject.BaseType baseType => baseType.Value.Module,
        NodeSubject.DerivedTypes derivedTypes => derivedTypes.Type.Module,
        NodeSubject.DerivedType derived => derived.Value.Module,
        NodeSubject.Resources resources => resources.Owner,
        NodeSubject.ResourceContainer container => container.Value.Module,
        NodeSubject.ResourceEntry entry => entry.Value.Module,
        NodeSubject.Metadata metadata => metadata.Value.Module,
        NodeSubject.MetadataTables tables => tables.Folder.Module,
        NodeSubject.MetadataTable table => table.Value.Module,
        NodeSubject.Package package => package.Value.Package,
        NodeSubject.PackageFile file => file.Value.Package,
        NodeSubject.Module module => module.Value,
        NodeSubject.Namespace ns => ns.Value.Module,
        NodeSubject.FailedAssembly failed => failed.Value,
        _ => throw new UnreachableException(),
    };

    /// <summary>The bytes of the resource the node stands for; null for a node of anything else.</summary>
    internal IResourceData? ResourceData => Subject switch
    {
        NodeSubject.ResourceContainer container => container.Value,
        NodeSubject.ResourceEntry entry => entry.Value,
        NodeSubject.PackageFile file => file.Value,
        NodeSubject.TypeDefinition or NodeSubject.MemberDefinition or NodeSubject.References or NodeSubject.AssemblyReference
            or NodeSubject.ReferencedTypes or NodeSubject.TypeReference or NodeSubject.MemberReference or NodeSubject.ModuleReference
            or NodeSubject.ImportedMethod or NodeSubject.ForwardedType or NodeSubject.BaseTypes or NodeSubject.BaseType
            or NodeSubject.DerivedTypes or NodeSubject.DerivedType or NodeSubject.Resources or NodeSubject.Metadata
            or NodeSubject.MetadataTables or NodeSubject.MetadataTable or NodeSubject.Package or NodeSubject.Module or NodeSubject.Namespace or NodeSubject.FailedAssembly => null,
        _ => throw new UnreachableException(),
    };

    /// <summary>The folder of a package the node lists, whose files can be extracted: the root folder for
    /// a package's own node; null for a node that lists none.</summary>
    public PackageFolderPlace? FolderInPackage => Subject switch
    {
        NodeSubject.Package package => new PackageFolderPlace(package.Value.Package, package.Value.Folder),
        NodeSubject.TypeDefinition or NodeSubject.MemberDefinition or NodeSubject.References or NodeSubject.AssemblyReference
            or NodeSubject.ReferencedTypes or NodeSubject.TypeReference or NodeSubject.MemberReference or NodeSubject.ModuleReference
            or NodeSubject.ImportedMethod or NodeSubject.ForwardedType or NodeSubject.BaseTypes or NodeSubject.BaseType
            or NodeSubject.DerivedTypes or NodeSubject.DerivedType or NodeSubject.Resources or NodeSubject.ResourceContainer
            or NodeSubject.ResourceEntry or NodeSubject.Metadata or NodeSubject.MetadataTables or NodeSubject.MetadataTable
            or NodeSubject.PackageFile or NodeSubject.Module or NodeSubject.Namespace or NodeSubject.FailedAssembly => null,
        _ => throw new UnreachableException(),
    };

    /// <summary>The type or member the node defines; null for a node of anything else.</summary>
    internal IEntity? Entity => Subject switch
    {
        NodeSubject.TypeDefinition type => type.Definition.Symbol,
        NodeSubject.MemberDefinition member => member.Definition.Symbol,
        NodeSubject.References or NodeSubject.AssemblyReference or NodeSubject.ReferencedTypes or NodeSubject.TypeReference
            or NodeSubject.MemberReference or NodeSubject.ModuleReference or NodeSubject.ImportedMethod or NodeSubject.ForwardedType
            or NodeSubject.BaseTypes or NodeSubject.BaseType or NodeSubject.DerivedTypes or NodeSubject.DerivedType or NodeSubject.Resources
            or NodeSubject.ResourceContainer or NodeSubject.ResourceEntry or NodeSubject.Metadata or NodeSubject.MetadataTables
            or NodeSubject.MetadataTable or NodeSubject.Package or NodeSubject.PackageFile or NodeSubject.Module or NodeSubject.Namespace or NodeSubject.FailedAssembly => null,
        _ => throw new UnreachableException(),
    };

    /// <summary>A qualified name for copying, e.g. <c>System.Collections.Generic.List&lt;T&gt;.Add(T)</c>.</summary>
    public string FullName => Subject switch
    {
        NodeSubject.TypeDefinition type => EntityText.FullName(type.Definition.Symbol),
        NodeSubject.MemberDefinition member => EntityText.FullName(member.Definition.Symbol),
        NodeSubject.AssemblyReference reference => reference.Value.Reference.FullName,
        NodeSubject.TypeReference type => type.Value.FullName,
        NodeSubject.MemberReference member => member.Value.FullName,
        NodeSubject.ImportedMethod import => $"{import.Value.DeclaringTypeName}.{import.Value.Label}",
        NodeSubject.ForwardedType forwarded => forwarded.Value.FullName,
        NodeSubject.Package package => package.Value.FullName,
        NodeSubject.PackageFile file => file.Value.File.FullName,
        NodeSubject.Module module => module.Value.FullName,
        NodeSubject.Namespace ns => ns.Value.Name,
        NodeSubject.FailedAssembly failed => failed.Value.FilePath.Text,
        NodeSubject.References or NodeSubject.ReferencedTypes or NodeSubject.ModuleReference or NodeSubject.BaseTypes
            or NodeSubject.BaseType or NodeSubject.DerivedTypes or NodeSubject.DerivedType or NodeSubject.Resources
            or NodeSubject.ResourceContainer or NodeSubject.ResourceEntry or NodeSubject.Metadata or NodeSubject.MetadataTables
            or NodeSubject.MetadataTable => Text,
        _ => throw new UnreachableException(),
    };

    private static NodeKind KindOf(NodeSubject subject) => subject switch
    {
        NodeSubject.TypeDefinition type => EntityText.KindOf(type.Definition.Symbol),
        NodeSubject.MemberDefinition member => EntityText.KindOf(member.Definition.Symbol),
        NodeSubject.References => NodeKind.ReferencesFolder,
        NodeSubject.AssemblyReference => NodeKind.AssemblyReference,
        NodeSubject.ReferencedTypes => NodeKind.ReferencedTypes,
        NodeSubject.TypeReference => NodeKind.TypeReference,
        NodeSubject.MemberReference member => member.Value.Declared switch
        {
            IField => NodeKind.FieldReference,
            IProperty => NodeKind.PropertyReference,
            IEvent => NodeKind.EventReference,
            _ => NodeKind.MethodReference,
        },
        NodeSubject.ModuleReference => NodeKind.ModuleReference,
        NodeSubject.ImportedMethod => NodeKind.ImportedMethod,
        NodeSubject.ForwardedType => NodeKind.ForwardedType,
        NodeSubject.BaseTypes => NodeKind.BaseTypes,
        NodeSubject.BaseType => NodeKind.BaseType,
        NodeSubject.DerivedTypes => NodeKind.DerivedTypes,
        NodeSubject.DerivedType => NodeKind.DerivedType,
        NodeSubject.Resources => NodeKind.ResourcesFolder,
        NodeSubject.ResourceContainer => NodeKind.ResourcesFile,
        NodeSubject.ResourceEntry entry => KindOf(entry.Value),
        NodeSubject.Metadata => NodeKind.Metadata,
        NodeSubject.MetadataTables => NodeKind.MetadataTables,
        NodeSubject.MetadataTable table => KindOf(table.Value),
        NodeSubject.Package package => KindOf(package.Value),
        NodeSubject.PackageFile file => ResourceNodes.KindOf(file.Value.File.Name),
        NodeSubject.Module => NodeKind.Assembly,
        NodeSubject.Namespace => NodeKind.Namespace,
        NodeSubject.FailedAssembly => NodeKind.FailedAssembly,
        _ => throw new UnreachableException(),
    };

    private static string TextOf(NodeSubject subject) => subject switch
    {
        NodeSubject.TypeDefinition type => EntityText.Label(type.Definition.Symbol),
        NodeSubject.MemberDefinition member => EntityText.Label(member.Definition.Symbol),
        NodeSubject.References => "References",
        NodeSubject.AssemblyReference reference => reference.Value.Reference.Name,
        NodeSubject.ReferencedTypes => "Referenced types",
        NodeSubject.TypeReference type => type.Value.Label,
        NodeSubject.MemberReference member => EntityText.Label(member.Value.Declared),
        NodeSubject.ModuleReference module => module.Value.Name,
        NodeSubject.ImportedMethod import => import.Value.Label,
        NodeSubject.ForwardedType forwarded => forwarded.Value.Label,
        NodeSubject.BaseTypes => "Base Types",
        NodeSubject.BaseType baseType => EntityText.TypeName(baseType.Value.Type),
        NodeSubject.DerivedTypes => "Derived Types",
        NodeSubject.DerivedType derived => EntityText.TypeName(derived.Value.Symbol),
        NodeSubject.Resources => "Resources",
        NodeSubject.ResourceContainer container => container.Value.Resource.Name,
        NodeSubject.ResourceEntry entry => entry.Value.Name,
        NodeSubject.Metadata metadata => metadata.Value.Title,
        NodeSubject.MetadataTables => "Tables",
        NodeSubject.MetadataTable table => table.Value.Title,
        NodeSubject.Package package => package.Value.Label,
        NodeSubject.PackageFile file => file.Value.File.Name,
        NodeSubject.Module module => LabelOf(module.Value),
        NodeSubject.Namespace ns => ns.Value.Label,
        NodeSubject.FailedAssembly failed => failed.Value.FilePath.FileName,
        _ => throw new UnreachableException(),
    };

    /// <summary>Whether this node may have children, guessed without building them.</summary>
    internal bool MayHaveChildren => Subject switch
    {
        NodeSubject.TypeDefinition type => MayListMembers(type.Definition),
        NodeSubject.MemberDefinition member => member.Definition.Symbol is IProperty or IEvent,
        NodeSubject.References references => HasReferences(references.Owner),
        NodeSubject.AssemblyReference => true,
        NodeSubject.ReferencedTypes types => types.Reference.Module.ReferenceUsage.Uses(types.Reference.Reference.Handle),
        NodeSubject.TypeReference type => UsesAnythingOf(type.Value),
        NodeSubject.ModuleReference module => module.Value.Imports.Count > 0,
        NodeSubject.BaseTypes or NodeSubject.DerivedTypes or NodeSubject.Resources or NodeSubject.ResourceContainer
            or NodeSubject.Metadata or NodeSubject.MetadataTables or NodeSubject.Package or NodeSubject.Module => true,
        NodeSubject.Namespace ns => !ns.Value.IsEnclosingOnly,
        NodeSubject.MetadataTable table => table.Value.Part is MetadataPart.DebugDirectory,
        NodeSubject.DerivedType derived => !derived.Value.Symbol.IsSealed,
        NodeSubject.MemberReference or NodeSubject.ImportedMethod or NodeSubject.ForwardedType or NodeSubject.BaseType
            or NodeSubject.ResourceEntry or NodeSubject.PackageFile or NodeSubject.FailedAssembly => false,
        _ => throw new UnreachableException(),
    };

    /// <summary>Builds the children in ILSpy's order; the <see cref="BrowseSession"/> calls it once.</summary>
    internal BrowseNode[] CreateChildren() => Subject switch
    {
        NodeSubject.TypeDefinition type => ListMembers(type.Definition),
        NodeSubject.MemberDefinition member => ListAccessors(member.Definition),
        NodeSubject.References references => ListAssemblyReferences(references.Owner, this).Concat(ListModuleReferences(references.Owner, this)).ToArray(),
        NodeSubject.AssemblyReference reference => ListReferenceContents(reference.Value),
        NodeSubject.ReferencedTypes types => ListUsedTypes(types.Reference),
        NodeSubject.TypeReference type => ListUsedOf(type.Value),
        NodeSubject.ModuleReference module => ListImports(module.Value),
        NodeSubject.BaseTypes baseTypes => ListBaseTypes(baseTypes.Type),
        NodeSubject.DerivedTypes derivedTypes => FindDerivedTypes(derivedTypes.Type.Module, this, derivedTypes.Type.Symbol),
        NodeSubject.DerivedType derived => FindDerivedTypes(derived.Value.Module, this, derived.Value.Symbol),
        NodeSubject.Resources resources => ListResources(resources.Owner),
        NodeSubject.ResourceContainer container => ListEntries(container.Value),
        NodeSubject.Metadata metadata => ListMetadata(metadata.Value),
        NodeSubject.MetadataTables tables => ListTables(tables.Folder),
        NodeSubject.MetadataTable table => table.Value.Part is MetadataPart.DebugDirectory directory ? ListDebugEntries(table.Value.Module, directory.File) : [],
        NodeSubject.Package package => ListPackageFolder(package.Value),
        NodeSubject.Module module => ListModule(module.Value),
        NodeSubject.Namespace ns => ListNamespace(ns.Value),
        NodeSubject.MemberReference or NodeSubject.ImportedMethod or NodeSubject.ForwardedType or NodeSubject.BaseType
            or NodeSubject.ResourceEntry or NodeSubject.PackageFile or NodeSubject.FailedAssembly => [],
        _ => throw new UnreachableException(),
    };

    /// <summary>The node of <paramref name="subject"/>, under <paramref name="parent"/>.</summary>
    internal static BrowseNode Of(BrowseNode parent, NodeSubject subject) => new(parent, subject);

    /// <summary>The node of <paramref name="type"/>, under <paramref name="parent"/>; null for a type with
    /// no metadata token, which the tree does not list.</summary>
    internal static BrowseNode? ForType(LoadedModule module, BrowseNode parent, ITypeDefinition type)
    {
        var definition = DefinedType.Of(module, type);
        if (definition == null)
            return null;
        return new BrowseNode(parent, new NodeSubject.TypeDefinition(definition));
    }

    /// <summary>The node of <paramref name="member"/>, under <paramref name="parent"/>; null for a member
    /// with no metadata token, which the tree does not list.</summary>
    internal static BrowseNode? ForMember(LoadedModule module, BrowseNode parent, IMember member)
    {
        var definition = DefinedMember.Of(module, member);
        if (definition == null)
            return null;
        return new BrowseNode(parent, new NodeSubject.MemberDefinition(definition));
    }

    /// <summary>The type or member <paramref name="node"/> defines; null for a node of anything else.</summary>
    internal static IEntity? EntityOf(BrowseNode? node)
    {
        if (node == null)
            return null;
        return node.Entity;
    }

    /// <summary>Whether <paramref name="node"/> defines a member, under which only its accessors sit.</summary>
    internal static bool IsMember(BrowseNode? node)
    {
        if (node == null)
            return false;
        return node.Subject is NodeSubject.MemberDefinition;
    }

    private static void AddIfListed(List<BrowseNode> nodes, BrowseNode? node)
    {
        if (node != null)
            nodes.Add(node);
    }
}
