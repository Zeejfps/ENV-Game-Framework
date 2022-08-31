using System.Numerics;
using EasyGameFramework.Api;

namespace EasyGameFramework;

public class Transform3D : ITransform3D
{
    private const float DegToRad = 0.0174533f;

    private Vector3 m_WorldPosition = Vector3.Zero;
    private Quaternion m_WorldRotation = Quaternion.Identity;

    public Transform3D()
    {
        UpdateWorldMatrix();
    }

    public Vector3 WorldPosition
    {
        get => m_WorldPosition;
        set
        {
            if (m_WorldPosition == value)
                return;
            m_WorldPosition = value;
            UpdateWorldMatrix();
        }
    }

    public Quaternion WorldRotation
    {
        get => m_WorldRotation;
        set
        {
            if (m_WorldRotation == value)
                return;
            m_WorldRotation = value;
            UpdateWorldMatrix();
        }
    }

    public Vector3 Right => Vector3.Transform(Vector3.UnitX, WorldRotation);
    public Vector3 Forward => Vector3.Transform(-Vector3.UnitZ, WorldRotation);
    public Vector3 Up => Vector3.Transform(Vector3.UnitY, WorldRotation);

    public Matrix4x4 WorldMatrix { get; private set; }

    public void LookAt(Vector3 target, Vector3 up)
    {
        Matrix4x4.Invert(Matrix4x4.CreateLookAt(WorldPosition, target, up), out var lookAt);
        WorldMatrix = lookAt;
        Matrix4x4.Decompose(WorldMatrix, out _, out m_WorldRotation, out m_WorldPosition);
    }

    public void RotateInWorldSpace(float x, float y, float z)
    {
        var xRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, DegToRad * x);
        var yRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, DegToRad * y);
        var zRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, DegToRad * z);

        var totalRotation = xRotation * yRotation * zRotation;
        WorldRotation *= totalRotation;
    }

    public void RotateInLocalSpace(float x, float y, float z)
    {
        var xRotation = Quaternion.CreateFromAxisAngle(Right, DegToRad * x);
        var yRotation = Quaternion.CreateFromAxisAngle(Up, DegToRad * y);
        var zRotation = Quaternion.CreateFromAxisAngle(Forward, DegToRad * z);

        var totalRotation = xRotation * yRotation * zRotation;
        WorldRotation *= totalRotation;
    }

    public void RotateAround(Vector3 point, Vector3 axis, float angle)
    {
        var position = WorldPosition;
        var vector3 = Vector3.Transform(position - point, Quaternion.CreateFromAxisAngle(axis, angle));
        WorldPosition = point + vector3;
    }

    public void TranslateInLocalSpace(float deltaX, float deltaY, float deltaZ)
    {
        WorldPosition += Right * deltaX;
    }

    private void UpdatePositionAndRotation(Vector3 worldPosition, Quaternion worldRotation)
    {
        m_WorldPosition = worldPosition;
        m_WorldRotation = worldRotation;
        UpdateWorldMatrix();
    }

    private void UpdateWorldMatrix()
    {
        WorldMatrix = Matrix4x4.CreateWorld(WorldPosition, Forward, Up);
    }
}