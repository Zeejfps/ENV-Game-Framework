using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>A named assembly list as the settings keep it: its name, its assemblies' file paths, in
/// order, the symbols analyzed in it, by assembly path the PDBs picked for its assemblies and the
/// frameworks picked to resolve their references against, and the documents it had open.</summary>
public sealed record AssemblyListRecord(string Name, List<string> Assemblies, List<CodeAnchor>? Analyzed = null,
    Dictionary<string, string>? Pdbs = null, Dictionary<string, string>? Frameworks = null, DocumentsRecord? Documents = null);

/// <summary>
/// The user's named assembly lists and which one is open. Names are never empty and never the same
/// twice, ignoring case, and there is always a list open: deleting the last one leaves an empty one
/// named <see cref="DefaultName"/>.
/// </summary>
public sealed class SavedAssemblyLists
{
    public const string DefaultName = "Default";

    private readonly List<SavedList> _lists = new();
    private SavedList _active;

    public SavedAssemblyLists()
    {
        _active = CreateEmpty(DefaultName);
        _lists.Add(_active);
    }

    /// <summary>Changes whenever a list is added, renamed, removed or opened, or its assemblies or analyses
    /// change; not when its documents do, which the open tabs count.</summary>
    public int Revision { get; private set; }

    /// <summary>The name of the list that is open.</summary>
    public string Active => _active.Name;

    /// <summary>The file paths of the open list's assemblies.</summary>
    public IReadOnlyList<string> ActivePaths => _active.Paths;

    /// <summary>The symbols analyzed in the open list.</summary>
    public IReadOnlyList<CodeAnchor> ActiveAnalyses => _active.Analyzed;

    /// <summary>The PDBs picked for the open list's assemblies, by assembly path.</summary>
    public IReadOnlyDictionary<string, string> ActivePdbs => _active.Pdbs;

    /// <summary>The frameworks picked for the open list's assemblies, by assembly path.</summary>
    public IReadOnlyDictionary<string, string> ActiveFrameworks => _active.Frameworks;

    /// <summary>The documents the open list had open when they were last kept with it, or null for none.</summary>
    public DocumentsRecord? ActiveDocuments => _active.Documents;

    public IReadOnlyList<string> Names => _lists.ConvertAll(static l => l.Name);

    public int Count => _lists.Count;

    /// <summary>Takes the lists the settings kept, dropping nameless ones and repeated names, and
    /// opens the one named <paramref name="active"/>, else the first.</summary>
    public void Load(List<AssemblyListRecord> records, string? active)
    {
        _lists.Clear();
        foreach (var record in records)
            LoadRecord(record);
        if (_lists.Count == 0)
            _lists.Add(CreateEmpty(DefaultName));
        _active = Find(active) ?? _lists[0];
        Revision++;
    }

    private void LoadRecord(AssemblyListRecord record)
    {
        var name = record.Name.Trim();
        if (name.Length == 0)
            return;
        if (Find(name) != null)
            return;
        var analyzed = record.Analyzed ?? new List<CodeAnchor>();
        var list = new SavedList(name, record.Assemblies.ToList(), analyzed.ToList());
        CopyPicks(record.Pdbs, list.Pdbs);
        CopyPicks(record.Frameworks, list.Frameworks);
        list.Documents = record.Documents;
        _lists.Add(list);
    }

    private static SavedList CreateEmpty(string name) => new(name, new List<string>(), new List<CodeAnchor>());

    private static void CopyPicks(IReadOnlyDictionary<string, string>? picks, Dictionary<string, string> into)
    {
        if (picks == null)
            return;
        foreach (var (assembly, pick) in picks)
            into[assembly] = pick;
    }

    public List<AssemblyListRecord> Save()
    {
        return _lists
            .Select(static l => new AssemblyListRecord(l.Name, l.Paths.ToList(), l.Analyzed.ToList(), SavePicks(l.Pdbs), SavePicks(l.Frameworks), l.Documents))
            .ToList();
    }

    /// <summary>The panels the documents kept with every list were docked as.</summary>
    public List<string> ListDocumentPanels()
    {
        return _lists
            .Select(static l => l.Documents)
            .OfType<DocumentsRecord>()
            .SelectMany(static d => d.Tabs)
            .Select(static t => t.Panel)
            .ToList();
    }

    /// <summary>A list's picks as the settings keep them; null for none, which leaves them out.</summary>
    private static Dictionary<string, string>? SavePicks(Dictionary<string, string> picks)
    {
        if (picks.Count == 0)
            return null;
        return new Dictionary<string, string>(picks);
    }

    public bool Contains(string name) => Find(name) != null;

    /// <summary>The file paths of the assemblies in the list named <paramref name="name"/>; none when there is no such list.</summary>
    public IReadOnlyList<string> PathsOf(string name)
    {
        var list = Find(name);
        if (list == null)
            return [];
        return list.Paths;
    }

    /// <summary>Opens the list named <paramref name="name"/>. False when it is the open one already or there is none.</summary>
    public bool Activate(string name)
    {
        var list = Find(name);
        if (list == null)
            return false;
        if (list == _active)
            return false;
        _active = list;
        Revision++;
        return true;
    }

    /// <summary>Records <paramref name="paths"/> as the open list's assemblies.</summary>
    public void RememberActive(IReadOnlyList<string> paths)
    {
        if (_active.Paths.SequenceEqual(paths))
            return;
        _active.Paths.Clear();
        _active.Paths.AddRange(paths);
        Revision++;
    }

    /// <summary>Records <paramref name="analyzed"/> as the symbols analyzed in the open list.</summary>
    public void RememberActiveAnalyses(IReadOnlyList<CodeAnchor> analyzed)
    {
        if (_active.Analyzed.SequenceEqual(analyzed))
            return;
        _active.Analyzed.Clear();
        _active.Analyzed.AddRange(analyzed);
        Revision++;
    }

