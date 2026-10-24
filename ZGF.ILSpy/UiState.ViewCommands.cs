namespace ZGF.ILSpy;

public sealed partial class UiState
{
    /// <summary>Returns the view of <paramref name="tab"/>, an open tab, to <paramref name="position"/>.</summary>
    public void RestoreView(DocumentTab tab, CodeViewPosition position)
    {
        var view = _tabViews.GetValueOrDefault(tab);
        if (view == null)
            return;
        if (tab.Grid == null)
        {
            view.Code.Restore(position);
            return;
        }
        view.Grid.Restore(position);
    }

    /// <summary>Scrolls the code of <paramref name="tab"/>, an open tab, to <paramref name="line"/> and flashes it.</summary>
    public void RevealLine(DocumentTab tab, int line)
    {
        var view = _tabViews.GetValueOrDefault(tab);
        if (view == null)
            return;
        if (line >= tab.Document.Lines.Length)
            return;
        view.Code.RevealLine(line);
    }

    /// <summary>Selects the grid row <paramref name="row"/> of <paramref name="tab"/>, an open tab, and scrolls to it.</summary>
    public void RevealRow(DocumentTab tab, int row)
    {
        var view = _tabViews.GetValueOrDefault(tab);
        if (view == null)
            return;
        if (!HasRow(tab, row))
            return;
        view.Grid.Reveal(row);
    }

    private static bool HasRow(DocumentTab tab, int row)
    {
        var grid = tab.Grid;
        if (grid == null)
            return false;
        return row < grid.RowCount;
    }
}
