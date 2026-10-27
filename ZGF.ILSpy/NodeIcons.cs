using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

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
    /// <summary>What a node of <paramref name="kind"/> draws in <paramref name="icons"/>.</summary>
    public static IconGlyph Glyph(NodeKind kind, AppIcons icons) => kind switch
    {
        NodeKind.Assembly => icons.NodeAssembly,
        NodeKind.ReferencesFolder => icons.NodeReferencesFolder,
        NodeKind.AssemblyReference => icons.NodeAssemblyReference,
        NodeKind.ResourcesFolder => icons.NodeResourcesFolder,
        NodeKind.Resource => icons.NodeResource,
        NodeKind.Namespace => icons.NodeNamespace,
        NodeKind.Class => icons.NodeClass,
        NodeKind.Interface => icons.NodeInterface,
        NodeKind.Struct => icons.NodeStruct,
        NodeKind.Enum => icons.NodeEnum,
        NodeKind.Delegate => icons.NodeDelegate,
        NodeKind.Record => icons.NodeRecord,
        NodeKind.RecordStruct => icons.NodeRecordStruct,
        NodeKind.BaseTypes => icons.NodeBaseTypes,
        NodeKind.BaseType => icons.NodeBaseType,
        NodeKind.Field => icons.NodeField,
        NodeKind.Constant => icons.NodeConstant,
        NodeKind.Property => icons.NodeProperty,
        NodeKind.Indexer => icons.NodeIndexer,
        NodeKind.Event => icons.NodeEvent,
        NodeKind.Method => icons.NodeMethod,
        NodeKind.Constructor => icons.NodeConstructor,
        NodeKind.Operator => icons.NodeOperator,
        NodeKind.Error => icons.NodeError,
        NodeKind.Loading => icons.NodeLoading,
        NodeKind.Metadata => icons.NodeMetadata,
        NodeKind.MetadataHeader => icons.NodeMetadataHeader,
        NodeKind.MetadataDirectory => icons.NodeMetadataDirectory,
        NodeKind.MetadataTables => icons.NodeMetadataTables,
        NodeKind.MetadataTable => icons.NodeMetadataTable,
        NodeKind.MetadataHeap => icons.NodeMetadataHeap,
        NodeKind.DerivedTypes => icons.NodeDerivedTypes,
        NodeKind.DerivedType => icons.NodeDerivedType,
        NodeKind.ResourcesFile => icons.NodeResourcesFile,
        NodeKind.ResourceImage => icons.NodeResourceImage,
        NodeKind.FailedAssembly => icons.NodeFailedAssembly,
        NodeKind.ForwardedType => icons.NodeForwardedType,
        NodeKind.Package => icons.NodePackage,
        NodeKind.PackageFolder => icons.NodePackageFolder,
        NodeKind.Bundle => icons.NodeBundle,
        NodeKind.ReferencedTypes => icons.NodeReferencedTypes,
        NodeKind.TypeReference => icons.NodeTypeReference,
        NodeKind.MethodReference => icons.NodeMethodReference,
        NodeKind.FieldReference => icons.NodeFieldReference,
        NodeKind.PropertyReference => icons.NodePropertyReference,
        NodeKind.EventReference => icons.NodeEventReference,
        NodeKind.ModuleReference => icons.NodeModuleReference,
        NodeKind.ImportedMethod => icons.NodeImportedMethod,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    public static NodeIconRole Role(NodeKind kind) => kind switch
    {
        NodeKind.Assembly => NodeIconRole.Neutral,
        NodeKind.ReferencesFolder => NodeIconRole.Neutral,
        NodeKind.AssemblyReference => NodeIconRole.Neutral,
        NodeKind.ResourcesFolder => NodeIconRole.Neutral,
        NodeKind.Resource => NodeIconRole.Neutral,
        NodeKind.Namespace => NodeIconRole.Neutral,
        NodeKind.Class => NodeIconRole.Type,
        NodeKind.Interface => NodeIconRole.Type,
        NodeKind.Struct => NodeIconRole.Type,
        NodeKind.Enum => NodeIconRole.Type,
        NodeKind.Delegate => NodeIconRole.Type,
        NodeKind.Record => NodeIconRole.Type,
        NodeKind.RecordStruct => NodeIconRole.Type,
        NodeKind.BaseTypes => NodeIconRole.Neutral,
        NodeKind.BaseType => NodeIconRole.Type,
        NodeKind.Field => NodeIconRole.Data,
        NodeKind.Constant => NodeIconRole.Data,
        NodeKind.Property => NodeIconRole.Data,
        NodeKind.Indexer => NodeIconRole.Data,
        NodeKind.Event => NodeIconRole.Data,
        NodeKind.Method => NodeIconRole.Method,
        NodeKind.Constructor => NodeIconRole.Method,
        NodeKind.Operator => NodeIconRole.Method,
        NodeKind.Error => NodeIconRole.Error,
        NodeKind.Loading => NodeIconRole.Neutral,
        NodeKind.Metadata => NodeIconRole.Neutral,
        NodeKind.MetadataHeader => NodeIconRole.Neutral,
        NodeKind.MetadataDirectory => NodeIconRole.Neutral,
        NodeKind.MetadataTables => NodeIconRole.Neutral,
        NodeKind.MetadataTable => NodeIconRole.Data,
        NodeKind.MetadataHeap => NodeIconRole.Data,
        NodeKind.DerivedTypes => NodeIconRole.Neutral,
        NodeKind.DerivedType => NodeIconRole.Type,
        NodeKind.ResourcesFile => NodeIconRole.Neutral,
        NodeKind.ResourceImage => NodeIconRole.Neutral,
        NodeKind.FailedAssembly => NodeIconRole.Error,
        NodeKind.ForwardedType => NodeIconRole.Type,
        NodeKind.Package => NodeIconRole.Neutral,
        NodeKind.PackageFolder => NodeIconRole.Neutral,
        NodeKind.Bundle => NodeIconRole.Neutral,
        NodeKind.ReferencedTypes => NodeIconRole.Neutral,
        NodeKind.TypeReference => NodeIconRole.Type,
        NodeKind.MethodReference => NodeIconRole.Method,
        NodeKind.FieldReference => NodeIconRole.Data,
        NodeKind.PropertyReference => NodeIconRole.Data,
        NodeKind.EventReference => NodeIconRole.Data,
        NodeKind.ModuleReference => NodeIconRole.Neutral,
        NodeKind.ImportedMethod => NodeIconRole.Method,
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
    public static UiIcon Icon(NodeKind kind, NodeAccessibility accessibility, NodeIconPalette palette, AppIcons icons) =>
        new(Glyph(kind, icons), palette.For(kind), Overlay: Overlay(accessibility, icons), OverlayColor: palette.Restricted);

    /// <summary>Records <paramref name="kind"/>'s <see cref="Icon"/> as the tree shows it, in the screen's
    /// icons and icon colors.</summary>
    public static void Draw(Ui<UiState> ui, NodeKind kind, NodeAccessibility accessibility = NodeAccessibility.None) =>
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

    public uint For(NodeKind kind) => this[NodeIcons.Role(kind)];
}
