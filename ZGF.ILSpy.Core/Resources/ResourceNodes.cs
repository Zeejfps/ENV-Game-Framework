using System.Globalization;
using System.Reflection;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Util;
using ZGF.ILSpy.Core.Metadata;
using MetadataResource = ICSharpCode.Decompiler.Metadata.Resource;

namespace ZGF.ILSpy.Core.Resources;

/// <summary>A node standing for a resource's bytes, which can be read to save them.</summary>
internal interface IResourceData
{
    /// <summary>What the resource is, as a resources folder lists it.</summary>
    string Description { get; }

    byte[] ReadData();
}

internal static class ResourceNodes
{
    /// <summary>The node for a manifest resource: a <c>.resources</c> file's entries, or the
    /// resource itself.</summary>
    public static BrowseNode Create(LoadedAssembly assembly, BrowseNode parent, MetadataResource resource) =>
        resource.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase)
            ? new ResourcesFileNode(assembly, parent, resource)
            : new ResourceNode(assembly, parent, resource);

    public static NodeKind KindOf(string name) => ResourceImage.IsImageName(name) ? NodeKind.ResourceImage : NodeKind.Resource;

    public static string Describe(MetadataResource resource)
    {
        var size = resource.TryGetLength() is { } length ? $", {length} bytes" : "";
        return $"{resource.Name} ({resource.ResourceType}{size})";
    }

    public static byte[] Read(MetadataResource resource)
    {
        using var stream = resource.TryOpenStream() ?? throw new IOException($"{resource.Name} cannot be read.");
        stream.Position = 0;
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    public static bool IsPublic(MetadataResource resource) =>
        (resource.Attributes & ManifestResourceAttributes.VisibilityMask) == ManifestResourceAttributes.Public;
}

/// <summary>A manifest resource that is no <c>.resources</c> file: shown as text, a picture or
/// its bytes.</summary>
internal sealed class ResourceNode : BrowseNode, IResourceData
{
    public ResourceNode(LoadedAssembly assembly, BrowseNode parent, MetadataResource resource)
        : base(assembly, parent, ResourceNodes.KindOf(resource.Name), resource.Name)
    {
        Resource = resource;
        IsPublicApi = ResourceNodes.IsPublic(resource);
        Accessibility = IsPublicApi ? NodeAccessibility.Public : NodeAccessibility.Private;
    }

    public MetadataResource Resource { get; }

    public string Description => ResourceNodes.Describe(Resource);

    public byte[] ReadData() => ResourceNodes.Read(Resource);
}

/// <summary>An entry of a <c>.resources</c> file holding bytes or a stream: shown as text, a
/// picture or its bytes.</summary>
internal sealed class ResourceEntryNode(LoadedAssembly assembly, BrowseNode parent, string name, byte[] data)
    : BrowseNode(assembly, parent, ResourceNodes.KindOf(name), name), IResourceData
{
    public string Description => $"{Text} ({data.Length} bytes)";

    public byte[] ReadData() => data;
}

/// <summary>
/// A <c>.resources</c> file: its entries as a grid of name, value and type — strings, other
/// values and the size of byte entries, as ILSpy's string and object tables show them — and its
/// byte and stream entries as nodes of their own.
/// </summary>
internal sealed class ResourcesFileNode : MetadataGridNode, IResourceData
{
    private readonly MetadataResource _resource;
    private readonly Lazy<List<KeyValuePair<string, object?>>> _entries;

    public ResourcesFileNode(LoadedAssembly assembly, BrowseNode parent, MetadataResource resource)
        : this(assembly, parent, resource, new Lazy<List<KeyValuePair<string, object?>>>(() => ReadEntries(resource)))
    {
    }

    private ResourcesFileNode(LoadedAssembly assembly, BrowseNode parent, MetadataResource resource, Lazy<List<KeyValuePair<string, object?>>> entries)
        : base(assembly, parent, NodeKind.ResourcesFile, resource.Name, assembly.File, Schema(assembly, entries))
    {
        _resource = resource;
        _entries = entries;
        IsPublicApi = ResourceNodes.IsPublic(resource);
        Accessibility = IsPublicApi ? NodeAccessibility.Public : NodeAccessibility.Private;
    }

    public string Description => ResourceNodes.Describe(_resource);

    public byte[] ReadData() => ResourceNodes.Read(_resource);

    private protected override bool MayHaveChildren => true;

    private protected override BrowseNode[] CreateChildren() =>
        _entries.Value
            .Select(e => e.Value switch
            {
                byte[] bytes => new ResourceEntryNode(Assembly, this, e.Key, bytes),
                MemoryStream stream => new ResourceEntryNode(Assembly, this, e.Key, stream.ToArray()),
                _ => (BrowseNode?)null,
            })
            .OfType<BrowseNode>()
            .ToArray();

    /// <summary>The entries in name order. The containers WPF generates key their entries by a
    /// URI-escaped path, which is unescaped as ILSpy does.</summary>
    private static List<KeyValuePair<string, object?>> ReadEntries(MetadataResource resource)
    {
        using var stream = new MemoryStream(ResourceNodes.Read(resource));
        IEnumerable<KeyValuePair<string, object?>> entries = new ResourcesFile(stream).ToList();
        if (WholeProjectDecompiler.IsWpfGeneratedResourceContainer(resource.Name))
            entries = entries.Select(static e => new KeyValuePair<string, object?>(Uri.UnescapeDataString(e.Key), e.Value));
        return entries.OrderBy(static e => e.Key, NaturalStringComparer.Instance).ToList();
    }

    /// <summary>The grid of the entries: each one's name, its value as text, and the type of value.</summary>
    private static GridSchema<(string Name, string Value, string Type)> Schema(LoadedAssembly assembly, Lazy<List<KeyValuePair<string, object?>>> entries) =>
        new GridSchema<(string Name, string Value, string Type)>("Resources", assembly.File, () => entries.Value.Select(static e => Row(e.Key, e.Value)))
            .Value("Name", static r => r.Name)
            .Value("Value", static r => r.Value, tooltip: static r => r.Value.Length > 80 || r.Value.Contains('\n') ? r.Value : null)
            .Value("Type", static r => r.Type);

    private static (string Name, string Value, string Type) Row(string name, object? value) =>
        value switch
        {
            null => (name, "null", ""),
            string text => (name, text, "System.String"),
            byte[] bytes => (name, $"{bytes.Length} bytes", "System.Byte[]"),
            MemoryStream stream => (name, $"{stream.Length} bytes", "System.IO.MemoryStream"),
            ResourceSerializedObject serialized => (name, "<serialized>", serialized.TypeName ?? ""),
            _ => (name, Convert.ToString(value, CultureInfo.InvariantCulture) ?? "", value.GetType().FullName ?? value.GetType().Name),
        };
}
