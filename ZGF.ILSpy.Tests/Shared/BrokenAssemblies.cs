using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace ZGF.ILSpy.Tests;

/// <summary>Assemblies that load but hold something that cannot be read.</summary>
internal static class BrokenAssemblies
{
    public const string AvaloniaContainer = "!AvaloniaResources";

    /// <summary>An assembly whose Avalonia asset container is of a version no reader knows, so the
    /// container's children cannot be built.</summary>
    public static string WriteUnreadableAvaloniaContainer(TempDir temp)
    {
        byte[] container = [8, 0, 0, 0, 9, 0, 0, 0, 0, 0, 0, 0];
        var metadata = new MetadataBuilder();
        var name = metadata.GetOrAddString("Unreadable");
        metadata.AddModule(0, name, metadata.GetOrAddGuid(Guid.NewGuid()), default, default);
        metadata.AddAssembly(name, new Version(1, 0), default, default, 0, AssemblyHashAlgorithm.None);
        metadata.AddTypeDefinition(default, default, metadata.GetOrAddString("<Module>"), default,
            MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
        metadata.AddManifestResource(ManifestResourceAttributes.Public, metadata.GetOrAddString(AvaloniaContainer), default, 0);

        var resources = new BlobBuilder();
        resources.WriteInt32(container.Length);
        resources.WriteBytes(container);
        var pe = new ManagedPEBuilder(PEHeaderBuilder.CreateLibraryHeader(), new MetadataRootBuilder(metadata), new BlobBuilder(),
            managedResources: resources);
        var image = new BlobBuilder();
        pe.Serialize(image);
        return temp.Write("Unreadable.dll", image.ToArray());
    }
}
