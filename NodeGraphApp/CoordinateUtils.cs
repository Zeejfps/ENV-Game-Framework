using System.Numerics;
using GLFW;

namespace NodeGraphApp;

public static class CoordinateUtils
{
    public static Vector4 ScreenToNdcPoint(Window window, Camera camera, Vector2 screenPoint)
    {
        Matrix4x4.Invert(camera.ProjectionMatrix, out var invProj);
        Glfw.GetWindowSize(window, out var windowWidth, out var windowHeight);
        return new Vector4
        {
            X = (screenPoint.X / windowWidth) * 2f - 1f,
            Y = 1f - (screenPoint.Y / windowHeight) * 2f,
            Z = 0,
            W = 0
        };
    }

    public static Vector2 ScreenToWorldPoint(Window window, Camera camera, Vector2 screenPoint)
    {
        var ndcCoords = ScreenToNdcPoint(window, camera, screenPoint);
        Matrix4x4.Invert(camera.ProjectionMatrix, out var invProj);
        var worldNdc = Vector4.Transform(ndcCoords, invProj);
        return new Vector2(worldNdc.X, worldNdc.Y) + camera.Position;
    }
}