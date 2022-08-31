using System.Numerics;
using EasyGameFramework.Api;

namespace EasyGameFramework.Cameras;

public class OrthographicCamera : ICamera
{
    public OrthographicCamera(float width, float height, float zNearPlane, float zFarPlane)
    {
        ProjectionMatrix = Matrix4x4.CreateOrthographic(width, height, zNearPlane, zFarPlane);
        Transform = new Transform3D();
    }

    public Matrix4x4 ProjectionMatrix { get; }
    public ITransform3D Transform { get; }

    public void Update()
    {
    }
}