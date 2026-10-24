using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>The selected row of a <see cref="UiWidgets.SelectableList"/>, owned by the app: an index
/// into its rows, or -1. Selecting asks the list to scroll the row into view the next time it is
/// recorded.</summary>
public sealed class ListSelection
{
    private bool _reveal;

    public int Index { get; private set; } = -1;

    public bool HasSelection => Index >= 0;

    /// <summary>Selects row <paramref name="index"/> of <paramref name="count"/> and, unless told
    /// otherwise, scrolls it into view; an index out of range clears the selection. Not revealing
    /// keeps a reveal already asked for, which then shows the row selected now.</summary>
    public void Select(int index, int count, bool reveal = true)
    {
        if (!IsRow(index, count))
        {
            Clear();
            return;
        }
        Index = index;
        if (reveal)
            _reveal = true;
    }

    /// <summary>Selects the row <paramref name="by"/> rows on from the selected one, kept within
    /// <paramref name="count"/>; with nothing selected, a move back selects the first.</summary>
    public void Move(int by, int count)
    {
        if (count == 0) return;
        var target = Index + by;
        if (Index < 0 && by < 0)
            target = 0;
        Select(Math.Clamp(target, 0, count - 1), count);
    }

    public void Clear()
    {
        Index = -1;
        _reveal = false;
    }

    /// <summary>Scrolls the selected row into view the next time the list is recorded.</summary>
    public void Reveal() => _reveal = HasSelection;

    internal bool TakeReveal()
    {
        var reveal = _reveal;
        _reveal = false;
        return reveal;
    }

    private static bool IsRow(int index, int count) => index >= 0 && index < count;
}

/// <summary>The rows of a <see cref="UiWidgets.SelectableList"/> to record this frame, from
/// <see cref="First"/> up to but not including <see cref="End"/>, each with <see cref="Row"/>, and
/// what the user asked of the list so far this frame.</summary>
public readonly struct ListScope : IDisposable
{
    private readonly Ui _ui;
    private readonly UiWidgets.ListFrame _frame;
    private readonly Ui.VirtualList _rows;

    internal ListScope(Ui ui, UiWidgets.ListFrame frame, Ui.VirtualList rows)
    {
        _ui = ui;
        _frame = frame;
        _rows = rows;
    }

    public int First => _rows.First;
    public int End => _rows.End;

    /// <summary>Whether the list held keyboard focus when it was opened.</summary>
    public bool Focused => _frame.Focused;

    /// <summary>The selected row, or -1.</summary>
    public int Index => _frame.Selection.Index;

    /// <summary>Enter, or a double-click on a row, asked to act on the selected row.</summary>
    public bool Activated => _frame.Activated && _frame.Selection.HasSelection;

    /// <summary>A right-click on a row, or the Menu key, asked for a context menu at
    /// <see cref="ContextMenuAt"/> for the selected row.</summary>
    public bool ContextMenuRequested => _frame.ContextMenu && _frame.Selection.HasSelection;

    public PointF ContextMenuAt => _frame.MenuAt;

    /// <summary>
    /// Opens row <paramref name="index"/> as a horizontal element <paramref name="id"/>, as tall as the
    /// list's rows and inset by <see cref="UiTheme.ShellPaddingX"/> unless <paramref name="box"/> says
    /// otherwise, its background showing whether it is selected, the list focused and the row
    /// <paramref name="hovered"/> or under the pointer. A press on it selects it, or a click with the
    /// list's <c>selectOnClick</c>; a double-click activates it; a right-click selects it and asks for
    /// a context menu. A row of separately interacting cells passes an <paramref name="id"/> of 0, has
    /// no pointer behavior of its own and leaves showing focus to its cells.
    /// </summary>
    public ListRow Row(int index, int id, Box box = default, bool hovered = false)
    {
        var row = Interaction.Inert(0);
        if (id != 0)
            row = _ui.Interact(id);
        HandlePointer(index, row);

        var selected = index == _frame.Selection.Index;
        if (selected)
            _frame.SelectedRow = id;
        var scope = _ui.Row(id, RowBox(box, selected, row.Hovered || hovered));
        var ringed = selected && id != 0 && _frame.Focused && _ui.FocusVisible;
        if (ringed)
            _ui.FocusRing(0f);
        return new ListRow(scope, row, selected, _frame.Focused);
    }

    private void HandlePointer(int index, Interaction row)
    {
        var menu = row.ClickedWith(PointerButton.Right);
        if (row.Pressed || menu)
            _ui.Focus(_frame.Id, takesText: false);
        var picked = _frame.SelectOnClick ? row.Clicked : row.Pressed;
        if (picked || menu)
            SelectRow(index);
        if (row.DoubleClicked)
            _frame.Activated = true;
        if (menu)
            RequestMenuAtPointer();
    }

    private void SelectRow(int index)
    {
        _frame.Selection.Select(index, _frame.Count, reveal: false);
        _ui.ScrollRowIntoView(_frame.Id, index);
    }

    private void RequestMenuAtPointer()
    {
        _frame.ContextMenu = true;
        _frame.MenuAt = _ui.Input.Pointer;
    }

    private Box RowBox(Box box, bool selected, bool hovered) =>
        UiWidgets.ListRowBox(_ui.Theme, box, _frame.RowHeight, selected, _frame.Focused, hovered);

    public void Dispose() => _rows.Dispose();
}

