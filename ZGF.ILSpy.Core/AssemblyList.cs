using System.Diagnostics;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ZGF.ILSpy.Core.Metadata;
using AssemblyReference = ICSharpCode.Decompiler.Metadata.AssemblyReference;

namespace ZGF.ILSpy.Core;

/// <summary>Thrown when a file cannot be opened as a .NET assembly or module.</summary>
public sealed class AssemblyLoadException(AssemblyPath filePath, string message, Exception? inner = null) : Exception(message, inner)
{
    public AssemblyPath FilePath { get; } = filePath;
}

/// <summary>
/// The assemblies open in the browser, plus those loaded behind the scenes to resolve references.
/// All members are thread-safe: decompilation and search read it from background threads while the
/// UI thread reads <see cref="Assemblies"/>, which is an immutable snapshot.
/// </summary>
public sealed class AssemblyList
{
    private readonly object _gate = new();
    /// <summary>What was read, by path: assemblies, packages and the assemblies read out of packages.</summary>
    private readonly Dictionary<AssemblyPath, LoadedAssembly> _known = new();
    private ImmutableArray<LoadedAssembly> _listed = ImmutableArray<LoadedAssembly>.Empty;
    private ImmutableArray<LoadedModule> _loaded = ImmutableArray<LoadedModule>.Empty;
    private int _version;
    private int _generation;
    private volatile bool _loadsReferences = true;
    private volatile MetadataReaderOptions _readerOptions = MetadataReaderOptions.ApplyWindowsRuntimeProjections;

    /// <summary>The open assemblies that loaded, in the order they were opened, with an open package's
    /// assemblies where the package is. A snapshot: it never changes after it is read.</summary>
    public IReadOnlyList<LoadedModule> Assemblies => _loaded;

    /// <summary>The open files in the order they were opened, packages and those that failed to load
    /// (<see cref="FailedAssembly"/>) among them. A snapshot, like <see cref="Assemblies"/>.</summary>
    public IReadOnlyList<LoadedAssembly> Listed => _listed;

    /// <summary>The open files' paths, in order, failed ones included, for persisting the list.</summary>
    public IReadOnlyList<AssemblyPath> FilePaths => _listed.Select(a => a.FilePath).ToArray();

    /// <summary>Increments whenever <see cref="Assemblies"/> changes.</summary>
    public int Version => Volatile.Read(ref _version);

    /// <summary>Increments whenever the list is cleared. Work started before a clear neither lists nor
    /// keeps the assemblies it loads after it, so that what it finds late never lands in the list
    /// that follows.</summary>
    public int Generation => Volatile.Read(ref _generation);

    /// <summary>The portable PDBs picked for assemblies, which they read in place of the ones they find
    /// themselves.</summary>
    public AssemblyPicks Pdbs { get; } = new();

    /// <summary>The frameworks picked for assemblies, by id such as <c>.NETCoreApp,Version=v8.0</c>,
    /// which their references are resolved against in place of the one they were built for.</summary>
    public AssemblyPicks TargetFrameworks { get; } = new();

    /// <summary>Raised after <see cref="Assemblies"/> changes, on the thread that changed it, including
    /// background threads when navigation opens a referenced assembly.</summary>
    public event Action? Changed;

