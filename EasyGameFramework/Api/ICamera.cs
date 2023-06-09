using System.Numerics;

namespace EasyGameFramework.Api;

public interface ICamera
{
    float AspectRatio { get; }
    Matrix4x4 ProjectionMatrix { get; }
    ITransform3D Transform { get; }
}