/// <summary>The rows of a <see cref="UiWidgets.SelectableList{TKey}"/> to record this frame, from
/// <see cref="First"/> up to but not including <see cref="End"/>, each with <see cref="Row"/>, and
/// what the user asked of the list so far this frame.</summary>
public readonly struct ListScope<TKey> : IDisposable where TKey : notnull
{
    private readonly Ui _ui;
    private readonly UiWidgets.MultiListFrame<TKey> _frame;
    private readonly Ui.VirtualList _rows;

    internal ListScope(Ui ui, UiWidgets.MultiListFrame<TKey> frame, Ui.VirtualList rows)
    {
        _ui = ui;
        _frame = frame;
        _rows = rows;
    }

    public int First => _rows.First;
    public int End => _rows.End;

    /// <summary>Whether the list held keyboard focus when it was opened.</summary>
    public bool Focused => _frame.Focused;

    /// <summary>Enter, or a double-click on a row, asked to act on the picked-out rows.</summary>
    public bool Activated => _frame.Activated && _frame.Selection.Count > 0;

    /// <summary>
    /// Opens row <paramref name="index"/> of the order as a horizontal element <paramref name="id"/>,
    /// as tall as the list's rows and inset by <see cref="UiTheme.ShellPaddingX"/> unless
    /// <paramref name="box"/> says otherwise, its background showing whether it is picked out, the
    /// list focused and the row under the pointer. A press on it picks it out alone, toggles it with
    /// Ctrl, or picks out the range from the anchor with Shift; a double-click activates it.
    /// </summary>
    public ListRow Row(int index, int id, Box box = default)
    {
        var row = _ui.Interact(id);
        HandlePointer(index, row);

        var selected = _frame.Selection.Contains(_frame.Order[index]);
        var scope = _ui.Row(id, UiWidgets.ListRowBox(_ui.Theme, box, _frame.RowHeight, selected, _frame.Focused, row.Hovered));
        var ringed = IsCurrent(index) && _frame.Focused && _ui.FocusVisible;
        if (ringed)
            _ui.FocusRing(0f);
        return new ListRow(scope, row, selected, _frame.Focused);
    }

    private void HandlePointer(int index, Interaction row)
    {
        if (row.Pressed)
            PickRow(index);
        if (row.DoubleClicked)
            _frame.Activated = true;
    }

    private void PickRow(int index)
    {
        _ui.Focus(_frame.Id, takesText: false);
        var change = UiWidgets.PickChange(_ui.Input.Modifiers);
        _frame.Selection.Select(_frame.Order[index], change, _frame.Order);
        _ui.ScrollRowIntoView(_frame.Id, index);
    }

    private bool IsCurrent(int index) => index == _frame.CurrentIndex;

    public void Dispose() => _rows.Dispose();
}

