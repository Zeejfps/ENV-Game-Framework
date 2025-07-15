using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class FakeCanvas : ICanvas
{
    private VisualTree _prevVisualTree = new();
    private VisualTree _currVisualTree = new();

    public void BeginFrame()
    {
        var tree = _prevVisualTree;
        _currVisualTree = _prevVisualTree;
        _prevVisualTree = tree;
        _currVisualTree.Clear();
    }

    public void DrawRect(RectF position, RectStyle style)
    {
        _prevVisualTree.AddRect(position, style);
    }

    public void DrawText(RectF position, string text, TextStyle style)
    {
        _prevVisualTree.AddText(position, text, style);
    }

    public void EndFrame()
    {
        foreach (var layer in _currVisualTree.Layers)
        {

        }
    }
}