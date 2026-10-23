using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class TableTests
{
    private static readonly float Padding = UiTheme.Dark.ShellPaddingX;

    private readonly UiFrames _frames;
    private readonly TableColumns _columns = new(new TableColumn(Fixed: 20f), new TableColumn(1f, 40f), new TableColumn(2f, 40f));
    private readonly ListSelection _rows = new();

    public TableTests() => _frames = new UiFrames(body: Body);

    private void Body(Ui ui)
    {
        using var pane = ui.Column(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Gap = 0f });
        var table = ui.Table("table", _columns, _rows);
        table.Header(["", "Name", "At"]);
        using var body = table.Body(1);
        using var row = body.Row(0, 0);
        row.TextCell(0, "*");
        row.TextCell(1, "first");
        row.TextCell(2, "here");
    }

    private float LeftOf(string text) => _frames.TextRect(text).X;

    private static int BorderOf(Ui ui, int column) => UiWidgets.TableBorderId(UiWidgets.TableHeaderId(ui.GetId("table")), column);

    [Fact]
    public void Fixed_columns_keep_their_width_the_rest_share_by_weight_and_headers_line_up_with_cells()
    {
        _frames.Frame(2);

        var flexible = _columns.Width(1) + _columns.Width(2);

        Assert.Equal(20f, _columns.Width(0));
        Assert.Equal(2f, _columns.Width(2) / _columns.Width(1), 1);
        Assert.InRange(flexible, 400f - 2 * Padding - 20f - 2f, 400f - 2 * Padding - 20f);
        Assert.Equal(LeftOf("Name"), LeftOf("first"));
        Assert.Equal(LeftOf("At"), LeftOf("here"));
    }

    [Fact]
    public void Dragging_a_border_moves_width_between_its_neighbors_down_to_their_minimum()
    {
        _frames.Frame(2);
        var border = _frames.RectOf(ui => BorderOf(ui, 1));
        var pair = _columns.Width(1) + _columns.Width(2);
        var x = border.X + border.Width / 2;
        var y = border.Y + border.Height / 2;

        _frames.Drag(new PointF(x, y), new PointF(x + 30f, y));
        var widened = _columns.Width(1);
        _frames.MovePointer(x + 1000f, y).Frame();
        _frames.Release().Frame(2);

        Assert.Equal(pair, _columns.Width(1) + _columns.Width(2), 0);
        Assert.Equal(40f, _columns.Width(2), 0);
        Assert.True(widened > pair / 3 + 25f, $"{widened}");
        Assert.Equal(LeftOf("At"), LeftOf("here"));
    }

    [Fact]
    public void Sized_columns_keep_their_widths_scroll_with_the_header_and_resize_alone()
    {
        var columns = TableColumns.Sized([300f, 250f, 200f]);
        var scroll = new TableScroll();
        var clicked = -1;
        _frames.Body = ui =>
        {
            using var screen = ui.Screen();
            var table = ui.Table("table", columns, _rows, scroll);
            var at = table.Header(["A", "B", "C"], new TableSort(1));
            if (at >= 0)
                clicked = at;
            using var body = table.Body(1);
            using var row = body.Row(0, 0);
            row.TextCell(0, "a");
            row.TextCell(1, "b");
            row.TextCell(2, "c");
        };
        _frames.Frame(2);
        Assert.Equal(750f, columns.TotalWidth);
        var unscrolled = LeftOf("B");

        scroll.X = 200f;
        _frames.Frame(2);
        Assert.Equal(unscrolled - 200f, LeftOf("B"), 0);
        Assert.Equal(LeftOf("b"), LeftOf("B"));
        Assert.Equal(LeftOf("c"), LeftOf("C"));

        var border = _frames.RectOf(ui => BorderOf(ui, 0));
        var x = border.X + border.Width / 2;
        var y = border.Y + border.Height / 2;
        _frames.Drag(new PointF(x, y), new PointF(x + 40f, y)).Release().Frame();

        Assert.Equal(340f, columns.Width(0), 0);
        Assert.Equal(250f, columns.Width(1));
        Assert.Equal([340f, 250f, 200f], columns.Weights.Select(w => MathF.Round(w)));

        var heading = _frames.TextRect("B");
        _frames.Click(heading.X + 1f, heading.Y + heading.Height / 2);
        Assert.Equal(1, clicked);
    }

    [Fact]
    public void Saved_weights_are_restored_and_mismatched_ones_ignored()
    {
        _columns.Restore([1f, 3f, 1f]);
        _frames.Frame(2);
        var restored = _columns.Width(1) / _columns.Width(2);

        _columns.Restore([1f, 1f]);
        _columns.Restore([1f, -1f, 1f]);
        _frames.Frame();

        Assert.Equal(3f, restored, 1);
        Assert.Equal(3f, _columns.Width(1) / _columns.Width(2), 1);
    }

    [Fact]
    public void A_press_selects_a_whole_row_a_double_click_activates_it_and_a_middle_click_asks_for_a_new_tab()
    {
        var rows = new RowTable();
        rows.Frames.Frame(2);

        rows.Frames.Click(rows.Frames.TextRect(RowTable.Text(2)));
        Assert.Equal(2, rows.Rows.Index);
        Assert.True(rows.Clicked);
        Assert.False(rows.Activated);

        rows.Frames.DoubleClick(rows.Frames.TextRect(RowTable.Text(3)));
        Assert.Equal(3, rows.Rows.Index);
        Assert.True(rows.Activated);
        Assert.False(rows.InNewTab);

        rows.Activated = false;
        rows.Frames.Click(rows.Frames.TextRect(RowTable.Text(1)), PointerButton.Middle);
        Assert.Equal(1, rows.Rows.Index);
        Assert.True(rows.Activated);
        Assert.True(rows.InNewTab);
    }

    [Fact]
    public void The_table_takes_the_keys_it_acts_on_and_leaves_the_rest()
    {
        var rows = new RowTable();
        rows.Frames.Frame(2);
        rows.Frames.Click(rows.Frames.TextRect(RowTable.Text(0)));

        rows.Frames.Key(KeyboardKey.DownArrow).Key(KeyboardKey.DownArrow).Key(KeyboardKey.Space).Frame();
        Assert.Equal(2, rows.Rows.Index);
        Assert.Equal([KeyboardKey.Space], rows.KeysLeft);

        rows.Frames.Key(KeyboardKey.Enter, KeyModifiers.Control).Frame();
        Assert.True(rows.Activated);
        Assert.True(rows.InNewTab);

        rows.Frames.Key(KeyboardKey.Menu).Frame();
        Assert.True(rows.MenuRequested);
        Assert.Empty(rows.KeysLeft);
    }

    [Fact]
    public void A_table_without_focus_takes_no_keys()
    {
        var rows = new RowTable();
        rows.Frames.Frame(2);

        rows.Frames.Key(KeyboardKey.DownArrow).Frame();

        Assert.False(rows.Rows.HasSelection);
        Assert.Equal([KeyboardKey.DownArrow], rows.KeysLeft);
    }

    [Fact]
    public void Clicking_a_cell_makes_it_current_with_all_its_text_selected_and_outlined_in_focus()
    {
        var grid = new CellTable();
        grid.Frames.Frame(2);

        grid.Frames.Click(grid.Frames.TextRect(CellTable.Text(1, 1)));

        Assert.True(grid.Cells.IsAt(1, 1));
        Assert.Equal(CellTable.Text(1, 1), grid.Cells.Text.Of(CellTable.Text(1, 1)));
        Assert.Contains(grid.Frames.Canvas.Rects, r => r.Inputs.Style.BorderColor.Top.Value == UiTheme.Dark.FocusRing);
        Assert.Equal(MouseCursor.Text, grid.Frames.Cursor);
    }

    [Fact]
    public void A_ctrl_click_on_a_link_cell_follows_it()
    {
        var grid = new CellTable();
        grid.Frames.Frame(2);

        grid.Frames.Click(grid.Frames.TextRect(CellTable.Text(2, 2)), modifiers: KeyModifiers.Control);

        Assert.True(grid.Followed);
    }

    [Fact]
    public void Pressing_a_column_that_is_not_selectable_selects_the_row_but_no_cell_and_never_activates()
    {
        var grid = new CellTable();
        grid.Frames.Frame(2);

        grid.Frames.DoubleClick(grid.Frames.TextRect(CellTable.Text(3, 0)));

        Assert.Equal(3, grid.Cells.Row);
        Assert.False(grid.Cells.HasCell);
        Assert.False(grid.Activated);
    }

    [Fact]
    public void Left_and_right_step_between_selectable_columns_and_up_and_down_keep_the_column()
    {
        var grid = new CellTable();
        grid.Frames.Frame(2);
        grid.Frames.Click(grid.Frames.TextRect(CellTable.Text(1, 2)));

        grid.Frames.Key(KeyboardKey.LeftArrow).Frame();
        Assert.True(grid.Cells.IsAt(1, 1));
        grid.Frames.Key(KeyboardKey.LeftArrow).Frame();
        Assert.True(grid.Cells.IsAt(1, 1));
        grid.Frames.Key(KeyboardKey.RightArrow).Key(KeyboardKey.RightArrow).Frame();
        Assert.True(grid.Cells.IsAt(1, 2));
        grid.Frames.Key(KeyboardKey.DownArrow).Frame();
        Assert.True(grid.Cells.IsAt(2, 2));
        Assert.Empty(grid.KeysLeft);
    }

    [Fact]
    public void Moving_down_into_a_row_without_a_current_cell_picks_the_first_selectable_column()
    {
        var grid = new CellTable();
        grid.Frames.Frame(2);
        grid.Frames.Click(grid.Frames.TextRect(CellTable.Text(0, 0)));

        grid.Frames.Key(KeyboardKey.DownArrow).Frame();

        Assert.True(grid.Cells.IsAt(1, 1));
    }

    [Fact]
    public void Ctrl_c_asks_to_copy_the_part_selected_else_the_whole_cell_after_moving_to_another()
    {
        var grid = new CellTable();
        grid.Frames.Frame(2);
        var cell = grid.Frames.TextRect(CellTable.Text(1, 1));
        var advance = cell.Width / CellTable.Text(1, 1).Length;
        var y = cell.Y + cell.Height / 2;
        grid.Frames.Drag(new PointF(cell.X + 1f, y), new PointF(cell.X + advance * 3, y)).Release().Frame();

        grid.Frames.Key(KeyboardKey.C, KeyModifiers.Control).Frame();
        var part = grid.Copied;
        grid.Frames.Key(KeyboardKey.DownArrow).Key(KeyboardKey.C, KeyModifiers.Control).Frame();

        Assert.Equal(CellTable.Text(1, 1).Substring(0, 3), part);
        Assert.Equal(CellTable.Text(2, 1), grid.Copied);
    }

    [Fact]
    public void Escape_collapses_the_text_selected_in_the_current_cell_and_then_is_left_for_the_app()
    {
        var grid = new CellTable();
        grid.Frames.Frame(2);
        grid.Frames.Click(grid.Frames.TextRect(CellTable.Text(1, 1)));

        grid.Frames.Key(KeyboardKey.Escape).Frame();
        Assert.False(grid.Cells.Text.HasSelection);
        Assert.Empty(grid.KeysLeft);

        grid.Frames.Key(KeyboardKey.Escape).Frame();
        Assert.Equal([KeyboardKey.Escape], grid.KeysLeft);
    }

    [Fact]
    public void A_right_click_on_a_cell_selects_it_and_asks_for_a_menu_at_the_pointer()
    {
        var grid = new CellTable();
        grid.Frames.Frame(2);
        var cell = grid.Frames.TextRect(CellTable.Text(4, 2));

        grid.Frames.Click(cell, PointerButton.Right);

        Assert.True(grid.Cells.IsAt(4, 2));
        Assert.True(grid.MenuRequested);
        Assert.Equal(cell.X + cell.Width / 2, grid.MenuAt.X, 0);
    }

    [Fact]
    public void Clearing_the_cells_leaves_no_row_and_no_cell()
    {
        var cells = new TableCellSelection();
        cells.Select(3, 10, 2);

        cells.Clear();

        Assert.False(cells.Rows.HasSelection);
        Assert.False(cells.HasCell);
    }

    [Fact]
    public void Stepping_skips_columns_that_are_not_selectable_and_stops_at_the_last()
    {
        var columns = new TableColumns(new TableColumn(Controls: true), new TableColumn(), new TableColumn(Controls: true), new TableColumn());

        Assert.Equal(1, columns.StepSelectable(-1, 1));
        Assert.Equal(3, columns.StepSelectable(1, 1));
        Assert.Equal(3, columns.StepSelectable(3, 1));
        Assert.Equal(1, columns.StepSelectable(3, -1));
        Assert.Equal(1, columns.StepSelectable(1, -1));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_table_of_cells_with_a_cell_selected_allocates_nothing_per_frame(bool scrolling)
    {
        var grid = new CellTable(rowCount: 500);
        var frames = grid.Frames;
        frames.Frame(2);
        frames.Click(frames.TextRect(CellTable.Text(2, 1)));
        frames.MovePointer(frames.TextRect(CellTable.Text(4, 2)));

        frames.Canvas.Recording = false;
        for (var i = 0; i < 400; i++)
            Sweep(frames, scrolling, i);
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 400; i++)
            Sweep(frames, scrolling, i);
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;

        Assert.Equal(0, allocated / 400);
    }

    /// <summary>Scrolls down and back up in sweeps, so measured frames show rows the warm-up showed.</summary>
    private static void Sweep(UiFrames frames, bool scrolling, int frame)
    {
        if (scrolling)
        {
            var down = frame / 100 % 2 == 0;
            frames.Wheel(down ? -1 : 1);
        }
        frames.Frame();
    }

    /// <summary>A table of whole rows, each a line of text.</summary>
    private sealed class RowTable
    {
        private const int RowCount = 6;

        private static readonly string[] Texts = Enumerable.Range(0, RowCount).Select(i => $"row {i}").ToArray();

        public readonly UiFrames Frames;
        public readonly TableColumns Columns = new(new TableColumn());
        public readonly ListSelection Rows = new();
        public readonly List<KeyboardKey> KeysLeft = new();
        public bool Clicked;
        public bool Activated;
        public bool InNewTab;
        public bool MenuRequested;

        public RowTable() => Frames = new UiFrames(body: Body) { FrameTime = 1f / 60f };

        public static string Text(int row) => Texts[row];

        private void Body(Ui ui)
        {
            KeysLeft.Clear();
            using var screen = ui.Screen();
            var table = ui.Table("rows", Columns, Rows);
            table.Header(["Name"]);
            using var body = table.Body(RowCount);
            foreach (var stroke in ui.Input.Keys)
                KeysLeft.Add(stroke.Key);
            for (var i = body.First; i < body.End; i++)
            {
                using var row = body.Row(i, i);
                Clicked |= row.Interaction.Clicked;
                row.TextCell(0, Texts[i]);
            }
            Activated |= body.Activated;
            InNewTab |= body.InNewTab;
            MenuRequested |= body.ContextMenuRequested;
        }
    }

    /// <summary>A table of cells: a mark column that can't be current, then two of text, the
    /// second a link.</summary>
    private sealed class CellTable
    {
        private static readonly UiIcon Link = new(">", "Icons");

        private readonly int _rowCount;
        private readonly string[][] _texts;

        public readonly UiFrames Frames;
        public readonly TableColumns Columns = new(new TableColumn(Fixed: 90f, Controls: true), new TableColumn(), new TableColumn());
        public readonly TableCellSelection Cells = new();
        public readonly List<KeyboardKey> KeysLeft = new();
        public bool Activated;
        public bool MenuRequested;
        public PointF MenuAt;
        public bool Followed;
        public string Copied = "";

        public CellTable(int rowCount = 8)
        {
            _rowCount = rowCount;
            _texts = Enumerable.Range(0, rowCount)
                .Select(i => new[] { Text(i, 0), Text(i, 1), Text(i, 2) })
                .ToArray();
            Frames = new UiFrames(body: Body) { FrameTime = 1f / 60f };
        }

        public static string Text(int row, int column) => $"cell {row}.{column}";

        private void Body(Ui ui)
        {
            KeysLeft.Clear();
            using var screen = ui.Screen();
            var table = ui.Table("cells", Columns, Cells);
            table.Header(["", "A", "B"]);
            using var body = table.Body(_rowCount);
            foreach (var stroke in ui.Input.Keys)
                KeysLeft.Add(stroke.Key);
            for (var i = body.First; i < body.End; i++)
                Row(body, i);
            Answer(body);
        }

        private void Row(TableBody body, int index)
        {
            var texts = _texts[index];
            using var row = body.Row(index, index);
            row.TextCell(0, texts[0]);
            row.TextCell(1, texts[1]);
            Followed |= row.TextCell(2, texts[2], link: Link, linkTooltip: "Go");
        }

        private void Answer(TableBody body)
        {
            Activated |= body.Activated;
            if (body.ContextMenuRequested)
            {
                MenuRequested = true;
                MenuAt = body.ContextMenuAt;
            }
            if (!body.CopyRequested) return;
            var shown = _texts[Cells.Row][Cells.Column];
            Copied = Cells.Copied(shown, shown);
        }
    }
}