/// <summary>A row a <see cref="ListScope.Row"/> opened; disposing it closes it.</summary>
public readonly struct ListRow : IDisposable
{
    private readonly Ui.Scope _scope;

    internal ListRow(Ui.Scope scope, Interaction interaction, bool selected, bool focused)
    {
        _scope = scope;
        Interaction = interaction;
        Selected = selected;
        Focused = focused;
    }

    /// <summary>The pointer on the row itself; inert for a row of cells.</summary>
    public Interaction Interaction { get; }

    public bool Selected { get; }

    /// <summary>Whether the row's list holds keyboard focus.</summary>
    public bool Focused { get; }

    public void Dispose() => _scope.Dispose();
}

public static partial class UiWidgets
{
    private const int ListSalt = 0x4C495354;
    private const int MultiListSalt = 0x4D4C5354;
    private const int UnmeasuredListPage = 10;

    internal sealed class ListFrame
    {
        public int Id;
        public int Count;
        public float RowHeight;
        public bool SelectOnClick;
        public bool Focused;
        public bool Activated;
        public bool ContextMenu;
        public PointF MenuAt;
        public int SelectedRow;
        public ListSelection Selection = new();

        public void Begin(int id, int count, float rowHeight, ListSelection selection, bool selectOnClick)
        {
            Id = id;
            Count = count;
            RowHeight = rowHeight;
            Selection = selection;
            SelectOnClick = selectOnClick;
            Activated = false;
            ContextMenu = false;
            MenuAt = default;
        }
    }

    /// <summary>
    /// A <see cref="Ui.VirtualScroll(int, int, float, Box)"/> of <paramref name="count"/> rows
    /// <paramref name="rowHeight"/> tall, the theme's by default, as a <see cref="ListArea"/>, one row
    /// of which <paramref name="selection"/> selects. While focused, and unless <paramref name="keys"/>
    /// is false, <see cref="ApplyListKey"/> moves the selection, Enter activates it, and the Menu key or
    /// Shift+F10 asks for a context menu under it. Record each row in view with
    /// <see cref="ListScope.Row"/>. The selection scrolls into view whenever it is selected, and is
    /// cleared when past the last row.
    /// </summary>
    public static ListScope SelectableList(this Ui ui, int id, int count, ListSelection selection, float? rowHeight = null, Box box = default,
        bool keys = true, bool selectOnClick = false)
    {
        var height = rowHeight ?? ui.Theme.RowHeight;
        if (selection.Index >= count)
            selection.Clear();
        var frame = ui.GetState<ListFrame>(HashCode.Combine(id, ListSalt));
        frame.Begin(id, count, height, selection, selectOnClick);
        frame.Focused = ui.ListArea(id);
        if (frame.Focused && keys)
            HandleListKeys(ui, frame, ui.MeasureListPage(id, height));
        frame.SelectedRow = 0;
        if (selection.TakeReveal())
            ui.ScrollRowIntoView(id, selection.Index);
        var rows = ui.VirtualScroll(id, count, height, box);
        return new ListScope(ui, frame, rows);
    }

    internal sealed class MultiListFrame<TKey> where TKey : notnull
    {
        public int Id;
        public IReadOnlyList<TKey> Order = Array.Empty<TKey>();
        public float RowHeight;
        public bool Focused;
        public bool Activated;
        public int CurrentIndex;
        public MultiSelection<TKey> Selection = new();

        public void Begin(int id, IReadOnlyList<TKey> order, float rowHeight, MultiSelection<TKey> selection)
        {
            Id = id;
            Order = order;
            RowHeight = rowHeight;
            Selection = selection;
            Activated = false;
        }
    }

