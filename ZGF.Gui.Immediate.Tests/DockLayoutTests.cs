using System.Text.Json;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class DockLayoutTests
{
    private static DockLayout TwoGroups() => new()
    {
        Root = DockLayout.Split(Direction.Horizontal, 0.5f, DockLayout.Group("a", "b"), DockLayout.Group("c")),
    };

    [Fact]
    public void Docking_beside_a_group_splits_it_and_gives_the_panel_its_share()
    {
        var layout = TwoGroups();
        var right = layout.GroupOf("c")!;

        layout.Dock("b", right, DockEdge.Bottom, 0.3f);

        var root = Assert.IsType<DockSplit>(layout.Root);
        var split = Assert.IsType<DockSplit>(root.Second);
        Assert.Equal(Direction.Vertical, split.Direction);
        Assert.Same(right, split.First);
        Assert.Equal(["b"], Assert.IsType<DockGroup>(split.Second).Panels);
        Assert.Equal(0.7f, split.Ratio, 3);
        Assert.Equal(["a"], layout.GroupOf("a")!.Panels);
    }

    [Fact]
    public void Moving_the_last_panel_out_of_a_group_collapses_its_split()
    {
        var layout = TwoGroups();

        layout.Dock("c", layout.GroupOf("a")!, DockEdge.Center);

        var group = Assert.IsType<DockGroup>(layout.Root);
        Assert.Equal(["a", "b", "c"], group.Panels);
        Assert.Equal("c", group.Active);
    }

    [Fact]
    public void Docking_against_a_split_the_move_collapses_docks_against_what_replaced_it()
    {
        var layout = TwoGroups();
        var left = layout.GroupOf("a")!;

        layout.Dock("c", layout.Root, DockEdge.Left, 0.25f);

        var root = Assert.IsType<DockSplit>(layout.Root);
        Assert.Equal(["c"], Assert.IsType<DockGroup>(root.First).Panels);
        Assert.Same(left, root.Second);
        Assert.Equal(0.25f, root.Ratio);
    }

    [Theory]
    [InlineData(0, new[] { "c", "a", "b" })]
    [InlineData(1, new[] { "a", "c", "b" })]
    [InlineData(-1, new[] { "a", "b", "c" })]
    public void Docking_into_a_group_inserts_at_the_index(int index, string[] expected)
    {
        var layout = TwoGroups();

        layout.Dock("c", layout.GroupOf("a")!, DockEdge.Center, index: index);

        Assert.Equal(expected, layout.GroupOf("a")!.Panels);
    }

    [Fact]
    public void Docking_a_panel_into_its_own_group_reorders_it()
    {
        var layout = TwoGroups();
        var group = layout.GroupOf("a")!;

        layout.Dock("a", group, DockEdge.Center, index: 2);

        Assert.Equal(["b", "a"], group.Panels);
        Assert.Equal("a", group.Active);
    }

    [Fact]
    public void Docking_a_lone_panel_beside_its_own_group_changes_nothing()
    {
        var layout = TwoGroups();
        var root = layout.Root;

        layout.Dock("c", layout.GroupOf("c")!, DockEdge.Left);

        Assert.Same(root, layout.Root);
        Assert.Equal(["c"], layout.GroupOf("c")!.Panels);
    }

    [Fact]
    public void Docking_a_groups_only_panel_beside_it_moves_it_there_and_leaves_a_new_focused_panel_when_the_layout_makes_them()
    {
        var layout = TwoGroups();
        layout.NewPanel = () => "new";
        var right = layout.GroupOf("c")!;

        layout.Dock("c", right, DockEdge.Right, 0.4f);

        var split = Assert.IsType<DockSplit>(((DockSplit)layout.Root).Second);
        Assert.Same(right, split.First);
        Assert.Equal(["new"], right.Panels);
        Assert.Equal(["c"], Assert.IsType<DockGroup>(split.Second).Panels);
        Assert.Equal(0.6f, split.Ratio, 3);
        Assert.Equal("new", layout.Focused);
    }

    [Fact]
    public void Moving_the_roots_only_panel_away_leaves_a_new_panel_in_the_root_when_the_layout_makes_them()
    {
        var layout = new DockLayout { Root = DockLayout.Group("a"), NewPanel = () => "new" };

        layout.Float("a", new UiRect(0f, 0f, 200f, 200f));

        Assert.Equal(["new"], Assert.IsType<DockGroup>(layout.Root).Panels);
        Assert.Equal(["a"], layout.GroupOf("a")!.Panels);
    }

    [Fact]
    public void Focus_follows_the_panel_last_activated_docked_or_floated_and_moves_to_the_front_one_when_it_closes()
    {
        var layout = TwoGroups();

        layout.Activate("b");
        Assert.Equal("b", layout.Focused);
        layout.Dock("a", layout.GroupOf("c")!, DockEdge.Center);
        Assert.Equal("a", layout.Focused);
        layout.Float("b", new UiRect(0f, 0f, 200f, 200f));
        Assert.Equal("b", layout.Focused);

        layout.Activate("a");
        layout.Close("a");

        Assert.Equal("c", layout.Focused);
    }

    [Fact]
    public void A_floated_panel_gets_a_window_in_front_and_docking_the_window_back_removes_it()
    {
        var layout = TwoGroups();

        var window = layout.Float("b", new UiRect(10f, 20f, 300f, 200f));
        layout.Float("a", new UiRect(30f, 40f, 300f, 200f));

        Assert.Equal(2, layout.Windows.Count);
        Assert.Same(window, layout.WindowOf(layout.GroupOf("b")!));
        Assert.Equal(["c"], Assert.IsType<DockGroup>(layout.Root).Panels);

        layout.BringToFront(window);
        Assert.Same(window, layout.Windows[^1]);

        layout.Dock(window, layout.Root, DockEdge.Center);
        Assert.Single(layout.Windows);
        Assert.Equal(["c", "b"], Assert.IsType<DockGroup>(layout.Root).Panels);
    }

    [Fact]
    public void Closing_the_last_panel_of_a_window_removes_it_and_raises_PanelClosed()
    {
        var layout = TwoGroups();
        var closed = new List<string>();
        layout.PanelClosed += closed.Add;
        layout.Float("b", new UiRect(0f, 0f, 200f, 200f));

        layout.Close("b");

        Assert.Empty(layout.Windows);
        Assert.Equal(["b"], closed);
        Assert.False(layout.Contains("b"));
    }

    [Fact]
    public void Closing_every_docked_panel_leaves_an_empty_root_group()
    {
        var layout = TwoGroups();

        layout.Close("a");
        layout.Close("b");
        layout.Close("c");

        Assert.Empty(Assert.IsType<DockGroup>(layout.Root).Panels);
    }

    [Fact]
    public void A_layout_has_panels_while_any_group_docked_or_floating_holds_one()
    {
        var layout = TwoGroups();
        layout.Float("b", new UiRect(0f, 0f, 200f, 200f));
        layout.Close("a");
        layout.Close("c");

        Assert.True(layout.HasPanels());

        layout.Close("b");

        Assert.False(layout.HasPanels());
    }

    [Fact]
    public void Retain_drops_unknown_panels_quietly()
    {
        var layout = TwoGroups();
        var closed = new List<string>();
        layout.PanelClosed += closed.Add;

        layout.Retain(p => p != "c");

        Assert.Equal(["a", "b"], Assert.IsType<DockGroup>(layout.Root).Panels);
        Assert.Empty(closed);
    }

    [Fact]
    public void Retain_keeps_a_panel_named_twice_only_where_it_first_shows()
    {
        var layout = new DockLayout
        {
            Root = DockLayout.Split(Direction.Horizontal, 0.5f, DockLayout.Group("a", "b"), DockLayout.Group("b", "c", "b")),
        };

        layout.Retain(_ => true);

        var root = Assert.IsType<DockSplit>(layout.Root);
        Assert.Equal(["a", "b"], Assert.IsType<DockGroup>(root.First).Panels);
        Assert.Equal(["c"], Assert.IsType<DockGroup>(root.Second).Panels);
        Assert.Equal("c", ((DockGroup)root.Second).Active);
    }

    [Fact]
    public void A_layout_round_trips_through_json_with_its_ids()
    {
        var layout = TwoGroups();
        layout.Float("b", new UiRect(10f, 20f, 300f, 200f));
        layout.AssignIds();

        var copy = JsonSerializer.Deserialize<DockLayout>(JsonSerializer.Serialize(layout))!;

        var root = Assert.IsType<DockSplit>(copy.Root);
        Assert.Equal(layout.Root.Id, root.Id);
        Assert.Equal(["a"], Assert.IsType<DockGroup>(root.First).Panels);
        Assert.Equal(new UiRect(10f, 20f, 300f, 200f), copy.Windows.Single().Rect);
        Assert.Equal(layout.NextId, copy.NextId);
    }

    [Fact]
    public void A_copied_arrangement_serializes_as_the_layout_does_and_shares_nothing_with_it()
    {
        var layout = TwoGroups();
        layout.Float("b", new UiRect(10f, 20f, 300f, 200f));
        layout.AssignIds();
        layout.NewPanel = () => "new";
        layout.PanelClosed += _ => { };

        var copy = layout.CopyArrangement();
        layout.GroupOf("a")!.Panels.Add("d");

        Assert.Equal(JsonSerializer.Serialize(TwoGroupsFloated()), JsonSerializer.Serialize(copy));
        Assert.Null(copy.NewPanel);
        Assert.NotSame(layout.Windows[0].Root, copy.Windows[0].Root);
        Assert.Equal(["a"], copy.GroupOf("a")!.Panels);
    }

    private static DockLayout TwoGroupsFloated()
    {
        var layout = TwoGroups();
        layout.Float("b", new UiRect(10f, 20f, 300f, 200f));
        layout.AssignIds();
        return layout;
    }

    [Fact]
    public void Ids_are_given_to_new_nodes_without_reusing_saved_ones()
    {
        var layout = new DockLayout
        {
            Root = DockLayout.Split(Direction.Horizontal, 0.5f, new DockGroup { Id = 7, Panels = { "a" } }, DockLayout.Group("b")),
        };

        layout.AssignIds();

        var root = (DockSplit)layout.Root;
        Assert.Equal(7, root.First.Id);
        Assert.True(root.Id > 7 && root.Second.Id > 7 && root.Id != root.Second.Id);
    }
}
