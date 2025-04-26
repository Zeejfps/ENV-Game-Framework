using static GL46;

public sealed class OpenGlNodeGraphRenderer
{
    private readonly NodeGraph _nodeGraph;

    public OpenGlNodeGraphRenderer(NodeGraph nodeGraph)
    {
        _nodeGraph = nodeGraph;
    }

    public void Setup()
    {
        glClearColor(0f, 0f, 0f, 1f);
    }

    public void Render()
    {
        glClear(GL_COLOR_BUFFER_BIT);

        var nodeGraph = _nodeGraph;
        var nodes = nodeGraph.Nodes.GetAll();
        foreach (var node in nodes)
        {
            RenderNode(node);
        }
    }

    private void RenderNode(Node node)
    {
        
    }

    public void Teardown()
    {

    }
}