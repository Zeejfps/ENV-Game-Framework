using System.Diagnostics;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The "open from framework" dialog: the runtime's assemblies not open yet, scanned once
/// in the background, filtered by name.</summary>
public sealed class FrameworkPicker
{
    private readonly Func<Strings> _strings;
    private readonly Action<AppProblem> _reportError;
    private readonly Func<IReadOnlyList<RuntimeAssembly>> _scan;
    private readonly List<RuntimeAssembly> _filtered = new();
    private readonly HashSet<string> _listed = new(StringComparer.OrdinalIgnoreCase);
    private int _listedVersion = -1;
    private IReadOnlyList<RuntimeAssembly>? _all;
    private string? _appliedFilter;
    private bool _focusFilter;

    /// <param name="strings">The interface's strings in the current language.</param>
    /// <param name="reportError">Tells the user the runtime could not be scanned.</param>
    /// <param name="scan">Lists the assemblies to offer; the running runtime's by default.</param>
    public FrameworkPicker(Func<Strings> strings, Action<AppProblem> reportError, Func<IReadOnlyList<RuntimeAssembly>>? scan = null)
    {
        _strings = strings;
        _reportError = reportError;
        _scan = scan ?? ScanRuntime;
    }

    private static IReadOnlyList<RuntimeAssembly> ScanRuntime() => RuntimeAssemblies.All;

    public bool IsOpen;
    public string Filter = "";
    public MultiSelection<RuntimeAssembly> Selection { get; } = new();

    public bool HasSelection => Selection.Count > 0;

    /// <summary>The picked-out assemblies, in the order they are shown.</summary>
    public List<RuntimeAssembly> ListSelected() => Selection.InOrder(_filtered);

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
        _appliedFilter = null;
        switch (scanned)
        {
            case Outcome<IReadOnlyList<RuntimeAssembly>>.Ok ok:
                _all = ok.Value;
                break;
            case Outcome<IReadOnlyList<RuntimeAssembly>>.Failed failed:
                Error = failed.Error.ToString();
                _reportError(new AppProblem.RuntimeNotScanned(failed.Error));
                break;
            default:
                throw new UnreachableException();
        }
    }

    public void Close() => IsOpen = false;

    /// <summary>True once after <see cref="Open"/>: the filter box should take focus.</summary>
    public bool TakeFilterFocus()
    {
        var focus = _focusFilter;
        _focusFilter = false;
        return focus;
    }

    /// <summary>Re-filters when the assembly list or the filter text changed since the last frame.</summary>
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
        _appliedFilter = null;
    }

    private void ApplyFilter()
    {
        if (_all == null)
            return;
        if (Filter == _appliedFilter)
            return;
        _appliedFilter = Filter;
        _filtered.Clear();
        var filter = Filter.Trim();
        foreach (var assembly in _all)
        {
            if (Offers(assembly, filter))
                _filtered.Add(assembly);
        }
        Selection.Reconcile(_filtered);
        if (!HasSelection)
            Selection.MoveTo(0, extend: false, _filtered);
    }

    /// <summary>Whether <paramref name="assembly"/> is not open yet and its name matches
    /// <paramref name="filter"/>.</summary>
    private bool Offers(RuntimeAssembly assembly, string filter)
    {
        if (_listed.Contains(assembly.FilePath))
            return false;
        if (filter.Length == 0)
            return true;
        return assembly.Name.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }
}
