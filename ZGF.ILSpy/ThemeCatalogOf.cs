using System.Globalization;

namespace ZGF.ILSpy;

/// <summary>The themes of one kind, color or icon, by id: the built-in ones, then the user's. Each is
/// resolved over the themes it extends once, when the catalog loads; one that cannot be resolved falls
/// back to its parent, else to the built-in theme <see cref="FallbackIdOf"/> names. Keeps what was wrong
/// in each file.</summary>
public abstract class ThemeCatalog<TFile, TTheme>
    where TFile : class, IThemeFile
    where TTheme : class
{
    private const string ChainArrow = " → ";

    private readonly IReadOnlyList<TFile> _builtIns;
    private readonly Dictionary<string, TFile> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TTheme> _themes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _shownIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _resolving = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _ids = new();
    private readonly List<ThemeError> _errors = new();
    private CultureInfo? _namesCulture;
    private string[] _names = [];

    /// <param name="builtIns">The built-in themes in the order a menu lists them, which must include
    /// <see cref="DefaultId"/>.</param>
    protected ThemeCatalog(IReadOnlyList<TFile> builtIns)
    {
        _builtIns = builtIns;
    }

    /// <summary>The built-in theme an id no theme has shows.</summary>
    protected abstract string DefaultId { get; }

    /// <summary>Every theme's id, built-in ones first, in the order a menu lists them.</summary>
    public IReadOnlyList<string> Ids => _ids;

    /// <summary>What was wrong in the theme files the last load read, each told once.</summary>
    public IReadOnlyList<ThemeError> Errors => _errors;

    /// <summary>Changes each time the themes are loaded, so a menu of them knows its items may have moved.</summary>
    public int Revision { get; private set; }

    /// <summary>Takes <paramref name="userThemes"/> in place of the user themes held before, and resolves
    /// every theme again. A user theme whose id is taken is left out.</summary>
    public void LoadUserThemes(IReadOnlyList<TFile> userThemes)
    {
        Revision++;
        Clear();
        foreach (var file in _builtIns)
            AddFile(file);
        foreach (var file in userThemes)
            AddUserFile(file);
        foreach (var id in _ids)
            Resolve(id);
    }

    private void Clear()
    {
        _files.Clear();
        _themes.Clear();
        _shownIds.Clear();
        _ids.Clear();
        _errors.Clear();
        _namesCulture = null;
    }

    private void AddUserFile(TFile file)
    {
        if (_files.ContainsKey(file.Id))
        {
            AddError(new ThemeError(file.File, 0, ThemeProblem.IdTaken, file.Id));
            return;
        }
        AddFile(file);
    }

    private void AddFile(TFile file)
    {
        _files[file.Id] = file;
        _ids.Add(file.Id);
        foreach (var error in file.Errors)
            AddError(error);
    }

    /// <summary>The theme <paramref name="id"/>, which must be one of <see cref="Ids"/>, resolved the
    /// first time it is asked for.</summary>
    protected TTheme Resolve(string id)
    {
        if (_themes.TryGetValue(id, out var resolved))
            return resolved;
        _resolving.Add(id);
        var file = _files[id];
        var theme = BuildChain(file);
        if (theme != null)
            _shownIds[id] = id;
        else
            theme = FallBack(file);
        _resolving.Remove(id);
        _themes[id] = theme;
        return theme;
    }

    private TTheme? BuildChain(TFile file)
    {
        var chain = ListChain(file);
        if (chain == null)
            return null;
        return Build(file, chain);
    }

    /// <summary>The theme <paramref name="file"/> and the themes it extends, <paramref name="chain"/>,
    /// the one extending no other first, make; or null after reporting why they make none.</summary>
    protected abstract TTheme? Build(TFile file, IReadOnlyList<TFile> chain);

    /// <summary><paramref name="file"/> and the themes it extends, the one extending no other first; null
    /// when one is unreadable or the chain breaks off or goes in a circle.</summary>
    private List<TFile>? ListChain(TFile file)
    {
        var chain = new List<TFile> { file };
        var current = file;
        while (current.Extends != null)
        {
            var parent = FindParent(current, chain);
            if (parent == null)
                return null;
            chain.Insert(0, parent);
            current = parent;
        }
        if (chain.Any(static f => f.IsUnreadable))
            return null;
        return chain;
    }

    /// <summary>The theme <paramref name="child"/> extends, or null after reporting that there is none or
    /// that it is already in <paramref name="chain"/>.</summary>
    private TFile? FindParent(TFile child, List<TFile> chain)
    {
        if (!_files.TryGetValue(child.Extends!, out var parent))
        {
            AddError(new ThemeError(child.File, child.ExtendsLine, ThemeProblem.UnknownParent, child.Extends!));
            return null;
        }
        if (chain.Contains(parent))
        {
            AddError(new ThemeError(child.File, child.ExtendsLine, ThemeProblem.ExtendsCycle, Detail: DescribeCycle(chain, parent)));
            return null;
        }
        return parent;
    }

    /// <summary>The circle <paramref name="chain"/> closes by extending <paramref name="parent"/>, from
    /// the theme being resolved on: <c>a → b → a</c>.</summary>
    private static string DescribeCycle(List<TFile> chain, TFile parent)
    {
        var ids = chain
            .Select(static f => f.Id)
            .Reverse()
            .Append(parent.Id);
        return string.Join(ChainArrow, ids);
    }

    /// <summary>What <paramref name="file"/> shows when it cannot be resolved: its parent as that resolves,
    /// else the built-in theme <see cref="FallbackIdOf"/> names.</summary>
    private TTheme FallBack(TFile file)
    {
        var target = FindFallBack(file);
        var theme = Resolve(target);
        _shownIds[file.Id] = _shownIds[target];
        return theme;
    }

    private string FindFallBack(TFile file)
    {
        if (CanFallBackToParent(file))
            return file.Extends!;
        var builtIn = FallbackIdOf(file);
        if (_resolving.Contains(builtIn))
            throw new InvalidOperationException($"The built-in theme '{builtIn}' cannot be resolved.");
        return builtIn;
    }

    /// <summary>The built-in theme <paramref name="file"/> shows when neither it nor its parent resolves.</summary>
    protected abstract string FallbackIdOf(TFile file);

    private bool CanFallBackToParent(TFile file)
    {
        if (file.Extends == null)
            return false;
        if (!_files.ContainsKey(file.Extends))
            return false;
        return !_resolving.Contains(file.Extends);
    }

    /// <summary>The file of the theme <paramref name="id"/>, which must be one of <see cref="Ids"/>.</summary>
    protected TFile FileOf(string id) => _files[id];

    protected void AddErrors(IEnumerable<ThemeError> errors)
    {
        foreach (var error in errors)
            AddError(error);
    }

    protected void AddError(ThemeError error)
    {
        if (_errors.Contains(error))
            return;
        _errors.Add(error);
    }

    /// <summary>The theme <paramref name="id"/> as resolved, or the built-in <see cref="DefaultId"/> when
    /// there is none with that id.</summary>
    public TTheme Find(string id)
    {
        if (_themes.TryGetValue(id, out var theme))
            return theme;
        return _themes[DefaultId];
    }

    /// <summary>The id of the theme <see cref="Find"/> shows for <paramref name="id"/>: the id itself when
    /// it resolves, else the theme it falls back to.</summary>
    public string FindShownId(string id)
    {
        if (_shownIds.TryGetValue(id, out var shown))
            return shown;
        return _shownIds[DefaultId];
    }

    /// <summary>The name of the theme <paramref name="id"/> in <paramref name="culture"/>'s language.</summary>
    public string NameOf(string id, CultureInfo culture) => _files[id].Name.In(culture);

    /// <summary>Where <paramref name="id"/> is in <see cref="Ids"/>, or -1. Allocates nothing.</summary>
    public int IndexOf(string id)
    {
        for (var i = 0; i < _ids.Count; i++)
        {
            if (string.Equals(_ids[i], id, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    /// <summary>Every theme's name in <paramref name="culture"/>'s language, in the order of
    /// <see cref="Ids"/>. Kept until the language or the themes change.</summary>
    public string[] ListNames(CultureInfo culture)
    {
        if (culture.Equals(_namesCulture))
            return _names;
        _names = _ids
            .Select(id => _files[id].Name.In(culture))
            .ToArray();
        _namesCulture = culture;
        return _names;
    }
}
