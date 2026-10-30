namespace ZGF.ILSpy.Core;

public sealed partial class BrowseNode
{
    /// <summary>The root of a listed package or bundle's tree.</summary>
    internal static BrowseNode PackageRoot(LoadedPackage package) => new(null, new NodeSubject.Package(PackageFolderInfo.RootOf(package)));

    /// <summary>The folder's folders, then its files. An assembly file is the root of that assembly's tree,
    /// read out of the package now, so that it shows as the assembly it is; any other file is shown as a
    /// resource is.</summary>
    private BrowseNode[] ListPackageFolder(PackageFolderInfo folder)
    {
        var package = folder.Package;
        var children = new List<BrowseNode>();
        foreach (var child in folder.Folder.Folders)
            children.Add(new BrowseNode(this, new NodeSubject.Package(MergedFolder(package, child))));
        foreach (var file in folder.Folder.Files)
            children.Add(FileNode(package, file));
        return children.ToArray();
    }

    /// <summary>The folder, merged with each folder it holds when that is all it holds.</summary>
    private static PackageFolderInfo MergedFolder(LoadedPackage package, PackageFolder folder)
    {
        var shown = folder;
        var label = folder.Name;
        while (HoldsOnlyOneFolder(shown))
        {
            shown = shown.Folders[0];
            label = $"{label}/{shown.Name}";
        }
        return new PackageFolderInfo(package, folder, shown, label);
    }

    private static bool HoldsOnlyOneFolder(PackageFolder folder)
    {
        if (folder.Files.Count != 0)
            return false;
        return folder.Folders.Count == 1;
    }

    private BrowseNode FileNode(LoadedPackage package, PackageFile file)
    {
        var assembly = PackageContents.IsAssemblyName(file.Name) ? package.FindAssembly(file) : null;
        if (assembly == null)
            return new BrowseNode(this, new NodeSubject.PackageFile(new PackageFileInfo(package, file)));
        return ModuleRoot(assembly, this);
    }
}
