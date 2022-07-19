using System.Numerics;

namespace Framework.Common.Cameras;

public class PerspectiveCamera : ICamera
{
    const float DegToRad = 0.0174533f;

    public Matrix4x4 ProjectionMatrix { get; private set; }
    public float Fov { get; set; }
    
    public ITransform3D Transform { get; }

    
    public PerspectiveCamera(float fov, float aspectRatio)
    {
        Transform = new Transform3D();
        Fov = fov;
        ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(Fov * DegToRad, aspectRatio, 0.1f, 500f);
    }
    
    public void Update()
    {
    }
}