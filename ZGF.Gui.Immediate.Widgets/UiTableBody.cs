using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>A table opened by <see cref="UiWidgets.Table(Ui, int, TableColumns, ListSelection, TableScroll?)"/>:
/// record its <see cref="Header"/>, then its <see cref="Body"/>.</summary>
public readonly struct TableFrame
{
    private readonly Ui _ui;
    private readonly UiWidgets.TableState _state;

    internal TableFrame(Ui ui, UiWidgets.TableState state)
    {
        _ui = ui;
        _state = state;
    }

    /// <summary>Whether the table held keyboard focus when it was opened.</summary>
    public bool Focused => _state.Focused;

    /// <summary>Records the header row, each column labelled from <paramref name="labels"/>. With a
    /// <paramref name="sort"/>, returns the column whose header was clicked, else -1.</summary>
    public int Header(ReadOnlySpan<string> labels, TableSort? sort = null) =>
        UiWidgets.TableHeader<int>(_ui, _state, labels, sort, null, 0);

    /// <summary>A <see cref="Header(ReadOnlySpan{string}, TableSort?)"/> whose <paramref name="heading"/>
    /// records each cell's content instead of its label, handed <paramref name="state"/>.</summary>
    public int Header<TState>(ReadOnlySpan<string> labels, TableSort? sort, TableHeading<TState> heading, in TState state) =>
        UiWidgets.TableHeader(_ui, _state, labels, sort, heading, state);

    /// <summary>
    /// The scroll of <paramref name="count"/> rows, the theme's row height tall but for those in
    /// <paramref name="tall"/>, of which only those in view are recorded, each with
    /// <see cref="TableBody.Row"/>. While focused, and unless <paramref name="keys"/> is false, the
    /// table takes the keys it acts on, so nothing after it sees them: Up, Down, PageUp, PageDown,
    /// Home and End move the selection; Enter activates it, Ctrl+Enter in a new tab; the Menu key or
    /// Shift+F10 asks for a context menu under it; and a table of cells takes its cell keys. The
    /// selection scrolls into view whenever it is selected, and is cleared when past the last row.
    /// </summary>
    public TableBody Body(int count, bool keys = true, IReadOnlyList<TallRow>? tall = null) =>
        UiWidgets.TableBody(_ui, _state, count, keys, tall);

    /// <summary>Takes the keys the <see cref="Body"/> of <paramref name="count"/> rows acts on now
    /// rather than when it opens, for an app that sizes its rows by the selection they move.</summary>
    public void TakeKeys(int count) => UiWidgets.TakeTableKeys(_ui, _state, count);
}

/// <summary>The rows of a table to record this frame, from <see cref="First"/> up to but not
/// including <see cref="End"/>, each with <see cref="Row"/>, and what the user asked of the table.
/// Read what was asked after recording the rows, which take the pointer.</summary>
public readonly struct TableBody : IDisposable
{
    private readonly Ui _ui;
    private readonly UiWidgets.TableState _state;
    private readonly Ui.VirtualList _rows;

    internal TableBody(Ui ui, UiWidgets.TableState state, Ui.VirtualList rows)
    {
        _ui = ui;
        _state = state;
        _rows = rows;
    }

    public int First => _rows.First;
    public int End => _rows.End;

    /// <summary>Whether the table held keyboard focus when it was opened.</summary>
    public bool Focused => _state.Focused;

    /// <summary>The selected row, or -1.</summary>
    public int Index => _state.Rows.Index;

    /// <summary>Enter, or a double-click or middle-click on a row, asked to act on the selected row.</summary>
    public bool Activated => _state.Activated && _state.Rows.HasSelection;

    /// <summary>Whether the activation asked for a new tab: Ctrl+Enter or a middle-click.</summary>
    public bool InNewTab => _state.InNewTab;

    /// <summary>A right-click on a row, or the Menu key, asked for a context menu at
    /// <see cref="ContextMenuAt"/> for the selected row.</summary>
    public bool ContextMenuRequested => _state.ContextMenu && _state.Rows.HasSelection;

    public PointF ContextMenuAt => _state.MenuAt;

    /// <summary>Ctrl+C asked a table of cells to copy the selected row's current cell.</summary>
    public bool CopyRequested => _state.Copy && _state.Rows.HasSelection;

    /// <summary>The height rows show in as last laid out; zero until laid out once.</summary>
    public float Viewport => _rows.Viewport;

    /// <summary>How far the rows are scrolled, in points from the top of their content.</summary>
    public float Offset => _rows.Offset;

    /// <summary>Where row <paramref name="index"/> starts, in points from the top of the content.</summary>
    public float TopOf(int index) => _rows.TopOf(index);

    /// <summary>Scrolls to <paramref name="offset"/> points from the top of the content, from the next frame.</summary>
    public void ScrollTo(float offset) => _rows.ScrollTo(offset);

    /// <summary>
    /// Opens row <paramref name="index"/>, whose item <paramref name="key"/> names so its ids and
    /// state follow the item rather than the index, as tall as the table's rows unless
    /// <paramref name="height"/> says otherwise, its background showing whether it is selected, the
    /// table focused and the row under the pointer. A press on it selects it; a double-click or a
    /// middle-click activates it; a right-click selects it and asks for a context menu. In a table of
    /// cells each cell takes the pointer instead, and a press makes a selectable one current.
    /// </summary>
    public TableRow Row(int index, int key, float? height = null) => UiWidgets.TableRow(_ui, _state, index, key, height);

    public void Dispose() => _rows.Dispose();
}

