using ZGF.Gui.Views;

namespace ZGF.Gui.Desktop.Components.DataGrid;

/// <summary>Builds and binds a column's preview (display-only) cell — shared by the preview row and the
/// read-only cells of an editable row, so a column renders identically whichever row hosts it.</summary>
internal static class DataGridCell
{
    public static View BuildPreview<TItem>(DataGridColumn<TItem> column, DataGridStyle style, ICanvas canvas)
    {
        if (column.CreateCell != null) return column.CreateCell(canvas);
        return new TextView(canvas)
        {
            FontSize = 14f,
            TextColor = style.Text,
            HorizontalTextAlignment = column.Align,
            VerticalTextAlignment = TextAlignment.Center,
            TextOverflow = TextOverflow.Ellipsis,
        };
    }

    public static void BindPreview<TItem>(View cell, DataGridColumn<TItem> column, in TItem item)
    {
        if (column.CreateCell != null) column.BindCell?.Invoke(cell, item);
        else if (cell is TextView text) text.Text = column.Text?.Invoke(item) ?? string.Empty;
    }
}
