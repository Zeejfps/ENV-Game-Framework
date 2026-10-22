using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using AssemblyReference = ICSharpCode.Decompiler.Metadata.AssemblyReference;

namespace ZGF.ILSpy.Core;

/// <summary>Thrown when a file cannot be opened as a .NET assembly or module.</summary>
public sealed class AssemblyLoadException(string filePath, string message, Exception? inner = null) : Exception(message, inner)
{
    public string FilePath { get; } = filePath;
}

/// <summary>
/// The assemblies open in the browser, plus those loaded behind the scenes to resolve references.
/// All members are thread-safe: decompilation and search read it from background threads while the
/// UI thread reads <see cref="Assemblies"/>, which is an immutable snapshot.
/// </summary>
public sealed class AssemblyList
{
    private readonly object _gate = new();
    private readonly Dictionary<string, LoadedAssembly> _known = new(StringComparer.OrdinalIgnoreCase);
    private ImmutableArray<LoadedAssembly> _listed = ImmutableArray<LoadedAssembly>.Empty;
    private int _version;

    /// <summary>The open assemblies, in the order they were opened. A snapshot: it never changes
    /// after it is read.</summary>
    public IReadOnlyList<LoadedAssembly> Assemblies => _listed;

    /// <summary>The open assemblies' file paths, in order, for persisting the list.</summary>
    public IReadOnlyList<string> FilePaths => _listed.Select(a => a.FilePath).ToArray();

    /// <summary>Increments whenever <see cref="Assemblies"/> changes.</summary>
    public int Version => Volatile.Read(ref _version);

    /// <summary>Raised after <see cref="Assemblies"/> changes, on the thread that changed it, including
    /// background threads when navigation opens a referenced assembly.</summary>
    public event Action? Changed;

    /// <summary>Opens the assembly at <paramref name="path"/>, or returns it if already open. Its type
    /// system starts loading in the background.</summary>
    /// <exception cref="AssemblyLoadException">The file is missing or not a .NET assembly.</exception>
    public LoadedAssembly Open(string path)
    {
        path = Path.GetFullPath(path);
        while (true)
        {
            var assembly = Load(path);
            if (TryList(assembly)) return assembly;
        }
    }

    /// <summary>Removes <paramref name="assembly"/> from the list. Documents and nodes already handed
    /// out stay valid.</summary>
    public void Close(LoadedAssembly assembly)
    {
        lock (_gate)
        {
            if (!_known.TryGetValue(assembly.FilePath, out var known) || known != assembly) return;
            _known.Remove(assembly.FilePath);
            if (!assembly.IsListed) return;
            assembly.IsListed = false;
            _listed = _listed.Remove(assembly);
            _version++;
        }
        Changed?.Invoke();
    }

