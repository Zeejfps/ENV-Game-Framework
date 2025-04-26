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
    }

    public void Teardown()
    {

    }
}