namespace ZGF.Gui.Desktop.Components.DataGrid;

/// <summary>
/// The index-oriented data seam a <see cref="DataGridView{TItem}"/> reads through, hiding any
/// pagination/windowing behind an integer index. Generalizes the ledger's data source: a grid asks only for
/// the count and the item at an index, and hints the resident window before binding a range.
/// </summary>
public interface IDataGridSource<TItem>
{
    int Count { get; }

    /// <summary>Returns the item at <paramref name="index"/> if it is resident; false if the index is out of
    /// range or not yet loaded (call <see cref="EnsureWindow"/> first for the latter).</summary>
    bool TryGetItem(int index, out TItem item);

    /// <summary>Hints that rows <paramref name="first"/>..<paramref name="last"/> are about to be bound, so a
    /// windowed source can make them resident. A fully in-memory source may no-op.</summary>
    void EnsureWindow(int first, int last);
}
