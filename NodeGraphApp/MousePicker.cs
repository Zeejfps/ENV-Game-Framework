using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace NodeGraphApp;

public sealed class MousePicker
{
    private readonly NodeGraph _nodeGraph;
    
    public VisualNode? HoveredNode { get; private set; }
    public Mouse Mouse { get; }
    public Viewport Viewport { get; }
    public Vector2 MouseWorldPosition => Viewport.ScreenToWorldPoint(Mouse.Position);

    public MousePicker(Viewport viewport, Mouse mouse, NodeGraph nodeGraph)
    {
        Mouse = mouse;
        Viewport = viewport;
        _nodeGraph = nodeGraph;
    }
    
    public void Update()
    {
        var mousePosition = Mouse.Position;
        var worldPosition = Viewport.ScreenToWorldPoint(mousePosition);
        var scene = _nodeGraph;
        VisualNode? hoveredNode = null;
        foreach (var node in scene.Nodes.TraverseDepthFirstPostOrder())
        {
            var bounds = node.Bounds;
            if (bounds.Contains(worldPosition))
            {
                hoveredNode = node;
                break;
            }
        }

        HoveredNode = hoveredNode;

        var links = _nodeGraph.BackgroundLinks.GetAll();
        foreach (var link in links)
        {
            var left = MathF.Min(link.StartPosition.X, link.EndPosition.X);
            var bottom = MathF.Max(link.StartPosition.Y, link.EndPosition.Y);
            var right = MathF.Max(link.StartPosition.X, link.EndPosition.X);
            var top = MathF.Max(link.StartPosition.Y, link.EndPosition.Y);
            var bounds = ScreenRect.FromLeftBottomTopRight(left, bottom, right, top);
            if (bounds.Contains(worldPosition))
            {
                var p0 = link.StartPosition;
                var p1 = link.StartPosition + new Vector2(20f, 0f);
                var p2 = link.StartPosition - new Vector2(20f, 0f);
                var p3 = link.EndPosition;
                if (BezierUtils.IsPointOverBezier(worldPosition, p0, p1, p2, p3))
                {
                    Console.WriteLine("Over Link");
                    break;
                }
            }
        }
    }

    public bool TryPick<T>([NotNullWhen(true)] out T? result, bool includeChildren = true) where T : VisualNode
    {
        if (HoveredNode == null)
        {
            result = null;
            return false;
        }

        result = HoveredNode as T;
        if (result != null)
            return true;

        if (includeChildren && HoveredNode.ChildOf<T>(out result))
            return true;

        return false;
    }

    public bool TryPick([NotNullWhen(true)] out Link? pickedLink)
    {
        pickedLink = null;
        return false;
    }
}