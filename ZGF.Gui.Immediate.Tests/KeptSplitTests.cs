using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

/// <summary>A split whose side keeps its size as the dock space resizes, as a border layout's edges do: here a
/// "bottom" panel docked against the bottom of a "main" one, in a dock space whose height the test changes.</summary>
public sealed class KeptSplitTests
{
    private readonly DockLayout _layout = new() { Root = DockLayout.Group("main") };
    private readonly UiFrames _frames;
    private float _height = 600f;

    public KeptSplitTests()
    {
        _layout.Dock("bottom", _layout.Root, DockEdge.Bottom, 0.25f);
        _frames = new UiFrames(new DefaultUiState(), width: 800f, height: 1000f, body: ui =>
        {
            using var screen = ui.Screen();
            using var dock = ui.DockSpace("dock", _layout, static p => new DockTab(p), new Box { Width = 800f, Height = _height });
            while (dock.Next(out var panel))
                ui.Text("content:" + panel);
        });
        _frames.Frame(3);
    }

    private UiRect GroupRect(string panel) => _frames.RectOf(ui => DockWalker.NodeId(ui, "dock", _layout.GroupOf(panel)!));

    [Fact]
    public void Docking_against_an_edge_keeps_the_docked_side()
    {
        var split = Assert.IsType<DockSplit>(_layout.Root);

        Assert.Equal(SplitSide.Second, split.KeptSide);
    }

    [Fact]
    public void The_kept_side_starts_at_its_share_then_keeps_its_size_as_the_dock_space_grows()
    {
        var before = GroupRect("bottom").Height;

        _height = 900f;
        _frames.Frame(2);

        Assert.Equal(before, GroupRect("bottom").Height, 1f);
        Assert.True(GroupRect("main").Height > 600f * 0.75f);
        Assert.Equal(MathF.Round(600f * 0.25f), ((DockSplit)_layout.Root).KeptSize, 1f);
    }

    [Fact]
    public void Dragging_the_divider_sets_the_kept_size()
    {
        var divider = GroupRect("main").Bottom + 2f;
        _frames.MovePointer(400f, divider).Press().Frame();
        _frames.MovePointer(400f, divider - 50f).Frame();
        _frames.Release().Frame();
        var dragged = GroupRect("bottom").Height;

        _height = 900f;
        _frames.Frame(2);

        Assert.Equal(dragged, GroupRect("bottom").Height, 1f);
        Assert.True(dragged > MathF.Round(600f * 0.25f) + 40f);
    }

    [Fact]
    public void A_split_without_a_kept_side_still_shares_the_space()
    {
        _layout.Root = DockLayout.Split(Direction.Vertical, 0.5f, DockLayout.Group("main"), DockLayout.Group("bottom"));
        _frames.Frame(2);
        var before = GroupRect("bottom").Height;

        _height = 900f;
        _frames.Frame(2);

        Assert.True(GroupRect("bottom").Height > before + 100f);
    }
}
