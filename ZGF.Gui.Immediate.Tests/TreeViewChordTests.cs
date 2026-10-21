using ZGF.Desktop;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class TreeViewChordTests
{
    private sealed class Model : ITreeModel<string>
    {
        public bool HasChildren(string node) => node.Length == 1;
        public IReadOnlyList<string> GetChildren(string node) => [node + "1", node + "2"];
        public TreeRow Row(string node) => new(node);
    }

    private readonly UiFrames _frames = new();
    private readonly Model _model = new();
    private readonly TreeViewState<string> _state = new();
    private readonly string[] _roots = ["A", "B"];

    private void Body(Ui ui)
    {
        using var screen = ui.Screen();
        ui.TreeView("tree", _roots, _model, _state);
    }

    [Theory]
    [InlineData(KeyboardKey.RightArrow, KeyModifiers.Alt)]
    [InlineData(KeyboardKey.DownArrow, KeyModifiers.Control)]
    [InlineData(KeyboardKey.End, KeyModifiers.Super)]
    public void Keys_chorded_with_app_modifiers_are_left_to_the_app(KeyboardKey key, KeyModifiers modifiers)
    {
        _frames.Run(0f, Body);
        _frames.Click(200f, 16f + 11f, Body);
        Assert.Equal("A", _state.Selected);

        _frames.Key(key, modifiers);
        _frames.Run(0f, Body);

        Assert.Equal("A", _state.Selected);
        Assert.False(_state.IsExpanded("A"));
    }

    [Fact]
    public void Shift_does_not_stop_navigation()
    {
        _frames.Run(0f, Body);
        _frames.Click(200f, 16f + 11f, Body);

        _frames.Key(KeyboardKey.DownArrow, KeyModifiers.Shift);
        _frames.Run(0f, Body);

        Assert.Equal("B", _state.Selected);
    }
}
