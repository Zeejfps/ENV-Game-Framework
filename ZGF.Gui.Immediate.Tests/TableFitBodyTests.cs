using ZGF.Desktop;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class TableFitBodyTests
{
    private static readonly string[] Rows = ["a", "b", "c"];

    private readonly MultiSelection<string> _selection = new();
    private readonly TableColumns _columns = new(new TableColumn(), new TableColumn());
    private readonly Dictionary<string, int> _rowIds = new();
    private readonly UiFrames _frames;
    private string _name = "b-name";
    private int _removeRequests;
    private int _keysAfter;

    public TableFitBodyTests() => _frames = new UiFrames(width: 400f, height: 300f, body: Body);

    private void Body(Ui ui)
    {
        using var screen = ui.Screen();
        var table = ui.Table("table", _columns, _selection);
        table.Header(["Name", "Edit"]);
        using (var rows = table.FitBody(Rows))
        {
            if (rows.RemoveRequested)
                _removeRequests++;
            for (var i = 0; i < Rows.Length; i++)
                Row(ui, rows, i);
        }
        _keysAfter += ui.Input.Keys.Count;
    }

    private void Row(Ui ui, TableRows<string> rows, int index)
    {
        var key = Rows[index];
        using var row = rows.Row(index);
        _rowIds[key] = row.Id;
        using (row.Cell(0))
            ui.Text(key, new Box { Width = Sizing.Grow() });
        using (row.Cell(1))
        {
            if (key == "b")
                ui.TextField("field", ref _name, width: Sizing.Grow());
        }
    }

    private UiRect RowRect(string key) => _frames.RectOf(_ => _rowIds[key]);

    private void ClickRow(string key, KeyModifiers modifiers = KeyModifiers.None)
    {
        var row = RowRect(key);
        _frames.Click(row.X + 10f, row.Y + row.Height / 2, modifiers: modifiers);
    }

    [Fact]
    public void A_row_is_as_tall_as_its_tallest_cell_and_no_shorter_than_a_row()
    {
        _frames.Frame(2);

        Assert.Equal(UiTheme.Dark.RowHeight, RowRect("a").Height);
        Assert.Equal(UiTheme.Dark.ControlHeight, RowRect("b").Height);
        Assert.Equal(RowRect("a").Bottom, RowRect("b").Y);
    }

    [Fact]
    public void The_header_columns_line_up_with_the_cells()
    {
        _frames.Frame(2);

        var heading = _frames.TextRect("Edit");
        var field = _frames.RectOf(ui =>
        {
            using (ui.Id(EqualityComparer<string>.Default.GetHashCode("b")))
                return ui.GetId("field");
        });

        Assert.Equal(heading.X, field.X, 0.5f);
    }

    [Fact]
    public void Clicks_pick_out_rows_with_ctrl_toggling_and_shift_extending()
    {
        _frames.Frame(2);

        ClickRow("a");
        ClickRow("c", KeyModifiers.Control);
        Assert.Equal(["a", "c"], _selection.InOrder(Rows));

        ClickRow("a");
        ClickRow("c", KeyModifiers.Shift);
        Assert.Equal(["a", "b", "c"], _selection.InOrder(Rows));
    }

    [Fact]
    public void Keys_move_the_picks_while_the_table_holds_focus_and_go_no_further()
    {
        _frames.Frame(2);
        ClickRow("a");
        _keysAfter = 0;

        _frames.Key(KeyboardKey.DownArrow, KeyModifiers.Shift).Frame();
        Assert.Equal(["a", "b"], _selection.InOrder(Rows));

        _frames.Key(KeyboardKey.End).Frame();
        Assert.Equal(["c"], _selection.InOrder(Rows));

        _frames.Key(KeyboardKey.A, KeyModifiers.Control).Frame();
        Assert.Equal(Rows, _selection.InOrder(Rows));
        Assert.Equal(0, _keysAfter);
    }

    [Fact]
    public void Delete_asks_to_remove_only_while_the_table_holds_focus()
    {
        _frames.Frame(2);
        _frames.Key(KeyboardKey.Delete).Frame();
        Assert.Equal(0, _removeRequests);

        ClickRow("a");
        _frames.Key(KeyboardKey.Delete).Frame();

        Assert.Equal(1, _removeRequests);
    }

    [Fact]
    public void A_click_in_a_field_in_a_cell_edits_rather_than_picks_out()
    {
        _frames.Frame(2);
        ClickRow("a");

        var field = RowRect("b");
        _frames.Click(field.Right - 20f, field.Y + field.Height / 2);
        _frames.Key(KeyboardKey.Delete).Frame();

        Assert.Equal(["a"], _selection.InOrder(Rows));
        Assert.Equal(0, _removeRequests);
    }
}
