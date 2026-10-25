using System.IO.Compression;

namespace ZGF.ILSpy.Core;

/// <summary>A file in a <see cref="AssemblyPackage"/>: its name, its path from the package's root,
/// with <c>/</c> between folders, and its size.</summary>
public sealed record PackageFile(string Name, string FullName, long Length)
{
    /// <summary>The path of the folder holding it, from the package's root; empty at the root.</summary>
    public string Folder
    {
        get
        {
            var at = FullName.LastIndexOf('/');
            if (at < 0)
                return "";
            return FullName.Substring(0, at);
        }
    }
}

/// <summary>A folder in a <see cref="AssemblyPackage"/>, with the folders and files right inside
/// it, each sorted by name.</summary>
public sealed class PackageFolder(string name, string fullName)
{
    public string Name { get; } = name;

    /// <summary>The path from the package's root, with <c>/</c> between folders; empty for the root.</summary>
    public string FullName { get; } = fullName;

    public List<PackageFolder> Folders { get; } = new();

    public List<PackageFile> Files { get; } = new();
}

/// <summary>
/// A zip file such as a NuGet package: its folders and files as the archive's directory lists them.
/// A file's bytes are read out of the archive only when asked for, and the archive is not kept open.
/// </summary>
public sealed class AssemblyPackage
{
    private const char Separator = '/';

    private readonly Dictionary<string, PackageFile> _files;

    private AssemblyPackage(string filePath, PackageFolder root, Dictionary<string, PackageFile> files)
    {
        FilePath = filePath;
        Root = root;
        _files = files;
    }

    /// <summary>The absolute path of the package file.</summary>
    public string FilePath { get; }

    public PackageFolder Root { get; }

    /// <summary>The package at <paramref name="path"/>, or null when the file is no zip archive.</summary>
    /// <exception cref="IOException">The file cannot be read.</exception>
    public static AssemblyPackage? TryRead(string path)
    {
        try
        {
            using var archive = ZipFile.OpenRead(path);
            return Read(path, archive);
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private static AssemblyPackage Read(string path, ZipArchive archive)
    {
        var root = new PackageFolder("", "");
        var folders = new Dictionary<string, PackageFolder>(StringComparer.Ordinal) { [""] = root };
        var files = new Dictionary<string, PackageFile>(StringComparer.Ordinal);
        foreach (var entry in archive.Entries)
            AddEntry(entry, folders, files);
        SortFolder(root);
        return new AssemblyPackage(path, root, files);
    }

    private static void AddEntry(ZipArchiveEntry entry, Dictionary<string, PackageFolder> folders, Dictionary<string, PackageFile> files)
    {
        var fullName = entry.FullName.Replace('\\', Separator);
        var (folderName, name) = SplitName(fullName.TrimEnd(Separator));
        if (name.Length == 0)
            return;
        if (fullName.EndsWith(Separator))
        {
            FindOrAddFolder(fullName.TrimEnd(Separator), folders);
            return;
        }
        var folder = FindOrAddFolder(folderName, folders);
        var file = new PackageFile(name, fullName, entry.Length);
        folder.Files.Add(file);
        files.TryAdd(fullName, file);
    }

    private static (string Folder, string Name) SplitName(string fullName)
    {
        var at = fullName.LastIndexOf(Separator);
        if (at < 0)
            return ("", fullName);
        return (fullName.Substring(0, at), fullName.Substring(at + 1));
    }

    private static PackageFolder FindOrAddFolder(string fullName, Dictionary<string, PackageFolder> folders)
    {
        if (folders.TryGetValue(fullName, out var found))
            return found;
        var (parentName, name) = SplitName(fullName);
        var parent = FindOrAddFolder(parentName, folders);
        var folder = new PackageFolder(name, fullName);
        parent.Folders.Add(folder);
        folders.Add(fullName, folder);
        return folder;
    }

    private static void SortFolder(PackageFolder folder)
    {
        folder.Folders.Sort(static (a, b) => NaturalStringComparer.Instance.Compare(a.Name, b.Name));
        folder.Files.Sort(static (a, b) => NaturalStringComparer.Instance.Compare(a.Name, b.Name));
        foreach (var child in folder.Folders)
            SortFolder(child);
    }

    /// <summary>Every file of the package, folder by folder in the order <see cref="Root"/> lists them:
    /// a folder's folders, then its files.</summary>
    public List<PackageFile> ListFiles()
    {
        var files = new List<PackageFile>();
        CollectFiles(Root, files);
        return files;
    }

    private static void CollectFiles(PackageFolder folder, List<PackageFile> files)
    {
        foreach (var child in folder.Folders)
            CollectFiles(child, files);
        files.AddRange(folder.Files);
    }

    /// <summary>The file at <paramref name="fullName"/>, a path from the package's root; matched
    /// exactly, else ignoring case. Null when the package has none.</summary>
    public PackageFile? FindFile(string fullName)
    {
        var name = fullName.Replace('\\', Separator);
        if (_files.TryGetValue(name, out var exact))
            return exact;
        foreach (var (key, file) in _files)
        {
            if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                return file;
        }
        return null;
    }

    /// <summary>The file at <paramref name="path"/>, a path made by <see cref="PathOf"/>; null when
    /// the package has none there.</summary>
    internal PackageFile? FindFileAt(string path)
    {
        var relative = RelativeName(FilePath, path);
        return FindFile(relative);
    }

    /// <summary>The bytes of <paramref name="file"/>, read out of the archive.</summary>
    /// <exception cref="IOException">The package or the file in it cannot be read.</exception>
    public byte[] ReadFile(PackageFile file)
    {
        using var archive = ZipFile.OpenRead(FilePath);
        var entry = archive.GetEntry(file.FullName) ?? throw new IOException($"'{file.FullName}' is no longer in '{FilePath}'.");
        using var stream = entry.Open();
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    /// <summary>The path <paramref name="file"/> is known by, as if the package were a folder:
    /// <c>C:\packages\Foo.nupkg\lib\net8.0\Foo.dll</c>.</summary>
    public string PathOf(PackageFile file)
    {
        var relative = file.FullName.Replace(Separator, Path.DirectorySeparatorChar);
        return Path.Combine(FilePath, relative);
    }

    /// <summary>The package file a path made by <see cref="PathOf"/> runs through, and the path of the
    /// file inside it; null when no file on disk lies along <paramref name="path"/>.</summary>
    internal static (string Package, string FullName)? SplitPath(string path)
    {
        var folder = Path.GetDirectoryName(path);
        while (folder != null)
        {
            if (File.Exists(folder))
                return (folder, RelativeName(folder, path));
            folder = Path.GetDirectoryName(folder);
        }
        return null;
    }

    private static string RelativeName(string package, string path)
    {
        var relative = Path.GetRelativePath(package, path);
        return relative.Replace(Path.DirectorySeparatorChar, Separator);
    }
}
