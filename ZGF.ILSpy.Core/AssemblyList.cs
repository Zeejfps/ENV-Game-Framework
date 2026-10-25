using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ZGF.ILSpy.Core.Metadata;
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
    private ImmutableArray<LoadedAssembly> _loaded = ImmutableArray<LoadedAssembly>.Empty;
    private int _version;
    private int _generation;

    /// <summary>The open assemblies that loaded, in the order they were opened. A snapshot: it never
    /// changes after it is read.</summary>
    public IReadOnlyList<LoadedAssembly> Assemblies => _loaded;

    /// <summary>The open files in the order they were opened, those that failed to load
    /// (<see cref="LoadedAssembly.HasLoadError"/>) among them. A snapshot, like <see cref="Assemblies"/>.</summary>
    public IReadOnlyList<LoadedAssembly> Listed => _listed;

    /// <summary>The open files' paths, in order, failed ones included, for persisting the list.</summary>
    public IReadOnlyList<string> FilePaths => _listed.Select(a => a.FilePath).ToArray();

    /// <summary>Increments whenever <see cref="Assemblies"/> changes.</summary>
    public int Version => Volatile.Read(ref _version);

    /// <summary>Increments whenever the list is cleared. Work started before a clear neither lists nor
    /// keeps the assemblies it loads after it, so that what it finds late never lands in the list
    /// that follows.</summary>
    public int Generation => Volatile.Read(ref _generation);

    /// <summary>Raised after <see cref="Assemblies"/> changes, on the thread that changed it, including
    /// background threads when navigation opens a referenced assembly.</summary>
    public event Action? Changed;

    /// <summary>Opens the assembly at <paramref name="path"/>, or returns it if already open. Its type
    /// system starts loading in the background. A file that is missing or not a .NET assembly is listed
    /// all the same, with <see cref="LoadedAssembly.HasLoadError"/> set, and the assembly takes its place
    /// once the file loads. Once <paramref name="cancellationToken"/> is cancelled
    /// the assembly is no longer listed, so cancelling before <see cref="Clear"/> keeps the opens of
    /// several files out of the list that follows.</summary>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled,
    /// or the list was cleared while the file was read.</exception>
    public LoadedAssembly Open(string path, CancellationToken cancellationToken = default)
    {
        path = Path.GetFullPath(path);
        var generation = Generation;
        while (true)
        {
            var assembly = LoadOrFail(path, generation);
            if (TryList(assembly, generation, cancellationToken)) return assembly;
            cancellationToken.ThrowIfCancellationRequested();
            if (generation != Generation)
                throw new OperationCanceledException($"The list was cleared while '{path}' was opened.");
        }
    }

    /// <summary>The assembly read from <paramref name="path"/>, or when it cannot be read, the failure
    /// listed for it, else a new one.</summary>
    private LoadedAssembly LoadOrFail(string path, int generation)
    {
        try
        {
            return Load(path, generation);
        }
        catch (AssemblyLoadException e)
        {
            return FindListedFailure(path) ?? new LoadedAssembly(this, e);
        }
    }

    /// <summary>Removes <paramref name="assembly"/> from the list. Documents and nodes already handed
    /// out stay valid.</summary>
    public void Close(LoadedAssembly assembly)
    {
        lock (_gate)
        {
            if (!assembly.HasLoadError && !ForgetKnown(assembly)) return;
            if (!assembly.IsListed) return;
            assembly.IsListed = false;
            ChangeListed(_listed.Remove(assembly));
        }
        Changed?.Invoke();
    }

    private bool ForgetKnown(LoadedAssembly assembly)
    {
        if (!_known.TryGetValue(assembly.FilePath, out var known))
            return false;
        if (known != assembly)
            return false;
        _known.Remove(assembly.FilePath);
        return true;
    }

    private void ChangeListed(ImmutableArray<LoadedAssembly> listed)
    {
        _listed = listed;
        _loaded = listed.RemoveAll(static a => a.HasLoadError);
        _version++;
    }

    /// <summary>Closes every assembly.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            _generation++;
            if (_listed.IsEmpty && _known.Count == 0) return;
            foreach (var assembly in _listed)
                assembly.IsListed = false;
            _known.Clear();
            ChangeListed(ImmutableArray<LoadedAssembly>.Empty);
        }
        Changed?.Invoke();
    }

    /// <summary>Orders the open files by name, and those of one name by version.</summary>
    public void Sort()
    {
        lock (_gate)
        {
            var sorted = _listed
                .OrderBy(a => a.Name, NaturalStringComparer.Instance)
                .ThenBy(a => a.Version)
                .ToImmutableArray();
            if (sorted.SequenceEqual(_listed))
                return;
            ChangeListed(sorted);
        }
        Changed?.Invoke();
    }

    /// <summary>Lists the assemblies <paramref name="assembly"/> references that can be found, as
    /// following a reference to them does, and returns them. Those that cannot be found are left out.
    /// May load files: call it off the UI thread.</summary>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    public List<LoadedAssembly> ListReferences(LoadedAssembly assembly, CancellationToken cancellationToken = default)
    {
        var generation = Generation;
        var listed = new List<LoadedAssembly>();
        foreach (var reference in assembly.File.AssemblyReferences)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var found = assembly.Resolver.ResolveAssembly(reference);
            if (found == null)
                continue;
            if (TryList(found, generation, cancellationToken))
                listed.Add(found);
        }
        return listed;
    }

    /// <summary>
    /// The node <paramref name="reference"/> points at, opening the assembly that defines it if it is
    /// not open yet and its file can be found. For an entity hidden from the tree (compiler-generated)
    /// the nearest visible ancestor is returned. Null if the reference is not navigable or cannot be
    /// resolved. May load files: prefer calling it off the UI thread.
    /// </summary>
    public BrowseNode? FindNode(CodeReference reference)
    {
        try
        {
            var (assembly, handle) = ResolveListed(reference);
            if (assembly is null)
                return null;
            if (handle.Kind == HandleKind.AssemblyDefinition)
                return assembly.Root;
            return assembly.FindNodeOrAncestor(handle);
        }
        catch (Exception e) when (IsUnreadable(e))
        {
            return null;
        }
    }

    /// <summary>
    /// The metadata table row that defines what <paramref name="reference"/> points at: the row of a
    /// type or member, or an assembly's own row, opening and listing the assembly that defines it as
    /// <see cref="FindNode(CodeReference)"/> does. Null if the reference is not navigable or cannot
    /// be resolved. May load files: prefer calling it off the UI thread.
    /// </summary>
    public MetadataTarget? FindMetadataRow(CodeReference reference)
    {
        try
        {
            var (assembly, handle) = ResolveListed(reference);
            if (assembly is null)
                return null;
            var root = (AssemblyNode)assembly.Root;
            return root.FindMetadataRow(handle);
        }
        catch (Exception e) when (IsUnreadable(e))
        {
            return null;
        }
    }

    /// <summary>The listed assembly that defines what <paramref name="reference"/> points at, and the
    /// handle of its definition there: the assembly's own handle for an assembly. A null assembly
    /// when it cannot be resolved or listed.</summary>
    private (LoadedAssembly? Assembly, EntityHandle Handle) ResolveListed(CodeReference reference)
    {
        if (reference.FilePath is null)
            return default;
        var generation = Generation;
        var owner = TryLoad(reference.FilePath, generation);
        if (owner is null)
            return default;
        var (assembly, handle) = ResolveTarget(owner, reference, generation);
        if (assembly is null)
            return default;
        if (!TryList(assembly, generation))
            return default;
        return (assembly, handle);
    }

    private (LoadedAssembly? Assembly, EntityHandle Handle) ResolveTarget(LoadedAssembly owner, CodeReference reference, int generation)
    {
        if (reference.Kind != CodeReferenceKind.Assembly)
            return ResolveDefinition(owner, reference.Handle, generation);
        if (reference.Handle.Kind == HandleKind.AssemblyDefinition)
            return (owner, EntityHandle.AssemblyDefinition);
        var target = owner.Resolver.ResolveAssembly(new AssemblyReference(owner.File, (AssemblyReferenceHandle)reference.Handle));
        return (target, EntityHandle.AssemblyDefinition);
    }

    private static bool IsUnreadable(Exception e) => e is BadImageFormatException or InvalidOperationException or IOException;

    /// <summary>
    /// What hovering <paramref name="reference"/> explains: the declaration and XML documentation of
    /// the type or member it points at. Null for an assembly, a local or a label, or when it cannot
    /// be resolved. Loads the files it needs without listing them, and reads documentation files:
    /// call it off the UI thread.
    /// </summary>
    public CodeTooltip? Describe(CodeReference reference)
    {
        try
        {
            var (entity, assembly) = ResolveEntity(reference);
            if (entity is null)
                return null;
            return CodeTooltip.Create(entity, assembly!);
        }
        catch (Exception e) when (IsUnreadable(e))
        {
            return null;
        }
    }

    /// <summary>
    /// The page of the .NET API reference about the type or member <paramref name="reference"/>
    /// points at, as <see cref="MicrosoftDocs.FindPage(BrowseNode)"/> names it. Null for an assembly,
    /// a local or a label, for what is not visible outside its assembly, or when it cannot be
    /// resolved. Loads the files it needs without listing them: call it off the UI thread.
    /// </summary>
    public string? FindApiReferencePage(CodeReference reference)
    {
        try
        {
            var (entity, _) = ResolveEntity(reference);
            if (entity is null)
                return null;
            return MicrosoftDocs.FindPage(entity);
        }
        catch (Exception e) when (IsUnreadable(e))
        {
            return null;
        }
    }

    /// <summary>The type or member <paramref name="reference"/> points at and the assembly that defines
    /// it, loading that assembly without listing it; nulls for an assembly, a local or a label, or
    /// when it cannot be resolved.</summary>
    private (IEntity? Entity, LoadedAssembly? Assembly) ResolveEntity(CodeReference reference)
    {
        if (reference.FilePath is null)
            return default;
        if (reference.Kind == CodeReferenceKind.Assembly)
            return default;
        var generation = Generation;
        var owner = TryLoad(reference.FilePath, generation);
        if (owner is null)
            return default;
        var (assembly, handle) = ResolveDefinition(owner, reference.Handle, generation);
        if (assembly is null)
            return default;
        var entity = assembly.Module.ResolveEntity(handle);
        return (entity, assembly);
    }

    /// <summary>
    /// The node <paramref name="path"/> names, opening its assembly if it is not open and its file
    /// still exists, or the root of a file listed as failed. Null if the file cannot be opened or no
    /// longer has that node. May load files and build children: prefer calling it off the UI thread.
    /// </summary>
    public BrowseNode? FindNode(NodePath path)
    {
        var generation = Generation;
        try
        {
            var assembly = TryLoad(path.FilePath, generation) ?? FindListedFailure(path.FilePath);
            return assembly is not null && TryList(assembly, generation) ? path.Resolve(assembly.Root) : null;
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
        AssemblySearch.Run(_loaded, query, cancellationToken);

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

    internal LoadedAssembly? TryLoad(string path) => TryLoad(path, Generation);

    internal LoadedAssembly? TryLoad(string path, int generation)
    {
        try
        {
            return Load(path, generation);
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

    /// <summary>The assembly at <paramref name="path"/>, read from the file unless it is loaded. One read
    /// for a <paramref name="generation"/> that a clear has ended is not kept.</summary>
    private LoadedAssembly Load(string path, int generation)
    {
        lock (_gate)
        {
            if (_known.TryGetValue(path, out var existing)) return existing;
        }

        var assembly = new LoadedAssembly(this, ReadFile(path));
        lock (_gate)
        {
            if (generation != _generation) return assembly;
            if (_known.TryGetValue(path, out var existing)) return existing;
            _known.Add(path, assembly);
        }
        return assembly;
    }

    internal bool TryList(LoadedAssembly assembly) => TryList(assembly, Generation);

    /// <summary>Lists <paramref name="assembly"/>, unless the list was cleared since
    /// <paramref name="generation"/>, <paramref name="cancellationToken"/> was cancelled, or another
    /// assembly took its path.</summary>
    internal bool TryList(LoadedAssembly assembly, int generation, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (generation != _generation) return false;
            if (cancellationToken.IsCancellationRequested) return false;
            if (!HoldsPathFor(assembly)) return false;
            if (assembly.IsListed) return true;
            assembly.IsListed = true;
            ChangeListed(ListInPlaceOfFailure(assembly));
        }
        if (!assembly.HasLoadError)
            ThreadPool.QueueUserWorkItem(static a => a.WarmUp(), assembly, preferLocal: false);
        Changed?.Invoke();
        return true;
    }

    /// <summary>Whether <paramref name="assembly"/> is what the list holds for its path: the assembly
    /// read from it, or for a failure, the one listed for it while nothing was read from it.</summary>
    private bool HoldsPathFor(LoadedAssembly assembly)
    {
        if (!assembly.HasLoadError)
            return _known.TryGetValue(assembly.FilePath, out var known) && known == assembly;
        if (_known.ContainsKey(assembly.FilePath))
            return false;
        var listed = FindListedFailure(assembly.FilePath);
        return listed == null || listed == assembly;
    }

    /// <summary>The list with <paramref name="assembly"/> where the failure listed for its path was,
    /// or last when there is none.</summary>
    private ImmutableArray<LoadedAssembly> ListInPlaceOfFailure(LoadedAssembly assembly)
    {
        var failure = FindListedFailure(assembly.FilePath);
        if (failure == null)
            return _listed.Add(assembly);
        failure.IsListed = false;
        return _listed.Replace(failure, assembly);
    }

    private LoadedAssembly? FindListedFailure(string path)
    {
        var listed = _listed;
        foreach (var assembly in listed)
        {
            if (!assembly.HasLoadError)
                continue;
            if (string.Equals(assembly.FilePath, path, StringComparison.OrdinalIgnoreCase))
                return assembly;
        }
        return null;
    }

    private (LoadedAssembly? Assembly, EntityHandle Handle) ResolveDefinition(LoadedAssembly owner, EntityHandle handle, int generation)
    {
        switch (handle.Kind)
        {
            case HandleKind.TypeDefinition or HandleKind.MethodDefinition or HandleKind.FieldDefinition
                or HandleKind.PropertyDefinition or HandleKind.EventDefinition:
                return (owner, handle);
        }

        return Definitions.Resolve(owner.Module, handle) is { } definition
            ? (TryLoad(definition.ParentModule!.MetadataFile!.FileName, generation), definition.MetadataToken)
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
