using System.Numerics;
using GLFW;

namespace NodeGraphApp;

public static class CoordinateUtils
{
    public static Vector4 ScreenToNdcPoint(Window window, Vector2 screenPoint)
    {
        Glfw.GetWindowSize(window, out var windowWidth, out var windowHeight);
        return new Vector4
        {
            X = (screenPoint.X / windowWidth) * 2f - 1f,
            Y = 1f - (screenPoint.Y / windowHeight) * 2f,
            Z = 0,
            W = 0
        };
    }

    public static Vector2 NdcToCameraViewPoint(Camera camera, Vector4 ndcPoint)
    {
        Matrix4x4.Invert(camera.ProjectionMatrix, out var invProj);
        var cameraViewPoint = Vector4.Transform(ndcPoint, invProj);
        return new Vector2(cameraViewPoint.X, cameraViewPoint.Y);
    }

    public static Vector2 WindowToCameraViewPoint(Window window, Camera camera, Vector2 screenPoint)
    {
        var ndcCoords = ScreenToNdcPoint(window, screenPoint);
        return NdcToCameraViewPoint(camera, ndcCoords);
    }

    public static Vector2 WindowToWorldPoint(Window window, Camera camera, Vector2 screenPoint)
    {
        var cameraViewPoint = WindowToCameraViewPoint(window, camera, screenPoint);
        return cameraViewPoint + camera.Position;
    }
}