using ZGF.Desktop;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class TableScrollBodyTests
{
    private static readonly string[] Rows = Enumerable.Range(0, 100).Select(static i => $"row {i}").ToArray();

    private readonly MultiSelection<string> _selection = new();
    private readonly TableColumns _columns = new(new TableColumn(), new TableColumn());
    private readonly Dictionary<string, int> _rowIds = new();
    private readonly List<string> _recorded = new();
    private readonly UiFrames _frames;
    private int _activations;
    private int _keysAfter;

    public TableScrollBodyTests() => _frames = new UiFrames(new DefaultUiState(), width: 400f, height: 300f, body: Body);

    private void Body(Ui ui)
    {
        using var screen = ui.Screen();
        var table = ui.Table("table", _columns, _selection);
        table.Header(["Name", "Other"]);
        _recorded.Clear();
        using (var rows = table.Body(Rows))
        {
            for (var i = rows.First; i < rows.End; i++)
                Row(rows, i);
            if (rows.Activated)
                _activations++;
        }
        _keysAfter += ui.Input.Keys.Count;
    }

    private void Row(TableScrollRows<string> rows, int index)
    {
        var key = Rows[index];
        _recorded.Add(key);
        using var row = rows.Row(index);
        _rowIds[key] = row.Id;
        row.TextCell(0, key);
        row.TextCell(1, "x");
    }

    private UiRect RowRect(string key) => _frames.RectOf(_ => _rowIds[key]);

    private void ClickRow(string key, KeyModifiers modifiers = KeyModifiers.None)
    {
        var row = RowRect(key);
        _frames.Click(row.X + 10f, row.Y + row.Height / 2, modifiers: modifiers);
    }

    [Fact]
    public void Only_the_rows_in_view_are_recorded_each_a_row_tall()
    {
        _frames.Frame(2);

        Assert.Contains("row 0", _recorded);
        Assert.DoesNotContain("row 99", _recorded);
        Assert.True(_recorded.Count < Rows.Length);
        Assert.Equal(UiTheme.Dark.RowHeight, RowRect("row 0").Height);
    }

    [Fact]
    public void Clicks_pick_out_rows_with_ctrl_toggling_and_shift_extending()
    {
        _frames.Frame(2);

        ClickRow("row 0");
        ClickRow("row 2", KeyModifiers.Control);
        Assert.Equal(["row 0", "row 2"], _selection.InOrder(Rows));

        ClickRow("row 0");
        ClickRow("row 2", KeyModifiers.Shift);
        Assert.Equal(["row 0", "row 1", "row 2"], _selection.InOrder(Rows));
    }

    [Fact]
    public void A_key_that_moves_past_the_view_scrolls_the_row_into_it_and_goes_no_further()
    {
        _frames.Frame(2);
        ClickRow("row 0");
        _keysAfter = 0;

        _frames.Key(KeyboardKey.End).Frame(2);

        Assert.Equal(["row 99"], _selection.InOrder(Rows));
        Assert.Contains("row 99", _recorded);
        Assert.DoesNotContain("row 0", _recorded);

        _frames.Key(KeyboardKey.UpArrow, KeyModifiers.Shift).Frame();
        Assert.Equal(["row 98", "row 99"], _selection.InOrder(Rows));

        _frames.Key(KeyboardKey.A, KeyModifiers.Control).Frame();
        Assert.Equal(Rows, _selection.InOrder(Rows));
        Assert.Equal(0, _keysAfter);
    }

    [Fact]
    public void A_double_click_or_enter_activates_the_picked_out_rows()
    {
        _frames.Frame(2);

        _frames.DoubleClick(RowRect("row 1")).Frame();
        Assert.Equal(1, _activations);
        Assert.Equal(["row 1"], _selection.InOrder(Rows));

        _frames.Key(KeyboardKey.Enter).Frame();
        Assert.Equal(2, _activations);
    }

    [Fact]
    public void Enter_activates_nothing_while_the_table_lacks_focus()
    {
        _frames.Frame(2);

        _frames.Key(KeyboardKey.Enter).Frame();

        Assert.Equal(0, _activations);
    }
}
