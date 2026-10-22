using ZGF.Gui.Immediate.Widgets;

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
}
