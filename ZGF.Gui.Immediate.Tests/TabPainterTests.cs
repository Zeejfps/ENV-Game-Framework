using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class TabPainterTests
{
    private const uint Underline = 0xFF17CDFF;

    private readonly Dictionary<string, TabState> _states = new();
    private readonly List<string> _selected = new();
    private readonly List<string> _closed = new();
    private readonly UiFrames _frames;
    private string _current = "Alpha";

    public TabPainterTests()
    {
        var state = new DefaultUiState { Theme = UiTheme.Dark with { TabPainter = PaintUnderlined } };
        _frames = new UiFrames(state, width: 600f, body: ui =>
        {
            using var screen = ui.Screen();
            using var strip = ui.TabStrip("strip");
            foreach (var label in new[] { "Alpha", "Beta" })
            {
                var tab = strip.Tab(label, label == _current);
                if (tab.Selected)
                    _selected.Add(label);
                if (tab.Closed)
                    _closed.Add(label);
            }
        });
    }

    private void PaintUnderlined(Ui ui, in TabState state, in TabFace face)
    {
        _states[face.Label] = state;
        using (ui.Row(state.Id, new Box { Width = Sizing.Fit(), Height = 30f, Padding = new Spacing(10f, 0f), Gap = 6f }))
        {
            ui.Text(face.Label.ToUpperInvariant(), color: state.Selected ? Underline : 0xFFFFFFFF);
            var close = state.Close;
            if (close != null)
                ui.PaintCloseButton(close.Value, size: 14f);
        }
    }

    [Fact]
    public void Theme_painter_draws_every_tab_with_the_framework_behavior()
    {
        _frames.Frame(2);

        Assert.True(_frames.Drew("ALPHA"));
        Assert.False(_frames.Drew("Alpha"));
        Assert.True(_states["Alpha"].Selected);
        Assert.NotNull(_states["Beta"].Close);

        _frames.Click(_frames.RectOf("Beta")).Frame();

        Assert.Equal(["Beta"], _selected);
    }

    [Fact]
    public void A_close_button_the_painter_records_closes_its_tab()
    {
        _frames.Frame(2);
        var close = _states["Beta"].Close ?? throw new InvalidOperationException("Beta is closable.");

        _frames.Click(_frames.RectOf(_ => close.Id)).Frame();

        Assert.Equal(["Beta"], _closed);
        Assert.Empty(_selected);
    }
}

public sealed class DockHeaderPainterTests
{
    private readonly DockLayout _layout = new()
    {
        Root = DockLayout.Split(Direction.Horizontal, 0.5f, DockLayout.Group("a", "b"), DockLayout.Group("c")),
    };

    private readonly Dictionary<string, int> _menuIds = new();
    private readonly Dictionary<string, int> _closeIds = new();
    private readonly Dictionary<string, int> _gripIds = new();
    private readonly UiFrames _frames;
    private DockTabMenu? _tabMenu;
    private DockDrop? _drop;

    public DockHeaderPainterTests() => _frames = new UiFrames(new DefaultUiState(), width: 800f, height: 600f, body: Body);

    private void Body(Ui ui)
    {
        using var dock = ui.DockSpace("dock", _layout, static p => new DockTab(p.ToUpperInvariant(), Closable: p != "c"), header: PaintHeader,
            drop: PaintDrop);
        while (dock.Next(out var panel))
            ui.Text("content:" + panel);
        _tabMenu = dock.TabMenu ?? _tabMenu;
    }

    private void PaintDrop(Ui ui, in DockDrop drop)
    {
        _drop = drop;
        ui.PaintDockDrop(drop);
    }

    private void PaintHeader(Ui ui, in DockHeader header)
    {
        using (ui.Row(new Box { Width = Sizing.Grow(), Height = 30f, Gap = 4f }))
        {
            var grip = header.Grip(header.IdOf("grip"));
            _gripIds[header.Panel] = grip.Id;
            ui.Leaf(grip.Id, new Box { Width = 12f, Height = 20f });
            if (header.PanelCount > 1)
                header.Tabs(new Box { Width = Sizing.Grow() });
            else
            {
                ui.Text("title:" + header.Tab.Title, id: header.TitleId);
                ui.Spacer();
            }
            var menu = header.MenuButton(header.IdOf("menu"), "Panel menu");
            _menuIds[header.Panel] = menu.Id;
            ui.Leaf(menu.Id, new Box { Width = 20f, Height = 20f });
            var close = header.CloseButton(header.IdOf("close"));
            if (close != null)
            {
                _closeIds[header.Panel] = close.Value.Id;
                ui.PaintCloseButton(close.Value);
            }
        }
    }

