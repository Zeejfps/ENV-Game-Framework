using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using ICSharpCode.Decompiler.TypeSystem;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Core.Resources;

namespace ZGF.ILSpy.Core;

/// <summary>A node made from what it stands for, its <see cref="Subject"/>: the assembly it is read
/// from, its label, flags and children all follow from that.</summary>
internal sealed partial class SubjectNode : BrowseNode
{
    private SubjectNode(BrowseNode? parent, NodeSubject subject)
        : base(AssemblyOf(subject), parent, KindOf(subject), TextOf(subject))
    {
        Subject = subject;
        switch (subject)
        {
            case NodeSubject.TypeDefinition type:
                (Accessibility, IsPublicApi, IsCompilerGenerated, Reference, Token) = DescribeDefinition(parent, type.Definition.Symbol, type.Definition.Address);
                break;
            case NodeSubject.MemberDefinition member:
                (Accessibility, IsPublicApi, IsCompilerGenerated, Reference, Token) = DescribeDefinition(parent, member.Definition.Symbol, member.Definition.Address);
                break;
            case NodeSubject.AssemblyReference reference:
                var assembly = reference.Value.Reference;
                Target = CodeReference.ToEntity(CodeReferenceKind.Assembly, assembly.Name, reference.Value.Module.FilePath, assembly.Handle);
                break;
            case NodeSubject.TypeReference type:
                Namespace = type.Value.Namespace;
                Target = CodeReference.ToEntity(CodeReferenceKind.Type, Text, type.Value.Module.FilePath, type.Value.Handle);
                break;
            case NodeSubject.MemberReference member:
                var declared = member.Value.Declared;
                Target = CodeReference.ToEntity(CodeReference.KindOf(declared.SymbolKind), declared.Name, member.Value.Module.FilePath, member.Value.Handle);
                break;
            case NodeSubject.ImportedMethod import:
                var shown = import.Value.Shown;
                Accessibility = EntityText.Map(shown.Symbol.Accessibility);
                IsPublicApi = EntityText.IsPublicApi(shown.Symbol);
                DeclaringTypeName = import.Value.DeclaringTypeName;
                Target = shown.Address.ToReference();
                break;
            case NodeSubject.ForwardedType forwarded:
                ForwardedTo = forwarded.Value.Destination;
                Target = CodeReference.ToEntity(CodeReferenceKind.Type, forwarded.Value.Label, forwarded.Value.Module.FilePath, forwarded.Value.Handle);
                break;
            case NodeSubject.BaseType baseType:
                if (baseType.Value.Target is BaseTypeTarget.Navigable navigable)
                {
                    Target = navigable.Address.ToReference();
                    Accessibility = EntityText.Map(navigable.Definition.Accessibility);
                    IsPublicApi = EntityText.IsPublicApi(navigable.Definition);
                }
                break;
            case NodeSubject.DerivedType derived:
                Accessibility = EntityText.Map(derived.Value.Symbol.Accessibility);
                IsPublicApi = EntityText.IsPublicApi(derived.Value.Symbol);
                Target = derived.Value.Address.ToReference();
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
            case NodeSubject.References or NodeSubject.ReferencedTypes or NodeSubject.ModuleReference or NodeSubject.BaseTypes
                or NodeSubject.DerivedTypes or NodeSubject.Resources or NodeSubject.Metadata or NodeSubject.MetadataTables
                or NodeSubject.Package or NodeSubject.PackageFile:
                break;
            default:
                throw new UnreachableException();
        }
    }

    /// <summary>What the node of a definition says of it: its accessibility, whether it is seen outside its
    /// assembly, whether the compiler made it or what holds it up, the reference to it and its token.</summary>
    private static (NodeAccessibility, bool, bool, CodeReference, int?) DescribeDefinition(BrowseNode? parent, IEntity entity, NavigationAddress address) =>
        (EntityText.Map(entity.Accessibility), EntityText.IsPublicApi(entity),
            (parent != null && parent.IsCompilerGenerated) || Members.IsCompilerGenerated(entity),
            address.ToReference(), MetadataTokens.GetToken(entity.MetadataToken));

    public NodeSubject Subject { get; }

    /// <summary>The module the node is read from; null for a node of a package's folders and files.</summary>
    public LoadedModule? SourceModule => Assembly as LoadedModule;

