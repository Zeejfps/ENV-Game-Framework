using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core;

internal sealed class AssemblyResolver : IAssemblyResolver
{
    private static readonly string[] Extensions = [".dll", ".exe", ".winmd"];

    private readonly AssemblyList _list;
    private readonly string _mainPath;
    private readonly Lazy<UniversalAssemblyResolver> _fallback;

    public AssemblyResolver(AssemblyList list, string mainPath, string? targetFramework)
    {
        _list = list;
        _mainPath = mainPath;
        _fallback = new Lazy<UniversalAssemblyResolver>(() =>
            new UniversalAssemblyResolver(mainPath, false, targetFramework, null, PEStreamOptions.PrefetchEntireImage, MetadataReaderOptions.Default));
    }

    public MetadataFile? Resolve(IAssemblyReference reference) => ResolveAssembly(reference)?.File;

    public LoadedAssembly? ResolveAssembly(IAssemblyReference reference)
    {
        if (_list.FindKnown(reference, exactOrNewer: true) is { } known) return known;
        var path = FindFile(reference);
        if (path != null && _list.TryLoad(path) is { } loaded) return loaded;
        return _list.FindKnown(reference, exactOrNewer: false);
    }

    public string? FindFile(IAssemblyReference reference)
    {
        var directory = Path.GetDirectoryName(_mainPath);
        if (directory != null)
        {
            foreach (var extension in Extensions)
            {
                var candidate = Path.Combine(directory, reference.Name + extension);
                if (File.Exists(candidate)) return candidate;
            }
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

    public MetadataFile? ResolveModule(MetadataFile mainModule, string moduleName)
    {
        var directory = Path.GetDirectoryName(mainModule.FileName);
        if (directory is null) return null;
        var path = Path.Combine(directory, moduleName);
        return File.Exists(path) ? _list.TryLoad(path)?.File : null;
    }

    public Task<MetadataFile?> ResolveAsync(IAssemblyReference reference) => Task.FromResult(Resolve(reference));

    public Task<MetadataFile?> ResolveModuleAsync(MetadataFile mainModule, string moduleName) =>
        Task.FromResult(ResolveModule(mainModule, moduleName));
}
