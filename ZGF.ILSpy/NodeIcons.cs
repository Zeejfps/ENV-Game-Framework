using System.Diagnostics;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy;

/// <summary>The icon a row shows for what it stands for, drawn as the icon theme draws its <c>node.*</c>
/// key: worked out from a node's subject, or from the kind a saved place or a result names.</summary>
public enum NodeIcon
{
    Assembly,
    ReferencesFolder,
    AssemblyReference,
    ResourcesFolder,
    Resource,
    Namespace,
    Class,
    Interface,
    Struct,
    Enum,
    Delegate,
    Record,
    RecordStruct,
    BaseTypes,
    BaseType,
    Field,
    Constant,
    Property,
    Indexer,
    Event,
    Method,
    Constructor,
    Operator,
    Metadata,
    MetadataHeader,
    MetadataDirectory,
    MetadataTables,
    MetadataTable,
    MetadataHeap,
    DerivedTypes,
    DerivedType,
    ResourcesFile,
    ResourceImage,
    FailedAssembly,
    ForwardedType,
    Package,
    PackageFolder,
    Bundle,
    ReferencedTypes,
    TypeReference,
    MethodReference,
    FieldReference,
    PropertyReference,
    EventReference,
    ModuleReference,
    ImportedMethod,
}

/// <summary>What a node kind's icon means, which picks its color from a <see cref="NodeIconPalette"/>.</summary>
public enum NodeIconRole
{
    Neutral,
    Type,
    Method,
    Data,
    Error,
}

/// <summary>Each node kind's icon, drawn as the icon theme draws its <c>node.*</c> key, and the role
/// that colors it.</summary>
public static class NodeIcons
{
    /// <summary>The icon of a node of <paramref name="subject"/>.</summary>
    public static NodeIcon Of(NodeSubject subject) => subject switch
    {
        NodeSubject.Module => NodeIcon.Assembly,
        NodeSubject.FailedAssembly => NodeIcon.FailedAssembly,
        NodeSubject.Namespace => NodeIcon.Namespace,
        NodeSubject.TypeDefinition type => Of(type.Definition.Shape),
        NodeSubject.MemberDefinition member => Of(member.Definition.Shape),
        NodeSubject.References => NodeIcon.ReferencesFolder,
        NodeSubject.AssemblyReference => NodeIcon.AssemblyReference,
        NodeSubject.ReferencedTypes => NodeIcon.ReferencedTypes,
        NodeSubject.TypeReference => NodeIcon.TypeReference,
        NodeSubject.MemberReference member => member.Value.Shape switch
        {
            MemberShape.Field or MemberShape.Constant => NodeIcon.FieldReference,
            MemberShape.Property or MemberShape.Indexer => NodeIcon.PropertyReference,
            MemberShape.Event => NodeIcon.EventReference,
            MemberShape.Method or MemberShape.Constructor or MemberShape.Operator => NodeIcon.MethodReference,
            _ => throw new UnreachableException(),
        },
        NodeSubject.ModuleReference => NodeIcon.ModuleReference,
        NodeSubject.ImportedMethod => NodeIcon.ImportedMethod,
        NodeSubject.ForwardedType => NodeIcon.ForwardedType,
        NodeSubject.BaseTypes => NodeIcon.BaseTypes,
        NodeSubject.BaseType => NodeIcon.BaseType,
        NodeSubject.DerivedTypes => NodeIcon.DerivedTypes,
        NodeSubject.DerivedType => NodeIcon.DerivedType,
        NodeSubject.Resources => NodeIcon.ResourcesFolder,
        NodeSubject.ResourceContainer => NodeIcon.ResourcesFile,
        NodeSubject.ResourceEntry entry => entry.Value.IsImage ? NodeIcon.ResourceImage : NodeIcon.Resource,
        NodeSubject.Metadata => NodeIcon.Metadata,
        NodeSubject.MetadataTables => NodeIcon.MetadataTables,
        NodeSubject.MetadataTable table => table.Value.Role switch
        {
            MetadataGridRole.Header => NodeIcon.MetadataHeader,
            MetadataGridRole.Directory => NodeIcon.MetadataDirectory,
            MetadataGridRole.Table => NodeIcon.MetadataTable,
            MetadataGridRole.Heap => NodeIcon.MetadataHeap,
            _ => throw new UnreachableException(),
        },
        NodeSubject.Package package => !package.Value.IsRoot ? NodeIcon.PackageFolder
            : package.Value.Package.Package.IsBundle ? NodeIcon.Bundle : NodeIcon.Package,
        NodeSubject.PackageFile file => file.Value.IsImage ? NodeIcon.ResourceImage : NodeIcon.Resource,
        _ => throw new UnreachableException(),
    };

