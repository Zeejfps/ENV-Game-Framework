using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>Which of a document's <see cref="DecompiledDocument.Folds"/> are collapsed, and the rows
/// that leaves: row r shows line <see cref="LineAt"/>(r). The row map is rebuilt when either changes,
/// never per frame.</summary>
internal sealed class CodeFolds
{
    private DecompiledDocument _document = DecompiledDocument.Empty;
    private bool[] _collapsed = [];
    private int[] _rows = [];
    private bool _identity = true;

    public int RowCount { get; private set; }

    public bool AnyCollapsed => !_identity;

    public void Reset(DecompiledDocument document)
    {
        _document = document;
        var folds = document.Folds;
        _collapsed = folds.Length == 0 ? [] : new bool[folds.Length];
        for (var i = 0; i < folds.Length; i++)
            _collapsed[i] = folds[i].DefaultCollapsed;
        Rebuild();
    }

    public bool IsCollapsed(int fold) => _collapsed[fold];

    public void SetCollapsed(int fold, bool collapsed)
    {
        if (_collapsed[fold] == collapsed) return;
        _collapsed[fold] = collapsed;
        Rebuild();
    }

    public void SetAll(bool collapsed)
    {
        Array.Fill(_collapsed, collapsed);
        Rebuild();
    }

    /// <summary>Expands every fold that hides <paramref name="line"/>; returns whether any did.</summary>
    public bool Reveal(int line)
    {
        if (_identity) return false;
        var folds = _document.Folds;
        var changed = false;
        for (var i = 0; i < folds.Length && folds[i].StartLine < line; i++)
        {
            if (_collapsed[i] && line <= folds[i].EndLine)
            {
                _collapsed[i] = false;
                changed = true;
            }
        }
        if (changed) Rebuild();
        return changed;
    }

    /// <summary>The collapsed fold whose row is <paramref name="line"/>, or -1.</summary>
    public int CollapsedAt(int line)
    {
        if (_identity) return -1;
        var fold = _document.FoldStartingAt(line);
        return fold >= 0 && _collapsed[fold] ? fold : -1;
    }

    /// <summary>The innermost fold around the visible line <paramref name="line"/> is shown on, of
    /// any state or only <paramref name="collapsed"/> ones; -1 when there is none.</summary>
    public int Innermost(int line, bool? collapsed = null)
    {
        line = VisibleLine(line);
        var folds = _document.Folds;
        var found = -1;
        for (var i = 0; i < folds.Length && folds[i].StartLine <= line; i++)
        {
            if (line <= folds[i].EndLine && (collapsed == null || _collapsed[i] == collapsed))
                found = i;
        }
        return found;
    }

    public int LineAt(int row) => _identity ? row : _rows[row];

    /// <summary>The row <paramref name="line"/> is shown on: its own, or that of the fold hiding it.</summary>
    public int RowOf(int line)
    {
        if (_identity) return Math.Clamp(line, 0, Math.Max(0, RowCount - 1));
        int low = 0, high = RowCount - 1;
        while (low < high)
        {
            var mid = (low + high + 1) >> 1;
            if (_rows[mid] <= line) low = mid;
            else high = mid - 1;
        }
        return low;
    }

    public int VisibleLine(int line) => RowCount == 0 ? 0 : LineAt(RowOf(line));

    /// <summary>The first line after the row <paramref name="line"/> is shown on.</summary>
    public int NextLine(int line)
    {
        var row = RowOf(line);
        return row + 1 < RowCount ? LineAt(row + 1) : _document.Lines.Length;
    }

    private void Rebuild()
    {
        var lines = _document.Lines.Length;
        var folds = _document.Folds;
        _identity = Array.IndexOf(_collapsed, true) < 0;
        if (_identity)
        {
            RowCount = lines;
            return;
        }

        if (_rows.Length < lines)
            _rows = new int[lines];
        var count = 0;
        var next = 0;
        for (var line = 0; line < lines;)
        {
            _rows[count++] = line;
            while (next < folds.Length && folds[next].StartLine < line)
                next++;
            line = next < folds.Length && folds[next].StartLine == line && _collapsed[next] ? folds[next].EndLine + 1 : line + 1;
        }
        RowCount = count;
    }
}
