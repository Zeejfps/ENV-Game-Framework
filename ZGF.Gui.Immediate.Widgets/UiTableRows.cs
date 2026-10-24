using ZGF.Desktop;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>A table opened by <see cref="UiWidgets.Table{TKey}"/>, whose rows are picked out several
/// at a time: record its <see cref="Header"/>, then its <see cref="FitBody"/>.</summary>
public readonly struct TableFrame<TKey> where TKey : notnull
{
    private readonly Ui _ui;
    private readonly UiWidgets.TableState _state;
    private readonly MultiSelection<TKey> _selection;

    internal TableFrame(Ui ui, UiWidgets.TableState state, MultiSelection<TKey> selection)
    {
        _ui = ui;
        _state = state;
        _selection = selection;
    }

    /// <summary>Whether the table held keyboard focus when it was opened.</summary>
    public bool Focused => _state.Focused;

    /// <summary>Records the header row, each column labelled from <paramref name="labels"/>, its
    /// borders dragged to resize the columns.</summary>
    public void Header(ReadOnlySpan<string> labels) => UiWidgets.TableHeader<int>(_ui, _state, labels, null, null, 0);

    /// <summary>
    /// Every row of <paramref name="order"/>, one per key, in a body that does not scroll but is as
    /// tall as its rows, so the table can sit in a page that scrolls. Each row is as tall as its
    /// tallest cell, at least the theme's row height. Keys gone from <paramref name="order"/> leave
    /// the selection. While the table holds focus it takes the keys it acts on, so nothing after it
    /// sees them: Up and Down move the selection, with Shift extending it, Home and End go to
    /// either end, Ctrl+A picks out every row, and Delete or Backspace asks for the picked-out rows
    /// to be removed — act on that once the rows are recorded.
    /// </summary>
    public TableRows<TKey> FitBody(IReadOnlyList<TKey> order) => UiWidgets.TableFitBody(_ui, _state, _selection, order);
}

/// <summary>The rows of a <see cref="TableFrame{TKey}.FitBody"/>, each recorded with <see cref="Row"/>,
/// and what the user asked of them.</summary>
public readonly struct TableRows<TKey> : IDisposable where TKey : notnull
{
    private readonly Ui _ui;
    private readonly UiWidgets.TableState _state;
    private readonly MultiSelection<TKey> _selection;
    private readonly IReadOnlyList<TKey> _order;
    private readonly Ui.Scope _body;

    internal TableRows(Ui ui, UiWidgets.TableState state, MultiSelection<TKey> selection, IReadOnlyList<TKey> order, Ui.Scope body,
        bool removeRequested)
    {
        _ui = ui;
        _state = state;
        _selection = selection;
        _order = order;
        _body = body;
        RemoveRequested = removeRequested;
    }

    /// <summary>Whether the table held keyboard focus when it was opened.</summary>
    public bool Focused => _state.Focused;

    /// <summary>Delete or Backspace was pressed while the table held focus: remove the picked-out rows.</summary>
    public bool RemoveRequested { get; }

    /// <summary>
    /// Opens row <paramref name="index"/> of the order, its ids and state following its key, its
    /// background showing whether it is picked out, the table focused and the row under the pointer.
    /// A press on the row itself — not on a control in one of its cells — focuses the table and
    /// picks it out alone, toggles it with Ctrl, or picks out the range from the anchor with Shift.
    /// Record its cells with <see cref="TableRow.Cell"/>, in column order.
    /// </summary>
    public TableRow Row(int index) => UiWidgets.TableFitRow(_ui, _state, _selection, _order, index);

    public void Dispose() => _body.Dispose();
}

public static partial class UiWidgets
{
    public static TableFrame<TKey> Table<TKey>(this Ui ui, string id, TableColumns columns, MultiSelection<TKey> rows) where TKey : notnull =>
        Table(ui, ui.GetId(id), columns, rows);

    /// <summary>
    /// Opens the table <paramref name="id"/> laid out in <paramref name="columns"/>, whose rows
    /// <paramref name="rows"/> picks out by key, any number at a time: record its
    /// <see cref="TableFrame{TKey}.Header"/>, then its <see cref="TableFrame{TKey}.FitBody"/>. The
    /// table is a Tab stop and takes keyboard focus, but no text, under the id of its body.
    /// </summary>
    public static TableFrame<TKey> Table<TKey>(this Ui ui, int id, TableColumns columns, MultiSelection<TKey> rows) where TKey : notnull
    {
        var state = ui.GetState<TableState>(HashCode.Combine(id, TableSalt));
        state.Begin(id, columns, state.Rows, null, null, ui.Theme.RowHeight);
        state.Focused = ui.ListArea(id);
        return new TableFrame<TKey>(ui, state, rows);
    }

