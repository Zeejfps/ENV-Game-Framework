using System.Numerics;

namespace Framework;

public interface ICamera
{
    Matrix4x4 ProjectionMatrix { get; }
    ITransform Transform { get; }
}