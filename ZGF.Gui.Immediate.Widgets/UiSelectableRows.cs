using ZGF.Desktop;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>The rows of <see cref="UiWidgets.SelectableRows"/> to record this frame, each with
/// <see cref="Row"/>, and what the user asked of them.</summary>
public readonly struct SelectableRowsScope<TKey> : IDisposable where TKey : notnull
{
    private readonly Ui _ui;
    private readonly Ui.Scope _scope;
    private readonly int _id;
    private readonly IReadOnlyList<TKey> _order;
    private readonly MultiSelection<TKey> _selection;

    internal SelectableRowsScope(Ui ui, Ui.Scope scope, int id, IReadOnlyList<TKey> order, MultiSelection<TKey> selection, bool focused,
        bool removeRequested)
    {
        _ui = ui;
        _scope = scope;
        _id = id;
        _order = order;
        _selection = selection;
        Focused = focused;
        RemoveRequested = removeRequested;
    }

    /// <summary>Whether the rows held keyboard focus when they were opened.</summary>
    public bool Focused { get; }

    /// <summary>Delete or Backspace was pressed while the rows held focus: remove the selected ones.</summary>
    public bool RemoveRequested { get; }

    /// <summary>
    /// Opens the row for <paramref name="key"/> as a horizontal element <paramref name="id"/> laid out
    /// by <paramref name="box"/>, its background showing whether it is selected, the rows focused and
    /// the row hovered. A press on the row itself — not on a control in it — focuses the rows and
    /// selects it alone, toggles it with Ctrl, or selects the range from the anchor with Shift.
    /// </summary>
    public Ui.Scope Row(TKey key, int id, Box box = default)
    {
        var row = _ui.Interact(id);
        if (row.Pressed)
            Press(key);
        var selected = _selection.Contains(key);
        var background = _ui.Theme.PickRowBackground(selected, Focused, row.Hovered);
        return _ui.Row(id, box with { Background = background });
    }

    private void Press(TKey key)
    {
        _ui.Focus(_id, takesText: false);
        var change = ChangeFor(_ui.Input.Modifiers);
        _selection.Select(key, change, _order);
    }

    private static SelectionChange ChangeFor(KeyModifiers modifiers)
    {
        if ((modifiers & (KeyModifiers.Control | KeyModifiers.Super)) != 0)
            return SelectionChange.Toggle;
        if ((modifiers & KeyModifiers.Shift) != 0)
            return SelectionChange.Extend;
        return SelectionChange.Replace;
    }

    public void Dispose() => _scope.Dispose();
}

public static partial class UiWidgets
{
    /// <summary>
    /// A column <paramref name="id"/> of rows, one per key of <paramref name="order"/>, any number of
    /// which <paramref name="selection"/> selects. It does not scroll, so it can sit in a page that
    /// does. It is a Tab stop; while it holds focus, Up and Down move the selection, with Shift
    /// extending it, Home and End go to either end, Ctrl+A selects every row, and Delete or Backspace
    /// asks for the selected rows to be removed. Keys gone from <paramref name="order"/> leave the
    /// selection. Record each row with <see cref="SelectableRowsScope{TKey}.Row"/>.
    /// </summary>
    public static SelectableRowsScope<TKey> SelectableRows<TKey>(this Ui ui, int id, IReadOnlyList<TKey> order, MultiSelection<TKey> selection,
        Box box = default) where TKey : notnull
    {
        selection.Reconcile(order);
        var focused = ui.ListArea(id);
        var removeRequested = false;
        if (focused)
            removeRequested = HandleRowKeys(ui, order, selection);
        var scope = ui.Column(id, box with { Gap = box.Gap ?? 0f });
        return new SelectableRowsScope<TKey>(ui, scope, id, order, selection, focused, removeRequested);
    }

    /// <summary>Applies the row keys to <paramref name="selection"/>; returns whether removal was asked for.</summary>
    private static bool HandleRowKeys<TKey>(Ui ui, IReadOnlyList<TKey> order, MultiSelection<TKey> selection) where TKey : notnull
    {
        var removeRequested = false;
        foreach (var stroke in ui.Input.Keys)
        {
            var extend = (stroke.Modifiers & KeyModifiers.Shift) != 0;
            var command = (stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Super)) != 0;
            if (command && stroke.Key == KeyboardKey.A)
                selection.SelectAll(order);
            if (command)
                continue;
            switch (stroke.Key)
            {
                case KeyboardKey.UpArrow:
                    selection.Move(-1, extend, order);
                    break;
                case KeyboardKey.DownArrow:
                    selection.Move(1, extend, order);
                    break;
                case KeyboardKey.Home:
                    selection.MoveTo(0, extend, order);
                    break;
                case KeyboardKey.End:
                    selection.MoveTo(order.Count - 1, extend, order);
                    break;
                case KeyboardKey.Delete:
                case KeyboardKey.Backspace:
                    if (!stroke.IsRepeat)
                        removeRequested = true;
                    break;
            }
        }
        return removeRequested;
    }
}
