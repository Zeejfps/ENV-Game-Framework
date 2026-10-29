using System.Globalization;
using System.Reflection;
using System.Text;
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

    /// <summary>The name a saved copy is offered under.</summary>
    string FileName { get; }

    /// <summary>What a saved copy holds.</summary>
    byte[] ReadData();
}

internal static class ResourceNodes
{
    /// <summary>The node for a manifest resource: the entries of a <c>.resources</c> file or of
    /// Avalonia's asset container, or the resource itself.</summary>
    public static BrowseNode Create(LoadedModule assembly, BrowseNode parent, MetadataResource resource)
    {
        if (AvaloniaResources.IsContainerName(resource.Name))
            return new AvaloniaResourcesNode(assembly, parent, resource);
        if (resource.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
            return new ResourcesFileNode(assembly, parent, resource);
        return new ResourceNode(assembly, parent, resource);
    }

    public static NodeKind KindOf(string name) => ResourceImage.IsImageName(name) ? NodeKind.ResourceImage : NodeKind.Resource;

    public static string Describe(MetadataResource resource)
    {
        var length = resource.TryGetLength();
        var size = length != null ? $", {length} bytes" : "";
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

    public static (bool IsPublic, NodeAccessibility Accessibility) Visibility(MetadataResource resource) =>
        (resource.Attributes & ManifestResourceAttributes.VisibilityMask) == ManifestResourceAttributes.Public
            ? (true, NodeAccessibility.Public)
            : (false, NodeAccessibility.Private);
}

/// <summary>A manifest resource that is no <c>.resources</c> file: shown as text, a picture or
/// its bytes.</summary>
internal sealed class ResourceNode : BrowseNode, IResourceData, IModuleNode
{
    public LoadedModule Module { get; }

    public ResourceNode(LoadedModule assembly, BrowseNode parent, MetadataResource resource)
        : base(assembly, parent, ResourceNodes.KindOf(resource.Name), resource.Name)
    {
        Module = assembly;
        Resource = resource;
        (IsPublicApi, Accessibility) = ResourceNodes.Visibility(resource);
    }

    public MetadataResource Resource { get; }

    public string Description => ResourceNodes.Describe(Resource);

    public string FileName => System.IO.Path.GetFileName(Text);

    public byte[] ReadData() => ResourceNodes.Read(Resource);
}

/// <summary>The container Avalonia embeds an app's assets in: each asset as a node of its own,
/// under its path.</summary>
internal sealed class AvaloniaResourcesNode : BrowseNode, IResourceData, IModuleNode
{
    private readonly MetadataResource _resource;

    public LoadedModule Module { get; }

    public AvaloniaResourcesNode(LoadedModule assembly, BrowseNode parent, MetadataResource resource)
        : base(assembly, parent, NodeKind.ResourcesFile, resource.Name)
    {
        Module = assembly;
        _resource = resource;
        (IsPublicApi, Accessibility) = ResourceNodes.Visibility(resource);
    }

    public string Description => ResourceNodes.Describe(_resource);

    public string FileName => System.IO.Path.GetFileName(Text);

    public byte[] ReadData() => ResourceNodes.Read(_resource);

    internal override bool MayHaveChildren => true;

    internal override BrowseNode[] CreateChildren() =>
        AvaloniaResources.ReadEntries(ReadData())
            .OrderBy(e => e.Path, NaturalStringComparer.Instance)
            .Select(e => new ResourceEntryNode(Module, this, e.Path, e.Data))
            .ToArray<BrowseNode>();
}

/// <summary>An entry of a <c>.resources</c> file holding bytes or a stream: shown as text, a
/// picture or its bytes.</summary>
internal sealed class ResourceEntryNode(LoadedModule assembly, BrowseNode parent, string name, byte[] data)
    : BrowseNode(assembly, parent, ResourceNodes.KindOf(name), name), IResourceData, IModuleNode
{
    public LoadedModule Module { get; } = assembly;

    public string Description => $"{Text} ({data.Length} bytes)";

    public string FileName => System.IO.Path.GetFileName(Text);

    public byte[] ReadData() => data;
}

/// <summary>A BAML entry of a <c>.resources</c> file: shown, and saved, as the XAML it was
/// compiled from.</summary>
internal sealed class BamlEntryNode(LoadedModule assembly, BrowseNode parent, string name, byte[] baml)
    : BrowseNode(assembly, parent, NodeKind.Resource, name), IResourceData, IModuleNode
{
    public LoadedModule Module { get; } = assembly;

    /// <summary>The compiled XAML, as the entry holds it.</summary>
    public byte[] Data => baml;

    public string Description => $"{Text} ({baml.Length} bytes)";

    public string FileName => Baml.XamlName(System.IO.Path.GetFileName(Text));

    public byte[] ReadData()
    {
        var xaml = Xaml(CancellationToken.None);
        return Encoding.UTF8.GetBytes(xaml);
    }

    public string Xaml(CancellationToken cancellationToken) => Baml.DecompileToText(Module, baml, cancellationToken);
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

    public ResourcesFileNode(LoadedModule assembly, BrowseNode parent, MetadataResource resource)
        : this(assembly, parent, resource, new Lazy<List<KeyValuePair<string, object?>>>(() => ReadEntries(resource)))
    {
    }

    private ResourcesFileNode(LoadedModule assembly, BrowseNode parent, MetadataResource resource, Lazy<List<KeyValuePair<string, object?>>> entries)
        : base(assembly, parent, NodeKind.ResourcesFile, resource.Name, assembly.File, Schema(assembly, entries))
    {
        _resource = resource;
        _entries = entries;
        (IsPublicApi, Accessibility) = ResourceNodes.Visibility(resource);
    }

    public string Description => ResourceNodes.Describe(_resource);

    public string FileName => System.IO.Path.GetFileName(Text);

    public byte[] ReadData() => ResourceNodes.Read(_resource);

    internal override bool MayHaveChildren => true;

    internal override BrowseNode[] CreateChildren() =>
        _entries.Value
            .Select(e => e.Value switch
            {
                byte[] bytes => CreateEntry(e.Key, bytes),
                MemoryStream stream => CreateEntry(e.Key, stream.ToArray()),
                _ => (BrowseNode?)null,
            })
            .OfType<BrowseNode>()
            .ToArray();

    private BrowseNode CreateEntry(string name, byte[] data)
    {
        if (Baml.IsBamlName(name))
            return new BamlEntryNode(Module, this, name, data);
        return new ResourceEntryNode(Module, this, name, data);
    }

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
    private static GridSchema<(string Name, string Value, string Type)> Schema(LoadedModule assembly, Lazy<List<KeyValuePair<string, object?>>> entries) =>
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
