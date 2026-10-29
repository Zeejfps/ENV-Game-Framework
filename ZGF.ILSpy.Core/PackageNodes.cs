using ZGF.ILSpy.Core.Resources;

namespace ZGF.ILSpy.Core;

/// <summary>A folder of a listed package, labelled <see cref="Label"/>: the package's root, labelled with
/// its file's name, or a folder in it, which when it holds nothing but one folder is merged with it into
/// one, labelled with both names, <c>lib/net8.0</c>, and listing the deeper one's contents.</summary>
public sealed class PackageFolderInfo
{
    internal PackageFolderInfo(LoadedPackage package, PackageFolder folder, string label)
    {
        Package = package;
        Folder = folder;
        Label = label;
    }

    public LoadedPackage Package { get; }

    /// <summary>The folder whose contents it lists.</summary>
    public PackageFolder Folder { get; }

    public string Label { get; }

    /// <summary>Whether this is the package's root, standing for the package itself.</summary>
    public bool IsRoot => ReferenceEquals(Folder, Package.Package.Root);

    public string FullName => IsRoot ? Package.FilePath.Text : Folder.FullName;

    internal static PackageFolderInfo RootOf(LoadedPackage package) => new(package, package.Package.Root, package.FilePath.FileName);
}

/// <summary>A file of a package that is no assembly: shown as text, a picture or its bytes, read out of
/// the package when needed.</summary>
public sealed class PackageFileInfo : IResourceData
{
    internal PackageFileInfo(LoadedPackage package, PackageFile file)
    {
        Package = package;
        File = file;
    }

    public LoadedPackage Package { get; }

    public PackageFile File { get; }

    public string Description => $"{File.FullName} ({File.Length} bytes)";

    public string FileName => File.Name;

    public byte[] ReadData() => Package.Package.ReadFile(File);
}

internal static class PackageContents
{
    private static readonly string[] AssemblyExtensions = [".dll", ".exe", ".winmd"];

    public static bool IsAssemblyName(string name)
    {
        foreach (var extension in AssemblyExtensions)
        {
            if (name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
