using ZGF.Desktop;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class TableCellSelectionTests
{
    [Fact]
    public void Selecting_a_cell_selects_all_its_text_and_reselecting_it_keeps_what_is_selected()
    {
        var cell = new TableCellSelection();

        cell.Select(2, 1, "hello");
        Assert.Equal("hello", cell.Text.Of("hello"));

        cell.Text.Select(0, 2);
        cell.Select(2, 1, "hello");
        Assert.Equal("he", cell.Text.Of("hello"));
    }

    [Fact]
    public void A_copy_takes_the_part_selected_else_the_whole_cell()
    {
        var cell = new TableCellSelection();
        cell.Select(0, 0, "first");

        Assert.Equal("first\nsecond", cell.Copied("first", "first\nsecond"));
        cell.Text.Select(1, 3);
        Assert.Equal("ir", cell.Copied("first", "first\nsecond"));
    }

    [Fact]
    public void Clearing_leaves_no_cell()
    {
        var cell = new TableCellSelection();
        cell.Select(3, 2, "x");

        cell.Clear();

        Assert.False(cell.HasCell);
        Assert.False(cell.Text.HasSelection);
    }

    private static KeyStroke Stroke(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None) => new(key, false, modifiers);

    [Fact]
    public void Left_and_right_ask_for_the_next_column_within_the_bounds()
    {
        var cell = new TableCellSelection();
        cell.Select(0, 2, "x");

        Assert.Equal(TableCellKey.Column, cell.ApplyKey(Stroke(KeyboardKey.LeftArrow), 1, 3, out var left));
        Assert.Equal(1, left);
        cell.ApplyKey(Stroke(KeyboardKey.RightArrow), 1, 3, out var right);
        Assert.Equal(3, right);
        cell.Select(0, 3, "x");
        cell.ApplyKey(Stroke(KeyboardKey.RightArrow), 1, 3, out var past);
        Assert.Equal(3, past);
        Assert.Equal(TableCellKey.None, cell.ApplyKey(Stroke(KeyboardKey.LeftArrow, KeyModifiers.Shift), 1, 3, out _));
    }

    [Fact]
    public void A_row_without_a_cell_steps_into_its_first_column()
    {
        var cell = new TableCellSelection();
        cell.Select(4, -1, "");

        cell.ApplyKey(Stroke(KeyboardKey.RightArrow), 1, 3, out var right);
        cell.ApplyKey(Stroke(KeyboardKey.LeftArrow), 0, 3, out var left);

        Assert.Equal(1, right);
        Assert.Equal(0, left);
    }

    [Fact]
    public void Escape_collapses_the_selected_text_and_otherwise_is_left_for_the_caller()
    {
        var cell = new TableCellSelection();
        cell.Select(0, 0, "hello");

        Assert.Equal(TableCellKey.Collapsed, cell.ApplyKey(Stroke(KeyboardKey.Escape), 0, 1, out _));
        Assert.False(cell.Text.HasSelection);
        Assert.Equal(TableCellKey.None, cell.ApplyKey(Stroke(KeyboardKey.Escape), 0, 1, out _));
    }

    [Fact]
    public void Ctrl_c_asks_to_copy_and_no_key_works_without_a_row()
    {
        var cell = new TableCellSelection();

        Assert.Equal(TableCellKey.None, cell.ApplyKey(Stroke(KeyboardKey.C, KeyModifiers.Control), 0, 1, out _));
        cell.Select(0, 0, "x");
        Assert.Equal(TableCellKey.Copy, cell.ApplyKey(Stroke(KeyboardKey.C, KeyModifiers.Control), 0, 1, out _));
    }

    private sealed class Table
    {
        public readonly UiFrames Frames;
        public readonly TableColumns Columns = new(new TableColumn(), new TableColumn());
        public readonly ListSelection Rows = new();
        public readonly TableCellSelection Cell = new();
        public bool Followed;

        public Table() => Frames = new UiFrames(body: Body) { FrameTime = 1f / 60f };

        private static readonly UiIcon Link = new(">", "Icons");

        public static int CellId(int row, int column) => HashCode.Combine(0x7E57, row, column);

        public static string Text(int row, int column) => $"cell {row}.{column}";

        private void Body(Ui ui)
        {
            using var screen = ui.Screen();
            var listId = ui.GetId("table");
            ui.TableHeader("header", Columns, listId, ["A", "B"]);
            using var list = ui.SelectableList(listId, 3, Rows);
            for (var i = list.First; i < list.End; i++)
            {
                var hovered = false;
                Span<Interaction> cells = stackalloc Interaction[2];
                for (var c = 0; c < 2; c++)
                {
                    cells[c] = ui.ListCell(listId, CellId(i, c), ref hovered);
                    if (!cells[c].Pressed) continue;
                    Rows.Select(i, 3);
                    Cell.Select(i, c, Text(i, c));
                }
                using var row = list.Row(i, 0, hovered: hovered);
                for (var c = 0; c < 2; c++)
                {
                    var current = row.Selected && Cell.Column == c;
                    UiIcon? link = null;
                    if (c == 1)
                        link = Link;
                    Followed |= ui.TableCellText(Columns, c, cells[c], Text(i, c), Cell, current, row.Focused, link: link, linkTooltip: "Go");
                }
            }
        }
    }

    [Fact]
    public void Clicking_a_cell_makes_it_current_with_all_its_text_selected_and_outlined_in_focus()
    {
        var table = new Table();
        table.Frames.Frame(2);

        table.Frames.ClickOn(_ => Table.CellId(1, 0)).Frame();

        Assert.True(table.Cell.IsAt(1, 0));
        Assert.Equal(1, table.Rows.Index);
        Assert.Equal(Table.Text(1, 0), table.Cell.Text.Of(Table.Text(1, 0)));
        Assert.Contains(table.Frames.Canvas.Rects, r => r.Inputs.Style.BorderColor.Top.Value == UiTheme.Dark.FocusRing);
        Assert.Equal(MouseCursor.Text, table.Frames.Cursor);
    }

    [Fact]
    public void A_ctrl_click_on_a_link_cell_follows_it()
    {
        var table = new Table();
        table.Frames.Frame(2);
        var rect = table.Frames.RectOf(_ => Table.CellId(2, 1));

        table.Frames.Click(rect.X + 8f, rect.Y + rect.Height / 2, modifiers: KeyModifiers.Control);

        Assert.True(table.Followed);
    }
}
