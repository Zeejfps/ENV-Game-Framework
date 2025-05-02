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
        var scene = _nodeGraph;
        VisualNode? hoveredNode = null;
        foreach (var node in scene.Nodes.TraverseDepthFirstPostOrder())
        {
            var mousePosition = Mouse.Position;
            var worldPosition = Viewport.ScreenToWorldPoint(mousePosition);
            var bounds = node.Bounds;
            if (bounds.Contains(worldPosition))
            {
                hoveredNode = node;
                break;
            }
        }

        HoveredNode = hoveredNode;
    }

    public bool TryPick<T>([NotNullWhen(true)] out T? result, bool includeChildren = true) where T : VisualNode
    {
        var scene = _nodeGraph;
        foreach (var node in scene.Nodes.TraverseDepthFirstPostOrder())
        {
            var mousePosition = Mouse.Position;
            var worldPosition = Viewport.ScreenToWorldPoint(mousePosition);
            var bounds = node.Bounds;
            if (bounds.Contains(worldPosition))
            {
                result = node as T;
                if (result != null)
                    return true;

                if (includeChildren && node.ChildOf<T>(out result))
                    return true;

                return false;
            }
        }

        result = null;
        return false;
    }
}