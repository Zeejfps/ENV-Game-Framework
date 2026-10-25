using ZGF.ILSpy.Core.Resources;

namespace ZGF.ILSpy.Core;

/// <summary>The root of a listed package: its folders, then its files. An assembly file is the root
/// of that assembly's tree; any other file is shown as a resource is.</summary>
internal sealed class PackageNode(LoadedAssembly package, AssemblyPackage contents)
    : BrowseNode(package, null, NodeKind.Package, System.IO.Path.GetFileName(package.FilePath))
{
    public override string FullName => Assembly.FilePath;

    private protected override bool MayHaveChildren => true;

    private protected override BrowseNode[] CreateChildren() => PackageContents.ListChildren(Assembly, this, contents.Root);

    /// <summary>The node of the folder at <paramref name="folder"/>, a path from the package's root:
    /// the folder's own node, or the node it is merged into; this node for the root.</summary>
    public BrowseNode FindFolderNode(string folder)
    {
        BrowseNode node = this;
        while (true)
        {
            var next = FindChildFolderHolding(node, folder);
            if (next == null)
                return node;
            if (next.Folder.FullName == folder)
                return next;
            node = next;
        }
    }

    private static PackageFolderNode? FindChildFolderHolding(BrowseNode node, string folder)
    {
        foreach (var child in node.Children)
        {
            if (child is not PackageFolderNode folderNode)
                continue;
            if (IsWithin(folder, folderNode.Folder.FullName))
                return folderNode;
        }
        return null;
    }

    private static bool IsWithin(string folder, string candidate)
    {
        if (folder == candidate)
            return true;
        return folder.StartsWith(candidate + "/", StringComparison.Ordinal);
    }
}

/// <summary>A folder of a package. A folder holding nothing but one folder is merged with it into one
/// node, labelled with both names: <c>lib/net8.0</c>.</summary>
internal sealed class PackageFolderNode(LoadedAssembly package, BrowseNode parent, PackageFolder folder, string label)
    : BrowseNode(package, parent, NodeKind.PackageFolder, label)
{
    /// <summary>The deepest of the folders this node stands for, whose contents it lists.</summary>
    public PackageFolder Folder { get; } = folder;

    public override string FullName => Folder.FullName;

    private protected override bool MayHaveChildren => true;

    private protected override BrowseNode[] CreateChildren() => PackageContents.ListChildren(Assembly, this, Folder);
}

/// <summary>A file of a package that is no assembly: shown as text, a picture or its bytes, read out
/// of the package when needed.</summary>
internal sealed class PackageFileNode(LoadedAssembly package, BrowseNode parent, PackageFile file)
    : BrowseNode(package, parent, ResourceNodes.KindOf(file.Name), file.Name), IResourceData
{
    public override string FullName => file.FullName;

    public string Description => $"{file.FullName} ({file.Length} bytes)";

    public string FileName => file.Name;

    public byte[] ReadData() => Assembly.Package!.ReadFile(file);
}

internal static class PackageContents
{
    private static readonly string[] AssemblyExtensions = [".dll", ".exe", ".winmd"];

    /// <summary>The nodes of <paramref name="folder"/>'s folders, then of its files. An assembly file
    /// is read out of the package now, so that it shows as the assembly it is.</summary>
    public static BrowseNode[] ListChildren(LoadedAssembly package, BrowseNode parent, PackageFolder folder)
    {
        var children = new List<BrowseNode>();
        foreach (var child in folder.Folders)
            children.Add(CreateFolderNode(package, parent, child));
        foreach (var file in folder.Files)
            children.Add(CreateFileNode(package, parent, file));
        return children.ToArray();
    }

    private static PackageFolderNode CreateFolderNode(LoadedAssembly package, BrowseNode parent, PackageFolder folder)
    {
        var shown = folder;
        var label = folder.Name;
        while (HoldsOnlyOneFolder(shown))
        {
            shown = shown.Folders[0];
            label = $"{label}/{shown.Name}";
        }
        return new PackageFolderNode(package, parent, shown, label);
    }

    private static bool HoldsOnlyOneFolder(PackageFolder folder)
    {
        if (folder.Files.Count != 0)
            return false;
        return folder.Folders.Count == 1;
    }

    private static BrowseNode CreateFileNode(LoadedAssembly package, BrowseNode parent, PackageFile file)
    {
        if (!IsAssemblyName(file.Name))
            return new PackageFileNode(package, parent, file);
        var path = package.Package!.PathOf(file);
        var assembly = package.List.TryLoad(path);
        if (assembly == null)
            return new PackageFileNode(package, parent, file);
        return assembly.PlaceInPackage(parent);
    }

    private static bool IsAssemblyName(string name)
    {
        foreach (var extension in AssemblyExtensions)
        {
            if (name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