    private static PointF Center(UiRect r) => new(r.X + r.Width / 2, r.Y + r.Height / 2);

    [Fact]
    public void Header_shows_tabs_for_a_group_of_several_and_a_title_for_one_alone()
    {
        _frames.Frame(2);

        Assert.True(_frames.Drew("A"));
        Assert.True(_frames.Drew("B"));
        Assert.True(_frames.Drew("title:C"));
        Assert.True(_frames.Drew("content:a"));
        Assert.True(_frames.Drew("content:c"));
        Assert.False(_closeIds.ContainsKey("c"));
    }

    [Fact]
    public void Menu_button_asks_for_the_front_panels_menu_below_it()
    {
        _frames.Frame(2);
        var button = _frames.RectOf(_ => _menuIds["c"]);

        _frames.Click(button).Frame();

        Assert.NotNull(_tabMenu);
        Assert.Equal("c", _tabMenu.Value.Panel);
        Assert.Equal(button.Bottom, _tabMenu.Value.At.Y);
    }

    [Fact]
    public void Close_button_closes_the_front_panel()
    {
        _frames.Frame(2);

        _frames.Click(_frames.RectOf(_ => _closeIds["a"])).Frame(2);

        Assert.False(_layout.Contains("a"));
        Assert.True(_layout.Contains("b"));
    }

    private PointF Grip(string panel) => Center(_frames.RectOf(_ => _gripIds[panel]));

    private UiRect GroupRect(string panel) => _frames.RectOf(ui => DockWalker.NodeId(ui, "dock", _layout.GroupOf(panel)!));

    private void DragGrip(string panel, PointF to)
    {
        var grip = Grip(panel);
        _frames.Drag(grip, [new PointF(grip.X + 20f, grip.Y + 20f), to, to with { X = to.X + 2f }]);
    }

    [Fact]
    public void The_grip_shows_an_open_hand_closed_while_held_and_arrows_to_all_sides_while_dragging()
    {
        _frames.Frame(2);
        var grip = Grip("c");

        _frames.MovePointer(grip).Frame();
        Assert.Equal(MouseCursor.Grab, _frames.Cursor);

        _frames.Press().Frame();
        Assert.Equal(MouseCursor.Grabbing, _frames.Cursor);

        _frames.MovePointer(grip.X - 60f, grip.Y + 60f).Frame().MovePointer(200f, 300f).Frame();
        Assert.Equal(MouseCursor.Move, _frames.Cursor);

        _frames.Release().Frame(2);
        Assert.Equal(MouseCursor.Default, _frames.Cursor);
    }

    [Fact]
    public void Dropping_in_the_middle_of_a_panel_alone_makes_the_two_tabs_of_one_group()
    {
        _frames.Frame(2);

        var group = GroupRect("c");
        var title = _frames.RectOf(ui => DockWalker.TitleId(DockWalker.NodeId(ui, "dock", _layout.GroupOf("c")!)));

        DragGrip("a", Center(group));
        var drop = _drop!.Value;
        Assert.Equal(DockDropKind.Tab, drop.Kind);
        Assert.Equal("Release to add as tab", drop.Hint);
        Assert.Equal("A", drop.Tab.Title);
        // The ghost is the body below the header, and the tab goes in the header just after the lone panel's title.
        Assert.Equal(group.Bottom, drop.Ghost.Bottom);
        Assert.True(drop.Ghost.Y > group.Y);
        var slot = drop.TabSlot!.Value;
        Assert.Equal(group.Y, slot.Y);
        Assert.Equal(drop.Ghost.Y, slot.Bottom);
        Assert.InRange(slot.X, title.Right, title.Right + 8f);
        Assert.Equal(group.Right, slot.Right);
        _frames.Release().Frame(2);

        Assert.Equal(["c", "a"], _layout.GroupOf("c")!.Panels);
        Assert.Equal("a", _layout.GroupOf("c")!.Active);
        Assert.True(_frames.Drew("A"));
        Assert.True(_frames.Drew("C"));
    }

    [Fact]
    public void Dropping_in_the_middle_of_a_group_of_tabs_shows_the_new_tab_after_its_last()
    {
        _frames.Frame(2);
        var last = _frames.RectOf(ui => DockWalker.TabId(ui, "dock", "b"));

        DragGrip("c", Center(GroupRect("a")));

        var slot = _drop!.Value.TabSlot!.Value;
        Assert.Equal(last.Right, slot.X);
        _frames.Release().Frame(2);
        Assert.Equal(["a", "b", "c"], _layout.GroupOf("a")!.Panels);
    }

