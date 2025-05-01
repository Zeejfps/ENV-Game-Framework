namespace NodeGraphApp;

public sealed class MousePicker
{
    private readonly Mouse _mouse;
    private readonly Viewport _viewport;
    private readonly NodeGraph _nodeGraph;
    
    public VisualNode? HoveredNode { get; private set; }
    
    public MousePicker(Viewport viewport, Mouse mouse, NodeGraph nodeGraph)
    {
        _mouse = mouse;
        _viewport = viewport;
        _nodeGraph = nodeGraph;
    }
    
    public void Update()
    {
        var scene = _nodeGraph;
        VisualNode? hoveredNode = null;
        foreach (var node in scene.Nodes.TraverseDepthFirstPostOrder())
        {
            var mousePosition = _mouse.Position;
            var worldPosition = _viewport.ScreenToWorldPoint(mousePosition);
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