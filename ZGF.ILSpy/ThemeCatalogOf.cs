using System.Globalization;

namespace ZGF.ILSpy;

/// <summary>The themes of one kind, color or icon, by id: the built-in ones, then the user's. Each is
/// resolved over the themes it extends once, when the catalog loads; one that cannot be resolved falls
/// back to its parent, else to the built-in theme <see cref="FallbackIdOf"/> names. Keeps what was wrong
/// in each file.</summary>
public abstract class ThemeCatalog<TFile, TContents, TTheme>
    where TFile : class, IThemeFile<TFile, TContents>
    where TContents : class
    where TTheme : class
{
    private const string ChainArrow = " → ";

    private readonly IReadOnlyList<TFile> _builtIns;
    private readonly Dictionary<ThemeId<TFile>, TFile> _files = new();
    private readonly Dictionary<ThemeId<TFile>, TTheme> _themes = new();
    private readonly Dictionary<ThemeId<TFile>, ThemeId<TFile>> _shownIds = new();
    private readonly HashSet<ThemeId<TFile>> _resolving = new();
    private readonly List<ThemeId<TFile>> _ids = new();
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
    protected abstract ThemeId<TFile> DefaultId { get; }

    /// <summary>Every theme's id, built-in ones first, in the order a menu lists them.</summary>
    public IReadOnlyList<ThemeId<TFile>> Ids => _ids;

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
            AddError(new ThemeError(file.File, 0, ThemeProblem.IdTaken, file.Id.Text));
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
    protected TTheme Resolve(ThemeId<TFile> id)
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
    /// each with what it sets, the one extending no other first, make; or null after reporting why they
    /// make none.</summary>
    protected abstract TTheme? Build(TFile file, IReadOnlyList<(TFile File, TContents Contents)> chain);

    /// <summary><paramref name="file"/> and the themes it extends, each with what it sets, the one extending
    /// no other first; null when one could not be read or the chain breaks off or goes in a circle.</summary>
    private List<(TFile File, TContents Contents)>? ListChain(TFile file)
    {
        var files = ListFiles(file);
        if (files == null)
            return null;
        var chain = new List<(TFile, TContents)>(files.Count);
        foreach (var link in files)
        {
            var contents = link.Contents;
            if (contents == null)
                return null;
            chain.Add((link, contents));
        }
        return chain;
    }

    /// <summary><paramref name="file"/> and the themes it extends, the one extending no other first; null
    /// when the chain breaks off or goes in a circle.</summary>
    private List<TFile>? ListFiles(TFile file)
    {
        var chain = new List<TFile> { file };
        var extends = file.Extends;
        var current = file;
        while (extends != null)
        {
            var parent = FindParent(current, extends, chain);
            if (parent == null)
                return null;
            chain.Insert(0, parent);
            current = parent;
            extends = parent.Extends;
        }
        return chain;
    }

    /// <summary>The theme <paramref name="child"/> extends as <paramref name="extends"/> says, or null after
    /// reporting that there is none or that it is already in <paramref name="chain"/>.</summary>
    private TFile? FindParent(TFile child, ThemeParent<TFile> extends, List<TFile> chain)
    {
        if (!_files.TryGetValue(extends.Id, out var parent))
        {
            AddError(new ThemeError(child.File, extends.Line, ThemeProblem.UnknownParent, extends.Id.Text));
            return null;
        }
        if (chain.Contains(parent))
        {
            AddError(new ThemeError(child.File, extends.Line, ThemeProblem.ExtendsCycle, Detail: DescribeCycle(chain, parent)));
            return null;
        }
        return parent;
    }

    /// <summary>The circle <paramref name="chain"/> closes by extending <paramref name="parent"/>, from
    /// the theme being resolved on: <c>a → b → a</c>.</summary>
    private static string DescribeCycle(List<TFile> chain, TFile parent)
    {
        var ids = chain
            .Select(static f => f.Id.Text)
            .Reverse()
            .Append(parent.Id.Text);
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

    private ThemeId<TFile> FindFallBack(TFile file)
    {
        var extends = file.Extends;
        if (extends != null && CanFallBackTo(extends.Id))
            return extends.Id;
        var builtIn = FallbackIdOf(file);
        if (_resolving.Contains(builtIn))
            throw new InvalidOperationException($"The built-in theme '{builtIn}' cannot be resolved.");
        return builtIn;
    }

    /// <summary>The built-in theme <paramref name="file"/> shows when neither it nor its parent resolves.</summary>
    protected abstract ThemeId<TFile> FallbackIdOf(TFile file);

    private bool CanFallBackTo(ThemeId<TFile> parent)
    {
        if (!_files.ContainsKey(parent))
            return false;
        return !_resolving.Contains(parent);
    }

    /// <summary>What the built-in theme <paramref name="id"/> sets; throws when it could not be read, as a
    /// built-in theme always can.</summary>
    protected TContents BuiltInContentsOf(ThemeId<TFile> id)
    {
        var contents = _files[id].Contents;
        if (contents == null)
            throw new InvalidOperationException($"The built-in theme '{id}' cannot be read.");
        return contents;
    }

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
    public TTheme Find(ThemeId<TFile> id)
    {
        if (_themes.TryGetValue(id, out var theme))
            return theme;
        return _themes[DefaultId];
    }

    /// <summary>The id of the theme <see cref="Find"/> shows for <paramref name="id"/>: the id itself when
    /// it resolves, else the theme it falls back to.</summary>
    public ThemeId<TFile> FindShownId(ThemeId<TFile> id)
    {
        if (_shownIds.TryGetValue(id, out var shown))
            return shown;
        return _shownIds[DefaultId];
    }

    /// <summary>The name of the theme <paramref name="id"/> in <paramref name="culture"/>'s language.</summary>
    public string NameOf(ThemeId<TFile> id, CultureInfo culture) => _files[id].Name.In(culture);

    /// <summary>Where <paramref name="id"/> is in <see cref="Ids"/>, or -1. Allocates nothing.</summary>
    public int IndexOf(ThemeId<TFile> id)
    {
        for (var i = 0; i < _ids.Count; i++)
        {
            if (_ids[i].Equals(id))
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