    [Fact]
    public void Dropping_on_the_title_of_a_panel_alone_adds_the_dragged_one_as_its_last_tab()
    {
        _frames.Frame(2);
        var group = GroupRect("c");

        DragGrip("a", new PointF(group.X + 60f, group.Y + 15f));
        Assert.Equal(DockDropKind.Tab, _drop!.Value.Kind);
        _frames.Release().Frame(2);

        Assert.Equal(["c", "a"], _layout.GroupOf("c")!.Panels);
    }

    [Fact]
    public void Dropping_near_a_side_of_a_panel_alone_splits_it_and_says_so_while_dragging()
    {
        _frames.Frame(2);
        var group = GroupRect("c");

        DragGrip("a", new PointF(group.Right - 50f, group.Y + group.Height / 2));
        Assert.Equal(DockDropKind.Split, _drop!.Value.Kind);
        Assert.Equal(DockEdge.Right, _drop.Value.Edge);
        Assert.Equal("Release to split right", _drop.Value.Hint);
        Assert.True(_frames.Drew("Release to split right"));
        Assert.True(_frames.Drew("A"));
        _frames.Release().Frame(2);

        var split = Assert.IsType<DockSplit>(Assert.IsType<DockSplit>(_layout.Root).Second);
        Assert.Equal(["a"], Assert.IsType<DockGroup>(split.Second).Panels);
    }

    [Fact]
    public void Dragging_the_grip_with_ctrl_floats_the_front_panel()
    {
        _frames.Frame(2);
        var grip = Center(_frames.RectOf(_ => _gripIds["c"]));

        _frames.Drag(KeyModifiers.Control, grip, [new PointF(grip.X + 20f, grip.Y + 20f), new PointF(300f, 300f)]);
        _frames.Release(modifiers: KeyModifiers.Control).Frame(2);

        var window = Assert.Single(_layout.Windows);
        Assert.Equal(["c"], DockLayout.PanelsIn(window.Root));
    }
}

public sealed class TabPainterAllocationTests
{
    private static readonly DockLayout Layout = new()
    {
        Root = DockLayout.Split(Direction.Horizontal, 0.5f, DockLayout.Group("a", "b"), DockLayout.Group("c")),
    };

    private static void Scene(Ui ui)
    {
        using var screen = ui.Screen();
        using (var strip = ui.TabStrip("strip"))
        {
            strip.Tab("One", true);
            strip.Tab("Two", false);
        }
        using var dock = ui.DockSpace("dock", Layout, static p => new DockTab(p), header: PaintHeader);
        while (dock.Next(out _))
            ui.Leaf(new Box { Width = 10f, Height = 10f });
    }

    private static void PaintTab(Ui ui, in TabState state, in TabFace face)
    {
        using (ui.Row(state.Id, new Box { Width = Sizing.Fit(), Height = 30f }))
            ui.Text(face.Label);
    }

    private static void PaintHeader(Ui ui, in DockHeader header)
    {
        using (ui.Row(new Box { Width = Sizing.Grow(), Height = 30f }))
        {
            var grip = header.Grip(header.IdOf("grip"));
            ui.Leaf(grip.Id, new Box { Width = 12f, Height = 20f });
            header.Tabs(new Box { Width = Sizing.Grow() });
            var menu = header.MenuButton(header.IdOf("menu"), "Panel menu");
            ui.Leaf(menu.Id, new Box { Width = 20f, Height = 20f });
        }
    }

    private static long AllocatedPerRun(UiTheme theme, Action<Ui> scene)
    {
        var frames = new UiFrames(new DefaultUiState { Theme = theme }, width: 800f, height: 400f, body: scene);
        frames.MovePointer(30f, 30f);
        for (var i = 0; i < 4; i++)
            frames.Run(0f, scene);
        frames.Canvas.Recording = false;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 20; i++)
            frames.Run(0f, scene);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact]
    public void Theme_painted_tabs_and_painted_dock_headers_allocate_no_more_than_the_stock_ones()
    {
        var stock = AllocatedPerRun(UiTheme.Dark, Scene);
        var painted = AllocatedPerRun(UiTheme.Dark with { TabPainter = PaintTab }, Scene);

        Assert.Equal(stock, painted);
    }
}
