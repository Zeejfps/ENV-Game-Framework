using ZGF.Desktop;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class SelectableRowsTests
{
    private static readonly string[] Rows = ["a", "b", "c"];

    private readonly MultiSelection<string> _selection = new();
    private readonly UiFrames _frames;
    private string _name = "b-name";
    private int _removeRequests;

    public SelectableRowsTests() => _frames = new UiFrames(body: Body);

    private void Body(Ui ui)
    {
        using var screen = ui.Screen();
        using var rows = ui.SelectableRows(ui.GetId("rows"), Rows, _selection);
        if (rows.RemoveRequested)
            _removeRequests++;
        foreach (var key in Rows)
        {
            using (rows.Row(key, ui.GetId("row-" + key), new Box { Height = 24f }))
            {
                ui.Leaf(new Box { Width = 100f, Height = 20f });
                if (key == "b")
                    ui.TextField("field", ref _name, width: 80f);
            }
        }
    }

    private UiRect RowRect(string key) => _frames.RectOf("row-" + key);

    private void ClickRow(string key, KeyModifiers modifiers = KeyModifiers.None)
    {
        var row = RowRect(key);
        _frames.Click(row.X + 10f, row.Y + 10f, modifiers: modifiers);
    }

    [Fact]
    public void Clicks_select_with_ctrl_toggling_and_shift_extending()
    {
        _frames.Frame();

        ClickRow("a");
        ClickRow("c", KeyModifiers.Control);
        Assert.Equal(["a", "c"], _selection.InOrder(Rows));

        ClickRow("a");
        ClickRow("c", KeyModifiers.Shift);
        Assert.Equal(["a", "b", "c"], _selection.InOrder(Rows));
    }

    [Fact]
    public void Keys_move_the_selection_while_the_rows_hold_focus()
    {
        _frames.Frame();
        ClickRow("a");

        _frames.Key(KeyboardKey.DownArrow, KeyModifiers.Shift).Frame();
        Assert.Equal(["a", "b"], _selection.InOrder(Rows));

        _frames.Key(KeyboardKey.End).Frame();
        Assert.Equal(["c"], _selection.InOrder(Rows));

        _frames.Key(KeyboardKey.A, KeyModifiers.Control).Frame();
        Assert.Equal(Rows, _selection.InOrder(Rows));
    }

    [Fact]
    public void Delete_asks_to_remove_only_while_the_rows_hold_focus()
    {
        _frames.Frame();
        _frames.Key(KeyboardKey.Delete).Frame();
        Assert.Equal(0, _removeRequests);

        ClickRow("a");
        _frames.Key(KeyboardKey.Delete).Frame();

        Assert.Equal(1, _removeRequests);
    }

    [Fact]
    public void A_click_in_a_field_on_a_row_edits_rather_than_selects()
    {
        _frames.Frame();
        ClickRow("a");

        _frames.ClickOn(ui => ui.GetId("field"));
        _frames.Key(KeyboardKey.Delete).Frame();

        Assert.Equal(["a"], _selection.InOrder(Rows));
        Assert.Equal(0, _removeRequests);
    }
}
