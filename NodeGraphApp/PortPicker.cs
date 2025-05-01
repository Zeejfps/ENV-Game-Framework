using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace NodeGraphApp;

public sealed class PortPicker
{
    private readonly Viewport _viewport;
    private readonly NodeGraph _nodeGraph;

    public PortPicker(Viewport viewport, NodeGraph nodeGraph)
    {
        _viewport = viewport;
        _nodeGraph = nodeGraph;
    }

    public bool TryPickInputPort(Vector2 screenPoint, [NotNullWhen(true)] out InputPort? inputPort)
    {
        var worldCursorPos = _viewport.ScreenToWorldPoint(screenPoint);
        var nodes = _nodeGraph.Nodes.GetAll().Reverse();

        foreach (var node in nodes)
        {
            foreach (var port in node.InputPorts)
            {
                if (Overlaps(worldCursorPos, port.VisualNode.Bounds))
                {
                    inputPort = port;
                    return true;
                }

                if (Overlaps(worldCursorPos, port.Socket.Bounds))
                {
                    inputPort = port;
                    return true;
                }
            }
        }

        inputPort = null;
        return false;
    }

    private bool Overlaps(Vector2 worldCursorPos, ScreenRect bounds)
    {
        if (bounds.Right < worldCursorPos.X)
            return false;
        if (bounds.Top < worldCursorPos.Y)
            return false;
        if (bounds.Left > worldCursorPos.X)
            return false;
        if (bounds.Bottom > worldCursorPos.Y)
            return false;

        return true;
    }
}