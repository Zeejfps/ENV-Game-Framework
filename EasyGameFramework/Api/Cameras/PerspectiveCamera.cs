using System.Numerics;

namespace EasyGameFramework.Api.Cameras;

public class PerspectiveCamera : ICamera
{
    private const float DegToRad = 0.0174533f;


    public PerspectiveCamera(float fov, float aspectRatio)
    {
        Transform = new Transform3D();
        Fov = fov;
        ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(Fov * DegToRad, aspectRatio, 0.1f, 500f);
        AspectRatio = aspectRatio;
    }

    public float Fov { get; set; }

    public float AspectRatio { get; }
    public Matrix4x4 ProjectionMatrix { get; }

    public ITransform3D Transform { get; }
    public Vector2 ViewportToWorldPoint(Vector2 point)
    {
        throw new NotImplementedException();
    }

    public void Update()
    {
    }
}