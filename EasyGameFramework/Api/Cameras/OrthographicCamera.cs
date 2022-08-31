using System.Numerics;

namespace EasyGameFramework.Api.Cameras;

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

    // public static OrthographicCamera FromLRTB(float left, float right, float top, float bottom, float zNearPlane, float zFarPlane)
    // {
    //     var projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(left, right, top, bottom, zNearPlane, zFarPlane);
    //     
    // }
}