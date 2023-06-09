using System.Numerics;
using EasyGameFramework.Api.Physics;

namespace EasyGameFramework.Api.Cameras;

public class OrthographicCamera : ICamera
{
    public static OrthographicCamera FromLRBT(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane)
    {
        var projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, zNearPlane, zFarPlane);
        return new OrthographicCamera(projectionMatrix);
    }

    public static OrthographicCamera Create(float width, float height, float zNearPlane, float zFarPlane)
    {
        var matrix = Matrix4x4.CreateOrthographic(width, height, zNearPlane, zFarPlane);
        var rect = new Rect
        {
            Width = width,
            Height = height,
            BottomLeft = new Vector2(width * -0.5f, height * -0.5f)
        };

        var aspectRatio = width / height;
        return new OrthographicCamera(matrix, rect, aspectRatio);
    }

    public float AspectRatio { get; }
    public Matrix4x4 ProjectionMatrix { get; private set; }
    public ITransform3D Transform { get; }
    public Rect Rect { get; }

    private float m_zNearPlane;
    private float m_zFarPlane;

    private OrthographicCamera(Matrix4x4 projectionMatrix, Rect rect, float aspectRatio)
    {
        ProjectionMatrix = projectionMatrix;
        Rect = rect;
        Transform = new Transform3D();
        AspectRatio = aspectRatio;
    }
    
    public OrthographicCamera(float size, float zNearPlane, float zFarPlane)
    {
        m_zNearPlane = zNearPlane;
        m_zFarPlane = zFarPlane;
        ProjectionMatrix = Matrix4x4.CreateOrthographic(size, size, zNearPlane, zFarPlane);
        Transform = new Transform3D();
    }

    private OrthographicCamera(Matrix4x4 projectionMatrix)
    {
        ProjectionMatrix = projectionMatrix;
        Transform = new Transform3D();
    }

    public void SetSize(float width, float height)
    {
        ProjectionMatrix = Matrix4x4.CreateOrthographic(width, height, m_zNearPlane, m_zFarPlane);
    }
}