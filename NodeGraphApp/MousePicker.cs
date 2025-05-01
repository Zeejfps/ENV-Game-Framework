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
}