    internal static TableRows<TKey> TableFitBody<TKey>(Ui ui, TableState state, MultiSelection<TKey> selection, IReadOnlyList<TKey> order)
        where TKey : notnull
    {
        selection.Reconcile(order);
        var removeRequested = false;
        if (state.Focused)
            removeRequested = TakeFitTableKeys(ui, selection, order);
        var body = ui.Column(state.Id, new Box { Width = Sizing.Grow(), Gap = 0f });
        return new TableRows<TKey>(ui, state, selection, order, body, removeRequested);
    }

    /// <summary>Takes the keys a table of picked-out rows acts on; returns whether removal was asked for.</summary>
    private static bool TakeFitTableKeys<TKey>(Ui ui, MultiSelection<TKey> selection, IReadOnlyList<TKey> order) where TKey : notnull
    {
        var removeRequested = false;
        var keys = ui.Input.Keys;
        var i = 0;
        while (i < keys.Count)
        {
            var stroke = keys[i];
            if (!ApplyFitTableKey(stroke, selection, order))
            {
                i++;
                continue;
            }
            if (IsRemoveKey(stroke))
                removeRequested = true;
            ui.Input.TakeKeyAt(i);
        }
        return removeRequested;
    }

    private static bool ApplyFitTableKey<TKey>(KeyStroke stroke, MultiSelection<TKey> selection, IReadOnlyList<TKey> order) where TKey : notnull
    {
        var extend = (stroke.Modifiers & KeyModifiers.Shift) != 0;
        if (IsCommand(stroke.Modifiers))
        {
            if (stroke.Key != KeyboardKey.A) return false;
            selection.SelectAll(order);
            return true;
        }
        switch (stroke.Key)
        {
            case KeyboardKey.UpArrow:
                selection.Move(-1, extend, order);
                return true;
            case KeyboardKey.DownArrow:
                selection.Move(1, extend, order);
                return true;
            case KeyboardKey.Home:
                selection.MoveTo(0, extend, order);
                return true;
            case KeyboardKey.End:
                selection.MoveTo(order.Count - 1, extend, order);
                return true;
            case KeyboardKey.Delete:
            case KeyboardKey.Backspace:
                return true;
            default:
                return false;
        }
    }

    /// <summary>Delete or Backspace pressed, not held down: a repeat must not remove the rows after.</summary>
    private static bool IsRemoveKey(KeyStroke stroke)
    {
        if (stroke.IsRepeat)
            return false;
        return stroke.Key == KeyboardKey.Delete || stroke.Key == KeyboardKey.Backspace;
    }

    private static bool IsCommand(KeyModifiers modifiers) => (modifiers & (KeyModifiers.Control | KeyModifiers.Super)) != 0;

    internal static TableRow TableFitRow<TKey>(Ui ui, TableState state, MultiSelection<TKey> selection, IReadOnlyList<TKey> order, int index)
        where TKey : notnull
    {
        var key = order[index];
        var rowKey = EqualityComparer<TKey>.Default.GetHashCode(key);
        var row = ui.Interact(HashCode.Combine(state.Id, TableRowSalt, rowKey));
        if (row.Pressed)
            PickFitRow(ui, state, selection, order, key);
        var selected = selection.Contains(key);
        var ids = ui.Id(rowKey);
        var content = ui.Row(row.Id, FitRowBox(ui, state, selected, row.Hovered));
        return new TableRow(ui, state, ids, content, rowKey, row, selected);
    }

    private static void PickFitRow<TKey>(Ui ui, TableState state, MultiSelection<TKey> selection, IReadOnlyList<TKey> order, TKey key)
        where TKey : notnull
    {
        ui.Focus(state.Id, takesText: false);
        var change = PickChange(ui.Input.Modifiers);
        selection.Select(key, change, order);
    }

    internal static SelectionChange PickChange(KeyModifiers modifiers)
    {
        if (IsCommand(modifiers))
            return SelectionChange.Toggle;
        if ((modifiers & KeyModifiers.Shift) != 0)
            return SelectionChange.Extend;
        return SelectionChange.Replace;
    }

    /// <summary>A row as tall as its tallest cell, at least the table's row height.</summary>
    private static Box FitRowBox(Ui ui, TableState state, bool selected, bool hovered) =>
        TableRowBox(ui, state, null, selected, hovered) with { Height = Sizing.Fit(state.RowHeight) };
}
