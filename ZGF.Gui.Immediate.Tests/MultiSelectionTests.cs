using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class MultiSelectionTests
{
    private static readonly string[] Rows = ["a", "b", "c", "d", "e"];

    private readonly MultiSelection<string> _selection = new();

    private List<string> Selected() => _selection.InOrder(Rows);

    [Fact]
    public void A_plain_select_keeps_only_that_row()
    {
        _selection.Select("a", SelectionChange.Replace, Rows);
        _selection.Select("c", SelectionChange.Replace, Rows);

        Assert.Equal(["c"], Selected());
    }

    [Fact]
    public void Toggling_adds_and_removes_single_rows()
    {
        _selection.Select("a", SelectionChange.Replace, Rows);
        _selection.Select("c", SelectionChange.Toggle, Rows);
        _selection.Select("a", SelectionChange.Toggle, Rows);

        Assert.Equal(["c"], Selected());
    }

    [Fact]
    public void Extending_selects_the_range_from_the_anchor_and_shrinks_when_reversed()
    {
        _selection.Select("b", SelectionChange.Replace, Rows);
        _selection.Select("d", SelectionChange.Extend, Rows);
        Assert.Equal(["b", "c", "d"], Selected());

        _selection.Select("a", SelectionChange.Extend, Rows);
        Assert.Equal(["a", "b"], Selected());
    }

    [Fact]
    public void A_toggled_row_becomes_the_anchor()
    {
        _selection.Select("a", SelectionChange.Replace, Rows);
        _selection.Select("d", SelectionChange.Toggle, Rows);
        _selection.Select("e", SelectionChange.Extend, Rows);

        Assert.Equal(["d", "e"], Selected());
    }

    [Fact]
    public void Moving_selects_the_next_row_and_extends_with_shift()
    {
        _selection.Move(1, extend: false, Rows);
        Assert.Equal(["a"], Selected());

        _selection.Move(1, extend: false, Rows);
        _selection.Move(1, extend: true, Rows);
        _selection.Move(1, extend: true, Rows);

        Assert.Equal(["b", "c", "d"], Selected());
    }

    [Fact]
    public void Moves_stop_at_either_end()
    {
        _selection.MoveTo(4, extend: false, Rows);
        _selection.Move(1, extend: false, Rows);
        Assert.Equal(["e"], Selected());

        _selection.MoveTo(-3, extend: false, Rows);
        Assert.Equal(["a"], Selected());
    }

    [Fact]
    public void Rows_that_are_gone_leave_the_selection()
    {
        _selection.SelectAll(Rows);

        _selection.Reconcile(["a", "c"]);

        Assert.Equal(["a", "c"], _selection.InOrder(["a", "c"]));
        Assert.Equal(2, _selection.Count);
    }

    [Fact]
    public void After_removal_the_row_now_at_the_same_place_is_selected()
    {
        _selection.SelectAt(1, ["a", "c", "d"]);
        Assert.Equal(["c"], _selection.InOrder(["a", "c", "d"]));

        _selection.SelectAt(5, ["a"]);
        Assert.Equal(["a"], _selection.InOrder(["a"]));

        _selection.SelectAt(0, []);
        Assert.Equal(0, _selection.Count);
    }
}
