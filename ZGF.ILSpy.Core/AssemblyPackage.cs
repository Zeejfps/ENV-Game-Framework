using System.IO.Compression;
using ICSharpCode.Decompiler;

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

/// <summary>A file of a package that was not extracted: <see cref="Error"/> says why, or is null when
/// its path would have put it outside the folder extracted into.</summary>
public sealed record ExtractFailure(PackageFile File, ExportError? Error);

/// <summary>The paths extracting a package's folder wrote, and the files it could not.</summary>
public sealed record ExtractedFiles(IReadOnlyList<string> Written, IReadOnlyList<ExtractFailure> Failures);

/// <summary>
/// A file holding assemblies and other files: a zip file such as a NuGet package, or an app published
/// as a single-file bundle. Its folders and files are listed from the zip's directory or the bundle's
/// manifest; a file's bytes are read out only when asked for, and the package is not kept open.
/// </summary>
public sealed class AssemblyPackage
{
    private const char Separator = '/';

    private readonly Dictionary<string, PackageFile> _files;
    private readonly Func<PackageFile, byte[]> _read;

    private AssemblyPackage(string filePath, bool isBundle, PackageFolder root, Dictionary<string, PackageFile> files, Func<PackageFile, byte[]> read)
    {
        FilePath = filePath;
        IsBundle = isBundle;
        Root = root;
        _files = files;
        _read = read;
    }

    /// <summary>The absolute path of the package file.</summary>
    public string FilePath { get; }

    /// <summary>Whether this is an app published as a single-file bundle, not a zip.</summary>
    public bool IsBundle { get; }

    public PackageFolder Root { get; }

    /// <summary>The package at <paramref name="path"/>: a zip archive, else a single-file bundle; null
    /// when the file is neither.</summary>
    /// <exception cref="IOException">The file cannot be read.</exception>
    public static AssemblyPackage? TryRead(string path)
    {
        var zip = TryReadZip(path);
        if (zip != null)
            return zip;
        return TryReadBundle(path);
    }