    /// <summary>The icon of a symbol of <paramref name="shape"/>, as an analysis, a comparison or a diagram lists it.</summary>
    public static NodeIcon Of(SymbolShape shape) => shape switch
    {
        SymbolShape.Assembly => NodeIcon.Assembly,
        SymbolShape.Namespace => NodeIcon.Namespace,
        SymbolShape.Type type => Of(type.Shape),
        SymbolShape.Member member => Of(member.Shape),
        _ => throw new UnreachableException(),
    };

    public static NodeIcon Of(TypeShape shape) => shape switch
    {
        TypeShape.Class => NodeIcon.Class,
        TypeShape.Interface => NodeIcon.Interface,
        TypeShape.Struct => NodeIcon.Struct,
        TypeShape.Enum => NodeIcon.Enum,
        TypeShape.Delegate => NodeIcon.Delegate,
        TypeShape.Record => NodeIcon.Record,
        TypeShape.RecordStruct => NodeIcon.RecordStruct,
        _ => throw new UnreachableException(),
    };

    public static NodeIcon Of(MemberShape shape) => shape switch
    {
        MemberShape.Field => NodeIcon.Field,
        MemberShape.Constant => NodeIcon.Constant,
        MemberShape.Property => NodeIcon.Property,
        MemberShape.Indexer => NodeIcon.Indexer,
        MemberShape.Event => NodeIcon.Event,
        MemberShape.Method => NodeIcon.Method,
        MemberShape.Constructor => NodeIcon.Constructor,
        MemberShape.Operator => NodeIcon.Operator,
        _ => throw new UnreachableException(),
    };

