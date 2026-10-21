using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>The "open from framework" dialog: the runtime's assemblies, scanned once in the
/// background, filtered by name.</summary>
public sealed class FrameworkPickerModel
{
    private readonly List<RuntimeAssembly> _filtered = new();
    private readonly HashSet<string> _listed = new(StringComparer.OrdinalIgnoreCase);
    private int _listedVersion = -1;
    private IReadOnlyList<RuntimeAssembly>? _all;
    private bool _loading;
    private string? _appliedFilter;

    public bool IsOpen;
    public bool FocusFilter;
    public string Filter = "";
    public int SelectedIndex = -1;
    public bool ScrollToSelected;

    public RuntimeAssembly? Selected => SelectedIndex >= 0 && SelectedIndex < _filtered.Count ? _filtered[SelectedIndex] : null;

    public bool IsLoading => _all == null;
    public IReadOnlyList<RuntimeAssembly> Filtered => _filtered;

    public void Open(Action<Action> post)
    {
        IsOpen = true;
        FocusFilter = true;
        _appliedFilter = null;
        if (_all != null || _loading) return;
        _loading = true;
        Task.Run(() =>
        {
            IReadOnlyList<RuntimeAssembly> all;
            try
            {
                all = RuntimeAssemblies.All;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                all = [];
            }
            post(() =>
            {
                _all = all;
                _loading = false;
                _appliedFilter = null;
            });
        });
    }

    public void Close() => IsOpen = false;

    /// <summary>Whether <paramref name="assembly"/> is already in the assembly list.</summary>
    public bool IsListed(RuntimeAssembly assembly) => _listed.Contains(assembly.FilePath);

    /// <summary>Re-filters when the filter text changed since the last frame.</summary>
    public void Update(AssemblyList assemblies)
    {
        if (assemblies.Version != _listedVersion)
        {
            _listedVersion = assemblies.Version;
            _listed.Clear();
            _listed.UnionWith(assemblies.FilePaths);
        }
        if (_all == null || Filter == _appliedFilter) return;
        _appliedFilter = Filter;
        var selected = Selected;
        _filtered.Clear();
        var filter = Filter.Trim();
        foreach (var assembly in _all)
        {
            if (filter.Length == 0 || assembly.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                _filtered.Add(assembly);
        }
        SelectedIndex = selected == null ? -1 : _filtered.IndexOf(selected);
        if (SelectedIndex < 0 && _filtered.Count > 0)
            SelectedIndex = 0;
        ScrollToSelected = true;
    }

    public void Move(int by)
    {
        if (_filtered.Count == 0) return;
        SelectedIndex = Math.Clamp(SelectedIndex + by, 0, _filtered.Count - 1);
        ScrollToSelected = true;
    }
}
