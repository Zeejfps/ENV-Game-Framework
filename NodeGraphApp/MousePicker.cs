namespace NodeGraphApp;

public sealed class MousePicker
{
    private readonly Mouse _mouse;
    private readonly Viewport _viewport;
    private readonly NodeGraph _nodeGraph;
    
    public MousePicker(Viewport viewport, Mouse mouse, NodeGraph nodeGraph)
    {
        _mouse = mouse;
        _viewport = viewport;
        _nodeGraph = nodeGraph;
    }
    
    public void Update()
    {
        var scene = _nodeGraph;
        foreach (var node in scene.Nodes.TraverseDepthFirstPostOrder())
        {
        }
    }
}