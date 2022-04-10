using System.Numerics;

namespace Framework;

public interface ITransform
{
    Vector3 WorldPosition { get; set; }
    Quaternion WorldRotation { get; set; }
    Matrix4x4 WorldMatrix { get; }
    
    Vector3 Up { get; }
    Vector3 Right { get; }
    Vector3 Forward { get; }

    void LookAt(Vector3 target, Vector3 up);
    void RotateInWorldSpace(float x, float y, float z);
    void RotateInLocalSpace(float x, float y, float z);
}