using System.Numerics;

namespace NodeGraphApp;

public static class CoordinateUtils
{
    public static Vector2 NdcToCameraViewPoint(Camera camera, Vector2 ndcPoint)
    {
        Matrix4x4.Invert(camera.ProjectionMatrix, out var invProj);
        var cameraViewPoint = Vector2.Transform(ndcPoint, invProj);
        return new Vector2(cameraViewPoint.X, cameraViewPoint.Y);
    }
}