    private static LoadedAssembly AssemblyOf(NodeSubject subject) => subject switch
    {
        NodeSubject.TypeDefinition type => type.Definition.Module,
        NodeSubject.MemberDefinition member => member.Definition.Module,
        NodeSubject.References references => references.Module,
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
        NodeSubject.Resources resources => resources.Module,
        NodeSubject.ResourceContainer container => container.Value.Module,
        NodeSubject.ResourceEntry entry => entry.Value.Module,
        NodeSubject.Metadata metadata => metadata.Value.Module,
        NodeSubject.MetadataTables tables => tables.Folder.Module,
        NodeSubject.MetadataTable table => table.Value.Module,
        NodeSubject.Package package => package.Value.Package,
        NodeSubject.PackageFile file => file.Value.Package,
        _ => throw new UnreachableException(),
    };

    /// <summary>The bytes of the resource the node stands for; null for a node of anything else.</summary>
    internal override IResourceData? ResourceData => Subject switch
    {
        NodeSubject.ResourceContainer container => container.Value,
        NodeSubject.ResourceEntry entry => entry.Value,
        NodeSubject.PackageFile file => file.Value,
        NodeSubject.TypeDefinition or NodeSubject.MemberDefinition or NodeSubject.References or NodeSubject.AssemblyReference
            or NodeSubject.ReferencedTypes or NodeSubject.TypeReference or NodeSubject.MemberReference or NodeSubject.ModuleReference
            or NodeSubject.ImportedMethod or NodeSubject.ForwardedType or NodeSubject.BaseTypes or NodeSubject.BaseType
            or NodeSubject.DerivedTypes or NodeSubject.DerivedType or NodeSubject.Resources or NodeSubject.Metadata
            or NodeSubject.MetadataTables or NodeSubject.MetadataTable or NodeSubject.Package => null,
        _ => throw new UnreachableException(),
    };

    /// <summary>The folder of a package the node lists, whose files can be extracted: the root folder for
    /// a package's own node; null for a node that lists none.</summary>
    public override PackageFolderPlace? FolderInPackage => Subject switch
    {
        NodeSubject.Package package => new PackageFolderPlace(package.Value.Package, package.Value.Folder),
        NodeSubject.TypeDefinition or NodeSubject.MemberDefinition or NodeSubject.References or NodeSubject.AssemblyReference
            or NodeSubject.ReferencedTypes or NodeSubject.TypeReference or NodeSubject.MemberReference or NodeSubject.ModuleReference
            or NodeSubject.ImportedMethod or NodeSubject.ForwardedType or NodeSubject.BaseTypes or NodeSubject.BaseType
            or NodeSubject.DerivedTypes or NodeSubject.DerivedType or NodeSubject.Resources or NodeSubject.ResourceContainer
            or NodeSubject.ResourceEntry or NodeSubject.Metadata or NodeSubject.MetadataTables or NodeSubject.MetadataTable
            or NodeSubject.PackageFile => null,
        _ => throw new UnreachableException(),
    };

    /// <summary>The type or member the node defines; null for a node of anything else.</summary>
    public IEntity? Entity => Subject switch
    {
        NodeSubject.TypeDefinition type => type.Definition.Symbol,
        NodeSubject.MemberDefinition member => member.Definition.Symbol,
        NodeSubject.References or NodeSubject.AssemblyReference or NodeSubject.ReferencedTypes or NodeSubject.TypeReference
            or NodeSubject.MemberReference or NodeSubject.ModuleReference or NodeSubject.ImportedMethod or NodeSubject.ForwardedType
            or NodeSubject.BaseTypes or NodeSubject.BaseType or NodeSubject.DerivedTypes or NodeSubject.DerivedType or NodeSubject.Resources
            or NodeSubject.ResourceContainer or NodeSubject.ResourceEntry or NodeSubject.Metadata or NodeSubject.MetadataTables
            or NodeSubject.MetadataTable or NodeSubject.Package or NodeSubject.PackageFile => null,
        _ => throw new UnreachableException(),
    };

