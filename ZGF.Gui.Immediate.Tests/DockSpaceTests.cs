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

    private readonly UiFrames _frames;
    private readonly DockLayout _layout = new()
    {
        Root = DockLayout.Split(Direction.Horizontal, 0.5f, DockLayout.Group("a", "b"), DockLayout.Group("c")),
    };
    private readonly Dictionary<string, Counter> _counters = new();
    private DockTabMenu? _tabMenu;

    public DockSpaceTests() => _frames = new UiFrames(new DefaultUiState(), width: 800f, height: 600f, body: Body);

    private void Body(Ui ui)
    {
        using var dock = ui.DockSpace("dock", _layout, static p => new DockTab(p.ToUpperInvariant(), Closable: p != "c"));
        while (dock.Next(out var panel))
        {
            _counters[panel] = ui.GetState<Counter>(ui.GetId("counter"));
            ui.Text("content:" + panel);
        }
        _tabMenu = dock.TabMenu;
    }

    private UiRect TabRect(string panel) => _frames.RectOf(ui => DockWalker.TabId(ui, "dock", panel));

    private UiRect GroupRect(string panel) => _frames.RectOf(ui => DockWalker.NodeId(ui, "dock", _layout.GroupOf(panel)!));

    private static PointF Center(UiRect r) => new(r.X + r.Width / 2, r.Y + r.Height / 2);

    private void Drag(PointF from, params PointF[] path) => Drag(KeyModifiers.None, from, path);

    private void Drag(KeyModifiers modifiers, PointF from, params PointF[] path) =>
        _frames.Drag(modifiers, from, [new PointF(from.X + 20f, from.Y + 20f), .. path]);

    private void Drop(KeyModifiers modifiers = KeyModifiers.None) => _frames.Release(modifiers: modifiers).Frame(2);

    [Fact]
    public void Each_groups_active_panel_is_recorded_under_its_tabs()
    {
        _frames.Frame();

        Assert.True(_frames.Drew("content:a"));
        Assert.True(_frames.Drew("content:c"));
        Assert.False(_frames.Drew("content:b"));
        Assert.True(_frames.Drew("B"));
        Assert.True(GroupRect("a").Right <= GroupRect("c").X);
    }

    [Fact]
    public void Pressing_a_tab_brings_its_panel_to_the_front()
    {
        _frames.Frame();

        _frames.ClickOn(ui => DockWalker.TabId(ui, "dock", "b")).Frame();

        Assert.Equal("b", _layout.GroupOf("b")!.Active);
        Assert.True(_frames.Drew("content:b"));
        Assert.False(_frames.Drew("content:a"));
    }

    [Fact]
    public void Closing_a_tab_removes_its_panel_and_raises_PanelClosed()
    {
        var closed = new List<string>();
        _layout.PanelClosed += closed.Add;
        _frames.Frame();

        _frames.ClickOn(ui => UiWidgets.CloseButtonId(DockWalker.TabId(ui, "dock", "a")));

        Assert.Equal(["a"], closed);
        Assert.Equal(["b"], _layout.GroupOf("b")!.Panels);
    }

    [Fact]
    public void Right_clicking_a_tab_names_its_panel_for_a_context_menu_without_bringing_it_to_the_front()
    {
        _frames.Frame();
        var tab = TabRect("b");

        _frames.ClickOn(ui => DockWalker.TabId(ui, "dock", "b"), PointerButton.Right);

        Assert.Equal("b", _tabMenu!.Value.Panel);
        Assert.True(tab.Contains(_tabMenu.Value.At));
        Assert.Equal("a", _layout.GroupOf("b")!.Active);
        _frames.Frame();
        Assert.Null(_tabMenu);
    }

    [Fact]
    public void Dropping_a_tab_near_a_groups_right_side_splits_that_group()
    {
        _frames.Frame();
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
        _frames.Frame();
        var tab = TabRect("b");

        Drag(Center(TabRect("c")), Center(tab), new PointF(tab.X + 2f, tab.Y + tab.Height / 2));
        Drop();

        Assert.Equal(["a", "c", "b"], Assert.IsType<DockGroup>(_layout.Root).Panels);
        Assert.Equal("c", _layout.GroupOf("c")!.Active);
    }

    [Fact]
    public void Dropping_a_tab_in_a_groups_middle_adds_it_to_that_group()
    {
        _frames.Frame();
        var target = Center(GroupRect("c"));

        Drag(Center(TabRect("b")), target, target with { X = target.X + 32f });
        Drop();

        Assert.Equal(["c", "b"], _layout.GroupOf("c")!.Panels);
        Assert.Empty(_layout.Windows);
    }

    [Fact]
    public void Dropping_a_tab_near_the_dock_spaces_rim_splits_the_whole_layout()
    {
        _frames.Frame();

        Drag(Center(TabRect("b")), new PointF(400f, 300f), new PointF(400f, 590f));
        Drop();

        var root = Assert.IsType<DockSplit>(_layout.Root);
        Assert.Equal(Direction.Vertical, root.Direction);
        Assert.Equal(["b"], Assert.IsType<DockGroup>(root.Second).Panels);
    }

    [Fact]
    public void Dropping_a_tab_with_ctrl_held_floats_it_in_a_window()
    {
        _frames.Frame();

        Drag(KeyModifiers.Control, Center(TabRect("b")), new PointF(150f, 400f));
        Drop(KeyModifiers.Control);

        var window = Assert.Single(_layout.Windows);
        Assert.Same(window, _layout.WindowOf(_layout.GroupOf("b")!));
        Assert.True(_frames.Drew("content:b"));
        var rect = _frames.RectOf(ui => DockWalker.WindowId(ui, "dock", window));
        Assert.True(rect.Contains(new PointF(150f, 400f)));
    }

    [Fact]
    public void A_window_follows_its_title_bar_and_docks_where_it_is_dropped()
    {
        var window = _layout.Float("b", new UiRect(100f, 100f, 300f, 200f));
        _frames.Frame();
        var title = new PointF(150f, 110f);

        Drag(title, new PointF(250f, 210f));
        Assert.Equal(new UiRect(200f, 200f, 300f, 200f), window.Rect);

        var target = Center(GroupRect("c"));
        _frames.MovePointer(target).Frame();
        Drop();

        Assert.Empty(_layout.Windows);
        Assert.Equal(["c", "b"], _layout.GroupOf("c")!.Panels);
    }

    [Fact]
    public void Dragging_a_window_with_ctrl_held_moves_it_without_docking()
    {
        var window = _layout.Float("b", new UiRect(100f, 100f, 300f, 200f));
        _frames.Frame();

        Drag(KeyModifiers.Control, new PointF(150f, 110f), new PointF(250f, 210f));
        Drop(KeyModifiers.Control);

        Assert.Same(window, Assert.Single(_layout.Windows));
        Assert.Equal(new UiRect(200f, 200f, 300f, 200f), window.Rect);
    }

    [Fact]
    public void Dragging_a_windows_corner_resizes_it()
    {
        var window = _layout.Float("b", new UiRect(100f, 100f, 300f, 200f));
        _frames.Frame();

        Drag(new PointF(398f, 298f), new PointF(458f, 338f));
        Drop();

        Assert.Equal(new UiRect(100f, 100f, 360f, 240f), window.Rect);
    }

    [Fact]
    public void Pressing_a_window_behind_another_brings_it_to_the_front()
    {
        var back = _layout.Float("a", new UiRect(100f, 100f, 300f, 200f));
        _layout.Float("b", new UiRect(300f, 100f, 300f, 200f));
        _frames.Frame();

        _frames.Click(150f, 250f);

        Assert.Same(back, _layout.Windows[^1]);
    }

    [Fact]
    public void Escape_cancels_a_drag()
    {
        _frames.Frame();
        Drag(Center(TabRect("b")), new PointF(150f, 400f));

        _frames.Key(KeyboardKey.Escape).Frame();
        Drop();

        Assert.Empty(_layout.Windows);
        Assert.Equal(["a", "b"], _layout.GroupOf("a")!.Panels);
    }

    [Fact]
    public void A_panels_widget_state_follows_it_when_it_moves()
    {
        _frames.Frame();
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
        _frames.Body = ui =>
        {
            using var dock = ui.DockSpace("dock", layout, static p => new DockTab(p, Movable: p != "editor"));
            while (dock.Next(out var panel))
                ui.Text("content:" + panel);
        };
        _frames.Frame();
        Assert.False(_frames.Drew("editor"));
        var editor = _frames.RectOf(ui => DockWalker.NodeId(ui, "dock", layout.GroupOf("editor")!));
        var tool = _frames.RectOf(ui => DockWalker.TabId(ui, "dock", "tool"));

        var center = Center(editor);
        _frames.Drag(new PointF(tool.X + 10f, tool.Y + 10f), center with { X = center.X - 40f }, center with { X = center.X + 20f }).Release().Frame();

        Assert.Equal(["editor"], layout.GroupOf("editor")!.Panels);
        Assert.Empty(layout.Windows);
        var root = Assert.IsType<DockSplit>(layout.Root);
        Assert.Equal(["editor"], Assert.IsType<DockGroup>(root.First).Panels);
        Assert.Equal(["tool"], Assert.IsType<DockGroup>(root.Second).Panels);
    }

    [Fact]
    public void Pressing_inside_a_group_focuses_its_front_panel()
    {
        _frames.Frame();

        _frames.Click(GroupRect("c"));

        Assert.Equal("c", _layout.Focused);
    }

    [Fact]
    public void Dragging_a_groups_only_tab_toward_its_own_right_side_moves_it_there_beside_a_new_focused_panel()
    {
        _layout.Root = DockLayout.Group("a");
        _layout.NewPanel = () => "new";
        _frames.Frame();
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
        _frames.Frame();
        var root = (DockSplit)_layout.Root;
        var divider = GroupRect("a").Right + 2.5f;

        Drag(new PointF(divider, 300f), new PointF(divider + 100f, 300f));
        Drop();

        Assert.Equal((400f + 100f) / 800f, root.Ratio, 3);
    }

    [Fact]
    public void Picking_another_tab_counts_as_one_change_and_picking_it_again_as_none()
    {
        _frames.Frame();
        var before = _layout.Changes;

        _frames.ClickOn(ui => DockWalker.TabId(ui, "dock", "b")).Frame();
        var afterPick = _layout.Changes;
        _frames.ClickOn(ui => DockWalker.TabId(ui, "dock", "b")).Frame();

        Assert.Equal("b", _layout.GroupOf("b")!.Active);
        Assert.Equal(before + 1, afterPick);
        Assert.Equal(afterPick, _layout.Changes);
    }

    [Fact]
    public void Changes_that_leave_the_layout_as_it_was_count_as_none()
    {
        _frames.Frame();
        var window = _layout.Float("b", new UiRect(100f, 100f, 300f, 200f));
        var before = _layout.Changes;

        _layout.Dock("a", _layout.GroupOf("a")!, DockEdge.Center, 0);
        _layout.Retain(static _ => true);
        _layout.BringToFront(window);
        _frames.Press().Release().Frame();

        Assert.Equal(before, _layout.Changes);
    }

    [Fact]
    public void Docking_a_dragged_window_counts_as_one_change()
    {
        _layout.Float("b", new UiRect(100f, 100f, 300f, 200f));
        _frames.Frame();
        var before = _layout.Changes;

        Drag(new PointF(150f, 110f), new PointF(250f, 210f));
        _frames.MovePointer(Center(GroupRect("c"))).Frame();
        Drop();

        Assert.Empty(_layout.Windows);
        Assert.Equal(before + 1, _layout.Changes);
    }

    [Fact]
    public void Dragging_a_divider_counts_as_one_change_when_it_ends()
    {
        _frames.Frame();
        var divider = GroupRect("a").Right + 2.5f;
        var before = _layout.Changes;

        Drag(new PointF(divider, 300f), new PointF(divider + 50f, 300f), new PointF(divider + 100f, 300f));
        var whileDragging = _layout.Changes;
        Drop();

        Assert.Equal(before, whileDragging);
        Assert.Equal(before + 1, _layout.Changes);
    }
}
