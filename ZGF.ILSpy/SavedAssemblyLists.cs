using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>A named assembly list as the settings keep it: its name, its assemblies' file paths, in
/// order, and the symbols analyzed in it.</summary>
public sealed record AssemblyListRecord(string Name, List<string> Assemblies, List<CodeAnchor>? Analyzed = null);

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

    /// <summary>Changes whenever a list is added, renamed, removed or opened, or its assemblies or analyses change.</summary>
    public int Revision { get; private set; }

    /// <summary>The name of the list that is open.</summary>
    public string Active => _active.Name;

    /// <summary>The file paths of the open list's assemblies.</summary>
    public IReadOnlyList<string> ActivePaths => _active.Paths;

    /// <summary>The symbols analyzed in the open list.</summary>
    public IReadOnlyList<CodeAnchor> ActiveAnalyses => _active.Analyzed;

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
        _lists.Add(new SavedList(name, record.Assemblies.ToList(), analyzed.ToList()));
    }

    private static SavedList CreateEmpty(string name) => new(name, new List<string>(), new List<CodeAnchor>());

    public List<AssemblyListRecord> Save()
    {
        return _lists
            .Select(static l => new AssemblyListRecord(l.Name, l.Paths.ToList(), l.Analyzed.ToList()))
            .ToList();
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

    /// <summary>Adds an empty list named <paramref name="name"/>, numbered when that name is taken,
    /// and returns the name it got.</summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is blank.</exception>
    public string Create(string name)
    {
        return Add(name, new List<string>(), new List<CodeAnchor>());
    }

    /// <summary>Adds a list of the assemblies at <paramref name="paths"/>, named <paramref name="name"/>
    /// or numbered when that is taken, and returns the name it got.</summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is blank.</exception>
    public string Create(string name, IReadOnlyList<string> paths)
    {
        return Add(name, paths.ToList(), new List<CodeAnchor>());
    }

    /// <summary>Adds a list with the assemblies and analyses of the one named <paramref name="source"/>,
    /// named <paramref name="name"/> or numbered when that is taken, and returns the name it got.</summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is blank.</exception>
    public string Duplicate(string source, string name)
    {
        var paths = PathsOf(source).ToList();
        var analyzed = AnalysesOf(source).ToList();
        return Add(name, paths, analyzed);
    }

    private IReadOnlyList<CodeAnchor> AnalysesOf(string name)
    {
        var list = Find(name);
        if (list == null)
            return [];
        return list.Analyzed;
    }

    private string Add(string name, List<string> paths, List<CodeAnchor> analyzed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var unique = MakeUnique(name.Trim());
        _lists.Add(new SavedList(unique, paths, analyzed));
        Revision++;
        return unique;
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

    private sealed class SavedList(string name, List<string> paths, List<CodeAnchor> analyzed)
    {
        public string Name = name;
        public List<string> Paths { get; } = paths;
        public List<CodeAnchor> Analyzed { get; } = analyzed;
    }
}
