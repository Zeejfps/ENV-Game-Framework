using System.Text;
using MetadataResource = ICSharpCode.Decompiler.Metadata.Resource;

namespace ZGF.ILSpy.Core.Resources;

/// <summary>Where a resource's bytes come from: a manifest resource of the module, the bytes of an entry
/// of a container, or the compiled XAML of a <c>.resources</c> file's BAML entry, saved as the XAML.</summary>
internal abstract record ResourceSource
{
    private ResourceSource()
    {
    }

    public sealed record Manifest(MetadataResource Resource) : ResourceSource;

    public sealed record Entry(byte[] Data) : ResourceSource;

    public sealed record Baml(byte[] Data) : ResourceSource;
}

/// <summary>A resource shown as its bytes: text, a picture, a hex dump, or for BAML the XAML it was
/// compiled from; saved as its bytes, or as that XAML.</summary>
public sealed class ResourceEntryInfo : IResourceData
{
    internal ResourceEntryInfo(LoadedModule module, string name, ResourceSource source)
    {
        Module = module;
        Name = name;
        Source = source;
    }

    public LoadedModule Module { get; }

    /// <summary>The resource's name, or the entry's path in its container.</summary>
    public string Name { get; }

    internal ResourceSource Source { get; }

    public string Description => Source switch
    {
        ResourceSource.Manifest manifest => ResourceNodes.Describe(manifest.Resource),
        ResourceSource.Entry entry => $"{Name} ({entry.Data.Length} bytes)",
        ResourceSource.Baml baml => $"{Name} ({baml.Data.Length} bytes)",
        _ => throw new System.Diagnostics.UnreachableException(),
    };

    public string FileName => Source switch
    {
        ResourceSource.Baml => Resources.Baml.XamlName(Path.GetFileName(Name)),
        ResourceSource.Manifest or ResourceSource.Entry => Path.GetFileName(Name),
        _ => throw new System.Diagnostics.UnreachableException(),
    };

    public byte[] ReadData() => Source switch
    {
        ResourceSource.Manifest manifest => ResourceNodes.Read(manifest.Resource),
        ResourceSource.Entry entry => entry.Data,
        ResourceSource.Baml baml => Encoding.UTF8.GetBytes(Resources.Baml.DecompileToText(Module, baml.Data, CancellationToken.None)),
        _ => throw new System.Diagnostics.UnreachableException(),
    };
}

/// <summary>What a resource container holds besides its bytes: a <c>.resources</c> file its entries, read
/// once, and the grid of them; Avalonia's asset container, assets read when it is listed.</summary>
internal abstract record ContainerContents
{
    private ContainerContents()
    {
    }

    public sealed record ResourcesFile(Lazy<List<KeyValuePair<string, object?>>> Entries, Lazy<Metadata.MetadataGrid> Grid) : ContainerContents;

    public sealed record AvaloniaAssets : ContainerContents;
}

/// <summary>A manifest resource that holds entries of its own: a <c>.resources</c> file, or the container
/// Avalonia embeds an app's assets in. It is its bytes, which it is saved as, and lists its entries.</summary>
public sealed class ResourceContainerInfo : IResourceData
{
    private ResourceContainerInfo(LoadedModule module, MetadataResource resource, ContainerContents contents)
    {
        Module = module;
        Resource = resource;
        Contents = contents;
    }

    public LoadedModule Module { get; }

    public MetadataResource Resource { get; }

    internal ContainerContents Contents { get; }

    public string Description => ResourceNodes.Describe(Resource);

    public string FileName => Path.GetFileName(Resource.Name);

    public byte[] ReadData() => ResourceNodes.Read(Resource);

    /// <summary>The grid of a <c>.resources</c> file's entries; null for an asset container, which has none.</summary>
    public Metadata.MetadataGrid? Grid => Contents switch
    {
        ContainerContents.ResourcesFile file => file.Grid.Value,
        ContainerContents.AvaloniaAssets => null,
        _ => throw new System.Diagnostics.UnreachableException(),
    };

    internal static ResourceContainerInfo ResourcesFileOf(LoadedModule module, MetadataResource resource)
    {
        var entries = new Lazy<List<KeyValuePair<string, object?>>>(() => ResourceNodes.ReadEntries(resource));
        var grid = new Lazy<Metadata.MetadataGrid>(() => new Metadata.MetadataGrid(resource.Name, module, module.File, ResourceNodes.Schema(module, entries)));
        return new ResourceContainerInfo(module, resource, new ContainerContents.ResourcesFile(entries, grid));
    }

    internal static ResourceContainerInfo AvaloniaAssetsOf(LoadedModule module, MetadataResource resource) =>
        new(module, resource, new ContainerContents.AvaloniaAssets());
}
