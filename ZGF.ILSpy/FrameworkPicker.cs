using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The "open from framework" dialog: the runtime's assemblies, scanned once in the
/// background, filtered by name.</summary>
public sealed class FrameworkPicker
{
    private readonly Func<Strings> _strings;
    private readonly Action<string, string> _reportError;
    private readonly Func<IReadOnlyList<RuntimeAssembly>> _scan;
    private readonly List<RuntimeAssembly> _filtered = new();
    private readonly HashSet<string> _listed = new(StringComparer.OrdinalIgnoreCase);
    private int _listedVersion = -1;
    private IReadOnlyList<RuntimeAssembly>? _all;
    private string? _appliedFilter;
    private bool _focusFilter;

    /// <param name="strings">The interface's strings in the current language.</param>
    /// <param name="reportError">Tells the user the runtime could not be scanned: a title and details.</param>
    /// <param name="scan">Lists the assemblies to offer; the running runtime's by default.</param>
    public FrameworkPicker(Func<Strings> strings, Action<string, string> reportError, Func<IReadOnlyList<RuntimeAssembly>>? scan = null)
    {
        _strings = strings;
        _reportError = reportError;
        _scan = scan ?? ScanRuntime;
    }

    private static IReadOnlyList<RuntimeAssembly> ScanRuntime() => RuntimeAssemblies.All;

    public bool IsOpen;
    public string Filter = "";
    public ListSelection Selection { get; } = new();

    public RuntimeAssembly? Selected
    {
        get
        {
            var index = Selection.Index;
            if (index < 0)
                return null;
            if (index >= _filtered.Count)
                return null;
            return _filtered[index];
        }
    }

    public bool IsLoading { get; private set; }
    public IReadOnlyList<RuntimeAssembly> Filtered => _filtered;

    /// <summary>Why the runtime could not be scanned, or null.</summary>
    public string? Error { get; private set; }

    public async void Open()
    {
        IsOpen = true;
        _focusFilter = true;
        _appliedFilter = null;
        if (_all != null)
            return;
        if (IsLoading)
            return;
        IsLoading = true;
        Error = null;
        var scanned = await Background.Run(_scan);
        IsLoading = false;
        _all = scanned.Value;
        Error = scanned.Error?.ToString();
        _appliedFilter = null;
        if (Error != null)
            _reportError(_strings().ErrorScanRuntime, Error);
    }

    public void Close() => IsOpen = false;

    /// <summary>True once after <see cref="Open"/>: the filter box should take focus.</summary>
    public bool TakeFilterFocus()
    {
        var focus = _focusFilter;
        _focusFilter = false;
        return focus;
    }

    /// <summary>Whether <paramref name="assembly"/> is already in the assembly list.</summary>
    public bool IsListed(RuntimeAssembly assembly) => _listed.Contains(assembly.FilePath);

    /// <summary>Catches up with the assembly list and re-filters when the filter text changed since
    /// the last frame.</summary>
    public void Update(AssemblyList assemblies)
    {
        RefreshListed(assemblies);
        ApplyFilter();
    }

    private void RefreshListed(AssemblyList assemblies)
    {
        if (assemblies.Version == _listedVersion)
            return;
        _listedVersion = assemblies.Version;
        _listed.Clear();
        _listed.UnionWith(assemblies.FilePaths);
    }

    private void ApplyFilter()
    {
        if (_all == null)
            return;
        if (Filter == _appliedFilter)
            return;
        _appliedFilter = Filter;
        var selected = Selected;
        _filtered.Clear();
        var filter = Filter.Trim();
        foreach (var assembly in _all)
        {
            if (filter.Length == 0 || assembly.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                _filtered.Add(assembly);
        }
        var index = 0;
        if (selected != null)
            index = Math.Max(0, _filtered.IndexOf(selected));
        Selection.Select(index, _filtered.Count);
    }
}