    /// <summary>Closes every assembly.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            if (_listed.IsEmpty && _known.Count == 0) return;
            foreach (var assembly in _listed)
                assembly.IsListed = false;
            _known.Clear();
            _listed = ImmutableArray<LoadedAssembly>.Empty;
            _version++;
        }
        Changed?.Invoke();
    }

    /// <summary>
    /// The node <paramref name="reference"/> points at, opening the assembly that defines it if it is
    /// not open yet and its file can be found. For an entity hidden from the tree (compiler-generated)
    /// the nearest visible ancestor is returned. Null if the reference is not navigable or cannot be
    /// resolved. May load files: prefer calling it off the UI thread.
    /// </summary>
    public BrowseNode? FindNode(CodeReference reference)
    {
        if (reference.FilePath is null) return null;
        try
        {
            var owner = TryLoad(reference.FilePath);
            if (owner is null) return null;

            if (reference.Kind == CodeReferenceKind.Assembly && reference.Handle.Kind == HandleKind.AssemblyDefinition)
                return TryList(owner) ? owner.Root : null;

            if (reference.Kind == CodeReferenceKind.Assembly)
            {
                var target = owner.Resolver.ResolveAssembly(new AssemblyReference(owner.File, (AssemblyReferenceHandle)reference.Handle));
                return target is not null && TryList(target) ? target.Root : null;
            }

            var (assembly, handle) = ResolveDefinition(owner, reference.Handle);
            return assembly is not null && TryList(assembly) ? assembly.FindNodeOrAncestor(handle) : null;
        }
        catch (Exception e) when (e is BadImageFormatException or InvalidOperationException or IOException)
        {
            return null;
        }
    }

    /// <summary>
    /// The node <paramref name="path"/> names, opening its assembly if it is not open and its file
    /// still exists. Null if the file cannot be opened or no longer has that node. May load files
    /// and build children: prefer calling it off the UI thread.
    /// </summary>
    public BrowseNode? FindNode(NodePath path)
    {
        try
        {
            var assembly = TryLoad(path.FilePath);
            return assembly is not null && TryList(assembly) ? path.Resolve(assembly.Root) : null;
        }
        catch (Exception e) when (e is BadImageFormatException or InvalidOperationException or IOException)
        {
            return null;
        }
    }

    /// <summary>
    /// Searches the open assemblies for what <paramref name="query"/> asks, on a background thread.
    /// Results arrive in batches as they are found.
    /// </summary>
    public IAsyncEnumerable<IReadOnlyList<SearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default) =>
        AssemblySearch.Run(_listed, query, cancellationToken);

    /// <summary><see cref="SearchAsync(SearchQuery, CancellationToken)"/> over
    /// <paramref name="assemblies"/> only, e.g. those opened since the last search.</summary>
    public IAsyncEnumerable<IReadOnlyList<SearchResult>> SearchAsync(SearchQuery query, IEnumerable<LoadedAssembly> assemblies,
        CancellationToken cancellationToken = default) =>
        AssemblySearch.Run([..assemblies], query, cancellationToken);

    /// <summary><see cref="SearchAsync(SearchQuery, CancellationToken)"/> for
    /// <paramref name="text"/> parsed with <see cref="SearchQuery.Parse"/>.</summary>
    public IAsyncEnumerable<IReadOnlyList<SearchResult>> SearchAsync(string text, SearchKinds kinds = SearchKinds.TypesAndMembers,
        CancellationToken cancellationToken = default) =>
        SearchAsync(SearchQuery.Parse(text, kinds), cancellationToken);

    internal LoadedAssembly? TryLoad(string path)
    {
        try
        {
            return Load(path);
        }
        catch (AssemblyLoadException)
        {
            return null;
        }
    }

    /// <summary>The type system of the assembly loaded from <paramref name="module"/>.</summary>
    internal DecompilerTypeSystem TypeSystemOf(MetadataFile module)
    {
        lock (_gate)
        {
            foreach (var assembly in _known.Values)
            {
                if (assembly.File == module)
                    return assembly.TypeSystem;
            }
        }
        throw new InvalidOperationException($"{module.FileName} was not loaded by this list.");
    }

    internal LoadedAssembly? FindKnown(IAssemblyReference reference, bool exactOrNewer)
    {
        lock (_gate)
        {
            LoadedAssembly? best = null;
            foreach (var assembly in _known.Values)
            {
                if (!string.Equals(assembly.Name, reference.Name, StringComparison.OrdinalIgnoreCase)) continue;
                if (exactOrNewer && reference.Version != null && assembly.Version != null && assembly.Version < reference.Version) continue;
                if (best is null || (assembly.Version ?? new Version()) > (best.Version ?? new Version()))
                    best = assembly;
            }
            return best;
        }
    }

    private LoadedAssembly Load(string path)
    {
        lock (_gate)
        {
            if (_known.TryGetValue(path, out var existing)) return existing;
        }

        var assembly = new LoadedAssembly(this, ReadFile(path));
        lock (_gate)
        {
            if (_known.TryGetValue(path, out var existing)) return existing;
            _known.Add(path, assembly);
        }
        return assembly;
    }

    internal bool TryList(LoadedAssembly assembly)
    {
        lock (_gate)
        {
            if (!_known.TryGetValue(assembly.FilePath, out var known) || known != assembly) return false;
            if (assembly.IsListed) return true;
            assembly.IsListed = true;
            _listed = _listed.Add(assembly);
            _version++;
        }
        ThreadPool.QueueUserWorkItem(static a => a.WarmUp(), assembly, preferLocal: false);
        Changed?.Invoke();
        return true;
    }

    private (LoadedAssembly? Assembly, EntityHandle Handle) ResolveDefinition(LoadedAssembly owner, EntityHandle handle)
    {
        switch (handle.Kind)
        {
            case HandleKind.TypeDefinition or HandleKind.MethodDefinition or HandleKind.FieldDefinition
                or HandleKind.PropertyDefinition or HandleKind.EventDefinition:
                return (owner, handle);
        }

        return Definitions.Resolve(owner.Module, handle) is { } definition
            ? (TryLoad(definition.ParentModule!.MetadataFile!.FileName), definition.MetadataToken)
            : (null, default);
    }

    private static PEFile ReadFile(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var file = new PEFile(path, stream, PEStreamOptions.PrefetchEntireImage, MetadataReaderOptions.Default);
            if (!file.Reader.HasMetadata)
                throw new AssemblyLoadException(path, $"'{path}' is not a .NET assembly.");
            return file;
        }
        catch (AssemblyLoadException)
        {
            throw;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or BadImageFormatException or InvalidOperationException or MetadataFileNotSupportedException)
        {
            throw new AssemblyLoadException(path, $"Cannot open '{path}': {e.Message}", e);
        }
    }
}
