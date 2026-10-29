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
    private readonly HashSet<AssemblyPath> _listed = new();
    private int _listedVersion = -1;
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

    public RuntimeScan Scan { get; private set; } = new RuntimeScan.NotStarted();

    public IReadOnlyList<RuntimeAssembly> Filtered => _filtered;

    /// <summary>Scans the runtime the first time it opens, and again after a scan failed.</summary>
    public async void Open()
    {
        IsOpen = true;
        _focusFilter = true;
        _appliedFilter = null;
        if (Scan is RuntimeScan.Scanned or RuntimeScan.Scanning)
            return;
        Scan = new RuntimeScan.Scanning();
        var scanned = await Background.Run(_scan);
        _appliedFilter = null;
        Scan = ScanOf(scanned);
    }

    private RuntimeScan ScanOf(Outcome<IReadOnlyList<RuntimeAssembly>> scanned)
    {
        switch (scanned)
        {
            case Outcome<IReadOnlyList<RuntimeAssembly>>.Ok ok:
                return new RuntimeScan.Scanned(ok.Value);
            case Outcome<IReadOnlyList<RuntimeAssembly>>.Failed failed:
                _reportError(new AppProblem.RuntimeNotScanned(failed.Error));
                return new RuntimeScan.Failed(failed.Error.ToString());
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
        if (Scan is not RuntimeScan.Scanned scanned)
            return;
        if (Filter == _appliedFilter)
            return;
        _appliedFilter = Filter;
        _filtered.Clear();
        var filter = Filter.Trim();
        foreach (var assembly in scanned.Assemblies)
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

/// <summary>How far scanning the runtime for its assemblies has got.</summary>
public abstract record RuntimeScan
{
    private RuntimeScan()
    {
    }

    public sealed record NotStarted : RuntimeScan;

    public sealed record Scanning : RuntimeScan;

    public sealed record Scanned(IReadOnlyList<RuntimeAssembly> Assemblies) : RuntimeScan;

    public sealed record Failed(string Error) : RuntimeScan;
}
