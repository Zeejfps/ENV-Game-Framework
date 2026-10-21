using ZGF.Desktop;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class SplitPaneTests
{
    private float _size = 100f;
    private readonly UiFrames _frames = new();

    private void Body(Ui ui)
    {
        using var split = ui.SplitPane("split", Direction.Horizontal, ref _size, minFirst: 50f, minSecond: 100f);
        using (split.First())
            ui.Leaf(ui.GetId("left"), new Box { Width = Sizing.Grow(), Height = Sizing.Grow() });
        using (split.Second())
            ui.Leaf(ui.GetId("right"), new Box { Width = Sizing.Grow(), Height = Sizing.Grow() });
    }

    private void Drag(float toX)
    {
        _frames.Run(0f, Body);
        _frames.MovePointer(118.5f, 150f);
        _frames.Run(0f, Body);
        _frames.Press();
        _frames.Run(0f, Body);
        _frames.MovePointer(toX, 150f);
        _frames.Run(0f, Body);
        _frames.Release();
        _frames.Run(0f, Body);
        _frames.Run(0f, Body);
    }

    [Fact]
    public void Panes_split_the_space_at_the_apps_size()
    {
        _frames.Run(0f, Body);

        Assert.Equal(100f, _frames.RectOf(Body, ui => ui.GetId("left")).Width);
        Assert.Equal(368f - 105f, _frames.RectOf(Body, ui => ui.GetId("right")).Width);
    }

    [Fact]
    public void Dragging_the_divider_writes_the_size_and_shows_a_resize_cursor()
    {
        _frames.Run(0f, Body);
        _frames.MovePointer(118.5f, 150f);
        _frames.Run(0f, Body);
        Assert.Equal(MouseCursor.ResizeHorizontal, _frames.Run(0f, ui =>
        {
            Body(ui);
            return ui.Cursor;
        }));

        Drag(218.5f);

        Assert.Equal(200f, _size);
        Assert.Equal(200f, _frames.RectOf(Body, ui => ui.GetId("left")).Width);
    }

    [Theory]
    [InlineData(0f, 50f)]
    [InlineData(400f, 368f - 5f - 100f)]
    public void Dragging_stops_at_the_minimum_sizes(float toX, float expected)
    {
        Drag(toX);

        Assert.Equal(expected, _size);
    }
}

public sealed class TabStripTests
{
    private readonly UiFrames _frames = new();
    private readonly List<string> _tabs = Enumerable.Range(1, 3).Select(i => $"Doc{i}").ToList();
    private readonly List<string> _closed = new();
    private int _selected;

    private void Body(Ui ui)
    {
        var next = _selected;
        using (var strip = ui.TabStrip("tabs"))
        {
            for (var i = 0; i < _tabs.Count; i++)
            {
                var tab = strip.Tab(_tabs[i], i == _selected);
                if (tab.Selected) next = i;
                if (tab.Closed) _closed.Add(_tabs[i]);
            }
        }
        _selected = next;
    }

    [Fact]
    public void Pressing_a_tab_selects_it()
    {
        _frames.Run(0f, Body);

        _frames.ClickOn(Body, ui => ui.GetId("Doc2"));

        Assert.Equal(1, _selected);
        Assert.Empty(_closed);
    }

    [Fact]
    public void The_close_button_and_a_middle_click_close_a_tab()
    {
        _frames.Run(0f, Body);

        _frames.ClickOn(Body, ui => HashCode.Combine(ui.GetId("Doc3"), "close"));
        _frames.ClickOn(Body, ui => ui.GetId("Doc2"), PointerButton.Middle);

        Assert.Equal(["Doc3", "Doc2"], _closed);
        Assert.Equal(0, _selected);
    }

    [Fact]
    public void Overflowing_tabs_scroll_and_a_newly_selected_tab_scrolls_into_view()
    {
        _tabs.AddRange(Enumerable.Range(4, 7).Select(i => $"Doc{i}"));
        _frames.Run(0f, Body);
        _frames.Run(0f, Body);
        Assert.True(_frames.RectOf(Body, ui => ui.GetId("Doc10")).Right > 384f);

        _selected = 9;
        _frames.Run(0f, Body);
        _frames.Run(0f, Body);
        _frames.Run(0f, Body);

        Assert.Equal(384f, _frames.RectOf(Body, ui => ui.GetId("Doc10")).Right, 0.01f);

        _frames.MovePointer(100f, 30f);
        _frames.Run(0f, Body);
        _frames.Wheel(2);
        _frames.Run(0f, Body);
        _frames.Run(0f, Body);
        Assert.True(_frames.RectOf(Body, ui => ui.GetId("Doc10")).Right > 384f);
    }
}