/// <summary>A row <see cref="TableBody.Row"/> opened; record its cells with <see cref="Cell"/> or
/// <see cref="TextCell"/>, in column order. Disposing it closes it.</summary>
public readonly struct TableRow : IDisposable
{
    private readonly Ui _ui;
    private readonly UiWidgets.TableState _state;
    private readonly Ui.Scope _ids;
    private readonly Ui.Scope _content;
    private readonly int _key;

    internal TableRow(Ui ui, UiWidgets.TableState state, Ui.Scope ids, Ui.Scope content, int key, Interaction interaction, bool selected)
    {
        _ui = ui;
        _state = state;
        _ids = ids;
        _content = content;
        _key = key;
        Interaction = interaction;
        Selected = selected;
    }

    /// <summary>The row's element id, for a tooltip on it; 0 in a table of cells.</summary>
    public int Id => Interaction.Id;

    /// <summary>The pointer on the row itself; inert in a table of cells.</summary>
    public Interaction Interaction { get; }

    public bool Selected { get; }

    /// <summary>Whether the row's table holds keyboard focus.</summary>
    public bool Focused => _state.Focused;

    /// <summary>The pointer on the cell in <paramref name="column"/>; inert in a table without cells.</summary>
    public Interaction CellAt(int column)
    {
        if (_state.Cells == null) return Interaction.Inert(0);
        return _state.CellHits[column];
    }

    /// <summary>Opens the cell in <paramref name="column"/> for content of the app's own.</summary>
    public Ui.Scope Cell(int column)
    {
        if (_state.Cells == null) return _ui.TableCell(_state.Columns, column);
        return _ui.TableCell(_state.Columns, column, CellAt(column).Id);
    }

    /// <summary>
    /// Records the cell in <paramref name="column"/> showing <paramref name="shown"/>: in a table of
    /// cells as text the user can select, with a <paramref name="link"/> icon that
    /// <paramref name="linkTooltip"/> names, then <paramref name="suffix"/> small and muted, or every
    /// line of it while <paramref name="expanded"/>; the current cell is outlined. In a table without
    /// cells as plain text, then <paramref name="suffix"/>. Returns true on the frame the link is followed.
    /// </summary>
    public bool TextCell(int column, string shown, uint? color = null, float? size = null, string? family = null,
        TextOverflow overflow = TextOverflow.Ellipsis, UiIcon? link = null, string linkTooltip = "", string? suffix = null, bool expanded = false)
    {
        var cells = _state.Cells;
        if (cells == null)
        {
            PlainCell(column, shown, color, size, family, overflow, suffix);
            return false;
        }
        var current = Selected && cells.Column == column;
        var cell = new CellText(shown, color, size, family, overflow, link, linkTooltip, suffix, expanded);
        return _ui.TableCellText(_state.Columns, column, CellAt(column), _key, cells, current, _state.Focused, cell);
    }

    private void PlainCell(int column, string shown, uint? color, float? size, string? family, TextOverflow overflow, string? suffix)
    {
        var theme = _ui.Theme;
        using (_ui.TableCell(_state.Columns, column))
        {
            _ui.Text(shown, new Box { Width = Sizing.Grow() }, color: color ?? theme.Text, size: size, family: family, overflow: overflow);
            if (suffix != null)
                _ui.Text(suffix, new Box { Width = Sizing.Fit() }, color: theme.TextMuted, size: theme.FontSizeSmall);
        }
    }

    public void Dispose()
    {
        _content.Dispose();
        _ids.Dispose();
    }
}

public static partial class UiWidgets
{
    private const int TableRowSalt = 0x54425257;
    private const int TableCellSalt = 0x54424345;

