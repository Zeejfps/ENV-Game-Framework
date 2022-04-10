using System.Numerics;

namespace ENV.Engine;

public interface ICamera : ISceneObject
{
    Matrix4x4 ProjectionMatrix { get; }
    ITransform Transform { get; }
}