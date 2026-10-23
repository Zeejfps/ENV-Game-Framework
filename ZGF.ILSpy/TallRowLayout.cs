using ZGF.Gui.Immediate;

namespace ZGF.ILSpy;

/// <summary>How tall a row of a metadata grid is: its <paramref name="Cells"/>, and the
/// <paramref name="Details"/> shown under it.</summary>
internal readonly record struct RowHeights(float Cells, float Details);

/// <summary>The rows of a metadata grid taller than one line as of the last <see cref="Measure"/>:
/// those with cells open in place, as tall as their longest, and the selected row with the details
/// under it. Kept between frames so measuring reuses its buffers.</summary>
internal sealed class TallRowLayout
{
    private readonly Dictionary<int, float> _cellsByRow = new();
    private readonly Dictionary<int, RowHeights> _byIndex = new();
    private readonly List<TallRow> _rows = new();

    /// <summary>The tall rows in view order, as a virtual scroll lays them out.</summary>
    public IReadOnlyList<TallRow> Rows => _rows;

    public void Measure(MetadataGridState state, float rowHeight, float details)
    {
        _cellsByRow.Clear();
        _byIndex.Clear();
        _rows.Clear();
        MeasureOpenCells(state, rowHeight);
        if (details > 0f)
            _cellsByRow.TryAdd(state.Selected, rowHeight);
        foreach (var (row, cells) in _cellsByRow)
            Add(state, row, cells, details);
        _rows.Sort(static (a, b) => a.Index.CompareTo(b.Index));
    }

    private void MeasureOpenCells(MetadataGridState state, float rowHeight)
    {
        foreach (var (cell, lines) in state.Open)
        {
            var tallest = _cellsByRow.GetValueOrDefault(cell.Row, rowHeight);
            _cellsByRow[cell.Row] = MathF.Max(tallest, lines * rowHeight);
        }
    }

    private void Add(MetadataGridState state, int row, float cells, float details)
    {
        var index = state.IndexOf(row);
        if (index < 0) return;
        var below = row == state.Selected ? details : 0f;
        _byIndex[index] = new RowHeights(cells, below);
        _rows.Add(new TallRow(index, cells + below));
    }

    /// <summary>How tall the row at view <paramref name="index"/> is; no taller than one line if
    /// zero.</summary>
    public RowHeights At(int index)
    {
        return _byIndex.GetValueOrDefault(index);
    }
}
