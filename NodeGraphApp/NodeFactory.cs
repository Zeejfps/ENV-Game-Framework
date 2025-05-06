using System.Numerics;

namespace NodeGraphApp;

public sealed class NodeFactory
{
    private readonly NodeGraph _nodeGraph;

    public NodeFactory(NodeGraph nodeGraph)
    {
        _nodeGraph = nodeGraph;
    }

    public void CreateNodeAtPosition(Vector2 mousePos)
    {
        var node = new Node
        {
            Title = "New Node",
            Bounds = RectF.FromLBWH(mousePos.X, mousePos.Y, 50, 0),
        };
        node.AddInputPort();
        node.AddInputPort();
        node.AddOutputPort();
        node.AddOutputPort();
        node.Update();
        _nodeGraph.Nodes.Add(node);
    }
}