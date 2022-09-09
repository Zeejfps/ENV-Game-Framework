using System.Numerics;

namespace EasyGameFramework.Api.Cameras;

public class OrthographicCamera : ICamera
{
    public static OrthographicCamera FromLRBT(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane)
    {
        var projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, zNearPlane, zFarPlane);
        return new OrthographicCamera(projectionMatrix);
    }
    
    public Matrix4x4 ProjectionMatrix { get; private set; }
    public ITransform3D Transform { get; }

    private float m_zNearPlane;
    private float m_zFarPlane;
    
    public OrthographicCamera(float width, float height, float zNearPlane, float zFarPlane)
    {
        m_zNearPlane = zNearPlane;
        m_zFarPlane = zFarPlane;
        ProjectionMatrix = Matrix4x4.CreateOrthographic(width, height, zNearPlane, zFarPlane);
        Transform = new Transform3D();
    }

    public void SetSize(float width, float height)
    {
        ProjectionMatrix = Matrix4x4.CreateOrthographic(width, height, m_zNearPlane, m_zFarPlane);
    }
    
    private OrthographicCamera(Matrix4x4 projectionMatrix)
    {
        ProjectionMatrix = projectionMatrix;
        Transform = new Transform3D();
    }
}