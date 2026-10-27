using ZGF.Desktop;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class TreeViewTests
{
    private static readonly float RowHeight = UiTheme.Dark.RowHeight;

    private sealed class Node(string name, params Node[] children)
    {
        public string Name { get; } = name;
        public List<Node> Children { get; } = children.ToList();

        public override string ToString() => Name;
    }

    private sealed class Model : ITreeModel<Node>
    {
        public int RowCalls;
        public int ChildCalls;
        public uint IconBackground;
        public bool Spinning;

        public bool HasChildren(Node node) => node.Children.Count > 0;

        public IReadOnlyList<Node> GetChildren(Node node)
        {
            ChildCalls++;
            return node.Children;
        }

        public TreeRow Row(Node node)
        {
            RowCalls++;
            return new TreeRow(node.Name, new UiIcon(IconGlyph.FromFont("#", "Icons"), 0xFF00FF00, IconBackground), Spinning: Spinning);
        }
    }

    private sealed class Tree
    {
        public readonly UiFrames Frames;
        public readonly Model Model = new();
        public readonly TreeViewState<Node> State = new(ReferenceEqualityComparer.Instance);
        public readonly List<Node> Roots;
        public TreeViewResult<Node> Last;
        public bool ToggleOnActivate = true;
        public readonly List<TreeViewResult<Node>> Results = new();

        public Tree(params Node[] roots)
        {
            Roots = roots.ToList();
            Frames = new UiFrames(new DefaultUiState(), body: Body) { FrameTime = 1f / 60f };
            Frame();
        }

        private void Body(Ui ui)
        {
            using var screen = ui.Screen();
            Last = ui.TreeView("tree", Roots, Model, State, toggleOnActivate: ToggleOnActivate);
            Results.Add(Last);
        }

        public TreeViewResult<Node> Frame()
        {
            Frames.Frame();
            return Last;
        }

        public void Click(int row, float x = 200f, PointerButton button = PointerButton.Left) =>
            Frames.Click(x, 16f + row * RowHeight + RowHeight / 2, button);

        public void ClickArrow(int row, int depth) => Click(row, 28f + depth * 16f);

        public void Key(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None) => Frames.Key(key, modifiers).Frame();

        public string Selected => State.HasSelection ? State.Selected!.Name : "";
    }

    private static Node[] Sample() =>
    [
        new("A", new Node("A1", new Node("A1a")), new Node("A2")),
        new("B", new Node("B1")),
        new("C"),
    ];

    [Fact]
    public void A_spinning_row_shows_the_spinner_in_its_icons_color_and_asks_for_the_frames_that_turn_it()
    {
        var tree = new Tree(new Node("a"));
        tree.Model.Spinning = true;

        tree.Frame();

        var dots = tree.Frames.Canvas.Circles;
        Assert.NotEmpty(dots);
        // Each dot fades from the icon's green toward the background by its distance from the
        // turning head, so all stay green rather than the theme's spinner color.
        Assert.Empty(dots.Select(d => d.Inputs.Color).Where(c => !IsGreenest(c)).Select(c => c.ToString("X8")));
        Assert.DoesNotContain(tree.Frames.Canvas.Texts, t => t.Inputs.Text == "#");
        Assert.True(double.IsFinite(tree.Frames.WakeAt), "the spinner asked for no next frame");
    }

    private static bool IsGreenest(uint argb)
    {
        var red = (argb >> 16) & 0xFF;
        var green = (argb >> 8) & 0xFF;
        var blue = argb & 0xFF;
        return green > red && green > blue;
    }

    [Fact]
    public void A_row_at_rest_shows_its_icon_and_asks_for_no_frames()
    {
        var tree = new Tree(new Node("a"));

        tree.Frame();

        Assert.Empty(tree.Frames.Canvas.Circles);
        Assert.True(double.IsPositiveInfinity(tree.Frames.WakeAt));
    }

    [Fact]
    public void Click_selects_a_row_and_reports_the_change()
    {
        var tree = new Tree(Sample());

        tree.Click(1);

        Assert.Equal("B", tree.Selected);
        Assert.Contains(tree.Results, r => r.SelectionChanged && r.Node!.Name == "B");
        var icon = tree.Frames.Canvas.Texts.First(t => t.Inputs.Text == "#").Inputs.Style;
        Assert.Equal(0xFF00FF00u, icon.TextColor.Value);
        Assert.Equal("Icons", icon.FontFamily.Value);
    }

    [Fact]
    public void The_disclosure_arrow_toggles_without_selecting()
    {
        var tree = new Tree(Sample());

        tree.ClickArrow(0, 0);
        tree.Frame();

        Assert.True(tree.State.IsExpanded(tree.Roots[0]));
        Assert.Equal(5, tree.State.VisibleCount);
        Assert.False(tree.State.HasSelection);
        Assert.True(tree.Frames.Drew("A2"));

        tree.ClickArrow(0, 0);
        tree.Frame();
        Assert.Equal(3, tree.State.VisibleCount);
    }

    [Fact]
    public void Double_click_activates_and_toggles()
    {
        var tree = new Tree(Sample());
        tree.Click(0);
        tree.Results.Clear();

        tree.Frames.Press().Frame();
        tree.Frames.Release().Frame();

        Assert.Contains(tree.Results, r => r.Activated && r.Node!.Name == "A");
        Assert.True(tree.State.IsExpanded(tree.Roots[0]));
    }

    [Fact]
    public void Keys_move_expand_collapse_and_activate_while_focused()
    {
        var tree = new Tree(Sample());
        tree.Click(0);

        tree.Key(KeyboardKey.RightArrow);
        Assert.True(tree.State.IsExpanded(tree.Roots[0]));
        tree.Key(KeyboardKey.RightArrow);
        Assert.Equal("A1", tree.Selected);
        tree.Key(KeyboardKey.DownArrow);
        Assert.Equal("A2", tree.Selected);
        tree.Key(KeyboardKey.LeftArrow);
        Assert.Equal("A", tree.Selected);
        tree.Key(KeyboardKey.LeftArrow);
        Assert.False(tree.State.IsExpanded(tree.Roots[0]));
        tree.Key(KeyboardKey.End);
        Assert.Equal("C", tree.Selected);
        tree.Key(KeyboardKey.Home);
        Assert.Equal("A", tree.Selected);
        tree.Key(KeyboardKey.PageDown);
        Assert.Equal("C", tree.Selected);

        tree.Results.Clear();
        tree.Key(KeyboardKey.Enter);
        Assert.Contains(tree.Results, r => r.Activated && r.Node!.Name == "C");
    }

    [Theory]
    [InlineData(KeyboardKey.RightArrow, KeyModifiers.Alt)]
    [InlineData(KeyboardKey.DownArrow, KeyModifiers.Control)]
    [InlineData(KeyboardKey.End, KeyModifiers.Super)]
    public void Keys_chorded_with_app_modifiers_are_left_to_the_app(KeyboardKey key, KeyModifiers modifiers)
    {
        var tree = new Tree(Sample());
        tree.Click(0);
        Assert.Equal("A", tree.Selected);

        tree.Key(key, modifiers);

        Assert.Equal("A", tree.Selected);
        Assert.False(tree.State.IsExpanded(tree.Roots[0]));
    }

    [Fact]
    public void Shift_does_not_stop_navigation()
    {
        var tree = new Tree(Sample());
        tree.Click(0);

        tree.Key(KeyboardKey.DownArrow, KeyModifiers.Shift);

        Assert.Equal("B", tree.Selected);
    }

    [Fact]
    public void Keys_do_nothing_without_focus()
    {
        var tree = new Tree(Sample());

        tree.Key(KeyboardKey.DownArrow);

        Assert.False(tree.State.HasSelection);
    }

    [Fact]
    public void Collapsing_above_the_selection_hides_it_until_it_shows_again()
    {
        var tree = new Tree(Sample());
        tree.State.Reveal([tree.Roots[0], tree.Roots[0].Children[0], tree.Roots[0].Children[0].Children[0]]);
        tree.Frame();
        Assert.Equal("A1a", tree.Selected);

        tree.ClickArrow(0, 0);
        var hidden = tree.State.SelectedIndex;
        tree.ClickArrow(0, 0);

        Assert.Equal(-1, hidden);
        Assert.Equal("A1a", tree.Selected);
        Assert.Equal(2, tree.State.SelectedIndex);
        Assert.DoesNotContain(tree.Results, r => r.SelectionChanged || r.SelectionMoved);
    }

    [Fact]
    public void Right_click_selects_and_asks_for_a_context_menu_at_the_pointer()
    {
        var tree = new Tree(Sample());

        tree.Click(2, 120f, PointerButton.Right);

        var request = Assert.Single(tree.Results, r => r.ContextMenuRequested);
        Assert.Equal("C", request.Node!.Name);
        Assert.Equal(120f, request.ContextMenuAt.X);
        Assert.Equal("C", tree.Selected);
    }

    [Fact]
    public void The_menu_key_asks_for_a_context_menu_under_the_selected_row()
    {
        var tree = new Tree(Sample());
        tree.Click(1);
        tree.Results.Clear();

        tree.Key(KeyboardKey.F10, KeyModifiers.Shift);

        var request = Assert.Single(tree.Results, r => r.ContextMenuRequested);
        Assert.Equal(16f + 2 * RowHeight, request.ContextMenuAt.Y);
    }

    [Fact]
    public void Reveal_expands_the_path_and_scrolls_its_node_into_view()
    {
        var big = new Node("big", Enumerable.Range(0, 1000).Select(i => new Node($"n{i}")).ToArray());
        var tree = new Tree(big);

        tree.State.Reveal([big, big.Children[700]]);
        tree.Frames.Frame(3);

        Assert.Equal("n700", tree.Selected);
        Assert.True(tree.Frames.Drew("n700"));
        Assert.False(tree.Frames.Drew("n0"));
    }

    [Fact]
    public void A_hundred_thousand_rows_cost_only_the_rows_in_view()
    {
        var big = new Node("big", Enumerable.Range(0, 100_000).Select(i => new Node($"n{i}")).ToArray());
        var tree = new Tree(big);
        tree.State.SetExpanded(big, true);
        tree.Frames.Frame(2);

        tree.Model.RowCalls = 0;
        tree.Model.ChildCalls = 0;
        tree.Frame();

        Assert.Equal(100_001, tree.State.VisibleCount);
        Assert.InRange(tree.Model.RowCalls, 1, 20);
        Assert.Equal(0, tree.Model.ChildCalls);

        tree.Click(0);
        tree.Key(KeyboardKey.End);
        tree.Frames.Frame(2);
        Assert.Equal("n99999", tree.Selected);
        Assert.True(tree.Frames.Drew("n99999"));
    }

    [Fact]
    public void Expansion_follows_node_identity_when_roots_change()
    {
        var sample = Sample();
        var tree = new Tree(sample);
        tree.State.SetExpanded(sample[1], true);
        tree.Frame();
        Assert.Equal(4, tree.State.VisibleCount);

        tree.Roots.RemoveAt(0);
        tree.Frame();

        Assert.Equal(3, tree.State.VisibleCount);
        Assert.True(tree.Frames.Drew("B1"));
    }

    [Fact]
    public void Collapsing_from_the_app_keeps_the_selection_hidden_where_it_was()
    {
        var tree = new Tree(Sample());
        var a = tree.Roots[0];
        tree.State.Reveal([a, a.Children[0], a.Children[0].Children[0]]);
        tree.Frame();
        tree.Results.Clear();

        tree.State.SetExpanded(a.Children[0], false);
        var result = tree.Frame();
        tree.State.CollapseAll();
        tree.Frame();

        Assert.False(result.SelectionMoved);
        Assert.False(result.SelectionChanged);
        Assert.Equal("A1a", tree.Selected);
        Assert.Equal(-1, tree.State.SelectedIndex);
    }

    [Fact]
    public void Removing_the_selected_node_moves_the_selection_to_its_parent_or_clears_it()
    {
        var tree = new Tree(Sample());
        var b = tree.Roots[1];
        tree.State.Reveal([b, b.Children[0]]);
        tree.Frame();

        b.Children.Clear();
        tree.State.Refresh();
        Assert.True(tree.Frame().SelectionMoved);
        Assert.Equal("B", tree.Selected);

        tree.Roots.Remove(b);
        var result = tree.Frame();
        Assert.True(result.SelectionMoved);
        Assert.False(tree.State.HasSelection);
    }

    [Fact]
    public void Revealing_a_showing_node_under_a_parent_still_loading_keeps_it_selected_until_it_shows_there()
    {
        var node = new Node("moved");
        var loading = new Node("loading");
        var parent = new Node("parent", loading);
        var root = new Node("root", node);
        var tree = new Tree(root);
        tree.State.Reveal([root, node]);
        tree.Frame();
        tree.Results.Clear();

        root.Children[0] = parent;
        tree.State.Refresh();
        tree.State.Reveal([root, parent, node]);
        var whileLoading = tree.Frame();
        parent.Children[0] = node;
        tree.State.Refresh();
        tree.Frame();

        Assert.False(whileLoading.SelectionMoved);
        Assert.DoesNotContain(tree.Results, r => r.SelectionMoved);
        Assert.Equal("moved", tree.Selected);
        Assert.Equal(2, tree.State.SelectedIndex);
    }

    [Fact]
    public void Selecting_a_hidden_node_scrolls_to_it_once_its_parent_expands()
    {
        var children = Enumerable.Range(0, 100).Select(i => new Node($"child {i}")).ToArray();
        var tree = new Tree(new Node("root", children));

        tree.State.Select(children[80]);
        tree.Frames.Frame(2);
        Assert.False(tree.Frames.Drew("child 80"));

        tree.State.SetExpanded(tree.Roots[0], true);
        tree.Frames.Frame(3);

        Assert.True(tree.Frames.Drew("child 80"));
        Assert.Equal("child 80", tree.Selected);
    }

    [Fact]
    public void A_hidden_selection_is_not_searched_for_every_frame()
    {
        var comparer = new CountingComparer();
        var state = new TreeViewState<int>(comparer);
        var roots = Enumerable.Range(0, 10_000).ToArray();
        var model = new FlatModel();
        var frames = new UiFrames(new DefaultUiState(), body: ui => ui.TreeView("tree", roots, model, state)).Frame();

        state.Select(-1);
        frames.Frame(2);
        comparer.Calls = 0;
        frames.Frame();

        Assert.True(comparer.Calls < 1_000, $"{comparer.Calls} comparisons");
    }

    [Fact]
    public void Toggling_reports_the_toggled_node()
    {
        var tree = new Tree(Sample());

        tree.ClickArrow(1, 0);

        var toggled = Assert.Single(tree.Results, r => r.Toggled);
        Assert.Equal("B", toggled.ToggledNode!.Name);
        Assert.False(toggled.SelectionChanged);
    }

    [Fact]
    public void Double_click_can_activate_without_toggling()
    {
        var tree = new Tree(Sample()) { ToggleOnActivate = false };
        tree.Click(0);
        tree.Results.Clear();

        tree.Frames.Press().Frame();
        tree.Frames.Release().Frame();

        Assert.Contains(tree.Results, r => r.Activated && r.Node!.Name == "A");
        Assert.False(tree.State.IsExpanded(tree.Roots[0]));
        Assert.DoesNotContain(tree.Results, r => r.Toggled);
    }

    [Fact]
    public void An_icon_background_draws_a_badge_behind_the_icon()
    {
        var tree = new Tree(Sample());
        tree.Model.IconBackground = 0xFF123456;

        tree.Frame();

        Assert.Contains(tree.Frames.Canvas.Rects, r => r.Inputs.Style.BackgroundColor == 0xFF123456u);
    }

    private sealed class CountingComparer : IEqualityComparer<int>
    {
        public int Calls;

        public bool Equals(int x, int y)
        {
            Calls++;
            return x == y;
        }

        public int GetHashCode(int obj) => obj;
    }

    private sealed class FlatModel : ITreeModel<int>
    {
        public bool HasChildren(int node) => false;
        public IReadOnlyList<int> GetChildren(int node) => [];
        public TreeRow Row(int node) => new("row");
    }
}
