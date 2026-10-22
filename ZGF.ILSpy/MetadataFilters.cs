using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy;

/// <summary>The flags filters of the metadata grids: one per kind of row and column, shared by every
/// grid of that kind in any assembly, as ILSpy's are, and kept between runs.</summary>
public sealed class MetadataFilters
{
    private readonly Dictionary<string, SavedFilterState> _saved;
    private readonly Dictionary<string, FilterState> _states = new();
    private int _seen;

    public MetadataFilters(Dictionary<string, SavedFilterState>? saved)
    {
        _saved = saved != null ? new Dictionary<string, SavedFilterState>(saved) : new Dictionary<string, SavedFilterState>();
    }

    /// <summary>Changes whenever a filter does.</summary>
    public int Revision { get; private set; }

    /// <summary>The filter of <paramref name="column"/>, a column of flags, as last left.</summary>
    public FilterState For(MetadataGrid grid, int column)
    {
        var key = $"{grid.Key}/{grid.Columns[column].Name}";
        if (_states.TryGetValue(key, out var state)) return state;
        state = new FilterState(grid.Columns[column].Flags ?? throw new ArgumentException($"{key} is not a flags column.", nameof(column)));
        if (_saved.TryGetValue(key, out var saved))
            state.Restore(saved);
        _states.Add(key, state);
        return state;
    }

    /// <summary>Notices filters changed since the last call, bumping <see cref="Revision"/>.</summary>
    public void Sync()
    {
        var seen = _states.Values.Sum(static s => s.Revision);
        if (seen == _seen) return;
        _seen = seen;
        Revision++;
    }

    /// <summary>Every filter that filters something, to save.</summary>
    public Dictionary<string, SavedFilterState> Save()
    {
        foreach (var (key, state) in _states)
        {
            if (state.Save() is { } saved)
                _saved[key] = saved;
            else
                _saved.Remove(key);
        }
        return new Dictionary<string, SavedFilterState>(_saved);
    }
}