    /// <summary>
    /// A <see cref="Ui.VirtualScroll(int, int, float, Box)"/> of the rows of <paramref name="order"/>,
    /// one per key, <paramref name="rowHeight"/> tall, the theme's by default, as a
    /// <see cref="ListArea"/>, any number of which <paramref name="selection"/> picks out. While
    /// focused, <see cref="ApplyListKey{TKey}"/> moves the current row, Ctrl+A picks out every row and
    /// Enter activates them. Record each row in view with <see cref="ListScope{TKey}.Row"/>. The
    /// current row scrolls into view whenever a key moves it. Keys gone from <paramref name="order"/>
    /// are the owner's to drop with <see cref="MultiSelection{TKey}.Reconcile"/>.
    /// </summary>
    public static ListScope<TKey> SelectableList<TKey>(this Ui ui, int id, IReadOnlyList<TKey> order, MultiSelection<TKey> selection,
        float? rowHeight = null, Box box = default) where TKey : notnull
    {
        var height = rowHeight ?? ui.Theme.RowHeight;
        var frame = ui.GetState<MultiListFrame<TKey>>(HashCode.Combine(id, MultiListSalt));
        frame.Begin(id, order, height, selection);
        frame.Focused = ui.ListArea(id);
        if (frame.Focused)
            HandleMultiListKeys(ui, frame, ui.MeasureListPage(id, height));
        frame.CurrentIndex = selection.IndexOfCurrent(order);
        if (selection.TakeReveal())
            ui.ScrollRowIntoView(id, frame.CurrentIndex);
        var rows = ui.VirtualScroll(id, order.Count, height, box);
        return new ListScope<TKey>(ui, frame, rows);
    }

    private static void HandleMultiListKeys<TKey>(Ui ui, MultiListFrame<TKey> frame, int page) where TKey : notnull
    {
        foreach (var stroke in ui.Input.Keys)
        {
            if (ApplyListKey(stroke, frame.Selection, frame.Order, page)) continue;
            if (IsSelectAll(stroke))
            {
                frame.Selection.SelectAll(frame.Order);
                continue;
            }
            if (HasCommandModifier(stroke)) continue;
            if (stroke.Key == KeyboardKey.Enter)
                frame.Activated = true;
        }
    }

    /// <summary>Moves the current row of <paramref name="selection"/> among <paramref name="order"/>
    /// for <paramref name="stroke"/> as <see cref="ApplyListKey(KeyStroke, ref int, int, int)"/> moves
    /// an index, picking it out alone or, with Shift, the range from the anchor to it. Returns whether
    /// it was one of those keys.</summary>
    public static bool ApplyListKey<TKey>(KeyStroke stroke, MultiSelection<TKey> selection, IReadOnlyList<TKey> order, int page)
        where TKey : notnull
    {
        var index = selection.IndexOfCurrent(order);
        if (!ApplyListKey(stroke, ref index, order.Count, page)) return false;
        var extend = (stroke.Modifiers & KeyModifiers.Shift) != 0;
        selection.MoveTo(index, extend, order);
        return true;
    }

    private static bool IsSelectAll(KeyStroke stroke) => stroke.Key == KeyboardKey.A && IsCommand(stroke.Modifiers);

    /// <summary>Makes the element <paramref name="id"/> a list that takes keyboard focus but no
    /// text: a press or a right-click on it focuses it, and it is a Tab stop. Returns whether it
    /// holds focus.</summary>
    public static bool ListArea(this Ui ui, int id)
    {
        var area = ui.Interact(id);
        if (area.Pressed || area.ClickedWith(PointerButton.Right))
            ui.Focus(id, takesText: false);
        return ui.TabStop(id);
    }

    private static void HandleListKeys(Ui ui, ListFrame frame, int page)
    {
        var index = frame.Selection.Index;
        foreach (var stroke in ui.Input.Keys)
        {
            if (ApplyListKey(stroke, ref index, frame.Count, page)) continue;
            if (HasCommandModifier(stroke)) continue;
            if (stroke.Key == KeyboardKey.Enter)
                frame.Activated = true;
            if (IsMenuKey(stroke))
            {
                frame.ContextMenu = true;
                frame.MenuAt = MenuPointUnder(ui, frame.SelectedRow);
            }
        }
        if (index != frame.Selection.Index)
            frame.Selection.Select(index, frame.Count);
    }