    private static AssemblyPackage? TryReadZip(string path)
    {
        try
        {
            using var archive = ZipFile.OpenRead(path);
            return ReadZip(path, archive);
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private static AssemblyPackage ReadZip(string path, ZipArchive archive)
    {
        var tree = new FileTree();
        foreach (var entry in archive.Entries)
            AddZipEntry(tree, entry);
        return tree.CreatePackage(path, isBundle: false, file => ReadZipFile(path, file));
    }

    private static void AddZipEntry(FileTree tree, ZipArchiveEntry entry)
    {
        var fullName = Normalize(entry.FullName);
        if (fullName.EndsWith(Separator))
        {
            tree.AddFolder(fullName.TrimEnd(Separator));
            return;
        }
        tree.AddFile(fullName, entry.Length);
    }

    private static byte[] ReadZipFile(string path, PackageFile file)
    {
        using var archive = ZipFile.OpenRead(path);
        var entry = archive.GetEntry(file.FullName) ?? throw new IOException($"'{file.FullName}' is no longer in '{path}'.");
        using var stream = entry.Open();
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    private static AssemblyPackage? TryReadBundle(string path)
    {
        var entries = BundleContents.TryReadManifest(path);
        if (entries == null)
            return null;
        var tree = new FileTree();
        var byName = new Dictionary<string, SingleFileBundle.Entry>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            var fullName = Normalize(entry.RelativePath);
            tree.AddFile(fullName, entry.Size);
            byName.TryAdd(fullName, entry);
        }
        return tree.CreatePackage(path, isBundle: true, file => BundleContents.Read(path, byName[file.FullName]));
    }

    private static string Normalize(string fullName) => fullName.Replace('\\', Separator);

    private static (string Folder, string Name) SplitName(string fullName)
    {
        var at = fullName.LastIndexOf(Separator);
        if (at < 0)
            return ("", fullName);
        return (fullName.Substring(0, at), fullName.Substring(at + 1));
    }

    /// <summary>The folders and files of a package as they are listed, sorted into a tree once all are in.</summary>
    private sealed class FileTree
    {
        private readonly PackageFolder _root = new("", "");
        private readonly Dictionary<string, PackageFolder> _folders = new(StringComparer.Ordinal);
        private readonly Dictionary<string, PackageFile> _files = new(StringComparer.Ordinal);

        public FileTree()
        {
            _folders.Add("", _root);
        }

        public void AddFile(string fullName, long length)
        {
            var (folderName, name) = SplitName(fullName);
            if (name.Length == 0)
                return;
            var file = new PackageFile(name, fullName, length);
            AddFolder(folderName).Files.Add(file);
            _files.TryAdd(fullName, file);
        }

        public PackageFolder AddFolder(string fullName)
        {
            if (_folders.TryGetValue(fullName, out var found))
                return found;
            var (parentName, name) = SplitName(fullName);
            var parent = AddFolder(parentName);
            var folder = new PackageFolder(name, fullName);
            parent.Folders.Add(folder);
            _folders.Add(fullName, folder);
            return folder;
        }

        public AssemblyPackage CreatePackage(string path, bool isBundle, Func<PackageFile, byte[]> read)
        {
            SortFolder(_root);
            return new AssemblyPackage(path, isBundle, _root, _files, read);
        }

        private static void SortFolder(PackageFolder folder)
        {
            folder.Folders.Sort(static (a, b) => NaturalStringComparer.Instance.Compare(a.Name, b.Name));
            folder.Files.Sort(static (a, b) => NaturalStringComparer.Instance.Compare(a.Name, b.Name));
            foreach (var child in folder.Folders)
                SortFolder(child);
        }
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

    /// <summary>Writes every file in <paramref name="folder"/> and the folders in it into
    /// <paramref name="target"/>, at its path from <paramref name="folder"/>. A file that cannot be
    /// read or written, or whose path leads out of <paramref name="target"/>, is left out and listed as
    /// failed.</summary>
    public ExtractedFiles ExtractFolder(PackageFolder folder, string target, IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var files = new List<PackageFile>();
        CollectFiles(folder, files);
        var root = Path.GetFullPath(target);
        var written = new List<string>();
        var failures = new List<ExtractFailure>();
        for (var i = 0; i < files.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new ExportProgress(folder.Name, 1, 1, (float)i / files.Count));
            var failure = TryExtract(files[i], folder, root, written);
            if (failure != null)
                failures.Add(failure);
        }
        return new ExtractedFiles(written, failures);
    }

    private ExtractFailure? TryExtract(PackageFile file, PackageFolder folder, string root, List<string> written)
    {
        try
        {
            var path = TargetPathOf(file, folder, root);
            if (path == null)
                return new ExtractFailure(file, null);
            WriteFile(file, path);
            written.Add(path);
            return null;
        }
        catch (Exception e) when (IsExtractError(e))
        {
            return new ExtractFailure(file, new ExportError(e.Message, e.ToString()));
        }
    }

    /// <summary>Where <paramref name="file"/> goes under <paramref name="root"/>, at its path from
    /// <paramref name="folder"/>; null when that path leads out of <paramref name="root"/>.</summary>
    private static string? TargetPathOf(PackageFile file, PackageFolder folder, string root)
    {
        var relative = RelativeTo(folder, file).Replace(Separator, Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(Path.Combine(root, relative));
        if (!IsInside(path, root))
            return null;
        return path;
    }

    private static string RelativeTo(PackageFolder folder, PackageFile file)
    {
        if (folder.FullName.Length == 0)
            return file.FullName;
        return file.FullName.Substring(folder.FullName.Length + 1);
    }

    private static bool IsInside(string path, string root)
    {
        var prefix = root;
        if (!Path.EndsInDirectorySeparator(prefix))
            prefix += Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return path.StartsWith(prefix, comparison);
    }

    private void WriteFile(PackageFile file, string path)
    {
        var bytes = ReadFile(file);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
    }

    private static bool IsExtractError(Exception e)
    {
        if (e is IOException)
            return true;
        if (e is UnauthorizedAccessException)
            return true;
        if (e is InvalidDataException)
            return true;
        return e is ArgumentException;
    }

    /// <summary>The bytes of <paramref name="file"/>, read out of the package.</summary>
    /// <exception cref="IOException">The package or the file in it cannot be read.</exception>
    /// <exception cref="InvalidDataException">The package says something about the file that cannot be true.</exception>
    public byte[] ReadFile(PackageFile file) => _read(file);

    /// <summary>The path <paramref name="file"/> is known by, as if the package were a folder:
    /// <c>C:\packages\Foo.nupkg\lib\net8.0\Foo.dll</c>.</summary>
    public string PathOf(PackageFile file)
    {
        var relative = file.FullName.Replace(Separator, Path.DirectorySeparatorChar);
        return Path.Combine(FilePath, relative);
    }

    /// <summary>Whether <paramref name="path"/> is one <see cref="PathOf"/> made for a file in the package
    /// at <paramref name="packagePath"/>.</summary>
    internal static bool Holds(string packagePath, string path) =>
        path.Length > packagePath.Length
        && path[packagePath.Length] == Path.DirectorySeparatorChar
        && path.StartsWith(packagePath, StringComparison.OrdinalIgnoreCase);

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
