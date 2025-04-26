using System.Numerics;
using GLFW;

namespace NodeGraphApp;

public sealed class NodeSelectionController
{
    private readonly Window _window;
    private readonly Mouse _mouse;
    private readonly Camera _camera;
    private readonly NodeGraph _nodeGraph;

    public NodeSelectionController(
        Window window,
        Mouse mouse,
        Camera camera,
        NodeGraph nodeGraph)
    {
        _window = window;
        _mouse = mouse;
        _camera = camera;
        _nodeGraph = nodeGraph;
    }

    public void Update()
    {
        var window = _window;
        var mouse = _mouse;
        var camera = _camera;
        if (mouse.WasButtonPressedThisFrame(MouseButton.Left))
        {
            var mousePos = mouse.Position;
            Matrix4x4.Invert(camera.ProjectionMatrix, out var invProj);

            Glfw.GetWindowSize(window, out var windowWidth, out var windowHeight);
            var ndcCoords = new Vector4
            {
                X = mousePos.X / windowWidth * 2f - 1f,
                Y = 1f - (mousePos.Y / windowHeight) * 2f,
                Z = 0,
                W = 0
            };
            var worldNdc = Vector4.Transform(ndcCoords, invProj);
            var worldCursorPos = new Vector2(worldNdc.X, worldNdc.Y) + camera.Position;
            var nodes = _nodeGraph.Nodes.GetAll();
            foreach (var node in nodes)
            {
                if (node.XPos + node.Width < worldCursorPos.X)
                    continue;
                if (node.YPos + node.Height < worldCursorPos.Y)
                    continue;
                if (node.XPos > worldCursorPos.X)
                    continue;
                if (node.YPos > worldCursorPos.Y)
                    continue;
                
                Console.WriteLine($"Hit node");
            }
        }
    }
}