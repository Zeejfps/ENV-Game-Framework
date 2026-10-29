using ZGF.ILSpy.Core.Resources;
using MetadataResource = ICSharpCode.Decompiler.Metadata.Resource;

namespace ZGF.ILSpy.Core;

public sealed partial class BrowseNode
{
    /// <summary>The module's manifest resources, sorted by name.</summary>
    private BrowseNode[] ListResources(LoadedModule module) =>
        module.File.Resources
            .OrderBy(r => r.Name, NaturalStringComparer.Instance)
            .Select(r => (BrowseNode)new BrowseNode(this, SubjectOf(module, r)))
            .ToArray();

    /// <summary>What a manifest resource stands for: the entries of a <c>.resources</c> file or of
    /// Avalonia's asset container, or the resource itself.</summary>
    private static NodeSubject SubjectOf(LoadedModule module, MetadataResource resource)
    {
        if (AvaloniaResources.IsContainerName(resource.Name))
            return new NodeSubject.ResourceContainer(ResourceContainerInfo.AvaloniaAssetsOf(module, resource));
        if (resource.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
            return new NodeSubject.ResourceContainer(ResourceContainerInfo.ResourcesFileOf(module, resource));
        return new NodeSubject.ResourceEntry(new ResourceEntryInfo(module, resource.Name, new ResourceSource.Manifest(resource)));
    }

    /// <summary>The entries of a container: a <c>.resources</c> file's byte and stream entries, its BAML
    /// among them, or Avalonia's assets, sorted by their paths.</summary>
    private BrowseNode[] ListEntries(ResourceContainerInfo container) => container.Contents switch
    {
        ContainerContents.ResourcesFile file => file.Entries.Value
            .Select(e => e.Value switch
            {
                byte[] bytes => Entry(container.Module, e.Key, bytes),
                MemoryStream stream => Entry(container.Module, e.Key, stream.ToArray()),
                _ => (BrowseNode?)null,
            })
            .OfType<BrowseNode>()
            .ToArray(),
        ContainerContents.AvaloniaAssets => AvaloniaResources.ReadEntries(container.ReadData())
            .OrderBy(e => e.Path, NaturalStringComparer.Instance)
            .Select(e => (BrowseNode)new BrowseNode(this, new NodeSubject.ResourceEntry(new ResourceEntryInfo(container.Module, e.Path, new ResourceSource.Entry(e.Data)))))
            .ToArray(),
        _ => throw new System.Diagnostics.UnreachableException(),
    };

    private BrowseNode Entry(LoadedModule module, string name, byte[] data)
    {
        ResourceSource source = Baml.IsBamlName(name) ? new ResourceSource.Baml(data) : new ResourceSource.Entry(data);
        return new BrowseNode(this, new NodeSubject.ResourceEntry(new ResourceEntryInfo(module, name, source)));
    }

    private static NodeKind KindOf(ResourceEntryInfo entry) => entry.Source switch
    {
        ResourceSource.Baml => NodeKind.Resource,
        ResourceSource.Manifest or ResourceSource.Entry => ResourceNodes.KindOf(entry.Name),
        _ => throw new System.Diagnostics.UnreachableException(),
    };
}