    /// <summary>What a table keeps between frames, and what the user asked of it this frame.</summary>
    internal sealed class TableState
    {
        public int Id;
        public TableColumns Columns = new();
        public ListSelection Rows = new();
        public TableCellSelection? Cells;
        public TableScroll Scroll = new();
        public float RowHeight;
        public bool Focused;
        public int Count;
        public bool KeysTaken;
        public bool Activated;
        public bool InNewTab;
        public bool ContextMenu;
        public bool Copy;
        public PointF MenuAt;
        public int SelectedRowId;
        public Interaction[] CellHits = [];

        private readonly TableScroll _ownScroll = new();

        public void Begin(int id, TableColumns columns, ListSelection rows, TableCellSelection? cells, TableScroll? scroll, float rowHeight)
        {
            Id = id;
            Columns = columns;
            Rows = rows;
            Cells = cells;
            Scroll = scroll ?? _ownScroll;
            RowHeight = rowHeight;
            if (CellHits.Length != columns.Count)
                CellHits = new Interaction[columns.Count];
            KeysTaken = false;
            Activated = false;
            InNewTab = false;
            ContextMenu = false;
            Copy = false;
            MenuAt = default;
        }

        public void Activate(bool inNewTab)
        {
            Activated = true;
            InNewTab = inNewTab;
        }
    }

    internal static TableBody TableBody(Ui ui, TableState state, int count, bool keys, IReadOnlyList<TallRow>? tall)
    {
        if (keys)
            TakeTableKeys(ui, state, count);
        state.Count = count;
        if (state.Rows.Index >= count)
            state.Rows.Clear();
        state.SelectedRowId = 0;
        if (state.Rows.TakeReveal())
            ui.ScrollRowIntoView(state.Id, state.Rows.Index);
        var rows = ScrollTableRows(ui, state, count, tall);
        return new TableBody(ui, state, rows);
    }

    private static Ui.VirtualList ScrollTableRows(Ui ui, TableState state, int count, IReadOnlyList<TallRow>? tall)
    {
        var scroll = state.Scroll;
        if (state.Columns.IsSized)
            return ui.VirtualScroll(state.Id, count, state.RowHeight, ref scroll.X, ref scroll.Y, default, tall);
        return ui.VirtualScroll(state.Id, count, state.RowHeight, ref scroll.Y, default, tall);
    }

    /// <summary>Takes the keys the table acts on, once a frame, while it has focus.</summary>
    internal static void TakeTableKeys(Ui ui, TableState state, int count)
    {
        if (state.KeysTaken) return;
        state.KeysTaken = true;
        state.Count = count;
        if (state.Rows.Index >= count)
            state.Rows.Clear();
        if (!state.Focused) return;
        ApplyTableKeys(ui, state, ui.MeasureListPage(state.Id, state.RowHeight));
    }

    private static void ApplyTableKeys(Ui ui, TableState state, int page)
    {
        var index = state.Rows.Index;
        var keys = ui.Input.Keys;
        var i = 0;
        while (i < keys.Count)
        {
            if (ApplyTableKey(ui, state, keys[i], ref index, page))
                ui.Input.TakeKeyAt(i);
            else
                i++;
        }
        if (index != state.Rows.Index)
            MoveTableSelection(state, index);
    }

    private static bool ApplyTableKey(Ui ui, TableState state, KeyStroke stroke, ref int index, int page)
    {
        if (ApplyListKey(stroke, ref index, state.Count, page)) return true;
        if (index < 0) return false;
        if (state.Cells != null && ApplyCellKey(state, state.Cells, stroke)) return true;
        if (IsActivateKey(stroke))
        {
            state.Activate((stroke.Modifiers & KeyModifiers.Control) != 0);
            return true;
        }
        if (!IsMenuKey(stroke)) return false;
        state.ContextMenu = true;
        state.MenuAt = MenuPointUnder(ui, state.SelectedRowId);
        return true;
    }