    public override string FullName => Subject switch
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
        _ => throw new UnreachableException(),
    };

    internal override bool MayHaveChildren => Subject switch
    {
        NodeSubject.TypeDefinition type => MayListMembers(type.Definition),
        NodeSubject.MemberDefinition member => member.Definition.Symbol is IProperty or IEvent,
        NodeSubject.References references => HasReferences(references.Module),
        NodeSubject.AssemblyReference => true,
        NodeSubject.ReferencedTypes types => types.Reference.Module.ReferenceUsage.Uses(types.Reference.Reference.Handle),
        NodeSubject.TypeReference type => UsesAnythingOf(type.Value),
        NodeSubject.ModuleReference module => module.Value.Imports.Count > 0,
        NodeSubject.BaseTypes or NodeSubject.DerivedTypes or NodeSubject.Resources or NodeSubject.ResourceContainer
            or NodeSubject.Metadata or NodeSubject.MetadataTables or NodeSubject.Package => true,
        NodeSubject.MetadataTable table => table.Value.Part is MetadataPart.DebugDirectory,
        NodeSubject.DerivedType derived => !derived.Value.Symbol.IsSealed,
        NodeSubject.MemberReference or NodeSubject.ImportedMethod or NodeSubject.ForwardedType or NodeSubject.BaseType
            or NodeSubject.ResourceEntry or NodeSubject.PackageFile => false,
        _ => throw new UnreachableException(),
    };

    internal override BrowseNode[] CreateChildren() => Subject switch
    {
        NodeSubject.TypeDefinition type => ListMembers(type.Definition),
        NodeSubject.MemberDefinition member => ListAccessors(member.Definition),
        NodeSubject.References references => ListAssemblyReferences(references.Module, this).Concat(ListModuleReferences(references.Module, this)).ToArray(),
        NodeSubject.AssemblyReference reference => ListReferenceContents(reference.Value),
        NodeSubject.ReferencedTypes types => ListUsedTypes(types.Reference),
        NodeSubject.TypeReference type => ListUsedOf(type.Value),
        NodeSubject.ModuleReference module => ListImports(module.Value),
        NodeSubject.BaseTypes baseTypes => ListBaseTypes(baseTypes.Type),
        NodeSubject.DerivedTypes derivedTypes => FindDerivedTypes(derivedTypes.Type.Module, this, derivedTypes.Type.Symbol),
        NodeSubject.DerivedType derived => FindDerivedTypes(derived.Value.Module, this, derived.Value.Symbol),
        NodeSubject.Resources resources => ListResources(resources.Module),
        NodeSubject.ResourceContainer container => ListEntries(container.Value),
        NodeSubject.Metadata metadata => ListMetadata(metadata.Value),
        NodeSubject.MetadataTables tables => ListTables(tables.Folder),
        NodeSubject.MetadataTable table => table.Value.Part is MetadataPart.DebugDirectory directory ? ListDebugEntries(table.Value.Module, directory.File) : [],
        NodeSubject.Package package => ListPackageFolder(package.Value),
        NodeSubject.MemberReference or NodeSubject.ImportedMethod or NodeSubject.ForwardedType or NodeSubject.BaseType
            or NodeSubject.ResourceEntry or NodeSubject.PackageFile => [],
        _ => throw new UnreachableException(),
    };

    /// <summary>The node of <paramref name="subject"/>, under <paramref name="parent"/>.</summary>
    public static SubjectNode Of(BrowseNode parent, NodeSubject subject) => new(parent, subject);

    /// <summary>The node of <paramref name="type"/>, under <paramref name="parent"/>; null for a type with
    /// no metadata token, which the tree does not list.</summary>
    public static SubjectNode? ForType(LoadedModule module, BrowseNode parent, ITypeDefinition type)
    {
        var definition = DefinedType.Of(module, type);
        if (definition == null)
            return null;
        return new SubjectNode(parent, new NodeSubject.TypeDefinition(definition));
    }

    /// <summary>The node of <paramref name="member"/>, under <paramref name="parent"/>; null for a member
    /// with no metadata token, which the tree does not list.</summary>
    public static SubjectNode? ForMember(LoadedModule module, BrowseNode parent, IMember member)
    {
        var definition = DefinedMember.Of(module, member);
        if (definition == null)
            return null;
        return new SubjectNode(parent, new NodeSubject.MemberDefinition(definition));
    }

    /// <summary>The type or member <paramref name="node"/> defines; null for a node of anything else.</summary>
    public static IEntity? EntityOf(BrowseNode? node)
    {
        if (node is not SubjectNode subject)
            return null;
        return subject.Entity;
    }

    /// <summary>Whether <paramref name="node"/> defines a member, under which only its accessors sit.</summary>
    public static bool IsMember(BrowseNode? node)
    {
        if (node is not SubjectNode subject)
            return false;
        return subject.Subject is NodeSubject.MemberDefinition;
    }

    private static void AddIfListed(List<BrowseNode> nodes, SubjectNode? node)
    {
        if (node != null)
            nodes.Add(node);
    }
}