    /// <summary>The icon of what <paramref name="kind"/> names, for a place saved with its kind, which has no subject.</summary>
    public static NodeIcon Of(NodeKind kind) => kind switch
    {
        NodeKind.Assembly => NodeIcon.Assembly,
        NodeKind.ReferencesFolder => NodeIcon.ReferencesFolder,
        NodeKind.AssemblyReference => NodeIcon.AssemblyReference,
        NodeKind.ResourcesFolder => NodeIcon.ResourcesFolder,
        NodeKind.Resource => NodeIcon.Resource,
        NodeKind.Namespace => NodeIcon.Namespace,
        NodeKind.Class => NodeIcon.Class,
        NodeKind.Interface => NodeIcon.Interface,
        NodeKind.Struct => NodeIcon.Struct,
        NodeKind.Enum => NodeIcon.Enum,
        NodeKind.Delegate => NodeIcon.Delegate,
        NodeKind.Record => NodeIcon.Record,
        NodeKind.RecordStruct => NodeIcon.RecordStruct,
        NodeKind.BaseTypes => NodeIcon.BaseTypes,
        NodeKind.BaseType => NodeIcon.BaseType,
        NodeKind.Field => NodeIcon.Field,
        NodeKind.Constant => NodeIcon.Constant,
        NodeKind.Property => NodeIcon.Property,
        NodeKind.Indexer => NodeIcon.Indexer,
        NodeKind.Event => NodeIcon.Event,
        NodeKind.Method => NodeIcon.Method,
        NodeKind.Constructor => NodeIcon.Constructor,
        NodeKind.Operator => NodeIcon.Operator,
        NodeKind.Metadata => NodeIcon.Metadata,
        NodeKind.MetadataHeader => NodeIcon.MetadataHeader,
        NodeKind.MetadataDirectory => NodeIcon.MetadataDirectory,
        NodeKind.MetadataTables => NodeIcon.MetadataTables,
        NodeKind.MetadataTable => NodeIcon.MetadataTable,
        NodeKind.MetadataHeap => NodeIcon.MetadataHeap,
        NodeKind.DerivedTypes => NodeIcon.DerivedTypes,
        NodeKind.DerivedType => NodeIcon.DerivedType,
        NodeKind.ResourcesFile => NodeIcon.ResourcesFile,
        NodeKind.ResourceImage => NodeIcon.ResourceImage,
        NodeKind.FailedAssembly => NodeIcon.FailedAssembly,
        NodeKind.ForwardedType => NodeIcon.ForwardedType,
        NodeKind.Package => NodeIcon.Package,
        NodeKind.PackageFolder => NodeIcon.PackageFolder,
        NodeKind.Bundle => NodeIcon.Bundle,
        NodeKind.ReferencedTypes => NodeIcon.ReferencedTypes,
        NodeKind.TypeReference => NodeIcon.TypeReference,
        NodeKind.MethodReference => NodeIcon.MethodReference,
        NodeKind.FieldReference => NodeIcon.FieldReference,
        NodeKind.PropertyReference => NodeIcon.PropertyReference,
        NodeKind.EventReference => NodeIcon.EventReference,
        NodeKind.ModuleReference => NodeIcon.ModuleReference,
        NodeKind.ImportedMethod => NodeIcon.ImportedMethod,
        _ => throw new UnreachableException(),
    };

    /// <summary>The icon of <paramref name="node"/> as the tree shows it, with the overlay of its accessibility.</summary>
    public static UiIcon Icon(BrowseNode node, NodeIconPalette palette, AppIcons icons) =>
        Icon(Of(node.Subject), NodeVisibility.Of(node.Subject).Accessibility, palette, icons);

    /// <summary>Records <paramref name="node"/>'s <see cref="Icon(BrowseNode, NodeIconPalette, AppIcons)"/>, in the
    /// screen's icons and icon colors.</summary>
    public static void Draw(Ui<UiState> ui, BrowseNode node) => ui.Icon(Icon(node, ui.State.NodeIconPalette, ui.State.Icons));