    /// <summary>Moves <paramref name="index"/> among <paramref name="count"/> rows for
    /// <paramref name="stroke"/>: Up and Down by one, PageUp and PageDown by <paramref name="page"/>,
    /// Home and End, also with Ctrl, to either end; Up or Down with nothing selected selects the
    /// first. Returns whether it was one of those keys. Strokes with Ctrl, Alt or Super are
    /// otherwise left alone.</summary>
    public static bool ApplyListKey(KeyStroke stroke, ref int index, int count, int page)
    {
        if (count == 0) return false;
        if (!TakesListModifiers(stroke)) return false;
        var moved = MovedIndex(stroke.Key, index, count, page);
        if (moved == null) return false;
        index = Math.Clamp(moved.Value, 0, count - 1);
        return true;
    }

    private static bool TakesListModifiers(KeyStroke stroke)
    {
        var modifiers = stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Super);
        if (modifiers == 0) return true;
        var toEnd = stroke.Key is KeyboardKey.Home or KeyboardKey.End;
        return toEnd && modifiers == KeyModifiers.Control;
    }

    private static int? MovedIndex(KeyboardKey key, int index, int count, int page) => key switch
    {
        KeyboardKey.UpArrow => index - 1,
        KeyboardKey.DownArrow => index + 1,
        KeyboardKey.PageUp => index - page,
        KeyboardKey.PageDown => Math.Max(0, index) + page,
        KeyboardKey.Home => 0,
        KeyboardKey.End => count - 1,
        _ => null,
    };

    private static bool HasCommandModifier(KeyStroke stroke) =>
        (stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Super)) != 0;

    private static bool IsMenuKey(KeyStroke stroke)
    {
        if (stroke.Key == KeyboardKey.Menu) return true;
        return stroke.Key == KeyboardKey.F10 && (stroke.Modifiers & KeyModifiers.Shift) != 0;
    }

    /// <summary>Where a context menu asked for by key opens: under the start of the selected row's
    /// element <paramref name="rowId"/>, else at the pointer.</summary>
    private static PointF MenuPointUnder(Ui ui, int rowId)
    {
        if (rowId == 0) return ui.Input.Pointer;
        if (!ui.TryGetRect(rowId, out var row)) return ui.Input.Pointer;
        return new PointF(row.X + row.Height, row.Bottom);
    }

    /// <summary>How many rows <paramref name="rowHeight"/> tall PageUp and PageDown move through the
    /// list <paramref name="id"/>: one fewer than it showed, at least one.</summary>
    public static int MeasureListPage(this Ui ui, int id, float rowHeight)
    {
        if (!ui.TryGetRect(id, out var view)) return UnmeasuredListPage;
        var shown = (int)(view.Height / rowHeight);
        return Math.Max(1, shown - 1);
    }

    /// <summary>A list row <paramref name="rowHeight"/> tall and inset by
    /// <see cref="UiTheme.ShellPaddingX"/> unless <paramref name="box"/> says otherwise, with its
    /// <see cref="PickRowBackground"/>.</summary>
    internal static Box ListRowBox(UiTheme theme, Box box, float rowHeight, bool selected, bool focused, bool hovered) => box with
    {
        Height = box.Height ?? rowHeight,
        Padding = box.Padding ?? new Spacing(theme.ShellPaddingX, 0f),
        Background = theme.PickRowBackground(selected, focused, hovered),
    };

    /// <summary>A list row's background: <see cref="UiTheme.Selection"/> while selected in a focused
    /// list, <see cref="UiTheme.SelectionInactive"/> while selected in another, else
    /// <see cref="UiTheme.RowHover"/> while hovered.</summary>
    public static uint PickRowBackground(this UiTheme theme, bool selected, bool focused, bool hovered)
    {
        if (selected && focused) return theme.Selection;
        if (selected) return theme.SelectionInactive;
        if (hovered) return theme.RowHover;
        return 0u;
    }
}
