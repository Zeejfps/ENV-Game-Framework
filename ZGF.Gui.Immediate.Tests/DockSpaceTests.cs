using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class DockSpaceTests
{
    private sealed class Counter
    {
        public int Value;
    }

    private readonly UiFrames _frames = new(width: 800f, height: 600f);
    private readonly DockLayout _layout = new()
    {
        Root = DockLayout.Split(Direction.Horizontal, 0.5f, DockLayout.Group("a", "b"), DockLayout.Group("c")),
    };
    private readonly Dictionary<string, Counter> _counters = new();

    private void Body(Ui ui)
    {
        using var dock = ui.DockSpace("dock", _layout, static p => new DockTab(p.ToUpperInvariant(), Closable: p != "c"));
        while (dock.Next(out var panel))
        {
            _counters[panel] = ui.GetState<Counter>(ui.GetId("counter"));
            ui.Text("content:" + panel);
        }
    }

    private UiRect Rect(Func<Ui, int> id) => _frames.RectOf(Body, id);

    private UiRect TabRect(string panel) => Rect(ui => DockWalker.TabId(ui, "dock", panel));

    private UiRect GroupRect(string panel) => Rect(ui => DockWalker.NodeId(ui, "dock", _layout.GroupOf(panel)!));

    private static PointF Center(UiRect r) => new(r.X + r.Width / 2, r.Y + r.Height / 2);

    private void Drag(PointF from, params PointF[] path) => Drag(KeyModifiers.None, from, path);

    private void Drag(KeyModifiers modifiers, PointF from, params PointF[] path)
    {
        _frames.MovePointer(from.X, from.Y);
        _frames.Run(0f, Body);
        _frames.Press(modifiers);
        _frames.Run(0f, Body);
        _frames.MovePointer(from.X + 20f, from.Y + 20f);
        _frames.Run(0f, Body);
        foreach (var point in path)
        {
            _frames.MovePointer(point.X, point.Y);
            _frames.Run(0f, Body);
        }
    }

    private void Drop(KeyModifiers modifiers = KeyModifiers.None)
    {
        _frames.Release(modifiers: modifiers);
        _frames.Run(0f, Body);
        _frames.Run(0f, Body);
    }

    [Fact]
    public void Each_groups_active_panel_is_recorded_under_its_tabs()
    {
        _frames.Run(0f, Body);

        Assert.True(_frames.Drew("content:a"));
        Assert.True(_frames.Drew("content:c"));
        Assert.False(_frames.Drew("content:b"));
        Assert.True(_frames.Drew("B"));
        Assert.True(GroupRect("a").Right <= GroupRect("c").X);
    }

    [Fact]
    public void Pressing_a_tab_brings_its_panel_to_the_front()
    {
        _frames.Run(0f, Body);

        _frames.ClickOn(Body, ui => DockWalker.TabId(ui, "dock", "b"));
        _frames.Run(0f, Body);

        Assert.Equal("b", _layout.GroupOf("b")!.Active);
        Assert.True(_frames.Drew("content:b"));
        Assert.False(_frames.Drew("content:a"));
    }

    [Fact]
    public void Closing_a_tab_removes_its_panel_and_raises_PanelClosed()
    {
        var closed = new List<string>();
        _layout.PanelClosed += closed.Add;
        _frames.Run(0f, Body);

        _frames.ClickOn(Body, ui => HashCode.Combine(DockWalker.TabId(ui, "dock", "a"), "close"));

        Assert.Equal(["a"], closed);
        Assert.Equal(["b"], _layout.GroupOf("b")!.Panels);
    }

    [Fact]
    public void Dropping_a_tab_near_a_groups_right_side_splits_that_group()
    {
        _frames.Run(0f, Body);
        var group = GroupRect("c");
        var target = Center(group);

        Drag(Center(TabRect("b")), target, target with { X = group.Right - 60f });
        Drop();

        var root = Assert.IsType<DockSplit>(_layout.Root);
        var split = Assert.IsType<DockSplit>(root.Second);
        Assert.Equal(Direction.Horizontal, split.Direction);
        Assert.Equal(["b"], Assert.IsType<DockGroup>(split.Second).Panels);
        Assert.True(_frames.Drew("content:b"));
    }

    [Fact]
    public void Dropping_a_tab_on_another_groups_tabs_inserts_it_there()
    {
        _frames.Run(0f, Body);
        var tab = TabRect("b");

        Drag(Center(TabRect("c")), Center(tab), new PointF(tab.X + 2f, tab.Y + tab.Height / 2));
        Drop();

        Assert.Equal(["a", "c", "b"], Assert.IsType<DockGroup>(_layout.Root).Panels);
        Assert.Equal("c", _layout.GroupOf("c")!.Active);
    }

    [Fact]
    public void Dropping_a_tab_in_a_groups_middle_adds_it_to_that_group()
    {
        _frames.Run(0f, Body);
        var target = Center(GroupRect("c"));

        Drag(Center(TabRect("b")), target, target with { X = target.X + 32f });
        Drop();

        Assert.Equal(["c", "b"], _layout.GroupOf("c")!.Panels);
        Assert.Empty(_layout.Windows);
    }

    [Fact]
    public void Dropping_a_tab_near_the_dock_spaces_rim_splits_the_whole_layout()
    {
        _frames.Run(0f, Body);

        Drag(Center(TabRect("b")), new PointF(400f, 300f), new PointF(400f, 590f));
        Drop();

        var root = Assert.IsType<DockSplit>(_layout.Root);
        Assert.Equal(Direction.Vertical, root.Direction);
        Assert.Equal(["b"], Assert.IsType<DockGroup>(root.Second).Panels);
    }

    [Fact]
    public void Dropping_a_tab_with_ctrl_held_floats_it_in_a_window()
    {
        _frames.Run(0f, Body);

        Drag(KeyModifiers.Control, Center(TabRect("b")), new PointF(150f, 400f));
        Drop(KeyModifiers.Control);

        var window = Assert.Single(_layout.Windows);
        Assert.Same(window, _layout.WindowOf(_layout.GroupOf("b")!));
        Assert.True(_frames.Drew("content:b"));
        var rect = Rect(ui => DockWalker.WindowId(ui, "dock", window));
        Assert.True(rect.Contains(new PointF(150f, 400f)));
    }

    [Fact]
    public void A_window_follows_its_title_bar_and_docks_where_it_is_dropped()
    {
        var window = _layout.Float("b", new UiRect(100f, 100f, 300f, 200f));
        _frames.Run(0f, Body);
        var title = new PointF(150f, 110f);

        Drag(title, new PointF(250f, 210f));
        Assert.Equal(new UiRect(200f, 200f, 300f, 200f), window.Rect);

        var target = Center(GroupRect("c"));
        _frames.MovePointer(target.X, target.Y);
        _frames.Run(0f, Body);
        Drop();

        Assert.Empty(_layout.Windows);
        Assert.Equal(["c", "b"], _layout.GroupOf("c")!.Panels);
    }

    [Fact]
    public void Dragging_a_window_with_ctrl_held_moves_it_without_docking()
    {
        var window = _layout.Float("b", new UiRect(100f, 100f, 300f, 200f));
        _frames.Run(0f, Body);

        Drag(KeyModifiers.Control, new PointF(150f, 110f), new PointF(250f, 210f));
        Drop(KeyModifiers.Control);

        Assert.Same(window, Assert.Single(_layout.Windows));
        Assert.Equal(new UiRect(200f, 200f, 300f, 200f), window.Rect);
    }

    [Fact]
    public void Dragging_a_windows_corner_resizes_it()
    {
        var window = _layout.Float("b", new UiRect(100f, 100f, 300f, 200f));
        _frames.Run(0f, Body);

        Drag(new PointF(398f, 298f), new PointF(458f, 338f));
        Drop();

        Assert.Equal(new UiRect(100f, 100f, 360f, 240f), window.Rect);
    }

    [Fact]
    public void Pressing_a_window_behind_another_brings_it_to_the_front()
    {
        var back = _layout.Float("a", new UiRect(100f, 100f, 300f, 200f));
        _layout.Float("b", new UiRect(300f, 100f, 300f, 200f));
        _frames.Run(0f, Body);

        _frames.Click(150f, 250f, Body);

        Assert.Same(back, _layout.Windows[^1]);
    }

    [Fact]
    public void Escape_cancels_a_drag()
    {
        _frames.Run(0f, Body);
        Drag(Center(TabRect("b")), new PointF(150f, 400f));

        _frames.Key(KeyboardKey.Escape);
        _frames.Run(0f, Body);
        Drop();

        Assert.Empty(_layout.Windows);
        Assert.Equal(["a", "b"], _layout.GroupOf("a")!.Panels);
    }

    [Fact]
    public void A_panels_widget_state_follows_it_when_it_moves()
    {
        _frames.Run(0f, Body);
        _counters["c"].Value = 7;

        Drag(KeyModifiers.Control, Center(TabRect("c")), new PointF(150f, 400f));
        Drop(KeyModifiers.Control);

        Assert.NotNull(_layout.WindowOf(_layout.GroupOf("c")!));
        Assert.Equal(7, _counters["c"].Value);
    }

    [Fact]
    public void A_fixed_panel_alone_shows_no_tab_and_takes_panels_only_beside_it()
    {
        var layout = new DockLayout
        {
            Root = DockLayout.Split(Direction.Horizontal, 0.5f, DockLayout.Group("tool"), DockLayout.Group("editor")),
        };
        void Fixed(Ui ui)
        {
            using var dock = ui.DockSpace("dock", layout, static p => new DockTab(p, Movable: p != "editor"));
            while (dock.Next(out var panel))
                ui.Text("content:" + panel);
        }
        _frames.Run(0f, Fixed);
        Assert.False(_frames.Drew("editor"));
        var editor = _frames.RectOf(Fixed, ui => DockWalker.NodeId(ui, "dock", layout.GroupOf("editor")!));
        var tool = _frames.RectOf(Fixed, ui => DockWalker.TabId(ui, "dock", "tool"));

        var center = Center(editor);
        _frames.MovePointer(tool.X + 10f, tool.Y + 10f);
        _frames.Run(0f, Fixed);
        _frames.Press();
        _frames.Run(0f, Fixed);
        foreach (var point in new[] { center with { X = center.X - 40f }, center with { X = center.X + 20f } })
        {
            _frames.MovePointer(point.X, point.Y);
            _frames.Run(0f, Fixed);
        }
        _frames.Release();
        _frames.Run(0f, Fixed);

        Assert.Equal(["editor"], layout.GroupOf("editor")!.Panels);
        Assert.Empty(layout.Windows);
        var root = Assert.IsType<DockSplit>(layout.Root);
        Assert.Equal(["editor"], Assert.IsType<DockGroup>(root.First).Panels);
        Assert.Equal(["tool"], Assert.IsType<DockGroup>(root.Second).Panels);
    }

    [Fact]
    public void Pressing_inside_a_group_focuses_its_front_panel()
    {
        _frames.Run(0f, Body);

        _frames.Click(Center(GroupRect("c")).X, Center(GroupRect("c")).Y, Body);

        Assert.Equal("c", _layout.Focused);
    }

    [Fact]
    public void Dragging_a_groups_only_tab_toward_its_own_right_side_moves_it_there_beside_a_new_focused_panel()
    {
        _layout.Root = DockLayout.Group("a");
        _layout.NewPanel = () => "new";
        _frames.Run(0f, Body);
        var group = GroupRect("a");
        var target = Center(group);

        Drag(Center(TabRect("a")), target, target with { X = group.Right - 60f });
        Drop();

        var root = Assert.IsType<DockSplit>(_layout.Root);
        Assert.Equal(["new"], Assert.IsType<DockGroup>(root.First).Panels);
        Assert.Equal(["a"], Assert.IsType<DockGroup>(root.Second).Panels);
        Assert.Equal("new", _layout.Focused);
        Assert.True(_frames.Drew("content:new"));
        Assert.True(_frames.Drew("content:a"));
    }

    [Fact]
    public void Dragging_a_divider_writes_the_splits_ratio()
    {
        _frames.Run(0f, Body);
        var root = (DockSplit)_layout.Root;
        var divider = GroupRect("a").Right + 2.5f;

        Drag(new PointF(divider, 300f), new PointF(divider + 100f, 300f));
        Drop();

        Assert.Equal((400f + 100f) / 800f, root.Ratio, 3);
    }
}
