using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class SplitPaneTests
{
    private float _size = 100f;
    private readonly UiFrames _frames;

    public SplitPaneTests() => _frames = new UiFrames(new DefaultUiState(), body: Body);

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

    public TabStripTests() => _frames = new UiFrames(new DefaultUiState(), height: 600f, body: Body);

    private void Body(Ui ui)
    {
        using var screen = ui.Screen();
        var next = _selected;
        using (var strip = ui.TabStrip("tabs", listTooltip: "All tabs"))
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
        Overflow();
        var strip = _frames.RectOf("tabs");
        Assert.True(_frames.RectOf("Doc10").Right > strip.Right);

        _selected = 9;
        _frames.Frame(3);

        Assert.Equal(strip.Right, _frames.RectOf("Doc10").Right, 0.01f);

        _frames.MovePointer(100f, 30f).Frame();
        _frames.Wheel(2).Frame(2);
        Assert.True(_frames.RectOf("Doc10").Right > strip.Right);
    }

    [Fact]
    public void Only_overflowing_tabs_end_in_a_list_button()
    {
        _frames.Frame(2);
        Assert.False(HasListButton());

        Overflow();

        Assert.True(HasListButton());
        Assert.True(_frames.RectOf("tabs").Right <= ListButton().X);
    }

    [Fact]
    public void The_list_button_shows_its_tooltip()
    {
        Overflow();

        _frames.MovePointer(ListButton()).Frame();
        _frames.Wait(0.6).Frame(2);

        Assert.True(_frames.Drew("All tabs"));
    }

    [Fact]
    public void Overflowing_tabs_draw_no_scroll_bar()
    {
        Overflow();

        _frames.MovePointer(100f, 30f).Frame(2);

        Assert.False(_frames.Frame(ui => ui.TryGetRect(Ui.HorizontalThumbId(ui.GetId("tabs"), ScrollAxes.Horizontal), out _)));
    }

    [Fact]
    public void Picking_a_tab_from_the_list_selects_it_and_scrolls_it_into_view()
    {
        Overflow();

        _frames.ClickOn(ListButtonId).Frame();
        _frames.ClickOn(ui => ListItemId(ui, "Doc10")).Frame(3);

        Assert.Equal(9, _selected);
        Assert.Equal(_frames.RectOf("tabs").Right, _frames.RectOf("Doc10").Right, 0.01f);
    }

    [Fact]
    public void Picking_the_selected_tab_from_the_list_scrolls_it_back_into_view()
    {
        Overflow();
        _frames.MovePointer(100f, 30f).Frame();
        _frames.Wheel(-20).Frame(2);
        Assert.True(_frames.RectOf("Doc1").X < _frames.RectOf("tabs").X);

        _frames.ClickOn(ListButtonId).Frame();
        _frames.ClickOn(ui => ListItemId(ui, "Doc1")).Frame(3);

        Assert.Equal(0, _selected);
        Assert.Equal(_frames.RectOf("tabs").X, _frames.RectOf("Doc1").X, 0.01f);
    }

    [Fact]
    public void Tabs_that_fit_do_not_fade()
    {
        _frames.Frame(2);

        Assert.Empty(FadeSlices());
    }

    [Fact]
    public void Overflowing_tabs_fade_out_only_at_the_edges_with_more_tabs_past_them()
    {
        Overflow();
        var strip = _frames.RectOf("tabs");
        var fadeWidth = UiTheme.Dark.TabFadeWidth;

        Assert.All(FadeSlices(), s => Assert.True(s.X >= strip.Right - fadeWidth - 0.01f));
        Assert.NotEmpty(FadeSlices());

        _frames.MovePointer(100f, 30f).Frame();
        _frames.Wheel(-100).Frame(2);

        Assert.All(FadeSlices(), s => Assert.True(s.Right <= strip.X + fadeWidth + 0.01f));
        Assert.NotEmpty(FadeSlices());
    }

    [Fact]
    public void A_tab_under_the_fade_still_takes_clicks()
    {
        Overflow();
        var strip = _frames.RectOf("tabs");
        var x = strip.Right - 4f;
        var under = _tabs.FindLastIndex(t => _frames.RectOf(t).X < x);

        _frames.Click(x, strip.Y + strip.Height / 2);

        Assert.Equal(under, _selected);
    }

    private List<UiRect> FadeSlices()
    {
        var background = UiTheme.Dark.TabStripBackground & 0x00FFFFFFu;
        return _frames.Canvas.Rects
            .Where(r => (r.Inputs.Style.BackgroundColor & 0x00FFFFFFu) == background)
            .Where(r => r.Inputs.Style.BackgroundColor >> 24 is > 0 and < 0xFF)
            .Select(r => _frames.ToUi(r.Inputs.Position))
            .ToList();
    }

    [Fact]
    public void A_tab_icon_sits_in_the_tab_before_its_label()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using var screen = ui.Screen();
            using var strip = ui.TabStrip("icons");
            strip.Tab("Doc", selected: true, icon: new UiIcon(IconGlyph.FromFont("@")));
        }).Frame(2);

        var icon = frames.TextRect("@");
        var label = frames.TextRect("Doc");

        Assert.True(icon.Right <= label.X);
        Assert.True(frames.RectOf("Doc").Contains(new PointF(icon.X + icon.Width / 2, icon.Y + icon.Height / 2)));
    }

    [Fact]
    public void The_tab_list_shows_each_tabs_icon_beside_its_label()
    {
        var frames = new UiFrames(new DefaultUiState(), height: 600f, body: ui =>
        {
            using var screen = ui.Screen();
            using var strip = ui.TabStrip("tabs");
            for (var i = 1; i <= 10; i++)
                strip.Tab($"Doc{i}", i == 1, icon: new UiIcon(IconGlyph.FromFont($"@{i}")));
        }).Frame(3);

        frames.ClickOn(ListButtonId).Frame(2);

        var menu = frames.RectOf(ui => UiWidgets.MenuPopupId(ListButtonId(ui)));
        var icon = frames.TextRects()
            .Where(t => t.Text == "@10")
            .Select(t => t.Rect)
            .Single(r => menu.Contains(new PointF(r.X + 1f, r.Y + 1f)));
        var label = frames.TextRects()
            .Where(t => t.Text == "Doc10")
            .Select(t => t.Rect)
            .Single(r => menu.Contains(new PointF(r.X + 1f, r.Y + 1f)));
        Assert.True(icon.Right <= label.X);
    }

    private void Overflow()
    {
        _tabs.AddRange(Enumerable.Range(4, 7).Select(i => $"Doc{i}"));
        _frames.Frame(3);
    }

    private bool HasListButton() => _frames.Frame(ui => ui.TryGetRect(ListButtonId(ui), out _));

    private UiRect ListButton() => _frames.RectOf(ListButtonId);

    private static int ListButtonId(Ui ui) => UiWidgets.TabListButtonId(ui.GetId("tabs"));

    /// <summary>The list's item for <paramref name="tab"/>, keyed as the list keys it: under the
    /// list's menu, then under the tab's own id.</summary>
    private static int ListItemId(Ui ui, string tab)
    {
        var tabId = ui.GetId(tab);
        using (ui.UniqueId(UiWidgets.MenuPopupId(ListButtonId(ui))))
        using (ui.Id(tabId))
            return ui.GetId(tab);
    }
}