    /// <summary>Opens the assembly at <paramref name="path"/>, or returns it if already open. Its type
    /// system starts loading in the background. A file that is missing or not a .NET assembly is listed
    /// all the same, as a <see cref="FailedAssembly"/>, and the assembly takes its place
    /// once the file loads. Once <paramref name="cancellationToken"/> is cancelled
    /// the assembly is no longer listed, so cancelling before <see cref="Clear"/> keeps the opens of
    /// several files out of the list that follows.</summary>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled,
    /// or the list was cleared while the file was read.</exception>
    public LoadedAssembly Open(AssemblyPath path, CancellationToken cancellationToken = default)
    {
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
    private LoadedAssembly LoadOrFail(AssemblyPath path, int generation)
    {
        try
        {
            return Load(path, generation);
        }
        catch (AssemblyLoadException e)
        {
            return FindListedFailure(path) ?? new FailedAssembly(this, e);
        }
    }

    /// <summary>Removes the item of the list holding <paramref name="assembly"/>: the assembly, or the
    /// package it was read out of, whose assemblies are forgotten with it. Documents and nodes already
    /// handed out stay valid.</summary>
    public void Close(LoadedAssembly assembly)
    {
        var item = assembly.ListedItem;
        lock (_gate)
        {
            if (item is not FailedAssembly && !ForgetKnown(item)) return;
            Pdbs.ForgetFor(item);
            TargetFrameworks.ForgetFor(item);
            ForgetReadOutOf(item);
            if (!item.IsListed) return;
            item.IsListed = false;
            ChangeListed(_listed.Remove(item));
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

    private void ForgetReadOutOf(LoadedAssembly package)
    {
        var read = _known
            .Where(k => k.Value is LoadedModule module && module.Place != null && module.Place.Container == package)
            .Select(k => k.Key)
            .ToList();
        foreach (var path in read)
            _known.Remove(path);
    }

    private void ChangeListed(ImmutableArray<LoadedAssembly> listed)
    {
        _listed = listed;
        _loaded = ListLoaded(listed);
        _version++;
    }

    private static ImmutableArray<LoadedModule> ListLoaded(ImmutableArray<LoadedAssembly> listed)
    {
        var loaded = ImmutableArray.CreateBuilder<LoadedModule>();
        foreach (var item in listed)
        {
            switch (item)
            {
                case FailedAssembly:
                    break;
                case LoadedPackage package:
                    loaded.AddRange(package.Assemblies);
                    break;
                case LoadedModule module:
                    loaded.Add(module);
                    break;
                default:
                    throw new UnreachableException();
            }
        }
        return loaded.ToImmutable();
    }

    /// <summary>Whether resolving a reference reads its assembly from a file, rather than only finding one listed.</summary>
    internal bool LoadsReferences => _loadsReferences;

    /// <summary>Takes from <paramref name="options"/> the toggles that apply as assemblies are read: whether
    /// references load their assemblies, and whether Windows Runtime metadata is projected. Returns true
    /// when they changed, so that the open assemblies must be read again to follow them.</summary>
    public bool UseLoadOptions(DecompilerOptions options)
    {
        var loadsReferences = options.IsOn(DecompilerToggle.AutoLoadAssemblyReferences);
        var readerOptions = ChooseReaderOptions(options);
        if (loadsReferences == _loadsReferences && readerOptions == _readerOptions)
            return false;
        _loadsReferences = loadsReferences;
        _readerOptions = readerOptions;
        return true;
    }

    private static MetadataReaderOptions ChooseReaderOptions(DecompilerOptions options)
    {
        if (options.IsOn(DecompilerToggle.ApplyWindowsRuntimeProjections))
            return MetadataReaderOptions.ApplyWindowsRuntimeProjections;
        return MetadataReaderOptions.None;
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

    /// <summary>Reads the item of the list holding <paramref name="assembly"/> again from its file: the
    /// assembly, or the package it was read out of with the assemblies in it, each taking what is picked
    /// for it now. It is not listed until <see cref="Replace"/> puts it in the item's place. Reads the
    /// file: call it off the UI thread.</summary>
    /// <exception cref="AssemblyLoadException">The file can no longer be read.</exception>
    public LoadedAssembly ReadAgain(LoadedAssembly assembly)
    {
        var fresh = ReadAssemblyOrPackage(assembly.ListedItem.FilePath);
        if (fresh is LoadedPackage package)
            package.ReadAssemblies();
        return fresh;
    }

    /// <summary>Lists <paramref name="fresh"/>, which <see cref="ReadAgain"/> read for the listed
    /// <paramref name="item"/>, where the item is, and forgets the item and the assemblies read out of
    /// it. Nodes already handed out stay valid. False, leaving the list as it is, when the item is no
    /// longer listed.</summary>
    public bool Replace(LoadedAssembly item, LoadedAssembly fresh)
    {
        lock (_gate)
        {
            if (!item.IsListed) return false;
            if (!ForgetKnown(item)) return false;
            ForgetReadOutOf(item);
            Know(fresh);
            item.IsListed = false;
            fresh.IsListed = true;
            ChangeListed(_listed.Replace(item, fresh));
        }
        WarmUpLater(fresh);
        Changed?.Invoke();
        return true;
    }

    private void Know(LoadedAssembly item)
    {
        _known[item.FilePath] = item;
        if (item is not LoadedPackage package)
            return;
        foreach (var assembly in package.Assemblies)
            _known[assembly.FilePath] = assembly;
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
    public List<LoadedModule> ListReferences(LoadedModule assembly, CancellationToken cancellationToken = default)
    {
        var generation = Generation;
        var listed = new List<LoadedModule>();
        foreach (var reference in assembly.File.AssemblyReferences)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var found = assembly.Resolver.LoadAssembly(reference);
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
    /// <summary>The node of the namespace, type or member <paramref name="idString"/> names, written as
    /// XML documentation ids are, such as <c>T:System.String</c>, in the first of <paramref name="assemblies"/>
    /// that defines it; null when none does. May read the assemblies: call it off the UI thread.</summary>
    public BrowseNode? FindNodeById(string idString, IEnumerable<LoadedModule> assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var node = FindNodeById(idString, assembly);
            if (node != null)
                return node;
        }
        return null;
    }

    private static BrowseNode? FindNodeById(string idString, LoadedModule assembly)
    {
        try
        {
            return assembly.FindNodeById(idString);
        }
        catch (Exception e) when (IsUnreadable(e))
        {
            return null;
        }
    }

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
            return assembly.AssemblyRoot.FindMetadataRow(handle);
        }
        catch (Exception e) when (IsUnreadable(e))
        {
            return null;
        }
    }

    /// <summary>The listed assembly that defines what <paramref name="reference"/> points at, and the
    /// handle of its definition there: the assembly's own handle for an assembly. A null assembly
    /// when it cannot be resolved or listed.</summary>
    private (LoadedModule? Assembly, EntityHandle Handle) ResolveListed(CodeReference reference)
    {
        if (reference.FilePath is null)
            return default;
        var generation = Generation;
        var owner = TryLoadAssembly(reference.FilePath, generation);
        if (owner is null)
            return default;
        var (assembly, handle) = ResolveTarget(owner, reference, generation);
        if (assembly is null)
            return default;
        if (!TryList(assembly, generation))
            return default;
        return (assembly, handle);
    }

    private (LoadedModule? Assembly, EntityHandle Handle) ResolveTarget(LoadedModule owner, CodeReference reference, int generation)
    {
        if (reference.Kind != CodeReferenceKind.Assembly)
            return ResolveDefinition(owner, reference.Handle, generation);
        if (reference.Handle.Kind == HandleKind.AssemblyDefinition)
            return (owner, EntityHandle.AssemblyDefinition);
        var target = owner.Resolver.ResolveAssembly(new AssemblyReference(owner.File, (AssemblyReferenceHandle)reference.Handle));
        return (target, EntityHandle.AssemblyDefinition);
    }

    private static bool IsUnreadable(Exception e) => e is BadImageFormatException or IOException;

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
            if (entity is null || assembly is null)
                return null;
            return CodeTooltip.Create(entity, assembly);
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

    /// <summary>The definition of the type <paramref name="reference"/> points at, loading the assembly
    /// that defines it without listing it; null when it points at no type, or cannot be resolved.
    /// Call it off the UI thread.</summary>
    internal ITypeDefinition? FindTypeDefinition(CodeReference reference)
    {
        if (reference.Kind != CodeReferenceKind.Type)
            return null;
        try
        {
            var (entity, _) = ResolveEntity(reference);
            return entity as ITypeDefinition;
        }
        catch (Exception e) when (IsUnreadable(e))
        {
            return null;
        }
    }

    /// <summary>The definition of the type <paramref name="anchor"/> names, loading the assembly that
    /// defines it without listing it; null when its file cannot be read or no longer defines it. Call it
    /// off the UI thread.</summary>
    internal ITypeDefinition? FindTypeDefinition(TypeAnchor anchor)
    {
        try
        {
            var assembly = TryLoadAssembly(anchor.AssemblyPath);
            if (assembly is null)
                return null;
            return assembly.MainModule.GetTypeDefinition(new FullTypeName(anchor.FullName));
        }
        catch (Exception e) when (IsUnreadable(e))
        {
            return null;
        }
    }

    /// <summary>The type or member <paramref name="reference"/> points at and the assembly that defines
    /// it, loading that assembly without listing it; nulls for an assembly, a local or a label, or
    /// when it cannot be resolved.</summary>
    private (IEntity? Entity, LoadedModule? Assembly) ResolveEntity(CodeReference reference)
    {
        if (reference.FilePath is null)
            return default;
        if (reference.Kind == CodeReferenceKind.Assembly)
            return default;
        var generation = Generation;
        var owner = TryLoadAssembly(reference.FilePath, generation);
        if (owner is null)
            return default;
        var (assembly, handle) = ResolveDefinition(owner, reference.Handle, generation);
        if (assembly is null)
            return default;
        var entity = assembly.MainModule.ResolveEntity(handle);
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
        catch (Exception e) when (IsUnreadable(e))
        {
            return null;
        }
    }

    /// <summary>The node <paramref name="path"/> leads to in an assembly the list holds now, or null when
    /// it holds none at that path or the node is no longer there. Opens and lists nothing, so a lookup
    /// that outlives a switch to another list leaves the new list as it is.</summary>
    public BrowseNode? FindListedNode(NodePath path)
    {
        var assembly = FindListedLoaded(path.FilePath);
        if (assembly == null)
            return null;
        try
        {
            return path.Resolve(assembly.Root);
        }
        catch (Exception e) when (IsUnreadable(e))
        {
            return null;
        }
    }

    private LoadedAssembly? FindListedLoaded(AssemblyPath path)
    {
        var listed = _listed;
        foreach (var assembly in listed)
        {
            if (assembly is FailedAssembly)
                continue;
            if (assembly.FilePath.Equals(path))
                return assembly;
        }
        return null;
    }

    /// <summary>
    /// Searches the open assemblies for what <paramref name="query"/> asks, on a background thread.
    /// Results arrive in batches as they are found.
    /// </summary>
    public IAsyncEnumerable<IReadOnlyList<SearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default) =>
        AssemblySearch.Run(_loaded, query, cancellationToken);

    /// <summary><see cref="SearchAsync(SearchQuery, CancellationToken)"/> over
    /// <paramref name="assemblies"/> only, e.g. those opened since the last search.</summary>
    public IAsyncEnumerable<IReadOnlyList<SearchResult>> SearchAsync(SearchQuery query, IEnumerable<LoadedModule> assemblies,
        CancellationToken cancellationToken = default) =>
        AssemblySearch.Run([..assemblies], query, cancellationToken);

    /// <summary><see cref="SearchAsync(SearchQuery, CancellationToken)"/> for
    /// <paramref name="text"/> parsed with <see cref="SearchQuery.Parse"/>.</summary>
    public IAsyncEnumerable<IReadOnlyList<SearchResult>> SearchAsync(string text, SearchKinds kinds = SearchKinds.TypesAndMembers,
        CancellationToken cancellationToken = default) =>
        SearchAsync(SearchQuery.Parse(text, kinds), cancellationToken);

    internal LoadedAssembly? TryLoad(AssemblyPath path) => TryLoad(path, Generation);

    /// <summary>The assembly at <paramref name="path"/>, as <see cref="TryLoad(AssemblyPath, int)"/> loads it;
    /// null for a package, which has no metadata of its own, or a file that failed to load.</summary>
    internal LoadedModule? TryLoadAssembly(AssemblyPath path) => TryLoadAssembly(path, Generation);

    private LoadedModule? TryLoadAssembly(AssemblyPath path, int generation) =>
        TryLoad(path, generation) is LoadedModule module ? module : null;

    internal LoadedAssembly? TryLoad(AssemblyPath path, int generation)
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
    internal DecompilerTypeSystem TypeSystemOf(MetadataFile module) => LoadedFrom(module).TypeSystem;

    private LoadedModule LoadedFrom(MetadataFile module)
    {
        lock (_gate)
        {
            foreach (var assembly in _known.Values)
            {
                if (assembly is LoadedModule loaded && loaded.File == module)
                    return loaded;
            }
        }
        throw new InvalidOperationException($"{module.FileName} was not loaded by this list.");
    }

    /// <summary>The newest assembly read of <paramref name="reference"/>'s name, only one as new as it asks
    /// for when <paramref name="exactOrNewer"/>, and only one listed, or read out of a listed package, when
    /// <paramref name="listedOnly"/>; null when there is none.</summary>
    internal LoadedModule? FindKnown(IAssemblyReference reference, bool exactOrNewer, bool listedOnly = false)
    {
        lock (_gate)
        {
            LoadedModule? best = null;
            foreach (var known in _known.Values)
            {
                if (known is not LoadedModule assembly) continue;
                if (listedOnly && !assembly.ListedItem.IsListed) continue;
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
    private LoadedAssembly Load(AssemblyPath path, int generation)
    {
        lock (_gate)
        {
            if (_known.TryGetValue(path, out var existing)) return existing;
        }

        var assembly = Read(path, generation);
        lock (_gate)
        {
            if (generation != _generation) return assembly;
            if (_known.TryGetValue(path, out var existing)) return existing;
            _known.Add(path, assembly);
        }
        return assembly;
    }

    internal bool TryList(LoadedAssembly assembly) => TryList(assembly, Generation);

    /// <summary>Lists <paramref name="assembly"/>, or for one read out of a package the package, unless
    /// the list was cleared since <paramref name="generation"/>, <paramref name="cancellationToken"/>
    /// was cancelled, or another assembly took its path. A package's assemblies are read before it is
    /// listed, so that listing never reads files while holding the lock.</summary>
    internal bool TryList(LoadedAssembly assembly, int generation, CancellationToken cancellationToken = default)
    {
        if (assembly is LoadedModule module && module.Place != null)
            return TryList(module.Place.Container, generation, cancellationToken);
        if (assembly is LoadedPackage package)
            package.ReadAssemblies();
        lock (_gate)
        {
            if (generation != _generation) return false;
            if (cancellationToken.IsCancellationRequested) return false;
            if (!HoldsPathFor(assembly)) return false;
            if (assembly.IsListed) return true;
            assembly.IsListed = true;
            ChangeListed(ListInPlaceOfFailure(assembly));
        }
        WarmUpLater(assembly);
        Changed?.Invoke();
        return true;
    }

    /// <summary>Loads a listed assembly's type system in the background, so that it is ready when
    /// first shown.</summary>
    private static void WarmUpLater(LoadedAssembly assembly)
    {
        if (assembly is not LoadedModule module)
            return;
        ThreadPool.QueueUserWorkItem(static m => m.WarmUp(), module, preferLocal: false);
    }

    /// <summary>The assembly <paramref name="package"/> holds as <paramref name="file"/>: the one read
    /// before, else one read now, kept while the list holds the package. Null when it does not load.</summary>
    internal LoadedModule? TryLoadOutOf(LoadedPackage package, PackageFile file)
    {
        var path = package.Package.PathOf(file);
        lock (_gate)
        {
            var known = FindReadOutOf(package, path);
            if (known != null) return known;
        }
        var read = TryReadOutOfPackage(package, path, file.FullName);
        if (read == null)
            return null;
        lock (_gate)
        {
            if (!IsKnown(package)) return read;
            var known = FindReadOutOf(package, path);
            if (known != null) return known;
            _known[path] = read;
        }
        return read;
    }

    private LoadedModule? FindReadOutOf(LoadedPackage package, AssemblyPath path)
    {
        if (!_known.TryGetValue(path, out var known))
            return null;
        if (known is not LoadedModule module || module.Place == null)
            return null;
        if (module.Place.Container != package)
            return null;
        return module;
    }

    private LoadedModule? TryReadOutOfPackage(LoadedPackage package, AssemblyPath path, string fullName)
    {
        try
        {
            return ReadOutOfPackage(package, path, fullName);
        }
        catch (AssemblyLoadException)
        {
            return null;
        }
    }

    private bool IsKnown(LoadedAssembly assembly)
    {
        if (!_known.TryGetValue(assembly.FilePath, out var known))
            return false;
        return known == assembly;
    }

    /// <summary>Whether <paramref name="assembly"/> is what the list holds for its path: the assembly
    /// read from it, or for a failure, the one listed for it while nothing was read from it.</summary>
    private bool HoldsPathFor(LoadedAssembly assembly)
    {
        if (assembly is not FailedAssembly)
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

    private FailedAssembly? FindListedFailure(AssemblyPath path)
    {
        var listed = _listed;
        foreach (var assembly in listed)
        {
            if (assembly is not FailedAssembly failed)
                continue;
            if (failed.FilePath.Equals(path))
                return failed;
        }
        return null;
    }

    private (LoadedModule? Assembly, EntityHandle Handle) ResolveDefinition(LoadedModule owner, EntityHandle handle, int generation)
    {
        switch (handle.Kind)
        {
            case HandleKind.TypeDefinition or HandleKind.MethodDefinition or HandleKind.FieldDefinition
                or HandleKind.PropertyDefinition or HandleKind.EventDefinition:
                return (owner, handle);
        }

        var definition = Definitions.Resolve(owner.MainModule, handle);
        if (definition == null)
            return (null, default);
        return (TryLoadAssembly(AssemblyPath.Of(definition.ParentModule!.MetadataFile!), generation), definition.MetadataToken);
    }

    /// <summary>What <paramref name="path"/> holds: an assembly, a package, or an assembly in a
    /// package, read from a path <see cref="AssemblyPackage.PathOf"/> made.</summary>
    /// <exception cref="AssemblyLoadException">There is no such file, or it is none of those.</exception>
    private LoadedAssembly Read(AssemblyPath path, int generation)
    {
        if (File.Exists(path.Text))
            return ReadAssemblyOrPackage(path);
        var inPackage = AssemblyPackage.SplitPath(path);
        if (inPackage == null)
            return new LoadedModule(this, path, ReadFile(path));
        var container = Load(inPackage.Value.Package, generation);
        if (container is not LoadedPackage package)
            throw new AssemblyLoadException(path, $"'{container.FilePath}' is not a package.");
        return ReadOutOfPackage(package, path, inPackage.Value.FullName);
    }

    /// <summary>The assembly at <paramref name="path"/>, else the WebCIL module, else the package, else
    /// why it is none of those.</summary>
    private LoadedAssembly ReadAssemblyOrPackage(AssemblyPath path)
    {
        try
        {
            return new LoadedModule(this, path, ReadFile(path));
        }
        catch (AssemblyLoadException)
        {
            var read = ReadWebCilOrPackage(path);
            if (read == null)
                throw;
            return read;
        }
    }

    private LoadedAssembly? ReadWebCilOrPackage(AssemblyPath path)
    {
        var module = ReadWebCil(path);
        if (module != null)
            return new LoadedModule(this, path, module);
        var package = ReadPackage(path);
        if (package == null)
            return null;
        return new LoadedPackage(this, package);
    }

    /// <summary>The assembly <paramref name="path"/> holds as WebCIL, wrapped in WebAssembly the way
    /// Blazor ships it; null when the file is no WebCIL.</summary>
    private WebCilFile? ReadWebCil(AssemblyPath path)
    {
        try
        {
            return WebCilFile.FromFile(path.Text, _readerOptions);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or BadImageFormatException or InvalidOperationException)
        {
            throw new AssemblyLoadException(path, $"Cannot open '{path}': {e.Message}", e);
        }
    }

    private static AssemblyPackage? ReadPackage(AssemblyPath path)
    {
        try
        {
            return AssemblyPackage.TryRead(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new AssemblyLoadException(path, $"Cannot open '{path}': {e.Message}", e);
        }
    }

    private LoadedModule ReadOutOfPackage(LoadedPackage container, AssemblyPath path, string fullName)
    {
        var package = container.Package;
        var file = package.FindFile(fullName);
        if (file == null)
            throw new AssemblyLoadException(path, $"'{container.FilePath}' holds no '{fullName}'.");
        var image = ReadPackageFile(package, file);
        return new LoadedModule(this, path, image, new PackagePlace(container, file));
    }

    private PEFile ReadPackageFile(AssemblyPackage package, PackageFile file)
    {
        var path = package.PathOf(file);
        try
        {
            var bytes = package.ReadFile(file);
            using var stream = new MemoryStream(bytes);
            return ReadImage(path, stream);
        }
        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            throw new AssemblyLoadException(path, $"Cannot open '{path}': {e.Message}", e);
        }
    }

    private PEFile ReadFile(AssemblyPath path)
    {
        try
        {
            using var stream = new FileStream(path.Text, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return ReadImage(path, stream);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new AssemblyLoadException(path, $"Cannot open '{path}': {e.Message}", e);
        }
    }

    /// <summary>The assembly or module <paramref name="stream"/> holds, known by <paramref name="path"/>.</summary>
    private PEFile ReadImage(AssemblyPath path, Stream stream)
    {
        try
        {
            var file = new PEFile(path.Text, stream, PEStreamOptions.PrefetchEntireImage, _readerOptions);
            if (!file.Reader.HasMetadata)
                throw new AssemblyLoadException(path, $"'{path}' is not a .NET assembly.");
            return file;
        }
        catch (Exception e) when (e is BadImageFormatException or InvalidOperationException or MetadataFileNotSupportedException)
        {
            throw new AssemblyLoadException(path, $"Cannot open '{path}': {e.Message}", e);
        }
    }
}