    /// <summary>What a node of <paramref name="kind"/> draws in <paramref name="icons"/>.</summary>
    public static IconGlyph Glyph(NodeIcon kind, AppIcons icons) => kind switch
    {
        NodeIcon.Assembly => icons.NodeAssembly,
        NodeIcon.ReferencesFolder => icons.NodeReferencesFolder,
        NodeIcon.AssemblyReference => icons.NodeAssemblyReference,
        NodeIcon.ResourcesFolder => icons.NodeResourcesFolder,
        NodeIcon.Resource => icons.NodeResource,
        NodeIcon.Namespace => icons.NodeNamespace,
        NodeIcon.Class => icons.NodeClass,
        NodeIcon.Interface => icons.NodeInterface,
        NodeIcon.Struct => icons.NodeStruct,
        NodeIcon.Enum => icons.NodeEnum,
        NodeIcon.Delegate => icons.NodeDelegate,
        NodeIcon.Record => icons.NodeRecord,
        NodeIcon.RecordStruct => icons.NodeRecordStruct,
        NodeIcon.BaseTypes => icons.NodeBaseTypes,
        NodeIcon.BaseType => icons.NodeBaseType,
        NodeIcon.Field => icons.NodeField,
        NodeIcon.Constant => icons.NodeConstant,
        NodeIcon.Property => icons.NodeProperty,
        NodeIcon.Indexer => icons.NodeIndexer,
        NodeIcon.Event => icons.NodeEvent,
        NodeIcon.Method => icons.NodeMethod,
        NodeIcon.Constructor => icons.NodeConstructor,
        NodeIcon.Operator => icons.NodeOperator,
        NodeIcon.Metadata => icons.NodeMetadata,
        NodeIcon.MetadataHeader => icons.NodeMetadataHeader,
        NodeIcon.MetadataDirectory => icons.NodeMetadataDirectory,
        NodeIcon.MetadataTables => icons.NodeMetadataTables,
        NodeIcon.MetadataTable => icons.NodeMetadataTable,
        NodeIcon.MetadataHeap => icons.NodeMetadataHeap,
        NodeIcon.DerivedTypes => icons.NodeDerivedTypes,
        NodeIcon.DerivedType => icons.NodeDerivedType,
        NodeIcon.ResourcesFile => icons.NodeResourcesFile,
        NodeIcon.ResourceImage => icons.NodeResourceImage,
        NodeIcon.FailedAssembly => icons.NodeFailedAssembly,
        NodeIcon.ForwardedType => icons.NodeForwardedType,
        NodeIcon.Package => icons.NodePackage,
        NodeIcon.PackageFolder => icons.NodePackageFolder,
        NodeIcon.Bundle => icons.NodeBundle,
        NodeIcon.ReferencedTypes => icons.NodeReferencedTypes,
        NodeIcon.TypeReference => icons.NodeTypeReference,
        NodeIcon.MethodReference => icons.NodeMethodReference,
        NodeIcon.FieldReference => icons.NodeFieldReference,
        NodeIcon.PropertyReference => icons.NodePropertyReference,
        NodeIcon.EventReference => icons.NodeEventReference,
        NodeIcon.ModuleReference => icons.NodeModuleReference,
        NodeIcon.ImportedMethod => icons.NodeImportedMethod,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    public static NodeIconRole Role(NodeIcon kind) => kind switch
    {
        NodeIcon.Assembly => NodeIconRole.Neutral,
        NodeIcon.ReferencesFolder => NodeIconRole.Neutral,
        NodeIcon.AssemblyReference => NodeIconRole.Neutral,
        NodeIcon.ResourcesFolder => NodeIconRole.Neutral,
        NodeIcon.Resource => NodeIconRole.Neutral,
        NodeIcon.Namespace => NodeIconRole.Neutral,
        NodeIcon.Class => NodeIconRole.Type,
        NodeIcon.Interface => NodeIconRole.Type,
        NodeIcon.Struct => NodeIconRole.Type,
        NodeIcon.Enum => NodeIconRole.Type,
        NodeIcon.Delegate => NodeIconRole.Type,
        NodeIcon.Record => NodeIconRole.Type,
        NodeIcon.RecordStruct => NodeIconRole.Type,
        NodeIcon.BaseTypes => NodeIconRole.Neutral,
        NodeIcon.BaseType => NodeIconRole.Type,
        NodeIcon.Field => NodeIconRole.Data,
        NodeIcon.Constant => NodeIconRole.Data,
        NodeIcon.Property => NodeIconRole.Data,
        NodeIcon.Indexer => NodeIconRole.Data,
        NodeIcon.Event => NodeIconRole.Data,
        NodeIcon.Method => NodeIconRole.Method,
        NodeIcon.Constructor => NodeIconRole.Method,
        NodeIcon.Operator => NodeIconRole.Method,
        NodeIcon.Metadata => NodeIconRole.Neutral,
        NodeIcon.MetadataHeader => NodeIconRole.Neutral,
        NodeIcon.MetadataDirectory => NodeIconRole.Neutral,
        NodeIcon.MetadataTables => NodeIconRole.Neutral,
        NodeIcon.MetadataTable => NodeIconRole.Data,
        NodeIcon.MetadataHeap => NodeIconRole.Data,
        NodeIcon.DerivedTypes => NodeIconRole.Neutral,
        NodeIcon.DerivedType => NodeIconRole.Type,
        NodeIcon.ResourcesFile => NodeIconRole.Neutral,
        NodeIcon.ResourceImage => NodeIconRole.Neutral,
        NodeIcon.FailedAssembly => NodeIconRole.Error,
        NodeIcon.ForwardedType => NodeIconRole.Type,
        NodeIcon.Package => NodeIconRole.Neutral,
        NodeIcon.PackageFolder => NodeIconRole.Neutral,
        NodeIcon.Bundle => NodeIconRole.Neutral,
        NodeIcon.ReferencedTypes => NodeIconRole.Neutral,
        NodeIcon.TypeReference => NodeIconRole.Type,
        NodeIcon.MethodReference => NodeIconRole.Method,
        NodeIcon.FieldReference => NodeIconRole.Data,
        NodeIcon.PropertyReference => NodeIconRole.Data,
        NodeIcon.EventReference => NodeIconRole.Data,
        NodeIcon.ModuleReference => NodeIconRole.Neutral,
        NodeIcon.ImportedMethod => NodeIconRole.Method,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>The small pictogram over a node's icon that marks it as not visible outside its
    /// assembly or type: a shield for internal, a lock for private, as <paramref name="icons"/> draw
    /// them. Null for anything wider.</summary>
    public static IconGlyph? Overlay(NodeAccessibility accessibility, AppIcons icons) => accessibility switch
    {
        NodeAccessibility.Internal => icons.OverlayInternal,
        NodeAccessibility.Private or NodeAccessibility.PrivateProtected => icons.OverlayPrivate,
        _ => null,
    };

    /// <summary>The icon of a node of <paramref name="kind"/> as the tree shows it: drawn as
    /// <paramref name="icons"/> draw it, colored from <paramref name="palette"/>, with the overlay of its
    /// <paramref name="accessibility"/>.</summary>
    public static UiIcon Icon(NodeIcon kind, NodeAccessibility accessibility, NodeIconPalette palette, AppIcons icons) =>
        new(Glyph(kind, icons), palette.For(kind), Overlay: Overlay(accessibility, icons), OverlayColor: palette.Restricted);

    /// <summary>The icon of the row shown while a node's children are being read.</summary>
    public static UiIcon Loading(NodeIconPalette palette, AppIcons icons) => new(icons.NodeLoading, palette.Neutral);

    /// <summary>The icon of the row shown in place of children that could not be read.</summary>
    public static UiIcon Failed(NodeIconPalette palette, AppIcons icons) => new(icons.NodeError, palette.Error);

    /// <summary>Records <paramref name="kind"/>'s <see cref="Icon"/> as the tree shows it, in the screen's
    /// icons and icon colors.</summary>
    public static void Draw(Ui<UiState> ui, NodeIcon kind, NodeAccessibility accessibility = NodeAccessibility.None) =>
        ui.Icon(Icon(kind, accessibility, ui.State.NodeIconPalette, ui.State.Icons));
}

/// <summary>The colors node icons are drawn in, one per <see cref="NodeIconRole"/>: muted hues
/// shared with the editor's syntax colors, so color supplements the pictogram rather than
/// carrying meaning alone.</summary>
[ThemeColors]
public sealed partial class NodeIconPalette
{
    public required uint Neutral { get; init; }
    public required uint Type { get; init; }
    public required uint Method { get; init; }
    public required uint Data { get; init; }
    public required uint Error { get; init; }

    /// <summary>The color of the accessibility overlay on non-public nodes' icons.</summary>
    public required uint Restricted { get; init; }

    public uint this[NodeIconRole role] => role switch
    {
        NodeIconRole.Type => Type,
        NodeIconRole.Method => Method,
        NodeIconRole.Data => Data,
        NodeIconRole.Error => Error,
        _ => Neutral,
    };

    public uint For(NodeIcon kind) => this[NodeIcons.Role(kind)];
}
