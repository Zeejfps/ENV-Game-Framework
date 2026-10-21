using ZGF.Desktop;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class TreeViewTests
{
    private const float RowHeight = 22f;

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

        public bool HasChildren(Node node) => node.Children.Count > 0;

        public IReadOnlyList<Node> GetChildren(Node node)
        {
            ChildCalls++;
            return node.Children;
        }

        public TreeRow Row(Node node)
        {
            RowCalls++;
            return new TreeRow(node.Name, Icon: "#", IconColor: 0xFF00FF00, IconFamily: "Icons");
        }
    }

    private sealed class Tree
    {
        public readonly UiFrames Frames = new();
        public readonly Model Model = new();
        public readonly TreeViewState<Node> State = new(ReferenceEqualityComparer.Instance);
        public readonly List<Node> Roots;
        public TreeViewResult<Node> Last;
        public readonly List<TreeViewResult<Node>> Results = new();

        public Tree(params Node[] roots)
        {
            Roots = roots.ToList();
            Frame();
        }

        public void Body(Ui ui)
        {
            Last = ui.TreeView("tree", Roots, Model, State);
            Results.Add(Last);
        }

        public TreeViewResult<Node> Frame()
        {
            Frames.Run(1f / 60f, Body);
            return Last;
        }

        public void Click(int row, float x = 200f, PointerButton button = PointerButton.Left) =>
            Frames.Click(x, 16f + row * RowHeight + RowHeight / 2, Body, button);

        public void ClickArrow(int row, int depth) => Click(row, 28f + depth * 16f);

        public void Key(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None)
        {
            Frames.Key(key, modifiers);
            Frame();
        }

        public string Selected => State.HasSelection ? State.Selected!.Name : "";
    }

    private static Node[] Sample() =>
    [
        new("A", new Node("A1", new Node("A1a")), new Node("A2")),
        new("B", new Node("B1")),
        new("C"),
    ];

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

        tree.Frames.Press();
        tree.Frame();
        tree.Frames.Release();
        tree.Frame();

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

    [Fact]
    public void Keys_do_nothing_without_focus()
    {
        var tree = new Tree(Sample());

        tree.Key(KeyboardKey.DownArrow);

        Assert.False(tree.State.HasSelection);
    }

    [Fact]
    public void Collapsing_above_the_selection_moves_it_to_the_collapsed_node()
    {
        var tree = new Tree(Sample());
        tree.State.Reveal([tree.Roots[0], tree.Roots[0].Children[0], tree.Roots[0].Children[0].Children[0]]);
        tree.Frame();
        Assert.Equal("A1a", tree.Selected);

        tree.ClickArrow(0, 0);

        Assert.Equal("A", tree.Selected);
        Assert.Contains(tree.Results, r => r.SelectionChanged);
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
        tree.Frame();
        tree.Frame();
        tree.Frame();

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
        tree.Frame();
        tree.Frame();

        tree.Model.RowCalls = 0;
        tree.Model.ChildCalls = 0;
        tree.Frame();

        Assert.Equal(100_001, tree.State.VisibleCount);
        Assert.InRange(tree.Model.RowCalls, 1, 20);
        Assert.Equal(0, tree.Model.ChildCalls);

        tree.Click(0);
        tree.Key(KeyboardKey.End);
        tree.Frame();
        tree.Frame();
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
}