    private static bool ApplyCellKey(TableState state, TableCellSelection cells, KeyStroke stroke)
    {
        var modifiers = stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Super);
        switch (modifiers, stroke.Key)
        {
            case (KeyModifiers.Control, KeyboardKey.C):
                state.Copy = true;
                return true;
            case (0, KeyboardKey.LeftArrow):
                cells.SelectColumn(state.Columns.StepSelectable(cells.Column, -1));
                return true;
            case (0, KeyboardKey.RightArrow):
                cells.SelectColumn(state.Columns.StepSelectable(cells.Column, 1));
                return true;
            case (0, KeyboardKey.Escape):
                return cells.CollapseText();
            default:
                return false;
        }
    }

    private static bool IsActivateKey(KeyStroke stroke) =>
        stroke.Key == KeyboardKey.Enter && (stroke.Modifiers & (KeyModifiers.Alt | KeyModifiers.Super)) == 0;

    /// <summary>Selects row <paramref name="index"/> moved to by a key, in a table of cells keeping
    /// the current column, or picking the first selectable one when there is none.</summary>
    private static void MoveTableSelection(TableState state, int index)
    {
        state.Rows.Select(index, state.Count);
        var cells = state.Cells;
        if (cells == null) return;
        if (cells.Column >= 0) return;
        cells.SelectColumn(state.Columns.StepSelectable(-1, 1));
    }

    internal static TableRow TableRow(Ui ui, TableState state, int index, int key, float? height)
    {
        var row = Interaction.Inert(0);
        bool hovered;
        if (state.Cells == null)
        {
            row = ui.Interact(HashCode.Combine(state.Id, TableRowSalt, key));
            HandleTableRowPointer(ui, state, index, row);
            hovered = row.Hovered;
        }
        else
        {
            hovered = InteractCells(ui, state, index, key);
        }

        var selected = index == state.Rows.Index;
        if (selected)
            state.SelectedRowId = SelectedRowElement(state, row);
        var ids = ui.Id(key);
        var content = ui.Row(row.Id, TableRowBox(ui, state, height, selected, hovered));
        var ringed = selected && row.Id != 0 && state.Focused && ui.FocusVisible;
        if (ringed)
            ui.FocusRing(0f);
        return new TableRow(ui, state, ids, content, key, row, selected);
    }

    private static void HandleTableRowPointer(Ui ui, TableState state, int index, Interaction row)
    {
        var menu = row.ClickedWith(PointerButton.Right);
        var middle = row.ClickedWith(PointerButton.Middle);
        if (row.Pressed || menu || middle)
            PickTableRow(ui, state, index);
        if (row.DoubleClicked)
            state.Activate(false);
        if (middle)
            state.Activate(true);
        if (menu)
            RequestTableMenuAtPointer(ui, state);
    }

    /// <summary>Interacts with each cell of the row with <paramref name="key"/> at
    /// <paramref name="index"/>; returns whether the pointer is over one of them, or over the icon
    /// of a link in one.</summary>
    private static bool InteractCells(Ui ui, TableState state, int index, int key)
    {
        var hovered = false;
        for (var c = 0; c < state.CellHits.Length; c++)
        {
            var cell = ui.Interact(HashCode.Combine(state.Id, TableCellSalt, key, c));
            state.CellHits[c] = cell;
            hovered |= cell.Hovered || ui.IsLinkIconHovered(cell);
            HandleCellPointer(ui, state, index, c, cell);
        }
        return hovered;
    }

    private static void HandleCellPointer(Ui ui, TableState state, int index, int column, Interaction cell)
    {
        var menu = cell.ClickedWith(PointerButton.Right);
        var middle = cell.ClickedWith(PointerButton.Middle);
        if (cell.Pressed || menu || middle)
            PickTableCell(ui, state, index, column);
        if (!state.Columns.IsSelectable(column)) return;
        if (cell.DoubleClicked)
            state.Activate(false);
        if (middle)
            state.Activate(true);
        if (menu)
            RequestTableMenuAtPointer(ui, state);
    }

    private static void PickTableCell(Ui ui, TableState state, int index, int column)
    {
        PickTableRow(ui, state, index);
        if (state.Columns.IsSelectable(column))
            state.Cells!.SelectColumn(column);
    }

    private static void PickTableRow(Ui ui, TableState state, int index)
    {
        ui.Focus(state.Id, takesText: false);
        state.Rows.Select(index, state.Count, reveal: false);
        ui.ScrollRowIntoView(state.Id, index);
    }

    private static void RequestTableMenuAtPointer(Ui ui, TableState state)
    {
        state.ContextMenu = true;
        state.MenuAt = ui.Input.Pointer;
    }

    /// <summary>The element a context menu asked for by key opens under: the row, or in a table of
    /// cells its first cell.</summary>
    private static int SelectedRowElement(TableState state, Interaction row)
    {
        if (state.Cells == null) return row.Id;
        if (state.CellHits.Length == 0) return 0;
        return state.CellHits[0].Id;
    }

    private static Box TableRowBox(Ui ui, TableState state, float? height, bool selected, bool hovered)
    {
        var theme = ui.Theme;
        var box = new Box
        {
            Width = Sizing.Grow(),
            Height = height ?? state.RowHeight,
            Gap = 0f,
            Padding = new Spacing(theme.ShellPaddingX, 0f),
            Background = theme.PickRowBackground(selected, state.Focused, hovered),
        };
        if (state.Columns.IsSized)
            box = box with { Width = state.Columns.TotalWidth + theme.ShellPaddingX * 2 };
        return box;
    }
}
