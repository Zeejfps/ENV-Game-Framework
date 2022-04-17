using System.Numerics;

namespace Framework.Common.Cameras;

public class OrthographicCamera : ICamera
{
    public Matrix4x4 ProjectionMatrix { get; }
    public ITransform Transform { get; }

    public OrthographicCamera(float width, float height, float zNearPlane, float zFarPlane)
    {
        ProjectionMatrix = Matrix4x4.CreateOrthographic(width, height, zNearPlane, zFarPlane);
        Transform = new Transform3D();
    }

    public void Update()
    {
        
    }
}