    /// <summary>Keeps <paramref name="documents"/>, or none when null, as the documents the open list has open.</summary>
    public void RememberActiveDocuments(DocumentsRecord? documents)
    {
        _active.Documents = documents;
    }

    /// <summary>Records <paramref name="pdbs"/> as the PDBs picked for the open list's assemblies.</summary>
    public void RememberActivePdbs(IReadOnlyDictionary<string, string> pdbs)
    {
        RememberPicks(_active.Pdbs, pdbs);
    }

    /// <summary>Records <paramref name="frameworks"/> as the frameworks picked for the open list's assemblies.</summary>
    public void RememberActiveFrameworks(IReadOnlyDictionary<string, string> frameworks)
    {
        RememberPicks(_active.Frameworks, frameworks);
    }

    private void RememberPicks(Dictionary<string, string> kept, IReadOnlyDictionary<string, string> picks)
    {
        if (HasPicks(kept, picks))
            return;
        kept.Clear();
        CopyPicks(picks, kept);
        Revision++;
    }

    private static bool HasPicks(Dictionary<string, string> kept, IReadOnlyDictionary<string, string> picks)
    {
        if (kept.Count != picks.Count)
            return false;
        return picks.All(p => kept.GetValueOrDefault(p.Key) == p.Value);
    }

    /// <summary>Adds an empty list named <paramref name="name"/>, numbered when that name is taken,
    /// and returns the name it got.</summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is blank.</exception>
    public string Create(string name)
    {
        return Add(new SavedList(name, new List<string>(), new List<CodeAnchor>()));
    }

    /// <summary>Adds a list of the assemblies at <paramref name="paths"/>, named <paramref name="name"/>
    /// or numbered when that is taken, and returns the name it got.</summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is blank.</exception>
    public string Create(string name, IReadOnlyList<string> paths)
    {
        return Add(new SavedList(name, paths.ToList(), new List<CodeAnchor>()));
    }

    /// <summary>Adds a list with the assemblies, analyses and picks of the one named
    /// <paramref name="source"/>, named <paramref name="name"/> or numbered when that is taken, and
    /// returns the name it got.</summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is blank.</exception>
    public string Duplicate(string source, string name)
    {
        var copy = new SavedList(name, PathsOf(source).ToList(), AnalysesOf(source).ToList());
        var original = Find(source);
        if (original != null)
            CopyPicksOf(original, copy);
        return Add(copy);
    }

    private static void CopyPicksOf(SavedList original, SavedList copy)
    {
        CopyPicks(original.Pdbs, copy.Pdbs);
        CopyPicks(original.Frameworks, copy.Frameworks);
    }

    private IReadOnlyList<CodeAnchor> AnalysesOf(string name)
    {
        var list = Find(name);
        if (list == null)
            return [];
        return list.Analyzed;
    }

    /// <summary>Adds <paramref name="list"/>, numbered when its name is taken, and returns the name it got.</summary>
    private string Add(SavedList list)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(list.Name);
        list.Name = MakeUnique(list.Name.Trim());
        _lists.Add(list);
        Revision++;
        return list.Name;
    }

    private string MakeUnique(string name)
    {
        if (Find(name) == null)
            return name;
        var number = 2;
        while (Find($"{name} {number}") != null)
            number++;
        return $"{name} {number}";
    }

    /// <summary>Whether the list named <paramref name="name"/> may be renamed to <paramref name="newName"/>:
    /// it is not empty and no other list has it.</summary>
    public bool CanRename(string name, string newName)
    {
        var trimmed = newName.Trim();
        if (trimmed.Length == 0)
            return false;
        var holder = Find(trimmed);
        if (holder == null)
            return true;
        return holder == Find(name);
    }

    /// <summary>Renames the list named <paramref name="name"/>, unless <see cref="CanRename"/> says no.</summary>
    public bool Rename(string name, string newName)
    {
        if (!CanRename(name, newName))
            return false;
        var list = Find(name);
        if (list == null)
            return false;
        list.Name = newName.Trim();
        Revision++;
        return true;
    }

    /// <summary>Removes the list named <paramref name="name"/>. When it was the open one, the list
    /// that took its place opens, or the one before it when it was last.</summary>
    public void Delete(string name)
    {
        var list = Find(name);
        if (list == null)
            return;
        var replacement = ReplacementFor(list.Name);
        _lists.Remove(list);
        if (_lists.Count == 0)
            _lists.Add(CreateEmpty(DefaultName));
        if (list == _active)
            _active = Find(replacement)!;
        Revision++;
    }

    /// <summary>The name of the list that opens when the open list named <paramref name="name"/> is
    /// deleted: the one after it, else the one before, else a new empty <see cref="DefaultName"/>.</summary>
    public string ReplacementFor(string name)
    {
        var list = Find(name);
        if (list == null)
            return Active;
        var index = _lists.IndexOf(list);
        if (index + 1 < _lists.Count)
            return _lists[index + 1].Name;
        if (index > 0)
            return _lists[index - 1].Name;
        return DefaultName;
    }

    private SavedList? Find(string? name)
    {
        if (name == null)
            return null;
        return _lists.Find(l => string.Equals(l.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>A list: its name, its assemblies' paths, the symbols analyzed in it, what was picked for
    /// its assemblies, by assembly path, and the documents it had open.</summary>
    private sealed class SavedList(string name, List<string> paths, List<CodeAnchor> analyzed)
    {
        public string Name = name;
        public DocumentsRecord? Documents;
        public List<string> Paths { get; } = paths;
        public List<CodeAnchor> Analyzed { get; } = analyzed;
        public Dictionary<string, string> Pdbs { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> Frameworks { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
