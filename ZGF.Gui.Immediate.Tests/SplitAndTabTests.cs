using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class SplitPaneTests
{
    private float _size = 100f;
    private readonly UiFrames _frames;

    public SplitPaneTests() => _frames = new UiFrames(body: Body);

    private void Body(Ui ui)
    {
        using var screen = ui.Screen();
        using var split = ui.SplitPane("split", Direction.Horizontal, ref _size, minFirst: 50f, minSecond: 100f);
        using (split.First())
            ui.Leaf(ui.GetId("left"), new Box { Width = Sizing.Grow(), Height = Sizing.Grow() });
        using (split.Second())
            ui.Leaf(ui.GetId("right"), new Box { Width = Sizing.Grow(), Height = Sizing.Grow() });
    }

    private void Drag(float toX) => _frames.Frame().Drag(new PointF(118.5f, 150f), new PointF(toX, 150f)).Release().Frame(2);

    [Fact]
    public void Panes_split_the_space_at_the_apps_size()
    {
        _frames.Frame();

        Assert.Equal(100f, _frames.RectOf("left").Width);
        Assert.Equal(368f - 106f, _frames.RectOf("right").Width);
    }

    [Fact]
    public void Dragging_the_divider_writes_the_size_and_shows_a_resize_cursor()
    {
        _frames.Frame().MovePointer(118.5f, 150f).Frame();
        Assert.Equal(MouseCursor.ResizeHorizontal, _frames.Frame(ui => ui.Cursor));

        Drag(218.5f);

        Assert.Equal(200f, _size);
        Assert.Equal(200f, _frames.RectOf("left").Width);
    }

    [Theory]
    [InlineData(0f, 50f)]
    [InlineData(400f, 368f - 6f - 100f)]
    public void Dragging_stops_at_the_minimum_sizes(float toX, float expected)
    {
        Drag(toX);

        Assert.Equal(expected, _size);
    }
}

public sealed class TabStripTests
{
    private readonly UiFrames _frames;
    private readonly List<string> _tabs = Enumerable.Range(1, 3).Select(i => $"Doc{i}").ToList();
    private readonly List<string> _closed = new();
    private int _selected;

    public TabStripTests() => _frames = new UiFrames(body: Body);

    private void Body(Ui ui)
    {
        using var screen = ui.Screen();
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
        _frames.Frame();

        _frames.ClickOn("Doc2");

        Assert.Equal(1, _selected);
        Assert.Empty(_closed);
    }

    [Fact]
    public void The_close_button_and_a_middle_click_close_a_tab()
    {
        _frames.Frame();

        _frames.ClickOn(ui => UiWidgets.CloseButtonId(ui.GetId("Doc3")));
        _frames.ClickOn("Doc2", PointerButton.Middle);

        Assert.Equal(["Doc3", "Doc2"], _closed);
        Assert.Equal(0, _selected);
    }

    [Fact]
    public void Overflowing_tabs_scroll_and_a_newly_selected_tab_scrolls_into_view()
    {
        _tabs.AddRange(Enumerable.Range(4, 7).Select(i => $"Doc{i}"));
        _frames.Frame(2);
        Assert.True(_frames.RectOf("Doc10").Right > 384f);

        _selected = 9;
        _frames.Frame(3);

        Assert.Equal(384f, _frames.RectOf("Doc10").Right, 0.01f);

        _frames.MovePointer(100f, 30f).Frame();
        _frames.Wheel(2).Frame(2);
        Assert.True(_frames.RectOf("Doc10").Right > 384f);
    }
}
