using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>The "compare with" dialog: the assembly to compare, and the other open assemblies whose API
/// it can be compared with, those of its name first, filtered by name or path, one of them selected. It
/// closes when the assembly to compare is closed.</summary>
public sealed class ComparisonPicker
{
    private readonly List<LoadedModule> _candidates = new();
    private readonly List<LoadedModule> _filtered = new();
    private int _listedVersion = -1;
    private string? _appliedFilter;
    private bool _focusFilter;

    public bool IsOpen;
    public string Filter = "";

    public ListSelection Selection { get; } = new();

    /// <summary>The assembly to compare, or null while the dialog is closed.</summary>
    public LoadedModule? Compared { get; private set; }

    /// <summary>The assemblies offered, in the order shown.</summary>
    public IReadOnlyList<LoadedModule> Filtered => _filtered;

    /// <summary>The assembly selected to compare with, or null when none is.</summary>
    public LoadedModule? Selected
    {
        get
        {
            if (!Selection.HasSelection)
                return null;
            return _filtered[Selection.Index];
        }
    }

    /// <summary>Opens the dialog to pick what to compare <paramref name="compared"/> with.</summary>
    public void Open(LoadedModule compared)
    {
        Compared = compared;
        IsOpen = true;
        Filter = "";
        Selection.Clear();
        _focusFilter = true;
        _listedVersion = -1;
        _appliedFilter = null;
    }

    public void Close()
    {
        IsOpen = false;
        Compared = null;
    }

    /// <summary>True once after <see cref="Open"/>: the filter box should take focus.</summary>
    public bool TakeFilterFocus()
    {
        var focus = _focusFilter;
        _focusFilter = false;
        return focus;
    }

    /// <summary>Lists the assemblies again when the open ones or the filter changed since the last frame;
    /// closes the dialog when the assembly to compare is no longer open.</summary>
    public void Update(AssemblyList assemblies)
    {
        RefreshCandidates(assemblies);
        ApplyFilter();
    }

    private void RefreshCandidates(AssemblyList assemblies)
    {
        if (assemblies.Version == _listedVersion)
            return;
        _listedVersion = assemblies.Version;
        var open = assemblies.Assemblies;
        if (!open.Contains(Compared!))
        {
            Close();
            return;
        }
        _candidates.Clear();
        _candidates.AddRange(ListCandidates(open));
        _appliedFilter = null;
    }

    private IEnumerable<LoadedModule> ListCandidates(IReadOnlyList<LoadedModule> open)
    {
        return open
            .Where(IsCandidate)
            .OrderByDescending(IsNamedAsCompared);
    }

    private bool IsCandidate(LoadedModule assembly)
    {
        return assembly != Compared;
    }

    private bool IsNamedAsCompared(LoadedModule assembly)
    {
        return string.Equals(assembly.Name, Compared!.Name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Filters the candidates, selecting the first shown unless the one selected still is.</summary>
    private void ApplyFilter()
    {
        if (!IsOpen)
            return;
        if (Filter == _appliedFilter)
            return;
        _appliedFilter = Filter;
        var selected = Selected;
        _filtered.Clear();
        var filter = Filter.Trim();
        foreach (var candidate in _candidates)
        {
            if (Matches(candidate, filter))
                _filtered.Add(candidate);
        }
        SelectAgain(selected);
    }

    private static bool Matches(LoadedModule assembly, string filter)
    {
        if (filter.Length == 0)
            return true;
        if (assembly.NameAndVersion.Contains(filter, StringComparison.OrdinalIgnoreCase))
            return true;
        return assembly.FilePath.Text.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private void SelectAgain(LoadedModule? selected)
    {
        var index = 0;
        if (selected != null)
            index = Math.Max(0, _filtered.IndexOf(selected));
        Selection.Select(index, _filtered.Count);
    }
}
