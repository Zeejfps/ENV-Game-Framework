using ZGF.Gui.Desktop.Components.TextInput;
using ZGF.Gui.Desktop.Controllers;

namespace ZGF.Gui.Desktop.Components.DataGrid;

/// <summary>Ergonomic factories for the common <see cref="DataGridColumn{TItem}"/> shapes, so a grid is
/// declared as a short list rather than hand-built object initializers.</summary>
public static class GridColumn
{
    /// <summary>A read-only text column.</summary>
    public static DataGridColumn<T> Text<T>(
        string key, string header, ColumnWidth width, Func<T, string> text,
        bool sortable = false, TextAlignment align = TextAlignment.Start) =>
        new()
        {
            Key = key,
            Header = header,
            Width = width,
            Align = align,
            Sortable = sortable,
            Text = text,
        };

    /// <summary>An editable text column: shows <paramref name="get"/> as its preview, and on edit hosts a
    /// <see cref="TextInputView"/> that writes back through <paramref name="set"/> on commit.</summary>
    public static DataGridColumn<T> TextEditable<T>(
        string key, string header, ColumnWidth width, Func<T, string> get, Action<T, string> set,
        bool sortable = false, TextAlignment align = TextAlignment.Start) =>
        new()
        {
            Key = key,
            Header = header,
            Width = width,
            Align = align,
            Sortable = sortable,
            Text = get,
            CreateEditor = ctx =>
            {
                var input = new TextInputView(ctx.Canvas)
                {
                    TextWrap = TextWrap.NoWrap,
                    BackgroundColor = ctx.Style.Surface,
                    TextColor = ctx.Style.Text,
                    CaretColor = ctx.Style.Text,
                    SelectionRectColor = ctx.Style.SelectionBar,
                    FontSize = 14f,
                    TextVerticalAlignment = TextAlignment.Center,
                };
                input.UseController(ctx.Input, new DataGridTextEditorController(input, ctx.Input, ctx.Session));
                return input;
            },
            BindEditor = (v, item) =>
            {
                var input = (TextInputView)v;
                input.SetText(get(item));
                input.SelectAll();
            },
            CommitEditor = (v, item) =>
            {
                var input = (TextInputView)v;
                set(item, new string(input.Text));
            },
        };
}
