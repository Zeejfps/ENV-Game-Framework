using ZGF.Desktop;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class MultiSelectListTests
{
    private readonly List<string> _rows = Enumerable.Range(0, 100)
        .Select(i => $"row {i}")
        .ToList();

    private readonly MultiSelection<string> _selection = new();
    private readonly UiFrames _frames;
    private int _activations;

    public MultiSelectListTests() => _frames = new UiFrames(width: 400f, height: 300f, body: Body);

    private void Body(Ui ui)
    {
        using var screen = ui.Screen();
        using var list = ui.SelectableList(ui.GetId("list"), _rows, _selection);
        for (var i = list.First; i < list.End; i++)
        {
            using (list.Row(i, ui.GetId(i)))
                ui.Text(_rows[i]);
        }
        if (list.Activated)
            _activations++;
    }

    private void ClickRow(int index, KeyModifiers modifiers = KeyModifiers.None)
    {
        _frames.Click(RowRect(index), modifiers: modifiers);
    }

    private UiRect RowRect(int index) => _frames.RectOf(ui => ui.GetId(index));

    private List<string> Picked() => _selection.InOrder(_rows);

    [Fact]
    public void Clicks_pick_out_rows_with_ctrl_toggling_and_shift_extending()
    {
        _frames.Frame(2);

        ClickRow(0);
        ClickRow(2, KeyModifiers.Control);
        Assert.Equal(["row 0", "row 2"], Picked());

        ClickRow(0, KeyModifiers.Control);
        Assert.Equal(["row 2"], Picked());

        ClickRow(4, KeyModifiers.Shift);
        Assert.Equal(["row 0", "row 1", "row 2", "row 3", "row 4"], Picked());
    }

    [Fact]
    public void Keys_extend_with_shift_and_ctrl_a_picks_out_every_row()
    {
        _frames.Frame(2);
        ClickRow(1);

        _frames.Key(KeyboardKey.DownArrow, KeyModifiers.Shift).Frame();
        Assert.Equal(["row 1", "row 2"], Picked());

        _frames.Key(KeyboardKey.DownArrow).Frame();
        Assert.Equal(["row 3"], Picked());

        _frames.Key(KeyboardKey.A, KeyModifiers.Control).Frame();
        Assert.Equal(_rows, Picked());
    }

    [Fact]
    public void Moving_to_the_end_scrolls_the_current_row_into_view()
    {
        _frames.Frame(2);
        ClickRow(0);

        _frames.Key(KeyboardKey.End).Frame(2);

        var list = _frames.RectOf("list");
        Assert.True(RowRect(99).Bottom <= list.Bottom + 0.01f);
    }

    [Fact]
    public void Enter_and_a_double_click_activate_the_picked_out_rows()
    {
        _frames.Frame(2);
        ClickRow(0);
        ClickRow(1, KeyModifiers.Shift);

        _frames.Key(KeyboardKey.Enter).Frame();
        Assert.Equal(1, _activations);
        Assert.Equal(["row 0", "row 1"], Picked());

        _frames.DoubleClick(RowRect(3));
        Assert.Equal(2, _activations);
        Assert.Equal(["row 3"], Picked());
    }
}
