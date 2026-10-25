using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core;

internal sealed class AssemblyResolver : IAssemblyResolver
{
    private static readonly string[] Extensions = [".dll", ".exe", ".winmd"];

    private readonly AssemblyList _list;
    private readonly string _mainPath;
    private readonly AssemblyPackage? _package;
    private readonly Lazy<UniversalAssemblyResolver> _fallback;

    /// <param name="package">The package the main assembly was read out of, whose folders are looked
    /// in first, from the main assembly's outwards; null for a file on disk.</param>
    public AssemblyResolver(AssemblyList list, string mainPath, string? targetFramework, AssemblyPackage? package)
    {
        _list = list;
        _mainPath = mainPath;
        _package = package;
        _fallback = new Lazy<UniversalAssemblyResolver>(() =>
            new UniversalAssemblyResolver(mainPath, false, targetFramework, null, PEStreamOptions.PrefetchEntireImage, MetadataReaderOptions.Default));
    }

    public MetadataFile? Resolve(IAssemblyReference reference) => ResolveAssembly(reference)?.File;

    public IDisposable? BeginSnapshot() => null;

    public LoadedAssembly? ResolveAssembly(IAssemblyReference reference)
    {
        if (_list.FindKnown(reference, exactOrNewer: true) is { } known) return known;
        var path = FindFile(reference);
        if (path != null && _list.TryLoad(path) is { } loaded) return loaded;
        return _list.FindKnown(reference, exactOrNewer: false);
    }

    public string? FindFile(IAssemblyReference reference)
    {
        foreach (var directory in ListNearbyDirectories())
        {
            var found = FindIn(directory, reference.Name);
            if (found != null)
                return found;
        }
        if (RuntimeAssemblies.Find(reference.Name) is { } runtime) return runtime;
        try
        {
            return _fallback.Value.FindAssemblyFile(reference);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or BadImageFormatException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>The main assembly's directory; for one read out of a package, then each folder of the
    /// package above it, up to the package's root.</summary>
    private IEnumerable<string> ListNearbyDirectories()
    {
        var directory = Path.GetDirectoryName(_mainPath);
        if (directory == null)
            yield break;
        yield return directory;
        if (_package == null)
            yield break;
        while (directory.Length > _package.FilePath.Length)
        {
            directory = Path.GetDirectoryName(directory)!;
            yield return directory;
        }
    }

    private string? FindIn(string directory, string name)
    {
        foreach (var extension in Extensions)
        {
            var candidate = Path.Combine(directory, name + extension);
            if (Exists(candidate))
                return candidate;
        }
        return null;
    }

    /// <summary>Whether there is a file at <paramref name="path"/>, on disk or in the main assembly's package.</summary>
    private bool Exists(string path)
    {
        if (File.Exists(path))
            return true;
        if (_package == null)
            return false;
        return _package.FindFileAt(path) != null;
    }

    public MetadataFile? ResolveModule(MetadataFile mainModule, string moduleName)
    {
        var directory = Path.GetDirectoryName(mainModule.FileName);
        if (directory is null) return null;
        var path = Path.Combine(directory, moduleName);
        return Exists(path) ? _list.TryLoad(path)?.File : null;
    }

    public Task<MetadataFile?> ResolveAsync(IAssemblyReference reference) => Task.FromResult(Resolve(reference));

    public Task<MetadataFile?> ResolveModuleAsync(MetadataFile mainModule, string moduleName) =>
        Task.FromResult(ResolveModule(mainModule, moduleName));
}
