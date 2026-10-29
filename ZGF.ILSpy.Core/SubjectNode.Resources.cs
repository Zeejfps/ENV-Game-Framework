using ZGF.ILSpy.Core.Resources;
using MetadataResource = ICSharpCode.Decompiler.Metadata.Resource;

namespace ZGF.ILSpy.Core;

internal sealed partial class SubjectNode
{
    /// <summary>The module's manifest resources, sorted by name.</summary>
    private BrowseNode[] ListResources() =>
        Module.File.Resources
            .OrderBy(r => r.Name, NaturalStringComparer.Instance)
            .Select(r => (BrowseNode)new SubjectNode(Module, this, SubjectOf(r)))
            .ToArray();

    /// <summary>What a manifest resource stands for: the entries of a <c>.resources</c> file or of
    /// Avalonia's asset container, or the resource itself.</summary>
    private NodeSubject SubjectOf(MetadataResource resource)
    {
        if (AvaloniaResources.IsContainerName(resource.Name))
            return new NodeSubject.ResourceContainer(ResourceContainerInfo.AvaloniaAssetsOf(Module, resource));
        if (resource.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
            return new NodeSubject.ResourceContainer(ResourceContainerInfo.ResourcesFileOf(Module, resource));
        return new NodeSubject.ResourceEntry(new ResourceEntryInfo(Module, resource.Name, new ResourceSource.Manifest(resource)));
    }

    /// <summary>The entries of a container: a <c>.resources</c> file's byte and stream entries, its BAML
    /// among them, or Avalonia's assets, sorted by their paths.</summary>
    private BrowseNode[] ListEntries(ResourceContainerInfo container) => container.Contents switch
    {
        ContainerContents.ResourcesFile file => file.Entries.Value
            .Select(e => e.Value switch
            {
                byte[] bytes => Entry(e.Key, bytes),
                MemoryStream stream => Entry(e.Key, stream.ToArray()),
                _ => (BrowseNode?)null,
            })
            .OfType<BrowseNode>()
            .ToArray(),
        ContainerContents.AvaloniaAssets => AvaloniaResources.ReadEntries(container.ReadData())
            .OrderBy(e => e.Path, NaturalStringComparer.Instance)
            .Select(e => (BrowseNode)new SubjectNode(Module, this, new NodeSubject.ResourceEntry(new ResourceEntryInfo(Module, e.Path, new ResourceSource.Entry(e.Data)))))
            .ToArray(),
        _ => throw new System.Diagnostics.UnreachableException(),
    };

    private BrowseNode Entry(string name, byte[] data)
    {
        ResourceSource source = Baml.IsBamlName(name) ? new ResourceSource.Baml(data) : new ResourceSource.Entry(data);
        return new SubjectNode(Module, this, new NodeSubject.ResourceEntry(new ResourceEntryInfo(Module, name, source)));
    }

    private static NodeKind KindOf(ResourceEntryInfo entry) => entry.Source switch
    {
        ResourceSource.Baml => NodeKind.Resource,
        ResourceSource.Manifest or ResourceSource.Entry => ResourceNodes.KindOf(entry.Name),
        _ => throw new System.Diagnostics.UnreachableException(),
    };
}
