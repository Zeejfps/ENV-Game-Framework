using System.Numerics;

namespace Framework;

public interface ICamera : ISceneObject
{
    Matrix4x4 ProjectionMatrix { get; }
    ITransform Transform { get; }
}